using Avalonia.Animation.Easings;

namespace Verifier.Desktop;

/// <summary>CSS timing curve, solved in x rather than treating time as the curve parameter.</summary>
public sealed class WebCubicBezierEasing(double x1, double y1, double x2, double y2) : Easing
{
    public override double Ease(double progress)
    {
        if (progress <= 0) return 0;
        if (progress >= 1) return 1;
        static double Curve(double t, double a, double b) => 3 * (1 - t) * (1 - t) * t * a + 3 * (1 - t) * t * t * b + t * t * t;
        double low = 0, high = 1;
        for (var i = 0; i < 40; i++)
        {
            var middle = (low + high) / 2;
            if (Curve(middle, x1, x2) < progress) low = middle;
            else high = middle;
        }
        return Curve((low + high) / 2, y1, y2);
    }
}

internal sealed class WebPulseEasing : Easing
{
    private readonly Easing _segment = WebMotion.EaseInOut();
    // CSS keyframe timing applies inside each interval; WAAPI effect easing is global.
    public override double Ease(double progress) => progress <= .5
        ? .5 * _segment.Ease(progress * 2)
        : .5 + .5 * _segment.Ease((progress - .5) * 2);
}
