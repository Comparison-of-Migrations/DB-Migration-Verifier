using System.Globalization;
using System.Numerics;
using System.Text;

namespace Verifier.Core;

// Version 1: length-framed values; only explicitly supported PostgreSQL built-ins.
public static class ExactRow
{
    public const int MaxRowBytes = 1024 * 1024;
    public static byte[] Encode(IReadOnlyList<string?> values, IReadOnlyList<Column> columns)
    {
        using var output = new MemoryStream();
        using var writer = new BinaryWriter(output,Encoding.UTF8,true);
        writer.Write(1); writer.Write(values.Count);
        for (var i=0; i<values.Count; i++)
        {
            if (!columns[i].SafeValue) throw new VerificationException(Reason.UnsupportedType);
            var value=values[i];
            writer.Write(value != null);
            if (value is null) continue;
            if (columns[i].Type == "numeric") value=CanonicalNumber(value);
            var bytes=Encoding.UTF8.GetBytes(value);
            if (output.Length + bytes.Length + 4 > MaxRowBytes) throw new VerificationException(Reason.RowTooLarge);
            writer.Write(bytes.Length); writer.Write(bytes);
        }
        return output.ToArray();
    }
    public static string CanonicalNumber(string text)
    {
        if (text is "NaN" or "Infinity" or "-Infinity") return "special:"+text;
        var negative=text.StartsWith('-');
        var start=negative || text.StartsWith('+') ? 1 : 0;
        var point=text.IndexOf('.');
        var scale=point < 0 ? 0 : text.Length-point-1;
        var digits=text[start..].Replace(".","",StringComparison.Ordinal);
        if (digits.Length == 0 || digits.Any(c => c is < '0' or > '9')) throw new VerificationException(Reason.UnsupportedType);
        var integer=BigInteger.Parse(digits,CultureInfo.InvariantCulture);
        if (integer.IsZero) return "0:0";
        while (scale>0 && integer % 10==0) { integer/=10; scale--; }
        return (negative ? "-" : "") + integer.ToString(CultureInfo.InvariantCulture) + ":" + scale.ToString(CultureInfo.InvariantCulture);
    }
    public static byte[] Reorder(byte[] row,IReadOnlyList<int> order)
        =>Project(row,order.Count,order);
    public static byte[] Project(byte[] row,int columnCount,IReadOnlyList<int> order)
    {
        if(row.Length>MaxRowBytes || columnCount<0 || order.Distinct().Count()!=order.Count || order.Any(i=>i<0||i>=columnCount)) throw new VerificationException(Reason.UnsupportedType);
        using var input=new MemoryStream(row,false);using var reader=new BinaryReader(input);
        try
        {
            if(reader.ReadInt32()!=1 || reader.ReadInt32()!=columnCount) throw new VerificationException(Reason.UnsupportedType);
            var cells=new (int Start,int Length)[columnCount];
            for(var i=0;i<cells.Length;i++)
            {
                var start=(int)input.Position;var present=reader.ReadByte();
                if(present>1)throw new VerificationException(Reason.UnsupportedType);
                if(present==1){var length=reader.ReadInt32();if(length<0||length>input.Length-input.Position)throw new VerificationException(Reason.UnsupportedType);input.Position+=length;}
                cells[i]=(start,(int)input.Position-start);
            }
            if(input.Position!=input.Length)throw new VerificationException(Reason.UnsupportedType);
            using var output=new MemoryStream(row.Length);using var writer=new BinaryWriter(output);
            writer.Write(1);writer.Write(order.Count);
            foreach(var index in order){var cell=cells[index];writer.Write(row.AsSpan(cell.Start,cell.Length));}
            return output.ToArray();
        }
        catch(EndOfStreamException){throw new VerificationException(Reason.UnsupportedType);}
    }
}
