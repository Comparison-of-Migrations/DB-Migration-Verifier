using SkiaSharp;
using System.Text.Json;

// Diagnostic only. The measured browser raster takes precedence over Chromium main.
internal static class ScrollGlyphFit
{
    internal static void Run(string folder)
    {
        using var face=SKTypeface.FromFamilyName("Segoe Fluent Icons");
        var results=new List<object>();
        foreach(var (name,up,gray) in new[]{("up-normal",true,159), ("down-normal",false,159), ("down-pressed",false,209)})
        {
            using var source=SKBitmap.Decode(Path.Combine(folder,"scroll-"+name+".png"));
            long best=long.MaxValue; double bestSize=0,bestX=0,bestY=0;var bestHint=SKFontHinting.Normal;bool bestSub=false;
            foreach(var hint in new[]{SKFontHinting.None,SKFontHinting.Slight,SKFontHinting.Normal,SKFontHinting.Full})
            foreach(var sub in new[]{false,true})
            for(var size=5.5;size<=10;size+=.25)
            for(var baseline=8d;baseline<=16;baseline+=.25)
            for(var shift=-.5;shift<=.5;shift+=.25)
            {
                var x=(15-size)/2+shift;
                using var bitmap=new SKBitmap(30,36);using var canvas=new SKCanvas(bitmap);canvas.Clear(new SKColor(44,44,44));canvas.Scale(2);
                using var font=new SKFont(face,(float)size){Edging=SKFontEdging.Antialias,Subpixel=sub,Hinting=hint};
                using var paint=new SKPaint{IsAntialias=true,Color=new SKColor((byte)gray,(byte)gray,(byte)gray)};
                using var blob=SKTextBlob.Create(up?"\uEDDB":"\uEDDC",font);canvas.DrawText(blob,(float)x,(float)baseline,paint);
                long error=0;for(var y=0;y<36;y++)for(var xx=0;xx<30;xx++)error+=Math.Abs(source.GetPixel(xx,y).Red-bitmap.GetPixel(xx,y).Red);
                if(error>=best)continue;best=error;bestSize=size;bestX=x;bestY=baseline;bestHint=hint;bestSub=sub;
            }
            results.Add(new{name,error=best,size=bestSize,x=bestX,baseline=bestY,hint=bestHint.ToString(),subpixel=bestSub});
        }
        File.WriteAllText(Path.Combine(folder,"scroll-glyph-fit.json"),JsonSerializer.Serialize(new{family=face.FamilyName,results},new JsonSerializerOptions{WriteIndented=true}));
        Console.WriteLine(JsonSerializer.Serialize(results));
    }
}
