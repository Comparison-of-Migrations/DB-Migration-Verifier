namespace Verifier.Core;

public static class BehaviorReporting
{
    public static BehaviorResult CancelledBeforeStart(TrialRequest request,Verdict targetStructure,IEnumerable<string> dependencies)
    {
        var parents=dependencies.Distinct(StringComparer.Ordinal).ToArray();
        return new(request.Id,request.Table,[new(TrialStage.NotStarted,TrialOutcome.Unverified,Reason.Cancelled)],TrialCleanup.Complete,
            [TrialEffect.SequenceStateUnverified,TrialEffect.TriggerEffectsUnverified,TrialEffect.ExternalEffectsUnverified],targetStructure,false)
            {RoleOperation=request.RoleOperation,DependencyTables=parents,SequenceObservations=parents.Append(request.Table).Select(t=>new TrialSequenceObservation(t,TrialEffect.SequenceStateUnverified)).ToArray()};
    }
    public static RunReport Attach(RunReport report,IReadOnlyList<TrialRequest> requested,IReadOnlyList<BehaviorResult> results)
    {
        if(report.Example||!report.Checks.Any(c=>c.Category==Category.Behavior&&c.Selected))throw new InvalidDataException();
        var snapshot=requested.ToArray();var observations=results.ToArray();
        ValidateScope(snapshot,observations);
        return report with{Version=3,Finished=DateTimeOffset.UtcNow,BehaviorRequests=snapshot,BehaviorResults=observations,
            Checks=report.Checks.Select(c=>c.Category==Category.Behavior?Summarize(c,snapshot,observations):c).ToArray()};
    }
    internal static void ValidateScope(IReadOnlyList<TrialRequest> requested,IReadOnlyList<BehaviorResult> results)
    {
        static bool Role(TrialStage? stage)=>stage==null||stage is TrialStage.RoleInsert or TrialStage.RoleRead or TrialStage.RoleUpdate or TrialStage.RoleDelete;
        if(requested==null||results==null||requested.Any(r=>r==null||r.Id==Guid.Empty||string.IsNullOrWhiteSpace(r.Table)||!Role(r.RoleOperation))
            ||requested.Select(r=>r.Id).Distinct().Count()!=requested.Count||results.Any(r=>r==null)||results.Select(r=>r.Id).Distinct().Count()!=results.Count)throw new InvalidDataException();
        foreach(var result in results)
        {
            var request=requested.SingleOrDefault(r=>r.Id==result.Id);
            if(request==null||result.Table!=request.Table||result.RoleOperation!=request.RoleOperation||!Enum.IsDefined(result.Cleanup)||!Enum.IsDefined(result.TargetStructure)
                ||result.TargetStructure is not(Verdict.Equal or Verdict.Different or Verdict.Indeterminate)||result.Steps==null||result.Steps.Count==0||result.Effects==null
                ||result.DependencyTables==null||result.SequenceObservations==null||result.DependencyTables.Any(string.IsNullOrWhiteSpace)||result.DependencyTables.Distinct(StringComparer.Ordinal).Count()!=result.DependencyTables.Count
                ||result.DependencyTables.Contains(result.Table,StringComparer.Ordinal)||result.Steps.Any(s=>s==null||!Enum.IsDefined(s.Stage)||!Enum.IsDefined(s.Outcome)||!Enum.IsDefined(s.Reason)||!Enum.IsDefined(s.Expected)||s.Outcome==TrialOutcome.Met&&s.Reason!=Reason.None||s.Outcome==TrialOutcome.Unverified&&s.Reason==Reason.None)
                ||result.Effects.Any(e=>!Enum.IsDefined(e))||result.Effects.Distinct().Count()!=result.Effects.Count)throw new InvalidDataException();
            var scope=result.DependencyTables.Append(result.Table).ToHashSet(StringComparer.Ordinal);
            if(result.SequenceObservations.Count!=scope.Count||result.SequenceObservations.Any(o=>o==null||!scope.Contains(o.Table)||o.Effect is not(TrialEffect.SequenceStateChanged or TrialEffect.SequenceStateUnchanged or TrialEffect.SequenceStateUnverified))
                ||result.SequenceObservations.Select(o=>o.Table).Distinct(StringComparer.Ordinal).Count()!=scope.Count
                ||!result.SequenceObservations.Select(o=>o.Effect).Distinct().Order().SequenceEqual(result.Effects.Where(e=>e is TrialEffect.SequenceStateChanged or TrialEffect.SequenceStateUnchanged or TrialEffect.SequenceStateUnverified).Order())
                ||!result.Effects.Contains(TrialEffect.TriggerEffectsUnverified)||!result.Effects.Contains(TrialEffect.ExternalEffectsUnverified))throw new InvalidDataException();
            var stages=result.Steps.Select(s=>s.Stage).ToArray();
            if(request.RoleOperation!=null)
            {
                if(result.RoleTest&&!stages.Contains(TrialStage.VerifyRole)||result.Steps.Any(s=>s.Stage==request.RoleOperation&&!result.RoleTest)
                    ||result.Steps.All(s=>s.Outcome==TrialOutcome.Met)&&(!result.RoleTest||stages[^1]!=request.RoleOperation))throw new InvalidDataException();
            }
            else if(result.Steps.All(s=>s.Outcome==TrialOutcome.Met))
            {
                var required=new[]{TrialStage.Insert,TrialStage.ReadInserted,TrialStage.Update,TrialStage.ReadUpdated,TrialStage.Delete,TrialStage.VerifyCleanup,TrialStage.RollbackCleanup};
                var observed=stages.Where(s=>required.Contains(s)).ToArray();if(!required.SequenceEqual(observed))throw new InvalidDataException();
            }
        }
    }
    internal static CheckResult Summarize(CheckResult check,IReadOnlyList<TrialRequest> requested,IReadOnlyList<BehaviorResult> results)
    {
        var cases=requested.Where(r=>(r.RoleOperation==null)==(check.CheckId=="BEHAVIOR-01")).ToArray();
        if(cases.Length==0)return check with{Execution=Execution.Pending,Verdict=Verdict.Unverified,Reason=Reason.NotImplemented,Findings=[],Issues=[],PairingLimits=[]};
        var ids=cases.Select(r=>r.Id).ToHashSet();var actual=results.Where(r=>ids.Contains(r.Id)).ToArray();var missing=actual.Length!=cases.Length;
        var interrupted=actual.Any(r=>r.Steps.Any(s=>s.Reason==Reason.Cancelled));
        var unknown=missing||actual.Any(r=>r.Steps.Any(s=>s.Outcome==TrialOutcome.Unverified));
        var findings=actual.Where(r=>r.Steps.Any(s=>s.Outcome==TrialOutcome.Unmet)).Select(r=>new Finding(r.Id.ToString("N"),r.Table,null,"TrialExpectation",r.Steps.LongCount(s=>s.Outcome==TrialOutcome.Unmet))).ToArray();
        return check with{Execution=interrupted?Execution.Interrupted:Execution.Completed,Verdict=findings.Length>0?Verdict.Different:unknown?Verdict.Indeterminate:Verdict.Equal,
            Reason=interrupted?Reason.Cancelled:unknown?actual.SelectMany(r=>r.Steps).FirstOrDefault(s=>s.Outcome==TrialOutcome.Unverified)?.Reason??Reason.ObservationChanged:Reason.None,
            Findings=findings,Issues=[],PairingLimits=[]};
    }
}
