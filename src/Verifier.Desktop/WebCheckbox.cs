using System.Diagnostics;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.VisualTree;

namespace Verifier.Desktop;

/// <summary>The source's 20px input only. Labels, hit areas and selection aggregation belong to parents.</summary>
public sealed class WebCheckbox : CheckBox
{
    public static readonly DirectProperty<WebCheckbox, bool> FocusVisibleProperty =
        AvaloniaProperty.RegisterDirect<WebCheckbox, bool>(nameof(FocusVisible), c => c.FocusVisible);
    private readonly WebSurfaceBox _surface = WebSurface.Create(Brushes.Transparent, Brushes.Transparent, new(1.5), new(5));
    private readonly CheckMark _mark = new();
    private readonly FocusOutline _outline;
    private readonly ScaleTransform _scale = new(1, 1);
    private readonly Track<Rgba> _background = new(Rgba.Mix, 120, WebMotion.CssEase().Ease);
    private readonly Track<Rgba> _border = new(Rgba.Mix, 120, WebMotion.CssEase().Ease);
    private readonly Track<Glow> _glow = new(Glow.Mix, 120, WebMotion.CssEase().Ease);
    private readonly Track<double> _press = new((a,b,t) => a+(b-a)*t, 80, WebMotion.CssEase().Ease);
    private readonly Track<Pose> _pose = new(Pose.Mix, 160, WebMotion.SourceEase().Ease);
    private bool _focusVisible, _reduced;
    private long _revision;
    private Appearance _paint;

    public WebCheckbox()
    {
        UseLayoutRounding = false; ClipToBounds = false;
        Width = Height = MinWidth = MaxWidth = MinHeight = MaxHeight = 20;
        Padding = Margin = default; BorderThickness = new(1.5);
        HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Left;
        VerticalAlignment = Avalonia.Layout.VerticalAlignment.Top;
        IsThreeState = false; FocusAdorner = null;
        RenderTransform = _scale; RenderTransformOrigin = new RelativePoint(.5,.5,RelativeUnit.Relative);
        _surface.Child = _mark;
        _outline = new FocusOutline { Child = _surface, UseLayoutRounding = false };
        Template = new FuncControlTemplate<WebCheckbox>((_,_) => _outline);
        ApplyState(false);
    }
    public bool FocusVisible => _focusVisible;
    public bool ReducedMotion { get => _reduced; set { _reduced = value; ApplyState(false); } }
    // A label click changes modality even when this input already holds focus.
    public void FocusFromPointer() { Focus(NavigationMethod.Pointer); SetFocusVisible(false); }
    // HTML mixed is underlying checked=false; its next activation selects, never cycles into mixed.
    protected override void Toggle() => SetCurrentValue(IsCheckedProperty, IsChecked != true);
    protected override void OnKeyDown(KeyEventArgs e)
    {
        if (e.Key is Key.Enter or Key.Space) SetFocusVisible(true);
        if (e.Key == Key.Enter) { e.Handled = true; return; }
        base.OnKeyDown(e);
    }
    protected override void OnKeyUp(KeyEventArgs e)
    {
        if (e.Key == Key.Enter) { e.Handled = true; return; }
        base.OnKeyUp(e);
    }
    protected override void OnGotFocus(FocusChangedEventArgs e)
    { base.OnGotFocus(e); SetFocusVisible(e.NavigationMethod != NavigationMethod.Pointer); ApplyState(); }
    protected override void OnLostFocus(FocusChangedEventArgs e)
    { base.OnLostFocus(e); SetFocusVisible(false); ApplyState(); }
    private void SetFocusVisible(bool value)
    { SetAndRaise(FocusVisibleProperty, ref _focusVisible, value); _outline.Visible = value; ApplyState(); }
    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (_outline is null) return;
        if (change.Property == IsCheckedProperty || change.Property == IsEffectivelyEnabledProperty ||
            change.Property == IsPointerOverProperty || change.Property == IsPressedProperty) ApplyState();
    }
    private Appearance Resolve(bool hover, bool pressed, bool focused, bool visible)
    {
        var disabled = !IsEffectivelyEnabled;
        var selected = IsChecked != false;
        var background = disabled ? "#2e3035" : selected ? "#5865f2" : "#1e1f22";
        var border = disabled ? "#727985" : selected ? "#8b94ff" : "#7c838c";
        var glow = focused && !visible ? new Glow(3,Rgba.Parse("#145865f2")) : default;
        // :hover:not(:disabled) has greater specificity than the focus-visible shadow reset.
        if (hover && !disabled) { border = "#a7b1ff"; glow = new(3,Rgba.Parse("#20a7b1ff")); }
        var pose = IsChecked is null ? new Pose(0,1) : new Pose(45,IsChecked == true ? 1 : 0);
        return new(Rgba.Parse(background),Rgba.Parse(border),glow,pressed && !disabled ? .94 : 1,pose);
    }
    private void ApplyState(bool animate = true)
    {
        if (_outline is null) return;
        Cursor = new Cursor(IsEffectivelyEnabled ? StandardCursorType.Hand : StandardCursorType.No);
        _mark.Mixed = IsChecked is null;
        _outline.Visible = _focusVisible;
        var target = Resolve(IsPointerOver,IsPressed,IsFocused,_focusVisible);
        var revision = ++_revision;
        if (!animate || _reduced || TopLevel.GetTopLevel(this) is not { } top)
        {
            _background.Snap(target.Background); _border.Snap(target.Border); _glow.Snap(target.Glow);
            _press.Snap(target.Scale); _pose.Snap(target.Pose); Paint(target); return;
        }
        _background.SetTarget(target.Background); _border.SetTarget(target.Border); _glow.SetTarget(target.Glow);
        _press.SetTarget(target.Scale); _pose.SetTarget(target.Pose);
        void Frame(TimeSpan _)
        {
            if (revision != _revision || !this.IsAttachedToVisualTree()) return;
            Paint(new(_background.Current,_border.Current,_glow.Current,_press.Current,_pose.Current));
            if (_background.Running || _border.Running || _glow.Running || _press.Running || _pose.Running)
                top.RequestAnimationFrame(Frame);
        }
        top.RequestAnimationFrame(Frame);
    }
    private void Paint(Appearance value)
    {
        _paint = value;
        _surface.Background = new SolidColorBrush(value.Background.Color);
        _surface.BorderBrush = new SolidColorBrush(value.Border.Color);
        _surface.BoxShadow = value.Glow.Spread == 0 && value.Glow.Color.A == 0 ? default
            : new BoxShadows(new BoxShadow { Spread = value.Glow.Spread, Color = value.Glow.Color.Color });
        _scale.ScaleX = _scale.ScaleY = value.Scale;
        _mark.Pose = value.Pose;
    }
    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    { ++_revision; base.OnDetachedFromVisualTree(e); }
    private readonly record struct Appearance(Rgba Background,Rgba Border,Glow Glow,double Scale,Pose Pose);
    private readonly record struct Pose(double Angle,double Scale)
    { public static Pose Mix(Pose a,Pose b,double t) => new(a.Angle+(b.Angle-a.Angle)*t,a.Scale+(b.Scale-a.Scale)*t); }
    private readonly record struct Glow(double Spread,Rgba Color)
    { public static Glow Mix(Glow a,Glow b,double t) => new(a.Spread+(b.Spread-a.Spread)*t,Rgba.Mix(a.Color,b.Color,t)); }
    private readonly record struct Rgba(double A,double R,double G,double B)
    {
        public static Rgba Parse(string s) { var c=Color.Parse(s); return new(c.A,c.R,c.G,c.B); }
        public Color Color => Color.FromArgb(Byte(A),Byte(R),Byte(G),Byte(B));
        private static byte Byte(double v) => (byte)Math.Clamp(Math.Round(v,MidpointRounding.AwayFromZero),0,255);
        public static Rgba Mix(Rgba a,Rgba b,double t)
        {
            var alpha=a.A+(b.A-a.A)*t;
            double Channel(double x,double y) => alpha==0 ? 0 : (x*a.A*(1-t)+y*b.A*t)/alpha;
            return new(alpha,Channel(a.R,b.R),Channel(a.G,b.G),Channel(a.B,b.B));
        }
    }
    private sealed class Track<T>(Func<T,T,double,T> mix,double duration,Func<double,double> easing,Func<double>? clock=null)
    {
        private T _from=default!,_target=default!,_reversingStart=default!;
        private double _factor=1,_duration,_started;
        private double Now => clock?.Invoke() ?? Stopwatch.GetTimestamp()*(1000d/Stopwatch.Frequency);
        private double Progress => _duration<=0 ? 1 : Math.Clamp((Now-_started)/_duration,0,1);
        public bool Running => Progress<1;
        public T Current => mix(_from,_target,easing(Progress));
        public void Snap(T value) { _from=_target=_reversingStart=value; _duration=0; _factor=1; }
        public void SetTarget(T target)
        {
            if (EqualityComparer<T>.Default.Equals(_target,target)) return;
            var progress=Progress; var value=mix(_from,_target,easing(progress));
            if (progress<1 && EqualityComparer<T>.Default.Equals(_reversingStart,target))
            { _factor=Math.Clamp(Math.Abs(easing(progress)*_factor+1-_factor),0,1); _reversingStart=_target; }
            else { _factor=1; _reversingStart=value; }
            _from=value;_target=target;_duration=duration*_factor;_started=Now;
        }
    }
    private sealed class CheckMark : Control
    {
        private bool _mixed;
        private Pose _pose;
        public bool Mixed { get => _mixed; set { _mixed=value; InvalidateVisual(); } }
        public Pose Pose { get => _pose; set { _pose=value; InvalidateVisual(); } }
        public CheckMark() { UseLayoutRounding=false; IsHitTestVisible=false; }
        public override void Render(DrawingContext context)
        {
            if (_pose.Scale==0) return;
            var center=new Point(Bounds.Width/2,Bounds.Height/2-(_mixed ? 0 : 1));
            var radians=_pose.Angle*Math.PI/180;
            Point Map(double x,double y) => new(center.X+_pose.Scale*(x*Math.Cos(radians)-y*Math.Sin(radians)),
                center.Y+_pose.Scale*(x*Math.Sin(radians)+y*Math.Cos(radians)));
            var path=new StreamGeometry();
            using(var p=path.Open())
            {
                if (_mixed)
                { p.BeginFigure(Map(-4.5,-1),true);p.LineTo(Map(4.5,-1));p.LineTo(Map(4.5,1));p.LineTo(Map(-4.5,1)); }
                else
                { p.BeginFigure(Map(3.5,-5.5),true);p.LineTo(Map(3.5,5.5));p.LineTo(Map(-3.5,5.5));p.LineTo(Map(-3.5,3.5));p.LineTo(Map(1.5,3.5));p.LineTo(Map(1.5,-5.5)); }
                p.EndFigure(true);
            }
            context.DrawGeometry(_mixed ? Brush.Parse("#2b2d31") : Brushes.White,null,path);
        }
    }
    private sealed class FocusOutline : Decorator
    {
        private bool _visible;
        public bool Visible { get => _visible; set { _visible=value;InvalidateVisual(); } }
        public override void Render(DrawingContext context)
        {
            if (_visible) context.DrawRectangle(null,new Pen(Brush.Parse("#a7b1ff"),3),new Rect(Bounds.Size).Inflate(4.5),9.5,9.5);
        }
    }
}
