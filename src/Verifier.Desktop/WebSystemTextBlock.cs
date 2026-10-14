using Avalonia;
using Avalonia.Controls;
using Avalonia.Automation.Peers;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.VisualTree;

namespace Verifier.Desktop;

/// <summary>Plain system-font text. Editing/inline text are separate components.</summary>
public sealed class WebSystemTextBlock : Decorator
{
    public static readonly StyledProperty<string?> TextProperty = TextBlock.TextProperty.AddOwner<WebSystemTextBlock>();
    public static readonly StyledProperty<FontFamily> FontFamilyProperty = TextBlock.FontFamilyProperty.AddOwner<WebSystemTextBlock>();
    public static readonly StyledProperty<double> FontSizeProperty = TextBlock.FontSizeProperty.AddOwner<WebSystemTextBlock>();
    public static readonly StyledProperty<FontWeight> FontWeightProperty = TextBlock.FontWeightProperty.AddOwner<WebSystemTextBlock>();
    public static readonly StyledProperty<double> LetterSpacingProperty = TextBlock.LetterSpacingProperty.AddOwner<WebSystemTextBlock>();
    public static readonly StyledProperty<double> LineHeightProperty = TextBlock.LineHeightProperty.AddOwner<WebSystemTextBlock>();
    public static readonly StyledProperty<IBrush?> ForegroundProperty = TextBlock.ForegroundProperty.AddOwner<WebSystemTextBlock>();
    public static readonly StyledProperty<TextWrapping> TextWrappingProperty = TextBlock.TextWrappingProperty.AddOwner<WebSystemTextBlock>();
    public static readonly StyledProperty<TextAlignment> TextAlignmentProperty = TextBlock.TextAlignmentProperty.AddOwner<WebSystemTextBlock>();
    public static readonly StyledProperty<bool> TabularNumbersProperty = AvaloniaProperty.Register<WebSystemTextBlock, bool>(nameof(TabularNumbers));
    public string? Text { get => GetValue(TextProperty); set => SetValue(TextProperty, value); }
    public FontFamily FontFamily { get => GetValue(FontFamilyProperty); set => SetValue(FontFamilyProperty, value); }
    public double FontSize { get => GetValue(FontSizeProperty); set => SetValue(FontSizeProperty, value); }
    public FontWeight FontWeight { get => GetValue(FontWeightProperty); set => SetValue(FontWeightProperty, value); }
    public double LetterSpacing { get => GetValue(LetterSpacingProperty); set => SetValue(LetterSpacingProperty, value); }
    public double LineHeight { get => GetValue(LineHeightProperty); set => SetValue(LineHeightProperty, value); }
    public IBrush? Foreground { get => GetValue(ForegroundProperty); set => SetValue(ForegroundProperty, value); }
    public TextWrapping TextWrapping { get => GetValue(TextWrappingProperty); set => SetValue(TextWrappingProperty, value); }
    public TextAlignment TextAlignment { get => GetValue(TextAlignmentProperty); set => SetValue(TextAlignmentProperty, value); }
    public bool TabularNumbers { get => GetValue(TabularNumbersProperty); set => SetValue(TabularNumbersProperty, value); }
    static WebSystemTextBlock() => AffectsRender<WebSystemTextBlock>(ForegroundProperty, TextAlignmentProperty);
    private MacSystemTextLayout? _layout;
    private LayoutKey? _layoutKey;
    private WriteableBitmap? _bitmap;
    private RasterKey? _rasterKey;
    private double _rasterOriginX;
    private double _rasterOriginY;
    private bool IsMonospaced => FontFamily.Name == WebTypography.MonospaceFontFamily.Name;
    private bool UsesMacSystem => OperatingSystem.IsMacOS() && (FontFamily.Name == WebTypography.SystemFontFamily.Name || IsMonospaced);
    public IReadOnlyList<double> SystemLineWidths => string.IsNullOrEmpty(WebTextWhitespace.Collapse(Text ?? "")) ? Array.Empty<double>() : _layout?.LineWidths ??
        (Child as WindowsWebTextBlock)?.CssLineWidths ??
        (Child as TextBlock)?.TextLayout.TextLines.Select(line => line.Width).ToArray() ?? Array.Empty<double>();
    public IReadOnlyList<string> SystemLineTexts => _layout?.LineTexts ??
        (Child as TextBlock)?.TextLayout.TextLines.Select(line => string.Concat(line.TextRuns.OfType<Avalonia.Media.TextFormatting.ShapedTextRun>().Select(run => run.Text.ToString())).TrimEnd('\n', ' ')).ToArray() ?? Array.Empty<string>();
    public double SystemBaseline => _layout?.GetBaseline(TopLevel.GetTopLevel(this)?.RenderScaling ?? 2) ??
        (Child is WindowsWebTextBlock windows ? windows.CssBaseline(TopLevel.GetTopLevel(this)?.RenderScaling ?? 2) :
            (Child as TextBlock)?.TextLayout.TextLines.FirstOrDefault()?.Baseline ?? 0);

    protected override Size MeasureOverride(Size availableSize)
    {
        if (!UsesMacSystem)
        {
            var text = Child as TextBlock ?? (OperatingSystem.IsWindows() ? new WindowsWebTextBlock { ClipToBounds = false } : new TextBlock());
            Child = text;
            text.Text = WebTextWhitespace.Collapse(Text ?? ""); text.FontFamily = FontFamily; text.FontSize = FontSize; text.FontWeight = FontWeight;
            text.LetterSpacing = LetterSpacing; text.Foreground = Foreground;
            if (text is WindowsWebTextBlock windows)
            {
                var scale = TopLevel.GetTopLevel(this)?.RenderScaling ?? 2;
                windows.CssLineHeight = LineHeight;
                text.LineHeight = Math.Floor(LineHeight * scale * 64 + .5) / (scale * 64);
            }
            else text.LineHeight = LineHeight;
            text.FontFeatures = TabularNumbers ? FontFeatureCollection.Parse("tnum") : null;
            text.TextWrapping = TextWrapping; text.TextAlignment = TextAlignment; text.UseLayoutRounding = false;
            return base.MeasureOverride(availableSize);
        }
        Child = null;
        return Layout(availableSize.Width).Size;
    }
    public override void Render(DrawingContext context)
    {
        if (!UsesMacSystem) { base.Render(context); return; }
        if (Bounds.Width <= 0 || Bounds.Height <= 0 || string.IsNullOrEmpty(Text)) return;
        var layout = Layout(Bounds.Width);
        // Source uses solid inherited text colors. Keep foreground opacity distinct from control opacity.
        if (Foreground is not ISolidColorBrush foreground)
            throw new NotSupportedException("System text requires the source's solid foreground.");
        var scale = TopLevel.GetTopLevel(this)?.RenderScaling ?? 2;
        var phase = DevicePhase(scale);
        var key = new RasterKey(Bounds.Size, foreground.Color, foreground.Opacity, TextAlignment, scale, phase);
        if (_bitmap is null || _rasterKey != key)
        {
            _bitmap?.Dispose();
            _bitmap = layout.Raster(Bounds.Size, foreground.Color, foreground.Opacity, TextAlignment, scale, phase, out _rasterOriginX, out _rasterOriginY);
            _rasterKey = key;
        }
        // Raster allocation rounds UP to device pixels; don't stretch that extra fraction into CSS bounds.
        context.DrawImage(_bitmap, new Rect(0, 0, _bitmap.PixelSize.Width, _bitmap.PixelSize.Height),
            new Rect(_rasterOriginX, _rasterOriginY, _bitmap.PixelSize.Width / scale, _bitmap.PixelSize.Height / scale));
    }
    private MacSystemTextLayout Layout(double width)
    {
        var wrapWidth = TextWrapping == TextWrapping.NoWrap || double.IsPositiveInfinity(width) ? double.MaxValue : Math.Max(0, width);
        var key = new LayoutKey(WebTextWhitespace.Collapse(Text ?? ""), FontSize, (int)FontWeight, LetterSpacing, LineHeight, wrapWidth, IsMonospaced, TabularNumbers, TextWrapping == TextWrapping.Wrap, TopLevel.GetTopLevel(this)?.RenderScaling ?? 2);
        if (_layout is null || _layoutKey != key)
        {
            _layout?.Dispose(); _bitmap?.Dispose(); _bitmap = null; _rasterKey = null;
            _layout = new MacSystemTextLayout(key.Text, key.Size, key.Weight, key.Spacing, key.LineHeight, key.Width, key.Monospaced, key.Tabular, key.Anywhere, key.Scale);
            _layoutKey = key;
        }
        return _layout;
    }
    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == TextProperty || change.Property == FontSizeProperty || change.Property == FontWeightProperty ||
            change.Property == FontFamilyProperty || change.Property == LetterSpacingProperty || change.Property == LineHeightProperty ||
            change.Property == TextWrappingProperty || change.Property == TabularNumbersProperty)
        {
            InvalidateMeasure(); InvalidateVisual();
        }
        else if (change.Property == ForegroundProperty || change.Property == TextAlignmentProperty)
        {
            if (Child is TextBlock child) { child.Foreground = Foreground; child.TextAlignment = TextAlignment; }
            InvalidateVisual();
        }
    }
    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        LayoutUpdated -= LayoutGeometryChanged;
        _layout?.Dispose(); _layout = null; _layoutKey = null;
        _bitmap?.Dispose(); _bitmap = null; _rasterKey = null;
        base.OnDetachedFromVisualTree(e);
    }
    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        LayoutUpdated += LayoutGeometryChanged;
    }
    private Point DevicePhase(double scale)
    {
        // Layout offsets affect glyph raster phase; CSS render transforms compose the finished raster.
        var origin = default(Point);
        for (Visual? visual = this; visual is not null && visual is not TopLevel; visual = visual.GetVisualParent())
            origin += visual.Bounds.Position;
        static double Fraction(double value) => value - Math.Floor(value);
        return new Point(Fraction(origin.X * scale) / scale, Fraction(origin.Y * scale) / scale);
    }
    private void LayoutGeometryChanged(object? sender, EventArgs e)
    {
        if (!UsesMacSystem) return;
        var scale = TopLevel.GetTopLevel(this)?.RenderScaling ?? 2;
        if (_layoutKey is not null && _layoutKey.Scale != scale) InvalidateMeasure();
        if (_rasterKey is not null && (_rasterKey.Scale != scale || _rasterKey.Phase != DevicePhase(scale)))
            InvalidateVisual();
    }
    private sealed record LayoutKey(string Text, double Size, int Weight, double Spacing, double LineHeight, double Width, bool Monospaced, bool Tabular, bool Anywhere, double Scale);
    private sealed record RasterKey(Size Bounds, Color Color, double Opacity, TextAlignment Alignment, double Scale, Point Phase);
    protected override AutomationPeer OnCreateAutomationPeer() => new SystemTextAutomationPeer(this);
    private sealed class SystemTextAutomationPeer(WebSystemTextBlock owner) : ControlAutomationPeer(owner)
    {
        protected override string GetNameCore()
        {
            var explicitName=OperatingSystem.IsWindows()?Avalonia.Automation.AutomationProperties.GetName(owner):null;
            return string.IsNullOrEmpty(explicitName)?owner.Text??"":explicitName;
        }
        protected override AutomationControlType GetAutomationControlTypeCore() => OperatingSystem.IsMacOS() && Avalonia.Automation.AutomationProperties.GetHeadingLevel(owner)>0 ? AutomationControlType.Header : AutomationControlType.Text;
        protected override IReadOnlyList<AutomationPeer>? GetChildrenCore() => OperatingSystem.IsWindows() ? Array.Empty<AutomationPeer>() : base.GetChildrenCore();
    }
}
