using System.Buffers.Binary;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Text;
using MySqlConnector;
using Verifier.Core;
namespace Verifier.Data;

// The driver binary protocol and frame validation are shared; DBMS inventory,
// SQL generation, transaction guarantees and definition guards remain adapters.
internal static class MySqlProtocolRows
{
    private static bool Floating(Column c)=>c.Type.Split([' ','('],2)[0] is "float" or "double";
    internal static async IAsyncEnumerable<Func<Stream,CancellationToken,Task>> ReadAsync(MySqlDataReader reader,IReadOnlyList<Column> columns,Func<MySqlException,VerificationException> map,[EnumeratorCancellation]CancellationToken token)
    {
        var buffer=new byte[65536];ulong previous=0;
        try
        {
            await using(reader)
            {
                var has=await Read(reader,token,map);
                while(has)
                {
                    var row=reader.GetUInt64(0);if(row!=previous+1)throw new VerificationException(Reason.ObservationChanged);previous=row;var available=true;
                    yield return async(output,cancel)=>
                    {
                        if(!available||reader.IsClosed)throw new VerificationException(Reason.ObservationChanged);available=false;
                        using var header=new BinaryWriter(output,Encoding.UTF8,true);header.Write(3);header.Write(columns.Count);
                        for(var col=0;col<columns.Count;col++)
                        {
                            cancel.ThrowIfCancellationRequested();if(!has||reader.GetUInt64(0)!=row||reader.GetInt32(1)!=col||reader.GetUInt64(4)!=0)throw new VerificationException(Reason.ObservationChanged);
                            var present=reader.GetBoolean(2);var length=reader.IsDBNull(3)?0:reader.GetInt64(3);if(length<0||!present&&!reader.IsDBNull(3))throw new VerificationException(Reason.ObservationChanged);header.Write(present);
                            var parts=Math.Max(1,(length+65535)/65536);using var cell=new ChunkCell(output);
                            for(long part=0;part<parts;part++)
                            {
                                cancel.ThrowIfCancellationRequested();if(!has||reader.GetUInt64(0)!=row||reader.GetInt32(1)!=col||reader.GetUInt64(4)!=(ulong)part||reader.GetBoolean(2)!=present||(reader.IsDBNull(3)?0:reader.GetInt64(3))!=length)throw new VerificationException(Reason.ObservationChanged);
                                if(!present&&(!reader.IsDBNull(5)||!reader.IsDBNull(6)))throw new VerificationException(Reason.ObservationChanged);
                                if(present&&Floating(columns[col]))
                                {if(length!=8||part!=0||!reader.IsDBNull(5)||reader.IsDBNull(6))throw new VerificationException(Reason.ObservationChanged);BinaryPrimitives.WriteInt64LittleEndian(buffer,BitConverter.DoubleToInt64Bits(reader.GetDouble(6)));cell.Write(buffer.AsSpan(0,8));}
                                else if(present)
                                {if(!reader.IsDBNull(6))throw new VerificationException(Reason.ObservationChanged);var n=reader.GetBytes(5,0,buffer,0,buffer.Length);if(n!=Math.Min(65536,length-part*65536)||reader.GetBytes(5,0,null,0,0)!=n)throw new VerificationException(Reason.ObservationChanged);cell.Write(buffer.AsSpan(0,(int)n));}
                                has=await Read(reader,cancel,map);
                            }
                            if(present)cell.Complete();
                        }
                        if(has&&reader.GetUInt64(0)==row)throw new VerificationException(Reason.ObservationChanged);
                    };
                    if(available)throw new VerificationException(Reason.ObservationChanged);
                }
            }
        }
        finally{CryptographicOperations.ZeroMemory(buffer);}
    }
    private static async Task<bool> Read(MySqlDataReader reader,CancellationToken token,Func<MySqlException,VerificationException> map)
    {try{return await reader.ReadAsync(token);}catch(MySqlException e){throw map(e);}}
    private sealed class ChunkCell(Stream output):Stream
    {
        private readonly byte[] bytes=new byte[65536];private int used;private bool complete;
        public override void Write(ReadOnlySpan<byte> source)
        {if(complete)throw new VerificationException(Reason.TemporaryIntegrity);while(!source.IsEmpty){var n=Math.Min(source.Length,bytes.Length-used);source[..n].CopyTo(bytes.AsSpan(used));used+=n;source=source[n..];if(used==bytes.Length)Block();}}
        private void Block(){Span<byte> size=stackalloc byte[4];BinaryPrimitives.WriteInt32LittleEndian(size,used);output.Write(size);output.Write(bytes.AsSpan(0,used));CryptographicOperations.ZeroMemory(bytes);used=0;}
        public void Complete(){if(used>0)Block();output.Write(new byte[4]);complete=true;}
        protected override void Dispose(bool disposing){CryptographicOperations.ZeroMemory(bytes);base.Dispose(disposing);}
        public override void Write(byte[] b,int o,int n)=>Write(b.AsSpan(o,n));public override void Flush(){}
        public override bool CanRead=>false;public override bool CanSeek=>false;public override bool CanWrite=>!complete;
        public override long Length=>throw new NotSupportedException();public override long Position{get=>throw new NotSupportedException();set=>throw new NotSupportedException();}
        public override int Read(byte[] b,int o,int n)=>throw new NotSupportedException();public override long Seek(long o,SeekOrigin s)=>throw new NotSupportedException();public override void SetLength(long n)=>throw new NotSupportedException();
    }
}
