using System.Buffers.Binary;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using Verifier.Core;
[assembly:InternalsVisibleTo("Verifier.Tests")]
namespace Verifier.Data;

public sealed partial class PostgreSqlDatabase
{
    // Version 3 value nodes are self-framed. A normalized child need not be buffered
    // to discover its output size before its parent can be written.
    internal static async Task NormalizeWireAsync(Stream input,Stream output,ValueCodec codec,CancellationToken token)
    {
        using(var writer=new BinaryWriter(output,System.Text.Encoding.UTF8,true))writer.Write(3);
        await NormalizeWireNode(input,null,output,codec,token);
        var tail=new byte[1];if(await input.ReadAsync(tail,token)!=0)throw InvalidCodec();
    }
    private static async Task NormalizeWireNode(Stream input,int? length,Stream output,ValueCodec codec,CancellationToken token)
    {
        token.ThrowIfCancellationRequested();
        using var limited=length is{} n?new WireSlice(input,n):null;var stream=(Stream?)limited??input;
        using var writer=new BinaryWriter(output,System.Text.Encoding.UTF8,true);writer.Write(codec.CastType);
        if(codec.Numeric)
        {
            var header=new byte[8];await Exact(header,stream,token);
            // numeric_send has an unsigned 16-bit digit count, so this buffer is
            // bounded by the wire type itself, independent of row/table size.
            var digits=BinaryPrimitives.ReadUInt16BigEndian(header);var bytes=new byte[8+digits*2];header.CopyTo(bytes,0);await Exact(bytes.AsMemory(8),stream,token);
            var normalized=PostgreSqlArrayCodec.NormalizeNumeric(bytes);writer.Write(normalized.Length);writer.Write(normalized);CryptographicOperations.ZeroMemory(bytes);CryptographicOperations.ZeroMemory(normalized);
        }
        else if(codec.ArrayElement!=0)
        {
            var dimensions=await WireInt(stream,token);var flags=await WireInt(stream,token);var oid=unchecked((uint)await WireInt(stream,token));
            if(dimensions is <0 or >6||flags is <0 or >1||oid!=codec.ArrayElement||codec.Element==null)throw InvalidCodec();
            writer.Write(dimensions);long elements=dimensions==0?0:1;
            for(var i=0;i<dimensions;i++)
            {var count=await WireInt(stream,token);var lower=await WireInt(stream,token);if(count<=0||count>int.MaxValue-(long)lower+1||elements>int.MaxValue/(long)count)throw InvalidCodec();elements*=count;writer.Write(count);writer.Write(lower);}
            for(long i=0;i<elements;i++)
            {var size=await WireInt(stream,token);if(size== -1&&flags==0)throw InvalidCodec();await Child(size,codec.Element);}
        }
        else if(codec.Fields!=null)
        {
            var count=await WireInt(stream,token);if(count!=codec.Fields.Count)throw InvalidCodec();writer.Write(count);
            foreach(var (oid,field) in codec.Fields)
            {if(unchecked((uint)await WireInt(stream,token))!=oid)throw InvalidCodec();await Child(await WireInt(stream,token),field);}
        }
        else if(codec.Range!=null)
        {
            if(codec.MultiRange)
            {
                var count=await WireInt(stream,token);if(count<0)throw InvalidCodec();writer.Write(count);
                for(var i=0;i<count;i++){var size=await WireInt(stream,token);if(size<0)throw InvalidCodec();await NormalizeWireNode(stream,size,output,codec.Range,token);}
            }
            else
            {
                var bytes=new byte[1];await Exact(bytes,stream,token);var flags=bytes[0];if((flags&~31)!=0||(flags&1)!=0&&flags!=1)throw InvalidCodec();writer.Write(flags);
                for(var bound=0;bound<2;bound++)
                {if((flags&1)!=0||(flags&(bound==0?8:16))!=0)continue;var size=await WireInt(stream,token);if(size<0)throw InvalidCodec();await NormalizeWireNode(stream,size,output,codec.Range,token);}
            }
        }
        else
        {
            if(length is not{} size)throw InvalidCodec();writer.Write(size);var buffer=new byte[Math.Min(65536,size)];
            try{while(size>0){var count=Math.Min(buffer.Length,size);await Exact(buffer.AsMemory(0,count),stream,token);writer.Write(buffer.AsSpan(0,count));size-=count;}}
            finally{CryptographicOperations.ZeroMemory(buffer);}
        }
        if(limited is{Remaining:>0})throw InvalidCodec();
        async Task Child(int size,ValueCodec child)
        {if(size< -1)throw InvalidCodec();writer.Write(size!= -1);if(size!= -1)await NormalizeWireNode(stream,size,output,child,token);}
    }
    private static async Task<int> WireInt(Stream stream,CancellationToken token)
    {var bytes=new byte[4];await Exact(bytes,stream,token);return BinaryPrimitives.ReadInt32BigEndian(bytes);}
    private static async Task Exact(Memory<byte> bytes,Stream stream,CancellationToken token)
    {try{await stream.ReadExactlyAsync(bytes,token);}catch(EndOfStreamException){throw InvalidCodec();}}
    private sealed class WireSlice(Stream parent,int length):Stream
    {
        public int Remaining{get;private set;}=length>=0?length:throw InvalidCodec();
        public override async ValueTask<int> ReadAsync(Memory<byte> output,CancellationToken token=default)
        {var count=await parent.ReadAsync(output[..Math.Min(output.Length,Remaining)],token);Remaining-=count;return count;}
        public override int Read(byte[] bytes,int offset,int count){var n=parent.Read(bytes,offset,Math.Min(count,Remaining));Remaining-=n;return n;}
        public override bool CanRead=>true;public override bool CanWrite=>false;public override bool CanSeek=>false;public override long Length=>throw new NotSupportedException();public override long Position{get=>throw new NotSupportedException();set=>throw new NotSupportedException();}
        public override void Flush(){}public override void Write(byte[] b,int o,int c)=>throw new NotSupportedException();public override long Seek(long o,SeekOrigin s)=>throw new NotSupportedException();public override void SetLength(long l)=>throw new NotSupportedException();
    }
    internal sealed class HexWireStream(TextReader reader):Stream
    {
        private readonly char[] chars=new char[4096];private int offset,available,high= -1;private bool ended;
        public override async ValueTask<int> ReadAsync(Memory<byte> output,CancellationToken token=default)
        {
            if(output.IsEmpty)return 0;var written=0;
            while(written<output.Length)
            {
                token.ThrowIfCancellationRequested();
                if(offset==available)
                {
                    if(ended)break;available=await reader.ReadAsync(chars,token);offset=0;
                    if(available==0){ended=true;if(high!= -1)throw InvalidCodec();break;}
                }
                var ch=chars[offset++];var nibble=ch is >= '0' and <= '9'?ch-'0':ch is >= 'a' and <= 'f'?ch-'a'+10:ch is >= 'A' and <= 'F'?ch-'A'+10:-1;
                if(nibble<0)throw InvalidCodec();if(high== -1)high=nibble;else{output.Span[written++]=(byte)(high*16+nibble);high= -1;}
            }
            return written;
        }
        protected override void Dispose(bool disposing){Array.Clear(chars);base.Dispose(disposing);}
        public override int Read(byte[] b,int o,int c)=>throw new NotSupportedException();public override bool CanRead=>true;public override bool CanWrite=>false;public override bool CanSeek=>false;public override long Length=>throw new NotSupportedException();public override long Position{get=>throw new NotSupportedException();set=>throw new NotSupportedException();}
        public override void Flush(){}public override void Write(byte[] b,int o,int c)=>throw new NotSupportedException();public override long Seek(long o,SeekOrigin s)=>throw new NotSupportedException();public override void SetLength(long l)=>throw new NotSupportedException();
    }
}
