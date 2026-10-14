using Verifier.Core;
namespace Verifier.Data;

// Shared UI contract; native preparation/execution remains in each DBMS adapter.
// Confirmed plans own their inputs. No credentials or receipts enter this interface's report surface.
public interface IPreparedTrial
{
    Guid Id {get;}
    DbKind Kind {get;}
    string Table {get;}
    Verdict TargetStructure {get;}
    IReadOnlyList<TrialColumn> Columns {get;}
    TrialDraft Draft {get;}
    IReadOnlyList<IPreparedTrial> Dependencies {get;}
    void ValidateConfirmedPlan(ConfirmedTrialPlan plan);
    Task<BehaviorResult> ExecuteAsync(ConfirmedTrialPlan plan,CancellationToken token);
    Task<BehaviorResult> ExecuteRoleAsync(ConfirmedRoleTrial plan,CancellationToken token);
}
