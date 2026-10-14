using System.Globalization;
using System.Numerics;
using System.Net;
using System.Net.Sockets;
using System.Text.Json;
using System.Buffers.Binary;
using System.Text;
using System.Text.RegularExpressions;
using MySqlConnector;
using Verifier.Core;
namespace Verifier.Data;

// Values are bound parameters. Native column declarations never become SQL casts.
// This codec is for confirmed synthetic write inputs, separate from migration comparison.
internal static class MySqlTrialValue
{
    internal static string Base(Column column)=>column.Type.Split(['(',' '],2)[0].ToLowerInvariant();
    internal static bool Supports(Column c)=>MySqlTrialGeometry.Is(c)||Base(c) is "tinyint" or "smallint" or "mediumint" or "int" or "bigint" or "decimal" or "float" or "double" or "year" or "char" or "varchar" or "tinytext" or "text" or "mediumtext" or "longtext" or "binary" or "varbinary" or "tinyblob" or "blob" or "mediumblob" or "longblob" or "enum" or "set" or "date" or "datetime" or "timestamp" or "time" or "json" or "bit" or "uuid" or "inet4" or "inet6";
    internal static bool Binary(Column c)=>Base(c) is "binary" or "varbinary" or "tinyblob" or "blob" or "mediumblob" or "longblob";
    internal static bool Numeric(Column c)=>Base(c) is "tinyint" or "smallint" or "mediumint" or "int" or "bigint" or "decimal" or "year";
    internal static string Quote(string name)=>"`"+name.Replace("`","``",StringComparison.Ordinal)+"`";
    internal static MySqlParameter Parameter(string name,Column column,string? input)
    {
        if(!Supports(column))throw new VerificationException(Reason.UnsupportedType);
        if(input==null)return new(name,MySqlDbType.VarChar){Value=DBNull.Value};
        var kind=Base(column);object value=input;var type=MySqlDbType.VarChar;
        try
        {
            if(MySqlTrialGeometry.Is(column)){var wkb=MySqlTrialGeometry.Parse(input).Wkb;MySqlTrialGeometry.ValidateRoot(column,wkb);value=wkb;type=MySqlDbType.Blob;}
            else if(kind=="json")_ = CanonicalJson(input);
            else if(kind=="uuid"){value=Guid.ParseExact(input,"D").ToString("D");}
            else if(kind is "inet4" or "inet6"){value=Address(column,input).ToString();}
            else if(kind=="bit")
            {
                var bits=BitWidth(column);if(!input.StartsWith("0b",StringComparison.Ordinal)||input.Length<3||input.Length-2>bits||input[2..].Any(c=>c is not('0' or '1')))throw new FormatException();
                value=Convert.ToUInt64(input[2..],2);type=MySqlDbType.UInt64;
            }
            else if(kind is "tinyint" or "smallint" or "mediumint" or "int" or "bigint" or "year")
            {
                if(!Regex.IsMatch(input,@"^[+-]?[0-9]+$",RegexOptions.CultureInvariant))throw new FormatException();
                var n=BigInteger.Parse(input,CultureInfo.InvariantCulture);var bits=kind switch{"tinyint"=>8,"smallint"=>16,"mediumint"=>24,"int"=>32,"year"=>16,_=>64};var unsigned=column.Type.Contains("unsigned",StringComparison.OrdinalIgnoreCase)||kind=="year";
                if(n<(unsigned?BigInteger.Zero:-(BigInteger.One<<(bits-1)))||n>(unsigned?(BigInteger.One<<bits)-1:(BigInteger.One<<(bits-1))-1)||kind=="year"&&n!=0&&(n<1901||n>2155))throw new FormatException();
                value=n.ToString(CultureInfo.InvariantCulture);
            }
            else if(kind=="decimal")
            {
                if(!Regex.IsMatch(input,@"^[+-]?[0-9]+(?:\.[0-9]+)?$",RegexOptions.CultureInvariant))throw new FormatException();
                _=ExactRow.CanonicalNumber(input); // Native strict assignment + exact readback rejects rounding.
            }
            else if(kind=="float"){var n=float.Parse(input,NumberStyles.Float,CultureInfo.InvariantCulture);if(!float.IsFinite(n))throw new FormatException();type=MySqlDbType.Float;value=n;}
            else if(kind=="double"){var n=double.Parse(input,NumberStyles.Float,CultureInfo.InvariantCulture);if(!double.IsFinite(n))throw new FormatException();type=MySqlDbType.Double;value=n;}
            else if(Binary(column)){if(!input.StartsWith("0x",StringComparison.Ordinal)||input.Length%2!=0)throw new FormatException();value=Convert.FromHexString(input[2..]);type=MySqlDbType.Blob;}
            else if(kind is "date" or "datetime" or "timestamp")
            {
                var patterns=kind=="date"?new[]{"yyyy-MM-dd"}:new[]{"yyyy-MM-dd HH:mm:ss","yyyy-MM-dd HH:mm:ss.FFFFFF","yyyy-MM-dd'T'HH:mm:ss","yyyy-MM-dd'T'HH:mm:ss.FFFFFF"};
                value=DateTime.SpecifyKind(DateTime.ParseExact(input,patterns,CultureInfo.InvariantCulture,DateTimeStyles.None),DateTimeKind.Unspecified);type=kind=="date"?MySqlDbType.Date:MySqlDbType.DateTime;
            }
            else if(kind=="time")
            {
                var m=Regex.Match(input,@"^(?<s>-?)(?<h>[0-9]{1,3}):(?<m>[0-9]{2}):(?<v>[0-9]{2})(?:\.(?<f>[0-9]{1,6}))?$",RegexOptions.CultureInvariant);
                if(!m.Success)throw new FormatException();int h=int.Parse(m.Groups["h"].Value),minutes=int.Parse(m.Groups["m"].Value),seconds=int.Parse(m.Groups["v"].Value);
                if(h>838||minutes>59||seconds>59)throw new FormatException();var ticks=((h*3600L+minutes*60+seconds)*1000000+int.Parse((m.Groups["f"].Value+"000000")[..6],CultureInfo.InvariantCulture))*10;
                value=TimeSpan.FromTicks(m.Groups["s"].Value=="-"?-ticks:ticks);type=MySqlDbType.Time;
            }
            return new(name,type){Value=value};
        }
        catch(Exception e)when(e is FormatException or OverflowException or ArgumentException or JsonException){throw new VerificationException(Reason.InvalidInput);}
    }
    internal static string ReadExpression(Column column)
    {
        var identifier=Quote(column.Name);var kind=Base(column);
        if(MySqlTrialGeometry.Is(column))return "CONCAT(UNHEX(LPAD(HEX(ST_SRID("+identifier+")),8,'0')),ST_AsWKB("+identifier+"))";
        if(kind=="bit")return "UNHEX(LPAD(HEX("+identifier+"),"+(((BitWidth(column)+7)/8)*2).ToString(CultureInfo.InvariantCulture)+",'0'))";
        if(kind is "inet4" or "inet6")return "INET6_ATON(CAST("+identifier+" AS CHAR))";
        if(kind=="uuid")return "UNHEX(REPLACE(CAST("+identifier+" AS CHAR),'-',''))";
        if(kind is "float" or "double" or "date" or "datetime" or "timestamp" or "time")return identifier;
        return "CAST("+(Binary(column)||Numeric(column)?identifier:"CONVERT("+identifier+" USING utf8mb4)")+" AS BINARY)";
    }
    internal static bool Matches(MySqlDataReader reader,int ordinal,Column column,string? input)
    {
        if(input==null)return reader.IsDBNull(ordinal);if(reader.IsDBNull(ordinal))return false;var expected=Parameter("@value",column,input).Value??throw new VerificationException(Reason.InvalidInput);var kind=Base(column);
        if(kind=="float")return BitConverter.SingleToInt32Bits(reader.GetFloat(ordinal))==BitConverter.SingleToInt32Bits((float)expected);
        if(kind=="double")return BitConverter.DoubleToInt64Bits(reader.GetDouble(ordinal))==BitConverter.DoubleToInt64Bits((double)expected);
        if(kind is "date" or "datetime" or "timestamp")return reader.GetDateTime(ordinal).Ticks==((DateTime)expected).Ticks;
        if(kind=="time")return reader.GetTimeSpan(ordinal).Ticks==((TimeSpan)expected).Ticks;
        var actual=(byte[])reader.GetValue(ordinal);
        if(MySqlTrialGeometry.Is(column)){var geometry=MySqlTrialGeometry.Parse(input);return actual.Length>=4&&BinaryPrimitives.ReadUInt32BigEndian(actual)==geometry.Srid&&actual.AsSpan(4).SequenceEqual(geometry.Wkb);}
        if(Binary(column))return actual.AsSpan().SequenceEqual((byte[])expected);
        if(kind=="bit"){Span<byte> bytes=stackalloc byte[8];BinaryPrimitives.WriteUInt64BigEndian(bytes,(ulong)expected);return actual.AsSpan().SequenceEqual(bytes[(8-(BitWidth(column)+7)/8)..]);}
        if(kind=="uuid")return actual.AsSpan().SequenceEqual(Guid.Parse((string)expected).ToByteArray(bigEndian:true));
        if(kind is "inet4" or "inet6")return actual.AsSpan().SequenceEqual(Address(column,input).GetAddressBytes());
        var text=Encoding.UTF8.GetString(actual);if(kind=="json")return CanonicalJson(text)==CanonicalJson(input);return Numeric(column)?ExactRow.CanonicalNumber(text)==ExactRow.CanonicalNumber((string)expected):text==(string)expected;
    }
    private static int BitWidth(Column column)
    {var match=Regex.Match(column.Type,@"^bit\(([0-9]+)\)$",RegexOptions.IgnoreCase|RegexOptions.CultureInvariant);if(!match.Success||!int.TryParse(match.Groups[1].Value,out var n)||n is <1 or >64)throw new VerificationException(Reason.UnsupportedType);return n;}
    private static IPAddress Address(Column column,string input)
    {
        if(input.Contains('%')||!IPAddress.TryParse(input,out var ip))throw new VerificationException(Reason.InvalidInput);
        if(Base(column)=="inet4")
        {if(ip.AddressFamily!=AddressFamily.InterNetwork||!Regex.IsMatch(input,@"^(?:0|[1-9][0-9]{0,2})(?:\.(?:0|[1-9][0-9]{0,2})){3}$",RegexOptions.CultureInvariant))throw new VerificationException(Reason.InvalidInput);}
        else if(ip.AddressFamily==AddressFamily.InterNetwork)ip=ip.MapToIPv6();
        return ip;
    }
    internal static string CanonicalJson(string input)
    {
        using var document=JsonDocument.Parse(input,new JsonDocumentOptions{MaxDepth=1024});return Canonical(document.RootElement);
        static string Canonical(JsonElement value)=>value.ValueKind switch
        {
            JsonValueKind.Object=>Object(value),JsonValueKind.Array=>"["+string.Join(',',value.EnumerateArray().Select(Canonical))+"]",
            JsonValueKind.String=>JsonSerializer.Serialize(value.GetString()),JsonValueKind.Number=>Number(value.GetRawText()),
            JsonValueKind.True=>"true",JsonValueKind.False=>"false",JsonValueKind.Null=>"null",_=>throw new VerificationException(Reason.InvalidInput)
        };
        static string Object(JsonElement value)
        {
            var properties=value.EnumerateObject().ToArray();if(properties.Select(p=>p.Name).Distinct(StringComparer.Ordinal).Count()!=properties.Length)throw new VerificationException(Reason.InvalidInput);
            return "{"+string.Join(',',properties.OrderBy(p=>p.Name,StringComparer.Ordinal).Select(p=>JsonSerializer.Serialize(p.Name)+":"+Canonical(p.Value)))+"}";
        }
        static string Number(string text)
        {
            var parts=text.Split(['e','E'],2);var coefficient=parts[0];var exponent=parts.Length==2?BigInteger.Parse(parts[1],CultureInfo.InvariantCulture):BigInteger.Zero;
            var negative=coefficient.StartsWith('-');if(negative)coefficient=coefficient[1..];var point=coefficient.IndexOf('.');if(point>=0)exponent-=coefficient.Length-point-1;
            var digits=coefficient.Replace(".","",StringComparison.Ordinal).TrimStart('0');if(digits.Length==0)return "0e0";
            var trimmed=digits.TrimEnd('0');exponent+=digits.Length-trimmed.Length;
            return (negative?"-":"")+trimmed+"e"+exponent.ToString(CultureInfo.InvariantCulture);
        }
    }

}
