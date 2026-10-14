using System.Data;
using System.Runtime.CompilerServices;
using MySqlConnector;
using Verifier.Core;
namespace Verifier.Data;

public sealed partial class MySqlDatabase
{
    private static bool Floating(Column c)=>c.Type.Split([' ','('],2)[0] is "float" or "double";
    private string ChunkQuery(Table table,IReadOnlyList<Column> columns)
    {
        // A window function prevents merging this CTE, so each stored row gets
        // exactly one local ordinal even without keys or with duplicate LOBs.
        // No application DDL/temp table, hashing, LIMIT/OFFSET or remote calls.
        if(!unicodeColumns.TryGetValue(table.Qualified,out var unicode))throw new VerificationException(Reason.ObservationChanged);
        // Capture enum index / SET bitmap in the base projection: a materialized
        // CTE may otherwise turn the field into a string and lose that state.
        var data="SELECT ROW_NUMBER() OVER() rid,"+string.Join(',',columns.Select((c,i)=>Quote(c.Name)+" AS v"+i+(Enumerated(c)?",CAST("+Quote(c.Name)+" AS UNSIGNED) AS e"+i:"")))+" FROM "+Quote(table.Schema)+"."+Quote(table.Name);
        string ValueBytes(int i,string prefix)=>"CAST("+(unicode.Contains(columns[i].Name)?"CONVERT("+prefix+"v"+i+" USING utf8mb4)":prefix+"v"+i)+" AS BINARY)";
        string Payload(int i,string prefix)=>Enumerated(columns[i])?"CONCAT(UNHEX(LPAD(HEX("+prefix+"e"+i+"),16,'0')),"+ValueBytes(i,prefix)+")":ValueBytes(i,prefix);
        var cells=string.Join(" UNION ALL ",columns.Select((c,i)=>"SELECT rid,"+i+" cid,v"+i+" IS NOT NULL present,"+(Floating(c)?"CASE WHEN v"+i+" IS NULL THEN NULL ELSE 8 END":"OCTET_LENGTH("+Payload(i,"")+")")+" len FROM data"));
        var bytes="CASE c.cid "+string.Join(' ',columns.Select((c,i)=>"WHEN "+i+" THEN "+(Floating(c)?"NULL":"SUBSTRING("+Payload(i,"d.")+",c.part*65536+1,65536)")))+" END";
        var floating="CASE c.cid "+string.Join(' ',columns.Select((c,i)=>"WHEN "+i+" THEN "+(Floating(c)?"d.v"+i:"NULL")))+" END";
        return "WITH RECURSIVE data AS ("+data+"),cells AS ("+cells+"),chunks(rid,cid,present,len,part) AS (SELECT rid,cid,present,len,CAST(0 AS UNSIGNED) FROM cells UNION ALL SELECT rid,cid,present,len,part+1 FROM chunks WHERE (part+1)*65536<len) SELECT c.rid,c.cid,c.present,c.len,c.part,"+bytes+","+floating+" FROM chunks c JOIN data d ON d.rid=c.rid ORDER BY c.rid,c.cid,c.part";
    }
    public async IAsyncEnumerable<Func<Stream,CancellationToken,Task>> StreamingRowsAsync(Table table,IReadOnlyList<int> order,[EnumeratorCancellation]CancellationToken token)
    {
        if(inventory==null||!inventory.Tables.Contains(table))throw new VerificationException(Reason.PermissionDenied);
        if(!table.SafeRead)throw new VerificationException(table.ReadReason);
        if(order.Count==0||order.Distinct().Count()!=order.Count||order.Any(i=>i<0||i>=table.Columns.Count))throw new VerificationException(Reason.InvalidInput);
        var columns=order.Select(i=>table.Columns[i]).ToArray();if(columns.Any(c=>!c.SafeValue))throw new VerificationException(Reason.UnsupportedType);
        await ValidateDefinition(table,token);
        await using var q=Command(ChunkQuery(table,columns));MySqlDataReader reader;
        // Prepared execution retains floating values in the binary protocol.
        try{await q.PrepareAsync(token);reader=await q.ExecuteReaderAsync(CommandBehavior.SequentialAccess,token);}catch(MySqlException e){throw Map(e);}
        await foreach(var write in MySqlProtocolRows.ReadAsync(reader,columns,Map,token))yield return write;
        await CheckWarnings(token);await ValidateDefinition(table,token);
    }
    public async IAsyncEnumerable<byte[]> RowsAsync(Table table,[EnumeratorCancellation]CancellationToken token)
    {
        await foreach(var write in StreamingRowsAsync(table,Enumerable.Range(0,table.Columns.Count).ToArray(),token))
        {using var output=new LimitedRow();await write(output,token);yield return output.ToArray();}
    }
    private sealed class LimitedRow:MemoryStream
    {
        private void Check(int n){if(Position+n>ExactRow.MaxRowBytes)throw new VerificationException(Reason.RowTooLarge);}
        public override void Write(ReadOnlySpan<byte> b){Check(b.Length);base.Write(b);}
        public override void Write(byte[] b,int o,int n){Check(n);base.Write(b,o,n);}
        public override void WriteByte(byte b){Check(1);base.WriteByte(b);}
    }
}
