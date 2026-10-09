using System.Diagnostics;
using Avalonia;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.VisualTree;

namespace Verifier.Desktop;

/// <summary>Original .step button; navigation policy belongs to Sidebar.</summary>
public sealed class WebSidebarItem : Button
{
    private readonly WebSurfaceBox _surface=WebSurface.Create(Brushes.Transparent,Brushes.Transparent,new(1),new(8));
    private readonly StepFlex _row=new();
    private readonly StepFocus _outline;
    private readonly ScaleTransform _scale=new(1,1);
    private readonly CssTransition<CssColor> _background=new(MixPrecise,120),_color=new(MixPrecise,120),_shadowColor=new(MixPrecise,120);
    private readonly CssTransition<double> _shadowX=new((a,b,t)=>a+(b-a)*t,120),_scaleTrack=new((a,b,t)=>a+(b-a)*t,80);
    private WebMotionPlayback? _press;
    private WebStepBadgeState _state;
    private bool _reduced;
    private long _revision;
    public WebStepBadge Badge=>_row.Badge;
    public WebSystemTextBlock Label=>_row.Label;
    public int Index {get=>Badge.Index;set{Badge.Index=value;UpdateName();}}
    public WebStepBadgeState State {get=>_state;set{_state=value;Badge.State=value;_row.Label.FontWeight=(FontWeight)(value==WebStepBadgeState.Active?650:400);_outline.Active=value==WebStepBadgeState.Active;UpdateName();InvalidateMeasure();ApplyState(false);}}
    public bool ReducedMotion {get=>_reduced;set{_reduced=value;if(value){_press?.Dispose();_press=null;}ApplyState(false);}}
    public WebSidebarItem()
    {
        UseLayoutRounding=false;ClipToBounds=false;MinHeight=52;Padding=new(14,12);BorderThickness=new(1);FocusAdorner=null;
        HorizontalAlignment=HorizontalAlignment.Stretch;VerticalAlignment=VerticalAlignment.Top;
        _surface.Padding=Padding;_surface.Child=_row;_outline=new(){Child=_surface,UseLayoutRounding=false};
        Template=new FuncControlTemplate<WebSidebarItem>((_,_)=>_outline);RenderTransformOrigin=WebMotion.OriginFor(WebMotionKind.Press);RenderTransform=_scale;
        UpdateName();ApplyState(false);
    }
    private void UpdateName()
    {
        _row.Label.Text=Content as string??"";
        AutomationProperties.SetName(this,_state==WebStepBadgeState.Done?_row.Label.Text:$"{Badge.Index} {_row.Label.Text}");
        AutomationProperties.SetItemStatus(this,_state==WebStepBadgeState.Active?"step":"");
    }
    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);if(_outline is null)return;
        if(change.Property==ContentProperty){UpdateName();InvalidateMeasure();}
        if(change.Property==IsPointerOverProperty||change.Property==IsPressedProperty||change.Property==IsEffectivelyEnabledProperty)ApplyState();
    }
    protected override void OnGotFocus(FocusChangedEventArgs e){base.OnGotFocus(e);_outline.Visible=e.NavigationMethod!=NavigationMethod.Pointer;}
    protected override void OnLostFocus(FocusChangedEventArgs e){base.OnLostFocus(e);_outline.Visible=false;}
    protected override void OnKeyDown(KeyEventArgs e){if(e.Key is Key.Space or Key.Enter)_outline.Visible=true;base.OnKeyDown(e);}
    protected override void OnClick()
    {
        if(!IsEffectivelyEnabled)return;
        if(!_reduced&&TopLevel.GetTopLevel(this) is not null){_press?.Dispose();_press=WebMotion.Play(this,WebMotionKind.Press);}base.OnClick();
    }
    private Appearance Resolve(bool pointerOver,bool pressed)
    {
        var disabled=!IsEffectivelyEnabled;var active=_state==WebStepBadgeState.Active;var hover=pointerOver&&!disabled;
        return new(Color.Parse(hover?"#35373c":active?"#3c3f4b":"#00000000"),Color.Parse(active?"#f2f3f5":disabled?"#b5bac1":"#cdd0d6"),active&&!hover?3:0,Color.Parse(active&&!hover?"#8b94ff":"#00000000"),pressed&&!disabled?.98:1);
    }
    private void ApplyState(bool animate=true)
    {
        if(_outline is null)return;Badge.ParentDisabled=!IsEffectivelyEnabled;
        Cursor=new Cursor(IsEffectivelyEnabled?StandardCursorType.Hand:StandardCursorType.No);
        var target=Resolve(IsPointerOver,IsPressed);var revision=++_revision;
        if(!animate||_reduced||TopLevel.GetTopLevel(this) is not {} top)
        {
            _background.Snap(CssColor.FromColor(target.Background));_color.Snap(CssColor.FromColor(target.Foreground));_shadowX.Snap(target.ShadowX);_shadowColor.Snap(CssColor.FromColor(target.ShadowColor));_scaleTrack.Snap(target.Scale);Paint(target);return;
        }
        _background.SetTarget(CssColor.FromColor(target.Background));_color.SetTarget(CssColor.FromColor(target.Foreground));_shadowX.SetTarget(target.ShadowX);_shadowColor.SetTarget(CssColor.FromColor(target.ShadowColor));_scaleTrack.SetTarget(target.Scale);
        void Frame(TimeSpan _){if(revision!=_revision||!this.IsAttachedToVisualTree())return;Paint(new(_background.Current.ToColor(),_color.Current.ToColor(),_shadowX.Current,_shadowColor.Current.ToColor(),_scaleTrack.Current));if(_background.Running||_color.Running||_shadowX.Running||_shadowColor.Running||_scaleTrack.Running)top.RequestAnimationFrame(Frame);}
        top.RequestAnimationFrame(Frame);
    }
    private void Paint(Appearance value)
    {
        _surface.Background=new SolidColorBrush(value.Background);_row.Label.Foreground=new SolidColorBrush(value.Foreground);Badge.InheritedForeground=_row.Label.Foreground;
        _surface.BoxShadow=value.ShadowColor.A==0?default:new(new BoxShadow{IsInset=true,OffsetX=value.ShadowX,OffsetY=0,Blur=0,Spread=0,Color=value.ShadowColor});_scale.ScaleX=_scale.ScaleY=value.Scale;
    }
    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e){++_revision;_press?.Dispose();_press=null;base.OnDetachedFromVisualTree(e);}
    private readonly record struct Appearance(Color Background,Color Foreground,double ShadowX,Color ShadowColor,double Scale);
    private sealed class StepFlex : Panel
    {
        private double _labelWidth;
        public WebStepBadge Badge {get;}=new();
        public WebSystemTextBlock Label {get;}=WebTypography.CreateCandidate("",13,20.8);
        public StepFlex(){UseLayoutRounding=false;Label.TextAlignment=TextAlignment.Left;Children.Add(Badge);Children.Add(Label);}
        protected override Size MeasureOverride(Size availableSize)
        {
            var units=(TopLevel.GetTopLevel(this)?.RenderScaling??2)*64;Label.LineHeight=Math.Floor(13*1.6*units)/units;Badge.Measure(new(24,24));
            Label.Measure(Size.Infinity);_labelWidth=Math.Min(Math.Ceiling(Label.DesiredSize.Width*units)/units,Math.Max(0,availableSize.Width-36));Label.Measure(new(_labelWidth,availableSize.Height));
            return new(36+_labelWidth,Math.Max(24,Label.DesiredSize.Height));
        }
        protected override Size ArrangeOverride(Size finalSize)
        {
            var units=(TopLevel.GetTopLevel(this)?.RenderScaling??2)*64;Badge.Arrange(new(0,(finalSize.Height-24)/2,24,24));
            Label.Arrange(new(36,Math.Floor((finalSize.Height-Label.DesiredSize.Height)*units/2)/units,Math.Min(_labelWidth,Math.Max(0,finalSize.Width-36)),Label.DesiredSize.Height));return finalSize;
        }
    }
    private sealed class StepFocus : Decorator
    {
        private bool _visible,_active;
        public bool Visible{get=>_visible;set{_visible=value;InvalidateVisual();}}
        public bool Active{get=>_active;set{_active=value;InvalidateVisual();}}
        public override void Render(DrawingContext context){if(_visible)context.DrawRectangle(null,new Pen(Brush.Parse(_active?"#bbc1ff":"#a7b1ff"),3),new Rect(Bounds.Size).Inflate(4.5),12.5,12.5);}
    }
    private static CssColor MixPrecise(CssColor a, CssColor b, double t)
    {
        var alpha = a.A + (b.A - a.A) * t;
        double Channel(double x, double y) => alpha == 0 ? 0 : (x * a.A * (1 - t) + y * b.A * t) / alpha;
        return new(alpha, Channel(a.R, b.R), Channel(a.G, b.G), Channel(a.B, b.B));
    }
    private readonly record struct CssColor(double A, double R, double G, double B)
    {
        public static CssColor FromColor(Color value) => new(value.A, value.R, value.G, value.B);
        public Color ToColor()
        {
            static byte Byte(double value) => (byte)Math.Clamp(Math.Round(value, MidpointRounding.AwayFromZero), 0, 255);
            return Color.FromArgb(Byte(A), Byte(R), Byte(G), Byte(B));
        }
    }

    // CSS Transitions reversing-shortening rule; unchanged tracks keep their original clocks.
    private sealed class CssTransition<T>(Func<T, T, double, T> interpolate, double duration, Func<double>? clock = null)
    {
        private T _from = default!, _target = default!, _reversingStart = default!;
        private double _factor = 1, _duration;
        private double _started;
        private double Now => clock?.Invoke() ?? Stopwatch.GetTimestamp() * (1000d / Stopwatch.Frequency);
        private double Progress => _duration <= 0 ? 1 : Math.Min(1, (Now - _started) / _duration);
        public bool Running => Progress < 1;
        public T Current => interpolate(_from, _target, WebMotion.SourceEase().Ease(Progress));
        public void Snap(T target) { _from = _target = _reversingStart = target; _duration = 0; _factor = 1; }
        public void SetTarget(T target)
        {
            if (EqualityComparer<T>.Default.Equals(_target, target)) return;
            var progress = Progress;
            var value = interpolate(_from, _target, WebMotion.SourceEase().Ease(progress));
            if (progress < 1 && EqualityComparer<T>.Default.Equals(_reversingStart, target))
            {
                _factor = Math.Clamp(Math.Abs(WebMotion.SourceEase().Ease(progress) * _factor + 1 - _factor), 0, 1);
                _reversingStart = _target;
            }
            else { _factor = 1; _reversingStart = value; }
            _from = value; _target = target; _duration = duration * _factor; _started = Now;
        }
    }

}
