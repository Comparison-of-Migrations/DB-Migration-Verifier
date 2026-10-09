using Avalonia.Controls.Presenters;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Media.TextFormatting;

namespace Verifier.Desktop;

/// <summary>Keep TextBox's editor/IME/hit testing while supplying the source system font's advances.</summary>
internal sealed class WebEditorTextPresenter : TextPresenter
{
    protected override TextLayout CreateTextLayout()
    {
        var native=base.CreateTextLayout();
        // TextPresenter creates a synthetic space for an empty caret line. Preserve its
        // zero text width rather than converting that placeholder into real input.
        if((!OperatingSystem.IsMacOS() && !OperatingSystem.IsWindows()) || (string.IsNullOrEmpty(Text) && string.IsNullOrEmpty(PreeditText)))return native;
        var glyphY=0d;
        if(OperatingSystem.IsWindows())
        {
            var metrics=new WindowsWebTextBlock{FontFamily=FontFamily,FontSize=FontSize,FontWeight=FontWeight,CssLineHeight=LineHeight};
            glyphY=metrics.CssBaseline(TopLevel.GetTopLevel(this)?.RenderScaling ?? 2)-(native.TextLines.FirstOrDefault()?.Baseline ?? 0);
        }
        var runs=new List<ShapedTextRun>();
        foreach(var run in native.TextLines.SelectMany(l=>l.TextRuns).OfType<ShapedTextRun>())
        {
            var old=run.ShapedBuffer;
            var buffer=OperatingSystem.IsMacOS() && old.GlyphTypeface.FamilyName=="System Font"
                ? MacEditorShaping.Shape(old)
                : Copy(old,glyphY);
            runs.Add(new(buffer,run.Properties));
        }
        if(runs.Count==0)return native;
        var properties=new GenericTextRunProperties(new Typeface(FontFamily),FontSize,foregroundBrush:Foreground);
        var paragraph=new GenericTextParagraphProperties(FlowDirection,TextAlignment,true,false,properties,TextWrapping.NoWrap,LineHeight,0,LetterSpacing);
        var result=new TextLayout(new InputSource(runs),paragraph);
        native.Dispose();return result;
    }
    private static ShapedBuffer Copy(ShapedBuffer old,double glyphY)
    {
        var b=new ShapedBuffer(old.Text,old.Length,old.GlyphTypeface,old.FontRenderingEmSize,old.BidiLevel);
        for(var i=0;i<old.Length;i++)
        {var g=old[i];b[i]=new(g.GlyphIndex,g.GlyphCluster,g.GlyphAdvance,new(g.GlyphOffset.X,g.GlyphOffset.Y+glyphY));}
        return b;
    }
    private sealed class InputSource(List<ShapedTextRun> runs):ITextSource
    {
        public TextRun? GetTextRun(int textSourceIndex)
        {
            var start=0;foreach(var run in runs){if(textSourceIndex==start)return run;start+=run.Length;}
            return null;
        }
    }
}
