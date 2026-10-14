namespace Verifier.Data;

public sealed partial class PostgreSqlDatabase
{
    private const string TypeRelations="""
        WITH RECURSIVE relations AS (SELECT c.*,n.nspname FROM pg_catalog.pg_class c JOIN pg_catalog.pg_namespace n ON n.oid=c.relnamespace
          WHERE c.relkind IN ('r','p','f','m') AND pg_catalog.left(n.nspname,3)<>'pg_' AND n.nspname<>'information_schema'),
        edges(typeid,reference) AS (
          SELECT oid,typbasetype FROM pg_catalog.pg_type WHERE typbasetype<>0
          UNION SELECT oid,typelem FROM pg_catalog.pg_type WHERE typelem<>0
          UNION SELECT t.oid,a.atttypid FROM pg_catalog.pg_type t JOIN pg_catalog.pg_attribute a ON a.attrelid=t.typrelid AND a.attnum>0 AND NOT a.attisdropped
          UNION SELECT rngtypid,rngsubtype FROM pg_catalog.pg_range
          UNION SELECT rngmultitypid,rngtypid FROM pg_catalog.pg_range
        ), types(root,oid) AS (
          SELECT c.oid,a.atttypid FROM relations c JOIN pg_catalog.pg_attribute a ON a.attrelid=c.oid AND a.attnum>0 AND NOT a.attisdropped
          UNION SELECT t.root,e.reference FROM types t JOIN edges e ON e.typeid=t.oid
        )
        """;
    private static readonly string TypeCatalogSql="""
        SELECT c.nspname,c.relname,'type:'||pg_catalog.jsonb_build_array(n.nspname,t.typname)::pg_catalog.text,NULL::pg_catalog.text,
          pg_catalog.jsonb_build_object('UserTypeDefinitions',pg_catalog.jsonb_build_array(n.nspname,t.typname,
            pg_catalog.pg_get_userbyid(t.typowner),t.typtype::pg_catalog.text,t.typcategory::pg_catalog.text,
            t.typlen,t.typbyval,t.typalign::pg_catalog.text,t.typstorage::pg_catalog.text,t.typdelim::pg_catalog.text,t.typispreferred,t.typisdefined,
            t.typnotnull,t.typtypmod,pg_catalog.format_type(t.typbasetype,t.typtypmod),pg_catalog.format_type(t.typelem,NULL),t.typndims,
            cn.nspname,co.collname,t.typdefault,pg_catalog.pg_get_expr(t.typdefaultbin,0,false),
            COALESCE((SELECT pg_catalog.jsonb_agg(e.enumlabel ORDER BY e.enumsortorder) FROM pg_catalog.pg_enum e WHERE e.enumtypid=t.oid),'[]'::pg_catalog.jsonb),
            COALESCE((SELECT pg_catalog.jsonb_agg(pg_catalog.jsonb_build_array(con.conname,con.contype::pg_catalog.text,
              pg_catalog.pg_get_constraintdef(con.oid,false),con.convalidated,con.condeferrable,con.condeferred) ORDER BY con.conname COLLATE pg_catalog."C")
              FROM pg_catalog.pg_constraint con WHERE con.contypid=t.oid),'[]'::pg_catalog.jsonb),
            COALESCE((SELECT pg_catalog.jsonb_agg(pg_catalog.jsonb_build_array(a.attname,pg_catalog.format_type(a.atttypid,a.atttypmod),
              a.attnum,a.attnotnull,an.nspname,ac.collname,a.attndims) ORDER BY a.attnum) FROM pg_catalog.pg_attribute a
              LEFT JOIN pg_catalog.pg_collation ac ON ac.oid=a.attcollation LEFT JOIN pg_catalog.pg_namespace an ON an.oid=ac.collnamespace
              WHERE a.attrelid=t.typrelid AND a.attnum>0 AND NOT a.attisdropped),'[]'::pg_catalog.jsonb),
            (SELECT pg_catalog.jsonb_build_array(pg_catalog.format_type(r.rngsubtype,NULL),rn.nspname,rc.collname,
              onsp.nspname,op.opcname,r.rngcanonical::pg_catalog.regprocedure::pg_catalog.text,r.rngsubdiff::pg_catalog.regprocedure::pg_catalog.text)
              FROM pg_catalog.pg_range r LEFT JOIN pg_catalog.pg_collation rc ON rc.oid=r.rngcollation LEFT JOIN pg_catalog.pg_namespace rn ON rn.oid=rc.collnamespace
              JOIN pg_catalog.pg_opclass op ON op.oid=r.rngsubopc JOIN pg_catalog.pg_namespace onsp ON onsp.oid=op.opcnamespace WHERE r.rngtypid=t.oid),
            ext.extname,ext.extversion,
        """+Acl("COALESCE(t.typacl,pg_catalog.acldefault('T',t.typowner))")+"""
          ))::pg_catalog.text,ext.oid IS NOT NULL OR t.typtype NOT IN ('d','e','c','r','m') AND t.typelem=0
        FROM relations c JOIN types u ON u.root=c.oid JOIN pg_catalog.pg_type t ON t.oid=u.oid JOIN pg_catalog.pg_namespace n ON n.oid=t.typnamespace
        LEFT JOIN pg_catalog.pg_collation co ON co.oid=t.typcollation LEFT JOIN pg_catalog.pg_namespace cn ON cn.oid=co.collnamespace
        LEFT JOIN pg_catalog.pg_depend d ON d.classid='pg_catalog.pg_type'::pg_catalog.regclass AND d.objid=t.oid AND d.refclassid='pg_catalog.pg_extension'::pg_catalog.regclass AND d.deptype='e'
        LEFT JOIN pg_catalog.pg_extension ext ON ext.oid=d.refobjid
        WHERE n.nspname<>'pg_catalog'
        """;
    private static readonly (string Check,string Sql)[] TypeQueries=[("OBJECT-03",TypeRelations+TypeCatalogSql)];
}
