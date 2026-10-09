using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
namespace Verifier.Desktop;

/// <summary>Literal research and permissions branches inside the original dialog body.</summary>
public sealed class WebInfoDialogBody:Panel
{
    public bool English{get;private set;}
    public bool Permissions{get;private set;}
    public bool ReducedMotion{get;set;}
    public string DialogTitle=>Permissions?(English?"To establish visibility":"조회 범위를 확인하려면"):(English?"Research → Checks → Screens":"연구 결과 → 검사 → 화면");
    public IReadOnlyList<WebSystemTextBlock> Paragraphs=>_paragraphs;
    public WebSummaryList? Summary{get;private set;}
    public WebNotice? Notice{get;private set;}
    private readonly List<WebSystemTextBlock> _paragraphs=[];
    public WebInfoDialogBody(){UseLayoutRounding=false;ClipToBounds=false;SetPresentation(false,false,false);}
    public void SetPresentation(bool permissions,bool english,bool compact)
    {
        English=english;Permissions=permissions;Children.Clear();_paragraphs.Clear();Summary=null;Notice=null;
        string T(string ko,string en)=>english?en:ko;
        WebSystemTextBlock P(string value,bool small=false){var p=WebTypography.CreateCandidate(value,small?12:14,small?19.2:22.4,Brush.Parse("#b5bac1"));_paragraphs.Add(p);Children.Add(p);return p;}
        if(permissions)
        {
            Notice=new(){Margin=default,Kind="warn",ReducedMotion=ReducedMotion,Title=T("권한 정의 2개를 확인하지 못했습니다","Two grant checks remain unverified"),Text=T("실제 앱은 숨겨진 객체 목록과 정의를 확인할 권한을 DBMS별로 안내합니다.","The actual app will explain DBMS-specific permissions needed for hidden catalogs and definitions.")};Children.Add(Notice);
            P(T("관리자 전체 권한을 자동 부여하지 않습니다. 필요한 조회 권한을 준비한 뒤 연결·사전 확인부터 전체 재검사합니다.","The app does not grant admin access. Prepare the necessary read permissions and perform a full new check from connection and preflight."));
        }
        else
        {
            P(T("기본 기능 성공과 데이터 상태·권한·동작의 충족을 구분한 연구 관찰을 다음 방식으로 반영합니다.","The research separates functional success from database-state, permission and behavioral contracts."));
            Summary=new();Summary.SetPresentation([
                new("RP-D01 · RP-C01/02",[T("전체 값과 구조·제약 검사 → 분류별 결과","Full values, schema and constraints → Category outcomes")]),
                new("RP-A01/02 · RP-O01",[T("사용자·권한·객체 정의 → 차이와 조회 한계","Users, permissions and object definitions → Differences and visibility limits")]),
                new("RP-B01",[T("별도 시험 DB 동작 → 기대 충족/불충족과 정리 결과","Separate-DB behavior → Expectations and cleanup outcomes")]),
                new("RP-S01",[T("권한 부족·중단·미지원 → 확인 불가/미검증 표시","Permissions, interruption and unsupported checks → Explicit unverified states")])
            ],compact,false);Children.Add(Summary);
            P(T("현재 보이는 것은 예시 화면 연결입니다. 실제 검증 엔진의 탐지 증거는 아직 없습니다.","This is a sample screen mapping. Detection evidence from a real verification engine is not yet available."),true);
        }
        InvalidateMeasure();
    }
    protected override Size MeasureOverride(Size available)
    {
        var units=(TopLevel.GetTopLevel(this)?.RenderScaling??2)*64;foreach(var p in _paragraphs)p.LineHeight=Math.Floor(p.FontSize*1.6*units)/units;
        double y=Permissions?20:0;foreach(var child in Children){if(child==_paragraphs[^1]&&(Permissions||_paragraphs.Count>1))y+=16;child.Measure(new(available.Width,double.PositiveInfinity));y+=child.DesiredSize.Height;}return new(available.Width,y);
    }
    protected override Size ArrangeOverride(Size final)
    {
        double y=Permissions?20:0;foreach(var child in Children){if(child==_paragraphs[^1]&&(Permissions||_paragraphs.Count>1))y+=16;child.Arrange(new(0,y,final.Width,child.DesiredSize.Height));y+=child.DesiredSize.Height;}return final;
    }
}
