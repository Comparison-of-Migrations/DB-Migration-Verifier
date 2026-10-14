namespace Verifier.Data;

public sealed partial class PostgreSqlDatabase
{
    // Resolve handler identities from catalog names, without invoking handlers,
    // validators, remote tables or type output functions.
    private static string RoutineIdentity(string oid)=>"""
        (SELECT pg_catalog.jsonb_build_array(n.nspname,p.proname,
          ARRAY(SELECT pg_catalog.jsonb_build_array(tn.nspname,t.typname)::pg_catalog.text
            FROM pg_catalog.unnest(p.proargtypes) WITH ORDINALITY a(oid,position)
            JOIN pg_catalog.pg_type t ON t.oid=a.oid JOIN pg_catalog.pg_namespace tn ON tn.oid=t.typnamespace ORDER BY a.position))
        FROM pg_catalog.pg_proc p JOIN pg_catalog.pg_namespace n ON n.oid=p.pronamespace WHERE p.oid=
        """+oid+")";
    private static string ForeignInfrastructureSql=>"""
        SELECT ''::pg_catalog.text,''::pg_catalog.text,pg_catalog.jsonb_build_array('foreign-wrapper',f.fdwname)::pg_catalog.text,NULL::pg_catalog.text,
          pg_catalog.jsonb_strip_nulls(pg_catalog.jsonb_build_object('ForeignDefinition',pg_catalog.jsonb_build_array('wrapper',f.fdwname,
        """+RoutineIdentity("f.fdwhandler")+","+RoutineIdentity("f.fdwvalidator")+"""
          ),'ForeignOptionsPresent',COALESCE(pg_catalog.cardinality(f.fdwoptions),0)>0,
          'UninspectedForeignOptions',CASE WHEN COALESCE(pg_catalog.cardinality(f.fdwoptions),0)>0
            THEN pg_catalog.jsonb_build_array(f.xmin::pg_catalog.text,f.ctid::pg_catalog.text) ELSE NULL END))::pg_catalog.text,false
        FROM pg_catalog.pg_foreign_data_wrapper f
        UNION ALL
        SELECT ''::pg_catalog.text,''::pg_catalog.text,pg_catalog.jsonb_build_array('foreign-server',s.srvname)::pg_catalog.text,NULL::pg_catalog.text,
          pg_catalog.jsonb_strip_nulls(pg_catalog.jsonb_build_object('ForeignDefinition',pg_catalog.jsonb_build_array('server',s.srvname,f.fdwname,s.srvtype,s.srvversion),
          'ForeignOptionsPresent',COALESCE(pg_catalog.cardinality(s.srvoptions),0)>0,
          'UninspectedForeignOptions',CASE WHEN COALESCE(pg_catalog.cardinality(s.srvoptions),0)>0
            THEN pg_catalog.jsonb_build_array(s.xmin::pg_catalog.text,s.ctid::pg_catalog.text) ELSE NULL END))::pg_catalog.text,false
        FROM pg_catalog.pg_foreign_server s JOIN pg_catalog.pg_foreign_data_wrapper f ON f.oid=s.srvfdw
        UNION ALL
        SELECT ''::pg_catalog.text,''::pg_catalog.text,pg_catalog.jsonb_build_array('foreign-mapping',m.srvname,m.umuser=0,m.usename)::pg_catalog.text,NULL::pg_catalog.text,
          pg_catalog.jsonb_build_object('ForeignUserMapping',pg_catalog.jsonb_build_array(m.srvname,m.umuser=0,m.usename),
            'UninspectedForeignOptions',pg_catalog.jsonb_build_array('withheld'))::pg_catalog.text,false
        FROM pg_catalog.pg_user_mappings m
        """;
    // The publicly readable mapping view exposes identities even when options
    // are hidden. Never select umoptions: NULL would not establish absence.
    private const string ForeignTableSql="""
        SELECT n.nspname,c.relname,pg_catalog.jsonb_build_array('foreign-table',n.nspname,c.relname)::pg_catalog.text,NULL::pg_catalog.text,
          pg_catalog.jsonb_strip_nulls(pg_catalog.jsonb_build_object('ForeignDefinition',pg_catalog.jsonb_build_array('table',s.srvname),
          'ForeignOptionsPresent',COALESCE(pg_catalog.cardinality(f.ftoptions),0)>0,
          'UninspectedForeignOptions',CASE WHEN COALESCE(pg_catalog.cardinality(f.ftoptions),0)>0
            THEN pg_catalog.jsonb_build_array(f.xmin::pg_catalog.text,f.ctid::pg_catalog.text) ELSE NULL END))::pg_catalog.text,false
        FROM pg_catalog.pg_foreign_table f JOIN pg_catalog.pg_class c ON c.oid=f.ftrelid JOIN pg_catalog.pg_namespace n ON n.oid=c.relnamespace
        JOIN pg_catalog.pg_foreign_server s ON s.oid=f.ftserver
        WHERE pg_catalog.left(n.nspname,3)<>'pg_' AND n.nspname<>'information_schema'
        UNION ALL
        SELECT n.nspname,c.relname,pg_catalog.jsonb_build_array('foreign-column',n.nspname,c.relname,a.attname)::pg_catalog.text,a.attname::pg_catalog.text,
          pg_catalog.jsonb_strip_nulls(pg_catalog.jsonb_build_object('ForeignOptionsPresent',COALESCE(pg_catalog.cardinality(a.attfdwoptions),0)>0,
          'UninspectedForeignOptions',CASE WHEN COALESCE(pg_catalog.cardinality(a.attfdwoptions),0)>0
            THEN pg_catalog.jsonb_build_array(a.xmin::pg_catalog.text,a.ctid::pg_catalog.text) ELSE NULL END))::pg_catalog.text,false
        FROM pg_catalog.pg_foreign_table f JOIN pg_catalog.pg_class c ON c.oid=f.ftrelid JOIN pg_catalog.pg_namespace n ON n.oid=c.relnamespace
        JOIN pg_catalog.pg_attribute a ON a.attrelid=c.oid AND a.attnum>0 AND NOT a.attisdropped
        WHERE pg_catalog.left(n.nspname,3)<>'pg_' AND n.nspname<>'information_schema'
        """;
    private static string ForeignAccessSql=>"""
        SELECT ''::pg_catalog.text,''::pg_catalog.text,pg_catalog.jsonb_build_array('foreign-wrapper',f.fdwname)::pg_catalog.text,NULL::pg_catalog.text,
        """+"pg_catalog.jsonb_build_object('ForeignAccess',pg_catalog.jsonb_build_array(pg_catalog.pg_get_userbyid(f.fdwowner),"+Acl("COALESCE(f.fdwacl,pg_catalog.acldefault('F',f.fdwowner))")+"))::pg_catalog.text,false "+"""
        FROM pg_catalog.pg_foreign_data_wrapper f
        UNION ALL
        SELECT ''::pg_catalog.text,''::pg_catalog.text,pg_catalog.jsonb_build_array('foreign-server',s.srvname)::pg_catalog.text,NULL::pg_catalog.text,
        """+"pg_catalog.jsonb_build_object('ForeignAccess',pg_catalog.jsonb_build_array(pg_catalog.pg_get_userbyid(s.srvowner),"+Acl("COALESCE(s.srvacl,pg_catalog.acldefault('S',s.srvowner))")+"))::pg_catalog.text,false "+"""
        FROM pg_catalog.pg_foreign_server s
        """;
}
