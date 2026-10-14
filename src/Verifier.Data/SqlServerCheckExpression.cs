using System.Text.RegularExpressions;
using Verifier.Core;

namespace Verifier.Data;

// A bounded native CHECK/filter grammar with a closed set of pure built-ins. Native
// SQL bodies are never accepted by a substring denylist or executed as modules.
internal static partial class SqlServerCheckExpression
{
    [GeneratedRegex("\\G(?:\\s+|N?'(?:''|[^'])*'|\\[(?:\\]\\]|[^\\]])+\\]|(?:[0-9]+(?:\\.[0-9]*)?|\\.[0-9]+)(?:[eE][+-]?[0-9]+)?|[A-Za-z_][A-Za-z_0-9$#]*|<=|>=|<>|!=|!<|!>|[-+*/%=<>() ,])",RegexOptions.CultureInvariant)]
    private static partial Regex Tokens();
    private static readonly HashSet<string> Keywords=new(StringComparer.OrdinalIgnoreCase){"AND","OR","NOT","IS","NULL","IN","BETWEEN","LIKE","ESCAPE","CASE","WHEN","THEN","ELSE","END"};
    private static readonly HashSet<string> Builtins=new(StringComparer.OrdinalIgnoreCase){
        "ABS","CEILING","FLOOR","SIGN","ROUND","POWER","SQRT",
        "LEN","DATALENGTH","LOWER","UPPER","LTRIM","RTRIM","TRIM","REPLACE","REVERSE",
        "LEFT","RIGHT","SUBSTRING","CHARINDEX","PATINDEX","ASCII","UNICODE","CHAR","NCHAR",
        "ISNULL","COALESCE","NULLIF","CONCAT","CONCAT_WS"};
    internal static string? Bind(string? expression,IReadOnlyList<Column> columns)
    {
        if(string.IsNullOrWhiteSpace(expression))return null;
        var output=new List<string>();var position=0;var depth=0;var identifier=false;
        while(position<expression.Length)
        {
            if(expression.AsSpan(position).StartsWith("--")||expression.AsSpan(position).StartsWith("/*"))return null;
            var match=Tokens().Match(expression,position);if(!match.Success||match.Index!=position||match.Length==0)return null;
            position+=match.Length;var token=match.Value;if(string.IsNullOrWhiteSpace(token))continue;
            if(token=="("){if(identifier)return null;depth++;}else if(token==")"&&--depth<0)return null;
            identifier=false;
            if(token[0]=='['||char.IsLetter(token[0])||token[0]=='_')
            {
                if(token.StartsWith("N'",StringComparison.Ordinal)){output.Add(token);continue;}
                if(token[0]!='['&&Keywords.Contains(token)){output.Add(token.ToUpperInvariant());continue;}
                // Only unqualified native built-ins are callable. Bracketed,
                // schema-qualified and unknown functions never reach SQL execution.
                if(token[0]!='['&&Builtins.Contains(token)&&expression.AsSpan(position).TrimStart().StartsWith("("))
                {output.Add(token.ToUpperInvariant());continue;}
                var name=token[0]=='['?token[1..^1].Replace("]]","]",StringComparison.Ordinal):token;
                // Resolve exact catalog spelling; never guess a case-folded or
                // ambiguous identifier under a different database collation.
                if(!columns.Any(c=>c.Name==name&&c.SafeValue))return null;
                output.Add("t."+SqlServerDatabase.Quote(name));identifier=true;
            }
            else output.Add(token);
        }
        return depth==0?string.Join(' ',output):null;
    }
}
