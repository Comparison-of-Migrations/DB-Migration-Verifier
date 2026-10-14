using System.Text.Json;
using Verifier.Core;
namespace Verifier.Data;

public sealed partial class MySqlDatabase
{
    // Explicit public columns only. Never SELECT *, authentication_string,
    // password_history, secondary credentials or the whole User_attributes.
    private const string DbPrivileges="Select_priv,Insert_priv,Update_priv,Delete_priv,Create_priv,Drop_priv,Grant_priv,References_priv,Index_priv,Alter_priv,Create_tmp_table_priv,Lock_tables_priv,Create_view_priv,Show_view_priv,Create_routine_priv,Alter_routine_priv,Execute_priv,Event_priv,Trigger_priv";
    private const string GlobalPrivileges=DbPrivileges+",Reload_priv,Shutdown_priv,Process_priv,File_priv,Show_db_priv,Super_priv,Repl_slave_priv,Repl_client_priv,Create_user_priv,Create_tablespace_priv,Create_role_priv,Drop_role_priv";
    private sealed record Principal(string Host,string User);
    private async Task<(List<Table> Tables,CatalogSection Database)> ReadAccess(List<Table> tables,CancellationToken token)
    {
        try
        {
            var configuration=(await ReadNative("SELECT @@GLOBAL.partial_revokes,@@GLOBAL.activate_all_roles_on_login,@@GLOBAL.mandatory_roles,@@GLOBAL.skip_name_resolve,@@GLOBAL.automatic_sp_privileges,@@lower_case_table_names",null,token)).Single();
            var partial=configuration[0]=="1";var own=(await ReadNative("SELECT DATABASE()",null,token)).Single()[0]!;
            var users=await ReadNative("SELECT Host,User,"+GlobalPrivileges+",plugin,account_locked,password_expired,password_lifetime,Password_reuse_history,Password_reuse_time,Password_require_current,ssl_type,HEX(ssl_cipher),HEX(x509_issuer),HEX(x509_subject),max_questions,max_updates,max_connections,max_user_connections,COALESCE(JSON_LENGTH(JSON_REMOVE(User_attributes,'$.Restrictions')),0) FROM mysql.user ORDER BY CAST(Host AS BINARY),CAST(User AS BINARY)",null,token);
            var dynamicGrants=await ReadNative("SELECT HOST,USER,PRIV,WITH_GRANT_OPTION FROM mysql.global_grants ORDER BY CAST(HOST AS BINARY),CAST(USER AS BINARY),CAST(PRIV AS BINARY)",null,token);
            // Let the native server match its database grant patterns. Exact
            // escaped scopes can be mapped to the selected DB namespace. A
            // matching wildcard retains its literal native rule; never rewrite
            // it as an exact namespace or union overlapping privileges.
            var schemas=await ReadNative("SELECT Host,User,Db,"+DbPrivileges+" FROM mysql.db WHERE IF(@@GLOBAL.partial_revokes,CAST(Db AS BINARY)=CAST(DATABASE() AS BINARY),CONVERT(DATABASE() USING utf8mb3) COLLATE utf8mb3_bin LIKE Db ESCAPE 0x5c) ORDER BY CAST(Host AS BINARY),CAST(User AS BINARY),CAST(Db AS BINARY)",null,token);
            const string selected="CAST(Db AS BINARY)=CAST(DATABASE() AS BINARY)";
            var tableGrants=await ReadNative("SELECT Host,User,Table_name,Table_priv FROM mysql.tables_priv WHERE "+selected+" ORDER BY CAST(Host AS BINARY),CAST(User AS BINARY),CAST(Table_name AS BINARY)",null,token);
            var columnGrants=await ReadNative("SELECT Host,User,Table_name,Column_name,Column_priv FROM mysql.columns_priv WHERE "+selected+" ORDER BY CAST(Host AS BINARY),CAST(User AS BINARY),CAST(Table_name AS BINARY),CAST(Column_name AS BINARY)",null,token);
            var routineGrants=await ReadNative("SELECT Host,User,Routine_name,Routine_type,Proc_priv FROM mysql.procs_priv WHERE "+selected+" ORDER BY CAST(Host AS BINARY),CAST(User AS BINARY),CAST(Routine_name AS BINARY),Routine_type",null,token);
            var edges=await ReadNative("SELECT FROM_HOST,FROM_USER,TO_HOST,TO_USER,WITH_ADMIN_OPTION FROM mysql.role_edges ORDER BY CAST(FROM_HOST AS BINARY),CAST(FROM_USER AS BINARY),CAST(TO_HOST AS BINARY),CAST(TO_USER AS BINARY)",null,token);
            var defaults=await ReadNative("SELECT HOST,USER,DEFAULT_ROLE_HOST,DEFAULT_ROLE_USER FROM mysql.default_roles ORDER BY CAST(HOST AS BINARY),CAST(USER AS BINARY),CAST(DEFAULT_ROLE_HOST AS BINARY),CAST(DEFAULT_ROLE_USER AS BINARY)",null,token);
            var proxies=await ReadNative("SELECT Host,User,Proxied_host,Proxied_user,With_grant FROM mysql.proxies_priv ORDER BY CAST(Host AS BINARY),CAST(User AS BINARY),CAST(Proxied_host AS BINARY),CAST(Proxied_user AS BINARY)",null,token);
            // A database restriction contains public privilege names, never
            // credentials. Query only this DB's path, not the account's JSON.
            var restrictions=await ReadNative("SELECT u.Host,u.User,j.Privileges FROM mysql.user u JOIN JSON_TABLE(JSON_EXTRACT(u.User_attributes,'$.Restrictions'),'$[*]' COLUMNS(DatabaseName VARCHAR(64) PATH '$.Database',Privileges JSON PATH '$.Privileges')) j WHERE CAST(j.DatabaseName AS BINARY)=CAST(DATABASE() AS BINARY) ORDER BY CAST(u.Host AS BINARY),CAST(u.User AS BINARY)",null,token);
            var definers=await ReadNative("SELECT DEFINER FROM information_schema.VIEWS WHERE CAST(TABLE_SCHEMA AS BINARY)=CAST(DATABASE() AS BINARY) UNION SELECT DEFINER FROM information_schema.ROUTINES WHERE CAST(ROUTINE_SCHEMA AS BINARY)=CAST(DATABASE() AS BINARY) UNION SELECT DEFINER FROM information_schema.TRIGGERS WHERE CAST(TRIGGER_SCHEMA AS BINARY)=CAST(DATABASE() AS BINARY) UNION SELECT DEFINER FROM information_schema.EVENTS WHERE CAST(EVENT_SCHEMA AS BINARY)=CAST(DATABASE() AS BINARY)",null,token);
            var globalCount=GlobalPrivileges.Split(',').Length;
            var globalSeeds=users.Where(x=>x.Skip(2).Take(globalCount).Contains("Y")||configuration[2]!="").Select(P).Concat(dynamicGrants.Select(P)).Concat(schemas.Select(P)).Concat(restrictions.Select(P)).ToHashSet();
            var definerSeeds=users.Where(u=>definers.Any(d=>d[0]==u[1]+"@"+u[0])).Select(P).ToArray();
            for(var index=0;index<tables.Count;index++)
            {
                var table=tables[index];var scope=Build(table);
                tables[index]=table with{Catalog=table.Catalog.Append(scope).ToArray()};
            }
            return(tables,Build(null));
            CatalogSection Build(Table? table)
            {
                var facts=new List<CatalogFact>();var gaps=new List<CatalogGap>();var principals=new HashSet<Principal>(globalSeeds);
                var tg=tableGrants.Where(x=>table==null||x[2]==table.Name).ToArray();var cg=columnGrants.Where(x=>table==null||x[2]==table.Name).ToArray();
                principals.UnionWith(tg.Select(P));principals.UnionWith(cg.Select(P));
                // Routine privileges/definers are included in database scope;
                // arbitrary routine-to-table binding is not inferred from SQL.
                if(table==null){principals.UnionWith(routineGrants.Select(P));principals.UnionWith(definerSeeds);}
                bool changed;do
                {
                    changed=false;
                    foreach(var e in edges.Concat(defaults))
                    {var a=P(e);var b=new Principal(e[2]!,e[3]!);if(principals.Contains(a)||principals.Contains(b)){changed|=principals.Add(a);changed|=principals.Add(b);}}
                    foreach(var p in proxies)
                    {
                        var owner=P(p);var endpoint=new Principal(p[2]!,p[3]!);
                        if(principals.Contains(owner)||principals.Contains(endpoint))
                        {
                            changed|=principals.Add(owner);
                            // Empty proxy target means any account, not a
                            // missing ''@'' account or a broken role edge.
                            foreach(var u in users.Where(u=>p[2]==""&&p[3]==""||P(u)==endpoint))changed|=principals.Add(P(u));
                        }
                    }
                }while(changed);
                Add("configuration","RoleConfiguration",configuration);
                foreach(var u in users.Where(x=>principals.Contains(P(x))))
                {
                    var key=Key("account",u.Take(2));Add(key,"RelatedRoles",u.Skip(2).Take(globalCount+15));
                    if(u[^1]!="0")gaps.Add(new(key,null,"UninspectedConfiguration","",Reason.NotImplemented));
                }
                foreach(var p in principals)if(!users.Any(u=>P(u)==p))gaps.Add(new(JsonSerializer.Serialize(p),null,"RelatedRoles","",Reason.ObservationChanged));
                foreach(var g in dynamicGrants.Where(x=>principals.Contains(P(x))))Add(Key("dynamic",g.Take(3)),"DatabaseAccess",g[3]);
                foreach(var s in schemas)
                {
                    var key=Key("schema",s.Take(2));
                    if(ExactGrantDatabase(s[2]!,partial)==own)Add(key,"SchemaAccess",s.Skip(3));
                    else Add(Key("schema-pattern",s.Take(3)),"SchemaAccess",new{pattern=s[2],privileges=s.Skip(3).ToArray()});
                }
                // mysql.db does not expose the native ACL cache's historical
                // first-match resolution order. Retain each known definition,
                // but do not certify overlapping rules as effective privileges.
                foreach(var group in schemas.GroupBy(P).Where(g=>g.Count()>1))
                    gaps.Add(new(JsonSerializer.Serialize(group.Key),null,"SchemaGrantPrecedence","",Reason.NotImplemented));
                foreach(var r in restrictions)
                {
                    var key=Key("restriction",r.Take(2));var privileges=CanonicalPrivileges(r[2]);
                    if(privileges==null)gaps.Add(new(key,null,"DatabaseAccess","",Reason.ObservationChanged));
                    else Add(key,"DatabaseAccess",privileges);
                }
                foreach(var e in edges.Where(x=>principals.Contains(P(x))))Add(Key("membership",e.Take(4)),"RelatedRoles",e[4]);
                foreach(var d in defaults.Where(x=>principals.Contains(P(x))))Add(Key("default-role",d),"RoleConfiguration",true);
                foreach(var p in proxies.Where(x=>principals.Contains(P(x))))Add(Key("proxy",p.Take(4)),"RelatedRoles",p[4]);
                foreach(var g in tg)Add(Key("table",g.Take(table==null?3:2)),"TableAccess",g[3]);
                if(table==null)
                {
                    foreach(var g in cg)Add(Key("column",g.Take(4)),"DatabaseColumnAccess",g[4]);
                    foreach(var g in routineGrants)Add(Key("routine",g.Take(4)),"RoutineAccess",g[4]);
                }
                else foreach(var column in table.Columns)
                    facts.Add(new(column.Name,column.Name,"ColumnAccess",JsonSerializer.Serialize(cg.Where(x=>x[3]==column.Name).Select(x=>new[]{x[0],x[1],x[4]}))));
                return new("ACCESS-01",facts,gaps.Count==0?Reason.None:Reason.NotImplemented){Gaps=gaps,CompleteExceptGaps=gaps.Count>0};
                void Add(string key,string field,object? value)=>facts.Add(new(key,null,field,JsonSerializer.Serialize(value)));
            }
        }
        catch(MySqlConnector.MySqlException e) when(e.Number is 1044 or 1142 or 1143 or 1227)
        {
            var unavailable=new CatalogSection("ACCESS-01",[],Reason.PermissionDenied);
            return(tables.Select(t=>t with{Catalog=t.Catalog.Append(unavailable).ToArray()}).ToList(),unavailable);
        }
        static Principal P(string?[] row)=>new(row[0]!,row[1]!);
        static string Key(string kind,IEnumerable<string?> parts)=>JsonSerializer.Serialize(new{kind,parts=parts.ToArray()});
    }
    private static string[]? CanonicalPrivileges(string? json)
    {
        if(json==null)return null;
        using var document=JsonDocument.Parse(json);if(document.RootElement.ValueKind!=JsonValueKind.Array)return null;
        var values=document.RootElement.EnumerateArray().ToArray();if(values.Any(x=>x.ValueKind!=JsonValueKind.String))return null;
        return values.Select(x=>x.GetString()!).Order(StringComparer.Ordinal).ToArray();
    }
    private static string? ExactGrantDatabase(string pattern,bool partial)
    {
        if(partial)return pattern;
        var result=new System.Text.StringBuilder();
        for(var i=0;i<pattern.Length;i++)
        {
            var c=pattern[i];if(c=='\\'){if(++i==pattern.Length)return null;result.Append(pattern[i]);}
            else if(c is '%' or '_')return null;else result.Append(c);
        }
        return result.ToString();
    }
}
