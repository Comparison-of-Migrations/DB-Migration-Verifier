using Verifier.Core;
namespace Verifier.Data;

public sealed partial class SqlServerDatabase
{
    private async Task<CatalogSection> ReadSpecialIndexes(Table table,IReadOnlyList<string?[]> indexes,CancellationToken token)
    {
        var facts=new List<CatalogFact>();var gaps=new List<CatalogGap>();
        if(indexes.Any(r=>r[5]=="3"))
        {
            var xml=await Native("""
                SELECT CONVERT(varbinary(max),CONVERT(nvarchar(max),i.name)) AS [__dbmv_utf16],i.xml_index_type,i.secondary_type,CONVERT(varbinary(max),CONVERT(nvarchar(max),parent.name)) AS [__dbmv_utf16],CONVERT(varbinary(max),CONVERT(nvarchar(max),path.name)) AS [__dbmv_utf16],
                  CASE WHEN i.using_xml_index_id IS NULL OR i.using_xml_index_id=0 THEN 0 ELSE 1 END
                FROM sys.xml_indexes i LEFT JOIN sys.xml_indexes parent ON parent.object_id=i.object_id AND parent.index_id=i.using_xml_index_id
                LEFT JOIN sys.selective_xml_index_paths path ON path.object_id=i.object_id AND path.index_id=i.using_xml_index_id AND path.path_id=i.path_id
                WHERE i.object_id=object_id(QUOTENAME(CONVERT(nvarchar(128),@schema))+'.'+QUOTENAME(CONVERT(nvarchar(128),@table)),'U')
                ORDER BY i.name COLLATE Latin1_General_100_BIN2
                """,table,token);
            Add("xml",xml,6,"3",r=>r[1] is "0" or "1" or "2" or "3"&&
                (r[5]=="0"||r[3]!=null)&&(r[1]!="3"||r[4]!=null));
            var selective=xml.Where(r=>r[1]=="2").Select(r=>r[0]!).ToHashSet(StringComparer.Ordinal);
            if(selective.Count>0)
            {
                var paths=await Native("""
                    SELECT CONVERT(varbinary(max),CONVERT(nvarchar(max),i.name)) AS [__dbmv_utf16],CONVERT(varbinary(max),CONVERT(nvarchar(max),p.name)) AS [__dbmv_utf16],CONVERT(varbinary(max),CONVERT(nvarchar(max),p.path)) AS [__dbmv_utf16],p.path_type,CONVERT(varbinary(max),CONVERT(nvarchar(max),p.xquery_type_description)) AS [__dbmv_utf16],p.is_xquery_type_inferred,
                      p.xquery_max_length,p.is_xquery_max_length_inferred,p.is_node,CONVERT(varbinary(max),CONVERT(nvarchar(max),ts.name)) AS [__dbmv_utf16],CONVERT(varbinary(max),CONVERT(nvarchar(max),t.name)) AS [__dbmv_utf16],
                      p.max_length,p.precision,p.scale,CONVERT(varbinary(max),CONVERT(nvarchar(max),p.collation_name)) AS [__dbmv_utf16],p.is_singleton,
                      CASE WHEN p.xml_component_id IS NULL OR p.xml_component_id=0 THEN 0
                        WHEN xc.xml_collection_id=1 AND xn.name=N'http://www.w3.org/2001/XMLSchema' AND xc.name IS NOT NULL THEN 0 ELSE 1 END
                    FROM sys.selective_xml_index_paths p JOIN sys.xml_indexes i ON i.object_id=p.object_id AND i.index_id=p.index_id
                    LEFT JOIN sys.types t ON t.user_type_id=p.user_type_id LEFT JOIN sys.schemas ts ON ts.schema_id=t.schema_id
                    LEFT JOIN sys.xml_schema_components xc ON xc.xml_component_id=p.xml_component_id
                    LEFT JOIN sys.xml_schema_namespaces xn ON xn.xml_collection_id=xc.xml_collection_id AND xn.xml_namespace_id=xc.xml_namespace_id
                    WHERE p.object_id=object_id(QUOTENAME(CONVERT(nvarchar(128),@schema))+'.'+QUOTENAME(CONVERT(nvarchar(128),@table)),'U')
                    ORDER BY i.name COLLATE Latin1_General_100_BIN2,p.name COLLATE Latin1_General_100_BIN2
                    """,table,token);
                var pathSection=SqlServerCatalog.IndexDetails("xml-paths",paths,17,selective,complete,r=>r[1]!=null&&r[2]!=null&&r[3] is "0" or "1");
                facts.AddRange(pathSection.Facts);gaps.AddRange(pathSection.Gaps);
                foreach(var row in paths.Where(r=>r[16]=="1"))gaps.Add(new(SqlServerCatalog.Key("index-detail","xml-paths",row[0]),null,"IndexOptions","",Reason.NotImplemented));
                // SQL Server 16 native catalog shape observed by the owned fixture.
                // Namespace binding is essential even when path text is identical.
                var namespaces=await Native("""
                    SELECT CONVERT(varbinary(max),CONVERT(nvarchar(max),i.name)) AS [__dbmv_utf16],CONVERT(varbinary(max),CONVERT(nvarchar(max),n.prefix)) AS [__dbmv_utf16],CONVERT(varbinary(max),CONVERT(nvarchar(max),n.uri)) AS [__dbmv_utf16],n.is_default_uri FROM sys.selective_xml_index_namespaces n
                    JOIN sys.xml_indexes i ON i.object_id=n.object_id AND i.index_id=n.index_id
                    WHERE n.object_id=object_id(QUOTENAME(CONVERT(nvarchar(128),@schema))+'.'+QUOTENAME(CONVERT(nvarchar(128),@table)),'U')
                    ORDER BY i.name COLLATE Latin1_General_100_BIN2,n.prefix COLLATE Latin1_General_100_BIN2,n.uri COLLATE Latin1_General_100_BIN2
                    """,table,token);
                // Empty namespaces are valid and must compare as an empty set.
                foreach(var index in selective)
                {
                    var rows=namespaces.Where(r=>r[0]==index).ToArray();
                    facts.Add(new(SqlServerCatalog.Key("index-detail","xml-namespaces",index),null,"IndexOptions",SqlServerMetadata.Serialize(rows.Select(r=>r[1..]))));
                }
            }
        }
        if(indexes.Any(r=>r[5]=="4"))
        {
            var spatial=await Native("""
                SELECT CONVERT(varbinary(max),CONVERT(nvarchar(max),i.name)) AS [__dbmv_utf16],i.spatial_index_type,CONVERT(varbinary(max),CONVERT(nvarchar(max),i.tessellation_scheme)) AS [__dbmv_utf16],
                  CONVERT(varchar(16),CONVERT(varbinary(8),t.bounding_box_xmin),2),
                  CONVERT(varchar(16),CONVERT(varbinary(8),t.bounding_box_ymin),2),
                  CONVERT(varchar(16),CONVERT(varbinary(8),t.bounding_box_xmax),2),
                  CONVERT(varchar(16),CONVERT(varbinary(8),t.bounding_box_ymax),2),
                  t.level_1_grid,t.level_2_grid,t.level_3_grid,t.level_4_grid,t.cells_per_object
                FROM sys.spatial_indexes i LEFT JOIN sys.spatial_index_tessellations t ON t.object_id=i.object_id AND t.index_id=i.index_id
                WHERE i.object_id=object_id(QUOTENAME(CONVERT(nvarchar(128),@schema))+'.'+QUOTENAME(CONVERT(nvarchar(128),@table)),'U')
                ORDER BY i.name COLLATE Latin1_General_100_BIN2
                """,table,token);
            Add("spatial",spatial,12,"4",r=>r[1] is "1" or "2"&&r[2]!=null&&r[11]!=null&&
                (r[1]!="1"||r[3..7].All(v=>v!=null))&&(!r[2]!.EndsWith("_GRID",StringComparison.Ordinal)||r[2]!.Contains("AUTO",StringComparison.Ordinal)||r[7..11].All(v=>v!=null)));
        }
        return SqlServerCatalog.Section("SCHEMA-03",facts,complete,gaps);
        void Add(string family,IReadOnlyList<string?[]> rows,int fields,string type,Func<string?[],bool> known)
        {
            var expected=indexes.Where(r=>r[5]==type).Select(r=>r[0]!).ToHashSet(StringComparer.Ordinal);
            var result=SqlServerCatalog.IndexDetails(family,rows,fields,expected,complete,known);facts.AddRange(result.Facts);gaps.AddRange(result.Gaps);
        }
    }
}
