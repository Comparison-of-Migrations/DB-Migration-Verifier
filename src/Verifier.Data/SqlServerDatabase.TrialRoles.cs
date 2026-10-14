using Microsoft.Data.SqlClient;
using Verifier.Core;
namespace Verifier.Data;
public sealed partial class SqlServerDatabase
{
    public sealed partial class PreparedTrial
    {
        public async Task<BehaviorResult> ExecuteRoleAsync(ConfirmedRoleTrial confirmed,CancellationToken token)
        {
            var plan=confirmed.Plan;ValidateConfirmedPlan(plan);
            if(plan.Role==null||plan.RejectedInput!=null||confirmed.Operation is not(TrialStage.RoleInsert or TrialStage.RoleRead or TrialStage.RoleUpdate or TrialStage.RoleDelete)||confirmed.ExpectNoRow&&(confirmed.ExpectPermissionDenied||confirmed.Operation==TrialStage.RoleInsert))throw new VerificationException(Reason.InvalidInput);
            var expected=confirmed.ExpectPermissionDenied?TrialExpectation.PermissionDenied:confirmed.ExpectNoRow?TrialExpectation.NoRow:TrialExpectation.Allowed;
            var stage=TrialStage.Insert;var steps=new List<TrialStep>();var cleanup=TrialCleanup.Unverified;SqlServerDatabase? db=null;var impersonating=false;
            try
            {
                db=await OpenGuard(plan,token);
                var dependencyReceipts=new List<(PreparedTrial Trial,Receipt Receipt)>();
                foreach(var parent in parents.Where(p=>plan.Dependencies.ContainsKey(p.Id)))
                {stage=TrialStage.DependencyInsert;var values=parent.BindGeneratedReferences(plan.Dependencies[parent.Id],plan,dependencyReceipts);var owned=await parent.Write(db,values,null,token);dependencyReceipts.Add((parent,owned));if(!await parent.Read(db,owned,values,token))throw new TrialMismatch();steps.Add(new(stage,TrialOutcome.Met,Reason.None));}
                var insert=BindGeneratedReferences(plan.Insert,plan,dependencyReceipts);
                Receipt? receipt=null;stage=TrialStage.Insert;
                if(confirmed.Operation!=TrialStage.RoleInsert)receipt=await Write(db,insert,null,token);
                else if(await Count(db,InputReceipt(insert),token)!=0)throw new VerificationException(Reason.AmbiguousRows);
                await Preview(db,confirmed.Operation==TrialStage.RoleUpdate?plan.Update:insert,token);
                stage=TrialStage.VerifyRole;
                // SQL Server cannot EXECUTE AS a database role. This field names
                // a database user; its native memberships/grants apply normally.
                await using(var user=db.Command("SELECT type FROM sys.database_principals WHERE name=@user"))
                {user.Parameters.AddWithValue("@user",plan.Role);if(await user.ExecuteScalarAsync(token) is not string type||type is not("S" or "U" or "E"))throw new VerificationException(Reason.InvalidInput);}
                await using(var change=db.Command("EXECUTE AS USER = @user"))
                {change.Parameters.AddWithValue("@user",plan.Role);await change.ExecuteNonQueryAsync(token);impersonating=true;}
                await using(var verify=db.Command("SELECT USER_NAME()"))
                    if((string?)await verify.ExecuteScalarAsync(token)!=plan.Role)throw new VerificationException(Reason.VisibilityUnknown);
                steps.Add(new(stage,TrialOutcome.Met,Reason.None));stage=confirmed.Operation;var missing=false;
                switch(stage)
                {
                    case TrialStage.RoleInsert:missing=await RoleMutation(db,insert,null,TrialStage.RoleInsert,token)==0;break;
                    case TrialStage.RoleRead:missing=!await Read(db,receipt!,insert,token);break;
                    case TrialStage.RoleUpdate:
                        missing=await RoleMutation(db,plan.Update,receipt,stage,token)==0;break;
                    case TrialStage.RoleDelete:
                        missing=await RoleMutation(db,plan.Update,receipt,stage,token)==0;break;
                }
                if(stage!=TrialStage.RoleRead&&!missing)
                {
                    await using(var revert=db.Command("REVERT"))await revert.ExecuteNonQueryAsync(token);impersonating=false;
                    if(stage==TrialStage.RoleInsert)
                    {var inserted=InputReceipt(insert);if(await Count(db,inserted,token)!=1||!await Read(db,inserted,insert,token))throw new TrialMismatch();}
                    else if(stage==TrialStage.RoleDelete)
                    {if(await Count(db,receipt!,token)!=0)throw new TrialMismatch();}
                    else
                    {
                        var updated=new Dictionary<string,object?>(receipt!.Values,StringComparer.Ordinal);
                        foreach(var v in plan.Update.Where(v=>updated.ContainsKey(v.Key)))updated[v.Key]=SqlServerTrialValue.Parameter(table.Columns.Single(c=>c.Name==v.Key),"@v",v.Value).Value;
                        if(!await Read(db,new(updated,receipt.HasKey,UpdatedInputs(receipt,plan.Update)),insert.SetItems(plan.Update),token))throw new TrialMismatch();
                    }
                }
                steps.Add(new(stage,!confirmed.ExpectPermissionDenied&&missing==confirmed.ExpectNoRow?TrialOutcome.Met:TrialOutcome.Unmet,Reason.None,expected));
            }
            catch(Exception e)when(e is SqlException or VerificationException or OperationCanceledException or TrialMismatch)
            {
                var denied=!token.IsCancellationRequested&&e is SqlException{Number:229 or 230};
                var outcome=stage==confirmed.Operation&&denied?(confirmed.ExpectPermissionDenied?TrialOutcome.Met:TrialOutcome.Unmet):e is TrialMismatch?TrialOutcome.Unmet:TrialOutcome.Unverified;
                steps.Add(new(stage,outcome,outcome==TrialOutcome.Met?Reason.None:TrialError(e,token),expected));
            }
            finally
            {
                if(db!=null&&impersonating)
                {try{using var timeout=new CancellationTokenSource(TimeSpan.FromSeconds(5));await using var revert=db.Command("REVERT");await revert.ExecuteNonQueryAsync(timeout.Token);}catch(Exception e)when(e is SqlException or InvalidOperationException or OperationCanceledException){}}
                cleanup=await Rollback(db);if(db!=null)await db.DisposeAsync();
            }
            return await Finish(confirmed.Id,plan,steps,cleanup,true,confirmed.Operation);
        }
        private async Task<long> RoleMutation(SqlServerDatabase db,IReadOnlyDictionary<string,string?> values,Receipt? receipt,TrialStage operation,CancellationToken token)
        {
            // Do not require SELECT/OUTPUT privileges just to test an INSERT.
            // The owner proved/locked the receipt before switching context.
            await using var q=db.Command("");var parameters=new List<string>();
            var nonce=Guid.NewGuid();q.Parameters.Add("@ownership",System.Data.SqlDbType.UniqueIdentifier).Value=nonce;
            if(operation!=TrialStage.RoleDelete)
                foreach(var (name,value) in values){var p="@v"+parameters.Count;q.Parameters.Add(SqlServerTrialValue.Parameter(table.Columns.Single(c=>c.Name==name),p,value));parameters.Add(SqlServerTrialValue.Binding(table.Columns.Single(c=>c.Name==name),p,value));}
            q.CommandText=SqlServerMetadata.CommandText((operation switch{
                TrialStage.RoleInsert=>"INSERT INTO "+Relation+" ("+string.Join(',',values.Keys.Select(Quote))+") VALUES("+string.Join(',',parameters)+")",
                TrialStage.RoleUpdate=>"UPDATE "+Relation+" SET "+string.Join(',',values.Keys.Select((n,i)=>Quote(n)+"="+parameters[i]))+" WHERE "+Predicate(q,receipt!),
                TrialStage.RoleDelete=>"DELETE FROM "+Relation+" WHERE "+Predicate(q,receipt!),_=>throw new VerificationException(Reason.InvalidInput)})+"; SELECT @ownership AS [DBMV_Receipt],CONVERT(bigint,@@ROWCOUNT)");
            await using var reader=await q.ExecuteReaderAsync(token);await VerifyOutput(reader,nonce,2,token);var count=reader.GetInt64(1);
            if(count>1||count<0)throw new VerificationException(Reason.AmbiguousRows);return count;
        }
    }
}
