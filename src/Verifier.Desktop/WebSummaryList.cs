using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
namespace Verifier.Desktop;

/// <summary>Zero-gap source summary grid, optionally inside .panel.panel-pad.</summary>
public sealed class WebSummaryList:Decorator
{
    private readonly WebSurfaceBox _surface;
    private readonly RowsPanel _body=new();
    public IReadOnlyList<WebSummaryItem> Rows=>_body.Items;
    public WebSurfaceBox Surface=>_surface;
    public WebSummaryList(){UseLayoutRounding=false;_surface=new(){Child=_body};Child=_surface;}
    public void SetPresentation(IEnumerable<WebSummaryValue> values,bool compact,bool panel)
    {
        _surface.Background=panel?Brush.Parse("#2b2d31"):null;_surface.BorderBrush=panel?Brush.Parse("#41434a"):null;_surface.BorderThickness=panel?new Thickness(1):default;_surface.Padding=panel?new Thickness(26):default;_surface.CornerRadius=panel?new CornerRadius(10):default;_surface.BoxShadow=panel?new BoxShadows(new BoxShadow{OffsetX=0,OffsetY=2,Blur=4,Color=Color.Parse("#10000000")}):default;
        _body.Set(values.ToArray(),compact);InvalidateMeasure();
    }
    private sealed class RowsPanel:Panel
    {
        public List<WebSummaryItem> Items{get;}=[];private double _columnWidth,_label;
        public RowsPanel(){UseLayoutRounding=false;}
        public void Set(WebSummaryValue[] rows,bool compact)
        {Children.Clear();Items.Clear();_label=compact?130:170;for(var i=0;i<rows.Length;i++){var row=new WebSummaryItem();row.SetPresentation(rows[i],i==rows.Length-1,compact);Items.Add(row);Children.Add(row);}InvalidateMeasure();}
        protected override Size MeasureOverride(Size available)
        {
            _columnWidth=available.Width;var units=(TopLevel.GetTopLevel(this)?.RenderScaling??2)*64;foreach(var row in Items){row.Measure(new(0,double.PositiveInfinity));_columnWidth=Math.Max(_columnWidth,Math.Ceiling((_label+20+row.ValueControl.DesiredSize.Width)*units)/units);}
            double height=0;foreach(var row in Items){row.Measure(new(_columnWidth,double.PositiveInfinity));height+=row.DesiredSize.Height;}return new(available.Width,height);
        }
        protected override Size ArrangeOverride(Size final)
        {double y=0;foreach(var row in Items){row.Arrange(new(0,y,_columnWidth,row.DesiredSize.Height));y+=row.DesiredSize.Height;}return final;}
    }
}
