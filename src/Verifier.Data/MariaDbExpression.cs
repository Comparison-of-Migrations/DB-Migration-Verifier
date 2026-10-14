using System.Text;
using System.Text.RegularExpressions;
namespace Verifier.Data;

// Safety recognizer for server-persisted CHECK/generated expressions, not a SQL
// rewriter. MariaDB permits loadable UDFs: native validation alone does not make
// evaluation read-only. This grammar accepts only scalar columns, literals and
// operators and explicitly verified pure native calls. Qualified calls, UDFs,
// subqueries, variables and executable comments cannot pass this recognizer.
internal static class MariaDbExpression
{
    // MariaDB 11.4.13 sql_yacc.yy function_call_generic resolves native builders
    // before UDFs and stored functions. Do not admit quoted/qualified names or
    // arbitrary documented functions: many builtins have effects or read state.
    private static readonly HashSet<string> PureNative=new(StringComparer.OrdinalIgnoreCase)
        {"ABS","COALESCE","IFNULL","NULLIF","LOWER","LCASE","UPPER","UCASE","CHAR_LENGTH","OCTET_LENGTH","JSON_VALID","JSON_EXTRACT"};
    private static readonly Regex Number=new(@"\G(?:[0-9]+(?:\.[0-9]*)?|\.[0-9]+)(?:[eE][+-]?[0-9]+)?",RegexOptions.CultureInvariant);
    internal static bool Safe(string sql,IReadOnlySet<string> columns)
    {
        if(sql.Length>65536)return false;
        var tokens=new List<(string Kind,string Text)>();
        for(var i=0;i<sql.Length;)
        {
            if(char.IsWhiteSpace(sql[i])){i++;continue;}
            if(tokens.Count>=8192)return false;
            var start=i;var c=sql[i++];
            if(c=='-'&&i<sql.Length&&sql[i]=='-')return false;
            if(c is '`' or '\'')
            {
                var value=new StringBuilder();var closed=false;
                while(i<sql.Length)
                {
                    var ch=sql[i++];
                    if(ch==c){if(i<sql.Length&&sql[i]==c){value.Append(ch);i++;continue;}closed=true;break;}
                    // Backslash meaning depends on SQL mode; reject ambiguity.
                    if(ch=='\\')return false;value.Append(ch);
                }
                if(!closed)return false;tokens.Add((c=='`'?"column":"literal",value.ToString()));continue;
            }
            if(char.IsAsciiDigit(c)||c=='.'&&i<sql.Length&&char.IsAsciiDigit(sql[i]))
            {
                var match=Number.Match(sql,start);
                i=start+match.Length;tokens.Add(("literal",match.Value));continue;
            }
            if(char.IsLetter(c)||c=='_')
            {while(i<sql.Length&&(char.IsLetterOrDigit(sql[i])||sql[i] is '_' or '$'))i++;tokens.Add(("word",sql[start..i]));continue;}
            if(c is '(' or ')' or ',' or '+' or '-' or '*' or '/' or '%' or '=' or '<' or '>' or '!')
            {
                if(i<sql.Length&&(c=='<'&&sql[i] is '=' or '>'||c is '>' or '!'&&sql[i]=='='))i++;
                if(sql[start..i]=="<="&&i<sql.Length&&sql[i]=='>')i++;
                tokens.Add(("symbol",sql[start..i]));continue;
            }
            return false;
        }
        var parser=new Parser(tokens,columns);return parser.Expression()&&parser.AtEnd;
    }
    private sealed class Parser(List<(string Kind,string Text)> tokens,IReadOnlySet<string> columns)
    {
        private int at,depth;
        internal bool AtEnd=>at==tokens.Count;
        private bool Take(params string[] values)
        {if(at<tokens.Count&&tokens[at].Kind!="column"&&tokens[at].Kind!="literal"&&values.Contains(tokens[at].Text,StringComparer.OrdinalIgnoreCase)){at++;return true;}return false;}
        internal bool Expression()
        {
            if(++depth>128)return false;
            try{if(!Comparison())return false;while(Take("AND","OR","XOR"))if(!Comparison())return false;return true;}finally{depth--;}
        }
        private bool Comparison()
        {
            while(Take("NOT")){}
            if(!Add())return false;
            if(Take("IS")){Take("NOT");return Take("NULL","TRUE","FALSE","UNKNOWN");}
            var not=Take("NOT");
            if(Take("BETWEEN"))return Add()&&Take("AND")&&Add();
            if(Take("IN")){if(!Take("(")||!Add())return false;while(Take(","))if(!Add())return false;return Take(")");}
            if(not)return false;
            while(Take("=","<",">","<=",">=","!=","<>","<=>"))if(!Add())return false;
            return true;
        }
        private bool Add(){if(!Multiply())return false;while(Take("+","-"))if(!Multiply())return false;return true;}
        private bool Multiply(){if(!Atom())return false;while(Take("*","/","%","DIV","MOD"))if(!Atom())return false;return true;}
        private bool Atom()
        {
            if(++depth>128)return false;
            try
            {
                if(Take("+","-"))return Atom();
                if(Take("("))return Expression()&&Take(")");
                if(at==tokens.Count)return false;
                var t=tokens[at++];
                if(t.Kind=="word"&&PureNative.Contains(t.Text)&&Take("("))
                {if(!Expression())return false;while(Take(","))if(!Expression())return false;return Take(")");}
                return t.Kind=="literal"||t.Kind=="column"&&columns.Contains(t.Text)||t.Kind=="word"&&(columns.Contains(t.Text)||new[]{"NULL","TRUE","FALSE"}.Contains(t.Text,StringComparer.OrdinalIgnoreCase));
            }
            finally{depth--;}
        }
    }
}
