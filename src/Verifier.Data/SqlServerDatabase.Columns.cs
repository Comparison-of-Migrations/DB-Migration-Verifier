using System.Globalization;
using System.Text.Json;
using Verifier.Core;

namespace Verifier.Data;

public sealed partial class SqlServerDatabase
{
    // Match native names directly; the sub-securable parser can misread quotes
    // or brackets and cannot establish permission on every valid identifier.
    internal const string ColumnPermissions = """
        LEFT JOIN (SELECT CONVERT(varbinary(256),subentity_name) AS ColumnName,
                          MAX(CASE WHEN permission_name='SELECT' THEN 1 ELSE 0 END) AS CanSelect
                   FROM sys.fn_my_permissions(QUOTENAME(CONVERT(nvarchar(128),@schema))+'.'+QUOTENAME(CONVERT(nvarchar(128),@table)),'OBJECT')
                   GROUP BY CONVERT(varbinary(256),subentity_name)) cp
          ON cp.ColumnName=CONVERT(varbinary(256),c.name)
        """;
    // UNMASK is not enumerated per column by fn_my_permissions on the native
    // fixture. Only use the raw-name API when native parsing preserves the
    // exact single identifier; e.g. literal [id] otherwise aliases id. An
    // ambiguous name remains unknown rather than certifying masked output.
    internal const string ColumnUnmaskPermission = """
        CASE WHEN CONVERT(varbinary(256),PARSENAME(c.name,1))=CONVERT(varbinary(256),c.name)
          THEN HAS_PERMS_BY_NAME(QUOTENAME(s.name)+'.'+QUOTENAME(t.name),'OBJECT','UNMASK',c.name,'COLUMN') END
        """;
    private async Task<(Table Table,IReadOnlyList<string?[]> Signature,bool MemoryOptimized)> ReadTable(Table table,CancellationToken token)
    {
        var securityVisible=await RowSecurityVisible(token);
        await using var q=Command($"""
            SELECT t.object_id,t.create_date,t.modify_date,t.is_memory_optimized,t.is_filetable,t.is_external,
                   HAS_PERMS_BY_NAME(QUOTENAME(s.name)+'.'+QUOTENAME(t.name),'OBJECT','SELECT'),
                   CASE WHEN EXISTS(SELECT 1 FROM sys.security_predicates p
                     JOIN sys.security_policies pol ON pol.object_id=p.object_id
                     WHERE p.target_object_id=t.object_id AND p.predicate_type=0 AND pol.is_enabled=1) THEN 1 ELSE 0 END,
                   CONVERT(varbinary(max),CONVERT(nvarchar(max),c.name)) AS [__dbmv_utf16],CONVERT(varbinary(max),CONVERT(nvarchar(max),bt.name)) AS [__dbmv_utf16],c.column_id,c.is_nullable,c.max_length,c.precision,c.scale,CONVERT(varbinary(max),CONVERT(nvarchar(max),c.collation_name)) AS [__dbmv_utf16],
                   CONVERT(varbinary(max),CONVERT(nvarchar(max),OBJECT_DEFINITION(c.default_object_id))) AS [__dbmv_utf16],c.is_identity,c.is_computed,CONVERT(varbinary(max),CONVERT(nvarchar(max),cc.definition)) AS [__dbmv_utf16],cc.is_persisted,
                   c.encryption_type,c.is_hidden,c.is_sparse,c.is_column_set,c.generated_always_type,
                   CONVERT(varbinary(max),CONVERT(nvarchar(max),ut.name)) AS [__dbmv_utf16],CONVERT(varbinary(max),CONVERT(nvarchar(max),ts.name)) AS [__dbmv_utf16],ut.is_assembly_type,
                   mc.is_masked,CONVERT(varbinary(max),CONVERT(nvarchar(max),mc.masking_function)) AS [__dbmv_utf16],
                   {ColumnUnmaskPermission},
                   CONVERT(varchar(100),ic.seed_value),CONVERT(varchar(100),ic.increment_value),CONVERT(varchar(100),ic.last_value),c.is_filestream,
                   cp.CanSelect,
                   HAS_PERMS_BY_NAME(DB_NAME(),'DATABASE','VIEW DEFINITION'),c.default_object_id,
                   CONVERT(varbinary(max),CONVERT(nvarchar(max),xs.name)) AS [__dbmv_utf16],CONVERT(varbinary(max),CONVERT(nvarchar(max),xc.name)) AS [__dbmv_utf16],c.is_xml_document,ut.is_user_defined,CONVERT(varbinary(max),CONVERT(nvarchar(max),asm.name)) AS [__dbmv_utf16],asm.is_user_defined,CONVERT(varbinary(max),CONVERT(nvarchar(max),atp.assembly_class)) AS [__dbmv_utf16]
            FROM sys.tables t JOIN sys.schemas s ON s.schema_id=t.schema_id
            JOIN sys.columns c ON c.object_id=t.object_id
            JOIN sys.types ut ON ut.user_type_id=c.user_type_id
            JOIN sys.schemas ts ON ts.schema_id=ut.schema_id
            LEFT JOIN sys.types bt ON bt.user_type_id=c.system_type_id AND bt.user_type_id=bt.system_type_id
            LEFT JOIN sys.computed_columns cc ON cc.object_id=c.object_id AND cc.column_id=c.column_id
            LEFT JOIN sys.masked_columns mc ON mc.object_id=c.object_id AND mc.column_id=c.column_id
            LEFT JOIN sys.identity_columns ic ON ic.object_id=c.object_id AND ic.column_id=c.column_id
            LEFT JOIN sys.xml_schema_collections xc ON xc.xml_collection_id=c.xml_collection_id
            LEFT JOIN sys.schemas xs ON xs.schema_id=xc.schema_id
            LEFT JOIN sys.assembly_types atp ON atp.user_type_id=ut.user_type_id
            LEFT JOIN sys.assemblies asm ON asm.assembly_id=atp.assembly_id
            {ColumnPermissions}
            WHERE s.name=CONVERT(nvarchar(128),@schema) AND t.name=CONVERT(nvarchar(128),@table) AND t.is_ms_shipped=0 ORDER BY c.column_id
            """);
        SqlServerMetadata.AddNames(q,table);
        var columns=new List<Column>();var facts=CollectCatalogFacts(token);var gaps=CollectCatalogGaps(token);var signature=new SqlServerNativeRowBuffer(nativeRows,token);Reason reason=Reason.None;var computed=new Dictionary<string,IReadOnlySet<string>>(StringComparer.Ordinal);var memoryOptimized=false;
        using var defaults=new ProtectedRowStore(token:token);
        await using var r=await q.ExecuteReaderAsync(System.Data.CommandBehavior.SequentialAccess,token);
        while(await r.ReadAsync(token))
        {
            CatalogText? defaultText=null,computedText=null;IReadOnlySet<string>? computedReferences=null;
            var values=new string?[r.FieldCount];
            for(var i=0;i<values.Length;i++)
                if(i==16){defaultText=SqlServerMetadataText.ReadJson(r,i,defaults,token);values[i]=defaultText==null?null:"present";}
                else if(i==19){computedText=SqlServerMetadataText.ReadJson(r,i,defaults,token,raw=>computedReferences=SqlServerCheckExpression.ComputedReferences(raw,token));values[i]=computedText==null?null:"present";}
                else values[i]=SqlServerMetadata.ReadField(r,i,token);
            signature.Add(values);
            // SequentialAccess cannot revisit the reader. These fields were
            // already decoded once by ReadField using invariant native types.
            bool Bool(int i)=>bool.Parse(values[i]!);
            int Int(int i)=>int.Parse(values[i]!,CultureInfo.InvariantCulture);
            bool Null(int i)=>values[i]==null;
            if(!complete||!securityVisible||Null(37)||Int(37)!=1)reason=Reason.VisibilityUnknown;
            else if(Int(7)==1)reason=Reason.RowFiltered;
            else if(Bool(4)||Bool(5))reason=Reason.UnsafeObservation;
            memoryOptimized=Bool(3);
            var name=values[8]!;
            var systemClr=SqlServerSystemTypes.Trusted(values[26]!,values[27]!,Bool(42),Bool(28),
                values[43],Null(44)?null:Bool(44),values[45]);
            var type=Null(9)?systemClr?values[26]!:"unsupported":values[9]!;
            // Unknown CLR/computed bodies and encrypted/masked substitutes must
            // never become native-value evidence. Pure computed expressions
            // are admitted below only after all stored columns are classified.
            var compatible=SqlServerRowCodec.Supported(type)&&Null(21)&&(!Bool(28)||systemClr)
                &&(Null(29)||!Bool(29)||!Null(31)&&Int(31)==1)
                &&!Null(36)&&Int(36)==1;
            // Trial binding and raw frames retain presence. Exact catalog facts
            // guard the bodies; computed safety retains stored-column names only.
            var metadata=values[12..];metadata[4]=metadata[4]==null?null:"present";metadata[7]=metadata[7]==null?null:"present";
            var definition=SqlServerMetadata.Serialize(metadata);
            columns.Add(new(name,type,Int(10),Bool(11),definition,compatible&&!Bool(18)));
            if(compatible&&Bool(18)&&computedReferences!=null)computed[name]=computedReferences;
            void Fact(string attribute,object? value)=>facts.Add(new(name,name,attribute,SqlServerMetadata.Serialize(value)));
            Fact("ColumnName",name);Fact("ColumnType",new[]{values[9],values[12],values[13],values[14],values[26],values[27],values[39],values[40],values[41]});
            Fact("ColumnPosition",values[10]);Fact("ColumnNullable",Bool(11));
            var defaultFact=new CatalogFact(name,name,"ColumnDefault","null");
            facts.Add(defaultText==null?defaultFact:defaultFact.WithText(defaultText));
            Fact("ColumnIdentity",new[]{values[17],values[32],values[33]});
            var generated=new CatalogFact(name,name,"ColumnGenerated",null);
            facts.Add(computedText==null?generated with{Value=SqlServerMetadata.Serialize(new[]{values[18],(string?)null,values[20],values[25]})}:
                generated.WithText(SqlServerMetadataText.JsonArray(computedText,[values[18]],[values[20],values[25]],defaults,token)));
            Fact("ColumnCollation",values[15]);Fact("ColumnOptions",new[]{values[21],values[22],values[23],values[24],values[29],values[30],values[35]});
            if(Int(38)!=0&&Null(16))gaps.Add(new(name,name,"ColumnDefault","",Reason.VisibilityUnknown));
            if(Bool(18)&&Null(19))gaps.Add(new(name,name,"ColumnGenerated","",Reason.VisibilityUnknown));
        }
        // Bind against the original stored-column classification. A UDF or
        // unreadable base column cannot become safe by referencing another
        // calculated value. Use the native column directly, never rewrite its body.
        var safeComputed=computed.Where(c=>c.Value.All(name=>columns.Any(column=>column.Name==name&&column.SafeValue))).Select(c=>c.Key).ToHashSet(StringComparer.Ordinal);
        for(var i=0;i<columns.Count;i++)if(safeComputed.Contains(columns[i].Name))columns[i]=columns[i] with{SafeValue=true};
        if(columns.Count==0)throw new VerificationException(Reason.ObservationChanged);
        var columnFacts=facts.Snapshot();var columnGaps=gaps.Snapshot();var raw=signature.Finish();
        var allocators=CollectCatalogFacts(token);
        foreach(var values in raw.Where(v=>v[17]=="True"))
        {
            var name=values[8]!;
            allocators.Add(new(name,name,"AutoIncrementDefinition",SqlServerMetadata.Serialize(new[]{values[9],values[13],values[14],values[32],values[33]})));
            allocators.Add(new(name,name,"AutoIncrementState",SqlServerMetadata.Serialize(new{last=values[34],seed=values[32],increment=values[33]})));
        }
        return(table with{Columns=columns,SafeRead=reason==Reason.None,SafeLocalRead=reason==Reason.None,ReadReason=reason,
            Catalog=[new("SCHEMA-01",columnFacts,complete&&gaps.Count==0?Reason.None:Reason.VisibilityUnknown){Gaps=columnGaps,CompleteExceptGaps=complete&&gaps.Count>0},
                new("OBJECT-02",allocators.Snapshot(),complete?Reason.None:Reason.VisibilityUnknown)]},raw,memoryOptimized);
    }
}
