using System.Buffers.Binary;
using Verifier.Core;
namespace Verifier.Data;
public sealed partial class PostgreSqlDatabase
{
    private static byte[] NormalizeCustom(ReadOnlySpan<byte> value,ValueCodec codec)
    {
        using var stream=new MemoryStream();using var writer=new BinaryWriter(stream);writer.Write(codec.CastType);
        if(codec.Numeric)writer.Write(PostgreSqlArrayCodec.NormalizeNumeric(value));
        else if(codec.ArrayElement!=0)
            writer.Write(PostgreSqlArrayCodec.Normalize(value,codec.ArrayElement,b=>NormalizeCustom(b,codec.Element!)));
        else if(codec.Fields!=null)
        {
            var cursor=0;var count=WireInt(value,ref cursor);if(count!=codec.Fields.Count)throw InvalidCodec();writer.Write(count);
            foreach(var (oid,field) in codec.Fields)
            {
                if(unchecked((uint)WireInt(value,ref cursor))!=oid)throw InvalidCodec();
                var length=WireInt(value,ref cursor);if(length== -1){writer.Write(-1);continue;}
                if(length<0||length>value.Length-cursor)throw InvalidCodec();var normalized=NormalizeCustom(value.Slice(cursor,length),field);cursor+=length;writer.Write(normalized.Length);writer.Write(normalized);
            }
            if(cursor!=value.Length)throw InvalidCodec();
        }
        else if(codec.Range!=null)
        {
            if(codec.MultiRange)
            {
                var cursor=0;var count=WireInt(value,ref cursor);if(count<0||count>value.Length/4)throw InvalidCodec();writer.Write(count);
                for(var i=0;i<count;i++)
                {var length=WireInt(value,ref cursor);if(length<0||length>value.Length-cursor)throw InvalidCodec();var range=NormalizeCustom(value.Slice(cursor,length),codec.Range);cursor+=length;writer.Write(range.Length);writer.Write(range);}
                if(cursor!=value.Length)throw InvalidCodec();
            }
            else
            {
                if(value.IsEmpty)throw InvalidCodec();var flags=value[0];var cursor=1;
                if((flags&~31)!=0||(flags&1)!=0&&flags!=1)throw InvalidCodec();writer.Write(flags);
                for(var bound=0;bound<2;bound++)
                {
                    if((flags&1)!=0||(flags&(bound==0?8:16))!=0)continue;
                    var length=WireInt(value,ref cursor);if(length<0||length>value.Length-cursor)throw InvalidCodec();var normalized=NormalizeCustom(value.Slice(cursor,length),codec.Range);cursor+=length;writer.Write(normalized.Length);writer.Write(normalized);
                }
                if(cursor!=value.Length)throw InvalidCodec();
            }
        }
        else writer.Write(value);
        if(stream.Length>ExactRow.MaxRowBytes)throw new VerificationException(Reason.RowTooLarge);
        return stream.ToArray();
    }
    private static int WireInt(ReadOnlySpan<byte> value,ref int cursor)
    {if(value.Length-cursor<4)throw InvalidCodec();var result=BinaryPrimitives.ReadInt32BigEndian(value[cursor..]);cursor+=4;return result;}
    private static VerificationException InvalidCodec()=>new(Reason.ObservationChanged);
}
