using System.Diagnostics;
using System.Text.Json;
using Avalonia;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Verifier.Desktop;
internal static class ProgressPageNativeScope
{
 internal static void Attach(Window window,Canvas host,WebProgressPage page,ReplicaSampleRun initial,string output)
 {
  var revision=0;var snapshot=0;var frames=new List<object>();string phase="initial";
  page.Width=980;page.ReducedMotion=false;page.SetPresentation(initial,false);window.UpdateLayout();
  AutomationProperties.SetAutomationId(page.Table,"Progress-table");
  void IDs(){foreach(var row in page.Table.Rows)AutomationProperties.SetAutomationId(row,"Progress-row-"+row.Key);}
  object Box(Control c){var p=c.TranslatePoint(default,window)??default;return new{x=p.X,y=p.Y,width=c.Bounds.Width,height=c.Bounds.Height};}
  object Read()=>new{snapshot=++snapshot,phase,headless=ReplicaPlatform.IsHeadless,dpr=window.RenderScaling,reduced=page.ReducedMotion,english=page.English,attached=page.IsAttachedToVisualTree(),pageOpacity=page.Opacity,run=page.Run,rows=page.IsAttachedToVisualTree()?page.Table.Rows.Select(r=>new{id=AutomationProperties.GetAutomationId(r),key=r.Key,status=r.Status,box=Box(r),pointer=r.IsPointerOver,background=(r.Background as ISolidColorBrush)?.Color.ToString(),cells=r.Cells.Select(c=>new{box=Box(c),opacity=c.Opacity,y=(c.RenderTransform as TranslateTransform)?.Y??0}).ToArray(),pillOpacity=r.Pill.Opacity}).ToArray():[]};
  void Record(){if(!window.IsVisible)return;window.UpdateLayout();var value=Read();var path=Path.Combine(output,"progresspage-actual-state.json");File.WriteAllText(path+".tmp",JsonSerializer.Serialize(value,new JsonSerializerOptions{WriteIndented=true}));File.Move(path+".tmp",path,true);}
  void Observe(){var current=++revision;var started=Stopwatch.GetTimestamp();frames.Clear();void Frame(TimeSpan _){if(current!=revision||!window.IsVisible)return;frames.Add(new{ms=Stopwatch.GetElapsedTime(started).TotalMilliseconds,state=Read()});File.WriteAllText(Path.Combine(output,"progresspage-actual-motion.json"),JsonSerializer.Serialize(frames,new JsonSerializerOptions{WriteIndented=true}));if(Stopwatch.GetElapsedTime(started).TotalMilliseconds<650)window.RequestAnimationFrame(Frame);}window.RequestAnimationFrame(Frame);}
  window.KeyDown+=(_,e)=>{
   if(e.Key==Key.F5){Record();e.Handled=true;return;}
   if(e.Key==Key.F6){page.ReducedMotion=!page.ReducedMotion;page.SetPresentation(page.Run,page.English);phase="reduced";}
   else if(e.Key==Key.F7){if(page.IsAttachedToVisualTree()){host.Children.Remove(page);phase="detached";}else{host.Children.Add(page);phase="remounted";}}
   else if(e.Key==Key.F8){page.SetPresentation(page.Run,!page.English);phase="language";}
   else if(e.Key==Key.F9){page.SetPresentation(initial,page.English);phase="reset";}
   else if(e.Key==Key.F10){page.Update(page.Run.Tick());phase="live";Observe();}
   else if(e.Key==Key.F11||(e.Key==Key.I&&e.KeyModifiers.HasFlag(KeyModifiers.Control))){page.SetPresentation(page.Run.Cancel(),page.English);phase="interrupted";}
   else return;
   IDs();window.UpdateLayout();Record();e.Handled=true;
  };
  var timer=new DispatcherTimer{Interval=TimeSpan.FromMilliseconds(64)};timer.Tick+=(_,_)=>Record();window.Closed+=(_,_)=>{++revision;timer.Stop();};IDs();Record();timer.Start();
  File.WriteAllText(Path.Combine(output,"progresspage-actual-ready.json"),JsonSerializer.Serialize(new{headless=ReplicaPlatform.IsHeadless,dpr=window.RenderScaling,scope="actualOS own page geometry80/lifecycle9 recorded first; readonly state64ms; F6-F11 probe preparation only; real row input/motion/AX pending"}));
 }
}
