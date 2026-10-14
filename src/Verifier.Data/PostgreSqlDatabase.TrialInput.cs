using System.Collections.Immutable;
using Npgsql;
namespace Verifier.Data;

public sealed partial class PostgreSqlDatabase
{
    private async Task<ImmutableDictionary<string,string>> ReadTrialCasts(Verifier.Core.Table table,CancellationToken token)
    {
        // A native array input function delegates to the element's input function.
        // Verify the whole input graph; domain elements can run arbitrary CHECKs
        // during parsing, even in read-only draft preparation. Keep those blocked.
        const string sql="""
            WITH RECURSIVE base(column_name,id) AS (
              SELECT a.attname,a.atttypid FROM pg_catalog.pg_attribute a JOIN pg_catalog.pg_class c ON c.oid=a.attrelid
                JOIN pg_catalog.pg_namespace n ON n.oid=c.relnamespace WHERE n.nspname=@schema AND c.relname=@table AND a.attnum>0 AND NOT a.attisdropped
              UNION ALL SELECT b.column_name,t.typbasetype FROM base b JOIN pg_catalog.pg_type t ON t.oid=b.id WHERE t.typtype='d'
            ), roots AS (SELECT b.* FROM base b JOIN pg_catalog.pg_type t ON t.oid=b.id WHERE t.typtype<>'d'),
            inputs(root,id) AS (SELECT id,id FROM roots UNION SELECT i.root,t.typelem FROM inputs i JOIN pg_catalog.pg_type t ON t.oid=i.id WHERE t.typelem<>0),
            safety AS (
              SELECT i.root,COALESCE(bool_and(pn.nspname='pg_catalog' AND l.lanname='internal' AND
                CASE WHEN t.typelem<>0 THEN t.typtype='b' AND t.typcategory='A' AND t.typlen=-1
                    AND element_type.typarray=t.oid AND p.proname='array_in'
                  ELSE (n.nspname='pg_catalog' AND t.typname=ANY(@scalars)) OR (t.typtype='e' AND p.proname='enum_in') END),false) AS allowed
              FROM inputs i JOIN pg_catalog.pg_type t ON t.oid=i.id JOIN pg_catalog.pg_namespace n ON n.oid=t.typnamespace
              LEFT JOIN pg_catalog.pg_type element_type ON element_type.oid=t.typelem
              LEFT JOIN pg_catalog.pg_proc p ON p.oid=t.typinput LEFT JOIN pg_catalog.pg_namespace pn ON pn.oid=p.pronamespace
              LEFT JOIN pg_catalog.pg_language l ON l.oid=p.prolang GROUP BY i.root)
            SELECT b.column_name,n.nspname,t.typname,s.allowed FROM roots b JOIN pg_catalog.pg_type t ON t.oid=b.id
              JOIN pg_catalog.pg_namespace n ON n.oid=t.typnamespace JOIN safety s ON s.root=t.oid
            """;
        var result=ImmutableDictionary.CreateBuilder<string,string>(StringComparer.Ordinal);
        await using var command=new NpgsqlCommand(sql,connection,transaction);command.Parameters.AddWithValue("schema",table.Schema);command.Parameters.AddWithValue("table",table.Name);command.Parameters.AddWithValue("scalars",Supported.Concat(BinaryCodecs.Keys).Distinct().ToArray());
        await using var reader=await command.ExecuteReaderAsync(token);
        while(await reader.ReadAsync(token))
        {
            var name=reader.GetString(0);
            if(reader.GetBoolean(3)&&table.Columns.Any(c=>c.Name==name&&c.SafeValue))result[name]=Quote(reader.GetString(1))+"."+Quote(reader.GetString(2));
        }
        return result.ToImmutable();
    }
}
