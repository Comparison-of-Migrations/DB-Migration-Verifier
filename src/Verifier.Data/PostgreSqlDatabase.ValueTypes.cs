using Npgsql;
using NpgsqlTypes;
using Verifier.Core;
namespace Verifier.Data;

public sealed partial class PostgreSqlDatabase
{
    private sealed record ValueType(uint Oid,string Schema,string Name,string Kind,uint Base,uint Element,string? Send,string? SendSchema,uint[] Fields,string[] FieldNames,uint Range,uint MultiRange);
    internal sealed record ValueCodec(string Send,string CastType,bool Numeric,uint ArrayElement=0)
    {
        public ValueCodec? Element{get;init;}
        public IReadOnlyList<(uint Oid,ValueCodec Codec)>? Fields{get;init;}
        public ValueCodec? Range{get;init;}
        public bool MultiRange{get;init;}
        public bool RequiresNormalization=>Numeric||ArrayElement!=0||Fields!=null||Range!=null;
    }
    private readonly Dictionary<uint,ValueCodec> customValueCodecs=[];
    private readonly HashSet<uint> domainValueTypes=[];
    private readonly Dictionary<uint,ValueType> valueTypes=[];
    private async Task<List<Table>> ReadCustomValueCodecs(List<Table> tables,CancellationToken token)
    {
        var roots=tables.SelectMany(t=>t.Columns).Where(c=>!c.SafeValue||ArrayCodecs.Contains(c.Type)).Select(c=>c.TypeOid).Where(o=>o!=0).Distinct().ToArray();
        if(roots.Length==0)return tables;
        var types=new Dictionary<uint,ValueType>();
        await using(var save=new NpgsqlCommand("SAVEPOINT dmv_value_types",connection,transaction))await save.ExecuteNonQueryAsync(token);
        try
        {
            const string sql="""
                WITH RECURSIVE edges(id,dependency) AS (
                    SELECT oid,typbasetype FROM pg_catalog.pg_type WHERE typbasetype<>0
                    UNION SELECT oid,typelem FROM pg_catalog.pg_type WHERE typelem<>0
                    UNION SELECT t.oid,a.atttypid FROM pg_catalog.pg_type t JOIN pg_catalog.pg_attribute a ON a.attrelid=t.typrelid AND a.attnum>0 AND NOT a.attisdropped
                    UNION SELECT rngtypid,rngsubtype FROM pg_catalog.pg_range
                    UNION SELECT rngmultitypid,rngtypid FROM pg_catalog.pg_range
                ), types(id) AS (SELECT pg_catalog.unnest(@ids::pg_catalog.oid[])
                    UNION SELECT e.dependency FROM types t JOIN edges e ON e.id=t.id)
                SELECT t.oid,n.nspname,t.typname,t.typtype::pg_catalog.text,t.typbasetype,t.typelem,p.proname,pn.nspname,
                  ARRAY(SELECT a.atttypid FROM pg_catalog.pg_attribute a WHERE a.attrelid=t.typrelid AND a.attnum>0 AND NOT a.attisdropped ORDER BY a.attnum),
                  ARRAY(SELECT a.attname::pg_catalog.text FROM pg_catalog.pg_attribute a WHERE a.attrelid=t.typrelid AND a.attnum>0 AND NOT a.attisdropped ORDER BY a.attnum),
                  COALESCE((SELECT r.rngsubtype FROM pg_catalog.pg_range r WHERE r.rngtypid=t.oid),0),
                  COALESCE((SELECT r.rngtypid FROM pg_catalog.pg_range r WHERE r.rngmultitypid=t.oid),0)
                FROM types x JOIN pg_catalog.pg_type t ON t.oid=x.id JOIN pg_catalog.pg_namespace n ON n.oid=t.typnamespace
                LEFT JOIN pg_catalog.pg_proc p ON p.oid=t.typsend LEFT JOIN pg_catalog.pg_namespace pn ON pn.oid=p.pronamespace
                WHERE t.typisdefined
                """;
            await using(var command=new NpgsqlCommand(sql,connection,transaction))
            {
                command.Parameters.AddWithValue("ids",NpgsqlDbType.Array|NpgsqlDbType.Oid,roots);
                await using var reader=await command.ExecuteReaderAsync(token);
                while(await reader.ReadAsync(token))
                {var type=new ValueType(reader.GetFieldValue<uint>(0),reader.GetString(1),reader.GetString(2),reader.GetString(3),reader.GetFieldValue<uint>(4),reader.GetFieldValue<uint>(5),reader.IsDBNull(6)?null:reader.GetString(6),reader.IsDBNull(7)?null:reader.GetString(7),reader.GetFieldValue<uint[]>(8),reader.GetFieldValue<string[]>(9),reader.GetFieldValue<uint>(10),reader.GetFieldValue<uint>(11));types.Add(type.Oid,type);}
            }
            await using(var release=new NpgsqlCommand("RELEASE SAVEPOINT dmv_value_types",connection,transaction))await release.ExecuteNonQueryAsync(token);
        }
        catch(PostgresException e) when(e.SqlState=="42501")
        {
            await using var restore=new NpgsqlCommand("ROLLBACK TO SAVEPOINT dmv_value_types; RELEASE SAVEPOINT dmv_value_types",connection,transaction);await restore.ExecuteNonQueryAsync(token);
            return tables.Select(t=>t with{Columns=t.Columns.Select(c=>ArrayCodecs.Contains(c.Type)?c with{SafeValue=false}:c).ToArray()}).ToList();
        }
        foreach(var (id,type) in types)valueTypes[id]=type;
        ValueCodec? Resolve(uint id,HashSet<uint> seen)
        {
            if(!seen.Add(id)||!types.TryGetValue(id,out var type))return null;
            var cast=Quote(type.Schema)+"."+Quote(type.Name);
            if(type.Kind=="d")return Resolve(type.Base,seen); // Domain -> base coercion does not re-evaluate domain checks.
            if(type.SendSchema!="pg_catalog")return null;
            if(type.Kind=="e"&&type.Send=="enum_send")return new("enum_send",cast,false);
            if(type.Element!=0&&type.Send=="array_send")
            {
                var element=Resolve(type.Element,seen);
                return element!=null?new("array_send",cast,false,type.Element){Element=element}:null;
            }
            if(type.Kind=="c"&&type.Send=="record_send")
            {
                var fields=type.Fields.Select(oid=>(Oid:oid,Codec:Resolve(oid,new(seen)))).ToArray();
                return fields.All(f=>f.Codec!=null)?new("record_send",cast,false){Fields=fields.Select(f=>(f.Oid,f.Codec!)).ToArray()}:null;
            }
            if(type.Kind is "r" or "m"&&type.Send==(type.Kind=="r"?"range_send":"multirange_send"))
            {
                var child=Resolve(type.Kind=="r"?type.Range:type.MultiRange,new(seen));
                return child!=null?new(type.Send,cast,false){Range=child,MultiRange=type.Kind=="m"}:null;
            }
            if(type.Schema!="pg_catalog")return null;
            var send=BinaryCodecs.GetValueOrDefault(type.Name)??type.Name switch
            {"int2"=>"int2send","int4"=>"int4send","int8"=>"int8send","text" or "varchar" or "bpchar"=>"textsend","bool"=>"boolsend","uuid"=>"uuid_send","numeric"=>"numeric_send","bytea"=>"byteasend",_=>null};
            // varchar/bpchar have their own send aliases, but the explicitly selected textsend
            // must not cast through text: bpchar padding is part of the stored observation.
            if(send==null)return null;
            return new(send,cast,type.Name=="numeric");
        }
        bool ContainsDomain(uint id,HashSet<uint> seen)=>seen.Add(id)&&types.TryGetValue(id,out var t)&&(t.Kind=="d"||t.Element!=0&&ContainsDomain(t.Element,seen)||t.Fields.Any(f=>ContainsDomain(f,new(seen)))||t.Range!=0&&ContainsDomain(t.Range,seen)||t.MultiRange!=0&&ContainsDomain(t.MultiRange,seen));
        foreach(var root in roots){if(ContainsDomain(root,[]))domainValueTypes.Add(root);var codec=Resolve(root,[]);if(codec!=null)customValueCodecs[root]=codec;}
        return tables.Select(t=>t with{Columns=t.Columns.Select(c=>customValueCodecs.ContainsKey(c.TypeOid)?c with{SafeValue=true}:ArrayCodecs.Contains(c.Type)?c with{SafeValue=false}:c).ToArray()}).ToList();
    }
    private static string CustomProjection(Column column,ValueCodec codec)
    {
        var value="("+Quote(column.Name)+")::"+codec.CastType;
        // Coercing bpchar to text would discard its right padding. Its own send function is used.
        var send=codec.CastType=="\"pg_catalog\".\"bpchar\""?"bpcharsend":codec.CastType=="\"pg_catalog\".\"varchar\""?"varcharsend":codec.Send;
        return "pg_catalog.encode(pg_catalog."+send+"("+value+"),'hex')";
    }
}
