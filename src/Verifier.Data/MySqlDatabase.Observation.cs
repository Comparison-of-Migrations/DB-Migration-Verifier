using System.Text.Json;
using MySqlConnector;
using Verifier.Core;
namespace Verifier.Data;

public sealed partial class MySqlDatabase
{
    public async Task<IReadOnlyList<CatalogObservationLimit>> InspectCatalogChangesAsync(IReadOnlyList<Table> tables,IReadOnlySet<string> checks,bool databaseScope,CancellationToken token)
    {
        if(inventory==null||tables.Any(t=>!inventory.Tables.Contains(t)))throw new VerificationException(Reason.InvalidInput);
        // Fresh dictionary/state observations, not an atomic global DDL/ACL
        // snapshot or proof that no intermediate change was later reversed.
        var requested=checks.Where(c=>c is "SCHEMA-01" or "SCHEMA-02" or "SCHEMA-03" or "ACCESS-01" or "OBJECT-01" or "OBJECT-02" or "OBJECT-03").ToHashSet(StringComparer.Ordinal);
        if(requested.Count==0||tables.Count==0&&!databaseScope)return [];
        var limits=new HashSet<CatalogObservationLimit>();
        try
        {
            var observer=await MetadataObserver(token);
            observer.transaction=await observer.connection!.BeginTransactionAsync(System.Data.IsolationLevel.RepeatableRead,true,token);
            try
            {
                var identity=(await observer.ReadNative("SELECT @@server_uuid,DATABASE(),@@lower_case_table_names",null,token)).Single();
                if(!Guid.TryParse(identity[0],out var uuid)||identity[1]==null||JsonSerializer.Serialize(new[]{uuid.ToString(),identity[2]=="0"?identity[1]:identity[1]!.ToLowerInvariant()})!=inventory.Identity)throw new VerificationException(Reason.ObservationChanged);
                if(requested.Contains("ACCESS-01"))await Observe(async()=>
                {
                    var current=await observer.ReadAccess(tables.Select(t=>t with{Catalog=[]}).ToList(),token);
                    foreach(var table in tables)Compare(table.Catalog.Single(c=>c.CheckId=="ACCESS-01"),current.Tables.Single(t=>t.Qualified==table.Qualified).Catalog.Single(c=>c.CheckId=="ACCESS-01"),table.Qualified);
                    if(databaseScope)Compare(inventory.DatabaseCatalog.Single(c=>c.CheckId=="ACCESS-01"),current.Database,null);
                },["ACCESS-01"],tables.Select(t=>(string?)t.Qualified).Concat(databaseScope?[null]:[]));
                foreach(var table in tables)
                {
                    var tableChecks=requested.Where(c=>c!="ACCESS-01").ToArray();if(tableChecks.Length==0)continue;
                    var baseTable=false;await Observe(async()=>
                    {
                        var kind=await observer.ReadNative("SELECT TABLE_TYPE FROM information_schema.TABLES WHERE "+TableFilter,table,token);
                        if(kind.Count!=1||kind[0][0]!="BASE TABLE")throw new VerificationException(Reason.ObservationChanged);
                        baseTable=true;
                    },tableChecks,[table.Qualified]);
                    if(!baseTable)continue;
                    Task<(IReadOnlyList<Column> Columns,List<CatalogFact> Facts,string Signature)>? columns=null;
                    Task<Structure>? structure=null;
                    Task<(IReadOnlyList<Column> Columns,List<CatalogFact> Facts,string Signature)> Columns()=>columns??=observer.ReadColumns(table,token);
                    foreach(var check in requested.Where(c=>c is "SCHEMA-01" or "SCHEMA-02" or "SCHEMA-03" or "OBJECT-02" or "OBJECT-03"))
                        await Observe(async()=>
                        {
                            CatalogSection current;
                            if(check is "SCHEMA-02" or "SCHEMA-03")
                            {structure??=observer.ReadStructure(table,token);var s=await structure;current=check=="SCHEMA-02"?s.Constraints:s.Indexes;}
                            else
                            {
                                var c=await Columns();if(c.Columns.Count==0)throw new VerificationException(Reason.ObservationChanged);
                                if(check=="SCHEMA-01"){var facts=c.Facts.ToList();facts.Add(new("table",null,"TableKind","\"BASE TABLE\""));current=new(check,facts,Reason.None);}
                                else if(check=="OBJECT-02")current=await observer.ReadAutoIncrement(table,c.Columns,token);
                                else current=await observer.ReadColumnTypes(table,c.Columns,c.Facts,token,true);
                            }
                            Compare(table.Catalog.Single(c=>c.CheckId==check),current,table.Qualified);
                        },[check],[table.Qualified]);
                }
                if(requested.Contains("OBJECT-01"))await Observe(async()=>
                {
                    var global=await observer.ReadNative("SELECT EXISTS(SELECT 1 FROM information_schema.USER_PRIVILEGES WHERE CAST(GRANTEE AS BINARY)=CAST("+CurrentGrantee+" AS BINARY) AND PRIVILEGE_TYPE='SELECT')",null,token);
                    observer.globalReadCoverage=global.Count==1&&global[0][0]=="1"&&(await observer.ReadGlobalSelectCoverage(token)).All;
                    var current=await observer.ReadObjects(tables.Select(t=>t with{Catalog=[]}).ToList(),token);
                    foreach(var table in tables)Compare(table.Catalog.Single(c=>c.CheckId=="OBJECT-01"),current.Tables.Single(t=>t.Qualified==table.Qualified).Catalog.Single(c=>c.CheckId=="OBJECT-01"),table.Qualified);
                    if(databaseScope)Compare(inventory.DatabaseCatalog.Single(c=>c.CheckId=="OBJECT-01"),current.Database,null);
                },["OBJECT-01"],tables.Select(t=>(string?)t.Qualified).Concat(databaseScope?[null]:[]));
                if(databaseScope&&requested.Contains("SCHEMA-01"))await Observe(async()=>
                {
                    var row=await observer.ReadNative("SELECT DEFAULT_CHARACTER_SET_NAME,DEFAULT_COLLATION_NAME,DEFAULT_ENCRYPTION FROM information_schema.SCHEMATA WHERE CAST(SCHEMA_NAME AS BINARY)=CAST(DATABASE() AS BINARY)",null,token);
                    if(row.Count!=1)throw new VerificationException(Reason.VisibilityUnknown);
                    Compare(inventory.DatabaseCatalog.Single(c=>c.CheckId=="SCHEMA-01"),new("SCHEMA-01",[new("database",null,"DatabaseDefinition",JsonSerializer.Serialize(row[0]))],Reason.None),null);
                },["SCHEMA-01"],[null]);
                // MySQL has no independent sequences or CREATE TYPE objects;
                // their DB-scope absence is invariant, not a cached live value.
            }
            finally{await observer.transaction.DisposeAsync();observer.transaction=null;}
        }
        catch(MySqlException e){Unavailable(Map(e).Reason,requested,tables.Select(t=>(string?)t.Qualified).Concat(databaseScope?[null]:[]));}
        catch(VerificationException e){Unavailable(e.Reason,requested,tables.Select(t=>(string?)t.Qualified).Concat(databaseScope?[null]:[]));}
        return limits.ToArray();
        async Task Observe(Func<Task> read,IEnumerable<string> ids,IEnumerable<string?> locations)
        {try{await read();}catch(MySqlException e){Unavailable(Map(e).Reason,ids,locations);}catch(VerificationException e){Unavailable(e.Reason,ids,locations);}}
        void Unavailable(Reason reason,IEnumerable<string> ids,IEnumerable<string?> locations)
        {foreach(var id in ids)foreach(var table in locations)if(table!=null||CatalogComparison.DatabaseChecks.Contains(id))limits.Add(new(id,table,reason));}
        void Compare(CatalogSection before,CatalogSection after,string? table)
        {
            string Snapshot(CatalogSection c)=>JsonSerializer.Serialize(new{c.Reason,c.CompleteExceptGaps,facts=c.Facts.OrderBy(f=>f.Key,StringComparer.Ordinal).ThenBy(f=>f.Attribute,StringComparer.Ordinal),gaps=c.Gaps.OrderBy(g=>g.Key,StringComparer.Ordinal).ThenBy(g=>g.Attribute,StringComparer.Ordinal)});
            if(Snapshot(before)==Snapshot(after))return;
            var reason=after.Reason!=Reason.None&&!after.CompleteExceptGaps?after.Reason:Reason.ObservationChanged;
            limits.Add(new(before.CheckId,table,reason));
        }
    }
}
