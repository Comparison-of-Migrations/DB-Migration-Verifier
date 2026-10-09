using System.Runtime.InteropServices;

namespace Verifier.Desktop;

/// <summary>Chromium's platform scrollbar repeat timings, without writing preferences.</summary>
internal static class NumberSpinTiming
{
    internal readonly record struct Timing(TimeSpan Initial,TimeSpan Period);
    internal static Timing Read()
    {
        if(!OperatingSystem.IsMacOS())return new(TimeSpan.FromMilliseconds(250),TimeSpan.FromMilliseconds(50));
        var defaults=Send(objc_getClass("NSUserDefaults"),sel_registerName("standardUserDefaults"));
        TimeSpan ReadSeconds(string key,float fallback)
        {
            var name=CFStringCreateWithCharacters(IntPtr.Zero,key,key.Length);
            try
            {
                var seconds=Send(defaults,sel_registerName("objectForKey:"),name)==IntPtr.Zero?fallback:
                    Float(defaults,sel_registerName("floatForKey:"),name);
                return TimeSpan.FromSeconds(float.IsFinite(seconds)&&seconds>=0?seconds:fallback);
            }
            finally{CFRelease(name);}
        }
        return new(ReadSeconds("NSScrollerButtonDelay",.5f),ReadSeconds("NSScrollerButtonPeriod",.05f));
    }
    private const string ObjC="/usr/lib/libobjc.A.dylib";
    private const string Cf="/System/Library/Frameworks/CoreFoundation.framework/CoreFoundation";
    [DllImport(ObjC)]private static extern IntPtr objc_getClass(string name);
    [DllImport(ObjC)]private static extern IntPtr sel_registerName(string name);
    [DllImport(ObjC,EntryPoint="objc_msgSend")]private static extern IntPtr Send(IntPtr receiver,IntPtr selector);
    [DllImport(ObjC,EntryPoint="objc_msgSend")]private static extern IntPtr Send(IntPtr receiver,IntPtr selector,IntPtr argument);
    [DllImport(ObjC,EntryPoint="objc_msgSend")]private static extern float Float(IntPtr receiver,IntPtr selector,IntPtr argument);
    [DllImport(Cf)]private static extern IntPtr CFStringCreateWithCharacters(IntPtr allocator,[MarshalAs(UnmanagedType.LPWStr)]string value,nint length);
    [DllImport(Cf)]private static extern void CFRelease(IntPtr value);
}
