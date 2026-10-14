using System.Data;
using Npgsql;
using Verifier.Core;
namespace Verifier.Data;

public sealed partial class PostgreSqlDatabase
{
    public sealed partial class PreparedTrial
    {
        public async Task<BehaviorResult> ExecuteRoleAsync(ConfirmedRoleTrial confirmed,CancellationToken token)
        {
            var plan=confirmed.Plan;Validate(plan);
            if(plan.Role==null||plan.RejectedInput!=null||confirmed.Operation is not(TrialStage.RoleInsert or TrialStage.RoleRead or TrialStage.RoleUpdate or TrialStage.RoleDelete)
                ||confirmed.ExpectNoRow&&(confirmed.ExpectPermissionDenied||confirmed.Operation==TrialStage.RoleInsert))throw new VerificationException(Reason.InvalidInput);
            var expected=confirmed.ExpectPermissionDenied?TrialExpectation.PermissionDenied:confirmed.ExpectNoRow?TrialExpectation.NoRow:TrialExpectation.Allowed;
            var steps=new List<TrialStep>();var stage=TrialStage.Insert;var verified=false;var cleanup=TrialCleanup.Unverified;
            var effects=new List<TrialEffect>{TrialEffect.SequenceStateUnverified,TrialEffect.TriggerEffectsUnverified,TrialEffect.ExternalEffectsUnverified};
            await using var connection=new NpgsqlConnection(settings.PostgreSqlString());NpgsqlTransaction? tx=null;
            try
            {
                await connection.OpenAsync(token);await VerifyIdentity(connection,token);tx=await connection.BeginTransactionAsync(IsolationLevel.RepeatableRead,token);await Configure(connection,tx,token);await LockRelation(connection,tx,token);
                await using var observer=new PostgreSqlDatabase(settings);var inventory=await observer.InspectAsync(token);
                var actual=inventory.Tables.SingleOrDefault(t=>t.Qualified==Table);
                if(inventory.Identity!=trialIdentity||actual==null||!SameDefinition(table,actual))throw new VerificationException(Reason.ObservationChanged);
                if(plan.Dependencies.Count>0){stage=TrialStage.DependencyInsert;await InsertDependencies(connection,tx,plan,token);steps.Add(new(stage,TrialOutcome.Met,Reason.None));stage=TrialStage.Insert;}
                // The login creates the baseline. The selected role can touch only this
                // exact app-created receipt; no broad key/range-based UPDATE or DELETE.
                Receipt? receipt=null;if(confirmed.Operation!=TrialStage.RoleInsert)receipt=await Insert(connection,tx,plan.Insert,token);
                stage=TrialStage.VerifyRole;await SetRole(connection,tx,plan.Role,token);verified=true;steps.Add(new(stage,TrialOutcome.Met,Reason.None));
                stage=confirmed.Operation;var noRow=false;
                switch(stage)
                {
                    case TrialStage.RoleInsert:
                        // Avoid requiring SELECT/RETURNING for an INSERT-only role.
                        // This transaction is always rolled back, never committed.
                        await using(var command=new NpgsqlCommand("",connection,tx))
                        {
                            var items=BindValues(command,plan.Insert);
                            command.CommandText="INSERT INTO "+Qualified(table)+(items.Count==0?" DEFAULT VALUES":" ("+string.Join(',',items.Select(i=>Quote(i.Name)))+") OVERRIDING SYSTEM VALUE VALUES ("+string.Join(',',items.Select(i=>i.Expression))+")");
                            if(await command.ExecuteNonQueryAsync(token)!=1)throw new VerificationException(Reason.ObservationChanged);
                        }
                        break;
                    case TrialStage.RoleRead:
                        noRow=!await Exists(connection,tx,receipt!,token);
                        if(!noRow&&!await observer.TrialValuesEqual(connection,tx,actual,receipt!,plan.Insert,casts,token))throw new TrialMismatch();break;
                    case TrialStage.RoleUpdate:
                        noRow=await UpdateOptional(connection,tx,receipt!,plan.Update,token)==null;break;
                    case TrialStage.RoleDelete:
                        await using(var command=new NpgsqlCommand("DELETE FROM "+Qualified(table)+" WHERE "+Owned+Returning,connection,tx))
                        {BindReceipt(command,receipt!);noRow=await ReturnedOptional(command,token)==null;}break;
                }
                steps.Add(new(stage,!confirmed.ExpectPermissionDenied&&noRow==confirmed.ExpectNoRow?TrialOutcome.Met:TrialOutcome.Unmet,Reason.None,expected));
            }
            catch(Exception e)when(e is NpgsqlException or VerificationException or OperationCanceledException or TrialMismatch or IOException or UnauthorizedAccessException)
            {
                var denied=e is PostgresException {SqlState:"42501"};
                var outcome=stage==confirmed.Operation&&denied?(confirmed.ExpectPermissionDenied?TrialOutcome.Met:TrialOutcome.Unmet):e is TrialMismatch?TrialOutcome.Unmet:TrialOutcome.Unverified;
                steps.Add(new(stage,outcome,outcome==TrialOutcome.Met?Reason.None:SafeReason(e),expected));
            }
            finally
            {
                if(tx!=null)
                {
                    try{await tx.RollbackAsync(CancellationToken.None);cleanup=TrialCleanup.Complete;}
                    catch(Exception e)when(e is NpgsqlException or InvalidOperationException){}
                    await tx.DisposeAsync();
                }
            }
            var sequences=await ObserveSequenceEffects(effects,plan);
            return new(confirmed.Id,Table,steps.ToArray(),cleanup,effects.ToArray(),TargetStructure,verified){DependencyTables=ConfirmedDependencies(plan).Select(p=>p.Table).ToArray(),SequenceObservations=sequences,RoleOperation=confirmed.Operation};
        }
    }
}
