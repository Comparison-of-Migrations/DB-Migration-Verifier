using Avalonia;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Verifier.Core;
namespace Verifier.Desktop;

public sealed partial class ReplicaWorkspace
{
    private readonly Dictionary<string,TableMapping> _liveMappings=new(StringComparer.Ordinal);
    private RunReportStorage? _reviewScopeOwner;
    private IReadOnlyList<MatchedScope>? _reviewScopes;
    private IReadOnlyList<string>? _reviewTables,_reviewLines;
    private IReadOnlyList<string> LiveReviewMappings()
    {
        var scopes=LiveScopes();
        if(ReferenceEquals(_reviewScopes,scopes)&&ReferenceEquals(_reviewTables,Tables)&&_reviewLines!=null)return _reviewLines;
        var owner=new RunReportStorage();IReadOnlyList<string> lines;
        try{lines=ReviewScopeLines(scopes,Tables,owner);}catch{owner.Dispose();throw;}
        ReleaseReviewScope();_reviewScopeOwner=owner;_reviewScopes=scopes;_reviewTables=Tables;_reviewLines=lines;return lines;
    }
    private static IReadOnlyList<string> ReviewScopeLines(IReadOnlyList<MatchedScope> scopes,IReadOnlyList<string> selected,RunReportStorage owner)
    {
        var keys=TableNameBuffer.SortedDistinct(selected.Where(key=>ScopeMatching.Find(scopes,key)!=null),owner);
        var lines=new WebResultItems<string>();
        lines.Add(keys.Count,i=>{var scope=ScopeMatching.Find(scopes,keys[i])??throw new VerificationException(Reason.ObservationChanged);return (scope.Source?.Qualified??"—")+" → "+(scope.Target?.Qualified??"—")+" · "+string.Join(" / ",scope.Columns.Select(c=>(c.Source??"—")+" → "+(c.Target??"—")));});
        return lines;
    }
    private void ReleaseReviewScope()
    {
        var owner=_reviewScopeOwner;_reviewScopeOwner=null;_reviewScopes=null;_reviewTables=null;_reviewLines=null;
        try{owner?.Dispose();}
        catch(Exception e)when(e is IOException or UnauthorizedAccessException or VerificationException)
        {ShowToast(English?"Temporary scope cleanup failed. Inspect storage before another run.":"임시 범위 정리에 실패했습니다. 다음 검사 전에 저장 공간을 확인하세요.");}
    }
    private Verifier.Data.ConnectedDatabasePair? _scopePair;
    private IReadOnlyList<MatchedScope>? _scopePlan;
    private WebMatchingDialog? _columnDialog;
    public WebMatchingDialog? ColumnDialog=>_columnDialog;
    private void InvalidateScopePlan()
    {_scopePair=null;_scopePlan=null;ReleaseReviewScope();}
    private IReadOnlyList<MatchedScope> LiveScopes()
    {
        if(_pair==null){InvalidateScopePlan();return [];}
        if(ReferenceEquals(_scopePair,_pair)&&_scopePlan!=null)return _scopePlan;
        var plan=ScopeMatching.Resolve(_pair.SourceInventory,_pair.TargetInventory,_liveMappings.Values.ToArray());
        _scopePlan=plan;_scopePair=_pair;return plan;
    }
    private void RefreshLiveScope(bool initial=false,IReadOnlyList<MatchedScope>? prepared=null)
    {
        InvalidateScopePlan();
        if(prepared!=null&&_pair!=null){_scopePlan=prepared;_scopePair=_pair;}
        var scopes=LiveScopes();var wasAll=initial||Tables.Count==Maps.Count;
        var previous=Maps;var selected=wasAll||Tables is WebScopeKeys?null:Tables.ToHashSet(StringComparer.Ordinal);
        Maps=new ScopeMap(scopes);
        if(wasAll)Tables=new WebScopeKeys(scopes);
        else if(Tables is WebScopeKeys keys)Tables=keys.Remap(scopes,previous);
        else Tables=scopes.Where(s=>selected!.Contains(s.Key)||!previous.ContainsKey(s.Key)).Select(s=>s.Key).ToArray();
        Stopped=false;Saved=false;_visited.Remove("review");
        InvalidateTrial(false);
    }
    private (string Source,string Target,string Method,bool Editable) LiveMappingPresentation(string key)
    {
        var scope=ScopeMatching.Find(LiveScopes(),key)??throw new KeyNotFoundException();
        var method=scope.Source==null?T("대상에만 관측","Target only"):scope.Target==null?T("대응 없음","Unmatched"):
            _liveMappings.ContainsKey(scope.Source.Qualified)?T("수동 대응","Manual matching"):T("동일 이름","Same name");
        // relkind 'm' is PostgreSQL-only. Other adapters' protected catalog
        // payloads must not be scanned on every row/language rerender for it.
        if(Connection.Dbms=="PostgreSQL"&&new[]{scope.Source,scope.Target}.OfType<Table>().Any(t=>t.Catalog.SelectMany(s=>s.Facts).Any(f=>f.Attribute=="TableKind"&&f.Value=="\"m\"")))method+=" · "+T("구체화된 뷰 · 저장 데이터","Materialized view · stored data");
        return(scope.Source?.Qualified??"—",scope.Target?.Qualified??"—",method,scope.Source!=null);
    }
    // Connection labels borrow their observer. Completion switches to the
    // report-owned names before that observer is released.
    private sealed class ScopeMap(IReadOnlyList<MatchedScope> scopes,bool sourceOnly=false):IReadOnlyDictionary<string,string>
    {
        private IEnumerable<MatchedScope> Entries=>sourceOnly?scopes.Where(s=>s.Source!=null):scopes;
        public int Count=>sourceOnly?Entries.Count():scopes.Count;
        public IEnumerable<string> Keys=>Entries.Select(s=>sourceOnly?s.Source!.Qualified:s.Key);
        public IEnumerable<string> Values=>Entries.Select(s=>s.Target?.Qualified??"");
        public string this[string key]=>TryGetValue(key,out var value)?value:throw new KeyNotFoundException();
        public bool ContainsKey(string key)=>ScopeMatching.Find(scopes,sourceOnly?"source:"+key:key)!=null;
        public bool TryGetValue(string key,out string value)
        {var scope=ScopeMatching.Find(scopes,sourceOnly?"source:"+key:key);value=scope?.Target?.Qualified??"";return scope!=null;}
        public IEnumerator<KeyValuePair<string,string>> GetEnumerator()=>Entries.Select(s=>new KeyValuePair<string,string>(sourceOnly?s.Source!.Qualified:s.Key,s.Target?.Qualified??"")).GetEnumerator();
        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()=>GetEnumerator();
    }
    private void RetainLiveScopeLabels(RunReport report)
    {
        var captured=new CapturedScopeMap(report.Mappings);Maps=captured;
        if(captured.Count==0){Tables=report.Tables;return;}
        var keys=new WebResultItems<string>();keys.Add(captured.Count,captured.KeyAt);Tables=keys;
    }
    private sealed class CapturedScopeMap(IReadOnlyList<TableMapping> mappings):IReadOnlyDictionary<string,string>
    {
        public int Count=>mappings.Count;
        private static string Key(TableMapping mapping)=>(mapping.Source!=null?"source:":"target:")+(mapping.Source??mapping.Target??throw new VerificationException(Reason.TemporaryIntegrity));
        internal string KeyAt(int index)=>Key(mappings[index]);
        public IEnumerable<string> Keys=>mappings.Select(Key);
        public IEnumerable<string> Values=>mappings.Select(m=>m.Target??"");
        public string this[string key]=>TryGetValue(key,out var value)?value:throw new KeyNotFoundException();
        public bool ContainsKey(string key)=>TryGetValue(key,out _);
        public bool TryGetValue(string key,out string value)
        {
            var low=0;var high=Count-1;
            while(low<=high)
            {
                var middle=low+(high-low)/2;var mapping=mappings[middle];var order=StringComparer.Ordinal.Compare(Key(mapping),key);
                if(order==0){value=mapping.Target??"";return true;}
                if(order<0)low=middle+1;else high=middle-1;
            }
            value="";return false;
        }
        public IEnumerator<KeyValuePair<string,string>> GetEnumerator()=>mappings.Select(m=>new KeyValuePair<string,string>(Key(m),m.Target??"")).GetEnumerator();
        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()=>GetEnumerator();
    }
    private static (IReadOnlyList<string> Tables,IReadOnlyDictionary<string,string> Maps) SavedReportScope(RunReport report)
        =>(report.Tables is IFrozenMetadata<string>?report.Tables:report.Tables.ToArray(),
            report.Mappings is IFrozenMetadata<TableMapping>?new SavedScopeMap(report.Mappings)
                :report.Mappings.Where(m=>m.Source!=null).ToDictionary(m=>m.Source!,m=>m.Target??"",StringComparer.Ordinal));
    // ponytail: valid saved mappings may have arbitrary order. Rare source-name
    // lookups scan; add a protected ordinal index if reopened selection needs them often.
    private sealed class SavedScopeMap(IReadOnlyList<TableMapping> mappings):IReadOnlyDictionary<string,string>
    {
        private readonly int count=mappings.Count(m=>m.Source!=null);
        private IEnumerable<TableMapping> Entries=>mappings.Where(m=>m.Source!=null);
        public int Count=>count;
        public IEnumerable<string> Keys=>Entries.Select(m=>m.Source!);
        public IEnumerable<string> Values=>Entries.Select(m=>m.Target??"");
        public string this[string key]=>TryGetValue(key,out var value)?value:throw new KeyNotFoundException();
        public bool ContainsKey(string key)=>TryGetValue(key,out _);
        public bool TryGetValue(string key,out string value)
        {
            ArgumentNullException.ThrowIfNull(key);
            foreach(var mapping in mappings)if(mapping.Source==key){value=mapping.Target??"";return true;}
            value="";return false;
        }
        public IEnumerator<KeyValuePair<string,string>> GetEnumerator()=>Entries.Select(m=>new KeyValuePair<string,string>(m.Source!,m.Target??"")).GetEnumerator();
        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()=>GetEnumerator();
    }
    private void ShowLiveMatching(string key,Control? opener)
    {
        if(_disposed||_pair==null||LiveBusy)return;
        if(ScopeMatching.Find(LiveScopes(),key) is not{}scope||scope.Source is not{}source)return;
        if(_columnDialog!=null){Children.Remove(_columnDialog);_columnDialog=null;}
        if(_matchingDialog!=null)Children.Remove(_matchingDialog);
        var dialog=new WebMatchingDialog{ReducedMotion=ReducedMotion};_matchingDialog=dialog;_matchingSource=key;Children.Add(dialog);
        var maps=new ScopeMap(LiveScopes(),true);var targets=_pair.TargetInventory.Tables;
        var targetNames=new WebResultItems<string>();targetNames.Add(1,_=>"");targetNames.Add(targets.Count,i=>targets[i].Qualified);
        var draftTarget=scope.Target?.Qualified??"";
        var draftTable=scope.Target;
        var draftColumns=_liveMappings.TryGetValue(source.Qualified,out var manual)?manual.Columns.ToArray():Array.Empty<ColumnMapping>();
        var columnPage=0;var targetColumnPage=0;IReadOnlyList<ColumnMapping> resolvedColumns=[];
        void Columns(int? pageIndex=null,int? targetPageIndex=null)
        {
            try{BuildColumns(pageIndex,targetPageIndex);}
            catch(Exception e)when(e is VerificationException or IOException or UnauthorizedAccessException or ObjectDisposedException){dialog.MarkUnavailable();}
        }
        void BuildColumns(int? pageIndex,int? targetPageIndex)
        {
            var target=draftTable;
            var forward=pageIndex.HasValue&&pageIndex.Value>columnPage;var targetForward=targetPageIndex.HasValue&&targetPageIndex.Value>targetColumnPage;
            if(pageIndex.HasValue)columnPage=pageIndex.Value;
            if(targetPageIndex.HasValue)targetColumnPage=targetPageIndex.Value;
            if(!pageIndex.HasValue&&!targetPageIndex.HasValue)resolvedColumns=ScopeMatching.ResolveObservedColumns(source,target,draftColumns);
            var columns=resolvedColumns;var sourceCount=source.Columns.Count;
            columnPage=Math.Clamp(columnPage,0,WebResultPager.LastPage(sourceCount));
            var list=new StackPanel{Spacing=8};
            list.Children.Add(WebTypography.CreateCandidate(T("컬럼 대응 · 적용 전까지 임시 설정","Column matching · Draft until applied"),15,24,weight:700));
            for(var index=columnPage*WebResultPager.PageSize;index<Math.Min(sourceCount,(columnPage+1)*WebResultPager.PageSize);index++)
            {
                var column=columns[index];
                var button=new WebActionButton{Variant="base",ReducedMotion=ReducedMotion,Content=column.Source+" → "+(column.Target??T("대응 없음","Unmatched")),HorizontalAlignment=Avalonia.Layout.HorizontalAlignment.Stretch};
                AutomationProperties.SetAutomationId(button,"Column-mapping-"+column.Source);
                button.IsEnabled=target!=null;button.Click+=(_,_)=>
                {
                    if(target==null||_columnDialog!=null)return;
                    var child=new WebMatchingDialog{ColumnContext=true,ReducedMotion=ReducedMotion};_columnDialog=child;Children.Add(child);
                    child.Applied+=(_,chosen)=>{draftColumns=draftColumns.Where(c=>c.Source!=column.Source).Append(new(column.Source,chosen.Length==0?null:chosen)).ToArray();Columns();};
                    child.Closed+=(_,_)=>{if(!ReferenceEquals(_columnDialog,child))return;Children.Remove(child);_columnDialog=null;InvalidateMeasure();Dispatcher.UIThread.Post(()=>dialog.ExtraContent?.GetVisualDescendants().OfType<WebActionButton>().FirstOrDefault(b=>AutomationProperties.GetAutomationId(b)=="Column-mapping-"+column.Source)?.Focus());};
                    var names=new WebResultItems<string>();names.Add(1,_=>"");names.Add(target.Columns.Count,i=>target.Columns[i].Name);
                    try{child.Open(column.Source!,new ColumnMap(columns,sourceCount),names,English,button,dialog);InvalidateMeasure();}
                    catch(Exception e)when(e is VerificationException or IOException or UnauthorizedAccessException or ObjectDisposedException)
                    {Children.Remove(child);_columnDialog=null;dialog.MarkUnavailable();}
                };
                list.Children.Add(button);
            }
            WebResultPager? sourcePager=null,targetPager=null;
            if(sourceCount>WebResultPager.PageSize)
                list.Children.Add(sourcePager=new WebResultPager(sourceCount,columnPage,English,ReducedMotion,"Column-mappings-page",page=>Columns(page)));
            var extraCount=columns.Count-sourceCount;
            targetColumnPage=Math.Clamp(targetColumnPage,0,WebResultPager.LastPage(extraCount));
            if(extraCount>0)list.Children.Add(WebTypography.CreateCandidate(T("대응되지 않은 대상 컬럼: ","Unmatched target columns: ")+string.Join(" · ",Enumerable.Range(targetColumnPage*WebResultPager.PageSize,Math.Min(WebResultPager.PageSize,extraCount-targetColumnPage*WebResultPager.PageSize)).Select(i=>columns[sourceCount+i].Target)),12,19.2));
            if(extraCount>WebResultPager.PageSize)
                list.Children.Add(targetPager=new WebResultPager(extraCount,targetColumnPage,English,ReducedMotion,"Column-unmatched-page",page=>Columns(null,page)));
            dialog.ExtraContent=list;InvalidateMeasure();
            if(pageIndex.HasValue||targetPageIndex.HasValue)Dispatcher.UIThread.Post(()=>
            {
                if(!ReferenceEquals(_matchingDialog,dialog)||dialog.IsClosing)return;
                var pager=pageIndex.HasValue?sourcePager:targetPager;if(pager==null)return;
                if(pageIndex.HasValue&&columnPage!=pageIndex.Value||targetPageIndex.HasValue&&targetColumnPage!=targetPageIndex.Value)return;
                var button=(pageIndex.HasValue?forward:targetForward)?pager.NextButton:pager.PreviousButton;
                if(!button.IsEnabled)button=pager.NextButton.IsEnabled?pager.NextButton:pager.PreviousButton;
                button.Focus();
            },DispatcherPriority.Loaded);
        }
        dialog.DraftChanged+=(_,_)=>
        {
            if(draftTarget==dialog.DraftTarget)return;
            try{draftTarget=dialog.DraftTarget;draftTable=draftTarget.Length==0?null:targets.FirstOrDefault(t=>t.Qualified==draftTarget);draftColumns=[];columnPage=targetColumnPage=0;Columns();}
            catch(Exception e)when(e is VerificationException or IOException or UnauthorizedAccessException or ObjectDisposedException){dialog.MarkUnavailable();}
        };
        dialog.Applied+=(_,target)=>
        {
            if(_pair==null||!ReferenceEquals(_matchingDialog,dialog))return;
            var candidate=new TableMapping(source.Qualified,target.Length==0?null:target,draftColumns);
            ScopeMatching.Resolve(_pair.SourceInventory,_pair.TargetInventory,_liveMappings.Values.Where(m=>m.Source!=source.Qualified).Append(candidate).ToArray());
            _liveMappings[source.Qualified]=candidate;RefreshLiveScope();Render(false);
            ShowToast(T("테이블·컬럼 대응을 적용했습니다. 값과 구조 차이는 그대로 검사합니다.","Table and column matching applied. Values and schema differences remain checked."));
        };
        dialog.Closed+=(_,_)=>
        {
            if(!ReferenceEquals(_matchingDialog,dialog))return;
            if(_columnDialog!=null){Children.Remove(_columnDialog);_columnDialog=null;}
            Children.Remove(dialog);_matchingDialog=null;_matchingSource=null;InvalidateMeasure();
            if(CurrentPage is WebSelectionPage page)Dispatcher.UIThread.Post(()=>page.Rows.FirstOrDefault(r=>r.ScopeKey==key)?.ChangeButton.Focus(NavigationMethod.Unspecified));
        };
        dialog.Open(source.Qualified,maps,targetNames,English,opener,_background);Columns();InvalidateMeasure();
    }
    // A column chooser borrows the immutable resolved draft; opening/searching
    // it must not copy every column into another dictionary/name array.
    private sealed class ColumnMap(IReadOnlyList<ColumnMapping> columns,int sourceCount):IReadOnlyDictionary<string,string>
    {
        private IEnumerable<ColumnMapping> Entries=>columns.Take(sourceCount);
        public int Count=>sourceCount;
        public IEnumerable<string> Keys=>Entries.Select(c=>c.Source!);
        public IEnumerable<string> Values=>Entries.Select(c=>c.Target??"");
        public string this[string key]=>TryGetValue(key,out var value)?value:throw new KeyNotFoundException();
        public bool ContainsKey(string key)=>Entries.Any(c=>c.Source==key);
        public bool TryGetValue(string key,out string value){var entry=Entries.FirstOrDefault(c=>c.Source==key);value=entry?.Target??"";return entry!=null;}
        public IEnumerator<KeyValuePair<string,string>> GetEnumerator()=>Entries.Select(c=>new KeyValuePair<string,string>(c.Source!,c.Target??"")).GetEnumerator();
        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()=>GetEnumerator();
    }
}
