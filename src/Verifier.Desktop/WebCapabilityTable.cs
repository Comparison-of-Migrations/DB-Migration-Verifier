using Avalonia;
using Avalonia.Automation.Peers;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.VisualTree;
namespace Verifier.Desktop;

/// <summary>Original capabilities table: intrinsic shared columns, collapsed rows and horizontal shell.</summary>
public sealed class WebCapabilityTable : Panel
{
    private readonly WebSurfaceBox _surface=WebSurface.Create(Brush.Parse("#2b2d31"),Brush.Parse("#41434a"),new(1),new(10));
    private readonly TableContent _table;
    public WebCapabilityViewport Viewport {get;}=new(){UseLayoutRounding=false};
    public WebCapabilityHeader Header {get;}=new();
    public Control TableControl=>_table;
    public IReadOnlyList<WebCapabilityRow> Rows {get;}
    public double[] ColumnWidths {get;private set;}=[36,36,36,36];
    public double[] MinimumColumns {get;private set;}=[36,36,36,36];
    public double[] MaximumColumns {get;private set;}=[36,36,36,36];
    public bool English {get;private set;}
    public bool ReducedMotion {get=>Rows[0].ReducedMotion;set{foreach(var row in Rows)row.ReducedMotion=value;}}
    public event EventHandler? PermissionsRequested;
    private double Units=>(TopLevel.GetTopLevel(this)?.RenderScaling??2)*64;
    public WebCapabilityTable()
    {
        UseLayoutRounding=false;Focusable=false;Background=Brushes.Transparent;
        Rows=WebCapabilityRow.Definitions.Select((d,i)=>{var row=new WebCapabilityRow{Background=Brushes.Transparent};row.SetPresentation(d.Key,false,i==3);row.PermissionsRequested+=(_,_)=>PermissionsRequested?.Invoke(this,EventArgs.Empty);return row;}).ToArray();
        _table=new TableContent(Header,Rows);Viewport.Content=_table;
        _surface.IsHitTestVisible=false;Children.Add(_surface);Children.Add(Viewport);
    }
    public void SetEnglish(bool english)
    {
        English=english;Header.SetEnglish(english);for(var i=0;i<Rows.Count;i++)Rows[i].SetPresentation(WebCapabilityRow.Definitions[i].Key,english,i==Rows.Count-1);InvalidateMeasure();
    }
    private double Ceil(double n)=>Math.Ceiling(n*Units)/Units;
    private (double Min,double Max) TextIntrinsic(WebSystemTextBlock text)
    {
        text.Measure(Size.Infinity);var max=Ceil(text.SystemLineWidths.DefaultIfEmpty(text.DesiredSize.Width).Max());
        text.Measure(new Size(1/Units,double.PositiveInfinity));var min=Ceil(text.SystemLineWidths.DefaultIfEmpty(text.DesiredSize.Width).Max());return(min,max);
    }
    private void Intrinsics()
    {
        var min=new double[4];var max=new double[4];
        void Add(int i,double a,double b){min[i]=Math.Max(min[i],a+36);max[i]=Math.Max(max[i],b+36);}
        for(var i=0;i<4;i++){var t=TextIntrinsic(Header.Labels[i]);Add(i,t.Min,t.Max);}
        foreach(var row in Rows)
        {
            var title=TextIntrinsic(row.TitleText);var description=TextIntrinsic(row.DescriptionText);Add(0,Math.Max(title.Min,description.Min),Math.Max(title.Max,description.Max));
            foreach(var item in new[]{(1,row.ConnectedPill),(2,row.CapabilityPill)}){var label=WebTypography.CreateCandidate(item.Item2.Text,12,19.2);var w=TextIntrinsic(label).Max+28;Add(item.Item1,w,w);}
            var details=TextIntrinsic(row.DetailText);
            if(row.Key=="access")
            {
                row.PermissionsButton.Measure(Size.Infinity);
                var label=WebTypography.CreateCandidate(row.PermissionsButton.Content?.ToString()??"",12,19.2,weight:600);var intrinsic=TextIntrinsic(label);
                Add(3,details.Min+intrinsic.Min+22+12,details.Max+intrinsic.Max+22+12);
            }
            else Add(3,details.Min,details.Max);
        }
        // Chromium min-content is a layout constraint, not the ordinary glyph advance.
        // Literal static original rows, default macOS system font, KO/EN, DPR2:
        // docs/replica/source/capabilitytable-mac-cell-intrinsics.json.
        MinimumColumns=OperatingSystem.IsMacOS()?OriginalMacMinimumColumns(English):OperatingSystem.IsWindows()?OriginalWindowsMinimumColumns(English):min;MaximumColumns=max;
    }
    private static double[] OriginalMacMinimumColumns(bool english)=>english
        ?[114.8671875,125.875,161.765625,213.21875]
        :[139.5078125,108.8984375,108.8984375,90.765625];
    private static double[] OriginalWindowsMinimumColumns(bool english)=>english
        ?[109.5625,122.4609375,157.484375,206.7421875]
        :[131.5546875,116.21875,116.21875,94];
    protected override Size MeasureOverride(Size available)
    {
        Intrinsics();var inner=double.IsPositiveInfinity(available.Width)?MaximumColumns.Sum():Math.Max(0,available.Width-2);var width=Math.Max(inner,MinimumColumns.Sum());var min=MinimumColumns.Sum();var max=MaximumColumns.Sum();var widths=new double[4];
        for(var i=0;i<3;i++)
        {
            var value=width>=max?MaximumColumns[i]*width/max:MinimumColumns[i]+(max==min?0:(width-min)*(MaximumColumns[i]-MinimumColumns[i])/(max-min));
            widths[i]=Math.Floor(value*Units)/Units;
        }
        widths[3]=width-widths.Take(3).Sum();ColumnWidths=widths;
        if(!Header.ColumnWidths.SequenceEqual(widths))Header.ColumnWidths=widths;
        foreach(var row in Rows)if(!row.ColumnWidths.SequenceEqual(widths))row.ColumnWidths=widths;
        _table.Measure(new Size(width,double.PositiveInfinity));
        var access=Rows.Single(r=>r.Key=="access");var detailsWidth=widths[3]-36;var buttonLabel=WebTypography.CreateCandidate(access.PermissionsButton.Content?.ToString()??"",12,19.2,weight:600);var buttonWidth=TextIntrinsic(buttonLabel).Max+22;var overflow=Math.Max(width,width-widths[3]+18+Math.Max(0,detailsWidth-buttonWidth-12)+12+buttonWidth);
        Viewport.LogicalWidth=width;Viewport.OverflowWidth=overflow;Viewport.Measure(new Size(inner,double.PositiveInfinity));var size=new Size(inner+2,Viewport.DesiredSize.Height+2);_surface.Measure(size);return size;
    }
    protected override Size ArrangeOverride(Size size)
    {
        _surface.Arrange(new Rect(size));Viewport.Arrange(new Rect(1,1,Math.Max(0,size.Width-2),Math.Max(0,size.Height-2)));Viewport.Clip=new RectangleGeometry{Rect=new Rect(Viewport.Bounds.Size),RadiusX=9,RadiusY=9};return size;
    }
    protected override AutomationPeer OnCreateAutomationPeer()=>new ShellPeer(this);
    private sealed class ShellPeer(WebCapabilityTable owner):ControlAutomationPeer(owner)
    {
        protected override bool IsControlElementCore()=>false;
        protected override bool IsContentElementCore()=>false;
        protected override IReadOnlyList<AutomationPeer>? GetChildrenCore()=>[CreatePeerForElement(owner.Viewport)];
    }
    private sealed class TableContent:Panel
    {
        public IReadOnlyList<RowGroup> Groups{get;}
        public TableContent(WebCapabilityHeader header,IReadOnlyList<WebCapabilityRow> rows){UseLayoutRounding=false;Groups=[new RowGroup([header]),new RowGroup(rows.Cast<Control>().ToArray())];foreach(var group in Groups)Children.Add(group);}
        protected override AutomationPeer OnCreateAutomationPeer()=>new TablePeer(this);
        private sealed class TablePeer(TableContent owner):ControlAutomationPeer(owner)
        {
            protected override AutomationControlType GetAutomationControlTypeCore()=>AutomationControlType.Table;
            protected override string GetClassNameCore()=>"Table";
        }
        protected override Size MeasureOverride(Size s){foreach(var group in Groups)group.Measure(new(s.Width,double.PositiveInfinity));return new(s.Width,Groups.Sum(x=>x.DesiredSize.Height));}
        protected override Size ArrangeOverride(Size s){var y=0d;foreach(var group in Groups){group.Arrange(new(0,y,s.Width,group.DesiredSize.Height));y+=group.DesiredSize.Height;}return s;}
    }
    private sealed class RowGroup:Panel
    {
        public RowGroup(IEnumerable<Control> rows){UseLayoutRounding=false;foreach(var row in rows)Children.Add(row);}
        protected override Size MeasureOverride(Size s){foreach(var row in Children)row.Measure(new(s.Width,double.PositiveInfinity));return new(s.Width,Children.Sum(x=>x.DesiredSize.Height));}
        protected override Size ArrangeOverride(Size s){var y=0d;foreach(var row in Children){row.Arrange(new(0,y,s.Width,row.DesiredSize.Height));y+=row.DesiredSize.Height;}return s;}
        protected override AutomationPeer OnCreateAutomationPeer()=>new GroupPeer(this);
        private sealed class GroupPeer(RowGroup owner):ControlAutomationPeer(owner)
        {
            protected override AutomationControlType GetAutomationControlTypeCore()=>AutomationControlType.Group;
            protected override string GetClassNameCore()=>"RowGroup";
            protected override bool IsControlElementCore()=>!OperatingSystem.IsWindows()&&base.IsControlElementCore();
            protected override bool IsContentElementCore()=>!OperatingSystem.IsWindows()&&base.IsContentElementCore();
        }
    }
}
