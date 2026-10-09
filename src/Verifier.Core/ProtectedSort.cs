using System.Security.Cryptography;
using System.Text;

namespace Verifier.Core;

// Per-run, owner-only authenticated spill. No values/hashes/keys enter permanent reports.
public sealed class ProtectedSort : IDisposable
{
    private readonly string directory;
    private readonly byte[] key=RandomNumberGenerator.GetBytes(32);
    private readonly Guid run=Guid.NewGuid();
    private readonly int budget;
    private readonly List<byte[]> buffer=[];
    private readonly List<(string Path,long Count,byte[] Id)> chunks=[];
    private int used, serial;
    public ProtectedSort(int bufferBytes=8*1024*1024)
    {
        if (OperatingSystem.IsWindows()) throw new VerificationException(Reason.PlatformNotVerified);
        budget=bufferBytes;
        directory=Path.Combine(Path.GetTempPath(),"dmv-"+run.ToString("N"));
        Directory.CreateDirectory(directory,UnixFileMode.UserRead|UnixFileMode.UserWrite|UnixFileMode.UserExecute);
        if (File.GetUnixFileMode(directory) != (UnixFileMode.UserRead|UnixFileMode.UserWrite|UnixFileMode.UserExecute))
            throw new VerificationException(Reason.TemporaryStorage);
    }
    public void Add(byte[] row)
    {
        if (row.Length>ExactRow.MaxRowBytes) throw new VerificationException(Reason.RowTooLarge);
        if (used+row.Length>budget) Flush();
        buffer.Add(row); used+=row.Length;
    }
    private void Flush()
    {
        if (buffer.Count==0) return;
        buffer.Sort(ByteComparer.Instance);
        chunks.Add(Write(buffer));
        buffer.Clear(); used=0;
    }
    public IEnumerable<byte[]> Finish(CancellationToken token)
    {
        Flush();
        while (chunks.Count>16)
        {
            var next=new List<(string Path,long Count,byte[] Id)>();
            foreach (var batch in chunks.Chunk(16))
            {
                token.ThrowIfCancellationRequested();
                next.Add(Write(Merge(batch,token)));
                foreach (var item in batch) File.Delete(item.Path);
            }
            chunks.Clear(); chunks.AddRange(next);
        }
        return Merge(chunks.ToArray(),token);
    }
    private (string Path,long Count,byte[] Id) Write(IEnumerable<byte[]> rows)
    {
        var path=Path.Combine(directory,(serial++).ToString("D8")+".chunk");
        if(OperatingSystem.IsWindows()) throw new VerificationException(Reason.PlatformNotVerified);
        using var stream=new FileStream(path,new FileStreamOptions { Mode=FileMode.CreateNew, Access=FileAccess.Write, UnixCreateMode=UnixFileMode.UserRead|UnixFileMode.UserWrite });
        using var writer=new BinaryWriter(stream);
        using var cipher=new AesGcm(key,16);
        var fileId=RandomNumberGenerator.GetBytes(16);
        writer.Write(fileId);
        long count=0;
        foreach (var row in rows) { Seal(writer,cipher,fileId,count++,row); }
        Seal(writer,cipher,fileId,count,[]); // Authenticated completion marker.
        writer.Flush(); stream.Flush(true);
        return (path,count,fileId);
    }
    private void Seal(BinaryWriter writer,AesGcm cipher,byte[] id,long position,byte[] row)
    {
        var nonce=RandomNumberGenerator.GetBytes(12);
        var tag=new byte[16]; var encrypted=new byte[row.Length];
        cipher.Encrypt(nonce,row,encrypted,tag,Associated(id,position,row.Length));
        writer.Write(row.Length); writer.Write(nonce); writer.Write(tag); writer.Write(encrypted);
    }
    private byte[] Associated(byte[] id,long position,int length)
    {
        using var m=new MemoryStream(); using var w=new BinaryWriter(m,Encoding.UTF8,true);
        w.Write(run.ToByteArray()); w.Write(id); w.Write(position); w.Write(length); return m.ToArray();
    }
    private IEnumerable<byte[]> Read((string Path,long Count,byte[] Id) chunk)
    {
        if ((File.GetAttributes(chunk.Path)&FileAttributes.ReparsePoint)!=0) throw new VerificationException(Reason.TemporaryIntegrity);
        using var stream=File.OpenRead(chunk.Path); using var reader=new BinaryReader(stream); using var cipher=new AesGcm(key,16);
        var id=reader.ReadBytes(16);
        if(!id.AsSpan().SequenceEqual(chunk.Id)) throw new VerificationException(Reason.TemporaryIntegrity);
        for (long i=0;i<=chunk.Count;i++)
        {
            byte[] row;
            try
            {
                var length=reader.ReadInt32();
                if (length<0 || length>ExactRow.MaxRowBytes || (i==chunk.Count ? length!=0 : length==0)) throw new VerificationException(Reason.TemporaryIntegrity);
                var nonce=reader.ReadBytes(12); var tag=reader.ReadBytes(16); var bytes=reader.ReadBytes(length); row=new byte[length];
                if (bytes.Length!=length || nonce.Length!=12 || tag.Length!=16) throw new VerificationException(Reason.TemporaryIntegrity);
                cipher.Decrypt(nonce,bytes,tag,row,Associated(id,i,length));
            }
            catch (Exception e) when (e is EndOfStreamException or CryptographicException or ArgumentException)
            { throw new VerificationException(Reason.TemporaryIntegrity); }
            if (i<chunk.Count) yield return row;
        }
        if (stream.Position!=stream.Length) throw new VerificationException(Reason.TemporaryIntegrity);
    }
    private IEnumerable<byte[]> Merge(IEnumerable<(string Path,long Count,byte[] Id)> files,CancellationToken token)
    {
        var iterators=files.Select(f=>Read(f).GetEnumerator()).ToArray();
        var queue=new PriorityQueue<int,byte[]>(ByteComparer.Instance);
        try
        {
            for(var i=0;i<iterators.Length;i++) if(iterators[i].MoveNext()) queue.Enqueue(i,iterators[i].Current);
            while(queue.TryDequeue(out var index,out var row))
            {
                token.ThrowIfCancellationRequested(); yield return row;
                if(iterators[index].MoveNext()) queue.Enqueue(index,iterators[index].Current);
            }
        }
        finally { foreach(var iterator in iterators) iterator.Dispose(); }
    }
    public void Dispose()
    {
        CryptographicOperations.ZeroMemory(key);
        foreach(var row in buffer) CryptographicOperations.ZeroMemory(row);
        if (Directory.Exists(directory) && (File.GetAttributes(directory)&FileAttributes.ReparsePoint)==0) Directory.Delete(directory,true);
    }
    public sealed class ByteComparer : IComparer<byte[]>
    {
        public static readonly ByteComparer Instance=new();
        public int Compare(byte[]? x,byte[]? y) => x.AsSpan().SequenceCompareTo(y);
    }
}
