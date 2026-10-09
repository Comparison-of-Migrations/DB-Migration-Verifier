using Avalonia;
using Avalonia.Controls;
using Avalonia.Automation.Peers;
using Avalonia.Media;
namespace Verifier.Desktop;

/// <summary>The original Home result-file hint, before its entry section layout.</summary>
public sealed class WebWelcomeEntryHint:Panel
{
    public SvgIcon InfoIcon{get;}=new(){IconName="info",Width=15,Height=15,Stroke=Brush.Parse("#b5bac1")};
    public WebSystemTextBlock HintText{get;}=WebTypography.CreateCandidate("",12,19.2,Brush.Parse("#b5bac1"));
    public bool English{get;private set;}
    private double _textWidth;
    private readonly HintHost _hintHost;
    public WebWelcomeEntryHint(){UseLayoutRounding=false;Margin=new(0,14,0,0);_hintHost=new(HintText);Children.Add(InfoIcon);Children.Add(_hintHost);SetPresentation(false);}
    public void SetPresentation(bool english){English=english;HintText.Text=english?"Open a saved result JSON without connecting to a DB or running a new check.":"저장한 결과 JSON은 DB 연결이나 새 검사 없이 바로 열어 확인할 수 있습니다.";InvalidateMeasure();}
    protected override Size MeasureOverride(Size available)
    {
        var units=(TopLevel.GetTopLevel(this)?.RenderScaling??2)*64;HintText.LineHeight=Math.Floor(19.2*units)/units;
        _hintHost.Measure(new(double.PositiveInfinity,double.PositiveInfinity));_textWidth=Math.Min(Math.Ceiling(HintText.DesiredSize.Width*units)/units,Math.Max(0,available.Width-23));_hintHost.Measure(new(_textWidth,double.PositiveInfinity));InfoIcon.Measure(new(15,15));return new(available.Width,Math.Max(15,HintText.DesiredSize.Height));
    }
    protected override Size ArrangeOverride(Size final)
    {
        var units=(TopLevel.GetTopLevel(this)?.RenderScaling??2)*64;double Center(double n)=>Math.Floor(n*units/2)/units;
        InfoIcon.Arrange(new(0,Center(final.Height-15),15,15));_hintHost.Arrange(new(23,Center(final.Height-HintText.DesiredSize.Height),_textWidth,HintText.DesiredSize.Height));return final;
    }
    private sealed class HintHost:Decorator
    {
        public WebSystemTextBlock TextControl{get;}
        public HintHost(WebSystemTextBlock text){UseLayoutRounding=false;TextControl=text;Child=text;}
        protected override AutomationPeer OnCreateAutomationPeer()=>OperatingSystem.IsWindows()?new HintTextPeer(this):base.OnCreateAutomationPeer();
        private sealed class HintTextPeer(HintHost owner):ControlAutomationPeer(owner)
        {
            protected override string GetNameCore()=>owner.TextControl.Text??"";
            protected override AutomationControlType GetAutomationControlTypeCore()=>AutomationControlType.Text;
            protected override IReadOnlyList<AutomationPeer>? GetChildrenCore()=>Array.Empty<AutomationPeer>();
        }
    }

}
