using Npgsql;
using NpgsqlTypes;
using Verifier.Core;
namespace Verifier.Data;
public sealed partial class PostgreSqlDatabase
{
    private async Task ObserveUniqueIndexes(Table table,List<ScopeIssue> issues,Func<string,string?,string,Task> count,CancellationToken token)
    {
        const string sql="""
            SELECT i.indexrelid,i.indexprs::pg_catalog.text,i.indpred::pg_catalog.text,i.indnullsnotdistinct,
              ARRAY(SELECT a.attname::pg_catalog.text FROM pg_catalog.unnest(i.indkey) WITH ORDINALITY k(num,pos)
                JOIN pg_catalog.pg_attribute a ON a.attrelid=i.indrelid AND a.attnum=k.num WHERE pos<=i.indnkeyatts ORDER BY pos),
              EXISTS(SELECT 1 FROM pg_catalog.unnest(i.indclass) cl JOIN pg_catalog.pg_opclass op ON op.oid=cl JOIN pg_catalog.pg_namespace n ON n.oid=op.opcnamespace
                WHERE n.nspname<>'pg_catalog' OR op.opcname IN ('array_ops','record_ops','range_ops','multirange_ops'))
            FROM pg_catalog.pg_index i JOIN pg_catalog.pg_class t ON t.oid=i.indrelid JOIN pg_catalog.pg_namespace n ON n.oid=t.relnamespace
            WHERE n.nspname=@schema AND t.relname=@table AND i.indisunique
              AND NOT EXISTS(SELECT 1 FROM pg_catalog.pg_constraint c WHERE c.conindid=i.indexrelid AND c.contype IN ('p','u'))
            """;
        var indexes=new List<(uint Id,string? Expression,string? Predicate,bool NullsEqual,string[] Columns,bool Unsafe)>();
        await using(var command=new NpgsqlCommand(sql,connection,transaction))
        {
            command.Parameters.AddWithValue("schema",table.Schema);command.Parameters.AddWithValue("table",table.Name);
            await using var reader=await command.ExecuteReaderAsync(token);
            while(await reader.ReadAsync(token))indexes.Add((reader.GetFieldValue<uint>(0),reader.IsDBNull(1)?null:reader.GetString(1),reader.IsDBNull(2)?null:reader.GetString(2),reader.GetBoolean(3),reader.GetFieldValue<string[]>(4),reader.GetBoolean(5)));
        }
        foreach(var index in indexes)
        {
            if(!CanDeparseNodes(index.Expression)||!CanDeparseNodes(index.Predicate)||index.Unsafe||index.Columns.Any(n=>!SafeIntegrityScalar(table.Columns.Single(c=>c.Name==n)))||index.Expression!=null&&!await SafeExpression(index.Expression,token)||index.Predicate!=null&&!await SafeExpression(index.Predicate,token))
            {issues.Add(new(table.Qualified,null,Reason.NotImplemented));continue;}
            var keys=new List<string>();string? predicate;
            await using(var command=new NpgsqlCommand("SELECT pg_catalog.pg_get_indexdef(i.indexrelid,k.pos::pg_catalog.int4,true),cn.nspname,co.collname,pg_catalog.pg_get_expr(i.indpred,i.indrelid,false) FROM pg_catalog.pg_index i CROSS JOIN pg_catalog.generate_series(1,i.indnkeyatts) k(pos) LEFT JOIN pg_catalog.pg_collation co ON co.oid=i.indcollation[k.pos-1] LEFT JOIN pg_catalog.pg_namespace cn ON cn.oid=co.collnamespace WHERE i.indexrelid=@id ORDER BY k.pos",connection,transaction))
            {
                command.Parameters.AddWithValue("id",NpgsqlDbType.Oid,index.Id);await using var reader=await command.ExecuteReaderAsync(token);predicate=null;
                while(await reader.ReadAsync(token))
                {var key="("+reader.GetString(0)+")";if(!reader.IsDBNull(1))key+=" COLLATE "+Quote(reader.GetString(1))+"."+Quote(reader.GetString(2));keys.Add(key);if(!reader.IsDBNull(3))predicate=reader.GetString(3);}
            }
            if(keys.Count==0){issues.Add(new(table.Qualified,null,Reason.ObservationChanged));continue;}
            var names=Enumerable.Range(0,keys.Count).Select(i=>Quote("key"+i)).ToArray();
            var projection=string.Join(',',keys.Select((key,i)=>key+" AS "+names[i]));
            var observed="SELECT "+projection+" FROM "+ReadRelation(table)+(predicate==null?"":" WHERE ("+predicate+")");
            var present=index.NullsEqual?"":" WHERE "+string.Join(" AND ",names.Select(n=>"pg_catalog.num_nonnulls("+n+")=1"));
            await count("Unique",index.Expression==null&&index.Columns.Length==1?index.Columns[0]:null,"SELECT COALESCE(sum(n-1),0)::pg_catalog.int8 FROM (SELECT count(*) n FROM ("+observed+") keys"+present+" GROUP BY "+string.Join(',',names)+" HAVING count(*)>1) duplicate_groups");
        }
    }
    private bool SafeIntegrityScalar(Column column)
    {
        if(!column.SafeValue)return false;
        if(Supported.Contains(column.Type)||BinaryCodecs.ContainsKey(column.Type))return true;
        return customValueCodecs.TryGetValue(column.TypeOid,out var codec)&&codec.ArrayElement==0&&codec.Fields==null&&codec.Range==null&&codec.Send!="enum_send";
    }
    private async Task GuardIntegrity(Table table,List<ScopeIssue> issues,Func<Task> observe,CancellationToken token)
    {
        await using(var save=new NpgsqlCommand("SAVEPOINT dmv_integrity_observer",connection,transaction))await save.ExecuteNonQueryAsync(token);
        try{await observe();}
        catch(PostgresException e)
        {
            if(Map(e).Reason is Reason.Timeout or Reason.ConnectionLost)throw ReadFailure(e);
            await using var restore=new NpgsqlCommand("ROLLBACK TO SAVEPOINT dmv_integrity_observer",connection,transaction);await restore.ExecuteNonQueryAsync(token);
            issues.Add(new(table.Qualified,null,Map(e).Reason));
        }
        await using var release=new NpgsqlCommand("RELEASE SAVEPOINT dmv_integrity_observer",connection,transaction);await release.ExecuteNonQueryAsync(token);
    }
}
