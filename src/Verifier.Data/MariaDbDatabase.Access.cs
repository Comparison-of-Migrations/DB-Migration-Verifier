using System.Text;
using System.Text.Json;
using MySqlConnector;
using Verifier.Core;
namespace Verifier.Data;

public sealed partial class MariaDbDatabase
{
    // MariaDB 11.4 native public grants. Never fetch Priv wholesale, Password,
    // authentication_string, password history, or SHOW CREATE USER output.
    private const string SchemaPrivileges="Select_priv,Insert_priv,Update_priv,Delete_priv,Create_priv,Drop_priv,Grant_priv,References_priv,Index_priv,Alter_priv,Create_tmp_table_priv,Lock_tables_priv,Create_view_priv,Show_view_priv,Create_routine_priv,Alter_routine_priv,Execute_priv,Event_priv,Trigger_priv,Delete_history_priv,Show_create_routine_priv";
    private sealed record AccessPrincipal(string Host,string User);
    private async Task<(List<Table> Tables,CatalogSection Database)> ReadAccess(List<Table> tables,CancellationToken token)
    {
        try
        {
            var configuration=(await Native("SELECT @@GLOBAL.skip_name_resolve,@@GLOBAL.automatic_sp_privileges,@@lower_case_table_names",null,token)).Single();
            var own=(await Native("SELECT DATABASE()",null,token)).Single()[0]!;
            // access is an unsigned 64-bit privilege mask, not the legacy
            // mysql.user view's truncated privilege projection. Keep its native
            // version tag; auth_or projects public plugin names only.
            var unreadable=new Dictionary<string,Reason>(StringComparer.Ordinal);
            var users=await AccessRows("SELECT u.Host,u.User,JSON_VALUE(g.Priv,'$.access'),u.is_role,u.default_role,u.plugin,u.ssl_type,HEX(u.ssl_cipher),HEX(u.x509_issuer),HEX(u.x509_subject),u.max_questions,u.max_updates,u.max_connections,u.max_user_connections,u.max_statement_time,JSON_VALUE(g.Priv,'$.version_id'),JSON_VALUE(g.Priv,'$.account_locked'),JSON_VALUE(g.Priv,'$.password_lifetime') FROM mysql.user u JOIN mysql.global_priv g ON BINARY g.Host=BINARY u.Host AND BINARY g.User=BINARY u.User ORDER BY BINARY u.Host,BINARY u.User","principals");
            var authentication=await AccessRows("SELECT g.Host,g.User,j.ordinal,j.plugin FROM mysql.global_priv g JOIN JSON_TABLE(g.Priv,'$.auth_or[*]' COLUMNS(ordinal FOR ORDINALITY,plugin VARCHAR(128) PATH '$.plugin' NULL ON EMPTY)) j ORDER BY BINARY g.Host,BINARY g.User,j.ordinal","principals");
            var schemas=await AccessRows("SELECT Host,User,Db,"+SchemaPrivileges+" FROM mysql.db WHERE CONVERT(DATABASE() USING utf8mb3) COLLATE utf8mb3_bin LIKE Db ESCAPE 0x5c ORDER BY BINARY Host,BINARY User,BINARY Db","schema");
            const string selected="CAST(Db AS BINARY)=CAST(DATABASE() AS BINARY)";
            var tableGrants=await AccessRows("SELECT Host,User,Table_name,Table_priv FROM mysql.tables_priv WHERE "+selected+" ORDER BY BINARY Host,BINARY User,BINARY Table_name","table");
            var columnGrants=await AccessRows("SELECT Host,User,Table_name,Column_name,Column_priv FROM mysql.columns_priv WHERE "+selected+" ORDER BY BINARY Host,BINARY User,BINARY Table_name,BINARY Column_name","column");
            var routineGrants=await AccessRows("SELECT Host,User,Routine_name,Routine_type,Proc_priv FROM mysql.procs_priv WHERE "+selected+" ORDER BY BINARY Host,BINARY User,BINARY Routine_name,Routine_type","routine");
            var edges=await AccessRows("SELECT Host,User,Role,Admin_option FROM mysql.roles_mapping ORDER BY BINARY Host,BINARY User,BINARY Role","principals");
            var proxies=await AccessRows("SELECT Host,User,Proxied_host,Proxied_user,With_grant FROM mysql.proxies_priv ORDER BY BINARY Host,BINARY User,BINARY Proxied_host,BINARY Proxied_user","principals");
            // Native public metadata only, without executing a view/routine.
            // mysql.proc/event inventory is readable independently of routine
            // body visibility. View/trigger completeness needs separate proof.
            var definers=new List<string?[]>();
            definers.AddRange(await AccessRows("SELECT 'VIEW',TABLE_NAME,DEFINER,NULL FROM information_schema.VIEWS WHERE BINARY TABLE_SCHEMA=BINARY DATABASE() ORDER BY BINARY TABLE_NAME","view-definers"));
            definers.AddRange(await AccessRows("SELECT type,name,definer,NULL FROM mysql.proc WHERE BINARY db=BINARY DATABASE() ORDER BY type,BINARY name","routine-definers"));
            definers.AddRange(await AccessRows("SELECT 'EVENT',name,definer,NULL FROM mysql.event WHERE BINARY db=BINARY DATABASE() ORDER BY BINARY name","event-definers"));
            definers.AddRange(await AccessRows("SELECT 'TRIGGER',TRIGGER_NAME,DEFINER,EVENT_OBJECT_TABLE FROM information_schema.TRIGGERS WHERE BINARY TRIGGER_SCHEMA=BINARY DATABASE() ORDER BY BINARY TRIGGER_NAME","trigger-definers"));
            var visibleViews=await Native("SELECT TABLE_NAME FROM information_schema.TABLES WHERE BINARY TABLE_SCHEMA=BINARY DATABASE() AND TABLE_TYPE='VIEW' ORDER BY BINARY TABLE_NAME",null,token);
            var completeTriggerInventory=await ScopePrivilege("TRIGGER",token);
            var completeViewInventory=await InventoryVisible(token)&&visibleViews.Select(v=>v[0]).ToHashSet(StringComparer.Ordinal).SetEquals(definers.Where(d=>d[0]=="VIEW").Select(d=>d[1]));
            var globalSeeds=users.Where(u=>u[2]!=null&&u[2]!="0").Select(P).Concat(schemas.Select(P)).ToHashSet();
            var result=tables.Select(t=>t with{Catalog=t.Catalog.Append(Build(t)).ToArray()}).ToList();
            return(result,Build(null));

            async Task<List<string?[]>> AccessRows(string sql,string scope)
            {
                try{return await Native(sql,null,token);}catch(MySqlException e) when(e.Number is 1044 or 1142 or 1143 or 1227){unreadable[scope]=Reason.PermissionDenied;return [];}
            }
            CatalogSection Build(Table? table)
            {
                var facts=new List<CatalogFact>();var gaps=new List<CatalogGap>();var principals=new HashSet<AccessPrincipal>(globalSeeds);
                var tg=tableGrants.Where(g=>table==null||g[2]==table.Name).ToArray();var cg=columnGrants.Where(g=>table==null||g[2]==table.Name).ToArray();
                principals.UnionWith(tg.Select(P));principals.UnionWith(cg.Select(P));
                if(table==null)principals.UnionWith(routineGrants.Select(P));
                var ds=definers.Where(d=>table==null||d[0]=="TRIGGER"&&d[3]==table.Name).ToArray();var relatedUnreadable=unreadable.Where(k=>table==null||k.Key is "principals" or "schema" or "table" or "column" or "trigger-definers").ToArray();var definerKnown=completeTriggerInventory&&!unreadable.ContainsKey("principals")&&!unreadable.ContainsKey("trigger-definers")&&(table!=null||completeViewInventory&&!unreadable.Keys.Any(k=>k.EndsWith("-definers",StringComparison.Ordinal)));
                foreach(var d in ds)
                {
                    var bound=users.Where(u=>u[1]+"@"+u[0]==d[2]||u[3]=="Y"&&u[0]==""&&u[1]==d[2]).Select(P).ToArray();
                    if(bound.Length==1)principals.Add(bound[0]);else definerKnown=false;
                }
                bool changed;do
                {
                    changed=false;
                    foreach(var e in edges)
                    {var member=P(e);var role=new AccessPrincipal("",e[2]!);if(principals.Contains(member)||principals.Contains(role)){changed|=principals.Add(member);changed|=principals.Add(role);}}
                    foreach(var u in users.Where(u=>u[4]!=null&&u[4]!=""))
                    {var member=P(u);var role=new AccessPrincipal("",u[4]!);if(principals.Contains(member)||principals.Contains(role)){changed|=principals.Add(member);changed|=principals.Add(role);}}
                    foreach(var p in proxies)
                    {
                        var owner=P(p);var endpoint=new AccessPrincipal(p[2]!,p[3]!);
                        if(principals.Contains(owner)||principals.Contains(endpoint))
                        {changed|=principals.Add(owner);foreach(var u in users.Where(u=>p[2]==""&&p[3]==""||P(u)==endpoint))changed|=principals.Add(P(u));}
                    }
                }while(changed);
                Add("configuration","RoleConfiguration",configuration);
                Add("principals","RelatedRoles",new{accounts=users.Where(u=>principals.Contains(P(u))),authentication=authentication.Where(a=>principals.Contains(P(a))),memberships=edges.Where(e=>principals.Contains(P(e))),proxies=proxies.Where(p=>principals.Contains(P(p)))});
                Add("definers","DefinerAccess",ds.Select(d=>d.Take(3).ToArray()));
                // A fixed aggregate blocks just the incomplete inventory. Known
                // schema/table/column/routine grants remain independently usable.
                if(!definerKnown||principals.Any(p=>!users.Any(u=>P(u)==p)))gaps.Add(new("definers",null,"DefinerAccess","",relatedUnreadable.Length>0?relatedUnreadable[0].Value:Reason.VisibilityUnknown));
                if(!definerKnown||relatedUnreadable.Length>0||principals.Any(p=>!users.Any(u=>P(u)==p)))gaps.Add(new("principals",null,"RelatedRoles","",relatedUnreadable.Length>0?relatedUnreadable[0].Value:Reason.VisibilityUnknown));
                Add("schema","SchemaAccess",schemas.Select(s=>new{principal=s.Take(2).ToArray(),pattern=ExactGrantDatabase(s[2]!)==own?null:s[2],privileges=s.Skip(3).ToArray()}));
                foreach(var group in schemas.GroupBy(P).Where(g=>g.Count()>1))gaps.Add(new(JsonSerializer.Serialize(group.Key),null,"SchemaGrantPrecedence","",Reason.NotImplemented));
                Add("table","TableAccess",tg.Select(g=>table==null?g:g.Where((_,i)=>i!=2).ToArray()));
                if(table==null){Add("column","DatabaseColumnAccess",cg);Add("routine","RoutineAccess",routineGrants);}
                else foreach(var column in table.Columns)facts.Add(new(column.Name,column.Name,"ColumnAccess",JsonSerializer.Serialize(cg.Where(g=>g[3]==column.Name).Select(g=>new[]{g[0],g[1],g[4]}))));
                if(unreadable.TryGetValue("schema",out var schemaReason))gaps.Add(new("schema",null,"SchemaAccess","",schemaReason));
                if(unreadable.TryGetValue("table",out var tableReason))gaps.Add(new("table",null,"TableAccess","",tableReason));
                if(unreadable.TryGetValue("column",out var columnReason)){if(table==null)gaps.Add(new("column",null,"DatabaseColumnAccess","",columnReason));else foreach(var column in table.Columns)gaps.Add(new(column.Name,column.Name,"ColumnAccess","",columnReason));}
                if(table==null&&unreadable.TryGetValue("routine",out var routineReason))gaps.Add(new("routine",null,"RoutineAccess","",routineReason));
                return new("ACCESS-01",facts,gaps.Count==0?Reason.None:gaps[0].Reason){Gaps=gaps,CompleteExceptGaps=gaps.Count>0};
                void Add(string key,string attribute,object? value)=>facts.Add(new(key,null,attribute,JsonSerializer.Serialize(value)));
            }
        }
        catch(MySqlException e) when(e.Number is 1044 or 1142 or 1143 or 1227)
        {var denied=new CatalogSection("ACCESS-01",[],Reason.PermissionDenied);return(tables.Select(t=>t with{Catalog=t.Catalog.Append(denied).ToArray()}).ToList(),denied);}
        static AccessPrincipal P(string?[] r)=>new(r[0]!,r[1]!);
    }
    private async Task<bool> ScopePrivilege(string privilege,CancellationToken token)
    {
        await using var q=Command("SELECT EXISTS(SELECT 1 FROM information_schema.USER_PRIVILEGES WHERE BINARY GRANTEE=BINARY "+CurrentGrantee+" AND PRIVILEGE_TYPE=@privilege) OR COALESCE((SELECT MIN(has_privilege) FROM (SELECT TABLE_SCHEMA,MAX(PRIVILEGE_TYPE=@privilege) has_privilege FROM information_schema.SCHEMA_PRIVILEGES WHERE BINARY GRANTEE=BINARY "+CurrentGrantee+" AND CONVERT(DATABASE() USING utf8mb3) COLLATE utf8mb3_bin LIKE TABLE_SCHEMA ESCAPE 0x5c GROUP BY TABLE_SCHEMA) matching),0)");q.Parameters.AddWithValue("privilege",privilege);return Convert.ToInt32(await q.ExecuteScalarAsync(token))==1;
    }
    private static string? ExactGrantDatabase(string pattern)
    {
        var result=new StringBuilder();for(var i=0;i<pattern.Length;i++){var c=pattern[i];if(c=='\\'){if(++i==pattern.Length)return null;result.Append(pattern[i]);}else if(c is '%' or '_')return null;else result.Append(c);}return result.ToString();
    }
}
