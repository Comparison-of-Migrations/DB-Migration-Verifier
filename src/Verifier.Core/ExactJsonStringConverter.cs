using System.Buffers;
using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

namespace Verifier.Core;

// SQL metadata and saved result locations must retain the same UTF-16 units.
internal sealed class ExactJsonStringConverter:JsonConverter<string>
{
    private static readonly UTF8Encoding Utf8=new(false,true);
    internal static bool HasUnpairedSurrogate(string value)
    {
        for(var i=0;i<value.Length;i++)
        {
            if(!char.IsSurrogate(value[i]))continue;
            if(char.IsHighSurrogate(value[i])&&i+1<value.Length&&char.IsLowSurrogate(value[i+1])){i++;continue;}
            return true;
        }
        return false;
    }
    public override string Read(ref Utf8JsonReader reader,Type type,JsonSerializerOptions options)
    {
        if(reader.TokenType!=JsonTokenType.String)throw new JsonException();
        try
        {
            var raw=reader.HasValueSequence?Utf8.GetString(reader.ValueSequence.ToArray()):Utf8.GetString(reader.ValueSpan);
            return reader.ValueIsEscaped?Regex.Unescape(raw):raw;
        }
        catch(DecoderFallbackException){throw new JsonException();}
    }
    public override void Write(Utf8JsonWriter writer,string value,JsonSerializerOptions options)
    {
        if(!HasUnpairedSurrogate(value)){writer.WriteStringValue(value);return;}
        var escaped=new StringBuilder(checked(value.Length*6+2));escaped.Append('"');
        foreach(var c in value)escaped.Append("\\u").Append(((int)c).ToString("X4",CultureInfo.InvariantCulture));
        escaped.Append('"');writer.WriteRawValue(escaped.ToString(),skipInputValidation:true);
    }
}
