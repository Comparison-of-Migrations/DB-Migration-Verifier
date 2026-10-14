using System.Text.Json;
using MySqlConnector;
using Verifier.Core;
namespace Verifier.Data;
public sealed partial class MariaDbDatabase
{
    private sealed record Structure(CatalogSection Constraints,CatalogSection Indexes,string Signature);
    private async Task<Structure> ReadStructure(Table table,CancellationToken token)
    {
        // Dictionary definitions only: never execute CHECK, generated expressions,
        // views or routines. MariaDB catalogs differ from MySQL's ENFORCED and
        // IS_VISIBLE/EXPRESSION columns, and use uppercase #P# leaf delimiters.
        var native=await Native("SELECT CONSTRAINT_NAME,CONSTRAINT_TYPE FROM information_schema.TABLE_CONSTRAINTS WHERE "+TableFilter+" ORDER BY CAST(CONSTRAINT_NAME AS BINARY)",table,token);
        var columns=await Native("SELECT CONSTRAINT_NAME,COLUMN_NAME,ORDINAL_POSITION,POSITION_IN_UNIQUE_CONSTRAINT,REFERENCED_TABLE_SCHEMA,REFERENCED_TABLE_NAME,REFERENCED_COLUMN_NAME FROM information_schema.KEY_COLUMN_USAGE WHERE "+TableFilter+" ORDER BY CAST(CONSTRAINT_NAME AS BINARY),ORDINAL_POSITION",table,token);
        var refs=await Native("SELECT CONSTRAINT_NAME,UNIQUE_CONSTRAINT_SCHEMA,UNIQUE_CONSTRAINT_NAME,MATCH_OPTION,UPDATE_RULE,DELETE_RULE,REFERENCED_TABLE_NAME FROM information_schema.REFERENTIAL_CONSTRAINTS WHERE CAST(CONSTRAINT_SCHEMA AS BINARY)=CAST(@schema AS BINARY) AND CAST(TABLE_NAME AS BINARY)=CAST(@table AS BINARY) ORDER BY CAST(CONSTRAINT_NAME AS BINARY)",table,token);
        var checks=await Native("SELECT CONSTRAINT_NAME,LEVEL,CHECK_CLAUSE FROM information_schema.CHECK_CONSTRAINTS WHERE CAST(CONSTRAINT_SCHEMA AS BINARY)=CAST(@schema AS BINARY) AND CAST(TABLE_NAME AS BINARY)=CAST(@table AS BINARY) ORDER BY CAST(CONSTRAINT_NAME AS BINARY)",table,token);
        var stats=await Native("SELECT INDEX_NAME,NON_UNIQUE,SEQ_IN_INDEX,COLUMN_NAME,COLLATION,SUB_PART,PACKED,NULLABLE,INDEX_TYPE,COMMENT,INDEX_COMMENT,IGNORED FROM information_schema.STATISTICS WHERE "+TableFilter+" ORDER BY CAST(INDEX_NAME AS BINARY),SEQ_IN_INDEX",table,token);
        var periods=await Native("SELECT `PERIOD`,START_COLUMN_NAME,END_COLUMN_NAME FROM information_schema.PERIODS WHERE "+TableFilter+" ORDER BY CAST(`PERIOD` AS BINARY)",table,token);
        var periodKeys=await Native("SELECT CONSTRAINT_NAME,PERIOD_NAME FROM information_schema.KEY_PERIOD_USAGE WHERE "+TableFilter+" ORDER BY CAST(CONSTRAINT_NAME AS BINARY)",table,token);
        var constraints=new List<CatalogFact>();var indexes=new List<CatalogFact>();var gaps=new List<CatalogGap>();
        foreach(var p in periods)constraints.Add(new(JsonSerializer.Serialize(new[]{"period",p[0]}),null,"PeriodDefinition",JsonSerializer.Serialize(p.Skip(1))));
        foreach(var k in periodKeys)
        {
            var key=JsonSerializer.Serialize(new[]{"constraint",k[0]});
            if(!native.Any(c=>c[0]==k[0]&&c[1] is "UNIQUE" or "PRIMARY KEY")||!periods.Any(p=>p[0]==k[1]))gaps.Add(new(key,null,"ConstraintPeriod","",Reason.VisibilityUnknown));
            else constraints.Add(new(key,null,"ConstraintPeriod",JsonSerializer.Serialize(k[1])));
        }
        foreach(var c in native)
        {
            var name=c[0]!;var key=JsonSerializer.Serialize(new[]{"constraint",name});var parts=columns.Where(x=>x[0]==name).Select(x=>new{column=x[1],order=x[2],referencedOrder=x[3],database=Reference(x[4]),table=x[5],targetColumn=x[6]}).ToArray();var reference=refs.Where(x=>x[0]==name).Select(x=>new{database=Reference(x[1]),key=x[2],match=x[3],update=x[4],delete=x[5],table=x[6]}).ToArray();var check=checks.Where(x=>x[0]==name).Select(x=>x.Skip(1).ToArray()).ToArray();
            constraints.Add(new(key,null,"ConstraintType",c[1]));
            if(c[1]=="CHECK"&&(check.Length!=1||check[0][1]==null)||c[1]=="FOREIGN KEY"&&(parts.Length==0||reference.Length!=1)||c[1] is "PRIMARY KEY" or "UNIQUE"&&parts.Length==0)gaps.Add(new(key,null,"ConstraintDefinition","",Reason.VisibilityUnknown));
            else constraints.Add(new(key,null,"ConstraintDefinition",JsonSerializer.Serialize(new{parts,reference,check})));
        }
        foreach(var g in stats.GroupBy(x=>x[0]!,StringComparer.Ordinal))
        {
            var rows=g.ToArray();var key=JsonSerializer.Serialize(new[]{"index",g.Key});indexes.Add(new(key,null,"IndexDefinition",JsonSerializer.Serialize(rows.Select(x=>new{column=x[3],order=x[2],direction=x[4],prefix=x[5],packed=x[6],nullable=x[7],type=x[8]}))));indexes.Add(new(key,null,"IndexUnique",rows[0][1]=="0"?"true":"false"));indexes.Add(new(key,null,"IndexPrimary",g.Key=="PRIMARY"?"true":"false"));indexes.Add(new(key,null,"IndexVisible",JsonSerializer.Serialize(rows.Select(x=>x[11]=="YES"?"NO":"YES"))));indexes.Add(new(key,null,"IndexOptions",JsonSerializer.Serialize(rows.Select(x=>new[]{x[9],x[10]}))));
        }
        var storage=await Native("SELECT ENGINE,ROW_FORMAT,TABLE_COLLATION,CREATE_OPTIONS,TABLE_COMMENT FROM information_schema.TABLES WHERE "+TableFilter+" AND TABLE_TYPE IN ('BASE TABLE','SYSTEM VERSIONED')",table,token);if(storage.Count!=1)throw new VerificationException(Reason.ObservationChanged);
        indexes.Add(new("table",null,"TableAccessMethod",storage[0][0]));indexes.Add(new("table",null,"TableComment",storage[0][4]));indexes.Add(new("table",null,"TableCollation",storage[0][2]));indexes.Add(new("table",null,"TableOptions",JsonSerializer.Serialize(new[]{storage[0][1],storage[0][3]})));
        var partitions=await Native("SELECT PARTITION_NAME,SUBPARTITION_NAME,PARTITION_ORDINAL_POSITION,SUBPARTITION_ORDINAL_POSITION,PARTITION_METHOD,SUBPARTITION_METHOD,PARTITION_EXPRESSION,SUBPARTITION_EXPRESSION,PARTITION_DESCRIPTION,PARTITION_COMMENT,NODEGROUP,TABLESPACE_NAME FROM information_schema.PARTITIONS WHERE "+TableFilter+" ORDER BY PARTITION_ORDINAL_POSITION,SUBPARTITION_ORDINAL_POSITION",table,token);indexes.Add(new("table",null,"PartitionChildren",JsonSerializer.Serialize(partitions)));
        var signature=JsonSerializer.Serialize(new{constraints,stats,storage,partitions,periods,periodKeys});var binding=storage[0][0]=="InnoDB"?await ReadTablespaces(table,partitions,token):StorageGap(Reason.NotImplemented);indexes.AddRange(binding.Facts);
        return new(new("SCHEMA-02",constraints,gaps.Count==0?Reason.None:Reason.VisibilityUnknown){CompleteExceptGaps=gaps.Count>0,Gaps=gaps},new("SCHEMA-03",indexes,binding.Reason){CompleteExceptGaps=binding.CompleteExceptGaps,Gaps=binding.Gaps},signature);
        object? Reference(string? name)=>name==null?null:new{ownDatabase=name==table.Schema,name=name==table.Schema?null:name};
    }
    private static CatalogSection StorageGap(Reason reason)=>new("SCHEMA-03",[],reason){CompleteExceptGaps=true,Gaps=[new("table",null,"TableTablespace","",reason)]};
    private async Task<CatalogSection> ReadTablespaces(Table table,List<string?[]> parts,CancellationToken token)
    {
        // Shared codec only after MariaDB's independent 2016-character native
        // oracle matched MySQL. Leaf delimiter/case remain MariaDB-specific.
        try
        {
            var logical=(await Native("SELECT IF(@@lower_case_table_names=0,@schema,LOWER(@schema)),IF(@@lower_case_table_names=0,@table,LOWER(@table))",table,token)).Single();var schema=InnoDbIdentifier.Encode(logical[0]!);var name=InnoDbIdentifier.Encode(logical[1]!);if(schema==null||name==null)return StorageGap(Reason.NotImplemented);
            var leaves=new List<(string Name,string? Partition,string? Subpartition)>();foreach(var p in parts){var part=p[0]==null?null:InnoDbIdentifier.Encode(p[0]!);var sub=p[1]==null?null:InnoDbIdentifier.Encode(p[1]!);if(p[0]!=null&&part==null||p[1]!=null&&sub==null)return StorageGap(Reason.NotImplemented);leaves.Add((schema+"/"+name+(part==null?"":"#P#"+part)+(sub==null?"":"#SP#"+sub),p[0],p[1]));}if(leaves.Count==0||leaves.Select(x=>x.Name).Distinct(StringComparer.Ordinal).Count()!=leaves.Count)return StorageGap(Reason.ObservationChanged);
            await using var q=Command("SELECT t.NAME,t.SPACE,t.SPACE_TYPE,t.ROW_FORMAT,t.ZIP_PAGE_SIZE,s.NAME,s.PAGE_SIZE,@@innodb_page_size FROM information_schema.INNODB_SYS_TABLES t LEFT JOIN information_schema.INNODB_SYS_TABLESPACES s ON t.SPACE=s.SPACE WHERE CAST(t.NAME AS BINARY) IN ("+string.Join(',',leaves.Select((_,i)=>"CAST(@leaf"+i+" AS BINARY)"))+")");for(var i=0;i<leaves.Count;i++)q.Parameters.AddWithValue("leaf"+i,leaves[i].Name);
            var rows=new Dictionary<string,string?[]>(StringComparer.Ordinal);await using(var r=await q.ExecuteReaderAsync(token)){while(await r.ReadAsync(token)){var row=Enumerable.Range(0,r.FieldCount).Select(i=>r.IsDBNull(i)?null:Convert.ToString(r.GetValue(i),System.Globalization.CultureInfo.InvariantCulture)).ToArray();if(!rows.TryAdd(row[0]!,row))return StorageGap(Reason.ObservationChanged);}}if(rows.Count!=leaves.Count)return StorageGap(Reason.ObservationChanged);
            var values=new List<object>();foreach(var leaf in leaves)
            {
                var r=rows[leaf.Name];var system=r[1]=="0";if(system?r[2]!="System":r[2]!="Single"||r[5]!=leaf.Name||r[6]==null)return StorageGap(Reason.ObservationChanged);
                values.Add(new{partition=leaf.Partition,subpartition=leaf.Subpartition,kind=r[2],format=r[3],zip=r[4],page=system?r[7]:r[6]});
            }
            return new("SCHEMA-03",[new("table",null,"TableTablespace",JsonSerializer.Serialize(values))],Reason.None);
        }
        catch(MySqlException e){return StorageGap(Map(e).Reason);}
    }
    private async Task<CatalogSection> DatabaseDefinition(CancellationToken token)
    {
        var rows=await Native("SELECT DEFAULT_CHARACTER_SET_NAME,DEFAULT_COLLATION_NAME FROM information_schema.SCHEMATA WHERE CAST(SCHEMA_NAME AS BINARY)=CAST(DATABASE() AS BINARY)",null,token);return new("SCHEMA-01",rows.Count==1?[new("database",null,"DatabaseDefinition",JsonSerializer.Serialize(rows[0]))]:[],rows.Count==1?Reason.None:Reason.VisibilityUnknown);
    }
}
