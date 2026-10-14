using System.Globalization;
using System.Text.Json;
using MySqlConnector;
using Verifier.Core;
namespace Verifier.Data;

public sealed partial class MySqlDatabase
{
    private async Task<CatalogSection> ReadTablespaces(Table table,List<string?[]> partitions,CancellationToken token)
    {
        try
        {
            // Native logical case conversion precedes filesystem encoding.
            var logical=(await ReadNative("SELECT IF(@@lower_case_table_names=0,@schema,LOWER(@schema)),IF(@@lower_case_table_names=0,@table,LOWER(@table))",table,token)).Single();
            var schema=InnoDbIdentifier.Encode(logical[0]!);var name=InnoDbIdentifier.Encode(logical[1]!);
            if(schema==null||name==null)return Gap(Reason.NotImplemented);
            var leaves=new List<(string Name,string? Partition,string? Subpartition)>();
            foreach(var part in partitions)
            {
                var p=part[0]==null?null:InnoDbIdentifier.Encode(part[12]!);var sub=part[1]==null?null:InnoDbIdentifier.Encode(part[13]!);
                if(part[0]!=null&&p==null||part[1]!=null&&sub==null)return Gap(Reason.NotImplemented);
                leaves.Add((schema+"/"+name+(p==null?"":"#p#"+p)+(sub==null?"":"#sp#"+sub),part[0],part[1]));
            }
            if(leaves.Count==0||leaves.Select(x=>x.Name).Distinct(StringComparer.Ordinal).Count()!=leaves.Count)return Gap(Reason.ObservationChanged);
            await using var q=Command("SELECT t.NAME,t.SPACE,t.SPACE_TYPE,s.NAME,s.SPACE_TYPE,s.ROW_FORMAT,s.PAGE_SIZE,s.ZIP_PAGE_SIZE,s.AUTOEXTEND_SIZE,s.ENCRYPTION,s.STATE,@@innodb_page_size FROM information_schema.INNODB_TABLES t LEFT JOIN information_schema.INNODB_TABLESPACES s ON s.SPACE=t.SPACE WHERE CAST(t.NAME AS BINARY) IN ("+string.Join(',',leaves.Select((_,i)=>"CAST(@n"+i+" AS BINARY)"))+")");
            for(var i=0;i<leaves.Count;i++)q.Parameters.AddWithValue("n"+i,leaves[i].Name);
            var rows=new Dictionary<string,string?[]>(StringComparer.Ordinal);
            await using(var r=await q.ExecuteReaderAsync(token))
            {while(await r.ReadAsync(token)){var row=Enumerable.Range(0,r.FieldCount).Select(i=>r.IsDBNull(i)?null:Convert.ToString(r.GetValue(i),CultureInfo.InvariantCulture)).ToArray();if(!rows.TryAdd(row[0]!,row))return Gap(Reason.ObservationChanged);}}
            if(rows.Count!=leaves.Count)return Gap(Reason.ObservationChanged);
            var bindings=new List<object>();
            foreach(var leaf in leaves)
            {
                var row=rows[leaf.Name];var system=row[1]=="0";
                if(system)
                {
                    if(row[2]!="System")return Gap(Reason.ObservationChanged);
                    bindings.Add(new{partition=leaf.Partition,subpartition=leaf.Subpartition,kind="System",name="innodb_system",page=row[11]});
                }
                else
                {
                    if(row[2] is not ("Single" or "General")||row[2]!=row[4]||row.Skip(3).Take(8).Any(x=>x==null))return Gap(Reason.ObservationChanged);
                    // Single space names contain physical schema/table names;
                    // the validated leaf binding already identifies that scope.
                    bindings.Add(new{partition=leaf.Partition,subpartition=leaf.Subpartition,kind=row[2],name=row[2]=="Single"?null:row[3],format=row[5],page=row[6],zip=row[7],autoextend=row[8],encryption=row[9],state=row[10]});
                }
            }
            return new("SCHEMA-03",[new("table",null,"TableTablespace",JsonSerializer.Serialize(bindings))],Reason.None);
        }
        catch(MySqlException e){return Gap(Map(e).Reason);}
        static CatalogSection Gap(Reason reason)=>new("SCHEMA-03",[],reason){CompleteExceptGaps=true,Gaps=[new("table",null,"TableTablespace","",reason)]};
    }
}
