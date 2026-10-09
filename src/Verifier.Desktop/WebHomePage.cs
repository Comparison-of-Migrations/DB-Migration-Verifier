using Avalonia;
using Avalonia.Automation;
using Avalonia.Automation.Peers;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Threading;
using Avalonia.Interactivity;
using Avalonia.VisualTree;
namespace Verifier.Desktop;

public sealed record WebHomeView(bool English);

/// <summary>Literal home() page; application actions and page entrance belong to its parent.</summary>
public sealed class WebHomePage:Panel
{
    public WebHomeView View{get;private set;}=new(false);
    public bool ReducedMotion{get;set;}
    public WebPageHeading Heading{get;}=new();
    public WebSystemTextBlock HelpText{get;}=WebTypography.CreateCandidate("",15,28.5,Brush.Parse("#b5bac1"));
    public WebSampleReport Report{get;}=new();
    public WebActionButton StartButton{get;}=new(){Variant="primary",Height=52,MinHeight=52,MinWidth=224,Padding=new(40,10),IconName="arrow",AbsoluteTrailingIcon=true,IconRightInset=18};
    public WebActionButton OpenButton{get;}=new(){Height=52,MinHeight=52,MinWidth=224,IconName="file",RestingBackgroundOverride=Colors.Transparent,BorderColorOverride=Color.Parse("#686e7b"),HoverBackgroundOverride=Color.Parse("#3c3f46"),ParentHoverIncludesDisabled=true};
    public WebActionButton SampleButton{get;}=new(){Variant="quiet",Height=52,MinHeight=52,Padding=new(12,10),LabelWeight=500};
    public WebWelcomeEntryHint Hint{get;}=new();
    public WebWelcomeSteps Steps{get;}=new();
    public WebWelcomePaper Paper{get;}=new();
    public Control Hero=>_hero;
    public Control HelpBox=>_hero.HelpHost;
    public Control Entry=>_entry;
    public Control Actions=>_entry.Actions;
    public Control MainActions=>_entry.Actions.Main;
    public event EventHandler<string>? ActionRequested;
    private readonly HeroPanel _hero;
    private readonly EntryPanel _entry;
    private double _viewport=1280;
    private long _revision;
    public WebHomePage()
    {
        UseLayoutRounding=false;ClipToBounds=false;Background=Brushes.Transparent;Focusable=true;KeyboardNavigation.SetIsTabStop(this,false);AutomationProperties.SetAutomationId(this,"home-body");
        AddHandler(InputElement.PointerPressedEvent,(_,e)=>{if(e.GetCurrentPoint(this).Properties.IsLeftButtonPressed && e.Source is Visual v && v is not WebActionButton && !v.GetVisualAncestors().OfType<WebActionButton>().Any())Focus(NavigationMethod.Pointer);},RoutingStrategies.Tunnel,true);
        Heading.TitleText.FontSize=36;Heading.TitleText.LetterSpacing=-1.26;Heading.TitleLineHeight=48.6;Heading.TitleMaximumWidth=700;
        _hero=new(HelpText,Report);_entry=new(StartButton,OpenButton,SampleButton,Hint);
        Children.Add(Heading);Children.Add(_hero);Children.Add(_entry);Children.Add(Steps);Children.Add(Paper);
        StartButton.Click+=(_,_)=>ActionRequested?.Invoke(this,"start");OpenButton.Click+=(_,_)=>ActionRequested?.Invoke(this,"open");SampleButton.Click+=(_,_)=>ActionRequested?.Invoke(this,"sample");Paper.ResearchRequested+=(_,_)=>ActionRequested?.Invoke(this,"research");
        AutomationProperties.SetAutomationId(StartButton,"start");AutomationProperties.SetAutomationId(OpenButton,"open");AutomationProperties.SetAutomationId(SampleButton,"sample");AutomationProperties.SetAutomationId(Paper.ResearchButton,"research");
        SetPresentation(View,_viewport);
    }
    public void SetPresentation(WebHomeView view,double viewportWidth,string? restoreIdentity=null,bool focusVisible=false)
    {
        View=view;_viewport=viewportWidth;var rev=++_revision;string T(string ko,string en)=>view.English?en:ko;
        Heading.SetPresentation(T("마이그레이션 결과를 비교하세요","Verify your migration results"),T("옮긴 뒤의 두 DB를 비교하고, 확인한 범위까지 분명하게.","Compare the two databases and see exactly what has been verified."));
        HelpText.Text=T("마이그레이션을 마친 개발자가 논문의 다축 검증 방법으로 두 DB의 데이터·구조·권한·객체를 비교하고, 차이와 확인하지 못한 영역을 찾도록 돕습니다.","Helps developers after migration compare data, schema, permissions and objects using the paper’s multi-axis approach, and identify differences and unverified areas.");
        Report.SetPresentation(view.English);Report.IsVisible=viewportWidth>1180;_hero.ReportVisible=Report.IsVisible;
        StartButton.Content=T("새 검사 시작","Start a new check");OpenButton.Content=T("검사 결과 파일로 보기","View a result file");SampleButton.Content=T("예시 결과 보기","See sample results");
        foreach(var b in new[]{StartButton,OpenButton,SampleButton}){b.ReducedMotion=ReducedMotion;b.RefreshParentStyle();}
        Hint.SetPresentation(view.English);Steps.SetPresentation(view.English);Paper.SetPresentation(view.English,viewportWidth,ReducedMotion);AutomationProperties.SetName(_entry,T("시작 방법","Getting started"));InvalidateMeasure();
        if(restoreIdentity is not null)Dispatcher.UIThread.Post(()=>{if(rev!=_revision||TopLevel.GetTopLevel(this) is null)return;var b=restoreIdentity switch{"start"=>StartButton,"open"=>OpenButton,"sample"=>SampleButton,"research"=>Paper.ResearchButton,_=>null};b?.Focus(focusVisible?NavigationMethod.Tab:NavigationMethod.Pointer);},DispatcherPriority.Loaded);
    }
    protected override Size MeasureOverride(Size available)
    {
        foreach(var c in Children)c.Measure(new(available.Width,double.PositiveInfinity));
        return new(available.Width,10+Children.Sum(x=>x.DesiredSize.Height));
    }
    protected override Size ArrangeOverride(Size final)
    {
        double y=10;foreach(var c in Children){c.Arrange(new(0,y,final.Width,c.DesiredSize.Height));y+=c.DesiredSize.Height;}return final;
    }
    private sealed class HeroPanel:Panel
    {
        public bool ReportVisible{get;set;}
        public Panel HelpHost{get;}
        private readonly WebSystemTextBlock _help;
        private readonly WebSampleReport _report;
        private double _left,_right;
        public HeroPanel(WebSystemTextBlock help,WebSampleReport report){UseLayoutRounding=false;_help=help;_report=report;HelpHost=new HelpPanel(help);Children.Add(HelpHost);Children.Add(report);}
        protected override Size MeasureOverride(Size available)
        {
            var units=(TopLevel.GetTopLevel(this)?.RenderScaling??2)*64;double Q(double n)=>Math.Floor(n*units)/units;
            _right=ReportVisible?Math.Max(270,Q((available.Width-48)*.35)):0;_left=ReportVisible?(_right==270?Math.Max(0,available.Width-318):Q((available.Width-48)*.65)):available.Width;
            _help.LineHeight=Q(28.5);HelpHost.Measure(new(_left,double.PositiveInfinity));if(ReportVisible)_report.Measure(new(_right,double.PositiveInfinity));
            return new(available.Width,Math.Max(HelpHost.DesiredSize.Height,ReportVisible?_report.DesiredSize.Height:0));
        }
        protected override Size ArrangeOverride(Size final){HelpHost.Arrange(new(0,0,_left,HelpHost.DesiredSize.Height));if(ReportVisible)_report.Arrange(new(_left+48,0,_right,_report.DesiredSize.Height));return final;}
        private sealed class HelpPanel:Panel
        {
            private readonly WebSystemTextBlock _text;
            public HelpPanel(WebSystemTextBlock text){UseLayoutRounding=false;_text=text;Children.Add(text);}
            protected override AutomationPeer OnCreateAutomationPeer()=>OperatingSystem.IsWindows()?new HelpPeer(this):base.OnCreateAutomationPeer();
            private sealed class HelpPeer(HelpPanel owner):ControlAutomationPeer(owner){protected override string GetNameCore()=>owner._text.Text??"";protected override AutomationControlType GetAutomationControlTypeCore()=>AutomationControlType.Text;protected override IReadOnlyList<AutomationPeer>? GetChildrenCore()=>Array.Empty<AutomationPeer>();}
            protected override Size MeasureOverride(Size available){_text.Measure(new(Math.Min(650,available.Width),double.PositiveInfinity));return new(available.Width,6+_text.DesiredSize.Height);}
            protected override Size ArrangeOverride(Size final){_text.Arrange(new(0,6,Math.Min(650,final.Width),_text.DesiredSize.Height));return final;}
        }
    }
    private sealed class EntryPanel:Panel
    {
        public ActionsPanel Actions{get;}
        private readonly WebWelcomeEntryHint _hint;
        private readonly WebSurfaceBox _rule=WebSurface.Create(Brush.Parse("#41434a"),Brushes.Transparent,default,default);
        public EntryPanel(WebActionButton start,WebActionButton open,WebActionButton sample,WebWelcomeEntryHint hint){UseLayoutRounding=false;Margin=new(0,28,0,0);Actions=new(start,open,sample);_hint=hint;Children.Add(Actions);Children.Add(hint);Children.Add(_rule);}
        protected override AutomationPeer OnCreateAutomationPeer()=>new EntryPeer(this);
        private sealed class EntryPeer(EntryPanel owner):ControlAutomationPeer(owner){protected override AutomationControlType GetAutomationControlTypeCore()=>AutomationControlType.Group;}
        protected override Size MeasureOverride(Size available){Actions.Measure(new(available.Width,double.PositiveInfinity));_hint.Measure(new(available.Width,double.PositiveInfinity));return new(available.Width,52+_hint.DesiredSize.Height+25);}
        protected override Size ArrangeOverride(Size final){Actions.Arrange(new(0,0,final.Width,52));_hint.Arrange(new(0,52,final.Width,_hint.DesiredSize.Height));_rule.Arrange(new(0,final.Height-1,final.Width,1));return final;}
    }
    private sealed class ActionsPanel:Panel
    {
        public Panel Main{get;}=new(){UseLayoutRounding=false};private readonly WebActionButton _start,_open,_sample;
        public ActionsPanel(WebActionButton start,WebActionButton open,WebActionButton sample){UseLayoutRounding=false;_start=start;_open=open;_sample=sample;Main.Children.Add(start);Main.Children.Add(open);Children.Add(Main);Children.Add(sample);}
        protected override Size MeasureOverride(Size available){_start.Measure(new(double.PositiveInfinity,52));_open.Measure(new(double.PositiveInfinity,52));_sample.Measure(new(double.PositiveInfinity,52));Main.Measure(new(_start.DesiredSize.Width+12+_open.DesiredSize.Width,52));return new(available.Width,52);}
        protected override Size ArrangeOverride(Size final){var w=_start.DesiredSize.Width+12+_open.DesiredSize.Width;Main.Arrange(new(0,0,w,52));_start.Arrange(new(0,0,_start.DesiredSize.Width,52));_open.Arrange(new(_start.DesiredSize.Width+12,0,_open.DesiredSize.Width,52));_sample.Arrange(new(w+20,0,_sample.DesiredSize.Width,52));return final;}
    }
}
