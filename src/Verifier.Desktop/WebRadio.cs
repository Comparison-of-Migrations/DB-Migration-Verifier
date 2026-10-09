using System.Diagnostics;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.VisualTree;
using Avalonia.LogicalTree;
using Avalonia.Threading;

namespace Verifier.Desktop;

/// <summary>The source's 20px input only. Labels, hit areas and selection aggregation belong to parents.</summary>
public sealed class WebRadio : RadioButton
{
    public static readonly DirectProperty<WebRadio, bool> FocusVisibleProperty =
        AvaloniaProperty.RegisterDirect<WebRadio, bool>(nameof(FocusVisible), c => c.FocusVisible);
    private readonly WebSurfaceBox _surface = WebSurface.Create(Brushes.Transparent, Brushes.Transparent, new(1.5), new(10));
    private readonly RadioDot _mark = new();
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

    public WebRadio()
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
        Template = new FuncControlTemplate<WebRadio>((_,_) => _outline);
        ApplyState(false);
    }
    public bool FocusVisible => _focusVisible;
    public bool ReducedMotion { get => _reduced; set { _reduced = value; ApplyState(false); } }
    /// <summary>A label pointer click clears keyboard focus indication even when focus stays on this input.</summary>
    public void FocusFromPointer()
    { Focus(NavigationMethod.Pointer); SetFocusVisible(false); }
    private WebRadio[] Group()
    {
        if (TopLevel.GetTopLevel(this) is not { } root) return new[]{this};
        var group=root.GetVisualDescendants().OfType<WebRadio>().Where(r=>
            !string.IsNullOrEmpty(GroupName) ? r.GroupName==GroupName : string.IsNullOrEmpty(r.GroupName) && r.GetLogicalParent()==this.GetLogicalParent());
        return group.ToArray();
    }
    private void UpdateTabStops()
    {
        var group=Group();var eligible=group.Where(r=>r.IsEffectivelyEnabled && r.IsEffectivelyVisible).ToArray();
        var stop=eligible.FirstOrDefault(r=>r.IsChecked==true) ?? eligible.FirstOrDefault();
        foreach(var r in group) r.IsTabStop=r==stop;
    }
    private void MoveGroup(int direction)
    {
        var group=Group().Where(r=>r.IsEffectivelyEnabled && r.IsEffectivelyVisible).ToArray();
        if (group.Length==0) return;
        var index=Array.IndexOf(group,this);var next=group[(index+direction+group.Length)%group.Length];
        // HTML focuses the destination before input/change. Parent change handlers may rebuild it.
        next.Focus(NavigationMethod.Directional);next.SetCurrentValue(IsCheckedProperty,true);UpdateTabStops();
    }
    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    { base.OnAttachedToVisualTree(e); Dispatcher.UIThread.Post(UpdateTabStops); }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        if (e.Key is Key.Left or Key.Up or Key.Right or Key.Down)
        { MoveGroup(e.Key is Key.Left or Key.Up ? -1 : 1);e.Handled=true;return; }
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
        if (change.Property == IsCheckedProperty || change.Property == GroupNameProperty || change.Property == IsEffectivelyEnabledProperty || change.Property == IsVisibleProperty) UpdateTabStops();
    }
    private Appearance Resolve(bool hover, bool pressed, bool focused, bool visible)
    {
        var disabled = !IsEffectivelyEnabled;
        var selected = IsChecked == true;
        var background = disabled ? "#2e3035" : selected ? "#5865f2" : "#1e1f22";
        var border = disabled ? "#727985" : selected ? "#8b94ff" : "#7c838c";
        var glow = focused && !visible ? new Glow(3,Rgba.Parse("#145865f2")) : default;
        // :hover:not(:disabled) has greater specificity than the focus-visible shadow reset.
        if (hover && !disabled) { border = "#a7b1ff"; glow = new(3,Rgba.Parse("#20a7b1ff")); }
        var pose = new Pose(IsChecked == true ? 1 : 0);
        return new(Rgba.Parse(background),Rgba.Parse(border),glow,pressed && !disabled ? .94 : 1,pose);
    }
    private void ApplyState(bool animate = true)
    {
        if (_outline is null) return;
        Cursor = new Cursor(IsEffectivelyEnabled ? StandardCursorType.Hand : StandardCursorType.No);
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
    private readonly record struct Pose(double Scale)
    { public static Pose Mix(Pose a,Pose b,double t) => new(a.Scale+(b.Scale-a.Scale)*t); }
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
    private sealed class RadioDot : Control
    {
        private Pose _pose;
        public Pose Pose { get => _pose;set {_pose=value;InvalidateVisual();} }
        public RadioDot() { UseLayoutRounding=false;IsHitTestVisible=false; }
        public override void Render(DrawingContext context)
        {
            if (_pose.Scale>0) context.DrawEllipse(Brush.Parse("#2b2d31"),null,new Point(Bounds.Width/2,Bounds.Height/2),4*_pose.Scale,4*_pose.Scale);
        }
    }
    private sealed class FocusOutline : Decorator
    {
        private bool _visible;
        public bool Visible { get => _visible; set { _visible=value;InvalidateVisual(); } }
        public override void Render(DrawingContext context)
        {
            if (_visible) context.DrawEllipse(null,new Pen(Brush.Parse("#a7b1ff"),3),new Point(Bounds.Width/2,Bounds.Height/2),Bounds.Width/2+4.5,Bounds.Height/2+4.5);
        }
    }
}
