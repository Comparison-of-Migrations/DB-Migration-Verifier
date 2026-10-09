using Avalonia;
using Avalonia.Animation;
using Avalonia.Animation.Easings;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Styling;

namespace Verifier.Desktop;

public enum WebMotionKind { Page, Modal, Drawer, Toast, ResultNumber, ResultBar, Press, ModalClose, SearchFade, RunningDot, Spinner }

/// <summary>Isolated source animation candidate. Parent state changes are assembled separately.</summary>
public static class WebMotion
{
    public static WebMotionPlayback Play(Control target, WebMotionKind kind, bool reducedMotion = false,
        CancellationToken cancellation = default) => new(target, Create(kind, reducedMotion), true, cancellation);
    public static RelativePoint OriginFor(WebMotionKind kind) => kind == WebMotionKind.ResultBar
        ? new RelativePoint(0, .5, RelativeUnit.Relative) : RelativePoint.Center;
    public static Easing SourceEase() => new WebCubicBezierEasing(.22, 1, .36, 1);
    public static Easing CssEase() => new WebCubicBezierEasing(.25, .1, .25, 1);
    public static Easing EaseOut() => new WebCubicBezierEasing(0, 0, .58, 1);
    public static Easing EaseInOut() => new WebCubicBezierEasing(.42, 0, .58, 1);

    // Reduced motion skips the effect; the caller still completes its state change.
    public static Animation? Create(WebMotionKind kind, bool reducedMotion = false)
    {
        if (reducedMotion) return null;
        var animation = new Animation { Easing = SourceEase(), FillMode = FillMode.None };
        void Frame(double cue, double? opacity = null, double? x = null, double? y = null, double? scaleX = null, double? scaleY = null, double? angle = null)
        {
            var frame = new KeyFrame { Cue = new Cue(cue) };
            if (opacity is double value) frame.Setters.Add(new Setter(Visual.OpacityProperty, value));
            if (x is double dx) frame.Setters.Add(new Setter(TranslateTransform.XProperty, dx));
            if (y is double dy) frame.Setters.Add(new Setter(TranslateTransform.YProperty, dy));
            if (scaleX is double sx) frame.Setters.Add(new Setter(ScaleTransform.ScaleXProperty, sx));
            if (scaleY is double sy) frame.Setters.Add(new Setter(ScaleTransform.ScaleYProperty, sy));
            if (angle is double a) frame.Setters.Add(new Setter(RotateTransform.AngleProperty, a));
            animation.Children.Add(frame);
        }
        var duration = kind switch
        {
            WebMotionKind.Modal or WebMotionKind.Drawer => 280,
            WebMotionKind.ResultNumber => 220,
            WebMotionKind.ResultBar => 320,
            WebMotionKind.Press or WebMotionKind.ModalClose or WebMotionKind.SearchFade => 160,
            WebMotionKind.RunningDot => 1000,
            WebMotionKind.Spinner => 800,
            _ => 200
        };
        animation.Duration = TimeSpan.FromMilliseconds(duration);
        switch (kind)
        {
            case WebMotionKind.Page: Frame(0, .35, y:6); Frame(1, 1, y:0); break;
            case WebMotionKind.Modal:
            case WebMotionKind.Toast: Frame(0, 0, y:10); Frame(1, 1, y:0); break;
            case WebMotionKind.Drawer: Frame(0, 0, x:20); Frame(1, 1, x:0); break;
            case WebMotionKind.ResultNumber: Frame(0, .6, y:5); Frame(1, 1, y:0); break;
            case WebMotionKind.ResultBar: Frame(0, scaleX:0); Frame(1, scaleX:1); break;
            case WebMotionKind.Press:
                animation.Easing = EaseOut(); Frame(0, 1, scaleX:1, scaleY:1); Frame(.5, .86, scaleX:.98, scaleY:.98); Frame(1, 1, scaleX:1, scaleY:1); break;
            case WebMotionKind.ModalClose:
                animation.Easing = EaseOut(); animation.FillMode = FillMode.Forward; Frame(0, 1, y:0); Frame(1, 0, y:6); break;
            case WebMotionKind.SearchFade: animation.Easing = EaseOut(); Frame(0, .5); Frame(1, 1); break;
            case WebMotionKind.RunningDot:
                animation.Easing = new WebPulseEasing(); animation.IterationCount = IterationCount.Infinite;
                Frame(0, 1); Frame(.5, .3); Frame(1, 1); break;
            case WebMotionKind.Spinner:
                animation.Easing = new LinearEasing(); animation.IterationCount = IterationCount.Infinite;
                Frame(0, angle:0); Frame(1, angle:360); break;
        }
        return animation;
    }
}
