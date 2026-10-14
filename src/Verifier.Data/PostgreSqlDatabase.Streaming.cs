using System.Data;
using System.Runtime.CompilerServices;
using System.Text;
using Npgsql;
using Verifier.Core;
namespace Verifier.Data;

public sealed partial class PostgreSqlDatabase
{
    private string ValueProjection(Column column,bool streaming)
    {
        if(streaming&&column.Type=="bytea")return Quote(column.Name);
        if(customValueCodecs.TryGetValue(column.TypeOid,out var codec))return CustomProjection(column,codec);
        if(ArrayCodecs.Contains(column.Type))return "pg_catalog.encode(pg_catalog.array_send("+Quote(column.Name)+"),'hex')";
        if(BinaryCodecs.TryGetValue(column.Type,out var send))return "pg_catalog.encode(pg_catalog."+send+"("+Quote(column.Name)+"),'hex')";
        if(column.Type=="bytea")return "pg_catalog.encode("+Quote(column.Name)+",'hex')";
        return column.Type is "text" or "varchar" or "bpchar"?Quote(column.Name):Quote(column.Name)+"::pg_catalog.text";
    }
    public async IAsyncEnumerable<Func<Stream,CancellationToken,Task>> StreamingRowsAsync(Table table,IReadOnlyList<int> order,[EnumeratorCancellation]CancellationToken token)
    {
        if(inventory==null||!inventory.Tables.Contains(table))throw new VerificationException(Reason.PermissionDenied);
        if(!table.SafeRead)throw new VerificationException(table.ReadReason);
        if(order.Distinct().Count()!=order.Count||order.Any(i=>i<0||i>=table.Columns.Count))throw new VerificationException(Reason.UnsupportedType);
        var columns=order.Select(i=>table.Columns[i]).ToArray();
        if(columns.Any(c=>!c.SafeValue))throw new VerificationException(Reason.UnsupportedType);
        EnsureReadSession(token);
        await ValidateReadScope(table,token);
        var sql="SELECT "+string.Join(',',columns.Select(c=>ValueProjection(c,true)))+" FROM "+(table.DataDescendants.Count>0?"":"ONLY ")+Quote(table.Schema)+"."+Quote(table.Name);
        await using var command=new NpgsqlCommand(sql,connection,transaction);NpgsqlDataReader reader;
        try{reader=await command.ExecuteReaderAsync(CommandBehavior.SequentialAccess,token);}catch(NpgsqlException e){throw ReadFailure(e);}catch(InvalidOperationException) when(connection?.State!=ConnectionState.Open){throw LostReadSession();}
        await using(reader)
        {
            while(await Read(reader,token))
            {
                var valid=true;
                yield return async(output,cancel)=>
                {
                    if(!valid)throw new VerificationException(Reason.ObservationChanged);
                    valid=false;
                    await WriteProjectedRow(reader,columns,output,cancel);
                };
                valid=false; // Reader-bound callbacks cannot be retained beyond their row.
            }
        }
        await ValidateReadScope(table,token);
    }
    private async Task WriteProjectedRow(NpgsqlDataReader reader,Column[] columns,Stream output,CancellationToken cancel,int offset=0)
    {
        try
        {
        using var header=new BinaryWriter(output,Encoding.UTF8,true);header.Write(3);header.Write(columns.Length);
        for(var i=0;i<columns.Length;i++)
        {
            cancel.ThrowIfCancellationRequested();var column=columns[i];var present=!await reader.IsDBNullAsync(i+offset,cancel);header.Write(present);if(!present)continue;
            using var cell=new FramedCell(output);
            if(column.Type=="bytea")
            {await using var bytes=reader.GetStream(i+offset);await bytes.CopyToAsync(cell,65536,cancel);}
            else
            {
                using var text=reader.GetTextReader(i+offset);
                if(customValueCodecs.TryGetValue(column.TypeOid,out var custom)&&custom.RequiresNormalization)
                {using var wire=new HexWireStream(text);await NormalizeWireAsync(wire,cell,custom,cancel);cell.Complete();continue;}
                var canonical=column.Type=="numeric";
                using var writer=new StreamWriter(cell,new UTF8Encoding(false,true),4096,true);
                var buffer=new char[4096];int count;
                if(canonical)
                {
                    var value=new StringBuilder();
                    while((count=await text.ReadAsync(buffer.AsMemory(),cancel))>0)
                    {if((long)(value.Length+count)*4>ExactRow.MaxRowBytes)throw new VerificationException(Reason.RowTooLarge);value.Append(buffer,0,count);}
                    var result=value.ToString();
                    result=ExactRow.CanonicalNumber(result);
                    await writer.WriteAsync(result.AsMemory(),cancel);
                }
                else
                {
                    if(customValueCodecs.TryGetValue(column.TypeOid,out var plain))await writer.WriteAsync((plain.CastType+"\0").AsMemory(),cancel);
                    while((count=await text.ReadAsync(buffer.AsMemory(),cancel))>0)await writer.WriteAsync(buffer.AsMemory(0,count),cancel);
                }
                await writer.FlushAsync(cancel);Array.Clear(buffer);
            }
            cell.Complete();
        }
        }
        catch(NpgsqlException e){throw ReadFailure(e);}catch(InvalidOperationException) when(connection?.State!=ConnectionState.Open){throw LostReadSession();}
    }
    // Fixed block boundaries make the row independent of driver/read chunk sizes.
    private sealed class FramedCell(Stream output) : Stream
    {
        private readonly byte[] buffer=new byte[65536];private int used;private bool completed;
        public override void Write(ReadOnlySpan<byte> bytes)
        {
            if(completed)throw new InvalidOperationException();
            while(!bytes.IsEmpty){var n=Math.Min(buffer.Length-used,bytes.Length);bytes[..n].CopyTo(buffer.AsSpan(used));used+=n;bytes=bytes[n..];if(used==buffer.Length)Block();}
        }
        private void Block(){Span<byte> length=stackalloc byte[4];System.Buffers.Binary.BinaryPrimitives.WriteInt32LittleEndian(length,used);output.Write(length);output.Write(buffer.AsSpan(0,used));System.Security.Cryptography.CryptographicOperations.ZeroMemory(buffer);used=0;}
        public void Complete(){if(used>0)Block();output.Write(new byte[4]);completed=true;}
        public override void Write(byte[] b,int offset,int count)=>Write(b.AsSpan(offset,count));
        protected override void Dispose(bool disposing){System.Security.Cryptography.CryptographicOperations.ZeroMemory(buffer);base.Dispose(disposing);}
        public override void Flush(){}public override bool CanRead=>false;public override bool CanSeek=>false;public override bool CanWrite=>!completed;public override long Length=>throw new NotSupportedException();public override long Position{get=>throw new NotSupportedException();set=>throw new NotSupportedException();}
        public override int Read(byte[] b,int o,int c)=>throw new NotSupportedException();public override long Seek(long o,SeekOrigin s)=>throw new NotSupportedException();public override void SetLength(long l)=>throw new NotSupportedException();
    }
}
