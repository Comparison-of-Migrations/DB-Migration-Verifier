using System.Runtime.InteropServices;
using Avalonia.Media;
namespace Verifier.Desktop;

/// <summary>Read AppKit's dark drawing palette, without changing OS/application preferences.</summary>
internal static class MacEditorSystemColors
{
    private const string ObjC="/usr/lib/libobjc.A.dylib";
    private const string Cf="/System/Library/Frameworks/CoreFoundation.framework/CoreFoundation";
    internal static Color SelectedBackground()
    {
        var cls=objc_getClass("NSAppearance");var previous=Send(cls,Selector("currentAppearance"));
        const string appearanceName="NSAppearanceNameDarkAqua";
        var name=CFStringCreateWithCharacters(IntPtr.Zero,appearanceName,appearanceName.Length);
        try
        {
            var dark=Send(cls,Selector("appearanceNamed:"),name);
            if(dark==IntPtr.Zero)throw new InvalidOperationException("Cannot read the dark editor palette.");
            Send(cls,Selector("setCurrentAppearance:"),dark);
            var selected=Send(objc_getClass("NSColor"),Selector("selectedTextBackgroundColor"));
            var space=Send(objc_getClass("NSColorSpace"),Selector("sRGBColorSpace"));
            var color=Send(selected,Selector("colorUsingColorSpace:"),space);
            if(color==IntPtr.Zero)throw new InvalidOperationException("Cannot convert the editor palette.");
            byte Channel(string component)=>(byte)Math.Clamp((int)(Math.BitDecrement(256d)*Double(color,Selector(component))),0,255);
            // Chromium's macOS dark selection applies 204/255 alpha to this system color.
            return Color.FromArgb(204,Channel("redComponent"),Channel("greenComponent"),Channel("blueComponent"));
        }
        finally{Send(cls,Selector("setCurrentAppearance:"),previous);CFRelease(name);}
    }
    internal static TimeSpan CaretBlinkInterval()
    {
        if(OperatingSystem.IsMacOSVersionAtLeast(15) && AXPrefersNonBlinkingTextInsertionIndicator())return TimeSpan.Zero;
        var defaults=Send(objc_getClass("NSUserDefaults"),Selector("standardUserDefaults"));
        long Read(string key)
        {
            var name=CFStringCreateWithCharacters(IntPtr.Zero,key,key.Length);
            try{return Integer(defaults,Selector("integerForKey:"),name);}finally{CFRelease(name);}
        }
        var on=Read("NSTextInsertionPointBlinkPeriodOn");var off=Read("NSTextInsertionPointBlinkPeriodOff");
        if(on<0 || off<0 || (on==0 && off==0))return TimeSpan.FromMilliseconds(500);
        return on>60000?TimeSpan.Zero:TimeSpan.FromMilliseconds((on+off)/2);
    }
    [DllImport("/System/Library/Frameworks/Accessibility.framework/Accessibility")]
    [return:MarshalAs(UnmanagedType.I1)]private static extern bool AXPrefersNonBlinkingTextInsertionIndicator();
    [DllImport(ObjC,EntryPoint="objc_msgSend")]private static extern nint Integer(IntPtr receiver,IntPtr selector,IntPtr argument);
    private static IntPtr Selector(string s)=>sel_registerName(s);
    [DllImport(ObjC)]private static extern IntPtr objc_getClass(string name);
    [DllImport(ObjC)]private static extern IntPtr sel_registerName(string name);
    [DllImport(ObjC,EntryPoint="objc_msgSend")]private static extern IntPtr Send(IntPtr receiver,IntPtr selector);
    [DllImport(ObjC,EntryPoint="objc_msgSend")]private static extern IntPtr Send(IntPtr receiver,IntPtr selector,IntPtr argument);
    [DllImport(ObjC,EntryPoint="objc_msgSend")]private static extern double Double(IntPtr receiver,IntPtr selector);
    [DllImport(Cf)]private static extern IntPtr CFStringCreateWithCharacters(IntPtr allocator,[MarshalAs(UnmanagedType.LPWStr)]string value,nint length);
    [DllImport(Cf)]private static extern void CFRelease(IntPtr value);
}
