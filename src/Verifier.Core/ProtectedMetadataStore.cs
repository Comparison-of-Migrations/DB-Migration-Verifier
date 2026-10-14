using System.Buffers.Binary;
using System.Collections;
using System.Security.Cryptography;
using System.Text.Json;
using System.Text.Json.Serialization;
namespace Verifier.Core;

// Append-only result metadata, not raw DB values. Reuses the existing encrypted
// row blocks/owner-only lease, with an ordinal-authenticated fixed-width disk
// index. Snapshot memory is independent of the total record count.
internal sealed class ProtectedMetadataStore<T>:IDisposable where T:class
{
    private const int EntrySize=96;
    private static readonly JsonSerializerOptions Options=new()
    {UnmappedMemberHandling=JsonUnmappedMemberHandling.Disallow,RespectRequiredConstructorParameters=true,RespectNullableAnnotations=true,AllowDuplicateProperties=false,Converters={new ExactJsonStringConverter()}};
    private readonly object gate=new();
    private readonly byte[] key=RandomNumberGenerator.GetBytes(32),run=RandomNumberGenerator.GetBytes(16);
    private readonly PrivateWorkspace workspace;
    private readonly ProtectedRowStore rows;
    private readonly FileStream index;
    private int count;
    private bool sealedStore,disposed,broken;
    internal string IndexPath{get;}
    internal int Count{get{lock(gate){EnsureOpen();return count;}}}
    private readonly Predicate<T> valid;
    internal ProtectedMetadataStore(Predicate<T> valid)
    {
        this.valid=valid;
        try{workspace=new("sort");}catch{CryptographicOperations.ZeroMemory(key);throw;}
        IndexPath=Path.Combine(workspace.DirectoryPath,"00000000.chunk");
        try{index=PrivateStorage.CreateFile(IndexPath);}
        catch{workspace.Dispose();CryptographicOperations.ZeroMemory(key);throw;}
        try{rows=new();}
        catch{index.Dispose();workspace.Dispose();CryptographicOperations.ZeroMemory(key);throw;}
    }
    internal void Add(T item)
    {
        lock(gate)
        {
            EnsureOpen();if(sealedStore)throw new InvalidOperationException();
            if(item==null||!valid(item))throw new VerificationException(Reason.InvalidInput);
            if(count==int.MaxValue)throw new VerificationException(Reason.RowTooLarge);
            CheckIndex();
            // Native identifiers/fixed kind/ID form a small metadata record.
            // This cap never truncates: exceptional oversized metadata aborts.
            var bytes=JsonSerializer.SerializeToUtf8Bytes(item,Options);
            byte[] descriptor;
            try
            {
                if(bytes.Length>ExactRow.MaxRowBytes)throw new VerificationException(Reason.RowTooLarge);
                try{descriptor=rows.AddMetadataAsync(bytes).GetAwaiter().GetResult();}
                catch(Exception e)when(e is IOException or UnauthorizedAccessException or VerificationException)
                {rows.DiscardIncompleteTail();throw;}
            }
            finally{CryptographicOperations.ZeroMemory(bytes);}
            Span<byte> entry=stackalloc byte[EntrySize];descriptor.CopyTo(entry);
            Authenticate(count,descriptor).CopyTo(entry[64..]);
            var completed=checked((long)count*EntrySize);index.Position=completed;
            try{index.Write(entry);index.Flush();count++;}
            catch(Exception e)when(e is IOException or UnauthorizedAccessException)
            {index.SetLength(completed);index.Position=completed;index.Flush();throw;}
        }
    }
    // Publish a range only after all records complete. A cancelled/failed
    // enumeration restores both encrypted payload and ordinal index, so
    // repeated attempts cannot accumulate authenticated orphan records.
    // The range consumer binds retries to a fresh batch identity: ordinal
    // authentication alone cannot distinguish rolled-back ciphertext replay.
    internal IReadOnlyList<T> Append(IEnumerable<T> values)
    {
        lock(gate)
        {
            EnsureOpen();if(sealedStore)throw new InvalidOperationException();CheckIndex();
            var start=count;var checkpoint=rows.MetadataCheckpoint();
            try{foreach(var item in values)Add(item);return Snapshot();}
            catch
            {
                try{index.SetLength(checked((long)start*EntrySize));index.Position=index.Length;index.Flush(true);rows.RollbackMetadata(checkpoint);count=start;}
                catch{broken=true;throw new VerificationException(Reason.TemporaryStorage);}
                throw;
            }
        }
    }
    internal IReadOnlyList<T> Snapshot()
    {lock(gate){EnsureOpen();CheckIndex();return new View(this,count);}}
    internal IReadOnlyList<T> Seal()
    {lock(gate){EnsureOpen();CheckIndex();rows.Flush();index.Flush(true);sealedStore=true;return new View(this,count);}}
    private byte[] Authenticate(int ordinal,ReadOnlySpan<byte> descriptor)
    {
        Span<byte> associated=stackalloc byte[88];run.CopyTo(associated);BinaryPrimitives.WriteInt64LittleEndian(associated[16..],ordinal);descriptor.CopyTo(associated[24..]);
        return HMACSHA256.HashData(key,associated);
    }
    private T Read(int ordinal,int snapshotCount)
    {
        lock(gate)
        {
            EnsureOpen();if(ordinal<0||ordinal>=snapshotCount)throw new ArgumentOutOfRangeException(nameof(ordinal));
            if(snapshotCount>count)throw Invalid();CheckIndex();
            Span<byte> entry=stackalloc byte[EntrySize];var offset=checked((long)ordinal*EntrySize);var used=0;
            while(used<EntrySize)
            {var n=RandomAccess.Read(index.SafeFileHandle,entry[used..],offset+used);if(n==0)throw Invalid();used+=n;}
            if(!CryptographicOperations.FixedTimeEquals(entry[64..],Authenticate(ordinal,entry[..64])))throw Invalid();
            using var stream=rows.OpenRead(entry[..64].ToArray());
            try
            {
                var item=JsonSerializer.Deserialize<T>(stream,Options)??throw Invalid();
                if(stream.ReadByte()!=-1||!valid(item))throw Invalid();
                return item;
            }
            catch(JsonException){throw Invalid();}
        }
    }
    private void CheckIndex()
    {
        if((File.GetAttributes(IndexPath)&FileAttributes.ReparsePoint)!=0||index.Length!=checked((long)count*EntrySize))throw Invalid();
    }
    private void EnsureOpen(){ObjectDisposedException.ThrowIf(disposed,this);if(broken)throw Invalid();}
    private static VerificationException Invalid()=>new(Reason.TemporaryIntegrity);
    public void Dispose()
    {
        lock(gate)
        {
            if(disposed)return;disposed=true;
            try{index.Dispose();}finally{try{rows.Dispose();}finally{CryptographicOperations.ZeroMemory(key);workspace.Dispose();}}
        }
    }
    private sealed class View(ProtectedMetadataStore<T> owner,int count):IFrozenMetadata<T>
    {
        public int Count=>count;
        public T this[int index]=>owner.Read(index,count);
        public IEnumerator<T> GetEnumerator(){for(var i=0;i<count;i++)yield return owner.Read(i,count);}
        IEnumerator IEnumerable.GetEnumerator()=>GetEnumerator();
    }
}
