using System.Collections.Immutable;
using System.Data;
using System.Globalization;
using System.Security.Cryptography;
using Npgsql;
using NpgsqlTypes;
using Verifier.Core;
namespace Verifier.Data;

public sealed partial class PostgreSqlDatabase
{
    // Preparation is read-only. A separate, explicitly confirmed plan is required for execution.
    public static async Task<PreparedTrial> PrepareTrialAsync(ConnectionSettings settings,Inventory source,Inventory target,string table,CancellationToken token)
    {
        if(settings.Kind!=DbKind.PostgreSql||string.IsNullOrWhiteSpace(source.Identity)||string.IsNullOrWhiteSpace(target.Identity))throw new VerificationException(Reason.InvalidInput);
        await using var observer=new PostgreSqlDatabase(settings);var inventory=await observer.InspectAsync(token);
        if(inventory.Identity==source.Identity||inventory.Identity==target.Identity)throw new VerificationException(Reason.SameDatabase);
        var actual=inventory.Tables.SingleOrDefault(t=>t.Qualified==table)??throw new VerificationException(Reason.InvalidInput);
        if(actual.Catalog.SelectMany(s=>s.Facts).Any(f=>f.Attribute=="TableKind"&&f.Value=="\"m\""))throw new VerificationException(Reason.InvalidInput);
        return await observer.PrepareTrialGraph(settings,source,target,inventory,actual,token);
    }
    private static async Task<uint> TrialRelationId(NpgsqlConnection connection,NpgsqlTransaction? tx,Table table,CancellationToken token)
    {
        await using var command=new NpgsqlCommand("SELECT c.oid FROM pg_catalog.pg_class c JOIN pg_catalog.pg_namespace n ON n.oid=c.relnamespace WHERE n.nspname=@schema AND c.relname=@table",connection,tx);
        command.Parameters.AddWithValue("schema",table.Schema);command.Parameters.AddWithValue("table",table.Name);
        return await command.ExecuteScalarAsync(token) is uint id?id:throw new VerificationException(Reason.ObservationChanged);
    }
    public sealed partial class PreparedTrial:IPreparedTrial
    {
        private readonly ConnectionSettings settings;
        private readonly string sourceIdentity,targetIdentity,trialIdentity;
        private readonly Table table;
        private readonly uint relationId;
        private readonly ImmutableDictionary<string,string> casts;
        public Guid Id {get;}=Guid.NewGuid();
        public DbKind Kind=>DbKind.PostgreSql;
        IReadOnlyList<TrialColumn> IPreparedTrial.Columns=>Columns;
        IReadOnlyList<IPreparedTrial> IPreparedTrial.Dependencies=>Dependencies.Cast<IPreparedTrial>().ToArray();
        public string Table=>table.Qualified;
        public Verdict TargetStructure {get;}
        public ImmutableArray<TrialColumn> Columns {get;}
        public TrialDraft Draft {get;}
        public ImmutableArray<PreparedTrial> Dependencies {get;}
        internal PreparedTrial(ConnectionSettings settings,string source,string target,string identity,Table table,uint relationId,ImmutableDictionary<string,string> casts,Verdict environment,TrialDraft draft,ImmutableArray<PreparedTrial> dependencies)
        {
            this.settings=settings;sourceIdentity=source;targetIdentity=target;trialIdentity=identity;this.table=table;this.relationId=relationId;this.casts=casts;TargetStructure=environment;Draft=draft;Dependencies=dependencies;
            Columns=table.Columns.Select(c=>
            {
                var definition=(c.Definition??"").Split('\u001f');var generated=definition.Length>3&&definition[3].Length>0;
                var defaults=definition.Length>2&&(definition[1].Length>0||definition[2].Length>0);
                // Suggestions are synthetic, never derived from production row values.
                var suggestion=Draft.Insert.GetValueOrDefault(c.Name);
                return new TrialColumn(c.Name,c.Type,c.Nullable,generated,defaults,suggestion,!generated&&!defaults&&(!casts.ContainsKey(c.Name)||suggestion==null));
            }).ToImmutableArray();
        }
        public override string ToString()=>"PreparedTrial (connection and identity redacted)";
        public void ValidateConfirmedPlan(ConfirmedTrialPlan plan)=>Validate(plan);
        public async Task<BehaviorResult> ExecuteAsync(ConfirmedTrialPlan plan,CancellationToken token)
        {
            Validate(plan);var steps=new List<TrialStep>();var effects=new List<TrialEffect>{TrialEffect.SequenceStateUnverified,TrialEffect.TriggerEffectsUnverified,TrialEffect.ExternalEffectsUnverified};
            var cleanup=TrialCleanup.Unverified;var stage=TrialStage.Insert;NpgsqlTransaction? tx=null;var roleVerified=false;
            await using var writable=new NpgsqlConnection(settings.PostgreSqlString());
            PostgreSqlDatabase? codec=null;
            try
            {
                await writable.OpenAsync(token);await VerifyIdentity(writable,token);
                tx=await writable.BeginTransactionAsync(IsolationLevel.RepeatableRead,token);await Configure(writable,tx,token);
                await LockRelation(writable,tx,token);
                // Reinspect after taking the relation lock. A stale preparation cannot authorize a changed table.
                codec=new(settings);var current=await codec.InspectAsync(token);
                var observed=current.Tables.SingleOrDefault(t=>t.Qualified==Table);
                if(observed==null||!SameDefinition(table,observed))throw new VerificationException(Reason.ObservationChanged);
                if(plan.Dependencies.Count>0){stage=TrialStage.DependencyInsert;await InsertDependencies(writable,tx,plan,token);Met();stage=TrialStage.Insert;}
                if(plan.Role!=null){stage=TrialStage.VerifyRole;await SetRole(writable,tx,plan.Role,token);roleVerified=true;Met();stage=TrialStage.Insert;}
                var receipt=await Insert(writable,tx,plan.Insert,token);Met();
                stage=TrialStage.ReadInserted;if(!await codec.TrialValuesEqual(writable,tx,observed,receipt,plan.Insert,casts,token))throw new TrialMismatch();Met();
                stage=TrialStage.Update;receipt=await Update(writable,tx,receipt,plan.Update,token);Met();
                stage=TrialStage.ReadUpdated;var expected=plan.Insert.SetItems(plan.Update);if(!await codec.TrialValuesEqual(writable,tx,observed,receipt,expected,casts,token))throw new TrialMismatch();Met();
                stage=TrialStage.Delete;await Delete(writable,tx,receipt,token);Met();
                stage=TrialStage.VerifyCleanup;if(await Exists(writable,tx,receipt,token))throw new VerificationException(Reason.ObservationChanged);Met();
                // The normal CRUD probe never persists trigger changes to existing DB rows.
                // Sequences/external effects still need separate observation; rollback is not a full restore claim.
                stage=TrialStage.RollbackCleanup;await tx.RollbackAsync(CancellationToken.None);await tx.DisposeAsync();tx=null;Met();cleanup=TrialCleanup.Complete;
                if(plan.RejectedInput!=null)
                {
                    var rejected=await Reject(writable,plan,token);steps.Add(rejected.Step);cleanup=rejected.Cleanup;
                }
            }
            catch(Exception e)when(e is PostgresException or NpgsqlException or VerificationException or OperationCanceledException or TrialMismatch or IOException or UnauthorizedAccessException)
            {
                steps.Add(new(stage,e is TrialMismatch?TrialOutcome.Unmet:TrialOutcome.Unverified,SafeReason(e)));
                if(tx!=null)
                {
                    try{await tx.RollbackAsync(CancellationToken.None);cleanup=TrialCleanup.Complete;}catch(Exception rollback)when(rollback is NpgsqlException or InvalidOperationException){cleanup=TrialCleanup.Unverified;}
                }
            }
            finally{if(tx!=null)await tx.DisposeAsync();if(codec!=null)await codec.DisposeAsync();}
            var sequences=await ObserveSequenceEffects(effects,plan);
            return new(plan.Id,Table,steps.ToArray(),cleanup,effects.ToArray(),TargetStructure,roleVerified){DependencyTables=ConfirmedDependencies(plan).Select(p=>p.Table).ToArray(),SequenceObservations=sequences};
            void Met()=>steps.Add(new(stage,TrialOutcome.Met,Reason.None));
        }
        private void Validate(ConfirmedTrialPlan plan)
        {
            ValidateDependencies(plan);
            if(plan.ExpectedNativeError!=null||plan.RoleHost!=null)throw new VerificationException(Reason.InvalidInput);
            if(plan.PreparationId!=Id||plan.Update.Count==0||plan.Role is {Length:0})throw new VerificationException(Reason.InvalidInput);
            foreach(var values in new[]{plan.Insert,plan.Update,plan.RejectedInput}.OfType<IReadOnlyDictionary<string,string?>>())
                if(values.Keys.Any(k=>!casts.ContainsKey(k)||Columns.Any(c=>c.Name==k&&c.Generated)))throw new VerificationException(Reason.UnsupportedType);
            if(Columns.Any(c=>!c.Generated&&!c.HasDefault&&!c.Nullable&&!plan.Insert.ContainsKey(c.Name)))throw new VerificationException(Reason.InvalidInput);
            if(plan.RejectedInput==null)
            {if(plan.RejectedStage!=null||plan.ExpectedSqlState!=null||plan.ExpectedConstraint!=null)throw new VerificationException(Reason.InvalidInput);}
            else if(plan.RejectedStage is not(TrialStage.RejectInsert or TrialStage.RejectUpdate or TrialStage.RejectCommit)
                ||plan.ExpectedSqlState is not("23502" or "23503" or "23505" or "23514" or "23P01" or "22001" or "22003" or "22P02"))throw new VerificationException(Reason.InvalidInput);
        }
        private async Task VerifyIdentity(NpgsqlConnection connection,CancellationToken token)
        {
            if(connection.PostgreSqlVersion.Major!=17)throw new VerificationException(Reason.PlatformNotVerified);
            await using var command=new NpgsqlCommand(IdentitySql,connection);var actual=await command.ExecuteScalarAsync(token) as string;
            if(string.IsNullOrWhiteSpace(actual))throw new VerificationException(Reason.IdentityUnknown);
            if(actual==sourceIdentity||actual==targetIdentity)throw new VerificationException(Reason.SameDatabase);
            if(actual!=trialIdentity)throw new VerificationException(Reason.ObservationChanged);
        }
        private static async Task Configure(NpgsqlConnection connection,NpgsqlTransaction tx,CancellationToken token)
        {await using var command=new NpgsqlCommand("SET LOCAL search_path=pg_catalog; SET LOCAL jit=off; SET LOCAL TimeZone='UTC'",connection,tx);await command.ExecuteNonQueryAsync(token);}
        private static async Task SetRole(NpgsqlConnection connection,NpgsqlTransaction tx,string role,CancellationToken token)
        {
            await using(var command=new NpgsqlCommand("SET LOCAL ROLE "+Quote(role)+";SET LOCAL row_security=on",connection,tx))await command.ExecuteNonQueryAsync(token);
            await using var identity=new NpgsqlCommand("SELECT CURRENT_USER",connection,tx);
            if(await identity.ExecuteScalarAsync(token) is not string actual||actual!=role)throw new VerificationException(Reason.IdentityUnknown);
        }
        private static bool SameDefinition(Table a,Table b)=>a.Columns.SequenceEqual(b.Columns)&&a.SafeRead==b.SafeRead&&a.DataDescendants.SequenceEqual(b.DataDescendants)
            &&Definitions(a).SequenceEqual(Definitions(b));
        private static IEnumerable<string> Definitions(Table table)=>table.Catalog.SelectMany(s=>s.Facts.Where(f=>f.Attribute!="SequenceState").Select(f=>System.Text.Json.JsonSerializer.Serialize(new{s.CheckId,s.Reason,f.Key,f.Column,f.Attribute,f.Value}))).Concat(table.Catalog.SelectMany(s=>s.Gaps.Select(g=>System.Text.Json.JsonSerializer.Serialize(new{s.CheckId,g.Key,g.Column,g.Attribute,g.LocalEvidence,g.Reason})))).Order(StringComparer.Ordinal);
        private async Task LockRelation(NpgsqlConnection connection,NpgsqlTransaction tx,CancellationToken token)
        {
            await using(var command=new NpgsqlCommand("LOCK TABLE "+Qualified(table)+" IN ROW EXCLUSIVE MODE",connection,tx))await command.ExecuteNonQueryAsync(token);
            if(await TrialRelationId(connection,tx,table,token)!=relationId)throw new VerificationException(Reason.ObservationChanged);
        }
        private async Task<IReadOnlyList<TrialSequenceObservation>> ObserveSequenceEffects(List<TrialEffect> effects,ConfirmedTrialPlan plan)
        {
            var observations=new List<TrialSequenceObservation>();
            foreach(var scope in ConfirmedDependencies(plan).Append(this))observations.Add(new(scope.Table,await scope.ObserveSequenceState()));
            effects.Remove(TrialEffect.SequenceStateUnverified);effects.AddRange(observations.Select(o=>o.Effect).Distinct());return observations;
        }
        private async Task<TrialEffect> ObserveSequenceState()
        {
            try
            {
                using var deadline=new CancellationTokenSource(TimeSpan.FromSeconds(5));
                await using var observer=new PostgreSqlDatabase(settings);var inventory=await observer.InspectAsync(deadline.Token);
                if(inventory.Identity!=trialIdentity)return TrialEffect.SequenceStateUnverified;
                var after=inventory.Tables.SingleOrDefault(t=>t.Qualified==Table);if(after==null)return TrialEffect.SequenceStateUnverified;
                var beforeSection=table.Catalog.Single(s=>s.CheckId=="OBJECT-02");var afterSection=after.Catalog.Single(s=>s.CheckId=="OBJECT-02");
                if(beforeSection.Reason!=Reason.None||afterSection.Reason!=Reason.None)return TrialEffect.SequenceStateUnverified;
                var before=beforeSection.Facts.Where(f=>f.Attribute=="SequenceState").OrderBy(f=>f.Key,StringComparer.Ordinal);
                var current=afterSection.Facts.Where(f=>f.Attribute=="SequenceState").OrderBy(f=>f.Key,StringComparer.Ordinal);
                return before.SequenceEqual(current)?TrialEffect.SequenceStateUnchanged:TrialEffect.SequenceStateChanged;
            }
            catch(Exception e)when(e is VerificationException or NpgsqlException or IOException or UnauthorizedAccessException or OperationCanceledException){}
            return TrialEffect.SequenceStateUnverified;
        }
        private sealed class TrialMismatch:Exception;
        internal sealed record Receipt(uint Relation,string Tuple,string Xmin);
        private const string Returning=" RETURNING tableoid,ctid::pg_catalog.text,xmin::pg_catalog.text";
        internal static void BindReceipt(NpgsqlCommand command,Receipt receipt)
        {command.Parameters.AddWithValue("relation",NpgsqlDbType.Oid,receipt.Relation);command.Parameters.AddWithValue("tuple",receipt.Tuple);command.Parameters.AddWithValue("xmin",receipt.Xmin);}
        internal const string Owned="tableoid=@relation AND ctid=@tuple::pg_catalog.tid AND xmin=@xmin::pg_catalog.xid";
        private async Task<Receipt> Insert(NpgsqlConnection connection,NpgsqlTransaction tx,IReadOnlyDictionary<string,string?> values,CancellationToken token)
        {
            await using var command=new NpgsqlCommand("",connection,tx);var items=BindValues(command,values);
            command.CommandText="INSERT INTO "+Qualified(table)+(items.Count==0?" DEFAULT VALUES":" ("+string.Join(',',items.Select(i=>Quote(i.Name)))+") OVERRIDING SYSTEM VALUE VALUES ("+string.Join(',',items.Select(i=>i.Expression))+")")+Returning;
            return await Returned(command,token);
        }
        private async Task<Receipt?> UpdateOptional(NpgsqlConnection connection,NpgsqlTransaction tx,Receipt receipt,IReadOnlyDictionary<string,string?> values,CancellationToken token)
        {
            await using var command=new NpgsqlCommand("",connection,tx);var items=BindValues(command,values);BindReceipt(command,receipt);
            command.CommandText="UPDATE "+Qualified(table)+" SET "+string.Join(',',items.Select(i=>Quote(i.Name)+"="+i.Expression))+" WHERE "+Owned+Returning;
            return await ReturnedOptional(command,token);
        }
        private async Task<Receipt> Update(NpgsqlConnection connection,NpgsqlTransaction tx,Receipt receipt,IReadOnlyDictionary<string,string?> values,CancellationToken token)=>await UpdateOptional(connection,tx,receipt,values,token)??throw new VerificationException(Reason.ObservationChanged);
        private async Task Delete(NpgsqlConnection connection,NpgsqlTransaction tx,Receipt receipt,CancellationToken token)
        {
            await using var command=new NpgsqlCommand("DELETE FROM "+Qualified(table)+" WHERE "+Owned+Returning,connection,tx);BindReceipt(command,receipt);
            _=await Returned(command,token);
        }
        private async Task<bool> Exists(NpgsqlConnection connection,NpgsqlTransaction tx,Receipt receipt,CancellationToken token)
        {
            await using var command=new NpgsqlCommand("SELECT EXISTS(SELECT 1 FROM "+Qualified(table)+" WHERE "+Owned+")",connection,tx);BindReceipt(command,receipt);
            return await command.ExecuteScalarAsync(token) is true;
        }
        private List<(string Name,string Expression)> BindValues(NpgsqlCommand command,IReadOnlyDictionary<string,string?> values)
        {
            var items=new List<(string,string)>();foreach(var (name,value) in values)
            {var parameter="v"+items.Count;command.Parameters.AddWithValue(parameter,NpgsqlDbType.Text,(object?)value??DBNull.Value);items.Add((name,"CAST(@"+parameter+" AS "+casts[name]+")"));}return items;
        }
        private static async Task<Receipt> Returned(NpgsqlCommand command,CancellationToken token)
            =>await ReturnedOptional(command,token)??throw new VerificationException(Reason.ObservationChanged);
        private static async Task<Receipt?> ReturnedOptional(NpgsqlCommand command,CancellationToken token)
        {
            await using var reader=await command.ExecuteReaderAsync(token);if(!await reader.ReadAsync(token))return null;
            var receipt=new Receipt(reader.GetFieldValue<uint>(0),reader.GetString(1),reader.GetString(2));if(await reader.ReadAsync(token))throw new VerificationException(Reason.ObservationChanged);return receipt;
        }
        private async Task<(TrialStep Step,TrialCleanup Cleanup)> Reject(NpgsqlConnection connection,ConfirmedTrialPlan plan,CancellationToken token)
        {
            await VerifyIdentity(connection,token);await using var tx=await connection.BeginTransactionAsync(IsolationLevel.RepeatableRead,token);await Configure(connection,tx,token);
            var receipts=new List<(PreparedTrial Owner,Receipt Row)>();var stage=TrialStage.Insert;var committed=false;
            try
            {
                await LockRelation(connection,tx,token);
                await using(var observer=new PostgreSqlDatabase(settings))
                {var current=await observer.InspectAsync(token);var observed=current.Tables.SingleOrDefault(t=>t.Qualified==Table);if(current.Identity!=trialIdentity||observed==null||!SameDefinition(table,observed))throw new VerificationException(Reason.ObservationChanged);}
                if(plan.Dependencies.Count>0){stage=TrialStage.DependencyInsert;receipts.AddRange(await InsertDependencies(connection,tx,plan,token));stage=TrialStage.Insert;}
                if(plan.Role!=null){stage=TrialStage.VerifyRole;await SetRole(connection,tx,plan.Role,token);stage=TrialStage.Insert;}
                receipts.Add((this,await Insert(connection,tx,plan.Insert,token)));var baseline=receipts.Count-1;stage=plan.RejectedStage==TrialStage.RejectUpdate?TrialStage.RejectUpdate:TrialStage.RejectInsert;
                if(stage==TrialStage.RejectUpdate)receipts[baseline]=(this,await Update(connection,tx,receipts[baseline].Row,plan.RejectedInput!,token));
                else receipts.Add((this,await Insert(connection,tx,plan.RejectedInput!,token)));
                if(plan.RejectedStage==TrialStage.RejectCommit){stage=TrialStage.RejectCommit;await tx.CommitAsync(token);committed=true;}
                else await tx.RollbackAsync(CancellationToken.None);
                var cleanup=committed?await CleanupCommitted(connection,receipts,token):TrialCleanup.Complete;
                return(new(stage,TrialOutcome.Unmet,Reason.None,TrialExpectation.ConstraintRejection),cleanup);
            }
            catch(Exception e)when(e is PostgresException or NpgsqlException or VerificationException or OperationCanceledException)
            {
                var cleanup=TrialCleanup.Unverified;
                try{if(!committed){if(tx.Connection!=null)await tx.RollbackAsync(CancellationToken.None);if(connection.State==ConnectionState.Open)cleanup=TrialCleanup.Complete;}}catch(Exception rollback)when(rollback is NpgsqlException or InvalidOperationException){}
                // A server-side deferred COMMIT error aborts the transaction. The driver
                // may already have completed/disposed its transaction object: verify idle
                // state on the same live connection instead of treating that as lost cleanup.
                if(e is PostgresException&&stage==TrialStage.RejectCommit&&connection.State==ConnectionState.Open)
                {
                    try
                    {
                        await using var idle=new NpgsqlCommand("SELECT pg_catalog.pg_current_xact_id_if_assigned() IS NULL",connection);
                        if(await idle.ExecuteScalarAsync(CancellationToken.None) is true)cleanup=TrialCleanup.Complete;
                    }
                    catch(Exception observation)when(observation is NpgsqlException or InvalidOperationException){}
                }
                var matched=e is PostgresException pg&&stage==plan.RejectedStage&&pg.SqlState==plan.ExpectedSqlState&&(plan.ExpectedConstraint==null||pg.ConstraintName==plan.ExpectedConstraint);
                var outcome=matched?TrialOutcome.Met:e is PostgresException wrong&&wrong.SqlState.StartsWith("23",StringComparison.Ordinal)?TrialOutcome.Unmet:TrialOutcome.Unverified;
                return(new(stage,outcome,matched?Reason.None:SafeReason(e),TrialExpectation.ConstraintRejection),cleanup);
            }
        }
        private async Task<TrialCleanup> CleanupCommitted(NpgsqlConnection connection,IReadOnlyList<(PreparedTrial Owner,Receipt Row)> receipts,CancellationToken token)
        {
            // Never delete by a user key, prefix or range. A committed receipt must still identify the unmodified app row.
            await VerifyIdentity(connection,token);await using var tx=await connection.BeginTransactionAsync(token);
            try
            {
                // Cleanup runs as the authenticated preparer, independently of the
                // tested role's DELETE privilege, and still touches exact receipts only.
                await using(var reset=new NpgsqlCommand("RESET ROLE",connection,tx))await reset.ExecuteNonQueryAsync(token);
                foreach(var owner in receipts.Select(r=>r.Owner).DistinctBy(p=>p.Id))await owner.LockRelation(connection,tx,token);
                foreach(var receipt in receipts.Reverse())await receipt.Owner.Delete(connection,tx,receipt.Row,token);
                await tx.CommitAsync(token);return TrialCleanup.Complete;
            }
            catch(Exception e)when(e is PostgresException or NpgsqlException or VerificationException or OperationCanceledException)
            {try{await tx.RollbackAsync(CancellationToken.None);}catch(Exception rollback)when(rollback is NpgsqlException or InvalidOperationException){}return TrialCleanup.Failed;}
        }
        private static Reason SafeReason(Exception e)=>e switch
        {OperationCanceledException=>Reason.Cancelled,VerificationException v=>v.Reason,PostgresException p=>p.SqlState switch{"42501"=>Reason.PermissionDenied,"57014"=>Reason.Timeout,_=>Reason.ObservationChanged},NpgsqlException=>Reason.ConnectionLost,IOException or UnauthorizedAccessException=>Reason.TemporaryStorage,_=>Reason.ObservationChanged};
    }
    private async Task<bool> TrialValuesEqual(NpgsqlConnection writable,NpgsqlTransaction tx,Table table,PreparedTrial.Receipt receipt,IReadOnlyDictionary<string,string?> values,ImmutableDictionary<string,string> casts,CancellationToken token)
    {
        var columns=table.Columns.Where(c=>values.ContainsKey(c.Name)).ToArray();if(columns.Length==0)return true;
        await using var command=new NpgsqlCommand("",writable,tx);PreparedTrial.BindReceipt(command,receipt);
        var expected=new List<string>();foreach(var column in columns)
        {var parameter="e"+expected.Count;command.Parameters.AddWithValue(parameter,NpgsqlDbType.Text,(object?)values[column.Name]??DBNull.Value);expected.Add("CAST(@"+parameter+" AS "+casts[column.Name]+") AS "+Quote(column.Name));}
        var projection=string.Join(',',columns.Select(c=>ValueProjection(c,true)));
        command.CommandText="WITH actual AS (SELECT * FROM "+Qualified(table)+" WHERE "+PreparedTrial.Owned+"), expected AS (SELECT "+string.Join(',',expected)+") SELECT 0,"+projection+" FROM actual UNION ALL SELECT 1,"+projection+" FROM expected";
        await using var reader=await command.ExecuteReaderAsync(CommandBehavior.SequentialAccess,token);using var store=new ProtectedRowStore(token:token);byte[]? actualRow=null,expectedRow=null;
        while(await reader.ReadAsync(token))
        {var kind=reader.GetInt32(0);var descriptor=await store.AddAsync((output,cancel)=>WriteProjectedRow(reader,columns,output,cancel,1),token);if(kind==0){if(actualRow!=null)throw new VerificationException(Reason.ObservationChanged);actualRow=descriptor;}else{if(expectedRow!=null)throw new VerificationException(Reason.ObservationChanged);expectedRow=descriptor;}}
        if(actualRow==null||expectedRow==null)throw new VerificationException(Reason.ObservationChanged);store.Flush();store.Validate(actualRow,token);store.Validate(expectedRow,token);return store.Compare(actualRow,expectedRow)==0;
    }
}
