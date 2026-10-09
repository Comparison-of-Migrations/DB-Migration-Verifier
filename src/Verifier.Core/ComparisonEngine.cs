namespace Verifier.Core;

public sealed class ComparisonEngine
{
    public async Task<RunReport> RunAsync(IReadDatabase source,IReadDatabase target,IReadOnlySet<Category> selected,IReadOnlySet<string>? tables,
        string adapter,bool example,IProgress<ProgressInfo>? progress,CancellationToken token,IReadOnlyList<TableMapping>? mappings=null)
    {
        var start=DateTimeOffset.UtcNow; var results=Checks.Initial(selected); var id=Guid.NewGuid();
        var scope=new List<string>();
        var appliedMappings=new List<TableMapping>();
        progress?.Report(new("Inspect",0,results.ToArray()));
        try
        {
            if(selected.Count==0 || selected.Any(x=>!Enum.IsDefined(x))) throw new VerificationException(Reason.InvalidInput);
            var a=await source.InspectAsync(token); var b=await target.InspectAsync(token);
            if(string.IsNullOrWhiteSpace(a.Identity) || string.IsNullOrWhiteSpace(b.Identity)) throw new VerificationException(Reason.IdentityUnknown);
            if(a.Identity==b.Identity) throw new VerificationException(Reason.SameDatabase);
            var plan=ScopeMatching.Resolve(a,b,mappings??[]);
            string SelectionKey(MatchedScope item)=>mappings==null?item.Location:item.Key;
            if(tables is { Count:0 } || tables!=null&&tables.Except(plan.Select(SelectionKey),StringComparer.Ordinal).Any()) throw new VerificationException(Reason.InvalidInput);
            var selectedScopes=plan.Where(item=>tables==null||tables.Contains(SelectionKey(item))).ToArray();
            if(selectedScopes.Length==0) throw new VerificationException(a.Complete&&b.Complete ? Reason.NoObjects : Reason.VisibilityUnknown);
            scope.AddRange(selectedScopes.SelectMany(s=>new[]{s.Source?.Qualified,s.Target?.Qualified}).OfType<string>().Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal));
            if(mappings!=null)appliedMappings.AddRange(selectedScopes.Select(s=>s.Snapshot));
            var schemaFindings=new List<Finding>(); var dataFindings=new List<Finding>();
            var schemaChecks=Checks.Definitions.Where(d=>d.Category is Category.Schema or Category.Access or Category.Objects).Select(d=>d.Id).ToArray();
            var observedCatalogChecks=selectedScopes.SelectMany(s=>new[]{s.Source,s.Target}).OfType<Table>()
                .SelectMany(t=>t.Catalog).Select(c=>c.CheckId).ToHashSet(StringComparer.Ordinal);
            var catalogs=schemaChecks.ToDictionary(c=>c,_=>new List<Finding>());
            var schemaIssues=schemaChecks.ToDictionary(c=>c,_=>new List<ScopeIssue>());
            var schemaReasons=schemaChecks.ToDictionary(c=>c,_=>Reason.None);
            var dataIssues=new List<ScopeIssue>();
            var dataReason=Reason.None;
            foreach(var item in selectedScopes)
            {
                token.ThrowIfCancellationRequested();
                var name=item.Location;var left=item.Source;var right=item.Target;
                if(left is null || right is null)
                {
                    if(a.Complete && b.Complete) schemaFindings.Add(NewFinding(name,null,"TablePresence",1));
                    foreach(var check in schemaChecks){schemaReasons[check]=Reason.MappingIncomplete;schemaIssues[check].Add(new(name,null,Reason.MappingIncomplete));}
                    dataReason=Reason.VisibilityUnknown; dataIssues.Add(new(name,null,dataReason)); Publish(); continue;
                }
                if(selected.Any(c=>c is Category.Schema or Category.Access or Category.Objects))
                {
                    if(selected.Contains(Category.Schema)&&left.Qualified!=right.Qualified)schemaFindings.Add(NewFinding(name,null,"TableName",1));
                    if(selected.Contains(Category.Schema)&&(!left.Catalog.Any(s=>s.CheckId=="SCHEMA-01")||!right.Catalog.Any(s=>s.CheckId=="SCHEMA-01")))foreach(var column in item.Columns)
                    {
                        var lc=left.Columns.SingleOrDefault(x=>x.Name==column.Source); var rc=right.Columns.SingleOrDefault(x=>x.Name==column.Target);
                        if((lc?.Name,lc?.Type,lc?.Position,lc?.Nullable,lc?.Definition)!=(rc?.Name,rc?.Type,rc?.Position,rc?.Nullable,rc?.Definition)) schemaFindings.Add(NewFinding(name,column.Source??column.Target,"ColumnDefinition",1));
                    }
                    foreach(var check in schemaChecks.Where(c=>results.Single(r=>r.CheckId==c).Selected))
                    {
                        var compared=CatalogComparison.Compare(check,item,a.Complete&&b.Complete);catalogs[check].AddRange(compared.Findings);
                        if(compared.Reason!=Reason.None){schemaReasons[check]=compared.Reason;schemaIssues[check].Add(new(name,null,compared.Reason));}
                    }
                }
                Publish();
                if(!selected.Contains(Category.Data)) continue;
                if(!item.Complete)
                { dataReason=Reason.MappingIncomplete;foreach(var column in item.Columns.Where(c=>c.Source==null||c.Target==null))dataIssues.Add(new(name,column.Source??column.Target,dataReason));Publish();continue; }
                if(!left.SafeRead || !right.SafeRead) { dataReason=adapter.StartsWith("SqlServer",StringComparison.Ordinal) ? Reason.NotImplemented : Reason.RowFiltered; dataIssues.Add(new(name,null,dataReason)); Publish(); continue; }
                var rightIndexes=right.Columns.Select((column,index)=>(column.Name,index)).ToDictionary(c=>c.Name,c=>c.index,StringComparer.Ordinal);
                var order=item.Columns.Select(c=>rightIndexes[c.Target!]).ToArray();
                if(left.Columns.Any(x=>!x.SafeValue) || right.Columns.Any(x=>!x.SafeValue) || !left.Columns.Select(x=>x.Type).SequenceEqual(order.Select(i=>right.Columns[i].Type)))
                { dataReason=Reason.UnsupportedType; dataIssues.Add(new(name,null,dataReason)); Publish(); continue; }
                try
                {
                    using var lsort=new ProtectedSort(); using var rsort=new ProtectedSort();
                    long rows=0;
                    await foreach(var row in source.RowsAsync(left,token)) { lsort.Add(row); if(++rows%1000==0) progress?.Report(new(name,rows)); }
                    await foreach(var row in target.RowsAsync(right,token)) { rsort.Add(ExactRow.Reorder(row,order)); if(++rows%1000==0) progress?.Report(new(name,rows)); }
                    progress?.Report(new(name,rows));
                    using var li=lsort.Finish(token).GetEnumerator(); using var ri=rsort.Finish(token).GetEnumerator();
                    var hasL=li.MoveNext(); var hasR=ri.MoveNext(); long onlyL=0,onlyR=0;
                    while(hasL || hasR)
                    {
                        token.ThrowIfCancellationRequested();
                        var diff=!hasL ? 1 : !hasR ? -1 : ProtectedSort.ByteComparer.Instance.Compare(li.Current,ri.Current);
                        if(diff<=0) { if(diff<0) onlyL++; hasL=li.MoveNext(); }
                        if(diff>=0) { if(diff>0) onlyR++; hasR=ri.MoveNext(); }
                    }
                    if(onlyL>0) dataFindings.Add(NewFinding(name,null,"SourceOnlyRow",onlyL));
                    if(onlyR>0) dataFindings.Add(NewFinding(name,null,"TargetOnlyRow",onlyR));
                }
                catch(VerificationException e) { dataReason=e.Reason; dataIssues.Add(new(name,null,dataReason)); }
                Publish();
            }
            SetSchema();
            Set("DATA-01",dataFindings,dataReason!=Reason.None ? dataReason : !a.Complete || !b.Complete ? Reason.VisibilityUnknown : Reason.None);
            AttachIssues();
            progress?.Report(new("Finalize",0,results.ToArray()));
            void Publish()
            {
                SetSchema(Execution.Running);
                Set("DATA-01",dataFindings,dataReason,Execution.Running);
                AttachIssues();
                progress?.Report(new("Inspect",0,results.ToArray()));
            }
            void AttachIssues()
            { var index=results.FindIndex(x=>x.CheckId=="DATA-01"); if(results[index].Selected) results[index]=results[index] with { Issues=dataIssues.ToArray() }; }
            void SetSchema(Execution execution=Execution.Completed)
            {
                foreach(var check in schemaChecks)
                {
                    // An absent collector is not a running or completed inspection.
                    if(results.Single(r=>r.CheckId==check).Category is Category.Access or Category.Objects && !observedCatalogChecks.Contains(check)) continue;
                    var findings=check=="SCHEMA-01"?schemaFindings.Concat(catalogs[check]).ToList():catalogs[check];
                    Set(check,findings,schemaReasons[check],execution);var index=results.FindIndex(r=>r.CheckId==check);
                    if(results[index].Selected)results[index]=results[index] with { Issues=schemaIssues[check].ToArray() };
                }
            }
        }
        catch(Exception e) when(e is OperationCanceledException or VerificationException or IOException or UnauthorizedAccessException)
        {
            var reason=e is OperationCanceledException ? Reason.Cancelled : e is VerificationException ve ? ve.Reason : Reason.TemporaryStorage;
            for(var i=0;i<results.Count;i++) if(results[i].Selected && results[i].Execution!=Execution.Completed)
                results[i]=results[i] with { Execution=results[i].Execution==Execution.Pending?Execution.Pending:Execution.Interrupted,Verdict=results[i].Findings.Count>0 ? Verdict.Different : Verdict.Indeterminate,Reason=reason };
        }
        return new(mappings==null?1:2,id,start,DateTimeOffset.UtcNow,example,adapter,scope,results) { Mappings=appliedMappings };
        void Set(string check,List<Finding> findings,Reason reason,Execution execution=Execution.Completed)
        {
            var index=results.FindIndex(x=>x.CheckId==check);
            if(!results[index].Selected) return;
            results[index]=results[index] with { Execution=execution, Verdict=findings.Count>0 ? Verdict.Different : reason!=Reason.None || execution!=Execution.Completed ? Verdict.Indeterminate : Verdict.Equal, Reason=reason,Findings=findings.ToArray() };
        }
    }
    private static Finding NewFinding(string table,string? column,string kind,long count) => new(Guid.NewGuid().ToString("N"),table,column,kind,count);
}
