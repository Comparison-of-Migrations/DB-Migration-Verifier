using System.Buffers.Binary;
using System.Security.Cryptography;
namespace Verifier.Core;

// A UTF-16 slice in an immutable authenticated row. Every use reopens the owner
// and consumes its completion record, including after an early value difference.
internal sealed class CatalogText(int length,long offset,Func<Stream> open)
{
    internal int Length=>length;
    internal Stream Open(CancellationToken token)
    {
        var input=open();var buffer=new byte[8192];
        try
        {
            for(var remaining=offset;remaining>0;)
            {token.ThrowIfCancellationRequested();var count=(int)Math.Min(buffer.Length,remaining);input.ReadExactly(buffer,0,count);remaining-=count;}
            return input;
        }
        catch(EndOfStreamException){input.Dispose();throw Invalid();}
        catch{input.Dispose();throw;}
        finally{CryptographicOperations.ZeroMemory(buffer);}
    }
    internal void CopyTo(Stream output,CancellationToken token)
    {
        using var input=Open(token);CopyTo(input,output,token);
    }
    internal void CopyTo(Stream input,Stream output,CancellationToken token)
    {
        var buffer=new byte[8192];
        try
        {
            for(var remaining=2L*length;remaining>0;)
            {token.ThrowIfCancellationRequested();var count=(int)Math.Min(buffer.Length,remaining);input.ReadExactly(buffer,0,count);output.Write(buffer,0,count);remaining-=count;}
            Complete(input,token);
        }
        catch(EndOfStreamException){throw Invalid();}
        finally{CryptographicOperations.ZeroMemory(buffer);}
    }
    internal string ReadString()=>string.Create(length,this,static(chars,text)=>
    {
        using var input=text.Open(default);var buffer=new byte[8192];
        try
        {
            for(var start=0;start<chars.Length;)
            {
                var count=Math.Min(buffer.Length/2,chars.Length-start);input.ReadExactly(buffer,0,count*2);
                for(var i=0;i<count;i++)chars[start+i]=(char)BinaryPrimitives.ReadUInt16LittleEndian(buffer.AsSpan(i*2));
                start+=count;
            }
            Complete(input,default);
        }
        catch(EndOfStreamException){throw Invalid();}
        finally{CryptographicOperations.ZeroMemory(buffer);}
    });
    internal static bool Equal(CatalogText? a,string? av,CatalogText? b,string? bv,CancellationToken token)
    {
        token.ThrowIfCancellationRequested();
        if(a==null&&b==null)return StringComparer.Ordinal.Equals(av,bv);
        var al=a?.Length??av?.Length??-1;var bl=b?.Length??bv?.Length??-1;var equal=al==bl;
        using var left=a?.Open(token);using var right=b?.Open(token);
        var aa=new byte[8192];var bb=new byte[8192];
        try
        {
            for(var start=0L;start<Math.Max(al,bl);start+=aa.Length/2)
            {
                token.ThrowIfCancellationRequested();var n=(int)Math.Max(0,Math.Min(aa.Length/2,al-start));var m=(int)Math.Max(0,Math.Min(bb.Length/2,bl-start));
                Read(left,av,start,n,aa);Read(right,bv,start,m,bb);
                if(n!=m||!aa.AsSpan(0,n*2).SequenceEqual(bb.AsSpan(0,m*2)))equal=false;
            }
            if(left!=null)Complete(left,token);if(right!=null)Complete(right,token);return equal;
        }
        catch(EndOfStreamException){throw Invalid();}
        finally{CryptographicOperations.ZeroMemory(aa);CryptographicOperations.ZeroMemory(bb);}
        static void Read(Stream? input,string? value,long start,int count,byte[] bytes)
        {
            if(input!=null){input.ReadExactly(bytes,0,count*2);return;}
            for(var i=0;i<count;i++)BinaryPrimitives.WriteUInt16LittleEndian(bytes.AsSpan(i*2),value![checked((int)start)+i]);
        }
    }
    private static void Complete(Stream input,CancellationToken token)
    {token.ThrowIfCancellationRequested();if(input.ReadByte()!=-1)throw Invalid();}
    private static VerificationException Invalid()=>new(Reason.TemporaryIntegrity);
}
