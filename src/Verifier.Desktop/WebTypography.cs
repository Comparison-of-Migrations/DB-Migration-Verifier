using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;

namespace Verifier.Desktop;

/// <summary>Explicit CSS text metrics; callers supply the original parent role's measured values.</summary>
public static class WebTypography
{
    public static FontFamily SystemFontFamily { get; } =
        new(OperatingSystem.IsMacOS() ? ".AppleSystemUIFont" :
            OperatingSystem.IsWindows() ? WindowsSystemFont.MenuFamily() : "Segoe UI");
    public static FontFamily MonospaceFontFamily { get; } =
        new(OperatingSystem.IsMacOS() ? "Menlo" : "Consolas");

    public static TextBlock Create(string text, double size, double lineHeight,
        IBrush? color = null, int weight = 400, double letterSpacing = 0) => new()
    {
        FontFamily = SystemFontFamily,
        FontSize = size,
        LineHeight = lineHeight,
        FontWeight = (FontWeight)weight,
        LetterSpacing = letterSpacing,
        Foreground = color ?? Brush.Parse("#dbdee1"),
        TextWrapping = TextWrapping.Wrap,
        VerticalAlignment = VerticalAlignment.Top,
        UseLayoutRounding = false,
        Text = text
    };

    // Isolated candidate until typography verification is complete; parents keep their existing contract.
    public static WebSystemTextBlock CreateCandidate(string text, double size, double lineHeight,
        IBrush? color = null, int weight = 400, double letterSpacing = 0,
        bool monospaced = false, bool tabularNumbers = false) => new()
    {
        FontFamily = monospaced ? MonospaceFontFamily : SystemFontFamily, FontSize = size, LineHeight = lineHeight,
        FontWeight = (FontWeight)weight, LetterSpacing = letterSpacing,
        Foreground = color ?? Brush.Parse("#dbdee1"), TextWrapping = TextWrapping.WrapWithOverflow,
        VerticalAlignment = VerticalAlignment.Top, UseLayoutRounding = false, Text = text,
        TabularNumbers = tabularNumbers
    };
}
