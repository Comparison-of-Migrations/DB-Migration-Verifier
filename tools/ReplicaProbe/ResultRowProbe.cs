using System.Reflection;
using System.Text.Json;
using Avalonia;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Layout;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Verifier.Desktop;

internal sealed class ResultRowProbeApplication:Application
{
 public override void Initialize()=>Styles.Add(new Avalonia.Themes.Fluent.FluentTheme());
 public override void OnFrameworkInitializationCompleted()
 {
  var args=((IClassicDesktopStyleApplicationLifetime)ApplicationLifetime!).Args!;var output=args[2];Directory.CreateDirectory(output);
  var cases=JsonSerializer.Deserialize<RowCase[]>(File.ReadAllText(args[1]),new JsonSerializerOptions{PropertyNameCaseInsensitive=true})!;
  var n=new WebResultSummaryRow{Width=858,ReducedMotion=true};var canvas=new Canvas{Width=1160,Height=150,Background=Brush.Parse("#2b2d31"),UseLayoutRounding=false};canvas.Children.Add(n);Canvas.SetLeft(n,30);Canvas.SetTop(n,30);
  var interactive=args[0]=="--resultrow-window"||args[0]=="--resultrow-lifecycle";
  var w=new Window{Title="Replica ResultSummaryRow verification",Width=1200,Height=interactive?330:230,Content=canvas,UseLayoutRounding=false};((IClassicDesktopStyleApplicationLifetime)ApplicationLifetime!).MainWindow=w;
  var requests=new List<string>();var lifecycle=new List<object>();bool changed=false,english=false;
  void Live()
  {
   w.UpdateLayout();var m=n.RenderTransform?.Value??Matrix.Identity;var ch=n.Chevron.RenderTransform?.Value??Matrix.Identity;
   var json=JsonSerializer.Serialize(new{requests,key=n.CategoryKey,enabled=n.IsEffectivelyEnabled,hover=n.IsPointerOver,pressed=n.IsPressed,focus=n.IsFocused,focusVisible=n.FocusVisible,reduced=n.ReducedMotion,english,changed,opacity=n.Opacity,scale=m.M11,chevron=ch.M31,background=(n.RowBackground as ISolidColorBrush)!.Color.ToString(),name=AutomationProperties.GetName(n),counts=n.Counts.Select(x=>x.Count),description=n.Description.Text},new JsonSerializerOptions{WriteIndented=true});
   var path=Path.Combine(output,"resultrow-input.json");File.WriteAllText(path+".tmp",json);File.Move(path+".tmp",path,true);
  }
  void Sample(){Dispatcher.UIThread.Post(Live,DispatcherPriority.Background);var timer=new DispatcherTimer{Interval=TimeSpan.FromMilliseconds(25)};var ticks=0;timer.Tick+=(_,_)=>{Live();if(++ticks==16)timer.Stop();};timer.Start();}
  void UpdateResult()=>n.SetResult("data",english?"Data":"데이터",changed?(english?"Interrupted · Gaps retained · Total 12":"중단 · 미검증 포함 · 전체 12"):(english?"Completed · Total 12":"실행 완료 · 전체 12"),"db",changed?7:1,changed?5:0,changed?0:11,20,english);
  n.Click+=(_,_)=>{requests.Add(n.CategoryKey);Sample();};n.PointerEntered+=(_,_)=>Sample();n.PointerExited+=(_,_)=>Sample();
  n.GotFocus+=(_,_)=>Sample();n.LostFocus+=(_,_)=>Sample();
  w.AddHandler(InputElement.PointerPressedEvent,(_,_)=>Sample(),RoutingStrategies.Tunnel,true);w.AddHandler(InputElement.PointerReleasedEvent,(_,_)=>Sample(),RoutingStrategies.Tunnel,true);
  w.AddHandler(InputElement.KeyDownEvent,(_,e)=>{if(e.Key==Key.M){n.ReducedMotion=!n.ReducedMotion;e.Handled=true;}if(e.Key==Key.D){n.IsEnabled=!n.IsEnabled;e.Handled=true;}if(e.Key==Key.U){changed=!changed;UpdateResult();e.Handled=true;}if(e.Key==Key.E){english=!english;UpdateResult();e.Handled=true;}Sample();},RoutingStrategies.Tunnel,true);w.AddHandler(InputElement.KeyUpEvent,(_,_)=>Sample(),RoutingStrategies.Tunnel,true);
  void Apply(RowCase c){n.Width=c.Width;n.Compact=c.Compact;n.IsEnabled=!c.Disabled;n.Last=c.Last;n.SetResult(c.Key,c.Name,c.Description,c.Icon,c.Different,c.Unknown,c.Equal,c.Maximum,c.Lang=="en");w.UpdateLayout();
   foreach(var(field,target)in new(string,object)[]{("_background",Color.Parse(c.Disabled?"#3b3d44":c.Hover?"#353740":"#00000000")),("_chevron",c.Hover?2d:0)}){var track=typeof(WebResultSummaryRow).GetField(field,BindingFlags.Instance|BindingFlags.NonPublic)!.GetValue(n)!;track.GetType().GetMethod("Snap")!.Invoke(track,[target]);}
   typeof(WebResultSummaryRow).GetMethod("Paint",BindingFlags.Instance|BindingFlags.NonPublic)!.Invoke(n,null);
   var focus=typeof(WebResultSummaryRow).GetField("_focus",BindingFlags.Instance|BindingFlags.NonPublic)!.GetValue(n)!;focus.GetType().GetProperty("Visible")!.SetValue(focus,c.Focus);
  }
  object Read(RowCase c){object Box(Control x){var p=x.TranslatePoint(default,n)??default;return new{x=p.X,y=p.Y,width=x.Bounds.Width,height=x.Bounds.Height};}var iconBox=n.Category.GetVisualChildren().OfType<WebSurfaceBox>().Single();var strut=n.Chevron.GetVisualChildren().OfType<WebSystemTextBlock>().Single();var xHeight=(double)n.Chevron.GetType().GetMethod("XHeight",BindingFlags.Static|BindingFlags.NonPublic)!.Invoke(null,null)!;return new{fontProbe=new{baseline=strut.SystemBaseline,xHeight},id=c.Id,width=n.Bounds.Width,height=n.Bounds.Height,children=new[]{Box(n.Category),Box(n.Counts[0]),Box(n.Counts[1]),Box(n.Counts[2]),Box(n.Chevron)},category=Box(n.Category),icon=Box(iconBox),iconSvg=Box(n.CategoryIcon),title=Box(n.Title),description=Box(n.Description),chevronSvg=Box(n.ChevronIcon),name=AutomationProperties.GetName(n),style=new{background=(n.RowBackground as ISolidColorBrush)!.Color.ToString(),borderBottom=n.Last?0:1,focus=n.FocusVisible}};}
  w.Opened+=(_,_)=>Dispatcher.UIThread.Post(()=>{
   if(interactive){canvas.Height=260;Apply(cases.Single(c=>c.Lang=="ko"&&c.Width==858&&c.Key=="data"&&!c.Hover&&!c.Focus&&!c.Compact&&!c.Disabled));n.ReducedMotion=false;AutomationProperties.SetAutomationId(n,"ResultRowTarget");var peer=new WebResultSummaryRow{Width=858,Last=true};peer.SetResult("objects","관련 객체","실행 완료 · 전체 12","layers",0,0,12,20);AutomationProperties.SetAutomationId(peer,"ResultRowPeer");peer.Click+=(_,_)=>{requests.Add(peer.CategoryKey);Sample();};canvas.Children.Add(peer);Canvas.SetLeft(peer,30);Canvas.SetTop(peer,142);Sample();if(args[0]=="--resultrow-lifecycle"){
    async Task Check(){
     void Record(string phase){w.UpdateLayout();var m=n.RenderTransform?.Value??Matrix.Identity;var press=(WebMotionPlayback?)typeof(WebResultSummaryRow).GetField("_press",BindingFlags.NonPublic|BindingFlags.Instance)!.GetValue(n);lifecycle.Add(new{phase,attached=n.IsAttachedToVisualTree(),requests=requests.ToArray(),opacity=n.Opacity,scale=m.M11,playing=press is not null&&!press.Completion.IsCompleted,reduced=n.ReducedMotion});}
     var click=typeof(WebResultSummaryRow).GetMethod("OnClick",BindingFlags.NonPublic|BindingFlags.Instance)!;
     // Let the newly opened fixture finish its first font/raster paint before measuring input playback.
     await Task.Delay(500);Record("attached");click.Invoke(n,null);await Task.Delay(45);Record("click-playing");await Task.Delay(220);Record("click-completed");
     click.Invoke(n,null);await Task.Delay(45);Record("second-playing");canvas.Children.Remove(n);Record("detached");await Task.Delay(200);Record("detached-settled");
     canvas.Children.Add(n);await Task.Delay(30);Record("reattached");n.ReducedMotion=true;click.Invoke(n,null);Record("reduced-click");await Task.Delay(200);Record("reduced-settled");
     File.WriteAllText(Path.Combine(output,"resultrow-lifecycle.json"),JsonSerializer.Serialize(new{phases=lifecycle},new JsonSerializerOptions{WriteIndented=true}));w.Close();
    }_=Check();}return;}
   var rows=new List<object>();foreach(var c in cases){Apply(c);rows.Add(Read(c));if(c.Id is "0" or "4" or "8" or "48"){using var image=new RenderTargetBitmap(new PixelSize(2320,300),new Vector(192,192));image.Render(canvas);image.Save(Path.Combine(output,"resultrow-"+c.Id+".png"),new PngBitmapEncoderOptions());}}File.WriteAllText(Path.Combine(output,"resultrow-native.json"),JsonSerializer.Serialize(new{scale=w.RenderScaling,rows},new JsonSerializerOptions{WriteIndented=true}));w.Close();Console.WriteLine($"ResultSummaryRow {rows.Count} geometry/update endpoints recorded; actual input/timing are separate and pending.");},DispatcherPriority.Background);
  base.OnFrameworkInitializationCompleted();
 }
 internal sealed record RowCase(string Id,string Lang,double Width,string Key,string Name,string Description,string Icon,long Different,long Unknown,long Equal,double Maximum,bool Hover,bool Focus,bool Last,bool Compact=false,bool Disabled=false);
}
