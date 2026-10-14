using System.Text.RegularExpressions;
using Npgsql;
using Verifier.Core;
namespace Verifier.Data;

public sealed partial class PostgreSqlDatabase
{
    private sealed record IntegrityRule(string Kind,string[] Columns,string? ReferenceSchema,string? ReferenceTable,string[] ReferenceColumns,
        string? Expression,string? Nodes,uint[] Operators,uint Index,string Match);
    public async Task<IntegrityObservation> InspectIntegrityAsync(Table table,CancellationToken token)
    {
        if(inventory==null||!inventory.Tables.Contains(table))throw new VerificationException(Reason.InvalidInput);
        EnsureReadSession(token);
        try
        {
            if(table.SafeRead||table.SafeLocalRead)await ValidateReadScope(table,token);
            await using(var save=new NpgsqlCommand("SAVEPOINT dmv_integrity_table",connection,transaction))await save.ExecuteNonQueryAsync(token);
            var result=await ReadIntegrity(table,token);
            if(table.SafeRead||table.SafeLocalRead)await ValidateReadScope(table,token);
            await using var release=new NpgsqlCommand("RELEASE SAVEPOINT dmv_integrity_table",connection,transaction);await release.ExecuteNonQueryAsync(token);return result;
        }
        catch(PostgresException e)
        {
            if(Map(e).Reason is Reason.Timeout or Reason.ConnectionLost)throw ReadFailure(e);
            try{await using var restore=new NpgsqlCommand("ROLLBACK TO SAVEPOINT dmv_integrity_table; RELEASE SAVEPOINT dmv_integrity_table",connection,transaction);await restore.ExecuteNonQueryAsync(token);}catch(NpgsqlException failure){throw ReadFailure(failure);}catch(InvalidOperationException) when(connection?.State!=System.Data.ConnectionState.Open){throw LostReadSession();}
            throw Map(e);
        }
        catch(NpgsqlException e){throw ReadFailure(e);}
        catch(InvalidOperationException) when(connection?.State!=System.Data.ConnectionState.Open){throw LostReadSession();}
    }
    private async Task<IntegrityObservation> ReadIntegrity(Table table,CancellationToken token)
    {
        if(inventory==null||!inventory.Tables.Contains(table))throw new VerificationException(Reason.InvalidInput);
        var findings=new List<IntegrityViolation>();var issues=new List<ScopeIssue>();
        if(!(table.Partitioned?table.SafeRead:table.SafeLocalRead))return new(findings,[new(table.Qualified,null,table.ReadReason==Reason.None?Reason.RowFiltered:table.ReadReason)]);
        await GuardIntegrity(table,issues,()=>ObserveDomainConstraints(table,issues,Count,token),token);
        foreach(var column in table.Columns.Where(c=>!c.SafeValue))issues.Add(new(table.Qualified,column.Name,Reason.UnsupportedType));
        foreach(var column in table.Columns.Where(c=>!c.Nullable))
            await Count("NotNull",column.Name,"SELECT count(*) FROM "+ReadRelation(table)+" a WHERE pg_catalog.num_nonnulls(a."+Quote(column.Name)+")=0");
        var rules=new List<IntegrityRule>();
        const string sql="""
            SELECT con.contype::pg_catalog.text,
              ARRAY(SELECT a.attname::pg_catalog.text FROM pg_catalog.unnest(con.conkey) WITH ORDINALITY k(id,position)
                JOIN pg_catalog.pg_attribute a ON a.attrelid=con.conrelid AND a.attnum=k.id ORDER BY k.position),
              rn.nspname,rc.relname,
              ARRAY(SELECT a.attname::pg_catalog.text FROM pg_catalog.unnest(con.confkey) WITH ORDINALITY k(id,position)
                JOIN pg_catalog.pg_attribute a ON a.attrelid=con.confrelid AND a.attnum=k.id ORDER BY k.position),
              pg_catalog.pg_get_expr(con.conbin,con.conrelid,false),con.conbin::pg_catalog.text,
              COALESCE(con.conpfeqop,'{}'::pg_catalog.oid[]),con.conindid,con.confmatchtype::pg_catalog.text
            FROM pg_catalog.pg_constraint con JOIN pg_catalog.pg_class c ON c.oid=con.conrelid JOIN pg_catalog.pg_namespace n ON n.oid=c.relnamespace
            LEFT JOIN pg_catalog.pg_class rc ON rc.oid=con.confrelid LEFT JOIN pg_catalog.pg_namespace rn ON rn.oid=rc.relnamespace
            WHERE n.nspname=@schema AND c.relname=@table
            """;
        await GuardIntegrity(table,issues,async()=>
        {
        await using(var command=new NpgsqlCommand(GuardDeparsers(sql),connection,transaction))
        {
            command.Parameters.AddWithValue("schema",table.Schema);command.Parameters.AddWithValue("table",table.Name);
            await using var reader=await command.ExecuteReaderAsync(token);
            while(await reader.ReadAsync(token))rules.Add(new(reader.GetString(0),reader.GetFieldValue<string[]>(1),reader.IsDBNull(2)?null:reader.GetString(2),reader.IsDBNull(3)?null:reader.GetString(3),reader.GetFieldValue<string[]>(4),reader.IsDBNull(5)?null:reader.GetString(5),reader.IsDBNull(6)?null:reader.GetString(6),reader.GetFieldValue<uint[]>(7),reader.GetFieldValue<uint>(8),reader.GetString(9)));
        }
        },token);
        await GuardIntegrity(table,issues,()=>ObserveUniqueIndexes(table,issues,Count,token),token);
        foreach(var rule in rules)
        {
            token.ThrowIfCancellationRequested();
            if(rule.Columns.Any(name=>!table.Columns.Any(c=>c.Name==name&&c.SafeValue)))
            {issues.Add(new(table.Qualified,null,Reason.UnsupportedType));continue;}
            if(rule.Kind is "p" or "u")
            {
                if(rule.Columns.Length==0||rule.Columns.Any(n=>!SafeIntegrityScalar(table.Columns.Single(c=>c.Name==n)))||!await SafeUniqueIndex(rule.Index,token)){issues.Add(new(table.Qualified,null,Reason.NotImplemented));continue;}
                var columns=string.Join(',',rule.Columns.Select(Quote));
                var notNull=string.Join(" AND ",rule.Columns.Select(c=>"pg_catalog.num_nonnulls("+Quote(c)+")=1"));
                // NULLS NOT DISTINCT changes which groups participate in uniqueness.
                await using var index=new NpgsqlCommand("SELECT indnullsnotdistinct FROM pg_catalog.pg_index WHERE indexrelid=@id",connection,transaction);
                index.Parameters.AddWithValue("id",NpgsqlTypes.NpgsqlDbType.Oid,rule.Index);var nullsEqual=(bool)(await index.ExecuteScalarAsync(token)??throw new VerificationException(Reason.ObservationChanged));
                await Count("Unique",rule.Columns.Length==1?rule.Columns[0]:null,"SELECT COALESCE(sum(n-1),0)::pg_catalog.int8 FROM (SELECT count(*) n FROM "+ReadRelation(table)+(nullsEqual?"":" WHERE "+notNull)+" GROUP BY "+columns+" HAVING count(*)>1) duplicate_groups");
            }
            else if(rule.Kind=="f")
            {
                var referenced=inventory.Tables.SingleOrDefault(t=>t.Schema==rule.ReferenceSchema&&t.Name==rule.ReferenceTable);
                if(referenced==null||!(referenced.Partitioned?referenced.SafeRead:referenced.SafeLocalRead)){issues.Add(new(table.Qualified,null,Reason.RowFiltered));continue;}
                if(rule.Columns.Length==0||rule.Columns.Length!=rule.ReferenceColumns.Length||rule.Operators.Length!=rule.Columns.Length||rule.ReferenceColumns.Any(n=>!referenced.Columns.Any(c=>c.Name==n&&c.SafeValue))||!await SafeOperators(rule.Operators,token))
                {issues.Add(new(table.Qualified,null,Reason.UnsupportedType));continue;}
                var allPresent=string.Join(" AND ",rule.Columns.Select(c=>"pg_catalog.num_nonnulls(a."+Quote(c)+")=1"));
                var anyPresent=string.Join(" OR ",rule.Columns.Select(c=>"pg_catalog.num_nonnulls(a."+Quote(c)+")=1"));
                var match=string.Join(" AND ",rule.Columns.Select((c,i)=>"b."+Quote(rule.ReferenceColumns[i])+" OPERATOR(pg_catalog.=) a."+Quote(c)));
                var absent="NOT EXISTS(SELECT 1 FROM "+ReadRelation(referenced)+" b WHERE "+match+")";
                var predicate=rule.Match switch{"s"=>"("+allPresent+") AND "+absent,"f"=>"(("+anyPresent+") AND NOT("+allPresent+")) OR (("+allPresent+") AND "+absent+")",_=>null};
                if(predicate==null){issues.Add(new(table.Qualified,null,Reason.NotImplemented));continue;}
                await Count("Reference",rule.Columns.Length==1?rule.Columns[0]:null,"SELECT count(*) FROM "+ReadRelation(table)+" a WHERE "+predicate);
            }
            else if(rule.Kind=="c")
            {
                if(rule.Expression==null||rule.Expression.StartsWith(BlockedDefinition,StringComparison.Ordinal)||rule.Nodes==null||!await SafeExpression(rule.Nodes,token))
                {issues.Add(new(table.Qualified,null,Reason.NotImplemented));continue;}
                await Count("Check",null,"SELECT count(*) FROM "+ReadRelation(table)+" WHERE ("+rule.Expression+") IS FALSE");
            }
            else if(rule.Kind=="x")
                await GuardIntegrity(table,issues,()=>ObserveExclusion(table,rule.Index,issues,Count,token),token);
            else issues.Add(new(table.Qualified,null,Reason.NotImplemented));
        }
        return new(findings,issues);
        async Task Count(string kind,string? column,string countSql)
        {
            await using(var save=new NpgsqlCommand("SAVEPOINT dmv_integrity",connection,transaction))await save.ExecuteNonQueryAsync(token);
            try
            {
                await using var command=new NpgsqlCommand(countSql,connection,transaction);var count=Convert.ToInt64(await command.ExecuteScalarAsync(token),System.Globalization.CultureInfo.InvariantCulture);
                if(count>0)findings.Add(new(kind,column,count));
            }
            catch(PostgresException e)
            {
                if(Map(e).Reason is Reason.Timeout or Reason.ConnectionLost)throw ReadFailure(e);
                await using var rollback=new NpgsqlCommand("ROLLBACK TO SAVEPOINT dmv_integrity",connection,transaction);await rollback.ExecuteNonQueryAsync(token);
                issues.Add(new(table.Qualified,column,e.SqlState switch{"42501"=>Reason.PermissionDenied,"57014"=>Reason.Timeout,"42883"=>Reason.UnsupportedType,_=>Reason.ObservationChanged}));
            }
            await using(var release=new NpgsqlCommand("RELEASE SAVEPOINT dmv_integrity",connection,transaction))await release.ExecuteNonQueryAsync(token);
        }
    }
    // Legacy inheritance constraints and referenced keys are local to their physical table.
    // Declarative partition constraints cover the logical partitioned relation instead.
    private static string ReadRelation(Table table)=>(table.Partitioned?"":"ONLY ")+Qualified(table);
    private static string Qualified(Table table)=>Quote(table.Schema)+"."+Quote(table.Name);
    private async Task<bool> SafeUniqueIndex(uint index,CancellationToken token)
    {
        await using var command=new NpgsqlCommand("SELECT i.indexprs IS NULL AND i.indpred IS NULL AND NOT EXISTS(SELECT 1 FROM pg_catalog.unnest(i.indclass) x JOIN pg_catalog.pg_opclass o ON o.oid=x JOIN pg_catalog.pg_namespace n ON n.oid=o.opcnamespace WHERE (n.nspname<>'pg_catalog' OR o.opcname IN ('array_ops','record_ops','range_ops','multirange_ops'))) FROM pg_catalog.pg_index i WHERE i.indexrelid=@id",connection,transaction);
        command.Parameters.AddWithValue("id",NpgsqlTypes.NpgsqlDbType.Oid,index);return await command.ExecuteScalarAsync(token) is true;
    }
    private async Task<bool> SafeOperators(uint[] operators,CancellationToken token)
    {
        await using var command=new NpgsqlCommand("SELECT count(*)=@count AND COALESCE(bool_and(n.nspname='pg_catalog' AND o.oprname='=' AND pn.nspname='pg_catalog' AND l.lanname='internal' AND p.provolatile='i' AND (p.proname IN ('num_nonnulls','num_nulls','array_length','array_lower','array_upper','array_ndims','cardinality','enum_eq','enum_ne','enum_lt','enum_le','enum_gt','enum_ge','enum_cmp') OR NOT EXISTS(SELECT 1 FROM pg_catalog.unnest(p.proargtypes) arg JOIN pg_catalog.pg_type at ON at.oid=arg WHERE at.typtype='p'))),false) FROM pg_catalog.pg_operator o JOIN pg_catalog.pg_namespace n ON n.oid=o.oprnamespace JOIN pg_catalog.pg_proc p ON p.oid=o.oprcode JOIN pg_catalog.pg_namespace pn ON pn.oid=p.pronamespace JOIN pg_catalog.pg_language l ON l.oid=p.prolang WHERE o.oid=ANY(@ids)",connection,transaction);
        command.Parameters.AddWithValue("ids",NpgsqlTypes.NpgsqlDbType.Array|NpgsqlTypes.NpgsqlDbType.Oid,operators.Distinct().ToArray());command.Parameters.AddWithValue("count",operators.Distinct().Count());return await command.ExecuteScalarAsync(token) is true;
    }
    private async Task<bool> SafeExpression(string nodes,CancellationToken token)
    {
        // Version-specific pg_node_tree, restricted to scalar built-in operations.
        // Reject user routines, domains, subplans and all unfamiliar executor nodes.
        var kinds=Regex.Matches(nodes,@"\{([A-Z0-9]+)").Select(m=>m.Groups[1].Value).ToArray();
        if(kinds.Any(k=>k is not("VAR" or "CONST" or "OPEXPR" or "BOOLEXPR" or "NULLTEST" or "BOOLEANTEST" or "RELABELTYPE" or "FUNCEXPR" or "COERCETODOMAINVALUE" or "FIELDSELECT" or "COALESCEEXPR" or "CASEEXPR" or "CASEWHEN" or "CASETESTEXPR" or "NULLIFEXPR" or "DISTINCTEXPR" or "ARRAYEXPR" or "ARRAYCOERCEEXPR" or "SCALARARRAYOPEXPR" or "MINMAXEXPR" or "COLLATEEXPR" or "ROWCOMPAREEXPR" or "SUBSCRIPTINGREF")))return false;
        if(kinds.Contains("SUBSCRIPTINGREF")&&!await SafeSubscripts(nodes,kinds.Count(k=>k=="SUBSCRIPTINGREF"),token))return false;
        // MINMAX selects its comparator implicitly rather than storing opfuncid.
        var minmax=Regex.Matches(nodes,@":minmaxtype ([0-9]+)").Select(m=>uint.Parse(m.Groups[1].Value,System.Globalization.CultureInfo.InvariantCulture)).Distinct().ToArray();
        if(minmax.Length>0)
        {
            await using var types=new NpgsqlCommand("SELECT count(*)=@count AND COALESCE(bool_and(n.nspname='pg_catalog' AND t.typname=ANY(@names)),false) FROM pg_catalog.pg_type t JOIN pg_catalog.pg_namespace n ON n.oid=t.typnamespace WHERE t.oid=ANY(@ids)",connection,transaction);
            types.Parameters.AddWithValue("ids",NpgsqlTypes.NpgsqlDbType.Array|NpgsqlTypes.NpgsqlDbType.Oid,minmax);types.Parameters.AddWithValue("count",minmax.Length);types.Parameters.AddWithValue("names",Supported.Concat(BinaryCodecs.Keys).ToArray());
            if(await types.ExecuteScalarAsync(token) is not true)return false;
        }
        var ids=Regex.Matches(nodes,@":(?:funcid|opfuncid|hashfuncid|negfuncid) ([0-9]+)").Select(m=>uint.Parse(m.Groups[1].Value,System.Globalization.CultureInfo.InvariantCulture)).Where(x=>x!=0).Distinct().ToArray();
        if(kinds.Contains("ROWCOMPAREEXPR"))
        {
            var comparisons=await RowComparisonFunctions(nodes,kinds.Count(k=>k=="ROWCOMPAREEXPR"),token);
            if(comparisons==null)return false;ids=ids.Concat(comparisons).Distinct().ToArray();
        }
        var operators=Regex.Matches(nodes,@":opno ([0-9]+)").Select(m=>uint.Parse(m.Groups[1].Value,System.Globalization.CultureInfo.InvariantCulture)).Distinct().ToArray();
        if(operators.Length>0)
        {
            await using var operatorsQuery=new NpgsqlCommand("SELECT oprcode::pg_catalog.oid FROM pg_catalog.pg_operator WHERE oid=ANY(@ids)",connection,transaction);
            operatorsQuery.Parameters.AddWithValue("ids",NpgsqlTypes.NpgsqlDbType.Array|NpgsqlTypes.NpgsqlDbType.Oid,operators);var functions=new List<uint>();
            await using(var reader=await operatorsQuery.ExecuteReaderAsync(token))while(await reader.ReadAsync(token))functions.Add(reader.GetFieldValue<uint>(0));
            if(functions.Count!=operators.Length)return false;ids=ids.Concat(functions).Distinct().ToArray();
        }
        if(ids.Length==0)return !kinds.Any(k=>k is "OPEXPR" or "FUNCEXPR" or "NULLIFEXPR" or "DISTINCTEXPR" or "SCALARARRAYOPEXPR");
        await using var command=new NpgsqlCommand("SELECT count(*)=@count AND COALESCE(bool_and(n.nspname='pg_catalog' AND l.lanname='internal' AND p.provolatile='i' AND (p.proname IN ('num_nonnulls','num_nulls','array_length','array_lower','array_upper','array_ndims','cardinality','enum_eq','enum_ne','enum_lt','enum_le','enum_gt','enum_ge','enum_cmp') OR NOT EXISTS(SELECT 1 FROM pg_catalog.unnest(p.proargtypes) arg JOIN pg_catalog.pg_type at ON at.oid=arg WHERE at.typtype='p'))),false) FROM pg_catalog.pg_proc p JOIN pg_catalog.pg_namespace n ON n.oid=p.pronamespace JOIN pg_catalog.pg_language l ON l.oid=p.prolang WHERE p.oid=ANY(@ids)",connection,transaction);
        command.Parameters.AddWithValue("ids",NpgsqlTypes.NpgsqlDbType.Array|NpgsqlTypes.NpgsqlDbType.Oid,ids);command.Parameters.AddWithValue("count",ids.Length);return await command.ExecuteScalarAsync(token) is true;
    }
}
