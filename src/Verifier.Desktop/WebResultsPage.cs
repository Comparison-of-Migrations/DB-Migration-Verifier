using System.Diagnostics;
using Avalonia;
using Avalonia.Automation;
using Avalonia.Automation.Peers;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Threading;
using Avalonia.VisualTree;
namespace Verifier.Desktop;

public sealed record WebResultsRunGroup(string Key,int Total,int Done,int Equal,int Different,int Unknown,string Status);
public sealed record WebResultsRun(string Id,IReadOnlyList<WebResultsRunGroup> Groups,bool Interrupted,bool Complete,bool TrialSelected,IReadOnlyList<string> TableNames,IReadOnlyDictionary<string,string> Maps);
public sealed record WebSafeFinding(string Id,string Key,string Kind,string Title,string Table,string Column,string Reason);

/// <summary>Literal results() assembly; the application owns navigation, scroll and modal content.</summary>
public sealed class WebResultsPage:Panel
{
 public IReadOnlyList<WebSafeFinding>? ActualFindings{get;init;}
 private static readonly (string Key,string Ko,string En)[] Categories=[("data","데이터","Data"),("schema","구조·제약","Schema & constraints"),("access","사용자·권한","Users & permissions"),("objects","관련 객체","Related objects"),("trial","별도 DB 동작 시험","Behavior tests in a separate DB")];
 public WebPageHeading Heading{get;private set;}=new();
 public WebMetrics Metrics{get;private set;}=new();
 public WebResultChart? Chart{get;private set;}
 public WebResultsTrialButton? TrialButton{get;private set;}
 public Control? TrialSection{get;private set;}
 public Control FindingsSection{get;private set;}=new Panel();
 public Control SectionHeading{get;private set;}=new Panel();
 public WebSystemTextBlock SectionTitle{get;private set;}=Text("",18,27,"#f2f3f5",700,-.27);
 public WebActionButton ResearchButton{get;private set;}=new();
 public Control FilterRow{get;private set;}=new Panel();
 public IReadOnlyList<WebActionButton> FilterButtons{get;private set;}=[];
 public Control FindingList{get;private set;}=new Panel();
 public IReadOnlyList<WebFindingArticle> Articles{get;private set;}=[];
 public Control? Empty{get;private set;}
 public WebNotice? TrialNotice{get;private set;}
 public WebResultsBreakText Disclaimer{get;private set;}=new("",12,"#b5bac1");
 public WebResultsRun Run{get;private set;}=new("",[],false,false,false,[],new Dictionary<string,string>());
 public bool English{get;private set;}
 public string Filter{get;private set;}="all";
 public int PageIndex{get;private set;}
 public WebResultPager? Pager{get;private set;}
 public event Action<int>? PageRequested;
 public bool ReducedMotion{get;set;}
 public event Action<string>? FilterRequested;
 public event Action<Control>? ResearchRequested;
 public event Action<string,Control>? DetailRequested;
 private double _viewport=1280;private long _revision;private WebMotionPlayback? _entry;
 private string T(string ko,string en)=>English?en:ko;
 public WebResultsPage(){UseLayoutRounding=false;AutomationProperties.SetAccessibilityView(this,AccessibilityView.Raw);}
 internal static WebSystemTextBlock Text(string text,double size,double line,string color="#dbdee1",int weight=400,double spacing=0,bool mono=false)=>WebTypography.CreateCandidate(text,size,line,Brush.Parse(color),weight,spacing,monospaced:mono);
 public void SetPresentation(WebResultsRun run,bool english,double viewportWidth,string filter="all",int? pageIndex=null)
 {
  PageIndex=pageIndex??(run.Id==Run.Id&&filter==Filter?PageIndex:0);
  _entry?.Dispose();_entry=null;++_revision;Run=run with{Groups=run.Groups.ToArray(),TableNames=run.TableNames.ToArray(),Maps=new Dictionary<string,string>(run.Maps)};English=english;_viewport=viewportWidth;Filter=filter;Children.Clear();Chart=null;TrialButton=null;TrialSection=null;TrialNotice=null;Empty=null;
  var different=Run.Groups.Where(g=>g.Key!="trial").Sum(g=>g.Different);var equal=Run.Groups.Where(g=>g.Key!="trial").Sum(g=>g.Equal);var unknown=ActualFindings is null?Run.Groups.Sum(g=>g.Total-g.Equal-g.Different):Run.Groups.Sum(g=>g.Unknown);
  var title=Run.Interrupted?T("중단된 범위가 있습니다","Some checks were interrupted"):different!=0?T("불일치가 발견됐습니다","Differences were found"):unknown!=0?T("확인하지 못한 영역이 있습니다","Some areas remain unverified"):Run.Groups.Any(g=>g.Key=="trial"&&g.Different!=0)?T("동작 시험 기대를 충족하지 못했습니다","A behavior test did not meet expectations"):T("선택 범위 내 일치입니다","Equal within the selected scope");
  Heading=new();Heading.SetPresentation(title,T("값·정의의 차이와 확인하지 못한 부분을 함께 봅니다. 선택하지 않은 범위는 검증한 것으로 표시하지 않습니다.","Review differences alongside unverified areas. Excluded scope is not presented as verified."));Children.Add(Heading);
  Metrics=new(){ViewportWidth=viewportWidth};Metrics.SetPresentation(english,different.ToString(),unknown.ToString(),equal.ToString(),run.Id);if(ActualFindings is not null){Metrics.Items[3].Label.Text=T("검사 ID","Run ID");Metrics.Items[0].Label.Text=T("불일치·위반 항목","Different / violated checks");}Children.Add(Metrics);
  var comparisons=run.Groups.Where(g=>g.Key!="trial").ToArray();if(comparisons.Length>0){Chart=new(){IntegrityIncluded=ActualFindings is not null};Chart.SetGroups(comparisons.Select(g=>new WebResultGroup(g.Key,g.Total,g.Equal,g.Different,g.Status){Unknown=ActualFindings is null?null:g.Unknown}),english,viewportWidth<=1180);Chart.DetailRequested+=key=>{var opener=Chart.Rows.First(r=>r.CategoryKey==key);DetailRequested?.Invoke(key,opener);};Children.Add(Chart);}
  if(run.Groups.FirstOrDefault(g=>g.Key=="trial") is{} trial){var section=new ResultsFlow{Margin=new(0,24,0,24)};var trialTitle=Text(T("별도 DB 동작 시험","Behavior test · Separate DB"),15,22.5,"#f2f3f5",700,-.225);trialTitle.Margin=new(0,0,0,10);section.Children.Add(new HeadingLabel(trialTitle));TrialButton=new(trial,english){ReducedMotion=ReducedMotion};TrialButton.Click+=(_,_)=>DetailRequested?.Invoke("trial",TrialButton);section.Children.Add(TrialButton);TrialSection=section;Children.Add(section);}
  var findings=new ResultsFlow{Margin=new(0,30,0,0)};SectionTitle=Text(T("확인할 차이와 미검증 이유","Differences and reasons for gaps"),18,27,"#f2f3f5",700,-.27);ResearchButton=new ResultsActionButton(){Content=T("연구 연결","Research mapping"),Variant="quiet",IconName="file",ReducedMotion=ReducedMotion};AutomationProperties.SetAutomationId(ResearchButton,"Results-research");ResearchButton.Click+=(_,_)=>ResearchRequested?.Invoke(ResearchButton);SectionHeading=new HeadingFlex(SectionTitle,ResearchButton){Margin=new(0,0,0,12)};findings.Children.Add(SectionHeading);
  var filters=new FiltersFlex{Margin=new(0,0,0,16)};AutomationProperties.SetName(filters,T("결과 필터","Result filter"));var buttons=new List<WebActionButton>();foreach(var pair in new[]{("all",T("전체","All")),("different",ActualFindings is not null?T("불일치·위반","Differences / violations"):T("불일치","Differences")),("unknown",T("확인 불가·미검증","Unverified"))}){var key=pair.Item1;var b=new ResultsFilterButton{Content=pair.Item2,Filter=true,Selected=filter==key,ReducedMotion=ReducedMotion};AutomationProperties.SetAutomationId(b,"Results-filter-"+key);b.Click+=(_,_)=>{if(Filter==key)return;var visible=b.FocusVisible;SetFilter(key);FocusFilter(key,visible);FilterRequested?.Invoke(key);};filters.Children.Add(b);buttons.Add(b);}FilterButtons=buttons;FilterRow=filters;findings.Children.Add(filters);
  FindingsSection=findings;Children.Add(findings);UpdateFindingList();
  if(run.TrialSelected){TrialNotice=new(){Kind="warn",ReducedMotion=ReducedMotion,Title=T("시험 판정과 정리는 별도입니다","Behavior outcomes and cleanup are separate"),Text=run.Groups.FirstOrDefault(g=>g.Key=="trial")?.Status=="completed"?T("예시: 기대 3개 충족·1개 불충족. 앱 생성 자료 정리 실패 1건, 추적 가능한 잔여 T-001과 확인 불가 기록을 별도로 보고합니다. 실제 DB 변경은 없습니다.","Sample: 3 expectations met, 1 unmet. One cleanup failure, a traceable remaining artifact T-001, and unconfirmed effects are reported separately. No real DB was changed."):T("시험은 완료하지 못했습니다. 정리 완료나 원상복구를 주장하지 않습니다.","The trial was not completed. Cleanup or restoration is not claimed.")};Children.Add(TrialNotice);}
  var excluded=string.Join(" · ",Categories.Where(c=>!run.Groups.Any(g=>g.Key==c.Key)).Select(c=>T(c.Ko,c.En)));Disclaimer=new WebResultsBreakText(T("원본 결함: 이 예시에는 입증된 원본 결함이 없습니다. 실제 검사는 위반 근거가 있을 때만 별도로 표시합니다.","Source defects: none are substantiated in this sample. The actual app must require evidence of a violation.")+"\n"+T("선택하지 않은 분류","Excluded categories")+": "+(excluded.Length>0?excluded:T("없음","None")),12,"#b5bac1");if(ActualFindings is not null)Disclaimer.Text=T("원본 결함은 DATA-02의 관측 근거로 별도 확인합니다. 미검증 검사는 결함 없음이나 전체 일치를 뜻하지 않습니다.","Source defects require separate DATA-02 evidence. Unverified checks do not certify absence of defects or full equality.")+"\n"+T("선택하지 않은 분류","Excluded categories")+": "+(excluded.Length>0?excluded:T("없음","None"));Disclaimer.Margin=new(0,20,0,0);Children.Add(Disclaimer);InvalidateMeasure();
 }
 public void SetFilter(string filter)
 {
  if(filter is not("all" or "different" or "unknown")||Filter==filter)return;
  Filter=filter;PageIndex=0;
  var keys=new[]{"all","different","unknown"};
  for(var i=0;i<FilterButtons.Count;i++)FilterButtons[i].Selected=keys[i]==filter;
  UpdateFindingList();
 }
 private void UpdateFindingList()
 {
  ++_revision;var findings=(Panel)FindingsSection;
  if(Pager is not null)findings.Children.Remove(Pager);
  findings.Children.Remove(FindingList);Empty=null;
  var all=GetSafeFindings();var matches=Filter=="all"?all:all is WebResultItems<WebSafeFinding> indexed?indexed.WithTag(Filter):all.Where(f=>f.Kind==Filter).ToArray();
  PageIndex=Math.Clamp(PageIndex,0,WebResultPager.LastPage(matches.Count));Pager=null;
  if(ActualFindings is not null&&matches.Count>WebResultPager.PageSize){Pager=new(matches.Count,PageIndex,English,ReducedMotion,"Results-page",index=>{var next=index>PageIndex;PageIndex=index;UpdateFindingList();PageRequested?.Invoke(index);var button=next?Pager?.NextButton:Pager?.PreviousButton;var fallback=next?Pager?.PreviousButton:Pager?.NextButton;(button?.IsEnabled==true?button:fallback)?.Focus(NavigationMethod.Tab);});Pager.Margin=new(0,0,0,16);findings.Children.Add(Pager);}
  var start=ActualFindings is null?0:PageIndex*WebResultPager.PageSize;var end=ActualFindings is null?matches.Count:(int)Math.Min((long)start+WebResultPager.PageSize,matches.Count);
  var list=new ResultsFlow{Gap=10};var articles=new List<WebFindingArticle>();for(var item=start;item<end;item++){var f=matches[item];var a=new WebFindingArticle{ReducedMotion=ReducedMotion};a.SetPresentation(f.Title,f.Id,f.Table,f.Column,f.Reason);list.Children.Add(a);articles.Add(a);}if(articles.Count==0){var surface=WebSurface.Create(Brush.Parse("#2b2d31"),Brush.Parse("#41434a"),new(1),new(10));surface.Padding=new(36);surface.BoxShadow=BoxShadows.Parse("0 2 4 #10000000");var label=Text(T("이 필터에 해당하는 항목이 없습니다.","No items match this filter."),14,22.4,"#b5bac1");label.TextAlignment=TextAlignment.Center;surface.Child=label;Empty=surface;list.Children.Add(surface);}Articles=articles;FindingList=list;findings.Children.Add(list);
  InvalidateMeasure();
 }
 public IReadOnlyList<WebSafeFinding> GetSafeFindings()
 {
  if(ActualFindings is not null)return ActualFindings;
  var rows=new List<WebSafeFinding>();foreach(var g in Run.Groups){
   if(g.Different!=0&&g.Key!="trial"&&(g.Key!="schema"||Run.TableNames.Contains("demo.orders")))rows.Add(g.Key=="data"?new("F-001",g.Key,"different",T("값 불일치","Value mismatch"),"demo.orders","amount",T("대응 행의 관측 값이 다릅니다. 실제 값과 행 키는 표시하지 않습니다.","An observed value differs in a matched row. Values and row keys are not displayed.")):g.Key=="schema"?new("F-002",g.Key,"different",T("NULL 허용 정의 불일치","NULL definition mismatch"),"demo.orders","reference",T("컬럼의 NULL 허용 속성이 다릅니다. 값 일치와 별도 검사입니다.","The column nullability differs. This is separate from value equality.")):new("F-003",g.Key,"different",T("권한 정의 불일치","Grant definition mismatch"),"—","—",T("내부 객체 PR-003의 권한 정의가 다릅니다. 실제 계정명은 표시하지 않습니다.","Grant definitions differ for internal object PR-003. Actual principal names are not displayed.")));
   if(g.Key=="schema"&&g.Status=="completed")for(var i=0;i<Run.TableNames.Count;i++){var name=Run.TableNames[i];if(Run.Maps.TryGetValue(name,out var map)&&map.Length>0&&map!=name)rows.Add(new("F-MAP-"+(i+1),"schema","different",T("테이블 이름 불일치","Table name mismatch"),name,"—",T("수동 대응한 대상의 이름이 다릅니다. 값 비교와 별도로 이름 차이를 유지합니다.","The manually matched target has a different name. This difference is retained separately from value comparison.")));}
   if(g.Unknown!=0)rows.Add(new("U-001",g.Key,"unknown",T("권한 정의 2개 조회 범위 미확인","Visibility unconfirmed for 2 grant checks"),"—","—",T("목록 가시성을 입증하지 못해 존재/누락을 판정하지 않았습니다.","Catalog completeness was not established; presence or absence was not adjudicated.")));
   if(g.Status!="completed")rows.Add(new("U-"+g.Key,g.Key,"unknown",g.Status=="interrupted"?T("검사 중단","Check interrupted"):T("미시작 검사","Check not started"),"—","—",(g.Total-g.Done)+" "+T("항목을 끝까지 확인하지 못했습니다.","checks have not been completed.")));
  }return rows;
 }
 public void FocusFilter(string key,bool visible=true){var revision=_revision;Dispatcher.UIThread.Post(()=>{if(revision==_revision&&this.IsAttachedToVisualTree())FilterButtons[new[]{"all","different","unknown"}.ToList().IndexOf(key)].Focus(visible?NavigationMethod.Tab:NavigationMethod.Pointer);},DispatcherPriority.Loaded);}
 public void SetFooter(WebActionFooter footer)=>footer.SetContent(T("확인한 범위와 확인 불가 이유를 함께 보관합니다.","Save verified scope and reasons for gaps together."),new("restart",T("전체 재검사","Full new check"),"quiet","refresh"),[new("save",T("결과 저장","Save results"),"primary","save")],ReducedMotion);
 public Task PlayEntrance(){_entry?.Dispose();_entry=WebMotion.Play(this,WebMotionKind.Page,ReducedMotion);return Task.WhenAll(_entry.Completion,Metrics.PlayEntrance(ReducedMotion),Chart?.PlayEntrance(ReducedMotion)??Task.CompletedTask);}
 protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e){++_revision;_entry?.Dispose();_entry=null;base.OnDetachedFromVisualTree(e);}
 protected override Size MeasureOverride(Size a){return ResultsFlow.Measure(Children,a,0);}
 protected override Size ArrangeOverride(Size s){ResultsFlow.Arrange(Children,s,0);return s;}
 internal static double Units(Control c)=>64*(TopLevel.GetTopLevel(c)?.RenderScaling??2);
 internal static void FloorText(WebSystemTextBlock t){var u=Units(t);t.LineHeight=Math.Floor(t.FontSize*1.6*u)/u;}
 internal static double Natural(WebSystemTextBlock t,bool minimum=false){var u=Units(t);t.Measure(new(minimum?1/u:double.PositiveInfinity,double.PositiveInfinity));var width=t.SystemLineWidths.DefaultIfEmpty(0).Max()*u;return (OperatingSystem.IsWindows()&&minimum?Math.Floor(width):Math.Ceiling(width))/u;}
 private sealed class ResultsFlow:Panel
 {
  public double Gap{get;set;}public ResultsFlow(){UseLayoutRounding=false;}
  internal static Size Measure(IEnumerable<Control> children,Size a,double gap){double y=0,bottom=0;var first=true;foreach(var c in children){if(c is WebSystemTextBlock t&&t.FontSize is 12 or 14)FloorText(t);c.Measure(new(a.Width,double.PositiveInfinity));y+=first?c.Margin.Top:Math.Max(bottom,c.Margin.Top)+gap;y+=c.DesiredSize.Height-c.Margin.Top-c.Margin.Bottom;bottom=c.Margin.Bottom;first=false;}return new(a.Width,y+bottom);}
  internal static void Arrange(IEnumerable<Control> children,Size s,double gap){double y=0,bottom=0;var first=true;foreach(var c in children){y+=first?c.Margin.Top:Math.Max(bottom,c.Margin.Top)+gap;c.Arrange(new(0,y-c.Margin.Top,s.Width,c.DesiredSize.Height));y+=c.DesiredSize.Height-c.Margin.Top-c.Margin.Bottom;bottom=c.Margin.Bottom;first=false;}}
  protected override Size MeasureOverride(Size a)=>Measure(Children,a,Gap);protected override Size ArrangeOverride(Size s){Arrange(Children,s,Gap);return s;}
 }
 private sealed class HeadingLabel:Decorator
 {
  private readonly WebSystemTextBlock _text;public HeadingLabel(WebSystemTextBlock text){UseLayoutRounding=false;_text=text;Margin=text.Margin;text.Margin=default;Child=text;AutomationProperties.SetHeadingLevel(this,2);}
  protected override AutomationPeer OnCreateAutomationPeer()=>new HeadingPeer(this);
  private sealed class HeadingPeer(HeadingLabel owner):ControlAutomationPeer(owner){protected override string GetNameCore()=>owner._text.Text??"";protected override AutomationControlType GetAutomationControlTypeCore()=>OperatingSystem.IsWindows()?AutomationControlType.Text:AutomationControlType.Header;protected override IReadOnlyList<AutomationPeer>? GetChildrenCore()=>Array.Empty<AutomationPeer>();}
 }
 private sealed class HeadingFlex(WebSystemTextBlock title,WebActionButton button):Panel
 {
  private double _tw,_bw;public HeadingFlex():this(Text("",18,27),new()){}protected override Size MeasureOverride(Size a){if(Children.Count==0){UseLayoutRounding=false;Children.Add(new HeadingLabel(title));Children.Add(button);}title.Measure(Size.Infinity);button.Measure(Size.Infinity);var u=Units(this);_tw=Math.Ceiling(title.DesiredSize.Width*u)/u;_bw=button.DesiredSize.Width;var free=a.Width-16-_tw-_bw;if(free<0){var weight=_tw+Math.Max(0,_bw-34);_tw=Math.Floor((_tw+free*_tw/weight)*u)/u;_bw=a.Width-16-_tw;}Children[0].Measure(new(_tw,double.PositiveInfinity));button.Measure(new(_bw,double.PositiveInfinity));return new(a.Width,Math.Max(title.DesiredSize.Height,button.DesiredSize.Height));}protected override Size ArrangeOverride(Size s){var u=Units(this);Children[0].Arrange(new(0,Math.Floor((s.Height-title.DesiredSize.Height)*u/2)/u,_tw,title.DesiredSize.Height));button.Arrange(new(s.Width-_bw,Math.Floor((s.Height-button.DesiredSize.Height)*u/2)/u,_bw,button.DesiredSize.Height));return s;}
 }
 private class ResultsActionButton:WebActionButton
 {
  protected override AutomationPeer OnCreateAutomationPeer()=>OperatingSystem.IsWindows()?new ResultsButtonPeer(this):base.OnCreateAutomationPeer();
  private sealed class ResultsButtonPeer(Button owner):ButtonAutomationPeer(owner){protected override IReadOnlyList<AutomationPeer>? GetChildrenCore()=>Array.Empty<AutomationPeer>();}
 }
 private sealed class ResultsFilterButton:ResultsActionButton
 {
  public bool FocusVisible{get;private set;}
  protected override void OnGotFocus(FocusChangedEventArgs e){base.OnGotFocus(e);FocusVisible=e.NavigationMethod!=NavigationMethod.Pointer;}
  protected override void OnKeyDown(KeyEventArgs e){if(e.Key is Key.Space or Key.Enter)FocusVisible=true;base.OnKeyDown(e);}
  protected override void OnPointerPressed(PointerPressedEventArgs e){if(e.GetCurrentPoint(this).Properties.IsLeftButtonPressed){var preserveVisible=IsFocused&&FocusVisible;Focus(NavigationMethod.Pointer);FocusVisible=preserveVisible;}base.OnPointerPressed(e);}
 }
 private sealed class FiltersFlex:Panel
 {
  private readonly List<List<Control>> _lines=[];public FiltersFlex(){UseLayoutRounding=false;}
  protected override Size MeasureOverride(Size a){_lines.Clear();var line=new List<Control>();double used=0;foreach(var c in Children){c.Measure(Size.Infinity);if(line.Count>0&&used+6+c.DesiredSize.Width>a.Width){_lines.Add(line);line=[];used=0;}used+=(line.Count>0?6:0)+c.DesiredSize.Width;line.Add(c);}if(line.Count>0)_lines.Add(line);return new(a.Width,_lines.Sum(l=>l.Max(c=>c.DesiredSize.Height))+6*(_lines.Count-1));}
  protected override Size ArrangeOverride(Size s){double y=0;foreach(var line in _lines){var h=line.Max(c=>c.DesiredSize.Height);double x=0;foreach(var c in line){c.Arrange(new(x,y+(h-c.DesiredSize.Height)/2,c.DesiredSize.Width,c.DesiredSize.Height));x+=c.DesiredSize.Width+6;}y+=h+6;}return s;}
 }
}

/// <summary>Page-specific trial-result-row, preserving its original plain-button grid.</summary>
public sealed class WebResultsTrialButton:Button
{
 private readonly TrialGrid _grid;private readonly WebSurfaceBox _surface;private readonly TrialFocus _focus;private readonly ScaleTransform _scale=new(1,1);private readonly Track _background=new(120);private readonly Track _scaleTrack=new(80);private long _revision;private WebMotionPlayback? _press;
 public bool ReducedMotion{get;set;}public Control Grid=>_grid;public WebStatusPill Pill=>_grid.Pill;public bool FocusVisible=>_focus.Visible;
 public WebResultsTrialButton(WebResultsRunGroup group,bool english)
 {
  UseLayoutRounding=false;Focusable=true;MinHeight=80;FocusAdorner=null;HorizontalAlignment=HorizontalAlignment.Stretch;VerticalAlignment=VerticalAlignment.Top;_grid=new(group,english);_surface=WebSurface.Create(Brush.Parse("#2b2d31"),Brush.Parse("#41434a"),new(1),new(8));_surface.Padding=new(20,14);_surface.Child=_grid;_focus=new(){Child=_surface,UseLayoutRounding=false};Template=new FuncControlTemplate<WebResultsTrialButton>((_,_)=>_focus);RenderTransform=_scale;RenderTransformOrigin=WebMotion.OriginFor(WebMotionKind.Press);AutomationProperties.SetAutomationId(this,"Results-trial");AutomationProperties.SetName(this,string.Join(" ",_grid.Title.Text,_grid.Help.Text,Pill.Text,_grid.Counts.Text).Replace("\n"," "));
 }
 protected override AutomationPeer OnCreateAutomationPeer()=>OperatingSystem.IsWindows()?new TrialButtonPeer(this):base.OnCreateAutomationPeer();
 private sealed class TrialButtonPeer(Button owner):ButtonAutomationPeer(owner){protected override IReadOnlyList<AutomationPeer>? GetChildrenCore()=>Array.Empty<AutomationPeer>();}
 protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs e){base.OnPropertyChanged(e);if(_focus is not null&&(e.Property==IsPointerOverProperty||e.Property==IsPressedProperty))Apply();}
 protected override void OnGotFocus(FocusChangedEventArgs e){base.OnGotFocus(e);_focus.Visible=e.NavigationMethod!=NavigationMethod.Pointer;}protected override void OnLostFocus(FocusChangedEventArgs e){base.OnLostFocus(e);_focus.Visible=false;}
 protected override void OnPointerPressed(PointerPressedEventArgs e){if(e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)_focus.Visible=false;base.OnPointerPressed(e);}protected override void OnKeyDown(KeyEventArgs e){if(e.Key is Key.Space or Key.Enter)_focus.Visible=true;base.OnKeyDown(e);}
 protected override void OnClick(){if(!ReducedMotion){_press?.Dispose();_press=WebMotion.Play(this,WebMotionKind.Press);}base.OnClick();}
 private void Apply(){Cursor=new(StandardCursorType.Hand);_background.Set(IsPointerOver?1:0);_scaleTrack.Set(IsPressed?1:0);var revision=++_revision;if(TopLevel.GetTopLevel(this)is not{} top)return;void Frame(TimeSpan _){if(revision!=_revision||!this.IsAttachedToVisualTree())return;var t=ReducedMotion?(IsPointerOver?1:0):_background.Current;_surface.Background=new SolidColorBrush(Color.FromRgb((byte)Math.Round(43+10*t),(byte)Math.Round(45+10*t),(byte)Math.Round(49+15*t)));var scale=1-.02*(ReducedMotion?(IsPressed?1:0):_scaleTrack.Current);_scale.ScaleX=_scale.ScaleY=scale;if(!ReducedMotion&&(_background.Running||_scaleTrack.Running))top.RequestAnimationFrame(Frame);}Frame(default);}
 protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e){++_revision;_press?.Dispose();_press=null;base.OnDetachedFromVisualTree(e);}
 private sealed class Track(double duration)
 {
  private double _from,_target,_reverse,_start,_duration,_factor=1;private static double Now=>Stopwatch.GetTimestamp()*1000d/Stopwatch.Frequency;private double Progress=>_duration<=0?1:Math.Clamp((Now-_start)/_duration,0,1);public bool Running=>Progress<1;public double Current=>_from+(_target-_from)*WebMotion.SourceEase().Ease(Progress);
  public void Set(double value){if(value==_target)return;var p=Progress;var current=Current;if(p<1&&value==_reverse){_factor=Math.Clamp(Math.Abs(WebMotion.SourceEase().Ease(p)*_factor+1-_factor),0,1);_reverse=_target;}else{_factor=1;_reverse=current;}_from=current;_target=value;_start=Now;_duration=duration*_factor;}
 }

 private sealed class TrialFocus:Decorator{private bool _visible;public bool Visible{get=>_visible;set{_visible=value;InvalidateVisual();}}public override void Render(DrawingContext c){if(_visible)c.DrawRectangle(null,new Pen(Brush.Parse("#a7b1ff"),3),new Rect(-4.5,-4.5,Bounds.Width+9,Bounds.Height+9),11,11);}}
 private sealed class PillInline:Panel
 {
  public WebStatusPill Pill{get;}=new();private readonly WebSystemTextBlock _strut=WebResultsPage.Text("X",14,22.4);private double _offset;
  public PillInline(){UseLayoutRounding=false;Children.Add(Pill);}
  protected override Size MeasureOverride(Size a){WebResultsPage.FloorText(_strut);_strut.Measure(Size.Infinity);Pill.Measure(a);var u=WebResultsPage.Units(this);_offset=Math.Max(0,_strut.SystemBaseline-(Math.Floor((Pill.DesiredSize.Height-6)*u/2)/u+6));return new(Pill.DesiredSize.Width,Math.Max(_strut.DesiredSize.Height,Pill.DesiredSize.Height+_offset));}
  protected override Size ArrangeOverride(Size s){Pill.Arrange(new(0,_offset,s.Width,Pill.DesiredSize.Height));return s;}
 }
 private sealed class TrialGrid:Panel
 {
  public WebSystemTextBlock Title{get;}=WebResultsPage.Text("",13,20.8,weight:700);public WebSystemTextBlock Help{get;}=WebResultsPage.Text("",11,17.6,"#b5bac1");public WebResultsBreakText Counts{get;}=new("",11,"#dbdee1",true);private readonly PillInline _pillLine=new();public WebStatusPill Pill=>_pillLine.Pill;private readonly SvgIcon _chevron=new(){IconName="chevron",Width=18,Height=18,Stroke=Brush.Parse("#dbdee1")};private readonly WebSystemTextBlock _strut=WebResultsPage.Text("X",14,22.4);private double _first,_pill,_counts,_pillHeight;
  public TrialGrid(WebResultsRunGroup group,bool english){UseLayoutRounding=false;Title.Text=english?"Trial expectations":"시험 기대 결과";Help.Text=english?"Separate from comparison · Includes cleanup details":"비교 결과와 별도 판정 · 정리 내역 포함";Counts.Text=(english?"Met / unmet / unverified":"충족 / 불충족 / 미검증")+"\n"+group.Equal+" / "+group.Different+" / "+(group.Total-group.Equal-group.Different);Pill.Kind=group.Status!="completed"?"warn":group.Different!=0?"bad":"ok";Pill.Text=group.Status!="completed"?(group.Status=="interrupted"?(english?"Interrupted · Unverified":"중단 · 미검증"):(english?"Unverified":"미검증")):group.Different!=0?(english?"Expectation unmet":"기대 불충족"):(english?"Expectations met":"기대 충족");foreach(var c in new Control[]{Title,Help,_pillLine,Counts,_chevron})Children.Add(c);if(OperatingSystem.IsWindows())foreach(var t in new[]{Title,Help})AutomationProperties.SetAccessibilityView(t,AccessibilityView.Raw);}
  protected override Size MeasureOverride(Size a){foreach(var t in new[]{Title,Help,_strut})WebResultsPage.FloorText(t);var minFirst=Math.Max(WebResultsPage.Natural(Title,true),WebResultsPage.Natural(Help,true));var minCount=Counts.Natural(true);var maxCount=Counts.Natural();_pillLine.Measure(Size.Infinity);_pill=_pillLine.DesiredSize.Width;if(OperatingSystem.IsWindows()&&minFirst+minCount+_pill+72>a.Width){var units=WebResultsPage.Units(this);_pill=Math.Ceiling(_pill*units)/units;}_counts=Math.Max(minCount,Math.Min(maxCount,a.Width-54-18-_pill-minFirst));_first=Math.Max(minFirst,a.Width-54-18-_pill-_counts);Title.Measure(new(_first,double.PositiveInfinity));Help.Measure(new(_first,double.PositiveInfinity));Counts.Measure(new(_counts,double.PositiveInfinity));_pillHeight=_pillLine.DesiredSize.Height;return new(a.Width,Math.Max(Title.DesiredSize.Height+Help.DesiredSize.Height,Math.Max(_pillHeight,Math.Max(Counts.DesiredSize.Height,18))));}
  protected override Size ArrangeOverride(Size s){var u=WebResultsPage.Units(this);double Center(double h)=>Math.Floor((s.Height-h)*u/2)/u;var h=Title.DesiredSize.Height+Help.DesiredSize.Height;Title.Arrange(new(0,Center(h),_first,Title.DesiredSize.Height));Help.Arrange(new(0,Center(h)+Title.DesiredSize.Height,_first,Help.DesiredSize.Height));_pillLine.Arrange(new(_first+18,Center(_pillHeight),_pill,_pillHeight));Counts.Arrange(new(_first+18+_pill+18,Center(Counts.DesiredSize.Height),_counts,Counts.DesiredSize.Height));_chevron.Arrange(new(_first+18+_pill+18+_counts+18,Center(18),18,18));return s;}
 }
}

// Explicit source <br> is a block-line boundary, not collapsible text whitespace.
public sealed class WebResultsBreakText:Panel
{
 private readonly double _size;private readonly string _color;private readonly bool _mono;private string _text="";
 public string Text{get=>_text;set{_text=value;Children.Clear();foreach(var line in value.Split('\n')){var text=WebResultsPage.Text(line,_size,_size*1.6,_color,mono:_mono);if(OperatingSystem.IsWindows())AutomationProperties.SetAccessibilityView(text,AccessibilityView.Raw);Children.Add(text);}InvalidateMeasure();}}
 public WebResultsBreakText(string text,double size,string color,bool mono=false){UseLayoutRounding=false;AutomationProperties.SetAccessibilityView(this,AccessibilityView.Raw);_size=size;_color=color;_mono=mono;Text=text;}
 public double Natural(bool minimum=false)=>Children.Cast<WebSystemTextBlock>().Select(t=>WebResultsPage.Natural(t,minimum)).DefaultIfEmpty(0).Max();
 protected override Size MeasureOverride(Size a){foreach(var t in Children.Cast<WebSystemTextBlock>()){WebResultsPage.FloorText(t);t.Measure(a);}return new(a.Width,Children.Sum(t=>t.DesiredSize.Height));}
 protected override Size ArrangeOverride(Size s){double y=0;foreach(var t in Children){t.Arrange(new(0,y,s.Width,t.DesiredSize.Height));y+=t.DesiredSize.Height;}return s;}
}
