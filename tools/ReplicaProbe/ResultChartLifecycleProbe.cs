using System.Text.Json;
using Avalonia;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Verifier.Desktop;
internal sealed class ResultChartLifecycleApplication:Application
{
 public override void Initialize()=>Styles.Add(new Avalonia.Themes.Fluent.FluentTheme());
 public override void OnFrameworkInitializationCompleted()
 {
  var args=((IClassicDesktopStyleApplicationLifetime)ApplicationLifetime!).Args!;var output=args[2];Directory.CreateDirectory(output);
  var cases=JsonSerializer.Deserialize<ResultChartProbeApplication.ChartCase[]>(File.ReadAllText(args[1]),new JsonSerializerOptions{PropertyNameCaseInsensitive=true})!;
  var initial=cases.Single(c=>c.Id=="2");var chart=new WebResultChart{Width=980};var canvas=new Canvas{Width=1160,Height=650,UseLayoutRounding=false,Background=Brush.Parse("#313338")};canvas.Children.Add(chart);Canvas.SetLeft(chart,30);Canvas.SetTop(chart,30);
  var w=new Window{Title="Replica ResultChart verification",Width=1200,Height=760,Content=canvas,UseLayoutRounding=false};((IClassicDesktopStyleApplicationLifetime)ApplicationLifetime!).MainWindow=w;
  var requests=new List<string>();var phases=new List<object>();
  object Count(WebResultCount count){var number=count.Number.RenderTransform?.Value??Matrix.Identity;var bar=count.Bar.RenderTransform?.Value??Matrix.Identity;return new{count=count.Count,maximum=count.Maximum,opacity=count.Number.Opacity,y=number.M32,barScale=bar.M11,attached=count.IsAttachedToVisualTree()};}
  void Assign(IEnumerable<WebResultGroup> groups,bool english=false){chart.SetGroups(groups,english);w.UpdateLayout();foreach(var row in chart.Rows)AutomationProperties.SetAutomationId(row,"ChartRow-"+row.CategoryKey);}
  object Read(string phase,Task? task=null,WebResultCount[]? old=null)=>new{phase,present=chart.IsVisible,attached=chart.IsAttachedToVisualTree(),requests=requests.ToArray(),status=task?.Status.ToString(),maximum=chart.Maximum,heading=chart.Heading.Text,rows=chart.Rows.Select(row=>new{key=row.CategoryKey,counts=row.Counts.Select(Count).ToArray()}).ToArray(),old=old?.Select(Count).ToArray()};
  void Live(){var json=JsonSerializer.Serialize(Read("input"),new JsonSerializerOptions{WriteIndented=true});var path=Path.Combine(output,"resultchart-input.json");File.WriteAllText(path+".tmp",json);File.Move(path+".tmp",path,true);}
  void Sample(){Dispatcher.UIThread.Post(Live,DispatcherPriority.Background);}
  chart.DetailRequested+=key=>{requests.Add(key);Sample();};w.AddHandler(InputElement.KeyUpEvent,(_,_)=>Sample(),RoutingStrategies.Tunnel,true);
  w.Opened+=(_,_)=>Dispatcher.UIThread.Post(async()=>{
   Assign(initial.Groups);if(args[0]=="--resultchart-window"){Live();return;}
   await Task.Delay(500);phases.Add(Read("mount"));var entry=chart.PlayEntrance();phases.Add(Read("start",entry));await Task.Delay(60);phases.Add(Read("running",entry));await entry;phases.Add(Read("completed",entry));
   Assign(initial.Groups,true);phases.Add(Read("ordinary-rerender"));var reduced=chart.PlayEntrance(true);await reduced;phases.Add(Read("reduced",reduced));
   using(var cancellation=new CancellationTokenSource()){var cancel=chart.PlayEntrance(false,cancellation.Token);await Task.Delay(60);cancellation.Cancel();try{await cancel;}catch(OperationCanceledException){}phases.Add(Read("cancelled",cancel));}
   var old=chart.Rows.SelectMany(row=>row.Counts).ToArray();var replaced=chart.PlayEntrance();await Task.Delay(60);Assign(cases.Single(c=>c.Id=="4").Groups);try{await replaced;}catch(OperationCanceledException){}phases.Add(Read("replaced",replaced,old));
   var detached=chart.PlayEntrance();await Task.Delay(60);canvas.Children.Remove(chart);try{await detached;}catch(OperationCanceledException){}phases.Add(Read("detached",detached));canvas.Children.Add(chart);w.UpdateLayout();phases.Add(Read("reattached"));var replay=chart.PlayEntrance();await replay;phases.Add(Read("replayed",replay));
   Assign([]);phases.Add(Read("empty"));Assign([new("trial",4,3,1,"completed")]);phases.Add(Read("trial-only"));
   Assign(initial.Groups.Append(new("trial",999,999,0,"completed")));phases.Add(Read("trial-excluded"));Assign(cases.Single(c=>c.Id=="37").Groups);phases.Add(Read("scale-update"));
   File.WriteAllText(Path.Combine(output,"resultchart-lifecycle.json"),JsonSerializer.Serialize(new{scale=w.RenderScaling,phases},new JsonSerializerOptions{WriteIndented=true}));w.Close();Console.WriteLine($"ResultChart {phases.Count} attached lifecycle states recorded.");
  },DispatcherPriority.Background);
  base.OnFrameworkInitializationCompleted();
 }
}
