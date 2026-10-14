using System.Text.Json;
using Verifier.Core;
using MySqlConnector;
namespace Verifier.Data;

public sealed partial class MariaDbDatabase
{
    private async Task<Reason> SystemHistory(Table table,string kind,List<string?[]> rows,CancellationToken token)
    {
        if(kind!="SYSTEM VERSIONED")return Reason.NotImplemented;
        // Native COLUMNS omits the two implicit timestamp period columns entirely.
        // They remain explicitly selectable and are part of the native version key.
        // Transaction-precise BIGINT periods require explicit declarations, so zero
        // native period flags may also mean restricted metadata visibility.
        // Prove the default names in a separate short read-only transaction;
        // never leave a table metadata lock on the frozen inventory session.
        if(!rows.Any(r=>r[11]=="YES"||r[12]=="YES"))
        {
            if(rows.Any(r=>string.Equals(r[0],"row_start",StringComparison.OrdinalIgnoreCase)||string.Equals(r[0],"row_end",StringComparison.OrdinalIgnoreCase)))return Reason.VisibilityUnknown;
            try
            {
                var o=await Observer(token);o.transaction=await o.connection!.BeginTransactionAsync(System.Data.IsolationLevel.ReadCommitted,true,token);
                try{await using var q=o.Command("SELECT `row_start`,`row_end` FROM "+Quote(table.Schema)+"."+Quote(table.Name)+" FOR SYSTEM_TIME ALL WHERE 0");await using(var reader=await q.ExecuteReaderAsync(token))if(reader.FieldCount!=2)return Reason.VisibilityUnknown;await o.CheckWarnings(token);}
                finally{await o.transaction.DisposeAsync();o.transaction=null;}
            }
            catch(MySqlException e){return e.Number==1054?Reason.VisibilityUnknown:Map(e).Reason;}
            catch(VerificationException e){return e.Reason;}
            foreach(var (name,expression,start,end) in new[]{("row_start","ROW START","YES","NO"),("row_end","ROW END","NO","YES")})
                rows.Add([name,"timestamp","timestamp(6)",(rows.Count+1).ToString(System.Globalization.CultureInfo.InvariantCulture),"NO",null,"STORED GENERATED, INVISIBLE",expression,null,null,"ALWAYS",start,end,""]);
        }
        var starts=rows.Where(r=>r[11]=="YES").ToArray();var ends=rows.Where(r=>r[12]=="YES").ToArray();
        if(starts.Length!=1||ends.Length!=1||starts[0]==ends[0]||starts[0][12]!="NO"||ends[0][11]!="NO")return Reason.VisibilityUnknown;
        if(starts[0][7]!="ROW START"||ends[0][7]!="ROW END")return Reason.NotImplemented;
        if(starts[0][2]=="timestamp(6)"&&ends[0][2]=="timestamp(6)")return Reason.None;
        if(starts[0][2]=="bigint(20) unsigned"&&ends[0][2]=="bigint(20) unsigned")return await TransactionRegistryPermission(token);
        return Reason.NotImplemented;
    }
    private static string StoredRelation(Table table)=>Quote(table.Schema)+"."+Quote(table.Name)+
        (table.Catalog.Any(c=>c.CheckId=="SCHEMA-01"&&c.Facts.Any(f=>f.Attribute=="TableKind"&&f.Value=="\"SYSTEM VERSIONED\""))?" FOR SYSTEM_TIME ALL":"");
    private static string? CurrentEnd(Table table)
    {
        var period=table.Catalog.Where(c=>c.CheckId=="SCHEMA-02").SelectMany(c=>c.Facts).SingleOrDefault(f=>f.Attribute=="SystemTimePeriod");
        return period?.Value==null?null:JsonSerializer.Deserialize<string[]>(period.Value)![1];
    }
    private static string CurrentRelation(Table table,string? alias=null)
    {
        if(CurrentEnd(table) is not{} end)return Quote(table.Schema)+"."+Quote(table.Name)+(alias==null?"":" "+Quote(alias));
        // An inherited system_versioning_asof can silently change an unqualified
        // current-table read. Explicit ALL plus the native open-ended marker
        // selects active versions even with future start timestamps. Do not
        // evaluate unproved generated columns via SELECT * in this projection.
        return "(SELECT "+string.Join(',',table.Columns.Where(c=>c.SafeValue).Select(c=>Quote(c.Name)))+" FROM "+StoredRelation(table)+" WHERE "+Quote(end)+"="+CurrentEndLiteral(table)+") "+Quote(alias??"__dmv_current");
    }
    private static CatalogFact? SystemPeriod(string kind,List<string?[]> rows)
    {
        if(kind!="SYSTEM VERSIONED")return null;
        var starts=rows.Where(r=>r[11]=="YES").ToArray();var ends=rows.Where(r=>r[12]=="YES").ToArray();
        if(starts.Length!=1||ends.Length!=1)return null;var start=starts[0];var end=ends[0];
        return new("system_time",null,"SystemTimePeriod",JsonSerializer.Serialize(new[]{start[0],end[0],start[2],end[2]}));
    }
}
