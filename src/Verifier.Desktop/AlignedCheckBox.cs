using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Layout;
using Avalonia.VisualTree;

namespace Verifier.Desktop;

// Retain Fluent's states and native checkbox semantics; align its independent glyph container.
public sealed class AlignedCheckBox : CheckBox
{
    protected override Type StyleKeyOverride => typeof(CheckBox);

    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        base.OnApplyTemplate(e);
        if (e.NameScope.Find<Border>("NormalRectangle")?.GetVisualParent() is Grid glyph)
            glyph.VerticalAlignment = VerticalAlignment.Center;
    }
}
