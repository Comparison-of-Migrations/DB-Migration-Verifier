using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Media.TextFormatting;
using Avalonia.Media.TextFormatting.Unicode;
using SkiaSharp;
using System.Globalization;
using System.Text;

namespace Verifier.Desktop;

// Plain CSS text only: preserve Avalonia shaping, place each shaped line
// at the browser's rounded primary DirectWrite baseline through public APIs.
internal sealed class WindowsWebTextBlock : TextBlock
{
    private double _cssLineHeight;
    public double CssLineHeight
    {
        get => _cssLineHeight;
        set { if (_cssLineHeight != value) { _cssLineHeight = value; InvalidateVisual(); } }
    }
    private (string Family, double Size, FontWeight Weight)? _metricKey;
    private SKFontMetrics _metrics;
    public IReadOnlyList<double> CssLineWidths => TextLayout.TextLines
        .Select(CssAdvance).ToArray();
    private double CssAdvance(TextLine line) => line.WidthIncludingTrailingWhitespace -
        (line.TextRuns.LastOrDefault() is ShapedTextRun run && run.Text.Span.EndsWith("\n".AsSpan()) ? LetterSpacing : 0);

    protected override Size MeasureOverride(Size availableSize) =>
        string.IsNullOrEmpty(Text) ? default : base.MeasureOverride(availableSize);

    protected override TextLayout CreateTextLayout(string? text)
    {
        // Match the actual Windows face before Avalonia's localized-family cache
        // can replace a real bold face with a simulation after a weight change.
        using var face = SKTypeface.FromFamilyName(FontFamily.Name,
            new SKFontStyle((int)FontWeight, 5, SKFontStyleSlant.Upright));
        var typeface = new Typeface(new FontFamily(face.FamilyName), FontStyle,
            (FontWeight)face.FontStyle.Weight, FontStretch);
        var value = (text ?? "").Normalize(NormalizationForm.FormC);
        var width = GetMaxSizeFromConstraint().Width;
        TextLayout Layout(string content, double maxWidth, TextAlignment alignment) =>
            new(content, typeface, FontSize, Foreground, alignment, TextWrapping.NoWrap,
                flowDirection: FlowDirection, maxWidth: maxWidth, lineHeight: LineHeight,
                letterSpacing: LetterSpacing, fontFeatures: FontFeatures);
        if (value.Length > 0 && TextWrapping != TextWrapping.NoWrap && double.IsFinite(width))
        {
            double Advance(string segment)
            {
                using var probe = Layout(segment, double.PositiveInfinity, TextAlignment.Left);
                return probe.WidthIncludingTrailingWhitespace;
            }
            value = Wrap(value, width, TextWrapping == TextWrapping.Wrap, Advance);
        }
        return Layout(value, width, TextAlignment);
    }

    private static string Wrap(string text, double width, bool anywhere, Func<string, double> advance)
    {
        var opportunities = new List<int>();
        var enumerator = new LineBreakEnumerator(text.AsSpan());
        while (enumerator.MoveNext(out var item)) opportunities.Add(item.PositionWrap);
        WebNumericLineBreaks.Apply(text,opportunities);
        if (opportunities.Count == 0 || opportunities[^1] != text.Length) opportunities.Add(text.Length);
        var lines = new List<string>();
        var start = 0;
        while (start < text.Length)
        {
            var end = start;
            foreach (var candidate in opportunities)
            {
                if (candidate <= start) continue;
                if (advance(text[start..candidate].TrimEnd(' ')) > width)
                {
                    if (end == start) end = candidate;
                    break;
                }
                end = candidate;
            }
            if (anywhere && advance(text[start..end].TrimEnd(' ')) > width)
            {
                var boundaries = StringInfo.ParseCombiningCharacters(text[start..end]);
                var fit = start;
                foreach (var boundary in boundaries.Skip(1).Append(end - start))
                {
                    var candidate = start + boundary;
                    if (advance(text[start..candidate]) > width)
                    {
                        if (fit == start) fit = candidate;
                        break;
                    }
                    fit = candidate;
                }
                end = fit;
            }
            lines.Add(text[start..end].TrimEnd(' '));
            start = end;
            while (start < text.Length && text[start] == ' ') start++;
        }
        return string.Join('\n', lines);
    }

    public double CssBaseline(double scale)
    {
        var key = (FontFamily.Name, FontSize, FontWeight);
        if (_metricKey != key)
        {
            using var face = SKTypeface.FromFamilyName(key.Name,
                new SKFontStyle((int)FontWeight, 5, SKFontStyleSlant.Upright));
            using var font = new SKFont(face, (float)FontSize);
            _metrics = font.Metrics;
            _metricKey = key;
        }
        var ascent = Math.Floor(-_metrics.Ascent * scale + .5) / scale;
        var descent = Math.Floor(_metrics.Descent * scale + .5) / scale;
        return ascent + Math.Floor((CssLineHeight - ascent - descent) * scale / 2) / scale;
    }

    protected override void RenderTextLayout(DrawingContext context, Point origin)
    {
        var baseline = CssBaseline(TopLevel.GetTopLevel(this)?.RenderScaling ?? 2);
        var y = origin.Y;
        foreach (var line in TextLayout.TextLines)
        {
            // CSS preserves trailing NBSP. Avalonia's alignment uses Width,
            // which excludes it; use the same significant width as our measure.
            var start = TextAlignment switch
            {
                TextAlignment.Center => (Bounds.Width - CssAdvance(line)) / 2,
                TextAlignment.Right => Bounds.Width - CssAdvance(line),
                _ => 0
            };
            line.Draw(context, new Point(origin.X + start - line.Start, y + baseline - line.Baseline));
            y += line.Height;
        }
    }
}
