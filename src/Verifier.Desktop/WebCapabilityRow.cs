using System.Diagnostics;
using Avalonia;
using Avalonia.Automation;
using Avalonia.Automation.Peers;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.VisualTree;
namespace Verifier.Desktop;

/// <summary>Literal capabilities() tr. The parent supplies shared table columns and collapsed borders.</summary>
public sealed class WebCapabilityRow : Panel
{
    public static readonly (string Key,string Ko,string En,string DescriptionKo,string DescriptionEn)[] Definitions = [
        ("data","데이터","Data","전체 값·누락·중복·참조 관계","All values, missing rows, duplicates and relationships"),
        ("schema","구조·제약","Schema & constraints","컬럼·자료형·키·제약·인덱스·partition","Columns, types, keys, constraints, indexes and partitions"),
        ("access","사용자·권한","Users & permissions","사용자·역할·소유자·권한·행 정책","Users, roles, ownership, grants and row policies"),
        ("objects","관련 객체","Related objects","view·함수·trigger·sequence·사용자 타입","Views, routines, triggers, sequences and user types")];
    public WebSystemTextBlock TitleText { get; } = WebTypography.CreateCandidate("",13,20.8,weight:700);
    public WebSystemTextBlock DescriptionText { get; } = WebTypography.CreateCandidate("",12,19.2,Brush.Parse("#b5bac1"));
    public WebSystemTextBlock DetailText { get; } = WebTypography.CreateCandidate("",12,19.2);
    public WebStatusPill ConnectedPill { get; } = new(){Kind="ok",HorizontalAlignment=Avalonia.Layout.HorizontalAlignment.Stretch};
    public WebStatusPill CapabilityPill { get; } = new(){HorizontalAlignment=Avalonia.Layout.HorizontalAlignment.Stretch};
    public WebActionButton PermissionsButton { get; } = new(){Variant="quiet",LabelFontSize=12,Padding=new(10,6)};
    public Panel Details { get; }
    public Control DetailSpan=>_details.Span;
    public IReadOnlyList<Cell> Cells=>_cells;
    public string Key { get; private set; } = "data";
    public bool English { get; private set; }
    public bool ReducedMotion { get=>_reduced; set{_reduced=value;PermissionsButton.ReducedMotion=value;Paint();} }
    public double TopBorderShare {get;set;}=.5;
    public double BottomBorderShare {get;set;}=.5;
    public double[] ColumnWidths {get=>_columns.ToArray();set{if(value.Length!=4||value.Any(x=>!double.IsFinite(x)||x<36))throw new ArgumentException("Four measured table columns required");_columns=value.ToArray();Width=value.Sum();InvalidateMeasure();}}
    public event EventHandler? PermissionsRequested;
    public IBrush? RowBackground=>Background;
    private double[] _columns=[314.8203125,140.0859375,140.0859375,383.0078125];
    private readonly DetailsPanel _details;
    private readonly BackgroundTrack _background=new();
    private bool _reduced,_hover;private long _revision;
    private readonly Cell[] _cells=[new(),new(),new(),new()];
    private double _categoryHeight;private readonly RowBorders _borders=new(){IsHitTestVisible=false};
    public WebCapabilityRow()
    {
        UseLayoutRounding=false;Focusable=false;ClipToBounds=false;
        _details=new(DetailText,PermissionsButton);Details=_details;
        Children.Add(_borders);AutomationProperties.SetAccessibilityView(_borders,AccessibilityView.Raw);
        _cells[0].Children.Add(TitleText);_cells[0].Children.Add(DescriptionText);_cells[1].Children.Add(ConnectedPill);_cells[2].Children.Add(CapabilityPill);_cells[3].Children.Add(Details);for(var i=0;i<4;i++){AutomationProperties.SetAutomationId(_cells[i],"CapabilityRow-cell-"+i);Children.Add(_cells[i]);}
        _cells[0].NameProvider=()=>TitleText.Text+" "+DescriptionText.Text;
        _cells[1].NameProvider=()=>ConnectedPill.Text;
        _cells[2].NameProvider=()=>CapabilityPill.Text;
        _cells[3].NameProvider=()=>DetailText.Text+(PermissionsButton.IsVisible?" "+PermissionsButton.Content:"");
        _cells[3].ButtonProvider=()=>PermissionsButton.IsVisible?PermissionsButton:null;
        PermissionsButton.Click+=(_,_)=>PermissionsRequested?.Invoke(this,EventArgs.Empty);
        void Track(PointerEventArgs e){var inside=new Rect(Bounds.Size).Contains(e.GetPosition(this));if(inside==_hover)return;_hover=inside;Paint();}
        PointerEntered+=(_,e)=>Track(e);PointerExited+=(_,e)=>{var captured=e.Pointer.Captured as Visual;if(captured==this||captured?.GetVisualAncestors().Contains(this)==true){Track(e);return;}if(!_hover)return;_hover=false;Paint();};
        AddHandler(InputElement.PointerMovedEvent,(_,e)=>Track(e),RoutingStrategies.Tunnel,true);
        AddHandler(InputElement.PointerPressedEvent,(_,e)=>{if(!e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)return;var v=e.Source as Visual;if(v==PermissionsButton||v?.GetVisualAncestors().Contains(PermissionsButton)==true)return;TopLevel.GetTopLevel(this)?.FocusManager?.Focus(null,NavigationMethod.Pointer);},RoutingStrategies.Tunnel,true);
        SetPresentation("data",false,false);
    }
    public void SetPresentation(string key,bool english,bool last)
    {
        var d=Definitions.Single(x=>x.Key==key);Key=key;English=english;BottomBorderShare=last?0:.5;
        string T(string ko,string en)=>english?en:ko;
        TitleText.Text=T(d.Ko,d.En);DescriptionText.Text=T(d.DescriptionKo,d.DescriptionEn);ConnectedPill.Text=T("연결 확인","Connected");
        CapabilityPill.Kind=key=="access"?"warn":"ok";CapabilityPill.Text=key=="access"?T("일부 가능","Partially available"):T("가능","Available");
        DetailText.Text=key=="access"?T("권한 정의 2개 조회 범위 미확인","Visibility unconfirmed for 2 grant checks"):T("선택 대상의 예시 조회 가능","Sample selected objects are readable");
        PermissionsButton.Content=T("필요한 권한 보기","Required permissions");PermissionsButton.IsVisible=key=="access";foreach(var cell in _cells)cell.Refresh();AutomationProperties.SetAutomationId(PermissionsButton,"CapabilityRow-permissions");InvalidateMeasure();
    }
    private double PillWidth(WebStatusPill pill)=>Math.Ceiling(pill.DesiredSize.Width*Units)/Units;
    private double Units=>(TopLevel.GetTopLevel(this)?.RenderScaling??2)*64;
    protected override Size MeasureOverride(Size size)
    {
        foreach(var t in new[]{TitleText,DescriptionText,DetailText})t.LineHeight=Math.Floor(t.FontSize*1.6*Units)/Units;
        var categoryWidth=Math.Max(0,_columns[0]-36);TitleText.Measure(new(categoryWidth,double.PositiveInfinity));DescriptionText.Measure(new(categoryWidth,double.PositiveInfinity));_categoryHeight=TitleText.DesiredSize.Height+3+DescriptionText.DesiredSize.Height;
        ConnectedPill.Measure(Size.Infinity);CapabilityPill.Measure(Size.Infinity);Details.Measure(new(Math.Max(0,_columns[3]-36),double.PositiveInfinity));
        var height=24+TopBorderShare+BottomBorderShare+Math.Max(_categoryHeight,Math.Max(ConnectedPill.DesiredSize.Height,Math.Max(CapabilityPill.DesiredSize.Height,Details.DesiredSize.Height)));for(var i=0;i<4;i++)_cells[i].Measure(new(_columns[i],height));return new(_columns.Sum(),height);
    }
    protected override Size ArrangeOverride(Size size)
    {
        double left=0;for(var i=0;i<4;i++){_cells[i].Arrange(new(left,0,_columns[i],size.Height));left+=_columns[i];}_borders.TopShare=TopBorderShare;_borders.BottomShare=BottomBorderShare;_borders.Arrange(new(default(Point),size));var h=size.Height-24-TopBorderShare-BottomBorderShare;double Y(double height)=>12+TopBorderShare+Math.Floor((h-height)*Units/2)/Units;
        var categoryWidth=Math.Max(0,_columns[0]-36);var y=Y(_categoryHeight);TitleText.Arrange(new(18,y,categoryWidth,TitleText.DesiredSize.Height));DescriptionText.Arrange(new(18,y+TitleText.DesiredSize.Height+3,categoryWidth,DescriptionText.DesiredSize.Height));
        ConnectedPill.Arrange(new(Math.Floor((_columns[1]-PillWidth(ConnectedPill))*Units/2)/Units,Y(ConnectedPill.DesiredSize.Height),PillWidth(ConnectedPill),ConnectedPill.DesiredSize.Height));
        CapabilityPill.Arrange(new(Math.Floor((_columns[2]-PillWidth(CapabilityPill))*Units/2)/Units,Y(CapabilityPill.DesiredSize.Height),PillWidth(CapabilityPill),CapabilityPill.DesiredSize.Height));Details.Arrange(new(18,Y(Details.DesiredSize.Height),Math.Max(0,_columns[3]-36),Details.DesiredSize.Height));return size;
    }
    protected override AutomationPeer OnCreateAutomationPeer()=>new RowPeer(this);
    private sealed class RowPeer(WebCapabilityRow row):ControlAutomationPeer(row)
    {
        protected override AutomationControlType GetAutomationControlTypeCore()=>AutomationControlType.DataItem;
        protected override string GetClassNameCore()=>"Row";
        protected override string GetLocalizedControlTypeCore()=>"row";
        protected override IReadOnlyList<AutomationPeer>? GetChildrenCore()=>row.Cells.Select(ControlAutomationPeer.CreatePeerForElement).OfType<AutomationPeer>().ToArray();
    }
    public sealed class Cell:Panel
    {
        private CellPeer? _peer;
        internal Func<string?>? NameProvider;
        internal Func<Control?>? ButtonProvider;
        internal void Refresh()=>_peer?.Refresh();
        public Cell(){UseLayoutRounding=false;Focusable=false;ClipToBounds=false;}
        protected override Size MeasureOverride(Size s)=>s;
        protected override Size ArrangeOverride(Size s)=>s;
        protected override AutomationPeer OnCreateAutomationPeer()=>_peer=new CellPeer(this);
        private sealed class CellPeer(Cell cell):ControlAutomationPeer(cell)
        {
            private string? _name;
            public void Refresh(){InvalidateChildren();if(OperatingSystem.IsWindows()){var name=cell.NameProvider?.Invoke()??"";RaisePropertyChangedEvent(AutomationElementIdentifiers.NameProperty,_name,name);_name=name;}}
            protected override AutomationControlType GetAutomationControlTypeCore()=>OperatingSystem.IsWindows()?AutomationControlType.DataItem:AutomationControlType.Custom;
            protected override string? GetNameCore()=>OperatingSystem.IsWindows()?cell.NameProvider?.Invoke()??"":base.GetNameCore();
            protected override IReadOnlyList<AutomationPeer>? GetChildrenCore()=>OperatingSystem.IsWindows()?(cell.ButtonProvider?.Invoke() is{}button&&ControlAutomationPeer.CreatePeerForElement(button) is{}peer?[peer]:Array.Empty<AutomationPeer>()):base.GetChildrenCore();
            protected override string GetClassNameCore()=>"Cell";
            protected override string GetLocalizedControlTypeCore()=>"cell";
        }
    }
    private sealed class RowBorders:Control
    {
        public double TopShare,BottomShare;
        public override void Render(DrawingContext context){base.Render(context);var pen=new Pen(Brush.Parse("#41434a"),1);if(TopShare>0)context.DrawLine(pen,new(0,0),new(Bounds.Width,0));if(BottomShare>0)context.DrawLine(pen,new(0,Bounds.Height),new(Bounds.Width,Bounds.Height));}
    }
    private void Paint(){var color=Color.Parse(_hover?"#3a3c44":"#00000000");var rev=++_revision;if(_reduced||TopLevel.GetTopLevel(this) is not{}top){_background.Snap(color);Background=new SolidColorBrush(color);return;}_background.Set(color);void Frame(TimeSpan _){if(rev!=_revision||!this.IsAttachedToVisualTree())return;Background=new SolidColorBrush(_background.Current);if(_background.Running)top.RequestAnimationFrame(Frame);}top.RequestAnimationFrame(Frame);}
    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e){++_revision;_hover=false;_background.Snap(default);Background=Brushes.Transparent;base.OnDetachedFromVisualTree(e);}
    private sealed class DetailsPanel(WebSystemTextBlock text,WebActionButton button):Panel
    {
        public Control Span{get;}=new OverflowSpan(text);
        protected override AutomationPeer OnCreateAutomationPeer()=>OperatingSystem.IsWindows()?new HiddenDetailsPeer(this):base.OnCreateAutomationPeer();
        private sealed class HiddenDetailsPeer(DetailsPanel owner):ControlAutomationPeer(owner)
        {
            protected override bool IsControlElementCore()=>false;
            protected override bool IsContentElementCore()=>false;
            protected override IReadOnlyList<AutomationPeer>? GetChildrenCore()=>Array.Empty<AutomationPeer>();
        }
        private bool _added;private double _textWidth;
        protected override Size MeasureOverride(Size s){if(!_added){Children.Add(Span);Children.Add(button);_added=true;}button.Measure(Size.Infinity);_textWidth=Math.Max(0,s.Width-(button.IsVisible?button.DesiredSize.Width+12:0));Span.Measure(new(_textWidth,double.PositiveInfinity));return new(s.Width,Math.Max(Span.DesiredSize.Height,button.IsVisible?button.DesiredSize.Height:0));}
        protected override Size ArrangeOverride(Size s){var units=(TopLevel.GetTopLevel(this)?.RenderScaling??2)*64;double Y(double h)=>Math.Floor((s.Height-h)*units/2)/units;Span.Arrange(new(0,Y(Span.DesiredSize.Height),_textWidth,Span.DesiredSize.Height));if(button.IsVisible)button.Arrange(new(_textWidth+12,Y(button.DesiredSize.Height),button.DesiredSize.Width,button.DesiredSize.Height));return s;}
    }
    // CSS span may have a zero flex width while its text node still paints overflowing glyphs.
    private sealed class OverflowSpan(WebSystemTextBlock text):Decorator
    {
        private double TextWidth(double width)=>Math.Max(width,1/((TopLevel.GetTopLevel(this)?.RenderScaling??2)*64));
        protected override Size MeasureOverride(Size s){Child=text;UseLayoutRounding=false;ClipToBounds=false;text.Measure(new(TextWidth(s.Width),double.PositiveInfinity));return new(s.Width,text.DesiredSize.Height);}
        protected override Size ArrangeOverride(Size s){text.Arrange(new(0,0,TextWidth(s.Width),s.Height));return s;}
    }
    private sealed class BackgroundTrack(Func<double>? clock=null)
    {
        private double Now=>clock?.Invoke()??Stopwatch.GetTimestamp()*1000d/Stopwatch.Frequency;private Color _from,_to,_reverse;private double _start,_duration,_factor=1;private double P=>_duration<=0?1:Math.Clamp((Now-_start)/_duration,0,1);public bool Running=>P<1;private static double Ease(double p)=>WebMotion.CssEase().Ease(p);
        public Color Current{get{var t=Ease(P);var a=_from.A+(_to.A-_from.A)*t;byte B(double n)=>(byte)Math.Clamp(Math.Round(n,MidpointRounding.AwayFromZero),0,255);byte C(byte x,byte y)=>a==0?(byte)0:B((x*_from.A*(1-t)+y*_to.A*t)/a);return Color.FromArgb(B(a),C(_from.R,_to.R),C(_from.G,_to.G),C(_from.B,_to.B));}}
        public void Snap(Color c){_from=_to=_reverse=c;_duration=0;_factor=1;}public void Set(Color c){if(c==_to)return;var p=P;var current=Current;if(p<1&&c==_reverse){_factor=Math.Clamp(Math.Abs(Ease(p)*_factor+1-_factor),0,1);_reverse=_to;}else{_factor=1;_reverse=current;}_from=current;_to=c;_start=Now;_duration=160*_factor;}
    }
}
