using System.Runtime.InteropServices;
using Avalonia.Controls;

/// <summary>Give the native macOS window the same sRGB paint space as CSS rgb()/hex colors.</summary>
namespace Verifier.Desktop;

public static class WebSurfaceColorSpace
{
    public static void Apply(Window window)
    {
        if(!OperatingSystem.IsMacOS())return;
        var handle=window.TryGetPlatformHandle();
        if(handle?.HandleDescriptor!="NSWindow")return;
        var srgb=Send(GetClass("NSColorSpace"),Selector("sRGBColorSpace"));
        Send(handle.Handle,Selector("setColorSpace:"),srgb);
    }
    [DllImport("/usr/lib/libobjc.A.dylib",EntryPoint="objc_getClass")] private static extern IntPtr GetClass(string name);
    [DllImport("/usr/lib/libobjc.A.dylib",EntryPoint="sel_registerName")] private static extern IntPtr Selector(string name);
    [DllImport("/usr/lib/libobjc.A.dylib",EntryPoint="objc_msgSend")] private static extern IntPtr Send(IntPtr target,IntPtr selector);
    [DllImport("/usr/lib/libobjc.A.dylib",EntryPoint="objc_msgSend")] private static extern void Send(IntPtr target,IntPtr selector,IntPtr value);
}
