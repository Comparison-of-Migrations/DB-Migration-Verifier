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
internal sealed class MetricsProbeApplication:Application
{
 private sealed record Case(string Id,bool English,double Viewport,double Width,string[] Values);
 public override void OnFrameworkInitializationCompleted()
 {
  var life=(IClassicDesktopStyleApplicationLifetime)ApplicationLifetime!;var args=life.Args!;using var doc=JsonDocument.Parse(File.ReadAllText(args[1]));var cases=doc.RootElement.GetProperty("cases").Deserialize<Case[]>(new JsonSerializerOptions{PropertyNameCaseInsensitive=true})!;var output=args[2];Directory.CreateDirectory(output);
  var n=new WebMetrics();var canvas=new Canvas{Width=1150,Height=900,Background=Brush.Parse("#313338"),UseLayoutRounding=false};canvas.Children.Add(n);Canvas.SetLeft(n,30);Canvas.SetTop(n,30);var w=new Window{Title="Replica Metrics verification",Width=1200,Height=950,Content=canvas,UseLayoutRounding=false};life.MainWindow=w;
  object Box(Control c){var p=c.TranslatePoint(default,n)??default;return new{x=p.X,y=p.Y,width=c.Bounds.Width,height=c.Bounds.Height};}
  void Apply(Case c){n.Width=c.Width;n.ViewportWidth=c.Viewport;n.SetPresentation(c.English,c.Values[0],c.Values[1],c.Values[2],c.Values[3]);w.UpdateLayout();}
  object Read(Case c)=>new{id=c.Id,root=Box(n),gap=n.Gap,marginBottom=n.Margin.Bottom,children=n.Items.Select(x=>new{root=Box(x),label=Box(x.Label),value=Box(x.Value),labelText=x.Label.Text,valueText=x.Value.Text}).ToArray(),peer=new{type=ControlAutomationPeer.CreatePeerForElement(n)?.GetAutomationControlType().ToString(),name=AutomationProperties.GetName(n),focusable=n.Focusable}};
  w.Opened+=(_,_)=>Dispatcher.UIThread.Post(async()=>{
   var rows=new List<object>();foreach(var c in cases){Apply(c);await Dispatcher.UIThread.InvokeAsync(()=>{},DispatcherPriority.Background);w.UpdateLayout();rows.Add(Read(c));if(c.Id is "en-1000-326-0" or "en-1280-326-2" or "ko-1280-904-0"){using var bmp=new RenderTargetBitmap(new PixelSize(2300,1800),new Vector(192,192));bmp.Render(canvas);bmp.Save(Path.Combine(output,"metrics-"+c.Id+".png"),new PngBitmapEncoderOptions());}}
   File.WriteAllText(Path.Combine(output,"metrics-native.json"),JsonSerializer.Serialize(new{dpr=w.RenderScaling,rows},new JsonSerializerOptions{WriteIndented=true}));
   var states=new List<object>();var children=n.Items.ToArray();void State(string label){w.UpdateLayout();states.Add(new{label,attached=n.IsAttachedToVisualTree(),sameItems=n.Items.SequenceEqual(children),focusable=n.Focusable,values=n.Items.Select(x=>x.Value.Text).ToArray(),effects=n.Items.Select(x=>new{opacity=x.Value.Opacity,y=(x.Value.RenderTransform as TranslateTransform)?.Y??0}).ToArray(),root=Box(n)});}
   Apply(cases.First(x=>x.Id=="ko-1280-904-0"));State("attached-no-auto");var play=n.PlayEntrance();State("normal-start");await play;State("normal-end");canvas.Children.Remove(n);State("detached");canvas.Children.Add(n);State("reattached-no-auto");await n.PlayEntrance(true);State("reduced");Apply(cases.First(x=>x.Id=="en-1000-326-2"));State("narrow-english-reuse");Apply(cases.First(x=>x.Id=="ko-1280-904-0"));State("wide-korean-reuse");
   File.WriteAllText(Path.Combine(output,"metrics-lifecycle.json"),JsonSerializer.Serialize(new{capturedAtUtc=DateTime.UtcNow,rows=states},new JsonSerializerOptions{WriteIndented=true}));if(args[0]!="--metrics-window")w.Close();else Apply(cases.First(x=>x.Id=="ko-1000-326-0"));Console.WriteLine($"Recorded {rows.Count} Metrics cases and {states.Count} lifecycle states");
  },DispatcherPriority.Background);base.OnFrameworkInitializationCompleted();
 }
}
