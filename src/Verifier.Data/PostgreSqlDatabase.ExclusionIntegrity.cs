using Npgsql;
using NpgsqlTypes;
using Verifier.Core;
namespace Verifier.Data;

public sealed partial class PostgreSqlDatabase
{
    private async Task ObserveExclusion(Table table,uint index,List<ScopeIssue> issues,Func<string,string?,string,Task> count,CancellationToken token)
    {
        string? expression=null,predicate=null;var operators=new List<(string Name,bool Safe)>();
        const string sql="""
            SELECT i.indexprs::pg_catalog.text,i.indpred::pg_catalog.text,o.oprname,
              onsp.nspname='pg_catalog' AND pn.nspname='pg_catalog' AND l.lanname='internal' AND p.provolatile='i'
                AND tn.nspname='pg_catalog' AND t.typname=ANY(@types)
            FROM pg_catalog.pg_constraint c JOIN pg_catalog.pg_index i ON i.indexrelid=c.conindid
            CROSS JOIN LATERAL pg_catalog.unnest(c.conexclop) WITH ORDINALITY x(id,pos)
            JOIN pg_catalog.pg_operator o ON o.oid=x.id JOIN pg_catalog.pg_namespace onsp ON onsp.oid=o.oprnamespace
            JOIN pg_catalog.pg_proc p ON p.oid=o.oprcode JOIN pg_catalog.pg_namespace pn ON pn.oid=p.pronamespace
            JOIN pg_catalog.pg_language l ON l.oid=p.prolang
            JOIN pg_catalog.pg_attribute a ON a.attrelid=i.indexrelid AND a.attnum=x.pos
            JOIN pg_catalog.pg_type t ON t.oid=a.atttypid JOIN pg_catalog.pg_namespace tn ON tn.oid=t.typnamespace
            WHERE c.conindid=@id AND c.contype='x' ORDER BY x.pos
            """;
        await using(var command=new NpgsqlCommand(sql,connection,transaction))
        {
            command.Parameters.AddWithValue("id",NpgsqlDbType.Oid,index);
            // Polymorphic range operators call the subtype comparator. Only built-in ranges
            // are approved here; arrays, records and user ranges may invoke user code.
            command.Parameters.AddWithValue("types",Supported.Concat(BinaryCodecs.Keys).Concat(new[]{"int4range","int8range","numrange","tsrange","tstzrange","daterange","int4multirange","int8multirange","nummultirange","tsmultirange","tstzmultirange","datemultirange"}).ToArray());
            await using var reader=await command.ExecuteReaderAsync(token);
            while(await reader.ReadAsync(token))
            {expression=reader.IsDBNull(0)?null:reader.GetString(0);predicate=reader.IsDBNull(1)?null:reader.GetString(1);operators.Add((reader.GetString(2),reader.GetBoolean(3)));}
        }
        if(operators.Count==0||operators.Any(o=>!o.Safe||!System.Text.RegularExpressions.Regex.IsMatch(o.Name,@"^[+*/<>=~!@#%^&|?\-]+$")||o.Name.Contains("--",StringComparison.Ordinal)||o.Name.Contains("/*",StringComparison.Ordinal))
            ||!CanDeparseNodes(expression)||!CanDeparseNodes(predicate)||expression!=null&&!await SafeExpression(expression,token)||predicate!=null&&!await SafeExpression(predicate,token))
        {issues.Add(new(table.Qualified,null,Reason.NotImplemented));return;}
        var keys=new List<string>();string? filter=null;
        await using(var command=new NpgsqlCommand("SELECT pg_catalog.pg_get_indexdef(i.indexrelid,k.pos::pg_catalog.int4,true),cn.nspname,co.collname,pg_catalog.pg_get_expr(i.indpred,i.indrelid,false) FROM pg_catalog.pg_index i CROSS JOIN pg_catalog.generate_series(1,i.indnkeyatts) k(pos) LEFT JOIN pg_catalog.pg_collation co ON co.oid=i.indcollation[k.pos-1] LEFT JOIN pg_catalog.pg_namespace cn ON cn.oid=co.collnamespace WHERE i.indexrelid=@id ORDER BY k.pos",connection,transaction))
        {
            command.Parameters.AddWithValue("id",NpgsqlDbType.Oid,index);await using var reader=await command.ExecuteReaderAsync(token);
            while(await reader.ReadAsync(token))
            {var key="("+reader.GetString(0)+")";if(!reader.IsDBNull(1))key+=" COLLATE "+Quote(reader.GetString(1))+"."+Quote(reader.GetString(2));keys.Add(key);if(!reader.IsDBNull(3))filter=reader.GetString(3);}
        }
        if(keys.Count!=operators.Count){issues.Add(new(table.Qualified,null,Reason.ObservationChanged));return;}
        var names=Enumerable.Range(0,keys.Count).Select(i=>Quote("key"+i)).ToArray();
        var projection=string.Join(',',keys.Select((k,i)=>k+" AS "+names[i]));
        var compare=string.Join(" AND ",operators.Select((o,i)=>"a."+names[i]+" OPERATOR(pg_catalog."+o.Name+") b."+names[i]));
        // A physical tuple pair is counted once. NULL/false on any operator is not a violation.
        var query="WITH observed AS MATERIALIZED (SELECT ctid AS row_id,"+projection+" FROM "+ReadRelation(table)+(filter==null?"":" WHERE ("+filter+")")+") SELECT count(*) FROM observed a JOIN observed b ON a.row_id OPERATOR(pg_catalog.<) b.row_id AND ("+compare+")";
        await count("Exclusion",null,query);
    }
}
