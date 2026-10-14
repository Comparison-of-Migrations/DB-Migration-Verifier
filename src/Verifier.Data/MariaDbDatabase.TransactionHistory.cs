using System.Text.Json;
using MySqlConnector;
using Verifier.Core;
namespace Verifier.Data;

public sealed partial class MariaDbDatabase
{
    private const string OpenTransaction="18446744073709551615";
    private static string[]? TransactionPeriod(Table table)
    {
        var fact=table.Catalog.Where(c=>c.CheckId=="SCHEMA-02").SelectMany(c=>c.Facts).SingleOrDefault(f=>f.Attribute=="SystemTimePeriod");
        var period=fact?.Value==null?null:JsonSerializer.Deserialize<string[]>(fact.Value);
        return period is{Length:4}&&period[2]=="bigint(20) unsigned"&&period[3]=="bigint(20) unsigned"?period:null;
    }
    private static string CurrentEndLiteral(Table table)=>TransactionPeriod(table)!=null?OpenTransaction:"TIMESTAMP'2038-01-19 03:14:07.999999'";

    private async Task<Reason> TransactionRegistryPermission(CancellationToken token)
    {
        // Fixed native system table only. Never evaluate a replaced/generated
        // registry column, or multiply rows through an unproved registry key.
        var shape=await Native("SELECT COLUMN_NAME,COLUMN_TYPE,EXTRA,COLUMN_KEY FROM information_schema.COLUMNS WHERE TABLE_SCHEMA='mysql' AND TABLE_NAME='transaction_registry' ORDER BY ORDINAL_POSITION",null,token);
        string[] names=["transaction_id","commit_id","begin_timestamp","commit_timestamp","isolation_level"];
        string[] types=["bigint(20) unsigned","bigint(20) unsigned","timestamp(6)","timestamp(6)","enum('READ-UNCOMMITTED','READ-COMMITTED','REPEATABLE-READ','SERIALIZABLE')"];
        if(shape.Count==0)return Reason.PermissionDenied;
        if(shape.Count!=5||shape.Where((r,i)=>r[0]!=names[i]||r[1]!=types[i]||r[2]!="").Any()||shape[0][3]!="PRI")return Reason.NotImplemented;
        var storage=await Native("SELECT ENGINE,TABLE_TYPE FROM information_schema.TABLES WHERE TABLE_SCHEMA='mysql' AND TABLE_NAME='transaction_registry'",null,token);
        if(storage.Count!=1||storage[0][0]!="InnoDB"||storage[0][1]!="BASE TABLE")return Reason.NotImplemented;
        try{await using var q=Command("SELECT transaction_id,commit_id,begin_timestamp,commit_timestamp,isolation_level FROM mysql.transaction_registry WHERE 0");await using(var reader=await q.ExecuteReaderAsync(token))if(reader.FieldCount!=5)return Reason.VisibilityUnknown;await CheckWarnings(token);return Reason.None;}
        catch(MySqlException e){return Map(e).Reason;}
    }

    private static string RegistryPayload(string value,string registry)=>
        "CASE WHEN "+value+"="+OpenTransaction+" THEN JSON_ARRAY(CAST("+value+" AS CHAR CHARACTER SET ascii)) ELSE JSON_ARRAY("+
        string.Join(',',new[]{value,registry+".commit_id",registry+".begin_timestamp",registry+".commit_timestamp",registry+".isolation_level"}.Select(v=>"CAST("+v+" AS CHAR CHARACTER SET ascii)"))+") END";

    private static string TransactionData(Table table,IReadOnlyList<Column> columns)
    {
        var period=TransactionPeriod(table)!;
        var projection=columns.Select((c,i)=>
        {
            var value="t."+Quote(c.Name);var boundary=Array.IndexOf(period,c.Name,0,2);
            return (boundary>=0?RegistryPayload(value,"r"+boundary):value)+" v"+i+(Enumerated(c)?",CAST("+value+" AS UNSIGNED) e"+i:"");
        });
        return "SELECT ROW_NUMBER()OVER() rid,"+string.Join(',',projection)+" FROM "+StoredRelation(table)+" t"+
            string.Concat(Enumerable.Range(0,2).Select(i=>" LEFT JOIN mysql.transaction_registry r"+i+" ON r"+i+".transaction_id=t."+Quote(period[i])+" AND t."+Quote(period[i])+"<>"+OpenTransaction));
    }

    private async Task<IReadOnlyList<IntegrityViolation>> MissingTransactionRecords(Table table,CancellationToken token)
    {
        if(TransactionPeriod(table) is not{} period)return [];
        var missing=new List<IntegrityViolation>();
        foreach(var boundary in period.Take(2))
        {
            await using var q=Command("SELECT COUNT(*) FROM "+StoredRelation(table)+" t LEFT JOIN mysql.transaction_registry r ON r.transaction_id=t."+Quote(boundary)+" WHERE t."+Quote(boundary)+"<>"+OpenTransaction+" AND r.transaction_id IS NULL");
            var count=Convert.ToInt64(await q.ExecuteScalarAsync(token),System.Globalization.CultureInfo.InvariantCulture);await CheckWarnings(token);
            if(count<0)throw new VerificationException(Reason.ObservationChanged);if(count>0)missing.Add(new("Reference",boundary,count));
        }
        return missing;
    }
}
