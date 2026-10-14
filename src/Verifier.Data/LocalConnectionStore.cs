using System.Security.Cryptography;
using System.Text.Json;
using System.Text.Json.Serialization;
using Verifier.Core;

namespace Verifier.Data;

// Only explicitly selected connection metadata is persisted. No Password field
// exists in these records; credentials live exclusively in the OS vault.
public sealed record SavedEndpoint(string Host,int Port,string Database,string User,string? RootCertificate,Guid? CredentialReference);
public sealed record SavedConnections(DbKind Kind,SavedEndpoint Source,SavedEndpoint Target);
public sealed record RestoredConnections(SavedConnections Settings,string? SourcePassword,string? TargetPassword,bool CredentialsUnavailable)
{public override string ToString()=>"RestoredConnections (credentials redacted)";}

public sealed class LocalConnectionStore(string directory,ICredentialVault vault)
{
    private sealed record Document(SavedConnections? Active,Guid[] PendingDeletion);
    private sealed record Envelope(string Format,Document Payload,string Sha256);
    private static readonly JsonSerializerOptions Json=new(){UnmappedMemberHandling=JsonUnmappedMemberHandling.Disallow,RespectRequiredConstructorParameters=true,RespectNullableAnnotations=true,AllowDuplicateProperties=false,Converters={new JsonStringEnumConverter(allowIntegerValues:false)}};
    private string FilePath=>Path.Combine(directory,"connections.json");
    public static string DefaultDirectory=>Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"DB-Migration-Verifier","connections");
    public bool Exists=>File.Exists(FilePath);

    // The file lock serializes app processes as well as async callers. Each
    // operation re-reads the committed state; no cached secret or stale profile.
    private FileStream Lock()
    {
        PrivateStorage.CreateDirectory(directory);
        var path=Path.Combine(directory,"store.lock");
        if(File.Exists(path))ValidateFile(path);
        var options=new FileStreamOptions{Mode=FileMode.OpenOrCreate,Access=FileAccess.ReadWrite,Share=FileShare.None};
        if(!OperatingSystem.IsWindows())options.UnixCreateMode=UnixFileMode.UserRead|UnixFileMode.UserWrite;
        var stream=new FileStream(path,options);try{ValidateFile(path);return stream;}catch{stream.Dispose();throw;}
    }
    private static void ValidateFile(string path)
    {
        if((File.GetAttributes(path)&FileAttributes.ReparsePoint)!=0)throw new InvalidDataException();
        if(OperatingSystem.IsWindows())PrivateStorage.ValidateWindowsFile(path);
        else if(File.GetUnixFileMode(path)!=(UnixFileMode.UserRead|UnixFileMode.UserWrite))throw new UnauthorizedAccessException();
    }
    private Document Read()
    {
        if(!File.Exists(FilePath))return new(null,[]);
        ValidateFile(FilePath);
        if(new FileInfo(FilePath).Length>262144)throw new InvalidDataException();
        using var stream=File.OpenRead(FilePath);
        var envelope=JsonSerializer.Deserialize<Envelope>(stream,Json)??throw new InvalidDataException();
        if(envelope.Format!="DMV-CONNECTIONS-1"||envelope.Payload==null||envelope.Sha256==null)throw new InvalidDataException();
        var actual=SHA256.HashData(JsonSerializer.SerializeToUtf8Bytes(envelope.Payload,Json));
        byte[] expected;try{expected=Convert.FromHexString(envelope.Sha256);}catch(FormatException){throw new InvalidDataException();}
        if(!CryptographicOperations.FixedTimeEquals(actual,expected))throw new InvalidDataException();
        Validate(envelope.Payload);return envelope.Payload;
    }
    private static void Validate(Document doc)
    {
        if(doc.PendingDeletion==null||doc.PendingDeletion.Length>4||doc.PendingDeletion.Any(g=>g==Guid.Empty)||doc.PendingDeletion.Distinct().Count()!=doc.PendingDeletion.Length)throw new InvalidDataException();
        if(doc.Active is not{}p)return;
        if(!Enum.IsDefined(p.Kind)||p.Source==null||p.Target==null)throw new InvalidDataException();
        foreach(var e in new[]{p.Source,p.Target})
        {
            if(string.IsNullOrWhiteSpace(e.Host)||string.IsNullOrWhiteSpace(e.Database)||string.IsNullOrWhiteSpace(e.User)||e.Port is <1 or >65535||e.Host.Length>4096||e.Database.Length>4096||e.User.Length>4096||e.RootCertificate?.Length>4096||e.CredentialReference==Guid.Empty)throw new InvalidDataException();
            if(e.CredentialReference is{}id&&doc.PendingDeletion.Contains(id))throw new InvalidDataException();
        }
        if(p.Source.CredentialReference!=null&&p.Source.CredentialReference==p.Target.CredentialReference)throw new InvalidDataException();
    }
    private Task Write(Document doc)
    {
        Validate(doc);var checksum=Convert.ToHexString(SHA256.HashData(JsonSerializer.SerializeToUtf8Bytes(doc,Json)));
        return AtomicFile.WriteAsync(FilePath,s=>JsonSerializer.SerializeAsync(s,new Envelope("DMV-CONNECTIONS-1",doc,checksum),Json));
    }
    private async Task<Document> Recover(Document doc)
    {
        if(doc.PendingDeletion.Length==0)return doc;
        // Delete only journaled references. If unavailable, retain the journal
        // and report failure instead of claiming the secret has been forgotten.
        foreach(var id in doc.PendingDeletion)vault.Delete(id);
        var clean=doc with{PendingDeletion=[]};await Write(clean);return clean;
    }
    private static Guid[] References(SavedConnections? p)=>p==null?[]:new[]{p.Source.CredentialReference,p.Target.CredentialReference}.Where(x=>x.HasValue).Select(x=>x!.Value).ToArray();
    public async Task SaveAsync(ConnectionSettings source,ConnectionSettings target,bool includePasswords)
    {
        source.Validate();target.Validate();if(source.Kind!=target.Kind)throw new InvalidDataException();
        using var fileLock=Lock();var old=await Recover(Read());
        SavedEndpoint Endpoint(ConnectionSettings s)=>new(s.Host,s.Port,s.Database,s.User,s.Kind==DbKind.SqlServer?s.ServerCertificate:s.RootCertificate,includePasswords?Guid.NewGuid():null);
        var next=new SavedConnections(source.Kind,Endpoint(source),Endpoint(target));Validate(new(next,[]));
        var staged=old with{PendingDeletion=References(next)};await Write(staged);
        // The pending journal is durable before any vault write, so a failed
        // save/crash can be cleaned on the next explicit operation.
        try
        {
            if(includePasswords){vault.Write(next.Source.CredentialReference!.Value,source.Password);vault.Write(next.Target.CredentialReference!.Value,target.Password);}
            await Write(new(next,References(old.Active)));
        }
        catch
        {try{await Recover(staged);}catch{/* Keep durable cleanup journal. */}throw;}
        await Recover(new(next,References(old.Active)));
    }
    public async Task<RestoredConnections?> RestoreAsync()
    {
        if(!Exists)return null;
        using var fileLock=Lock();var doc=await Recover(Read());if(doc.Active is not{}p)return null;
        string? a=null,b=null;var failed=false;
        foreach(var (e,source) in new[]{(p.Source,true),(p.Target,false)})
        {
            if(e.CredentialReference is not{}id)continue;
            try{var password=vault.Read(id);if(password==null)failed=true;else if(source)a=password;else b=password;}
            catch(CredentialVaultException){failed=true;}
        }
        return new(p,a,b,failed);
    }
    public async Task ForgetAsync()
    {
        if(!Exists)return;
        using var fileLock=Lock();var doc=await Recover(Read());
        var deleting=new Document(null,References(doc.Active));await Write(deleting);
        await Recover(deleting);File.Delete(FilePath);
    }
}
