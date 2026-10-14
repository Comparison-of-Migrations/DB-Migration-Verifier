using System.Text.Json;
namespace Verifier.Data;

// This is a namespace recognizer for native, server-validated VIEW_DEFINITION,
// not a general SQL parser or semantic optimizer. Preserve every other token,
// literal, comment and whitespace. Ambiguous qualifiers are not compared.
internal static class MySqlViewDefinition
{
    private sealed record Token(string Kind,string Text,string? Identifier=null);
    internal static string? Observe(string sql,string database,IReadOnlySet<string> tables,IReadOnlySet<string> routines,bool noBackslashEscapes,bool ansiQuotes,bool normalizeNamespace=true)
    {
        var tokens=new List<Token>();
        for(var i=0;i<sql.Length;)
        {
            var start=i;var c=sql[i];
            if(char.IsWhiteSpace(c)){while(i<sql.Length&&char.IsWhiteSpace(sql[i]))i++;tokens.Add(new("space",sql[start..i]));continue;}
            if(c=='`'||c=='\''||c=='"')
            {
                var identifier=c=='`'||c=='"'&&ansiQuotes;var decoded=new System.Text.StringBuilder();i++;var closed=false;
                while(i<sql.Length)
                {
                    if(sql[i]==c){if(i+1<sql.Length&&sql[i+1]==c){decoded.Append(c);i+=2;continue;}i++;closed=true;break;}
                    if(!identifier&&sql[i]=='\\'&&!noBackslashEscapes){if(i+1==sql.Length)return null;decoded.Append(sql[i++]);decoded.Append(sql[i++]);continue;}
                    decoded.Append(sql[i++]);
                }
                if(!closed)return null;tokens.Add(new(identifier?"identifier":"literal",sql[start..i],identifier?decoded.ToString():null));continue;
            }
            if(c=='/'&&i+1<sql.Length&&sql[i+1]=='*')
            {var end=sql.IndexOf("*/",i+2,StringComparison.Ordinal);if(end<0)return null;i=end+2;tokens.Add(new("comment",sql[start..i]));continue;}
            if(c=='#'||c=='-'&&i+2<sql.Length&&sql[i+1]=='-'&&char.IsWhiteSpace(sql[i+2]))
            {while(i<sql.Length&&sql[i]!='\n')i++;tokens.Add(new("comment",sql[start..i]));continue;}
            if(char.IsLetterOrDigit(c)||c is '_' or '$')
            {while(i<sql.Length&&(char.IsLetterOrDigit(sql[i])||sql[i] is '_' or '$'))i++;var text=sql[start..i];tokens.Add(new("word",text,text));continue;}
            i++;tokens.Add(new("symbol",sql[start..i]));
        }
        var significant=Enumerable.Range(0,tokens.Count).Where(i=>tokens[i].Kind is not ("space" or "comment")).ToArray();
        for(var n=0;normalizeNamespace&&n<significant.Length;n++)
        {
            var at=significant[n];var t=tokens[at];if(t.Identifier!=database||n+1==significant.Length||tokens[significant[n+1]].Text!=".")continue;
            if(n+2>=significant.Length)return null;var name=tokens[significant[n+2]].Identifier;if(name==null)return null;
            var column=n+4<significant.Length&&tokens[significant[n+3]].Text=="."&&(tokens[significant[n+4]].Identifier!=null||tokens[significant[n+4]].Text=="*");
            var call=n+3<significant.Length&&tokens[significant[n+3]].Text=="("&&routines.Contains(name);
            var from=n>0&&tokens[significant[n-1]].Kind=="word"&&tokens[significant[n-1]].Text.ToUpperInvariant() is "FROM" or "JOIN"&&tables.Contains(name);
            if(!(column&&tables.Contains(name)||call||from))return null;
            // Typed framing avoids collisions with identifiers or literal text.
            tokens[at]=new("self-database","");n+=2;
        }
        return JsonSerializer.Serialize(tokens.Select(t=>new[]{t.Kind,t.Text}));
    }
}
