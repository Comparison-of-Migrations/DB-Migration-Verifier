using System.Text.Json;
using Npgsql;
using Verifier.Core;
namespace Verifier.Data;

public sealed partial class PostgreSqlDatabase
{
    // These roots address real catalog objects. They are never materialized as
    // user tables or exported. Negative routine roots avoid cross-catalog OID collisions.
    private const string DatabaseObjectRoots="""
        WITH relations AS (
          SELECT c.oid::pg_catalog.int8 oid,n.nspname,c.relname,'relation' kind FROM pg_catalog.pg_class c
            JOIN pg_catalog.pg_namespace n ON n.oid=c.relnamespace
            WHERE pg_catalog.left(n.nspname,3)<>'pg_' AND n.nspname<>'information_schema' AND c.relkind IN ('r','p','f','v','m')
          UNION ALL SELECT -(p.oid::pg_catalog.int8),n.nspname,p.proname,'routine' FROM pg_catalog.pg_proc p
            JOIN pg_catalog.pg_namespace n ON n.oid=p.pronamespace
            WHERE pg_catalog.left(n.nspname,3)<>'pg_' AND n.nspname<>'information_schema'
        ), views(root,oid) AS(SELECT oid,oid::pg_catalog.oid FROM relations WHERE kind='relation'),
        routines(root,oid) AS(SELECT oid,(-oid)::pg_catalog.oid FROM relations WHERE kind='routine')
        """;
    private const string DatabaseSequenceRoots="""
        WITH relations AS(SELECT c.oid,n.nspname,c.relname FROM pg_catalog.pg_class c
          JOIN pg_catalog.pg_namespace n ON n.oid=c.relnamespace WHERE c.relkind='S'
          AND pg_catalog.left(n.nspname,3)<>'pg_' AND n.nspname<>'information_schema'),
        links(root,sequence) AS(SELECT oid,oid FROM relations)
        """;
    private const string DatabaseTypeRoots="""
        WITH relations AS(SELECT t.oid,n.nspname,t.typname relname FROM pg_catalog.pg_type t
          JOIN pg_catalog.pg_namespace n ON n.oid=t.typnamespace
          WHERE pg_catalog.left(n.nspname,3)<>'pg_' AND n.nspname<>'information_schema'),
        types(root,oid) AS(SELECT oid,oid FROM relations)
        """;
    private static (string Check,string Sql)[] DatabaseQueries=>
    [
        ("SCHEMA-01","""
          SELECT n.nspname,''::pg_catalog.text,pg_catalog.jsonb_build_array('schema',n.nspname)::pg_catalog.text,NULL::pg_catalog.text,
            pg_catalog.jsonb_build_object('SchemaPresence',true)::pg_catalog.text,false
          FROM pg_catalog.pg_namespace n WHERE pg_catalog.left(n.nspname,3)<>'pg_' AND n.nspname<>'information_schema'
          """),
        ("ACCESS-01","("+DatabaseAccessSql+") UNION ALL ("+ForeignAccessSql+")"),
        ("OBJECT-01",DatabaseObjectRoots+ObjectCatalogSql),
        ("OBJECT-02",DatabaseSequenceRoots+SequenceCatalogSql),
        ("OBJECT-03","("+DatabaseTypeRoots+TypeCatalogSql+") UNION ALL ("+"""
          SELECT n.nspname,e.extname,pg_catalog.jsonb_build_array('extension',e.extname)::pg_catalog.text,NULL::pg_catalog.text,
            pg_catalog.jsonb_build_object('ExtensionDefinition',pg_catalog.jsonb_build_array(e.extname,e.extversion,n.nspname,
              pg_catalog.pg_get_userbyid(e.extowner),e.extrelocatable))::pg_catalog.text,true
          FROM pg_catalog.pg_extension e JOIN pg_catalog.pg_namespace n ON n.oid=e.extnamespace
          WHERE e.extname<>'plpgsql'
          """+") UNION ALL ("+ForeignInfrastructureSql+") UNION ALL ("+ForeignTableSql+")")
    ];
    private async Task<IReadOnlyList<CatalogSection>> ReadDatabaseCatalog(CancellationToken token)
    {
        var sections=new List<CatalogSection>();
        foreach(var (check,sql) in DatabaseQueries)
        {
            var facts=new List<CatalogFact>();var gaps=new List<CatalogGap>();var reason=deparseLimit!=Reason.None&&NativeDeparsers.IsMatch(sql)?deparseLimit:Reason.None;
            await using(var save=new NpgsqlCommand("SAVEPOINT dmv_database_catalog",connection,transaction))await save.ExecuteNonQueryAsync(token);
            try
            {
                await using var command=new NpgsqlCommand(GuardDeparsers(sql),connection,transaction);await using var reader=await command.ExecuteReaderAsync(token);
                while(await reader.ReadAsync(token))
                {
                    var key=reader.GetString(2);using var attributes=JsonDocument.Parse(reader.GetString(4));
                    foreach(var property in attributes.RootElement.EnumerateObject())
                    {
                        if(property.Name is "UninspectedConfiguration" or "UninspectedForeignOptions"&&property.Value.GetArrayLength()>0)
                            gaps.Add(new(key,null,property.Name,property.Value.GetRawText(),Reason.NotImplemented));
                        else if(ContainsBlockedDefinition(property.Value)||deparseLimit!=Reason.None&&DeparsedAttributes.Contains(property.Name))
                            gaps.Add(new(key,null,property.Name,property.Value.GetRawText(),deparseLimit==Reason.None?Reason.UnsupportedType:deparseLimit));
                        else facts.Add(new(key,null,property.Name,property.Value.GetRawText()));
                    }
                    // Known attributes remain comparable; unimplemented semantics
                    // never authorize equality of extension/external execution.
                    if(reader.GetBoolean(5))reason=Reason.NotImplemented;
                }
            }
            catch(PostgresException e) when(e.SqlState=="42501")
            {
                await using var restore=new NpgsqlCommand("ROLLBACK TO SAVEPOINT dmv_database_catalog",connection,transaction);await restore.ExecuteNonQueryAsync(token);reason=Reason.PermissionDenied;
            }
            await using(var release=new NpgsqlCommand("RELEASE SAVEPOINT dmv_database_catalog",connection,transaction))await release.ExecuteNonQueryAsync(token);
            sections.Add(new(check,facts.Distinct().ToArray(),reason!=Reason.None?reason:gaps.FirstOrDefault()?.Reason??Reason.None)
                {Gaps=gaps.Distinct().ToArray(),CompleteExceptGaps=reason==Reason.None&&gaps.Count>0});
        }
        return await ReadDatabaseSequenceStates(sections,token);
    }
    private async Task<IReadOnlyList<CatalogSection>> ReadDatabaseSequenceStates(List<CatalogSection> sections,CancellationToken token)
    {
        var index=sections.FindIndex(s=>s.CheckId=="OBJECT-02");var section=sections[index];var facts=section.Facts.ToList();var gaps=section.Gaps.ToList();
        // Names originate in a trusted inspected definition; quote each identifier.
        foreach(var fact in section.Facts.Where(f=>f.Attribute=="SequenceDefinitions"))
        {
            using var value=JsonDocument.Parse(fact.Value!);var schema=value.RootElement[0].GetString()!;var name=value.RootElement[1].GetString()!;
            await using(var save=new NpgsqlCommand("SAVEPOINT dmv_database_sequence",connection,transaction))await save.ExecuteNonQueryAsync(token);
            try
            {
                await using var command=new NpgsqlCommand("SELECT last_value::pg_catalog.text,is_called FROM "+Quote(schema)+"."+Quote(name),connection,transaction);
                await using var reader=await command.ExecuteReaderAsync(token);if(!await reader.ReadAsync(token))throw new VerificationException(Reason.ObservationChanged);
                facts.Add(new(fact.Key,null,"SequenceState",JsonSerializer.Serialize(new object[]{reader.GetString(0),reader.GetBoolean(1)})));
            }
            catch(PostgresException e) when(e.SqlState=="42501")
            {
                await using var restore=new NpgsqlCommand("ROLLBACK TO SAVEPOINT dmv_database_sequence",connection,transaction);await restore.ExecuteNonQueryAsync(token);
                gaps.Add(new(fact.Key,null,"SequenceState","",Reason.PermissionDenied));
            }
            await using(var release=new NpgsqlCommand("RELEASE SAVEPOINT dmv_database_sequence",connection,transaction))await release.ExecuteNonQueryAsync(token);
        }
        sections[index]=section with{Facts=facts,Gaps=gaps,Reason=section.Reason!=Reason.None?section.Reason:gaps.FirstOrDefault()?.Reason??Reason.None,
            CompleteExceptGaps=(section.Reason==Reason.None||section.CompleteExceptGaps)&&gaps.Count>0};
        return sections;
    }
}
