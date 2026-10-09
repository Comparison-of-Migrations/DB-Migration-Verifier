using System.Reflection;
using System.Text.Json;
using Avalonia;
using Avalonia.Automation.Peers;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Input;
using Avalonia.VisualTree;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using Verifier.Desktop;

internal static class ProgressPageHeadlessInput
{
    internal static async Task HoverDiagnostic(Window window,WebProgressPage page,ReplicaSampleRun initial,string output)
    {
        var states=new List<object>();
        async Task Flush(){window.UpdateLayout();await Dispatcher.UIThread.InvokeAsync(()=>{},DispatcherPriority.Background);AvaloniaHeadlessPlatform.ForceRenderTimerTick(1);}
        page.Width=980;page.ReducedMotion=true;page.SetPresentation(initial,true);page.Update(initial.Tick());await Flush();
        var row=page.Table.Rows[0];var cell=row.Cells[0];var point=cell.TranslatePoint(new(cell.Bounds.Width/2,cell.Bounds.Height/2),window)!.Value;
        void Read(string phase){var hit=window.InputHitTest(point);states.Add(new{phase,point,window=window.Bounds,row=row.Bounds,cell=cell.Bounds,page=page.Bounds,background=(row.Background as ISolidColorBrush)?.Color.ToString(),rowPointer=row.IsPointerOver,cellPointer=cell.IsPointerOver,hit=hit?.GetType().Name,ancestors=(hit as Visual)?.GetVisualAncestors().Select(v=>v.GetType().Name).ToArray(),labels=row.Labels.Select(x=>new{type=x.GetType().Name,box=x.Bounds}).ToArray()});File.WriteAllText(Path.Combine(output,"progresspage-hover-diagnostic.json"),JsonSerializer.Serialize(states,new JsonSerializerOptions{WriteIndented=true,NumberHandling=System.Text.Json.Serialization.JsonNumberHandling.AllowNamedFloatingPointLiterals}));}
        Read("initial");window.MouseMove(new(1400,950));await Flush();Read("outside");window.MouseMove(point);await Flush();Read("centre");
        var labelPoint=row.Labels[0].TranslatePoint(new(5,row.Labels[0].Bounds.Height/2),window)!.Value;window.MouseMove(labelPoint);await Flush();Read("label");
    }
    private const BindingFlags Fields=BindingFlags.Instance|BindingFlags.NonPublic|BindingFlags.Public;
    internal static async Task Run(Window window,Canvas host,WebProgressPage page,ReplicaSampleRun initial,string output,bool hoverOnly=false)
    {
        var states=new List<object>();
        WebMotionPlayback? Playback(WebProgressPage.TableRow row)=>(WebMotionPlayback?)typeof(WebProgressPage.TableRow).GetField("_enter",Fields)!.GetValue(row);
        Control? Target(WebMotionPlayback? p)=>p is null?null:(Control)typeof(WebMotionPlayback).GetField("_target",Fields)!.GetValue(p)!;
        void Check(string id,bool condition,object? detail=null){states.Add(new{id,passed=condition,detail});File.WriteAllText(Path.Combine(output,"progresspage-headless-input.json"),JsonSerializer.Serialize(new{states},new JsonSerializerOptions{WriteIndented=true}));if(!condition)throw new InvalidOperationException("Progress input mismatch: "+id);}
        async Task Flush(){window.UpdateLayout();await Dispatcher.UIThread.InvokeAsync(()=>{},DispatcherPriority.Background);AvaloniaHeadlessPlatform.ForceRenderTimerTick(1);}
        Point Centre(Control c)=>c.TranslatePoint(new(c.Bounds.Width/2,c.Bounds.Height/2),window)??throw new InvalidOperationException("Attached target required");
        Color Background(WebProgressPage.TableRow row)=>(row.Background as ISolidColorBrush)?.Color??default;
        if(!hoverOnly){
        page.Width=980;page.ReducedMotion=false;page.SetPresentation(initial,false);await Flush();
        Check("initial-status-no-enter",page.Table.Rows.All(r=>Playback(r) is null));
        var mid=initial.Tick();page.Update(mid);
        var first=page.Table.Rows.Select(Playback).ToArray();
        Check("first-live-all-cells",first.All(p=>p is not null)&&page.Table.Rows.Select((r,i)=>ReferenceEquals(Target(first[i]),r.Cells[1])).All(x=>x),new{count=first.Length,statuses=page.Table.Rows.Select(r=>r.Status).ToArray()});
        Check("pill-not-animation-owner",page.Table.Rows.All(r=>r.Pill.RenderTransform is null&&r.Pill.Opacity==1));
        foreach(var p in first)typeof(WebMotionPlayback).GetMethod("Advance",Fields)!.Invoke(p,new object[]{TimeSpan.FromMilliseconds(80)});
        var ease=WebMotion.SourceEase().Ease(.4);var expectedOpacity=.35+.65*ease;var expectedY=6*(1-ease);
        Check("cell-timeline-80ms",page.Table.Rows.All(r=>Math.Abs(r.Cells[1].Opacity-expectedOpacity)<1e-9&&r.Cells[1].RenderTransform is TranslateTransform t&&Math.Abs(t.Y-expectedY)<1e-9),new{expectedOpacity,expectedY});
        page.Update(mid.Tick());Check("same-status-no-restart",page.Table.Rows.Select((r,i)=>ReferenceEquals(Playback(r),first[i])).All(x=>x));
        var boundary=mid with{Groups=mid.Groups.Select((g,i)=>i==0?mid.CompleteGroup(g):g).ToArray()};page.Update(boundary);
        Check("only-changed-cell-restart",!ReferenceEquals(Playback(page.Table.Rows[0]),first[0])&&page.Table.Rows.Skip(1).Select((r,i)=>ReferenceEquals(Playback(r),first[i+1])).All(x=>x));
        page.SetPresentation(boundary,true);await Flush();
        Check("full-render-clears-live-cell",page.Table.Rows.All(r=>Playback(r)is null&&r.Cells[1].Opacity==1&&r.Cells[1].RenderTransform is null));
        page.Update(boundary);Check("full-render-next-live-all",page.Table.Rows.All(r=>Playback(r)is not null));
        page.ReducedMotion=true;page.SetPresentation(initial,true);page.Update(initial.Tick());await Flush();
        Check("reduced-cell-immediate",page.Table.Rows.All(r=>Playback(r)!.Completion.IsCompletedSuccessfully&&r.Cells[1].Opacity==1&&r.Cells[1].RenderTransform is null));
        var table=ControlAutomationPeer.CreatePeerForElement(page.Table)!;var groups=table.GetChildren()!;
        Check("table-two-rowgroups",table.GetAutomationControlType()==AutomationControlType.Table&&groups.Count==2&&groups.All(g=>g.GetClassName()=="RowGroup"));
        var body=groups[1].GetChildren()!;
        var rawRows=body.Select(r=>new{className=r.GetClassName(),children=r.GetChildren()!.Select(c=>new{className=c.GetClassName(),type=c.GetAutomationControlType().ToString()}).ToArray()}).ToArray();
        Check("rows-three-cells",body.Count==4&&body.All(r=>r.GetClassName()=="Row"&&r.GetChildren()!.Count(c=>c.GetClassName()=="Cell")==3),rawRows);
        }else{page.Width=980;page.ReducedMotion=true;page.SetPresentation(initial,true);page.Update(initial.Tick());await Flush();}
        var row=page.Table.Rows[0];var outside=new Point(1400,950);
        window.MouseMove(outside);await Flush();window.MouseMove(Centre(row.Cells[0]));await Flush();
        Check("reduced-hover",Background(row)==Color.Parse("#3a3c44"));
        using(var image=window.CaptureRenderedFrame()??throw new InvalidOperationException("Real headless rendering required"))image.Save(Path.Combine(output,"progresspage-hover.png"),new PngBitmapEncoderOptions());
        window.MouseMove(outside);await Flush();Check("reduced-leave",Background(row).A==0);
        page.ReducedMotion=false;page.SetPresentation(initial,false);window.MouseMove(Centre(row.Cells[0]));await Task.Delay(190);await Flush();Check("normal-hover",Background(row)==Color.Parse("#3a3c44"));
        window.MouseMove(outside);await Task.Delay(190);await Flush();Check("normal-leave",Background(row).A==0);
        page.Update(initial.Tick());host.Children.Remove(page);await Flush();Check("detach-clears-status-hover",page.Table.Rows.All(r=>Playback(r)is null&&Background(r).A==0));
        host.Children.Add(page);page.ReducedMotion=true;page.SetPresentation(initial,true);await Flush();window.MouseMove(Centre(page.Table.Rows[0].Cells[0]));await Flush();Check("remount-hover",Background(page.Table.Rows[0])==Color.Parse("#3a3c44"));
        window.MouseMove(outside);await Flush();Check("remount-leave",Background(page.Table.Rows[0]).A==0);
        File.WriteAllText(Path.Combine(output,"progresspage-headless-input.json"),JsonSerializer.Serialize(new{scope="New parent cell motion ownership, live lifecycle, routed row hover, managed peer structure; actual OS still required",states},new JsonSerializerOptions{WriteIndented=true}));
        if(!hoverOnly||OperatingSystem.IsWindows())Timing(output);
    }
    private static void Timing(string output)
    {
        var type=typeof(WebProgressPage.TableRow).GetNestedType("BackgroundTrack",BindingFlags.NonPublic)!;double clock=0;var rows=new List<object>();
        foreach(var scenario in new[]{"hover","unhover","hover-reverse","unhover-reverse"})
        {
            clock=0;var initial=Color.Parse(scenario.StartsWith("unhover")?"#3a3c44":"#00000000");var target=Color.Parse(scenario.StartsWith("hover")?"#3a3c44":"#00000000");
            var track=Activator.CreateInstance(type,Fields,null,new object[]{(Func<double>)(()=>clock)},null)!;type.GetMethod("Snap",Fields)!.Invoke(track,new object[]{initial});type.GetMethod("Set",Fields)!.Invoke(track,new object[]{target});
            var reverse=scenario.EndsWith("reverse");if(reverse){clock=30;type.GetMethod("Set",Fields)!.Invoke(track,new object[]{initial});}
            foreach(var ms in new[]{0d,.5,8,16,30,40,60,80,120,160,200,240}){clock=ms+(reverse?30:0);rows.Add(new{scenario,ms,value=type.GetProperty("Current",Fields)!.GetValue(track)!.ToString(),reverseDuration=reverse?(double?)type.GetField("_duration",Fields)!.GetValue(track):null});}
        }
        File.WriteAllText(Path.Combine(output,"progresspage-timing.json"),JsonSerializer.Serialize(new{rows},new JsonSerializerOptions{WriteIndented=true}));
    }
}
