namespace Verifier.Core;

// One table's catalog checks share a lazy target -> confirmed source lookup.
// Small mappings use the old lookup; large mappings keep a sorted protected root.
internal sealed class CatalogColumnMap(IReadOnlyList<ColumnMapping> columns,CancellationToken token):IDisposable
{
    private ILookup<string,ColumnMapping>? memory;
    private ProtectedCatalogStore? owner;
    private IReadOnlyList<CatalogFact>? records;
    private bool disposed;
    internal string? Source(string target)
    {
        ObjectDisposedException.ThrowIf(disposed,this);token.ThrowIfCancellationRequested();
        if(memory==null&&records==null)Build();
        if(memory!=null)return memory[target].SingleOrDefault()?.Source;
        var low=0;var high=records!.Count-1;
        while(low<=high)
        {
            token.ThrowIfCancellationRequested();var middle=low+(high-low)/2;var fact=records[middle];
            var order=StringComparer.Ordinal.Compare(fact.Key,target);
            if(order<0)low=middle+1;else if(order>0)high=middle-1;else return fact.Value;
        }
        return null;
    }
    private void Build()
    {
        if(columns.Count<=256)
        {
            var lookup=columns.Where(c=>{token.ThrowIfCancellationRequested();return c.Target!=null;}).ToLookup(c=>c.Target!,StringComparer.Ordinal);
            if(lookup.Any(group=>group.Skip(1).Any()))throw new VerificationException(Reason.AmbiguousScope);
            memory=lookup;
            return;
        }
        using var sorted=new CatalogComparison.CatalogFactIndex(columns.Where(c=>
        {token.ThrowIfCancellationRequested();return c.Target!=null;}).Select(c=>new CatalogFact(c.Target!,null,"mapped-column",c.Source)),false,token);
        var next=new ProtectedCatalogStore(token);
        try
        {
            foreach(var entry in sorted.Entries(token))
            {
                if(entry.Duplicate)throw new VerificationException(Reason.AmbiguousScope);
                next.Add(entry.Fact);
            }
            next.Seal();records=next.Snapshot(0,next.Count);owner=next;
        }
        catch{next.Dispose();throw;}
    }
    public void Dispose(){if(disposed)return;disposed=true;owner?.Dispose();}
}
