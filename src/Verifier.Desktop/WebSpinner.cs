using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.VisualTree;

namespace Verifier.Desktop;

/// <summary>Decorative 16px source spinner. The parent supplies currentColor and reduced motion.</summary>
public sealed class WebSpinner : Control
{
    private static readonly Geometry Ring = Geometry.Parse(
        "M13.65685424949238 13.65685424949238 A8 8 0 1 1 13.65685424949238 2.34314575050762 " +
        "L12.242640687119286 3.757359312880715 A6 6 0 1 0 12.242640687119286 12.242640687119286 Z");
    private IBrush _color = Brush.Parse("#dbdee1");
    private bool _reduced;
    private WebMotionPlayback? _playback;

    public WebSpinner()
    {
        Width = Height = 16;
        UseLayoutRounding = false;
        RenderTransformOrigin = WebMotion.OriginFor(WebMotionKind.Spinner);
    }
    public IBrush CurrentColor { get => _color; set { _color = value; InvalidateVisual(); } }
    public bool ReducedMotion
    {
        get => _reduced;
        set
        {
            _playback?.Dispose(); _playback = null;
            _reduced = value; Opacity = value ? .7 : 1;
            RenderTransform = null;
            InvalidateVisual();
            if (this.IsAttachedToVisualTree()) Start();
        }
    }
    public override void Render(DrawingContext context)
    {
        base.Render(context);
        if (_reduced) context.DrawEllipse(_color, null, new Point(8, 8), 8, 8);
        else context.DrawGeometry(_color, null, Ring);
    }
    private void Start()
    {
        _playback?.Dispose();
        _playback = WebMotion.Play(this, WebMotionKind.Spinner, _reduced);
    }
    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    { base.OnAttachedToVisualTree(e); Start(); }
    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    { _playback?.Dispose(); _playback = null; base.OnDetachedFromVisualTree(e); }
}
