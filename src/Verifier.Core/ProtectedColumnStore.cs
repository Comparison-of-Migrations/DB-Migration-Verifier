using System.Collections;
namespace Verifier.Core;

// Column text shares the exact UTF-16 protected payload format; these private
// records never enter catalog comparison. Scalar shape uses the authenticated
// ordinal metadata index, and both slices belong to the same observer lifetime.
internal sealed class ProtectedColumnStore:IDisposable
{
    private sealed record Shape(int Position,bool Nullable,bool SafeValue,uint ElementOid,uint TypeOid);
    private readonly ProtectedCatalogStore text;
    private readonly ProtectedMetadataStore<Shape> shapes;
    private readonly object mappingGate=new();
    private ProtectedMetadataStore<ScopeMatching.ColumnOrdinals>? mappings;
    private readonly CancellationToken token;
    private bool disposed,sealedStore;
    internal ProtectedColumnStore(CancellationToken token=default)
    {
        this.token=token;text=new(token);
        try{shapes=new(s=>s.Position>0);}catch{text.Dispose();throw;}
    }
    internal int Count=>shapes.Count;
    internal void Add(Column column)
    {
        ObjectDisposedException.ThrowIf(disposed,this);token.ThrowIfCancellationRequested();
        if(sealedStore)throw new InvalidOperationException();
        if(column==null||column.Position<=0||string.IsNullOrEmpty(column.Name)||string.IsNullOrEmpty(column.Type))
            throw new VerificationException(Reason.InvalidInput);
        if(text.Count!=Count)throw new VerificationException(Reason.TemporaryIntegrity);
        text.Add(new(column.Name,null,column.Type,column.Definition));
        shapes.Add(new(column.Position,column.Nullable,column.SafeValue,column.ElementOid,column.TypeOid));
    }
    internal IReadOnlyList<Column> Snapshot(int start,int count)
    {
        ObjectDisposedException.ThrowIf(disposed,this);
        if(text.Count!=Count)throw new VerificationException(Reason.TemporaryIntegrity);
        var slice=text.Snapshot(start,count);return new View(this,slice,shapes.Snapshot(),start);
    }
    internal (int Start,int Count) Range(IReadOnlyList<Column> view)
    {
        ObjectDisposedException.ThrowIf(disposed,this);
        if(view is not View slice||!ReferenceEquals(slice.Owner,this))throw new VerificationException(Reason.TemporaryIntegrity);
        return (slice.Start,slice.Count);
    }
    internal static ProtectedColumnStore? OwnerOf(IReadOnlyList<Column> view)=>(view as View)?.Owner;
    // One shared ordinal store prevents a separate set of open files for
    // every mixed table visited in the UI. Unpublished partial ranges cannot
    // authorize a mapping; previous authenticated ranges remain unchanged.
    internal (IReadOnlyList<ScopeMatching.ColumnOrdinals> Records,int Start,int Count,Guid Batch) StoreMappings(IEnumerable<ScopeMatching.ColumnOrdinals> values)
    {
        lock(mappingGate)
        {
            ObjectDisposedException.ThrowIf(disposed,this);
            mappings??=new(p=>p.Batch!=Guid.Empty&&(p.Source!=null||p.Target!=null)&&(p.Source==null||p.Source>=0)&&(p.Target==null||p.Target>=0));
            var start=mappings.Count;var batch=Guid.NewGuid();token.ThrowIfCancellationRequested();
            // Ordinals can be reused after rollback. Bind each new published
            // range to its own identity so old orphan ciphertext cannot replay.
            var complete=mappings.Append(values.Select(p=>p with{Batch=batch}));
            return(complete,start,complete.Count-start,batch);
        }
    }
    internal void Seal()
    {
        ObjectDisposedException.ThrowIf(disposed,this);
        if(text.Count!=Count)throw new VerificationException(Reason.TemporaryIntegrity);
        text.Seal();shapes.Seal();sealedStore=true;
    }
    public void Dispose()
    {lock(mappingGate){if(disposed)return;disposed=true;try{mappings?.Dispose();}finally{try{text.Dispose();}finally{shapes.Dispose();}}}}
    private sealed class View(ProtectedColumnStore owner,IReadOnlyList<CatalogFact> text,IReadOnlyList<Shape> shapes,int start):IFrozenMetadata<Column>
    {
        internal ProtectedColumnStore Owner=>owner;
        internal int Start=>start;
        public int Count=>text.Count;
        public Column this[int index]
        {
            get
            {
                var record=text[index];var shape=shapes[start+index];
                if(record.Column!=null)throw new VerificationException(Reason.TemporaryIntegrity);
                return new(record.Key,record.Attribute,shape.Position,shape.Nullable,record.Value,shape.SafeValue)
                    {ElementOid=shape.ElementOid,TypeOid=shape.TypeOid};
            }
        }
        public IEnumerator<Column> GetEnumerator(){for(var i=0;i<Count;i++)yield return this[i];}
        IEnumerator IEnumerable.GetEnumerator()=>GetEnumerator();
    }
}
