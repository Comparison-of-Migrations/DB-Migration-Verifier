using System.Globalization;
using System.Text.RegularExpressions;
using Npgsql;
using NpgsqlTypes;
namespace Verifier.Data;

public sealed partial class PostgreSqlDatabase
{
    // PG17 native node header. Arguments are checked by SafeExpression's complete
    // node/function walk. A row comparison executes BTORDER_PROC, not oprcode.
    private static readonly Regex RowComparisonHeader=new(@"\{ROWCOMPAREEXPR :rctype ([1245]) :opnos \(o ([0-9 ]+)\) :opfamilies \(o ([0-9 ]+)\) :inputcollids \(o ([0-9 ]+)\) :largs ",RegexOptions.CultureInvariant);
    private async Task<uint[]?> RowComparisonFunctions(string nodes,int expected,CancellationToken token)
    {
        var headers=RowComparisonHeader.Matches(nodes);if(headers.Count!=expected)return null;
        var operators=new List<uint>();var families=new List<uint>();var strategies=new List<short>();
        foreach(Match header in headers)
        {
            var ops=OidList(header.Groups[2].Value);var groups=OidList(header.Groups[3].Value);var collations=OidList(header.Groups[4].Value);
            if(ops.Length==0||ops.Length!=groups.Length||ops.Length!=collations.Length)return null;
            operators.AddRange(ops);families.AddRange(groups);strategies.AddRange(Enumerable.Repeat(short.Parse(header.Groups[1].Value,CultureInfo.InvariantCulture),ops.Length));
        }
        const string sql="""
            WITH requested AS (SELECT * FROM ROWS FROM (pg_catalog.unnest(@operators),pg_catalog.unnest(@families),pg_catalog.unnest(@strategies)) AS r(operator_id,family_id,strategy))
            SELECT support.amproc::pg_catalog.oid,
                family_namespace.nspname='pg_catalog' AND operator_namespace.nspname='pg_catalog' AND method.amname='btree'
            FROM requested r
            JOIN pg_catalog.pg_operator op ON op.oid=r.operator_id
            JOIN pg_catalog.pg_namespace operator_namespace ON operator_namespace.oid=op.oprnamespace
            JOIN pg_catalog.pg_opfamily family ON family.oid=r.family_id
            JOIN pg_catalog.pg_namespace family_namespace ON family_namespace.oid=family.opfnamespace
            JOIN pg_catalog.pg_am method ON method.oid=family.opfmethod
            JOIN pg_catalog.pg_amop member ON member.amopfamily=family.oid AND member.amopopr=op.oid
                AND member.amoppurpose='s' AND member.amopstrategy=r.strategy AND member.amopmethod=method.oid
            JOIN pg_catalog.pg_amproc support ON support.amprocfamily=family.oid
                AND support.amproclefttype=member.amoplefttype AND support.amprocrighttype=member.amoprighttype AND support.amprocnum=1
            """;
        await using var command=new NpgsqlCommand(sql,connection,transaction);
        command.Parameters.AddWithValue("operators",NpgsqlDbType.Array|NpgsqlDbType.Oid,operators.ToArray());
        command.Parameters.AddWithValue("families",NpgsqlDbType.Array|NpgsqlDbType.Oid,families.ToArray());
        command.Parameters.AddWithValue("strategies",NpgsqlDbType.Array|NpgsqlDbType.Smallint,strategies.ToArray());
        var functions=new List<uint>();await using var reader=await command.ExecuteReaderAsync(token);
        while(await reader.ReadAsync(token)){if(!reader.GetBoolean(1))return null;functions.Add(reader.GetFieldValue<uint>(0));}
        // Missing bindings never certify safety. Common function checks additionally
        // reject user routines and polymorphic comparators (array/record/range).
        return functions.Count==operators.Count?functions.Distinct().ToArray():null;
    }
    private static uint[] OidList(string value)=>value.Split(' ',StringSplitOptions.RemoveEmptyEntries).Select(v=>uint.Parse(v,CultureInfo.InvariantCulture)).ToArray();
}
