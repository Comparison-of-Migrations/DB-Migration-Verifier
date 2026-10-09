using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Threading;
namespace Verifier.Desktop;

public sealed record WebReviewView(bool English, string Dbms, string SourceDatabase, string TargetDatabase, IReadOnlyList<string> Categories, int TableCount, bool Stopped);
public sealed record WebFlagSelection(bool Selected, bool FocusVisible);

/// <summary>Literal review() assembly and stopped-flag replacement. The application owns navigation and execution.</summary>
public sealed class WebReviewPage : Panel
{
    private static readonly (string Key,string Ko,string En)[] Categories = [
        ("data","데이터","Data"),("schema","구조·제약","Schema & constraints"),("access","사용자·권한","Users & permissions"),("objects","관련 객체","Related objects"),("trial","별도 DB 동작 시험","Behavior tests in a separate DB")];
    public WebPageHeading Heading { get; private set; } = new();
    public WebSummaryList Summary { get; private set; } = new();
    public WebNotice? PermissionNotice { get; private set; }
    public WebChoice StoppedChoice { get; private set; } = new();
    public WebSystemTextBlock Disclaimer { get; private set; } = WebTypography.CreateCandidate("",12,19.2,Brush.Parse("#b5bac1"));
    public WebReviewView View { get; private set; } = new(false,"SQL Server","migration_sample","migration_sample",["data","schema","access","objects"],3,false);
    public bool ReducedMotion { get; set; }
    public event EventHandler<WebFlagSelection>? StoppedChanged;
    private double _viewport = 1280;
    private bool _attached;
    private long _revision;
    private WebMotionPlayback? _entry;
    public WebReviewPage() { UseLayoutRounding=false;SetPresentation(View,_viewport); }
    public void SetPresentation(WebReviewView view,double viewportWidth,bool restoreChoice=false,bool focusVisible=false)
    {
        View=view with { Categories=view.Categories.Distinct().ToArray() };view=View;_viewport=viewportWidth;var revision=++_revision;Children.Clear();PermissionNotice=null;
        string T(string ko,string en)=>view.English?en:ko;
        string Name(string key){var c=Categories.FirstOrDefault(c=>c.Key==key);return c.Key is null?key:T(c.Ko,c.En);}
        Heading=new();Heading.SetPresentation(T("시작 전에 범위를 확인하세요","Review the scope before starting"),T("이번 검사가 읽는 대상과 확인하지 못할 부분을 한 번에 확인합니다.","Review the databases, selected checks and known limitations together."));Children.Add(Heading);
        var excluded=string.Join(" · ",Categories.Where(c=>!view.Categories.Contains(c.Key)).Select(c=>T(c.Ko,c.En)));
        var boundary=new List<string>{T("원본·대상 읽기 전용","Source and target: read-only")};if(view.Categories.Contains("trial"))boundary.Add(T("동작 시험: 준비한 시험 DB에서만 쓰기","Behavior tests: writes only in the prepared test DB"));
        Summary=new();Summary.SetPresentation([
            new("DBMS",[view.Dbms+" · "+T("예시 연결","Sample connections")],true),
            new(T("원본 → 대상","Source → target"),[view.SourceDatabase+" → "+view.TargetDatabase],false,true,T("원본과 대상은 서로 다른 예시 서버입니다.","Source and target are separate sample servers.")),
            new(T("선택 분류","Selected categories"),[string.Join(" · ",view.Categories.Select(Name))]),
            new(T("검사 테이블","Selected tables"),[view.TableCount+" "+T("개 · 대응 확인","tables · Matching reviewed")]),
            new(T("선택하지 않은 분류","Excluded categories"),[excluded.Length>0?excluded:T("없음","None")]),
            new(T("변경 경계","Write boundary"),boundary)
        ],viewportWidth<=1100,true);Children.Add(Summary);
        if(view.Categories.Contains("access")){PermissionNotice=new(){ReducedMotion=ReducedMotion,Kind="warn",Title=T("권한 조회 2개 항목은 확인 불가로 남습니다","Two permission checks will remain unverified"),Text=T("그 밖의 가능한 검사는 진행하지만 전체 일치로 표시하지 않습니다.","Other available checks can proceed, but the result cannot be presented as fully equal.")};Children.Add(PermissionNotice);}
        StoppedChoice=new(){Margin=new(0,20,0,0),ReducedMotion=ReducedMotion};StoppedChoice.SetPresentation(T("원본과 대상의 쓰기를 멈춘 상태임을 확인했습니다.","I confirm that writes to source and target are stopped."),"stopped",view.Stopped);
        StoppedChoice.SelectionRequested+=(_,_)=>{var change=new WebFlagSelection(StoppedChoice.IsSelected,StoppedChoice.Checkbox.FocusVisible);SetPresentation(View with {Stopped=change.Selected},_viewport,true,change.FocusVisible);StoppedChanged?.Invoke(this,change);};Children.Add(StoppedChoice);
        Disclaimer=WebTypography.CreateCandidate(T("시안에서의 확인입니다. 앱이 모든 동시 쓰기를 탐지한다는 보장은 아닙니다.","This is a preview confirmation, not a guarantee that all concurrent writes can be detected."),12,19.2,Brush.Parse("#b5bac1"));Children.Add(Disclaimer);InvalidateMeasure();if(_attached)PlayEntry();
        if(restoreChoice)Dispatcher.UIThread.Post(()=>{if(revision!=_revision||!_attached)return;if(focusVisible)StoppedChoice.Checkbox.Focus(NavigationMethod.Tab);else StoppedChoice.Checkbox.FocusFromPointer();},DispatcherPriority.Loaded);
    }
    public void SetFooter(WebActionFooter footer)
    {
        string T(string ko,string en)=>View.English?en:ko;
        footer.SetContent(View.Stopped?T("준비한 범위로 예시 검사를 시작합니다.","Start sample checks for the prepared scope."):T("쓰기를 멈춘 상태를 확인하세요.","Confirm that writes are stopped."),new("back",T("선택 수정","Edit selection"),"quiet","back"),[new("run",T("비교 시작","Start comparison"),"primary","play",View.Stopped)],ReducedMotion);
    }
    private void PlayEntry(){_entry?.Dispose();_entry=WebMotion.Play(this,WebMotionKind.Page,ReducedMotion);}
    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e){base.OnAttachedToVisualTree(e);_attached=true;PlayEntry();}
    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e){_attached=false;_entry?.Dispose();_entry=null;base.OnDetachedFromVisualTree(e);}
    protected override Size MeasureOverride(Size available){var units=(TopLevel.GetTopLevel(this)?.RenderScaling??2)*64;Disclaimer.LineHeight=Math.Floor(19.2*units)/units;double y=0;foreach(var child in Children){child.Measure(new(available.Width,double.PositiveInfinity));y+=child.DesiredSize.Height;}return new(available.Width,y);}
    protected override Size ArrangeOverride(Size final){double y=0;foreach(var child in Children){child.Arrange(new(0,y,final.Width,child.DesiredSize.Height));y+=child.DesiredSize.Height;}return final;}
}
