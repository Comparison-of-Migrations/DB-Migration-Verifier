using Avalonia;
using Avalonia.Automation;
using Avalonia.Automation.Peers;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Input;
using Avalonia.Interactivity;
namespace Verifier.Desktop;

/// <summary>Literal capabilities thead row; its parent owns shared table column allocation.</summary>
public sealed class WebCapabilityHeader : Panel
{
    private double[] _columns=[291,129.484375,129.484375,354.03125];
    private readonly WebSurfaceBox _surface=WebSurface.Create(Brush.Parse("#232428"),Brush.Parse("#41434a"),new(0,0,0,.5),new(0));
    public IReadOnlyList<HeaderCell> Cells {get;}
    public IReadOnlyList<WebSystemTextBlock> Labels {get;}
    public bool English{get;private set;}
    public IBrush? HeaderBackground=>_surface.Background;
    public IBrush? HeaderBorder=>_surface.BorderBrush;
    public double BottomBorderShare{get=>_surface.BorderThickness.Bottom;set{_surface.BorderThickness=new(0,0,0,value);InvalidateMeasure();}}
    public double[] ColumnWidths{get=>_columns.ToArray();set{if(value.Length!=4||value.Any(x=>!double.IsFinite(x)||x<36))throw new ArgumentException("Four source table columns required");_columns=value.ToArray();Width=value.Sum();InvalidateMeasure();}}
    private double Units=>(TopLevel.GetTopLevel(this)?.RenderScaling??2)*64;
    public WebCapabilityHeader()
    {
        UseLayoutRounding=false;Focusable=false;ClipToBounds=false;Background=Brushes.Transparent;
        _surface.IsHitTestVisible=false;AutomationProperties.SetAccessibilityView(_surface,AccessibilityView.Raw);Children.Add(_surface);
        var labels=Enumerable.Range(0,4).Select(_=>WebTypography.CreateCandidate("",12,19.2,Brush.Parse("#b5bac1"),600)).ToArray();Labels=labels;
        AddHandler(InputElement.PointerPressedEvent,(_,e)=>{if(e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)TopLevel.GetTopLevel(this)?.FocusManager?.Focus(null,NavigationMethod.Pointer);},RoutingStrategies.Tunnel,true);
        if(OperatingSystem.IsWindows())foreach(var label in labels)AutomationProperties.SetAccessibilityView(label,AccessibilityView.Raw);
        Cells=labels.Select((label,index)=>new HeaderCell(label,index)).ToArray();foreach(var cell in Cells)Children.Add(cell);SetEnglish(false);
    }
    public void SetEnglish(bool english){English=english;var text=english?new[]{"Category","Source / target","Capability","Details"}:new[]{"검사 분류","원본 / 대상","검사 가능 범위","안내"};for(var i=0;i<4;i++)Labels[i].Text=text[i];InvalidateMeasure();}
    protected override Size MeasureOverride(Size s)
    {
        var height=0d;for(var i=0;i<4;i++){Labels[i].LineHeight=Math.Floor(12*1.6*Units)/Units;Labels[i].Measure(new(Math.Max(0,_columns[i]-36)+(OperatingSystem.IsWindows()?1/Units:0),double.PositiveInfinity));height=Math.Max(height,Labels[i].DesiredSize.Height);}
        height+=24+BottomBorderShare;_surface.Measure(new(_columns.Sum(),height));for(var i=0;i<4;i++)Cells[i].Measure(new(_columns[i],height));return new(_columns.Sum(),height);
    }
    protected override Size ArrangeOverride(Size s)
    {
        _surface.Arrange(new(default(Point),s));double x=0;for(var i=0;i<4;i++){Cells[i].Arrange(new(x,0,_columns[i],s.Height));var t=Labels[i];var width=Math.Ceiling(t.DesiredSize.Width*Units)/Units;var left=i is 1 or 2?Math.Floor((_columns[i]-width)*Units/2)/Units:18;var y=12+Math.Floor((s.Height-24-BottomBorderShare-t.DesiredSize.Height)*Units/2)/Units;t.Arrange(new(left,y,width,t.DesiredSize.Height));x+=_columns[i];}return s;
    }
    protected override AutomationPeer OnCreateAutomationPeer()=>new HeaderRowPeer(this);
    private sealed class HeaderRowPeer(WebCapabilityHeader row):ControlAutomationPeer(row)
    {
        protected override AutomationControlType GetAutomationControlTypeCore()=>AutomationControlType.DataItem;
        protected override string GetClassNameCore()=>"Row";
        protected override string GetLocalizedControlTypeCore()=>"row";
        protected override IReadOnlyList<AutomationPeer>? GetChildrenCore()=>row.Cells.Select(ControlAutomationPeer.CreatePeerForElement).OfType<AutomationPeer>().ToArray();
    }
    public sealed class HeaderCell:Panel
    {
        internal HeaderCell(WebSystemTextBlock text,int index){UseLayoutRounding=false;Focusable=false;Children.Add(text);AutomationProperties.SetAutomationId(this,"CapabilityHeader-cell-"+index);}
        protected override Size MeasureOverride(Size s)=>s;
        protected override Size ArrangeOverride(Size s)=>s;
        protected override AutomationPeer OnCreateAutomationPeer()=>new HeaderCellPeer(this);
        private sealed class HeaderCellPeer(HeaderCell cell):ControlAutomationPeer(cell)
        {
            protected override string? GetNameCore()=>OperatingSystem.IsWindows()?((WebSystemTextBlock)cell.Children[0]).Text??"":base.GetNameCore();
            protected override IReadOnlyList<AutomationPeer>? GetChildrenCore()=>OperatingSystem.IsWindows()?Array.Empty<AutomationPeer>():base.GetChildrenCore();
            protected override AutomationControlType GetAutomationControlTypeCore()=>AutomationControlType.HeaderItem;
            protected override string GetClassNameCore()=>"ColumnHeader";
            protected override string GetLocalizedControlTypeCore()=>"column header";
        }
    }
}
