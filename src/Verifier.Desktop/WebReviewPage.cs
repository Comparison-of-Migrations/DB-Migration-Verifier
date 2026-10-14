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
    public bool LiveMode{get;init;}
    public bool DatabaseScope{get;init;}
    public IReadOnlyList<string> TrialSummary{get;init;}=[];
    public IReadOnlyList<string> ScopeMappings{get;init;}=[];
    public int ScopePageIndex{get;set;}
    public WebResultPager? ScopePager{get;private set;}
    public event EventHandler<int>? ScopePageChanged;
    public IReadOnlyList<string> KnownLimitations{get;init;}=[];
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
        Heading=new();Heading.SetPresentation(T("시작 전에 범위를 확인하세요","Review the scope before starting"),T("이번 검사가 읽는 대상과 확인하지 못할 부분을 한 번에 확인합니다.","Review the databases, selected checks and known limitations together."));Children.Add(Heading);
        Summary=new();Children.Add(Summary);UpdateSummaryViewport(viewportWidth);
        if(!LiveMode&&view.Categories.Contains("access")){PermissionNotice=new(){ReducedMotion=ReducedMotion,Kind="warn",Title=T("권한 조회 2개 항목은 확인 불가로 남습니다","Two permission checks will remain unverified"),Text=T("그 밖의 가능한 검사는 진행하지만 전체 일치로 표시하지 않습니다.","Other available checks can proceed, but the result cannot be presented as fully equal.")};Children.Add(PermissionNotice);}
        if(LiveMode&&KnownLimitations.Count>0){PermissionNotice=new(){ReducedMotion=ReducedMotion,Kind="warn",Title=T("확인된 조회 제한을 함께 기록합니다","Known observation limits are retained"),Text=string.Join(" · ",KnownLimitations)};Children.Add(PermissionNotice);}
        StoppedChoice=new(){Margin=new(0,20,0,0),ReducedMotion=ReducedMotion};StoppedChoice.SetPresentation(T("원본과 대상의 쓰기를 멈춘 상태임을 확인했습니다.","I confirm that writes to source and target are stopped."),"stopped",view.Stopped);
        StoppedChoice.SelectionRequested+=(_,_)=>{var change=new WebFlagSelection(StoppedChoice.IsSelected,StoppedChoice.Checkbox.FocusVisible);SetPresentation(View with {Stopped=change.Selected},_viewport,true,change.FocusVisible);StoppedChanged?.Invoke(this,change);};Children.Add(StoppedChoice);
        Disclaimer=WebTypography.CreateCandidate(T("시안에서의 확인입니다. 앱이 모든 동시 쓰기를 탐지한다는 보장은 아닙니다.","This is a preview confirmation, not a guarantee that all concurrent writes can be detected."),12,19.2,Brush.Parse("#b5bac1"));if(LiveMode)Disclaimer.Text=T("쓰기가 정지된 DB를 읽기 전용으로 검사합니다. 모든 동시 쓰기를 탐지한다고 보장하지 않습니다.","Read-only checks require stopped writes. Detection of every concurrent write is not guaranteed.");Children.Add(Disclaimer);InvalidateMeasure();if(_attached)PlayEntry();
        if(restoreChoice)Dispatcher.UIThread.Post(()=>{if(revision!=_revision||!_attached)return;if(focusVisible)StoppedChoice.Checkbox.Focus(NavigationMethod.Tab);else StoppedChoice.Checkbox.FocusFromPointer();},DispatcherPriority.Loaded);
    }
    public void UpdateSummaryViewport(double width)
    {
        _viewport=width;var view=View;
        string T(string ko,string en)=>view.English?en:ko;
        string Name(string key){var c=Categories.FirstOrDefault(c=>c.Key==key);return c.Key is null?key:T(c.Ko,c.En);}
        var excluded=string.Join(" · ",Categories.Where(c=>!view.Categories.Contains(c.Key)).Select(c=>T(c.Ko,c.En)));
        var boundary=new List<string>{T("원본·대상 읽기 전용","Source and target: read-only")};if(view.Categories.Contains("trial"))boundary.Add(T("동작 시험: 준비한 시험 DB에서만 쓰기","Behavior tests: writes only in the prepared test DB"));if(view.Categories.Contains("trial")&&LiveMode)boundary.AddRange(TrialSummary);
        ScopePageIndex=Math.Clamp(ScopePageIndex,0,WebResultPager.LastPage(ScopeMappings.Count));
        if(ScopePager!=null)Children.Remove(ScopePager);ScopePager=null;
        var paged=LiveMode&&ScopeMappings.Count>WebResultPager.PageSize;
        IEnumerable<string> scopeLines=LiveMode?(paged?Enumerable.Range(ScopePageIndex*WebResultPager.PageSize,Math.Min(WebResultPager.PageSize,ScopeMappings.Count-ScopePageIndex*WebResultPager.PageSize)).Select(i=>ScopeMappings[i]):ScopeMappings):[];
        if(paged)
        {
            ScopePager=new(ScopeMappings.Count,ScopePageIndex,View.English,ReducedMotion,"Review-scope-page",page=>
            {
                var forward=page>ScopePageIndex;ScopePageIndex=page;UpdateSummaryViewport(_viewport);ScopePageChanged?.Invoke(this,page);InvalidateMeasure();
                Dispatcher.UIThread.Post(()=>
                {
                    if(!_attached||ScopePager is not{}pager||pager.PageIndex!=page)return;
                    var button=forward?pager.NextButton:pager.PreviousButton;
                    if(!button.IsEnabled)button=pager.NextButton.IsEnabled?pager.NextButton:pager.PreviousButton;
                    button.Focus();
                },DispatcherPriority.Loaded);
            }){Margin=new(0,12)};Children.Insert(Children.IndexOf(Summary)+1,ScopePager);
        }
        Summary.SetPresentation([
            new("DBMS",[view.Dbms+" · "+(LiveMode?T("실제 연결 확인","Actual connections verified"):T("예시 연결","Sample connections"))],true),
            new(T("원본 → 대상","Source → target"),[view.SourceDatabase+" → "+view.TargetDatabase],false,true,(LiveMode?T(" · 서로 다른 실제 DB로 식별했습니다."," · Source and target identify distinct actual databases."):T("원본과 대상은 서로 다른 예시 서버입니다.","Source and target are separate sample servers."))),
            new(T("선택 분류","Selected categories"),[string.Join(" · ",view.Categories.Select(Name))]),
            new(T("검사 테이블","Selected tables"),new[]{view.TableCount+" "+T("개 · 대응 확인","tables · Matching reviewed")}.Concat(LiveMode?[DatabaseScope?T("DB 범위 포함 · 빈 스키마·독립 객체·DB 권한", "Database scope included · empty schemas, standalone objects and database access"):T("DB 범위 제외 · 선택 테이블과 연결된 객체만", "Database scope excluded · selected tables and their linked objects only")]:[]).Concat(scopeLines).ToArray()),
            new(T("선택하지 않은 분류","Excluded categories"),[excluded.Length>0?excluded:T("없음","None")]),
            new(T("변경 경계","Write boundary"),boundary)
        ],_viewport<=1100,true);
    }
    public void SetFooter(WebActionFooter footer)
    {
        string T(string ko,string en)=>View.English?en:ko;
        footer.SetContent(View.Stopped?(LiveMode?T("확인한 범위로 실제 비교를 시작합니다.","Start actual comparison of the confirmed scope."):T("준비한 범위로 예시 검사를 시작합니다.","Start sample checks for the prepared scope.")):T("쓰기를 멈춘 상태를 확인하세요.","Confirm that writes are stopped."),new("back",T("선택 수정","Edit selection"),"quiet","back"),[new("run",T("비교 시작","Start comparison"),"primary","play",View.Stopped)],ReducedMotion);
    }
    private void PlayEntry(){_entry?.Dispose();_entry=WebMotion.Play(this,WebMotionKind.Page,ReducedMotion);}
    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e){base.OnAttachedToVisualTree(e);_attached=true;PlayEntry();}
    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e){_attached=false;_entry?.Dispose();_entry=null;base.OnDetachedFromVisualTree(e);}
    protected override Size MeasureOverride(Size available){var units=(TopLevel.GetTopLevel(this)?.RenderScaling??2)*64;Disclaimer.LineHeight=Math.Floor(19.2*units)/units;double y=0;foreach(var child in Children){child.Measure(new(available.Width,double.PositiveInfinity));y+=child.DesiredSize.Height;}return new(available.Width,y);}
    protected override Size ArrangeOverride(Size final){double y=0;foreach(var child in Children){child.Arrange(new(0,y,final.Width,child.DesiredSize.Height));y+=child.DesiredSize.Height;}return final;}
}
