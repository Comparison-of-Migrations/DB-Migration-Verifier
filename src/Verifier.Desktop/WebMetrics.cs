using Avalonia;
using Avalonia.Automation;
using Avalonia.Controls;
namespace Verifier.Desktop;

/// <summary>Original Results summary flex rows. Results supplies counts, ID and viewport.</summary>
public sealed class WebMetrics:Panel
{
 private readonly WebMetricItem[] _items=Enumerable.Range(0,4).Select(_=>new WebMetricItem()).ToArray();
 private readonly WebSurfaceBox _border=new(){Background=Avalonia.Media.Brush.Parse("#41434a"),IsHitTestVisible=false};
 private double _viewportWidth=1280;
 public IReadOnlyList<WebMetricItem> Items=>_items;
 public double ViewportWidth{get=>_viewportWidth;set{if(value==_viewportWidth)return;_viewportWidth=value;InvalidateMeasure();}}
 public double Gap=>ViewportWidth<=1180?28:40;
 public WebMetrics(){UseLayoutRounding=false;Focusable=false;Margin=new Thickness(0,0,0,24);AutomationProperties.SetAccessibilityView(this,AccessibilityView.Raw);foreach(var item in _items)Children.Add(item);Children.Add(_border);}
 public void SetPresentation(bool english,string different,string unknown,string equal,string runId)
 {
  var labels=english?new[]{"Different checks","Unverified / unobserved","Equal checks","Sample run ID"}:new[]{"불일치 항목","확인 불가·미검증 항목","일치 항목","예시 검사 ID"};
  var values=new[]{different,unknown,equal,runId};var kinds=new[]{"different","unknown","equal","id"};
  for(var i=0;i<4;i++)_items[i].SetPresentation(labels[i],values[i],kinds[i]);InvalidateMeasure();
 }
 public Task PlayEntrance(bool reducedMotion=false,CancellationToken cancellation=default)=>Task.WhenAll(_items.Take(3).Select(x=>x.PlayEntrance(reducedMotion,cancellation)));
 private List<List<WebMetricItem>> Lines(double width)
 {
  var lines=new List<List<WebMetricItem>>();var line=new List<WebMetricItem>();double used=0;
  foreach(var item in _items){var next=item.DesiredSize.Width+(line.Count>0?Gap:0);if(line.Count>0&&used+next>width){lines.Add(line);line=new();used=0;next=item.DesiredSize.Width;}line.Add(item);used+=next;}
  if(line.Count>0)lines.Add(line);return lines;
 }
 protected override Size MeasureOverride(Size available)
 {
  foreach(var item in _items)item.Measure(Size.Infinity);
  _border.Measure(new Size(available.Width,1));
  var width=double.IsFinite(available.Width)?available.Width:_items.Sum(x=>x.DesiredSize.Width)+Gap*3;
  var lines=Lines(width);return new(width,8+lines.Sum(l=>l.Max(x=>x.DesiredSize.Height))+Gap*(lines.Count-1)+24+1);
 }
 protected override Size ArrangeOverride(Size size)
 {
  double y=8;foreach(var line in Lines(size.Width)){
   var height=line.Max(x=>x.DesiredSize.Height);var used=line.Sum(x=>x.DesiredSize.Width)+Gap*(line.Count-1);double x=0;
   foreach(var item in line){if(ReferenceEquals(item,_items[3]))x+=Math.Max(0,size.Width-used);item.Arrange(new Rect(x,y+(height-item.DesiredSize.Height)/2,item.DesiredSize.Width,item.DesiredSize.Height));x+=item.DesiredSize.Width+Gap;}y+=height+Gap;
  }
  _border.Arrange(new Rect(0,size.Height-1,size.Width,1));return size;
 }
}
