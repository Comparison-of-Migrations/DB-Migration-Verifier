using Verifier.Core;
using Verifier.Data;
namespace Verifier.Desktop;

// Mutable editor state is private to this workspace. Confirmed plans are deep copies.
internal sealed class LiveTrialSetup
{
    public string Host="localhost",Port="5432",Database="",User="",Password="",Ca="",Table="",Role="";
    public string RoleHost="%";
    public (Reason Reason,bool MaterializedView)? Error;
    public string ErrorText(bool english)=>Error switch
    {
        null=>"",
        (_,true)=>english?"Materialized views support stored-data comparison, not insert/update/delete trials. Select a writable table.":"구체화된 뷰는 저장 데이터 비교 대상입니다. 입력·수정·삭제 시험에는 쓰기 가능한 테이블을 선택하세요.",
        (Reason.SameDatabase,_)=>english?"This is the source or target DB. Test writes are blocked.":"원본 또는 대상과 같은 실제 DB입니다. 시험 쓰기를 차단했습니다.",
        {} error=>ReplicaWorkspace.LiveError(error.Reason,english)
    };
    public bool Busy,Reviewed,Reject;
    public TrialStage RejectionStage=TrialStage.RejectInsert;
    public string ExpectedSqlState="23502",ExpectedConstraint="",ExpectedNativeError="";
    public IPreparedTrial? Prepared;
    public Dictionary<Guid,Dictionary<string,string?>> Inserts=[];
    public Dictionary<string,string?> Update=[],Rejected=[];
    public HashSet<Guid> IncludedDependencies=[];
    public Dictionary<TrialStage,TrialExpectation> RoleExpectations=new(){[TrialStage.RoleInsert]=TrialExpectation.Allowed,[TrialStage.RoleRead]=TrialExpectation.Allowed,[TrialStage.RoleUpdate]=TrialExpectation.Allowed,[TrialStage.RoleDelete]=TrialExpectation.Allowed};
    public void SetPrepared(IPreparedTrial prepared)
    {
        Prepared=prepared;Reviewed=false;Error=null;
        Inserts=prepared.Dependencies.Append(prepared).ToDictionary(p=>p.Id,p=>p.Draft.Insert.ToDictionary(v=>v.Key,v=>v.Value,StringComparer.Ordinal));
        IncludedDependencies=prepared.Dependencies.Select(p=>p.Id).ToHashSet();Update=prepared.Draft.Update.ToDictionary(v=>v.Key,v=>v.Value,StringComparer.Ordinal);
        ExpectedConstraint="";Rejected=new(Inserts[prepared.Id],StringComparer.Ordinal);var required=prepared.Columns.FirstOrDefault(c=>!c.Nullable&&!c.Generated&&!c.HasDefault&&Rejected.ContainsKey(c.Name));
        Reject=required!=null;RejectionStage=TrialStage.RejectInsert;ExpectedSqlState=prepared.Kind==DbKind.PostgreSql?"23502":prepared.Kind==DbKind.SqlServer?"":"23000";ExpectedNativeError=prepared.Kind==DbKind.PostgreSql?"":prepared.Kind==DbKind.SqlServer?"515":"1048";if(required!=null)Rejected[required.Name]=null;
    }
    public void Reset(){ClearPreparation();RoleHost="%";Host="localhost";Port="5432";Database=User=Password=Ca=Table=Role=ExpectedConstraint=ExpectedNativeError="";Error=null;Reject=false;ExpectedSqlState="23502";foreach(var key in RoleExpectations.Keys.ToArray())RoleExpectations[key]=TrialExpectation.Allowed;}
    public void ClearPreparation(){Prepared=null;Inserts.Clear();Update.Clear();Rejected.Clear();IncludedDependencies.Clear();Reviewed=false;}
    public override string ToString()=>"LiveTrialSetup (inputs and credentials redacted)";
}
