using Verifier.Core;
namespace Verifier.Data;

public sealed partial class PostgreSqlDatabase
{
    // pg_roles exposes masked credentials; select explicit non-secret attributes only.
    // ACL entries and principal names remain in memory, never in a finding/report.
    private static string Acl(string expression)=>"""
        COALESCE((SELECT pg_catalog.jsonb_agg(pg_catalog.jsonb_build_array(pg_catalog.pg_get_userbyid(x.grantor),
          CASE WHEN x.grantee=0 THEN 'PUBLIC' ELSE pg_catalog.pg_get_userbyid(x.grantee) END,x.privilege_type,x.is_grantable)
          ORDER BY pg_catalog.pg_get_userbyid(x.grantor) COLLATE pg_catalog."C",x.grantee=0,
          pg_catalog.pg_get_userbyid(x.grantee) COLLATE pg_catalog."C",x.privilege_type COLLATE pg_catalog."C",x.is_grantable)
          FROM pg_catalog.aclexplode(
        """+expression+") x),'[]'::pg_catalog.jsonb)";
    private const string RoleClosure="""
          WITH RECURSIVE seeds(id) AS (
            SELECT c.relowner UNION SELECT n.nspowner UNION SELECT db.datdba
            UNION SELECT x.grantee FROM pg_catalog.aclexplode(c.relacl) x UNION SELECT x.grantor FROM pg_catalog.aclexplode(c.relacl) x
            UNION SELECT x.grantee FROM pg_catalog.aclexplode(n.nspacl) x UNION SELECT x.grantor FROM pg_catalog.aclexplode(n.nspacl) x
            UNION SELECT x.grantee FROM pg_catalog.aclexplode(db.datacl) x UNION SELECT x.grantor FROM pg_catalog.aclexplode(db.datacl) x
            UNION SELECT x.grantee FROM pg_catalog.pg_attribute a CROSS JOIN LATERAL pg_catalog.aclexplode(a.attacl) x WHERE a.attrelid=c.oid
            UNION SELECT x.grantor FROM pg_catalog.pg_attribute a CROSS JOIN LATERAL pg_catalog.aclexplode(a.attacl) x WHERE a.attrelid=c.oid
            UNION SELECT p FROM pg_catalog.pg_policy pol CROSS JOIN LATERAL pg_catalog.unnest(pol.polroles) p WHERE pol.polrelid=c.oid
            UNION SELECT d.defaclrole FROM pg_catalog.pg_default_acl d WHERE d.defaclnamespace IN (0,c.relnamespace)
            UNION SELECT x.grantee FROM pg_catalog.pg_default_acl d CROSS JOIN LATERAL pg_catalog.aclexplode(d.defaclacl) x WHERE d.defaclnamespace IN (0,c.relnamespace)
            UNION SELECT x.grantor FROM pg_catalog.pg_default_acl d CROSS JOIN LATERAL pg_catalog.aclexplode(d.defaclacl) x WHERE d.defaclnamespace IN (0,c.relnamespace)
          ), closure(id) AS (
            SELECT id FROM seeds UNION SELECT other.id FROM closure r JOIN pg_catalog.pg_auth_members m ON r.id IN(m.roleid,m.member,m.grantor)
            CROSS JOIN LATERAL (VALUES(m.roleid),(m.member),(m.grantor)) other(id)
          )
        """;
    private static readonly (string Check,string Sql)[] AccessQueries=
    [
        ("ACCESS-01",Relations+"""
        SELECT c.nspname,c.relname,'table',NULL::pg_catalog.text,
          pg_catalog.jsonb_build_object('TableOwner',pg_catalog.pg_get_userbyid(c.relowner),
        """+"'TableAccess',"+Acl("COALESCE(c.relacl,pg_catalog.acldefault('r',c.relowner))")+","+
          "'SchemaAccess',pg_catalog.jsonb_build_array(pg_catalog.pg_get_userbyid(n.nspowner),"+Acl("COALESCE(n.nspacl,pg_catalog.acldefault('n',n.nspowner))")+"),"+
          "'DatabaseAccess',pg_catalog.jsonb_build_array(pg_catalog.pg_get_userbyid(db.datdba),"+Acl("COALESCE(db.datacl,pg_catalog.acldefault('d',db.datdba))")+"))::pg_catalog.text,false "+"""
        FROM relations c JOIN pg_catalog.pg_namespace n ON n.oid=c.relnamespace
        JOIN pg_catalog.pg_database db ON db.datname=pg_catalog.current_database()
        UNION ALL
        SELECT c.nspname,c.relname,a.attname,a.attname,
        """+"pg_catalog.jsonb_build_object('ColumnAccess',"+Acl("a.attacl")+")::pg_catalog.text,false "+"""
        FROM relations c JOIN pg_catalog.pg_attribute a ON a.attrelid=c.oid AND a.attnum>0 AND NOT a.attisdropped
        UNION ALL
        SELECT c.nspname,c.relname,'default:'||pg_catalog.pg_get_userbyid(d.defaclrole)||':'||(d.defaclnamespace=0)::pg_catalog.text||':'||d.defaclobjtype::pg_catalog.text,NULL::pg_catalog.text,
        """+"pg_catalog.jsonb_build_object('DefaultAccess',pg_catalog.jsonb_build_array(d.defaclnamespace=0,"+Acl("d.defaclacl")+"))::pg_catalog.text,false "+"""
        FROM relations c JOIN pg_catalog.pg_default_acl d ON d.defaclnamespace IN (0,c.relnamespace)
        UNION ALL
        SELECT c.nspname,c.relname,'roles',NULL::pg_catalog.text,
        pg_catalog.jsonb_build_object('RelatedRoles',(
        """+RoleClosure+"""
          SELECT pg_catalog.jsonb_build_array(
            (SELECT pg_catalog.jsonb_agg(pg_catalog.jsonb_build_array(r.rolname,r.rolsuper,r.rolinherit,r.rolcreaterole,r.rolcreatedb,
              r.rolcanlogin,r.rolreplication,r.rolconnlimit,r.rolvaliduntil::pg_catalog.text,r.rolbypassrls) ORDER BY r.rolname COLLATE pg_catalog."C")
              FROM pg_catalog.pg_roles r WHERE r.oid IN(SELECT id FROM closure)),
            (SELECT pg_catalog.jsonb_agg(pg_catalog.jsonb_build_array(pg_catalog.pg_get_userbyid(m.roleid),pg_catalog.pg_get_userbyid(m.member),
              pg_catalog.pg_get_userbyid(m.grantor),m.admin_option,m.inherit_option,m.set_option)
              ORDER BY pg_catalog.pg_get_userbyid(m.roleid) COLLATE pg_catalog."C",pg_catalog.pg_get_userbyid(m.member) COLLATE pg_catalog."C",pg_catalog.pg_get_userbyid(m.grantor) COLLATE pg_catalog."C")
              FROM pg_catalog.pg_auth_members m WHERE m.roleid IN(SELECT id FROM closure) OR m.member IN(SELECT id FROM closure)))
        ))::pg_catalog.text,false
        FROM relations c JOIN pg_catalog.pg_namespace n ON n.oid=c.relnamespace
        JOIN pg_catalog.pg_database db ON db.datname=pg_catalog.current_database()
        """),
        ("ACCESS-02",Relations+"""
        SELECT c.nspname,c.relname,'table',NULL::pg_catalog.text,
          pg_catalog.jsonb_build_object('RlsEnabled',c.relrowsecurity,'RlsForced',c.relforcerowsecurity)::pg_catalog.text,false FROM relations c
        UNION ALL
        SELECT c.nspname,c.relname,'policy:'||p.polname,NULL::pg_catalog.text,
          pg_catalog.jsonb_build_object('RowPolicies',pg_catalog.jsonb_build_array(p.polcmd::pg_catalog.text,p.polpermissive,
            ARRAY(SELECT CASE WHEN r=0 THEN 'PUBLIC' ELSE pg_catalog.pg_get_userbyid(r) END FROM pg_catalog.unnest(p.polroles) r
              ORDER BY CASE WHEN r=0 THEN 'PUBLIC' ELSE pg_catalog.pg_get_userbyid(r) END COLLATE pg_catalog."C"),
            pg_catalog.pg_get_expr(p.polqual,p.polrelid,false),pg_catalog.pg_get_expr(p.polwithcheck,p.polrelid,false)))::pg_catalog.text,false
        FROM relations c JOIN pg_catalog.pg_policy p ON p.polrelid=c.oid
        """)
    ];
}
