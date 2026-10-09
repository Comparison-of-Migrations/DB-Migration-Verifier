using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;

namespace Verifier.Desktop;

/// <summary>CSS outer-radius paint and border-box child layout, including unequal-border shadows.</summary>
public sealed class WebSurfaceBox : Decorator
{
    public static readonly StyledProperty<IBrush?> BackgroundProperty=Border.BackgroundProperty.AddOwner<WebSurfaceBox>();
    public static readonly StyledProperty<IBrush?> BorderBrushProperty=Border.BorderBrushProperty.AddOwner<WebSurfaceBox>();
    public static readonly StyledProperty<Thickness> BorderThicknessProperty=Border.BorderThicknessProperty.AddOwner<WebSurfaceBox>();
    public static readonly StyledProperty<CornerRadius> CornerRadiusProperty=Border.CornerRadiusProperty.AddOwner<WebSurfaceBox>();
    public static readonly StyledProperty<BoxShadows> BoxShadowProperty=Border.BoxShadowProperty.AddOwner<WebSurfaceBox>();
    public IBrush? Background { get=>GetValue(BackgroundProperty); set=>SetValue(BackgroundProperty,value); }
    public IBrush? BorderBrush { get=>GetValue(BorderBrushProperty); set=>SetValue(BorderBrushProperty,value); }
    public Thickness BorderThickness { get=>GetValue(BorderThicknessProperty); set=>SetValue(BorderThicknessProperty,value); }
    public CornerRadius CornerRadius { get=>GetValue(CornerRadiusProperty); set=>SetValue(CornerRadiusProperty,value); }
    public BoxShadows BoxShadow { get=>GetValue(BoxShadowProperty); set=>SetValue(BoxShadowProperty,value); }

    static WebSurfaceBox()
    {
        AffectsRender<WebSurfaceBox>(BackgroundProperty,BorderBrushProperty,BorderThicknessProperty,CornerRadiusProperty,BoxShadowProperty);
        AffectsMeasure<WebSurfaceBox>(BorderThicknessProperty);
    }

    public override void Render(DrawingContext context)
    {
        var rect=new Rect(Bounds.Size);if(rect.Width<=0||rect.Height<=0)return;
        var radius=CornerRadius;
        var scale=Math.Min(1,Math.Min(
            Math.Min(Ratio(rect.Width,radius.TopLeft+radius.TopRight),Ratio(rect.Width,radius.BottomLeft+radius.BottomRight)),
            Math.Min(Ratio(rect.Height,radius.TopLeft+radius.BottomLeft),Ratio(rect.Height,radius.TopRight+radius.BottomRight))));
        var corners=new[]{new Vector(radius.TopLeft*scale,radius.TopLeft*scale),new Vector(radius.TopRight*scale,radius.TopRight*scale),new Vector(radius.BottomRight*scale,radius.BottomRight*scale),new Vector(radius.BottomLeft*scale,radius.BottomLeft*scale)};
        WebSurfaceShadows.Draw(context,rect,corners,BorderThickness,BoxShadow,false);
        context.DrawRectangle(Background,null,new RoundedRect(rect,corners[0],corners[1],corners[2],corners[3]));
        WebSurfaceShadows.Draw(context,rect,corners,BorderThickness,BoxShadow,true);
        var border=BorderThickness;if(border==default||BorderBrush is null)return;
        var ring=new StreamGeometry();
        using(var path=ring.Open())
        {
            path.SetFillRule(FillRule.EvenOdd);Figure(path,rect,corners);
            var inner=rect.Deflate(border);
            if(inner.Width>0&&inner.Height>0)
                Figure(path,inner,new[]{Inner(corners[0],border.Left,border.Top),Inner(corners[1],border.Right,border.Top),Inner(corners[2],border.Right,border.Bottom),Inner(corners[3],border.Left,border.Bottom)});
        }
        context.DrawGeometry(BorderBrush,null,ring);
    }

    private static double Ratio(double available,double requested)=>requested==0?1:available/requested;
    private static Vector Inner(Vector radius,double x,double y)=>new(Math.Max(0,radius.X-x),Math.Max(0,radius.Y-y));
    private static void Figure(StreamGeometryContext path,Rect r,Vector[] c)
    {
        path.BeginFigure(new(r.Left+c[0].X,r.Top),true);
        path.LineTo(new(r.Right-c[1].X,r.Top));Arc(path,new(r.Right,r.Top+c[1].Y),c[1]);
        path.LineTo(new(r.Right,r.Bottom-c[2].Y));Arc(path,new(r.Right-c[2].X,r.Bottom),c[2]);
        path.LineTo(new(r.Left+c[3].X,r.Bottom));Arc(path,new(r.Left,r.Bottom-c[3].Y),c[3]);
        path.LineTo(new(r.Left,r.Top+c[0].Y));Arc(path,new(r.Left+c[0].X,r.Top),c[0]);path.EndFigure(true);
    }
    private static void Arc(StreamGeometryContext path,Point end,Vector radius)
    {
        if(radius.X==0||radius.Y==0)path.LineTo(end);
        else path.ArcTo(end,new Size(radius.X,radius.Y),0,false,SweepDirection.Clockwise);
    }
    protected override Size MeasureOverride(Size availableSize)
    {
        var inset=Padding+BorderThickness;Child?.Measure(availableSize.Deflate(inset));
        return (Child?.DesiredSize??default).Inflate(inset);
    }
    protected override Size ArrangeOverride(Size finalSize)
    {
        Child?.Arrange(new Rect(finalSize).Deflate(Padding+BorderThickness));return finalSize;
    }
}
