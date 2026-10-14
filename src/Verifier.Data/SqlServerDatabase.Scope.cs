using System.Text.Json;
using Verifier.Core;
namespace Verifier.Data;

public sealed partial class SqlServerDatabase
{
    private IReadOnlyList<CatalogSection>? databaseBaseline;
    private async Task<IReadOnlyList<CatalogSection>> ReadDatabaseScope(CancellationToken token)
    {
        using var activity=ObservationActivity.StartActivity("DatabaseCatalog",System.Diagnostics.ActivityKind.Internal);
        var dummy=new Table("","",[],false);
        var definitions=await Native("SELECT CONVERT(varbinary(max),CONVERT(nvarchar(max),collation_name)) AS [__dbmv_utf16],compatibility_level,is_read_committed_snapshot_on,snapshot_isolation_state FROM sys.databases WHERE database_id=DB_ID()",dummy,token);
        var visibility=await Native("SELECT HAS_PERMS_BY_NAME(DB_NAME(),'DATABASE','VIEW DEFINITION')",dummy,token);
        var schemas=await Native("SELECT CONVERT(varbinary(max),CONVERT(nvarchar(max),name)) AS [__dbmv_utf16] FROM sys.schemas WHERE name NOT IN ('sys','INFORMATION_SCHEMA') ORDER BY name",dummy,token);
        var facts=CollectCatalogFacts(token);
        if(definitions.Count!=1)throw new VerificationException(Reason.IdentityUnknown);
        facts.Add(new("database",null,"DatabaseDefinition",SqlServerMetadata.Serialize(definitions[0])));
        foreach(var schema in schemas)facts.Add(new(SqlServerMetadata.Serialize(new[]{"schema",schema[0]}),null,"SchemaPresence",schema[0]));
        return new CatalogSection[]{new("SCHEMA-01",facts.Snapshot(),complete&&visibility.Count==1&&visibility[0][0]=="1"?Reason.None:Reason.VisibilityUnknown),await ReadDatabaseAccess(token)}
            .Concat((await ReadObjects(null,token)).Sections).Append(await ReadTypes(null,token)).ToArray();
    }
}
