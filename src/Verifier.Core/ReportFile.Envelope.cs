using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace Verifier.Core;

public static partial class ReportFile
{
    // The existing string envelope is retained. Stream its one potentially huge
    // string instead of materializing encoded text and decoded JSON alongside the model.
    private static async Task<RunReport> ReadEnvelope(Stream input)
    {
        var reader=new EnvelopeReader(input);string? format=null,checksum=null;byte[]? actual=null;RunReport? report=null;
        var seen=new HashSet<string>(StringComparer.Ordinal);
        try
        {
        await reader.Start();
        while(true)
        {
            await reader.Expect('"');var name=await reader.String(16);
            if(!seen.Add(name)||name is not ("Format" or "Payload" or "Sha256"))throw new InvalidDataException();
            await reader.Expect(':');await reader.Expect('"');
            if(name=="Payload")
            {
                using var decode=new FromBase64Transform(FromBase64TransformMode.IgnoreWhiteSpaces);
                using var hash=IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
                await using var encoded=new Base64StringStream(reader);
                await using var decoded=new CryptoStream(encoded,decode,CryptoStreamMode.Read,leaveOpen:true);
                await using var hashing=new HashingStream(decoded,hash);
                try{report=await ReadPayload(hashing);}
                catch(CryptographicException){throw new InvalidDataException();}
                if(!encoded.Ended)throw new InvalidDataException();
                actual=hash.GetHashAndReset();
            }
            else if(name=="Format")format=await reader.String(32);
            else checksum=await reader.String(64);
            var next=await reader.NonSpace();if(next=='}')break;if(next!=',')throw new InvalidDataException();
        }
        if(await reader.NonSpace()!=-1||seen.Count!=3||format!="DMV-RESULT-1"||checksum is not {Length:64}||report==null||actual==null)throw new InvalidDataException();
        byte[] expected;try{expected=Convert.FromHexString(checksum);}catch(FormatException){throw new InvalidDataException();}
        if(!CryptographicOperations.FixedTimeEquals(actual,expected))throw new InvalidDataException();
        Validate(report);return report;
        }
        catch{report?.Dispose();throw;}
    }

    private sealed class EnvelopeReader(Stream input)
    {
        private readonly byte[] buffer=new byte[16384];private int position,length;
        private async ValueTask<bool> Fill()
        {if(position<length)return true;length=await input.ReadAsync(buffer);position=0;return length!=0;}
        private async ValueTask<int> Byte()=>await Fill()?buffer[position++]:-1;
        internal async ValueTask Start()
        {
            var first=await Byte();if(first==239){if(await Byte()!=187||await Byte()!=191)throw new InvalidDataException();first=await Byte();}
            while(first is ' ' or '\t' or '\r' or '\n')first=await Byte();if(first!='{')throw new InvalidDataException();
        }
        internal async ValueTask<int> NonSpace()
        {int value;do{value=await Byte();}while(value is ' ' or '\t' or '\r' or '\n');return value;}
        internal async ValueTask Expect(char expected){if(await NonSpace()!=expected)throw new InvalidDataException();}
        // String scalar characters are ASCII (envelope names, checksum, base64).
        // JSON escapes remain supported, including escaped slashes and \uXXXX.
        private async ValueTask<int> Character()
        {
            var value=await Byte();if(value=='"')return -1;
            if(value=='\\')
            {
                value=await Byte();if(value=='u')
                {
                    value=0;for(var i=0;i<4;i++){var c=await Byte();var digit=c is >= '0' and <= '9'?c-'0':c is >= 'a' and <= 'f'?c-'a'+10:c is >= 'A' and <= 'F'?c-'A'+10:-1;if(digit<0)throw new InvalidDataException();value=value*16+digit;}
                    if(value>127)throw new InvalidDataException();return value;
                }
                return value switch{'"'=>'"','\\'=>'\\','/'=>'/','b'=>'\b','f'=>'\f','n'=>'\n','r'=>'\r','t'=>'\t',_=>throw new InvalidDataException()};
            }
            if(value<32||value>127)throw new InvalidDataException();return value;
        }
        internal async ValueTask<string> String(int maximum)
        {
            var text=new StringBuilder();int c;while((c=await Character())>=0){if(text.Length==maximum)throw new InvalidDataException();text.Append((char)c);}return text.ToString();
        }
        internal async ValueTask<(int Count,bool Ended)> StringBytes(Memory<byte> output)
        {
            if(!await Fill())throw new InvalidDataException();
            var count=Math.Min(length-position,output.Length);
            var special=buffer.AsSpan(position,count).IndexOfAny((byte)'"',(byte)'\\');
            if(special>=0)count=special;
            if(count>0)
            {
                if(buffer.AsSpan(position,count).IndexOfAnyExceptInRange((byte)32,(byte)127)>=0)throw new InvalidDataException();
                buffer.AsMemory(position,count).CopyTo(output);position+=count;return(count,false);
            }
            var c=await Character();if(c<0)return(0,true);output.Span[0]=(byte)c;return(1,false);
        }
    }

    private sealed class Base64StringStream(EnvelopeReader reader):Stream
    {
        private long symbols;private int padding;internal bool Ended{get;private set;}
        public override async ValueTask<int> ReadAsync(Memory<byte> buffer,CancellationToken cancellationToken=default)
        {
            if(buffer.Length==0||Ended)return 0;cancellationToken.ThrowIfCancellationRequested();
            var (count,ended)=await reader.StringBytes(buffer);
            for(var i=0;i<count;i++)
            {
                var c=buffer.Span[i];if(c is (byte)' ' or (byte)'\t' or (byte)'\r' or (byte)'\n')continue;
                if(c=='='){if(symbols%4<2||++padding>2)throw new InvalidDataException();}
                else if(padding!=0||!(c is >= (byte)'A' and <= (byte)'Z' or >= (byte)'a' and <= (byte)'z' or >= (byte)'0' and <= (byte)'9' or (byte)'+' or (byte)'/'))throw new InvalidDataException();
                symbols++;
            }
            if(ended){if(symbols==0||symbols%4!=0)throw new InvalidDataException();Ended=true;}
            return count;
        }
        public override Task<int> ReadAsync(byte[] buffer,int offset,int count,CancellationToken cancellationToken)=>ReadAsync(buffer.AsMemory(offset,count),cancellationToken).AsTask();
        public override int Read(byte[] buffer,int offset,int count)=>ReadAsync(buffer.AsMemory(offset,count)).AsTask().GetAwaiter().GetResult();
        public override bool CanRead=>true;public override bool CanSeek=>false;public override bool CanWrite=>false;
        public override long Length=>throw new NotSupportedException();public override long Position{get=>throw new NotSupportedException();set=>throw new NotSupportedException();}
        // CryptoStream finalizes even when parsing aborts before EOF. A read-only
        // flush has no work; it must not hide the original safe file error.
        public override void Flush(){}public override long Seek(long offset,SeekOrigin origin)=>throw new NotSupportedException();public override void SetLength(long value)=>throw new NotSupportedException();public override void Write(byte[] buffer,int offset,int count)=>throw new NotSupportedException();
    }
}
