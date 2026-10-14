using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Verifier.Core;

// A local index of explicitly saved/opened JSON results. No DB endpoints,
// principals, raw values, DDL or credentials are copied into this index.
public sealed record ResultHistoryEntry(string Id,Guid RunId,DateTimeOffset Recorded,string Path,string Sha256)
{public override string ToString()=>"ResultHistoryEntry (local path redacted)";}
public sealed record HistoryCursor(long Ticks,string Id);
public sealed record HistoryPage(IReadOnlyList<ResultHistoryEntry> Entries,HistoryCursor? Next,bool UnreadableEntries);

public sealed class ResultHistory(string directory)
{
    private sealed record Envelope(string Format,ResultHistoryEntry Entry,string Sha256);
    private static readonly JsonSerializerOptions Json=new(){UnmappedMemberHandling=JsonUnmappedMemberHandling.Disallow,RespectNullableAnnotations=true,RespectRequiredConstructorParameters=true,AllowDuplicateProperties=false};
    public static string DefaultDirectory=>System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"DB-Migration-Verifier","history");
    private static bool IsId(string id)=>id.Length==64&&id.All(c=>c is >= '0' and <= '9' or >= 'A' and <= 'F');
    private static string IdFor(string path)=>Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(OperatingSystem.IsWindows()?path.ToUpperInvariant():path)));
    private string RecordPath(string id){if(!IsId(id))throw new InvalidDataException();return System.IO.Path.Combine(directory,id+".json");}
    private void ValidateDirectory()
    {PrivateStorage.CreateDirectory(directory);}
    private static void ValidateFile(string path)
    {
        if((File.GetAttributes(path)&FileAttributes.ReparsePoint)!=0)throw new InvalidDataException();
        if(OperatingSystem.IsWindows())PrivateStorage.ValidateWindowsFile(path);
        else if(File.GetUnixFileMode(path)!=(UnixFileMode.UserRead|UnixFileMode.UserWrite))throw new UnauthorizedAccessException();
    }
    public async Task RecordAsync(string path,RunReport report)
    {
        using var lease=report.AcquireStorage();
        ReportFile.Validate(report);if(report.Example)throw new InvalidDataException();
        path=System.IO.Path.GetFullPath(path);if(path.Length>16384)throw new InvalidDataException();
        await using var input=File.OpenRead(path);var checksum=Convert.ToHexString(await SHA256.HashDataAsync(input));
        var entry=new ResultHistoryEntry(IdFor(path),report.RunId,DateTimeOffset.UtcNow,path,checksum);
        ValidateDirectory();var destination=RecordPath(entry.Id);if(File.Exists(destination))ValidateFile(destination);
        var hash=Convert.ToHexString(SHA256.HashData(JsonSerializer.SerializeToUtf8Bytes(entry,Json)));
        await AtomicFile.WriteAsync(destination,s=>JsonSerializer.SerializeAsync(s,new Envelope("DMV-HISTORY-1",entry,hash),Json));
    }
    private async Task<ResultHistoryEntry> Read(string id)
    {
        var path=RecordPath(id);ValidateFile(path);if(new FileInfo(path).Length>131072)throw new InvalidDataException();
        await using var input=File.OpenRead(path);var doc=await JsonSerializer.DeserializeAsync<Envelope>(input,Json)??throw new InvalidDataException();var e=doc.Entry;
        if(doc.Format!="DMV-HISTORY-1"||e==null||e.Id!=id||e.RunId==Guid.Empty||e.Recorded==default||e.Path==null||e.Path.Length>16384||!System.IO.Path.IsPathFullyQualified(e.Path)||e.Path.Contains('\0')||e.Id!=IdFor(e.Path)||e.Sha256==null||!IsId(e.Sha256)||doc.Sha256==null||!IsId(doc.Sha256))throw new InvalidDataException();
        var actual=SHA256.HashData(JsonSerializer.SerializeToUtf8Bytes(e,Json));if(!CryptographicOperations.FixedTimeEquals(actual,Convert.FromHexString(doc.Sha256)))throw new InvalidDataException();return e;
    }
    public async Task<RunReport> OpenAsync(string id)
    {ValidateDirectory();var e=await Read(id);return await ReportFile.LoadRecordedAsync(e.Path,e.Sha256,e.RunId);}
    public Task RemoveAsync(string id)
    {
        // Removing an index entry never deletes the user's report file. A
        // damaged record can also be removed by its validated, fixed filename.
        if(!Directory.Exists(directory))return Task.CompletedTask;ValidateDirectory();var path=RecordPath(id);if(File.Exists(path)){ValidateFile(path);File.Delete(path);}return Task.CompletedTask;
    }
    public async Task<HistoryPage> ReadPageAsync(HistoryCursor? before=null,int pageSize=20)
    {
        if(pageSize is <1 or >100||before!=null&&(!IsId(before.Id)||before.Ticks<0))throw new ArgumentException();
        if(!Directory.Exists(directory))return new([],null,false);ValidateDirectory();
        // Keep only pageSize+1 candidates, regardless of the number of records.
        var compare=Comparer<(long Ticks,string Id)>.Create((a,b)=>a.Ticks!=b.Ticks?a.Ticks.CompareTo(b.Ticks):StringComparer.Ordinal.Compare(a.Id,b.Id));
        var queue=new PriorityQueue<(string Id,long Ticks),(long Ticks,string Id)>(compare);var unreadable=false;
        foreach(var path in Directory.EnumerateFiles(directory,"*.json"))
        {
            var id=System.IO.Path.GetFileNameWithoutExtension(path);if(!IsId(id)){unreadable=true;continue;}
            try
            {
                var ticks=File.GetLastWriteTimeUtc(path).Ticks;var key=(ticks,id);
                if(before!=null&&compare.Compare(key,(before.Ticks,before.Id))>=0)continue;
                queue.Enqueue((id,ticks),key);if(queue.Count>pageSize+1)queue.Dequeue();
            }
            catch(Exception e) when(e is IOException or UnauthorizedAccessException){unreadable=true;}
        }
        var ordered=queue.UnorderedItems.Select(x=>x.Element).OrderByDescending(x=>(x.Ticks,x.Id),compare).ToArray();var result=new List<ResultHistoryEntry>();
        foreach(var candidate in ordered.Take(pageSize))
        {try{result.Add(await Read(candidate.Id));}catch(Exception e)when(e is IOException or InvalidDataException or UnauthorizedAccessException or JsonException or ArgumentException or VerificationException){unreadable=true;}}
        var last=ordered.Take(pageSize).LastOrDefault();return new(result,ordered.Length>pageSize?new(last.Ticks,last.Id):null,unreadable);
    }
}
