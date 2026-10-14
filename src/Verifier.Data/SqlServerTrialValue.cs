using System.Data;
using System.Data.SqlTypes;
using System.Globalization;
using System.Numerics;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.Data.SqlClient;
using Verifier.Core;
namespace Verifier.Data;

internal static class SqlServerTrialValue
{
    private static readonly HashSet<string> Types=new(StringComparer.Ordinal){"bit","tinyint","smallint","int","bigint","decimal","numeric","money","smallmoney","real","float","date","datetime","smalldatetime","datetime2","datetimeoffset","time","uniqueidentifier","binary","varbinary","image","char","varchar","text","nchar","nvarchar","ntext","xml","sql_variant","geometry","geography","hierarchyid"};
    internal static string GeneratedKeyInput(Column column,object value)=>value switch
    {
        SqlDecimal number=>DecimalInput(number),
        byte[] bytes=>"0x"+Convert.ToHexString(bytes),
        bool flag=>flag?"1":"0",
        Guid id=>id.ToString("D"),
        DateTime date=>date.ToString(column.Type=="date"?"yyyy-MM-dd":"yyyy-MM-ddTHH:mm:ss.fffffff",CultureInfo.InvariantCulture),
        DateTimeOffset date=>date.ToString("yyyy-MM-ddTHH:mm:ss.fffffffzzz",CultureInfo.InvariantCulture),
        TimeSpan time=>time.ToString("hh\\:mm\\:ss\\.fffffff",CultureInfo.InvariantCulture),
        double number=>number.ToString("R",CultureInfo.InvariantCulture),
        float number=>number.ToString("R",CultureInfo.InvariantCulture),
        string text=>text,
        byte or short or int or long or decimal=>((IFormattable)value).ToString(null,CultureInfo.InvariantCulture),
        _=>throw new VerificationException(Reason.UnsupportedType)
    };
    private static string DecimalInput(SqlDecimal number)
    {
        var exact=SqlServerRowCodec.Decimal(number).Split(':');var scale=int.Parse(exact[1],CultureInfo.InvariantCulture);
        var coefficient=BigInteger.Parse(exact[0],CultureInfo.InvariantCulture);var digits=BigInteger.Abs(coefficient).ToString(CultureInfo.InvariantCulture).PadLeft(scale+1,'0');
        return (coefficient.Sign<0?"-":"")+(scale==0?digits:digits[..^scale]+"."+digits[^scale..]);
    }
    internal static bool Supports(Column c)=>c.SafeValue&&Types.Contains(c.Type);
    internal static string?[] Metadata(Column c)
    {
        try {var values=SqlServerMetadata.Deserialize<string?[]>(c.Definition??"");if(values==null||values.Length<30)throw new VerificationException(Reason.ObservationChanged);return values;}
        catch(JsonException){throw new VerificationException(Reason.ObservationChanged);}
    }
    internal static bool Generated(Column c){var d=Metadata(c);return d[5]=="True"||d[6]=="True"||d[13]!="0"||c.Type is "timestamp" or "rowversion";}
    internal static bool Default(Column c)=>Metadata(c)[4]!=null;
    internal static string Cast(Column c)
    {
        if(!Supports(c))throw new VerificationException(Reason.UnsupportedType);var d=Metadata(c);
        int Number(int i,int min,int max)=>int.TryParse(d[i],NumberStyles.Integer,CultureInfo.InvariantCulture,out var n)&&n>=min&&n<=max?n:throw new VerificationException(Reason.ObservationChanged);
        return c.Type switch{
            "geometry" or "geography" or "hierarchyid"=>"[sys].["+c.Type+"]",
            "decimal" or "numeric"=>c.Type+"("+Number(1,1,38)+","+Number(2,0,38)+")",
            "datetime2" or "datetimeoffset" or "time"=>c.Type+"("+Number(2,0,7)+")",
            "binary" or "varbinary" or "char" or "varchar" or "nchar" or "nvarchar"=>c.Type+"("+(d[0]=="-1"?"max":(Number(0,1,8000)/(c.Type is "nchar" or "nvarchar"?2:1)).ToString(CultureInfo.InvariantCulture))+")",
            "text"=>"varchar(max)","ntext"=>"nvarchar(max)","image"=>"varbinary(max)",_=>c.Type};
    }
    internal static string Expression(Column c,string parameter,string? confirmedInput=null)
    {
        if(c.Type=="sql_variant"&&confirmedInput!=null)
        {var variant=SqlServerTrialVariant.Parse(confirmedInput);return "CONVERT(sql_variant,"+Expression(variant.Column,parameter)+")";}
        if(c.Type is "geometry" or "geography")return "CASE WHEN "+parameter+" IS NULL THEN CONVERT([sys].["+c.Type+"],NULL) ELSE [sys].["+c.Type+"]::STGeomFromText(SUBSTRING("+parameter+",CHARINDEX(';',"+parameter+")+1,2147483647),CONVERT(int,SUBSTRING("+parameter+",6,CHARINDEX(';',"+parameter+")-6))) END";
        var d=Metadata(c);var collation=d[3];
        if(collation!=null&&!Regex.IsMatch(collation,"^[A-Za-z0-9_]+$",RegexOptions.CultureInvariant))throw new VerificationException(Reason.ObservationChanged);
        return "CONVERT("+Cast(c)+","+parameter+(collation==null?"":" COLLATE "+collation)+")"+(collation==null?"":" COLLATE "+collation);
    }
    internal static string Binding(Column c,string parameter,string? value)=>c.Type is "sql_variant" or "geometry" or "geography" or "hierarchyid"?Expression(c,parameter,value):parameter;
    internal static string Declaration(Column c)
    {
        var collation=Metadata(c)[3];if(collation!=null&&!Regex.IsMatch(collation,"^[A-Za-z0-9_]+$",RegexOptions.CultureInvariant))throw new VerificationException(Reason.ObservationChanged);
        return Cast(c)+(collation==null?"":" COLLATE "+collation);
    }
    internal static SqlParameter Parameter(Column c,string name,string? value,bool rejection=false)
    {
        if(!Supports(c))throw new VerificationException(Reason.UnsupportedType);
        if(value==null){if(!c.Nullable&&!rejection)throw new VerificationException(Reason.InvalidInput);return new(name,SqlDbType.NVarChar,-1){Value=DBNull.Value};}
        // Invalid conversions requested for a rejection case remain bound text,
        // never interpolated SQL. Normal inputs are validated before any write.
        if(c.Type=="sql_variant"){var variant=SqlServerTrialVariant.Parse(value);return Parameter(variant.Column,name,variant.Value,rejection);}
        if(c.Type is "geometry" or "geography")
        {
            var match=Regex.Match(value,@"^SRID=(-?[0-9]+);(.+)$",RegexOptions.CultureInvariant|RegexOptions.Singleline);
            if(!match.Success||!int.TryParse(match.Groups[1].Value,NumberStyles.Integer,CultureInfo.InvariantCulture,out _))throw new VerificationException(Reason.InvalidInput);
            return new(name,SqlDbType.NVarChar,-1){Value=value};
        }
        if(c.Type=="hierarchyid")
        {
            if(!rejection&&!Regex.IsMatch(value,@"^/(?:-?[0-9]+(?:\.-?[0-9]+)*/)*$",RegexOptions.CultureInvariant))throw new VerificationException(Reason.InvalidInput);
            return new(name,SqlDbType.NVarChar,-1){Value=value};
        }
        if(rejection)return new(name,SqlDbType.NVarChar,-1){Value=value};
        var d=Metadata(c);
        try
        {
            object data;SqlDbType type;
            switch(c.Type)
            {
                case "bit":data=value switch{"0" or "false"=>false,"1" or "true"=>true,_=>throw new FormatException()};type=SqlDbType.Bit;break;
                case "tinyint":data=byte.Parse(value,NumberStyles.Integer,CultureInfo.InvariantCulture);type=SqlDbType.TinyInt;break;
                case "smallint":data=short.Parse(value,NumberStyles.Integer,CultureInfo.InvariantCulture);type=SqlDbType.SmallInt;break;
                case "int":data=int.Parse(value,NumberStyles.Integer,CultureInfo.InvariantCulture);type=SqlDbType.Int;break;
                case "bigint":data=long.Parse(value,NumberStyles.Integer,CultureInfo.InvariantCulture);type=SqlDbType.BigInt;break;
                case "decimal":case "numeric":case "money":case "smallmoney":
                    if(!Regex.IsMatch(value,@"^[+-]?(?:[0-9]+(?:\.[0-9]*)?|\.[0-9]+)$",RegexOptions.CultureInvariant))throw new FormatException();
                    var number=SqlDecimal.Parse(value);var exact=SqlServerRowCodec.Decimal(number).Split(':');var scale=int.Parse(exact[1],CultureInfo.InvariantCulture);
                    var precision=int.Parse(d[1]!,CultureInfo.InvariantCulture);var nativeScale=int.Parse(d[2]!,CultureInfo.InvariantCulture);
                    if(scale>nativeScale||BigInteger.Abs(BigInteger.Parse(exact[0],CultureInfo.InvariantCulture)).ToString(CultureInfo.InvariantCulture).Length-scale>precision-nativeScale)throw new FormatException();
                    if(c.Type is "money" or "smallmoney")
                    {var amount=number.Value;if(c.Type=="smallmoney"&&(amount< -214748.3648m||amount>214748.3647m)||amount< -922337203685477.5808m||amount>922337203685477.5807m)throw new FormatException();}
                    data=number;type=SqlDbType.Decimal;break;
                case "float":var dbl=double.Parse(value,NumberStyles.Float,CultureInfo.InvariantCulture);if(!double.IsFinite(dbl))throw new FormatException();data=dbl;type=SqlDbType.Float;break;
                case "real":var single=float.Parse(value,NumberStyles.Float,CultureInfo.InvariantCulture);if(!float.IsFinite(single))throw new FormatException();data=single;type=SqlDbType.Real;break;
                case "uniqueidentifier":data=Guid.ParseExact(value,"D");type=SqlDbType.UniqueIdentifier;break;
                case "date":data=DateTime.ParseExact(value,"yyyy-MM-dd",CultureInfo.InvariantCulture);type=SqlDbType.Date;break;
                case "datetimeoffset":
                    if(!Regex.IsMatch(value,@"^\d{4}-\d{2}-\d{2}T\d{2}:\d{2}:\d{2}(?:\.\d{1,7})?(?:[+-]\d{2}:\d{2}|Z)$",RegexOptions.CultureInvariant))throw new FormatException();
                    var offset=DateTimeOffset.Parse(value,CultureInfo.InvariantCulture,DateTimeStyles.None);CheckTicks(offset.Ticks);data=offset;type=SqlDbType.DateTimeOffset;break;
                case "time":var elapsed=TimeSpan.ParseExact(value,["hh\\:mm\\:ss","hh\\:mm\\:ss\\.FFFFFFF"],CultureInfo.InvariantCulture);if(elapsed<TimeSpan.Zero||elapsed>=TimeSpan.FromDays(1))throw new FormatException();CheckTicks(elapsed.Ticks);data=elapsed;type=SqlDbType.Time;break;
                case "datetime":case "datetime2":case "smalldatetime":
                    if(!Regex.IsMatch(value,@"^\d{4}-\d{2}-\d{2}[T ]\d{2}:\d{2}:\d{2}(?:\.\d{1,7})?$",RegexOptions.CultureInvariant))throw new FormatException();
                    var stamp=DateTime.Parse(value,CultureInfo.InvariantCulture,DateTimeStyles.None);
                    if(c.Type=="datetime"&&new SqlDateTime(stamp).Value!=stamp||c.Type=="smalldatetime"&&(stamp.Year<1900||stamp>new DateTime(2079,6,6,23,59,0)||stamp.Ticks%TimeSpan.TicksPerMinute!=0))throw new FormatException();
                    if(c.Type=="datetime2")CheckTicks(stamp.Ticks);data=stamp;type=c.Type=="datetime"?SqlDbType.DateTime:c.Type=="smalldatetime"?SqlDbType.SmallDateTime:SqlDbType.DateTime2;break;
                case "binary":case "varbinary":case "image":
                    if(!value.StartsWith("0x",StringComparison.Ordinal)||value.Length%2!=0)throw new FormatException();
                    var bytes=Convert.FromHexString(value[2..]);if(d[0]!="-1"&&bytes.Length>int.Parse(d[0]!,CultureInfo.InvariantCulture))throw new FormatException();data=bytes;type=SqlDbType.VarBinary;break;
                case "xml":
                    using(var reader=System.Xml.XmlReader.Create(new StringReader(value),new(){DtdProcessing=System.Xml.DtdProcessing.Prohibit,XmlResolver=null,ConformanceLevel=System.Xml.ConformanceLevel.Fragment}))while(reader.Read()){}
                    data=value;type=SqlDbType.Xml;break;
                default:
                    if(c.Type is "nchar" or "nvarchar"&&d[0]!="-1"&&value.Length*2>int.Parse(d[0]!,CultureInfo.InvariantCulture))throw new FormatException();
                    data=value;type=SqlDbType.NVarChar;break;
            }
            var p=new SqlParameter(name,type){Value=data};if(type is SqlDbType.NVarChar or SqlDbType.VarBinary)p.Size=-1;
            if(type is SqlDbType.DateTime2 or SqlDbType.DateTimeOffset or SqlDbType.Time)p.Scale=7;
            return p;
            void CheckTicks(long ticks){var quantum=(long)Math.Pow(10,7-int.Parse(d[2]!,CultureInfo.InvariantCulture));if(ticks%quantum!=0)throw new FormatException();}
        }
        catch(Exception e)when(e is FormatException or OverflowException or SqlTypeException or System.Xml.XmlException)
        {throw new VerificationException(Reason.InvalidInput);}
    }
}
