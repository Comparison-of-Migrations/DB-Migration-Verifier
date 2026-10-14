namespace Verifier.Data;

public sealed partial class PostgreSqlDatabase
{
    // A custom GUC may contain an application password or token. Never fetch its
    // value. Whitelisted native settings are compared exactly, not applied.
    private const string SafeConfigurationNames="'search_path','row_security','default_transaction_read_only','default_transaction_isolation','default_transaction_deferrable','statement_timeout','lock_timeout','idle_in_transaction_session_timeout','idle_session_timeout','transaction_timeout','session_replication_role','timezone','datestyle','intervalstyle','standard_conforming_strings','client_encoding','bytea_output','extra_float_digits','lc_numeric','lc_monetary','lc_time'";
    private static readonly string ConfigurationValuesSql="SELECT COALESCE(pg_catalog.jsonb_agg(value ORDER BY value COLLATE pg_catalog.\"C\") FILTER(WHERE pg_catalog.lower(pg_catalog.split_part(value,'=',1)) IN ("+SafeConfigurationNames+")),'[]'::pg_catalog.jsonb) known,"+
        "COALESCE(pg_catalog.jsonb_agg(pg_catalog.split_part(value,'=',1) ORDER BY pg_catalog.split_part(value,'=',1) COLLATE pg_catalog.\"C\") FILTER(WHERE pg_catalog.lower(pg_catalog.split_part(value,'=',1)) NOT IN ("+SafeConfigurationNames+")),'[]'::pg_catalog.jsonb) names FROM pg_catalog.unnest(config.setconfig) value";
    private static readonly string ConfigurationSql=Relations+"""
        SELECT c.nspname,c.relname,
          pg_catalog.jsonb_build_array('configuration',config.setdatabase=0,config.setrole=0,r.rolname)::pg_catalog.text,NULL::pg_catalog.text,
          pg_catalog.jsonb_strip_nulls(pg_catalog.jsonb_build_object('RoleConfiguration',NULLIF(cfg.known,'[]'::pg_catalog.jsonb),
            'UninspectedConfiguration',CASE WHEN cfg.names='[]'::pg_catalog.jsonb THEN NULL
              ELSE pg_catalog.jsonb_build_array(cfg.names,config.xmin::pg_catalog.text,config.ctid::pg_catalog.text) END))::pg_catalog.text,false
        FROM relations c JOIN pg_catalog.pg_namespace n ON n.oid=c.relnamespace
        JOIN pg_catalog.pg_database db ON db.datname=pg_catalog.current_database()
        JOIN pg_catalog.pg_db_role_setting config ON config.setdatabase IN(0,db.oid)
        LEFT JOIN pg_catalog.pg_roles r ON r.oid=config.setrole
        CROSS JOIN LATERAL (
        """+ConfigurationValuesSql+"""
        ) cfg
        WHERE config.setdatabase=db.oid OR config.setrole=0 OR config.setrole IN(
        """+RoleClosure+" SELECT id FROM closure)";
}
