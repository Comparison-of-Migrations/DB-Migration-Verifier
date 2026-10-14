using System.Buffers.Binary;
using System.Globalization;
using System.Text.RegularExpressions;
using Verifier.Core;
namespace Verifier.Data;

// Confirmed synthetic inputs only. Canonicalize endian markers, preserving every
// native coordinate bit and geometry/ring/child order. No tolerance or WKT rounding.
internal static class MySqlTrialGeometry
{
    internal static bool Is(Column column)=>MySqlTrialValue.Base(column) is "geometry" or "point" or "linestring" or "polygon" or "multipoint" or "multilinestring" or "multipolygon" or "geomcollection" or "geometrycollection";
    internal static (uint Srid,byte[] Wkb) Parse(string text)
    {
        var m=Regex.Match(text,@"^SRID=(0|[1-9][0-9]{0,9});0x([0-9a-fA-F]+)$",RegexOptions.CultureInvariant);
        if(!m.Success||!uint.TryParse(m.Groups[1].Value,NumberStyles.None,CultureInfo.InvariantCulture,out var srid)||m.Groups[2].Length%2!=0)throw new VerificationException(Reason.InvalidInput);
        var input=Convert.FromHexString(m.Groups[2].Value);using var output=new MemoryStream(input.Length);using var writer=new BinaryWriter(output);var position=0;
        Geometry(0,0);if(position!=input.Length)throw new VerificationException(Reason.InvalidInput);return(srid,output.ToArray());
        uint Read(bool little)
        {if(position>input.Length-4)throw new VerificationException(Reason.InvalidInput);var value=little?BinaryPrimitives.ReadUInt32LittleEndian(input.AsSpan(position,4)):BinaryPrimitives.ReadUInt32BigEndian(input.AsSpan(position,4));position+=4;return value;}
        void Points(uint count,bool little)
        {
            if(count>(input.Length-position)/16)throw new VerificationException(Reason.InvalidInput);
            for(uint i=0;i<count;i++)for(var coordinate=0;coordinate<2;coordinate++)
            {var bits=little?BinaryPrimitives.ReadInt64LittleEndian(input.AsSpan(position,8)):BinaryPrimitives.ReadInt64BigEndian(input.AsSpan(position,8));position+=8;if(!double.IsFinite(BitConverter.Int64BitsToDouble(bits)))throw new VerificationException(Reason.InvalidInput);writer.Write(bits);}
        }
        void Geometry(uint expected,int depth)
        {
            if(depth>128||position>=input.Length||input[position]>1)throw new VerificationException(Reason.InvalidInput);var little=input[position++]==1;var type=Read(little);
            if(type is <1 or >7||expected!=0&&type!=expected)throw new VerificationException(Reason.InvalidInput);writer.Write((byte)1);writer.Write(type);
            if(type==1){Points(1,little);return;}var count=Read(little);writer.Write(count);
            if(type==2){Points(count,little);return;}
            if(count>(input.Length-position)/4)throw new VerificationException(Reason.InvalidInput);
            if(type==3){for(uint i=0;i<count;i++){var n=Read(little);writer.Write(n);Points(n,little);}return;}
            for(uint i=0;i<count;i++)Geometry(type==7?0:type-3,depth+1);
        }
    }
    internal static void ValidateRoot(Column column,byte[] wkb)
    {
        var expected=MySqlTrialValue.Base(column) switch{"point"=>1u,"linestring"=>2u,"polygon"=>3u,"multipoint"=>4u,"multilinestring"=>5u,"multipolygon"=>6u,"geomcollection" or "geometrycollection"=>7u,_=>0u};
        if(expected!=0&&BinaryPrimitives.ReadUInt32LittleEndian(wkb.AsSpan(1,4))!=expected)throw new VerificationException(Reason.InvalidInput);
    }
    internal static string Sample(Column column,bool changed=false)
    {
        using var output=new MemoryStream();using var w=new BinaryWriter(output);
        void Header(uint type){w.Write((byte)1);w.Write(type);}void Point(){Header(1);w.Write(changed?3d:1d);w.Write(changed?4d:2d);}
        void Line(){Header(2);w.Write(2u);w.Write(0d);w.Write(0d);w.Write(changed?2d:1d);w.Write(1d);}
        void Polygon(){Header(3);w.Write(1u);w.Write(4u);foreach(var (x,y) in new[]{(0d,0d),(changed?2d:1d,0d),(0d,1d),(0d,0d)}){w.Write(x);w.Write(y);}}
        switch(MySqlTrialValue.Base(column))
        {case "point":Point();break;case "linestring":Line();break;case "polygon":Polygon();break;case "multipoint":Header(4);w.Write(1u);Point();break;case "multilinestring":Header(5);w.Write(1u);Line();break;case "multipolygon":Header(6);w.Write(1u);Polygon();break;default:Header(7);w.Write(changed?1u:0u);if(changed)Point();break;}
        return "SRID=0;0x"+Convert.ToHexString(output.ToArray()).ToLowerInvariant();
    }
}
