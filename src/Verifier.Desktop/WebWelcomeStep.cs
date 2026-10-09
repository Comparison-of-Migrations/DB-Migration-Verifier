using Avalonia;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Media;
namespace Verifier.Desktop;

public readonly record struct WebWelcomeRowTracks(double Number,double Title,double Body)
{
    public double Height=>Number+Title+Body+16;
}

/// <summary>One original Home article, arranged in its parent's three shared CSS subgrid tracks.</summary>
public sealed class WebWelcomeStep:Panel
{
    public WebSystemTextBlock NumberText{get;}=WebTypography.CreateCandidate("",12,14,Brush.Parse("#b5bac1"),monospaced:true);
    public WebSystemTextBlock TitleText{get;}=WebTypography.CreateCandidate("",14,22.4,Brush.Parse("#f2f3f5"),700);
    public WebSystemTextBlock BodyText{get;}=WebTypography.CreateCandidate("",13,24.05,Brush.Parse("#b5bac1"));
    public WebWelcomeRowTracks NaturalTracks{get;private set;}
    public WebWelcomeRowTracks? SharedTracks{get;private set;}
    public bool English{get;private set;}
    public int Index{get;private set;}
    private static readonly string[] TitlesKo=["두 DB를 연결합니다","범위를 고르고 비교합니다","차이와 미검증을 확인합니다"];
    private static readonly string[] TitlesEn=["Connect two databases","Choose what to compare","Review differences and gaps"];
    private static readonly string[] BodiesKo=["접속 후 앱이 조회 가능한 범위와 필요한 준비를 안내합니다.","데이터·구조·권한·객체를 선택하고 테이블 대응을 확인합니다.","확인한 결과를 구분해 보고, 안전한 결과 파일로 보관합니다."];
    private static readonly string[] BodiesEn=["The app checks visibility and explains any additional preparation.","Select categories and confirm how tables are matched.","Review verified outcomes and save a report without row values."];
    public WebWelcomeStep(){UseLayoutRounding=false;Children.Add(NumberText);Children.Add(TitleText);Children.Add(BodyText);AutomationProperties.SetHeadingLevel(TitleText,3);SetPresentation(0,false);}
    public void SetPresentation(int index,bool english)
    {
        if(index is <0 or >2)throw new ArgumentOutOfRangeException(nameof(index));Index=index;English=english;NumberText.Text=$"0{index+1}";TitleText.Text=(english?TitlesEn:TitlesKo)[index];BodyText.Text=(english?BodiesEn:BodiesKo)[index];AutomationProperties.SetName(TitleText,TitleText.Text);InvalidateMeasure();
    }
    public void SetSharedTracks(WebWelcomeRowTracks? tracks){if(SharedTracks==tracks)return;SharedTracks=tracks;InvalidateMeasure();}
    protected override Size MeasureOverride(Size available)
    {
        var units=(TopLevel.GetTopLevel(this)?.RenderScaling??2)*64;TitleText.LineHeight=Math.Floor(22.4*units)/units;BodyText.LineHeight=Math.Floor(24.05*units)/units;
        foreach(var text in Children)text.Measure(new(available.Width,double.PositiveInfinity));NaturalTracks=new(NumberText.DesiredSize.Height,TitleText.DesiredSize.Height,BodyText.DesiredSize.Height);return new(available.Width,(SharedTracks??NaturalTracks).Height);
    }
    protected override Size ArrangeOverride(Size final)
    {
        var tracks=SharedTracks??NaturalTracks;NumberText.Arrange(new(0,0,final.Width,NumberText.DesiredSize.Height));TitleText.Arrange(new(0,tracks.Number+8,final.Width,TitleText.DesiredSize.Height));BodyText.Arrange(new(0,tracks.Number+tracks.Title+16,final.Width,BodyText.DesiredSize.Height));return final;
    }
}
