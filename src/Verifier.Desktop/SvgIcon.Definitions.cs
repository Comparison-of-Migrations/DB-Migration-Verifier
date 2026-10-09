using Avalonia;
using Avalonia.Media;

namespace Verifier.Desktop;

public sealed partial class SvgIcon
{
    // Exact geometry from the captured web icons; circles/ellipses/rects remain separate figures.
    private static readonly IReadOnlyDictionary<string, Geometry[]> Definitions = new Dictionary<string, Geometry[]>
    {
        ["search"] = [new EllipseGeometry(new Rect(4, 4, 12, 12)), StreamGeometry.Parse("m15 15 6 6")],
        ["layers"] = [StreamGeometry.Parse("m12 3 10 6-10 6L2 9l10-6ZM2 13l10 6 10-6M2 17l10 6 10-6")],
        ["grid"] = [StreamGeometry.Parse("M6 3 H18 A3 3 0 0 1 21 6 V18 A3 3 0 0 1 18 21 H6 A3 3 0 0 1 3 18 V6 A3 3 0 0 1 6 3 Z"), StreamGeometry.Parse("M3 9h18M9 9v12")],
        ["db"] = [new EllipseGeometry(new Rect(4, 2, 16, 6)), StreamGeometry.Parse("M4 5v14c0 1.7 3.6 3 8 3s8-1.3 8-3V5M4 12c0 1.7 3.6 3 8 3s8-1.3 8-3")],
        ["arrow"] = [StreamGeometry.Parse("M5 12h14m-6-6 6 6-6 6")],
        ["back"] = [StreamGeometry.Parse("M19 12H5m6-6-6 6 6 6")],
        ["chevron"] = [StreamGeometry.Parse("m9 5 7 7-7 7")],
        ["check"] = [StreamGeometry.Parse("m5 12 4 4L19 6")],
        ["info"] = [new EllipseGeometry(new Rect(3, 3, 18, 18)), StreamGeometry.Parse("M12 11v6m0-10v.1")],
        ["shield"] = [StreamGeometry.Parse("m12 3 8 3v6c0 5-8 9-8 9S4 17 4 12V6l8-3Z"), StreamGeometry.Parse("m8 12 3 3 5-6")],
        ["file"] = [StreamGeometry.Parse("M14 3H5v18h14V8l-5-5Z"), StreamGeometry.Parse("M14 3v5h5M8 12h8m-8 4h6")],
        ["close"] = [StreamGeometry.Parse("m6 6 12 12M6 18 18 6")],
        ["warning"] = [StreamGeometry.Parse("m12 3 10 18H2L12 3Z"), StreamGeometry.Parse("M12 9v5m0 3v.1")],
        ["save"] = [StreamGeometry.Parse("M4 3h14l3 3v15H3V3h1Z"), StreamGeometry.Parse("M7 3v6h10V3M7 21v-8h10v8")],
        ["swap"] = [StreamGeometry.Parse("M4 8h15m-4-4 4 4-4 4M20 16H5m4-4-4 4 4 4")],
        ["play"] = [StreamGeometry.Parse("m7 4 14 8-14 8V4Z")],
        ["stop"] = [StreamGeometry.Parse("M7 5 H17 A2 2 0 0 1 19 7 V17 A2 2 0 0 1 17 19 H7 A2 2 0 0 1 5 17 V7 A2 2 0 0 1 7 5 Z")],
        ["refresh"] = [StreamGeometry.Parse("M20 7v6h-6M4 17v-6h6M20 12a8 8 0 0 0-14-5M4 12a8 8 0 0 0 14 5")],
    };
}
