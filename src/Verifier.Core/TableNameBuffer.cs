using System.Collections;

namespace Verifier.Core;

// Result scope names share the report's protected catalog and lifetime. Small
// snapshots stay cheap; large reports never duplicate the full root array.
internal sealed class TableNameBuffer(RunReportStorage owner,CancellationToken token=default)
{
    private List<string>? memory=[];
    private ProtectedCatalogStore? disk;
    private long bytes;
    internal void Add(string name)
    {
        token.ThrowIfCancellationRequested();
        if(string.IsNullOrWhiteSpace(name))throw new VerificationException(Reason.InvalidInput);
        var size=32L+2L*name.Length;
        if(disk==null&&(memory!.Count>=256||size>2*1024*1024-bytes))
        {
            var next=new ProtectedCatalogStore(token);
            try{owner.Add(next);}catch{next.Dispose();throw;}
            disk=next;foreach(var old in memory)Write(old);memory=null;bytes=0;
        }
        if(disk==null){memory!.Add(name);bytes+=size;}else Write(name);
    }
    private void Write(string name)=>disk!.Add(new("table",name,"scope",null));
    internal IReadOnlyList<string> Snapshot()=>disk==null?memory!.ToArray():new View(disk.Snapshot(0,disk.Count));
    internal static IReadOnlyList<string> SortedDistinct(IEnumerable<string> names,RunReportStorage owner,CancellationToken token=default)
    {
        using var index=new CatalogComparison.CatalogFactIndex(names.Select(n=>
            string.IsNullOrWhiteSpace(n)?throw new VerificationException(Reason.InvalidInput):new CatalogFact(n,null,"scope",null)),false,token);
        var buffer=new TableNameBuffer(owner,token);
        foreach(var item in index.Entries(token))buffer.Add(item.Fact.Key);
        return buffer.Snapshot();
    }
    internal static IReadOnlyList<string> Unique(IEnumerable<string> names,RunReportStorage owner)
    {
        var count=0;
        IEnumerable<string> Counted(){foreach(var name in names){count=checked(count+1);yield return name;}}
        var sorted=SortedDistinct(Counted(),owner);
        if(sorted.Count!=count)throw new InvalidDataException();
        return sorted;
    }
    internal static bool Contains(IReadOnlyList<string> sorted,string? name)
    {
        if(name==null)return false;
        var start=0;var end=sorted.Count-1;
        while(start<=end)
        {
            var middle=start+(end-start)/2;var comparison=StringComparer.Ordinal.Compare(sorted[middle],name);
            if(comparison==0)return true;
            if(comparison<0)start=middle+1;else end=middle-1;
        }
        return false;
    }
    private sealed class View(IReadOnlyList<CatalogFact> records):IFrozenMetadata<string>
    {
        public int Count=>records.Count;
        public string this[int index]
        {
            get
            {
                var fact=records[index];
                if(fact.Key!="table"||fact.Attribute!="scope"||fact.Value!=null||string.IsNullOrWhiteSpace(fact.Column))
                    throw new VerificationException(Reason.TemporaryIntegrity);
                return fact.Column;
            }
        }
        public IEnumerator<string> GetEnumerator(){for(var i=0;i<Count;i++)yield return this[i];}
        IEnumerator IEnumerable.GetEnumerator()=>GetEnumerator();
    }
}
