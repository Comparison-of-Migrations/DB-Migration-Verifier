using System.Data;
using System.Runtime.CompilerServices;
using MySqlConnector;
using Verifier.Core;
namespace Verifier.Data;
public sealed partial class MariaDbDatabase
{
    private static bool Floating(Column c)=>c.Type.Split([' ','('],2)[0] is "float" or "double";
    private static bool Enumerated(Column c)=>c.Type.StartsWith("enum(",StringComparison.Ordinal)||c.Type.StartsWith("set(",StringComparison.Ordinal);
    private string ChunkQuery(Table table,IReadOnlyList<Column> cols)
    {
        if(!unicodeColumns.TryGetValue(table.Qualified,out var unicode))throw new VerificationException(Reason.ObservationChanged);
        var data="SELECT ROW_NUMBER()OVER() rid,"+string.Join(',',cols.Select((c,i)=>Quote(c.Name)+" v"+i+(Enumerated(c)?",CAST("+Quote(c.Name)+" AS UNSIGNED) e"+i:"")))+" FROM "+StoredRelation(table);
        if(TransactionPeriod(table)!=null)data=TransactionData(table,cols);
        string Bytes(int i)=>"CAST("+(unicode.Contains(cols[i].Name)?"CONVERT(d.v"+i+" USING utf8mb4)":"d.v"+i)+" AS BINARY)";
        string Payload(int i)=>Enumerated(cols[i])?"CONCAT(UNHEX(LPAD(HEX(d.e"+i+"),16,'0')),"+Bytes(i)+")":Bytes(i);
        string Choice(Func<int,string> value)=>"CASE c.cid "+string.Join(' ',Enumerable.Range(0,cols.Count).Select(i=>"WHEN "+i+" THEN "+value(i)))+" END";
        var present=Choice(i=>"d.v"+i+" IS NOT NULL");var length=Choice(i=>Floating(cols[i])?"CASE WHEN d.v"+i+" IS NULL THEN NULL ELSE 8 END":"OCTET_LENGTH("+Payload(i)+")");var payload=Choice(i=>Floating(cols[i])?"NULL":Payload(i));var floating=Choice(i=>Floating(cols[i])?"d.v"+i:"NULL");
        // MariaDB may materialize a CTE independently per reference. Keep row and
        // all cells together through ONE ordinal-bearing data reference. The
        // second cells reference computes only an order-independent maximum;
        // its row numbers are never joined back to the main rows.
        return "WITH RECURSIVE data AS ("+data+"),cells AS (SELECT d.rid,c.cid,"+present+" present,"+length+" len,"+payload+" payload,"+floating+" floating FROM data d CROSS JOIN ("+string.Join(" UNION ALL ",Enumerable.Range(0,cols.Count).Select(i=>"SELECT "+i+" cid"))+") c),extent AS (SELECT GREATEST(CEIL(COALESCE(MAX(len),0)/65536),1)-1 last_part FROM cells),parts(part,last_part) AS (SELECT CAST(0 AS UNSIGNED),last_part FROM extent UNION ALL SELECT part+1,last_part FROM parts WHERE part<last_part) SELECT c.rid,c.cid,c.present,c.len,p.part,SUBSTRING(c.payload,p.part*65536+1,65536),c.floating FROM cells c JOIN parts p ON p.part=0 OR p.part*65536<c.len ORDER BY c.rid,c.cid,p.part";
    }
    public async IAsyncEnumerable<Func<Stream,CancellationToken,Task>> StreamingRowsAsync(Table table,IReadOnlyList<int> order,[EnumeratorCancellation]CancellationToken token)
    {
        if(inventory==null||!inventory.Tables.Contains(table))throw new VerificationException(Reason.PermissionDenied);if(!table.SafeRead)throw new VerificationException(table.ReadReason);
        if(order.Count==0||order.Distinct().Count()!=order.Count||order.Any(i=>i<0||i>=table.Columns.Count))throw new VerificationException(Reason.InvalidInput);
        var cols=order.Select(i=>table.Columns[i]).ToArray();if(cols.Any(c=>!c.SafeValue))throw new VerificationException(Reason.UnsupportedType);await ValidateDefinition(table,token);
        try{if((await MissingTransactionRecords(table,token)).Count>0)throw new VerificationException(Reason.VisibilityUnknown);}catch(MySqlException e){throw Map(e);}
        await using var q=Command(ChunkQuery(table,cols));MySqlDataReader reader;try{await q.PrepareAsync(token);reader=await q.ExecuteReaderAsync(CommandBehavior.SequentialAccess,token);}catch(MySqlException e){throw Map(e);}
        await foreach(var write in MySqlProtocolRows.ReadAsync(reader,cols,Map,token))yield return write;
        await CheckWarnings(token);await ValidateDefinition(table,token);
    }
    public async IAsyncEnumerable<byte[]> RowsAsync(Table table,[EnumeratorCancellation]CancellationToken token)
    {await foreach(var write in StreamingRowsAsync(table,Enumerable.Range(0,table.Columns.Count).ToArray(),token)){using var stream=new LimitedRow();await write(stream,token);yield return stream.ToArray();}}
    private sealed class LimitedRow:MemoryStream
    {
        private void Check(int n){if(Position+n>ExactRow.MaxRowBytes)throw new VerificationException(Reason.RowTooLarge);}
        public override void Write(ReadOnlySpan<byte> b){Check(b.Length);base.Write(b);}public override void Write(byte[] b,int o,int n){Check(n);base.Write(b,o,n);}public override void WriteByte(byte b){Check(1);base.WriteByte(b);}
    }
}
