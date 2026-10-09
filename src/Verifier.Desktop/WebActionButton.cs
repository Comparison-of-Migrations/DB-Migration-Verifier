using System.Diagnostics;
using Avalonia;
using Avalonia.Automation;
using Avalonia.Automation.Peers;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.VisualTree;

namespace Verifier.Desktop;

/// <summary>Isolated .btn replica. Parent overrides are explicit; no action-name inference.</summary>
public class WebActionButton : Button
{
    private readonly WebSurfaceBox _surface = WebSurface.Create(Brushes.Transparent, Brushes.Transparent, new(1), new(8));
    private readonly ButtonLabelPanel _labelPanel = new();
    private readonly FocusOutline _outline;
    private readonly ScaleTransform _activeScale = new(1, 1);
    private WebMotionPlayback? _press;
    private string _variant = "base";
    private bool _filter, _selected, _reduced, _focusVisible;
    private Appearance _paint;
    private readonly CssTransition<CssColor> _background = new(MixPrecise, 120);
    private readonly CssTransition<CssColor> _color = new(MixPrecise, 120);
    private readonly CssTransition<CssShadow> _shadow = new(MixShadow, 120);
    private readonly CssTransition<double> _scale = new((a, b, t) => a + (b - a) * t, 80);
    private long _revision;

    public WebActionButton()
    {
        UseLayoutRounding = false; ClipToBounds = false;
        HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Left;
        VerticalAlignment = Avalonia.Layout.VerticalAlignment.Top;
        Padding = new(16, 10); MinHeight = 44; BorderThickness = new(1);
        FocusAdorner = null;
        RenderTransformOrigin = WebMotion.OriginFor(WebMotionKind.Press);
        RenderTransform = _activeScale;
        _surface.Child = _labelPanel;
        _outline = new FocusOutline { Child = _surface, UseLayoutRounding = false };
        Template = new FuncControlTemplate<WebActionButton>((_, _) => _outline);
        UpdateContent(); ApplyState(false);
    }

    protected override AutomationPeer OnCreateAutomationPeer() => OperatingSystem.IsWindows() ? new LeafButtonPeer(this) : base.OnCreateAutomationPeer();
    private sealed class LeafButtonPeer(WebActionButton button) : ButtonAutomationPeer(button)
    {
        protected override IReadOnlyList<AutomationPeer>? GetChildrenCore() => [];
    }

    public string Variant
    {
        get => _variant;
        set
        {
            if (value is not ("base" or "primary" or "quiet" or "danger")) throw new ArgumentOutOfRangeException(nameof(value));
            _variant = value; MinHeight = value == "primary" ? 48 : 44; MinWidth = value == "primary" ? 164 : 0; ApplyState(false);
        }
    }
    public string IconName { get => _labelPanel.IconName; set { _labelPanel.IconName = value; InvalidateMeasure(); } }
    public bool IsLoading { get => _labelPanel.Loading; set { _labelPanel.Loading = value; _labelPanel.Spinner.ReducedMotion = _reduced; InvalidateMeasure(); } }
    public bool Filter { get => _filter; set { _filter = value; _labelPanel.Label.FontWeight = (FontWeight)(value ? 500 : 600); UpdateContent(); ApplyState(false); } }
    public bool Selected { get => _selected; set { _selected = value; ApplyState(); } }
    public bool ReducedMotion { get => _reduced; set { _reduced = value; _labelPanel.Spinner.ReducedMotion = value; if (value) { _press?.Dispose(); _press = null; } ApplyState(false); } }
    public double LabelFontSize { get => _labelPanel.Label.FontSize; set { _labelPanel.Label.FontSize = value; _labelPanel.Label.LineHeight = value * 1.6; InvalidateMeasure(); } }
    public int LabelWeight { get => (int)_labelPanel.Label.FontWeight; set { _labelPanel.Label.FontWeight = (FontWeight)value; InvalidateMeasure(); } }
    public bool AbsoluteTrailingIcon { get => _labelPanel.AbsoluteTrailing; set { _labelPanel.AbsoluteTrailing = value; InvalidateMeasure(); } }
    public double IconRightInset { get => _labelPanel.RightInset; set { _labelPanel.RightInset = value; InvalidateArrange(); } }
    // The Home file-entry overrides are supplied by its parent, never inferred from Content/action.
    public Color? RestingBackgroundOverride { get; set; }
    public Color? BorderColorOverride { get; set; }
    public Color? HoverBackgroundOverride { get; set; }
    public bool ParentHoverIncludesDisabled { get; set; }
    public void RefreshParentStyle() => ApplyState(false);

    private void UpdateContent()
    {
        _labelPanel.Label.Text = Content as string ?? "";
        if (_filter) _labelPanel.Label.FontWeight = (FontWeight)500;
        AutomationProperties.SetName(this, _labelPanel.Label.Text);
        InvalidateMeasure();
    }
    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (_outline is null) return;
        if (change.Property == ContentProperty) UpdateContent();
        if (change.Property == PaddingProperty) { _surface.Padding = Padding; _labelPanel.ParentPadding = Padding; InvalidateMeasure(); }
        if (change.Property == IsPointerOverProperty || change.Property == IsPressedProperty || change.Property == IsEffectivelyEnabledProperty) ApplyState();
    }
    protected override void OnGotFocus(FocusChangedEventArgs e)
    {
        base.OnGotFocus(e);
        _focusVisible = e.NavigationMethod != NavigationMethod.Pointer;
        _outline.Visible = _focusVisible;
    }
    protected override void OnLostFocus(FocusChangedEventArgs e)
    {
        base.OnLostFocus(e); _focusVisible = false; _outline.Visible = false;
    }
    protected override void OnKeyDown(KeyEventArgs e)
    {
        if (e.Key is Key.Space or Key.Enter) { _focusVisible = true; _outline.Visible = true; }
        base.OnKeyDown(e);
    }
    protected override void OnClick()
    {
        if (!IsEffectivelyEnabled) return;
        if (!_reduced && TopLevel.GetTopLevel(this) is not null)
        {
            // No delay before the action; the source starts its effect and dispatches immediately.
            _press?.Dispose();
            _press = WebMotion.Play(this, WebMotionKind.Press);
        }
        base.OnClick();
    }

    private Appearance Resolve() => Resolve(IsPointerOver, IsPressed);
    private Appearance Resolve(bool pointerOver, bool isPressed)
    {
        var disabled = !IsEffectivelyEnabled;
        var hover = pointerOver && !disabled;
        var pressed = isPressed && !disabled;
        string bg = hover ? "#45474f" : "#383a40", color = "#dbdee1", border = "#7c838c";
        var shadow = hover ? BoxShadows.Parse("0 2 5 #18000000") : default;
        if (disabled) { bg = "#3b3d44"; color = "#b5bac1"; border = "#5d636d"; shadow = default; }
        switch (_variant)
        {
            case "primary":
                if (!disabled) { bg = pressed ? "#3c45a5" : hover ? "#4752c4" : "#5865f2"; color = "#ffffff"; border = "#5865f2"; shadow = BoxShadows.Parse(hover ? "0 2 5 #18000000" : "0 2 3 #24000000"); }
                break;
            // Specificity retains these variant colors even for disabled quiet/danger.
            case "quiet": bg = hover ? "#3c3f46" : "#00000000"; border = "#00000000"; color = hover ? "#f2f3f5" : "#b5bac1"; break;
            case "danger": bg = hover ? "#4a3038" : "#00000000"; border = "#94616a"; color = "#ff959d"; break;
        }
        if (_filter)
        {
            border = "#00000000"; color = "#b5bac1";
            if (_selected) { bg = "#3b3d52"; color = "#c6ccff"; }
        }
        if (RestingBackgroundOverride is Color resting) bg = resting.ToString();
        if (BorderColorOverride is Color edge) border = edge.ToString();
        if (HoverBackgroundOverride is Color over && pointerOver && (!disabled || ParentHoverIncludesDisabled)) bg = over.ToString();
        return new(Color.Parse(bg), Color.Parse(color), Color.Parse(border), shadow, pressed ? .98 : 1);
    }
    private void ApplyState(bool animate = true)
    {
        if (_outline is null) return;
        _surface.Padding = Padding;
        _labelPanel.ParentPadding = Padding;
        Cursor = new Cursor(IsEffectivelyEnabled ? StandardCursorType.Hand : StandardCursorType.No);
        var target = Resolve(); var revision = ++_revision;
        // Border color has no CSS transition.
        _surface.BorderBrush = new SolidColorBrush(target.Border);
        if (!animate || _reduced || TopLevel.GetTopLevel(this) is not { } top)
        {
            _background.Snap(CssColor.FromColor(target.Background)); _color.Snap(CssColor.FromColor(target.Color)); _shadow.Snap(CssShadow.FromShadow(target.Shadow)); _scale.Snap(target.Scale);
            Paint(target); return;
        }
        _background.SetTarget(CssColor.FromColor(target.Background)); _color.SetTarget(CssColor.FromColor(target.Color)); _shadow.SetTarget(CssShadow.FromShadow(target.Shadow)); _scale.SetTarget(target.Scale);
        void Frame(TimeSpan _)
        {
            if (revision != _revision || !this.IsAttachedToVisualTree()) return;
            Paint(new(_background.Current.ToColor(), _color.Current.ToColor(), target.Border, _shadow.Current.ToShadow(), _scale.Current));
            if (_background.Running || _color.Running || _shadow.Running || _scale.Running) top.RequestAnimationFrame(Frame);
        }
        top.RequestAnimationFrame(Frame);
    }
    private void Paint(Appearance value)
    {
        _paint = value; _surface.Background = new SolidColorBrush(value.Background); _surface.BorderBrush = new SolidColorBrush(value.Border); _surface.BoxShadow = value.Shadow;
        _labelPanel.Label.Foreground = new SolidColorBrush(value.Color); _labelPanel.Icon.Stroke = _labelPanel.Label.Foreground; _labelPanel.Spinner.CurrentColor = _labelPanel.Label.Foreground;
        _activeScale.ScaleX = _activeScale.ScaleY = value.Scale;
    }
    private static CssColor MixPrecise(CssColor a, CssColor b, double t)
    {
        var alpha = a.A + (b.A - a.A) * t;
        double Channel(double x, double y) => alpha == 0 ? 0 : (x * a.A * (1 - t) + y * b.A * t) / alpha;
        return new(alpha, Channel(a.R, b.R), Channel(a.G, b.G), Channel(a.B, b.B));
    }
    private static CssShadow MixShadow(CssShadow a, CssShadow b, double t)
        => t <= 0 ? a : t >= 1 ? b : new(a.Present || b.Present, a.X + (b.X - a.X) * t, a.Y + (b.Y - a.Y) * t,
            a.Blur + (b.Blur - a.Blur) * t, MixPrecise(a.Color, b.Color, t));
    private readonly record struct CssShadow(bool Present, double X, double Y, double Blur, CssColor Color)
    {
        // Source uses one shadow. CSS pairs none with transparent, zero-length geometry.
        public static CssShadow FromShadow(BoxShadows value) => value.Count == 0 ? default
            : new(true, value[0].OffsetX, value[0].OffsetY, value[0].Blur, CssColor.FromColor(value[0].Color));
        public BoxShadows ToShadow() => !Present ? default
            : new(new BoxShadow { OffsetX = X, OffsetY = Y, Blur = Blur, Color = Color.ToColor() });
    }
    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    { ++_revision; _press?.Dispose(); _press = null; base.OnDetachedFromVisualTree(e); }
    private readonly record struct Appearance(Color Background, Color Color, Color Border, BoxShadows Shadow, double Scale);
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

    private sealed class ButtonLabelPanel : Panel
    {
        private double _flexWidth;
        public bool AbsoluteTrailing { get; set; }
        public double RightInset { get; set; } = 18;
        public Thickness ParentPadding { get; set; }
        public WebSystemTextBlock Label { get; } = WebTypography.CreateCandidate("", 13, 20.8, weight:600);
        public SvgIcon Icon { get; } = new() { Width = 18, Height = 18 };
        public WebSpinner Spinner { get; } = new();
        private string _iconName = "";
        private bool _loading;
        public string IconName { get => _iconName; set { _iconName = value; if (value.Length > 0) Icon.IconName = value; Icon.IsVisible = !_loading && value.Length > 0; InvalidateMeasure(); } }
        public bool Loading
        {
            get => _loading;
            set
            {
                if (_loading == value) return;
                _loading = value;
                if (value) Children.Add(Spinner); else Children.Remove(Spinner);
                Icon.IsVisible = !value && _iconName.Length > 0; InvalidateMeasure();
            }
        }
        private Control? Leading => _loading ? Spinner : Icon.IsVisible ? Icon : null;
        private double LeadingWidth => _loading ? 16 : Icon.IsVisible ? 18 : 0;
        public ButtonLabelPanel() { UseLayoutRounding = false; Icon.IsVisible = false; Children.Add(Icon); Children.Add(Label); Label.TextAlignment = TextAlignment.Center; }
        protected override Size MeasureOverride(Size availableSize)
        {
            var units = 64 * (TopLevel.GetTopLevel(this)?.RenderScaling ?? 2);
            // The button inherits unitless line-height:1.6; Blink truncates its resolved layout contribution.
            Label.LineHeight = Math.Floor(Label.FontSize * 1.6 * units) / units;
            Leading?.Measure(new(LeadingWidth, LeadingWidth));
            var fixedWidth = Leading is not null && !AbsoluteTrailing ? LeadingWidth + 9 : 0;
            Label.Measure(new(double.PositiveInfinity, availableSize.Height));
            // Blink's intrinsic flex contribution uses its fixed-point layout units.
            _flexWidth = Math.Min(Math.Ceiling(Label.DesiredSize.Width * units) / units, Math.Max(0, availableSize.Width - fixedWidth));
            Label.Measure(new(_flexWidth, availableSize.Height));
            return new(_flexWidth + fixedWidth, Math.Max(Label.DesiredSize.Height, LeadingWidth));
        }
        protected override Size ArrangeOverride(Size finalSize)
        {
            var fixedWidth = Leading is not null && !AbsoluteTrailing ? LeadingWidth + 9 : 0;
            var width = Math.Min(_flexWidth, Math.Max(0, finalSize.Width - fixedWidth));
            var units = 64 * (TopLevel.GetTopLevel(this)?.RenderScaling ?? 2);
            double Center(double space) => Math.Floor(space * units / 2) / units;
            var x = Center(finalSize.Width - width - fixedWidth);
            if (Leading is Control leading) leading.Arrange(new(AbsoluteTrailing ? finalSize.Width + ParentPadding.Right - RightInset - LeadingWidth : x, (finalSize.Height - LeadingWidth) / 2, LeadingWidth, LeadingWidth));
            Label.Arrange(new(x + fixedWidth, Center(finalSize.Height - Label.DesiredSize.Height), width, Label.DesiredSize.Height));
            return finalSize;
        }
    }
    private sealed class FocusOutline : Decorator
    {
        private bool _visible;
        public bool Visible { get => _visible; set { _visible = value; InvalidateVisual(); } }
        public override void Render(DrawingContext context)
        {
            if (_visible) context.DrawRectangle(null, new Pen(Brush.Parse("#a7b1ff"), 3), new Rect(Bounds.Size).Inflate(4.5), 12.5, 12.5);
        }
    }
}
