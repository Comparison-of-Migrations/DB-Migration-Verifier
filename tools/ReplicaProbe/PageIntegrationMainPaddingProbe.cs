using System.Text.Json;
using Avalonia;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Verifier.Desktop;

/// <summary>New MainContent blank-padding hit route only; no product or source changes.</summary>
internal static class PageIntegrationMainPaddingProbe
{
    internal static async Task Check(ReplicaWorkspace workspace,Window window,string output,string oracle)
    {
        if(!ReplicaPlatform.IsHeadless)throw new InvalidOperationException("Rendered headless diagnostic only; trusted native actor is separate");
        using var original=JsonDocument.Parse(File.ReadAllText(oracle));
        if(original.RootElement.GetProperty("phase").GetString()!="ORIGINAL_DOM_ONLY"||original.RootElement.GetProperty("rows").GetArrayLength()!=4)
            throw new InvalidOperationException("Fresh original padding DOM4 required before candidate scope");
        var rows=new List<object>();var events=new List<object>();
        object Focus(){var e=window.FocusManager?.GetFocusedElement();return new{type=e?.GetType().Name,id=e is Control c?AutomationProperties.GetAutomationId(c):null};}
        object Hit(Point point){var hit=workspace.InputHitTest(point);var chain=hit is Visual v?v.GetVisualAncestors().Prepend(v).ToArray():Array.Empty<Visual>();return new{type=hit?.GetType().Name,viewportAncestor=chain.Contains(workspace.Viewport),ancestry=chain.Select(v=>v.GetType().Name).ToArray()};}
        void Observe(object? sender,PointerEventArgs e){var v=e.Source as Visual;events.Add(new{kind=e.RoutedEvent?.Name,source=e.Source?.GetType().Name,route=v?.GetVisualAncestors().Prepend(v).Select(x=>x.GetType().Name).ToArray(),handled=e.Handled,point=e.GetPosition(workspace),offset=workspace.Viewport.Offset});}
        workspace.AddHandler(InputElement.PointerWheelChangedEvent,Observe,RoutingStrategies.Bubble,true);
        workspace.AddHandler(InputElement.PointerPressedEvent,Observe,RoutingStrategies.Bubble,true);
        async Task Flush(int ms=60){await Task.Delay(ms);await Dispatcher.UIThread.InvokeAsync(()=>{},DispatcherPriority.Background);window.UpdateLayout();AvaloniaHeadlessPlatform.ForceRenderTimerTick(1);}
        window.Height=900;workspace.ReducedMotion=false;window.Activate();
        try
        {
            foreach(var source in original.RootElement.GetProperty("rows").EnumerateArray())
            {
                var id=source.GetProperty("id").GetString()!;var english=id.StartsWith("en-",StringComparison.Ordinal);var width=source.GetProperty("width").GetInt32();
                window.Width=width;workspace.SetLanguage(english);
                workspace.SetScope(new[]{"data","schema","access","objects","trial"},new[]{"demo.customers","demo.orders","demo.line_items"});
                workspace.SetTrial(new(english,"trial.example.invalid","migration_test","1","-1",false,false,""));workspace.Navigate("trial");await Flush(450);
                var viewport=workspace.Viewport;var origin=viewport.TranslatePoint(default,workspace)??throw new InvalidOperationException("Attached main viewport required");
                var points=new[]{("left-padding",new Point(origin.X+8,origin.Y+viewport.ClientHeight/2)),("top-padding",new Point(origin.X+viewport.ClientWidth/2,origin.Y+8))};
                foreach(var (label,point) in points)
                {
                    viewport.Offset=0;await Flush();var focusBefore=Focus();var hitBefore=Hit(point);var eventStart=events.Count;
                    var windowPoint=workspace.TranslatePoint(point,window)??throw new InvalidOperationException("Attached own window point required");
                    window.MouseMove(windowPoint);window.MouseWheel(windowPoint,new Vector(0,OperatingSystem.IsWindows()?-1:-2));await Flush(700);
                    var afterWheel=viewport.Offset;var focusAfterWheel=Focus();viewport.Offset=0;await Flush();
                    window.MouseDown(windowPoint,MouseButton.Left);window.MouseUp(windowPoint,MouseButton.Left);await Flush();
                    rows.Add(new{id=id+"-"+label,width,language=english?"en":"ko",point,hitBefore,hitAfter=Hit(point),focusBefore,focusAfterWheel,focusAfterClick=Focus(),wheelOffset=afterWheel,clickOffset=viewport.Offset,
                        viewport=new{x=origin.X,y=origin.Y,width=viewport.Bounds.Width,height=viewport.Bounds.Height,clientWidth=viewport.ClientWidth,clientHeight=viewport.ClientHeight,scrollHeight=viewport.ScrollHeight,maximumOffset=viewport.MaximumOffset},
                        original=source,events=events.Skip(eventStart).ToArray(),input="Avalonia.Headless real rendered routed input; not actualOS trusted input"});
                }
                using var image=window.CaptureRenderedFrame()??throw new InvalidOperationException("Real rendered padding frame required");image.Save(Path.Combine(output,"main-padding-"+id+".png"),new PngBitmapEncoderOptions());
            }
            File.WriteAllText(Path.Combine(output,"main-padding-headless.json"),JsonSerializer.Serialize(new{phase="HEADLESS_RECORDED",scope="Blank padding only; diagnostic no LOCK/pass claim",dpr=window.RenderScaling,rows},new JsonSerializerOptions{WriteIndented=true}));
        }
        finally
        {
            workspace.RemoveHandler(InputElement.PointerWheelChangedEvent,Observe);workspace.RemoveHandler(InputElement.PointerPressedEvent,Observe);
        }
    }
}
