using System.Text.Json;
using Npgsql;
using Verifier.Core;

namespace Verifier.Data;

public sealed partial class PostgreSqlDatabase
{
    private async Task<List<Table>> ReadStructure(List<Table> tables,CancellationToken token)
    {
        var collected=tables.ToDictionary(t=>t.Qualified,_=>new Dictionary<string,List<CatalogFact>>());
        var issues=tables.ToDictionary(t=>t.Qualified,_=>new Dictionary<string,Reason>());
        foreach(var (check,sql) in StructureQueries)
        {
            foreach(var table in tables){collected[table.Qualified][check]=[];issues[table.Qualified][check]=Reason.None;}
            // A denied catalog query must not abort the snapshot or erase known
            // column differences. SAVEPOINT changes no persistent source data.
            await using(var save=new NpgsqlCommand("SAVEPOINT dmv_structure",connection,transaction))await save.ExecuteNonQueryAsync(token);
            try
            {
                await using var command=new NpgsqlCommand(sql,connection,transaction);await using var reader=await command.ExecuteReaderAsync(token);
                while(await reader.ReadAsync(token))
                {
                    var name=new Table(reader.GetString(0),reader.GetString(1),[],false).Qualified;
                    if(!collected.TryGetValue(name,out var sections))throw new VerificationException(Reason.ObservationChanged);
                    var key=reader.GetString(2);var column=reader.IsDBNull(3)?null:reader.GetString(3);
                    using var attributes=JsonDocument.Parse(reader.GetString(4));
                    foreach(var property in attributes.RootElement.EnumerateObject())sections[check].Add(new(key,column,property.Name,property.Value.GetRawText()));
                    if(reader.GetBoolean(5))issues[name][check]=Reason.NotImplemented;
                }
            }
            catch(PostgresException e) when(e.SqlState=="42501")
            {
                await using var restore=new NpgsqlCommand("ROLLBACK TO SAVEPOINT dmv_structure",connection,transaction);await restore.ExecuteNonQueryAsync(token);
                foreach(var table in tables)issues[table.Qualified][check]=Reason.PermissionDenied;
            }
            await using(var release=new NpgsqlCommand("RELEASE SAVEPOINT dmv_structure",connection,transaction))await release.ExecuteNonQueryAsync(token);
        }
        foreach(var table in tables)
            if(collected[table.Qualified]["SCHEMA-01"].Any(f=>f.Attribute=="TableKind"&&f.Value=="\"f\""))
                foreach(var query in StructureQueries)issues[table.Qualified][query.Check]=Reason.NotImplemented;
        return tables.Select(t=>t with { Catalog=StructureQueries.Select(q=>new CatalogSection(q.Check,collected[t.Qualified][q.Check].ToArray(),issues[t.Qualified][q.Check])).ToArray() }).ToList();
    }

    private const string Relations="""
        WITH relations AS (SELECT c.*,n.nspname FROM pg_catalog.pg_class c JOIN pg_catalog.pg_namespace n ON n.oid=c.relnamespace
        WHERE c.relkind IN ('r','p','f') AND pg_catalog.left(n.nspname,3)<>'pg_' AND n.nspname<>'information_schema')
        """;
    private static readonly (string Check,string Sql)[] StructureQueries=
    [
        ("SCHEMA-01",Relations+"""
        SELECT c.nspname,c.relname,'table',NULL::pg_catalog.text,
          pg_catalog.jsonb_build_object('TableKind',c.relkind::pg_catalog.text,'TablePersistence',c.relpersistence::pg_catalog.text)::pg_catalog.text,c.relkind='f'
        FROM relations c
        UNION ALL
        SELECT c.nspname,c.relname,a.attname,a.attname,
          pg_catalog.jsonb_build_object('ColumnName',a.attname,'ColumnType',pg_catalog.jsonb_build_array(tn.nspname,t.typname,pg_catalog.format_type(a.atttypid,a.atttypmod)),
            'ColumnPosition',a.attnum,'ColumnNullable',NOT a.attnotnull,'ColumnDefault',pg_catalog.pg_get_expr(d.adbin,d.adrelid,false),
            'ColumnIdentity',a.attidentity::pg_catalog.text,'ColumnGenerated',a.attgenerated::pg_catalog.text,
            'ColumnCollation',CASE WHEN co.oid IS NULL THEN NULL ELSE pg_catalog.jsonb_build_array(cn.nspname,co.collname,co.collprovider::pg_catalog.text,co.collisdeterministic,co.collencoding,co.collcollate,co.collctype,co.colllocale,co.collicurules,co.collversion,
              CASE WHEN co.collprovider='d' THEN pg_catalog.jsonb_build_array(db.datlocprovider::pg_catalog.text,db.datcollate,db.datctype,db.datlocale,db.daticurules,db.datcollversion) ELSE NULL END) END,
            'ColumnStorage',a.attstorage::pg_catalog.text,'ColumnCompression',pg_catalog.jsonb_build_array(a.attcompression::pg_catalog.text,CASE WHEN a.attcompression='' THEN pg_catalog.current_setting('default_toast_compression') ELSE NULL END),
            'ColumnDimensions',a.attndims,'ColumnLocal',a.attislocal,'ColumnInherited',a.attinhcount,'ColumnStatisticsTarget',a.attstattarget,
            'ColumnOptions',ARRAY(SELECT option FROM pg_catalog.unnest(a.attoptions) option ORDER BY option))::pg_catalog.text,c.relkind='f'
        FROM relations c JOIN pg_catalog.pg_attribute a ON a.attrelid=c.oid AND a.attnum>0 AND NOT a.attisdropped
        JOIN pg_catalog.pg_type t ON t.oid=a.atttypid JOIN pg_catalog.pg_namespace tn ON tn.oid=t.typnamespace
        JOIN pg_catalog.pg_database db ON db.datname=pg_catalog.current_database()
        LEFT JOIN pg_catalog.pg_attrdef d ON d.adrelid=c.oid AND d.adnum=a.attnum
        LEFT JOIN pg_catalog.pg_collation co ON co.oid=a.attcollation LEFT JOIN pg_catalog.pg_namespace cn ON cn.oid=co.collnamespace
        """),
        ("SCHEMA-02",Relations+"""
        SELECT c.nspname,c.relname,'constraint:'||con.conname,NULL::pg_catalog.text,
          pg_catalog.jsonb_build_object('ConstraintType',con.contype::pg_catalog.text,'ConstraintDefinition',pg_catalog.pg_get_constraintdef(con.oid,false),
            'ConstraintDeferrable',con.condeferrable,'ConstraintDeferred',con.condeferred,'ConstraintValidated',con.convalidated,
            'ConstraintLocal',con.conislocal,'ConstraintInherited',con.coninhcount,'ConstraintNoInherit',con.connoinherit,
            'ConstraintParent',CASE WHEN parent.oid IS NULL THEN NULL ELSE pg_catalog.jsonb_build_array(pn.nspname,pc.relname,parent.conname) END,
            'ConstraintOperators',pg_catalog.jsonb_build_array(
              ARRAY(SELECT pg_catalog.pg_describe_object('pg_catalog.pg_operator'::pg_catalog.regclass,op,0) FROM pg_catalog.unnest(con.conpfeqop) WITH ORDINALITY u(op,position) ORDER BY position),
              ARRAY(SELECT pg_catalog.pg_describe_object('pg_catalog.pg_operator'::pg_catalog.regclass,op,0) FROM pg_catalog.unnest(con.conppeqop) WITH ORDINALITY u(op,position) ORDER BY position),
              ARRAY(SELECT pg_catalog.pg_describe_object('pg_catalog.pg_operator'::pg_catalog.regclass,op,0) FROM pg_catalog.unnest(con.conffeqop) WITH ORDINALITY u(op,position) ORDER BY position),
              ARRAY(SELECT pg_catalog.pg_describe_object('pg_catalog.pg_operator'::pg_catalog.regclass,op,0) FROM pg_catalog.unnest(con.conexclop) WITH ORDINALITY u(op,position) ORDER BY position)))::pg_catalog.text,c.relkind='f' OR con.contype='t'
        FROM relations c JOIN pg_catalog.pg_constraint con ON con.conrelid=c.oid
        LEFT JOIN pg_catalog.pg_constraint parent ON parent.oid=con.conparentid
        LEFT JOIN pg_catalog.pg_class pc ON pc.oid=parent.conrelid LEFT JOIN pg_catalog.pg_namespace pn ON pn.oid=pc.relnamespace
        """),
        ("SCHEMA-03",Relations+"""
        SELECT c.nspname,c.relname,'table',NULL::pg_catalog.text,
          pg_catalog.jsonb_build_object('TableOptions',ARRAY(SELECT option FROM pg_catalog.unnest(c.reloptions) option ORDER BY option),
            'TableAccessMethod',am.amname,'TableTablespace',sp.spcname,'TableReplicaIdentity',c.relreplident::pg_catalog.text,
            'PartitionKey',pg_catalog.pg_get_partkeydef(c.oid),'PartitionBound',pg_catalog.pg_get_expr(c.relpartbound,c.oid,false),
            'PartitionDefault',CASE WHEN dc.oid IS NULL THEN NULL ELSE pg_catalog.jsonb_build_array(dn.nspname,dc.relname) END,
            'PartitionChildren',COALESCE((WITH RECURSIVE descendants AS (
              SELECT inh.* FROM pg_catalog.pg_inherits inh WHERE inh.inhparent=c.oid
              UNION ALL SELECT inh.* FROM pg_catalog.pg_inherits inh JOIN descendants d ON d.inhrelid=inh.inhparent)
              SELECT pg_catalog.jsonb_agg(pg_catalog.jsonb_build_array(pn.nspname,pc.relname,cn.nspname,cc.relname,d.inhseqno,d.inhdetachpending,pg_catalog.pg_get_expr(cc.relpartbound,cc.oid,false))
                ORDER BY pn.nspname COLLATE pg_catalog."C",pc.relname COLLATE pg_catalog."C",cn.nspname COLLATE pg_catalog."C",cc.relname COLLATE pg_catalog."C",d.inhseqno)
              FROM descendants d JOIN pg_catalog.pg_class pc ON pc.oid=d.inhparent JOIN pg_catalog.pg_namespace pn ON pn.oid=pc.relnamespace
              JOIN pg_catalog.pg_class cc ON cc.oid=d.inhrelid JOIN pg_catalog.pg_namespace cn ON cn.oid=cc.relnamespace),'[]'::pg_catalog.jsonb),
            'PartitionParents',COALESCE((SELECT pg_catalog.jsonb_agg(pg_catalog.jsonb_build_array(pn.nspname,pc.relname,inh.inhseqno,inh.inhdetachpending) ORDER BY inh.inhseqno)
              FROM pg_catalog.pg_inherits inh JOIN pg_catalog.pg_class pc ON pc.oid=inh.inhparent JOIN pg_catalog.pg_namespace pn ON pn.oid=pc.relnamespace WHERE inh.inhrelid=c.oid),'[]'::pg_catalog.jsonb))::pg_catalog.text,c.relkind='f'
        FROM relations c LEFT JOIN pg_catalog.pg_am am ON am.oid=c.relam
        JOIN pg_catalog.pg_database db ON db.datname=pg_catalog.current_database() LEFT JOIN pg_catalog.pg_tablespace sp ON sp.oid=CASE WHEN c.reltablespace=0 THEN db.dattablespace ELSE c.reltablespace END
        LEFT JOIN pg_catalog.pg_partitioned_table pt ON pt.partrelid=c.oid LEFT JOIN pg_catalog.pg_class dc ON dc.oid=pt.partdefid LEFT JOIN pg_catalog.pg_namespace dn ON dn.oid=dc.relnamespace
        UNION ALL
        SELECT c.nspname,c.relname,'index:'||ic.relname,NULL::pg_catalog.text,
          pg_catalog.jsonb_build_object('IndexDefinition',pg_catalog.pg_get_indexdef(i.indexrelid,0,false),
            'IndexUnique',i.indisunique,'IndexNulls',i.indnullsnotdistinct,'IndexPrimary',i.indisprimary,'IndexExclusion',i.indisexclusion,
            'IndexImmediate',i.indimmediate,'IndexClustered',i.indisclustered,'IndexValid',i.indisvalid,'IndexReady',i.indisready,'IndexLive',i.indislive,'IndexReplicaIdentity',i.indisreplident,
            'IndexCheckXmin',i.indcheckxmin,'IndexKeyCount',i.indnkeyatts,'IndexColumnCount',i.indnatts,'IndexPersistence',ic.relpersistence::pg_catalog.text,
            'IndexOptions',ARRAY(SELECT option FROM pg_catalog.unnest(ic.reloptions) option ORDER BY option),'IndexTablespace',sp.spcname,
            'IndexParent',COALESCE((SELECT pg_catalog.jsonb_agg(pg_catalog.jsonb_build_array(pn.nspname,pc.relname,inh.inhseqno,inh.inhdetachpending) ORDER BY inh.inhseqno)
              FROM pg_catalog.pg_inherits inh JOIN pg_catalog.pg_class pc ON pc.oid=inh.inhparent JOIN pg_catalog.pg_namespace pn ON pn.oid=pc.relnamespace WHERE inh.inhrelid=ic.oid),'[]'::pg_catalog.jsonb))::pg_catalog.text,c.relkind='f'
        FROM relations c JOIN pg_catalog.pg_index i ON i.indrelid=c.oid JOIN pg_catalog.pg_class ic ON ic.oid=i.indexrelid
        JOIN pg_catalog.pg_database db ON db.datname=pg_catalog.current_database() LEFT JOIN pg_catalog.pg_tablespace sp ON sp.oid=CASE WHEN ic.reltablespace=0 THEN db.dattablespace ELSE ic.reltablespace END
        """)
    ];
}
