using System.Collections;
using System.Globalization;

namespace Verifier.Core;

public sealed partial class ComparisonEngine
{
    private sealed record ScopeOrdinal(int Value);
    // Selection and successful parents retain integers, never native tables.
    // The existing protected metadata store handles large ordinal lists.
    private sealed class ScopeOrdinals(int limit):IReadOnlyList<int>,IDisposable
    {
        private List<int>? memory=[];
        private ProtectedMetadataStore<ScopeOrdinal>? disk;
        private IReadOnlyList<ScopeOrdinal>? records;
        public int Count=>memory?.Count??records!.Count;
        public int this[int index]=>memory!=null?memory[index]:records![index].Value;
        internal void Add(int value)
        {
            if(value<0||value>=limit)throw new VerificationException(Reason.TemporaryIntegrity);
            if(memory?.Count==256)
            {
                var next=new ProtectedMetadataStore<ScopeOrdinal>(r=>r.Value>=0&&r.Value<limit);
                try{foreach(var old in memory)next.Add(new(old));}catch{next.Dispose();throw;}
                disk=next;memory=null;
            }
            if(memory!=null)memory.Add(value);
            else{disk!.Add(new(value));records=disk.Snapshot();}
        }
        public IEnumerator<int> GetEnumerator(){for(var i=0;i<Count;i++)yield return this[i];}
        IEnumerator IEnumerable.GetEnumerator()=>GetEnumerator();
        public void Dispose()=>disk?.Dispose();
    }
    private static IReadOnlyList<MatchedScope> SelectScopes(IReadOnlyList<MatchedScope> plan,IReadOnlyCollection<string>? tables,bool mapped,
        ScopeOrdinals ordinals,CancellationToken token)
    {
        if(tables==null)return plan;
        // Selection names remain exact even when a caller supplies a set with
        // case-insensitive equality. Never build a hash set of the whole plan.
        using var namesOwner=new RunReportStorage();
        var names=TableNameBuffer.SortedDistinct(tables,namesOwner,token);
        if(names.Count!=tables.Count)throw new VerificationException(Reason.InvalidInput);
        for(var i=0;i<plan.Count;i++)
        {token.ThrowIfCancellationRequested();var scope=plan[i];if(TableNameBuffer.Contains(names,mapped?scope.Key:scope.Location))ordinals.Add(i);}
        if(ordinals.Count!=tables.Count)throw new VerificationException(Reason.InvalidInput);
        return new SelectedScopes(plan,ordinals);
    }
    private sealed class SelectedScopes(IReadOnlyList<MatchedScope> plan,IReadOnlyList<int> ordinals):IReadOnlyList<MatchedScope>
    {
        public int Count=>ordinals.Count;
        public MatchedScope this[int index]=>plan[ordinals[index]];
        public IEnumerator<MatchedScope> GetEnumerator(){for(var i=0;i<Count;i++)yield return this[i];}
        IEnumerator IEnumerable.GetEnumerator()=>GetEnumerator();
    }
    private sealed class RetainedScopes(IReadOnlyList<MatchedScope> scopes,IReadOnlyList<TableMapping> mappings):IReadOnlyList<MatchedScope>
    {
        public int Count=>scopes.Count;
        public MatchedScope this[int index]
        {
            get
            {
                var scope=scopes[index];var mapping=mappings[index];
                if(mapping.Source!=scope.Source?.Qualified||mapping.Target!=scope.Target?.Qualified)throw new VerificationException(Reason.TemporaryIntegrity);
                return scope with{Columns=mapping.Columns};
            }
        }
        public IEnumerator<MatchedScope> GetEnumerator(){for(var i=0;i<Count;i++)yield return this[i];}
        IEnumerator IEnumerable.GetEnumerator()=>GetEnumerator();
    }
    private sealed class ScopeTables:IReadOnlyList<Table>
    {
        private readonly IReadOnlyList<MatchedScope> scopes;private readonly bool target;private readonly CancellationToken token;
        public int Count{get;}
        internal ScopeTables(IReadOnlyList<MatchedScope> scopes,bool target,CancellationToken token)
        {
            this.scopes=scopes;this.target=target;this.token=token;
            for(var i=0;i<scopes.Count;i++){token.ThrowIfCancellationRequested();if(Side(scopes[i])!=null)Count++;}
        }
        private Table? Side(MatchedScope scope)=>target?scope.Target:scope.Source;
        // ponytail: adapters enumerate. Rare indexed access scans; retain side
        // ordinals if an adapter starts indexing every table instead.
        public Table this[int index]
        {
            get
            {
                if((uint)index>=(uint)Count)throw new ArgumentOutOfRangeException(nameof(index));
                foreach(var table in this)if(index--==0)return table;
                throw new VerificationException(Reason.ObservationChanged);
            }
        }
        public IEnumerator<Table> GetEnumerator()
        {for(var i=0;i<scopes.Count;i++){token.ThrowIfCancellationRequested();if(Side(scopes[i]) is{} table)yield return table;}}
        IEnumerator IEnumerable.GetEnumerator()=>GetEnumerator();
    }
    private static string CatalogBlockKey(string check,string? location)
        =>check+":"+(location==null?"-1:":location.Length.ToString(CultureInfo.InvariantCulture)+":"+location);
    // Limit order stays adapter-authored. A lazy sorted side-name index avoids
    // reissuing every native table for each limit, without retaining handles.
    internal sealed class ObservationScopeLocations(IReadOnlyList<MatchedScope> scopes,bool target,CancellationToken token):IDisposable
    {
        private sealed record Location(string Name,int Ordinal);
        private ProtectedMetadataStore<Location>? owner;
        private IReadOnlyList<Location>? locations;
        private bool disposed;
        private Table? Side(MatchedScope scope)=>target?scope.Target:scope.Source;
        internal string? Find(string name)
        {
            ObjectDisposedException.ThrowIf(disposed,this);token.ThrowIfCancellationRequested();
            if(locations==null)Build();
            var low=0;var high=locations!.Count-1;
            while(low<=high)
            {
                token.ThrowIfCancellationRequested();var middle=low+(high-low)/2;var entry=locations[middle];
                var order=StringComparer.Ordinal.Compare(entry.Name,name);
                if(order<0)low=middle+1;else if(order>0)high=middle-1;
                else
                {
                    var scope=scopes[entry.Ordinal];
                    if(Side(scope)?.Qualified!=name)throw new VerificationException(Reason.TemporaryIntegrity);
                    return scope.Location;
                }
            }
            return null;
        }
        private void Build()
        {
            IEnumerable<CatalogFact> Names()
            {
                for(var i=0;i<scopes.Count;i++)
                {token.ThrowIfCancellationRequested();if(Side(scopes[i]) is{} table)yield return new(table.Qualified,null,"observation-scope",i.ToString(CultureInfo.InvariantCulture));}
            }
            using var sorted=new CatalogComparison.CatalogFactIndex(Names(),false,token);
            var next=new ProtectedMetadataStore<Location>(l=>!string.IsNullOrEmpty(l.Name)&&l.Ordinal>=0&&l.Ordinal<scopes.Count);
            try
            {
                foreach(var entry in sorted.Entries(token))
                {
                    var fact=entry.Fact;
                    if(entry.Duplicate||fact.Column!=null||fact.Attribute!="observation-scope"||!int.TryParse(fact.Value,NumberStyles.None,CultureInfo.InvariantCulture,out var ordinal)||ordinal<0||ordinal>=scopes.Count)
                        throw new VerificationException(Reason.ObservationChanged);
                    next.Add(new(fact.Key,ordinal));
                }
                next.Seal();locations=next.Snapshot();owner=next;
            }
            catch{next.Dispose();throw;}
        }
        public void Dispose(){if(disposed)return;disposed=true;owner?.Dispose();}
    }
    private static string ScopeOrderKey(int descendants,int ordinal)
        =>(int.MaxValue-descendants).ToString("D10",CultureInfo.InvariantCulture)+ordinal.ToString("D10",CultureInfo.InvariantCulture);
    private static IEnumerable<(int Ordinal,MatchedScope Scope)> OrderedScopes(IReadOnlyList<MatchedScope> scopes,CancellationToken token)
    {
        IEnumerable<CatalogFact> Order()
        {
            for(var i=0;i<scopes.Count;i++)
            {token.ThrowIfCancellationRequested();yield return new(ScopeOrderKey(scopes[i].Source?.DataDescendants.Count??0,i),null,"scope-order",i.ToString(CultureInfo.InvariantCulture));}
        }
        using var ordered=new CatalogComparison.CatalogFactIndex(Order(),false,token);
        foreach(var entry in ordered.Entries(token))
        {
            var fact=entry.Fact;
            if(entry.Duplicate||fact.Attribute!="scope-order"||fact.Column!=null||!int.TryParse(fact.Value,NumberStyles.None,CultureInfo.InvariantCulture,out var ordinal)
                ||ordinal<0||ordinal>=scopes.Count)throw new VerificationException(Reason.TemporaryIntegrity);
            var scope=scopes[ordinal];
            if(fact.Key!=ScopeOrderKey(scope.Source?.DataDescendants.Count??0,ordinal))throw new VerificationException(Reason.TemporaryIntegrity);
            yield return(ordinal,scope);
        }
    }
}
