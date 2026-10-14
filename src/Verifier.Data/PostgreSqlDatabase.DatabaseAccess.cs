namespace Verifier.Data;

public sealed partial class PostgreSqlDatabase
{
    // Principals are scoped through objects in this database, never through
    // unrelated databases or pg_authid/password verifier columns.
    private const string DatabaseRoleClosure="""
        WITH RECURSIVE schemas AS(SELECT * FROM pg_catalog.pg_namespace WHERE pg_catalog.left(nspname,3)<>'pg_' AND nspname<>'information_schema'),
        seeds(id) AS(
          SELECT nspowner FROM schemas UNION SELECT datdba FROM pg_catalog.pg_database WHERE datname=pg_catalog.current_database()
          UNION SELECT x.grantee FROM schemas s CROSS JOIN LATERAL pg_catalog.aclexplode(s.nspacl) x
          UNION SELECT x.grantor FROM schemas s CROSS JOIN LATERAL pg_catalog.aclexplode(s.nspacl) x
          UNION SELECT x.grantee FROM pg_catalog.pg_database d CROSS JOIN LATERAL pg_catalog.aclexplode(d.datacl) x WHERE d.datname=pg_catalog.current_database()
          UNION SELECT x.grantor FROM pg_catalog.pg_database d CROSS JOIN LATERAL pg_catalog.aclexplode(d.datacl) x WHERE d.datname=pg_catalog.current_database()
          UNION SELECT c.relowner FROM pg_catalog.pg_class c JOIN schemas s ON s.oid=c.relnamespace
          UNION SELECT p.proowner FROM pg_catalog.pg_proc p JOIN schemas s ON s.oid=p.pronamespace
          UNION SELECT t.typowner FROM pg_catalog.pg_type t JOIN schemas s ON s.oid=t.typnamespace
          UNION SELECT x.grantee FROM pg_catalog.pg_class c JOIN schemas s ON s.oid=c.relnamespace CROSS JOIN LATERAL pg_catalog.aclexplode(c.relacl) x
          UNION SELECT x.grantor FROM pg_catalog.pg_class c JOIN schemas s ON s.oid=c.relnamespace CROSS JOIN LATERAL pg_catalog.aclexplode(c.relacl) x
          UNION SELECT x.grantee FROM pg_catalog.pg_proc p JOIN schemas s ON s.oid=p.pronamespace CROSS JOIN LATERAL pg_catalog.aclexplode(p.proacl) x
          UNION SELECT x.grantor FROM pg_catalog.pg_proc p JOIN schemas s ON s.oid=p.pronamespace CROSS JOIN LATERAL pg_catalog.aclexplode(p.proacl) x
          UNION SELECT x.grantee FROM pg_catalog.pg_type t JOIN schemas s ON s.oid=t.typnamespace CROSS JOIN LATERAL pg_catalog.aclexplode(t.typacl) x
          UNION SELECT x.grantor FROM pg_catalog.pg_type t JOIN schemas s ON s.oid=t.typnamespace CROSS JOIN LATERAL pg_catalog.aclexplode(t.typacl) x
          UNION SELECT x.grantee FROM pg_catalog.pg_attribute a JOIN pg_catalog.pg_class c ON c.oid=a.attrelid JOIN schemas s ON s.oid=c.relnamespace CROSS JOIN LATERAL pg_catalog.aclexplode(a.attacl) x
          UNION SELECT x.grantor FROM pg_catalog.pg_attribute a JOIN pg_catalog.pg_class c ON c.oid=a.attrelid JOIN schemas s ON s.oid=c.relnamespace CROSS JOIN LATERAL pg_catalog.aclexplode(a.attacl) x
          UNION SELECT r FROM pg_catalog.pg_policy p JOIN pg_catalog.pg_class c ON c.oid=p.polrelid JOIN schemas s ON s.oid=c.relnamespace CROSS JOIN LATERAL pg_catalog.unnest(p.polroles) r
          UNION SELECT defaclrole FROM pg_catalog.pg_default_acl
          UNION SELECT fdwowner FROM pg_catalog.pg_foreign_data_wrapper
          UNION SELECT srvowner FROM pg_catalog.pg_foreign_server
          UNION SELECT umuser FROM pg_catalog.pg_user_mappings
          UNION SELECT x.grantee FROM pg_catalog.pg_foreign_data_wrapper f CROSS JOIN LATERAL pg_catalog.aclexplode(f.fdwacl) x
          UNION SELECT x.grantor FROM pg_catalog.pg_foreign_data_wrapper f CROSS JOIN LATERAL pg_catalog.aclexplode(f.fdwacl) x
          UNION SELECT x.grantee FROM pg_catalog.pg_foreign_server s CROSS JOIN LATERAL pg_catalog.aclexplode(s.srvacl) x
          UNION SELECT x.grantor FROM pg_catalog.pg_foreign_server s CROSS JOIN LATERAL pg_catalog.aclexplode(s.srvacl) x
          UNION SELECT x.grantee FROM pg_catalog.pg_default_acl d CROSS JOIN LATERAL pg_catalog.aclexplode(d.defaclacl) x
          UNION SELECT x.grantor FROM pg_catalog.pg_default_acl d CROSS JOIN LATERAL pg_catalog.aclexplode(d.defaclacl) x
        ), closure(id) AS(
          SELECT id FROM seeds UNION SELECT other.id FROM closure r JOIN pg_catalog.pg_auth_members m ON r.id IN(m.roleid,m.member,m.grantor)
            CROSS JOIN LATERAL (VALUES(m.roleid),(m.member),(m.grantor)) other(id)
        )
        """;
    private static string DatabaseAccessSql=>"""
        SELECT n.nspname,''::pg_catalog.text,pg_catalog.jsonb_build_array('schema',n.nspname)::pg_catalog.text,NULL::pg_catalog.text,
        """+"pg_catalog.jsonb_build_object('SchemaAccess',pg_catalog.jsonb_build_array(pg_catalog.pg_get_userbyid(n.nspowner),"+Acl("COALESCE(n.nspacl,pg_catalog.acldefault('n',n.nspowner))")+"))::pg_catalog.text,false "+"""
        FROM pg_catalog.pg_namespace n WHERE pg_catalog.left(n.nspname,3)<>'pg_' AND n.nspname<>'information_schema'
        UNION ALL SELECT ''::pg_catalog.text,''::pg_catalog.text,'database',NULL::pg_catalog.text,
        """+"pg_catalog.jsonb_build_object('DatabaseAccess',pg_catalog.jsonb_build_array(pg_catalog.pg_get_userbyid(d.datdba),"+Acl("COALESCE(d.datacl,pg_catalog.acldefault('d',d.datdba))")+"))::pg_catalog.text,false "+"""
        FROM pg_catalog.pg_database d WHERE d.datname=pg_catalog.current_database()
        UNION ALL SELECT ''::pg_catalog.text,''::pg_catalog.text,pg_catalog.jsonb_build_array('default',pg_catalog.pg_get_userbyid(d.defaclrole),n.nspname,d.defaclobjtype::pg_catalog.text)::pg_catalog.text,NULL::pg_catalog.text,
        """+"pg_catalog.jsonb_build_object('DefaultAccess',"+Acl("d.defaclacl")+")::pg_catalog.text,false "+"""
        FROM pg_catalog.pg_default_acl d LEFT JOIN pg_catalog.pg_namespace n ON n.oid=d.defaclnamespace
        UNION ALL SELECT ''::pg_catalog.text,''::pg_catalog.text,'roles',NULL::pg_catalog.text,
          pg_catalog.jsonb_build_object('RelatedRoles',(
        """+DatabaseRoleClosure+"""
        SELECT pg_catalog.jsonb_build_array(
          (SELECT pg_catalog.jsonb_agg(pg_catalog.jsonb_build_array(r.rolname,r.rolsuper,r.rolinherit,r.rolcreaterole,r.rolcreatedb,
            r.rolcanlogin,r.rolreplication,r.rolconnlimit,r.rolvaliduntil::pg_catalog.text,r.rolbypassrls) ORDER BY r.rolname COLLATE pg_catalog."C")
            FROM pg_catalog.pg_roles r WHERE r.oid IN(SELECT id FROM closure)),
          (SELECT pg_catalog.jsonb_agg(pg_catalog.jsonb_build_array(pg_catalog.pg_get_userbyid(m.roleid),pg_catalog.pg_get_userbyid(m.member),
            pg_catalog.pg_get_userbyid(m.grantor),m.admin_option,m.inherit_option,m.set_option)
            ORDER BY pg_catalog.pg_get_userbyid(m.roleid) COLLATE pg_catalog."C",pg_catalog.pg_get_userbyid(m.member) COLLATE pg_catalog."C",pg_catalog.pg_get_userbyid(m.grantor) COLLATE pg_catalog."C")
            FROM pg_catalog.pg_auth_members m WHERE m.roleid IN(SELECT id FROM closure) OR m.member IN(SELECT id FROM closure)))
          ))::pg_catalog.text,false
        UNION ALL
        SELECT ''::pg_catalog.text,''::pg_catalog.text,pg_catalog.jsonb_build_array('configuration',config.setdatabase=0,config.setrole=0,r.rolname)::pg_catalog.text,NULL::pg_catalog.text,
          pg_catalog.jsonb_strip_nulls(pg_catalog.jsonb_build_object('RoleConfiguration',NULLIF(cfg.known,'[]'::pg_catalog.jsonb),
            'UninspectedConfiguration',CASE WHEN cfg.names='[]'::pg_catalog.jsonb THEN NULL
              ELSE pg_catalog.jsonb_build_array(cfg.names,config.xmin::pg_catalog.text,config.ctid::pg_catalog.text) END))::pg_catalog.text,false
        FROM pg_catalog.pg_database db JOIN pg_catalog.pg_db_role_setting config ON config.setdatabase IN(0,db.oid)
          LEFT JOIN pg_catalog.pg_roles r ON r.oid=config.setrole CROSS JOIN LATERAL (
        """+ConfigurationValuesSql+"""
          ) cfg WHERE db.datname=pg_catalog.current_database() AND (config.setdatabase=db.oid OR config.setrole=0 OR config.setrole IN(
        """+DatabaseRoleClosure+"SELECT id FROM closure))";
}
