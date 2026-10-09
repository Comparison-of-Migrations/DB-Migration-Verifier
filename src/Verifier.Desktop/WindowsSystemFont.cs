using System.ComponentModel;
using System.Runtime.InteropServices;

namespace Verifier.Desktop;

// Chromium resolves Windows CSS system-ui from the menu font, not a fixed Segoe UI.
// SPI_GETNONCLIENTMETRICS reads metadata only; no SPI_SET action or font installation.
internal static class WindowsSystemFont
{
    public static string MenuFamily()
    {
        var metrics = new NonClientMetrics { Size = (uint)Marshal.SizeOf<NonClientMetrics>() };
        if (!SystemParametersInfoW(0x0029, metrics.Size, ref metrics, 0))
            throw new Win32Exception(Marshal.GetLastWin32Error(), "Unable to read Windows system-ui font.");
        return string.IsNullOrWhiteSpace(metrics.MenuFont.FaceName)
            ? throw new InvalidOperationException("Windows menu font family is empty.")
            : metrics.MenuFont.FaceName;
    }

    [DllImport("user32.dll", ExactSpelling = true, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool SystemParametersInfoW(uint action, uint parameter, ref NonClientMetrics metrics, uint flags);

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct LogFont
    {
        public int Height, Width, Escapement, Orientation, Weight;
        public byte Italic, Underline, StrikeOut, CharacterSet, OutPrecision, ClipPrecision, Quality, PitchAndFamily;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)] public string FaceName;
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct NonClientMetrics
    {
        public uint Size;
        public int BorderWidth, ScrollWidth, ScrollHeight, CaptionWidth, CaptionHeight;
        public LogFont CaptionFont;
        public int SmallCaptionWidth, SmallCaptionHeight;
        public LogFont SmallCaptionFont;
        public int MenuWidth, MenuHeight;
        public LogFont MenuFont, StatusFont, MessageFont;
        public int PaddedBorderWidth;
    }
}
