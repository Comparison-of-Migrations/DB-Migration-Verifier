using Avalonia;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Media;

namespace Verifier.Desktop;

/// <summary>Original topAndRail aside. The application owns visits and page navigation.</summary>
public sealed class WebSidebar : Panel
{
    public static readonly string[] StageKeys={"connect","capabilities","select","review","running","results","save"};
    private static readonly string[] Korean={"DB 연결","사전 확인","검사 선택","실행 전 확인","검사 진행","결과 확인","결과 저장"};
    private static readonly string[] English={"Connect DBs","Preflight","Choose checks","Review","Run checks","Results","Save results"};
    private readonly WebSurfaceBox _surface=WebSurface.Create(Brush.Parse("#232428"),Brushes.Transparent,default,default);
    private SidebarFlow _flow=null!;
    private string _screen="home";
    private HashSet<string> _visited=new();
    public WebScrollViewport Viewport{get;private set;}=null!;
    public Control TitleBox=>_flow.TitleBox;
    public WebSystemTextBlock TitleText=>_flow.Title;
    public Control NavigationBox=>_flow.Nav;
    public Control NoteBox=>_flow.NoteBox;
    public WebSystemTextBlock NoteHeading=>_flow.Note.Heading;
    public WebSystemTextBlock NoteText=>_flow.Note.Text;
    public IReadOnlyList<WebSidebarItem> Items=>_flow.Nav.Items;
    public event EventHandler<string>? NavigationRequested;
    public WebSidebar(){Width=220;UseLayoutRounding=false;ClipToBounds=false;Children.Add(_surface);SetState("ko","home",Array.Empty<string>());}
    public void SetState(string language,string screen,IEnumerable<string> visited,bool reducedMotion=false)
    {
        _screen=screen;_visited=new(visited);var korean=language=="ko";
        _flow=new SidebarFlow(korean);
        Viewport=new WebScrollViewport{Padding=new(14,30),Content=_flow};
        _surface.Child=Viewport;
        AutomationProperties.SetName(_flow.Nav,korean?"검사 진행 단계":"Verification steps");
        var index=Array.IndexOf(StageKeys,screen=="trial"?"select":screen);
        for(var i=0;i<StageKeys.Length;i++)
        {
            var key=StageKeys[i];var b=new WebSidebarItem{Index=i+1,Content=(korean?Korean:English)[i],
                State=index==i?WebStepBadgeState.Active:_visited.Contains(key)&&i<index?WebStepBadgeState.Done:WebStepBadgeState.Normal,
                IsEnabled=_visited.Contains(key)&&screen!="running",ReducedMotion=reducedMotion};
            AutomationProperties.SetAutomationId(b,"Sidebar-"+key);
            b.Click+=(_,_)=>{if(_screen!="running"&&_visited.Contains(key)&&b.IsEffectivelyEnabled)NavigationRequested?.Invoke(this,key);};
            _flow.Nav.Add(b);
        }
        InvalidateMeasure();
    }
    protected override Size MeasureOverride(Size availableSize)
    {
        _flow.MinimumHeight=double.IsFinite(availableSize.Height)?Math.Max(0,availableSize.Height-60):0;
        _surface.Measure(availableSize);return _surface.DesiredSize;
    }
    protected override Size ArrangeOverride(Size finalSize){_surface.Arrange(new Rect(finalSize));return finalSize;}
    private sealed class SidebarFlow:Panel
    {
        public WebSystemTextBlock Title{get;}
        public WebSurfaceBox TitleBox{get;}=WebSurface.Create(Brushes.Transparent,Brushes.Transparent,default,default);
        public SidebarNavigation Nav{get;}=new();
        public SidebarNote Note{get;}
        public WebSurfaceBox NoteBox{get;}=WebSurface.Create(Brushes.Transparent,Brush.Parse("#41434a"),new(0,1,0,0),default);
        public double MinimumHeight;
        public SidebarFlow(bool korean)
        {
            UseLayoutRounding=false;ClipToBounds=false;
            Title=WebTypography.CreateCandidate(korean?"마이그레이션 결과 비교":"Migration result comparison",11,18.7,Brush.Parse("#b5bac1"));
            TitleBox.Padding=new(14,0);TitleBox.Child=Title;
            Note=new SidebarNote(korean);NoteBox.Padding=new(14,20);NoteBox.Child=Note;
            Children.Add(TitleBox);Children.Add(Nav);Children.Add(NoteBox);
        }
        protected override Size MeasureOverride(Size availableSize)
        {
            var units=(TopLevel.GetTopLevel(this)?.RenderScaling??2)*64;
            Title.LineHeight=Math.Floor(11*1.7*units)/units;
            foreach(var c in Children)c.Measure(new(availableSize.Width,double.PositiveInfinity));
            return new(availableSize.Width,Math.Max(MinimumHeight,TitleBox.DesiredSize.Height+32+Nav.DesiredSize.Height+32+NoteBox.DesiredSize.Height));
        }
        protected override Size ArrangeOverride(Size finalSize)
        {
            TitleBox.Arrange(new(0,0,finalSize.Width,TitleBox.DesiredSize.Height));
            Nav.Arrange(new(0,TitleBox.DesiredSize.Height+32,finalSize.Width,Nav.DesiredSize.Height));
            NoteBox.Arrange(new(0,finalSize.Height-NoteBox.DesiredSize.Height,finalSize.Width,NoteBox.DesiredSize.Height));return finalSize;
        }
    }
    private sealed class SidebarNavigation:Panel
    {
        public List<WebSidebarItem> Items{get;}=new();
        public SidebarNavigation(){UseLayoutRounding=false;ClipToBounds=false;}
        public void Add(WebSidebarItem item){Items.Add(item);Children.Add(item);}
        protected override Size MeasureOverride(Size availableSize)
        {
            foreach(var b in Items)b.Measure(new(availableSize.Width,double.PositiveInfinity));
            return new(availableSize.Width,Items.Sum(b=>b.DesiredSize.Height)+Math.Max(0,Items.Count-1)*8);
        }
        protected override Size ArrangeOverride(Size finalSize)
        {var y=0d;foreach(var b in Items){b.Arrange(new(0,y,finalSize.Width,b.DesiredSize.Height));y+=b.DesiredSize.Height+8;}return finalSize;}
    }
    private sealed class SidebarNote:Panel
    {
        public WebSystemTextBlock Heading{get;}
        public WebSystemTextBlock Text{get;}
        public SidebarNote(bool korean)
        {
            UseLayoutRounding=false;ClipToBounds=false;
            Heading=WebTypography.CreateCandidate(korean?"비교는 읽기 전용":"Read-only comparison",13,20.8,Brush.Parse("#f2f3f5"),700);
            Text=WebTypography.CreateCandidate(korean?"동작 시험의 쓰기는 별도 시험 DB에서만 합니다.":"Behavior-test writes belong only in a separate test DB.",12,19.2,Brush.Parse("#b5bac1"));
            Children.Add(Heading);Children.Add(Text);
        }
        protected override Size MeasureOverride(Size availableSize)
        {
            var units=(TopLevel.GetTopLevel(this)?.RenderScaling??2)*64;
            Heading.LineHeight=Math.Floor(13*1.6*units)/units;Text.LineHeight=Math.Floor(12*1.6*units)/units;
            Heading.Measure(new(availableSize.Width,double.PositiveInfinity));Text.Measure(new(availableSize.Width,double.PositiveInfinity));
            return new(availableSize.Width,Heading.DesiredSize.Height+8+Text.DesiredSize.Height);
        }
        protected override Size ArrangeOverride(Size finalSize)
        {Heading.Arrange(new(0,0,finalSize.Width,Heading.DesiredSize.Height));Text.Arrange(new(0,Heading.DesiredSize.Height+8,finalSize.Width,Text.DesiredSize.Height));return finalSize;}
    }
}
