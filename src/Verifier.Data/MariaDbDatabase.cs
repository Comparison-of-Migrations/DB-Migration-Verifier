using System.Data;
using System.Globalization;
using System.Text.Json;
using MySqlConnector;
using Verifier.Core;
namespace Verifier.Data;

// MariaDB has its own native catalogs, generated-column safety and CTE behavior.
// Only the MySqlConnector binary wire/frame reader is shared with MySQL.
public sealed partial class MariaDbDatabase(ConnectionSettings settings):IReadDatabase,IStreamingReadDatabase,ICatalogObservationDatabase,IIntegrityDatabase
{
    private MySqlConnection? connection;private MySqlTransaction? transaction;private Inventory? inventory;
    private MariaDbDatabase? observer;
    private readonly Dictionary<string,string> definitions=new(StringComparer.Ordinal);
    private readonly Dictionary<string,HashSet<string>> unicodeColumns=new(StringComparer.Ordinal);
    // MariaDB 11.4 native UUID/INET casts expose canonical 16/4/16-byte values;
    // retain those bytes through the existing binary streaming frame, not CLR
    // Guid endian conversion or a locale-dependent string representation.
    private static readonly HashSet<string> Supported=new(StringComparer.Ordinal){"tinyint","smallint","mediumint","int","bigint","decimal","float","double","bit","date","datetime","timestamp","time","year","char","varchar","tinytext","text","mediumtext","longtext","binary","varbinary","tinyblob","blob","mediumblob","longblob","enum","set","geometry","point","linestring","polygon","multipoint","multilinestring","multipolygon","geomcollection","geometrycollection","uuid","inet4","inet6"};
    private static string Quote(string s)=>"`"+s.Replace("`","``")+"`";
    private const string TableFilter="CAST(TABLE_SCHEMA AS BINARY)=CAST(@schema AS BINARY) AND CAST(TABLE_NAME AS BINARY)=CAST(@table AS BINARY)";
    private const string CurrentGrantee="CONCAT(QUOTE(LEFT(CURRENT_USER(),LENGTH(CURRENT_USER())-LENGTH(SUBSTRING_INDEX(CURRENT_USER(),'@',-1))-1)),'@',QUOTE(SUBSTRING_INDEX(CURRENT_USER(),'@',-1)))";
    private MySqlCommand Command(string sql)=>connection is{State:ConnectionState.Open}?new(sql,connection,transaction):throw new VerificationException(Reason.ConnectionLost);
    private static VerificationException Map(MySqlException e)=>new(e.ErrorCode==MySqlErrorCode.CommandTimeoutExpired?Reason.Timeout:e.Number switch{1044 or 1045 or 1142 or 1143 or 1227=>Reason.PermissionDenied,1205 or 1317 or 1969=>Reason.Timeout,1146 or 1054 or 1412 or 1347 or 1932=>Reason.ObservationChanged,_=>Reason.ConnectionLost});
    private async Task Open(CancellationToken token)
    {
        await OpenTls(token);
        await using(var q=Command("SELECT VERSION()")){var v=Convert.ToString(await q.ExecuteScalarAsync(token),CultureInfo.InvariantCulture)!;if(!v.StartsWith("11.4.",StringComparison.Ordinal)||!v.Contains("MariaDB",StringComparison.Ordinal))throw new VerificationException(Reason.PlatformNotVerified);}
        await using var setup=Command("SET SESSION time_zone='+00:00';SET SESSION max_recursive_iterations=65536");await setup.ExecuteNonQueryAsync(token);
    }
    private async Task<string> Identity(CancellationToken token)
    {
        await using var q=Command("SELECT @@server_uid,DATABASE(),@@lower_case_table_names");await using var r=await q.ExecuteReaderAsync(token);
        if(!await r.ReadAsync(token)||r.IsDBNull(0)||r.IsDBNull(1))throw new VerificationException(Reason.IdentityUnknown);
        var id=r.GetString(0);try{if(Convert.FromBase64String(id).Length!=20)throw new VerificationException(Reason.IdentityUnknown);}catch(FormatException){throw new VerificationException(Reason.IdentityUnknown);}
        var db=r.GetString(1);if(r.GetInt32(2)!=0)db=db.ToLowerInvariant();return JsonSerializer.Serialize(new[]{"MariaDB",id,db});
    }
    public async Task<Inventory> InspectAsync(CancellationToken token)
    {
        if(inventory!=null)return inventory;
        try
        {
            await Open(token);transaction=await connection!.BeginTransactionAsync(IsolationLevel.RepeatableRead,true,token);var identity=await Identity(token);
            // Direct global/schema grants establish this inventory. Role-only or
            // column-only visibility is not evidence of absent hidden objects.
            // Native schema grant names are LIKE patterns, including escaped
            // underscores. Prove SELECT in every matching candidate rather than
            // guessing MariaDB's most-specific rule or unioning privileges.
            var complete=await InventoryVisible(token);
            var names=await Native("SELECT TABLE_SCHEMA,TABLE_NAME FROM information_schema.TABLES WHERE CAST(TABLE_SCHEMA AS BINARY)=CAST(DATABASE() AS BINARY) AND TABLE_TYPE IN ('BASE TABLE','SYSTEM VERSIONED') ORDER BY CAST(TABLE_NAME AS BINARY)",null,token);
            var tables=new List<Table>();foreach(var n in names)
            {
                var table=new Table(n[0]!,n[1]!,[],true);var snapshot=await Describe(table,token);definitions[table.Qualified]=snapshot.Signature;unicodeColumns[table.Qualified]=snapshot.Unicode;
                var reason=snapshot.Engine!="InnoDB"?Reason.NotImplemented:snapshot.Kind=="BASE TABLE"?Reason.None:snapshot.HistoryReason;
                table=table with{Columns=snapshot.Columns,Catalog=snapshot.Catalog,RowKeys=snapshot.Keys,SafeRead=reason==Reason.None,SafeLocalRead=reason==Reason.None,ReadReason=reason};
                if(reason==Reason.None){reason=await CheckReadPermission(table,token);table=table with{SafeRead=reason==Reason.None,SafeLocalRead=reason==Reason.None,ReadReason=reason};}tables.Add(table);
            }
            var access=await ReadAccess(tables,token);tables=access.Tables;var objects=await ReadObjects(tables,token);tables=objects.Tables;
            return inventory=new(identity,tables,complete,"Independent MariaDB READ ONLY REPEATABLE READ; InnoDB stored values and system-history versions including original transaction IDs and referenced native registry semantics; writes stopped required; native definitions observed separately, not an atomic cross-server DDL snapshot."){DatabaseIsNamespace=true,DatabaseCatalog=[await DatabaseDefinition(token),await ReadSequences(complete,token),access.Database,objects.Database]};
        }
        catch(MySqlException e){throw Map(e);}
    }
    private async Task<bool> InventoryVisible(CancellationToken token)
    {await using var grant=Command("SELECT EXISTS(SELECT 1 FROM information_schema.USER_PRIVILEGES WHERE CAST(GRANTEE AS BINARY)=CAST("+CurrentGrantee+" AS BINARY) AND PRIVILEGE_TYPE='SELECT') OR COALESCE((SELECT MIN(has_select) FROM (SELECT TABLE_SCHEMA,MAX(PRIVILEGE_TYPE='SELECT') has_select FROM information_schema.SCHEMA_PRIVILEGES WHERE CAST(GRANTEE AS BINARY)=CAST("+CurrentGrantee+" AS BINARY) AND CONVERT(DATABASE() USING utf8mb3) COLLATE utf8mb3_bin LIKE TABLE_SCHEMA ESCAPE 0x5c GROUP BY TABLE_SCHEMA) matching),0)");return Convert.ToInt32(await grant.ExecuteScalarAsync(token))==1;}
    private sealed record Description(IReadOnlyList<Column> Columns,HashSet<string> Unicode,IReadOnlyList<CatalogSection> Catalog,IReadOnlyList<IReadOnlyList<string>> Keys,string Engine,string Kind,Reason HistoryReason,string Signature);
    private async Task<List<string?[]>> Native(string sql,Table? table,CancellationToken token)
    {
        await using var q=Command(sql);if(table!=null){q.Parameters.AddWithValue("schema",table.Schema);q.Parameters.AddWithValue("table",table.Name);}await using var r=await q.ExecuteReaderAsync(token);var rows=new List<string?[]>();
        while(await r.ReadAsync(token))rows.Add(Enumerable.Range(0,r.FieldCount).Select(i=>r.IsDBNull(i)?null:Convert.ToString(r.GetValue(i),CultureInfo.InvariantCulture)).ToArray());return rows;
    }
    private async Task<Description> Describe(Table table,CancellationToken token)
    {
        var storage=await Native("SELECT ENGINE,TABLE_TYPE FROM information_schema.TABLES WHERE "+TableFilter,table,token);if(storage.Count!=1||storage[0][1] is not ("BASE TABLE" or "SYSTEM VERSIONED"))throw new VerificationException(Reason.ObservationChanged);
        var rows=await Native("SELECT COLUMN_NAME,DATA_TYPE,COLUMN_TYPE,ORDINAL_POSITION,IS_NULLABLE,COLUMN_DEFAULT,EXTRA,GENERATION_EXPRESSION,CHARACTER_SET_NAME,COLLATION_NAME,IS_GENERATED,IS_SYSTEM_TIME_PERIOD_START,IS_SYSTEM_TIME_PERIOD_END,COLUMN_COMMENT FROM information_schema.COLUMNS WHERE "+TableFilter+" ORDER BY ORDINAL_POSITION",table,token);
        var historyReason=await SystemHistory(table,storage[0][1]!,rows,token);var history=historyReason==Reason.None;
        var cols=new List<Column>();var facts=new List<CatalogFact>{new("table",null,"TableKind",JsonSerializer.Serialize(storage[0][1]))};var unicode=new HashSet<string>(StringComparer.Ordinal);
        foreach(var r in rows)
        {
            var name=r[0]!;var position=int.Parse(r[3]!,CultureInfo.InvariantCulture);var nullable=r[4]=="YES";var safe=Supported.Contains(r[1]!)&&(r[10]=="NEVER"||history&&(r[11]=="YES"||r[12]=="YES"));var definition=JsonSerializer.Serialize(r.Skip(2));cols.Add(new(name,r[2]!,position,nullable,definition,safe));
            if(r[8]!=null&&r[8]!="binary")unicode.Add(name);
            facts.Add(new(name,name,"ColumnName",JsonSerializer.Serialize(name)));facts.Add(new(name,name,"ColumnComment",r[13]));facts.Add(new(name,name,"ColumnType",JsonSerializer.Serialize(new[]{r[2],r[8]})));facts.Add(new(name,name,"ColumnPosition",r[3]));facts.Add(new(name,name,"ColumnNullable",nullable?"true":"false"));facts.Add(new(name,name,"ColumnDefault",r[5]));facts.Add(new(name,name,"ColumnGenerated",JsonSerializer.Serialize(new[]{r[6],r[7],r[10],r[11],r[12]})));facts.Add(new(name,name,"ColumnCollation",r[9]));facts.Add(new(name,name,"ColumnIdentity",r[6]!.Contains("auto_increment",StringComparison.Ordinal)?"auto_increment":null));
        }
        // Resolve only expressions whose complete column dependency closure is
        // already safe. Unknown calls/cycles never become safe through iteration.
        var safeNames=cols.Where(c=>c.SafeValue).Select(c=>c.Name).ToHashSet(StringComparer.Ordinal);
        for(var pass=0;pass<cols.Count;pass++)
        {
            var changed=false;
            for(var i=0;i<cols.Count;i++)if(!cols[i].SafeValue&&Supported.Contains(rows[i][1]!)&&rows[i][10]!="NEVER"&&rows[i][7]!=null&&MariaDbExpression.Safe(rows[i][7]!,safeNames))
            {cols[i]=cols[i] with{SafeValue=true};safeNames.Add(cols[i].Name);changed=true;}
            if(!changed)break;
        }
        var stats=await Native("SELECT INDEX_NAME,NON_UNIQUE,SEQ_IN_INDEX,COLUMN_NAME,SUB_PART FROM information_schema.STATISTICS WHERE "+TableFilter+" ORDER BY CAST(INDEX_NAME AS BINARY),SEQ_IN_INDEX",table,token);
        var keys=stats.GroupBy(x=>x[0]!,StringComparer.Ordinal).Where(g=>g.All(r=>r[1]=="0"&&r[3]!=null&&r[4]==null&&cols.Any(c=>c.Name==r[3]&&c.SafeValue))).Select(g=>(IReadOnlyList<string>)g.Select(r=>r[3]!).ToArray()).OrderBy(x=>x.Count).ToArray();
        if(history)
        {
            // STATISTICS hides implicit row_end (and leaves an ordinal hole).
            // Native KCU exposes the full version key, including that endpoint.
            var versionKeys=await Native("SELECT CONSTRAINT_NAME,COLUMN_NAME,ORDINAL_POSITION FROM information_schema.KEY_COLUMN_USAGE WHERE "+TableFilter+" AND REFERENCED_TABLE_NAME IS NULL ORDER BY CAST(CONSTRAINT_NAME AS BINARY),ORDINAL_POSITION",table,token);
            keys=versionKeys.GroupBy(r=>r[0]!,StringComparer.Ordinal).Where(g=>stats.Any(r=>r[0]==g.Key&&r[1]=="0")&&stats.Where(r=>r[0]==g.Key).All(r=>r[1]=="0"&&r[3]!=null&&r[4]==null)&&g.All(r=>r[1]!=null&&cols.Any(c=>c.Name==r[1]&&c.SafeValue))).Select(g=>(IReadOnlyList<string>)g.Select(r=>r[1]!).ToArray()).OrderBy(k=>k.Count).ToArray();
        }
        var structure=await ReadStructure(table,token);if(SystemPeriod(storage[0][1]!,rows) is{} period)structure=structure with{Constraints=structure.Constraints with{Facts=structure.Constraints.Facts.Append(period).ToArray()}};if(storage[0][1]=="SYSTEM VERSIONED"&&SystemPeriod(storage[0][1]!,rows)==null)structure=structure with{Constraints=structure.Constraints with{Reason=historyReason,CompleteExceptGaps=true,Gaps=structure.Constraints.Gaps.Append(new("system_time",null,"SystemTimePeriod","",historyReason)).ToArray()}};var allocator=await ReadAutoIncrement(table,cols,storage[0][0]!,storage[0][1]!,token);
        return new(cols,unicode,[new("SCHEMA-01",facts,Reason.None),structure.Constraints,structure.Indexes,allocator,ReadColumnTypes(rows,facts)],keys,storage[0][0]!,storage[0][1]!,historyReason,JsonSerializer.Serialize(new{storage,rows,stats,structure.Signature}));
    }
    private async Task<MariaDbDatabase> Observer(CancellationToken token)
    {if(observer!=null)return observer;var opened=new MariaDbDatabase(settings);try{await opened.Open(token);return observer=opened;}catch{await opened.DisposeAsync();throw;}}
    private async Task<Reason> CheckReadPermission(Table table,CancellationToken token)
    {
        var cols=table.Columns.Where(c=>c.SafeValue).ToArray();if(cols.Length==0)return Reason.UnsupportedType;
        try
        {
            var o=await Observer(token);o.transaction=await o.connection!.BeginTransactionAsync(IsolationLevel.ReadCommitted,true,token);try{await using var q=o.Command("SELECT "+string.Join(',',cols.Select(c=>Quote(c.Name)))+" FROM "+Quote(table.Schema)+"."+Quote(table.Name)+" WHERE 0");await using(var r=await q.ExecuteReaderAsync(token))if(await r.ReadAsync(token))return Reason.ObservationChanged;await o.CheckWarnings(token);return Reason.None;}finally{await o.transaction.DisposeAsync();o.transaction=null;}
        }
        catch(MySqlException e){return Map(e).Reason;}
    }
    private async Task ValidateDefinition(Table table,CancellationToken token)
    {
        try
        {
            var o=await Observer(token);o.transaction=await o.connection!.BeginTransactionAsync(IsolationLevel.ReadCommitted,true,token);
            try{if(await o.Identity(token)!=inventory!.Identity)throw new VerificationException(Reason.ObservationChanged);var current=await o.Describe(table,token);if(table.SafeRead&&current.Kind=="SYSTEM VERSIONED"&&current.HistoryReason!=Reason.None)throw new VerificationException(current.HistoryReason);if(current.Signature!=definitions[table.Qualified])throw new VerificationException(Reason.ObservationChanged);}finally{await o.transaction.DisposeAsync();o.transaction=null;}
        }
        catch(MySqlException e){throw Map(e);}
    }
    public async Task<IReadOnlyList<CatalogObservationLimit>> InspectCatalogChangesAsync(IReadOnlyList<Table> tables,IReadOnlySet<string> checks,bool databaseScope,CancellationToken token)
    {
        var limits=new List<CatalogObservationLimit>();var selected=checks.Intersect(new[]{"SCHEMA-01","SCHEMA-02","SCHEMA-03","OBJECT-02","OBJECT-03"},StringComparer.Ordinal).ToArray();if(selected.Length==0&&!checks.Contains("ACCESS-01")&&!checks.Contains("OBJECT-01"))return limits;
        if(checks.Contains("OBJECT-01"))
        {
            try
            {
                var o=await Observer(token);if(await o.Identity(token)!=inventory!.Identity)throw new VerificationException(Reason.ObservationChanged);
                (List<Table> Tables,CatalogSection Database) current;o.transaction=await o.connection!.BeginTransactionAsync(IsolationLevel.ReadCommitted,true,token);
                try{current=await o.ReadObjects(tables.Select(t=>t with{Catalog=t.Catalog.Where(c=>c.CheckId!="OBJECT-01").ToArray()}).ToList(),token);}finally{await o.transaction.DisposeAsync();o.transaction=null;}
                foreach(var table in tables){var before=table.Catalog.Single(c=>c.CheckId=="OBJECT-01");var after=current.Tables.Single(t=>t.Qualified==table.Qualified).Catalog.Single(c=>c.CheckId=="OBJECT-01");if(JsonSerializer.Serialize(before)!=JsonSerializer.Serialize(after))limits.Add(new("OBJECT-01",table.Qualified,after.Reason==Reason.None||after.CompleteExceptGaps?Reason.ObservationChanged:after.Reason));}
                if(databaseScope&&JsonSerializer.Serialize(inventory.DatabaseCatalog.Single(c=>c.CheckId=="OBJECT-01"))!=JsonSerializer.Serialize(current.Database))limits.Add(new("OBJECT-01",null,current.Database.Reason==Reason.None||current.Database.CompleteExceptGaps?Reason.ObservationChanged:current.Database.Reason));
            }
            catch(VerificationException e){foreach(var t in tables)limits.Add(new("OBJECT-01",t.Qualified,e.Reason));if(databaseScope)limits.Add(new("OBJECT-01",null,e.Reason));}
            catch(MySqlException e){foreach(var t in tables)limits.Add(new("OBJECT-01",t.Qualified,Map(e).Reason));if(databaseScope)limits.Add(new("OBJECT-01",null,Map(e).Reason));}
        }
        if(checks.Contains("ACCESS-01"))
        {
            try
            {
                var o=await Observer(token);if(await o.Identity(token)!=inventory!.Identity)throw new VerificationException(Reason.ObservationChanged);
                var current=await o.ReadAccess(tables.Select(t=>t with{Catalog=t.Catalog.Where(c=>c.CheckId!="ACCESS-01").ToArray()}).ToList(),token);
                foreach(var table in tables){var before=table.Catalog.Single(c=>c.CheckId=="ACCESS-01");var after=current.Tables.Single(t=>t.Qualified==table.Qualified).Catalog.Single(c=>c.CheckId=="ACCESS-01");if(JsonSerializer.Serialize(before)!=JsonSerializer.Serialize(after))limits.Add(new("ACCESS-01",table.Qualified,after.Reason==Reason.None||after.CompleteExceptGaps?Reason.ObservationChanged:after.Reason));}
                if(databaseScope&&JsonSerializer.Serialize(inventory.DatabaseCatalog.Single(c=>c.CheckId=="ACCESS-01"))!=JsonSerializer.Serialize(current.Database))limits.Add(new("ACCESS-01",null,current.Database.Reason==Reason.None||current.Database.CompleteExceptGaps?Reason.ObservationChanged:current.Database.Reason));
            }
            catch(VerificationException e){foreach(var t in tables)limits.Add(new("ACCESS-01",t.Qualified,e.Reason));if(databaseScope)limits.Add(new("ACCESS-01",null,e.Reason));}
            catch(MySqlException e){foreach(var t in tables)limits.Add(new("ACCESS-01",t.Qualified,Map(e).Reason));if(databaseScope)limits.Add(new("ACCESS-01",null,Map(e).Reason));}
        }
        if(selected.Length>0)foreach(var table in tables)
        {
            try
            {
                var o=await Observer(token);o.transaction=await o.connection!.BeginTransactionAsync(IsolationLevel.ReadCommitted,true,token);
                try
                {
                    if(await o.Identity(token)!=inventory!.Identity)throw new VerificationException(Reason.ObservationChanged);var current=await o.Describe(table,token);
                    foreach(var check in selected){var before=table.Catalog.Single(c=>c.CheckId==check);var after=current.Catalog.Single(c=>c.CheckId==check);if(JsonSerializer.Serialize(before)!=JsonSerializer.Serialize(after))limits.Add(new(check,table.Qualified,after.Reason==Reason.None?Reason.ObservationChanged:after.Reason));}
                }
                finally{await o.transaction.DisposeAsync();o.transaction=null;}
            }
            catch(VerificationException e){foreach(var check in selected)limits.Add(new(check,table.Qualified,e.Reason));}
            catch(MySqlException e){foreach(var check in selected)limits.Add(new(check,table.Qualified,Map(e).Reason));}
        }
        if(databaseScope&&selected.Contains("SCHEMA-01",StringComparer.Ordinal))
        {
            try{var o=await Observer(token);var current=await o.DatabaseDefinition(token);if(JsonSerializer.Serialize(current)!=JsonSerializer.Serialize(inventory!.DatabaseCatalog.Single(c=>c.CheckId=="SCHEMA-01")))limits.Add(new("SCHEMA-01",null,current.Reason==Reason.None?Reason.ObservationChanged:current.Reason));}
            catch(MySqlException e){limits.Add(new("SCHEMA-01",null,Map(e).Reason));}
            catch(VerificationException e){limits.Add(new("SCHEMA-01",null,e.Reason));}
        }
        if(databaseScope&&selected.Contains("OBJECT-02",StringComparer.Ordinal))
        {
            try{var o=await Observer(token);if(await o.Identity(token)!=inventory!.Identity)throw new VerificationException(Reason.ObservationChanged);var current=await o.ReadSequences(inventory.Complete,token);if(JsonSerializer.Serialize(current)!=JsonSerializer.Serialize(inventory.DatabaseCatalog.Single(c=>c.CheckId=="OBJECT-02")))limits.Add(new("OBJECT-02",null,current.Reason==Reason.None||current.CompleteExceptGaps?Reason.ObservationChanged:current.Reason));}
            catch(MySqlException e){limits.Add(new("OBJECT-02",null,Map(e).Reason));}
            catch(VerificationException e){limits.Add(new("OBJECT-02",null,e.Reason));}
        }
        return limits;
    }

    private async Task CheckWarnings(CancellationToken token)
    {await using var q=Command("SHOW COUNT(*) WARNINGS");if(Convert.ToInt64(await q.ExecuteScalarAsync(token))!=0)throw new VerificationException(Reason.ObservationChanged);}
    public async ValueTask DisposeAsync()
    {try{if(observer!=null)await observer.DisposeAsync();}finally{try{if(transaction!=null)await transaction.DisposeAsync();}finally{if(connection!=null)await connection.DisposeAsync();}}}
}
