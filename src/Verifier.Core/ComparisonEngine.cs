namespace Verifier.Core;

public sealed partial class ComparisonEngine
{
    public async Task<RunReport> RunAsync(IReadDatabase source,IReadDatabase target,IReadOnlySet<Category> selected,IReadOnlyCollection<string>? tables,
        string adapter,bool example,IProgress<ProgressInfo>? progress,CancellationToken token,IReadOnlyList<TableMapping>? mappings=null,bool databaseScope=false)
    {
        var start=DateTimeOffset.UtcNow; var results=Checks.Initial(selected); var id=Guid.NewGuid();var storage=new RunReportStorage();
        IReadOnlyList<string> scope=[];
        IReadOnlyList<TableMapping> appliedMappings=[];
        var databaseIsNamespace=false;
        progress?.Report(new("Inspect",0,results.ToArray()));
        try
        {
            if(selected.Count==0 || selected.Any(x=>!Enum.IsDefined(x))) throw new VerificationException(Reason.InvalidInput);
            var a=await source.InspectAsync(token); var b=await target.InspectAsync(token);
            databaseIsNamespace=a.DatabaseIsNamespace&&b.DatabaseIsNamespace;
            if(string.IsNullOrWhiteSpace(a.Identity) || string.IsNullOrWhiteSpace(b.Identity)) throw new VerificationException(Reason.IdentityUnknown);
            if(a.Identity==b.Identity) throw new VerificationException(Reason.SameDatabase);
            var plan=ScopeMatching.Resolve(a,b,mappings??[],token);
            if(tables is { Count:0 }&&!databaseScope)throw new VerificationException(Reason.InvalidInput);
            using var selection=new ScopeOrdinals(plan.Count);
            IReadOnlyList<MatchedScope> selectedScopes=SelectScopes(plan,tables,mappings!=null,selection,token);
            if(selectedScopes.Count==0&&!databaseScope) throw new VerificationException(a.Complete&&b.Complete ? Reason.NoObjects : Reason.VisibilityUnknown);
            if(mappings!=null)
            {
                // Publish names and detached fallback headers together. A
                // failed column write cannot borrow a disposed native root.
                var headers=MappingBuffer.Headers(selectedScopes,storage,token);
                var names=TableNameBuffer.SortedDistinct(selectedScopes.SelectMany(s=>new[]{s.Source?.Qualified,s.Target?.Qualified}).OfType<string>(),storage,token);
                appliedMappings=headers;scope=names;
                var retained=new MappingBuffer(storage,token);
                foreach(var item in selectedScopes)retained.Add(item);
                appliedMappings=retained.Snapshot();
                selectedScopes=new RetainedScopes(selectedScopes,appliedMappings);
            }
            else scope=TableNameBuffer.SortedDistinct(selectedScopes.SelectMany(s=>new[]{s.Source?.Qualified,s.Target?.Qualified}).OfType<string>(),storage,token);
            var schemaFindings=new FindingBuffer(storage); var dataFindings=new FindingBuffer(storage);
            var schemaChecks=Checks.Definitions.Where(d=>d.Category is Category.Schema or Category.Access or Category.Objects).Select(d=>d.Id).ToArray();
            var observedCatalogChecks=selectedScopes.SelectMany(s=>new[]{s.Source,s.Target}).OfType<Table>()
                .SelectMany(t=>t.Catalog).Select(c=>c.CheckId).ToHashSet(StringComparer.Ordinal);
            if(databaseScope)observedCatalogChecks.UnionWith(CatalogComparison.DatabaseChecks);
            var catalogs=schemaChecks.ToDictionary(c=>c,_=>new FindingBuffer(storage));
            var schemaIssues=schemaChecks.ToDictionary(c=>c,_=>new IssueBuffer(storage));
            var schemaReasons=schemaChecks.ToDictionary(c=>c,_=>Reason.None);
            var dataIssues=new IssueBuffer(storage);
            var pairingLimits=new IssueBuffer(storage);
            var dataReason=Reason.None;
            var integrityFindings=new FindingBuffer(storage);var integrityIssues=new IssueBuffer(storage);
            var integrityObserved=source is IIntegrityDatabase||target is IIntegrityDatabase;
            using var comparedPartitionScopes=new ScopeOrdinals(selectedScopes.Count);
            using var blockStorage=new RunReportStorage();
            var blockNames=new TableNameBuffer(blockStorage,token);IReadOnlyList<string> blockedCatalogs=[];var collectBlocks=true;
            var activeStage="ObserveCatalogs";
            progress?.Report(new(activeStage,0,results.ToArray()));
            await ObserveCatalogs(source,false);await ObserveCatalogs(target,true);
            blockedCatalogs=TableNameBuffer.SortedDistinct(blockNames.Snapshot(),blockStorage,token);collectBlocks=false;
            if(databaseScope)
            {
                foreach(var check in CatalogComparison.DatabaseChecks.Where(c=>results.Single(r=>r.CheckId==c).Selected))
                {
                    if(TableNameBuffer.Contains(blockedCatalogs,CatalogBlockKey(check,null)))continue;
                    var compared=CatalogComparison.CompareDatabaseForRun(check,a,b,storage,token);
                    catalogs[check].AddRange(compared.Findings);schemaReasons[check]=compared.Reason;schemaIssues[check].AddRange(compared.Issues);
                }
                if(selectedScopes.Count==0)
                {
                    dataReason=Reason.NoObjects;dataIssues.Add(new(null,null,Reason.NoObjects));
                    integrityIssues.Add(new(null,null,Reason.NoObjects));
                    foreach(var check in schemaChecks.Except(CatalogComparison.DatabaseChecks))
                    {schemaReasons[check]=Reason.NoObjects;schemaIssues[check].Add(new(null,null,Reason.NoObjects));}
                }
                Publish();
            }
            foreach(var (scopeOrdinal,item) in OrderedScopes(selectedScopes,token))
            {
                token.ThrowIfCancellationRequested();
                var name=item.Location;var left=item.Source;var right=item.Target;
                activeStage=name;
                progress?.Report(new(name,0,results.ToArray()));
                if(selected.Contains(Category.Data)&&integrityObserved){await ObserveIntegrity(source,left,true,name);await ObserveIntegrity(target,right,false,name);Publish();}
                if(left is null || right is null)
                {
                    if(a.Complete && b.Complete) schemaFindings.Add(NewFinding(name,null,"TablePresence",1));
                    foreach(var check in schemaChecks){schemaReasons[check]=Reason.MappingIncomplete;schemaIssues[check].Add(new(name,null,Reason.MappingIncomplete));}
                    dataReason=Reason.VisibilityUnknown; dataIssues.Add(new(name,null,dataReason)); Publish(); continue;
                }
                if(selected.Any(c=>c is Category.Schema or Category.Access or Category.Objects))
                {
                    if(selected.Contains(Category.Schema)&&!TableNameBuffer.Contains(blockedCatalogs,CatalogBlockKey("SCHEMA-01",name))&&(databaseIsNamespace?left.Name!=right.Name:left.Qualified!=right.Qualified))schemaFindings.Add(NewFinding(name,null,"TableName",1));
                    if(selected.Contains(Category.Schema)&&!TableNameBuffer.Contains(blockedCatalogs,CatalogBlockKey("SCHEMA-01",name))&&(!left.Catalog.Any(s=>s.CheckId=="SCHEMA-01")||!right.Catalog.Any(s=>s.CheckId=="SCHEMA-01")))foreach(var column in item.Columns)
                    {
                        var lc=left.Columns.SingleOrDefault(x=>x.Name==column.Source); var rc=right.Columns.SingleOrDefault(x=>x.Name==column.Target);
                        if((lc?.Name,lc?.Type,lc?.Position,lc?.Nullable,lc?.Definition)!=(rc?.Name,rc?.Type,rc?.Position,rc?.Nullable,rc?.Definition)) schemaFindings.Add(NewFinding(name,column.Source??column.Target,"ColumnDefinition",1));
                    }
                    using var columnMap=new CatalogColumnMap(item.Columns,token);
                    foreach(var check in schemaChecks.Where(c=>results.Single(r=>r.CheckId==c).Selected))
                    {
                        if(TableNameBuffer.Contains(blockedCatalogs,CatalogBlockKey(check,name)))continue;
                        var compared=CatalogComparison.CompareForRun(check,item,a.Complete&&b.Complete,storage,token,columnMap);catalogs[check].AddRange(compared.Findings);
                        if(compared.Reason!=Reason.None)schemaReasons[check]=compared.Reason;
                        schemaIssues[check].AddRange(compared.Issues);
                    }
                }
                Publish();
                if(!selected.Contains(Category.Data)) continue;
                if(!item.Complete)
                { dataReason=Reason.MappingIncomplete;foreach(var column in item.Columns.Where(c=>c.Source==null||c.Target==null))dataIssues.Add(new(name,column.Source??column.Target,dataReason)); }
                if(!left.SafeRead || !right.SafeRead) { dataReason=UnreadableReason(!left.SafeRead?left:right); dataIssues.Add(new(name,null,dataReason)); Publish(); continue; }
                // Skip a child only after a successful logical parent comparison with the same projection.
                // Failed/unsupported parent reads must never suppress independently readable child evidence.
                if(comparedPartitionScopes.Any(ordinal=>selectedScopes[ordinal] is{} parent&&parent.Source!.DataDescendants.Contains(left.Qualified,StringComparer.Ordinal)
                    &&parent.Target!.DataDescendants.Contains(right.Qualified,StringComparer.Ordinal)
                    &&parent.Columns.SequenceEqual(item.Columns)))continue;
                var leftIndexes=left.Columns.Select((column,index)=>(column.Name,index)).ToDictionary(c=>c.Name,c=>c.index,StringComparer.Ordinal);
                var rightIndexes=right.Columns.Select((column,index)=>(column.Name,index)).ToDictionary(c=>c.Name,c=>c.index,StringComparer.Ordinal);
                var comparable=new List<ColumnMapping>();
                foreach(var column in item.Columns.Where(c=>c.Source!=null&&c.Target!=null))
                {
                    var lc=left.Columns[leftIndexes[column.Source!]];var rc=right.Columns[rightIndexes[column.Target!]];
                    if(lc.SafeValue&&rc.SafeValue&&lc.Type==rc.Type)comparable.Add(column);
                    else{dataReason=Reason.UnsupportedType;dataIssues.Add(new(name,column.Source,dataReason));}
                }
                if(comparable.Count==0&&(left.Columns.Count>0||right.Columns.Count>0)){Publish();continue;}
                var leftOrder=comparable.Select(c=>leftIndexes[c.Source!]).ToArray();var rightOrder=comparable.Select(c=>rightIndexes[c.Target!]).ToArray();
                var completeProjection=item.Complete&&comparable.Count==left.Columns.Count&&comparable.Count==right.Columns.Count;
                try
                {
                    (long onlyL,long onlyR) difference;
                    var key=CommonKey(left,right,comparable);
                    if(key!=null)
                    {
                        var keyed=await CompareKeyed(source,target,left,right,leftOrder,rightOrder,key,name,progress,token);
                        difference=(keyed.SourceOnly,keyed.TargetOnly);
                        foreach(var (index,count) in keyed.Columns)dataFindings.Add(NewFinding(name,comparable[index].Source,"ColumnValue",count));
                        if(keyed.Ambiguous)pairingLimits.Add(new(name,null,Reason.AmbiguousRows));
                    }
                    else if(source is IStreamingReadDatabase ls&&target is IStreamingReadDatabase rs)
                        difference=await CompareStreaming(ls,rs,left,right,leftOrder,rightOrder,name,progress,token);
                    else difference=await CompareBuffered(source,target,left,right,leftOrder,rightOrder,name,progress,token);
                    var (onlyL,onlyR)=difference;
                    if(completeProjection&&left.DataDescendants.Count>0&&right.DataDescendants.Count>0)comparedPartitionScopes.Add(scopeOrdinal);
                    var column=completeProjection||comparable.Count!=1?null:comparable[0].Source;
                    if(onlyL>0) dataFindings.Add(NewFinding(name,column,completeProjection?"SourceOnlyRow":"SourceOnlyProjection",onlyL));
                    if(onlyR>0) dataFindings.Add(NewFinding(name,column,completeProjection?"TargetOnlyRow":"TargetOnlyProjection",onlyR));
                }
                catch(VerificationException e) { dataReason=e.Reason; dataIssues.Add(new(name,null,dataReason)); }
                Publish();
            }
            activeStage="ObserveCatalogs";
            progress?.Report(new(activeStage,0,results.ToArray()));
            await ObserveCatalogs(source,false);await ObserveCatalogs(target,true);
            SetSchema();
            Set("DATA-01",dataFindings,dataReason!=Reason.None ? dataReason : !a.Complete || !b.Complete ? Reason.VisibilityUnknown : Reason.None);
            if(integrityObserved)Set("DATA-02",integrityFindings,IntegrityReason());
            AttachIssues();
            progress?.Report(new("Finalize",0,results.ToArray()));
            async Task ObserveCatalogs(IReadDatabase database,bool isTarget)
            {
                if(database is not ICatalogObservationDatabase observer)return;
                var requested=schemaChecks.Where(c=>results.Single(r=>r.CheckId==c).Selected).ToHashSet(StringComparer.Ordinal);
                if(requested.Count==0)return;
                // Resolve guarantees one-to-one pairs; each projection obtains
                // the native inventory's issued handle without cloning it.
                var observed=new ScopeTables(selectedScopes,isTarget,token);
                var limits=await observer.InspectCatalogChangesAsync(observed,requested,databaseScope,token);
                if(limits==null)throw new VerificationException(Reason.ObservationChanged);
                using var locations=new ObservationScopeLocations(selectedScopes,isTarget,token);
                foreach(var limit in limits)
                {
                    if(limit==null||!requested.Contains(limit.CheckId)||limit.Reason==Reason.None||!Enum.IsDefined(limit.Reason)||limit.Attribute!=null&&(!CatalogComparison.Fields.TryGetValue(limit.Attribute,out var field)||field.Check!=limit.CheckId))throw new VerificationException(Reason.ObservationChanged);
                    var location=limit.Table==null?null:locations.Find(limit.Table);
                    if(limit.Table==null? !databaseScope||!CatalogComparison.DatabaseChecks.Contains(limit.CheckId):location==null)throw new VerificationException(Reason.ObservationChanged);
                    if(collectBlocks)blockNames.Add(CatalogBlockKey(limit.CheckId,location));
                    catalogs[limit.CheckId].RemoveAll(f=>f.Table==location);
                    if(limit.CheckId=="SCHEMA-01")schemaFindings.RemoveAll(f=>f.Table==location);
                    schemaReasons[limit.CheckId]=limit.Reason;
                    var issue=new ScopeIssue(location,null,limit.Reason){Attribute=limit.Attribute};
                    if(!schemaIssues[limit.CheckId].Contains(issue))schemaIssues[limit.CheckId].Add(issue);
                }
            }
            void Publish()
            {
                SetSchema(Execution.Running);
                Set("DATA-01",dataFindings,dataReason,Execution.Running);
                if(integrityObserved)Set("DATA-02",integrityFindings,IntegrityReason(),Execution.Running);
                AttachIssues();
                progress?.Report(new(activeStage,0,results.ToArray()));
            }
            void AttachIssues()
            {
                var index=results.FindIndex(x=>x.CheckId=="DATA-01");if(results[index].Selected)results[index]=results[index] with{Issues=dataIssues.Snapshot(),PairingLimits=pairingLimits.Snapshot()};
                index=results.FindIndex(x=>x.CheckId=="DATA-02");if(results[index].Selected)results[index]=results[index] with{Issues=integrityIssues.Snapshot()};
            }
            Reason IntegrityReason()=>integrityIssues.Count>0?integrityIssues[0].Reason:!a.Complete||!b.Complete?Reason.VisibilityUnknown:Reason.None;
            async Task ObserveIntegrity(IReadDatabase database,Table? table,bool isSource,string location)
            {
                if(table==null)return;
                if(database is not IIntegrityDatabase observer){integrityIssues.Add(new(location,null,Reason.NotImplemented));return;}
                try
                {
                    var observation=await observer.InspectIntegrityAsync(table,token);
                    foreach(var violation in observation.Violations)
                    {
                        if(violation.Count<=0||violation.Kind is not("NotNull" or "Unique" or "Reference" or "Check" or "Exclusion"))throw new VerificationException(Reason.ObservationChanged);
                        integrityFindings.Add(NewFinding(location,violation.Column,(isSource?"Source":"Target")+violation.Kind+"Violation",violation.Count));
                    }
                    integrityIssues.AddRange(observation.Issues.Select(i=>i with{Table=location}));
                }
                catch(VerificationException e){integrityIssues.Add(new(location,null,e.Reason));}
            }
            void SetSchema(Execution execution=Execution.Completed)
            {
                foreach(var check in schemaChecks)
                {
                    // An absent collector is not a running or completed inspection.
                    if(results.Single(r=>r.CheckId==check).Category is Category.Access or Category.Objects && !observedCatalogChecks.Contains(check)) continue;
                    var findings=check=="SCHEMA-01"?FindingBuffer.Join(schemaFindings,catalogs[check]):catalogs[check];
                    Set(check,findings,schemaReasons[check],execution);var index=results.FindIndex(r=>r.CheckId==check);
                    if(results[index].Selected)results[index]=results[index] with { Issues=schemaIssues[check].Snapshot() };
                }
            }
        }
        catch(Exception e) when(e is OperationCanceledException or VerificationException or IOException or UnauthorizedAccessException)
        {
            var reason=e is OperationCanceledException ? Reason.Cancelled : e is VerificationException ve ? ve.Reason : Reason.TemporaryStorage;
            for(var i=0;i<results.Count;i++) if(results[i].Selected && results[i].Execution!=Execution.Completed)
                results[i]=results[i] with { Execution=results[i].Execution==Execution.Pending?Execution.Pending:Execution.Interrupted,Verdict=results[i].Findings.Count>0 ? Verdict.Different : Verdict.Indeterminate,Reason=reason };
        }
        catch{storage.Dispose();throw;}
        return new(mappings==null?1:2,id,start,DateTimeOffset.UtcNow,example,adapter,scope,results) { Mappings=appliedMappings,DatabaseScope=databaseScope,DatabaseIsNamespace=databaseIsNamespace,LocalStorage=storage };
        void Set(string check,IReadOnlyList<Finding> findings,Reason reason,Execution execution=Execution.Completed)
        {
            var index=results.FindIndex(x=>x.CheckId==check);
            if(!results[index].Selected) return;
            results[index]=results[index] with { Execution=execution, Verdict=findings.Count>0 ? Verdict.Different : reason!=Reason.None || execution!=Execution.Completed ? Verdict.Indeterminate : Verdict.Equal, Reason=reason,Findings=FindingBuffer.Freeze(findings) };
        }
    }
    private static Reason UnreadableReason(Table table)=>table.ReadReason!=Reason.None&&Enum.IsDefined(table.ReadReason)?table.ReadReason:Reason.ObservationChanged;
    private static async Task<(long,long)> CompareStreaming(IStreamingReadDatabase source,IStreamingReadDatabase target,Table left,Table right,IReadOnlyList<int> leftOrder,IReadOnlyList<int> rightOrder,string name,IProgress<ProgressInfo>? progress,CancellationToken token)
    {
        using var store=new ProtectedRowStore(token:token);using var a=new ProtectedSort(rowComparer:store);using var b=new ProtectedSort(rowComparer:store);long rows=0;
        await foreach(var writer in source.StreamingRowsAsync(left,leftOrder,token))
        {a.Add(await store.AddAsync(writer,token));if(++rows%1000==0)progress?.Report(new(name,rows));}
        await foreach(var writer in target.StreamingRowsAsync(right,rightOrder,token))
        {b.Add(await store.AddAsync(writer,token));if(++rows%1000==0)progress?.Report(new(name,rows));}
        store.Flush();progress?.Report(new(name,rows));
        return CompareSorted(Validated(a.Finish(token)),Validated(b.Finish(token)),store,token);
        IEnumerable<byte[]> Validated(IEnumerable<byte[]> descriptors)
        {foreach(var descriptor in descriptors){store.Validate(descriptor,token);yield return descriptor;}}
    }
    private static async Task<(long,long)> CompareBuffered(IReadDatabase source,IReadDatabase target,Table left,Table right,IReadOnlyList<int> leftOrder,IReadOnlyList<int> rightOrder,string name,IProgress<ProgressInfo>? progress,CancellationToken token)
    {
        using var a=new ProtectedSort();using var b=new ProtectedSort();long rows=0;
        await foreach(var row in source.RowsAsync(left,token)){a.Add(ExactRow.Project(row,left.Columns.Count,leftOrder));if(++rows%1000==0)progress?.Report(new(name,rows));}
        await foreach(var row in target.RowsAsync(right,token)){b.Add(ExactRow.Project(row,right.Columns.Count,rightOrder));if(++rows%1000==0)progress?.Report(new(name,rows));}
        progress?.Report(new(name,rows));return CompareSorted(a.Finish(token),b.Finish(token),ProtectedSort.ByteComparer.Instance,token);
    }
    private static (long,long) CompareSorted(IEnumerable<byte[]> left,IEnumerable<byte[]> right,IComparer<byte[]> comparer,CancellationToken token)
    {
        using var a=left.GetEnumerator();using var b=right.GetEnumerator();var hasA=a.MoveNext();var hasB=b.MoveNext();long onlyA=0,onlyB=0;
        while(hasA||hasB)
        {
            token.ThrowIfCancellationRequested();var diff=!hasA?1:!hasB?-1:comparer.Compare(a.Current,b.Current);
            if(diff<=0){if(diff<0)onlyA++;hasA=a.MoveNext();}if(diff>=0){if(diff>0)onlyB++;hasB=b.MoveNext();}
        }
        return (onlyA,onlyB);
    }
    private static Finding NewFinding(string table,string? column,string kind,long count) => new(Guid.NewGuid().ToString("N"),table,column,kind,count);
}
