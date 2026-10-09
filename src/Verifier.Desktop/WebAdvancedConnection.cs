using Avalonia;
using Avalonia.Automation;
using Avalonia.Automation.Peers;
using Avalonia.Automation.Provider;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Interactivity;
using Avalonia.Threading;
namespace Verifier.Desktop;

public sealed record WebAdvancedSelection(bool Expanded,bool FocusVisible);

/// <summary>The original advanced-connection section; its parent handles full-page replacement.</summary>
public sealed class WebAdvancedConnection:Panel
{
    public AdvancedButton ToggleButton{get;private set;}=new();
    public WebSurfaceBox? Explanation{get;private set;}
    public IReadOnlyList<WebSystemTextBlock> Titles=>_columns?.Children.Cast<TextColumn>().Select(c=>c.Title).ToArray()??[];
    public IReadOnlyList<WebSystemTextBlock> Descriptions=>_columns?.Children.Cast<TextColumn>().Select(c=>c.Description).ToArray()??[];
    public bool English{get;private set;}
    public bool Expanded{get;private set;}
    public bool ReducedMotion{get;set;}
    public event EventHandler<WebAdvancedSelection>? ExpansionRequested;
    private Columns? _columns;
    private bool _attached;
    private long _revision;
    private WebMotionPlayback? _entry;
    public WebAdvancedConnection(){UseLayoutRounding=false;ClipToBounds=false;SetPresentation(false,false,1280);}
    public void SetPresentation(bool english,bool expanded,double viewportWidth,bool restoreFocus=false,bool focusVisible=false)
    {
        _entry?.Dispose();_entry=null;English=english;Expanded=expanded;var revision=++_revision;Children.Clear();Explanation=null;_columns=null;
        ToggleButton=new(){Content=english?"Advanced connection settings":"고급 연결 설정",Variant="quiet",IconName="chevron",Expanded=expanded,ReducedMotion=ReducedMotion};AutomationProperties.SetAutomationId(ToggleButton,"advanced");
        ToggleButton.Click+=(_,_)=>ExpansionRequested?.Invoke(this,new(!Expanded,ToggleButton.FocusVisible));Children.Add(ToggleButton);
        if(expanded)
        {
            _columns=new(){Gap=viewportWidth<=1100?16:20};
            _columns.Children.Add(new TextColumn(english?"TLS · Certificate verification":"TLS · 인증서 검증",english?"Certificates and server names are verified. Failures will not trigger a silent security downgrade.":"기본은 인증서와 서버 이름 검증입니다. 실패 시 보안을 자동으로 낮추지 않습니다."));
            _columns.Children.Add(new TextColumn(english?"Authentication · Timeouts":"인증 · 제한 시간",english?"The actual app will expose supported authentication and connection/command limits. This preview shows only the form structure.":"실제 앱에서 지원되는 인증 방식과 연결·명령 제한 시간을 안내합니다. 이 시안은 DB 계정 입력 구조만 보여줍니다."));
            Explanation=WebSurface.Create(Brush.Parse("#2b2d31"),Brush.Parse("#41434a"),new(1),new(10),new BoxShadows(new BoxShadow{OffsetY=2,Blur=4,Color=Color.FromArgb(16,0,0,0)}));Explanation.Padding=new(26);Explanation.Child=_columns;AutomationProperties.SetAutomationId(Explanation,"advanced-settings");Children.Add(Explanation);if(_attached)PlayEntry();
        }
        InvalidateMeasure();if(restoreFocus)Dispatcher.UIThread.Post(()=>{if(revision==_revision&&_attached)ToggleButton.Focus(focusVisible?NavigationMethod.Tab:NavigationMethod.Pointer);},DispatcherPriority.Loaded);
    }
    public void SetViewport(double viewportWidth)
    {
        _columns?.SetGap(viewportWidth<=1100?16:20);
    }
    private void PlayEntry(){_entry?.Dispose();if(Explanation is{}panel)_entry=WebMotion.Play(panel,WebMotionKind.Page,ReducedMotion);}
    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e){base.OnAttachedToVisualTree(e);_attached=true;PlayEntry();}
    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e){_attached=false;_entry?.Dispose();_entry=null;base.OnDetachedFromVisualTree(e);}
    protected override Size MeasureOverride(Size available){ToggleButton.Measure(Size.Infinity);Explanation?.Measure(new(available.Width,double.PositiveInfinity));return new(available.Width,ToggleButton.DesiredSize.Height+(Explanation?.DesiredSize.Height??0));}
    protected override Size ArrangeOverride(Size final){ToggleButton.Arrange(new(0,0,ToggleButton.DesiredSize.Width,ToggleButton.DesiredSize.Height));Explanation?.Arrange(new(0,ToggleButton.DesiredSize.Height,final.Width,Explanation.DesiredSize.Height));return final;}
    public sealed class AdvancedButton:WebActionButton
    {
        public bool Expanded{get;set;}
        public bool FocusVisible{get;private set;}
        protected override void OnGotFocus(FocusChangedEventArgs e){base.OnGotFocus(e);FocusVisible=e.NavigationMethod!=NavigationMethod.Pointer;}
        protected override void OnLostFocus(FocusChangedEventArgs e){base.OnLostFocus(e);FocusVisible=false;}
        protected override void OnKeyDown(KeyEventArgs e){if(e.Key is Key.Space or Key.Enter)FocusVisible=true;base.OnKeyDown(e);}
        protected override AutomationPeer OnCreateAutomationPeer()=>new AdvancedPeer(this);
        private sealed class AdvancedPeer(AdvancedButton button):ButtonAutomationPeer(button),IExpandCollapseProvider
        {
            public ExpandCollapseState ExpandCollapseState=>button.Expanded?ExpandCollapseState.Expanded:ExpandCollapseState.Collapsed;
            public bool ShowsMenu=>false;
            public void Expand(){if(!button.Expanded)button.OnClick();}
            public void Collapse(){if(button.Expanded)button.OnClick();}
        }
    }
    private sealed class TextColumn:Panel
    {
        public WebSystemTextBlock Title{get;}
        public WebSystemTextBlock Description{get;}
        public TextColumn(string title,string description){UseLayoutRounding=false;Title=WebTypography.CreateCandidate(title,15,24,Brush.Parse("#f2f3f5"),700);Description=WebTypography.CreateCandidate(description,14,22.4,Brush.Parse("#b5bac1"));Children.Add(Title);Children.Add(Description);}
        protected override Size MeasureOverride(Size available){var units=(TopLevel.GetTopLevel(this)?.RenderScaling??2)*64;Description.LineHeight=Math.Floor(22.4*units)/units;Title.Measure(new(available.Width,double.PositiveInfinity));Description.Measure(new(available.Width,double.PositiveInfinity));return new(available.Width,Title.DesiredSize.Height+Description.DesiredSize.Height);}
        protected override Size ArrangeOverride(Size final){Title.Arrange(new(0,0,final.Width,Title.DesiredSize.Height));Description.Arrange(new(0,Title.DesiredSize.Height,final.Width,Description.DesiredSize.Height));return final;}
    }
    private sealed class Columns:Panel
    {
        public double Gap{get;set;}
        public Columns(){UseLayoutRounding=false;}
        public void SetGap(double gap){if(Gap==gap)return;Gap=gap;InvalidateMeasure();}
        private double Units=>(TopLevel.GetTopLevel(this)?.RenderScaling??2)*64;
        private double LeftWidth(double width)=>Math.Floor((width-Gap)/2*Units)/Units;
        protected override Size MeasureOverride(Size available){var left=LeftWidth(available.Width);Children[0].Measure(new(left,double.PositiveInfinity));Children[1].Measure(new(available.Width-left-Gap,double.PositiveInfinity));return new(available.Width,Children.Max(c=>c.DesiredSize.Height));}
        protected override Size ArrangeOverride(Size final){var left=LeftWidth(final.Width);Children[0].Arrange(new(0,0,left,final.Height));Children[1].Arrange(new(left+Gap,0,final.Width-left-Gap,final.Height));return final;}
    }
}
