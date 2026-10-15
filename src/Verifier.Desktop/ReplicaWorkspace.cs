using Avalonia;
using Avalonia.Automation;
using Avalonia.Automation.Peers;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Threading;
using Avalonia.VisualTree;

namespace Verifier.Desktop;

/// <summary>Incremental source-preview route in the shipping executable. Never opens a database.</summary>
public sealed partial class ReplicaWorkspace : Panel, IDisposable
{
    public WebTopBar TopBar {get;}=new();
    public WebSidebar Sidebar {get;}=new();
    public WebActionFooter Footer {get;}=new();
    public WebScrollViewport Viewport {get;}=new(){PreferNarrowOverflowLayout=true,Background=Brushes.Transparent};
    public Control? CurrentPage => _pageHost.Child;
    public Control SampleStripBox => _strip;
    public Control PageHostBox => _pageHost;
    public WebInfoDialog? InfoDialog=>_infoDialog;
    public WebResultDetails? ResultDetails=>_resultDetails;
    public WebMatchingDialog? MatchingDialog=>_matchingDialog;
    public string ResultFilter {get;private set;}="all";
    public int ResultPageIndex {get;private set;}
    public int SelectionPageIndex{get;private set;}
    public int ReviewScopePageIndex{get;private set;}
    public string Announcement {get;private set;}="";
    public Control AnnouncementRegion {get;}=new AnnouncementMarker(){Width=1,Height=1,ClipToBounds=true,IsHitTestVisible=false};
    public string Screen {get;private set;}="home";
    public bool English {get;private set;}
    public bool ReducedMotion {get;set;}
    public IReadOnlySet<string> Visited => _visited;
    public WebConnectionView Connection {get;private set;}=new WebConnectionPage().View;
    public WebTrialView Trial {get;private set;}=new(false,"trial.example.invalid","migration_test","1","-1",false,false,"");
    public bool Stopped {get;private set;}
    public IReadOnlyList<string> Categories {get;private set;}=["data","schema","access","objects"];
    public IReadOnlyList<string> Tables {get;private set;}=["demo.customers","demo.orders","demo.line_items"];
    public IReadOnlyList<string> Formats {get;private set;}=["json"];
    public IReadOnlyList<string> ExpandedCategories {get;private set;}=[];
    public string TableQuery {get;private set;}="";
    public bool Saved {get;private set;}
    public ReplicaSampleRun? SampleRun {get;private set;}
    public IReadOnlyDictionary<string,string> Maps {get;private set;}=ReplicaSampleRun.AllowedTables.ToDictionary(x=>x,x=>x);
    public Func<ReplicaSampleRun,bool,object[]>? SafeFindingProvider {get;set;}
    public double ShellViewportWidth=>_width;
    public event EventHandler<string>? ExternalActionRequested;
    public event EventHandler<string>? ScreenChanged;
    public event EventHandler? SampleRunChanged;
    private readonly BackgroundLayer _background=new();
    public WebScrollViewport? DocumentVertical {get;private set;}
    public WebCapabilityViewport? DocumentHorizontal {get;private set;}
    private DocumentExtent? _documentExtent;
    private double _documentBodyWidth=1280;
    private WebInfoDialog? _infoDialog;
    private WebResultDetails? _resultDetails;
    private string? _resultDetailKey;
    private WebMatchingDialog? _matchingDialog;
    private string? _matchingSource;
    private bool _permissionsDialog;
    private readonly PageHost _pageHost=new();
    private readonly SampleStrip _strip=new();
    private readonly Dictionary<string,Func<ReplicaWorkspace,Control>> _factories=[];
    private readonly HashSet<string> _visited=[];
    private double _width=1280;
    private bool _disposed,_keyboardModality,_navigationFocusPending;
    private long _revision;
    private readonly DispatcherTimer _runTimer=new(){Interval=TimeSpan.FromMilliseconds(140)};
    private readonly DispatcherTimer _toastTimer=new(){Interval=TimeSpan.FromMilliseconds(4200)};
    private WebSurfaceBox? _toast;
    private WebMotionPlayback? _toastMotion;
    private WebMotionPlayback? _homeEntry;
    private WebMotionPlayback? _trialEntry;

    public ReplicaWorkspace(bool live=false,Verifier.Data.LocalConnectionStore? connectionStore=null)
    {
        Live=live;_connectionStore=connectionStore;
        if(live){Connection=new(false,"PostgreSQL",new("localhost","5432","",""),new("localhost","5432","",""),false,false,false,"");Tables=[];Maps=new Dictionary<string,string>();}
        UseLayoutRounding=false;Background=Brush.Parse("#313338");Focusable=true;KeyboardNavigation.SetIsTabStop(this,false);
        AddHandler(KeyDownEvent,(_,_)=>_keyboardModality=true,Avalonia.Interactivity.RoutingStrategies.Tunnel,true);
        AddHandler(PointerPressedEvent,(_,_)=>_keyboardModality=false,Avalonia.Interactivity.RoutingStrategies.Tunnel,true);
        // A disabled original HTML button still blurs the previous heading to BODY.
        AddHandler(PointerPressedEvent,(_,e)=>{
            if(Screen!="trial"||!e.GetCurrentPoint(this).Properties.IsLeftButtonPressed||Footer.ActionButtons.Count!=1)return;
            var primary=Footer.ActionButtons[0];if(primary.IsEffectivelyEnabled)return;
            var point=e.GetPosition(primary);if(new Rect(primary.Bounds.Size).Contains(point))Focus(NavigationMethod.Pointer);
        },Avalonia.Interactivity.RoutingStrategies.Tunnel,true);
        if(OperatingSystem.IsWindows())
        {
            _documentExtent=new(this);DocumentVertical=new(){Content=_documentExtent};DocumentHorizontal=new();
            // Document scrollbar chrome is not an additional DOM tab stop.
            DocumentHorizontal.PropertyChanged+=(_,e)=>{if(e.Property==InputElement.IsTabStopProperty&&DocumentHorizontal.IsTabStop)DocumentHorizontal.IsTabStop=false;};
            DocumentHorizontal.OffsetChanged+=(_,_)=>_documentExtent.InvalidateArrange();
            Children.Add(DocumentVertical);Children.Add(DocumentHorizontal);
        }else Children.Add(_background);
        _background.Children.Add(TopBar);_background.Children.Add(Sidebar);_background.Children.Add(_strip);_background.Children.Add(Viewport);_background.Children.Add(Footer);
        Viewport.Content=_pageHost;
        Children.Add(AnnouncementRegion);AutomationProperties.SetAutomationId(AnnouncementRegion,"announcer");AutomationProperties.SetLiveSetting(AnnouncementRegion,AutomationLiveSetting.Polite);
        SafeFindingProvider=(run,english)=>{var page=new WebResultsPage();page.SetPresentation(ToResultsRun(run),english,_width);return page.GetSafeFindings().Select(f=>(object)new{id=f.Id,category=f.Key,kind=f.Kind,table=f.Table,column=f.Column,reason=f.Reason}).ToArray();};
        TopBar.LanguageRequested+=(_,_)=>SetLanguage(!English);
        Sidebar.NavigationRequested+=(_,screen)=>{if(!LiveBusy&&Screen!="running"&&screen!=Screen&&_visited.Contains(screen))Navigate(screen);};
        Footer.ActionRequested+=async(_,action)=>await DispatchAsync(action);
        _toastTimer.Tick+=(_,_)=>ClearToast();
        _runTimer.Tick+=(_,_)=>AdvanceSampleRun();
        Render(false);
    }
    public void RegisterPage(string screen,Func<ReplicaWorkspace,Control> factory)
    { _factories[screen]=factory;if(screen==Screen)Render(false); }
    public void Navigate(string screen)
    {
        if(_disposed)return;
        Screen=screen;Connection=Connection with{Error=""};Trial=Trial with{Error=""};if(WebSidebar.StageKeys.Contains(screen))_visited.Add(screen);
        Render(true);ScreenChanged?.Invoke(this,screen);
    }
    public async Task DispatchAsync(string action)
    {
        if(_disposed)return;
        if(Live&&await DispatchLiveAsync(action))return;
        if(action is "research" or "permissions"){ShowInfo(action=="permissions");return;}
        if(action.StartsWith("detail:",StringComparison.Ordinal)){ShowResultDetails(action[7..]);return;}
        if(action.StartsWith("filter:",StringComparison.Ordinal)){ApplyResultFilter(action[7..]);return;}
        if(action=="start") { Reset();Navigate("connect");return; }
        if(action=="sample") {SampleRun=ReplicaSampleRun.Create(Categories,Tables,Maps).CompleteAll();Navigate("results");return;}
        if(action=="restart") { TableQuery="";_runTimer.Stop();SampleRun=null;Saved=false;Stopped=false;Trial=Trial with{Ready=false};Connection=Connection with{Connected=false};_visited.Clear();Navigate("connect");return; }
        if(action=="back") { var previous=Screen switch{"connect"=>"home","capabilities"=>"connect","select"=>"capabilities","review"=>"select","save"=>"results",_=>null};if(previous is not null)Navigate(previous);return; }
        if(Screen=="connect"&&action is "connect" or "preflight")
        {
            if(CurrentPage is not WebConnectionPage page)return;
            if(page.ResolveFooterAction(action)=="preflight") { if(page.View.Connected)Navigate("capabilities");return; }
            var focusedBefore=TopLevel.GetTopLevel(this)?.FocusManager?.GetFocusedElement() as Control;
            var focusIdentity=focusedBefore is null?null:AutomationProperties.GetAutomationId(focusedBefore);
            var focusMethod=_keyboardModality?NavigationMethod.Tab:NavigationMethod.Pointer;
            await page.CheckSampleConnectionsAsync();
            if(_disposed)return;
            Connection=page.View with{English=English};
            if(Screen=="connect"&&!ReferenceEquals(CurrentPage,page))Render(false);
            if(!_disposed&&ReferenceEquals(CurrentPage,page)&&page.View.Connected)
                {Footer.ActionButtons.FirstOrDefault()?.Focus(NavigationMethod.Pointer);Announce(English?"Sample connections checked. Visibility can now be reviewed.":"예시 연결 확인 완료. 조회 범위 확인이 가능합니다.");}
            else if(Screen=="connect"&&!string.IsNullOrEmpty(focusIdentity))
                this.GetVisualDescendants().OfType<Control>().FirstOrDefault(c=>AutomationProperties.GetAutomationId(c)==focusIdentity)?.Focus(focusMethod);
            return;
        }
        if(action=="choose") {Navigate("select");return;}
        if(action=="review") {if(Categories.Count>0&&(!Categories.Contains("trial")||Trial.Ready))Navigate("review");return;}
        if(Screen=="trial"&&action is "trial-check" or "trial-primary")
        {
            if(action=="trial-primary"&&Trial.Ready){Navigate("select");return;}
            if(!Trial.Consent)return;
            string T(string ko,string en)=>English?en:ko;
            if(IsSourceBlank(Trial.ValidInput)||IsSourceBlank(Trial.InvalidInput))
                Trial=Trial with{Error=T("시험 값과 기대 결과를 확인하세요.","Confirm trial inputs and expected outcomes.")};
            else if(IsSourceBlank(Trial.Host)||IsSourceBlank(Trial.Database)||
                (Trial.Host==Connection.Source.Host&&Trial.Database==Connection.Source.Database)||
                (Trial.Host==Connection.Target.Host&&Trial.Database==Connection.Target.Database))
                Trial=Trial with{Error=T("다른 예시 시험 DB를 지정하세요. 분리를 확인할 수 없으면 쓰기 시험은 실행할 수 없습니다.","Choose another sample test database. Writes cannot run without established isolation.")};
            else Trial=Trial with{Ready=true};
            Render(false);
            if(Trial.Ready)ShowToast(T("예시 시험 준비를 확인했습니다. 실제 DB 검증은 아닙니다.","Sample trial setup confirmed. No actual DB was verified."));
            return;
        }
        if(action=="run")
        {
            if(!Stopped)return;
            _runTimer.Stop();SampleRun=ReplicaSampleRun.Create(Categories,Tables,Maps);Navigate("running");_runTimer.Start();return;
        }
        if(action=="cancel")
        {
            if(SampleRun is null)return;
            _runTimer.Stop();SampleRun=SampleRun.Cancel();Render(false);Announce(English?"Checks interrupted. Partial results are available.":"검사가 중단됐습니다. 부분 결과를 확인할 수 있습니다.");SampleRunChanged?.Invoke(this,EventArgs.Empty);return;
        }
        if(action=="save") {Navigate("save");return;}
        if(action=="trial"||action=="trial-back") {Navigate(action=="trial"?"trial":"select");return;}
        if(action=="show-results") {Navigate("results");return;}
        ExternalActionRequested?.Invoke(this,action);
    }
    private static bool IsSourceBlank(string value)=>value.All(c=>c=='\uFEFF'||(c!='\u0085'&&char.IsWhiteSpace(c)));
    private void AdvanceSampleRun()
    {
        if(_disposed||SampleRun is null){_runTimer.Stop();return;}
        var hadCancelFocus=AutomationProperties.GetAutomationId(TopLevel.GetTopLevel(this)?.FocusManager?.GetFocusedElement() as Control ?? this)=="Footer-cancel";
        SampleRun=SampleRun.Tick();
        if(SampleRun.Complete)
        {
            _runTimer.Stop();Render(false);
            if(hadCancelFocus)Footer.ActionButtons.FirstOrDefault()?.Focus(_keyboardModality?NavigationMethod.Tab:NavigationMethod.Pointer);
            Announce(English?"Sample checks finished. Use View results.":"예시 검사 완료. 결과 보기 버튼을 사용하세요.");
        }
        else if(CurrentPage is WebProgressPage progress)progress.Update(SampleRun);
        SampleRunChanged?.Invoke(this,EventArgs.Empty);
    }
    public void ShowInfo(bool permissions,Control? opener=null)
    {
        if(_disposed)return;
        if(_infoDialog is not null){Children.Remove(_infoDialog);_infoDialog=null;}
        _permissionsDialog=permissions;opener??=TopLevel.GetTopLevel(this)?.FocusManager?.GetFocusedElement() as Control;
        var dialog=new WebInfoDialog{ReducedMotion=ReducedMotion};_infoDialog=dialog;Children.Add(dialog);
        var openerIdentity=opener is null?null:AutomationProperties.GetAutomationId(opener);
        dialog.Closed+=(_,_)=>{if(ReferenceEquals(_infoDialog,dialog)){Children.Remove(dialog);_infoDialog=null;
            if(opener is not null&&TopLevel.GetTopLevel(opener) is null&&!string.IsNullOrEmpty(openerIdentity))
                this.GetVisualDescendants().OfType<Control>().FirstOrDefault(c=>AutomationProperties.GetAutomationId(c)==openerIdentity)?.Focus(_keyboardModality?NavigationMethod.Tab:NavigationMethod.Pointer);
            InvalidateMeasure();}};
        dialog.Open(permissions,English,_width<=1180,opener,_background,_keyboardModality?NavigationMethod.Tab:NavigationMethod.Pointer);InvalidateMeasure();
    }
    private string T(string ko,string en)=>English?en:ko;
    private WebSelectionView SelectionView()=>new(English,Categories,Tables,Maps,ExpandedCategories,TableQuery,Trial.Ready,Connection.Dbms,Trial.Database,Trial.Host,Connection.Error,!IsExample){DatabaseScopeAvailable=!IsExample&&LiveDatabaseScopeAvailable,DatabaseScope=!IsExample&&_databaseScope&&LiveDatabaseScopeAvailable,PageIndex=SelectionPageIndex};
    public void ShowMatching(string source,Control? opener=null)
    {
        if(!IsExample){ShowLiveMatching(source,opener);return;}
        if(_disposed||!Maps.ContainsKey(source))return;
        if(_matchingDialog is not null){Children.Remove(_matchingDialog);_matchingDialog=null;}
        _matchingSource=source;opener??=TopLevel.GetTopLevel(this)?.FocusManager?.GetFocusedElement() as Control;
        var dialog=new WebMatchingDialog{ReducedMotion=ReducedMotion};_matchingDialog=dialog;Children.Add(dialog);
        dialog.Applied+=(_,target)=>{
            if(!ReferenceEquals(_matchingDialog,dialog))return;
            var maps=new Dictionary<string,string>(Maps){[source]=target};Maps=maps;Stopped=false;_visited.Remove("review");Render(false);
            ShowToast(T("대응을 적용했습니다. 이름 차이는 검사에 남습니다.","Match applied. Name differences remain part of verification."));
        };
        dialog.Closed+=(_,_)=>{
            if(!ReferenceEquals(_matchingDialog,dialog))return;
            Children.Remove(dialog);_matchingDialog=null;_matchingSource=null;
            if(opener is not null&&TopLevel.GetTopLevel(opener) is null&&CurrentPage is WebSelectionPage selection)
                Dispatcher.UIThread.Post(()=>{
                    if(_disposed||!ReferenceEquals(CurrentPage,selection)||_matchingDialog is not null)return;
                    selection.UpdateLayout();
                    selection.Rows.FirstOrDefault(row=>row.SourceName==source)?.ChangeButton.Focus(_keyboardModality?NavigationMethod.Tab:NavigationMethod.Pointer);
                },DispatcherPriority.Loaded);
            InvalidateMeasure();
        };
        dialog.Open(source,Maps,(!IsExample&&_pair!=null?_pair.TargetInventory.Tables.Select(t=>t.Qualified).ToArray():ReplicaSampleRun.AllowedTables.Append("archive.orders").ToArray()),English,opener,_background);InvalidateMeasure();
    }
    public void ShowToast(string text)
    {
        ClearToast();
        var label=WebTypography.CreateCandidate(text,13,20.8,Brush.Parse("#f2f3f5"));
        _toast=WebSurface.Create(Brush.Parse("#202125"),Brush.Parse("#686e7b"),new(1),new(8),new(new BoxShadow{OffsetY=10,Blur=30,Color=Color.FromArgb(48,0,0,0)}));
        _toast.Padding=new(18,14);_toast.Child=label;_toast.IsHitTestVisible=false;_toast.MaxWidth=420;
        AutomationProperties.SetControlTypeOverride(_toast,AutomationControlType.StatusBar);
        AutomationProperties.SetAccessibilityView(_toast,AccessibilityView.Content);
        AutomationProperties.SetLiveSetting(_toast,AutomationLiveSetting.Polite);
        // CSS shrink-to-fit uses unwrapped max-content, before the label wraps.
        label.Measure(new(double.PositiveInfinity,double.PositiveInfinity));
        var units=(TopLevel.GetTopLevel(this)?.RenderScaling??2)*64;
        _toast.Width=Math.Min(420,Math.Ceiling((label.DesiredSize.Width+38)*units)/units);
        Children.Add(_toast);_toastMotion=WebMotion.Play(_toast,WebMotionKind.Toast,ReducedMotion);_toastTimer.Start();InvalidateMeasure();
    }
    private void ClearToast(){_toastTimer.Stop();_toastMotion?.Dispose();_toastMotion=null;if(_toast is not null){Children.Remove(_toast);_toast=null;}}
    public void SetScope(IReadOnlyList<string> categories,IReadOnlyList<string> tables)
    {Categories=categories.ToArray();Tables=tables.ToArray();Stopped=false;}
    public void SetTrial(WebTrialView view)
    {Trial=view with{English=English};if(Screen=="trial")Render(false);}
    public void LoadSampleReport(ReplicaSampleRun run)
    {_runTimer.Stop();SampleRun=run;ResultFilter="all";ResultPageIndex=0;Tables=run.TableNames.ToArray();Maps=new Dictionary<string,string>(run.Maps);Categories=run.Groups.Select(g=>g.Key).ToArray();_visited.Clear();_visited.UnionWith(["results","save"]);Navigate("results");}
    public void SetSaved(bool requested) {if(Saved==requested)return;Saved=requested;if(Screen=="save")Render(false);}
    public void SetLanguage(bool english)
    {
        var detail=_resultDetailKey;var detailPage=_resultDetails?.Body.PageIndex??0;var info=_infoDialog is not null;var matching=_matchingSource;English=english;
        if(!IsExample&&Screen=="connect"&&Connection.Error.Length>0&&_connectionFailure is{}reason)Connection=Connection with{Error=LiveError(reason)};
        Render(false);
        if(detail is not null)ShowResultDetails(detail,this,detailPage);else if(info)ShowInfo(_permissionsDialog,this);else if(matching is not null)ShowMatching(matching,this);
    }
    private void Reset()
    {SelectionPageIndex=0;ReviewScopePageIndex=0;TableQuery="";ExpandedCategories=[];_runTimer.Stop();Connection=new WebConnectionPage().View;Trial=new(false,"trial.example.invalid","migration_test","1","-1",false,false,"");SampleRun=null;ResultFilter="all";ResultPageIndex=0;Maps=ReplicaSampleRun.AllowedTables.ToDictionary(x=>x,x=>x);Stopped=false;Saved=false;_visited.Clear();Categories=["data","schema","access","objects"];Tables=["demo.customers","demo.orders","demo.line_items"];Formats=["json"];}
    private static WebResultsRun ToResultsRun(ReplicaSampleRun run)=>new(run.Id,run.Groups.Select(g=>new WebResultsRunGroup(g.Key,g.Total,g.Done,g.Equal,g.Different,g.Unknown,g.Status)).ToArray(),run.Interrupted,run.Complete,run.TrialSelected,run.TableNames,run.Maps);
    private void Announce(string text){Announcement=text;((AnnouncementMarker)AnnouncementRegion).SetText(text);}
    private void ApplyResultFilter(string key)
    {
        if(Screen!="results"||key is not("all" or "different" or "unknown"))return;
        if(CurrentPage is not WebResultsPage page)return;
        if(ResultFilter==key&&page.Filter==key)return;
        ResultFilter=key;ResultPageIndex=0;
        if(page.Filter!=key){page.SetFilter(key);page.FocusFilter(key,_keyboardModality);}
    }
    public void ShowResultDetails(string key,Control? opener=null,int pageIndex=0)
    {
        if(_disposed||SampleRun is null)return;
        var index=Array.IndexOf(ReplicaSampleRun.CategoryKeys,key);if(index<0)return;
        if(_resultDetails is not null){Children.Remove(_resultDetails);_resultDetails=null;}
        opener??=TopLevel.GetTopLevel(this)?.FocusManager?.GetFocusedElement() as Control;
        _resultDetailKey=key;var dialog=new WebResultDetails{ReducedMotion=ReducedMotion};_resultDetails=dialog;Children.Add(dialog);
        var identity=opener is null?null:AutomationProperties.GetAutomationId(opener);
        dialog.Closed+=(_,_)=>{if(!ReferenceEquals(_resultDetails,dialog))return;Children.Remove(dialog);_resultDetails=null;_resultDetailKey=null;
            if(opener is not null&&TopLevel.GetTopLevel(opener) is null&&!string.IsNullOrEmpty(identity))
                this.GetVisualDescendants().OfType<Control>().FirstOrDefault(c=>AutomationProperties.GetAutomationId(c)==identity)?.Focus(_keyboardModality?NavigationMethod.Tab:NavigationMethod.Pointer);
            InvalidateMeasure();};
        var group=SampleRun.Groups.FirstOrDefault(g=>g.Key==key);
        dialog.Open(English?DetailNames[index].En:DetailNames[index].Ko,!IsExample?LiveDetailLines(key):(English?DetailChecks[index].En:DetailChecks[index].Ko),key=="trial"&&IsExample,group is null?null:new(group.Done,group.Total,group.Status),English,opener,_background,_keyboardModality?NavigationMethod.Tab:NavigationMethod.Pointer,pageIndex);InvalidateMeasure();
    }
    private static readonly (string Ko,string En)[] DetailNames=[("데이터","Data"),("구조·제약","Schema & constraints"),("사용자·권한","Users & permissions"),("관련 객체","Related objects"),("별도 DB 동작 시험","Behavior tests in a separate DB")];
    private static readonly (string[] Ko,string[] En)[] DetailChecks=[
        (["DATA-01 · 모든 선택 컬럼의 정확한 값과 중복 개수","DATA-02 · 입증 가능한 키·참조·제약 위반"],["DATA-01 · Exact values and duplicate multiplicities","DATA-02 · Evidence of key, reference or constraint violations"]),
        (["SCHEMA-01 · 컬럼 순서·타입·NULL·기본값·계산식","SCHEMA-02 · PK·UNIQUE·FK·CHECK·활성/검증 상태","SCHEMA-03 · 인덱스·partition 키와 경계"],["SCHEMA-01 · Column order, type, NULL, default and expressions","SCHEMA-02 · Keys, references, CHECKs and validation state","SCHEMA-03 · Indexes, partition keys and bounds"]),
        (["ACCESS-01 · 같은 이름 principal·멤버십·권한 정의","ACCESS-02 · RLS·DEFINER·실행 문맥 정의","2개 예시 항목은 조회 범위를 확인하지 못했습니다."],["ACCESS-01 · Same-name principals, membership and grants","ACCESS-02 · RLS, DEFINER and execution context","Full visibility is unconfirmed for 2 sample checks."]),
        (["OBJECT-01 · 정의·signature·소유·참조 연결","OBJECT-02 · 번호 발급 없는 sequence·identity 상태","OBJECT-03 · 사용자 타입·enum·domain·외부 객체"],["OBJECT-01 · Definitions, signatures, owners and references","OBJECT-02 · Sequence and identity state without advancing values","OBJECT-03 · User types, enums, domains and external objects"]),
        (["BEHAVIOR-01 · 확인한 값과 기대 결과의 기본 동작","BEHAVIOR-02 · 선택 역할의 허용·거부·행 제한","앱 소유 자료만 정리하며 남은 변경을 따로 표시합니다."],["BEHAVIOR-01 · Basic behavior using confirmed values and expectations","BEHAVIOR-02 · Allowed, denied and filtered actions of selected roles","Only app-owned data is cleaned up. Remaining changes are separate."])
    ];
    private void SetTrialFooter(bool preservePrimary=false)
    {
        string T(string ko,string en)=>English?en:ko;
        var label=Trial.Ready?T("준비 확인 · 돌아가기","Ready · Return"):T("시험 준비 확인","Confirm test setup");
        if(!preservePrimary||Footer.ActionButtons.Count!=1)
            Footer.SetContent(T("실제 앱은 분리 확인과 시험안 확인이 필요합니다.","The actual app must verify isolation and the trial plan."),new("trial-back",T("검사 선택으로","Back to selection"),"quiet","back"),[new("trial-primary",label,"primary","check",Trial.Ready||Trial.Consent)],ReducedMotion);
        var button=Footer.ActionButtons[0];button.Content=label;button.IconName="check";button.IsEnabled=Trial.Ready||Trial.Consent;
        AutomationProperties.SetAutomationId(button,Trial.Ready?"Footer-trial-back":"Footer-trial-check");
    }
    private void PlayTrialEntry(WebTrialPage page)
    {_trialEntry?.Dispose();_trialEntry=WebMotion.Play(page,WebMotionKind.Page,ReducedMotion);}
    private void Render(bool focus)
    {
        focus|=_navigationFocusPending;_navigationFocusPending=focus;
        var revision=++_revision;var scroll=focus?0:Viewport.Offset;
        var prior=!focus?TopLevel.GetTopLevel(this)?.FocusManager?.GetFocusedElement() as Control:null;
        var identity=prior is not null?AutomationProperties.GetAutomationId(prior):null;
        var keyboard=prior switch{WebActionButton=>_keyboardModality,WebCheckbox c=>c.FocusVisible,WebRadio r=>r.FocusVisible,_=>false};
        TopBar.SetLanguage(English?"en":"ko",ReducedMotion);if(!IsExample)TopBar.PreviewText.Text=English?"Actual DB comparison":"실제 DB 비교";AutomationProperties.SetAutomationId(TopBar.LanguageButton,"language");Sidebar.SetState(English?"en":"ko",Screen,_visited,ReducedMotion);
        _strip.Live=!IsExample;_strip.SetLanguage(English);Footer.SetViewportWidth(_width);Footer.IsVisible=true;
        if(Screen=="home")Footer.SetContent(English?"Explore the sample flow without logging in.":"로그인 없이 예시 흐름을 살펴볼 수 있습니다.",null,[],ReducedMotion);
        Control page;
        if(_factories.TryGetValue(Screen,out var factory))page=factory(this);
        else if(Screen=="home")
        {
            var p=new WebHomePage{ReducedMotion=ReducedMotion};p.SetPresentation(new(English),_width);
            if(!IsExample)
            {
                var history=new WebActionButton{Content=English?"Saved inspection history":"저장한 검사 이력",Variant="quiet",IconName="file",ReducedMotion=ReducedMotion,Margin=new(0,12,0,20)};
                AutomationProperties.SetAutomationId(history,"history");history.Click+=async(_,_)=>await DispatchAsync("history");p.Children.Insert(p.Children.IndexOf(p.Paper),history);
            }
            p.ActionRequested+=async(_,action)=>{if(ReferenceEquals(CurrentPage,p))await DispatchAsync(action);};
            p.AttachedToVisualTree+=(_,_)=>{if(!_disposed&&ReferenceEquals(CurrentPage,p)){_homeEntry?.Dispose();_homeEntry=WebMotion.Play(p,WebMotionKind.Page,ReducedMotion);}};
            page=p;
        }
        else if(Screen=="history")page=CreateHistoryPage();
        else if(Screen=="connect")
        {
            var p=new WebConnectionPage{ReducedMotion=ReducedMotion,LiveMode=!IsExample};
            if(!IsExample){p.SetLiveCredentials(_sourcePassword,_targetPassword,_sourceCa,_targetCa,_writesStopped);p.SetLocalStorage(_savePasswords,LocalStorageMessage());}p.SetPresentation(Connection with{English=English},_width);p.SetFooter(Footer);
            p.LocalStorageActionRequested+=async(_,action)=>{if(ReferenceEquals(CurrentPage,p))await DispatchAsync(action);};
            p.StateChanged+=(_,change)=>{
                if(!ReferenceEquals(CurrentPage,p))return;
                Connection=change.View;
                if(!IsExample){CaptureLiveConnection(p);if(change.Reason is "input" or "dbms")InvalidateLiveConnection();}
                if(change.Reason is "input" or "dbms") {Trial=Trial with{Ready=false,Consent=false};_visited.Clear();_visited.Add("connect");Sidebar.SetState(English?"en":"ko",Screen,_visited,ReducedMotion);}
                p.SetFooter(Footer,change.Reason=="input");
            };page=p;
        }
        else if(Screen=="capabilities")
        {
            var p=new WebPreflightPage{ReducedMotion=ReducedMotion};p.SetPresentation(new(English),_width);p.SetFooter(Footer);
            if(!IsExample)ApplyLiveCapabilities(p);
            p.ActionRequested+=async(_,action)=>{if(ReferenceEquals(CurrentPage,p))await DispatchAsync(action);};page=p;
        }
        else if(Screen=="select")
        {
            var p=new WebSelectionPage{ReducedMotion=ReducedMotion,CapabilityProvider=!IsExample?LiveCapabilityLabel:null,MappingPresentation=!IsExample?LiveMappingPresentation:null};p.SetPresentation(SelectionView(),_width);SelectionPageIndex=p.View.PageIndex;p.SetFooter(Footer);
            p.ViewChanged+=(_,view)=>{
                if(!ReferenceEquals(CurrentPage,p))return;
                if(!ReferenceEquals(Categories,view.Categories))Categories=view.Categories.ToArray();
                if(!ReferenceEquals(Tables,view.Tables))Tables=view.Tables.ToArray();
                _databaseScope=view.DatabaseScope;if(!ReferenceEquals(Maps,view.Maps))Maps=new Dictionary<string,string>(view.Maps);
                SelectionPageIndex=view.PageIndex;
                ExpandedCategories=view.Expanded.ToArray();TableQuery=view.Query;Connection=Connection with{Error=view.Error};
                var reason=p.LastChangeReason;
                if(reason is "database-scope" or "bulk" or "clear-tables"||reason.StartsWith("category:",StringComparison.Ordinal)||reason.StartsWith("table:",StringComparison.Ordinal)){Stopped=false;_visited.Remove("review");if(!IsExample){InvalidateTrial(false);p.SetPresentation(SelectionView(),_width);}}
                p.SetFooter(Footer);
                if(reason.StartsWith("category:",StringComparison.Ordinal))Announce(Categories.Count+T(" 개 분류 선택"," categories selected"));
                else if(reason.StartsWith("expand:",StringComparison.Ordinal))
                {
                    var key=reason[7..];var index=Array.IndexOf(ReplicaSampleRun.CategoryKeys,key);var name=index<0?key:T(DetailNames[index].Ko,DetailNames[index].En);
                    Announce(name+T(" 세부 검사 "," details ")+(ExpandedCategories.Contains(key)?T("펼침","expanded"):T("닫힘","collapsed")));
                }
                else if(reason=="bulk")Announce(T("검색 결과의 선택을 변경했습니다.","Selection updated for matching tables."));
                else if(reason=="clear-tables")Announce(T("검색 결과의 선택을 해제했습니다.","Matching tables were deselected."));
            };
            p.ActionRequested+=async(_,action)=>{
                if(!ReferenceEquals(CurrentPage,p))return;
                if(action.StartsWith("mapping:",StringComparison.Ordinal))ShowMatching(action[8..],p.Rows.FirstOrDefault(row=>row.ScopeKey==action[8..])?.ChangeButton);
                else await DispatchAsync(action);
            };page=p;
        }
        else if(Screen=="trial"&&!IsExample)page=CreateActualTrialPage();
        else if(Screen=="trial")
        {
            Trial=Trial with{English=English};var p=new WebTrialPage{ReducedMotion=ReducedMotion};p.SetPresentation(Trial,_width);SetTrialFooter();
            p.Changed+=(_,view)=>{
                if(!ReferenceEquals(CurrentPage,p))return;
                var input=view.Host!=Trial.Host||view.Database!=Trial.Database||view.ValidInput!=Trial.ValidInput||view.InvalidInput!=Trial.InvalidInput;
                Trial=view;SetTrialFooter(input);
            };
            p.AttachedToVisualTree+=(_,_)=>{if(!_disposed&&ReferenceEquals(CurrentPage,p))PlayTrialEntry(p);};page=p;
        }
        else if(Screen=="running"&&SampleRun is not null)
        {
            var p=new WebProgressPage{ReducedMotion=ReducedMotion};p.SetPresentation(SampleRun,English);p.SetFooter(Footer);if(!IsExample){ApplyLiveProgress(p);ApplyLiveProgressFooter();}page=p;
        }
        else if(Screen=="results"&&SampleRun is not null)
        {
            var p=new WebResultsPage{ReducedMotion=ReducedMotion,ActualFindings=!IsExample?LiveFindings():null};p.SetPresentation(ToResultsRun(SampleRun),English,_width,ResultFilter,ResultPageIndex);p.SetFooter(Footer);if(!IsExample)ApplyLiveResults(p);
            p.PageRequested+=index=>
            {
                if(!ReferenceEquals(CurrentPage,p))return;ResultPageIndex=index;if(!IsExample)ApplyLiveResults(p);
                Dispatcher.UIThread.Post(()=>{if(!_disposed&&ReferenceEquals(CurrentPage,p)){p.UpdateLayout();Viewport.Offset=p.FindingsSection.TranslatePoint(default,p)?.Y??0;}},DispatcherPriority.Loaded);
            };
            p.FilterRequested+=key=>{if(ReferenceEquals(CurrentPage,p))ApplyResultFilter(key);};
            p.ResearchRequested+=opener=>{if(ReferenceEquals(CurrentPage,p))ShowInfo(false,opener);};
            p.DetailRequested+=(key,opener)=>{if(ReferenceEquals(CurrentPage,p))ShowResultDetails(key,opener);};
            p.AttachedToVisualTree+=(_,_)=>Dispatcher.UIThread.Post(()=>{if(focus&&!_disposed&&ReferenceEquals(CurrentPage,p)&&p.IsAttachedToVisualTree())_=p.PlayEntrance();},DispatcherPriority.Loaded);page=p;
        }
        else if(Screen=="review")
        {
            var p=new WebReviewPage{ReducedMotion=ReducedMotion,LiveMode=!IsExample,DatabaseScope=!IsExample&&_databaseScope&&LiveDatabaseScopeAvailable,TrialSummary=!IsExample?ConfirmedTrialSummary():[],ScopeMappings=!IsExample?LiveReviewMappings():[],ScopePageIndex=ReviewScopePageIndex,KnownLimitations=!IsExample?Categories.Where(k=>!LiveCapability(k).Full).Select(k=>k+": "+LiveCapabilityLabel(k).Text).ToArray():[]};p.SetPresentation(new(English,Connection.Dbms,Connection.Source.Database,Connection.Target.Database,Categories,Tables.Count,Stopped),_width);p.SetFooter(Footer);
            p.ScopePageChanged+=(_,index)=>ReviewScopePageIndex=index;
            p.StoppedChanged+=(_,change)=>{Stopped=change.Selected;p.SetFooter(Footer);UpdateSummaryViewport(p,_width);};page=p;
        }
        else if(Screen=="save")
        {
            var p=new WebSavePage{ReducedMotion=ReducedMotion,LiveMode=!IsExample};p.SetPresentation(new(English,Saved,Formats),_width);p.SetFooter(Footer);
            p.FormatChanged+=(_,_)=>{Formats=p.View.Formats;p.SetFooter(Footer);UpdateSummaryViewport(p,_width);};
            p.ActionRequested+=async(_,action)=>await DispatchAsync(action);page=p;
        }
        else page=PendingPage();
        _homeEntry?.Dispose();_homeEntry=null;_trialEntry?.Dispose();_trialEntry=null;_pageHost.Child=page;Viewport.Padding=new(_width<=1180?28:40,_width<=1180?28:36);
        InvalidateMeasure();
        Dispatcher.UIThread.Post(()=>{
            if(_disposed||revision!=_revision)return;
            Viewport.Offset=scroll;
            if(!focus&&!string.IsNullOrEmpty(identity))
            {
                var restored=this.GetVisualDescendants().OfType<Control>().FirstOrDefault(c=>AutomationProperties.GetAutomationId(c)==identity);
                restored?.Focus(keyboard?NavigationMethod.Tab:NavigationMethod.Pointer);Viewport.Offset=scroll;
            }
            if(focus){var heading=page switch{WebHomePage p=>p.Heading,WebConnectionPage p=>p.Heading,WebPreflightPage p=>p.Heading,WebSelectionPage p=>p.Heading,WebActualTrialPage p=>p.Heading,WebTrialPage p=>p.Heading,WebProgressPage p=>p.Heading,WebResultsPage p=>p.Heading,WebReviewPage p=>p.Heading,WebSavePage p=>p.Heading,_=>null};heading?.FocusTitle(false);Announce(heading?.TitleText.Text??"");Viewport.Offset=0;_navigationFocusPending=false;}
        },DispatcherPriority.Loaded);
    }
    private Control PendingPage()
    {
        // Development-only gap marker; it is neither a copied page nor a passed flow.
        var stack=new StackPanel{Spacing=0,UseLayoutRounding=false};
        var heading=new WebPageHeading();heading.SetPresentation(English?"Page integration pending":"페이지 연결 대기",Screen);stack.Children.Add(heading);
        stack.Children.Add(new WebNotice{Title=English?"Incomplete replica route":"미완성 복제 경로",Text=English?"Completed pages are being connected incrementally. This page and the whole application remain unverified.":"완성된 페이지를 순서대로 연결하고 있습니다. 이 페이지와 전체 앱은 아직 미검증입니다.",Kind="warn",ReducedMotion=ReducedMotion});
        if(Screen=="home") {var start=new WebActionButton{Content=English?"Start a new check":"새 검사 시작",Variant="primary",IconName="arrow",ReducedMotion=ReducedMotion};start.Click+=async(_,_)=>await DispatchAsync("start");stack.Children.Add(start);}
        Footer.SetContent(English?"Incomplete page · flow unverified":"미완성 페이지 · 흐름 미검증",null,[],ReducedMotion);return stack;
    }
    // Original CSS resize changes only presentation. Summary text has no independent state.
    private static void UpdateSummaryViewport(Control page,double width)
    {
        if(page is WebReviewPage review)review.UpdateSummaryViewport(width);
        else if(page is WebSavePage save)save.UpdateSummaryViewport(width);
    }
    protected override Size MeasureOverride(Size available)
    {
        var width=double.IsFinite(available.Width)?available.Width:1280;var height=double.IsFinite(available.Height)?available.Height:860;
        if(_width!=width)
        {
            _width=width;
            var updated=true;
            switch(CurrentPage)
            {
                case WebTrialPage trial:trial.SetViewport(width);break;
                case WebConnectionPage connection:connection.SetViewport(width);break;
                case WebHomePage home:home.SetPresentation(home.View,width);break;
                case WebPreflightPage preflight:preflight.SetPresentation(preflight.View,width);if(!IsExample)ApplyLiveCapabilities(preflight);break;
                case WebSelectionPage selection:selection.SetPresentation(SelectionView(),width);break;
                case WebReviewPage review:UpdateSummaryViewport(review,width);break;
                case WebSavePage save:UpdateSummaryViewport(save,width);break;
                case WebProgressPage progress:progress.InvalidateMeasure();break;
                case WebResultsPage results:
                    results.Metrics.ViewportWidth=width;
                    if(results.Chart is{} chart)foreach(var row in chart.Rows)row.Compact=width<=1180;
                    results.InvalidateMeasure();break;
                default:updated=false;break;
            }
            if(!updated)Render(false);
            else {Footer.SetViewportWidth(width);Viewport.Padding=new(width<=1180?28:40,width<=1180?28:36);}
        }
        _documentBodyWidth=width;
        if(DocumentVertical is not null&&DocumentHorizontal is not null&&_documentExtent is not null)
        {
            _documentExtent.ShellHeight=height;DocumentVertical.Measure(new(width,height));
            DocumentHorizontal.IsVisible=true;DocumentHorizontal.LogicalWidth=_documentExtent.BodyWidth;DocumentHorizontal.Measure(new(Math.Max(0,width-DocumentVertical.GutterWidth),double.PositiveInfinity));
            DocumentHorizontal.IsVisible=DocumentHorizontal.GutterHeight>0;
            DocumentVertical.Measure(new(width,Math.Max(0,height-DocumentHorizontal.GutterHeight)));_documentBodyWidth=_documentExtent.BodyWidth;
        }
        _background.Measure(new(_documentBodyWidth,height));_infoDialog?.Measure(new(width,height));_resultDetails?.Measure(new(width,height));_matchingDialog?.Measure(new(width,height));_columnDialog?.Measure(new(width,height));AnnouncementRegion.Measure(new(1,1));
        TopBar.Measure(new(_documentBodyWidth,60));Sidebar.Measure(new(220,Math.Max(0,height-60)));
        _strip.ViewportWidth=width;_strip.Measure(new(Math.Max(0,_documentBodyWidth-220),double.PositiveInfinity));
        Footer.Measure(new(Math.Max(0,_documentBodyWidth-220),double.PositiveInfinity));
        var footerHeight=Screen=="home"?84:Footer.DesiredSize.Height;
        Viewport.Measure(new(Math.Max(0,_documentBodyWidth-220),Math.Max(0,height-60-_strip.DesiredSize.Height-footerHeight)));
        _toast?.Measure(new(420,double.PositiveInfinity));
        return new(width,height);
    }
    protected override Size ArrangeOverride(Size final)
    {
        var width=Math.Max(0,_documentBodyWidth-220);var stripHeight=_strip.DesiredSize.Height;var footHeight=Screen=="home"?84:Footer.DesiredSize.Height;
        if(DocumentVertical is not null&&DocumentHorizontal is not null)
        {
            var h=DocumentHorizontal.GutterHeight;DocumentVertical.Arrange(new(0,0,final.Width,Math.Max(0,final.Height-h)));
            DocumentHorizontal.Arrange(new(0,final.Height-h,Math.Max(0,final.Width-DocumentVertical.GutterWidth),h));
            DocumentVertical.IsTabStop=DocumentHorizontal.IsTabStop=false;
        }else _background.Arrange(new Rect(final));
        // CSS viewport units determine the measured modal size; the browser's
        // containing block excludes document scrollbar chrome when centering.
        var overlay=new Rect(0,0,Math.Max(0,final.Width-(DocumentVertical?.GutterWidth??0)),Math.Max(0,final.Height-(DocumentHorizontal?.GutterHeight??0)));
        _infoDialog?.Arrange(overlay);_resultDetails?.Arrange(overlay);_matchingDialog?.Arrange(overlay);_columnDialog?.Arrange(overlay);AnnouncementRegion.Arrange(new(0,final.Height,1,1));
        TopBar.Arrange(new(0,0,_documentBodyWidth,60));Sidebar.Arrange(new(0,60,220,Math.Max(0,final.Height-60)));
        _strip.Arrange(new(220,60,width,stripHeight));Viewport.Arrange(new(220,60+stripHeight,width,Math.Max(0,final.Height-60-stripHeight-footHeight)));
        Footer.Arrange(new(220,Math.Max(60,final.Height-footHeight),width,footHeight));
        if(_toast is not null)_toast.Arrange(new(overlay.Right-24-_toast.DesiredSize.Width,overlay.Bottom-104-_toast.DesiredSize.Height,_toast.DesiredSize.Width,_toast.DesiredSize.Height));return final;
    }
    public void Dispose(){CancelLive();ReleaseLiveReport();InvalidateTrial(true);_trialSetup.Password="";var pair=_pair;_pair=null;InvalidateScopePlan();if(pair!=null)_=DisposePairQuietly(pair);_sourcePassword=_targetPassword="";_runTimer.Stop();_disposed=true;++_revision;_homeEntry?.Dispose();_homeEntry=null;_trialEntry?.Dispose();_trialEntry=null;ClearToast();if(_infoDialog is not null){Children.Remove(_infoDialog);_infoDialog=null;}if(_resultDetails is not null){Children.Remove(_resultDetails);_resultDetails=null;}if(_columnDialog is not null){Children.Remove(_columnDialog);_columnDialog=null;}if(_matchingDialog is not null){Children.Remove(_matchingDialog);_matchingDialog=null;}_pageHost.Child=null;}
    protected override AutomationPeer OnCreateAutomationPeer()=>new WorkspacePeer(this);
    private sealed class WorkspacePeer(ReplicaWorkspace owner):ControlAutomationPeer(owner)
    {
        protected override IReadOnlyList<AutomationPeer>? GetChildrenCore()
        {
            if(owner._columnDialog is{IsVisible:true} column)return [ControlAutomationPeer.CreatePeerForElement(column)!];
            if(owner._matchingDialog is{IsVisible:true} matching)return [ControlAutomationPeer.CreatePeerForElement(matching)!];
            if(owner._resultDetails is{IsVisible:true} detail)return [ControlAutomationPeer.CreatePeerForElement(detail)!];
            if(owner._infoDialog is{IsVisible:true} dialog)return [ControlAutomationPeer.CreatePeerForElement(dialog)!];
            return base.GetChildrenCore();
        }
    }
    // The original off-screen status is global shell state, not a new visual component.
    private sealed class DocumentExtent:Panel
    {
        private readonly ReplicaWorkspace _owner;
        private double _shellHeight=900;
        public double ShellHeight {get=>_shellHeight;set{if(_shellHeight==value)return;_shellHeight=value;InvalidateMeasure();}}
        public double BodyWidth {get;private set;}=1280;
        public DocumentExtent(ReplicaWorkspace owner){_owner=owner;UseLayoutRounding=false;Children.Add(owner._background);}
        protected override Size MeasureOverride(Size available)
        {
            BodyWidth=Math.Max(960,available.Width);_owner._background.Measure(new(BodyWidth,ShellHeight));return new(BodyWidth,ShellHeight+1);
        }
        protected override Size ArrangeOverride(Size final)
        {
            _owner._background.Arrange(new(-(_owner.DocumentHorizontal?.Offset??0),0,BodyWidth,ShellHeight));return final;
        }
    }
    private sealed class AnnouncementMarker:Panel
    {
        private readonly TextBlock? _text;
        public AnnouncementMarker()
        {
            UseLayoutRounding=false;
            if(OperatingSystem.IsWindows())
            {
                _text=new WindowsWebTextBlock{Text="",FontFamily=WebTypography.SystemFontFamily,FontSize=14,CssLineHeight=22.4,LineHeight=Math.Floor(22.4*2*64+.5)/(2*64),TextWrapping=TextWrapping.WrapWithOverflow,Foreground=Brush.Parse("#f2f3f5"),UseLayoutRounding=false,ClipToBounds=false};
                Children.Add(_text);
            }
        }
        public void SetText(string text)
        {
            if(_text is not null)_text.Text=text;
            else AutomationProperties.SetName(this,text);
        }
        protected override Size MeasureOverride(Size available)
        {
            if(_text is WindowsWebTextBlock windows)
            {var scale=TopLevel.GetTopLevel(this)?.RenderScaling??2;windows.LineHeight=Math.Floor(22.4*scale*64+.5)/(scale*64);}
            _text?.Measure(new(1,double.PositiveInfinity));return new(1,1);
        }
        protected override Size ArrangeOverride(Size final)
        {_text?.Arrange(new(0,0,1,_text.DesiredSize.Height));return final;}
        protected override AutomationPeer OnCreateAutomationPeer()=>new AnnouncementPeer(this);
        private sealed class AnnouncementPeer(AnnouncementMarker owner):ControlAutomationPeer(owner)
        {
            protected override AutomationControlType GetAutomationControlTypeCore()=>OperatingSystem.IsWindows()?AutomationControlType.StatusBar:AutomationControlType.Text;
            protected override string? GetNameCore()=>OperatingSystem.IsWindows()?"":base.GetNameCore();
            protected override bool IsControlElementCore()=>true;
            protected override bool IsContentElementCore()=>true;
            protected override IReadOnlyList<AutomationPeer>? GetChildrenCore()=>owner._text is not{} text?Array.Empty<AutomationPeer>():[ControlAutomationPeer.CreatePeerForElement(text)!];
        }
    }
    private sealed class BackgroundLayer:Panel
    {
        public BackgroundLayer(){UseLayoutRounding=false;}
        protected override Size MeasureOverride(Size available)=>available;
        protected override Size ArrangeOverride(Size final)=>final;
    }
    private sealed class PageHost:Decorator
    {
        public PageHost(){UseLayoutRounding=false;ClipToBounds=false;}
        protected override Size MeasureOverride(Size available){Child?.Measure(new(Math.Min(1120,available.Width),double.PositiveInfinity));return new(available.Width,Child?.DesiredSize.Height??0);}
        protected override Size ArrangeOverride(Size final){var width=Math.Min(1120,final.Width);Child?.Arrange(new((final.Width-width)/2,0,width,Child.DesiredSize.Height));return final;}
    }
    private sealed class SampleStrip:Panel
    {
        public double ViewportWidth=1280;
        private readonly WebSurfaceBox _surface=WebSurface.Create(Brush.Parse("#292b30"),Brush.Parse("#3d3f46"),new(0,0,0,1),default);
        private readonly SvgIcon _icon=new(){IconName="info",Width=18,Height=18,Stroke=Brush.Parse("#b5bac1")};
        private readonly WebSystemTextBlock _text=WebTypography.CreateCandidate("",12,19.2,Brush.Parse("#b5bac1"));
        public SampleStrip(){UseLayoutRounding=false;Children.Add(_surface);Children.Add(_icon);Children.Add(_text);}
        public bool Live{get;set;}
        public void SetLanguage(bool english){_text.Text=Live?(english?"Read-only comparison · Actual database connections. Unverified scopes remain visible.":"읽기 전용 비교 · 실제 DB에 접속합니다. 확인하지 못한 범위도 표시합니다."):english?"Design preview · All databases and outcomes are samples. No real DB connection.":"화면 시안 · 모든 DB와 결과는 예시입니다. 실제 DB에 접속하지 않습니다.";InvalidateMeasure();}
        protected override Size MeasureOverride(Size available){var p=ViewportWidth<=1180?28:40;_icon.Measure(new(18,18));_text.Measure(new(Math.Max(0,available.Width-2*p-26),double.PositiveInfinity));return new(available.Width,Math.Max(36,Math.Max(18,_text.DesiredSize.Height)+15));}
        protected override Size ArrangeOverride(Size final){_surface.Arrange(new Rect(final));var p=ViewportWidth<=1180?28:40;_icon.Arrange(new(p,(final.Height-1-18)/2,18,18));_text.Arrange(new(p+26,(final.Height-1-_text.DesiredSize.Height)/2,Math.Max(0,final.Width-2*p-26),_text.DesiredSize.Height));return final;}
    }
}
