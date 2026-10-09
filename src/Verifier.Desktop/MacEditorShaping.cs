using System.Runtime.InteropServices;
using Avalonia;
using Avalonia.Media.TextFormatting;
using HarfBuzzSharp;

namespace Verifier.Desktop;

/// <summary>Editing run positions from the OS-selected UI font; never collapse input whitespace.</summary>
internal static class MacEditorShaping
{
    private const string Ct="/System/Library/Frameworks/CoreText.framework/CoreText";
    private const string Cf="/System/Library/Frameworks/CoreFoundation.framework/CoreFoundation";
    public static ShapedBuffer Shape(ShapedBuffer original)
    {
        var system=CTFontCreateUIFontForLanguage(2,original.FontRenderingEmSize,IntPtr.Zero);
        if(system==IntPtr.Zero)throw new InvalidOperationException("Cannot select the macOS UI font.");
        try
        {
            using var tables=new Tables(system);using var face=new Face(tables.Copy);using var font=new Font(face);
            font.SetFunctionsOpenType();var scale=checked((int)(original.FontRenderingEmSize*65536));font.SetScale(scale,scale);
            var axes=Axes(system);hb_font_set_variations(font.Handle,axes,(uint)axes.Length);
            hb_font_set_ptem(font.Handle,(float)original.FontRenderingEmSize);
            using var buffer=new HarfBuzzSharp.Buffer();buffer.AddUtf16(original.Text.Span.ToString());buffer.GuessSegmentProperties();
            buffer.Direction=original.BidiLevel%2==0?Direction.LeftToRight:Direction.RightToLeft;
            font.Shape(buffer);tables.Check();var infos=buffer.GlyphInfos;var positions=buffer.GlyphPositions;
            var result=new ShapedBuffer(original.Text,infos.Length,original.GlyphTypeface,original.FontRenderingEmSize,original.BidiLevel);
            for(var i=0;i<infos.Length;i++)result[i]=new Avalonia.Media.TextFormatting.GlyphInfo(checked((ushort)infos[i].Codepoint),checked((int)infos[i].Cluster),positions[i].XAdvance/65536d,new Vector(positions[i].XOffset/65536d,-positions[i].YOffset/65536d));
            return result;
        }
        finally{CFRelease(system);}
    }
    private sealed class Tables(IntPtr font):IDisposable
    {
        private readonly List<Blob> _blobs=[];private Exception? _error;
        public Blob Copy(Face _,Tag tag)
        {
            try
            {
                var data=CTFontCopyTable(font,tag,0);if(data==IntPtr.Zero)return Blob.Empty;
                try{var b=new Blob(CFDataGetBytePtr(data),checked((int)CFDataGetLength(data)),MemoryMode.Duplicate);_blobs.Add(b);hb_blob_reference(b.Handle);return b;}
                finally{CFRelease(data);}
            }
            catch(Exception e){_error??=e;return Blob.Empty;}
        }
        public void Check(){if(_error is not null)throw new InvalidOperationException("Cannot read the UI font tables.",_error);}
        public void Dispose(){foreach(var b in _blobs)b.Dispose();}
    }
    private static Axis[] Axes(IntPtr font)
    {
        var data=CTFontCopyVariation(font);if(data==IntPtr.Zero)return [];
        try
        {
            var count=checked((int)CFDictionaryGetCount(data));var keys=new IntPtr[count];var values=new IntPtr[count];CFDictionaryGetKeysAndValues(data,keys,values);
            return Enumerable.Range(0,count).Select(i=>{if(!CFNumberGetValue(keys[i],6,out var tag)||!CFNumberGetValue(values[i],6,out var value))throw new InvalidOperationException("Invalid UI font variation.");return new Axis((uint)tag,(float)value);}).ToArray();
        }
        finally{CFRelease(data);}
    }
    [StructLayout(LayoutKind.Sequential)]private readonly record struct Axis(uint Tag,float Value);
    [DllImport(Ct)]private static extern IntPtr CTFontCreateUIFontForLanguage(int type,double size,IntPtr language);
    [DllImport(Ct)]private static extern IntPtr CTFontCopyTable(IntPtr font,uint tag,uint options);
    [DllImport(Ct)]private static extern IntPtr CTFontCopyVariation(IntPtr font);
    [DllImport(Cf)]private static extern nint CFDictionaryGetCount(IntPtr data);
    [DllImport(Cf)]private static extern void CFDictionaryGetKeysAndValues(IntPtr data,[Out]IntPtr[] keys,[Out]IntPtr[] values);
    [DllImport(Cf)][return:MarshalAs(UnmanagedType.I1)]private static extern bool CFNumberGetValue(IntPtr data,int type,out double value);
    [DllImport(Cf)]private static extern IntPtr CFDataGetBytePtr(IntPtr data);
    [DllImport(Cf)]private static extern nint CFDataGetLength(IntPtr data);
    [DllImport(Cf)]private static extern void CFRelease(IntPtr data);
    [DllImport("libHarfBuzzSharp")]private static extern IntPtr hb_blob_reference(IntPtr data);
    [DllImport("libHarfBuzzSharp")]private static extern void hb_font_set_variations(IntPtr font,Axis[] axes,uint count);
    [DllImport("libHarfBuzzSharp")]private static extern void hb_font_set_ptem(IntPtr font,float size);
}
