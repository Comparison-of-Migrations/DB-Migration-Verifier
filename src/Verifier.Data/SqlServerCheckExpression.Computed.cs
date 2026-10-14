using System.Buffers.Binary;
using System.Security.Cryptography;
using System.Text;
using Verifier.Core;
namespace Verifier.Data;

internal static partial class SqlServerCheckExpression
{
    // Computed reads use the native column, not rewritten SQL. Scan the same
    // closed token/function set, retaining names only, never literal bodies.
    internal static IReadOnlySet<string>? ComputedReferences(CatalogText text,CancellationToken token)
    {
        using var input=new Characters(text,token);var references=new HashSet<string>(StringComparer.Ordinal);
        var depth=0;var identifier=false;var any=false;
        while(input.Read() is var code&&code>=0)
        {
            var ch=(char)code;if(char.IsWhiteSpace(ch))continue;any=true;
            if(ch=='\''||ch=='N'&&input.Peek()=='\'')
            {
                if(ch=='N')input.Read();var closed=false;
                while(input.Read() is var c&&c>=0)
                    if(c=='\''){if(input.Peek()=='\''){input.Read();continue;}closed=true;break;}
                if(!closed)return null;identifier=false;continue;
            }
            if(ch=='['||Letter(ch)||ch=='_')
            {
                var bracket=ch=='[';var name=new StringBuilder();
                if(bracket)
                {
                    var closed=false;
                    while(input.Read() is var c&&c>=0)
                    {
                        if(c==']'){if(input.Peek()!=']'){closed=true;break;}input.Read();}
                        name.Append((char)c);if(name.Length>128)return null; // Native SQL identifier limit.
                    }
                    if(!closed||name.Length==0)return null;
                }
                else
                {
                    name.Append(ch);
                    while(input.Peek() is var c&&c>=0&&(Letter((char)c)||Digit(c)||c is '_' or '$' or '#'))
                    {name.Append((char)input.Read());if(name.Length>128)return null;}
                }
                var word=name.ToString();identifier=false;
                if(!bracket&&Keywords.Contains(word))continue;
                while(input.Peek() is var space&&space>=0&&char.IsWhiteSpace((char)space))input.Read();
                if(!bracket&&Builtins.Contains(word)&&input.Peek()=='(')continue;
                references.Add(word);identifier=true;continue;
            }
            identifier=ch=='('?identifier:false;
            if(Digit(ch)||ch=='.')
            {
                if(ch=='.'&&!Digit(input.Peek()))return null;
                while(Digit(input.Peek()))input.Read();
                if(ch!='.'&&input.Peek()=='.'){input.Read();while(Digit(input.Peek()))input.Read();}
                if(input.Peek() is 'e' or 'E')
                {input.Read();if(input.Peek() is '+' or '-')input.Read();if(!Digit(input.Peek()))return null;while(Digit(input.Peek()))input.Read();}
                continue;
            }
            if(ch=='('){if(identifier)return null;depth++;identifier=false;continue;}
            if(ch==')'){if(--depth<0)return null;continue;}
            if(ch=='-'&&input.Peek()=='-'||ch=='/'&&input.Peek()=='*')return null;
            if(ch=='!'){if(input.Peek() is not('=' or '<' or '>'))return null;input.Read();continue;}
            if(ch=='<'){if(input.Peek() is '=' or '>')input.Read();continue;}
            if(ch=='>'){if(input.Peek()=='=')input.Read();continue;}
            if(ch is not('-' or '+' or '*' or '/' or '%' or '=' or ','))return null;
        }
        return any&&depth==0?references:null;
        static bool Digit(int c)=>c is >= '0' and <= '9';
        static bool Letter(char c)=>c is >= 'A' and <= 'Z' or >= 'a' and <= 'z';
    }
    private sealed class Characters:IDisposable
    {
        private readonly Stream input;private readonly CancellationToken token;
        private readonly byte[] bytes=new byte[8192];private long remaining;private int start,end;private int? next;
        internal Characters(CatalogText text,CancellationToken token)
        {this.token=token;remaining=2L*text.Length;input=text.Open(token);}
        internal int Peek()=>next??=Take();
        internal int Read(){var result=Peek();next=null;return result;}
        private int Take()
        {
            if(start==end)
            {
                token.ThrowIfCancellationRequested();
                if(remaining==0){if(input.ReadByte()!=-1)throw new VerificationException(Reason.TemporaryIntegrity);return -1;}
                end=(int)Math.Min(bytes.Length,remaining);input.ReadExactly(bytes,0,end);remaining-=end;start=0;
            }
            var result=BinaryPrimitives.ReadUInt16LittleEndian(bytes.AsSpan(start));start+=2;return result;
        }
        public void Dispose(){try{input.Dispose();}finally{CryptographicOperations.ZeroMemory(bytes);}}
    }
}
