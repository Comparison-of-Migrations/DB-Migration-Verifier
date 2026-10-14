using System.Globalization;
using Avalonia;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Threading;
namespace Verifier.Desktop;

public sealed record WebConnectionEndpoint(string Host,string Port,string Database,string Reader);
public sealed record WebConnectionView(bool English,string Dbms,WebConnectionEndpoint Source,WebConnectionEndpoint Target,bool Connected,bool Advanced,bool Busy,string Error);
public sealed record WebConnectionChange(string Reason,string? Field,WebConnectionView View);

/// <summary>Literal connect() preview assembly. No database connection or credential persistence occurs here.</summary>
public sealed partial class WebConnectionPage:Panel
{
    public WebPageHeading Heading{get;private set;}=new();
    public WebDbmsPicker Picker{get;private set;}=new();
    public WebConnectionPanel Source{get;private set;}=new();
    public WebConnectionPanel Target{get;private set;}=new();
    public WebAdvancedConnection Advanced{get;private set;}=new();
    public WebNotice? ErrorNotice{get;private set;}
    public WebNotice? ConnectedNotice{get;private set;}
    public Control Panels=>_pair;
    public bool ReducedMotion{get;set;}
    public WebConnectionView View{get;private set;}=new(false,"SQL Server",new("source.example.invalid","1433","migration_sample","demo_reader"),new("target.example.invalid","1433","migration_sample","demo_reader"),false,false,false,"");
    public event EventHandler<WebConnectionChange>? StateChanged;
    private PanelPair _pair=new();
    private double _viewport=1280;
    private long _revision;
    private bool _attached;
    private WebMotionPlayback? _entry;
    private readonly Dictionary<string,InputElement> _focus=[];
    public WebConnectionPage(){UseLayoutRounding=false;ClipToBounds=false;SetPresentation(View,_viewport);}
    public void SetPresentation(WebConnectionView view,double viewportWidth,string? restoreIdentity=null,bool focusVisible=false)
    {
        _entry?.Dispose();_entry=null;View=view;_viewport=viewportWidth;var revision=++_revision;Children.Clear();_focus.Clear();ErrorNotice=ConnectedNotice=null;
        string T(string ko,string en)=>view.English?en:ko;
        Heading=new();Heading.SetPresentation(T("비교할 두 DB를 연결하세요","Connect the databases to compare"),T("같은 DBMS의 원본과 대상을 지정합니다. 접속 정보는 결과 파일에 넣지 않습니다.","Choose a source and target of the same DBMS. Connection details stay out of result files."));Children.Add(Heading);
        Picker=new(){Margin=new(0,0,0,24),ReducedMotion=ReducedMotion};Picker.SetPresentation(view.English,view.Dbms,viewportWidth);Children.Add(Picker);
        for(var i=0;i<Picker.Options.Count;i++)_focus["dbms-"+i]=Picker.Options[i].Radio;
        Picker.EngineChanged+=(_,engine)=>{var index=Picker.Options.Select((x,i)=>(x,i)).First(x=>x.x.EngineName==engine);var port=engine=="SQL Server"?"1433":engine=="PostgreSQL"?"5432":"3306";SetPresentation(View with{Dbms=engine,Source=View.Source with{Port=port},Target=View.Target with{Port=port},Connected=false},_viewport,"dbms-"+index.i,index.x.Radio.FocusVisible);Changed("dbms",null);};
        Picker.IsEnabled=!LiveMode||!view.Busy;
        Source=CreatePanel("source",view.Source);Target=CreatePanel("target",view.Target);_pair=new(){Gap=viewportWidth<=1100?16:20};_pair.Children.Add(Source);_pair.Children.Add(Target);Children.Add(_pair);
        Advanced=new(){Margin=new(0,30,0,0),ReducedMotion=ReducedMotion};Advanced.SetPresentation(view.English,view.Advanced,viewportWidth);_focus["advanced"]=Advanced.ToggleButton;
        Advanced.ExpansionRequested+=(_,change)=>{SetPresentation(View with{Advanced=change.Expanded},_viewport,"advanced",change.FocusVisible);Changed("advanced",null);};Children.Add(Advanced);
        if(LiveMode)ConfigureLiveInputs();
        if(view.Error.Length>0){ErrorNotice=new(){Title=T("연결 정보를 확인하세요","Check connection details"),Text=view.Error,Kind="error",ReducedMotion=ReducedMotion};Children.Add(ErrorNotice);}
        if(view.Connected){ConnectedNotice=new(){Title=LiveMode?T("양쪽 DB의 실제 연결을 확인했습니다","Both database connections are verified"):T("양쪽 예시 연결을 확인했습니다","Both sample connections are checked"),Text=LiveMode?T("조회 범위와 읽지 못한 항목을 다음 화면에서 확인합니다.","Review visibility and unreadable scopes on the next page."):T("이어서 어떤 범위를 읽을 수 있는지 확인합니다. 실제 서버에 접속한 결과는 아닙니다.","Next, check visibility. This is not evidence of a real server connection."),Kind="ok",ReducedMotion=ReducedMotion};Children.Add(ConnectedNotice);}
        InvalidateMeasure();if(_attached)PlayEntry();
        if(restoreIdentity is not null)Dispatcher.UIThread.Post(()=>{if(revision==_revision&&_attached&&_focus.TryGetValue(restoreIdentity,out var control)){if(control is WebRadio radio&&!focusVisible)radio.FocusFromPointer();else control.Focus(focusVisible?NavigationMethod.Tab:NavigationMethod.Pointer);}},DispatcherPriority.Loaded);
    }
    public void SetViewport(double viewportWidth)
    {
        if(_viewport==viewportWidth)return;
        _viewport=viewportWidth;_pair.SetGap(viewportWidth<=1100?16:20);
        foreach(var option in Picker.Options)option.Compact=viewportWidth<=1180;
        Advanced.SetViewport(viewportWidth);InvalidateMeasure();
    }
    private WebConnectionPanel CreatePanel(string side,WebConnectionEndpoint data)
    {
        var panel=new WebConnectionPanel();panel.SetPresentation(View.English?"en":"ko",side,View.Connected);
        panel.HostInput.Text=data.Host;panel.PortInput.Value=data.Port;panel.DatabaseInput.Text=data.Database;panel.UserInput.Text=data.Reader;
        foreach(var (key,input) in new (string,InputElement)[]{("host",panel.HostInput),("port",panel.PortInput),("db",panel.DatabaseInput),("user",panel.UserInput),("pass",panel.PasswordInput)}){AutomationProperties.SetAutomationId(input,side+"-"+key);_focus[side+"-"+key]=key=="port"?panel.PortInput.Editor:input;}
        AutomationProperties.SetAutomationId(panel.PortInput.Editor,side+"-port-editor");
        panel.InputChanged+=(_,key)=>
        {
            var endpoint=side=="source"?View.Source:View.Target;var next=key switch{"host"=>endpoint with{Host=panel.HostInput.Text??""},"port"=>endpoint with{Port=panel.PortInput.Value},"db"=>endpoint with{Database=panel.DatabaseInput.Text??""},_=>endpoint with{Reader=panel.UserInput.Text??""}};
            View=side=="source"?View with{Source=next,Connected=false}:View with{Target=next,Connected=false};Source.SetPresentation(View.English?"en":"ko","source",false);Target.SetPresentation(View.English?"en":"ko","target",false);if(LiveMode)RefreshLiveHints();
            if(LiveMode&&SaveConnectionButton!=null)SaveConnectionButton.IsEnabled=false;
            if(ConnectedNotice is{}notice){Children.Remove(notice);ConnectedNotice=null;}InvalidateMeasure();Changed("input",side+"-"+key);
        };return panel;
    }
    private void Changed(string reason,string? field)=>StateChanged?.Invoke(this,new(reason,field,View));
    public static string Validate(WebConnectionView view)
    {
        string T(string ko,string en)=>view.English?en:ko;
        static bool Empty(string value)=>value.Trim('\u0009','\u000A','\u000B','\u000C','\u000D','\u0020','\u00A0','\u1680','\u2000','\u2001','\u2002','\u2003','\u2004','\u2005','\u2006','\u2007','\u2008','\u2009','\u200A','\u2028','\u2029','\u202F','\u205F','\u3000','\uFEFF').Length==0;
        foreach(var d in new[]{view.Source,view.Target})
        {
            if(Empty(d.Host)||Empty(d.Database)||Empty(d.Reader))return T("양쪽 서버·DB·조회 계정을 모두 입력하세요.","Fill in the server, database and reader account for both connections.");
            if(d.Port.Length==0||d.Port.Any(c=>c<'0'||c>'9')||!double.TryParse(d.Port,NumberStyles.None,CultureInfo.InvariantCulture,out var port)||port<1||port>65535)return T("포트는 1–65535 범위의 정수여야 합니다.","Ports must be integers from 1 to 65535.");
            if(d.Host.StartsWith("bad",StringComparison.Ordinal))return T("예시 서버 주소가 올바르지 않습니다. 주소를 수정해 다시 확인하세요.","The sample server address is invalid. Edit it and try again.");
        }
        if(view.Source.Host==view.Target.Host&&view.Source.Port==view.Target.Port&&view.Source.Database==view.Target.Database)return T("같은 DB가 양쪽에 지정됐습니다. 원본과 대상을 확인하세요.","Both sides point to the same sample database. Check the source and target.");return "";
    }
    public async Task CheckSampleConnectionsAsync()
    {
        if(View.Busy)return;var error=Validate(View);if(error.Length>0){SetPresentation(View with{Error=error},_viewport);Changed("validation",null);return;}
        SetPresentation(View with{Busy=true,Error=""},_viewport);Changed("busy",null);await Task.Delay(550);SetPresentation(View with{Busy=false,Connected=true},_viewport);Changed("connected",null);
    }
    public string ResolveFooterAction(string action)=>action is "connect" or "preflight"?(View.Connected?"preflight":"connect"):action;
    public void SetFooter(WebActionFooter footer,bool preserveExistingAction=false)
    {
        if(preserveExistingAction&&footer.ActionButtons.Count==1){var button=footer.ActionButtons[0];if(AutomationProperties.GetAutomationId(button)!="Footer-preflight")return;button.Content=View.English?"Check both connections":"두 연결 확인";button.IconName="db";AutomationProperties.SetAutomationId(button,"Footer-connect");return;}
        string T(string ko,string en)=>View.English?en:ko;
        WebFooterAction back=LiveMode&&View.Busy?new("cancel",T("연결 확인 취소","Cancel connection check"),"quiet","stop"):new("back",T("뒤로","Back"),"quiet","back");
        footer.SetContent(T("연결 정보를 바꾸면 다시 확인합니다.","Changes require another connection check."),back,[View.Connected?new("preflight",T("조회 범위 확인","Check visibility"),"primary","arrow",!View.Busy):new WebFooterAction("connect",View.Busy?T("확인 중…","Checking…"):T("두 연결 확인","Check both connections"),"primary",View.Busy?"":"db",!View.Busy,View.Busy)],ReducedMotion);
    }
    private void PlayEntry(){_entry?.Dispose();_entry=WebMotion.Play(this,WebMotionKind.Page,ReducedMotion);}
    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e){base.OnAttachedToVisualTree(e);_attached=true;PlayEntry();}
    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e){_attached=false;_entry?.Dispose();_entry=null;base.OnDetachedFromVisualTree(e);}
    protected override Size MeasureOverride(Size available){double y=0;foreach(var child in Children){child.Measure(new(available.Width,double.PositiveInfinity));y+=child.DesiredSize.Height;}return new(available.Width,y);}
    protected override Size ArrangeOverride(Size final){double y=0;foreach(var child in Children){child.Arrange(new(0,y,final.Width,child.DesiredSize.Height));y+=child.DesiredSize.Height;}return final;}
    private sealed class PanelPair:Panel
    {
        public double Gap{get;set;}
        public PanelPair(){UseLayoutRounding=false;ClipToBounds=false;}
        public void SetGap(double gap){if(Gap==gap)return;Gap=gap;InvalidateMeasure();}
        private double Units=>(TopLevel.GetTopLevel(this)?.RenderScaling??2)*64;
        private double LeftWidth(double width)=>Math.Floor((width-Gap)/2*Units)/Units;
        protected override Size MeasureOverride(Size available){var left=LeftWidth(available.Width);Children[0].Measure(new(left,double.PositiveInfinity));Children[1].Measure(new(available.Width-left-Gap,double.PositiveInfinity));return new(available.Width,Children.Max(c=>c.DesiredSize.Height));}
        protected override Size ArrangeOverride(Size final){var left=LeftWidth(final.Width);Children[0].Arrange(new(0,0,left,final.Height));Children[1].Arrange(new(left+Gap,0,final.Width-left-Gap,final.Height));return final;}
    }
}
