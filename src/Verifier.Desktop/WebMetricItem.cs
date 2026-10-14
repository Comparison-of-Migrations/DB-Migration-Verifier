using Avalonia;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.VisualTree;
namespace Verifier.Desktop;

/// <summary>Literal Results metric child. Metrics owns flex sizing and Results requests entrance.</summary>
public sealed class WebMetricItem:Panel
{
 public WebSystemTextBlock Label{get;}=WebTypography.CreateCandidate("",11,17.6,Brush.Parse("#b5bac1"));
 public WebSystemTextBlock Value{get;}=WebTypography.CreateCandidate("",44,55,Brush.Parse("#ff959d"),650,-1.76,tabularNumbers:true);
 private WebMotionPlayback? _motion;private bool _identifier;
 public string Kind{get;private set;}="different";
 public WebMetricItem(){UseLayoutRounding=false;Focusable=false;Children.Add(Label);Children.Add(Value);AutomationProperties.SetAccessibilityView(this,AccessibilityView.Raw);}
 public void SetPresentation(string label,string value,string kind)
 {
  if(kind is not("different" or "unknown" or "equal" or "id"))throw new ArgumentException("Original metric role is required.",nameof(kind));
  StopEntrance();Kind=kind;_identifier=kind=="id";Label.Text=label;Value.Text=value;
  Value.FontSize=_identifier?11:44;Value.LineHeight=_identifier?17.6:55;Value.FontWeight=(FontWeight)(_identifier?400:650);Value.LetterSpacing=_identifier?0:-1.76;
  Value.FontFamily=_identifier?WebTypography.MonospaceFontFamily:WebTypography.SystemFontFamily;Value.TabularNumbers=true;Value.Foreground=Brush.Parse(_identifier?"#b5bac1":kind=="unknown"?"#f0c47c":kind=="equal"?"#6bdda3":"#ff959d");InvalidateMeasure();
 }
 public Task PlayEntrance(bool reducedMotion=false,CancellationToken cancellation=default){StopEntrance();if(_identifier)return Task.CompletedTask;_motion=WebMotion.Play(Value,WebMotionKind.ResultNumber,reducedMotion,cancellation);return _motion.Completion;}
 private void StopEntrance(){_motion?.Dispose();_motion=null;}
 protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e){StopEntrance();base.OnDetachedFromVisualTree(e);}
 protected override Size MeasureOverride(Size a){var units=(TopLevel.GetTopLevel(this)?.RenderScaling??2)*64;Label.LineHeight=Math.Floor(17.6*units)/units;Value.LineHeight=_identifier?Math.Floor(17.6*units)/units:55;Label.Measure(a);Value.Measure(a);return new(double.IsFinite(a.Width)?a.Width:Math.Ceiling(Math.Max(Label.DesiredSize.Width,Value.DesiredSize.Width)*units)/units,Label.DesiredSize.Height+(_identifier?0:4)+Value.DesiredSize.Height);}
 protected override Size ArrangeOverride(Size s){Label.Arrange(new(0,0,s.Width,Label.DesiredSize.Height));Value.Arrange(new(0,Label.DesiredSize.Height+(_identifier?0:4),s.Width,Value.DesiredSize.Height));return s;}
}
