using System.Data.Common;
using System.Data.SqlTypes;
using System.Numerics;
using System.Security.Cryptography;
using Microsoft.Data.SqlClient;
using Verifier.Core;

namespace Verifier.Data;

internal static class SqlServerRowCodec
{
    private static readonly HashSet<string> Types=new(StringComparer.Ordinal)
    {"bit","tinyint","smallint","int","bigint","decimal","numeric","money","smallmoney","real","float",
     "date","datetime","smalldatetime","datetime2","datetimeoffset","time","uniqueidentifier",
     "binary","varbinary","image","timestamp","rowversion","char","varchar","text","nchar","nvarchar","ntext","xml","sql_variant","geometry","geography","hierarchyid"};
    internal static bool Supported(string type)=>Types.Contains(type);
    // XML uses SQL Server's stored-value UTF-16 serialization (with BOM),
    // streamed as binary. Never parse/reformat it or use a collation code page.
    internal static bool Bytes(string type)=>type is "binary" or "varbinary" or "image" or "timestamp" or "rowversion" or "char" or "varchar" or "text" or "nchar" or "nvarchar" or "ntext" or "xml" or "geometry" or "geography" or "hierarchyid";
    internal static byte[] Scalar(DbDataReader reader,int index,string type)
    {
        using var output=new MemoryStream();using var w=new BinaryWriter(output);
        switch(type)
        {
            case "bit":w.Write(reader.GetBoolean(index));break;
            case "tinyint":w.Write(reader.GetByte(index));break;
            case "smallint":w.Write(reader.GetInt16(index));break;
            case "int":w.Write(reader.GetInt32(index));break;
            case "bigint":w.Write(reader.GetInt64(index));break;
            case "real":w.Write(BitConverter.SingleToInt32Bits(reader.GetFloat(index)));break;
            case "float":w.Write(BitConverter.DoubleToInt64Bits(reader.GetDouble(index)));break;
            case "decimal":case "numeric":
                var number=reader is SqlDataReader sql?sql.GetSqlDecimal(index):(SqlDecimal)reader.GetProviderSpecificValue(index);
                w.Write(Decimal(number));break;
            case "money":case "smallmoney":w.Write(Decimal(new SqlDecimal(reader.GetDecimal(index))));break;
            case "date":case "datetime":case "smalldatetime":case "datetime2":w.Write(reader.GetDateTime(index).Ticks);break;
            case "time":w.Write(reader.GetFieldValue<TimeSpan>(index).Ticks);break;
            case "datetimeoffset":var time=reader.GetFieldValue<DateTimeOffset>(index);w.Write(time.Ticks);w.Write(time.Offset.Ticks);break;
            case "uniqueidentifier":w.Write(reader.GetGuid(index).ToByteArray());break;
            default:throw new VerificationException(Reason.UnsupportedType);
        }
        return output.ToArray();
    }
    internal static string Decimal(SqlDecimal number)
    {
        if(number.IsNull)throw new VerificationException(Reason.UnsupportedType);
        var value=BigInteger.Zero;var words=number.Data;
        for(var i=words.Length-1;i>=0;i--)value=(value<<32)+(uint)words[i];
        var scale=(int)number.Scale;if(value.IsZero)return "0:0";
        while(scale>0&&value%10==0){value/=10;scale--;}
        return (number.IsPositive?"":"-")+value.ToString(System.Globalization.CultureInfo.InvariantCulture)+":"+scale.ToString(System.Globalization.CultureInfo.InvariantCulture);
    }
    internal static async Task WriteAsync(DbDataReader reader,IReadOnlyList<Column> columns,Stream output,CancellationToken token,byte[]? sharedBuffer=null)
    {
        if(sharedBuffer!=null&&sharedBuffer.Length!=65536)throw new VerificationException(Reason.InvalidInput);
        if(reader.FieldCount!=columns.Sum(c=>c.Type=="sql_variant"?SqlServerVariantCodec.Fields:1))throw new VerificationException(Reason.ObservationChanged);
        using var header=new BinaryWriter(output,System.Text.Encoding.UTF8,true);header.Write(3);header.Write(columns.Count);
        var buffer=sharedBuffer??new byte[65536];
        try
        {
            var index=0;
            for(var i=0;i<columns.Count;i++)
            {
                token.ThrowIfCancellationRequested();var c=columns[i];if(!c.SafeValue||!Supported(c.Type))throw new VerificationException(Reason.UnsupportedType);
                if(c.Type=="sql_variant")
                {
                    var value=await SqlServerVariantCodec.ReadAsync(reader,index,token);index+=SqlServerVariantCodec.Fields;header.Write(value!=null);if(value==null)continue;
                    try{header.Write(value.Length);header.Write(value);header.Write(0);}finally{CryptographicOperations.ZeroMemory(value);}continue;
                }
                var field=index++;var present=!await reader.IsDBNullAsync(field,token);header.Write(present);if(!present)continue;
                if(Bytes(c.Type))
                {
                    using var stream=reader.GetStream(field);
                    int n;while((n=await stream.ReadAtLeastAsync(buffer,buffer.Length,false,token))>0){header.Write(n);await output.WriteAsync(buffer.AsMemory(0,n),token);}
                }
                else{var bytes=Scalar(reader,field,c.Type);header.Write(bytes.Length);header.Write(bytes);CryptographicOperations.ZeroMemory(bytes);}
                header.Write(0);
            }
        }
        catch(InvalidCastException){throw new VerificationException(Reason.ObservationChanged);}
        finally{CryptographicOperations.ZeroMemory(buffer);}
    }
}
