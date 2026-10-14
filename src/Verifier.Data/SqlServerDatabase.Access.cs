using Microsoft.Data.SqlClient;
using Verifier.Core;
namespace Verifier.Data;

public sealed partial class SqlServerDatabase
{
    internal const string PrincipalQuery="""
        SELECT CONVERT(varbinary(max),CONVERT(nvarchar(max),p.name)) AS [__dbmv_utf16],p.type,p.authentication_type,CONVERT(varbinary(max),CONVERT(nvarchar(max),p.default_schema_name)) AS [__dbmv_utf16],
               CONVERT(varchar(172),p.sid,2),p.is_fixed_role,CONVERT(varbinary(max),CONVERT(nvarchar(max),o.name)) AS [__dbmv_utf16],p.allow_encrypted_value_modifications,CONVERT(varbinary(max),CONVERT(nvarchar(max),p.default_language_name)) AS [__dbmv_utf16]
        FROM sys.database_principals p LEFT JOIN sys.database_principals o ON o.principal_id=p.owning_principal_id
        ORDER BY p.name COLLATE Latin1_General_100_BIN2
        """;
    internal const string MembershipQuery="""
        SELECT CONVERT(varbinary(max),CONVERT(nvarchar(max),r.name)) AS [__dbmv_utf16],CONVERT(varbinary(max),CONVERT(nvarchar(max),m.name)) AS [__dbmv_utf16] FROM sys.database_role_members rm
        LEFT JOIN sys.database_principals r ON r.principal_id=rm.role_principal_id
        LEFT JOIN sys.database_principals m ON m.principal_id=rm.member_principal_id
        ORDER BY r.name COLLATE Latin1_General_100_BIN2,m.name COLLATE Latin1_General_100_BIN2
        """;
    internal const string GrantQuery="""
        SELECT p.class,CONVERT(varbinary(max),CONVERT(nvarchar(max),CASE WHEN p.class=3 THEN sc.name WHEN p.class=6 THEN ts.name WHEN p.class=10 THEN xs.name WHEN p.class=1 THEN COALESCE(os.name,OBJECT_SCHEMA_NAME(p.major_id)) ELSE os.name END COLLATE Latin1_General_100_BIN2)) AS [__dbmv_utf16],
          CONVERT(varbinary(max),CONVERT(nvarchar(max),CASE p.class WHEN 0 THEN '' WHEN 1 THEN COALESCE(ob.name,OBJECT_NAME(p.major_id)) WHEN 3 THEN sc.name WHEN 4 THEN pr.name
            WHEN 5 THEN asm.name WHEN 6 THEN ty.name WHEN 10 THEN xc.name WHEN 15 THEN mt.name
            WHEN 16 THEN ct.name WHEN 17 THEN svc.name WHEN 18 THEN rb.name WHEN 19 THEN rt.name
            WHEN 23 THEN ft.name WHEN 24 THEN sk.name WHEN 25 THEN cert.name WHEN 26 THEN ak.name
            WHEN 29 THEN sl.name WHEN 31 THEN spl.name WHEN 32 THEN cred.name WHEN 34 THEN lang.language END COLLATE Latin1_General_100_BIN2)) AS [__dbmv_utf16],
          CONVERT(varbinary(max),CONVERT(nvarchar(max),CASE WHEN p.class=1 AND p.minor_id>0 THEN COALESCE(c.name,COL_NAME(p.major_id,p.minor_id),'') END COLLATE Latin1_General_100_BIN2)) AS [__dbmv_utf16],CONVERT(varbinary(max),CONVERT(nvarchar(max),grantee.name)) AS [__dbmv_utf16],CONVERT(varbinary(max),CONVERT(nvarchar(max),grantor.name)) AS [__dbmv_utf16],p.permission_name,p.state
        FROM sys.database_permissions p
        LEFT JOIN sys.all_objects ob ON p.class=1 AND ob.object_id=p.major_id
        LEFT JOIN sys.schemas os ON os.schema_id=ob.schema_id
        LEFT JOIN sys.schemas sc ON p.class=3 AND sc.schema_id=p.major_id
        LEFT JOIN sys.all_columns c ON p.class=1 AND c.object_id=p.major_id AND c.column_id=p.minor_id
        LEFT JOIN sys.database_principals pr ON p.class=4 AND pr.principal_id=p.major_id
        LEFT JOIN sys.assemblies asm ON p.class=5 AND asm.assembly_id=p.major_id
        LEFT JOIN sys.types ty ON p.class=6 AND ty.user_type_id=p.major_id
        LEFT JOIN sys.schemas ts ON ts.schema_id=ty.schema_id
        LEFT JOIN sys.xml_schema_collections xc ON p.class=10 AND xc.xml_collection_id=p.major_id
        LEFT JOIN sys.schemas xs ON xs.schema_id=xc.schema_id
        LEFT JOIN sys.service_message_types mt ON p.class=15 AND mt.message_type_id=p.major_id
        LEFT JOIN sys.service_contracts ct ON p.class=16 AND ct.service_contract_id=p.major_id
        LEFT JOIN sys.services svc ON p.class=17 AND svc.service_id=p.major_id
        LEFT JOIN sys.remote_service_bindings rb ON p.class=18 AND rb.remote_service_binding_id=p.major_id
        LEFT JOIN sys.routes rt ON p.class=19 AND rt.route_id=p.major_id
        LEFT JOIN sys.fulltext_catalogs ft ON p.class=23 AND ft.fulltext_catalog_id=p.major_id
        LEFT JOIN sys.symmetric_keys sk ON p.class=24 AND sk.symmetric_key_id=p.major_id
        LEFT JOIN sys.certificates cert ON p.class=25 AND cert.certificate_id=p.major_id
        LEFT JOIN sys.asymmetric_keys ak ON p.class=26 AND ak.asymmetric_key_id=p.major_id
        LEFT JOIN sys.fulltext_stoplists sl ON p.class=29 AND sl.stoplist_id=p.major_id
        LEFT JOIN sys.registered_search_property_lists spl ON p.class=31 AND spl.property_list_id=p.major_id
        LEFT JOIN sys.database_scoped_credentials cred ON p.class=32 AND cred.credential_id=p.major_id
        LEFT JOIN sys.external_languages lang ON p.class=34 AND lang.external_language_id=p.major_id
        LEFT JOIN sys.database_principals grantee ON grantee.principal_id=p.grantee_principal_id
        LEFT JOIN sys.database_principals grantor ON grantor.principal_id=p.grantor_principal_id
        ORDER BY p.class,COALESCE(sc.name,ts.name,xs.name,os.name,CASE WHEN p.class=1 THEN OBJECT_SCHEMA_NAME(p.major_id) END) COLLATE Latin1_General_100_BIN2,
          COALESCE(ob.name,sc.name,pr.name,asm.name,ty.name,xc.name,mt.name,ct.name,svc.name,rb.name,rt.name,
            ft.name,sk.name,cert.name,ak.name,sl.name,spl.name,cred.name,lang.language,CASE WHEN p.class=1 THEN OBJECT_NAME(p.major_id) END) COLLATE Latin1_General_100_BIN2,
          c.name COLLATE Latin1_General_100_BIN2,grantee.name COLLATE Latin1_General_100_BIN2,
          grantor.name COLLATE Latin1_General_100_BIN2,p.permission_name,p.state
        """;
    internal static string GrantQueryFor(bool credentialCatalogVisible,bool tableScope=false)
    {
        var query=credentialCatalogVisible?GrantQuery:GrantQuery
            .Replace("WHEN 32 THEN cred.name","WHEN 32 THEN NULL",StringComparison.Ordinal)
            .Replace("LEFT JOIN sys.database_scoped_credentials cred ON p.class=32 AND cred.credential_id=p.major_id\n","",StringComparison.Ordinal)
            .Replace("spl.name,cred.name,lang.language","spl.name,lang.language",StringComparison.Ordinal);
        // AddAccess discards resolved grants on other objects. Filter those
        // before decoding/spilling them, while retaining every unresolved or
        // malformed row that can affect global coverage, reasons or gaps.
        return !tableScope?query:query.Replace("ORDER BY p.class,","""
            WHERE p.class IS NULL OR p.class<>1 OR p.major_id=
            """+ObjectFilter+"""

              OR ob.object_id IS NULL OR grantee.name IS NULL OR grantor.name IS NULL
              OR p.permission_name IS NULL OR p.state IS NULL OR p.state NOT IN ('G','W','D','R')
              OR (p.minor_id>0 AND (c.name IS NULL OR c.name=N''))
            ORDER BY p.class,
            """,StringComparison.Ordinal);
    }
    private async Task<bool> SecurityVisible(CancellationToken token)
    {
        var rows=await NativeShared("SELECT HAS_PERMS_BY_NAME(DB_NAME(),'DATABASE','VIEW SECURITY DEFINITION'),HAS_PERMS_BY_NAME(DB_NAME(),'DATABASE','VIEW DEFINITION'),HAS_PERMS_BY_NAME(DB_NAME(),'DATABASE','ALTER ANY USER'),HAS_PERMS_BY_NAME(DB_NAME(),'DATABASE','ALTER ANY ROLE')",new("","",[],false),token);
        return complete&&rows.Count==1&&rows[0].All(x=>x=="1");
    }
    private async Task<bool> RowSecurityVisible(CancellationToken token)
    {
        var rows=await NativeShared("SELECT HAS_PERMS_BY_NAME(DB_NAME(),'DATABASE','VIEW SECURITY DEFINITION'),HAS_PERMS_BY_NAME(DB_NAME(),'DATABASE','VIEW DEFINITION')",new("","",[],false),token);
        return complete&&rows.Count==1&&rows[0].All(x=>x=="1");
    }
    private async Task<CatalogSection> ReadDatabaseAccess(CancellationToken token)
    {
        token.ThrowIfCancellationRequested();
        if(inventory!=null)return await ReadDatabaseAccessFresh(token);
        return inspectionAccess??=await ReadDatabaseAccessFresh(token);
    }
    private async Task<CatalogSection> ReadDatabaseAccessFresh(CancellationToken token,Table? table=null)
    {
        var dummy=new Table("","",[],false);
        try
        {
            using var inputs=ObservationActivity.StartActivity("AccessInputs");
            var visible=await SecurityVisible(token);
            var principals=await Native(PrincipalQuery,dummy,token);var memberships=await Native(MembershipQuery,dummy,token);
            // This catalog requires CONTROL DATABASE, independently of public
            // permission visibility. Omit the protected join when unavailable;
            // credential permissions then remain unresolved, not absent.
            var credentialCoverage=await NativeShared("SELECT HAS_PERMS_BY_NAME(DB_NAME(),'DATABASE','CONTROL')",dummy,token);
            var credentialVisible=credentialCoverage.Count==1&&credentialCoverage[0][0]=="1";
            var grants=await Native(GrantQueryFor(credentialVisible,table!=null),table??dummy,token);
            var owners=await Native("""
                SELECT 'database',CONVERT(varbinary(max),CONVERT(nvarchar(max),N'')) AS [__dbmv_utf16],CONVERT(varbinary(max),CONVERT(nvarchar(max),CONVERT(varchar(172),owner_sid,2))) AS [__dbmv_utf16] FROM sys.databases WHERE database_id=DB_ID()
                UNION ALL SELECT 'schema',CONVERT(varbinary(max),CONVERT(nvarchar(max),s.name)) AS [__dbmv_utf16],CONVERT(varbinary(max),CONVERT(nvarchar(max),p.name)) AS [__dbmv_utf16] FROM sys.schemas s
                  LEFT JOIN sys.database_principals p ON p.principal_id=s.principal_id
                  WHERE s.name NOT IN ('sys','INFORMATION_SCHEMA')
                ORDER BY 1,2
                """,dummy,token);
            var server=await ReadServerAccess(principals,token);
            inputs?.Stop();
            using var roles=ObservationActivity.StartActivity("AccessPrincipals");
            var principalSection=SqlServerCatalog.Principals(principals,memberships,grants,owners,visible,server,CollectCatalogFacts(token),CollectCatalogGaps(token));roles?.Stop();
            using var credentials=ObservationActivity.StartActivity("AccessCredentials");
            var credentialSection=await ReadCredentialAccess(credentialVisible,token);credentials?.Stop();
            using var merge=ObservationActivity.StartActivity("AccessMerge");
            return SqlServerCatalog.MergeAccess(principalSection,credentialSection,CollectCatalogFacts(token),CollectCatalogGaps(token),reuseFrozenFacts:true);
        }
        catch(SqlException e){return new("ACCESS-01",[],FaultReason(e,token));}
    }
    private async Task<CatalogSection> ReadServerAccess(IReadOnlyList<string?[]> databasePrincipals,CancellationToken token)
    {
        var dummy=new Table("","",[],false);
        try
        {
            var coverage=await NativeShared("SELECT HAS_PERMS_BY_NAME(NULL,NULL,'VIEW ANY SECURITY DEFINITION'),HAS_PERMS_BY_NAME(NULL,NULL,'VIEW ANY DEFINITION'),HAS_PERMS_BY_NAME(NULL,NULL,'ALTER ANY LOGIN'),HAS_PERMS_BY_NAME(NULL,NULL,'ALTER ANY SERVER ROLE')",dummy,token);
            var principals=await Native("""
                SELECT CONVERT(varbinary(max),CONVERT(nvarchar(max),p.name)) AS [__dbmv_utf16],p.type,CONVERT(varchar(172),p.sid,2),p.is_disabled,CONVERT(varbinary(max),CONVERT(nvarchar(max),p.default_database_name)) AS [__dbmv_utf16],CONVERT(varbinary(max),CONVERT(nvarchar(max),p.default_language_name)) AS [__dbmv_utf16],
                  CONVERT(varbinary(max),CONVERT(nvarchar(max),owner.name)) AS [__dbmv_utf16],p.is_fixed_role,l.is_policy_checked,l.is_expiration_checked,
                  CAST(CASE WHEN p.credential_id IS NULL OR p.credential_id=0 THEN 0 ELSE 1 END AS bit),
                  CONVERT(varbinary(max),CONVERT(nvarchar(max),l.type_desc)) AS [__dbmv_utf16],CONVERT(varbinary(max),CONVERT(nvarchar(max),p.type_desc)) AS [__dbmv_utf16]
                FROM sys.server_principals p LEFT JOIN sys.server_principals owner ON owner.principal_id=p.owning_principal_id
                LEFT JOIN sys.sql_logins l ON l.principal_id=p.principal_id
                ORDER BY p.name COLLATE Latin1_General_100_BIN2
                """,dummy,token);
            var memberships=await Native("""
                SELECT CONVERT(varbinary(max),CONVERT(nvarchar(max),role.name)) AS [__dbmv_utf16],CONVERT(varbinary(max),CONVERT(nvarchar(max),member.name)) AS [__dbmv_utf16] FROM sys.server_role_members rm
                LEFT JOIN sys.server_principals role ON role.principal_id=rm.role_principal_id
                LEFT JOIN sys.server_principals member ON member.principal_id=rm.member_principal_id
                ORDER BY role.name COLLATE Latin1_General_100_BIN2,member.name COLLATE Latin1_General_100_BIN2
                """,dummy,token);
            var grants=await Native("""
                SELECT p.class,CONVERT(varbinary(max),CONVERT(nvarchar(max),p.class_desc)) AS [__dbmv_utf16],CONVERT(varbinary(max),CONVERT(nvarchar(max),CASE p.class WHEN 100 THEN '' WHEN 101 THEN subject.name WHEN 105 THEN endpoint.name END)) AS [__dbmv_utf16],
                  CONVERT(varbinary(max),CONVERT(nvarchar(max),grantee.name)) AS [__dbmv_utf16],CONVERT(varbinary(max),CONVERT(nvarchar(max),grantor.name)) AS [__dbmv_utf16],p.permission_name,p.state
                FROM sys.server_permissions p
                LEFT JOIN sys.server_principals subject ON p.class=101 AND subject.principal_id=p.major_id
                LEFT JOIN sys.endpoints endpoint ON p.class=105 AND endpoint.endpoint_id=p.major_id
                LEFT JOIN sys.server_principals grantee ON grantee.principal_id=p.grantee_principal_id
                LEFT JOIN sys.server_principals grantor ON grantor.principal_id=p.grantor_principal_id
                ORDER BY p.class,subject.name COLLATE Latin1_General_100_BIN2,endpoint.name COLLATE Latin1_General_100_BIN2,
                  grantee.name COLLATE Latin1_General_100_BIN2,grantor.name COLLATE Latin1_General_100_BIN2,p.permission_name,p.state
                """,dummy,token);
            return SqlServerCatalog.ServerPrincipals(databasePrincipals,principals,memberships,grants,coverage.Count==1&&coverage[0].All(x=>x=="1"),CollectCatalogFacts(token),CollectCatalogGaps(token));
        }
        catch(SqlException e){return new("ACCESS-01",[],FaultReason(e,token));}
    }
    private async Task<Table> AddAccess(Table table,CancellationToken token)
    {
        CatalogSection access,policies;
        try
        {
            var visible=await RowSecurityVisible(token);
            var owners=await Native("""
                SELECT CONVERT(varbinary(max),CONVERT(nvarchar(max),p.name)) AS [__dbmv_utf16],CONVERT(varbinary(max),CONVERT(nvarchar(max),sp.name)) AS [__dbmv_utf16] FROM sys.tables t JOIN sys.schemas s ON s.schema_id=t.schema_id
                LEFT JOIN sys.database_principals p ON p.principal_id=t.principal_id
                LEFT JOIN sys.database_principals sp ON sp.principal_id=s.principal_id
                WHERE t.object_id=object_id(QUOTENAME(CONVERT(nvarchar(128),@schema))+'.'+QUOTENAME(CONVERT(nvarchar(128),@table)),'U')
                """,table,token);
            var db=inventory==null?await ReadDatabaseAccess(token):await ReadDatabaseAccessFresh(token,table);
            using var selected=ObservationActivity.StartActivity("SelectTableAccess");
            var facts=CollectCatalogFacts(token);facts.AddRange(db.Facts.Where(f=>f.Attribute is not("TableAccess" or "DatabaseColumnAccess")||
                IsSelectedGrant(f.Key,table)));
            if(owners.Count!=1)throw new VerificationException(Reason.ObservationChanged);
            facts.Add(new("table",null,"TableOwner",SqlServerCatalog.Key(owners[0][0],owners[0][1])));
            access=db with{Facts=facts.Snapshot()};
            selected?.Stop();
            var rows=await Native("""
                SELECT CONVERT(varbinary(max),CONVERT(nvarchar(max),s.name)) AS [__dbmv_utf16],CONVERT(varbinary(max),CONVERT(nvarchar(max),pol.name)) AS [__dbmv_utf16],CONCAT(p.predicate_type,':',p.operation),pol.is_enabled,pol.is_schema_bound,
                       pol.is_not_for_replication,CONVERT(varbinary(max),CONVERT(nvarchar(max),p.predicate_definition)) AS [__dbmv_utf16],p.predicate_type,p.operation
                FROM sys.security_predicates p JOIN sys.security_policies pol ON pol.object_id=p.object_id
                JOIN sys.schemas s ON s.schema_id=pol.schema_id
                WHERE p.target_object_id=object_id(QUOTENAME(CONVERT(nvarchar(128),@schema))+'.'+QUOTENAME(CONVERT(nvarchar(128),@table)),'U')
                ORDER BY s.name COLLATE Latin1_General_100_BIN2,pol.name COLLATE Latin1_General_100_BIN2,p.security_predicate_id
                """,table,token);
            policies=SqlServerCatalog.Policies(rows,visible,CollectCatalogFacts(token),CollectCatalogGaps(token));
        }
        catch(SqlException e){access=new("ACCESS-01",[],FaultReason(e,token));policies=new("ACCESS-02",[],FaultReason(e,token));}
        return table with{Catalog=table.Catalog.Concat(new[]{access,policies}).ToArray()};
    }
    private static bool IsSelectedGrant(string key,Table table)
    {
        var parts=SqlServerMetadata.Deserialize<string?[]>(key)!;
        return parts.Length>=4&&parts[2]==table.Schema&&parts[3]==table.Name;
    }
}
