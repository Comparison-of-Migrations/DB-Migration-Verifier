using System.Text.Json;
using Avalonia;
using Avalonia.Automation.Peers;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Verifier.Desktop;
internal sealed class FindingArticleProbeApplication:Application
{
 private sealed record Case(string Id,string FindingId,bool English,double Width,string Title,string Table,string Column,string Reason);
 public override void OnFrameworkInitializationCompleted()
 {
  var life=(IClassicDesktopStyleApplicationLifetime)ApplicationLifetime!;var args=life.Args!;using var doc=JsonDocument.Parse(File.ReadAllText(args[1]));var cases=doc.RootElement.GetProperty("cases").Deserialize<Case[]>(new JsonSerializerOptions{PropertyNameCaseInsensitive=true})!;var output=args[2];Directory.CreateDirectory(output);
  var n=new WebFindingArticle{ReducedMotion=true,Width=904};var canvas=new Canvas{Width=1000,Height=650,Background=Brush.Parse("#2b2d31"),UseLayoutRounding=false};canvas.Children.Add(n);Canvas.SetLeft(n,30);Canvas.SetTop(n,30);var w=new Window{Title="Replica FindingArticle verification",Width=1050,Height=750,Content=canvas,UseLayoutRounding=false};life.MainWindow=w;
  object Box(Control c){var p=c.TranslatePoint(default,n)??default;return new{x=p.X,y=p.Y,width=c.Bounds.Width,height=c.Bounds.Height};}
  // Fixture identity and finding ID are distinct; the literal finding ID is carried as findingId.
  void Apply(Case c){n.Width=c.Width;n.SetPresentation(c.Title,c.FindingId,c.Table,c.Column,c.Reason);w.UpdateLayout();}
  object Read(Case c)=>new{id=c.Id,root=Box(n),top=Box(n.Top),title=Box(n.TitleText),identifier=Box(n.IdentifierText),location=Box(n.Location),reason=Box(n.ReasonText),titleText=n.TitleText.Text,identifierText=n.IdentifierText.Text,locationText=n.LocationText.Text,reasonText=n.ReasonText.Text,locationBaseline=(n.LocationText.TranslatePoint(default,n.Location)??default).Y+n.LocationText.SystemBaseline,peer=new{type=ControlAutomationPeer.CreatePeerForElement(n)?.GetAutomationControlType().ToString(),focusable=n.Focusable,name=Avalonia.Automation.AutomationProperties.GetName(n)}};
  w.Opened+=(_,_)=>Dispatcher.UIThread.Post(async()=>{
   if(args.Length>3&&args[3]=="--peer-only"){Apply(cases.First(x=>x.Id=="ko-904-data"));File.WriteAllText(Path.Combine(output,"findingarticle-peer-target.json"),JsonSerializer.Serialize(Read(cases.First(x=>x.Id=="ko-904-data"))));return;}
   var rows=new List<object>();foreach(var c in cases){Apply(c);await Dispatcher.UIThread.InvokeAsync(()=>{},DispatcherPriority.Background);w.UpdateLayout();rows.Add(Read(c));if(!args.Contains("--precision-only")&&(c.Id is "en-326-unknown" or "ko-904-data")){using var bmp=new RenderTargetBitmap(new PixelSize(2000,1300),new Vector(192,192));bmp.Render(canvas);bmp.Save(Path.Combine(output,"findingarticle-"+c.Id+".png"),new PngBitmapEncoderOptions());}}
   File.WriteAllText(Path.Combine(output,"findingarticle-native.json"),JsonSerializer.Serialize(new{dpr=w.RenderScaling,rows},new JsonSerializerOptions{WriteIndented=true}));
   if(args.Contains("--precision-only")){if(args[0]!="--findingarticle-window")w.Close();Console.WriteLine($"Recorded {rows.Count} affected FindingArticle line cases only");return;}
   var states=new List<object>();var oldTitle=n.TitleText;var oldLocation=n.LocationText;void State(string label){w.UpdateLayout();states.Add(new{label,attached=n.IsAttachedToVisualTree(),opacity=n.Opacity,y=(n.RenderTransform as TranslateTransform)?.Y??0,reduced=n.ReducedMotion,sameTitle=ReferenceEquals(oldTitle,n.TitleText),sameLocation=ReferenceEquals(oldLocation,n.LocationText),title=n.TitleText.Text,identifier=n.IdentifierText.Text,width=n.Bounds.Width,height=n.Bounds.Height,peer=ControlAutomationPeer.CreatePeerForElement(n)?.GetAutomationControlType().ToString(),focusable=n.Focusable});}
   n.ReducedMotion=false;State("normal-start");await Task.Delay(260);State("normal-end");canvas.Children.Remove(n);State("detached");canvas.Children.Add(n);State("reattached");await Task.Delay(260);State("reattached-end");n.ReducedMotion=true;State("reduced");Apply(cases.First(x=>x.Id=="en-326-unknown"));State("narrow-english-reuse");Apply(cases.First(x=>x.Id=="ko-904-data"));State("wide-korean-reuse");
   File.WriteAllText(Path.Combine(output,"findingarticle-lifecycle.json"),JsonSerializer.Serialize(new{capturedAtUtc=DateTime.UtcNow,rows=states},new JsonSerializerOptions{WriteIndented=true}));if(args[0]!="--findingarticle-window")w.Close();Console.WriteLine($"Recorded {rows.Count} FindingArticle cases and {states.Count} lifecycle states");
  },DispatcherPriority.Background);base.OnFrameworkInitializationCompleted();
 }
}
