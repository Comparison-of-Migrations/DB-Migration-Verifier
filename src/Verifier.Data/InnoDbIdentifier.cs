using System.Globalization;
using System.Text;
namespace Verifier.Data;

// Native filename codec verified independently against both pinned DBMS servers.
internal static partial class InnoDbIdentifier
{
    internal static string? Encode(string name)
    {
        var b=new StringBuilder();
        foreach(var c in name)
        {
            if(char.IsAsciiLetterOrDigit(c)||c=='_'){b.Append(c);continue;}
            if(char.IsSurrogate(c))return null;
            var code=StorageLetterCode(c);
            if(code!=0){b.Append('@').Append((char)(code/80+0x30)).Append((char)(code%80+0x30));continue;}
            b.Append('@').Append(((int)c).ToString("x4",CultureInfo.InvariantCulture));
        }
        if(name.Equals("CON",StringComparison.OrdinalIgnoreCase)||name.Equals("PRN",StringComparison.OrdinalIgnoreCase)||name.Equals("AUX",StringComparison.OrdinalIgnoreCase)||name.Equals("NUL",StringComparison.OrdinalIgnoreCase)||name.Length==4&&(name.StartsWith("COM",StringComparison.OrdinalIgnoreCase)||name.StartsWith("LPT",StringComparison.OrdinalIgnoreCase))&&name[3] is >= '1' and <= '9')b.Append("@@@");
        return b.ToString();
    }
}
