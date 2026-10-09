using Avalonia;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Media;
namespace Verifier.Desktop;

/// <summary>Original paper label, research title and explanation, before parent grid/icon/action layout.</summary>
public sealed class WebWelcomePaperContent:Panel
{
    public const string PaperTitle="Functional Success versus Database-State, Behavioral, and Operational Contracts in AI-Generated PostgreSQL Schema Migrations";
    public WebSystemTextBlock LabelText{get;}=WebTypography.CreateCandidate("",12,19.2,Brush.Parse("#b5bac1"));
    public WebSystemTextBlock TitleText{get;}=WebTypography.CreateCandidate(PaperTitle,13,22.1,Brush.Parse("#f2f3f5"),600,-.195);
    public WebSystemTextBlock BodyText{get;}=WebTypography.CreateCandidate("",12,19.2,Brush.Parse("#b5bac1"));
    public bool English{get;private set;}
    public WebWelcomePaperContent(){UseLayoutRounding=false;Children.Add(LabelText);Children.Add(TitleText);Children.Add(BodyText);AutomationProperties.SetHeadingLevel(TitleText,2);SetPresentation(false);}
    public void SetPresentation(bool english)
    {
        English=english;LabelText.Text=english?"Research behind this app":"이 앱의 연구 근거";
        BodyText.Text=english?"The research distinction between functional success and wider contracts informs the check categories and unverified states.":"기본 기능 성공과 넓은 DB 계약 충족을 구분하는 연구를 검사 분류와 미검증 표시에 반영합니다.";
        AutomationProperties.SetName(TitleText,PaperTitle);InvalidateMeasure();
    }
    protected override Size MeasureOverride(Size available)
    {
        var units=(TopLevel.GetTopLevel(this)?.RenderScaling??2)*64;
        LabelText.LineHeight=BodyText.LineHeight=Math.Floor(19.2*units)/units;TitleText.LineHeight=Math.Floor(22.1*units)/units;
        LabelText.Measure(new(available.Width,double.PositiveInfinity));TitleText.Measure(new(Math.Min(740,available.Width),double.PositiveInfinity));BodyText.Measure(new(available.Width,double.PositiveInfinity));
        return new(available.Width,LabelText.DesiredSize.Height+6+TitleText.DesiredSize.Height+4+BodyText.DesiredSize.Height);
    }
    protected override Size ArrangeOverride(Size final)
    {
        LabelText.Arrange(new(0,0,final.Width,LabelText.DesiredSize.Height));
        var y=LabelText.DesiredSize.Height+6;TitleText.Arrange(new(0,y,Math.Min(740,final.Width),TitleText.DesiredSize.Height));
        y+=TitleText.DesiredSize.Height+4;BodyText.Arrange(new(0,y,final.Width,BodyText.DesiredSize.Height));return final;
    }
}
