using System.Security.Cryptography;

namespace Verifier.Core;

// Leaves both collaborators open. Hashing has no transform finalization that
// could inadvertently finalize a nested base64 stream or the atomic file writer.
internal sealed class HashingStream(Stream inner,IncrementalHash hash):Stream
{
    public override async ValueTask<int> ReadAsync(Memory<byte> buffer,CancellationToken token=default)
    {var count=await inner.ReadAsync(buffer,token);hash.AppendData(buffer.Span[..count]);return count;}
    public override int Read(byte[] buffer,int offset,int count)
    {var read=inner.Read(buffer,offset,count);hash.AppendData(buffer,offset,read);return read;}
    public override Task<int> ReadAsync(byte[] buffer,int offset,int count,CancellationToken token)=>ReadAsync(buffer.AsMemory(offset,count),token).AsTask();
    public override async ValueTask WriteAsync(ReadOnlyMemory<byte> buffer,CancellationToken token=default)
    {await inner.WriteAsync(buffer,token);hash.AppendData(buffer.Span);}
    public override void Write(byte[] buffer,int offset,int count){inner.Write(buffer,offset,count);hash.AppendData(buffer,offset,count);}
    public override Task WriteAsync(byte[] buffer,int offset,int count,CancellationToken token)=>WriteAsync(buffer.AsMemory(offset,count),token).AsTask();
    public override void Flush()=>inner.Flush();public override Task FlushAsync(CancellationToken token)=>inner.FlushAsync(token);
    public override bool CanRead=>inner.CanRead;public override bool CanWrite=>inner.CanWrite;public override bool CanSeek=>false;
    public override long Length=>throw new NotSupportedException();public override long Position{get=>throw new NotSupportedException();set=>throw new NotSupportedException();}
    public override long Seek(long offset,SeekOrigin origin)=>throw new NotSupportedException();public override void SetLength(long value)=>throw new NotSupportedException();
}
