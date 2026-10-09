using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
namespace Verifier.Desktop;
public sealed record WebSummaryValue(string Label,IReadOnlyList<string> Lines,bool Strong=false,bool Mono=false,string Suffix="");

/// <summary>Literal .summary-item; viewport media choice and last-child identity belong to the list.</summary>
public sealed class WebSummaryItem:Decorator
{
    private readonly Contents _content=new();
    private readonly WebSurfaceBox _surface;
    public WebSystemTextBlock LabelText=>_content.Label;
    public Control ValueControl=>_content.Value;
    public IReadOnlyList<WebSystemTextBlock> ValueLines=>_content.Value.Lines;
    public WebInlineText? InlineValue=>_content.Value.Inline;
    public WebSummaryItem(){UseLayoutRounding=false;_surface=new(){Padding=new(0,16),BorderBrush=Brush.Parse("#41434a"),Child=_content};Child=_surface;}
    public void SetPresentation(WebSummaryValue value,bool last,bool compact)
    {_surface.BorderThickness=new(0,0,0,last?0:1);_content.Column=compact?130:170;_content.Label.Text=value.Label;_content.Value.Set(value);InvalidateMeasure();}
    private sealed class Contents:Panel
    {
        public WebSystemTextBlock Label{get;}=WebTypography.CreateCandidate("",13,22,Brush.Parse("#b5bac1"));
        public ValueBody Value{get;}=new();public double Column{get;set;}=170;private double _valueWidth;
        public Contents(){UseLayoutRounding=false;Children.Add(Label);Children.Add(Value);}
        protected override Size MeasureOverride(Size available)
        {Label.Measure(new(Column,double.PositiveInfinity));var units=(TopLevel.GetTopLevel(this)?.RenderScaling??2)*64;Value.Measure(new(1/units,double.PositiveInfinity));_valueWidth=Math.Max(Math.Max(0,available.Width-Column-20),Value.MinimumWidth);Value.Measure(new(_valueWidth,double.PositiveInfinity));return new(available.Width,Math.Max(Label.DesiredSize.Height,Value.DesiredSize.Height));}
        protected override Size ArrangeOverride(Size final)
        {Label.Arrange(new(0,0,Column,Label.DesiredSize.Height));Value.Arrange(new(Column+20,0,_valueWidth,Value.DesiredSize.Height));return final;}
    }
    private sealed class ValueBody:Panel
    {
        public List<WebSystemTextBlock> Lines{get;}=[];public WebInlineText? Inline{get;private set;}public double MinimumWidth{get;private set;}
        public ValueBody(){UseLayoutRounding=false;}
        public void Set(WebSummaryValue v)
        {Children.Clear();Lines.Clear();Inline=null;if(v.Mono){Inline=new();Inline.SetPresentation(v.Lines.FirstOrDefault()??"",v.Suffix);Children.Add(Inline);}else foreach(var text in v.Lines){var child=WebTypography.CreateCandidate(text,13,22,Brush.Parse("#dbdee1"),v.Strong?700:400);Lines.Add(child);Children.Add(child);}InvalidateMeasure();}
        protected override Size MeasureOverride(Size available)
        {double height=0;MinimumWidth=0;foreach(var child in Children){child.Measure(available);height+=child.DesiredSize.Height;var widths=child is WebInlineText inline?inline.TextControl.SystemLineWidths:((WebSystemTextBlock)child).SystemLineWidths;MinimumWidth=Math.Max(MinimumWidth,widths.Count==0?0:widths.Max());}return new(available.Width,height);}
        protected override Size ArrangeOverride(Size final)
        {double y=0;foreach(var child in Children){child.Arrange(new(0,y,final.Width,child.DesiredSize.Height));y+=child.DesiredSize.Height;}return final;}
    }
}
