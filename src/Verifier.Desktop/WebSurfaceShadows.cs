using Avalonia;
using Avalonia.Media;
using Avalonia.Rendering.SceneGraph;
using Avalonia.Skia;
using SkiaSharp;

namespace Verifier.Desktop;

/// <summary>CSS shadow sigma and logical offsets, with inset shadows clipped to the padding edge.</summary>
internal static class WebSurfaceShadows
{
    public static void Draw(DrawingContext context, Rect rect, Vector[] corners,
        Thickness border, BoxShadows shadows, bool inset)
    {
        foreach(var shadow in shadows)
            if(shadow.Color.A!=0 && shadow.IsInset==inset)
                context.Custom(new Shadow(rect,corners,border,shadow));
    }

    private sealed class Shadow(Rect rect, Vector[] corners, Thickness border, BoxShadow shadow) : ICustomDrawOperation
    {
        public Rect Bounds => rect.Inflate(shadow.Blur*1.5+Math.Abs(shadow.Spread)+Math.Max(Math.Abs(shadow.OffsetX),Math.Abs(shadow.OffsetY)));
        public bool HitTest(Point p)=>false;
        public bool Equals(ICustomDrawOperation? other)=>false;
        public void Dispose() { }

        public void Render(ImmediateDrawingContext context)
        {
            var feature=context.TryGetFeature<ISkiaSharpApiLeaseFeature>();
            if(feature is null)return;
            using var lease=feature.Lease();
            var canvas=lease.SkCanvas;
            var sigma=(float)(shadow.Blur/2);
            using var filter=sigma>0?SKImageFilter.CreateBlur(sigma,sigma):null;
            using var paint=new SKPaint { IsAntialias=true,Color=new SKColor(shadow.Color.R,shadow.Color.G,shadow.Color.B,(byte)(shadow.Color.A*lease.CurrentOpacity)),ImageFilter=filter };
            using var outer=Rounded(rect,corners);
            canvas.Save();
            try
            {
                if(!shadow.IsInset)
                {
                    canvas.ClipRoundRect(outer,SKClipOperation.Difference,true);
                    canvas.Translate((float)shadow.OffsetX,(float)shadow.OffsetY);
                    using var casting=Rounded(rect.Inflate(shadow.Spread),Spread(corners,shadow.Spread));
                    canvas.DrawRoundRect(casting,paint);
                }
                else
                {
                    var padding=rect.Deflate(border);
                    if(padding.Width<=0 || padding.Height<=0)return;
                    var radii=new[]{Inner(corners[0],border.Left,border.Top),Inner(corners[1],border.Right,border.Top),Inner(corners[2],border.Right,border.Bottom),Inner(corners[3],border.Left,border.Bottom)};
                    using var clip=Rounded(padding,radii);
                    canvas.ClipRoundRect(clip,SKClipOperation.Intersect,true);
                    canvas.Translate((float)shadow.OffsetX,(float)shadow.OffsetY);
                    using var hole=Rounded(padding.Deflate(shadow.Spread),Spread(radii,-shadow.Spread));
                    using var path=new SKPath { FillType=SKPathFillType.EvenOdd };
                    var reach=shadow.Blur*3+Math.Abs(shadow.Spread)+Math.Max(Math.Abs(shadow.OffsetX),Math.Abs(shadow.OffsetY))+1;
                    path.AddRect(ToSk(padding.Inflate(reach)));
                    path.AddRoundRect(hole);
                    canvas.DrawPath(path,paint);
                }
            }
            finally { canvas.Restore(); }
        }
        private static Vector Inner(Vector radius,double x,double y)=>new(Math.Max(0,radius.X-x),Math.Max(0,radius.Y-y));
        private static Vector[] Spread(Vector[] radii,double spread)=>radii.Select(r=>new Vector(Math.Max(0,r.X+spread),Math.Max(0,r.Y+spread))).ToArray();
        private static SKRect ToSk(Rect r)=>new((float)r.Left,(float)r.Top,(float)r.Right,(float)r.Bottom);
        private static SKRoundRect Rounded(Rect r,Vector[] radii)
        {
            var result=new SKRoundRect();
            result.SetRectRadii(ToSk(r),radii.Select(v=>new SKPoint((float)v.X,(float)v.Y)).ToArray());
            return result;
        }
    }
}
