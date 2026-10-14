using System.Collections.Immutable;
namespace Verifier.Core;

public enum TrialStage { Insert, ReadInserted, Update, ReadUpdated, Delete, VerifyCleanup, RollbackCleanup, Commit, RejectInsert, RejectUpdate, RejectCommit, VerifyRole, RoleInsert, RoleRead, RoleUpdate, RoleDelete, DependencyInsert, DependencyRead, DependencyDelete, NotStarted }
public enum TrialOutcome { Met, Unmet, Unverified }
public enum TrialCleanup { Complete, Failed, Unverified }
public enum TrialExpectation { Allowed, ConstraintRejection, PermissionDenied, NoRow }
public enum TrialEffect { SequenceStateChanged, SequenceStateUnchanged, SequenceStateUnverified, TriggerEffectsUnverified, ExternalEffectsUnverified }
public sealed record TrialStep(TrialStage Stage,TrialOutcome Outcome,Reason Reason,TrialExpectation Expected=TrialExpectation.Allowed);
// Only fixed outcomes, a table location and random IDs survive execution. Inputs/receipts never do.
public sealed record TrialSequenceObservation(string Table,TrialEffect Effect);
public sealed record TrialRequest(Guid Id,string Table,TrialStage? RoleOperation);
public sealed record BehaviorResult(Guid Id,string Table,IReadOnlyList<TrialStep> Steps,TrialCleanup Cleanup,IReadOnlyList<TrialEffect> Effects,Verdict TargetStructure,bool RoleTest)
{
    public IReadOnlyList<string> DependencyTables {get;init;}=[];
    public IReadOnlyList<TrialSequenceObservation> SequenceObservations {get;init;}=[];
    public TrialStage? RoleOperation {get;init;}
}
public sealed record TrialGeneratedReference(Guid DependencyId,string Table,string Column,bool Generated=true);
public sealed record TrialColumn(string Name,string Type,bool Nullable,bool Generated,bool HasDefault,string? SuggestedValue,bool RequiresInput)
{public string? InputFormat{get;init;}public TrialGeneratedReference? GeneratedReference{get;init;}}

// Proposals stay in memory until the user confirms them; they are not run evidence.
public sealed record TrialInputIssue(string? Column,string Kind,Reason Reason);
public sealed class TrialDraft
{
    public ImmutableDictionary<string,string?> Insert { get; }
    public ImmutableDictionary<string,string?> Update { get; }
    public ImmutableArray<TrialInputIssue> NeedsInput { get; }
    public TrialDraft(IReadOnlyDictionary<string,string?> insert,IReadOnlyDictionary<string,string?> update,IEnumerable<TrialInputIssue> needsInput)
    {Insert=insert.ToImmutableDictionary(StringComparer.Ordinal);Update=update.ToImmutableDictionary(StringComparer.Ordinal);NeedsInput=needsInput.Distinct().ToImmutableArray();}
    public override string ToString()=>"TrialDraft (synthetic values redacted; confirmation required)";
}

// An immutable, explicitly confirmed plan. The data adapter creates it after checking the inputs.
public sealed class ConfirmedTrialPlan
{
    public Guid Id {get;}=Guid.NewGuid();
    public Guid PreparationId { get; }
    public ImmutableDictionary<string,string?> Insert { get; }
    public ImmutableDictionary<string,string?> Update { get; }
    public ImmutableDictionary<string,string?>? RejectedInput { get; }
    public TrialStage? RejectedStage { get; }
    public string? ExpectedSqlState { get; }
    public string? ExpectedConstraint { get; }
    public int? ExpectedNativeError { get; }
    public string? Role { get; }
    public string? RoleHost { get; }
    public ImmutableDictionary<Guid,ImmutableDictionary<string,string?>> Dependencies { get; }
    public ConfirmedTrialPlan(Guid preparationId,IReadOnlyDictionary<string,string?> insert,IReadOnlyDictionary<string,string?> update,
        IReadOnlyDictionary<string,string?>? rejectedInput=null,TrialStage? rejectedStage=null,string? expectedSqlState=null,string? expectedConstraint=null,string? role=null,
        IReadOnlyDictionary<Guid,IReadOnlyDictionary<string,string?>>? dependencies=null,int? expectedNativeError=null,string? roleHost=null)
    {
        PreparationId=preparationId;Insert=insert.ToImmutableDictionary(StringComparer.Ordinal);Update=update.ToImmutableDictionary(StringComparer.Ordinal);
        RejectedInput=rejectedInput?.ToImmutableDictionary(StringComparer.Ordinal);RejectedStage=rejectedStage;ExpectedSqlState=expectedSqlState;ExpectedConstraint=expectedConstraint;ExpectedNativeError=expectedNativeError;Role=role;RoleHost=roleHost;
        Dependencies=(dependencies??new Dictionary<Guid,IReadOnlyDictionary<string,string?>>()).ToImmutableDictionary(p=>p.Key,p=>p.Value.ToImmutableDictionary(StringComparer.Ordinal));
    }
    public override string ToString()=>"ConfirmedTrialPlan (values and role redacted)";
}

// Tests the selected role's actual guarded operation, not its password/login.
public sealed record ConfirmedRoleTrial(ConfirmedTrialPlan Plan,TrialStage Operation,bool ExpectPermissionDenied,bool ExpectNoRow=false)
{
    public Guid Id {get;}=Guid.NewGuid();
}
