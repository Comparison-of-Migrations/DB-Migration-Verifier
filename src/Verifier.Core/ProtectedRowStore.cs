using System.Buffers.Binary;
using System.Security.Cryptography;
namespace Verifier.Core;

// Exact comparison of arbitrarily long rows with bounded authenticated blocks.
// Digests only order candidates. Every equal digest is compared using the full row stream.
public sealed class ProtectedRowStore : IDisposable,IComparer<byte[]>
{
    private const int BlockSize=65536;
    private readonly string directory;
    private readonly PrivateWorkspace workspace;
    private readonly string path;
    private readonly FileStream file;
    private readonly byte[] key=RandomNumberGenerator.GetBytes(32);
    private readonly byte[] run=Guid.NewGuid().ToByteArray();
    private readonly Func<byte[],byte[]> transformDigest;
    private bool disposed;private readonly CancellationToken token;private long completedLength;
    public ProtectedRowStore(Func<byte[],byte[]>? digestTransform=null,CancellationToken token=default)
    {
        this.token=token;transformDigest=digestTransform??(digest=>digest);
        try{workspace=new("rows");}catch{CryptographicOperations.ZeroMemory(key);throw;}
        directory=workspace.DirectoryPath;path=Path.Combine(directory,"rows.chunk");
        try{file=PrivateStorage.CreateFile(path);}catch{try{workspace.Dispose();}finally{CryptographicOperations.ZeroMemory(key);}throw;}
    }
    public Task<byte[]> AddAsync(Func<Stream,CancellationToken,Task> write,CancellationToken token)=>AddAsync(write,token,BlockSize);
    internal Task<byte[]> AddMetadataAsync(ReadOnlyMemory<byte> bytes)
        =>AddAsync((stream,_)=>{stream.Write(bytes.Span);return Task.CompletedTask;},default,Math.Min(BlockSize,Math.Max(256,bytes.Length)));
    internal async Task<byte[]> AddAsync(Func<Stream,CancellationToken,Task> write,CancellationToken token,int bufferSize)
    {
        ObjectDisposedException.ThrowIf(disposed,this);token.ThrowIfCancellationRequested();
        var start=file.Position;var id=RandomNumberGenerator.GetBytes(16);file.Write(id);
        using var output=new RowWriter(this,id,bufferSize);await write(output,token).ConfigureAwait(false);token.ThrowIfCancellationRequested();
        var digest=output.Complete();file.Flush();completedLength=file.Position;
        var descriptor=new byte[64];digest.CopyTo(descriptor,0);BinaryPrimitives.WriteInt64LittleEndian(descriptor.AsSpan(32),start);BinaryPrimitives.WriteInt64LittleEndian(descriptor.AsSpan(40),file.Position);id.CopyTo(descriptor,48);return descriptor;
    }
    public void Flush(){ObjectDisposedException.ThrowIf(disposed,this);if(file.Length!=completedLength)throw Invalid();file.Flush(true);}
    internal long MetadataCheckpoint(){Flush();return completedLength;}
    internal void RollbackMetadata(long position)
    {
        ObjectDisposedException.ThrowIf(disposed,this);
        if(position<0||position>completedLength||file.Length<position||(File.GetAttributes(path)&FileAttributes.ReparsePoint)!=0)throw Invalid();
        file.SetLength(position);file.Position=position;file.Flush(true);completedLength=position;
    }
    // Result-metadata append failure recovery only. Completed authenticated rows
    // remain immutable; an unfinished tail can never become resumable input.
    internal void DiscardIncompleteTail()
    {
        ObjectDisposedException.ThrowIf(disposed,this);
        if((File.GetAttributes(path)&FileAttributes.ReparsePoint)!=0||file.Length<completedLength)throw Invalid();
        file.SetLength(completedLength);file.Position=completedLength;file.Flush();
    }
    public int Compare(byte[]? left,byte[]? right)
    {
        if(left is not{Length:64}||right is not{Length:64})throw Invalid();
        var digest=left.AsSpan(0,32).SequenceCompareTo(right.AsSpan(0,32));if(digest!=0)return digest;
        using var a=Open(left);using var b=Open(right);
        var aa=new byte[BlockSize];var bb=new byte[BlockSize];int comparison=0;
        while(true)
        {
            token.ThrowIfCancellationRequested();var n=a.ReadAtLeast(aa,BlockSize,false);var m=b.ReadAtLeast(bb,BlockSize,false);
            if(comparison==0){comparison=aa.AsSpan(0,Math.Min(n,m)).SequenceCompareTo(bb.AsSpan(0,Math.Min(n,m)));if(comparison==0)comparison=n.CompareTo(m);}
            if(n==0&&m==0)return comparison;
        }
    }
    public void Validate(byte[] descriptor,CancellationToken token)
    {
        using var row=Open(descriptor);var buffer=new byte[BlockSize];
        while(row.Read(buffer)>0)token.ThrowIfCancellationRequested();
    }
    // Callers must read through EOF to verify the authenticated completion record.
    internal Stream OpenRead(byte[] descriptor)
    {ObjectDisposedException.ThrowIf(disposed,this);return Open(descriptor);}
    private RowReader Open(byte[] descriptor)
    {
        if(descriptor.Length!=64||(File.GetAttributes(path)&FileAttributes.ReparsePoint)!=0)throw Invalid();
        return new(this,descriptor);
    }
    private byte[] Associated(byte[] id,long index,int length)
    {var result=new byte[44];run.CopyTo(result,0);id.CopyTo(result,16);BinaryPrimitives.WriteInt64LittleEndian(result.AsSpan(32),index);BinaryPrimitives.WriteInt32LittleEndian(result.AsSpan(40),length);return result;}
    private void Seal(byte[] id,long index,ReadOnlySpan<byte> bytes)
    {
        var nonce=RandomNumberGenerator.GetBytes(12);var tag=new byte[16];var encrypted=new byte[bytes.Length];using var cipher=new AesGcm(key,16);
        cipher.Encrypt(nonce,bytes,encrypted,tag,Associated(id,index,bytes.Length));
        Span<byte> length=stackalloc byte[4];BinaryPrimitives.WriteInt32LittleEndian(length,bytes.Length);file.Write(length);file.Write(nonce);file.Write(tag);file.Write(encrypted);
    }
    private byte[] Digest(IncrementalHash hash)
    {var result=transformDigest(hash.GetHashAndReset());if(result.Length!=32)throw Invalid();return result;}
    public void Dispose()
    {
        if(disposed)return;disposed=true;try{file.Dispose();}finally{CryptographicOperations.ZeroMemory(key);workspace.Dispose();}
    }
    private static VerificationException Invalid()=>new(Reason.TemporaryIntegrity);
    private sealed class RowWriter(ProtectedRowStore owner,byte[] id,int bufferSize) : Stream
    {
        private readonly byte[] buffer=new byte[bufferSize];private readonly IncrementalHash hash=IncrementalHash.CreateHash(HashAlgorithmName.SHA256);private int used;private long index;private bool completed;
        public override void Write(ReadOnlySpan<byte> bytes)
        {
            if(completed)throw new InvalidOperationException();hash.AppendData(bytes);
            while(!bytes.IsEmpty){owner.token.ThrowIfCancellationRequested();var n=Math.Min(buffer.Length-used,bytes.Length);bytes[..n].CopyTo(buffer.AsSpan(used));used+=n;bytes=bytes[n..];if(used==buffer.Length)FlushBlock();}
        }
        public override void Write(byte[] bytes,int offset,int count)=>Write(bytes.AsSpan(offset,count));
        private void FlushBlock(){if(used==0)return;owner.Seal(id,index++,buffer.AsSpan(0,used));CryptographicOperations.ZeroMemory(buffer);used=0;}
        public byte[] Complete(){FlushBlock();owner.Seal(id,index,[]);completed=true;return owner.Digest(hash);}
        protected override void Dispose(bool disposing){CryptographicOperations.ZeroMemory(buffer);hash.Dispose();base.Dispose(disposing);}
        public override bool CanRead=>false;public override bool CanSeek=>false;public override bool CanWrite=>!completed;public override long Length=>throw new NotSupportedException();public override long Position{get=>throw new NotSupportedException();set=>throw new NotSupportedException();}
        public override void Flush(){}public override int Read(byte[] b,int o,int c)=>throw new NotSupportedException();public override long Seek(long o,SeekOrigin origin)=>throw new NotSupportedException();public override void SetLength(long v)=>throw new NotSupportedException();
    }
    private sealed class RowReader : Stream
    {
        private readonly ProtectedRowStore owner;private readonly FileStream input;private readonly byte[] id;private readonly byte[] digest;private readonly long end;private readonly IncrementalHash hash=IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
        private byte[] buffer=[];private int position;private long index;private bool completed;
        internal RowReader(ProtectedRowStore owner,byte[] descriptor)
        {
            this.owner=owner;id=descriptor[48..64];digest=descriptor[..32];var offset=BinaryPrimitives.ReadInt64LittleEndian(descriptor.AsSpan(32));end=BinaryPrimitives.ReadInt64LittleEndian(descriptor.AsSpan(40));
            input=new FileStream(owner.path,FileMode.Open,FileAccess.Read,FileShare.ReadWrite,1,FileOptions.RandomAccess);
            try{if(input.Length!=owner.completedLength||offset<0||end-offset<48||end>input.Length)throw Invalid();input.Position=offset;var header=new byte[16];input.ReadExactly(header);if(!header.AsSpan().SequenceEqual(id))throw Invalid();}catch{input.Dispose();hash.Dispose();throw;}
        }
        private void Next()
        {
            try
            {
                // Read the fixed header together without caching later blocks:
                // every access still authenticates current ciphertext through EOF.
                if(input.Position+32>end)throw Invalid();Span<byte> header=stackalloc byte[32];input.ReadExactly(header);var n=BinaryPrimitives.ReadInt32LittleEndian(header);
                if(n<0||n>BlockSize||input.Position+n>end)throw Invalid();
                var bytes=new byte[n];input.ReadExactly(bytes);
                CryptographicOperations.ZeroMemory(buffer);buffer=new byte[n];position=0;using var cipher=new AesGcm(owner.key,16);cipher.Decrypt(header.Slice(4,12),bytes,header[16..],buffer,owner.Associated(id,index++,n));
                if(n==0){if(input.Position!=end||!owner.Digest(hash).AsSpan().SequenceEqual(digest))throw Invalid();completed=true;}else hash.AppendData(buffer);
            }
            catch(Exception e) when(e is EndOfStreamException or CryptographicException or ArgumentException){throw Invalid();}
        }
        public override int Read(Span<byte> output)
        {
            if(output.IsEmpty)return 0;if(position==buffer.Length&&!completed)Next();if(completed)return 0;
            var n=Math.Min(output.Length,buffer.Length-position);buffer.AsSpan(position,n).CopyTo(output);position+=n;return n;
        }
        public override int Read(byte[] bytes,int offset,int count)=>Read(bytes.AsSpan(offset,count));
        protected override void Dispose(bool disposing){input.Dispose();hash.Dispose();CryptographicOperations.ZeroMemory(buffer);base.Dispose(disposing);}
        public override bool CanRead=>true;public override bool CanSeek=>false;public override bool CanWrite=>false;public override long Length=>throw new NotSupportedException();public override long Position{get=>throw new NotSupportedException();set=>throw new NotSupportedException();}
        public override void Flush(){}public override void Write(byte[] b,int o,int c)=>throw new NotSupportedException();public override long Seek(long o,SeekOrigin origin)=>throw new NotSupportedException();public override void SetLength(long v)=>throw new NotSupportedException();
    }
}
