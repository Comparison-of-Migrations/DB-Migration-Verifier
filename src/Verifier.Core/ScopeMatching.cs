namespace Verifier.Core;

public sealed record MatchedScope(Table? Source,Table? Target,IReadOnlyList<ColumnMapping> Columns)
{
    public string Key=>(Source!=null?"source:":"target:")+(Source?.Qualified??Target!.Qualified);
    public string Location=>Source?.Qualified??Target!.Qualified;
    // Reports outlive the observer that owns protected column metadata.
    public TableMapping Snapshot=>new(Source?.Qualified,Target?.Qualified,Columns.ToArray());
    public bool Complete=>Source!=null&&Target!=null&&Columns.All(c=>c.Source!=null&&c.Target!=null);
}

public static class ScopeMatching
{
    // Only inventories already validated by Resolve may use this borrowed view.
    internal static IReadOnlyList<ColumnMapping> ResolveObservedColumns(Table source,Table? target,IReadOnlyList<ColumnMapping> overrides,CancellationToken token=default)
        =>overrides.Count==0&&source.Columns is IFrozenMetadata<Column>&&(target==null||target.Columns is IFrozenMetadata<Column>)
            ?new DeferredColumns(()=>ResolveDefaultColumns(source,target,token)):ResolveColumns(source,target,overrides,token);
    internal static string[]? QualifiedParts(string name)
    {
        var parts=new List<string>();var i=0;
        while(i<name.Length)
        {
            var text=new System.Text.StringBuilder();
            if(name[i]=='"')
            {
                i++;var closed=false;
                while(i<name.Length){var c=name[i++];if(c!='"'){text.Append(c);continue;}if(i<name.Length&&name[i]=='"'){i++;text.Append('"');continue;}closed=true;break;}
                if(!closed||i<name.Length&&name[i]!='.')return null;
            }
            else{while(i<name.Length&&name[i]!='.'){if(name[i]=='"')return null;text.Append(name[i++]);}}
            if(text.Length==0)return null;parts.Add(text.ToString());
            if(i<name.Length&&++i==name.Length)return null;
        }
        return parts.Count==2?parts.ToArray():null;
    }
    public static IReadOnlyList<MatchedScope> Resolve(Inventory source,Inventory target,IReadOnlyList<TableMapping> overrides,CancellationToken token=default)
    {
        if(source.Tables is IFrozenMetadata<Table>&&target.Tables is IFrozenMetadata<Table>)
            return ResolveFrozen(source,target,overrides,token);
        ValidateInventory(source,token);ValidateInventory(target,token);
        var left=source.Tables.ToDictionary(x=>{token.ThrowIfCancellationRequested();return x.Qualified;},StringComparer.Ordinal);
        var right=target.Tables.ToDictionary(x=>{token.ThrowIfCancellationRequested();return x.Qualified;},StringComparer.Ordinal);
        // A namespace inventory has one schema (validated above), so local
        // names are unique. Do not scan every target for every source table.
        var localTargets=source.DatabaseIsNamespace&&target.DatabaseIsNamespace
            ?target.Tables.ToDictionary(x=>{token.ThrowIfCancellationRequested();return x.Name;},StringComparer.Ordinal):null;
        if(overrides.Any(x=>x==null||x.Columns==null||x.Source==null||!left.ContainsKey(x.Source)||x.Target!=null&&!right.ContainsKey(x.Target)) || overrides.Select(x=>x.Source).Distinct(StringComparer.Ordinal).Count()!=overrides.Count || overrides.Where(x=>x.Target!=null).Select(x=>x.Target).Distinct(StringComparer.Ordinal).Count()!=overrides.Count(x=>x.Target!=null)) throw new VerificationException(Reason.InvalidInput);
        var manual=overrides.ToDictionary(x=>x.Source!,StringComparer.Ordinal);
        var reserved=overrides.Where(x=>x.Target!=null).Select(x=>x.Target!).ToHashSet(StringComparer.Ordinal);
        var used=new HashSet<string>(StringComparer.Ordinal);var scopes=new List<MatchedScope>();
        foreach(var table in source.Tables.OrderBy(x=>{token.ThrowIfCancellationRequested();return x.Qualified;},StringComparer.Ordinal))
        {
            token.ThrowIfCancellationRequested();
            Table? destination=null;IReadOnlyList<ColumnMapping> columns=[];
            if(manual.TryGetValue(table.Qualified,out var mapping))
            { destination=mapping.Target==null?null:right[mapping.Target];columns=mapping.Columns; }
            else if(localTargets!=null)
            {
                if(localTargets.TryGetValue(table.Name,out var candidate)&&!reserved.Contains(candidate.Qualified))destination=candidate;
            }
            else if(!reserved.Contains(table.Qualified)) right.TryGetValue(table.Qualified,out destination);
            if(destination!=null && !used.Add(destination.Qualified)) throw new VerificationException(Reason.InvalidInput);
            // Protected snapshots are immutable for their observer lifetime.
            // Table lists need no per-column mappings until the user opens that
            // table or executes it. Mutable inputs/manual overrides stay eager.
            var resolved=columns.Count==0&&table.Columns is IFrozenMetadata<Column>&&(destination==null||destination.Columns is IFrozenMetadata<Column>)
                ?new DeferredColumns(()=>ResolveDefaultColumns(table,destination,token)):ResolveColumns(table,destination,columns,token);
            scopes.Add(new(table,destination,resolved));
        }
        foreach(var table in target.Tables.Where(t=>!used.Contains(t.Qualified)).OrderBy(t=>{token.ThrowIfCancellationRequested();return t.Qualified;},StringComparer.Ordinal))
        {
            token.ThrowIfCancellationRequested();
            IReadOnlyList<ColumnMapping> TargetColumns()=>table.Columns.Select(c=>{token.ThrowIfCancellationRequested();return new ColumnMapping(null,c.Name);}).ToArray();
            scopes.Add(new(null,table,table.Columns is IFrozenMetadata<Column>?new DeferredColumns(()=>{token.ThrowIfCancellationRequested();return new OrdinalColumns(null,table.Columns);}):TargetColumns()));
        }
        return scopes;
    }
    // Inventory validation has already established unique names/positions.
    // Identically ordered immutable columns need neither name sets nor a full
    // mapping array. Keep their authenticated ordinal owners until detachment.
    private static IReadOnlyList<ColumnMapping> ResolveDefaultColumns(Table source,Table? target,CancellationToken token)
    {
        token.ThrowIfCancellationRequested();
        if(target==null)return new OrdinalColumns(source.Columns,null);
        if(source.Columns.Count==target.Columns.Count)
        {
            var ordered=true;
            for(var i=0;i<source.Columns.Count;i++)
            {
                token.ThrowIfCancellationRequested();var name=source.Columns[i].Name;token.ThrowIfCancellationRequested();
                if(name!=target.Columns[i].Name){ordered=false;break;}
            }
            token.ThrowIfCancellationRequested();
            if(ordered)return new OrdinalColumns(source.Columns,target.Columns);
        }
        return ProtectedColumnStore.OwnerOf(source.Columns) is{} owner
            ?ResolveMixedColumns(source.Columns,target.Columns,owner,token):ResolveColumns(source,target,[],token);
    }
    internal sealed record ColumnOrdinals(int? Source,int? Target)
    {public Guid Batch{get;init;}public bool Tables{get;init;}};
    private static IReadOnlyList<ColumnMapping> ResolveMixedColumns(IReadOnlyList<Column> source,IReadOnlyList<Column> target,ProtectedColumnStore owner,CancellationToken token,IReadOnlyList<ColumnMapping>? overrides=null)
    {
        var manual=new Dictionary<string,ColumnMapping>(StringComparer.Ordinal);var reserved=new HashSet<string>(StringComparer.Ordinal);
        foreach(var mapping in overrides??[])
        {
            token.ThrowIfCancellationRequested();
            if(mapping==null||mapping.Source==null||!manual.TryAdd(mapping.Source,mapping)||mapping.Target!=null&&!reserved.Add(mapping.Target))throw new VerificationException(Reason.InvalidInput);
        }
        var found=new HashSet<string>(StringComparer.Ordinal);var destinations=new Dictionary<string,int>(StringComparer.Ordinal);
        using var left=new CatalogComparison.CatalogFactIndex(Names(source,true),false,token);
        using var right=new CatalogComparison.CatalogFactIndex(Names(target,false),false,token);
        if(found.Count!=manual.Count||destinations.Count!=reserved.Count)throw new VerificationException(Reason.InvalidInput);
        using var ordered=new CatalogComparison.CatalogFactIndex(Pairs(),false,token);
        var stored=owner.StoreMappings(Ordinals());
        return new StoredColumns(stored.Records,stored.Start,stored.Count,stored.Batch,source,target);
        IEnumerable<ColumnOrdinals> Ordinals()
        {
            foreach(var entry in ordered.Entries(token))
            {
                token.ThrowIfCancellationRequested();var fact=entry.Fact;
                var from=fact.Column==null?(int?)null:Ordinal(fact.Column,source.Count);
                var to=fact.Value==null?(int?)null:Ordinal(fact.Value,target.Count);
                if(entry.Duplicate||from==null&&to==null||fact.Attribute!="column-ordinal"||fact.Key!=Key(from,to))throw new VerificationException(Reason.TemporaryIntegrity);
                yield return new(from,to);
            }
        }
        IEnumerable<CatalogFact> Names(IReadOnlyList<Column> columns,bool sourceSide)
        {
            for(var i=0;i<columns.Count;i++)
            {
                token.ThrowIfCancellationRequested();var name=columns[i].Name;
                if(sourceSide){if(manual.ContainsKey(name))found.Add(name);}
                else if(reserved.Contains(name)&&!destinations.TryAdd(name,i))throw new VerificationException(Reason.AmbiguousScope);
                yield return new(name,null,"column-name",i.ToString(System.Globalization.CultureInfo.InvariantCulture));
            }
        }
        IEnumerable<CatalogFact> Pairs()
        {
            using var a=left.Entries(token).GetEnumerator();using var b=right.Entries(token).GetEnumerator();
            var hasA=a.MoveNext();var hasB=b.MoveNext();
            while(hasA||hasB)
            {
                token.ThrowIfCancellationRequested();
                if(hasA&&manual.TryGetValue(a.Current.Fact.Key,out var mapping))
                {
                    var ordinal=Read(a.Current,source);var destination=mapping.Target==null?(int?)null:destinations[mapping.Target];
                    if(destination is{} manualTarget&&target[manualTarget].Name!=mapping.Target)throw new VerificationException(Reason.TemporaryIntegrity);
                    yield return Pair(ordinal,destination);hasA=a.MoveNext();continue;
                }
                if(hasB&&reserved.Contains(b.Current.Fact.Key)){_=Read(b.Current,target);hasB=b.MoveNext();continue;}
                var order=!hasA?1:!hasB?-1:StringComparer.Ordinal.Compare(a.Current.Fact.Key,b.Current.Fact.Key);
                var from=hasA&&order<=0?Read(a.Current,source):(int?)null;
                var to=hasB&&order>=0?Read(b.Current,target):(int?)null;
                yield return Pair(from,to);
                if(order<=0)hasA=a.MoveNext();if(order>=0)hasB=b.MoveNext();
            }
        }
        static CatalogFact Pair(int? from,int? to)=>new(Key(from,to),from?.ToString(System.Globalization.CultureInfo.InvariantCulture),"column-ordinal",to?.ToString(System.Globalization.CultureInfo.InvariantCulture));
        static int Read(CatalogComparison.CatalogFactIndex.Entry entry,IReadOnlyList<Column> columns)
        {
            if(entry.Duplicate)throw new VerificationException(Reason.AmbiguousScope);
            if(entry.Fact.Attribute!="column-name"||entry.Fact.Column!=null)throw new VerificationException(Reason.TemporaryIntegrity);
            var ordinal=Ordinal(entry.Fact.Value,columns.Count);
            if(entry.Fact.Key!=columns[ordinal].Name)throw new VerificationException(Reason.TemporaryIntegrity);
            return ordinal;
        }
        static int Ordinal(string? value,int count)
            =>int.TryParse(value,System.Globalization.NumberStyles.None,System.Globalization.CultureInfo.InvariantCulture,out var ordinal)&&ordinal<count
                ?ordinal:throw new VerificationException(Reason.TemporaryIntegrity);
        static string Key(int? from,int? to)=>from is{} ordinal?"source:"+ordinal.ToString("D10",System.Globalization.CultureInfo.InvariantCulture)
            :"target:"+to!.Value.ToString("D10",System.Globalization.CultureInfo.InvariantCulture);
    }
    private sealed class StoredColumns(IReadOnlyList<ColumnOrdinals> records,int start,int count,Guid batch,IReadOnlyList<Column> source,IReadOnlyList<Column> target):IFrozenMetadata<ColumnMapping>
    {
        public int Count=>count;
        public ColumnMapping this[int index]
        {
            get
            {
                if(index<0||index>=Count)throw new ArgumentOutOfRangeException(nameof(index));
                var pair=records[start+index];
                if(pair.Tables||pair.Batch!=batch||(index<source.Count?pair.Source!=index:pair.Source!=null)||pair.Source>=source.Count||pair.Target>=target.Count)throw new VerificationException(Reason.TemporaryIntegrity);
                return new(pair.Source is{} from?source[from].Name:null,pair.Target is{} to?target[to].Name:null);
            }
        }
        public IEnumerator<ColumnMapping> GetEnumerator(){for(var i=0;i<Count;i++)yield return this[i];}
        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()=>GetEnumerator();
    }
    private sealed class OrdinalColumns(IReadOnlyList<Column>? source,IReadOnlyList<Column>? target):IFrozenMetadata<ColumnMapping>
    {
        public int Count=>source?.Count??target!.Count;
        public ColumnMapping this[int index]
        {
            get
            {
                if(index<0||index>=Count)throw new ArgumentOutOfRangeException(nameof(index));
                return new(source?[index].Name,target?[index].Name);
            }
        }
        public IEnumerator<ColumnMapping> GetEnumerator(){for(var i=0;i<Count;i++)yield return this[i];}
        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()=>GetEnumerator();
    }
    private sealed class DeferredColumns(Func<IReadOnlyList<ColumnMapping>> resolve):IReadOnlyList<ColumnMapping>
    {
        private Lazy<IReadOnlyList<ColumnMapping>> value=new(resolve);
        internal void Retain(IReadOnlyList<ColumnMapping> columns)=>value=new(()=>columns);
        public int Count=>value.Value.Count;
        public ColumnMapping this[int index]=>value.Value[index];
        public IEnumerator<ColumnMapping> GetEnumerator()=>value.Value.GetEnumerator();
        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()=>GetEnumerator();
    }
    internal static void RetainColumns(IReadOnlyList<ColumnMapping> original,IReadOnlyList<ColumnMapping> retained)
    {if(original is DeferredColumns deferred)deferred.Retain(retained);}
    // Resolve always returns source-first ordinal qualified order. Its keys
    // therefore have ordinal order too, without another whole-scope name map.
    internal static MatchedScope? Find(IReadOnlyList<MatchedScope> scopes,string key)
    {
        var low=0;var high=scopes.Count-1;
        while(low<=high)
        {
            var middle=low+(high-low)/2;var scope=scopes[middle];var order=StringComparer.Ordinal.Compare(scope.Key,key);
            if(order==0)return scope;if(order<0)low=middle+1;else high=middle-1;
        }
        return null;
    }
    public static IReadOnlyList<ColumnMapping> ResolveColumns(Table source,Table? target,IReadOnlyList<ColumnMapping> overrides,CancellationToken token=default)
    {
        token.ThrowIfCancellationRequested();
        if(ProtectedColumnStore.OwnerOf(source.Columns) is{} owner&&(target==null||target.Columns is IFrozenMetadata<Column>))
        {
            // Validate and freeze explicit choices now, before the caller can
            // edit its draft. The wrapper can later bind the run-owned snapshot.
            var columns=ResolveMixedColumns(source.Columns,target?.Columns??[],owner,token,overrides);
            return new DeferredColumns(()=>columns);
        }
        var names=source.Columns.Select(c=>{token.ThrowIfCancellationRequested();return c.Name;}).ToHashSet(StringComparer.Ordinal);
        var destinations=(target?.Columns??[]).Select(c=>{token.ThrowIfCancellationRequested();return c.Name;}).ToHashSet(StringComparer.Ordinal);
        if(overrides.Any(x=>x==null||x.Source==null||!names.Contains(x.Source)||x.Target!=null&&!destinations.Contains(x.Target)) || overrides.Select(x=>x.Source).Distinct(StringComparer.Ordinal).Count()!=overrides.Count || overrides.Where(x=>x.Target!=null).Select(x=>x.Target).Distinct(StringComparer.Ordinal).Count()!=overrides.Count(x=>x.Target!=null)) throw new VerificationException(Reason.InvalidInput);
        var manual=overrides.ToDictionary(x=>x.Source!,StringComparer.Ordinal);
        var reserved=overrides.Where(x=>x.Target!=null).Select(x=>x.Target!).ToHashSet(StringComparer.Ordinal);
        var used=new HashSet<string>(StringComparer.Ordinal);var result=new List<ColumnMapping>();
        foreach(var column in source.Columns)
        {
            token.ThrowIfCancellationRequested();
            var destination=manual.TryGetValue(column.Name,out var mapping)?mapping.Target:!reserved.Contains(column.Name)&&destinations.Contains(column.Name)?column.Name:null;
            if(destination!=null && !used.Add(destination)) throw new VerificationException(Reason.InvalidInput);
            result.Add(new(column.Name,destination));
        }
        foreach(var column in target?.Columns??[]){token.ThrowIfCancellationRequested();if(!used.Contains(column.Name))result.Add(new(null,column.Name));}
        return result;
    }
    // Native roots borrow their observer's existing ordinal owner. Only table
    // pairs are retained; each lookup obtains fresh authenticated issued tables.
    // Ownerless inputs retain the existing eager public API behavior.
    private static IReadOnlyList<MatchedScope> ResolveFrozen(Inventory source,Inventory target,IReadOnlyList<TableMapping> overrides,CancellationToken token)
    {
        token.ThrowIfCancellationRequested();var local=source.DatabaseIsNamespace&&target.DatabaseIsNamespace;
        var manual=new Dictionary<string,TableMapping>(StringComparer.Ordinal);
        var reserved=new HashSet<string>(StringComparer.Ordinal);var invalid=false;
        foreach(var mapping in overrides)
        {
            token.ThrowIfCancellationRequested();
            if(mapping==null||mapping.Source==null||mapping.Columns==null){invalid=true;continue;}
            if(!manual.TryAdd(mapping.Source,mapping))invalid=true;
            if(mapping.Target!=null&&!reserved.Add(mapping.Target))invalid=true;
        }
        var found=new HashSet<string>(StringComparer.Ordinal);var destinations=new Dictionary<string,int>(StringComparer.Ordinal);
        ProtectedColumnStore? sourceOwner=null,targetOwner=null;var sourceOwned=true;var targetOwned=true;
        var manualColumns=new Dictionary<int,IReadOnlyList<ColumnMapping>>();
        using var left=new CatalogComparison.CatalogFactIndex(Names(source,true),false,token);
        using var right=new CatalogComparison.CatalogFactIndex(Names(target,false),false,token);
        if(invalid||found.Count!=manual.Count||destinations.Count!=reserved.Count)throw new VerificationException(Reason.InvalidInput);
        using var ordered=new CatalogComparison.CatalogFactIndex(Pairs(),false,token);
        var owner=sourceOwned&&targetOwned?sourceOwner??targetOwner:null;
        if(owner==null)return Ordinals().Select(pair=>Scope(pair,source,target,manualColumns,token)).ToArray();
        var stored=owner.StoreMappings(Ordinals());
        return new StoredScopes(stored.Records,stored.Start,stored.Count,stored.Batch,source,target,manualColumns,token);

        IEnumerable<ColumnOrdinals> Ordinals()
        {
            foreach(var entry in ordered.Entries(token))
            {
                token.ThrowIfCancellationRequested();var fact=entry.Fact;
                if(entry.Duplicate||fact.Attribute!="table-scope-ordinal")throw new VerificationException(Reason.TemporaryIntegrity);
                var from=fact.Column==null?(int?)null:TableOrdinal(fact.Column,source.Tables.Count);
                var to=fact.Value==null?(int?)null:TableOrdinal(fact.Value,target.Tables.Count);
                var pair=new ColumnOrdinals(from,to){Tables=true};
                if(from==null&&to==null||fact.Key!=ScopeKey(from is{} i?source.Tables[i]:null,to is{} j?target.Tables[j]:null))
                    throw new VerificationException(Reason.TemporaryIntegrity);
                yield return pair;
            }
            token.ThrowIfCancellationRequested();
        }
        IEnumerable<CatalogFact> Pairs()
        {
            using var a=left.Entries(token).GetEnumerator();using var b=right.Entries(token).GetEnumerator();
            var from=Next(source,a);var to=Next(target,b);
            while(from!=null||to!=null)
            {
                token.ThrowIfCancellationRequested();
                if(from!=null&&manual.TryGetValue(from.Table.Qualified,out var mapping))
                {
                    var destination=mapping.Target==null?null:new IndexedTable(destinations[mapping.Target],target.Tables[destinations[mapping.Target]]);
                    if(destination!=null&&destination.Table.Qualified!=mapping.Target)throw new VerificationException(Reason.TemporaryIntegrity);
                    manualColumns.Add(from.Ordinal,mapping.Columns.Count==0?DefaultScopeColumns(from.Table,destination?.Table,token):ResolveColumns(from.Table,destination?.Table,mapping.Columns,token));
                    yield return Pair(from,destination);from=Next(source,a);continue;
                }
                if(to!=null&&reserved.Contains(to.Table.Qualified)){to=Next(target,b);continue;}
                var order=from==null?1:to==null?-1:StringComparer.Ordinal.Compare(local?from.Table.Name:from.Table.Qualified,local?to.Table.Name:to.Table.Qualified);
                yield return Pair(order<=0?from:null,order>=0?to:null);
                if(order<=0)from=Next(source,a);if(order>=0)to=Next(target,b);
            }
        }
        static CatalogFact Pair(IndexedTable? from,IndexedTable? to)=>new(ScopeKey(from?.Table,to?.Table),
            from?.Ordinal.ToString(System.Globalization.CultureInfo.InvariantCulture),"table-scope-ordinal",to?.Ordinal.ToString(System.Globalization.CultureInfo.InvariantCulture));
        IndexedTable? Next(Inventory inventory,IEnumerator<CatalogComparison.CatalogFactIndex.Entry> entries)
        {
            token.ThrowIfCancellationRequested();if(!entries.MoveNext())return null;
            var entry=entries.Current;if(entry.Duplicate)throw new VerificationException(Reason.AmbiguousScope);
            var fact=entry.Fact;
            if(fact.Attribute!="scope-ordinal"||fact.Column!=null||fact.Value==null)throw new VerificationException(Reason.TemporaryIntegrity);
            var ordinal=TableOrdinal(fact.Value,inventory.Tables.Count);var table=inventory.Tables[ordinal];
            if(fact.Key!=(local?table.Name:table.Qualified))throw new VerificationException(Reason.TemporaryIntegrity);
            return new(ordinal,table);
        }
        IEnumerable<CatalogFact> Names(Inventory inventory,bool sourceSide)
        {
            string? schema=null;var count=0;
            foreach(var table in inventory.Tables)
            {
                token.ThrowIfCancellationRequested();
                if(string.IsNullOrWhiteSpace(table.Schema)||string.IsNullOrWhiteSpace(table.Name))throw new VerificationException(Reason.AmbiguousScope);
                if(inventory.DatabaseIsNamespace){if(schema!=null&&schema!=table.Schema)throw new VerificationException(Reason.AmbiguousScope);schema=table.Schema;}
                ValidateColumns(table,token);var columnOwner=ProtectedColumnStore.OwnerOf(table.Columns);
                if(sourceSide)
                {
                    if(manual.ContainsKey(table.Qualified))found.Add(table.Qualified);
                    if(columnOwner==null||sourceOwner!=null&&!ReferenceEquals(sourceOwner,columnOwner))sourceOwned=false;
                    sourceOwner??=columnOwner;
                }
                else
                {
                    if(reserved.Contains(table.Qualified)&&!destinations.TryAdd(table.Qualified,count))throw new VerificationException(Reason.AmbiguousScope);
                    if(columnOwner==null||targetOwner!=null&&!ReferenceEquals(targetOwner,columnOwner))targetOwned=false;
                    targetOwner??=columnOwner;
                }
                yield return new(local?table.Name:table.Qualified,null,"scope-ordinal",count.ToString(System.Globalization.CultureInfo.InvariantCulture));count=checked(count+1);
            }
            if(count!=inventory.Tables.Count)throw new VerificationException(Reason.AmbiguousScope);
        }
    }
    private sealed record IndexedTable(int Ordinal,Table Table);
    private static int TableOrdinal(string value,int count)
        =>int.TryParse(value,System.Globalization.NumberStyles.None,System.Globalization.CultureInfo.InvariantCulture,out var ordinal)&&ordinal<count
            ?ordinal:throw new VerificationException(Reason.TemporaryIntegrity);
    private static string ScopeKey(Table? source,Table? target)=>(source!=null?"source:":"target:")+(source?.Qualified??target?.Qualified??throw new VerificationException(Reason.TemporaryIntegrity));
    private static IReadOnlyList<ColumnMapping> DefaultScopeColumns(Table? source,Table? target,CancellationToken token)
        =>source!=null?ResolveObservedColumns(source,target,[],token):target!.Columns is IFrozenMetadata<Column>
            ?new DeferredColumns(()=>{token.ThrowIfCancellationRequested();return new OrdinalColumns(null,target.Columns);})
            :target.Columns.Select(c=>{token.ThrowIfCancellationRequested();return new ColumnMapping(null,c.Name);}).ToArray();
    private static MatchedScope Scope(ColumnOrdinals pair,Inventory source,Inventory target,IReadOnlyDictionary<int,IReadOnlyList<ColumnMapping>> manual,CancellationToken token)
    {
        if(!pair.Tables||pair.Source==null&&pair.Target==null||pair.Source is{} i&&(i<0||i>=source.Tables.Count)||pair.Target is{} j&&(j<0||j>=target.Tables.Count))
            throw new VerificationException(Reason.TemporaryIntegrity);
        var from=pair.Source is{} a?source.Tables[a]:null;var to=pair.Target is{} b?target.Tables[b]:null;
        var columns=pair.Source is{} ordinal&&manual.TryGetValue(ordinal,out var resolved)?resolved:DefaultScopeColumns(from,to,token);
        return new(from,to,columns);
    }
    private sealed class StoredScopes(IReadOnlyList<ColumnOrdinals> records,int start,int count,Guid batch,Inventory source,Inventory target,
        IReadOnlyDictionary<int,IReadOnlyList<ColumnMapping>> manual,CancellationToken token):IFrozenMetadata<MatchedScope>
    {
        public int Count=>count;
        public MatchedScope this[int index]
        {
            get
            {
                if(index<0||index>=count)throw new ArgumentOutOfRangeException(nameof(index));
                var pair=records[start+index];if(pair.Batch!=batch)throw new VerificationException(Reason.TemporaryIntegrity);
                return Scope(pair,source,target,manual,token);
            }
        }
        public IEnumerator<MatchedScope> GetEnumerator(){for(var i=0;i<count;i++)yield return this[i];}
        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()=>GetEnumerator();
    }
    private static void ValidateColumns(Table table,CancellationToken token)
    {
        using var index=new CatalogComparison.CatalogFactIndex(Identifiers(),false,token);
        foreach(var entry in index.Entries(token))
            if(entry.Duplicate)throw new VerificationException(Reason.AmbiguousScope);
        IEnumerable<CatalogFact> Identifiers()
        {
            var count=0;
            foreach(var column in table.Columns)
            {
                token.ThrowIfCancellationRequested();count=checked(count+1);
                if(column.Name==null)throw new VerificationException(Reason.AmbiguousScope);
                yield return new("name:"+column.Name,null,"scope-column",null);
                yield return new("position:"+column.Position.ToString(System.Globalization.CultureInfo.InvariantCulture),null,"scope-column",null);
            }
            if(count!=table.Columns.Count)throw new VerificationException(Reason.AmbiguousScope);
        }
    }
    private static void ValidateInventory(Inventory inventory,CancellationToken token)
    {
        token.ThrowIfCancellationRequested();
        var tables=new HashSet<string>(StringComparer.Ordinal);var schemas=new HashSet<string>(StringComparer.Ordinal);
        foreach(var table in inventory.Tables)
        {
            token.ThrowIfCancellationRequested();
            if(string.IsNullOrWhiteSpace(table.Schema)||string.IsNullOrWhiteSpace(table.Name)||!tables.Add(table.Qualified))throw new VerificationException(Reason.AmbiguousScope);
            if(inventory.DatabaseIsNamespace&&schemas.Add(table.Schema)&&schemas.Count>1)throw new VerificationException(Reason.AmbiguousScope);
            ValidateColumns(table,token);
        }
        if(tables.Count!=inventory.Tables.Count)throw new VerificationException(Reason.AmbiguousScope);
    }
}
