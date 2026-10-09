using System.Globalization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.VisualTree;

namespace Verifier.Desktop;

/// <summary>Original result-count child; the row owns counts, common maximum and entrance requests.</summary>
public sealed class WebResultCount : Panel
{
    private readonly TrackPanel _track;
    private WebMotionPlayback? _numberMotion, _barMotion;
    private long _count;
    private double _maximum=5;
    private string _kind="different";
    public WebSystemTextBlock Number { get; }=WebTypography.CreateCandidate("0",26,31.2,weight:600,letterSpacing:-.65,tabularNumbers:true);
    public Control Track=>_track;
    public WebSurfaceBox Bar=>_track.Bar;
    public long Count=>_count;
    public double Maximum=>_maximum;
    public string Kind=>_kind;
    public WebResultCount()
    {
        UseLayoutRounding=false;Focusable=false;IsHitTestVisible=false;
        _track=new();Bar.RenderTransformOrigin=WebMotion.OriginFor(WebMotionKind.ResultBar);
        Children.Add(Number);Children.Add(_track);SetValue(0,5,"different");
    }
    public void SetValue(long count,double maximum,string kind)
    {
        if(count<0||!double.IsFinite(maximum)||maximum<=0||kind is not("different" or "unknown" or "equal"))
            throw new ArgumentException("A nonnegative result count, positive shared maximum and original kind are required.");
        StopEntrance();_count=count;_maximum=maximum;_kind=kind;
        Number.Text=count.ToString(CultureInfo.InvariantCulture);
        Number.Foreground=Brush.Parse(count==0?"#b5bac1":kind=="unknown"?"#f0c47c":kind=="equal"?"#6bdda3":"#ff959d");
        Bar.Background=kind=="unknown"?StripedBrush():Brush.Parse(kind=="equal"?"#6bdda3":"#ff959d");
        // Blink stores a computed percentage as float before resolving its CSS layout length.
        _track.Fraction=(float)(count/maximum*100)/100d;_track.InvalidateMeasure();InvalidateMeasure();
    }
    // Source render() calls animateResult only for a focused result entrance, never an ordinary rerender.
    public Task PlayEntrance(bool reducedMotion=false,CancellationToken cancellation=default)
    {
        StopEntrance();
        Bar.RenderTransformOrigin=WebMotion.OriginFor(WebMotionKind.ResultBar);
        _numberMotion=WebMotion.Play(Number,WebMotionKind.ResultNumber,reducedMotion,cancellation);
        _barMotion=WebMotion.Play(Bar,WebMotionKind.ResultBar,reducedMotion,cancellation);
        return Task.WhenAll(_numberMotion.Completion,_barMotion.Completion);
    }
    private void StopEntrance(){_numberMotion?.Dispose();_barMotion?.Dispose();_numberMotion=_barMotion=null;}
    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e){StopEntrance();base.OnDetachedFromVisualTree(e);}
    protected override Size MeasureOverride(Size size)
    {
        var units=64*(TopLevel.GetTopLevel(this)?.RenderScaling??2);
        Number.LineHeight=Math.Floor(31.2*units)/units;Number.Measure(new(size.Width,double.PositiveInfinity));
        var width=double.IsFinite(size.Width)?size.Width:Number.DesiredSize.Width;
        _track.Measure(new(width,5));
        return new(width,Number.DesiredSize.Height+14);
    }
    protected override Size ArrangeOverride(Size size)
    {
        Number.Arrange(new(0,0,size.Width,Number.DesiredSize.Height));
        _track.Arrange(new(0,Number.DesiredSize.Height+9,size.Width,5));return size;
    }
    private static IBrush StripedBrush()=>new LinearGradientBrush
    {
        StartPoint=new(0,0,RelativeUnit.Absolute),EndPoint=new(5/Math.Sqrt(2),5/Math.Sqrt(2),RelativeUnit.Absolute),
        SpreadMethod=GradientSpreadMethod.Repeat,
        GradientStops=new(){new(Color.Parse("#f0c47c"),0),new(Color.Parse("#f0c47c"),.6),new(Color.Parse("#98753e"),.6),new(Color.Parse("#98753e"),1)}
    };
    private sealed class TrackPanel : Panel
    {
        private readonly WebSurfaceBox _background=WebSurface.Create(Brush.Parse("#41434a"),Brushes.Transparent,default,new(2));
        public WebSurfaceBox Bar {get;}=WebSurface.Create(Brush.Parse("#ff959d"),Brushes.Transparent,default,new(2));
        public double Fraction;
        public TrackPanel(){UseLayoutRounding=false;Children.Add(_background);Children.Add(Bar);}
        protected override Size MeasureOverride(Size size){_background.Measure(size);Bar.Measure(size);return new(size.Width,5);}
        protected override Size ArrangeOverride(Size size)
        {
            var units=64*(TopLevel.GetTopLevel(this)?.RenderScaling??2);
            _background.Arrange(new Rect(size));Bar.Arrange(new(0,0,Math.Max(0,Math.Floor(size.Width*Fraction*units)/units),5));
            var radius=Math.Min(2,Math.Min(size.Width/2,size.Height/2));
            Clip=new RectangleGeometry{Rect=new Rect(size),RadiusX=radius,RadiusY=radius};return size;
        }
    }
}
