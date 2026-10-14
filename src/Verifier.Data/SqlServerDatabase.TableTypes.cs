using System.Globalization;
using Verifier.Core;
namespace Verifier.Data;

public sealed partial class SqlServerDatabase
{
    // IDs are used only to bind native reads, never as cross-database identities.
    private async Task<CatalogSection> ReadTableType(string schema,string name,string nativeId,Table scope,bool visible,CancellationToken token)
    {
        var typeId=int.Parse(nativeId,CultureInfo.InvariantCulture).ToString(CultureInfo.InvariantCulture);
        Task<IReadOnlyList<string?[]>> Read(string sql)=>Native(sql.Replace("@type",typeId,StringComparison.Ordinal),scope,token);
        var options=await Read("SELECT is_memory_optimized FROM sys.table_types WHERE user_type_id=@type");
        var columns=await Read("""
            SELECT CONVERT(varbinary(max),CONVERT(nvarchar(max),c.name)) AS [__dbmv_utf16],c.column_id,CONVERT(varbinary(max),CONVERT(nvarchar(max),ts.name)) AS [__dbmv_utf16],CONVERT(varbinary(max),CONVERT(nvarchar(max),t.name)) AS [__dbmv_utf16],c.max_length,c.precision,c.scale,CONVERT(varbinary(max),CONVERT(nvarchar(max),c.collation_name)) AS [__dbmv_utf16],
              c.is_nullable,c.is_identity,c.is_computed,CONVERT(varbinary(max),CONVERT(nvarchar(max),OBJECT_DEFINITION(c.default_object_id))) AS [__dbmv_utf16],CONVERT(varbinary(max),CONVERT(nvarchar(max),cc.definition)) AS [__dbmv_utf16],
              CASE WHEN c.default_object_id<>0 THEN 1 ELSE 0 END,CONVERT(varbinary(max),CONVERT(nvarchar(max),OBJECT_DEFINITION(c.rule_object_id))) AS [__dbmv_utf16],
              CASE WHEN c.rule_object_id<>0 THEN 1 ELSE 0 END,cc.is_persisted,
              CONVERT(varchar(100),ic.seed_value),CONVERT(varchar(100),ic.increment_value),ic.is_not_for_replication,
              CONVERT(varbinary(max),CONVERT(nvarchar(max),xs.name)) AS [__dbmv_utf16],CONVERT(varbinary(max),CONVERT(nvarchar(max),xc.name)) AS [__dbmv_utf16],c.is_xml_document,c.is_rowguidcol,c.is_sparse,c.is_column_set,c.is_ansi_padded,
              c.is_hidden,c.generated_always_type,c.encryption_type
            FROM sys.table_types tt JOIN sys.columns c ON c.object_id=tt.type_table_object_id
            JOIN sys.types t ON t.user_type_id=c.user_type_id JOIN sys.schemas ts ON ts.schema_id=t.schema_id
            LEFT JOIN sys.computed_columns cc ON cc.object_id=c.object_id AND cc.column_id=c.column_id
            LEFT JOIN sys.identity_columns ic ON ic.object_id=c.object_id AND ic.column_id=c.column_id
            LEFT JOIN sys.xml_schema_collections xc ON xc.xml_collection_id=c.xml_collection_id
            LEFT JOIN sys.schemas xs ON xs.schema_id=xc.schema_id
            WHERE tt.user_type_id=@type ORDER BY c.column_id
            """);
        // SQL Server assigns table-type constraint names. Compare their stored
        // definitions/bindings and the system-named flag, not random generated names.
        var checks=await Read("""
            SELECT CONVERT(varbinary(max),CONVERT(nvarchar(max),CASE WHEN ck.is_system_named=0 THEN ck.name END)) AS [__dbmv_utf16],CONVERT(varbinary(max),CONVERT(nvarchar(max),c.name)) AS [__dbmv_utf16],CONVERT(varbinary(max),CONVERT(nvarchar(max),ck.definition)) AS [__dbmv_utf16],
              ck.is_disabled,ck.is_not_trusted,ck.is_not_for_replication,ck.uses_database_collation,ck.is_system_named
            FROM sys.table_types tt JOIN sys.check_constraints ck ON ck.parent_object_id=tt.type_table_object_id
            LEFT JOIN sys.columns c ON c.object_id=ck.parent_object_id AND c.column_id=ck.parent_column_id
            WHERE tt.user_type_id=@type
            """);
        var keys=await Read("""
            SELECT CONVERT(varbinary(max),CONVERT(nvarchar(max),CASE WHEN kc.is_system_named=0 THEN kc.name END)) AS [__dbmv_utf16],kc.type,kc.is_system_named,
              CONVERT(varbinary(max),CONVERT(nvarchar(max),c.name)) AS [__dbmv_utf16],ic.key_ordinal,ic.is_descending_key,kc.unique_index_id
            FROM sys.table_types tt JOIN sys.key_constraints kc ON kc.parent_object_id=tt.type_table_object_id
            JOIN sys.index_columns ic ON ic.object_id=kc.parent_object_id AND ic.index_id=kc.unique_index_id
            JOIN sys.columns c ON c.object_id=ic.object_id AND c.column_id=ic.column_id
            WHERE tt.user_type_id=@type
            """);
        var indexes=await Read("""
            SELECT CONVERT(varbinary(max),CONVERT(nvarchar(max),CASE WHEN kc.is_system_named=1 THEN NULL ELSE i.name END)) AS [__dbmv_utf16],i.type,i.is_unique,i.is_primary_key,
              i.is_unique_constraint,i.is_disabled,i.has_filter,CONVERT(varbinary(max),CONVERT(nvarchar(max),i.filter_definition)) AS [__dbmv_utf16],i.fill_factor,i.is_padded,
              i.ignore_dup_key,i.allow_row_locks,i.allow_page_locks,i.optimize_for_sequential_key,
              i.is_hypothetical,CONVERT(varbinary(max),CONVERT(nvarchar(max),c.name)) AS [__dbmv_utf16],ic.key_ordinal,ic.is_descending_key,ic.is_included_column,
              hi.bucket_count,kc.is_system_named,ic.index_column_id,i.index_id
            FROM sys.table_types tt JOIN sys.indexes i ON i.object_id=tt.type_table_object_id
            LEFT JOIN sys.key_constraints kc ON kc.parent_object_id=i.object_id AND kc.unique_index_id=i.index_id
            LEFT JOIN sys.index_columns ic ON ic.object_id=i.object_id AND ic.index_id=i.index_id
            LEFT JOIN sys.columns c ON c.object_id=ic.object_id AND c.column_id=ic.column_id
            LEFT JOIN sys.hash_indexes hi ON hi.object_id=i.object_id AND hi.index_id=i.index_id
            WHERE tt.user_type_id=@type
            """);
        return SqlServerCatalog.TableType(schema,name,options,columns,checks,keys,indexes,visible);
    }
}
