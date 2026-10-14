using System.Text.Json;
using Npgsql;
using Verifier.Core;
namespace Verifier.Data;

public sealed partial class PostgreSqlDatabase
{
    // Both ownership (serial/identity) and default-expression sequence dependencies.
    private const string SequenceLinks=Relations+"""
        , links AS (
          SELECT c.oid root,s.oid sequence FROM relations c JOIN pg_catalog.pg_depend d
            ON d.refclassid='pg_catalog.pg_class'::pg_catalog.regclass AND d.refobjid=c.oid AND d.classid='pg_catalog.pg_class'::pg_catalog.regclass AND d.deptype IN ('a','i')
            JOIN pg_catalog.pg_class s ON s.oid=d.objid AND s.relkind='S'
          UNION SELECT c.oid,s.oid FROM relations c JOIN pg_catalog.pg_attrdef a ON a.adrelid=c.oid
            JOIN pg_catalog.pg_depend d ON d.classid='pg_catalog.pg_attrdef'::pg_catalog.regclass AND d.objid=a.oid AND d.refclassid='pg_catalog.pg_class'::pg_catalog.regclass
            JOIN pg_catalog.pg_class s ON s.oid=d.refobjid AND s.relkind='S')
        """;
    private static readonly string SequenceCatalogSql="""
        SELECT c.nspname,c.relname,'sequence:'||pg_catalog.jsonb_build_array(n.nspname,s.relname)::pg_catalog.text,NULL::pg_catalog.text,
          pg_catalog.jsonb_build_object('SequenceDefinitions',pg_catalog.jsonb_build_array(n.nspname,s.relname,
            pg_catalog.pg_get_userbyid(s.relowner),pg_catalog.format_type(q.seqtypid,NULL),q.seqstart::pg_catalog.text,q.seqincrement::pg_catalog.text,
            q.seqmax::pg_catalog.text,q.seqmin::pg_catalog.text,q.seqcache::pg_catalog.text,q.seqcycle,s.relpersistence::pg_catalog.text,
            COALESCE((SELECT pg_catalog.jsonb_agg(pg_catalog.jsonb_build_array(tn.nspname,t.relname,a.attname,d.deptype::pg_catalog.text)
              ORDER BY tn.nspname COLLATE pg_catalog."C",t.relname COLLATE pg_catalog."C",a.attname COLLATE pg_catalog."C")
              FROM pg_catalog.pg_depend d JOIN pg_catalog.pg_class t ON t.oid=d.refobjid JOIN pg_catalog.pg_namespace tn ON tn.oid=t.relnamespace
              LEFT JOIN pg_catalog.pg_attribute a ON a.attrelid=t.oid AND a.attnum=d.refobjsubid
              WHERE d.classid='pg_catalog.pg_class'::pg_catalog.regclass AND d.objid=s.oid AND d.refclassid='pg_catalog.pg_class'::pg_catalog.regclass AND d.deptype IN ('a','i')),'[]'::pg_catalog.jsonb),
        """+Acl("COALESCE(s.relacl,pg_catalog.acldefault('s',s.relowner))")+"""
          ))::pg_catalog.text,false
        FROM relations c JOIN links l ON l.root=c.oid JOIN pg_catalog.pg_class s ON s.oid=l.sequence
        JOIN pg_catalog.pg_namespace n ON n.oid=s.relnamespace JOIN pg_catalog.pg_sequence q ON q.seqrelid=s.oid
        """;
    private static readonly (string Check,string Sql)[] SequenceQueries=[("OBJECT-02",SequenceLinks+SequenceCatalogSql)];
    private async Task<List<Table>> ReadSequenceStates(List<Table> tables,CancellationToken token)
    {
        var links=new List<(string Table,string Schema,string Name,string Key)>();
        await using(var save=new NpgsqlCommand("SAVEPOINT dmv_sequence_links",connection,transaction))await save.ExecuteNonQueryAsync(token);
        try
        {
        await using(var command=new NpgsqlCommand(SequenceLinks+"SELECT c.nspname,c.relname,n.nspname,s.relname,'sequence:'||pg_catalog.jsonb_build_array(n.nspname,s.relname)::pg_catalog.text FROM relations c JOIN links l ON l.root=c.oid JOIN pg_catalog.pg_class s ON s.oid=l.sequence JOIN pg_catalog.pg_namespace n ON n.oid=s.relnamespace",connection,transaction))
        await using(var reader=await command.ExecuteReaderAsync(token))
            while(await reader.ReadAsync(token))links.Add((new Table(reader.GetString(0),reader.GetString(1),[],false).Qualified,reader.GetString(2),reader.GetString(3),reader.GetString(4)));
        }
        catch(PostgresException e) when(e.SqlState=="42501")
        {
            await using(var restore=new NpgsqlCommand("ROLLBACK TO SAVEPOINT dmv_sequence_links; RELEASE SAVEPOINT dmv_sequence_links",connection,transaction))await restore.ExecuteNonQueryAsync(token);
            return tables.Select(t=>t with{Catalog=t.Catalog.Select(s=>s.CheckId=="OBJECT-02"?s with{Reason=Reason.PermissionDenied}:s).ToArray()}).ToList();
        }
        await using(var release=new NpgsqlCommand("RELEASE SAVEPOINT dmv_sequence_links",connection,transaction))await release.ExecuteNonQueryAsync(token);
        var observed=new Dictionary<(string,string),(string? Value,Reason Reason)>();
        foreach(var link in links)
        {
            var key=(link.Schema,link.Name);if(observed.ContainsKey(key))continue;
            await using(var save=new NpgsqlCommand("SAVEPOINT dmv_sequence",connection,transaction))await save.ExecuteNonQueryAsync(token);
            try
            {
                // Reading sequence state does not call nextval, currval or setval.
                await using var command=new NpgsqlCommand("SELECT last_value::pg_catalog.text,is_called FROM "+Quote(link.Schema)+"."+Quote(link.Name),connection,transaction);
                await using var reader=await command.ExecuteReaderAsync(token);
                if(!await reader.ReadAsync(token))throw new VerificationException(Reason.ObservationChanged);
                observed[key]=(JsonSerializer.Serialize(new object[]{reader.GetString(0),reader.GetBoolean(1)}),Reason.None);
            }
            catch(PostgresException e) when(e.SqlState=="42501")
            {
                await using var rollback=new NpgsqlCommand("ROLLBACK TO SAVEPOINT dmv_sequence",connection,transaction);await rollback.ExecuteNonQueryAsync(token);
                observed[key]=(null,Reason.PermissionDenied);
            }
            await using(var release=new NpgsqlCommand("RELEASE SAVEPOINT dmv_sequence",connection,transaction))await release.ExecuteNonQueryAsync(token);
        }
        return tables.Select(table=>
        {
            var section=table.Catalog.Single(s=>s.CheckId=="OBJECT-02");var facts=section.Facts.ToList();var reason=section.Reason;
            foreach(var link in links.Where(l=>l.Table==table.Qualified))
            {var state=observed[(link.Schema,link.Name)];if(state.Reason!=Reason.None){reason=state.Reason;continue;}facts.Add(new(link.Key,null,"SequenceState",state.Value));}
            return table with{Catalog=table.Catalog.Select(s=>s.CheckId=="OBJECT-02"?new CatalogSection(s.CheckId,facts,reason):s).ToArray()};
        }).ToList();
    }
}
