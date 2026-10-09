using System.Reflection;
using System.Text.Json;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using Verifier.Desktop;

internal sealed class ResultCountProbeApplication:Application
{
 public override void Initialize()=>Styles.Add(new Avalonia.Themes.Fluent.FluentTheme());
 public override void OnFrameworkInitializationCompleted()
 {
  var args=((IClassicDesktopStyleApplicationLifetime)ApplicationLifetime!).Args!;
  var output=args[2];Directory.CreateDirectory(output);
  var cases=JsonSerializer.Deserialize<CountCase[]>(File.ReadAllText(args[1]),new JsonSerializerOptions{PropertyNameCaseInsensitive=true})!;
  var n=new WebResultCount();var canvas=new Canvas{Width=500,Height=100,Background=Brush.Parse("#313338"),UseLayoutRounding=false};canvas.Children.Add(n);Canvas.SetLeft(n,30);Canvas.SetTop(n,30);
  var w=new Window{Title="Replica ResultCount verification",Width=540,Height=180,Content=canvas,UseLayoutRounding=false};((IClassicDesktopStyleApplicationLifetime)ApplicationLifetime!).MainWindow=w;
  object Matrix(Control c){var m=c.RenderTransform?.Value??Avalonia.Matrix.Identity;return new[]{m.M11,m.M12,m.M21,m.M22,m.M31,m.M32};}
  object Read(CountCase c){object Box(Control x){var p=x.TranslatePoint(default,n)??default;return new{x=p.X,y=p.Y,width=x.Bounds.Width,height=x.Bounds.Height};}return new{id=c.Id,width=n.Bounds.Width,height=n.Bounds.Height,number=Box(n.Number),track=Box(n.Track),bar=Box(n.Bar),glyphWidth=n.Number.SystemLineWidths.Single(),text=n.Number.Text,color=(n.Number.Foreground as ISolidColorBrush)!.Color.ToString(),fontSize=n.Number.FontSize,fontWeight=(int)n.Number.FontWeight,lineHeight=n.Number.LineHeight,letterSpacing=n.Number.LetterSpacing,trackColor="#41434a",barColor=(n.Bar.Background as ISolidColorBrush)?.Color.ToString(),radius=n.Bar.CornerRadius.TopLeft,gradient=n.Bar.Background is LinearGradientBrush g?new{start=g.StartPoint.Point,end=g.EndPoint.Point,spread=g.SpreadMethod.ToString(),stops=g.GradientStops.Select(x=>new{x.Offset,color=x.Color.ToString()})}:null};}
  void Apply(CountCase c){n.Width=c.Width;n.SetValue(c.Count,c.Maximum,c.Kind);w.UpdateLayout();}
  w.Opened+=(_,_)=>Dispatcher.UIThread.Post(async()=>
  {
   var rows=new List<object>();foreach(var c in cases){Apply(c);rows.Add(Read(c));if(c.Id is "8" or "9" or "10" or "11" or "32"){using var image=new RenderTargetBitmap(new PixelSize(1000,200),new Vector(192,192));image.Render(canvas);image.Save(Path.Combine(output,"resultcount-"+c.Id+".png"),new PngBitmapEncoderOptions());}}
   Apply(cases.Single(c=>c.Id=="8"));
   var constructor=typeof(WebMotionPlayback).GetConstructors(BindingFlags.Instance|BindingFlags.NonPublic).Single();var advance=typeof(WebMotionPlayback).GetMethod("Advance",BindingFlags.Instance|BindingFlags.NonPublic)!;var timing=new List<object>();
   foreach(var reduced in new[]{false,true})foreach(var kind in new[]{"number","bar"})
   {
    Control target=kind=="number"?n.Number:n.Bar;using var playback=(WebMotionPlayback)constructor.Invoke(new object?[]{target,WebMotion.Create(kind=="number"?WebMotionKind.ResultNumber:WebMotionKind.ResultBar,reduced),false,CancellationToken.None});
    foreach(var fraction in new[]{0d,.125,.25,.375,.5,.625,.75,.875,.999,1,1.001,1.25}){advance.Invoke(playback,new object[]{TimeSpan.FromMilliseconds(fraction*(kind=="number"?220:320))});timing.Add(new{reduced,kind,fraction,opacity=target.Opacity,matrix=Matrix(target)});}
   }
   var lifecycle=new List<object>();object Live(string phase,Task? task=null)=>new{phase,text=n.Number.Text,count=n.Count,opacity=n.Number.Opacity,number=Matrix(n.Number),bar=Matrix(n.Bar),status=task?.Status.ToString()};
   lifecycle.Add(Live("mount"));var normal=n.PlayEntrance();lifecycle.Add(Live("started",normal));await Task.Delay(60);lifecycle.Add(Live("running",normal));await normal;lifecycle.Add(Live("completed",normal));
   var reducedTask=n.PlayEntrance(true);await reducedTask;lifecycle.Add(Live("reduced",reducedTask));
   var replaced=n.PlayEntrance();n.SetValue(0,20,"equal");w.UpdateLayout();lifecycle.Add(Live("replaced",replaced));try{await replaced;}catch(OperationCanceledException){}lifecycle.Add(Live("replaced-settled",replaced));
   n.SetValue(11,20,"unknown");w.UpdateLayout();var detached=n.PlayEntrance();canvas.Children.Remove(n);try{await detached;}catch(OperationCanceledException){}lifecycle.Add(Live("detached",detached));canvas.Children.Add(n);w.UpdateLayout();lifecycle.Add(Live("reattached"));
   var replay=n.PlayEntrance();await replay;lifecycle.Add(Live("replayed",replay));
   File.WriteAllText(Path.Combine(output,"resultcount-native.json"),JsonSerializer.Serialize(new{scale=w.RenderScaling,rows,timing,lifecycle},new JsonSerializerOptions{WriteIndented=true}));w.Close();Console.WriteLine($"ResultCount {rows.Count} geometry/update, {timing.Count} timing, {lifecycle.Count} attached lifecycle states recorded.");
  },DispatcherPriority.Background);
  base.OnFrameworkInitializationCompleted();
 }
 internal sealed record CountCase(string Id,double Width,string Kind,long Count,double Maximum);
}
