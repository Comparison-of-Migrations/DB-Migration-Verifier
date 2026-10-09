using Avalonia;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Controls.Documents;
using Avalonia.Media;

namespace Verifier.Desktop;

/// <summary>The original web's decorative SVG geometry in an unchanged 24×24 viewBox.</summary>
public sealed partial class SvgIcon : Control
{
    public static readonly StyledProperty<string> IconNameProperty =
        AvaloniaProperty.Register<SvgIcon, string>(nameof(IconName), "info");
    public static readonly StyledProperty<IBrush?> StrokeProperty =
        AvaloniaProperty.Register<SvgIcon, IBrush?>(nameof(Stroke));
    public static readonly StyledProperty<double> StrokeWidthProperty =
        AvaloniaProperty.Register<SvgIcon, double>(nameof(StrokeWidth), 1.7);

    static SvgIcon() => AffectsRender<SvgIcon>(IconNameProperty, StrokeProperty,
        StrokeWidthProperty, TextElement.ForegroundProperty);

    public SvgIcon()
    {
        UseLayoutRounding = false;
        Focusable = false;
        IsHitTestVisible = false;
        AutomationProperties.SetAccessibilityView(this, AccessibilityView.Raw);
    }

    public string IconName
    {
        get => GetValue(IconNameProperty);
        set => SetValue(IconNameProperty, value);
    }
    public IBrush? Stroke
    {
        get => GetValue(StrokeProperty);
        set => SetValue(StrokeProperty, value);
    }
    public double StrokeWidth
    {
        get => GetValue(StrokeWidthProperty);
        set => SetValue(StrokeWidthProperty, value);
    }

    protected override Size MeasureOverride(Size availableSize) => new(20, 20);

    public override void Render(DrawingContext context)
    {
        base.Render(context);
        if (Bounds.Width <= 0 || Bounds.Height <= 0) return;
        var scale = Math.Min(Bounds.Width, Bounds.Height) / 24;
        var offset = new Vector((Bounds.Width - 24 * scale) / 2,
            (Bounds.Height - 24 * scale) / 2);
        // SVG's default preserveAspectRatio is xMidYMid meet. Stroke scales with the viewBox.
        using var transform = context.PushTransform(Matrix.CreateScale(scale, scale) *
            Matrix.CreateTranslation(offset));
        var pen = new Pen(Stroke ?? TextElement.GetForeground(this), StrokeWidth,
            lineCap: PenLineCap.Round, lineJoin: PenLineJoin.Round);
        var figures = Definitions.TryGetValue(IconName ?? "", out var found) ? found : Definitions["info"];
        foreach (var figure in figures) context.DrawGeometry(null, pen, figure);
    }
}
