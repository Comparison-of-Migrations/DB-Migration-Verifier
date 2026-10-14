using System.Globalization;
using System.Text.Json;
using Verifier.Core;

namespace Verifier.Data;

public sealed partial class SqlServerDatabase
{
    private async Task<IReadOnlyList<string?[]>> Native(string sql,Table table,CancellationToken token)
    {
        return await QueryNative(sql,table,nativeRows,token);
    }
    // Only fixed, parameter-independent global catalog queries opt into this
    // initial-inspection cache. Every later guard gets a fresh native query.
    private async Task<IReadOnlyList<string?[]>> NativeShared(string sql,Table table,CancellationToken token)
    {
        token.ThrowIfCancellationRequested();
        if(inventory!=null||sql.Contains("@schema",StringComparison.OrdinalIgnoreCase)||sql.Contains("@table",StringComparison.OrdinalIgnoreCase))return await Native(sql,table,token);
        if(inspectionQueries.TryGetValue(sql,out var cached))return cached;
        var rows=await QueryNative(sql,table,inspectionRows,token);inspectionQueries.Add(sql,rows);return rows;
    }
    private async Task<IReadOnlyList<string?[]>> QueryNative(string sql,Table table,RunReportStorage owner,CancellationToken token)
    {
        await using var q=Command(sql);SqlServerMetadata.AddNames(q,table);
        var rows=new SqlServerNativeRowBuffer(owner,token);await using var r=await q.ExecuteReaderAsync(System.Data.CommandBehavior.SequentialAccess,token);
        while(await r.ReadAsync(token))
        {
            var values=new string?[r.FieldCount];for(var i=0;i<values.Length;i++)values[i]=SqlServerMetadata.ReadField(r,i,token);rows.Add(values);
        }
        return rows.Finish();
    }
    private const string ObjectFilter="object_id(QUOTENAME(CONVERT(nvarchar(128),@schema))+'.'+QUOTENAME(CONVERT(nvarchar(128),@table)),'U')";
    private const string KeyQuery="""
        SELECT CONVERT(varbinary(max),CONVERT(nvarchar(max),i.name)) AS [__dbmv_utf16],i.is_unique,i.is_primary_key,i.is_disabled,i.has_filter,i.type,
               CONVERT(varbinary(max),CONVERT(nvarchar(max),c.name)) AS [__dbmv_utf16],ic.key_ordinal,ic.is_descending_key,ic.is_included_column,CONVERT(varbinary(max),CONVERT(nvarchar(max),i.filter_definition)) AS [__dbmv_utf16],
               i.fill_factor,i.is_padded,i.ignore_dup_key,i.allow_row_locks,i.allow_page_locks,
               i.optimize_for_sequential_key,i.is_hypothetical,CONVERT(varbinary(max),CONVERT(nvarchar(max),ds.name)) AS [__dbmv_utf16],ds.type,
               ic.column_store_order_ordinal,i.compression_delay,i.suppress_dup_key_messages,i.is_unique_constraint,hi.bucket_count
        FROM sys.indexes i LEFT JOIN sys.index_columns ic ON ic.object_id=i.object_id AND ic.index_id=i.index_id
        LEFT JOIN sys.columns c ON c.object_id=ic.object_id AND c.column_id=ic.column_id
        LEFT JOIN sys.data_spaces ds ON ds.data_space_id=i.data_space_id
        LEFT JOIN sys.hash_indexes hi ON hi.object_id=i.object_id AND hi.index_id=i.index_id
        WHERE i.object_id=object_id(QUOTENAME(CONVERT(nvarchar(128),@schema))+'.'+QUOTENAME(CONVERT(nvarchar(128),@table)),'U')
        ORDER BY i.index_id,ic.index_column_id
        """;
    private async Task<Table> AddStructure(Table table,CancellationToken token)
    {
        var constraints=new List<CatalogFact>();var indexes=new List<CatalogFact>();var gaps=new List<CatalogGap>();var boundsComplete=true;
        var keys=new List<IReadOnlyList<string>>();
        var indexRows=await Native(KeyQuery,table,token);
        foreach(var group in indexRows.GroupBy(x=>x[0],StringComparer.Ordinal))
        {
            var rows=group.ToArray();var first=rows[0];var key=SqlServerMetadata.Serialize(new[]{"index",group.Key});
            indexes.Add(new(key,null,"IndexDefinition",SqlServerMetadata.Serialize(rows.Select(x=>new[]{x[5],x[6],x[7],x[8],x[9],x[10],x[20]}))));
            indexes.Add(new(key,null,"IndexUnique",first[1]));indexes.Add(new(key,null,"IndexPrimary",first[2]));
            indexes.Add(new(key,null,"IndexValid",first[3]=="False"?"true":"false"));indexes.Add(new(key,null,"IndexClustered",first[5]));
            indexes.Add(new(key,null,"IndexOptions",SqlServerMetadata.Serialize(first[11..18].Concat(first[21..25]))));
            indexes.Add(new(key,null,"IndexTablespace",SqlServerMetadata.Serialize(first[18..20])));
            var parts=rows.Where(x=>x[9]=="False"&&int.TryParse(x[7],out var n)&&n>0).OrderBy(x=>int.Parse(x[7]!,CultureInfo.InvariantCulture)).ToArray();
            if(first[1]=="True"&&first[3]=="False"&&first[4]=="False"&&first[17]=="False"&&parts.Length>0&&parts.All(x=>x[6]!=null))keys.Add(parts.Select(x=>x[6]!).ToArray());
            if(first[5] is not("0" or "1" or "2" or "3" or "4" or "5" or "6" or "7"))gaps.Add(new(key,null,"IndexOptions","",Reason.NotImplemented));
            if(first[5]=="7"&&first[24]==null)gaps.Add(new(key,null,"IndexOptions","",Reason.VisibilityUnknown));
        }
        var special=await ReadSpecialIndexes(table,indexRows,token);indexes.AddRange(special.Facts);gaps.AddRange(special.Gaps);
        var keyConstraints=await Native("SELECT CONVERT(varbinary(max),CONVERT(nvarchar(max),kc.name)) AS [__dbmv_utf16],kc.type,CONVERT(varbinary(max),CONVERT(nvarchar(max),i.name)) AS [__dbmv_utf16] FROM sys.key_constraints kc JOIN sys.indexes i ON i.object_id=kc.parent_object_id AND i.index_id=kc.unique_index_id WHERE kc.parent_object_id="+ObjectFilter+" ORDER BY kc.name",table,token);
        foreach(var row in keyConstraints)
        {
            var key=SqlServerMetadata.Serialize(new[]{"constraint",row[0]});constraints.Add(new(key,null,"ConstraintType",row[1]));
            constraints.Add(new(key,null,"ConstraintDefinition",SqlServerMetadata.Serialize(indexRows.Where(x=>x[0]==row[2]).Select(x=>new[]{x[6],x[7],x[8],x[9]}))));
        }
        var checks=await Native("SELECT CONVERT(varbinary(max),CONVERT(nvarchar(max),name)) AS [__dbmv_utf16],CONVERT(varbinary(max),CONVERT(nvarchar(max),definition)) AS [__dbmv_utf16],is_disabled,is_not_trusted,is_not_for_replication,uses_database_collation FROM sys.check_constraints WHERE parent_object_id="+ObjectFilter+" ORDER BY name",table,token);
        foreach(var row in checks)
        {
            var key=SqlServerMetadata.Serialize(new[]{"constraint",row[0]});constraints.Add(new(key,null,"ConstraintType","CHECK"));
            constraints.Add(new(key,null,"ConstraintDefinition",SqlServerMetadata.Serialize(new[]{row[1],row[4],row[5]})));
            constraints.Add(new(key,null,"ConstraintEnforced",row[2]=="False"?"true":"false"));constraints.Add(new(key,null,"ConstraintValidated",row[3]=="False"?"true":"false"));
        }
        var references=await Native(ForeignQuery,table,token);
        foreach(var group in references.GroupBy(x=>x[0]!,StringComparer.Ordinal))
        {
            var rows=group.ToArray();var key=SqlServerMetadata.Serialize(new[]{"constraint",group.Key});constraints.Add(new(key,null,"ConstraintType","FOREIGN KEY"));
            constraints.Add(new(key,null,"ConstraintDefinition",SqlServerMetadata.Serialize(rows.Select(x=>x[1..]))));
        }
        var storage=await Native("""
            SELECT t.temporal_type,CONVERT(varbinary(max),CONVERT(nvarchar(max),hs.name)) AS [__dbmv_utf16],CONVERT(varbinary(max),CONVERT(nvarchar(max),h.name)) AS [__dbmv_utf16],t.lock_escalation,t.durability,t.is_memory_optimized,
                   t.is_filetable,t.is_external,t.is_node,t.is_edge,t.text_in_row_limit,t.large_value_types_out_of_row,
                   CONVERT(varbinary(max),CONVERT(nvarchar(max),ds.name)) AS [__dbmv_utf16],t.is_tracked_by_cdc,t.filestream_data_space_id,
                   (SELECT COUNT(*) FROM sys.fulltext_indexes f WHERE f.object_id=t.object_id)
            FROM sys.tables t LEFT JOIN sys.tables h ON h.object_id=t.history_table_id
            LEFT JOIN sys.schemas hs ON hs.schema_id=h.schema_id
            LEFT JOIN sys.data_spaces ds ON ds.data_space_id=t.lob_data_space_id
            WHERE t.object_id=object_id(QUOTENAME(CONVERT(nvarchar(128),@schema))+'.'+QUOTENAME(CONVERT(nvarchar(128),@table)),'U')
            """,table,token);
        if(storage.Count!=1)throw new VerificationException(Reason.ObservationChanged);
        indexes.Add(new("table",null,"TableOptions",SqlServerMetadata.Serialize(storage[0])));
        var partitionRows=await Native("""
            SELECT CONVERT(varbinary(max),CONVERT(nvarchar(max),i.name)) AS [__dbmv_utf16],p.partition_number,p.data_compression,CONVERT(varbinary(max),CONVERT(nvarchar(max),ps.name)) AS [__dbmv_utf16],CONVERT(varbinary(max),CONVERT(nvarchar(max),pf.name)) AS [__dbmv_utf16],pf.boundary_value_on_right,
                   ic.partition_ordinal,CONVERT(varbinary(max),CONVERT(nvarchar(max),c.name)) AS [__dbmv_utf16],p.xml_compression
            FROM sys.indexes i JOIN sys.partitions p ON p.object_id=i.object_id AND p.index_id=i.index_id
            LEFT JOIN sys.partition_schemes ps ON ps.data_space_id=i.data_space_id
            LEFT JOIN sys.partition_functions pf ON pf.function_id=ps.function_id
            LEFT JOIN sys.index_columns ic ON ic.object_id=i.object_id AND ic.index_id=i.index_id AND ic.partition_ordinal>0
            LEFT JOIN sys.columns c ON c.object_id=ic.object_id AND c.column_id=ic.column_id
            WHERE i.object_id=object_id(QUOTENAME(CONVERT(nvarchar(128),@schema))+'.'+QUOTENAME(CONVERT(nvarchar(128),@table)),'U')
            ORDER BY i.name COLLATE Latin1_General_100_BIN2,p.partition_number,ic.partition_ordinal
            """,table,token);
        indexes.Add(new("table",null,"PartitionKey",SqlServerMetadata.Serialize(partitionRows)));
        var partitioned=partitionRows.Any(x=>x[3]!=null);
        if(partitioned)
        {var bounds=await ReadPartitionBounds(table,token);indexes.AddRange(bounds.Facts);gaps.AddRange(bounds.Gaps);boundsComplete=bounds.Complete;}
        if(storage[0][15]!="0")
        {var fulltext=await ReadFulltext(table,token);indexes.AddRange(fulltext.Facts);gaps.AddRange(fulltext.Gaps);}
        return table with{RowKeys=keys.OrderBy(x=>x.Count).ToArray(),Partitioned=partitioned,Catalog=table.Catalog.Concat(new CatalogSection[]{
            new("SCHEMA-02",constraints,complete?Reason.None:Reason.VisibilityUnknown),
            new("SCHEMA-03",indexes,!complete?Reason.VisibilityUnknown:gaps.Count>0?gaps[0].Reason:Reason.None){Gaps=gaps,CompleteExceptGaps=complete&&boundsComplete&&gaps.Count>0}
        }).ToArray()};
    }
    private const string ForeignQuery="""
        SELECT CONVERT(varbinary(max),CONVERT(nvarchar(max),fk.name)) AS [__dbmv_utf16],CONVERT(varbinary(max),CONVERT(nvarchar(max),pc.name)) AS [__dbmv_utf16],CONVERT(varbinary(max),CONVERT(nvarchar(max),rs.name)) AS [__dbmv_utf16],CONVERT(varbinary(max),CONVERT(nvarchar(max),rt.name)) AS [__dbmv_utf16],CONVERT(varbinary(max),CONVERT(nvarchar(max),rc.name)) AS [__dbmv_utf16],fkc.constraint_column_id,
               fk.delete_referential_action,fk.update_referential_action,fk.is_disabled,fk.is_not_trusted,fk.is_not_for_replication
        FROM sys.foreign_keys fk JOIN sys.foreign_key_columns fkc ON fkc.constraint_object_id=fk.object_id
        JOIN sys.columns pc ON pc.object_id=fkc.parent_object_id AND pc.column_id=fkc.parent_column_id
        JOIN sys.tables rt ON rt.object_id=fkc.referenced_object_id JOIN sys.schemas rs ON rs.schema_id=rt.schema_id
        JOIN sys.columns rc ON rc.object_id=fkc.referenced_object_id AND rc.column_id=fkc.referenced_column_id
        WHERE fk.parent_object_id=object_id(QUOTENAME(CONVERT(nvarchar(128),@schema))+'.'+QUOTENAME(CONVERT(nvarchar(128),@table)),'U') ORDER BY fk.name,fkc.constraint_column_id
        """;
}
