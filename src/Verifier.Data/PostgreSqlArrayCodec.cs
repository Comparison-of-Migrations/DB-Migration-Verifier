using System.Buffers.Binary;
using Verifier.Core;

namespace Verifier.Data;

// PostgreSQL 17 array_send/numeric_send layouts, not Npgsql CLR array conversions.
// Dimension lower bounds, empty arrays, null elements and numeric precision are retained.
public delegate byte[] PostgreSqlElementNormalizer(ReadOnlySpan<byte> input);

public static class PostgreSqlArrayCodec
{
    public static byte[] Normalize(ReadOnlySpan<byte> input,bool numericElements,uint expectedElementOid)
        =>Normalize(input,expectedElementOid,numericElements?NormalizeNumeric:null);
    public static byte[] Normalize(ReadOnlySpan<byte> input,uint expectedElementOid,PostgreSqlElementNormalizer? normalizeElement)
    {
        if(input.Length>ExactRow.MaxRowBytes)throw new VerificationException(Reason.RowTooLarge);
        var cursor=0;
        var dimensions=ReadInt(input,ref cursor);var flags=ReadInt(input,ref cursor);var elementOid=unchecked((uint)ReadInt(input,ref cursor));
        if(elementOid!=expectedElementOid || expectedElementOid==0 || dimensions is <0 or >6 || flags is <0 or >1)throw Invalid();
        using var output=new MemoryStream();using var writer=new BinaryWriter(output);
        writer.Write(1);writer.Write(dimensions);long elements=dimensions==0?0:1;
        for(var i=0;i<dimensions;i++)
        {
            var length=ReadInt(input,ref cursor);var lower=ReadInt(input,ref cursor);
            if(length<=0||length>int.MaxValue-(long)lower+1)throw Invalid();
            elements*=length;if(elements>input.Length/4)throw Invalid();
            writer.Write(length);writer.Write(lower);
        }
        for(long i=0;i<elements;i++)
        {
            var length=ReadInt(input,ref cursor);
            if(length == -1){if(flags==0)throw Invalid();writer.Write(-1);continue;}
            if(length<0||length>input.Length-cursor)throw Invalid();
            var value=input.Slice(cursor,length);cursor+=length;
            if(normalizeElement!=null){var canonical=normalizeElement(value);writer.Write(canonical.Length);writer.Write(canonical);}
            else{writer.Write(length);writer.Write(value);}
        }
        if(cursor!=input.Length)throw Invalid();
        return output.ToArray();
    }
    public static byte[] NormalizeNumeric(ReadOnlySpan<byte> value)
    {
        if(value.Length<8)throw Invalid();
        var digits=BinaryPrimitives.ReadUInt16BigEndian(value);var weight=BinaryPrimitives.ReadInt16BigEndian(value[2..]);
        var sign=BinaryPrimitives.ReadUInt16BigEndian(value[4..]);var scale=BinaryPrimitives.ReadUInt16BigEndian(value[6..]);
        if(value.Length!=8+digits*2||scale>16383||sign is not(0 or 0x4000 or 0xc000 or 0xd000 or 0xf000))throw Invalid();
        if(sign>=0xc000&&digits!=0)throw Invalid();
        for(var i=0;i<digits;i++)if(BinaryPrimitives.ReadUInt16BigEndian(value[(8+i*2)..])>=10000)throw Invalid();
        var first=0;var last=(int)digits;
        while(first<last&&BinaryPrimitives.ReadUInt16BigEndian(value[(8+first*2)..])==0)first++;
        while(last>first&&BinaryPrimitives.ReadUInt16BigEndian(value[(8+(last-1)*2)..])==0)last--;
        using var stream=new MemoryStream();using var writer=new BinaryWriter(stream);
        writer.Write(sign>=0xc000?sign:last==first?(ushort)0:sign);
        writer.Write(last==first?0:(int)weight-first);writer.Write(last-first);
        // dscale controls display only. Strip base-10000 padding without rounding any digit.
        writer.Write(value.Slice(8+first*2,(last-first)*2));return stream.ToArray();
    }
    private static int ReadInt(ReadOnlySpan<byte> input,ref int cursor)
    {if(input.Length-cursor<4)throw Invalid();var value=BinaryPrimitives.ReadInt32BigEndian(input[cursor..]);cursor+=4;return value;}
    private static VerificationException Invalid()=>new(Reason.ObservationChanged);
}
