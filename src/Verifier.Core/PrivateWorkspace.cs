using System.Text;
using System.Text.RegularExpressions;
using System.Security.Cryptography;

namespace Verifier.Core;

// The lease survives as an ownership marker, never as resumable comparison data.
// Its OS lock is released even on process termination. No keys or values are stored.
internal sealed class PrivateWorkspace : IDisposable
{
    private static readonly string Root=Path.Combine(Path.GetTempPath(),"db-migration-verifier-spill-v1");
    private static readonly Mutex Gate=new(false,"DBMV-SPILL-V1-"+Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(Root))));
    private static readonly Regex Name=new("^(rows|sort)-[a-f0-9]{32}$",RegexOptions.CultureInvariant);
    private static readonly Regex Chunk=new("^[0-9]{8,10}\\.chunk$",RegexOptions.CultureInvariant);
    private const string LeaseName="owner.lease";
    private readonly FileStream lease;
    private bool disposed;
    internal string DirectoryPath {get;}

    internal PrivateWorkspace(string kind)
    {
        if(kind is not("rows" or "sort"))throw new ArgumentOutOfRangeException(nameof(kind));
        using var gate=new GateLease();
        PrivateStorage.CreateDirectory(Root);CleanAbandoned();
        DirectoryPath=Path.Combine(Root,kind+"-"+Guid.NewGuid().ToString("N"));
        PrivateStorage.CreateDirectory(DirectoryPath);
        try
        {
            lease=PrivateStorage.CreateFile(Path.Combine(DirectoryPath,LeaseName),FileShare.None);
            try{lease.Write(Marker(DirectoryPath));lease.Flush(true);}
            catch{lease.Dispose();throw;}
        }
        catch
        {
            var path=Path.Combine(DirectoryPath,LeaseName);
            if(File.Exists(path)&&!Link(path))File.Delete(path);
            System.IO.Directory.Delete(DirectoryPath,false);throw;
        }
    }
    private static byte[] Marker(string directory)=>Encoding.ASCII.GetBytes("DBMV-SPILL-OWNERSHIP-V1\n"+Path.GetFileName(directory));
    private static bool Link(string path)=>(File.GetAttributes(path)&FileAttributes.ReparsePoint)!=0;
    private static bool Owned(FileStream stream,string directory)
    {
        var expected=Marker(directory);if(stream.Length!=expected.Length)return false;
        stream.Position=0;Span<byte> actual=stackalloc byte[expected.Length];stream.ReadExactly(actual);return actual.SequenceEqual(expected);
    }
    private static void CleanAbandoned()
    {
        foreach(var directory in System.IO.Directory.EnumerateDirectories(Root))
        {
            try{Clean(directory);}
            catch(DirectoryNotFoundException){}
            catch(FileNotFoundException){}
        }
        static void Clean(string directory)
        {
            if(!Name.IsMatch(Path.GetFileName(directory))||Link(directory))return;
            var path=Path.Combine(directory,LeaseName);if(!File.Exists(path)||Link(path))return;
            FileStream claim;
            // FileShare.None provides an OS-backed exclusive lease across processes.
            // A lease held by another active comparison must not be removed.
            try{claim=new FileStream(path,FileMode.Open,FileAccess.ReadWrite,FileShare.None);}
            catch(IOException){return;}
            using(claim)
            {
                if(!Owned(claim,directory))return;
                DeleteChunks(directory);
            }
            // Windows cannot unlink the open lease. No process resumes old workspaces.
            // Another scavenger may win this final removal; that is already clean.
            try{if(File.Exists(path)){if(Link(path))throw new VerificationException(Reason.TemporaryIntegrity);File.Delete(path);}System.IO.Directory.Delete(directory,false);}
            catch(DirectoryNotFoundException){}
            catch(FileNotFoundException){}
        }
    }
    private static void DeleteChunks(string directory)
    {
        if(Link(Root)||Link(directory))throw new VerificationException(Reason.TemporaryIntegrity);
        var entries=System.IO.Directory.GetFileSystemEntries(directory);
        foreach(var path in entries)
        {
            var name=Path.GetFileName(path);
            if(Link(path)||(File.GetAttributes(path)&FileAttributes.Directory)!=0
                ||name!=LeaseName&&name!="rows.chunk"&&!Chunk.IsMatch(name))throw new VerificationException(Reason.TemporaryIntegrity);
        }
        foreach(var path in entries)if(Path.GetFileName(path)!=LeaseName)File.Delete(path);
    }
    public void Dispose()
    {
        if(disposed)return;disposed=true;
        GateLease acquired;try{acquired=new();}catch{lease.Dispose();throw;}
        using var gate=acquired;
        try
        {
            if(!Owned(lease,DirectoryPath))throw new VerificationException(Reason.TemporaryIntegrity);
            DeleteChunks(DirectoryPath);
        }
        finally{lease.Dispose();}
        var path=Path.Combine(DirectoryPath,LeaseName);
        if(Link(path))throw new VerificationException(Reason.TemporaryIntegrity);
        File.Delete(path);System.IO.Directory.Delete(DirectoryPath,false);
    }
    private sealed class GateLease : IDisposable
    {
        internal GateLease()
        {
            try{if(!Gate.WaitOne(TimeSpan.FromSeconds(10)))throw new VerificationException(Reason.TemporaryStorage);}
            catch(AbandonedMutexException){} // OS released a terminated creator's gate.
        }
        public void Dispose()=>Gate.ReleaseMutex();
    }
}
