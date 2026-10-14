using System.Data;
using MySqlConnector;
using Verifier.Core;
namespace Verifier.Data;

public sealed partial class MySqlPreparedTrial
{
    public async Task<BehaviorResult> ExecuteRoleAsync(ConfirmedRoleTrial confirmed,CancellationToken token)
    {
        var plan=confirmed.Plan;ValidateConfirmedPlan(plan);
        if(plan.Role==null||plan.RejectedInput!=null||confirmed.Operation is not(TrialStage.RoleInsert or TrialStage.RoleRead or TrialStage.RoleUpdate or TrialStage.RoleDelete)
            ||confirmed.ExpectNoRow&&(confirmed.ExpectPermissionDenied||confirmed.Operation==TrialStage.RoleInsert))throw new VerificationException(Reason.InvalidInput);
        var expected=confirmed.ExpectPermissionDenied?TrialExpectation.PermissionDenied:confirmed.ExpectNoRow?TrialExpectation.NoRow:TrialExpectation.Allowed;
        var steps=new List<TrialStep>();var stage=TrialStage.Insert;var cleanup=TrialCleanup.Unverified;var verified=false;
        await using var connection=NewConnection();MySqlTransaction? tx=null;
        try
        {
            await Open(connection,token);tx=await connection.BeginTransactionAsync(IsolationLevel.RepeatableRead,token);await Guard(connection,tx,plan,token);
            if(plan.Dependencies.Count>0){stage=TrialStage.DependencyInsert;await InsertParents(connection,tx,plan,token);steps.Add(new(stage,TrialOutcome.Met,Reason.None));}
            stage=TrialStage.Insert;
            if(confirmed.Operation!=TrialStage.RoleInsert)await Insert(connection,tx,plan.Insert,token);
            else if(await Read(connection,tx,plan.Insert,null,token))throw new VerificationException(Reason.AmbiguousRows);
            stage=TrialStage.VerifyRole;
            // SET ROLE replaces active roles, but native direct account grants remain.
            // Role and account host are separate literal identifiers, never split SQL.
            await using(var q=new MySqlCommand("SET ROLE "+MySqlTrialValue.Quote(plan.Role)+(Kind==DbKind.MySql?"@"+MySqlTrialValue.Quote(plan.RoleHost??"%") : ""),connection,tx))await q.ExecuteNonQueryAsync(token);
            if(Kind==DbKind.MySql){await using var q=new MySqlCommand("SET SESSION sql_quote_show_create=1",connection,tx);await q.ExecuteNonQueryAsync(token);}
            await using(var q=new MySqlCommand("SELECT CURRENT_ROLE()",connection,tx))
            {
                var active=await q.ExecuteScalarAsync(token) as string;
                var wanted=Kind==DbKind.MySql?MySqlTrialValue.Quote(plan.Role)+"@"+MySqlTrialValue.Quote(plan.RoleHost??"%"):plan.Role;
                if(active!=wanted)throw new VerificationException(Reason.VisibilityUnknown);
            }
            verified=true;steps.Add(new(stage,TrialOutcome.Met,Reason.None));stage=confirmed.Operation;var noRow=false;
            switch(stage)
            {
                case TrialStage.RoleInsert:await Insert(connection,tx,plan.Insert,token,checkVacant:false);break;
                case TrialStage.RoleRead:noRow=!await Read(connection,tx,plan.Insert,plan.Insert,token,locking:false);break;
                case TrialStage.RoleUpdate:noRow=await Update(connection,tx,plan.Insert,plan.Update,token)==0;break;
                case TrialStage.RoleDelete:noRow=await Delete(connection,tx,plan.Insert,token)==0;break;
            }
            steps.Add(new(stage,!confirmed.ExpectPermissionDenied&&noRow==confirmed.ExpectNoRow?TrialOutcome.Met:TrialOutcome.Unmet,Reason.None,expected));
        }
        catch(Exception e)when(e is MySqlException or VerificationException or OperationCanceledException or TrialMismatch or IOException or UnauthorizedAccessException)
        {
            var denied=e is MySqlException {Number:1142 or 1143 or 1227};
            var outcome=stage==confirmed.Operation&&denied?(confirmed.ExpectPermissionDenied?TrialOutcome.Met:TrialOutcome.Unmet):e is TrialMismatch?TrialOutcome.Unmet:TrialOutcome.Unverified;
            steps.Add(new(stage,outcome,outcome==TrialOutcome.Met?Reason.None:Error(e),expected));
        }
        finally{if(tx!=null)cleanup=await Rollback(tx);}
        return await Finish(confirmed.Id,plan,steps,cleanup,verified,confirmed.Operation);
    }
}
