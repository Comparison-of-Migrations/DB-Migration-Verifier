using System.Globalization;
using System.Text.Json;
using System.Text.RegularExpressions;
using Verifier.Core;
namespace Verifier.Data;

internal static class SqlServerTrialVariant
{
    private static readonly HashSet<string> Types=new(StringComparer.Ordinal){"bit","tinyint","smallint","int","bigint","decimal","numeric","money","smallmoney","real","float","date","datetime","smalldatetime","datetime2","datetimeoffset","time","uniqueidentifier","binary","varbinary","char","varchar","nchar","nvarchar"};
    internal static (Column Column,string Value) Parse(string input)
    {
        try
        {
            using var document=JsonDocument.Parse(input,new(){MaxDepth=2});var root=document.RootElement;
            if(root.ValueKind!=JsonValueKind.Object)throw new FormatException();
            var fields=root.EnumerateObject().ToArray();
            var allowed=new HashSet<string>(StringComparer.Ordinal){"type","value"};
            var type=root.GetProperty("type").GetString()!;var value=root.GetProperty("value").GetString()!;
            if(type==null||value==null||!Types.Contains(type))throw new FormatException();
            var d=new string?[34];d[0]="8";d[1]="38";d[2]="0";d[5]=d[6]="False";d[13]="0";
            int Number(string name,int fallback,int min,int max)
            {
                allowed.Add(name);var number=root.TryGetProperty(name,out var property)?property.GetInt32():fallback;
                return number>=min&&number<=max?number:throw new FormatException();
            }
            if(type is "decimal" or "numeric")
            {var precision=Number("precision",38,1,38);d[1]=precision.ToString(CultureInfo.InvariantCulture);d[2]=Number("scale",0,0,precision).ToString(CultureInfo.InvariantCulture);}
            if(type is "money" or "smallmoney"){d[1]=type=="money"?"19":"10";d[2]="4";}
            if(type is "datetime2" or "datetimeoffset" or "time")d[2]=Number("scale",7,0,7).ToString(CultureInfo.InvariantCulture);
            if(type is "char" or "varchar" or "nchar" or "nvarchar" or "binary" or "varbinary")
            {
                var unicode=type is "nchar" or "nvarchar";var length=Number("length",Math.Max(1,value.Length),1,unicode?4000:8000);
                d[0]=(length*(unicode?2:1)).ToString(CultureInfo.InvariantCulture);
                if(type is "char" or "varchar" or "nchar" or "nvarchar")
                {
                    allowed.Add("collation");
                    if(root.TryGetProperty("collation",out var collation))
                    {d[3]=collation.GetString();if(d[3]==null||d[3]!.Length>128||!Regex.IsMatch(d[3]!,"^[A-Za-z0-9_]+$",RegexOptions.CultureInvariant))throw new FormatException();}
                }
            }
            if(fields.Select(f=>f.Name).Distinct(StringComparer.Ordinal).Count()!=fields.Length||fields.Any(f=>!allowed.Contains(f.Name)))throw new FormatException();
            return(new("value",type,1,false,JsonSerializer.Serialize(d),true),value);
        }
        catch(Exception e)when(e is JsonException or InvalidOperationException or KeyNotFoundException or FormatException or OverflowException)
        {throw new VerificationException(Reason.InvalidInput);}
    }
}
