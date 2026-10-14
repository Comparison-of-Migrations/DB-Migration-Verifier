using System.Buffers.Binary;
using System.Security.Cryptography;
namespace Verifier.Core;

// Exact UTF-16 code units, including unpaired surrogates. JSON replacement
// fallback must never make distinct native definitions or identifiers equal.
internal static class CatalogFactWire
{
    internal static long Length(CatalogFact fact)=>16L+2L*(fact.Key.Length+(long)fact.Attribute.Length+(fact.Column?.Length??0)+Math.Max(0,fact.ValueLength));
    internal static void Write(CatalogFact fact,Stream output,CancellationToken token,Stream? valueInput=null)
    {
        var buffer=new byte[8192];
        try
        {
            foreach(var field in new[]{fact.Key,fact.Attribute,fact.Column})WriteField(field);
            if(fact.Text is {} text)
            {
                BinaryPrimitives.WriteInt32LittleEndian(buffer,text.Length);output.Write(buffer,0,4);
                if(valueInput==null)text.CopyTo(output,token);else text.CopyTo(valueInput,output,token);
            }
            else WriteField(fact.Value);
            void WriteField(string? field)
            {
                token.ThrowIfCancellationRequested();
                BinaryPrimitives.WriteInt32LittleEndian(buffer,field?.Length??-1);output.Write(buffer,0,4);
                if(field==null)return;
                for(var offset=0;offset<field.Length;)
                {
                    token.ThrowIfCancellationRequested();var count=Math.Min(buffer.Length/2,field.Length-offset);
                    for(var i=0;i<count;i++)BinaryPrimitives.WriteUInt16LittleEndian(buffer.AsSpan(i*2),field[offset+i]);
                    output.Write(buffer,0,count*2);offset+=count;
                }
            }
        }
        finally{CryptographicOperations.ZeroMemory(buffer);}
    }
    internal static CatalogFact Read(Stream input,CancellationToken token,Func<Stream>? reopen=null)
    {
        var buffer=new byte[8192];
        try
        {
            var key=ReadField();var attribute=ReadField();var column=ReadField();
            token.ThrowIfCancellationRequested();input.ReadExactly(buffer,0,4);var valueLength=BinaryPrimitives.ReadInt32LittleEndian(buffer);
            if(valueLength< -1||key==null||attribute==null)throw Invalid();
            CatalogFact fact;
            if(valueLength>4096&&reopen!=null)
            {
                // Validate now, and revalidate on every later use. No cached
                // plaintext can hide ciphertext damage or an expired owner.
                for(var remaining=2L*valueLength;remaining>0;)
                {token.ThrowIfCancellationRequested();var count=(int)Math.Min(buffer.Length,remaining);input.ReadExactly(buffer,0,count);remaining-=count;}
                var offset=16L+2L*(key.Length+(long)attribute.Length+(column?.Length??0));
                fact=new CatalogFact(key,column,attribute,null).WithText(new(valueLength,offset,reopen));
            }
            else fact=new(key,column,attribute,valueLength==-1?null:ReadText(valueLength));
            // Consume the authenticated completion record, not merely the fields.
            if(input.ReadByte()!=-1)throw Invalid();
            return fact;
            string? ReadField()
            {
                token.ThrowIfCancellationRequested();input.ReadExactly(buffer,0,4);
                var count=BinaryPrimitives.ReadInt32LittleEndian(buffer);
                if(count==-1)return null;if(count<0)throw Invalid();
                return ReadText(count);
            }
            string ReadText(int count)=>string.Create(count,(input,buffer,token),static(chars,state)=>
                {
                    var (source,bytes,cancellation)=state;
                    for(var offset=0;offset<chars.Length;)
                    {
                        cancellation.ThrowIfCancellationRequested();var length=Math.Min(bytes.Length/2,chars.Length-offset);
                        source.ReadExactly(bytes,0,length*2);
                        for(var i=0;i<length;i++)chars[offset+i]=(char)BinaryPrimitives.ReadUInt16LittleEndian(bytes.AsSpan(i*2));
                        offset+=length;
                    }
                });
        }
        catch(EndOfStreamException){throw Invalid();}
        finally{CryptographicOperations.ZeroMemory(buffer);}
    }
    private static VerificationException Invalid()=>new(Reason.TemporaryIntegrity);
}
