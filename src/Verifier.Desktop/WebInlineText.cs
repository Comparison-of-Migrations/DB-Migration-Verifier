using Avalonia;
using Avalonia.Automation;
using Avalonia.Automation.Peers;
using Avalonia.Controls;
using Avalonia.Media;
namespace Verifier.Desktop;

/// <summary>Same-font inline value and muted suffix: one layout, two complementary paint clips.</summary>
public sealed class WebInlineText:Panel
{
    public WebSystemTextBlock TextControl{get;}=WebTypography.CreateCandidate("",12,22,Brush.Parse("#dbdee1"),monospaced:true,tabularNumbers:true);
    private readonly WebSystemTextBlock _muted=WebTypography.CreateCandidate("",12,22,Brush.Parse("#b5bac1"),monospaced:true,tabularNumbers:true);
    private readonly WebSystemTextBlock _measure=WebTypography.CreateCandidate("",12,22,monospaced:true,tabularNumbers:true);
    public IReadOnlyList<Rect> MutedRegions=>_regions;
    private readonly List<Rect> _regions=[];
    private int _split;
    public WebInlineText(){UseLayoutRounding=false;Children.Add(TextControl);Children.Add(_muted);if(!OperatingSystem.IsWindows())AutomationProperties.SetAccessibilityView(_muted,AccessibilityView.Raw);}
    protected override AutomationPeer OnCreateAutomationPeer() =>
        OperatingSystem.IsMacOS() ? new InlinePeer(this) : base.OnCreateAutomationPeer()!;
    private sealed class InlinePeer(WebInlineText owner) : ControlAutomationPeer(owner)
    {
        protected override string GetNameCore() => owner.TextControl.Text ?? "";
        protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.Text;
        protected override IReadOnlyList<AutomationPeer> GetChildrenCore() => Array.Empty<AutomationPeer>();
        protected override bool IsControlElementCore() => true;
        protected override bool IsContentElementCore() => true;
    }
    public void SetPresentation(string prefix,string suffix)
    {
        var text=WebTextWhitespace.Collapse(prefix+suffix);_split=WebTextWhitespace.Collapse(prefix+"X").Length-1;
        TextControl.Text=_muted.Text=text;
        if(OperatingSystem.IsWindows())
        {
            SetReadingRegion(TextControl,WebTextWhitespace.Collapse(prefix));
            SetReadingRegion(_muted,WebTextWhitespace.Collapse(suffix));
        }
        InvalidateMeasure();
    }
    private static void SetReadingRegion(WebSystemTextBlock control,string name)
    {
        AutomationProperties.SetName(control,name);
        if(name.Length==0)AutomationProperties.SetAccessibilityView(control,AccessibilityView.Raw);
        else control.ClearValue(AutomationProperties.AccessibilityViewProperty);
    }
    protected override Size MeasureOverride(Size available)
    {TextControl.Measure(available);_muted.Measure(available);return new(double.IsFinite(available.Width)?available.Width:TextControl.DesiredSize.Width,TextControl.DesiredSize.Height);}
    protected override Size ArrangeOverride(Size final)
    {
        TextControl.Arrange(new Rect(final));_muted.Arrange(new Rect(final));_regions.Clear();var full=TextControl.Text??"";var offset=0;var normal=new GeometryGroup();var muted=new GeometryGroup();var scale=(TopLevel.GetTopLevel(this)?.RenderScaling??2)*64;
        for(var i=0;full.Length>0&&i<TextControl.SystemLineTexts.Count;i++)
        {
            var line=TextControl.SystemLineTexts[i];var start=full.IndexOf(line,offset,StringComparison.Ordinal);if(start<0)throw new InvalidOperationException("Inline line does not belong to source text");offset=start+line.Length;
            var length=Math.Clamp(_split-start,0,line.Length);double x=0;
            if(length>0){_measure.Text=line[..length]+"\u00a0";_measure.Measure(Size.Infinity);var width=_measure.SystemLineWidths.FirstOrDefault();_measure.Text="\u00a0";_measure.Measure(Size.Infinity);x=Math.Ceiling((width-_measure.SystemLineWidths.FirstOrDefault())*scale)/scale;}
            var y=i*22d;var end=Math.Max(final.Width,Math.Ceiling(TextControl.SystemLineWidths[i]*scale)/scale);
            if(length>0)normal.Children.Add(new RectangleGeometry(new(0,y,length==line.Length?end:x,22)));
            if(length<line.Length){var region=new Rect(x,y,Math.Max(0,end-x),22);muted.Children.Add(new RectangleGeometry(region));_regions.Add(region);}
        }
        TextControl.Clip=normal.Children.Count==0?new RectangleGeometry(new Rect(0,0,0,0)):normal;_muted.Clip=muted.Children.Count==0?new RectangleGeometry(new Rect(0,0,0,0)):muted;return final;
    }
}
