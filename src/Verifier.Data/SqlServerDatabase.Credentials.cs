using Microsoft.Data.SqlClient;
using Verifier.Core;
namespace Verifier.Data;

public sealed partial class SqlServerDatabase
{
    internal const string DatabaseCredentialQuery="""
        SELECT 'database',CAST(NULL AS nvarchar(128)),CONVERT(varbinary(max),CONVERT(nvarchar(max),c.name)) AS [__dbmv_utf16],CONVERT(varbinary(max),CONVERT(nvarchar(max),c.credential_identity)) AS [__dbmv_utf16],
          CAST(NULL AS nvarchar(100)),CAST(NULL AS nvarchar(128)),CAST(NULL AS nvarchar(36)),
          CAST(NULL AS nvarchar(50)),CAST(NULL AS bit),0,CONVERT(varbinary(max),CONVERT(nvarchar(max),p.name)) AS [__dbmv_utf16]
        FROM sys.database_scoped_credentials c
        LEFT JOIN sys.database_principals p ON p.principal_id=c.principal_id
        ORDER BY c.name COLLATE Latin1_General_100_BIN2
        """;
    // SQL Server 16's native mapping catalog was observed in the owned fixture.
    // UNION deduplicates the legacy mapping when it also occurs in that catalog.
    internal const string ServerCredentialQuery="""
        WITH bindings AS(
          SELECT principal_id,credential_id FROM sys.server_principal_credentials
          UNION SELECT principal_id,credential_id FROM sys.server_principals
            WHERE credential_id IS NOT NULL AND credential_id<>0
        )
        SELECT 'server',CONVERT(varbinary(max),CONVERT(nvarchar(max),sp.name)) AS [__dbmv_utf16],CONVERT(varbinary(max),CONVERT(nvarchar(max),c.name)) AS [__dbmv_utf16],CONVERT(varbinary(max),CONVERT(nvarchar(max),c.credential_identity)) AS [__dbmv_utf16],c.target_type,CONVERT(varbinary(max),CONVERT(nvarchar(max),cp.name)) AS [__dbmv_utf16],
          CONVERT(nvarchar(36),cp.guid),cp.version,cp.is_enabled,
          CASE WHEN c.target_id<>0 THEN 1 ELSE 0 END,CAST(NULL AS nvarchar(128))
        FROM bindings b LEFT JOIN sys.server_principals sp ON sp.principal_id=b.principal_id
        LEFT JOIN sys.credentials c ON c.credential_id=b.credential_id
        LEFT JOIN sys.cryptographic_providers cp ON cp.provider_id=c.target_id
        WHERE sp.sid IN
          (SELECT sid FROM sys.database_principals WHERE authentication_type=1 OR type IN('U','G','E','X','C','K'))
          OR sp.principal_id IS NULL
        ORDER BY sp.name COLLATE Latin1_General_100_BIN2,c.name COLLATE Latin1_General_100_BIN2
        """;
    // Only public catalog fields. No secret/password/hash is queried, decrypted
    // or exercised by making an outbound connection.
    private async Task<CatalogSection> ReadCredentialAccess(bool databaseVisible,CancellationToken token)
    {
        var scope=new Table("","",[],false);CatalogSection database,server;
        try
        {
            var rows=databaseVisible?await Native(DatabaseCredentialQuery,scope,token):[];
            database=SqlServerCatalog.Credentials(rows,databaseVisible,CollectCatalogFacts(token),CollectCatalogGaps(token));
        }
        catch(SqlException e){database=new("ACCESS-01",[],FaultReason(e,token));}
        try
        {
            var coverage=await Native("SELECT HAS_PERMS_BY_NAME(NULL,NULL,'VIEW ANY DEFINITION')",scope,token);
            var visible=coverage.Count==1&&coverage[0][0]=="1";
            var rows=visible?await Native(ServerCredentialQuery,scope,token):[];
            server=SqlServerCatalog.Credentials(rows,visible,CollectCatalogFacts(token),CollectCatalogGaps(token));
        }
        catch(SqlException e){server=new("ACCESS-01",[],FaultReason(e,token));}
        return SqlServerCatalog.MergeAccess(database,server,CollectCatalogFacts(token),CollectCatalogGaps(token),reuseFrozenFacts:true);
    }
}
