using Verifier.Core;
using Verifier.Data;
namespace Verifier.Desktop;

public sealed partial class ReplicaWorkspace
{
    private readonly LiveTrialSetup _trialSetup=new();
    private IPreparedTrial? _confirmedTrialPreparation;
    private ConfirmedTrialPlan? _confirmedTrial;
    private ConfirmedRoleTrial[] _confirmedRoleTrials=[];
    private long _trialRevision;
    private void InvalidateTrial(bool preparation)
    {
        ++_trialRevision;_confirmedTrial=null;_confirmedTrialPreparation=null;_confirmedRoleTrials=[];_trialSetup.Reviewed=false;
        if(preparation)_trialSetup.ClearPreparation();Trial=Trial with{Ready=false,Consent=false};Stopped=false;_visited.Remove("review");
    }
    private WebActualTrialPage CreateActualTrialPage()
    {
        if(_trialSetup.Database.Length==0&&_trialSetup.Table.Length==0&&_trialSetup.Port=="5432"&&Connection.Dbms is "MySQL" or "MariaDB")_trialSetup.Port="3306";
        if(_trialSetup.Database.Length==0&&_trialSetup.Table.Length==0&&_trialSetup.Port=="5432"&&Connection.Dbms=="SQL Server")_trialSetup.Port="1433";
        var page=new WebActualTrialPage(_trialSetup,English,ReducedMotion,_pair?.TargetInventory.Tables.Where(t=>!t.Catalog.SelectMany(s=>s.Facts).Any(f=>f.Attribute=="TableKind"&&f.Value=="\"m\"")).Select(t=>t.Qualified).ToArray()??[],Connection.Dbms switch{"MySQL"=>DbKind.MySql,"MariaDB"=>DbKind.MariaDb,"SQL Server"=>DbKind.SqlServer,_=>DbKind.PostgreSql});
        page.Edited+=(_,connection)=>
        {
            if(!ReferenceEquals(CurrentPage,page))return;
            var reviewed=_trialSetup.Reviewed;InvalidateTrial(connection);_trialSetup.Reviewed=reviewed;
            Trial=Trial with{Host=_trialSetup.Host,Database=_trialSetup.Database,Consent=reviewed};SetActualTrialFooter();
        };
        page.ActionRequested+=async(_,action)=>{if(ReferenceEquals(CurrentPage,page))await DispatchAsync(action);};SetActualTrialFooter();return page;
    }
    private void SetActualTrialFooter()=>Footer.SetContent(
        English?"Preparation only reads. Confirmed writes use only the separate test DB.":"준비는 읽기만 합니다. 확인한 시험 쓰기는 별도 DB에만 실행합니다.",
        new("trial-back",English?"Back to selection":"검사 선택으로","quiet","back"),
        [new("trial-primary",Trial.Ready?(English?"Ready · Return":"준비 확인 · 돌아가기"):(English?"Confirm these inputs and expectations":"이 입력과 기대 결과 확정"),"primary","check",!_trialSetup.Busy&&(Trial.Ready||_trialSetup.Prepared!=null&&_trialSetup.Reviewed))],ReducedMotion);
    private IReadOnlyList<string> ConfirmedTrialSummary()=>_confirmedTrialPreparation is not{} p||_confirmedTrial is not{} plan?[]:
        new[]{_trialSetup.Host+" · "+_trialSetup.Database+" · "+p.Table,(English?"Confirmed case: ":"확정 시험: ")+plan.Id,(English?"Role cases: ":"역할 시험 수: ")+_confirmedRoleTrials.Length}.Concat(p.Dependencies.Where(d=>plan.Dependencies.ContainsKey(d.Id)).Select(d=>(English?"Dependency row: ":"부모 자료: ")+d.Table)).ToArray();
    private async Task PrepareActualTrialAsync()
    {
        if(_pair==null||!Connection.Connected||_trialSetup.Busy)return;
        InvalidateTrial(true);var revision=_trialRevision;var pair=_pair;
        if(pair.TargetInventory.Tables.Any(t=>t.Qualified==_trialSetup.Table&&t.Catalog.SelectMany(s=>s.Facts).Any(f=>f.Attribute=="TableKind"&&f.Value=="\"m\"")))
        {
            _trialSetup.Error=(Reason.NotImplemented,true);
            Render(false);return;
        }
        try
        {
            var kind=Connection.Dbms switch{"PostgreSQL"=>DbKind.PostgreSql,"MySQL"=>DbKind.MySql,"MariaDB"=>DbKind.MariaDb,"SQL Server"=>DbKind.SqlServer,_=>throw new VerificationException(Reason.NotImplemented)};
            if(!int.TryParse(_trialSetup.Port,out var port))throw new VerificationException(Reason.InvalidInput);
            var certificate=string.IsNullOrWhiteSpace(_trialSetup.Ca)?null:_trialSetup.Ca;
            var settings=new ConnectionSettings(kind,_trialSetup.Host,port,_trialSetup.Database,_trialSetup.User,_trialSetup.Password,kind==DbKind.SqlServer?null:certificate,kind==DbKind.SqlServer?certificate:null);
            _trialSetup.Busy=true;_trialSetup.Error=null;Render(false);_liveCancellation?.Dispose();_liveCancellation=new();var token=_liveCancellation.Token;var table=_trialSetup.Table;
            IPreparedTrial prepared=await Task.Run(async()=>kind switch{
                DbKind.PostgreSql=>(IPreparedTrial)await PostgreSqlDatabase.PrepareTrialAsync(settings,pair.SourceInventory,pair.TargetInventory,table,token),
                DbKind.SqlServer=>await SqlServerDatabase.PrepareTrialAsync(settings,pair.SourceInventory,pair.TargetInventory,table,token),
                _=>await MySqlPreparedTrial.PrepareAsync(settings,pair.SourceInventory,pair.TargetInventory,table,token)});
            if(_disposed||revision!=_trialRevision)return;_trialSetup.SetPrepared(prepared);
            Trial=Trial with{Host=settings.Host,Database=settings.Database,Consent=false,Ready=false,Error=""};
        }
        catch(Exception e)when(e is VerificationException or OperationCanceledException or IOException or UnauthorizedAccessException or ArgumentException or Npgsql.NpgsqlException or MySqlConnector.MySqlException or Microsoft.Data.SqlClient.SqlException)
        {if(!_disposed&&revision==_trialRevision)_trialSetup.Error=(LiveReason(e),false);}
        finally{_trialSetup.Busy=false;if(!_disposed&&Screen=="trial")Render(false);}
    }
    private void ConfirmActualTrial()
    {
        if(Trial.Ready){Navigate("select");return;}
        if(_trialSetup.Prepared is not{}prepared||!_trialSetup.Reviewed||_pair==null)return;
        try
        {
            var dependencies=_trialSetup.IncludedDependencies.ToDictionary(id=>id,id=>(IReadOnlyDictionary<string,string?>)_trialSetup.Inserts[id]);
            var plan=new ConfirmedTrialPlan(prepared.Id,_trialSetup.Inserts[prepared.Id],_trialSetup.Update,
                _trialSetup.Reject?_trialSetup.Rejected:null,_trialSetup.Reject?_trialSetup.RejectionStage:null,_trialSetup.Reject&&prepared.Kind!=DbKind.SqlServer?_trialSetup.ExpectedSqlState:null,_trialSetup.Reject&&!string.IsNullOrWhiteSpace(_trialSetup.ExpectedConstraint)?_trialSetup.ExpectedConstraint:null,dependencies:dependencies,expectedNativeError:_trialSetup.Reject&&prepared.Kind!=DbKind.PostgreSql?int.TryParse(_trialSetup.ExpectedNativeError,out var nativeError)?nativeError:throw new VerificationException(Reason.InvalidInput):null);
            prepared.ValidateConfirmedPlan(plan);
            var roles=string.IsNullOrWhiteSpace(_trialSetup.Role)?[]:_trialSetup.RoleExpectations.Select(e=>
                new ConfirmedRoleTrial(new(prepared.Id,plan.Insert,plan.Update,role:_trialSetup.Role,dependencies:dependencies,roleHost:prepared.Kind==DbKind.MySql?_trialSetup.RoleHost:null),e.Key,e.Value==TrialExpectation.PermissionDenied,e.Value==TrialExpectation.NoRow)).ToArray();
            foreach(var role in roles)prepared.ValidateConfirmedPlan(role.Plan);
            _confirmedTrial=plan;_confirmedTrialPreparation=prepared;_confirmedRoleTrials=roles;
            Trial=Trial with{Ready=true,Consent=true,Host=_trialSetup.Host,Database=_trialSetup.Database,Error=""};_trialSetup.Error=null;Stopped=false;Navigate("select");
        }
        catch(VerificationException e){_trialSetup.Error=(e.Reason,false);Render(false);}
    }
    private async Task<RunReport> RunConfirmedTrialsAsync(RunReport report,IPreparedTrial prepared,ConfirmedTrialPlan plan,ConfirmedRoleTrial[] roles,IProgress<ProgressInfo> progress,CancellationToken token)
    {
        var requests=new[]{new TrialRequest(plan.Id,prepared.Table,null)}.Concat(roles.Select(r=>new TrialRequest(r.Id,prepared.Table,r.Operation))).ToArray();var results=new List<BehaviorResult>();
        var pending=BehaviorReporting.Attach(report,requests,results);progress.Report(new("trial",0,pending.Checks));
        results.Add(token.IsCancellationRequested?Cancelled(requests[0],plan):await prepared.ExecuteAsync(plan,token));pending=BehaviorReporting.Attach(report,requests,results);progress.Report(new("trial",0,pending.Checks));
        foreach(var role in roles)
        {
            results.Add(token.IsCancellationRequested?Cancelled(requests.Single(r=>r.Id==role.Id),role.Plan):await prepared.ExecuteRoleAsync(role,token));pending=BehaviorReporting.Attach(report,requests,results);progress.Report(new("trial",0,pending.Checks));
        }
        return BehaviorReporting.Attach(report,requests,results);
        BehaviorResult Cancelled(TrialRequest request,ConfirmedTrialPlan confirmed)=>BehaviorReporting.CancelledBeforeStart(request,prepared.TargetStructure,prepared.Dependencies.Where(d=>confirmed.Dependencies.ContainsKey(d.Id)).Select(d=>d.Table));
    }
    private IEnumerable<string> LiveTrialDetailLines()
    {
        if(LiveReport==null)yield break;
        foreach(var request in LiveReport.BehaviorRequests)
        {
            yield return request.Table+" · "+(request.RoleOperation?.ToString()??"CRUD")+" · "+request.Id;
            var result=LiveReport.BehaviorResults.SingleOrDefault(r=>r.Id==request.Id);
            if(result==null){yield return English?"No result · unverified":"실행 결과 없음 · 미검증";continue;}
            foreach(var step in result.Steps)yield return step.Stage+" · "+(English?"Expected: ":"기대: ")+step.Expected+" · "+step.Outcome+" / "+step.Reason;
            yield return (English?"Cleanup: ":"정리: ")+result.Cleanup+" · "+(English?"Target vs trial structure: ":"대상과 시험 구조: ")+result.TargetStructure;
            foreach(var table in result.DependencyTables)yield return (English?"Confirmed dependency scope: ":"확인한 부모 자료 범위: ")+table;
            foreach(var sequence in result.SequenceObservations)yield return sequence.Table+" · "+sequence.Effect;
            yield return English?"Trigger and external effects are unverified. Rollback is not a full-restore guarantee.":"트리거·외부 부작용은 미확인입니다. rollback이 완전 복구를 보장하지 않습니다.";
        }
    }
}
