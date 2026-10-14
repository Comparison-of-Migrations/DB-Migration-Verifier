using System.Collections.Immutable;
using Npgsql;
using NpgsqlTypes;
using Verifier.Core;
namespace Verifier.Data;

public sealed partial class PostgreSqlDatabase
{
    private sealed record TrialReference(string Schema,string Table,string[] Columns,string[] ParentColumns);
    private async Task<PreparedTrial> PrepareTrialGraph(ConnectionSettings settings,Inventory source,Inventory target,Inventory inventory,Table root,CancellationToken token)
    {
        var prepared=new Dictionary<string,PreparedTrial>(StringComparer.Ordinal);var visiting=new HashSet<string>(StringComparer.Ordinal);
        return await Visit(root,false);
        async Task<PreparedTrial> Visit(Table actual,bool dependency)
        {
            token.ThrowIfCancellationRequested();if(prepared.TryGetValue(actual.Qualified,out var known))return known;
            if(!await CanPrepareTrialTable(actual,token))throw new VerificationException(Reason.RowFiltered);
            visiting.Add(actual.Qualified);var casts=await ReadTrialCasts(actual,token);var relation=await TrialRelationId(connection!,transaction,actual,token);
            var references=new List<TrialReference>();
            const string sql="""
                SELECT n.nspname,c.relname,
                  ARRAY(SELECT a.attname::pg_catalog.text FROM pg_catalog.unnest(f.conkey) WITH ORDINALITY k(id,pos) JOIN pg_catalog.pg_attribute a ON a.attrelid=f.conrelid AND a.attnum=k.id ORDER BY k.pos),
                  ARRAY(SELECT a.attname::pg_catalog.text FROM pg_catalog.unnest(f.confkey) WITH ORDINALITY k(id,pos) JOIN pg_catalog.pg_attribute a ON a.attrelid=f.confrelid AND a.attnum=k.id ORDER BY k.pos)
                FROM pg_catalog.pg_constraint f JOIN pg_catalog.pg_class c ON c.oid=f.confrelid JOIN pg_catalog.pg_namespace n ON n.oid=c.relnamespace WHERE f.conrelid=@id AND f.contype='f' ORDER BY f.oid
                """;
            await using(var command=new NpgsqlCommand(sql,connection,transaction))
            {command.Parameters.AddWithValue("id",NpgsqlDbType.Oid,relation);await using var reader=await command.ExecuteReaderAsync(token);while(await reader.ReadAsync(token))references.Add(new(reader.GetString(0),reader.GetString(1),reader.GetFieldValue<string[]>(2),reader.GetFieldValue<string[]>(3)));}
            var dependencies=new List<PreparedTrial>();var fixedValues=new Dictionary<string,string?>(StringComparer.Ordinal);var issues=new List<TrialInputIssue>();
            foreach(var reference in references)
            {
                var parent=inventory.Tables.SingleOrDefault(t=>t.Schema==reference.Schema&&t.Name==reference.Table);
                if(parent==null||!await CanPrepareTrialTable(parent,token)){issues.Add(new(null,"ForeignKeyVisibility",Reason.VisibilityUnknown));continue;}
                if(visiting.Contains(parent.Qualified)){issues.Add(new(null,"ForeignKeyCycle",Reason.NotImplemented));continue;}
                var p=await Visit(parent,true);dependencies.AddRange(p.Dependencies);dependencies.Add(p);
                for(var i=0;i<reference.Columns.Length;i++)
                {
                    if(i>=reference.ParentColumns.Length||!p.Draft.Insert.TryGetValue(reference.ParentColumns[i],out var value))
                    {issues.Add(new(reference.Columns[i],"ForeignKeyInput",Reason.InvalidInput));continue;}
                    if(fixedValues.TryGetValue(reference.Columns[i],out var prior)&&prior!=value)
                    {issues.Add(new(reference.Columns[i],"ForeignKeyConflict",Reason.InvalidInput));continue;}
                    fixedValues[reference.Columns[i]]=value;
                }
                if(p.Draft.NeedsInput.Length>0)issues.Add(new(null,"ForeignKeyParentInput",Reason.InvalidInput));
            }
            var draft=await GenerateTrialDraft(actual,casts,token,dependency,fixedValues);
            // Every parent proposal is visible and must be included in confirmation.
            // Unresolved cycles/conflicts stay explicit; no existing row values are read.
            draft=new(draft.Insert,draft.Update,draft.NeedsInput.Where(i=>i.Kind!="ForeignKeyPreparation").Concat(issues));
            var expected=target.Tables.SingleOrDefault(t=>t.Qualified==actual.Qualified);var environment=expected==null?Verdict.Different:Verdict.Equal;
            if(expected!=null)
            {
                var scope=new MatchedScope(expected,actual,ScopeMatching.ResolveColumns(expected,actual,[]));
                foreach(var check in Checks.Definitions.Where(d=>d.Category is Category.Schema or Category.Access or Category.Objects))
                {var compared=CatalogComparison.Compare(check.Id,scope,target.Complete&&inventory.Complete);if(compared.Findings.Count>0)environment=Verdict.Different;else if(compared.Reason!=Reason.None&&environment==Verdict.Equal)environment=Verdict.Indeterminate;}
            }
            var result=new PreparedTrial(settings,source.Identity,target.Identity,inventory.Identity,actual,relation,casts,environment,draft,dependencies.DistinctBy(p=>p.Id).ToImmutableArray());
            prepared.Add(actual.Qualified,result);visiting.Remove(actual.Qualified);return result;
        }
    }
    private async Task<bool> CanPrepareTrialTable(Table table,CancellationToken token)
    {
        if(table.Catalog.SelectMany(s=>s.Facts).Any(f=>f.Attribute=="TableKind"&&f.Value=="\"m\""))return false;
        if(table.SafeRead)return true;
        // Comparison still refuses filtered inventories. A trial baseline may use
        // an account that actually bypasses RLS, without changing the table/policy.
        const string sql="""
            WITH RECURSIVE relations(id) AS (SELECT c.oid FROM pg_catalog.pg_class c JOIN pg_catalog.pg_namespace n ON n.oid=c.relnamespace WHERE n.nspname=@schema AND c.relname=@table
              UNION SELECT i.inhrelid FROM relations r JOIN pg_catalog.pg_inherits i ON i.inhparent=r.id)
            SELECT count(*)>0 AND COALESCE(bool_and(c.relkind IN ('r','p') AND (c.relkind='p' OR a.amname='heap') AND pg_catalog.has_table_privilege(c.oid,'SELECT') AND NOT pg_catalog.row_security_active(c.oid)),false)
            FROM relations r JOIN pg_catalog.pg_class c ON c.oid=r.id LEFT JOIN pg_catalog.pg_am a ON a.oid=c.relam
            """;
        await using var command=new NpgsqlCommand(sql,connection,transaction);command.Parameters.AddWithValue("schema",table.Schema);command.Parameters.AddWithValue("table",table.Name);return await command.ExecuteScalarAsync(token) is true;
    }
    public sealed partial class PreparedTrial
    {
        private IEnumerable<PreparedTrial> ConfirmedDependencies(ConfirmedTrialPlan plan)=>Dependencies.Where(p=>plan.Dependencies.ContainsKey(p.Id));
        private async Task<List<(PreparedTrial Owner,Receipt Row)>> InsertDependencies(NpgsqlConnection connection,NpgsqlTransaction tx,ConfirmedTrialPlan plan,CancellationToken token)
        {
            var rows=new List<(PreparedTrial,Receipt)>();
            foreach(var p in ConfirmedDependencies(plan))
            {
                await p.LockRelation(connection,tx,token);
                await using var observer=new PostgreSqlDatabase(settings);var inventory=await observer.InspectAsync(token);var actual=inventory.Tables.SingleOrDefault(t=>t.Qualified==p.Table);
                if(inventory.Identity!=trialIdentity||actual==null||!SameDefinition(p.table,actual))throw new VerificationException(Reason.ObservationChanged);
                var values=plan.Dependencies[p.Id];var receipt=await p.Insert(connection,tx,values,token);
                if(!await observer.TrialValuesEqual(connection,tx,actual,receipt,values,p.casts,token))throw new TrialMismatch();
                rows.Add((p,receipt));
            }
            return rows;
        }
        private void ValidateDependencies(ConfirmedTrialPlan plan)
        {
            if(plan.Dependencies.Keys.Any(id=>!Dependencies.Any(p=>p.Id==id)))throw new VerificationException(Reason.InvalidInput);
            foreach(var p in ConfirmedDependencies(plan))
            {
                var values=plan.Dependencies[p.Id];
                if(values.Keys.Any(k=>!p.casts.ContainsKey(k)||p.Columns.Any(c=>c.Name==k&&c.Generated)))throw new VerificationException(Reason.UnsupportedType);
                if(p.Columns.Any(c=>!c.Generated&&!c.HasDefault&&!c.Nullable&&!values.ContainsKey(c.Name)))throw new VerificationException(Reason.InvalidInput);
            }
        }
    }
}
