using System.Globalization;
using System.Security.Cryptography;
using System.Text.RegularExpressions;
using Npgsql;
using NpgsqlTypes;
using Verifier.Core;
namespace Verifier.Data;

public sealed partial class PostgreSqlDatabase
{
    private sealed record DraftColumn(string Name,string Type,int Modifier,bool Identity,bool Generated,string[] Labels,string? Collation);
    private sealed record DraftCheck(string? Expression,string? Nodes);
    private sealed record DraftDomainCheck(string Column,bool Required,string? Expression,string? Nodes);
    private async Task<TrialDraft> GenerateTrialDraft(Table table,IReadOnlyDictionary<string,string> casts,CancellationToken token,bool includeIdentity=false,IReadOnlyDictionary<string,string?>? references=null)
    {
        var issues=new List<TrialInputIssue>();var metadata=new List<DraftColumn>();var checks=new List<DraftCheck>();
        // Candidate predicates must not cause planner-selected user index support
        // callbacks to execute during read-only preparation.
        await using(var config=new NpgsqlCommand("SET LOCAL enable_seqscan=on;SET LOCAL enable_indexscan=off;SET LOCAL enable_indexonlyscan=off;SET LOCAL enable_bitmapscan=off",connection,transaction))await config.ExecuteNonQueryAsync(token);
        const string columnsSql="""
            WITH RECURSIVE attributes AS (SELECT * FROM pg_catalog.pg_attribute WHERE attrelid=@id AND attnum>0 AND NOT attisdropped),
            bases(name,id,modifier) AS (
              SELECT attname,atttypid,atttypmod FROM attributes
              UNION ALL SELECT b.name,t.typbasetype,CASE WHEN b.modifier>=0 THEN b.modifier ELSE t.typtypmod END
                FROM bases b JOIN pg_catalog.pg_type t ON t.oid=b.id WHERE t.typtype='d')
            SELECT a.attname,t.typname,b.modifier,a.attidentity<>''::pg_catalog."char",a.attgenerated<>''::pg_catalog."char",
              ARRAY(SELECT e.enumlabel::pg_catalog.text FROM pg_catalog.pg_enum e WHERE e.enumtypid=t.oid ORDER BY e.enumsortorder),cn.nspname,co.collname
            FROM attributes a JOIN bases b ON b.name=a.attname JOIN pg_catalog.pg_type t ON t.oid=b.id
            LEFT JOIN pg_catalog.pg_collation co ON co.oid=a.attcollation LEFT JOIN pg_catalog.pg_namespace cn ON cn.oid=co.collnamespace
            WHERE t.typtype<>'d' ORDER BY a.attnum
            """;
        var id=await TrialRelationId(connection!,transaction,table,token);
        await using(var command=new NpgsqlCommand(columnsSql,connection,transaction))
        {
            command.Parameters.AddWithValue("id",NpgsqlDbType.Oid,id);await using var reader=await command.ExecuteReaderAsync(token);
            while(await reader.ReadAsync(token))metadata.Add(new(reader.GetString(0),reader.GetString(1),reader.GetInt32(2),reader.GetBoolean(3),reader.GetBoolean(4),reader.GetFieldValue<string[]>(5),reader.IsDBNull(6)?null:Quote(reader.GetString(6))+"."+Quote(reader.GetString(7))));
        }
        var collations=metadata.Where(c=>c.Collation!=null).ToDictionary(c=>c.Name,c=>c.Collation!,StringComparer.Ordinal);
        await using(var command=new NpgsqlCommand(GuardDeparsers("SELECT contype::pg_catalog.text,pg_catalog.pg_get_expr(conbin,conrelid,false),conbin::pg_catalog.text FROM pg_catalog.pg_constraint WHERE conrelid=@id"),connection,transaction))
        {
            command.Parameters.AddWithValue("id",NpgsqlDbType.Oid,id);await using var reader=await command.ExecuteReaderAsync(token);
            while(await reader.ReadAsync(token))
            {
                var kind=reader.GetString(0);
                if(kind=="c")checks.Add(new(reader.IsDBNull(1)?null:reader.GetString(1),reader.IsDBNull(2)?null:reader.GetString(2)));
                else if(kind=="f")issues.Add(new(null,"ForeignKeyPreparation",Reason.NotImplemented));
                else if(kind=="x")issues.Add(new(null,"ExclusionCandidate",Reason.NotImplemented));
            }
        }
        var safeChecks=new List<string>();
        foreach(var check in checks)
        {
            if(check.Expression==null||check.Expression.StartsWith(BlockedDefinition,StringComparison.Ordinal)||check.Nodes==null||!await SafeExpression(check.Nodes,token))
            {issues.Add(new(null,"CheckCandidate",Reason.NotImplemented));continue;}
            var dependencies=Regex.Matches(check.Nodes,@":varattno ([0-9]+)").Select(m=>int.Parse(m.Groups[1].Value,CultureInfo.InvariantCulture)).ToHashSet();
            if(table.Columns.Where(c=>dependencies.Contains(c.Position)).Any(c=>metadata.Single(m=>m.Name==c.Name) is {Generated:true} or {Identity:true}||!casts.ContainsKey(c.Name)))
            {issues.Add(new(null,"GeneratedCheckCandidate",Reason.NotImplemented));continue;}
            safeChecks.Add(check.Expression);
        }
        var safeDomains=new List<DraftDomainCheck>();
        foreach(var domain in await ReadDraftDomainChecks(id,token))
        {
            if(!casts.ContainsKey(domain.Column)||metadata.Single(c=>c.Name==domain.Column).Generated)
            {issues.Add(new(domain.Column,"GeneratedDomainCandidate",Reason.NotImplemented));continue;}
            if(domain.Nodes!=null&&(domain.Expression==null||domain.Expression.StartsWith(BlockedDefinition,StringComparison.Ordinal)||!await SafeExpression(domain.Nodes,token)))
            {issues.Add(new(domain.Column,"DomainCheckCandidate",Reason.NotImplemented));continue;}
            safeDomains.Add(domain);
        }
        // Literal candidates are taken only from already approved CHECK expressions.
        // They are bound as data, never pasted into SQL or taken from existing rows.
        var literals=safeChecks.Concat(safeDomains.Where(d=>d.Expression!=null).Select(d=>d.Expression!)).SelectMany(c=>Regex.Matches(c,@"'(?:''|[^'])*'").Select(m=>m.Value[1..^1].Replace("''","'"))).Distinct(StringComparer.Ordinal).ToArray();
        var candidates=new Dictionary<string,string?[]>(StringComparer.Ordinal);
        foreach(var column in metadata.Where(c=>!c.Generated&&(!c.Identity||includeIdentity)))
        {
            if(!casts.ContainsKey(column.Name)){issues.Add(new(column.Name,"InputType",Reason.UnsupportedType));continue;}
            var values=references!=null&&references.TryGetValue(column.Name,out var reference)?new[]{reference}:DraftCandidates(column,literals);
            if(values.Length==0){issues.Add(new(column.Name,"InputValue",Reason.InvalidInput));continue;}
            candidates[column.Name]=values;
        }
        var insert=new Dictionary<string,string?>(StringComparer.Ordinal);var found=false;
        // Bounded proposal search is not a completeness claim. Execution still tests
        // the confirmed plan against the actual constraints and reports exact failures.
        for(var attempt=0;attempt<96;attempt++)
        {
            token.ThrowIfCancellationRequested();var ordinal=0;insert.Clear();
            foreach(var (name,values) in candidates){insert[name]=values[(attempt*(ordinal*2+1)+attempt/values.Length)%values.Length];ordinal++;}
            if(await DraftChecksAccept(table,casts,insert,safeChecks,collations,safeDomains,token)&&await DraftKeysAvailable(table,casts,insert,issues,token)){found=true;break;}
        }
        if(!found)issues.Add(new(null,"InsertCandidate",Reason.InvalidInput));
        var update=new Dictionary<string,string?>(StringComparer.Ordinal);var updated=false;
        foreach(var (name,values) in candidates)
        {
            foreach(var value in values.Where(v=>v!=insert[name]))
            {
                var changed=new Dictionary<string,string?>(insert,StringComparer.Ordinal){[name]=value};
                if(await DraftChecksAccept(table,casts,changed,safeChecks,collations,safeDomains,token)&&await DraftKeysAvailable(table,casts,changed,issues,token))
                {update[name]=value;updated=true;break;}
            }
            if(updated)break;
        }
        if(!updated)issues.Add(new(null,"UpdateCandidate",Reason.InvalidInput));
        return new(insert,update,issues);
    }
    private static string?[] DraftCandidates(DraftColumn column,string[] literals)
    {
        var random=RandomNumberGenerator.GetInt32(1000,1_000_000_000).ToString(CultureInfo.InvariantCulture);
        if(column.Labels.Length>0)return column.Labels.Cast<string?>().ToArray();
        switch(column.Type)
        {
            case "int2":return [RandomNumberGenerator.GetInt32(10,32767).ToString(CultureInfo.InvariantCulture),"0","1","-1","2","10","100"];
            case "int4":case "int8":case "numeric":case "float4":case "float8":
                var numbers=new[]{random,"0","1","-1","2","-2","10","100","1000","-1000"};
                if(column.Type=="numeric"&&column.Modifier>=4)
                {
                    var precision=((column.Modifier-4)>>16)&65535;var encoded=(column.Modifier-4)&2047;var scale=(encoded^1024)-1024;
                    var digits=scale<0?precision:precision-scale;
                    if(digits<=0)return ["0"];if(digits<10)numbers=numbers.Where(n=>n.TrimStart('-').Length<=digits).ToArray();
                    if(scale<0)numbers=numbers.Select(n=>n=="0"?n:n+new string('0',Math.Min(-scale,1000))).ToArray();
                }
                return numbers;
            case "text":case "varchar":case "bpchar":
                var max=column.Modifier>=4?column.Modifier-4:int.MaxValue;
                string Fit(string text)=>string.Concat(text.EnumerateRunes().Take(max).Select(r=>r.ToString()));
                return new[]{Fit("DMV-SYNTHETIC-"+Guid.NewGuid().ToString("N")),Fit("DMV-UPDATED-"+Guid.NewGuid().ToString("N"))}.Concat(literals.Where(l=>l.EnumerateRunes().Count()<=max)).Distinct(StringComparer.Ordinal).Cast<string?>().ToArray();
            case "bool":return ["true","false"];
            case "uuid":return [Guid.NewGuid().ToString(),Guid.NewGuid().ToString()];
            case "bytea":return ["\\x444d562d53594e544845544943","\\x444d562d55504441544544"];
            case "date":return ["2000-01-01","2000-01-02"];
            case "time":return ["12:00:00","12:00:01"];
            case "timetz":return ["12:00:00+00","12:00:01+00"];
            case "timestamp":case "timestamptz":return ["2000-01-01 12:00:00+00","2000-01-02 12:00:00+00"];
            case "interval":return ["1 second","2 seconds"];
            case "json":case "jsonb":return ["{\"synthetic\":1}","{\"synthetic\":2}"];
            case "inet":case "cidr":return ["192.0.2.1/32","192.0.2.2/32"];
            case "macaddr":return ["02:00:00:00:00:01","02:00:00:00:00:02"];
            case "macaddr8":return ["02:00:00:00:00:00:00:01","02:00:00:00:00:00:00:02"];
            case "bit":case "varbit":return column.Modifier>1024?[]:column.Modifier>0?[new string('0',column.Modifier),new string('1',column.Modifier)]:["0","1"];
            case "pg_lsn":return ["0/1","0/2"];
            case "point":return ["(0,0)","(1,1)"];
            case "lseg":case "box":return ["(0,0),(1,1)","(0,0),(2,2)"];
            case "path":return ["[(0,0),(1,1)]","[(0,0),(2,2)]"];
            case "polygon":return ["((0,0),(1,0),(0,1))","((0,0),(2,0),(0,2))"];
            case "circle":return ["<(0,0),1>","<(0,0),2>"];
            default:return [];
        }
    }
    private async Task<bool> DraftChecksAccept(Table table,IReadOnlyDictionary<string,string> casts,IReadOnlyDictionary<string,string?> values,IReadOnlyList<string> checks,IReadOnlyDictionary<string,string> collations,IReadOnlyList<DraftDomainCheck> domains,CancellationToken token)
    {
        if(checks.Count>0)
        {
            await using var command=new NpgsqlCommand("",connection,transaction);var projection=new List<string>();
            foreach(var column in table.Columns.Where(c=>casts.ContainsKey(c.Name)))
            {var name="v"+projection.Count;command.Parameters.AddWithValue(name,NpgsqlDbType.Text,values.TryGetValue(column.Name,out var value)?(object?)value??DBNull.Value:DBNull.Value);projection.Add("CAST(@"+name+" AS "+casts[column.Name]+")"+(collations.TryGetValue(column.Name,out var collation)?" COLLATE "+collation:"")+" AS "+Quote(column.Name));}
            command.CommandText="SELECT "+string.Join(" AND ",checks.Select(c=>"("+c+") IS NOT FALSE"))+" FROM (SELECT "+string.Join(',',projection)+") AS candidate";
            if(!await DraftBoolean(command,token))return false;
        }
        foreach(var domain in domains)
        {
            values.TryGetValue(domain.Column,out var value);
            if(value==null&&domain.Required)return false;
            if(domain.Expression==null)continue;
            // Domain -> base input avoids executing its arbitrary CHECK callbacks.
            // Only separately approved predicates run against a synthetic VALUE.
            var collation=collations.TryGetValue(domain.Column,out var co)?" COLLATE "+co:"";
            await using var command=new NpgsqlCommand("SELECT ("+domain.Expression+") IS NOT FALSE FROM (SELECT CAST(@value AS "+casts[domain.Column]+")"+collation+" AS value) domain_candidate",connection,transaction);
            command.Parameters.AddWithValue("value",NpgsqlDbType.Text,(object?)value??DBNull.Value);
            if(!await DraftBoolean(command,token))return false;
        }
        return true;
    }
    private async Task<List<DraftDomainCheck>> ReadDraftDomainChecks(uint relation,CancellationToken token)
    {
        const string sql="""
            WITH RECURSIVE domains(name,id) AS (
              SELECT a.attname,a.atttypid FROM pg_catalog.pg_attribute a WHERE a.attrelid=@id AND a.attnum>0 AND NOT a.attisdropped
              UNION SELECT d.name,t.typbasetype FROM domains d JOIN pg_catalog.pg_type t ON t.oid=d.id WHERE t.typtype='d')
            SELECT d.name,t.typnotnull,pg_catalog.pg_get_expr(c.conbin,0,false),c.conbin::pg_catalog.text
            FROM domains d JOIN pg_catalog.pg_type t ON t.oid=d.id AND t.typtype='d'
            LEFT JOIN pg_catalog.pg_constraint c ON c.contypid=t.oid AND c.contype='c'
            """;
        var result=new List<DraftDomainCheck>();await using var command=new NpgsqlCommand(GuardDeparsers(sql),connection,transaction);command.Parameters.AddWithValue("id",NpgsqlDbType.Oid,relation);
        await using var reader=await command.ExecuteReaderAsync(token);
        while(await reader.ReadAsync(token))result.Add(new(reader.GetString(0),reader.GetBoolean(1),reader.IsDBNull(2)?null:reader.GetString(2),reader.IsDBNull(3)?null:reader.GetString(3)));
        return result.Distinct().ToList();
    }
    private async Task<bool> DraftKeysAvailable(Table table,IReadOnlyDictionary<string,string> casts,IReadOnlyDictionary<string,string?> values,List<TrialInputIssue> issues,CancellationToken token)
    {
        foreach(var key in table.RowKeys)
        {
            if(key.Any(k=>!values.ContainsKey(k)||!casts.ContainsKey(k)))continue;
            if(key.Any(k=>!SafeIntegrityScalar(table.Columns.Single(c=>c.Name==k))))
            {issues.Add(new(null,"UniqueCandidate",Reason.UnsupportedType));continue;}
            await using var command=new NpgsqlCommand("",connection,transaction);var predicates=new List<string>();
            foreach(var name in key)
            {var parameter="k"+predicates.Count;command.Parameters.AddWithValue(parameter,NpgsqlDbType.Text,(object?)values[name]??DBNull.Value);predicates.Add(Quote(name)+" OPERATOR(pg_catalog.=) CAST(@"+parameter+" AS "+casts[name]+")");}
            command.CommandText="SELECT NOT EXISTS(SELECT 1 FROM "+ReadRelation(table)+" WHERE "+string.Join(" AND ",predicates)+")";
            if(!await DraftBoolean(command,token))return false;
        }
        return true;
    }
    private async Task<bool> DraftBoolean(NpgsqlCommand command,CancellationToken token)
    {
        await using(var save=new NpgsqlCommand("SAVEPOINT dmv_draft_candidate",connection,transaction))await save.ExecuteNonQueryAsync(token);
        try{return await command.ExecuteScalarAsync(token) is true;}
        catch(PostgresException e)when(e.SqlState is "22001" or "22003" or "22P02" or "22012" or "22007" or "22008")
        {await using var restore=new NpgsqlCommand("ROLLBACK TO SAVEPOINT dmv_draft_candidate",connection,transaction);await restore.ExecuteNonQueryAsync(token);return false;}
        finally{await using var release=new NpgsqlCommand("RELEASE SAVEPOINT dmv_draft_candidate",connection,transaction);await release.ExecuteNonQueryAsync(token);}
    }
}
