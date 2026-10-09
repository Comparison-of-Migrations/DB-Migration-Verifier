using Avalonia;
using Avalonia.Automation;
using Avalonia.Automation.Peers;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.VisualTree;
namespace Verifier.Desktop;

/// <summary>Literal passive article.finding. Parent owns generation, order, filtering and list gap.</summary>
public sealed class WebFindingArticle:Decorator
{
 private readonly Contents _content=new();private WebMotionPlayback? _entry;private bool _reduced;
 public Control Top=>_content.Top;public WebSystemTextBlock TitleText=>_content.Top.Title;public WebSystemTextBlock IdentifierText=>_content.Top.Identifier;
 public Control Location=>_content.Location;public WebSystemTextBlock LocationText=>_content.Location.Text;public WebSystemTextBlock ReasonText=>_content.Reason;
 public bool ReducedMotion{get=>_reduced;set{if(_reduced==value)return;_reduced=value;if(this.IsAttachedToVisualTree())StartEntry();}}
 public WebFindingArticle(){UseLayoutRounding=false;Focusable=false;var surface=WebSurface.Create(Brush.Parse("#2b2d31"),Brush.Parse("#41434a"),new(1),new(8));surface.Padding=new(16);surface.Child=_content;Child=surface;AutomationProperties.SetAutomationId(this,"FindingArticle");if(OperatingSystem.IsWindows())foreach(var text in new[]{TitleText,IdentifierText,LocationText,ReasonText})AutomationProperties.SetAccessibilityView(text,AccessibilityView.Raw);AttachedToVisualTree+=(_,_)=>StartEntry();DetachedFromVisualTree+=(_,_)=>{_entry?.Dispose();_entry=null;};}
 private void StartEntry(){_entry?.Dispose();_entry=WebMotion.Play(this,WebMotionKind.Page,ReducedMotion);}
 public void SetPresentation(string title,string identifier,string table,string column,string reason){TitleText.Text=title;IdentifierText.Text=identifier;LocationText.Text=table+" · "+column;ReasonText.Text=reason;InvalidateMeasure();}
 protected override AutomationPeer OnCreateAutomationPeer()=>new ArticlePeer(this);
 private sealed class ArticlePeer(WebFindingArticle owner):ControlAutomationPeer(owner){protected override AutomationControlType GetAutomationControlTypeCore()=>AutomationControlType.Group;protected override string GetClassNameCore()=>"FindingArticle";}
 private sealed class Contents:Panel
 {
  public TopRow Top{get;}=new();public LocationLine Location{get;}=new();public WebSystemTextBlock Reason{get;}=WebTypography.CreateCandidate("",13,20.8,Brush.Parse("#b5bac1"));
  public Contents(){UseLayoutRounding=false;Children.Add(Top);Children.Add(Location);Children.Add(Reason);}
  protected override Size MeasureOverride(Size a){Top.Measure(a);Location.Measure(a);Reason.Measure(a);return new(double.IsFinite(a.Width)?a.Width:Math.Max(Top.DesiredSize.Width,Math.Max(Location.DesiredSize.Width,Reason.DesiredSize.Width)),Top.DesiredSize.Height+7+Location.DesiredSize.Height+Reason.DesiredSize.Height);}
  protected override Size ArrangeOverride(Size s){Top.Arrange(new(0,0,s.Width,Top.DesiredSize.Height));double y=Top.DesiredSize.Height+7;Location.Arrange(new(0,y,s.Width,Location.DesiredSize.Height));y+=Location.DesiredSize.Height;Reason.Arrange(new(0,y,s.Width,Reason.DesiredSize.Height));return s;}
 }
 private sealed class TopRow:Panel
 {
  public WebSystemTextBlock Title{get;}=WebTypography.CreateCandidate("",14,22.4,Brush.Parse("#dbdee1"),700);
  public WebSystemTextBlock Identifier{get;}=WebTypography.CreateCandidate("",12,19.2,Brush.Parse("#dbdee1"),monospaced:true);private double _tw,_iw;
  public TopRow(){UseLayoutRounding=false;Children.Add(Title);Children.Add(Identifier);}
  protected override Size MeasureOverride(Size a){var units=(TopLevel.GetTopLevel(this)?.RenderScaling??2)*64;Identifier.LineHeight=Math.Floor(19.2*units)/units;Title.Measure(Size.Infinity);Identifier.Measure(Size.Infinity);double Quant(double x)=>Math.Ceiling(x*units)/units;_tw=Quant(Title.DesiredSize.Width);_iw=Quant(Identifier.DesiredSize.Width);var total=_tw+_iw;var width=double.IsFinite(a.Width)?a.Width:total+12;var usable=Math.Max(0,width-12);if(total>usable){_tw=Math.Floor(usable*_tw/total*units)/units;_iw=usable-_tw;}Title.Measure(new(_tw,double.PositiveInfinity));Identifier.Measure(new(_iw,double.PositiveInfinity));return new(width,Math.Max(Title.DesiredSize.Height,Identifier.DesiredSize.Height));}
  protected override Size ArrangeOverride(Size s){var units=(TopLevel.GetTopLevel(this)?.RenderScaling??2)*64;double Center(double h)=>Math.Floor((s.Height-h)*units/2)/units;Title.Arrange(new(0,Center(Title.DesiredSize.Height),_tw,Title.DesiredSize.Height));Identifier.Arrange(new(s.Width-_iw,Center(Identifier.DesiredSize.Height),_iw,Identifier.DesiredSize.Height));return s;}
 }
 private sealed class LocationLine:Panel
 {
  public WebSystemTextBlock Text{get;}=WebTypography.CreateCandidate("",12,19.2,Brush.Parse("#b5bac1"),monospaced:true);
  private readonly WebSystemTextBlock _strut=WebTypography.CreateCandidate("X",13,20.8,Brush.Parse("#b5bac1"));private double _offset;
  public LocationLine(){UseLayoutRounding=false;Children.Add(Text);}
  protected override Size MeasureOverride(Size a){var units=(TopLevel.GetTopLevel(this)?.RenderScaling??2)*64;Text.LineHeight=Math.Floor(19.2*units)/units;_strut.Measure(Size.Infinity);Text.Measure(a);_offset=_strut.SystemBaseline-Text.SystemBaseline;return new(double.IsFinite(a.Width)?a.Width:Text.DesiredSize.Width,Math.Max(_strut.DesiredSize.Height,Text.DesiredSize.Height+Math.Max(0,_offset)));}
  protected override Size ArrangeOverride(Size s){Text.Arrange(new(0,_offset,s.Width,Text.DesiredSize.Height));return s;}
 }
}
