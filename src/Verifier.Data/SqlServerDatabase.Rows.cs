using System.Data;
using System.Runtime.CompilerServices;
using Microsoft.Data.SqlClient;
using Verifier.Core;

namespace Verifier.Data;

public sealed partial class SqlServerDatabase
{
    // Only this parameter-free SELECT, generated from authenticated inventory
    // and quoted identifiers, may bypass client SQL text encoding. Keep the
    // general CommandText guard for every other command and user input.
    private SqlCommand RowCommand(string sql)
    {
        if(!SqlServerMetadata.HasUnpairedSurrogate(sql))return Command(sql);
        var command=Command("DECLARE @dbmv_statement nvarchar(max)=CONVERT(nvarchar(max),@dbmv_bytes); EXEC sys.sp_executesql @dbmv_statement;");
        var bytes=new byte[checked(sql.Length*2)];
        for(var i=0;i<sql.Length;i++)System.Buffers.Binary.BinaryPrimitives.WriteUInt16LittleEndian(bytes.AsSpan(i*2),sql[i]);
        command.Parameters.Add("@dbmv_bytes",SqlDbType.VarBinary,-1).Value=bytes;return command;
    }
    internal static string Projection(Column column)=>column.Type=="sql_variant"?SqlServerVariantCodec.Projection(Quote(column.Name)):
        SqlServerRowCodec.Bytes(column.Type)?"CONVERT(varbinary(max),"+Quote(column.Name)+")":Quote(column.Name);
    public async IAsyncEnumerable<Func<Stream,CancellationToken,Task>> StreamingRowsAsync(Table table,IReadOnlyList<int> order,[EnumeratorCancellation]CancellationToken token)
    {
        if(inventory==null||!inventory.Tables.Contains(table))throw new VerificationException(Reason.PermissionDenied);
        if(!table.SafeRead)throw new VerificationException(table.ReadReason);
        if(order.Count==0||order.Distinct().Count()!=order.Count||order.Any(i=>i<0||i>=table.Columns.Count))throw new VerificationException(Reason.InvalidInput);
        var columns=order.Select(i=>table.Columns[i]).ToArray();if(columns.Any(c=>!c.SafeValue))throw new VerificationException(Reason.UnsupportedType);
        try{await ValidateTable(table,token);}catch(SqlException e){throw Map(e,token);}
        await using var q=RowCommand("SELECT "+string.Join(',',columns.Select(Projection))+" FROM "+ReadRelation(table));
        SqlDataReader reader;
        try{reader=await q.ExecuteReaderAsync(CommandBehavior.SequentialAccess,token);}catch(SqlException e){throw Map(e,token);}
        var buffer=new byte[65536];try
        {
            while(await ReadNext(reader,token))
            {
                var available=true;
                yield return async(output,cancel)=>
                {
                    if(!available||reader.IsClosed)throw new VerificationException(Reason.ObservationChanged);available=false;
                    try{await SqlServerRowCodec.WriteAsync(reader,columns,output,cancel,buffer);}catch(SqlException e){throw Map(e,cancel);}
                };
                if(available)throw new VerificationException(Reason.ObservationChanged);
            }
        }
        finally
        {
            System.Security.Cryptography.CryptographicOperations.ZeroMemory(buffer);
            try{await reader.DisposeAsync();}catch(SqlException e){throw Map(e,token);}
        }
        try{await ValidateTable(table,token);}catch(SqlException e){throw Map(e,token);}
    }
    private static async Task<bool> ReadNext(SqlDataReader reader,CancellationToken token)
    {try{return await reader.ReadAsync(token);}catch(SqlException e){throw Map(e,token);}}
    public async IAsyncEnumerable<byte[]> RowsAsync(Table table,[EnumeratorCancellation]CancellationToken token)
    {
        await foreach(var write in StreamingRowsAsync(table,Enumerable.Range(0,table.Columns.Count).ToArray(),token))
        {using var output=new LimitedRow();await write(output,token);yield return output.ToArray();}
    }
    private sealed class LimitedRow:MemoryStream
    {
        private void Check(int n){if(Position+n>ExactRow.MaxRowBytes)throw new VerificationException(Reason.RowTooLarge);}
        public override void Write(ReadOnlySpan<byte> bytes){Check(bytes.Length);base.Write(bytes);}
        public override void Write(byte[] bytes,int offset,int count){Check(count);base.Write(bytes,offset,count);}
        public override void WriteByte(byte value){Check(1);base.WriteByte(value);}
        public override ValueTask WriteAsync(ReadOnlyMemory<byte> bytes,CancellationToken token=default){token.ThrowIfCancellationRequested();Check(bytes.Length);return base.WriteAsync(bytes,token);}
    }
}
