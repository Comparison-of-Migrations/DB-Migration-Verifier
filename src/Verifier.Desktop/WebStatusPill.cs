using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Layout;
using Avalonia.Media;

namespace Verifier.Desktop;

/// <summary>Original passive .pill span; its parent supplies verdict and context.</summary>
public sealed class WebStatusPill : UserControl
{
    private readonly Border _surface;
    private readonly Ellipse _dot;
    private readonly WebSystemTextBlock _label;
    private string _text="",_kind="";
    private bool _showDot=true,_categoryContext;
    private TopLevel? _top;
    public string Text {get=>_text;set{_text=value??"";Apply();}}
    public string Kind {get=>_kind;set{_kind=value??"";Apply();}}
    public bool ShowDot {get=>_showDot;set{_showDot=value;Apply();}}
    public bool CategoryContext {get=>_categoryContext;set{_categoryContext=value;Apply();}}
    public WebStatusPill()
    {
        Focusable=false;IsTabStop=false;UseLayoutRounding=false;
        HorizontalAlignment=HorizontalAlignment.Left;VerticalAlignment=VerticalAlignment.Center;
        _dot=new(){Width=6,Height=6,VerticalAlignment=VerticalAlignment.Center,UseLayoutRounding=false};
        _label=WebTypography.CreateCandidate("",12,19.2);
        _label.TextWrapping=TextWrapping.NoWrap;_label.VerticalAlignment=VerticalAlignment.Center;
        var row=new PillFlex{UseLayoutRounding=false};
        row.Children.Add(_dot);row.Children.Add(_label);
        _surface=new(){Padding=new(8,3),CornerRadius=new(5),BorderThickness=new(0),Child=row,UseLayoutRounding=false};
        Content=_surface;Apply();
    }
    protected override Size MeasureOverride(Size availableSize)
    {
        var units=(TopLevel.GetTopLevel(this)?.RenderScaling??2)*64;
        // Original .pill inherits unitless 1.6, whose LayoutUnit product truncates.
        _label.LineHeight=Math.Floor(12*1.6*units)/units;
        return base.MeasureOverride(availableSize);
    }
    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {base.OnAttachedToVisualTree(e);_top=TopLevel.GetTopLevel(this);if(_top is not null)_top.ScalingChanged+=ScaleChanged;}
    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {if(_top is not null)_top.ScalingChanged-=ScaleChanged;_top=null;base.OnDetachedFromVisualTree(e);}
    private void ScaleChanged(object? sender,EventArgs e)=>InvalidateMeasure();
    private sealed class PillFlex : Panel
    {
        protected override Size MeasureOverride(Size availableSize)
        {
            double width=0,height=0;var count=0;
            foreach(var child in Children.Where(c=>c.IsVisible))
            {child.Measure(Size.Infinity);width+=child.DesiredSize.Width;height=Math.Max(height,child.DesiredSize.Height);count++;}
            return new(width+Math.Max(0,count-1)*6,height);
        }
        protected override Size ArrangeOverride(Size finalSize)
        {
            var units=(TopLevel.GetTopLevel(this)?.RenderScaling??2)*64;double x=0;
            foreach(var child in Children.Where(c=>c.IsVisible))
            {
                var y=Math.Floor((finalSize.Height-child.DesiredSize.Height)*units/2)/units;
                child.Arrange(new(x,y,child.DesiredSize.Width,child.DesiredSize.Height));x+=child.DesiredSize.Width+6;
            }
            return finalSize;
        }
    }
    private void Apply()
    {
        var (background,foreground)=(_kind,_categoryContext) switch
        {
            ("ok",true)=>("#00000000","#b5bac1"),
            ("ok",false)=>("#263d33","#6bdda3"),
            ("warn",_)=>("#3b342a","#f0c47c"),
            ("bad",_)=>("#3c2a31","#ff959d"),
            ("accent",_)=>("#3b3d52","#b4bdff"),
            _=>("#2b2d31","#b5bac1")
        };
        _surface.Background=Brush.Parse(background);
        _label.Text=_text;_label.Foreground=_dot.Fill=Brush.Parse(foreground);
        _label.IsVisible=_text.Length>0;_dot.IsVisible=_showDot;
    }
}
