using System.Runtime.InteropServices;
using System.Text.Json;
using HarfBuzzSharp;

// Investigation only: actual OS variable font, no font file copied or shipped.
internal static class HarfBuzzMeasure
{
    public static void Run(string input, string output)
    {
        if (!OperatingSystem.IsMacOS()) throw new PlatformNotSupportedException();
        var cases=JsonSerializer.Deserialize<Case[]>(File.ReadAllText(input),new JsonSerializerOptions {PropertyNameCaseInsensitive=true})!;
        using var blob=Blob.FromFile("/System/Library/Fonts/SFNS.ttf");
        using var face=new Face(blob,0);
        var result=cases.Where(c=>c.Id.StartsWith("en-") && c.Id is not ("en-3" or "en-7")).Select(c=>
        {
            var metrics=new List<object>();
            foreach(var ptem in new[]{false,true})
            {
                using var font=new Font(face);font.SetFunctionsOpenType();font.SetScale((int)(c.Size*65536),(int)(c.Size*65536));
                var variations=new[]{new Axis(0x77676874,c.Weight),new Axis(0x6f70737a,(float)Math.Clamp(c.Size,17,96))};
                hb_font_set_variations(font.Handle,variations,(uint)variations.Length);
                if(ptem)hb_font_set_ptem(font.Handle,(float)c.Size);
                using var buffer=new HarfBuzzSharp.Buffer();buffer.AddUtf16(c.Text);buffer.GuessSegmentProperties();font.Shape(buffer);
                var width=buffer.GlyphPositions.Sum(p=>(double)p.XAdvance)/65536+c.Text.Length*c.Spacing;
                metrics.Add(new {ptem,width,glyphCount=buffer.GlyphPositions.Length});
            }
            return new {c.Id,metrics};
        }).ToArray();
        File.WriteAllText(output,JsonSerializer.Serialize(result,new JsonSerializerOptions {WriteIndented=true}));
    }
    private sealed record Case(string Id,string Text,double Size,int Weight,double Spacing);
    [StructLayout(LayoutKind.Sequential)] private readonly record struct Axis(uint Tag,float Value);
    [DllImport("libHarfBuzzSharp")] private static extern void hb_font_set_variations(IntPtr font,Axis[] axes,uint count);
    [DllImport("libHarfBuzzSharp")] private static extern void hb_font_set_ptem(IntPtr font,float value);
}
