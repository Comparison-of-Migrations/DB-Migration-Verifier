using System.Runtime.InteropServices;
using HarfBuzzSharp;

// Measurement only. Font tables come from the selected CTFont, never a font file path.
internal static class CoreTextTableShape
{
    private const string Cf = "/System/Library/Frameworks/CoreFoundation.framework/CoreFoundation";
    private const string Ct = "/System/Library/Frameworks/CoreText.framework/CoreText";
    public static object MeasureLine(IntPtr line, string text, double spacing, bool tabular=false,bool defaultOptical=false)
    {
        var ctLibrary = NativeLibrary.Load(Ct);
        try
        {
            var fontKey = Marshal.ReadIntPtr(NativeLibrary.GetExport(ctLibrary, "kCTFontAttributeName"));
            var nativeRuns = CTLineGetGlyphRuns(line);
            var lineRange = CTLineGetStringRange(line);
            var trimEnd = (int)(lineRange.Location + lineRange.Length);
            while (trimEnd > lineRange.Location && char.IsWhiteSpace(text[trimEnd - 1])) trimEnd--;
            var runs = new List<object>();
            double width = 0;
            for (nint i = 0; i < CFArrayGetCount(nativeRuns); i++)
            {
                var run = CFArrayGetValueAtIndex(nativeRuns, i);
                var range = CTRunGetStringRange(run);
                var font = CFDictionaryGetValue(CTRunGetAttributes(run), fontKey);
                var content = text.Substring((int)range.Location, Math.Max(0, Math.Min((int)(range.Location + range.Length), trimEnd) - (int)range.Location));
                var axes = ReadAxes(font);
                if(defaultOptical)axes=axes.Where(a=>a.Tag!=0x6f70737a).ToArray();
                using var tables = new Tables(font);
                using var face = new Face(tables.CopyTable);
                using var hbFont = new Font(face);
                hbFont.SetFunctionsOpenType();
                var size = CTFontGetSize(font);
                hbFont.SetScale((int)(size * 65536), (int)(size * 65536));
                hb_font_set_variations(hbFont.Handle, axes, (uint)axes.Length);
                hb_font_set_ptem(hbFont.Handle, (float)size);
                using var buffer = new HarfBuzzSharp.Buffer();
                buffer.AddUtf16(content); buffer.GuessSegmentProperties();
                hbFont.Shape(buffer,tabular?[new Feature(new Tag('t','n','u','m'),1)]:[]);
                tables.ThrowIfFailed();
                var runWidth = buffer.GlyphPositions.Sum(p => (double)p.XAdvance) / 65536 + content.Length * spacing;
                width += runWidth;
                runs.Add(new { start = (long)range.Location, length = (long)range.Length, content, size,
                    axes = axes.Select(a => new { tag = ((Tag)a.Tag).ToString(), a.Value }),
                    width = runWidth, glyphs = buffer.GlyphInfos.Select(g => g.Codepoint),
                    missingGlyphs = buffer.GlyphInfos.Count(g => g.Codepoint == 0), status = CTRunGetStatus(run) });
            }
            return new { width, runs };
        }
        finally { NativeLibrary.Free(ctLibrary); }
    }
    private sealed class Tables(IntPtr font) : IDisposable
    {
        private readonly List<Blob> _blobs = [];
        private Exception? _failure;
        public Blob CopyTable(Face _, Tag tag)
        {
            try
            {
                var data = CTFontCopyTable(font, tag, 0);
                if (data == IntPtr.Zero) return Blob.Empty;
                try
                {
                    var blob = new Blob(CFDataGetBytePtr(data), checked((int)CFDataGetLength(data)), MemoryMode.Duplicate);
                    _blobs.Add(blob); hb_blob_reference(blob.Handle); return blob;
                }
                finally { CFRelease(data); }
            }
            catch (Exception ex) { _failure ??= ex; return Blob.Empty; }
        }
        public void ThrowIfFailed()
        {
            if (_failure is not null) throw new InvalidOperationException("Font table measurement failed.", _failure);
        }
        public void Dispose() { foreach(var blob in _blobs) blob.Dispose(); _blobs.Clear(); }
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
