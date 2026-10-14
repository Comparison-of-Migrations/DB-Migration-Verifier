using Npgsql;
using NpgsqlTypes;
using Verifier.Core;
namespace Verifier.Data;

public sealed partial class PostgreSqlDatabase
{
    private IReadOnlyDictionary<uint,string> readShapes=new Dictionary<uint,string>();
    private readonly HashSet<string> readLocks=new(StringComparer.Ordinal);
    private readonly HashSet<string> materializedRelations=new(StringComparer.Ordinal);
    private NpgsqlConnection? readObserver;
    // Only fixed native catalog columns: never attmissingval, user value output,
    // expression deparsers, views, routines, or constraint evaluation.
    private const string ReadShapeSql="""
        WITH RECURSIVE roots(id) AS(SELECT oid FROM pg_catalog.pg_class WHERE oid=ANY(@ids)),
        rels(root,id) AS(SELECT id,id FROM roots UNION SELECT r.root,i.inhrelid FROM rels r JOIN pg_catalog.pg_inherits i ON i.inhparent=r.id),
        edges(parent,id) AS(
          SELECT t.oid,x.id FROM pg_catalog.pg_type t CROSS JOIN LATERAL(VALUES(t.typelem),(t.typbasetype)) x(id) WHERE x.id<>0
          UNION SELECT t.oid,a.atttypid FROM pg_catalog.pg_type t JOIN pg_catalog.pg_attribute a ON a.attrelid=t.typrelid AND a.attnum>0 AND NOT a.attisdropped
          UNION SELECT r.rngtypid,r.rngsubtype FROM pg_catalog.pg_range r
          UNION SELECT r.rngmultitypid,r.rngsubtype FROM pg_catalog.pg_range r WHERE r.rngmultitypid<>0
        ), types(root,id) AS(
          SELECT r.root,a.atttypid FROM rels r JOIN pg_catalog.pg_attribute a ON a.attrelid=r.id AND a.attnum>0 AND NOT a.attisdropped
          UNION SELECT t.root,e.id FROM types t JOIN edges e ON e.parent=t.id
        )
        SELECT root.id,pg_catalog.jsonb_build_array(
          (SELECT pg_catalog.jsonb_agg(pg_catalog.jsonb_build_array(c.oid,n.nspname,c.relname,c.relkind,c.relam,c.relfilenode,
             c.relrowsecurity,c.relforcerowsecurity,c.relispopulated,c.relpersistence,pg_catalog.has_table_privilege(c.oid,'SELECT'),
             (SELECT pg_catalog.jsonb_agg(pg_catalog.jsonb_build_array(a.attname,a.attnum,a.atttypid,a.atttypmod,a.attnotnull,a.attisdropped,
                 a.attgenerated,a.attidentity,a.atthasmissing,a.attcollation) ORDER BY a.attnum)
               FROM pg_catalog.pg_attribute a WHERE a.attrelid=c.oid AND a.attnum>0),
             (SELECT pg_catalog.jsonb_agg(pg_catalog.jsonb_build_array(d.adnum,d.adbin::pg_catalog.text) ORDER BY d.adnum)
               FROM pg_catalog.pg_attrdef d WHERE d.adrelid=c.oid),
             (SELECT pg_catalog.jsonb_agg(pg_catalog.to_jsonb(k) ORDER BY k.oid) FROM pg_catalog.pg_constraint k WHERE k.conrelid=c.oid),
             (SELECT pg_catalog.jsonb_agg(pg_catalog.to_jsonb(i) ORDER BY i.indexrelid) FROM pg_catalog.pg_index i WHERE i.indrelid=c.oid)) ORDER BY c.oid)
            FROM rels r JOIN pg_catalog.pg_class c ON c.oid=r.id JOIN pg_catalog.pg_namespace n ON n.oid=c.relnamespace WHERE r.root=root.id),
          (SELECT pg_catalog.jsonb_agg(pg_catalog.jsonb_build_array(pg_catalog.to_jsonb(t),
              (SELECT pg_catalog.jsonb_agg(pg_catalog.jsonb_build_array(e.oid,e.enumsortorder,e.enumlabel) ORDER BY e.oid) FROM pg_catalog.pg_enum e WHERE e.enumtypid=t.oid),
              (SELECT pg_catalog.jsonb_agg(pg_catalog.to_jsonb(k) ORDER BY k.oid) FROM pg_catalog.pg_constraint k WHERE k.contypid=t.oid),
              (SELECT pg_catalog.jsonb_agg(pg_catalog.to_jsonb(r) ORDER BY r.rngtypid) FROM pg_catalog.pg_range r WHERE t.oid IN(r.rngtypid,r.rngmultitypid)),
              (SELECT pg_catalog.jsonb_agg(pg_catalog.jsonb_build_array(a.attname,a.attnum,a.atttypid,a.atttypmod,a.attisdropped,a.attcollation) ORDER BY a.attnum)
                 FROM pg_catalog.pg_attribute a WHERE a.attrelid=t.typrelid AND a.attnum>0)) ORDER BY t.oid)
             FROM types ids JOIN pg_catalog.pg_type t ON t.oid=ids.id WHERE ids.root=root.id)
        )::pg_catalog.text FROM roots root ORDER BY root.id
        """;

    private static async Task<IReadOnlyDictionary<uint,string>> ReadShapes(NpgsqlConnection session,NpgsqlTransaction tx,uint[] ids,CancellationToken token)
    {
        var result=new Dictionary<uint,string>();if(ids.Length==0)return result;
        await using var command=new NpgsqlCommand(ReadShapeSql,session,tx);command.Parameters.AddWithValue("ids",NpgsqlDbType.Array|NpgsqlDbType.Oid,ids);
        await using var reader=await command.ExecuteReaderAsync(token);
        while(await reader.ReadAsync(token))result.Add(reader.GetFieldValue<uint>(0),reader.GetString(1));return result;
    }

    private async Task ValidateReadScope(Table table,CancellationToken token)
    {
        EnsureReadSession(token);
        if(!relationIdentities.TryGetValue(table.Qualified,out var id)||!readShapes.TryGetValue(id,out var baseline))throw new VerificationException(Reason.ObservationChanged);
        try
        {
            if(!readLocks.Contains(table.Qualified))
            {
                // Same lock acquired by a SELECT, held in the original RR session.
                // It cannot replace/reconnect that session or change user data.
                // PostgreSQL rejects explicit LOCK TABLE on materialized views.
                // A zero-row constant SELECT locks their stored relation without
                // executing the materialized definition or reading sample values.
                var sql=materializedRelations.Contains(table.Qualified)?"SELECT 1 FROM ONLY "+Qualified(table)+" LIMIT 0":"LOCK TABLE "+Qualified(table)+" IN ACCESS SHARE MODE";
                await using var gate=new NpgsqlCommand(sql,connection,transaction);
                await gate.ExecuteNonQueryAsync(token);readLocks.Add(table.Qualified);
            }
            // Reuse the auxiliary authenticated connection, never its snapshot.
            // Each observation below gets a new short read-only transaction.
            if(readObserver==null){readObserver=new NpgsqlConnection(settings.PostgreSqlString());await readObserver.OpenAsync(token);}
            var observer=readObserver;
            await using var observation=await observer.BeginTransactionAsync(System.Data.IsolationLevel.RepeatableRead,token);
            await using(var config=new NpgsqlCommand("SET TRANSACTION READ ONLY; SET LOCAL search_path=pg_catalog; SET LOCAL TimeZone='UTC'; SET LOCAL row_security=off; SET LOCAL jit=off",observer,observation))await config.ExecuteNonQueryAsync(token);
            await using(var identity=new NpgsqlCommand(IdentitySql,observer,observation))
                if((string?)await identity.ExecuteScalarAsync(token)!=inventory!.Identity)throw new VerificationException(Reason.ObservationChanged);
            var current=await ReadShapes(observer,observation,[id],token);
            if(!current.TryGetValue(id,out var actual)||actual!=baseline)throw new VerificationException(Reason.ObservationChanged);
        }
        catch(NpgsqlException e){throw ReadFailure(e);}
        catch(InvalidOperationException) when(connection?.State!=System.Data.ConnectionState.Open){throw LostReadSession();}
    }
}
