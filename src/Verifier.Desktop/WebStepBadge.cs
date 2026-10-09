using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;

namespace Verifier.Desktop;

public enum WebStepBadgeState { Normal, Active, Done }

/// <summary>Passive .step-index. The containing step owns input and motion.</summary>
public sealed class WebStepBadge : UserControl
{
    private readonly WebSurfaceBox _surface;
    private readonly WebSystemTextBlock _number;
    private readonly SvgIcon _check;
    private int _index=1;
    private WebStepBadgeState _state;
    private bool _parentDisabled;
    private IBrush? _inheritedForeground;
    public int Index {get=>_index;set{_index=value;Apply();}}
    public WebStepBadgeState State {get=>_state;set{_state=value;Apply();}}
    public bool ParentDisabled {get=>_parentDisabled;set{_parentDisabled=value;Apply();}}
    public IBrush? InheritedForeground {get=>_inheritedForeground;set{_inheritedForeground=value;Apply();}}
    public WebStepBadge()
    {
        Width=Height=24;UseLayoutRounding=false;Focusable=false;IsTabStop=false;
        _number=WebTypography.CreateCandidate("1",12,19.2);_number.TextWrapping=TextWrapping.NoWrap;
        _check=new(){IconName="check",Width=15,Height=15,UseLayoutRounding=false};
        var center=new BadgeCenter{UseLayoutRounding=false};center.Children.Add(_number);center.Children.Add(_check);
        _surface=WebSurface.Create(Brushes.Transparent,Brush.Parse("#7c838c"),new(1),new(5));_surface.Child=center;Content=_surface;Apply();
    }
    protected override Size MeasureOverride(Size availableSize)
    {
        var units=(TopLevel.GetTopLevel(this)?.RenderScaling??2)*64;
        _number.LineHeight=Math.Floor(12*1.6*units)/units;
        return base.MeasureOverride(availableSize);
    }
    private void Apply()
    {
        var(bg,border,color)=_state switch
        {
            WebStepBadgeState.Active=>("#5865f2","#5865f2","#ffffff"),
            WebStepBadgeState.Done=>("#363846","#697185","#d1d6ff"),
            _=>("#00000000","#7c838c",_parentDisabled?"#b5bac1":"#cdd0d6")
        };
        _surface.Background=Brush.Parse(bg);_surface.BorderBrush=Brush.Parse(border);
        _number.Text=_index.ToString(System.Globalization.CultureInfo.InvariantCulture);
        _number.Foreground=_check.Stroke=Brush.Parse(color);_number.FontWeight=(FontWeight)(_state==WebStepBadgeState.Active?650:400);
        if(_state==WebStepBadgeState.Normal&&_inheritedForeground is not null)_number.Foreground=_inheritedForeground;
        _number.IsVisible=_state!=WebStepBadgeState.Done;_check.IsVisible=_state==WebStepBadgeState.Done;
    }
    private sealed class BadgeCenter : Panel
    {
        protected override Size MeasureOverride(Size availableSize)
        {var child=Children.Single(c=>c.IsVisible);child.Measure(Size.Infinity);return child.DesiredSize;}
        protected override Size ArrangeOverride(Size finalSize)
        {
            var child=Children.Single(c=>c.IsVisible);var size=child.DesiredSize;var units=(TopLevel.GetTopLevel(this)?.RenderScaling??2)*64;
            child.Arrange(new(Math.Floor((finalSize.Width-size.Width)*units/2)/units,Math.Floor((finalSize.Height-size.Height)*units/2)/units,size.Width,size.Height));return finalSize;
        }
    }
}
