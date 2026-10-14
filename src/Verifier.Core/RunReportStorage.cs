namespace Verifier.Core;

// A report owns its spill until explicitly released. An in-flight export keeps
// a lease so navigation cannot close its files halfway through serialization.
internal sealed class RunReportStorage:IDisposable
{
    private readonly object gate=new();private readonly List<IDisposable> stores=[];private int leases=1;private bool released;
    internal void Add(IDisposable store){lock(gate){if(released)throw new ObjectDisposedException(nameof(RunReportStorage));stores.Add(store);}}
    internal IDisposable Acquire()
    {lock(gate){if(released)throw new ObjectDisposedException(nameof(RunReportStorage));leases++;return new Lease(this);}}
    public void Dispose(){lock(gate){if(released)return;released=true;Release();}}
    private void Release()
    {
        lock(gate)
        {
            if(--leases!=0)return;Exception? failure=null;
            foreach(var store in stores)try{store.Dispose();}catch(Exception e)when(e is IOException or UnauthorizedAccessException or VerificationException){failure??=e;}
            stores.Clear();if(failure!=null)throw new VerificationException(Reason.TemporaryStorage);
        }
    }
    private sealed class Lease(RunReportStorage owner):IDisposable
    {private RunReportStorage? current=owner;public void Dispose()=>Interlocked.Exchange(ref current,null)?.Release();}
}
internal interface IFrozenMetadata<T>:IReadOnlyList<T>;
internal interface IFrozenFindings:IFrozenMetadata<Finding>;

internal sealed class FindingBuffer(RunReportStorage owner):IReadOnlyList<Finding>
{
    private const int Threshold=256;
    private List<Finding>? memory=[];private ProtectedFindingStore? disk;
    public int Count=>memory?.Count??disk!.Count;
    public Finding this[int index]=>memory!=null?memory[index]:disk!.Snapshot()[index];
    internal void Add(Finding finding)
    {
        if(memory?.Count==Threshold)
        {
            var next=new ProtectedFindingStore();owner.Add(next);
            foreach(var item in memory)next.Add(item);
            disk=next;memory=null;
        }
        if(memory!=null)memory.Add(finding);else disk!.Add(finding);
    }
    internal void AddRange(IEnumerable<Finding> findings){foreach(var item in findings)Add(item);}
    internal IReadOnlyList<Finding> Snapshot()=>memory!=null?memory.ToArray():disk!.Snapshot();
    internal void RemoveAll(Predicate<Finding> predicate)
    {
        if(memory!=null){memory.RemoveAll(predicate);return;}
        var old=disk!.Snapshot();if(!old.Any(f=>predicate(f)))return;
        var replacement=new FindingBuffer(owner);foreach(var item in old)if(!predicate(item))replacement.Add(item);
        memory=replacement.memory;disk=replacement.disk;
    }
    public IEnumerator<Finding> GetEnumerator()=>Snapshot().GetEnumerator();
    System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()=>GetEnumerator();
    internal static IReadOnlyList<Finding> Freeze(IReadOnlyList<Finding> values)=>values is FindingBuffer buffer?buffer.Snapshot():values is IFrozenMetadata<Finding>?values:values.ToArray();
    internal static IReadOnlyList<Finding> Join(IReadOnlyList<Finding> left,IReadOnlyList<Finding> right)=>new Joined(Freeze(left),Freeze(right));
    private sealed class Joined(IReadOnlyList<Finding> left,IReadOnlyList<Finding> right):IFrozenFindings
    {
        public int Count=>checked(left.Count+right.Count);
        public Finding this[int index]=>index<0||index>=Count?throw new ArgumentOutOfRangeException(nameof(index)):index<left.Count?left[index]:right[index-left.Count];
        public IEnumerator<Finding> GetEnumerator(){foreach(var item in left)yield return item;foreach(var item in right)yield return item;}
        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()=>GetEnumerator();
    }
}
