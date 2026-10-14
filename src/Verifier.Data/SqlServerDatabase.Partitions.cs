using System.Globalization;
using System.Security.Cryptography;
using System.Text.Json;
using Verifier.Core;
namespace Verifier.Data;

public sealed partial class SqlServerDatabase
{
    internal static string PartitionCast(string type,int length,int precision,int scale)
    {
        if(!SqlServerRowCodec.Supported(type)||type is "text" or "ntext" or "image" or "timestamp" or "rowversion" or "xml" or "sql_variant" or "geometry" or "geography" or "hierarchyid")throw new VerificationException(Reason.UnsupportedType);
        if(type is "char" or "varchar" or "binary" or "varbinary" or "nchar" or "nvarchar")
        {
            if(length<1||length>8000||type is "nchar" or "nvarchar"&&length%2!=0)throw new VerificationException(Reason.ObservationChanged);
            return type+"("+(type is "nchar" or "nvarchar"?length/2:length).ToString(CultureInfo.InvariantCulture)+")";
        }
        if(type is "decimal" or "numeric")
        {if(precision is <1 or >38||scale<0||scale>precision)throw new VerificationException(Reason.ObservationChanged);return type+"("+precision.ToString(CultureInfo.InvariantCulture)+","+scale.ToString(CultureInfo.InvariantCulture)+")";}
        if(type is "time" or "datetime2" or "datetimeoffset")
        {if(scale is <0 or >7)throw new VerificationException(Reason.ObservationChanged);return type+"("+scale.ToString(CultureInfo.InvariantCulture)+")";}
        if(type=="float")
        {if(precision is <25 or >53)throw new VerificationException(Reason.ObservationChanged);return "float("+precision.ToString(CultureInfo.InvariantCulture)+")";}
        return type;
    }
    private async Task<(IReadOnlyList<CatalogFact> Facts,IReadOnlyList<CatalogGap> Gaps,bool Complete)> ReadPartitionBounds(Table table,CancellationToken token)
    {
        var facts=new List<CatalogFact>();var gaps=new List<CatalogGap>();var boundsComplete=true;
        var functions=await Native("""
            SELECT CONVERT(varbinary(max),CONVERT(nvarchar(max),i.name)) AS [__dbmv_utf16],CONVERT(varbinary(max),CONVERT(nvarchar(max),ps.name)) AS [__dbmv_utf16],CONVERT(varbinary(max),CONVERT(nvarchar(max),pf.name)) AS [__dbmv_utf16],pf.function_id,pf.boundary_value_on_right,pp.parameter_id,
              CONVERT(varbinary(max),CONVERT(nvarchar(max),bt.name)) AS [__dbmv_utf16],pp.max_length,pp.precision,pp.scale,CONVERT(varbinary(max),CONVERT(nvarchar(max),pp.collation_name)) AS [__dbmv_utf16]
            FROM sys.indexes i JOIN sys.partition_schemes ps ON ps.data_space_id=i.data_space_id
            JOIN sys.partition_functions pf ON pf.function_id=ps.function_id
            JOIN sys.partition_parameters pp ON pp.function_id=pf.function_id
            JOIN sys.types bt ON bt.user_type_id=pp.system_type_id AND bt.user_type_id=bt.system_type_id
            WHERE i.object_id=object_id(QUOTENAME(CONVERT(nvarchar(128),@schema))+'.'+QUOTENAME(CONVERT(nvarchar(128),@table)),'U')
            ORDER BY i.name COLLATE Latin1_General_100_BIN2,pp.parameter_id
            """,table,token);
        foreach(var f in functions)
        {
            var key=SqlServerCatalog.Key("partition-function",f[0],f[1],f[2],f[5]);
            facts.Add(new(key,null,"PartitionKey",SqlServerMetadata.Serialize(new[]{f[4],f[6],f[7],f[8],f[9],f[10]})));
            string cast;
            try{cast=PartitionCast(f[6]!,int.Parse(f[7]!,CultureInfo.InvariantCulture),int.Parse(f[8]!,CultureInfo.InvariantCulture),int.Parse(f[9]!,CultureInfo.InvariantCulture));}
            catch(VerificationException e){gaps.Add(new(key,null,"PartitionBound","",e.Reason));boundsComplete=false;continue;}
            var binary=SqlServerRowCodec.Bytes(f[6]!);
            var projected=binary?"CONVERT(varbinary(max),v.value)":"CONVERT("+cast+",v.value)";
            await using var q=Command("SELECT v.boundary_id,v.parameter_id,CONVERT(varchar(128),SQL_VARIANT_PROPERTY(v.value,'BaseType')),"+projected+" FROM sys.partition_range_values v WHERE v.function_id=@function AND v.parameter_id=@parameter ORDER BY v.boundary_id,v.parameter_id");
            q.Parameters.AddWithValue("@function",int.Parse(f[3]!,CultureInfo.InvariantCulture));q.Parameters.AddWithValue("@parameter",int.Parse(f[5]!,CultureInfo.InvariantCulture));
            await using var reader=await q.ExecuteReaderAsync(System.Data.CommandBehavior.SequentialAccess,token);
            while(await reader.ReadAsync(token))
            {
                var boundaryKey=SqlServerCatalog.Key("partition-boundary",f[0],f[1],f[2],reader.GetInt32(0).ToString(CultureInfo.InvariantCulture),reader.GetInt32(1).ToString(CultureInfo.InvariantCulture));
                if(reader.IsDBNull(2))
                {if(!reader.IsDBNull(3))throw new VerificationException(Reason.ObservationChanged);facts.Add(new(boundaryKey,null,"PartitionBound",null));continue;}
                var actual=reader.GetString(2);
                if(actual!=f[6]&&!(actual is "decimal" or "numeric"&&f[6] is "decimal" or "numeric"))throw new VerificationException(Reason.ObservationChanged);
                byte[] bytes;
                if(binary)
                {
                    // Partition parameters cannot be MAX/LOB. Keep raw text code
                    // units from the native sql_variant and enforce the 8000-byte limit.
                    using var stream=reader.GetStream(3);var buffer=new byte[8001];var n=await stream.ReadAtLeastAsync(buffer,buffer.Length,false,token);
                    try{if(n>8000)throw new VerificationException(Reason.ObservationChanged);bytes=buffer[..n];}finally{CryptographicOperations.ZeroMemory(buffer);}
                }
                else bytes=SqlServerRowCodec.Scalar(reader,3,f[6]!);
                try{facts.Add(new(boundaryKey,null,"PartitionBound",Convert.ToHexString(bytes)));}finally{CryptographicOperations.ZeroMemory(bytes);}
            }
        }
        var destinations=await Native("""
            SELECT CONVERT(varbinary(max),CONVERT(nvarchar(max),i.name)) AS [__dbmv_utf16],CONVERT(varbinary(max),CONVERT(nvarchar(max),ps.name)) AS [__dbmv_utf16],d.destination_id,CONVERT(varbinary(max),CONVERT(nvarchar(max),fg.name)) AS [__dbmv_utf16],fg.type,fg.is_default,fg.is_read_only,fg.is_autogrow_all_files
            FROM sys.indexes i JOIN sys.partition_schemes ps ON ps.data_space_id=i.data_space_id
            JOIN sys.destination_data_spaces d ON d.partition_scheme_id=ps.data_space_id
            LEFT JOIN sys.filegroups fg ON fg.data_space_id=d.data_space_id
            WHERE i.object_id=object_id(QUOTENAME(CONVERT(nvarchar(128),@schema))+'.'+QUOTENAME(CONVERT(nvarchar(128),@table)),'U')
            ORDER BY i.name COLLATE Latin1_General_100_BIN2,d.destination_id
            """,table,token);
        foreach(var row in destinations)
        {
            var key=SqlServerCatalog.Key("partition-destination",row[0],row[1],row[2]);
            if(row[3]==null)gaps.Add(new(key,null,"PartitionParents","",Reason.VisibilityUnknown));
            else facts.Add(new(key,null,"PartitionParents",SqlServerMetadata.Serialize(row[3..])));
        }
        return(facts,gaps,boundsComplete);
    }
}
