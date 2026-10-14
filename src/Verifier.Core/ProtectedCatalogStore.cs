using System.Collections;
namespace Verifier.Core;

// One observer owns all catalog payloads. Section views retain only an ordinal
// range; the existing authenticated disk index binds each exact row descriptor.
internal sealed class ProtectedCatalogStore:IDisposable
{
    private readonly ProtectedMetadataStore<byte[]> records;
    private readonly ProtectedRowStore rows;
    private readonly CancellationToken token;
    private bool sealedStore,disposed;
    internal ProtectedCatalogStore(CancellationToken token=default)
    {
        this.token=token;records=new(d=>d.Length==64);
        try{rows=new(token:token);}catch{records.Dispose();throw;}
    }
    internal int Count=>records.Count;
    internal void Add(CatalogFact fact)
    {
        ObjectDisposedException.ThrowIf(disposed,this);token.ThrowIfCancellationRequested();
        if(sealedStore)throw new InvalidOperationException();
        if(fact==null||string.IsNullOrEmpty(fact.Key)||string.IsNullOrEmpty(fact.Attribute))throw new VerificationException(Reason.InvalidInput);
        // Open before append: the source may be an earlier row in this store,
        // whose completed-length guard must not observe our unfinished tail.
        using var valueInput=fact.Text?.Open(token);
        var descriptor=rows.AddAsync((stream,ct)=>{CatalogFactWire.Write(fact,stream,ct,valueInput);return Task.CompletedTask;},token,(int)Math.Min(65536,Math.Max(256,CatalogFactWire.Length(fact)))).GetAwaiter().GetResult();
        records.Add(descriptor);
    }
    internal IReadOnlyList<CatalogFact> Snapshot(int start,int count)
    {
        ObjectDisposedException.ThrowIf(disposed,this);
        if(start<0||count<0||start>Count||count>Count-start)throw new ArgumentOutOfRangeException();
        return new View(this,records.Snapshot(),start,count);
    }
    internal (int Start,int Count) Range(IReadOnlyList<CatalogFact> view)
    {
        ObjectDisposedException.ThrowIf(disposed,this);
        if(view is not View slice||!ReferenceEquals(slice.Owner,this))throw new VerificationException(Reason.TemporaryIntegrity);
        return (slice.Start,slice.Count);
    }
    internal void Seal(){ObjectDisposedException.ThrowIf(disposed,this);rows.Flush();records.Seal();sealedStore=true;}
    public void Dispose()
    {if(disposed)return;disposed=true;try{records.Dispose();}finally{rows.Dispose();}}
    private sealed class View(ProtectedCatalogStore owner,IReadOnlyList<byte[]> records,int start,int count):IFrozenMetadata<CatalogFact>
    {
        internal ProtectedCatalogStore Owner=>owner;
        internal int Start=>start;
        public int Count=>count;
        public CatalogFact this[int index]
        {
            get
            {
                ObjectDisposedException.ThrowIf(owner.disposed,owner);
                if(index<0||index>=count)throw new ArgumentOutOfRangeException(nameof(index));
                Stream Open()
                {ObjectDisposedException.ThrowIf(owner.disposed,owner);return owner.rows.OpenRead(records[start+index]);}
                using var input=Open();return CatalogFactWire.Read(input,CancellationToken.None,Open);
            }
        }
        public IEnumerator<CatalogFact> GetEnumerator(){for(var i=0;i<count;i++)yield return this[i];}
        IEnumerator IEnumerable.GetEnumerator()=>GetEnumerator();
    }
}
