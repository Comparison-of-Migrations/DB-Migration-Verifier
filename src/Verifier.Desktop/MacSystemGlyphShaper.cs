using System.Runtime.InteropServices;
using HarfBuzzSharp;

namespace Verifier.Desktop;

/// <summary>Browser-style OpenType positions using the OS-selected font and fallback runs.</summary>
internal static class MacSystemGlyphShaper
{
    private const string Cf = "/System/Library/Frameworks/CoreFoundation.framework/CoreFoundation";
    private const string Ct = "/System/Library/Frameworks/CoreText.framework/CoreText";
    private static readonly IntPtr CtLibrary = NativeLibrary.Load(Ct);
    private static readonly IntPtr FontKey = Marshal.ReadIntPtr(NativeLibrary.GetExport(CtLibrary, "kCTFontAttributeName"));
    internal sealed record Run(IntPtr Font, ushort[] Glyphs, GlyphPoint[] Positions);
    internal sealed record Line(Run[] Runs, double Width);
    [StructLayout(LayoutKind.Sequential)] internal readonly record struct GlyphPoint(double X, double Y);

    // CTLine retains the borrowed font references until the owning layout is disposed.
    public static Line ShapeLine(IntPtr line, string text, double spacing, bool tabular = false)
    {
        var nativeRuns = CTLineGetGlyphRuns(line);
        var range = CTLineGetStringRange(line);
        var trimEnd = checked((int)(range.Location + range.Length));
        while (trimEnd > range.Location && text[trimEnd - 1] == ' ') trimEnd--;
        var result = new List<Run>();
        double x = 0;
        for (nint i = 0; i < CFArrayGetCount(nativeRuns); i++)
        {
            var run = CFArrayGetValueAtIndex(nativeRuns, i);
            var span = CTRunGetStringRange(run);
            var length = Math.Max(0, Math.Min(checked((int)(span.Location + span.Length)), trimEnd) - (int)span.Location);
            if (length == 0) continue;
            var font = CFDictionaryGetValue(CTRunGetAttributes(run), FontKey);
            if (font == IntPtr.Zero) throw new InvalidOperationException("System text run has no font.");
            var content = text.Substring((int)span.Location, length);
            using var tables = new FontTables(font);
            using var face = new Face(tables.CopyTable);
            using var hbFont = new Font(face);
            hbFont.SetFunctionsOpenType();
            var size = CTFontGetSize(font);
            var scale = checked((int)(size * 65536));
            hbFont.SetScale(scale, scale);
            var axes = ReadAxes(font);
            hb_font_set_variations(hbFont.Handle, axes, (uint)axes.Length);
            hb_font_set_ptem(hbFont.Handle, (float)size);
            using var buffer = new HarfBuzzSharp.Buffer();
            buffer.AddUtf16(content);
            buffer.GuessSegmentProperties();
            buffer.Direction = (CTRunGetStatus(run) & 1) != 0 ? Direction.RightToLeft : Direction.LeftToRight;
            hbFont.Shape(buffer, tabular ? [new Feature(new Tag('t', 'n', 'u', 'm'), 1)] : []);
            tables.ThrowIfFailed();
            var infos = buffer.GlyphInfos;
            var advances = buffer.GlyphPositions;
            var glyphs = new ushort[infos.Length];
            var positions = new GlyphPoint[infos.Length];
            for (var j = 0; j < infos.Length; j++)
            {
                glyphs[j] = checked((ushort)infos[j].Codepoint);
                positions[j] = new(x + advances[j].XOffset / 65536d, advances[j].YOffset / 65536d);
                x += advances[j].XAdvance / 65536d;
                if (j == infos.Length - 1 || infos[j].Cluster != infos[j + 1].Cluster) x += spacing;
            }
            result.Add(new(font, glyphs, positions));
        }
        return new(result.ToArray(), x);
    }

    private sealed class FontTables(IntPtr font) : IDisposable
    {
        private readonly List<Blob> _blobs = [];
        private Exception? _failure;
        public Blob CopyTable(Face _, Tag tag)
        {
            // Never throw across the native callback. The caller checks after shaping.
            try
            {
                var data = CTFontCopyTable(font, tag, 0);
                if (data == IntPtr.Zero) return Blob.Empty;
                try
                {
                    var blob = new Blob(CFDataGetBytePtr(data), checked((int)CFDataGetLength(data)), MemoryMode.Duplicate);
                    _blobs.Add(blob);
                    // Face owns the returned reference; the managed wrapper keeps its own.
                    hb_blob_reference(blob.Handle);
                    return blob;
                }
                finally { CFRelease(data); }
            }
            catch (Exception ex) { _failure ??= ex; return Blob.Empty; }
        }
        public void ThrowIfFailed()
        {
            if (_failure is not null) throw new InvalidOperationException("Cannot read system-font tables.", _failure);
        }
        public void Dispose() { foreach (var blob in _blobs) blob.Dispose(); _blobs.Clear(); }
    }
    private static Axis[] ReadAxes(IntPtr font)
    {
        var variations = CTFontCopyVariation(font);
        if (variations == IntPtr.Zero) return [];
        try
        {
            var count = checked((int)CFDictionaryGetCount(variations));
            var keys = new IntPtr[count]; var values = new IntPtr[count];
            CFDictionaryGetKeysAndValues(variations, keys, values);
            return Enumerable.Range(0, count).Select(i =>
            {
                if (!CFNumberGetValue(keys[i], 6, out var tag) || !CFNumberGetValue(values[i], 6, out var value))
                    throw new InvalidOperationException("Invalid system-font variation.");
                return new Axis((uint)tag, (float)value);
            }).ToArray();
        }
        finally { CFRelease(variations); }
    }
    [StructLayout(LayoutKind.Sequential)] private readonly record struct Axis(uint Tag, float Value);
    [StructLayout(LayoutKind.Sequential)] private readonly record struct Range(nint Location, nint Length);
    [DllImport(Ct)] private static extern IntPtr CTFontCopyTable(IntPtr font, uint tag, uint options);
    [DllImport(Ct)] private static extern IntPtr CTFontCopyVariation(IntPtr font);
    [DllImport(Ct)] private static extern double CTFontGetSize(IntPtr font);
    [DllImport(Ct)] private static extern IntPtr CTLineGetGlyphRuns(IntPtr line);
    [DllImport(Ct)] private static extern Range CTLineGetStringRange(IntPtr line);
    [DllImport(Ct)] private static extern IntPtr CTRunGetAttributes(IntPtr run);
    [DllImport(Ct)] private static extern Range CTRunGetStringRange(IntPtr run);
    [DllImport(Ct)] private static extern uint CTRunGetStatus(IntPtr run);
    [DllImport(Cf)] private static extern nint CFArrayGetCount(IntPtr array);
    [DllImport(Cf)] private static extern IntPtr CFArrayGetValueAtIndex(IntPtr array, nint index);
    [DllImport(Cf)] private static extern IntPtr CFDictionaryGetValue(IntPtr dictionary, IntPtr key);
    [DllImport(Cf)] private static extern nint CFDictionaryGetCount(IntPtr dictionary);
    [DllImport(Cf)] private static extern void CFDictionaryGetKeysAndValues(IntPtr dictionary, [Out] IntPtr[] keys, [Out] IntPtr[] values);
    [DllImport(Cf)] [return: MarshalAs(UnmanagedType.I1)] private static extern bool CFNumberGetValue(IntPtr number, int type, out double value);
    [DllImport(Cf)] private static extern IntPtr CFDataGetBytePtr(IntPtr data);
    [DllImport(Cf)] private static extern nint CFDataGetLength(IntPtr data);
    [DllImport(Cf)] private static extern void CFRelease(IntPtr value);
    [DllImport("libHarfBuzzSharp")] private static extern IntPtr hb_blob_reference(IntPtr blob);
    [DllImport("libHarfBuzzSharp")] private static extern void hb_font_set_variations(IntPtr font, Axis[] axes, uint count);
    [DllImport("libHarfBuzzSharp")] private static extern void hb_font_set_ptem(IntPtr font, float size);
}
