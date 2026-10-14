namespace Verifier.Core;

// Keep finding validation and the existing store identity while sharing the
// authenticated ordinal storage with other result metadata.
internal sealed class ProtectedFindingStore:IDisposable
{
    private readonly ProtectedMetadataStore<Finding> records=new(f=>Guid.TryParseExact(f.Id,"N",out _)&&f.Count>0&&!string.IsNullOrEmpty(f.Kind));
    internal string IndexPath=>records.IndexPath;
    internal int Count=>records.Count;
    internal void Add(Finding finding)=>records.Add(finding);
    internal IReadOnlyList<Finding> Snapshot()=>records.Snapshot();
    internal IReadOnlyList<Finding> Seal()=>records.Seal();
    public void Dispose()=>records.Dispose();
}
