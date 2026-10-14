using System.Globalization;
using System.Text.Json;
using System.Text.RegularExpressions;
using Npgsql;
using Verifier.Core;
namespace Verifier.Data;
public sealed partial class PostgreSqlDatabase
{
    private Reason deparseLimit;
    private readonly HashSet<uint> unsafeOutputTypes=[];
    private readonly HashSet<uint> knownOutputTypes=[];
    private readonly HashSet<uint> unsafeTypmodTypes=[];
    // Never compared or exported. A blocked definition is missing evidence, not SQL NULL.
    private const string BlockedDefinition="DMV-BLOCKED-DEFINITION-43a14244-2d0b-476e-9e50-877683351d95";
    private static readonly Regex NonNullConstants=new(@"\{CONST :consttype ([0-9]+)[^{}]*:constisnull false",RegexOptions.CultureInvariant);
    private static readonly HashSet<string> DeparsedAttributes=new(StringComparer.Ordinal)
    {"ColumnType","SequenceDefinitions","ColumnDefault","ConstraintDefinition","PartitionKey","PartitionBound","PartitionChildren","IndexDefinition","RowPolicies","TriggerDefinitions","RelatedRoutines","RelatedViews","UserTypeDefinitions"};
    private async Task InspectDeparseSafety(CancellationToken token)
    {
        // Native deparsers call constant output functions. Inspect their transitive
        // type graph without invoking any output callback or reading user rows.
        const string sql="""
            WITH RECURSIVE edges(id,dependency) AS (
                SELECT oid,typbasetype FROM pg_catalog.pg_type WHERE typbasetype<>0
                UNION SELECT oid,typelem FROM pg_catalog.pg_type WHERE typelem<>0
                UNION SELECT t.oid,a.atttypid FROM pg_catalog.pg_type t JOIN pg_catalog.pg_attribute a ON a.attrelid=t.typrelid AND a.attnum>0 AND NOT a.attisdropped
                UNION SELECT rngtypid,rngsubtype FROM pg_catalog.pg_range
                UNION SELECT rngmultitypid,rngtypid FROM pg_catalog.pg_range
            ), output_graph(root,id) AS (SELECT oid,oid FROM pg_catalog.pg_type
                UNION SELECT g.root,e.dependency FROM output_graph g JOIN edges e ON e.id=g.id)
            SELECT g.root,bool_or(pn.nspname IS DISTINCT FROM 'pg_catalog' OR pl.lanname IS DISTINCT FROM 'internal' OR t.typtype='p'
                OR n.nspname<>'pg_catalog' AND t.typtype='b' AND t.typelem=0),
                bool_or(t.typmodout<>0 AND (mn.nspname IS DISTINCT FROM 'pg_catalog' OR ml.lanname IS DISTINCT FROM 'internal'))
            FROM output_graph g JOIN pg_catalog.pg_type t ON t.oid=g.id
            JOIN pg_catalog.pg_namespace n ON n.oid=t.typnamespace
            LEFT JOIN pg_catalog.pg_proc p ON p.oid=t.typoutput LEFT JOIN pg_catalog.pg_namespace pn ON pn.oid=p.pronamespace
            LEFT JOIN pg_catalog.pg_language pl ON pl.oid=p.prolang
            LEFT JOIN pg_catalog.pg_proc mp ON mp.oid=t.typmodout LEFT JOIN pg_catalog.pg_namespace mn ON mn.oid=mp.pronamespace LEFT JOIN pg_catalog.pg_language ml ON ml.oid=mp.prolang
            GROUP BY g.root
            """;
        await using(var save=new NpgsqlCommand("SAVEPOINT dmv_deparse_safety",connection,transaction))await save.ExecuteNonQueryAsync(token);
        try
        {
            await using var command=new NpgsqlCommand(sql,connection,transaction);await using var reader=await command.ExecuteReaderAsync(token);
            while(await reader.ReadAsync(token)){var id=reader.GetFieldValue<uint>(0);knownOutputTypes.Add(id);if(reader.GetBoolean(1))unsafeOutputTypes.Add(id);if(reader.GetBoolean(2))unsafeTypmodTypes.Add(id);}
        }
        catch(PostgresException e)when(e.SqlState=="42501")
        {await using var restore=new NpgsqlCommand("ROLLBACK TO SAVEPOINT dmv_deparse_safety",connection,transaction);await restore.ExecuteNonQueryAsync(token);deparseLimit=Reason.PermissionDenied;}
        await using var release=new NpgsqlCommand("RELEASE SAVEPOINT dmv_deparse_safety",connection,transaction);await release.ExecuteNonQueryAsync(token);
    }
    private static readonly Regex NodeTypes=new(@":[A-Za-z_]*(?:type|typeid|typeId) ([0-9]+)",RegexOptions.CultureInvariant);
    private bool CanDeparseNodes(string? nodes)
    {
        if(deparseLimit!=Reason.None)return false;
        if(nodes==null)return true;
        foreach(Match match in NonNullConstants.Matches(nodes))
        {
            var id=uint.Parse(match.Groups[1].Value,CultureInfo.InvariantCulture);
            if(!knownOutputTypes.Contains(id)||unsafeOutputTypes.Contains(id))return false;
        }
        return !NodeTypes.Matches(nodes).Any(m=>unsafeTypmodTypes.Contains(uint.Parse(m.Groups[1].Value,CultureInfo.InvariantCulture)));
    }
    private static bool ContainsBlockedDefinition(JsonElement value)=>value.ValueKind switch
    {
        JsonValueKind.String=>value.GetString()?.StartsWith(BlockedDefinition,StringComparison.Ordinal)==true,
        JsonValueKind.Array=>value.EnumerateArray().Any(ContainsBlockedDefinition),
        JsonValueKind.Object=>value.EnumerateObject().Any(p=>ContainsBlockedDefinition(p.Value)),
        _=>false
    };
    private static readonly Regex NativeDeparsers=new(@"pg_catalog\.(?:pg_get_(expr|constraintdef|indexdef|partkeydef|triggerdef|functiondef|viewdef)|(format_type))\(",RegexOptions.CultureInvariant);
    private string GuardDeparsers(string sql)
    {
        foreach(var match in NativeDeparsers.Matches(sql).Cast<Match>().Reverse())
        {
            var start=match.Index+match.Length;var cursor=start;var depth=1;char quote='\0';var firstEnd=-1;
            for(;cursor<sql.Length&&depth>0;cursor++)
            {
                var ch=sql[cursor];
                if(quote!='\0'){if(ch==quote){if(cursor+1<sql.Length&&sql[cursor+1]==quote)cursor++;else quote='\0';}continue;}
                if(ch is '\'' or '"')quote=ch;else if(ch=='(')depth++;else if(ch==')')depth--;else if(ch==','&&depth==1&&firstEnd<0)firstEnd=cursor;
            }
            if(depth!=0)throw new VerificationException(Reason.ObservationChanged);
            var call=sql[match.Index..cursor];var arg=sql[start..(firstEnd<0?cursor-1:firstEnd)];
            var kind=match.Groups[1].Success?match.Groups[1].Value:match.Groups[2].Value;
            var condition=kind=="format_type"?FormatTypeCondition(arg,sql[(firstEnd+1)..(cursor-1)]):DeparseCondition(kind,arg);
            var blocked=deparseLimit!=Reason.None?"'"+BlockedDefinition+"'::pg_catalog.text":BlockedEvidence(kind,arg);
            var guarded=deparseLimit!=Reason.None?"'"+BlockedDefinition+"'::pg_catalog.text":"(CASE WHEN "+condition+" THEN "+call+" ELSE "+blocked+" END)";
            sql=sql[..match.Index]+guarded+sql[cursor..];
        }
        return sql;
    }
    private string BlockedEvidence(string kind,string arg)
    {
        // Exact local metadata is retained only to invalidate stale write consent.
        // It is not compared across DBs, evaluated, logged, or exported.
        var table=kind switch{"constraintdef"=>"pg_constraint","indexdef"=>"pg_index","partkeydef"=>"pg_partitioned_table","triggerdef"=>"pg_trigger","functiondef"=>"pg_proc","viewdef"=>"pg_rewrite",_=>null};
        if(table==null)return "('"+BlockedDefinition+"'||pg_catalog.to_jsonb(("+arg+")::pg_catalog.text)::pg_catalog.text)";
        var key=kind switch{"indexdef"=>"indexrelid","partkeydef"=>"partrelid","viewdef"=>"ev_class",_=>"oid"};
        return "('"+BlockedDefinition+"'||COALESCE((SELECT pg_catalog.jsonb_agg(pg_catalog.to_jsonb(dmv_e) ORDER BY pg_catalog.to_jsonb(dmv_e)::pg_catalog.text COLLATE pg_catalog.\"C\")::pg_catalog.text FROM pg_catalog."+table+" dmv_e WHERE dmv_e."+key+"=("+arg+")),'null'))";
    }
    private string UnsafeTypmods()=>"ARRAY["+string.Join(',',unsafeTypmodTypes.Order().Select(id=>id.ToString(CultureInfo.InvariantCulture)))+"]::pg_catalog.oid[]";
    private string FormatTypeCondition(string type,string modifier)=>"("+modifier+") IS NULL OR ("+modifier+")<0 OR NOT (("+type+")=ANY("+UnsafeTypmods()+"))";
    private string DeparseCondition(string kind,string arg)
    {
        var trees=kind switch
        {
            "expr"=>"SELECT ("+arg+")::pg_catalog.text",
            "constraintdef"=>"SELECT dmv_c.conbin::pg_catalog.text FROM pg_catalog.pg_constraint dmv_c WHERE dmv_c.oid=("+arg+") UNION ALL SELECT dmv_i.indexprs::pg_catalog.text FROM pg_catalog.pg_constraint dmv_c JOIN pg_catalog.pg_index dmv_i ON dmv_i.indexrelid=dmv_c.conindid WHERE dmv_c.oid=("+arg+") UNION ALL SELECT dmv_i.indpred::pg_catalog.text FROM pg_catalog.pg_constraint dmv_c JOIN pg_catalog.pg_index dmv_i ON dmv_i.indexrelid=dmv_c.conindid WHERE dmv_c.oid=("+arg+")",
            "indexdef"=>"SELECT dmv_i.indexprs::pg_catalog.text FROM pg_catalog.pg_index dmv_i WHERE dmv_i.indexrelid=("+arg+") UNION ALL SELECT dmv_i.indpred::pg_catalog.text FROM pg_catalog.pg_index dmv_i WHERE dmv_i.indexrelid=("+arg+")",
            "partkeydef"=>"SELECT dmv_p.partexprs::pg_catalog.text FROM pg_catalog.pg_partitioned_table dmv_p WHERE dmv_p.partrelid=("+arg+")",
            "triggerdef"=>"SELECT dmv_t.tgqual::pg_catalog.text FROM pg_catalog.pg_trigger dmv_t WHERE dmv_t.oid=("+arg+")",
            "functiondef"=>"SELECT dmv_p.proargdefaults::pg_catalog.text FROM pg_catalog.pg_proc dmv_p WHERE dmv_p.oid=("+arg+") UNION ALL SELECT dmv_p.prosqlbody::pg_catalog.text FROM pg_catalog.pg_proc dmv_p WHERE dmv_p.oid=("+arg+")",
            "viewdef"=>"SELECT dmv_r.ev_action::pg_catalog.text FROM pg_catalog.pg_rewrite dmv_r WHERE dmv_r.ev_class=("+arg+") UNION ALL SELECT dmv_r.ev_qual::pg_catalog.text FROM pg_catalog.pg_rewrite dmv_r WHERE dmv_r.ev_class=("+arg+")",
            _=>throw new VerificationException(Reason.NotImplemented)
        };
        // Only numeric catalog OIDs are embedded. The original row expression is
        // retained in a CASE branch, so PostgreSQL cannot fold a constant callback.
        var ids=string.Join(',',unsafeOutputTypes.Order().Select(id=>id.ToString(CultureInfo.InvariantCulture)));
        var outputs="NOT EXISTS(SELECT 1 FROM ("+trees+") dmv_trees(tree) CROSS JOIN LATERAL pg_catalog.regexp_matches(dmv_trees.tree,'\\{CONST :consttype ([0-9]+)[^{}]*:constisnull false','g') dmv_const(parts) WHERE dmv_const.parts[1]::pg_catalog.oid=ANY(ARRAY["+ids+"]::pg_catalog.oid[]) OR NOT EXISTS(SELECT 1 FROM pg_catalog.pg_type dmv_type WHERE dmv_type.oid=dmv_const.parts[1]::pg_catalog.oid))";
        if(unsafeTypmodTypes.Count==0)return outputs;
        return "("+outputs+" AND NOT EXISTS(SELECT 1 FROM ("+trees+") dmv_trees(tree) CROSS JOIN LATERAL pg_catalog.regexp_matches(dmv_trees.tree,':[A-Za-z_]*(type|typeid|typeId) ([0-9]+)','g') dmv_type(parts) WHERE dmv_type.parts[2]::pg_catalog.oid=ANY("+UnsafeTypmods()+")))";
    }
}
