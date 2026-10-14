using Npgsql;
using Verifier.Core;
namespace Verifier.Data;

public sealed partial class PostgreSqlDatabase
{
    private async Task<List<Table>> ReadRowKeys(List<Table> tables,CancellationToken token)
    {
        var keys=new Dictionary<string,List<IReadOnlyList<string>>>(StringComparer.Ordinal);
        await using(var save=new NpgsqlCommand("SAVEPOINT dmv_row_keys",connection,transaction))await save.ExecuteNonQueryAsync(token);
        try
        {
            const string sql="""
                SELECT n.nspname,c.relname,
                  ARRAY(SELECT a.attname::pg_catalog.text FROM pg_catalog.unnest(i.indkey) WITH ORDINALITY k(attnum,position)
                    JOIN pg_catalog.pg_attribute a ON a.attrelid=c.oid AND a.attnum=k.attnum AND NOT a.attisdropped
                    WHERE k.position<=i.indnkeyatts ORDER BY k.position)
                FROM pg_catalog.pg_index i JOIN pg_catalog.pg_class c ON c.oid=i.indrelid
                JOIN pg_catalog.pg_namespace n ON n.oid=c.relnamespace
                WHERE i.indisunique AND i.indisvalid AND i.indisready AND i.indislive
                  AND i.indpred IS NULL AND i.indexprs IS NULL AND i.indnkeyatts>0
                  AND pg_catalog.left(n.nspname,3)<>'pg_' AND n.nspname<>'information_schema'
                ORDER BY n.nspname,c.relname,i.indisprimary DESC,i.indnkeyatts,i.indexrelid
                """;
            await using(var command=new NpgsqlCommand(sql,connection,transaction))
            {
                await using var reader=await command.ExecuteReaderAsync(token);
                while(await reader.ReadAsync(token))
                {
                    var table=new Table(reader.GetString(0),reader.GetString(1),[],false).Qualified;
                    if(!keys.TryGetValue(table,out var list))keys[table]=list=[];
                    list.Add(reader.GetFieldValue<string[]>(2));
                }
            }
        }
        catch(PostgresException e)when(e.SqlState=="42501")
        {
            await using var restore=new NpgsqlCommand("ROLLBACK TO SAVEPOINT dmv_row_keys",connection,transaction);await restore.ExecuteNonQueryAsync(token);
            // Key discovery is an optimization; exact multiset comparison needs no declared key.
            keys.Clear();
        }
        await using(var release=new NpgsqlCommand("RELEASE SAVEPOINT dmv_row_keys",connection,transaction))await release.ExecuteNonQueryAsync(token);
        return tables.Select(t=>t with{RowKeys=keys.GetValueOrDefault(t.Qualified)??[]}).ToList();
    }
}
