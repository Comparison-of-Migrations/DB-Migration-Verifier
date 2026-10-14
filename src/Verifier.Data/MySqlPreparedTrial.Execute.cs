using System.Data;
using System.Globalization;
using System.Text.Json;
using MySqlConnector;
using Verifier.Core;
namespace Verifier.Data;

public sealed partial class MySqlPreparedTrial
{
    private string Relation=>MySqlTrialValue.Quote(table.Schema)+"."+MySqlTrialValue.Quote(table.Name);
    private sealed class TrialMismatch:Exception;
    private static Reason Error(Exception e)=>e switch
    {
        VerificationException v=>v.Reason,OperationCanceledException=>Reason.Cancelled,
        MySqlException m when m.ErrorCode==MySqlErrorCode.CommandTimeoutExpired=>Reason.Timeout,
        MySqlException m when m.Number is 1044 or 1045 or 1142 or 1143 or 1227 or 3530 or 1959=>Reason.PermissionDenied,
        MySqlException m when m.Number is 1205 or 1317 or 1969 or 3024=>Reason.Timeout,
        MySqlException m when m.Number is 1146 or 1054 or 1412 or 1932=>Reason.ObservationChanged,
        MySqlException m when ConstraintError(m)||m.SqlState?.StartsWith("22",StringComparison.Ordinal)==true=>Reason.InvalidInput,
        MySqlException=>Reason.ConnectionLost,TrialMismatch=>Reason.None,_=>Reason.InvalidInput
    };
    private static bool ConstraintError(MySqlException e)=>e.Number is 1048 or 1062 or 1451 or 1452 or 3819 or 4025 or 1406 or 1264 or 1292 or 1366 or 3140;
    private async Task Open(MySqlConnection connection,CancellationToken token)
    {
        await connection.OpenAsync(token);
        await using(var q=new MySqlCommand("SELECT VERSION()",connection))
        {var version=(string)(await q.ExecuteScalarAsync(token))!;if(Kind==DbKind.MySql?(!version.StartsWith("8.4.",StringComparison.Ordinal)||version.Contains("MariaDB",StringComparison.OrdinalIgnoreCase)):(!version.StartsWith("11.4.",StringComparison.Ordinal)||!version.Contains("MariaDB",StringComparison.Ordinal)))throw new VerificationException(Reason.PlatformNotVerified);}
        await using(var q=new MySqlCommand("SELECT "+(Kind==DbKind.MySql?"@@server_uuid":"@@server_uid")+",DATABASE(),@@lower_case_table_names",connection))
        await using(var r=await q.ExecuteReaderAsync(token))
        {
            if(!await r.ReadAsync(token)||r.IsDBNull(0)||r.IsDBNull(1))throw new VerificationException(Reason.IdentityUnknown);
            var server=r.GetString(0);var db=r.GetString(1);if(r.GetInt32(2)!=0)db=db.ToLowerInvariant();
            if(Kind==DbKind.MySql){if(!Guid.TryParse(server,out var uuid))throw new VerificationException(Reason.IdentityUnknown);server=uuid.ToString();}
            else{try{if(Convert.FromBase64String(server).Length!=20)throw new VerificationException(Reason.IdentityUnknown);}catch(FormatException){throw new VerificationException(Reason.IdentityUnknown);}}
            var actual=JsonSerializer.Serialize(Kind==DbKind.MySql?new[]{server,db}:new[]{"MariaDB",server,db});
            if(actual==sourceIdentity||actual==targetIdentity)throw new VerificationException(Reason.SameDatabase);if(actual!=identity)throw new VerificationException(Reason.ObservationChanged);
        }
        await using(var q=new MySqlCommand("SET SESSION time_zone='+00:00';SET SESSION sql_mode=CONCAT_WS(',',@@SESSION.sql_mode,'STRICT_ALL_TABLES','NO_ZERO_DATE','NO_ZERO_IN_DATE','ERROR_FOR_DIVISION_BY_ZERO','NO_ENGINE_SUBSTITUTION');SET SESSION foreign_key_checks=1;SET SESSION unique_checks=1"+(Kind==DbKind.MySql?";SET SESSION information_schema_stats_expiry=0":";SET SESSION check_constraint_checks=1"),connection))await q.ExecuteNonQueryAsync(token);
    }
    private async Task Guard(MySqlConnection connection,MySqlTransaction tx,ConfirmedTrialPlan plan,CancellationToken token)
    {
        // Taking native metadata locks before reinspection closes the DDL gap.
        foreach(var scope in Scope(plan))
        {await using var q=new MySqlCommand("SELECT "+"1"+" FROM "+scope.Relation+" WHERE 0 FOR UPDATE",connection,tx);await using var r=await q.ExecuteReaderAsync(token);if(await r.ReadAsync(token))throw new VerificationException(Reason.ObservationChanged);}
        await using var observer=DatabaseFactory.Create(settings);var current=await observer.InspectAsync(token);
        if(current.Identity!=identity)throw new VerificationException(Reason.ObservationChanged);
        foreach(var scope in Scope(plan))
        {if(await scope.RelationIdentity(connection,tx,token)!=scope.nativeRelation)throw new VerificationException(Reason.ObservationChanged);var observed=current.Tables.SingleOrDefault(t=>t.Qualified==scope.Table);if(observed==null||Definition(observed)!=scope.signature)throw new VerificationException(Reason.ObservationChanged);}
    }
    private async Task<string> RelationIdentity(MySqlConnection connection,MySqlTransaction tx,CancellationToken token)
    {
        // Endpoint-local native IDs authorize only this prepared object. They never
        // enter migration definition comparisons, reports or persistent history.
        var schema=InnoDbIdentifier.Encode(table.Schema);var name=InnoDbIdentifier.Encode(table.Name);
        if(schema==null||name==null)throw new VerificationException(Reason.UnsupportedType);
        await using var q=new MySqlCommand("SELECT NAME,TABLE_ID FROM information_schema."+(Kind==DbKind.MySql?"INNODB_TABLES":"INNODB_SYS_TABLES")+" WHERE CAST(NAME AS BINARY)=CAST(@name AS BINARY) OR CAST(LEFT(NAME,CHAR_LENGTH(@name)+1) AS BINARY)=CAST(CONCAT(@name,'#') AS BINARY) ORDER BY CAST(NAME AS BINARY)",connection,tx);
        q.Parameters.AddWithValue("name",schema+"/"+name);var rows=new List<(string Name,ulong Id)>();
        try{await using var r=await q.ExecuteReaderAsync(token);while(await r.ReadAsync(token))rows.Add((r.GetString(0),r.GetUInt64(1)));}
        catch(MySqlException e){throw new VerificationException(Error(e));}
        if(rows.Count==0)throw new VerificationException(Reason.VisibilityUnknown);
        return JsonSerializer.Serialize(rows.Select(r=>new{r.Name,r.Id}));
    }
    private string[] Bind(MySqlCommand query,IReadOnlyDictionary<string,string?> values,bool rejection=false)
    {
        var expressions=new List<string>();var i=0;
        foreach(var (name,value) in values)
        {
            var column=table.Columns.Single(c=>c.Name==name);var parameterName="@v"+i++;var expression=parameterName;MySqlParameter parameter;
            try
            {
                parameter=MySqlTrialValue.Parameter(parameterName,column,value);
                if(value!=null&&MySqlTrialGeometry.Is(column))
                {query.Parameters.AddWithValue(parameterName+"_srid",MySqlTrialGeometry.Parse(value).Srid);expression="ST_GeomFromWKB("+parameterName+","+parameterName+"_srid)";}
            }
            catch(VerificationException e)when(rejection&&e.Reason==Reason.InvalidInput)
            {parameter=new MySqlParameter(parameterName,MySqlDbType.VarChar){Value=(object?)value??DBNull.Value};}
            query.Parameters.Add(parameter);expressions.Add(expression);
        }
        return expressions.ToArray();
    }
    private string Owned(MySqlCommand query,IReadOnlyDictionary<string,string?> receipt)
    {
        // A full unique key is preferred. Otherwise the confirmed values form a
        // transaction-local receipt: an empty locking read precedes INSERT, then
        // every read must identify exactly one row. No receipt survives rollback.
        var names=key.Count>0?key:receipt.Keys.ToArray();
        if(names.Count==0)throw new VerificationException(Reason.AmbiguousRows);
        var i=0;return string.Join(" AND ",names.Select(name=>
        {
            if(!receipt.TryGetValue(name,out var value)||key.Count>0&&value==null)throw new VerificationException(Reason.AmbiguousRows);
            var column=table.Columns.Single(c=>c.Name==name);var parameter="@k"+i++;query.Parameters.Add(MySqlTrialValue.Parameter(parameter,column,value));var expression=value!=null&&Kind==DbKind.MySql&&MySqlTrialValue.Base(column)=="json"?"CAST("+parameter+" AS JSON)":parameter;
            if(value!=null&&MySqlTrialGeometry.Is(column)){query.Parameters.AddWithValue(parameter+"_srid",MySqlTrialGeometry.Parse(value).Srid);expression="ST_GeomFromWKB("+parameter+","+parameter+"_srid)";}
            return MySqlTrialValue.Quote(name)+" <=> "+expression;
        }));
    }

    private async Task Insert(MySqlConnection connection,MySqlTransaction tx,IReadOnlyDictionary<string,string?> values,CancellationToken token,bool rejection=false,bool checkVacant=true)
    {
        if(!rejection&&checkVacant&&await Read(connection,tx,values,null,token))throw new VerificationException(Reason.AmbiguousRows);
        await using var query=new MySqlCommand("",connection,tx);var expressions=Bind(query,values,rejection);query.CommandText="INSERT INTO "+Relation+" ("+string.Join(',',values.Keys.Select(MySqlTrialValue.Quote))+") VALUES ("+string.Join(',',expressions)+")";await query.PrepareAsync(token);
        if(await query.ExecuteNonQueryAsync(token)!=1)throw new VerificationException(Reason.ObservationChanged);await Warnings(connection,tx,token);
    }
    private async Task<int> Update(MySqlConnection connection,MySqlTransaction tx,IReadOnlyDictionary<string,string?> receipt,IReadOnlyDictionary<string,string?> values,CancellationToken token,bool rejection=false)
    {
        // For a keyless receipt the new predicate must be vacant or still match
        // the exact old app row before UPDATE. Otherwise refuse before modifying
        // any row; checking affected count only afterwards would be too late.
        if(key.Count==0&&!rejection){var updated=receipt.ToDictionary(v=>v.Key,v=>v.Value,StringComparer.Ordinal);foreach(var (name,value) in values)updated[name]=value;try{_=await Read(connection,tx,updated,receipt,token);}catch(TrialMismatch){throw new VerificationException(Reason.AmbiguousRows);}}
        await using var query=new MySqlCommand("",connection,tx);var expressions=Bind(query,values,rejection);query.CommandText="UPDATE "+Relation+" SET "+string.Join(',',values.Select((v,i)=>MySqlTrialValue.Quote(v.Key)+"="+expressions[i]))+" WHERE "+Owned(query,receipt);await query.PrepareAsync(token);var count=await query.ExecuteNonQueryAsync(token);if(count>1)throw new VerificationException(Reason.AmbiguousRows);await Warnings(connection,tx,token);return count;
    }
    private async Task<int> Delete(MySqlConnection connection,MySqlTransaction tx,IReadOnlyDictionary<string,string?> receipt,CancellationToken token)
    {
        await using var query=new MySqlCommand("",connection,tx);query.CommandText="DELETE FROM "+Relation+" WHERE "+Owned(query,receipt);await query.PrepareAsync(token);var count=await query.ExecuteNonQueryAsync(token);if(count>1)throw new VerificationException(Reason.AmbiguousRows);await Warnings(connection,tx,token);return count;
    }
    private async Task<bool> Read(MySqlConnection connection,MySqlTransaction tx,IReadOnlyDictionary<string,string?> receipt,IReadOnlyDictionary<string,string?>? expected,CancellationToken token,bool locking=true)
    {
        var columns=(expected??receipt).Keys.Select(name=>table.Columns.Single(c=>c.Name==name)).ToArray();await using var query=new MySqlCommand("",connection,tx);
        query.CommandText="SELECT "+string.Join(',',columns.Select(MySqlTrialValue.ReadExpression))+" FROM "+Relation+" WHERE "+Owned(query,receipt)+(locking?" FOR UPDATE":"");await query.PrepareAsync(token);bool found;
        await using(var reader=await query.ExecuteReaderAsync(token))
        {
            found=await reader.ReadAsync(token);if(found)
            {
                if(expected!=null)for(var i=0;i<columns.Length;i++)if(!MySqlTrialValue.Matches(reader,i,columns[i],expected[columns[i].Name]))throw new TrialMismatch();
                if(await reader.ReadAsync(token))throw new VerificationException(Reason.AmbiguousRows);
            }
        }
        await Warnings(connection,tx,token);return found;
    }
    private static async Task Warnings(MySqlConnection connection,MySqlTransaction tx,CancellationToken token)
    {await using var query=new MySqlCommand("SHOW WARNINGS",connection,tx);await using var reader=await query.ExecuteReaderAsync(token);if(await reader.ReadAsync(token))throw new VerificationException(Reason.InvalidInput);}
    private async Task InsertParents(MySqlConnection connection,MySqlTransaction tx,ConfirmedTrialPlan plan,CancellationToken token)
    {
        foreach(var parent in parents.Where(p=>plan.Dependencies.ContainsKey(p.Id)))
        {var values=plan.Dependencies[parent.Id];await parent.Insert(connection,tx,values,token);if(!await parent.Read(connection,tx,values,values,token))throw new TrialMismatch();}
    }
    private async Task<BehaviorResult> Finish(Guid id,ConfirmedTrialPlan plan,List<TrialStep> steps,TrialCleanup cleanup,bool role,TrialStage? operation=null)
    {
        var observed=new List<TrialSequenceObservation>();
        try
        {
            using var deadline=new CancellationTokenSource(TimeSpan.FromSeconds(5));await using var observer=DatabaseFactory.Create(settings);var inventory=await observer.InspectAsync(deadline.Token);
            foreach(var scope in Scope(plan))
            {
                var after=inventory.Tables.SingleOrDefault(t=>t.Qualified==scope.Table)?.Catalog.SingleOrDefault(s=>s.CheckId=="OBJECT-02");
                var effect=inventory.Identity!=identity||scope.allocator==null||after==null||scope.allocator.Reason!=Reason.None||after.Reason!=Reason.None?TrialEffect.SequenceStateUnverified:
                    JsonSerializer.Serialize(scope.allocator.Facts.Where(f=>f.Attribute is "SequenceState" or "AutoIncrementState"))==JsonSerializer.Serialize(after.Facts.Where(f=>f.Attribute is "SequenceState" or "AutoIncrementState"))?TrialEffect.SequenceStateUnchanged:TrialEffect.SequenceStateChanged;
                observed.Add(new(scope.Table,effect));
            }
        }
        catch(Exception e)when(e is VerificationException or MySqlException or OperationCanceledException or IOException or UnauthorizedAccessException){observed.Clear();}
        if(observed.Count==0)observed.AddRange(Scope(plan).Select(p=>new TrialSequenceObservation(p.Table,TrialEffect.SequenceStateUnverified)));
        var environment=TargetStructure;foreach(var (name,value) in plan.Insert.Concat(plan.Update)){var column=table.Columns.Single(c=>c.Name==name);if(value!=null&&MySqlTrialGeometry.Is(column)&&MySqlTrialGeometry.Parse(value).Srid!=0&&environment==Verdict.Equal)environment=Verdict.Indeterminate;}
        return new(id,Table,steps,cleanup,observed.Select(o=>o.Effect).Distinct().Concat(new[]{TrialEffect.TriggerEffectsUnverified,TrialEffect.ExternalEffectsUnverified}).ToArray(),environment,role){RoleOperation=operation,DependencyTables=Scope(plan).Where(p=>p!=this).Select(p=>p.Table).ToArray(),SequenceObservations=observed};
    }
    public async Task<BehaviorResult> ExecuteAsync(ConfirmedTrialPlan plan,CancellationToken token)
    {
        ValidateConfirmedPlan(plan);if(plan.Role!=null)throw new VerificationException(Reason.InvalidInput);
        var steps=new List<TrialStep>();var stage=TrialStage.Insert;var cleanup=TrialCleanup.Unverified;MySqlTransaction? tx=null;await using var connection=NewConnection();
        try
        {
            await Open(connection,token);tx=await connection.BeginTransactionAsync(IsolationLevel.RepeatableRead,token);await Guard(connection,tx,plan,token);
            if(plan.Dependencies.Count>0){stage=TrialStage.DependencyInsert;await InsertParents(connection,tx,plan,token);Met();}
            stage=TrialStage.Insert;await Insert(connection,tx,plan.Insert,token);Met();
            stage=TrialStage.ReadInserted;if(!await Read(connection,tx,plan.Insert,plan.Insert,token))throw new TrialMismatch();Met();
            stage=TrialStage.Update;if(await Update(connection,tx,plan.Insert,plan.Update,token)!=1)throw new TrialMismatch();Met();var receipt=plan.Insert.SetItems(plan.Update);
            stage=TrialStage.ReadUpdated;if(!await Read(connection,tx,receipt,receipt,token))throw new TrialMismatch();Met();
            stage=TrialStage.Delete;if(await Delete(connection,tx,receipt,token)!=1)throw new TrialMismatch();Met();
            stage=TrialStage.VerifyCleanup;if(await Read(connection,tx,receipt,null,token))throw new TrialMismatch();Met();
            stage=TrialStage.RollbackCleanup;await tx.RollbackAsync(CancellationToken.None);await tx.DisposeAsync();tx=null;cleanup=TrialCleanup.Complete;Met();
            if(plan.RejectedInput!=null){var rejected=await Reject(connection,plan,token);steps.AddRange(rejected.Steps);cleanup=rejected.Cleanup;}
        }
        catch(Exception e)when(e is MySqlException or VerificationException or OperationCanceledException or TrialMismatch or IOException or UnauthorizedAccessException)
        {steps.Add(new(stage,e is TrialMismatch||e is MySqlException m&&ConstraintError(m)?TrialOutcome.Unmet:TrialOutcome.Unverified,Error(e)));}
        finally
        {
            if(tx!=null)cleanup=await Rollback(tx);
        }
        return await Finish(plan.Id,plan,steps,cleanup,false);
        void Met()=>steps.Add(new(stage,TrialOutcome.Met,Reason.None));
    }
    private MySqlConnection NewConnection()=>new(new MySqlConnectionStringBuilder(settings.MySqlString()){AllowUserVariables=true}.ConnectionString);
    private static async Task<TrialCleanup> Rollback(MySqlTransaction tx)
    {
        var result=TrialCleanup.Unverified;
        try{await tx.RollbackAsync(CancellationToken.None);result=TrialCleanup.Complete;}
        catch(Exception e)when(e is MySqlException or InvalidOperationException or IOException){}
        try{await tx.DisposeAsync();}catch(Exception e)when(e is MySqlException or InvalidOperationException or IOException){result=TrialCleanup.Unverified;}
        return result;
    }
    private async Task<(IReadOnlyList<TrialStep> Steps,TrialCleanup Cleanup)> Reject(MySqlConnection connection,ConfirmedTrialPlan plan,CancellationToken token)
    {
        MySqlTransaction? tx=null;var stage=TrialStage.NotStarted;var steps=new List<TrialStep>();var cleanup=TrialCleanup.Unverified;
        try
        {
            tx=await connection.BeginTransactionAsync(IsolationLevel.RepeatableRead,token);
            await Guard(connection,tx,plan,token);await InsertParents(connection,tx,plan,token);
            if(plan.RejectedStage==TrialStage.RejectUpdate)await Insert(connection,tx,plan.Insert,token);
            stage=plan.RejectedStage!.Value;
            try
            {
                if(stage==TrialStage.RejectUpdate)await Update(connection,tx,plan.Insert,plan.RejectedInput!,token,true);else await Insert(connection,tx,plan.RejectedInput!,token,true);
                steps.Add(new(stage,TrialOutcome.Unmet,Reason.None,TrialExpectation.ConstraintRejection));
            }
            catch(MySqlException e)when(ConstraintError(e))
            {
                var matches=e.SqlState==plan.ExpectedSqlState&&e.Number==plan.ExpectedNativeError;
                var outcome=matches?TrialOutcome.Met:TrialOutcome.Unmet;var reason=Reason.None;
                if(matches&&plan.ExpectedConstraint!=null)
                {
                    await using(var query=new MySqlCommand("GET DIAGNOSTICS CONDITION 1 @dmv_constraint=CONSTRAINT_NAME",connection,tx))await query.ExecuteNonQueryAsync(token);
                    await using var get=new MySqlCommand("SELECT @dmv_constraint",connection,tx);var constraint=await get.ExecuteScalarAsync(token) as string;
                    if(string.IsNullOrEmpty(constraint)){outcome=TrialOutcome.Unverified;reason=Reason.VisibilityUnknown;}
                    else outcome=constraint==plan.ExpectedConstraint?TrialOutcome.Met:TrialOutcome.Unmet;
                }
                steps.Add(new(stage,outcome,reason,TrialExpectation.ConstraintRejection));
            }
        }
        catch(Exception e)when(e is MySqlException or VerificationException or OperationCanceledException or TrialMismatch or IOException or UnauthorizedAccessException)
        {steps.Add(new(stage,TrialOutcome.Unverified,Error(e),TrialExpectation.ConstraintRejection));}
        finally{if(tx!=null)cleanup=await Rollback(tx);}
        return (steps,cleanup);
    }
}
