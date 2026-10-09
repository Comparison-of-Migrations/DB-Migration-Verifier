using Avalonia;
using Avalonia.Automation;
using Avalonia.Automation.Peers;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.VisualTree;

namespace Verifier.Desktop;

public sealed record WebResultGroup(string Key,long Total,long Equal,long Different,string Status);

/// <summary>Original figure.result-chart, composed from locked result rows. The parent opens details.</summary>
public sealed class WebResultChart:Decorator
{
    private readonly WebSurfaceBox _surface=WebSurface.Create(Brush.Parse("#2b2d31"),Brush.Parse("#41434a"),new(1),new(10));
    private readonly StackPanel _sections=new(){Spacing=0,UseLayoutRounding=false};
    private readonly HeaderPanel _header=new();
    private readonly ColumnsPanel _columns=new();
    private readonly WebSurfaceBox _columnBox=WebSurface.Create(Brush.Parse("#232428"),Brush.Parse("#41434a"),new(0,1,0,1),default);
    private readonly StackPanel _rows=new(){Spacing=0,UseLayoutRounding=false};
    private readonly WebSurfaceBox _captionBox=WebSurface.Create(Brush.Parse("#292b30"),Brush.Parse("#41434a"),new(0,1,0,0),default);
    public WebSystemTextBlock Caption{get;}=WebTypography.CreateCandidate("",10,18);
    public WebSystemTextBlock Heading=>_header.Title;
    public WebSystemTextBlock Help=>_header.Help;
    public WebSystemTextBlock[] UnitLines=>_header.UnitLines;
    public Control Header=>_header;
    public Control Columns=>_columnBox;
    public Control CaptionBox=>_captionBox;
    public Control RowsPanel=>_rows;
    public IReadOnlyList<WebResultSummaryRow> Rows=>_rows.Children.OfType<WebResultSummaryRow>().ToArray();
    public IReadOnlyList<Control> ColumnLabels=>_columns.Children.Cast<Control>().ToArray();
    public double Maximum{get;private set;}=5;
    public bool Compact{get;private set;}
    public bool English{get;private set;}
    public event Action<string>? DetailRequested;
    public WebResultChart()
    {
        UseLayoutRounding=false;HorizontalAlignment=HorizontalAlignment.Stretch;Focusable=false;Margin=new(0,0,0,24);
        AutomationProperties.SetHeadingLevel(Heading,2);AutomationProperties.SetAccessibilityView(_columnBox,AccessibilityView.Raw);
        _captionBox.Padding=new(20,12);_captionBox.Child=Caption;
        _columnBox.Padding=new(20,12);_columnBox.Child=_columns;
        _sections.Children.Add(_header);_sections.Children.Add(_columnBox);_sections.Children.Add(_rows);_sections.Children.Add(_captionBox);
        _surface.Child=_sections;Child=_surface;IsVisible=false;
    }
    public void SetGroups(IEnumerable<WebResultGroup> groups,bool english=false,bool compact=false)
    {
        var comparison=groups.Where(g=>g.Key!="trial").ToArray();
        if(comparison.Any(g=>g.Total<0||g.Equal<0||g.Different<0||g.Equal>g.Total||g.Different>g.Total-g.Equal||g.Key is not("data" or "schema" or "access" or "objects")))
            throw new ArgumentException("Original comparison groups require known categories and consistent check counts.");
        English=english;Compact=compact;IsVisible=comparison.Length!=0;
        Maximum=Math.Max(5,Math.Ceiling(comparison.Select(g=>Math.Max(g.Different,Math.Max(g.Equal,g.Total-g.Equal-g.Different))).DefaultIfEmpty(0).Max()/5d)*5);
        Heading.Text=english?"Comparison results by category":"비교 결과, 분류별로";
        Help.Text=english?"Select a category for detailed checks and evidence.":"분류를 눌러 세부 검사와 판정 근거를 확인하세요.";
        UnitLines[0].Text=english?"Check counts":"검사 항목 수";UnitLines[1].Text=(english?"Shared bar scale":"막대 공통 축")+" 0–"+Maximum.ToString(System.Globalization.CultureInfo.InvariantCulture);
        Caption.Text=english?"These are check counts, not row counts or a whole-DB success rate. Unverified includes unreadable, interrupted and unstarted checks.":"검사 항목 수를 비교합니다. 행 수나 전체 DB 성공률이 아닙니다. 미검증에는 확인 불가·중단·미시작 항목이 포함됩니다.";
        _columns.SetLabels(english,compact);_columnBox.Padding=new(compact?16:20,12);
        // Source rerender replaces row DOM. Removal cancels the locked child's previous entrance.
        _rows.Children.Clear();
        for(var index=0;index<comparison.Length;index++)
        {
            var g=comparison[index];
            var (ko,en,icon)=g.Key switch{"data"=>("데이터","Data","db"),"schema"=>("구조·제약","Schema & constraints","grid"),"access"=>("사용자·권한","Users & permissions","shield"),_=>("관련 객체","Related objects","layers")};
            var status=g.Status=="completed"?(english?"Completed":"실행 완료"):g.Status=="interrupted"?(english?"Interrupted · Gaps retained":"중단 · 미검증 포함"):(english?"Not started · Unverified":"미시작 · 미검증");
            var row=new WebResultSummaryRow{Compact=compact,Last=index==comparison.Length-1};
            row.SetResult(g.Key,english?en:ko,status+(english?" · Total ":" · 전체 ")+g.Total,icon,g.Different,g.Total-g.Equal-g.Different,g.Equal,Maximum,english);
            row.Click+=(_,_)=>DetailRequested?.Invoke(row.CategoryKey);_rows.Children.Add(row);
        }
        InvalidateMeasure();
    }
    public Task PlayEntrance(bool reducedMotion=false,CancellationToken cancellation=default)=>Task.WhenAll(Rows.SelectMany(row=>row.Counts).Select(count=>count.PlayEntrance(reducedMotion,cancellation)));
    protected override Size ArrangeOverride(Size size){var result=base.ArrangeOverride(size);Clip=new RectangleGeometry{Rect=new Rect(size),RadiusX=10,RadiusY=10};return result;}
    private sealed class HeaderPanel:Panel
    {
        public WebSystemTextBlock Title{get;}=WebTypography.CreateCandidate("",16,24,Brush.Parse("#f2f3f5"),700,-.24);
        public WebSystemTextBlock Help{get;}=WebTypography.CreateCandidate("",12,19.2,Brush.Parse("#b5bac1"));
        public WebSystemTextBlock[] UnitLines{get;}=[WebTypography.CreateCandidate("",10,17,Brush.Parse("#b5bac1")),WebTypography.CreateCandidate("",10,17,Brush.Parse("#b5bac1"))];
        private double _leftWidth,_unitWidth,_leftHeight;
        public HeaderPanel(){UseLayoutRounding=false;Children.Add(Title);Children.Add(Help);foreach(var u in UnitLines){u.TextWrapping=TextWrapping.NoWrap;u.TextAlignment=TextAlignment.Right;Children.Add(u);}}
        protected override Size MeasureOverride(Size size)
        {
            var units=64*(TopLevel.GetTopLevel(this)?.RenderScaling??2);Help.LineHeight=Math.Floor(19.2*units)/units;
            Title.Measure(Size.Infinity);Help.Measure(Size.Infinity);foreach(var u in UnitLines)u.Measure(Size.Infinity);
            _unitWidth=Math.Ceiling(UnitLines.Max(x=>x.DesiredSize.Width)*units)/units;
            _leftWidth=Math.Min(Math.Max(0,size.Width-40-16-_unitWidth),Math.Ceiling(Math.Max(Title.DesiredSize.Width,Help.DesiredSize.Width)*units)/units);
            Title.Measure(new(_leftWidth,double.PositiveInfinity));Help.Measure(new(_leftWidth,double.PositiveInfinity));_leftHeight=Title.DesiredSize.Height+4+Help.DesiredSize.Height;
            return new(size.Width,36+Math.Max(_leftHeight,34));
        }
        protected override Size ArrangeOverride(Size size)
        {
            var units=64*(TopLevel.GetTopLevel(this)?.RenderScaling??2);var h=size.Height-36;var y=20+Math.Floor((h-_leftHeight)*units/2)/units;
            Title.Arrange(new(20,y,_leftWidth,Title.DesiredSize.Height));Help.Arrange(new(20,y+Title.DesiredSize.Height+4,_leftWidth,Help.DesiredSize.Height));
            var uy=20+Math.Floor((h-34)*units/2)/units;for(var i=0;i<2;i++)UnitLines[i].Arrange(new(size.Width-20-_unitWidth,uy+i*17,_unitWidth,17));return size;
        }
    }
    private sealed class ColumnsPanel:Panel
    {
        private readonly WebSystemTextBlock _category=WebTypography.CreateCandidate("",11,17.6,Brush.Parse("#b5bac1"));
        private readonly LabelPanel[] _labels=[new("warning","#ff959d"),new("info","#f0c47c"),new("check","#6bdda3")];
        private double[] _widths=[170,90,90,90,18];private bool _compact;
        public ColumnsPanel(){UseLayoutRounding=false;Children.Add(_category);foreach(var l in _labels)Children.Add(l);}
        public void SetLabels(bool english,bool compact){_compact=compact;_category.Text=english?"Check category":"검사 분류";var names=english?new[]{"Different","Unverified","Equal"}:new[]{"불일치","미검증","일치"};for(var i=0;i<3;i++)_labels[i].Text.Text=names[i];InvalidateMeasure();}
        private void Widths(double width)
        {
            var units=64*(TopLevel.GetTopLevel(this)?.RenderScaling??2);var min=_compact?155:170;var num=_compact?75:90;var gap=_compact?16:20;var tail=_compact?16:18;var space=Math.Max(min+num*3,width-4*gap-tail);
            var fr=(float)(space*units)/4.4f;var frozen=fr*1.4f<min*units;if(frozen)fr=(float)((space-min)*units)/3;float left=0;
            for(var i=0;i<4;i++){if(i==0&&frozen){_widths[i]=min;continue;}var share=fr*(i==0?1.4f:1)+left;var raw=Math.Floor(share+1e-6);_widths[i]=raw/units;left=(float)Math.Max(0,share-raw);}_widths[4]=tail;
        }
        protected override Size MeasureOverride(Size size){Widths(size.Width);var units=64*(TopLevel.GetTopLevel(this)?.RenderScaling??2);_category.LineHeight=Math.Floor(17.6*units)/units;for(var i=0;i<4;i++)Children[i].Measure(new(_widths[i],double.PositiveInfinity));return new(size.Width,Children.Max(x=>x.DesiredSize.Height));}
        protected override Size ArrangeOverride(Size size){Widths(size.Width);double x=0;for(var i=0;i<4;i++){var c=Children[i];c.Arrange(new(x,0,_widths[i],size.Height));x+=_widths[i]+(_compact?16:20);}return size;}
        protected override AutomationPeer OnCreateAutomationPeer()=>new HiddenColumnsPeer(this);
        private sealed class HiddenColumnsPeer(ColumnsPanel owner):ControlAutomationPeer(owner){protected override IReadOnlyList<AutomationPeer>? GetChildrenCore()=>null;}
    }
    private sealed class LabelPanel:Panel
    {
        public WebSystemTextBlock Text{get;}private readonly SvgIcon _icon;
        public LabelPanel(string icon,string color){UseLayoutRounding=false;_icon=new(){IconName=icon,Width=14,Height=14,Stroke=Brush.Parse(color)};Text=WebTypography.CreateCandidate("",11,17.6,Brush.Parse(color));Children.Add(_icon);Children.Add(Text);}
        protected override Size MeasureOverride(Size size){var units=64*(TopLevel.GetTopLevel(this)?.RenderScaling??2);Text.LineHeight=Math.Floor(17.6*units)/units;_icon.Measure(new(14,14));Text.Measure(new(Math.Max(0,size.Width-20),double.PositiveInfinity));return new(size.Width,Math.Max(14,Text.DesiredSize.Height));}
        protected override Size ArrangeOverride(Size size){var units=64*(TopLevel.GetTopLevel(this)?.RenderScaling??2);_icon.Arrange(new(0,Math.Floor((size.Height-14)*units/2)/units,14,14));Text.Arrange(new(20,0,Math.Max(0,size.Width-20),size.Height));return size;}
    }
}
