using Avalonia.Input;
using Verifier.Core;
using Verifier.Data;
namespace Verifier.Desktop;

public sealed partial class ReplicaWorkspace
{
    public bool Live{get;}
    public bool IsExample=>!Live||_liveExample;
    public RunReport? LiveReport{get;private set;}
    public ProgressInfo? LiveProgress{get;private set;}
    internal bool IsDisposed=>_disposed;
    public bool LiveBusy=>!IsExample&&(Connection.Busy||_trialSetup.Busy||_historyBusy||Screen=="running"&&LiveReport==null);
    public void CancelLive()=>_liveCancellation?.Cancel();
    private bool _liveExample,_writesStopped,_databaseScope=true;
    private string _sourcePassword="",_targetPassword="",_sourceCa="",_targetCa="";
    private ConnectedDatabasePair? _pair;
    private IReadOnlyDictionary<(string Category,bool DatabaseScope),(int Collectors,int Readable,int Tables,bool Full)>? _liveCapabilities;
    private bool LiveDatabaseScopeAvailable=>Connection.Dbms is "PostgreSQL" or "MySQL" or "MariaDB" or "SQL Server";
    private CancellationTokenSource? _liveCancellation;
    private Reason? _connectionFailure;
    private long _connectionRevision,_runRevision;
    private void CaptureLiveConnection(WebConnectionPage page)
    {(_sourcePassword,_targetPassword,_sourceCa,_targetCa,_writesStopped)=page.LiveCredentials;_savePasswords=page.SavePasswords;}
    private void InvalidateLiveConnection(RunReport? keepReport=null)
    {
        ++_connectionRevision;++_runRevision;_liveCancellation?.Cancel();Connection=Connection with{Connected=false};
        InvalidateTrial(true);
        ReleaseLiveReport(keepReport);LiveReport=null;LiveProgress=null;SampleRun=null;ResultPageIndex=0;ResultFilter="all";Stopped=false;Saved=false;Tables=[];Maps=new Dictionary<string,string>();_liveMappings.Clear();
        var old=_pair;_pair=null;_liveCapabilities=null;InvalidateScopePlan();if(old!=null)_=DisposePairQuietly(old);
    }
    private static async Task DisposePairQuietly(ConnectedDatabasePair pair){try{await pair.DisposeAsync();}catch{}}
    private void ReleaseLiveReport(RunReport? keep=null)
    {
        if(LiveReport==null||ReferenceEquals(LiveReport,keep))return;
        try{LiveReport.Dispose();}
        catch(Exception e)when(e is IOException or UnauthorizedAccessException or VerificationException)
        {ShowToast(English?"Temporary result cleanup failed. Inspect storage before another run.":"임시 결과 정리에 실패했습니다. 다음 검사 전에 저장 공간을 확인하세요.");}
    }
    private ConnectionSettings LiveSettings(WebConnectionEndpoint endpoint,bool source)
    {
        var kind=Connection.Dbms switch{"PostgreSQL"=>DbKind.PostgreSql,"SQL Server"=>DbKind.SqlServer,"MySQL"=>DbKind.MySql,"MariaDB"=>DbKind.MariaDb,_=>throw new VerificationException(Reason.NotImplemented)};
        if(!int.TryParse(endpoint.Port,out var port))throw new VerificationException(Reason.InvalidInput);
        var ca=source?_sourceCa:_targetCa;
        var certificate=string.IsNullOrWhiteSpace(ca)?null:ca;
        return new(kind,endpoint.Host,port,endpoint.Database,endpoint.Reader,source?_sourcePassword:_targetPassword,kind==DbKind.SqlServer?null:certificate,kind==DbKind.SqlServer?certificate:null);
    }
    private async Task<bool> DispatchLiveAsync(string action)
    {
        if(LiveBusy&&action!="cancel")return true;
        if(action is "start" or "restart")
        {
            InvalidateLiveConnection();_trialSetup.Reset();_liveExample=false;_databaseScope=true;Reset();
            Connection=new(English,"PostgreSQL",new("localhost","5432","",""),new("localhost","5432","",""),false,false,false,"");
            Tables=[];Maps=new Dictionary<string,string>();_sourcePassword=_targetPassword="";_writesStopped=false;_savePasswords=false;_localStorageStatus="";Navigate("connect");return true;
        }
        if(action=="sample"){InvalidateLiveConnection();Reset();_liveExample=true;return false;}
        if(IsExample)return false;
        if(action=="history-back"){Navigate("home");return true;}
        if(action is "history" or "history-refresh" or "history-next"||action.StartsWith("history-open:",StringComparison.Ordinal)||action.StartsWith("history-remove:",StringComparison.Ordinal)){ExternalActionRequested?.Invoke(this,action);return true;}
        if(action is "save-local" or "restore-local" or "forget-local"){await DispatchLocalStorageAsync(action);return true;}
        if(Screen=="connect"&&action is "connect" or "preflight")
        {
            if(Connection.Busy)return true;
            if(Connection.Connected&&_pair!=null){Navigate("capabilities");return true;}
            if(CurrentPage is not WebConnectionPage page)return true;
            CaptureLiveConnection(page);var revision=++_connectionRevision;
            _connectionFailure=null;
            _liveCancellation?.Dispose();_liveCancellation=new();ConnectedDatabasePair? opened=null;
            try
            {
                if(!_writesStopped)throw new VerificationException(Reason.InvalidInput);
                var source=LiveSettings(Connection.Source,true);var target=LiveSettings(Connection.Target,false);
                Connection=Connection with{Busy=true,Connected=false,Error=""};page.SetPresentation(Connection,_width);page.SetFooter(Footer);
                // Catalog observation includes synchronous protected-file work.
                // Keep it off the dispatcher so input/cancellation can proceed.
                var connectionToken=_liveCancellation.Token;
                var connectionMappings=_liveMappings.Values.ToArray();
                var prepared=await Task.Run(async()=>
                {
                    var pair=await ConnectedDatabasePair.OpenAsync(source,target,connectionToken);
                    try
                    {
                        var scopes=ScopeMatching.Resolve(pair.SourceInventory,pair.TargetInventory,connectionMappings,connectionToken);
                        var capabilities=ObserveCapabilities(pair,connectionToken);
                        connectionToken.ThrowIfCancellationRequested();return(Pair:pair,Scopes:scopes,Capabilities:capabilities);
                    }
                    catch{await DisposePairQuietly(pair);throw;}
                },connectionToken);
                opened=prepared.Pair;
                if(_disposed||revision!=_connectionRevision){await DisposePairQuietly(opened);return true;}
                if(_pair!=null)await DisposePairQuietly(_pair);_pair=opened;opened=null;_liveCapabilities=prepared.Capabilities;
                RefreshLiveScope(true,prepared.Scopes);
                Connection=Connection with{Busy=false,Connected=true,Error=""};Render(false);
                Footer.ActionButtons.FirstOrDefault()?.Focus(NavigationMethod.Pointer);
                Announce(English?"Both actual connections verified. Review visibility next.":"양쪽 실제 연결 확인 완료. 조회 범위를 확인하세요.");
            }
            catch(Exception e) when(e is VerificationException or OperationCanceledException or IOException or UnauthorizedAccessException or ArgumentException)
            {
                if(opened!=null)await DisposePairQuietly(opened);
                if(!_disposed&&revision==_connectionRevision){if(_pair!=null){await DisposePairQuietly(_pair);_pair=null;}_liveCapabilities=null;InvalidateScopePlan();_connectionFailure=LiveReason(e);Connection=Connection with{Busy=false,Connected=false,Error=LiveError(_connectionFailure.Value)};Render(false);}
            }
            return true;
        }
        if(action=="run"){await RunLiveAsync();return true;}
        if(action=="cancel"){_liveCancellation?.Cancel();return true;}
        if(action=="trial"&&_pair==null){ShowToast(English?"Verify source and target connections first.":"원본·대상 실제 연결부터 확인하세요.");return true;}
        if(action=="trial-prepare-actual"){await PrepareActualTrialAsync();return true;}
        if(action is "trial-check" or "trial-primary"){ConfirmActualTrial();return true;}
        if(action is "open" or "download"){ExternalActionRequested?.Invoke(this,action);return true;}
        return false;
    }
    private static Reason LiveReason(Exception error)=>error is VerificationException e?e.Reason:error is OperationCanceledException?Reason.Cancelled:Reason.InvalidInput;
    private string LiveError(Exception error)=>LiveError(LiveReason(error));
    private string LiveError(Reason reason)=>LiveError(reason,English);
    internal static string LiveError(Reason reason,bool english)
    {
        string T(string a,string b)=>english?b:a;
        return reason switch
        {
            Reason.SameDatabase=>T("원본과 대상이 동일한 실제 DB입니다.","Source and target identify the same actual database."),
            Reason.PermissionDenied=>T("접속 계정의 식별·조회 권한이 부족합니다. 권한을 확인하세요.","The account lacks identity or read permissions. Review access."),
            Reason.ConnectionLost=>T("접속·인증·인증서 검증에 실패했습니다. 주소·비밀번호·인증서를 확인하세요.","Connection, authentication or certificate verification failed. Check address, password and certificates."),
            Reason.NotImplemented=>T("이 DBMS의 실제 어댑터는 아직 미구현입니다.","The actual adapter for this DBMS is not implemented yet."),
            Reason.PlatformNotVerified=>T("이 버전 또는 OS의 실제 검사를 아직 지원하지 않습니다.","Actual checks for this version or OS are not supported yet."),
            Reason.Cancelled=>T("작업이 취소됐습니다. 다시 확인하세요.","The operation was cancelled. Check again."),
            Reason.Timeout=>T("제한 시간을 초과했습니다. 새로 확인하세요.","The operation timed out. Check again."),
            _=>T("입력과 양쪽 DB 쓰기 정지 확인을 점검하세요.","Check the inputs and confirm writes to both databases are stopped.")
        };
    }
    private (int Collectors,int Readable,int Tables,bool Full) LiveCapability(string key)
    {
        if(_pair==null)return(0,0,0,false);
        return _liveCapabilities?.GetValueOrDefault((key,_databaseScope&&LiveDatabaseScopeAvailable))??(0,0,0,false);
    }
    private static IReadOnlyDictionary<(string Category,bool DatabaseScope),(int Collectors,int Readable,int Tables,bool Full)> ObserveCapabilities(ConnectedDatabasePair pair,CancellationToken token)
    {
        var result=new Dictionary<(string,bool),(int,int,int,bool)>();
        var tableCount=checked(pair.SourceInventory.Tables.Count+pair.TargetInventory.Tables.Count);
        var global=pair.SourceInventory.DatabaseCatalog.Concat(pair.TargetInventory.DatabaseCatalog);
        var categories=new[]{"data","schema","access","objects"};
        var definitions=categories.ToDictionary(k=>k,k=>Checks.Definitions.Where(c=>c.Category==CategoryFor(k)).ToArray());
        var observed=new HashSet<string>(StringComparer.Ordinal);var readable=categories.ToDictionary(k=>k,_=>0);
        foreach(var table in pair.SourceInventory.Tables.Concat(pair.TargetInventory.Tables))
        {
            token.ThrowIfCancellationRequested();
            foreach(var section in table.Catalog)if(Checks.Definitions.Any(c=>c.Id==section.CheckId))observed.Add(section.CheckId);
            foreach(var key in categories)
            {
                var safe=key=="data"?table.SafeRead&&table.Columns.All(c=>{token.ThrowIfCancellationRequested();return c.SafeValue;})
                    :definitions[key].All(c=>table.Catalog.Any(s=>s.CheckId==c.Id&&s.Reason==Reason.None));
                if(safe)readable[key]++;
            }
        }
        foreach(var key in categories)
        {
            token.ThrowIfCancellationRequested();var checks=definitions[key];
            var collectors=checks.Count(c=>c.Id=="DATA-01"?pair.Source is IStreamingReadDatabase&&pair.Target is IStreamingReadDatabase:c.Id=="DATA-02"?pair.Source is IIntegrityDatabase&&pair.Target is IIntegrityDatabase:observed.Contains(c.Id)||global.Any(s=>s.CheckId==c.Id));
            var globalComplete=checks.Where(c=>CatalogComparison.DatabaseChecks.Contains(c.Id)).All(c=>new[]{pair.SourceInventory,pair.TargetInventory}.All(i=>i.DatabaseCatalog.Any(s=>s.CheckId==c.Id&&s.Reason==Reason.None)));
            var full=collectors==checks.Length&&(tableCount>0||key=="objects")&&readable[key]==tableCount&&pair.SourceInventory.Complete&&pair.TargetInventory.Complete;
            result[(key,false)]=(collectors,readable[key],tableCount,full);
            result[(key,true)]=(collectors,readable[key],tableCount,full&&globalComplete);
        }
        return result;
    }
    private (string Text,string Kind) LiveCapabilityLabel(string key)
    {
        if(key=="trial")return(Trial.Ready?(English?"Ready · Separate DB":"준비됨 · 별도 DB"):(English?"Separate DB setup required":"별도 DB 준비 필요"),Trial.Ready?"ok":"warn");
        var c=LiveCapability(key);
        return(c.Full?(English?"Available":"가능"):c.Collectors==0?(English?"Not implemented":"미구현"):(English?"Partial":"일부 가능"),c.Full?"ok":"warn");
    }
    private void ApplyLiveCapabilities(WebPreflightPage page)
    {
        if(_pair==null)return;
        foreach(var row in page.Table.Rows)
        {
            row.ConnectedPill.Text=English?"Connected":"연결 확인";
            var (collectors,readable,tables,full)=LiveCapability(row.Key);var label=LiveCapabilityLabel(row.Key);
            row.CapabilityPill.Text=label.Text;row.CapabilityPill.Kind=label.Kind;
            var count=Checks.Definitions.Count(c=>c.Category==CategoryFor(row.Key));
            row.DetailText.Text=(English?"Collectors: ":"수집 검사: ")+collectors+" / "+count+(English?" · readable tables: ":" · 조회 가능 테이블: ")+readable+" / "+tables;
            row.DescriptionText.Text=_pair.SourceInventory.Complete&&_pair.TargetInventory.Complete?(full?row.DescriptionText.Text:(English?"Types, privileges or detailed collectors limit comparison.":"자료형·권한 또는 세부 수집 구현에 제한이 있습니다.")):(English?"Inventory visibility unconfirmed; no absence claims.":"목록 가시성 미확인 · 객체 없음으로 판단하지 않습니다.");
            if(Connection.Dbms=="SQL Server"&&row.Key=="data")
                row.DescriptionText.Text+=" · "+(new[]{_pair.SourceInventory,_pair.TargetInventory}.Any(i=>i.Observation.Contains("SERIALIZABLE",StringComparison.Ordinal))
                    ?(English?"SNAPSHOT unavailable on at least one side; locking reads. Keep writes stopped.":"한쪽 이상에서 SNAPSHOT 미사용 · 잠금 방식 조회. 쓰기 정지를 유지하세요.")
                    :(English?"Independent SNAPSHOT reads. Keep writes stopped.":"독립 SNAPSHOT 조회. 쓰기 정지를 유지하세요."));
            row.InvalidateMeasure();
        }
        page.Table.InvalidateMeasure();
    }
    private static Category CategoryFor(string key)=>key switch{"data"=>Category.Data,"schema"=>Category.Schema,"access"=>Category.Access,"objects"=>Category.Objects,_=>Category.Behavior};
    private async Task RunLiveAsync()
    {
        if(!Stopped||_pair==null||!Connection.Connected){ShowToast(English?"Verify actual connections and confirm the scope first.":"실제 연결과 실행 범위를 먼저 확인하세요.");return;}
        // Settings are copied before running. A full new session is required after this run.
        var pair=_pair;var includeDatabaseScope=_databaseScope&&LiveDatabaseScopeAvailable;var selected=Categories.Select(CategoryFor).ToHashSet();
        var trialPreparation=_confirmedTrialPreparation;var trialPlan=_confirmedTrial;var roleTrials=_confirmedRoleTrials.ToArray();
        if(selected.Contains(Category.Behavior)&&(!Trial.Ready||trialPreparation==null||trialPlan==null))
        {Stopped=false;ShowToast(English?"Confirm the actual separate-DB trial plan first.":"실제 별도 DB 시험안을 먼저 확정하세요.");Navigate("trial");return;}
        var adapter=Connection.Dbms switch{"PostgreSQL"=>"PostgreSql-v3","MySQL"=>"MySql-v1","MariaDB"=>"MariaDb-v1",_=>"SqlServer-v1"};
        IReadOnlyList<MatchedScope> plan;
        try{plan=LiveScopes();}
        catch(VerificationException e){Stopped=false;ShowToast(LiveError(e));Navigate("select");return;}
        var scope=CaptureLiveScope(plan,Tables);
        // Native inventories are immutable for this connection. Capture only
        // manual choices; the engine resolves automatic names from that pair.
        var mappings=_liveMappings.Values.Select(m=>m with{Columns=m.Columns.ToArray()}).ToArray();
        var runRevision=++_runRevision;_pair=null;InvalidateScopePlan();_liveCancellation?.Dispose();_liveCancellation=new();var token=_liveCancellation.Token;
        var initial=Checks.Initial(selected);SampleRun=ProjectLiveRun(initial,Guid.NewGuid(),false,false);ReleaseLiveReport();LiveReport=null;LiveProgress=new("Inspect",0,initial);Navigate("running");
        var feedback=new Progress<ProgressInfo>(p=>UpdateLiveProgress(p,runRevision));
        try
        {
            var completed=await Task.Run(()=>new ComparisonEngine().RunAsync(pair.Source,pair.Target,selected,scope,adapter,false,feedback,token,mappings,includeDatabaseScope));
            if(selected.Contains(Category.Behavior))completed=await RunConfirmedTrialsAsync(completed,trialPreparation!,trialPlan!,roleTrials,feedback,token);
            if(_disposed||runRevision!=_runRevision){completed.Dispose();return;}LiveReport=completed;
            RetainLiveScopeLabels(completed);
            SampleRun=ProjectLiveRun(LiveReport.Checks,LiveReport.RunId,LiveReport.Checks.Any(c=>c.Reason==Reason.Cancelled),true,LiveReport.BehaviorResults);Render(false);
        }
        finally{await DisposePairQuietly(pair);Connection=Connection with{Connected=false};_visited.Remove("capabilities");_visited.Remove("select");_visited.Remove("review");}
    }
    private static IReadOnlyCollection<string>? CaptureLiveScope(IReadOnlyList<MatchedScope> plan,IReadOnlyList<string> tables)
        =>tables is WebScopeKeys keys&&keys.Covers(plan)?keys.AllSelected?null:keys:plan.Where(s=>tables.Contains(s.Key)).Select(s=>s.Key).ToHashSet(StringComparer.Ordinal);
    private void UpdateLiveProgress(ProgressInfo p,long runRevision)
    {
        if(_disposed||runRevision!=_runRevision||LiveReport!=null)return;
        LiveProgress=p;
        if(p.Checks!=null)SampleRun=ProjectLiveRun(p.Checks,Guid.Parse(SampleRun!.Id),false,false);
        if(CurrentPage is WebProgressPage page)
        {
            if(p.Checks!=null)page.Update(SampleRun!);
            ApplyLiveProgress(page);ApplyLiveProgressFooter();
        }
    }
    private ReplicaSampleRun ProjectLiveRun(IReadOnlyList<CheckResult> checks,Guid id,bool interrupted,bool complete,IReadOnlyList<BehaviorResult>? trials=null)
    {
        var groups=Categories.Select(key=>
        {
            var c=checks.Where(c=>c.Selected&&c.Category==CategoryFor(key)).ToArray();
            return new ReplicaSampleGroup(key,c.Length,c.Count(x=>x.Execution==Execution.Completed),c.Count(x=>x.Execution==Execution.Completed&&x.Verdict==Verdict.Equal&&x.Reason==Reason.None&&x.Issues.Count==0),c.Count(x=>x.Verdict==Verdict.Different),c.Count(x=>x.Execution!=Execution.Completed||x.Verdict is Verdict.Indeterminate or Verdict.Unverified||x.Reason!=Reason.None||x.Issues.Count>0||x.Category==Category.Behavior&&trials?.Any(r=>(r.RoleOperation==null)==(x.CheckId=="BEHAVIOR-01")&&(r.Cleanup!=TrialCleanup.Complete||r.TargetStructure!=Verdict.Equal||r.Effects.Any(e=>e is TrialEffect.SequenceStateUnverified or TrialEffect.TriggerEffectsUnverified or TrialEffect.ExternalEffectsUnverified)))==true),c.Any(x=>x.Execution==Execution.Interrupted)?"interrupted":c.Any(x=>x.Execution==Execution.Running)?"running":complete?"completed":"pending");
        }).ToArray();
        // Settings replace these immutable views; progress must not repeatedly
        // clone all names, and a later edit cannot mutate this run snapshot.
        return new(id.ToString(),groups,interrupted,complete,Categories.Contains("trial"),Tables,Maps);
    }
    private void ApplyLiveProgress(WebProgressPage page)
    {
        Avalonia.Automation.AutomationProperties.SetName(page.Meter,English?"Completed actual checks":"완료한 실제 검사");page.Heading.SetPresentation(LiveReport==null?(English?"Comparing actual databases":"실제 DB를 비교합니다"):(English?"Checks finished · review outcomes":"검사가 끝났습니다 · 판정을 확인하세요"),English?"Counts show completed checks; unverified scopes remain explicit.":"완료한 검사 수를 표시합니다. 미검증 범위를 숨기지 않습니다.");
        var text=Categories.Contains("trial")?(English?"Source/target read-only · confirmed trial writes in separate DB":"원본·대상 읽기 전용 · 별도 DB에 확인한 시험 쓰기"):(English?"Read-only actual comparison":"읽기 전용 실제 비교");
        if(LiveReport==null&&LiveProgress is{} progress)
        {
            var stage=progress.Stage switch{"Inspect"=>English?"Inspecting metadata":"메타데이터 확인","ObserveCatalogs"=>English?"Checking catalog stability":"객체 관측 상태 확인","Finalize"=>English?"Finalizing results":"결과 정리",_=>progress.Stage};
            text+=" · "+stage;
            if(progress.Rows>0)text+=" · "+progress.Rows.ToString("N0",System.Globalization.CultureInfo.InvariantCulture)+(English?" rows read":"행 읽음");
        }
        page.Note.SetPresentation(text,LiveReport==null,ReducedMotion);
    }
    private void ApplyLiveProgressFooter()=>Footer.SetContent(Categories.Contains("trial")?(English?"Source/target are read-only. Trial writes use the confirmed separate DB.":"원본·대상은 읽기 전용이며 시험 쓰기는 확인한 별도 DB에만 합니다."):(English?"Actual read-only comparison. Completed and unverified checks remain separate.":"실제 읽기 전용 비교입니다. 완료와 미검증을 구분합니다."),null,[LiveReport!=null?new("show-results",English?"View results":"결과 보기","primary","arrow"):new WebFooterAction("cancel",English?"Cancel checks":"검사 취소","danger","stop")],ReducedMotion);
    private string LiveFindingName(string kind)=>kind switch
    {
        "TrialExpectation"=>English?"Confirmed expectation not met":"확인한 기대 결과 불충족",
        "ColumnValue"=>English?"Value differs in a matched row":"같은 키의 행에서 컬럼 값 불일치",
        "SourceOnlyProjection"=>English?"Source-only values in compared columns":"비교한 컬럼 조합이 원본에만 있음",
        "TargetOnlyProjection"=>English?"Target-only values in compared columns":"비교한 컬럼 조합이 대상에만 있음",
        _=>CatalogComparison.Fields.TryGetValue(kind,out var field)?(English?field.English:field.Korean):kind
    };
    private string LiveScopeReason(Reason reason)=>reason switch
    {
        Reason.None=>English?"No observation limit recorded.":"기록된 관측 제한이 없습니다.",
        Reason.NotImplemented=>English?"Some requested checks are not implemented. Uninspected parts are not certified equal.":"일부 요청 항목의 검사가 미구현입니다. 검사하지 못한 부분은 일치를 보장하지 않습니다.",
        Reason.UnsafeObservation=>English?"No safe observation method was established for this execution or value check. Observable definitions and differences are retained.":"안전한 관측 방법을 확인하지 못해 해당 실행·값 검사는 확인 불가로 남깁니다. 관측 가능한 정의와 차이는 유지합니다.",
        Reason.Unpopulated=>English?"Materialized view is unpopulated. Stored rows cannot be read; no refresh was performed.":"구체화된 뷰가 미적재 상태라 저장 행을 읽을 수 없습니다. 새로 고침은 실행하지 않았습니다.",
        Reason.UnsupportedType=>English?"This type or definition cannot be inspected safely with the supported codec.":"지원하는 방식으로 이 자료형·정의를 안전하게 검사하지 못했습니다.",
        Reason.MissingDependency=>English?"A referenced object definition is missing. This scope cannot be certified equal.":"참조된 객체 정의가 없습니다. 이 범위의 일치를 확정할 수 없습니다.",
        Reason.VisibilityUnknown=>English?"The complete object inventory could not be verified.":"전체 객체 목록을 확인하지 못했습니다.",
        Reason.PermissionDenied=>English?"The connected account lacks permission to inspect this scope.":"접속 계정의 조회 권한이 부족합니다.",
        Reason.RowFiltered=>English?"Row filtering prevents a complete observation.":"행 제한으로 전체 내용을 확인하지 못했습니다.",
        Reason.Cancelled=>English?"The check was cancelled.":"검사를 취소했습니다.",
        Reason.Timeout=>English?"The check timed out.":"검사 응답 시간이 초과됐습니다.",
        Reason.RowTooLarge=>English?"An item exceeded this path's processing limit. The affected scope remains unverified; it was not truncated.":"항목이 해당 경로의 처리 한계를 초과했습니다. 내용을 잘라 비교하지 않고 영향 범위를 확인 불가로 남깁니다.",
        Reason.ConnectionLost=>English?"The database connection was lost.":"DB 연결이 끊겼습니다.",
        Reason.TemporaryIntegrity=>English?"Temporary data integrity or cleanup ownership could not be verified.":"임시 자료의 무결성 또는 정리 대상의 소유를 확인하지 못했습니다.",
        Reason.TemporaryStorage=>English?"Temporary storage, access or cleanup failed.":"임시 저장·접근 또는 정리에 실패했습니다.",
        Reason.ObservationChanged=>English?"The observed database conditions changed; run a full check again.":"확인한 DB 조건이 바뀌었습니다. 전체 검사를 다시 실행하세요.",
        Reason.AmbiguousRows=>English?"Duplicate or NULL keys prevent row pairing. All compared values were compared as multisets.":"키 중복·NULL로 행 대응을 확정하지 않았습니다. 비교 컬럼 전체는 다중집합으로 비교했습니다.",
        Reason.MappingIncomplete=>English?"Some tables or columns have no confirmed counterpart.":"일부 테이블·컬럼의 대응이 확정되지 않았습니다.",
        Reason.AmbiguousScope=>English?"Object or column identities are ambiguous.":"객체·컬럼의 식별이 모호합니다.",
        Reason.NoObjects=>English?"No applicable objects were found in the observed scope.":"확인한 범위에 검사할 객체가 없습니다.",
        Reason.PlatformNotVerified=>English?"This database version or platform has not been verified.":"이 DB 버전·플랫폼은 검증되지 않았습니다.",
        _=>reason.ToString()
    };
    private string LiveLocation(string? table)=>table??(English?"Database scope":"DB 범위");
    private IReadOnlyList<WebSafeFinding> LiveFindings()
    {
        if(LiveReport==null)return [];
        var rows=new WebResultItems<WebSafeFinding>();
        foreach(var c in LiveReport.Checks.Where(c=>c.Selected))
        {
            var key=ReplicaSampleRun.CategoryKeys.First(k=>CategoryFor(k)==c.Category);
            rows.Add(c.Findings.Count,i=>{var f=ReadLiveFinding(c,i);return f==null?new("U-STORAGE-"+c.CheckId+"-"+i,key,"unknown",English?"Stored result unavailable":"보관한 결과를 읽을 수 없습니다","—","—",LiveScopeReason(Reason.TemporaryIntegrity)):new(f.Id,key,"different",c.CheckId+" · "+(f.Kind.StartsWith("Source",StringComparison.Ordinal)&&c.CheckId=="DATA-02"?(English?"Source defect · ":"원본에 존재하는 결함 · "):f.Kind.StartsWith("Target",StringComparison.Ordinal)&&c.CheckId=="DATA-02"?(English?"Target violation · ":"대상 제약 위반 · "):"")+LiveFindingName(f.Kind),LiveLocation(f.Table),f.Column??"—",c.ResearchId+" · "+f.Count);},"different");
            if(c.Reason!=Reason.None||c.Execution!=Execution.Completed)
                rows.Add(1,_=>new("U-"+c.CheckId,key,"unknown",c.CheckId+" · "+c.Execution,"—","—",c.ResearchId+" · "+LiveScopeReason(c.Reason)),"unknown");
            rows.Add(c.Issues.Count,i=>{var issue=ReadLiveIssue(c,i);if(issue==null)return new("U-STORAGE-ISSUE-"+c.CheckId+"-"+i,key,"unknown",English?"Stored limit unavailable":"보관한 사유를 읽을 수 없습니다","—","—",LiveScopeReason(Reason.TemporaryIntegrity));return new("U-"+c.CheckId+"-"+i,key,"unknown",c.CheckId+(issue.Attribute==null?"":" · "+LiveFindingName(issue.Attribute)),LiveLocation(issue.Table),issue.Column??"—",issue.Attribute=="UninspectedForeignOptions"?(English?"Foreign connection options may contain credentials. Their values were not collected or certified equal.":"외부 연결 옵션에 인증 정보가 포함될 수 있어 값을 수집하지 않았습니다. 해당 옵션은 미검증입니다."):issue.Attribute=="UninspectedConfiguration"?(English?"Additional settings may contain secrets. Their values were not collected or certified equal.":"추가 설정에 비밀값이 포함될 수 있어 값을 수집하지 않았습니다. 해당 설정은 미검증입니다."):LiveScopeReason(issue.Reason));},"unknown");
            rows.Add(c.PairingLimits.Count,i=>{var limit=ReadLiveIssue(c,i,true);if(limit==null)return new("U-STORAGE-LIMIT-"+c.CheckId+"-"+i,key,"unknown",English?"Stored limit unavailable":"보관한 사유를 읽을 수 없습니다","—","—",LiveScopeReason(Reason.TemporaryIntegrity));return new("L-"+c.CheckId+"-"+i,key,"note",English?"Row pairing limit · values compared":"행 대응 제한 · 값 비교 완료",LiveLocation(limit.Table),"—",LiveScopeReason(limit.Reason));},"note");
        }
        foreach(var request in LiveReport.BehaviorRequests)
        {
            var result=LiveReport.BehaviorResults.SingleOrDefault(r=>r.Id==request.Id);
            if(result==null){rows.Add(1,_=>new("T-"+request.Id,"trial","unknown",English?"Trial not executed":"시험 미실행",request.Table,"—",request.RoleOperation?.ToString()??"CRUD"),"unknown");continue;}
            rows.Add(1,_=>new("C-"+result.Id,"trial",result.Cleanup==TrialCleanup.Complete?"note":"unknown",English?"Test-data cleanup":"시험 자료 정리",result.Table,"—",result.Cleanup.ToString()),result.Cleanup==TrialCleanup.Complete?"note":"unknown");
            rows.Add(1,_=>new("E-"+result.Id,"trial",result.TargetStructure==Verdict.Equal?"note":"unknown",English?"Target vs trial environment":"대상과 시험 환경",result.Table,"—",result.TargetStructure.ToString()),result.TargetStructure==Verdict.Equal?"note":"unknown");
            foreach(var sequence in result.SequenceObservations)rows.Add(1,_=>new("S-"+result.Id+"-"+sequence.Table,"trial",sequence.Effect==TrialEffect.SequenceStateUnverified?"unknown":"note",English?"Linked sequence observation":"연결된 시퀀스 관측",sequence.Table,"—",sequence.Effect.ToString()),sequence.Effect==TrialEffect.SequenceStateUnverified?"unknown":"note");
            rows.Add(1,_=>new("X-"+result.Id,"trial","unknown",English?"Trigger/external effects unverified":"트리거·외부 부작용 미확인",result.Table,"—",English?"Full restoration is not guaranteed.":"완전 복구를 보장하지 않습니다."),"unknown");
        }
        return rows;
    }
    private IReadOnlyList<string> LiveDetailLines(string key)
    {
        var rows=new WebResultItems<string>();if(LiveReport is null)return rows;
        foreach(var c in LiveReport.Checks.Where(c=>c.Category==CategoryFor(key)))
        {
            rows.Add(1,_=>c.CheckId+" · "+c.ResearchId+" · "+c.Execution+" / "+c.Verdict+" / "+c.Reason);
            rows.Add(c.Findings.Count,i=>{var f=ReadLiveFinding(c,i);return f==null?(English?"Stored result unavailable · temporary integrity failure":"보관한 결과를 읽을 수 없습니다 · 임시 자료 무결성 실패"):LiveLocation(f.Table)+" · "+(f.Column??"—")+" · "+LiveFindingName(f.Kind)+" · "+f.Id;});
            rows.Add(c.Issues.Count,i=>{var item=ReadLiveIssue(c,i);return item==null?LiveScopeReason(Reason.TemporaryIntegrity):LiveLocation(item.Table)+" · "+(item.Column??"—")+" · "+(item.Attribute==null?"":LiveFindingName(item.Attribute)+" · ")+LiveScopeReason(item.Reason);});
            rows.Add(c.PairingLimits.Count,i=>{var item=ReadLiveIssue(c,i,true);return item==null?LiveScopeReason(Reason.TemporaryIntegrity):LiveLocation(item.Table)+" · "+LiveScopeReason(item.Reason);});
        }
        if(key=="trial")foreach(var line in LiveTrialDetailLines())rows.Add(1,_=>line);
        return rows;
    }
    private void ApplyLiveResults(WebResultsPage page)
    {
        if(LiveReport==null)return;
        page.Heading.SetPresentation(LiveReport.HasDifferences?(English?"Differences found":"불일치가 발견됐습니다"):LiveReport.HasIntegrityViolations?(English?"Constraint violations observed":"제약 위반이 확인됐습니다"):LiveReport.HasUnknown?(English?"Unverified scope remains":"확인하지 못한 범위가 있습니다"):(English?"Selected checks match":"선택한 검사가 일치합니다"),LiveReport.Example?(English?"Saved synthetic example. No actual database was checked.":"저장된 합성 예시입니다. 실제 DB 검사 결과가 아닙니다."):(English?"Actual database results. Detailed checks and observation limits are retained.":"실제 DB 검사 결과입니다. 세부 검사와 관측 제한을 함께 보존합니다."));
    }
    public void LoadLiveReport(RunReport report)
    {
        ObjectDisposedException.ThrowIf(_disposed,this);
        if(report.Example)throw new InvalidDataException();
        InvalidateLiveConnection(report);ResultPageIndex=0;ResultFilter="all";_liveExample=false;_databaseScope=report.DatabaseScope;LiveReport=report;
        Categories=report.Checks.Where(c=>c.Selected).Select(c=>ReplicaSampleRun.CategoryKeys.First(k=>CategoryFor(k)==c.Category)).Distinct().ToArray();
        var labels=SavedReportScope(report);Tables=labels.Tables;Maps=labels.Maps;
        SampleRun=ProjectLiveRun(report.Checks,report.RunId,report.Checks.Any(c=>c.Reason==Reason.Cancelled),true,report.BehaviorResults);Navigate("results");
    }
    private Finding? ReadLiveFinding(CheckResult check,int index)=>ReadLiveMetadata(check,()=>check.Findings[index]);
    private ScopeIssue? ReadLiveIssue(CheckResult check,int index,bool pairing=false)=>ReadLiveMetadata(check,()=>(pairing?check.PairingLimits:check.Issues)[index]);
    private T? ReadLiveMetadata<T>(CheckResult check,Func<T> read) where T:class
    {
        try{return read();}
        catch(Exception e)when(e is VerificationException or IOException or UnauthorizedAccessException or ObjectDisposedException)
        {
            if(LiveReport!=null&&LiveReport.Checks.Contains(check)&&check.Reason!=Reason.TemporaryIntegrity)
            {
                LiveReport=LiveReport with{Checks=LiveReport.Checks.Select(c=>ReferenceEquals(c,check)?c with{Reason=Reason.TemporaryIntegrity}:c).ToArray()};
                SampleRun=ProjectLiveRun(LiveReport.Checks,LiveReport.RunId,false,true,LiveReport.BehaviorResults);
                var run=LiveReport.RunId;
                Avalonia.Threading.Dispatcher.UIThread.Post(()=>{if(!_disposed&&LiveReport?.RunId==run)Render(false);});
            }
            return null;
        }
    }
}
