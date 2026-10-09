using System.Runtime.InteropServices;
using System.Globalization;
using Avalonia;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;

namespace Verifier.Desktop;

/// <summary>Public macOS system-font layout, including CSS numeric weight and OS fallback.</summary>
internal sealed class MacSystemTextLayout : IDisposable
{
    private const string Cf = "/System/Library/Frameworks/CoreFoundation.framework/CoreFoundation";
    private const string Ct = "/System/Library/Frameworks/CoreText.framework/CoreText";
    private const string Cg = "/System/Library/Frameworks/CoreGraphics.framework/CoreGraphics";
    private static readonly IntPtr CfLibrary = NativeLibrary.Load(Cf);
    private static readonly IntPtr CtLibrary = NativeLibrary.Load(Ct);
    private static readonly IntPtr CgLibrary = NativeLibrary.Load(Cg);
    private readonly List<IntPtr> _owned = [];
    private readonly List<IntPtr> _lines = [];
    private readonly List<MacSystemGlyphShaper.Line> _shaped = [];
    private readonly List<double> _widths = [];
    private readonly IntPtr _font;
    private readonly double _lineHeight;
    public IReadOnlyList<double> LineWidths => _widths;
    private readonly List<string> _lineTexts=[];
    public IReadOnlyList<string> LineTexts=>_lineTexts;
    public Size Size { get; }
    public double GetBaseline(double scale)
    {
        // The source rounds primary metrics at its effective device scale, then floors half-leading.
        var ascent = Math.Floor(FontAscent * scale + .5) / scale;
        var descent = Math.Floor(FontDescent * scale + .5) / scale;
        var leading = Math.Floor((_lineHeight - ascent - descent) * scale / 2) / scale;
        return ascent + leading;
    }
    private double FontAscent => CTFontGetAscent(_font);
    private double FontDescent => CTFontGetDescent(_font);

    public MacSystemTextLayout(string text, double size, int weight, double spacing, double lineHeight, double width,
        bool monospaced = false, bool tabular = false, bool anywhere = false, double layoutScale = 2)
    {
        _lineHeight = Math.Floor(lineHeight * layoutScale * 64 + .5) / (layoutScale * 64);
        try
        {
            _font = Own(CTFontCreateUIFontForLanguage(monospaced ? 1 : 2, size, IntPtr.Zero));
            if (weight >= 600)
                _font = Own(CTFontCreateCopyWithSymbolicTraits(_font, size, IntPtr.Zero, 2, 2));
            if (weight != 400)
            {
                var tag = 0x77676874; // OpenType wght, same axis used by Chromium's macOS matcher.
                var axis = Own(CFNumberCreateInt(IntPtr.Zero, 3, ref tag));
                double weightValue = weight;
                var number = Own(CFNumberCreate(IntPtr.Zero, 6, ref weightValue));
                var variation = Own(Dictionary([axis], [number]));
                var attributes = Own(Dictionary([Constant("kCTFontVariationAttribute")], [variation]));
                var descriptor = Own(CTFontDescriptorCreateWithAttributes(attributes));
                _font = Own(CTFontCreateCopyWithAttributes(_font, size, IntPtr.Zero, descriptor));
            }
            if (tabular)
            {
                var tag = Own(CFStringCreateWithCharacters(IntPtr.Zero, "tnum", 4));
                double enabled = 1;
                var value = Own(CFNumberCreate(IntPtr.Zero, 6, ref enabled));
                var feature = Own(Dictionary([Constant("kCTFontOpenTypeFeatureTag"), Constant("kCTFontOpenTypeFeatureValue")], [tag, value]));
                var settings = Own(CFArrayCreate(IntPtr.Zero, [feature], 1, NativeLibrary.GetExport(CfLibrary, "kCFTypeArrayCallBacks")));
                var attributes = Own(Dictionary([Constant("kCTFontFeatureSettingsAttribute")], [settings]));
                var descriptor = Own(CTFontDescriptorCreateWithAttributes(attributes));
                _font = Own(CTFontCreateCopyWithAttributes(_font, size, IntPtr.Zero, descriptor));
            }
            var str = Own(CFStringCreateWithCharacters(IntPtr.Zero, text, text.Length));
            var keys = new List<IntPtr> { Constant("kCTFontAttributeName"), Constant("kCTForegroundColorFromContextAttributeName") };
            var values = new List<IntPtr> { _font, Marshal.ReadIntPtr(NativeLibrary.GetExport(CfLibrary, "kCFBooleanTrue")) };
            if (spacing != 0)
            {
                keys.Add(Constant("kCTKernAttributeName"));
                values.Add(Own(CFNumberCreate(IntPtr.Zero, 6, ref spacing)));
            }
            var attrs = Own(Dictionary(keys.ToArray(), values.ToArray()));
            var attributed = Own(CFAttributedStringCreate(IntPtr.Zero, str, attrs));
            var setter = Own(CTTypesetterCreateWithAttributedString(attributed));
            var breaks = new List<int>();
            var tokenizer = Own(CFStringTokenizerCreate(IntPtr.Zero, str, new(0, text.Length), 3, IntPtr.Zero));
            while (CFStringTokenizerAdvanceToNextToken(tokenizer) != 0)
            {
                var range = CFStringTokenizerGetCurrentTokenRange(tokenizer);
                breaks.Add(checked((int)(range.Location + range.Length)));
            }
            // CSS normal keeps slash-separated ASCII words together; retain
            // CoreFoundation's other opportunities (including CJK boundaries).
            breaks.RemoveAll(end => end < text.Length && text[end - 1] == '/' && char.IsAsciiLetter(text[end]));
            WebNumericLineBreaks.Apply(text,breaks);
            var clusters = anywhere ? StringInfo.ParseCombiningCharacters(text).Skip(1).Append(text.Length).ToArray() : [];
            if (text.Length > 0 && (breaks.Count == 0 || breaks[^1] != text.Length)) breaks.Add(text.Length);
            for (var start = 0; start < text.Length;)
            {
                while (start < text.Length && text[start] == ' ') start++;
                if (start == text.Length) break;
                var first = breaks.FindIndex(end => end > start);
                var low = first;
                var high = breaks.Count - 1;
                var selected = first; // An unbreakable first token overflows, as CSS normal does.
                while (low <= high)
                {
                    var middle = low + (high - low) / 2;
                    var probe = CTTypesetterCreateLine(setter, new(start, breaks[middle] - start));
                    if (probe == IntPtr.Zero) throw new InvalidOperationException("CoreText returned null line.");
                    double measured;
                    try { measured = MacSystemGlyphShaper.ShapeLine(probe, text, spacing, tabular).Width; }
                    finally { CFRelease(probe); }
                    if (measured <= width) { selected = middle; low = middle + 1; }
                    else high = middle - 1;
                }
                var count = breaks[selected] - start;
                if (anywhere && selected == first)
                {
                    double MeasureEnd(int end)
                    {
                        var probe = CTTypesetterCreateLine(setter, new(start, end - start));
                        if (probe == IntPtr.Zero) throw new InvalidOperationException("CoreText returned null line.");
                        try { return MacSystemGlyphShaper.ShapeLine(probe, text, spacing, tabular).Width; }
                        finally { CFRelease(probe); }
                    }
                    if (MeasureEnd(start + count) > width)
                    {
                        var candidates = clusters.Where(end => end > start && end <= start + count).ToArray();
                        var best = 0;
                        low = 0; high = candidates.Length - 1;
                        while (low <= high)
                        {
                            var middle = low + (high - low) / 2;
                            if (MeasureEnd(candidates[middle]) <= width) { best = middle; low = middle + 1; }
                            else high = middle - 1;
                        }
                        count = candidates[best] - start;
                    }
                }
                var line = Own(CTTypesetterCreateLine(setter, new(start, count)));
                _lines.Add(line);
                _lineTexts.Add(text.Substring(start,count).TrimEnd(' '));
                var shaped = MacSystemGlyphShaper.ShapeLine(line, text, spacing, tabular);
                _shaped.Add(shaped);
                _widths.Add(shaped.Width);
                start += count;
            }
            Size = new(_widths.Count == 0 ? 0 : Math.Max(0, _widths.Max()), _lines.Count * _lineHeight);
        }
        catch { Dispose(); throw; }
    }

    public WriteableBitmap Raster(Size bounds, Color color, double opacity, TextAlignment alignment, double scale, Point phase, out double originX, out double originY)
    {
        double LineX(double lineWidth) => alignment switch
        {
            TextAlignment.Center => (bounds.Width - lineWidth) / 2,
            TextAlignment.Right => bounds.Width - lineWidth,
            _ => 0
        };
        originX = Math.Floor((Math.Min(0, _widths.Count == 0 ? 0 : _widths.Min(LineX)) + phase.X) * scale) / scale - phase.X;
        originY = -phase.Y;
        var right = Math.Max(bounds.Width, _widths.Count == 0 ? 0 : _widths.Max(w => LineX(w) + w));
        var width = Math.Max(1, checked((int)Math.Ceiling((right - originX) * scale)));
        var height = Math.Max(1, checked((int)Math.Ceiling((bounds.Height - originY) * scale)));
        var rowBytes = checked(width * 4);
        var pixels = new byte[checked(rowBytes * height)];
        var pinned = GCHandle.Alloc(pixels, GCHandleType.Pinned);
        IntPtr space = IntPtr.Zero, context = IntPtr.Zero;
        try
        {
            space = CGColorSpaceCreateWithName(Marshal.ReadIntPtr(NativeLibrary.GetExport(CgLibrary, "kCGColorSpaceSRGB")));
            context = CGBitmapContextCreate(pinned.AddrOfPinnedObject(), (nuint)width, (nuint)height, 8, (nuint)rowBytes, space, 0x4001);
            if (context == IntPtr.Zero) throw new InvalidOperationException("Cannot create system-font bitmap.");
            CGContextTranslateCTM(context, 0, height);
            CGContextScaleCTM(context, scale, -scale);
            CGContextSetShouldSmoothFonts(context, false);
            CGContextSetAllowsFontSubpixelPositioning(context, true);
            CGContextSetShouldSubpixelPositionFonts(context, true);
            CGContextSetAllowsFontSubpixelQuantization(context, false);
            CGContextSetShouldSubpixelQuantizeFonts(context, false);
            CGContextSetRGBFillColor(context, color.R / 255d, color.G / 255d, color.B / 255d, color.A / 255d * opacity);
            var baseline = GetBaseline(scale) - originY;
            for (var i = 0; i < _lines.Count; i++)
            {
                var x = LineX(_widths[i]) - originX;
                CGContextSaveGState(context);
                CGContextTranslateCTM(context, x, baseline);
                CGContextScaleCTM(context, 1, -1);
                CGContextSetTextPosition(context, 0, 0);
                foreach (var run in _shaped[i].Runs)
                    CTFontDrawGlyphs(run.Font, run.Glyphs, run.Positions, (nuint)run.Glyphs.Length, context);
                CGContextRestoreGState(context);
                baseline += _lineHeight;
            }
            return new WriteableBitmap(PixelFormat.Rgba8888, AlphaFormat.Premul, pinned.AddrOfPinnedObject(),
                new PixelSize(width, height), new Vector(96 * scale, 96 * scale), rowBytes);
        }
        finally
        {
            if (context != IntPtr.Zero) CGContextRelease(context);
            if (space != IntPtr.Zero) CGColorSpaceRelease(space);
            pinned.Free();
        }
    }

    private IntPtr Own(IntPtr value)
    {
        if (value == IntPtr.Zero) throw new InvalidOperationException("CoreText returned null.");
        _owned.Add(value); return value;
    }
    public void Dispose()
    {
        foreach (var value in _owned.AsEnumerable().Reverse()) CFRelease(value);
        _owned.Clear();
    }
    private static IntPtr Constant(string name) => Marshal.ReadIntPtr(NativeLibrary.GetExport(CtLibrary, name));
    private static IntPtr Dictionary(IntPtr[] keys, IntPtr[] values) => CFDictionaryCreate(IntPtr.Zero, keys, values, keys.Length,
        NativeLibrary.GetExport(CfLibrary, "kCFTypeDictionaryKeyCallBacks"), NativeLibrary.GetExport(CfLibrary, "kCFTypeDictionaryValueCallBacks"));
    [StructLayout(LayoutKind.Sequential)] private readonly record struct Range(nint Location, nint Length);
    [DllImport(Cf)] private static extern IntPtr CFStringCreateWithCharacters(IntPtr allocator, [MarshalAs(UnmanagedType.LPWStr)] string text, nint length);
    [DllImport(Cf)] private static extern IntPtr CFNumberCreate(IntPtr allocator, int type, ref double value);
    [DllImport(Cf, EntryPoint = "CFNumberCreate")] private static extern IntPtr CFNumberCreateInt(IntPtr allocator, int type, ref int value);
    [DllImport(Cf)] private static extern IntPtr CFDictionaryCreate(IntPtr allocator, IntPtr[] keys, IntPtr[] values, nint count, IntPtr keyCallbacks, IntPtr valueCallbacks);
    [DllImport(Cf)] private static extern IntPtr CFAttributedStringCreate(IntPtr allocator, IntPtr text, IntPtr attributes);
    [DllImport(Cf)] private static extern IntPtr CFArrayCreate(IntPtr allocator, IntPtr[] values, nint count, IntPtr callbacks);
    [DllImport(Cf)] private static extern void CFRelease(IntPtr value);
    [DllImport(Cf)] private static extern IntPtr CFStringTokenizerCreate(IntPtr allocator, IntPtr str, Range range, nuint options, IntPtr locale);
    [DllImport(Cf)] private static extern nuint CFStringTokenizerAdvanceToNextToken(IntPtr tokenizer);
    [DllImport(Cf)] private static extern Range CFStringTokenizerGetCurrentTokenRange(IntPtr tokenizer);
    [DllImport(Ct)] private static extern IntPtr CTFontCreateUIFontForLanguage(int type, double size, IntPtr language);
    [DllImport(Ct)] private static extern IntPtr CTFontCreateCopyWithSymbolicTraits(IntPtr font, double size, IntPtr matrix, uint traits, uint mask);
    [DllImport(Ct)] private static extern IntPtr CTFontDescriptorCreateWithAttributes(IntPtr attrs);
    [DllImport(Ct)] private static extern IntPtr CTFontCreateCopyWithAttributes(IntPtr font, double size, IntPtr matrix, IntPtr descriptor);
    [DllImport(Ct)] private static extern IntPtr CTTypesetterCreateWithAttributedString(IntPtr text);
    [DllImport(Ct)] private static extern nint CTTypesetterSuggestLineBreak(IntPtr setter, nint start, double width);
    [DllImport(Ct)] private static extern IntPtr CTTypesetterCreateLine(IntPtr setter, Range range);
    [DllImport(Ct)] private static extern double CTFontGetAscent(IntPtr font);
    [DllImport(Ct)] private static extern double CTFontGetDescent(IntPtr font);
    [DllImport(Ct)] private static extern void CTFontDrawGlyphs(IntPtr font, ushort[] glyphs, MacSystemGlyphShaper.GlyphPoint[] positions, nuint count, IntPtr context);
    [DllImport(Cg)] private static extern IntPtr CGColorSpaceCreateWithName(IntPtr name);
    [DllImport(Cg)] private static extern void CGColorSpaceRelease(IntPtr value);
    [DllImport(Cg)] private static extern IntPtr CGBitmapContextCreate(IntPtr data, nuint width, nuint height, nuint bits, nuint rowBytes, IntPtr space, uint info);
    [DllImport(Cg)] private static extern void CGContextRelease(IntPtr context);
    [DllImport(Cg)] private static extern void CGContextSetRGBFillColor(IntPtr context, double r, double g, double b, double a);
    [DllImport(Cg)] private static extern void CGContextTranslateCTM(IntPtr context, double x, double y);
    [DllImport(Cg)] private static extern void CGContextScaleCTM(IntPtr context, double x, double y);
    [DllImport(Cg)] private static extern void CGContextSetTextPosition(IntPtr context, double x, double y);
    [DllImport(Cg)] private static extern void CGContextSaveGState(IntPtr context);
    [DllImport(Cg)] private static extern void CGContextRestoreGState(IntPtr context);
    [DllImport(Cg)] private static extern void CGContextSetShouldSmoothFonts(IntPtr context, [MarshalAs(UnmanagedType.I1)] bool smooth);
    [DllImport(Cg)] private static extern void CGContextSetAllowsFontSubpixelPositioning(IntPtr context, [MarshalAs(UnmanagedType.I1)] bool enabled);
    [DllImport(Cg)] private static extern void CGContextSetShouldSubpixelPositionFonts(IntPtr context, [MarshalAs(UnmanagedType.I1)] bool enabled);
    [DllImport(Cg)] private static extern void CGContextSetAllowsFontSubpixelQuantization(IntPtr context, [MarshalAs(UnmanagedType.I1)] bool enabled);
    [DllImport(Cg)] private static extern void CGContextSetShouldSubpixelQuantizeFonts(IntPtr context, [MarshalAs(UnmanagedType.I1)] bool enabled);
}
