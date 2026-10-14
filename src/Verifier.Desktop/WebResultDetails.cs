using Avalonia;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.VisualTree;
using Avalonia.Threading;

namespace Verifier.Desktop;

/// <summary>The original centered detail modal, composed solely from verified child contracts.</summary>
public sealed class WebResultDetails : Panel
{
    private readonly WebSurfaceBox _surface=WebSurface.Create(Brush.Parse("#2b2d31"),Brush.Parse("#535963"),new(1),new(14));
    private readonly DialogFlow _flow;
    private Control? _opener,_backgroundContent;
    private IEffect? _previousEffect;
    private bool _previousEnabled,_closing,_reduced;
    private WebMotionPlayback? _playback;
    private NavigationMethod _lastNavigationMethod=NavigationMethod.Unspecified;
    public WebSurfaceBox DialogBox=>_surface;
    public WebScrollViewport OuterViewport{get;}=new();
    public WebScrollViewport BodyViewport{get;}=new(){Padding=new(24)};
    public WebResultDetailsBody Body{get;}=new();
    public WebSystemTextBlock TitleText{get;}=WebTypography.CreateCandidate("",18,27,Brush.Parse("#f2f3f5"),700,-.27);
    public WebActionButton CloseButton{get;}=WebIconButton.Create("닫기");
    public WebActionButton FooterCloseButton{get;private set;}=new();
    public Control HeaderRegion=>_flow.Header;
    public Control FooterRegion=>_flow.Footer;
    public bool IsClosing=>_closing;
    public bool ReducedMotion{get=>_reduced;set{_reduced=value;Body.ReducedMotion=value;CloseButton.ReducedMotion=FooterCloseButton.ReducedMotion=value;}}
    public event EventHandler? Closed;
    public WebResultDetails()
    {
        UseLayoutRounding=false;Focusable=true;IsTabStop=false;FocusAdorner=null;IsVisible=false;Background=new SolidColorBrush(Color.FromArgb(128,14,15,20));
        KeyboardNavigation.SetTabNavigation(this,KeyboardNavigationMode.Cycle);_surface.BoxShadow=new(new BoxShadow{OffsetY=18,Blur=48,Color=Color.FromArgb(36,0,0,0)});
        Body.PageRequested+=_=>{BodyViewport.Offset=0;InvalidateMeasure();};
        BodyViewport.Content=Body;_flow=new(this);OuterViewport.Content=_flow;_surface.Child=OuterViewport;Children.Add(_surface);
        AutomationProperties.SetAutomationId(this,"ResultDetails");AutomationProperties.SetAutomationId(CloseButton,"ResultDetails-close");AutomationProperties.SetAutomationId(FooterCloseButton,"ResultDetails-footer-close");AutomationProperties.SetAutomationId(BodyViewport,"ResultDetails-body");
        AddHandler(KeyDownEvent,(_,_)=>_lastNavigationMethod=NavigationMethod.Tab,RoutingStrategies.Tunnel,true);
        CloseButton.GotFocus+=(_,_)=>FocusWithinOuter(CloseButton);
        CloseButton.Click+=(_,_)=>_=CloseAsync();FooterCloseButton.Click+=(_,_)=>_=CloseAsync();
        AddHandler(KeyDownEvent,(_,e)=>{if(e.Key==Key.Escape&&!e.Handled){e.Handled=true;_=CloseAsync();}},RoutingStrategies.Bubble);
        AddHandler(PointerPressedEvent,(_,e)=>
        {
            if(!e.GetCurrentPoint(this).Properties.IsLeftButtonPressed||e.Source is not Visual v)return;
            _lastNavigationMethod=NavigationMethod.Pointer;
            var focusable=v.GetVisualAncestors().Prepend(v).TakeWhile(x=>x!=this).OfType<InputElement>().Any(x=>x.Focusable&&x.IsEffectivelyEnabled&&x is not WebScrollViewport);
            if(!focusable)Focus(NavigationMethod.Pointer);
        },RoutingStrategies.Bubble,true);
        DetachedFromVisualTree+=(_,_)=>{_playback?.Dispose();RestoreBackground();};
    }
    public void Open(string title,IEnumerable<string> checks,bool behavior,WebDetailScope? scope,bool english=false,Control? opener=null,Control? backgroundContent=null,NavigationMethod initialFocusMethod=NavigationMethod.Unspecified,int pageIndex=0)
    {
        _playback?.Dispose();RestoreBackground();_closing=false;_lastNavigationMethod=initialFocusMethod;_opener=opener;_backgroundContent=backgroundContent;
        if(backgroundContent is not null){_previousEffect=backgroundContent.Effect;_previousEnabled=backgroundContent.IsEnabled;backgroundContent.Effect=new BlurEffect{Radius=2};backgroundContent.SetCurrentValue(IsEnabledProperty,false);}
        TitleText.Text=title;AutomationProperties.SetName(this,title);WebIconButton.SetAccessibleName(CloseButton,english?"Close":"닫기");_flow.ReplaceFooter(new WebActionButton{Content=english?"Close":"닫기",ReducedMotion=ReducedMotion});Body.ReducedMotion=ReducedMotion;Body.SetPresentation(checks,behavior,scope,english,pageIndex);IsVisible=true;BodyViewport.Offset=OuterViewport.Offset=0;InvalidateMeasure();
        if(TopLevel.GetTopLevel(this) is not null){_playback=WebMotion.Play(_surface,WebMotionKind.Modal,ReducedMotion);CloseButton.Focus(initialFocusMethod);}
    }
    public async Task CloseAsync()
    {
        if(!IsVisible||_closing)return;_closing=true;_playback?.Dispose();_playback=WebMotion.Play(_surface,WebMotionKind.ModalClose,ReducedMotion);
        try{await _playback.Completion;}catch(TaskCanceledException){return;}
        IsVisible=false;RestoreBackground();_opener?.Focus(_lastNavigationMethod);Closed?.Invoke(this,EventArgs.Empty);
    }
    private void FocusWithinOuter(Control target)=>Dispatcher.UIThread.Post(()=>{if(!IsVisible||!target.IsFocused||target.TranslatePoint(default,OuterViewport) is not { } p)return;if(p.Y<0||p.Y+target.Bounds.Height>OuterViewport.ClientHeight)OuterViewport.Offset+=p.Y+target.Bounds.Height/2-OuterViewport.ClientHeight/2;});
    private void RestoreBackground(){if(_backgroundContent is not null){_backgroundContent.Effect=_previousEffect;_backgroundContent.SetCurrentValue(IsEnabledProperty,_previousEnabled);}_backgroundContent=null;_previousEffect=null;}
    protected override Size MeasureOverride(Size available)
    {
        var width=Math.Max(0,Math.Min(620,available.Width-32));if(_flow.BodyMaximum!=.65*available.Height){_flow.BodyMaximum=.65*available.Height;_flow.InvalidateMeasure();}
        _flow.Measure(new(Math.Max(0,width-2),double.PositiveInfinity));var height=Math.Min(_flow.DesiredSize.Height+2,Math.Max(0,available.Height-48));_surface.Measure(new(width,height));return available;
    }
    protected override Size ArrangeOverride(Size final)
    {
        var width=Math.Max(0,Math.Min(620,final.Width-32));var height=Math.Min(_surface.DesiredSize.Height,Math.Max(0,final.Height-48));var units=(TopLevel.GetTopLevel(this)?.RenderScaling??2)*64;
        var x=Math.Floor((final.Width-width)*units/2)/units;var y=Math.Floor((final.Height-height)*units/2)/units;_surface.Arrange(new(x,y,width,height));OuterViewport.Clip=new RectangleGeometry(new Rect(OuterViewport.Bounds.Size),13,13);return final;
    }
    private sealed class DialogFlow:Panel
    {
        private readonly WebResultDetails _o;
        public WebSurfaceBox Header{get;}=WebSurface.Create(Brushes.Transparent,Brush.Parse("#41434a"),new(0,0,0,1),default);
        public WebSurfaceBox Footer{get;}=WebSurface.Create(Brush.Parse("#232428"),Brush.Parse("#41434a"),new(0,1,0,0),default);
        public double BodyMaximum{get;set;}
        public void ReplaceFooter(WebActionButton button){Children.Remove(_o.FooterCloseButton);_o.FooterCloseButton=button;AutomationProperties.SetAutomationId(button,"ResultDetails-footer-close");button.GotFocus+=(_,_)=>_o.FocusWithinOuter(button);button.Click+=(_,_)=>_=_o.CloseAsync();Children.Add(button);InvalidateMeasure();}
        public DialogFlow(WebResultDetails o){_o=o;UseLayoutRounding=false;Children.Add(Header);Children.Add(o.TitleText);Children.Add(o.CloseButton);Children.Add(o.BodyViewport);Children.Add(Footer);Children.Add(o.FooterCloseButton);}
        protected override Size MeasureOverride(Size a)
        {
            _o.TitleText.Measure(new(Math.Max(0,a.Width-112),double.PositiveInfinity));_o.CloseButton.Measure(new(44,44));_o.FooterCloseButton.Measure(Size.Infinity);var h=Math.Max(44,_o.TitleText.DesiredSize.Height)+41;var foot=_o.FooterCloseButton.DesiredSize.Height+37;
            _o.Body.Measure(new(Math.Max(0,a.Width-48),double.PositiveInfinity));var body=Math.Min(_o.Body.DesiredSize.Height+48,BodyMaximum);_o.BodyViewport.Measure(new(a.Width,body));return new(a.Width,h+body+foot);
        }
        protected override Size ArrangeOverride(Size f)
        {
            var h=Math.Max(44,_o.TitleText.DesiredSize.Height)+41;var foot=_o.FooterCloseButton.DesiredSize.Height+37;var body=Math.Max(48,f.Height-h-foot);
            Header.Arrange(new(0,0,f.Width,h));_o.TitleText.Arrange(new(24,20+(h-41-_o.TitleText.DesiredSize.Height)/2,Math.Max(0,f.Width-112),_o.TitleText.DesiredSize.Height));_o.CloseButton.Arrange(new(f.Width-68,20+(h-85)/2,44,44));_o.BodyViewport.Arrange(new(0,h,f.Width,body));Footer.Arrange(new(0,h+body,f.Width,foot));_o.FooterCloseButton.Arrange(new(f.Width-24-_o.FooterCloseButton.DesiredSize.Width,h+body+19,_o.FooterCloseButton.DesiredSize.Width,foot-37));return f;
        }
    }
}
