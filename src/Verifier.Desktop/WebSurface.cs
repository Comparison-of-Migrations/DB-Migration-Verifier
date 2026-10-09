using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;

namespace Verifier.Desktop;

/// <summary>Paint-only border box. Each parent supplies its measured source values.</summary>
public static class WebSurface
{
    public static WebSurfaceBox Create(IBrush background, IBrush border, Thickness thickness,
        CornerRadius radius, BoxShadows shadows = default) => new()
    {
        Background = background,
        BorderBrush = border,
        BorderThickness = thickness,
        CornerRadius = radius,
        BoxShadow = shadows,
        UseLayoutRounding = false
    };
}
