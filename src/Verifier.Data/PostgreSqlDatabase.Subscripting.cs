using System.Globalization;
using System.Text.RegularExpressions;
using Npgsql;
using NpgsqlTypes;
namespace Verifier.Data;

public sealed partial class PostgreSqlDatabase
{
    // PG17 chooses fetch callbacks through pg_type.typsubscript, not FUNCEXPR.
    // Check every native binding before allowing the common expression walker
    // to inspect index/input functions. Assignment nodes remain unsupported.
    private static readonly Regex SubscriptHeader=new(@"\{SUBSCRIPTINGREF :refcontainertype ([0-9]+) :refelemtype ([0-9]+) :refrestype ([0-9]+) :reftypmod -?[0-9]+ :refcollid [0-9]+ :refupperindexpr ",RegexOptions.CultureInvariant);
    private async Task<bool> SafeSubscripts(string nodes,int expected,CancellationToken token)
    {
        var headers=SubscriptHeader.Matches(nodes);
        if(headers.Count!=expected||Regex.Matches(nodes,@":refassgnexpr <>",RegexOptions.CultureInvariant).Count!=expected)return false;
        const string sql="""
            WITH requested AS (SELECT * FROM ROWS FROM (pg_catalog.unnest(@containers),pg_catalog.unnest(@elements),pg_catalog.unnest(@results)) AS r(container,element,result))
            SELECT count(*)=@count AND COALESCE(bool_and(
                (t.typtype='b' AND t.typcategory='A' AND t.typlen=-1 AND t.typelem=r.element AND element_type.typarray=t.oid
                    AND r.result IN (t.oid,t.typelem) AND t.typsubscript='pg_catalog.array_subscript_handler'::pg_catalog.regproc)
                OR (t.oid='pg_catalog.jsonb'::pg_catalog.regtype AND t.typelem=0 AND r.element=0 AND r.result=t.oid
                    AND t.typsubscript='pg_catalog.jsonb_subscript_handler'::pg_catalog.regproc)),false)
            FROM requested r JOIN pg_catalog.pg_type t ON t.oid=r.container
            LEFT JOIN pg_catalog.pg_type element_type ON element_type.oid=t.typelem
            """;
        await using var command=new NpgsqlCommand(sql,connection,transaction);
        uint[] Field(int n)=>headers.Select(h=>uint.Parse(h.Groups[n].Value,CultureInfo.InvariantCulture)).ToArray();
        command.Parameters.AddWithValue("containers",NpgsqlDbType.Array|NpgsqlDbType.Oid,Field(1));
        command.Parameters.AddWithValue("elements",NpgsqlDbType.Array|NpgsqlDbType.Oid,Field(2));
        command.Parameters.AddWithValue("results",NpgsqlDbType.Array|NpgsqlDbType.Oid,Field(3));command.Parameters.AddWithValue("count",expected);
        return await command.ExecuteScalarAsync(token) is true;
    }
}
