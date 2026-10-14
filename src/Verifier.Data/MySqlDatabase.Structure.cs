using System.Globalization;
using System.Text.Json;
using Verifier.Core;
namespace Verifier.Data;

public sealed partial class MySqlDatabase
{
    private sealed record Structure(CatalogSection Constraints,CatalogSection Indexes,IReadOnlyList<IReadOnlyList<string>> Keys,bool Partitioned,string Signature);
    // Only native dictionary columns: never execute a CHECK, expression index,
    // generated expression, trigger or user routine to inspect its definition.
    private async Task<List<string?[]>> ReadNative(string sql,Table? table,CancellationToken token)
    {
        await using var q=Command(sql);
        if(table!=null){q.Parameters.AddWithValue("schema",table.Schema);q.Parameters.AddWithValue("table",table.Name);}
        await using var r=await q.ExecuteReaderAsync(token);var rows=new List<string?[]>();
        while(await r.ReadAsync(token))rows.Add(Enumerable.Range(0,r.FieldCount).Select(i=>r.IsDBNull(i)?null:Convert.ToString(r.GetValue(i),CultureInfo.InvariantCulture)).ToArray());
        return rows;
    }
    private const string TableFilter="CAST(TABLE_SCHEMA AS BINARY)=CAST(@schema AS BINARY) AND CAST(TABLE_NAME AS BINARY)=CAST(@table AS BINARY)";
    private async Task<Structure> ReadStructure(Table table,CancellationToken token)
    {
        var constraints=new List<CatalogFact>();var gaps=new List<CatalogGap>();var indexes=new List<CatalogFact>();var keys=new List<IReadOnlyList<string>>();
        var c=await ReadNative("SELECT CONSTRAINT_NAME,CONSTRAINT_TYPE,ENFORCED FROM information_schema.TABLE_CONSTRAINTS WHERE "+TableFilter+" ORDER BY CAST(CONSTRAINT_NAME AS BINARY)",table,token);
        var columns=await ReadNative("SELECT CONSTRAINT_NAME,COLUMN_NAME,ORDINAL_POSITION,POSITION_IN_UNIQUE_CONSTRAINT,REFERENCED_TABLE_SCHEMA,REFERENCED_TABLE_NAME,REFERENCED_COLUMN_NAME FROM information_schema.KEY_COLUMN_USAGE WHERE "+TableFilter+" ORDER BY CAST(CONSTRAINT_NAME AS BINARY),ORDINAL_POSITION",table,token);
        var references=await ReadNative("SELECT CONSTRAINT_NAME,UNIQUE_CONSTRAINT_SCHEMA,UNIQUE_CONSTRAINT_NAME,MATCH_OPTION,UPDATE_RULE,DELETE_RULE,REFERENCED_TABLE_NAME FROM information_schema.REFERENTIAL_CONSTRAINTS WHERE CAST(CONSTRAINT_SCHEMA AS BINARY)=CAST(@schema AS BINARY) AND CAST(TABLE_NAME AS BINARY)=CAST(@table AS BINARY) ORDER BY CAST(CONSTRAINT_NAME AS BINARY)",table,token);
        var checks=await ReadNative("SELECT x.CONSTRAINT_NAME,x.CHECK_CLAUSE FROM information_schema.CHECK_CONSTRAINTS x JOIN information_schema.TABLE_CONSTRAINTS t ON CAST(x.CONSTRAINT_SCHEMA AS BINARY)=CAST(t.CONSTRAINT_SCHEMA AS BINARY) AND CAST(x.CONSTRAINT_NAME AS BINARY)=CAST(t.CONSTRAINT_NAME AS BINARY) WHERE CAST(t.TABLE_SCHEMA AS BINARY)=CAST(@schema AS BINARY) AND CAST(t.TABLE_NAME AS BINARY)=CAST(@table AS BINARY) ORDER BY CAST(x.CONSTRAINT_NAME AS BINARY)",table,token);
        var stats=await ReadNative("SELECT INDEX_NAME,NON_UNIQUE,SEQ_IN_INDEX,COLUMN_NAME,COLLATION,SUB_PART,PACKED,NULLABLE,INDEX_TYPE,COMMENT,INDEX_COMMENT,IS_VISIBLE,EXPRESSION FROM information_schema.STATISTICS WHERE "+TableFilter+" ORDER BY CAST(INDEX_NAME AS BINARY),SEQ_IN_INDEX",table,token);
        foreach(var row in c)
        {
            var name=row[0]!;var key=JsonSerializer.Serialize(new[]{"constraint",name});
            constraints.Add(new(key,null,"ConstraintType",row[1]));constraints.Add(new(key,null,"ConstraintEnforced",row[2]));
            // A selected MySQL database is a namespace, not a renamed FK target.
            // Preserve external database names and use an explicit flag for self.
            var parts=columns.Where(x=>x[0]==name).Select(x=>new{x=x[1],order=x[2],referencedOrder=x[3],database=Reference(x[4]),table=x[5],column=x[6]}).ToArray();
            var refs=references.Where(x=>x[0]==name).Select(x=>new{database=Reference(x[1]),key=x[2],match=x[3],update=x[4],delete=x[5],table=x[6]}).ToArray();
            var check=checks.Where(x=>x[0]==name).ToArray();
            // KCU intentionally omits functional key parts; STATISTICS exposes
            // their native expressions and prefix lengths without evaluation.
            var indexParts=row[1] is "PRIMARY KEY" or "UNIQUE"?stats.Where(x=>x[0]==name).Select(x=>new{column=x[3],order=x[2],prefix=x[5],expression=x[12]}).ToArray():[];
            if(row[1]=="CHECK"&&(check.Length!=1||check[0][1]==null)||row[1]=="FOREIGN KEY"&&(parts.Length==0||refs.Length!=1)||row[1] is "PRIMARY KEY" or "UNIQUE"&&indexParts.Length==0)
                gaps.Add(new(key,null,"ConstraintDefinition","",Reason.VisibilityUnknown));
            else constraints.Add(new(key,null,"ConstraintDefinition",JsonSerializer.Serialize(new{parts,refs,indexParts,check=check.Select(x=>x[1]).ToArray()})));
        }
        foreach(var group in stats.GroupBy(x=>x[0]!,StringComparer.Ordinal))
        {
            var rows=group.ToArray();var key=JsonSerializer.Serialize(new[]{"index",group.Key});var first=rows[0];
            indexes.Add(new(key,null,"IndexDefinition",JsonSerializer.Serialize(rows.Select(x=>new{x=x[3],order=x[2],direction=x[4],prefix=x[5],packed=x[6],nullable=x[7],type=x[8],expression=x[12]}))));
            indexes.Add(new(key,null,"IndexUnique",first[1]=="0"?"true":"false"));indexes.Add(new(key,null,"IndexPrimary",group.Key=="PRIMARY"?"true":"false"));
            indexes.Add(new(key,null,"IndexVisible",JsonSerializer.Serialize(rows.Select(x=>x[11]))));indexes.Add(new(key,null,"IndexOptions",JsonSerializer.Serialize(rows.Select(x=>new[]{x[9],x[10]}))));
            // Visibility is an optimizer hint, not whether uniqueness is enforced.
            // Prefix and functional indexes cannot identify a full stored value.
            if(rows.All(x=>x[1]=="0"&&x[3]!=null&&x[5]==null&&x[12]==null)&&rows.Select((x,i)=>x[2]==(i+1).ToString(CultureInfo.InvariantCulture)).All(x=>x))keys.Add(rows.Select(x=>x[3]!).ToArray());
        }
        keys=keys.OrderBy(k=>k.Count).ToList();
        var storage=await ReadNative("SELECT ENGINE,ROW_FORMAT,TABLE_COLLATION,CREATE_OPTIONS,TABLE_COMMENT FROM information_schema.TABLES WHERE "+TableFilter+" AND TABLE_TYPE='BASE TABLE'",table,token);
        if(storage.Count!=1)throw new VerificationException(Reason.ObservationChanged);
        indexes.Add(new("table",null,"TableAccessMethod",storage[0][0]));indexes.Add(new("table",null,"TableComment",storage[0][4]));indexes.Add(new("table",null,"TableCollation",storage[0][2]));indexes.Add(new("table",null,"TableOptions",JsonSerializer.Serialize(new[]{storage[0][1],storage[0][3]})));
        // Deliberately exclude row counts, sizes, cached cardinalities and times:
        // those are observations, not definitions, and may be cached for 24 hours.
        var partitions=await ReadNative("SELECT PARTITION_NAME,SUBPARTITION_NAME,PARTITION_ORDINAL_POSITION,SUBPARTITION_ORDINAL_POSITION,PARTITION_METHOD,SUBPARTITION_METHOD,PARTITION_EXPRESSION,SUBPARTITION_EXPRESSION,PARTITION_DESCRIPTION,PARTITION_COMMENT,NODEGROUP,TABLESPACE_NAME,LOWER(PARTITION_NAME),LOWER(SUBPARTITION_NAME) FROM information_schema.PARTITIONS WHERE "+TableFilter+" ORDER BY PARTITION_ORDINAL_POSITION,SUBPARTITION_ORDINAL_POSITION",table,token);
        var partitioned=partitions.Any(x=>x[0]!=null);
        indexes.Add(new("table",null,"PartitionChildren",JsonSerializer.Serialize(partitions)));
        var constraintSection=new CatalogSection("SCHEMA-02",constraints,gaps.Count==0?Reason.None:Reason.VisibilityUnknown){Gaps=gaps,CompleteExceptGaps=gaps.Count>0};
        var binding=storage[0][0]=="InnoDB"?await ReadTablespaces(table,partitions,token):new CatalogSection("SCHEMA-03",[],Reason.NotImplemented){CompleteExceptGaps=true,Gaps=[new("table",null,"TableTablespace","",Reason.NotImplemented)]};
        indexes.AddRange(binding.Facts);
        var indexSection=new CatalogSection("SCHEMA-03",indexes,binding.Reason){CompleteExceptGaps=binding.CompleteExceptGaps,Gaps=binding.Gaps};
        return new(constraintSection,indexSection,keys,partitioned,JsonSerializer.Serialize(new{constraints=constraintSection,indexes=indexSection,keys}));
        object? Reference(string? name)=>name==null?null:new{ownDatabase=name==table.Schema,name=name==table.Schema?null:name};
    }
    private async Task<MySqlDatabase> MetadataObserver(CancellationToken token)
    {
        if(metadataObserver!=null)return metadataObserver;
        var opened=new MySqlDatabase(settings){connection=new(settings.MySqlString())};
        try
        {
            await opened.connection.OpenAsync(token);
            await using var setup=opened.Command("SET SESSION time_zone='+00:00';SET SESSION show_gipk_in_create_table_and_information_schema=ON;SET SESSION information_schema_stats_expiry=0");await setup.ExecuteNonQueryAsync(token);
            return metadataObserver=opened;
        }
        catch{await opened.DisposeAsync();throw;}
    }
    private async Task ValidateDefinition(Table table,CancellationToken token)
    {
        try
        {
            // INFORMATION_SCHEMA dictionary reads inside the data RR transaction
            // can keep an earlier dictionary version after DDL. Re-reading there
            // is not evidence of unchanged current definitions. Use a separate
            // authenticated, read-only, short READ COMMITTED metadata observation.
            var observer=await MetadataObserver(token);
            observer.transaction=await observer.connection!.BeginTransactionAsync(System.Data.IsolationLevel.ReadCommitted,true,token);
            try
            {
                var columns=await observer.ReadColumns(table,token);var structure=await observer.ReadStructure(table,token);
                if(columns.Signature+structure.Signature!=tableDefinitions[table.Qualified])throw new VerificationException(Reason.ObservationChanged);
            }
            finally{await observer.transaction.DisposeAsync();observer.transaction=null;}
        }
        catch(MySqlConnector.MySqlException e){throw Map(e);}
    }
}
