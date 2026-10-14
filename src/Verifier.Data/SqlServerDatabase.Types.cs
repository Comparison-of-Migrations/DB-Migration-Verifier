using System.Text.Json;
using Microsoft.Data.SqlClient;
using Verifier.Core;
namespace Verifier.Data;

public sealed partial class SqlServerDatabase
{
    private async Task<CatalogSection> ReadTypes(Table? table,CancellationToken token,IReadOnlySet<string>? selectedObjects=null)
    {
        var scope=table??new Table("","",[],false);
        try
        {
            var coverage=await Native("SELECT HAS_PERMS_BY_NAME(DB_NAME(),'DATABASE','VIEW DEFINITION')",scope,token);
            var visible=complete&&coverage.Count==1&&coverage[0][0]=="1";
            HashSet<string>? typeIds=null,xmlIds=null;
            if(table!=null)
            {
                typeIds=[];xmlIds=[];
                var batches=selectedObjects==null?new[]{Array.Empty<string>()}:selectedObjects.Chunk(256);
                foreach(var batch in batches)
                {
                    // All IDs came from native metadata, not user text. Bound
                    // batch size limits compilation cost without a scope cap.
                    var filter=batch.Length==0?"="+ObjectFilter:" IN("+string.Join(',',batch.Select(id=>int.Parse(id,System.Globalization.CultureInfo.InvariantCulture).ToString(System.Globalization.CultureInfo.InvariantCulture)))+")";
                    var usage=await Native("SELECT user_type_id,xml_collection_id FROM sys.columns WHERE object_id"+filter+
                        " UNION SELECT user_type_id,xml_collection_id FROM sys.parameters WHERE object_id"+filter+
                        " UNION SELECT CASE WHEN referenced_class=6 THEN referenced_id ELSE 0 END,CASE WHEN referenced_class=10 THEN referenced_id ELSE 0 END FROM sys.sql_expression_dependencies WHERE referencing_id"+filter+" AND referenced_class IN(6,10)",scope,token);
                    foreach(var r in usage)
                    {if(r[0]!=null&&r[0]!="0")typeIds.Add(r[0]!);if(r[1]!=null&&r[1]!="0")xmlIds.Add(r[1]!);}
                }
            }
            var rows=await NativeTypeScope("""
                SELECT CONVERT(varbinary(max),CONVERT(nvarchar(max),s.name)) AS [__dbmv_utf16],CONVERT(varbinary(max),CONVERT(nvarchar(max),t.name)) AS [__dbmv_utf16],CONVERT(varbinary(max),CONVERT(nvarchar(max),bt.name)) AS [__dbmv_utf16],t.max_length,t.precision,t.scale,CONVERT(varbinary(max),CONVERT(nvarchar(max),t.collation_name)) AS [__dbmv_utf16],t.is_nullable,
                  t.is_user_defined,t.is_assembly_type,t.is_table_type,CONVERT(varbinary(max),CONVERT(nvarchar(max),OBJECT_DEFINITION(t.default_object_id))) AS [__dbmv_utf16],
                  CASE WHEN t.default_object_id<>0 THEN 1 ELSE 0 END,CONVERT(varbinary(max),CONVERT(nvarchar(max),OBJECT_DEFINITION(t.rule_object_id))) AS [__dbmv_utf16],
                  CASE WHEN t.rule_object_id<>0 THEN 1 ELSE 0 END,CONVERT(varbinary(max),CONVERT(nvarchar(max),p.name)) AS [__dbmv_utf16],
                  CONVERT(varbinary(max),CONVERT(nvarchar(max),at.assembly_class)) AS [__dbmv_utf16],CONVERT(varbinary(max),CONVERT(nvarchar(max),a.name)) AS [__dbmv_utf16],CONVERT(varbinary(max),CONVERT(nvarchar(max),a.clr_name)) AS [__dbmv_utf16],CONVERT(varbinary(max),CONVERT(nvarchar(max),a.permission_set_desc)) AS [__dbmv_utf16],at.is_binary_ordered,at.is_fixed_length,
                  t.user_type_id
                FROM sys.types t JOIN sys.schemas s ON s.schema_id=t.schema_id
                LEFT JOIN sys.types bt ON bt.user_type_id=t.system_type_id AND bt.user_type_id=bt.system_type_id
                LEFT JOIN sys.database_principals p ON p.principal_id=COALESCE(t.principal_id,s.principal_id)
                LEFT JOIN sys.assembly_types at ON at.user_type_id=t.user_type_id
                LEFT JOIN sys.assemblies a ON a.assembly_id=at.assembly_id
                WHERE (t.is_user_defined=1 OR t.is_assembly_type=1)
                /*SCOPE*/
                ORDER BY s.name COLLATE Latin1_General_100_BIN2,t.name COLLATE Latin1_General_100_BIN2
                ""","t.user_type_id",typeIds,scope,token);
            var facts=CollectCatalogFacts(token);var gaps=CollectCatalogGaps(token);
            if(table!=null&&selectedObjects==null)gaps.Add(new("type-dependency-coverage",null,"UserTypeDefinitions","",Reason.MissingDependency));
            foreach(var row in rows)
            {
                var key=SqlServerCatalog.Key("type",row[0],row[1]);
                facts.Add(new(key,null,"UserTypeDefinitions",SqlServerMetadata.Serialize(new{shape=row[..11],owner=row[15],assembly=row[16..22]})));
                foreach(var (body,present,label) in new[]{(11,12,"default"),(13,14,"rule")})
                {
                    var binding=SqlServerCatalog.Key("type-binding",row[0],row[1],label);
                    if(row[present]=="1"&&row[body]==null)gaps.Add(new(binding,null,"UserTypeDefinitions","",Reason.VisibilityUnknown));
                    else facts.Add(new(binding,null,"UserTypeDefinitions",row[body]));
                }
                if(row[15]==null)gaps.Add(new(key,null,"UserTypeDefinitions","",Reason.VisibilityUnknown));
                if(row[8]=="True"&&row[9]=="True")
                    // User CLR contracts remain unverified. The engine's own
                    // geometry/geography/hierarchyid definitions are still
                    // observed above, rather than mislabelled as user code.
                    gaps.Add(new(SqlServerCatalog.Key("type-executable",row[0],row[1]),null,"UserTypeDefinitions","",Reason.UnsafeObservation));
                if(row[10]=="True")
                {
                    var tableType=await ReadTableType(row[0]!,row[1]!,row[22]!,scope,visible,token);
                    facts.AddRange(tableType.Facts);gaps.AddRange(tableType.Gaps);
                }
            }
            var xml=await NativeTypeScope("""
                SELECT CONVERT(varbinary(max),CONVERT(nvarchar(max),s.name)) AS [__dbmv_utf16],CONVERT(varbinary(max),CONVERT(nvarchar(max),x.name)) AS [__dbmv_utf16],CONVERT(varbinary(max),CONVERT(nvarchar(max),p.name)) AS [__dbmv_utf16],CONVERT(varbinary(max),CONVERT(nvarchar(max),CONVERT(nvarchar(max),XML_SCHEMA_NAMESPACE(s.name,x.name)))) AS [__dbmv_utf16]
                FROM sys.xml_schema_collections x JOIN sys.schemas s ON s.schema_id=x.schema_id
                LEFT JOIN sys.database_principals p ON p.principal_id=COALESCE(x.principal_id,s.principal_id)
                WHERE x.xml_collection_id>1
                /*SCOPE*/
                ORDER BY s.name COLLATE Latin1_General_100_BIN2,x.name COLLATE Latin1_General_100_BIN2
                ""","x.xml_collection_id",xmlIds,scope,token);
            SqlServerCatalog.XmlSchemas(xml,visible,facts,gaps);
            var section=SqlServerCatalog.Section("OBJECT-03",facts.Snapshot(),visible,gaps.Snapshot());
            try{return await WithAssemblyDefinitions(section,scope,typeIds,selectedObjects,table==null,token,facts,gaps);}
            catch(SqlException error)
            {
                // A newly inspected assembly area cannot erase already read
                // alias/table-type/XML definitions. Incomplete global coverage
                // still prevents certifying absence on either side.
                var reason=FaultReason(error,token);
                gaps.Add(new("assembly-catalog",null,"UserTypeDefinitions","",reason));
                return section with{Reason=reason,CompleteExceptGaps=false,Gaps=gaps.Snapshot()};
            }
        }
        catch(SqlException e){return new("OBJECT-03",[],FaultReason(e,token));}
    }
    private async Task<IReadOnlyList<string?[]>> NativeTypeScope(string query,string column,HashSet<string>? ids,Table scope,CancellationToken token)
    {
        if(ids==null)return await Native(query.Replace("/*SCOPE*/","",StringComparison.Ordinal),scope,token);
        var rows=new SqlServerNativeRowBuffer(nativeRows,token);
        foreach(var batch in ids.Chunk(256))
        {
            var values=string.Join(',',batch.Select(id=>int.Parse(id,System.Globalization.CultureInfo.InvariantCulture).ToString(System.Globalization.CultureInfo.InvariantCulture)));
            foreach(var row in await Native(query.Replace("/*SCOPE*/"," AND "+column+" IN("+values+")",StringComparison.Ordinal),scope,token))rows.Add(row);
        }
        return rows.Finish();
    }
}
