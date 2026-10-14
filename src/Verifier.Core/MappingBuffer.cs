using System.Collections;

namespace Verifier.Core;

// One run shares a small mapping budget. Large scopes keep exact column names
// in the existing protected catalog storage, including when reopening reports.
internal sealed class MappingBuffer(RunReportStorage owner,CancellationToken token=default)
{
    // Detach labels before columns: a failed column write must still return
    // a report that can be read after its native inventory has been closed.
    internal static IReadOnlyList<TableMapping> Headers(IReadOnlyList<MatchedScope> scopes,RunReportStorage owner,CancellationToken token)
    {
        var buffer=new MappingBuffer(owner,token);
        foreach(var scope in scopes){buffer.Begin(scope.Source?.Qualified,scope.Target?.Qualified);buffer.End();}
        return buffer.Snapshot();
    }
    private List<TableMapping>? values=[];
    private readonly List<ColumnMapping> active=[];
    private ProtectedCatalogStore? disk;
    private sealed record TableRange(int Start,int Count,int Header);
    private ProtectedMetadataStore<TableRange>? tableIndex;
    private long headerBytes;
    private IReadOnlyList<ColumnMapping>? lastColumns;
    private int retained,start,count;
    private bool inTable;
    private string? source,target;
    internal void Begin(string? source,string? target)
    {
        if(inTable)throw new InvalidOperationException();token.ThrowIfCancellationRequested();
        this.source=source;this.target=target;start=disk?.Count??0;count=0;active.Clear();inTable=true;
    }
    internal void AddColumn(ColumnMapping column)
    {
        if(!inTable)throw new InvalidOperationException();token.ThrowIfCancellationRequested();
        if(column==null||column.Source==null&&column.Target==null)throw new VerificationException(Reason.InvalidInput);
        if(disk==null&&retained==256)EnsureDisk();
        if(disk==null){active.Add(column);retained++;}else Write(column);
        count=checked(count+1);
    }
    internal void End()
    {
        if(!inTable)throw new InvalidOperationException();
        if(source==null&&target==null)throw new VerificationException(Reason.InvalidInput);
        var size=64L+2L*((source?.Length??0)+(long)(target?.Length??0));
        if(tableIndex==null&&(values!.Count>=256||size>2*1024*1024-headerBytes))PromoteTables();
        lastColumns=disk==null?active.ToArray():new View(disk.Snapshot(start,count));
        var mapping=new TableMapping(source,target,lastColumns);
        if(tableIndex==null){values!.Add(mapping);headerBytes+=size;}else WriteTable(mapping);
        active.Clear();inTable=false;
    }
    internal void End(string? source,string? target){this.source=source;this.target=target;End();}
    private void EnsureDisk()
    {
        if(disk!=null)return;
        disk=new(token);owner.Add(disk);
        for(var i=0;i<values!.Count;i++)
        {
            var offset=disk.Count;foreach(var old in values[i].Columns)Write(old);
            values[i]=values[i] with{Columns=new View(disk.Snapshot(offset,disk.Count-offset))};
        }
        start=disk.Count;foreach(var old in active)Write(old);active.Clear();
    }
    private void PromoteTables()
    {
        EnsureDisk();
        var next=new ProtectedMetadataStore<TableRange>(r=>r.Start>=0&&r.Count>=0&&r.Count<=int.MaxValue-r.Start&&r.Header>=r.Start+r.Count);
        try{owner.Add(next);}catch{next.Dispose();throw;}
        tableIndex=next;
        foreach(var mapping in values!){token.ThrowIfCancellationRequested();WriteTable(mapping);}
        values=null;headerBytes=0;
    }
    private void WriteTable(TableMapping mapping)
    {
        var range=disk!.Range(((View)mapping.Columns).Records);var header=disk.Count;
        disk.Add(new("mapping-table",mapping.Source,"table",mapping.Target));
        tableIndex!.Add(new(range.Start,range.Count,header));
    }
    internal void Add(MatchedScope scope)
    {
        Begin(scope.Source?.Qualified,scope.Target?.Qualified);
        foreach(var column in scope.Columns)AddColumn(column);
        End();ScopeMatching.RetainColumns(scope.Columns,lastColumns!);
    }
    internal IReadOnlyList<TableMapping> Snapshot()
    {if(inTable)throw new InvalidOperationException();return tableIndex==null?values!.ToArray():new Tables(tableIndex.Snapshot(),disk!);}
    private void Write(ColumnMapping column)
    {token.ThrowIfCancellationRequested();disk!.Add(new("mapping",column.Source,"column",column.Target));}
    private sealed class View(IReadOnlyList<CatalogFact> records):IFrozenMetadata<ColumnMapping>
    {
        internal IReadOnlyList<CatalogFact> Records=>records;
        public int Count=>records.Count;
        public ColumnMapping this[int index]
        {
            get
            {
                var record=records[index];
                if(record.Key!="mapping"||record.Attribute!="column"||record.Column==null&&record.Value==null)throw new VerificationException(Reason.TemporaryIntegrity);
                return new(record.Column,record.Value);
            }
        }
        public IEnumerator<ColumnMapping> GetEnumerator(){for(var i=0;i<Count;i++)yield return this[i];}
        IEnumerator IEnumerable.GetEnumerator()=>GetEnumerator();
    }
    private sealed class Tables(IReadOnlyList<TableRange> ranges,ProtectedCatalogStore catalog):IFrozenMetadata<TableMapping>
    {
        public int Count=>ranges.Count;
        public TableMapping this[int index]
        {
            get
            {
                var range=ranges[index];var header=catalog.Snapshot(range.Header,1)[0];
                if(header.Key!="mapping-table"||header.Attribute!="table"||header.Column==null&&header.Value==null)throw new VerificationException(Reason.TemporaryIntegrity);
                return new(header.Column,header.Value,new View(catalog.Snapshot(range.Start,range.Count)));
            }
        }
        public IEnumerator<TableMapping> GetEnumerator(){for(var i=0;i<Count;i++)yield return this[i];}
        IEnumerator IEnumerable.GetEnumerator()=>GetEnumerator();
    }
}
