using Avalonia;
using Avalonia.Automation;

namespace Verifier.Desktop;

/// <summary>The source's .btn.quiet.icon-button, assembled from the locked button.</summary>
public static class WebIconButton
{
    public static WebActionButton Create(string accessibleName, bool reducedMotion = false)
    {
        var button = new WebActionButton
        {
            Variant = "quiet", Width = 44, Height = 44, Padding = new Thickness(0),
            IconName = "close", Content = "", ReducedMotion = reducedMotion,
            // The source has no anonymous label item or gap. Use the locked
            // icon-only absolute slot: 44 - two borders - 18 - 12 = 12.
            AbsoluteTrailingIcon = true, IconRightInset = 12
        };
        SetAccessibleName(button, accessibleName);
        return button;
    }

    public static void SetAccessibleName(WebActionButton button, string accessibleName)
        => AutomationProperties.SetName(button, accessibleName);
}
