using System.Text.Json;
using Avalonia;
using Avalonia.Automation;
using Avalonia.Automation.Peers;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Verifier.Desktop;
internal sealed class MetricItemProbeApplication:Application
{
 private sealed record Case(string Id,bool English,double Width,string Kind,string Label,string Value);
 public override void OnFrameworkInitializationCompleted()
 {
  var life=(IClassicDesktopStyleApplicationLifetime)ApplicationLifetime!;var args=life.Args!;using var doc=JsonDocument.Parse(File.ReadAllText(args[1]));var cases=doc.RootElement.GetProperty("cases").Deserialize<Case[]>(new JsonSerializerOptions{PropertyNameCaseInsensitive=true})!;var output=args[2];Directory.CreateDirectory(output);
  var n=new WebMetricItem{Width=240};var canvas=new Canvas{Width=1000,Height=650,Background=Brush.Parse("#313338"),UseLayoutRounding=false};canvas.Children.Add(n);Canvas.SetLeft(n,30);Canvas.SetTop(n,30);var w=new Window{Title="Replica MetricItem verification",Width=1050,Height=750,Content=canvas,UseLayoutRounding=false};life.MainWindow=w;
  object Box(Control c){var p=c.TranslatePoint(default,n)??default;return new{x=p.X,y=p.Y,width=c.Bounds.Width,height=c.Bounds.Height};}
  double naturalWidth=0,naturalHeight=0;
  void Natural(Case c){n.Width=double.NaN;n.SetPresentation(c.Label,c.Value,c.Kind);w.UpdateLayout();n.Measure(Size.Infinity);naturalWidth=n.DesiredSize.Width;naturalHeight=n.DesiredSize.Height;}
  void Apply(Case c){Natural(c);n.Width=c.Width;w.UpdateLayout();}
  object Read(Case c)=>new{id=c.Id,root=Box(n),label=Box(n.Label),value=Box(n.Value),naturalWidth,naturalHeight,labelText=n.Label.Text,valueText=n.Value.Text,labelColor=n.Label.Foreground?.ToString(),valueColor=n.Value.Foreground?.ToString(),valueSize=n.Value.FontSize,valueLineHeight=n.Value.LineHeight,valueWeight=(int)n.Value.FontWeight,valueSpacing=n.Value.LetterSpacing,tabular=n.Value.TabularNumbers,monospaced=n.Value.FontFamily.Name==WebTypography.MonospaceFontFamily.Name,peer=new{type=ControlAutomationPeer.CreatePeerForElement(n)?.GetAutomationControlType().ToString(),name=AutomationProperties.GetName(n),focusable=n.Focusable}};
  w.Opened+=(_,_)=>Dispatcher.UIThread.Post(async()=>{
   if(args.Length>3&&args[3]=="--peer-only"){Apply(cases.First(x=>x.Id=="ko-326-different-0"));File.WriteAllText(Path.Combine(output,"metricitem-peer-target.json"),JsonSerializer.Serialize(Read(cases.First(x=>x.Id=="ko-326-different-0"))));if(args[0]!="--metricitem-window")w.Close();return;}
   if(args.Length>3&&args[3]=="--natural-only"){var naturalRows=new List<object>();foreach(var c in cases){Natural(c);naturalRows.Add(new{id=c.Id,naturalWidth,naturalHeight});}File.WriteAllText(Path.Combine(output,"metricitem-natural.json"),JsonSerializer.Serialize(new{rows=naturalRows},new JsonSerializerOptions{WriteIndented=true}));if(args[0]!="--metricitem-window")w.Close();Console.WriteLine($"Recorded {naturalRows.Count} natural metric sizes");return;}
   var rows=new List<object>();foreach(var c in cases){Apply(c);await Dispatcher.UIThread.InvokeAsync(()=>{},DispatcherPriority.Background);w.UpdateLayout();rows.Add(Read(c));if(c.Id is "en-326-unknown-2" or "ko-904-equal-1" or "en-656-id-2"){using var bmp=new RenderTargetBitmap(new PixelSize(2000,1300),new Vector(192,192));bmp.Render(canvas);bmp.Save(Path.Combine(output,"metricitem-"+c.Id+".png"),new PngBitmapEncoderOptions());}}
   File.WriteAllText(Path.Combine(output,"metricitem-native.json"),JsonSerializer.Serialize(new{dpr=w.RenderScaling,rows},new JsonSerializerOptions{WriteIndented=true}));
   var states=new List<object>();var oldLabel=n.Label;var oldValue=n.Value;void State(string label){w.UpdateLayout();states.Add(new{label,attached=n.IsAttachedToVisualTree(),opacity=n.Value.Opacity,y=(n.Value.RenderTransform as TranslateTransform)?.Y??0,sameLabel=ReferenceEquals(oldLabel,n.Label),sameValue=ReferenceEquals(oldValue,n.Value),kind=n.Kind,value=n.Value.Text,width=n.Bounds.Width,height=n.Bounds.Height,focusable=n.Focusable});}
   Apply(cases.First(x=>x.Id=="ko-904-different-1"));State("attached-no-auto");var play=n.PlayEntrance();State("normal-start");await play;State("normal-end");canvas.Children.Remove(n);State("detached");canvas.Children.Add(n);State("reattached-no-auto");await n.PlayEntrance(true);State("reduced");Apply(cases.First(x=>x.Id=="en-656-id-2"));await n.PlayEntrance();State("id-no-animation");Apply(cases.First(x=>x.Id=="en-326-unknown-2"));State("narrow-english-reuse");Apply(cases.First(x=>x.Id=="ko-904-equal-1"));State("wide-korean-reuse");
   File.WriteAllText(Path.Combine(output,"metricitem-lifecycle.json"),JsonSerializer.Serialize(new{capturedAtUtc=DateTime.UtcNow,rows=states},new JsonSerializerOptions{WriteIndented=true}));if(args[0]!="--metricitem-window")w.Close();else Apply(cases.First(x=>x.Id=="ko-326-different-0"));Console.WriteLine($"Recorded {rows.Count} MetricItem cases and {states.Count} lifecycle states");
  },DispatcherPriority.Background);base.OnFrameworkInitializationCompleted();
 }
}
