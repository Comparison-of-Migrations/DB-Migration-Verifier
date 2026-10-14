namespace Verifier.Data;

public sealed partial class PostgreSqlDatabase
{
    private const string ObjectRelations="""
        WITH RECURSIVE relations AS (SELECT c.*,n.nspname FROM pg_catalog.pg_class c JOIN pg_catalog.pg_namespace n ON n.oid=c.relnamespace
          WHERE c.relkind IN ('r','p','f','m') AND pg_catalog.left(n.nspname,3)<>'pg_' AND n.nspname<>'information_schema'),
        views(root,oid) AS (
          SELECT c.oid,c.oid FROM relations c
          UNION SELECT v.root,r.ev_class FROM views v JOIN pg_catalog.pg_depend d ON d.refclassid='pg_catalog.pg_class'::pg_catalog.regclass AND d.refobjid=v.oid
            JOIN pg_catalog.pg_rewrite r ON d.classid='pg_catalog.pg_rewrite'::pg_catalog.regclass AND r.oid=d.objid
        ), seeds(root,oid) AS (
          SELECT v.root,t.tgfoid FROM views v JOIN pg_catalog.pg_trigger t ON t.tgrelid=v.oid
          UNION SELECT v.root,d.objid FROM views v JOIN pg_catalog.pg_depend d ON d.refclassid='pg_catalog.pg_class'::pg_catalog.regclass
            AND d.refobjid=v.oid AND d.classid='pg_catalog.pg_proc'::pg_catalog.regclass
          UNION SELECT v.root,d.refobjid FROM views v JOIN pg_catalog.pg_rewrite r ON r.ev_class=v.oid
            JOIN pg_catalog.pg_depend d ON d.classid='pg_catalog.pg_rewrite'::pg_catalog.regclass AND d.objid=r.oid AND d.refclassid='pg_catalog.pg_proc'::pg_catalog.regclass
          UNION SELECT c.oid,d.refobjid FROM relations c JOIN pg_catalog.pg_constraint con ON con.conrelid=c.oid
            JOIN pg_catalog.pg_depend d ON d.classid='pg_catalog.pg_constraint'::pg_catalog.regclass AND d.objid=con.oid AND d.refclassid='pg_catalog.pg_proc'::pg_catalog.regclass
          UNION SELECT c.oid,d.refobjid FROM relations c JOIN pg_catalog.pg_attrdef a ON a.adrelid=c.oid
            JOIN pg_catalog.pg_depend d ON d.classid='pg_catalog.pg_attrdef'::pg_catalog.regclass AND d.objid=a.oid AND d.refclassid='pg_catalog.pg_proc'::pg_catalog.regclass
          UNION SELECT c.oid,d.refobjid FROM relations c JOIN pg_catalog.pg_policy p ON p.polrelid=c.oid
            JOIN pg_catalog.pg_depend d ON d.classid='pg_catalog.pg_policy'::pg_catalog.regclass AND d.objid=p.oid AND d.refclassid='pg_catalog.pg_proc'::pg_catalog.regclass
        ), routines(root,oid) AS (
          SELECT root,oid FROM seeds UNION SELECT r.root,d.refobjid FROM routines r JOIN pg_catalog.pg_depend d
            ON d.classid='pg_catalog.pg_proc'::pg_catalog.regclass AND d.objid=r.oid AND d.refclassid='pg_catalog.pg_proc'::pg_catalog.regclass
        )
        """;
    private static readonly string ObjectCatalogSql="""
        SELECT c.nspname,c.relname,'trigger:'||pg_catalog.jsonb_build_array(vn.nspname,vc.relname,
          CASE WHEN t.tgisinternal THEN pg_catalog.jsonb_build_array('internal',cn.nspname,cc.relname,con.conname,t.tgtype,pn.nspname,p.proname)::pg_catalog.text ELSE t.tgname::pg_catalog.text END)::pg_catalog.text,NULL::pg_catalog.text,
          pg_catalog.jsonb_build_object('TriggerDefinitions',pg_catalog.jsonb_build_array(
            CASE WHEN NOT t.tgisinternal THEN pg_catalog.pg_get_triggerdef(t.oid,false) END,t.tgenabled::pg_catalog.text,t.tgisinternal,
            pn.nspname,p.proname,pg_catalog.pg_get_function_identity_arguments(p.oid),t.tgtype,t.tgdeferrable,t.tginitdeferred,
            con.conname,cn.nspname,cc.relname,fn.nspname,fc.relname,
            CASE WHEN pt.tgisinternal THEN pg_catalog.jsonb_build_array(pcn.nspname,pcc.relname,pcon.conname,pt.tgtype,pt.tgfoid::pg_catalog.regproc::pg_catalog.text) ELSE pg_catalog.to_jsonb(pt.tgname::pg_catalog.text) END,t.tgoldtable,t.tgnewtable))::pg_catalog.text,false
        FROM relations c JOIN views v ON v.root=c.oid JOIN pg_catalog.pg_class vc ON vc.oid=v.oid JOIN pg_catalog.pg_namespace vn ON vn.oid=vc.relnamespace
        JOIN pg_catalog.pg_trigger t ON t.tgrelid=v.oid JOIN pg_catalog.pg_proc p ON p.oid=t.tgfoid JOIN pg_catalog.pg_namespace pn ON pn.oid=p.pronamespace
        LEFT JOIN pg_catalog.pg_constraint con ON con.oid=t.tgconstraint
        LEFT JOIN pg_catalog.pg_class cc ON cc.oid=con.conrelid LEFT JOIN pg_catalog.pg_namespace cn ON cn.oid=cc.relnamespace
        LEFT JOIN pg_catalog.pg_class fc ON fc.oid=con.confrelid LEFT JOIN pg_catalog.pg_namespace fn ON fn.oid=fc.relnamespace
        LEFT JOIN pg_catalog.pg_trigger pt ON pt.oid=t.tgparentid LEFT JOIN pg_catalog.pg_constraint pcon ON pcon.oid=pt.tgconstraint
        LEFT JOIN pg_catalog.pg_class pcc ON pcc.oid=pcon.conrelid LEFT JOIN pg_catalog.pg_namespace pcn ON pcn.oid=pcc.relnamespace
        UNION ALL
        SELECT c.nspname,c.relname,'routine:'||pg_catalog.jsonb_build_array(pn.nspname,p.proname,pg_catalog.pg_get_function_identity_arguments(p.oid))::pg_catalog.text,NULL::pg_catalog.text,
          pg_catalog.jsonb_build_object('RelatedRoutines',pg_catalog.jsonb_build_array(
            CASE WHEN p.prokind<>'a' THEN pg_catalog.pg_get_functiondef(p.oid) END,pg_catalog.pg_get_userbyid(p.proowner),l.lanname,
            p.prokind::pg_catalog.text,p.prosecdef,p.proleakproof,p.proisstrict,p.proretset,p.provolatile::pg_catalog.text,p.proparallel::pg_catalog.text,
            p.procost::pg_catalog.text,p.prorows::pg_catalog.text,
        """+Acl("COALESCE(p.proacl,pg_catalog.acldefault('f',p.proowner))")+"""
            ))::pg_catalog.text,p.prokind='a' OR l.lanname NOT IN ('sql','plpgsql','internal')
        FROM relations c JOIN routines r ON r.root=c.oid JOIN pg_catalog.pg_proc p ON p.oid=r.oid
        JOIN pg_catalog.pg_namespace pn ON pn.oid=p.pronamespace JOIN pg_catalog.pg_language l ON l.oid=p.prolang
        UNION ALL
        SELECT c.nspname,c.relname,'view:'||pg_catalog.jsonb_build_array(vn.nspname,vc.relname)::pg_catalog.text,NULL::pg_catalog.text,
          pg_catalog.jsonb_build_object('RelatedViews',pg_catalog.jsonb_build_array(vn.nspname,vc.relname,vc.relkind::pg_catalog.text,
            pg_catalog.pg_get_viewdef(vc.oid,false),pg_catalog.pg_get_userbyid(vc.relowner),
            ARRAY(SELECT option FROM pg_catalog.unnest(vc.reloptions) option ORDER BY option),vc.relispopulated,
            (SELECT pg_catalog.jsonb_agg(pg_catalog.jsonb_build_array(a.attname,pg_catalog.format_type(a.atttypid,a.atttypmod),a.attnum,
              a.attnotnull,cn.nspname,co.collname) ORDER BY a.attnum) FROM pg_catalog.pg_attribute a
              LEFT JOIN pg_catalog.pg_collation co ON co.oid=a.attcollation LEFT JOIN pg_catalog.pg_namespace cn ON cn.oid=co.collnamespace
              WHERE a.attrelid=vc.oid AND a.attnum>0 AND NOT a.attisdropped),
        """+Acl("COALESCE(vc.relacl,pg_catalog.acldefault('r',vc.relowner))")+"""
          ))::pg_catalog.text,false
        FROM relations c JOIN views v ON v.root=c.oid JOIN pg_catalog.pg_class vc ON vc.oid=v.oid AND vc.relkind IN ('v','m')
        JOIN pg_catalog.pg_namespace vn ON vn.oid=vc.relnamespace
        """;
    private static readonly (string Check,string Sql)[] ObjectQueries=[("OBJECT-01",ObjectRelations+ObjectCatalogSql)];
}
