using System.Data.Common;
using System.Security.Cryptography;
using Verifier.Core;

namespace Verifier.Data;

internal static class SqlServerVariantCodec
{
    internal const int Fields=6;
    internal static string Projection(string quoted)
        =>string.Join(',',new[]{"BaseType","Precision","Scale","MaxLength","Collation"}.Select(p=>
            "CONVERT("+(p is "BaseType" or "Collation"?"nvarchar(128)":"int")+",SQL_VARIANT_PROPERTY("+quoted+",'"+p+"'))"))+
            ",CONVERT(varbinary(max),"+quoted+")";

    internal static async Task<byte[]?> ReadAsync(DbDataReader reader,int index,CancellationToken token)
    {
        if(await reader.IsDBNullAsync(index,token))
        {
            for(var i=1;i<Fields;i++)if(!await reader.IsDBNullAsync(index+i,token))throw new VerificationException(Reason.ObservationChanged);
            return null;
        }
        var type=reader.GetString(index);
        if(!SqlServerRowCodec.Supported(type)||type is "xml" or "sql_variant" or "text" or "ntext" or "image" or "timestamp" or "rowversion" or "geometry" or "geography" or "hierarchyid")throw new VerificationException(Reason.ObservationChanged);
        var precision=reader.GetInt32(index+1);var scale=reader.GetInt32(index+2);var length=reader.GetInt32(index+3);
        if(precision is <0 or >53||scale is <0 or >38||length is <0 or >8000)throw new VerificationException(Reason.ObservationChanged);
        var collation=reader.IsDBNull(index+4)?null:reader.GetString(index+4);
        if(collation?.Length>128||(type is "char" or "varchar" or "nchar" or "nvarchar")!=(collation!=null))throw new VerificationException(Reason.ObservationChanged);
        if(await reader.IsDBNullAsync(index+5,token))throw new VerificationException(Reason.ObservationChanged);
        var buffer=new byte[8017];
        try
        {
            using var source=reader.GetStream(index+5);var count=await source.ReadAtLeastAsync(buffer,buffer.Length,false,token);
            if(count>8016)throw new VerificationException(Reason.ObservationChanged);
            using var output=new MemoryStream();using var writer=new BinaryWriter(output);
            writer.Write(type);writer.Write(precision);writer.Write(scale);writer.Write(length);writer.Write(collation!=null);if(collation!=null)writer.Write(collation);
            writer.Write(count);writer.Write(buffer,0,count);return output.ToArray();
        }
        finally{CryptographicOperations.ZeroMemory(buffer);}
    }
}
