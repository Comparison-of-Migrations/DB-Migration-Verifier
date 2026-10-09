using System.Runtime.InteropServices;
using System.Text.Json;

// Investigation only. Match the browser's system-font selection through the OS API;
// do not rewrite CSS metrics or ship modified system font files.
internal static class CoreTextMeasure
{
    private const string Cf="/System/Library/Frameworks/CoreFoundation.framework/CoreFoundation";
    private const string Cg="/System/Library/Frameworks/CoreGraphics.framework/CoreGraphics";
    private const string Ct="/System/Library/Frameworks/CoreText.framework/CoreText";
    private static readonly IntPtr CfLibrary=NativeLibrary.Load(Cf);
    private static readonly IntPtr CtLibrary=NativeLibrary.Load(Ct);
    private static IntPtr Constant(string name)=>Marshal.ReadIntPtr(NativeLibrary.GetExport(CtLibrary,name));
    private static IntPtr Dictionary(IntPtr[] keys,IntPtr[] values)=>CFDictionaryCreate(IntPtr.Zero,keys,values,keys.Length,NativeLibrary.GetExport(CfLibrary,"kCFTypeDictionaryKeyCallBacks"),NativeLibrary.GetExport(CfLibrary,"kCFTypeDictionaryValueCallBacks"));
    private static IntPtr Number(double value)=>CFNumberCreate(IntPtr.Zero,6,ref value);

    public static void Run(string input,string output,bool render=false,bool tableShape=false)
    {
        if(!OperatingSystem.IsMacOS())throw new PlatformNotSupportedException("CoreText investigation requires macOS.");
        var cases=JsonSerializer.Deserialize<Case[]>(File.ReadAllText(input),new JsonSerializerOptions {PropertyNameCaseInsensitive=true})!;
        var result=cases.Select(c=>Measure(c,render?Path.GetDirectoryName(output):null,tableShape)).ToArray();
        File.WriteAllText(output,JsonSerializer.Serialize(result,new JsonSerializerOptions {WriteIndented=true}));
    }
    private static object Measure(Case c,string? renderDirectory,bool tableShape)
    {
        var owned=new List<IntPtr>();
        IntPtr Own(IntPtr value){if(value==IntPtr.Zero)throw new InvalidOperationException("CoreText returned null.");owned.Add(value);return value;}
        try
        {
            var font=Own(CTFontCreateUIFontForLanguage(c.Family=="mono"?1:2,c.Size,IntPtr.Zero));
            if(c.Weight>=600)font=Own(CTFontCreateCopyWithSymbolicTraits(font,c.Size,IntPtr.Zero,2,2));
            if(c.Weight!=400)
            {
                var tag=0x77676874;
                var axis=Own(CFNumberCreateInt(IntPtr.Zero,3,ref tag));var weight=Own(Number(c.Weight));
                var variation=Own(Dictionary(new[]{axis},new[]{weight}));
                var attrs=Own(Dictionary(new[]{Constant("kCTFontVariationAttribute")},new[]{variation}));
                var descriptor=Own(CTFontDescriptorCreateWithAttributes(attrs));
                font=Own(CTFontCreateCopyWithAttributes(font,c.Size,IntPtr.Zero,descriptor));
            }
            if(c.OpticalSize is double optical)
            {
                var tag=0x6f70737a;var axis=Own(CFNumberCreateInt(IntPtr.Zero,3,ref tag));var value=Own(Number(optical));
                var variation=Own(Dictionary([axis],[value]));var attrs=Own(Dictionary([Constant("kCTFontVariationAttribute")],[variation]));
                var descriptor=Own(CTFontDescriptorCreateWithAttributes(attrs));font=Own(CTFontCreateCopyWithAttributes(font,c.Size,IntPtr.Zero,descriptor));
            }
            if(c.Tabular)
            {
                var tag=Own(CFStringCreateWithCharacters(IntPtr.Zero,"tnum",4));
                var value=Own(Number(1));
                var feature=Own(Dictionary([Constant("kCTFontOpenTypeFeatureTag"),Constant("kCTFontOpenTypeFeatureValue")],[tag,value]));
                var settings=Own(CFArrayCreate(IntPtr.Zero,[feature],1,NativeLibrary.GetExport(CfLibrary,"kCFTypeArrayCallBacks")));
                var attrs=Own(Dictionary([Constant("kCTFontFeatureSettingsAttribute")],[settings]));
                var descriptor=Own(CTFontDescriptorCreateWithAttributes(attrs));
                font=Own(CTFontCreateCopyWithAttributes(font,c.Size,IntPtr.Zero,descriptor));
            }
            if(c.FromGraphics)
            {
                var graphics=Own(CTFontCopyGraphicsFont(font,IntPtr.Zero));font=Own(CTFontCreateWithGraphicsFont(graphics,c.Size,IntPtr.Zero,IntPtr.Zero));
            }
            var name=Own(CTFontCopyPostScriptName(font));
            var nameChars=new ushort[CFStringGetLength(name)];CFStringGetCharacters(name,new(0,nameChars.Length),nameChars);
            var axes=new Dictionary<string,double>();
            var actualVariation=CTFontCopyVariation(font);
            if(actualVariation!=IntPtr.Zero)
            {
                Own(actualVariation);
                var count=(int)CFDictionaryGetCount(actualVariation);var axisKeys=new IntPtr[count];var axisValues=new IntPtr[count];
                CFDictionaryGetKeysAndValues(actualVariation,axisKeys,axisValues);
                for(var i=0;i<count;i++)
                {
                    CFNumberGetValue(axisKeys[i],6,out var axis);CFNumberGetValue(axisValues[i],6,out var value);
                    var tag=(uint)axis;var label=new string(new[]{(char)(tag>>24),(char)((tag>>16)&255),(char)((tag>>8)&255),(char)(tag&255)});
                    axes[label]=value;
                }
            }
            var str=Own(CFStringCreateWithCharacters(IntPtr.Zero,c.Text,c.Text.Length));
            var keys=new List<IntPtr>{Constant("kCTFontAttributeName"),Constant("kCTForegroundColorFromContextAttributeName")};var values=new List<IntPtr>{font,Marshal.ReadIntPtr(NativeLibrary.GetExport(CfLibrary,"kCFBooleanTrue"))};
            if(c.Spacing!=0){keys.Add(Constant("kCTKernAttributeName"));values.Add(Own(Number(c.Spacing)));}
            var attributes=Own(Dictionary(keys.ToArray(),values.ToArray()));
            var text=Own(CFAttributedStringCreate(IntPtr.Zero,str,attributes));
            var setter=Own(CTTypesetterCreateWithAttributedString(text));
            var lines=new List<object>();var nativeLines=new List<IntPtr>();
            for(nint start=0;start<c.Text.Length;)
            {
                var length=CTTypesetterSuggestLineBreak(setter,start,480);
                if(length<=0)throw new InvalidOperationException("Typesetter made no progress.");
                var line=Own(CTTypesetterCreateLine(setter,new(start,length)));
                nativeLines.Add(line);
                var width=CTLineGetTypographicBounds(line,out var ascent,out var descent,out var leading);
                lines.Add(new {start=(long)start,length=(long)length,width,trimmedWidth=width-CTLineGetTrailingWhitespaceWidth(line),ascent,descent,leading,text=c.Text.Substring((int)start,(int)length),tableShape=tableShape?CoreTextTableShape.MeasureLine(line,c.Text,c.Spacing,c.Tabular,c.DefaultOptical):null});
                start+=length;
            }
            if(renderDirectory is not null)Render(c,font,nativeLines,renderDirectory);
            return new {c.Id,c.Weight,c.Size,c.Family,c.Tabular,postscript=new string(nameChars.Select(v=>(char)v).ToArray()),primaryAscent=CTFontGetAscent(font),primaryDescent=CTFontGetDescent(font),axes,lines,lineBreaks=GetBreaks(str,c.Text.Length,Own)};
        }
        finally { foreach(var value in owned.AsEnumerable().Reverse())CFRelease(value); }
    }
    private static List<int> GetBreaks(IntPtr str,int length,Func<IntPtr,IntPtr> own)
    {
        var tokenizer=own(CFStringTokenizerCreate(IntPtr.Zero,str,new(0,length),3,IntPtr.Zero));
        var breaks=new List<int>();
        while(CFStringTokenizerAdvanceToNextToken(tokenizer)!=0)
        {
            var range=CFStringTokenizerGetCurrentTokenRange(tokenizer);
            breaks.Add(checked((int)(range.Location+range.Length)));
        }
        return breaks;
    }
    private static void Render(Case c,IntPtr font,List<IntPtr> lines,string directory)
    {
        const int scale=2,width=500*scale,height=130*scale;
        var pixels=new byte[width*height*4];var pinned=GCHandle.Alloc(pixels,GCHandleType.Pinned);
        var cgLibrary=NativeLibrary.Load(Cg);
        var space=CGColorSpaceCreateWithName(Marshal.ReadIntPtr(NativeLibrary.GetExport(cgLibrary,"kCGColorSpaceSRGB")));NativeLibrary.Free(cgLibrary);
        var context=CGBitmapContextCreate(pinned.AddrOfPinnedObject(),width,height,8,width*4,space,0x4001);
        if(context==IntPtr.Zero)throw new InvalidOperationException("Cannot create native text bitmap.");
        try
        {
            CGContextSetRGBFillColor(context,49/255d,51/255d,56/255d,1);
            CGContextFillRect(context,new(0,0,width,height));
            CGContextTranslateCTM(context,0,height);CGContextScaleCTM(context,scale,-scale);
            CGContextSetShouldSmoothFonts(context,false);
            var color=Avalonia.Media.Color.Parse(c.Color);
            CGContextSetRGBFillColor(context,color.R/255d,color.G/255d,color.B/255d,color.A/255d);
            var ascent=CTFontGetAscent(font);var descent=CTFontGetDescent(font);
            var baseline=10+(c.LineHeight-ascent-descent)/2+ascent;
            foreach(var line in lines)
            {
                CGContextSaveGState(context);CGContextTranslateCTM(context,10,baseline);CGContextScaleCTM(context,1,-1);
                CGContextSetTextPosition(context,0,0);CTLineDraw(line,context);CGContextRestoreGState(context);
                baseline+=c.LineHeight;
            }
            File.WriteAllBytes(Path.Combine(directory,$"coretext-{c.Id}.rgba"),pixels);
        }
        finally{CGContextRelease(context);CGColorSpaceRelease(space);pinned.Free();}
    }
    [StructLayout(LayoutKind.Sequential)] private readonly record struct Rect(double X,double Y,double Width,double Height);
    [DllImport(Cg)] private static extern IntPtr CGColorSpaceCreateWithName(IntPtr name);
    [DllImport(Cg)] private static extern void CGColorSpaceRelease(IntPtr value);
    [DllImport(Cg)] private static extern IntPtr CGBitmapContextCreate(IntPtr data,nuint width,nuint height,nuint bits,nuint rowBytes,IntPtr space,uint info);
    [DllImport(Cg)] private static extern void CGContextRelease(IntPtr context);
    [DllImport(Cg)] private static extern void CGContextSetRGBFillColor(IntPtr context,double r,double g,double b,double a);
    [DllImport(Cg)] private static extern void CGContextFillRect(IntPtr context,Rect rect);
    [DllImport(Cg)] private static extern void CGContextTranslateCTM(IntPtr context,double x,double y);
    [DllImport(Cg)] private static extern void CGContextScaleCTM(IntPtr context,double x,double y);
    [DllImport(Cg)] private static extern void CGContextSetTextPosition(IntPtr context,double x,double y);
    [DllImport(Cg)] private static extern void CGContextSaveGState(IntPtr context);
    [DllImport(Cg)] private static extern void CGContextRestoreGState(IntPtr context);
    [DllImport(Cg)] private static extern void CGContextSetShouldSmoothFonts(IntPtr context,[MarshalAs(UnmanagedType.I1)]bool smooth);
    [DllImport(Ct)] private static extern IntPtr CTFontCopyGraphicsFont(IntPtr font,IntPtr attributes);
    [DllImport(Ct)] private static extern IntPtr CTFontCreateWithGraphicsFont(IntPtr font,double size,IntPtr matrix,IntPtr attributes);
    [DllImport(Ct)] private static extern double CTFontGetAscent(IntPtr font);
    [DllImport(Ct)] private static extern double CTFontGetDescent(IntPtr font);
    [DllImport(Ct)] private static extern void CTLineDraw(IntPtr line,IntPtr context);
    private sealed record Case(string Id,string Text,double Size,int Weight,double Spacing,double LineHeight,string Color,string Family="system",bool Tabular=false,double? OpticalSize=null,bool FromGraphics=false,bool DefaultOptical=false);
    [StructLayout(LayoutKind.Sequential)] private readonly record struct Range(nint Location,nint Length);
    [DllImport(Cf)] private static extern IntPtr CFStringCreateWithCharacters(IntPtr allocator,[MarshalAs(UnmanagedType.LPWStr)]string chars,nint length);
    [DllImport(Cf)] private static extern nint CFStringGetLength(IntPtr str);
    [DllImport(Cf)] private static extern void CFStringGetCharacters(IntPtr str,Range range,[Out]ushort[] chars);
    [DllImport(Cf)] private static extern IntPtr CFNumberCreate(IntPtr allocator,int type,ref double value);
    [DllImport(Cf,EntryPoint="CFNumberCreate")] private static extern IntPtr CFNumberCreateInt(IntPtr allocator,int type,ref int value);
    [DllImport(Cf)] private static extern IntPtr CFDictionaryCreate(IntPtr allocator,IntPtr[] keys,IntPtr[] values,nint count,IntPtr keyCallbacks,IntPtr valueCallbacks);
    [DllImport(Cf)] private static extern IntPtr CFAttributedStringCreate(IntPtr allocator,IntPtr text,IntPtr attributes);
    [DllImport(Cf)] private static extern IntPtr CFArrayCreate(IntPtr allocator,IntPtr[] values,nint count,IntPtr callbacks);
    [DllImport(Cf)] private static extern IntPtr CFStringTokenizerCreate(IntPtr allocator,IntPtr str,Range range,nuint options,IntPtr locale);
    [DllImport(Cf)] private static extern nuint CFStringTokenizerAdvanceToNextToken(IntPtr tokenizer);
    [DllImport(Cf)] private static extern Range CFStringTokenizerGetCurrentTokenRange(IntPtr tokenizer);
    [DllImport(Cf)] private static extern void CFRelease(IntPtr value);
    [DllImport(Cf)] private static extern nint CFDictionaryGetCount(IntPtr value);
    [DllImport(Cf)] private static extern void CFDictionaryGetKeysAndValues(IntPtr value,[Out]IntPtr[] keys,[Out]IntPtr[] values);
    [DllImport(Cf)] private static extern bool CFNumberGetValue(IntPtr value,int type,out double number);
    [DllImport(Ct)] private static extern IntPtr CTFontCopyVariation(IntPtr font);
    [DllImport(Ct)] private static extern IntPtr CTFontCreateUIFontForLanguage(int type,double size,IntPtr language);
    [DllImport(Ct)] private static extern IntPtr CTFontCreateCopyWithSymbolicTraits(IntPtr font,double size,IntPtr matrix,uint traits,uint mask);
    [DllImport(Ct)] private static extern IntPtr CTFontDescriptorCreateWithAttributes(IntPtr attributes);
    [DllImport(Ct)] private static extern IntPtr CTFontCreateCopyWithAttributes(IntPtr font,double size,IntPtr matrix,IntPtr descriptor);
    [DllImport(Ct)] private static extern IntPtr CTFontCopyPostScriptName(IntPtr font);
    [DllImport(Ct)] private static extern IntPtr CTTypesetterCreateWithAttributedString(IntPtr text);
    [DllImport(Ct)] private static extern nint CTTypesetterSuggestLineBreak(IntPtr setter,nint start,double width);
    [DllImport(Ct)] private static extern IntPtr CTTypesetterCreateLine(IntPtr setter,Range range);
    [DllImport(Ct)] private static extern double CTLineGetTypographicBounds(IntPtr line,out double ascent,out double descent,out double leading);
    [DllImport(Ct)] private static extern double CTLineGetTrailingWhitespaceWidth(IntPtr line);
}
