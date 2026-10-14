namespace Verifier.Core;

// The same report lease owns both findings and observation limits. Progress
// snapshots capture their own count; later appends never alter an earlier view.
internal sealed class IssueBuffer(RunReportStorage owner):IReadOnlyList<ScopeIssue>
{
    private const int Threshold=256;
    private List<ScopeIssue>? memory=[];
    private ProtectedMetadataStore<ScopeIssue>? disk;
    public int Count=>memory?.Count??disk!.Count;
    public ScopeIssue this[int index]=>memory!=null?memory[index]:disk!.Snapshot()[index];
    internal void Add(ScopeIssue issue)
    {
        if(issue==null||issue.Reason==Reason.None||!Enum.IsDefined(issue.Reason))throw new VerificationException(Reason.InvalidInput);
        if(memory?.Count==Threshold)
        {
            var next=new ProtectedMetadataStore<ScopeIssue>(i=>i.Reason!=Reason.None&&Enum.IsDefined(i.Reason));owner.Add(next);
            foreach(var item in memory)next.Add(item);disk=next;memory=null;
        }
        if(memory!=null)memory.Add(issue);else disk!.Add(issue);
    }
    internal void AddRange(IEnumerable<ScopeIssue> issues){foreach(var issue in issues)Add(issue);}
    internal IReadOnlyList<ScopeIssue> Snapshot()=>memory!=null?memory.ToArray():disk!.Snapshot();
    internal static IReadOnlyList<ScopeIssue> Freeze(IReadOnlyList<ScopeIssue> values)=>values is IssueBuffer buffer?buffer.Snapshot():values is IFrozenMetadata<ScopeIssue>?values:values.ToArray();
    public IEnumerator<ScopeIssue> GetEnumerator()=>Snapshot().GetEnumerator();
    System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()=>GetEnumerator();
}
