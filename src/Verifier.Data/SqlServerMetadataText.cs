using System.Buffers.Binary;
using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.Data.SqlClient;
using Verifier.Core;

namespace Verifier.Data;

// One selected native definition: spool UTF-16 bytes, then emit the same JSON
// string as ExactJsonStringConverter without creating its full CLR string.
internal static class SqlServerMetadataText
{
    internal static CatalogText JsonArray(CatalogText first,string?[] rest,ProtectedRowStore destination,CancellationToken token)
        =>JsonArray(first,[],rest,destination,token);
    internal static CatalogText JsonArray(CatalogText value,string?[] before,string?[] rest,ProtectedRowStore destination,CancellationToken token)
    {
        var prefix=before.Length==0?"[":SqlServerMetadata.Serialize(before)[..^1]+",";
        var suffix=rest.Length==0?"]":","+SqlServerMetadata.Serialize(rest)[1..];
        var length=checked((long)prefix.Length+value.Length+suffix.Length);
        if(length>int.MaxValue)throw new VerificationException(Reason.RowTooLarge);
        // The first token can already belong to destination; open before append.
        using var input=value.Open(token);
        var descriptor=destination.AddAsync((output,ct)=>
        {
            Write(prefix);value.CopyTo(input,output,ct);Write(suffix);return Task.CompletedTask;
            void Write(string text)=>WriteUtf16(text,output,ct);
        },token).GetAwaiter().GetResult();
        return new((int)length,0,()=>destination.OpenRead(descriptor));
    }
    internal static CatalogText JsonRows(ProtectedCatalogStore rows,ProtectedRowStore destination,CancellationToken token)
    {
        var values=rows.Snapshot(0,rows.Count);long length=2L+Math.Max(0,values.Count-1);
        foreach(var value in values)
        {token.ThrowIfCancellationRequested();if(value.ValueLength<0)throw new VerificationException(Reason.TemporaryIntegrity);length=checked(length+value.ValueLength);}
        if(length>int.MaxValue)throw new VerificationException(Reason.RowTooLarge);
        var descriptor=destination.AddAsync((output,ct)=>
        {
            WriteUtf16("[",output,ct);var first=true;
            foreach(var value in values)
            {
                ct.ThrowIfCancellationRequested();if(!first)WriteUtf16(",",output,ct);first=false;
                if(value.Text is{} text)text.CopyTo(output,ct);
                else WriteUtf16(value.Value??throw new VerificationException(Reason.TemporaryIntegrity),output,ct);
            }
            WriteUtf16("]",output,ct);return Task.CompletedTask;
        },token).GetAwaiter().GetResult();
        return new((int)length,0,()=>destination.OpenRead(descriptor));
    }
    private static void WriteUtf16(string text,Stream output,CancellationToken token)
    {
        var bytes=new byte[8192];
        try
        {
            for(var start=0;start<text.Length;)
            {
                token.ThrowIfCancellationRequested();var count=Math.Min(bytes.Length/2,text.Length-start);
                for(var i=0;i<count;i++)BinaryPrimitives.WriteUInt16LittleEndian(bytes.AsSpan(i*2),text[start+i]);
                output.Write(bytes,0,count*2);start+=count;
            }
        }
        finally{CryptographicOperations.ZeroMemory(bytes);}
    }
    internal static CatalogText? ReadJson(SqlDataReader reader,int ordinal,ProtectedRowStore destination,CancellationToken token,Action<CatalogText>? inspect=null)
    {
        token.ThrowIfCancellationRequested();if(reader.IsDBNull(ordinal))return null;
        long offset=0;
        return ReadJson((buffer,count)=>
        {var read=checked((int)reader.GetBytes(ordinal,offset,buffer,0,count));offset=checked(offset+read);return read;},destination,token,inspect);
    }
    internal static CatalogText ReadJson(Func<byte[],int,int> read,ProtectedRowStore destination,CancellationToken token,Action<CatalogText>? inspect=null)
    {
        using var raw=new ProtectedRowStore(token:token);var bytes=new byte[8192];long length=0;
        try
        {
            var descriptor=raw.AddAsync((output,ct)=>
            {
                while(true)
                {
                    ct.ThrowIfCancellationRequested();var count=read(bytes,bytes.Length);
                    if(count<0||count>bytes.Length)throw new VerificationException(Reason.ObservationChanged);
                    if(count==0)break;length=checked(length+count);output.Write(bytes,0,count);
                }
                return Task.CompletedTask;
            },token).GetAwaiter().GetResult();
            if(length%2!=0||length/2>int.MaxValue)throw new VerificationException(Reason.ObservationChanged);
            inspect?.Invoke(new((int)(length/2),0,()=>raw.OpenRead(descriptor)));
            bool unpaired=false,pending=false;
            Visit(chars=>
            {
                foreach(var ch in chars)
                {
                    if(pending){pending=false;if(char.IsLowSurrogate(ch))continue;unpaired=true;}
                    if(char.IsHighSurrogate(ch))pending=true;else if(char.IsLowSurrogate(ch))unpaired=true;
                }
            });
            unpaired|=pending;long encodedLength=0;
            var encoded=destination.AddAsync((output,ct)=>
            {
                var escaped=new byte[4097*12];
                try
                {
                    WriteAscii("\""u8,output);Visit(chars=>
                    {
                        if(unpaired)
                        {
                            const string hex="0123456789ABCDEF";var start=0;
                            foreach(var ch in chars)
                            {
                                Code('\\');Code('u');Code(hex[ch>>12]);Code(hex[(ch>>8)&15]);Code(hex[(ch>>4)&15]);Code(hex[ch&15]);
                            }
                            output.Write(escaped,0,start);encodedLength=checked(encodedLength+start/2);
                            void Code(char ch){BinaryPrimitives.WriteUInt16LittleEndian(escaped.AsSpan(start),ch);start+=2;}
                        }
                        else WriteAscii(JsonEncodedText.Encode(chars).EncodedUtf8Bytes,output);
                    },pairBoundaries:true);
                    WriteAscii("\""u8,output);return Task.CompletedTask;
                }
                finally{CryptographicOperations.ZeroMemory(escaped);}
            },token).GetAwaiter().GetResult();
            if(encodedLength>int.MaxValue)throw new VerificationException(Reason.RowTooLarge);
            return new((int)encodedLength,0,()=>destination.OpenRead(encoded));

            void Visit(CharBlock consume,bool pairBoundaries=false)
            {
                using var input=raw.OpenRead(descriptor);var chars=new char[4097];var carry=0;
                try
                {
                    for(var remaining=length;remaining>0;)
                    {
                        token.ThrowIfCancellationRequested();var count=(int)Math.Min(bytes.Length,remaining);input.ReadExactly(bytes,0,count);remaining-=count;
                        for(var i=0;i<count/2;i++)chars[carry+i]=(char)BinaryPrimitives.ReadUInt16LittleEndian(bytes.AsSpan(i*2));
                        var used=carry+count/2;carry=pairBoundaries&&remaining>0&&char.IsHighSurrogate(chars[used-1])?1:0;
                        consume(chars.AsSpan(0,used-carry));if(carry!=0)chars[0]=chars[used-1];
                    }
                    if(input.ReadByte()!=-1)throw new VerificationException(Reason.TemporaryIntegrity);
                }
                finally{Array.Clear(chars);}
            }
            void WriteAscii(ReadOnlySpan<byte> ascii,Stream output)
            {
                encodedLength=checked(encodedLength+ascii.Length);
                while(!ascii.IsEmpty)
                {
                    token.ThrowIfCancellationRequested();var count=Math.Min(bytes.Length/2,ascii.Length);
                    for(var i=0;i<count;i++)
                    {if(ascii[i]>127)throw new VerificationException(Reason.TemporaryIntegrity);BinaryPrimitives.WriteUInt16LittleEndian(bytes.AsSpan(i*2),ascii[i]);}
                    output.Write(bytes,0,count*2);ascii=ascii[count..];
                }
            }
        }
        finally{CryptographicOperations.ZeroMemory(bytes);}
    }
    private delegate void CharBlock(ReadOnlySpan<char> chars);
}
