using Avalonia;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Verifier.Core;

namespace Verifier.Desktop;

/// <summary>The original mapping drawer. Connection/query execution stays outside this component.</summary>
public sealed class WebMatchingDialog : Panel
{
    private readonly WebSurfaceBox _surface=WebSurface.Create(Brush.Parse("#2b2d31"),Brush.Parse("#535963"),new(1),new(14,0,0,14));
    private readonly DialogFlow _flow;
    private readonly WebScrollViewport _outerViewport=new();
    private readonly MatchingBody _body;
    private readonly StackPanel _options=new(){Spacing=8,UseLayoutRounding=false};
    private readonly string _group="matching-"+Guid.NewGuid().ToString("N");
    private IReadOnlyDictionary<string,string> _maps=new Dictionary<string,string>();
    private IReadOnlyList<string> _targets=[];
    private Control? _opener,_backgroundContent;
    private IEffect? _previousEffect;
    private bool _previousBackgroundEnabled;
    private WebMotionPlayback? _playback;
    private bool _english,_reduced,_closing,_updating;
    private int _optionPage;
    private bool _dataUnavailable;
    public WebResultPager? Pager{get;private set;}
    public WebActionButton CloseButton{get;}=WebIconButton.Create("닫기");
    public WebActionButton CancelButton{get;}=new(){Variant="quiet"};
    public WebActionButton ApplyButton{get;}=new(){Variant="primary",IconName="check"};
    public WebSearchInput Search{get;}=new(){Context=WebSearchContext.Matching};
    public WebScrollViewport BodyViewport{get;}=new(){Padding=new(24)};
    public WebScrollViewport OptionsViewport{get;}=new(){Padding=new(4)};
    public WebSystemTextBlock TitleText{get;}=WebTypography.CreateCandidate("",18,27,Brush.Parse("#f2f3f5"),700,-.27);
    public WebSystemTextBlock SourceLabel{get;}=WebTypography.CreateCandidate("",12,19.2,Brush.Parse("#b5bac1"));
    public WebSystemTextBlock SourceNameText{get;}=WebTypography.CreateCandidate("",12,19.2,monospaced:true);
    public WebSystemTextBlock TargetLabel{get;}=WebTypography.CreateCandidate("",15,24,Brush.Parse("#f2f3f5"),700);
    public WebSystemTextBlock QueryNote{get;}=WebTypography.CreateCandidate("",12,19.2,Brush.Parse("#b5bac1"));
    public WebSystemTextBlock PolicyText{get;}=WebTypography.CreateCandidate("",12,19.2,Brush.Parse("#b5bac1"));
    public WebSurfaceBox SourceBox{get;}
    public string SourceName{get;private set;}="";
    public string DraftTarget{get;private set;}="";
    public bool ColumnContext{get;init;}
    public Control? ExtraContent{get=>_body.Extra;set=>_body.Extra=value;}
    public bool IsClosing=>_closing;
    public IReadOnlyList<WebMatchOption> Options=>_options.Children.OfType<WebMatchOption>().ToArray();
    public bool ReducedMotion{get=>_reduced;set{_reduced=value;CloseButton.ReducedMotion=CancelButton.ReducedMotion=ApplyButton.ReducedMotion=Search.ReducedMotion=value;foreach(var option in Options)option.ReducedMotion=value;}}
    public event EventHandler<string>? Applied;
    public event EventHandler? Closed;
    public event EventHandler? DraftChanged;
    public WebMatchingDialog()
    {
        UseLayoutRounding=false;Focusable=true;IsTabStop=false;FocusAdorner=null;IsVisible=false;Background=new SolidColorBrush(Color.FromArgb(128,14,15,20));
        KeyboardNavigation.SetTabNavigation(this,KeyboardNavigationMode.Cycle);
        _surface.BoxShadow=new(new BoxShadow{OffsetY=18,Blur=48,Color=Color.FromArgb(36,0,0,0)});
        var sourceFlow=new SourceFlow(this);SourceBox=WebSurface.Create(Brush.Parse("#232428"),Brush.Parse("#4b4f59"),new(1),new(8));SourceBox.Padding=new(16,14);SourceBox.Child=sourceFlow;
        OptionsViewport.Content=_options;_body=new(this);BodyViewport.Content=_body;
        _flow=new(this);_outerViewport.Content=_flow;_surface.Child=_outerViewport;Children.Add(_surface);
        CancelButton.VerticalAlignment=Avalonia.Layout.VerticalAlignment.Stretch;QueryNote.MinHeight=22;
        AutomationProperties.SetAutomationId(this,"MatchingDialog");AutomationProperties.SetAutomationId(CloseButton,"MatchingDialog-close");AutomationProperties.SetAutomationId(CancelButton,"MatchingDialog-cancel");AutomationProperties.SetAutomationId(ApplyButton,"MatchingDialog-apply");AutomationProperties.SetAutomationId(Search.Editor,"MatchingDialog-search");
        AutomationProperties.SetLiveSetting(QueryNote,AutomationLiveSetting.Polite);
        Search.QueryChanged+=(_,_)=>{if(_updating)return;_optionPage=0;RebuildOptions();if(!_dataUnavailable)QueryNote.Text=T("검색은 대응을 바꾸지 않습니다. 선택 후 적용하세요.","Searching does not change the match. Select a target, then apply.");};
        CloseButton.Click+=(_,_)=>_=CloseAsync();CancelButton.Click+=(_,_)=>_=CloseAsync();ApplyButton.Click+=(_,_)=>Apply();
        AddHandler(KeyDownEvent,(_,e)=>{if(e.Key==Key.Escape&&!e.Handled){e.Handled=true;_=CloseAsync();}},RoutingStrategies.Bubble);
        // The source dialog, rather than its disabled label, owns pointer focus.
        // Run after nested scroll boxes, which use focus internally for scrolling.
        AddHandler(PointerPressedEvent,(_,e)=>
        {
            if(!e.GetCurrentPoint(this).Properties.IsLeftButtonPressed||e.Source is not Visual v)return;
            var focusable=v.GetVisualAncestors().Prepend(v).TakeWhile(x=>x!=this).OfType<InputElement>().Any(x=>x.Focusable&&x.IsEffectivelyEnabled&&x is not WebScrollViewport);
            if(!focusable)Focus(NavigationMethod.Pointer);
        },RoutingStrategies.Bubble,true);
        DetachedFromVisualTree+=(_,_)=>{_playback?.Dispose();RestoreBackground();};
    }
    private string T(string ko,string en)=>_english?en:ko;
    public void Open(string source,IReadOnlyDictionary<string,string> maps,IReadOnlyList<string> targets,bool english=false,Control? opener=null,Control? backgroundContent=null)
    {
        if(!maps.TryGetValue(source,out var current))throw new ArgumentException("Source must have a current target.",nameof(source));
        _playback?.Dispose();RestoreBackground();_closing=false;_english=english;SourceName=source;DraftTarget=current;_maps=maps;_targets=targets;_opener=opener;
        _backgroundContent=backgroundContent;if(backgroundContent is not null){_previousEffect=backgroundContent.Effect;_previousBackgroundEnabled=backgroundContent.IsEnabled;backgroundContent.Effect=new BlurEffect{Radius=2};backgroundContent.SetCurrentValue(IsEnabledProperty,false);}
        TitleText.Text=T("테이블 대응 변경","Change table matching");AutomationProperties.SetName(this,TitleText.Text);WebIconButton.SetAccessibleName(CloseButton,T("닫기","Close"));SourceLabel.Text=T("원본 테이블","Source table");SourceNameText.Text=source;TargetLabel.Text=T("대응할 대상 테이블","Choose a target table");
        if(ColumnContext){TitleText.Text=T("컬럼 대응 변경","Change column matching");AutomationProperties.SetName(this,TitleText.Text);SourceLabel.Text=T("원본 컬럼","Source column");TargetLabel.Text=T("대응할 대상 컬럼","Choose a target column");}
        Search.AccessibleName=T("대상 테이블 검색","Search target tables");Search.Placeholder=T("대상 테이블 이름 검색","Search target table names");CancelButton.Content=T("취소","Cancel");ApplyButton.Content=T("대응 적용","Apply matching");
        if(ColumnContext){Search.AccessibleName=T("대상 컬럼 검색","Search target columns");Search.Placeholder=T("대상 컬럼 이름 검색","Search target column names");}
        AutomationProperties.SetName(OptionsViewport,TargetLabel.Text);
        PolicyText.Text=T("일대일 대응만 허용합니다. 이름 차이는 구조 불일치로 남으며 값 변환을 승인하지 않습니다.","Matching must be one-to-one. Name differences remain schema differences; no value transformation is approved.");
        QueryNote.Text=T("이미 다른 원본과 연결된 대상은 선택할 수 없습니다.","Targets already matched to another source are unavailable.");
        _body.Error=null;_dataUnavailable=false;ApplyButton.IsEnabled=true;_optionPage=0;_updating=true;try{Search.Text="";}finally{_updating=false;}RebuildOptions();IsVisible=true;BodyViewport.Offset=OptionsViewport.Offset=_outerViewport.Offset=0;
        if(TopLevel.GetTopLevel(this) is not null){_playback=WebMotion.Play(_surface,WebMotionKind.Drawer,ReducedMotion);CloseButton.Focus(NavigationMethod.Unspecified);}
    }
    private bool Busy(string target)=>_maps.Any(pair=>target.Length>0&&pair.Key!=SourceName&&pair.Value==target);
    private void RebuildOptions()
    {
        if(_dataUnavailable)return;
        try{BuildOptions();}
        catch(Exception e)when(e is VerificationException or IOException or UnauthorizedAccessException or ObjectDisposedException){MarkUnavailable();}
    }
    internal void MarkUnavailable()
    {
        _dataUnavailable=true;ApplyButton.IsEnabled=false;_options.Children.Clear();Pager=null;
        QueryNote.Text=T("대응 정보를 확인하지 못했습니다. 연결을 다시 확인하세요.","Matching information could not be verified. Check the connection again.");
        _body.Error=new WebNotice{Title=T("대응 정보 확인 불가","Matching information unavailable"),Text=QueryNote.Text,Kind="error",ReducedMotion=ReducedMotion};InvalidateMeasure();
    }
    private void BuildOptions()
    {
        // DOM replacement resets the list scroll, while the draft survives filtering.
        _options.Children.Clear();OptionsViewport.Offset=0;Pager=null;var query=Search.Text.Trim().ToLowerInvariant();
        var matches=_targets.Where(name=>name.ToLowerInvariant().Contains(query,StringComparison.Ordinal));var count=matches.Count();
        _optionPage=Math.Clamp(_optionPage,0,WebResultPager.LastPage(count));
        var visiblePage=matches.Skip(_optionPage*WebResultPager.PageSize).Take(WebResultPager.PageSize).ToArray();
        var visible=visiblePage.Where(name=>name.Length>0).ToHashSet(StringComparer.Ordinal);var used=new HashSet<string>(StringComparer.Ordinal);
        foreach(var pair in _maps)if(pair.Key!=SourceName&&visible.Contains(pair.Value))used.Add(pair.Value);
        foreach(var name in visiblePage)
        {
            var busy=used.Contains(name);var option=new WebMatchOption{TargetName=name.Length==0?T("대응 없음","Unmatched"):name,GroupName=_group,Unavailable=busy,ReducedMotion=ReducedMotion,
                Note=busy?T("다른 원본 테이블과 이미 대응되어 있습니다.","Already matched to another source table."):name==SourceName?T("원본과 이름이 같습니다.","Same name as the source."):T("이름 차이는 결과에 남습니다.","Name differences remain in the result."),IsSelected=name==DraftTarget};
            option.Badge=busy?T("사용 중","In use"):option.IsSelected?T("선택","Selected"):"";AutomationProperties.SetAutomationId(option.Radio,"MatchingDialog-choice-"+_options.Children.Count);
            option.Radio.GotFocus+=(_,_)=>RevealFocusedRadio(option.Radio);
            option.SelectionRequested+=(_,_)=>{DraftTarget=name;foreach(var other in Options)other.Badge=other.Unavailable?T("사용 중","In use"):other.IsSelected?T("선택","Selected"):"";QueryNote.Text=T("선택한 대응은 적용 버튼을 눌러 확정합니다.","Press Apply matching to confirm your selection.");DraftChanged?.Invoke(this,EventArgs.Empty);};_options.Children.Add(option);
        }
        if(count>WebResultPager.PageSize)
        {
            Pager=new(count,_optionPage,_english,ReducedMotion,"Matching-page",page=>
            {
                var forward=page>_optionPage;_optionPage=page;RebuildOptions();
                Dispatcher.UIThread.Post(()=>
                {
                    if(!IsVisible||_closing||Pager is not{}pager||pager.PageIndex!=page)return;
                    var button=forward?pager.NextButton:pager.PreviousButton;
                    if(!button.IsEnabled)button=pager.NextButton.IsEnabled?pager.NextButton:pager.PreviousButton;
                    button.Focus();
                },DispatcherPriority.Loaded);
            });_options.Children.Add(Pager);
        }
        if(_options.Children.Count==0){var empty=WebTypography.CreateCandidate(T("검색된 대상이 없습니다. 검색어를 바꿔 보세요.","No matching targets. Try another search."),14,22.4,Brush.Parse("#b5bac1"));empty.TextAlignment=TextAlignment.Center;_options.Children.Add(new Decorator{Padding=new(28),Child=empty,UseLayoutRounding=false});}
        _body.InvalidateMeasure();BodyViewport.InvalidateMeasure();_flow.InvalidateMeasure();_outerViewport.InvalidateMeasure();InvalidateMeasure();
    }
    private void RevealFocusedRadio(WebRadio radio)
    {
        // HTML focus exposes the input in each containing scroll box. The
        // isolated Sidebar viewport contract does not propagate nested requests.
        double Reveal(WebScrollViewport viewport,double y,double height)
        {
            var before=viewport.Offset;var next=before;
            if(y<0)next+=y;else if(y+height>viewport.ClientHeight)next+=y+height-viewport.ClientHeight;
            viewport.Offset=Math.Round(next,MidpointRounding.AwayFromZero);return viewport.Offset-before;
        }
        if(radio.TranslatePoint(default,OptionsViewport) is not { } listPoint||radio.TranslatePoint(default,BodyViewport) is not { } bodyPoint)return;
        var shift=Reveal(OptionsViewport,listPoint.Y,radio.Bounds.Height);shift+=Reveal(BodyViewport,bodyPoint.Y-shift,radio.Bounds.Height);
        if(radio.TranslatePoint(default,_outerViewport) is { } outerPoint)Reveal(_outerViewport,outerPoint.Y-shift,radio.Bounds.Height);
    }
    private void Apply()
    {
        if(_dataUnavailable)return;
        try
        {
        if(Busy(DraftTarget)){_body.Error=new WebNotice{Title=T("대응이 중복됩니다","Matching is duplicated"),Text=T("다른 원본에 이미 대응된 대상은 선택할 수 없습니다.","This target is already matched to another source."),Kind="error",ReducedMotion=ReducedMotion};InvalidateMeasure();return;}
        Applied?.Invoke(this,DraftTarget);_=CloseAsync();
        }
        catch(Exception e)when(e is VerificationException or IOException or UnauthorizedAccessException or ObjectDisposedException){MarkUnavailable();}
    }
    public async Task CloseAsync()
    {
        if(!IsVisible||_closing)return;_closing=true;_playback?.Dispose();
        _playback=WebMotion.Play(_surface,WebMotionKind.ModalClose,ReducedMotion);
        try{await _playback.Completion;}catch(TaskCanceledException){return;}
        IsVisible=false;RestoreBackground();_opener?.Focus(NavigationMethod.Unspecified);Closed?.Invoke(this,EventArgs.Empty);
    }
    private void RestoreBackground(){if(_backgroundContent is not null){_backgroundContent.Effect=_previousEffect;_backgroundContent.SetCurrentValue(IsEnabledProperty,_previousBackgroundEnabled);}_backgroundContent=null;_previousEffect=null;}
    protected override Size MeasureOverride(Size available)
    {
        var width=Math.Max(0,Math.Min(500,available.Width-32));if(_flow.BodyMaximum!=.65*available.Height){_flow.BodyMaximum=.65*available.Height;_flow.InvalidateMeasure();}
        _flow.Measure(new(Math.Max(0,width-2),double.PositiveInfinity));var height=Math.Min(_flow.DesiredSize.Height+2,Math.Max(0,available.Height-48));_surface.Measure(new(width,height));return available;
    }
    protected override Size ArrangeOverride(Size final)
    {
        var width=Math.Max(0,Math.Min(500,final.Width-32));var height=Math.Min(_surface.DesiredSize.Height,Math.Max(0,final.Height-48));
        var units=(TopLevel.GetTopLevel(this)?.RenderScaling??2)*64;var y=Math.Floor((final.Height-height)*units/2)/units;
        _surface.Arrange(new(final.Width-20-width,y,width,height));_outerViewport.Clip=DrawerInnerClip(_outerViewport.Bounds.Size);return final;
    }
    private static Geometry DrawerInnerClip(Size size)
    {
        // The drawer has rounded left corners and square right corners. Its
        // scrolling content clips at the inner edge of the one-pixel border.
        var radius=Math.Min(13,Math.Min(size.Width,size.Height/2));var clip=new StreamGeometry();
        using(var path=clip.Open())
        {
            path.BeginFigure(new(radius,0),true);path.LineTo(new(size.Width,0));path.LineTo(new(size.Width,size.Height));path.LineTo(new(radius,size.Height));
            if(radius>0)path.ArcTo(new(0,size.Height-radius),new(radius,radius),0,false,SweepDirection.Clockwise);
            path.LineTo(new(0,radius));if(radius>0)path.ArcTo(new(radius,0),new(radius,radius),0,false,SweepDirection.Clockwise);path.EndFigure(true);
        }
        return clip;
    }
    private sealed class SourceFlow:Panel
    {
        private readonly WebMatchingDialog o;
        private readonly WebSystemTextBlock _strut=WebTypography.CreateCandidate("x",14,22.4);
        public SourceFlow(WebMatchingDialog owner){o=owner;UseLayoutRounding=false;Children.Add(o.SourceLabel);Children.Add(o.SourceNameText);}
        protected override Size MeasureOverride(Size available){o.SourceLabel.LineHeight=Q(19.2);o.SourceLabel.Measure(available);o.SourceNameText.LineHeight=Q(19.2);o.SourceNameText.Measure(available);_strut.LineHeight=Q(22.4);_strut.Measure(available);return new(available.Width,o.SourceLabel.DesiredSize.Height+4+Math.Max(_strut.DesiredSize.Height,o.SourceNameText.DesiredSize.Height));}
        private double Q(double v){var units=(TopLevel.GetTopLevel(this)?.RenderScaling??2)*64;return Math.Floor(v*units)/units;}
        protected override Size ArrangeOverride(Size final){o.SourceLabel.Arrange(new(0,0,final.Width,o.SourceLabel.DesiredSize.Height));var y=o.SourceLabel.DesiredSize.Height+4;var dy=_strut.SystemBaseline-o.SourceNameText.SystemBaseline;o.SourceNameText.Arrange(new(0,y+dy,final.Width,o.SourceNameText.DesiredSize.Height));return final;}
    }
    private sealed class MatchingBody:Panel
    {
        private readonly WebMatchingDialog _o;private Control? _error,_extra;
        public Control? Extra{get=>_extra;set{if(_extra!=null)Children.Remove(_extra);_extra=value;if(value!=null)Children.Add(value);InvalidateMeasure();}}
        public MatchingBody(WebMatchingDialog o){_o=o;UseLayoutRounding=false;Children.Add(o.SourceBox);Children.Add(o.TargetLabel);Children.Add(o.Search);Children.Add(o.OptionsViewport);Children.Add(o.QueryNote);Children.Add(o.PolicyText);}
        public Control? Error{get=>_error;set{if(_error is not null)Children.Remove(_error);_error=value;if(value is not null)Children.Add(value);InvalidateMeasure();}}
        protected override Size MeasureOverride(Size a)
        {
            _o.SourceBox.Measure(new(a.Width,double.PositiveInfinity));_o.TargetLabel.Measure(a);_o.Search.Measure(new(a.Width,double.PositiveInfinity));
            _o._options.Measure(new(Math.Max(0,a.Width-8),double.PositiveInfinity));var optionHeight=Math.Min(300,_o._options.DesiredSize.Height+8);_o.OptionsViewport.Measure(new(a.Width,optionHeight));
            _o.QueryNote.LineHeight=Q(19.2);_o.PolicyText.LineHeight=Q(19.2);_o.QueryNote.Measure(a);_o.PolicyText.Measure(a);_error?.Measure(a);_extra?.Measure(new(a.Width,double.PositiveInfinity));
            return new(a.Width,_o.SourceBox.DesiredSize.Height+20+_o.TargetLabel.DesiredSize.Height+_o.Search.DesiredSize.Height+optionHeight+10+Math.Max(22,_o.QueryNote.DesiredSize.Height)+16+_o.PolicyText.DesiredSize.Height+(_error?.DesiredSize.Height??0)+(_extra==null?0:20+_extra.DesiredSize.Height));
        }
        private double Q(double v){var units=(TopLevel.GetTopLevel(this)?.RenderScaling??2)*64;return Math.Floor(v*units)/units;}
        protected override Size ArrangeOverride(Size f)
        {
            double y=0;void Put(Control c,double height){c.Arrange(new(0,y,f.Width,height));y+=height;}
            Put(_o.SourceBox,_o.SourceBox.DesiredSize.Height);y+=20;Put(_o.TargetLabel,_o.TargetLabel.DesiredSize.Height);Put(_o.Search,_o.Search.DesiredSize.Height);Put(_o.OptionsViewport,_o.OptionsViewport.DesiredSize.Height);y+=10;Put(_o.QueryNote,Math.Max(22,_o.QueryNote.DesiredSize.Height));y+=16;Put(_o.PolicyText,_o.PolicyText.DesiredSize.Height);if(_error is not null)Put(_error,_error.DesiredSize.Height);if(_extra!=null){y+=20;Put(_extra,_extra.DesiredSize.Height);}return f;
        }
    }
    private sealed class DialogFlow:Panel
    {
        private readonly WebMatchingDialog _o;private readonly WebSurfaceBox _head=WebSurface.Create(Brushes.Transparent,Brush.Parse("#41434a"),new(0,0,0,1),default),_footer=WebSurface.Create(Brush.Parse("#232428"),Brush.Parse("#41434a"),new(0,1,0,0),default);
        public double BodyMaximum{get;set;}
        public DialogFlow(WebMatchingDialog o){_o=o;UseLayoutRounding=false;Children.Add(_head);Children.Add(o.TitleText);Children.Add(o.CloseButton);Children.Add(o.BodyViewport);Children.Add(_footer);Children.Add(o.CancelButton);Children.Add(o.ApplyButton);}
        protected override Size MeasureOverride(Size a)
        {
            _o.TitleText.Measure(new(Math.Max(0,a.Width-112),double.PositiveInfinity));_o.CloseButton.Measure(new(44,44));_o.CancelButton.Measure(new(Math.Max(0,a.Width-224),double.PositiveInfinity));_o.ApplyButton.Measure(new(164,double.PositiveInfinity));
            var h=Math.Max(44,_o.TitleText.DesiredSize.Height)+41;var foot=Math.Max(_o.CancelButton.DesiredSize.Height,_o.ApplyButton.DesiredSize.Height)+37;
            _o._body.Measure(new(Math.Max(0,a.Width-48),double.PositiveInfinity));var body=Math.Min(_o._body.DesiredSize.Height+48,BodyMaximum);_o.BodyViewport.Measure(new(a.Width,body));return new(a.Width,h+body+foot);
        }
        protected override Size ArrangeOverride(Size f)
        {
            var h=Math.Max(44,_o.TitleText.DesiredSize.Height)+41;var foot=Math.Max(_o.CancelButton.DesiredSize.Height,_o.ApplyButton.DesiredSize.Height)+37;var body=Math.Max(48,f.Height-h-foot);
            _head.Arrange(new(0,0,f.Width,h));_o.TitleText.Arrange(new(24,20+(h-41-_o.TitleText.DesiredSize.Height)/2,Math.Max(0,f.Width-112),_o.TitleText.DesiredSize.Height));_o.CloseButton.Arrange(new(f.Width-68,20+(h-85)/2,44,44));_o.BodyViewport.Arrange(new(0,h,f.Width,body));_footer.Arrange(new(0,h+body,f.Width,foot));var bh=foot-37;_o.ApplyButton.Arrange(new(f.Width-24-_o.ApplyButton.DesiredSize.Width,h+body+19,_o.ApplyButton.DesiredSize.Width,bh));_o.CancelButton.Arrange(new(f.Width-36-_o.ApplyButton.DesiredSize.Width-_o.CancelButton.DesiredSize.Width,h+body+19,_o.CancelButton.DesiredSize.Width,bh));return f;
        }
    }
}
