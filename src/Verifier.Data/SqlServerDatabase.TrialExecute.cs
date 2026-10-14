using System.Data;
using System.Collections.Immutable;
using System.Globalization;
using System.Security.Cryptography;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.Data.SqlClient;
using Verifier.Core;
namespace Verifier.Data;
public sealed partial class SqlServerDatabase
{
    public sealed partial class PreparedTrial
    {
        private sealed class TrialMismatch:Exception;
        private sealed record Receipt(Dictionary<string,object?> Values,bool HasKey,IReadOnlyDictionary<string,string?>? Inputs=null);
        private string Relation=>Name(table);
        private static Reason TrialError(Exception e,CancellationToken token)=>token.IsCancellationRequested?Reason.Cancelled:e switch{
            VerificationException v=>v.Reason,OperationCanceledException=>Reason.Cancelled,
            SqlException s when s.Number is 515 or 547 or 2601 or 2627 or 2628 or 8152 or 243 or 448 or 6522=>Reason.InvalidInput,
            SqlException s=>ErrorReason(s.Number),TrialMismatch=>Reason.None,_=>Reason.InvalidInput};
        private System.Collections.Immutable.ImmutableDictionary<string,string?> BindGeneratedReferences(IReadOnlyDictionary<string,string?> values,ConfirmedTrialPlan plan,IReadOnlyList<(PreparedTrial Trial,Receipt Receipt)> receipts)
        {
            var result=values.ToImmutableDictionary(StringComparer.Ordinal);
            foreach(var column in Columns.Where(c=>c.GeneratedReference!=null))
            {
                var reference=column.GeneratedReference!;if(!plan.Dependencies.ContainsKey(reference.DependencyId))continue;
                var owned=receipts.SingleOrDefault(r=>r.Trial.Id==reference.DependencyId).Receipt;
                if(owned==null||!owned.HasKey)throw new VerificationException(Reason.ObservationChanged);
                var childColumn=table.Columns.Single(c=>c.Name==column.Name);string input;
                if(owned.Values.TryGetValue(reference.Column,out var value)&&value!=null&&value!=DBNull.Value)
                    input=SqlServerTrialValue.GeneratedKeyInput(childColumn,value);
                else if(!reference.Generated&&owned.Inputs?.TryGetValue(reference.Column,out var confirmed)==true&&confirmed!=null)
                    input=confirmed; // Parent.Read already verified this confirmed field against its owned row.
                else throw new VerificationException(Reason.ObservationChanged);
                _=SqlServerTrialValue.Parameter(childColumn,"@reference",input);
                result=result.SetItem(column.Name,input);
            }
            return result;
        }
        private async Task<SqlServerDatabase> OpenGuard(ConfirmedTrialPlan plan,CancellationToken token)
        {
            var db=new SqlServerDatabase(settings){writableTrial=true,trialTables=Scope(plan).Select(p=>p.Table).ToHashSet(StringComparer.Ordinal)};
            try
            {
                var current=await db.InspectAsync(token);
                if(current.Identity==sourceIdentity||current.Identity==targetIdentity)throw new VerificationException(Reason.SameDatabase);
                if(current.Identity!=identity)throw new VerificationException(Reason.ObservationChanged);
                await using(var setup=db.Command("SET XACT_ABORT OFF; SET LANGUAGE us_english; SET LOCK_TIMEOUT 10000"))await setup.ExecuteNonQueryAsync(token);
                foreach(var scope in Scope(plan).OrderBy(p=>p.Table,StringComparer.Ordinal))
                {
                    // Exclusive table locks cover empty/keyless tables as well,
                    // prevent concurrent writes, and keep schema stability until rollback.
                    await using(var q=db.Command("SELECT COUNT_BIG(*) FROM "+scope.Relation+" WITH(TABLOCKX,HOLDLOCK)"))await q.ExecuteScalarAsync(token);
                    var observed=current.Tables.SingleOrDefault(t=>t.Qualified==scope.Table);
                    if(observed==null||await db.TrialRelationToken(observed,token)!=scope.relationToken||Definition(observed)!=scope.signature)throw new VerificationException(Reason.ObservationChanged);
                    await db.ValidateTable(observed,token);
                }
                return db;
            }
            catch{await db.DisposeAsync();throw;}
        }
        private static object Value(SqlDataReader r,int i,Column c)=>c.Type is "decimal" or "numeric"?r.GetSqlDecimal(i):r.GetValue(i);
        private string Predicate(SqlCommand q,Receipt receipt)
        {
            var parts=new List<string>();
            foreach(var (name,value) in receipt.Values)
            {
                var c=table.Columns.Single(c=>c.Name==name);var parameter="@r"+parts.Count.ToString(CultureInfo.InvariantCulture);
                if(value==null||value==DBNull.Value){parts.Add(Quote(name)+" IS NULL");continue;}
                q.Parameters.AddWithValue(parameter,value);
                if(c.Type=="sql_variant")
                {
                    var expected=SqlServerTrialValue.Expression(c,parameter,receipt.Inputs?.GetValueOrDefault(name));
                    foreach(var property in new[]{"BaseType","Precision","Scale","MaxLength","Collation"})
                    {var actual="SQL_VARIANT_PROPERTY("+Quote(name)+",'"+property+"')";var wanted="SQL_VARIANT_PROPERTY("+expected+",'"+property+"')";parts.Add("("+actual+"="+wanted+" OR ("+actual+" IS NULL AND "+wanted+" IS NULL))");}
                }
                parts.Add(receipt.HasKey?Quote(name)+"="+parameter:
                    "CONVERT(varbinary(max),"+Quote(name)+")=CONVERT(varbinary(max),"+SqlServerTrialValue.Expression(c,parameter,receipt.Inputs?.GetValueOrDefault(name))+")");
            }
            if(parts.Count==0)throw new VerificationException(Reason.AmbiguousRows);return string.Join(" AND ",parts);
        }
        private static IReadOnlyDictionary<string,string?>? UpdatedInputs(Receipt receipt,IReadOnlyDictionary<string,string?> changes)
        {
            if(receipt.Inputs==null)return null;
            var merged=new Dictionary<string,string?>(receipt.Inputs,StringComparer.Ordinal);foreach(var entry in changes)merged[entry.Key]=entry.Value;return merged;
        }
        private Receipt InputReceipt(IReadOnlyDictionary<string,string?> values)
            =>new(values.ToDictionary(v=>v.Key,v=>(object?)SqlServerTrialValue.Parameter(table.Columns.Single(c=>c.Name==v.Key),"@v",v.Value).Value,StringComparer.Ordinal),false,values);
        private async Task<long> Count(SqlServerDatabase db,Receipt receipt,CancellationToken token)
        {await using var q=db.Command("");q.CommandText=SqlServerMetadata.CommandText("SELECT COUNT_BIG(*) FROM "+Relation+" WHERE "+Predicate(q,receipt));return Convert.ToInt64(await q.ExecuteScalarAsync(token),CultureInfo.InvariantCulture);}
        private async Task Preview(SqlServerDatabase db,IReadOnlyDictionary<string,string?> values,CancellationToken token)
        {
            // Catch lossy string/binary conversions before INSERT. Native type
            // casts are allowed for padding; never accept truncation/code-page loss.
            foreach(var (name,provided) in values.Where(v=>v.Value!=null))
            {
                var c=table.Columns.Single(c=>c.Name==name);var value=provided;
                if(c.Type=="sql_variant"){var variant=SqlServerTrialVariant.Parse(value!);c=variant.Column;value=variant.Value;}
                if(c.Type is not("char" or "varchar" or "text" or "nchar" or "nvarchar" or "ntext" or "binary" or "varbinary" or "image"))continue;
                await using var q=db.Command("");q.Parameters.Add(SqlServerTrialValue.Parameter(c,"@value",value));
                var expression=SqlServerTrialValue.Expression(c,"@value");
                q.CommandText=SqlServerMetadata.CommandText("SELECT "+(c.Type is "binary" or "varbinary" or "image"?"CONVERT(varbinary(max),"+expression+")":"CONVERT(nvarchar(max),"+expression+")"));
                var actual=await q.ExecuteScalarAsync(token);
                if(actual is byte[] bytes)
                {
                    var input=Convert.FromHexString(value![2..]);
                    if(c.Type=="binary"?(bytes.Length<input.Length||!bytes.AsSpan(0,input.Length).SequenceEqual(input)||bytes.AsSpan(input.Length).IndexOfAnyExcept((byte)0)>=0):!bytes.AsSpan().SequenceEqual(input))throw new VerificationException(Reason.InvalidInput);
                }
                else
                {
                    if(actual is not string text)throw new VerificationException(Reason.InvalidInput);
                    if(c.Type is "char" or "nchar")
                    {if(!text.StartsWith(value!,StringComparison.Ordinal)||text.AsSpan(value!.Length).IndexOfAnyExcept(' ')>=0)throw new VerificationException(Reason.InvalidInput);}
                    else if(text!=value)throw new VerificationException(Reason.InvalidInput);
                }
            }
        }
        private async Task<Receipt> Write(SqlServerDatabase db,IReadOnlyDictionary<string,string?> values,Receipt? receipt,CancellationToken token,bool rejected=false)
        {
            if(!rejected)await Preview(db,values,token);
            if(!rejected&&receipt==null&&key.Count==0&&await Count(db,InputReceipt(values),token)!=0)throw new VerificationException(Reason.AmbiguousRows);
            if(receipt!=null&&await Count(db,receipt,token)!=1)throw new VerificationException(Reason.AmbiguousRows);
            await using var q=db.Command("");var parameters=new List<string>();
            var nonce=Guid.NewGuid();q.Parameters.Add("@ownership",SqlDbType.UniqueIdentifier).Value=nonce;
            foreach(var (name,value) in values){var p="@v"+parameters.Count.ToString(CultureInfo.InvariantCulture);q.Parameters.Add(SqlServerTrialValue.Parameter(table.Columns.Single(c=>c.Name==name),p,value,rejected));parameters.Add(SqlServerTrialValue.Binding(table.Columns.Single(c=>c.Name==name),p,value));}
            var definitions=key.Count==0?"[owned] int":string.Join(',',key.Select(n=>Quote(n)+" "+SqlServerTrialValue.Declaration(table.Columns.Single(c=>c.Name==n))));
            var output=key.Count==0?"1":string.Join(',',key.Select(n=>"inserted."+Quote(n)));
            var statement=receipt==null?values.Count==0?"INSERT INTO "+Relation+" OUTPUT "+output+" INTO @receipt DEFAULT VALUES":"INSERT INTO "+Relation+" ("+string.Join(',',values.Keys.Select(Quote))+") OUTPUT "+output+" INTO @receipt VALUES("+string.Join(',',parameters)+")":
                "UPDATE "+Relation+" SET "+string.Join(',',values.Keys.Select((n,i)=>Quote(n)+"="+parameters[i]))+" OUTPUT "+output+" INTO @receipt WHERE "+Predicate(q,receipt);
            q.CommandText=SqlServerMetadata.CommandText("DECLARE @receipt TABLE("+definitions+"); "+statement+"; SELECT @ownership AS [DBMV_Receipt],* FROM @receipt");
            await using var reader=await q.ExecuteReaderAsync(token);await VerifyOutput(reader,nonce,(key.Count==0?1:key.Count)+1,token);
            var result=key.Count==0?(receipt==null?InputReceipt(values):receipt):new Receipt(key.Select((name,i)=>(name,value:Value(reader,i+1,table.Columns.Single(c=>c.Name==name)))).ToDictionary(x=>x.name,x=>(object?)x.value,StringComparer.Ordinal),true,receipt==null?values:UpdatedInputs(receipt,values));
            if(await reader.ReadAsync(token))throw new VerificationException(Reason.AmbiguousRows);
            if(key.Count==0&&receipt!=null)
            {var changed=new Dictionary<string,object?>(receipt.Values,StringComparer.Ordinal);foreach(var v in values)changed[v.Key]=SqlServerTrialValue.Parameter(table.Columns.Single(c=>c.Name==v.Key),"@v",v.Value).Value;result=new(changed,false,UpdatedInputs(receipt,values));}
            return result;
        }
        private async Task<bool> Read(SqlServerDatabase db,Receipt receipt,IReadOnlyDictionary<string,string?> expected,CancellationToken token)
        {
            if(expected.Count==0)return await Count(db,receipt,token)==1;
            var columns=expected.Keys.Select(n=>table.Columns.Single(c=>c.Name==n)).ToArray();await using var q=db.Command("");var declarations=new List<string>();
            foreach(var c in columns){var p="@e"+declarations.Count.ToString(CultureInfo.InvariantCulture);q.Parameters.Add(SqlServerTrialValue.Parameter(c,p,expected[c.Name]));declarations.Add(SqlServerTrialValue.Expression(c,p,expected[c.Name])+" AS "+Quote(c.Name));}
            q.CommandText=SqlServerMetadata.CommandText("SELECT "+string.Join(',',columns.Select(Projection))+" FROM "+Relation+" WHERE "+Predicate(q,receipt)+"; SELECT "+string.Join(',',columns.Select(Projection))+" FROM (SELECT "+string.Join(',',declarations)+") expected");
            await using var reader=await q.ExecuteReaderAsync(CommandBehavior.SequentialAccess,token);
            if(!await reader.ReadAsync(token))return false;
            using var actual=new MemoryStream();await SqlServerRowCodec.WriteAsync(reader,columns,actual,token);
            if(await reader.ReadAsync(token))throw new VerificationException(Reason.AmbiguousRows);
            if(!await reader.NextResultAsync(token)||!await reader.ReadAsync(token))throw new VerificationException(Reason.ObservationChanged);
            using var wanted=new MemoryStream();await SqlServerRowCodec.WriteAsync(reader,columns,wanted,token);
            var matches=actual.GetBuffer().AsSpan(0,(int)actual.Length).SequenceEqual(wanted.GetBuffer().AsSpan(0,(int)wanted.Length));
            CryptographicOperations.ZeroMemory(actual.GetBuffer());CryptographicOperations.ZeroMemory(wanted.GetBuffer());
            if(!matches)throw new TrialMismatch();return true;
        }
        private async Task Delete(SqlServerDatabase db,Receipt receipt,CancellationToken token)
        {
            if(await Count(db,receipt,token)!=1)throw new VerificationException(Reason.AmbiguousRows);
            await using var q=db.Command("");var nonce=Guid.NewGuid();q.Parameters.Add("@ownership",SqlDbType.UniqueIdentifier).Value=nonce;
            q.CommandText=SqlServerMetadata.CommandText("DECLARE @receipt TABLE([owned] int); DELETE FROM "+Relation+" OUTPUT 1 INTO @receipt WHERE "+Predicate(q,receipt)+"; SELECT @ownership AS [DBMV_Receipt],COUNT_BIG(*) FROM @receipt");
            await using var reader=await q.ExecuteReaderAsync(token);await VerifyOutput(reader,nonce,2,token);if(reader.GetInt64(1)!=1)throw new TrialMismatch();
        }
        private static async Task VerifyOutput(SqlDataReader reader,Guid nonce,int fields,CancellationToken token)
        {
            // A trigger can emit additional result sets. None can authorize a
            // receipt or masquerade as our row count merely by its position.
            if(reader.FieldCount!=fields||reader.GetName(0)!="DBMV_Receipt"||reader.GetFieldType(0)!=typeof(Guid)||!await reader.ReadAsync(token)||reader.GetGuid(0)!=nonce)
                throw new VerificationException(Reason.ObservationChanged);
        }
        private static async Task<TrialCleanup> Rollback(SqlServerDatabase? db)
        {
            if(db?.transaction==null)return TrialCleanup.Unverified;
            try{using var timeout=new CancellationTokenSource(TimeSpan.FromSeconds(5));await db.transaction.RollbackAsync(timeout.Token);return TrialCleanup.Complete;}
            catch(Exception e)when(e is SqlException or InvalidOperationException or OperationCanceledException){return TrialCleanup.Unverified;}
        }
        private static bool ExpectedConstraint(SqlException e,string? wanted)
        {
            if(wanted==null)return true;
            var patterns=e.Number switch{
                547=>new[]{"^The (?:INSERT|UPDATE|DELETE) statement conflicted with the (?:CHECK|FOREIGN KEY|REFERENCE) constraint \"([^\"]+)\"\\."},
                2627=>new[]{"^Violation of (?:PRIMARY KEY|UNIQUE KEY) constraint '((?:''|[^'])+)'\\."},
                2601=>new[]{"^Cannot insert duplicate key row in object '(?:''|[^'])+' with unique index '((?:''|[^'])+)'\\."},_=>Array.Empty<string>()};
            return patterns.Any(p=>Regex.Match(e.Message,p,RegexOptions.CultureInvariant) is{Success:true} m&&m.Groups[1].Value.Replace("''","'",StringComparison.Ordinal)==wanted);
        }
        public async Task<BehaviorResult> ExecuteAsync(ConfirmedTrialPlan plan,CancellationToken token)
        {
            ValidateConfirmedPlan(plan);if(plan.Role!=null)throw new VerificationException(Reason.InvalidInput);
            SqlServerDatabase? db=null;var steps=new List<TrialStep>();var stage=TrialStage.Insert;var cleanup=TrialCleanup.Unverified;
            var dependencyReceipts=new List<(PreparedTrial Trial,Receipt Receipt)>();
            try
            {
                db=await OpenGuard(plan,token);
                foreach(var parent in parents.Where(p=>plan.Dependencies.ContainsKey(p.Id)))
                {stage=TrialStage.DependencyInsert;var values=parent.BindGeneratedReferences(plan.Dependencies[parent.Id],plan,dependencyReceipts);var owned=await parent.Write(db,values,null,token);dependencyReceipts.Add((parent,owned));if(!await parent.Read(db,owned,values,token))throw new TrialMismatch();Met();}
                stage=TrialStage.Insert;var insert=BindGeneratedReferences(plan.Insert,plan,dependencyReceipts);var receipt=await Write(db,insert,null,token);Met();
                stage=TrialStage.ReadInserted;if(!await Read(db,receipt,insert,token))throw new TrialMismatch();Met();
                if(plan.RejectedInput!=null)
                {
                    stage=plan.RejectedStage!.Value;await using(var save=db.Command("SAVE TRANSACTION dmv_rejection"))await save.ExecuteNonQueryAsync(token);
                    try{_=await Write(db,plan.RejectedInput,stage==TrialStage.RejectUpdate?receipt:null,token,true);steps.Add(new(stage,TrialOutcome.Unmet,Reason.None,TrialExpectation.ConstraintRejection));}
                    catch(SqlException e){token.ThrowIfCancellationRequested();var match=e.Number==plan.ExpectedNativeError&&ExpectedConstraint(e,plan.ExpectedConstraint);steps.Add(new(stage,match?TrialOutcome.Met:TrialOutcome.Unmet,match?Reason.None:TrialError(e,token),TrialExpectation.ConstraintRejection));}
                    await using(var restore=db.Command("ROLLBACK TRANSACTION dmv_rejection"))await restore.ExecuteNonQueryAsync(token);
                }
                stage=TrialStage.Update;
                if(!receipt.HasKey)
                {var merged=insert.SetItems(plan.Update);if(!plan.Update.All(v=>insert.GetValueOrDefault(v.Key)==v.Value)&&await Count(db,InputReceipt(merged),token)!=0)throw new VerificationException(Reason.AmbiguousRows);}
                receipt=await Write(db,plan.Update,receipt,token);Met();
                stage=TrialStage.ReadUpdated;if(!await Read(db,receipt,insert.SetItems(plan.Update),token))throw new TrialMismatch();Met();
                stage=TrialStage.Delete;await Delete(db,receipt,token);Met();
                stage=TrialStage.VerifyCleanup;if(await Count(db,receipt,token)!=0)throw new TrialMismatch();Met();
                foreach(var parent in dependencyReceipts.AsEnumerable().Reverse()){stage=TrialStage.DependencyDelete;await parent.Trial.Delete(db,parent.Receipt,token);Met();}
            }
            catch(Exception e)when(e is SqlException or VerificationException or OperationCanceledException or TrialMismatch or IOException or UnauthorizedAccessException)
            {steps.Add(new(stage,e is TrialMismatch?TrialOutcome.Unmet:TrialOutcome.Unverified,TrialError(e,token)));}
            finally
            {
                cleanup=await Rollback(db);
                steps.Add(new(TrialStage.RollbackCleanup,cleanup==TrialCleanup.Complete?TrialOutcome.Met:TrialOutcome.Unverified,cleanup==TrialCleanup.Complete?Reason.None:Reason.ObservationChanged));
                if(db!=null)await db.DisposeAsync();
            }
            return await Finish(plan.Id,plan,steps,cleanup,false,null);
            void Met()=>steps.Add(new(stage,TrialOutcome.Met,Reason.None));
        }
        private async Task<BehaviorResult> Finish(Guid id,ConfirmedTrialPlan plan,IReadOnlyList<TrialStep> steps,TrialCleanup cleanup,bool role,TrialStage? operation)
        {
            var effects=new List<TrialEffect>{TrialEffect.TriggerEffectsUnverified,TrialEffect.ExternalEffectsUnverified};var sequences=new List<TrialSequenceObservation>();
            try
            {
                using var timeout=new CancellationTokenSource(TimeSpan.FromSeconds(15));await using var observer=new SqlServerDatabase(settings){trialTables=Scope(plan).Select(p=>p.Table).ToHashSet(StringComparer.Ordinal)};var after=await observer.InspectAsync(timeout.Token);
                if(after.Identity!=identity)throw new VerificationException(Reason.ObservationChanged);
                foreach(var scope in Scope(plan))
                {
                    var current=after.Tables.SingleOrDefault(t=>t.Qualified==scope.Table);var before=scope.table.Catalog.Single(s=>s.CheckId=="OBJECT-02");var now=current?.Catalog.Single(s=>s.CheckId=="OBJECT-02");
                    var effect=now==null||now.Reason!=Reason.None||before.Reason!=Reason.None?TrialEffect.SequenceStateUnverified:before.Facts.SequenceEqual(now.Facts)?TrialEffect.SequenceStateUnchanged:TrialEffect.SequenceStateChanged;
                    sequences.Add(new(scope.Table,effect));
                }
                
            }
            catch(Exception e)when(e is SqlException or VerificationException or OperationCanceledException){}
            foreach(var scope in Scope(plan).Where(p=>sequences.All(s=>s.Table!=p.Table)))sequences.Add(new(scope.Table,TrialEffect.SequenceStateUnverified));
            effects.AddRange(sequences.Select(s=>s.Effect).Distinct());
            return new(id,Table,steps,cleanup,effects,TargetStructure,role){DependencyTables=parents.Where(p=>plan.Dependencies.ContainsKey(p.Id)).Select(p=>p.Table).ToArray(),SequenceObservations=sequences,RoleOperation=operation};
        }
    }
}
