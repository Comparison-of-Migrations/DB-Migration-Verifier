using System.Text;
namespace Verifier.Core;

// Exact duplicate-ID validation has a small in-memory threshold, then uses the
// existing authenticated external sorter. Digest equality is never an ID match.
internal sealed class ReportFindingIds:IDisposable
{
    private const int Threshold=4096;
    private HashSet<string>? memory=new(StringComparer.Ordinal);
    private ProtectedSort? disk;
    internal bool Add(string id)
    {
        if(memory?.Count==Threshold)
        {
            disk=new(128*1024);foreach(var prior in memory)disk.Add(Encoding.ASCII.GetBytes(prior));memory=null;
        }
        if(memory!=null)return memory.Add(id);
        disk!.Add(Encoding.ASCII.GetBytes(id));return true;
    }
    internal void Verify()
    {
        if(disk==null)return;byte[]? previous=null;
        foreach(var id in disk.Finish(default))
        {if(previous!=null&&previous.AsSpan().SequenceEqual(id))throw new InvalidDataException();previous=id;}
    }
    public void Dispose()=>disk?.Dispose();
}
