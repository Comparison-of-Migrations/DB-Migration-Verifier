using System.Text.Json;
using Verifier.Core;
namespace Verifier.Data;
public sealed partial class SqlServerDatabase
{
    internal const string FulltextQuery="""
        SELECT N'fulltext',CONVERT(varbinary(max),CONVERT(nvarchar(max),i.name)) AS [__dbmv_utf16],CONVERT(varbinary(max),CONVERT(nvarchar(max),fc.name)) AS [__dbmv_utf16],fc.is_accent_sensitivity_on,CONVERT(varbinary(max),CONVERT(nvarchar(max),cp.name)) AS [__dbmv_utf16],
          f.is_enabled,f.change_tracking_state,
          CASE WHEN f.stoplist_id IS NULL THEN N'none' WHEN f.stoplist_id=0 THEN N'system' ELSE N'custom' END,
          CONVERT(varbinary(max),CONVERT(nvarchar(max),sl.name)) AS [__dbmv_utf16],CONVERT(varbinary(max),CONVERT(nvarchar(max),sp.name)) AS [__dbmv_utf16],CONVERT(varbinary(max),CONVERT(nvarchar(max),pl.name)) AS [__dbmv_utf16],CONVERT(varbinary(max),CONVERT(nvarchar(max),pp.name)) AS [__dbmv_utf16],CONVERT(varbinary(max),CONVERT(nvarchar(max),ds.name)) AS [__dbmv_utf16],
          CASE WHEN f.property_list_id IS NULL THEN 0 ELSE 1 END
        FROM sys.fulltext_indexes f LEFT JOIN sys.indexes i ON i.object_id=f.object_id AND i.index_id=f.unique_index_id
        LEFT JOIN sys.fulltext_catalogs fc ON fc.fulltext_catalog_id=f.fulltext_catalog_id
        LEFT JOIN sys.database_principals cp ON cp.principal_id=fc.principal_id
        LEFT JOIN sys.fulltext_stoplists sl ON sl.stoplist_id=f.stoplist_id
        LEFT JOIN sys.database_principals sp ON sp.principal_id=sl.principal_id
        LEFT JOIN sys.registered_search_property_lists pl ON pl.property_list_id=f.property_list_id
        LEFT JOIN sys.database_principals pp ON pp.principal_id=pl.principal_id
        LEFT JOIN sys.data_spaces ds ON ds.data_space_id=f.data_space_id
        WHERE f.object_id=object_id(QUOTENAME(CONVERT(nvarchar(128),@schema))+'.'+QUOTENAME(CONVERT(nvarchar(128),@table)),'U')
        """;
    private async Task<CatalogSection> ReadFulltext(Table table,CancellationToken token)
    {
        var header=await Native(FulltextQuery,table,token);
        var section=SqlServerCatalog.IndexDetails("fulltext",header,14,new HashSet<string>{"fulltext"},complete,
            r=>r[1..7].All(v=>v!=null)&&(r[7]!="custom"||r[8]!=null&&r[9]!=null)&&(r[13]=="0"||r[10]!=null&&r[11]!=null));
        var facts=section.Facts.ToList();var gaps=section.Gaps.ToList();
        var columns=await Native("""
            SELECT N'fulltext',CONVERT(varbinary(max),CONVERT(nvarchar(max),c.name)) AS [__dbmv_utf16],CONVERT(varbinary(max),CONVERT(nvarchar(max),tc.name)) AS [__dbmv_utf16],f.language_id,f.statistical_semantics,
              CASE WHEN f.type_column_id IS NULL OR f.type_column_id=0 THEN 0 ELSE 1 END
            FROM sys.fulltext_index_columns f LEFT JOIN sys.columns c ON c.object_id=f.object_id AND c.column_id=f.column_id
            LEFT JOIN sys.columns tc ON tc.object_id=f.object_id AND tc.column_id=f.type_column_id
            WHERE f.object_id=object_id(QUOTENAME(CONVERT(nvarchar(128),@schema))+'.'+QUOTENAME(CONVERT(nvarchar(128),@table)),'U')
            """,table,token);
        var bound=SqlServerCatalog.IndexDetails("fulltext-columns",columns,6,new HashSet<string>{"fulltext"},complete,
            r=>r[1]!=null&&r[3]!=null&&r[4]!=null&&(r[5]=="0"||r[2]!=null));
        facts.AddRange(bound.Facts);gaps.AddRange(bound.Gaps);
        var words=await Native("""
            SELECT sw.language_id,CONVERT(varbinary(max),CONVERT(nvarchar(max),sw.stopword COLLATE Latin1_General_100_BIN2)) AS [__dbmv_utf16] FROM sys.fulltext_stopwords sw
            JOIN sys.fulltext_indexes f ON f.stoplist_id=sw.stoplist_id
            WHERE f.object_id=object_id(QUOTENAME(CONVERT(nvarchar(128),@schema))+'.'+QUOTENAME(CONVERT(nvarchar(128),@table)),'U')
            UNION ALL
            SELECT sw.language_id,CONVERT(varbinary(max),CONVERT(nvarchar(max),sw.stopword COLLATE Latin1_General_100_BIN2)) AS [__dbmv_utf16] FROM sys.fulltext_system_stopwords sw
            WHERE EXISTS(SELECT 1 FROM sys.fulltext_indexes f WHERE f.stoplist_id=0 AND f.object_id=object_id(QUOTENAME(CONVERT(nvarchar(128),@schema))+'.'+QUOTENAME(CONVERT(nvarchar(128),@table)),'U'))
            """,table,token);
        var properties=await Native("""
            SELECT CONVERT(varbinary(max),CONVERT(nvarchar(max),p.property_name)) AS [__dbmv_utf16],CONVERT(varchar(36),p.property_set_guid),p.property_int_id,CONVERT(varbinary(max),CONVERT(nvarchar(max),p.property_description)) AS [__dbmv_utf16]
            FROM sys.registered_search_properties p JOIN sys.fulltext_indexes f ON f.property_list_id=p.property_list_id
            WHERE f.object_id=object_id(QUOTENAME(CONVERT(nvarchar(128),@schema))+'.'+QUOTENAME(CONVERT(nvarchar(128),@table)),'U')
            """,table,token);
        foreach(var (family,rows) in new[]{("fulltext-stopwords",words),("fulltext-properties",properties)})
            facts.Add(new(SqlServerCatalog.Key("index-detail",family,"fulltext"),null,"IndexOptions",SqlServerMetadata.Serialize(rows.Select(r=>SqlServerMetadata.Serialize(r)).Order(StringComparer.Ordinal))));
        return SqlServerCatalog.Section("SCHEMA-03",facts,complete,gaps);
    }
}
