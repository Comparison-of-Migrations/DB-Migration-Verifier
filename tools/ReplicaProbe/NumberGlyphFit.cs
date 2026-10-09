using SkiaSharp;
using System.Text.Json;

// Diagnostic only: compare Segoe Fluent raster settings to preserved live Edge crops.
internal static class NumberGlyphFit
{
    internal static void Run(string folder)
    {
        using var face=SKTypeface.FromFamilyName("Segoe Fluent Icons");
        var results=new List<object>();
        foreach(var (name,filename,y,direction,size,gray) in new[]{
            ("up-hover","source-up-hover.png",0,1,6d,209),
            ("down-normal","source-up-hover.png",22,-1,6d,159),
            ("up-pressed","source-up-hold.png",0,1,5.5,209),
            ("down-pressed","source-down-hold.png",22,-1,5.5,209)})
        {
            using var source=SKBitmap.Decode(Path.Combine(folder,filename));
            var best=long.MaxValue;double bestSize=size,bestX=(15-size)/2,bestY=8.5;
            SKFontHinting bestHint=SKFontHinting.Normal;bool bestSubpixel=true;
            long Error(double em,double x,double baseline,SKFontHinting hint,bool subpixel)
            {
                using var bitmap=new SKBitmap(30,22);using var canvas=new SKCanvas(bitmap);
                canvas.Clear(new SKColor(44,44,44));canvas.Scale(2);
                using var font=new SKFont(face,(float)em){Edging=SKFontEdging.Antialias,Subpixel=subpixel,Hinting=hint};
                using var paint=new SKPaint{IsAntialias=true,Color=new SKColor((byte)gray,(byte)gray,(byte)gray)};
                using var blob=SKTextBlob.Create(direction>0?"\ueddb":"\ueddc",font);
                canvas.DrawText(blob,(float)x,(float)baseline,paint);
                long error=0;
                for(var yy=0;yy<22;yy++)for(var xx=0;xx<30;xx++)error+=Math.Abs(source.GetPixel(xx,yy+y).Red-bitmap.GetPixel(xx,yy).Red);
                return error;
            }
            foreach(var hint in new[]{SKFontHinting.None,SKFontHinting.Slight,SKFontHinting.Normal,SKFontHinting.Full})
            foreach(var sub in new[]{false,true})
            for(var e=-10;e<=10;e++)for(var dy=-4;dy<=4;dy++)for(var dx=-4;dx<=4;dx++)
            {
                var em=size+e*.005;var x=(15-em)/2+dx*.03125;var baseline=8.5+dy*.03125;
                var error=Error(em,x,baseline,hint,sub);
                if(error<best){best=error;bestSize=em;bestX=x;bestY=baseline;bestHint=hint;bestSubpixel=sub;}
            }
            results.Add(new{name,error=best,size=bestSize,x=bestX,baseline=bestY,hint=bestHint.ToString(),subpixel=bestSubpixel});
        }
        File.WriteAllText(Path.Combine(folder,"number-glyph-fit.json"),JsonSerializer.Serialize(results,new JsonSerializerOptions{WriteIndented=true}));
        Console.WriteLine(JsonSerializer.Serialize(results));
    }
}
