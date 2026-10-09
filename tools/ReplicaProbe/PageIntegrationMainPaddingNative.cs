using System.Text.Json;
using Avalonia;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Verifier.Desktop;

/// <summary>Readonly native observer plus explicitly labeled four-case fixture advancement.</summary>
internal static class PageIntegrationMainPaddingNative
{
    internal static async Task Prepare(ReplicaWorkspace workspace,Window window,string output,string oracle)
    {
        if(ReplicaPlatform.IsHeadless)throw new InvalidOperationException("Native observer must use actual platform");
        using var document=JsonDocument.Parse(File.ReadAllText(oracle));
        var cases=document.RootElement.GetProperty("rows").EnumerateArray().Select(r=>new{Id=r.GetProperty("id").GetString()!,Width=r.GetProperty("width").GetInt32()}).ToArray();
        if(cases.Length!=4)throw new InvalidOperationException("Fresh original DOM4 required");
        var resumeFirstWheel=document.RootElement.TryGetProperty("candidateResumeOffset",out var resume)&&resume.GetDouble()==100;
        var index=0;var sequence=0;var ready=false;var preparing=false;var events=new List<object>();
        object Box(Control c){var p=c.TranslatePoint(default,workspace)??throw new InvalidOperationException("Attached box required");return new{x=p.X,y=p.Y,width=c.Bounds.Width,height=c.Bounds.Height};}
        object Hit(Point p){var hit=workspace.InputHitTest(p);var chain=hit is Visual v?v.GetVisualAncestors().Prepend(v).ToArray():Array.Empty<Visual>();return new{type=hit?.GetType().Name,viewportAncestor=chain.Contains(workspace.Viewport),ancestry=chain.Select(v=>v.GetType().Name).ToArray()};}
        void Record()
        {
            if(!ready||preparing)return;
            var v=workspace.Viewport;var p=v.TranslatePoint(default,workspace)??throw new InvalidOperationException("Attached viewport required");
            var focus=window.FocusManager?.GetFocusedElement();
            var point=new Point(p.X+8,p.Y+v.ClientHeight/2);
            File.WriteAllText(Path.Combine(output,$"main-padding-native-state-{sequence++:D5}.json"),JsonSerializer.Serialize(new{phase="NATIVE_READONLY_OBSERVATION",index,id=cases[index].Id,ready,point,hit=Hit(point),viewport=Box(v),clientSize=new{width=window.ClientSize.Width,height=window.ClientSize.Height},dpr=window.RenderScaling,
                heading=new{text=((WebTrialPage)workspace.CurrentPage!).Heading.TitleText.Text,box=Box(((WebTrialPage)workspace.CurrentPage!).Heading.TitleText)},isFocusVisible=v.IsFocusVisible,isTabStop=v.IsTabStop,offset=v.Offset,maximumOffset=v.MaximumOffset,scrollHeight=v.ScrollHeight,clientHeight=v.ClientHeight,focus=new{type=focus?.GetType().Name,id=focus is Control c?AutomationProperties.GetAutomationId(c):null},events=events.ToArray()},new JsonSerializerOptions{WriteIndented=true}));
        }
        void Observe(object? sender,PointerEventArgs e){events.Add(new{kind=e.RoutedEvent?.Name,source=e.Source?.GetType().Name,handled=e.Handled,point=e.GetPosition(workspace),offset=workspace.Viewport.Offset});Record();}
        workspace.AddHandler(InputElement.PointerWheelChangedEvent,Observe,RoutingStrategies.Bubble,true);workspace.AddHandler(InputElement.PointerPressedEvent,Observe,RoutingStrategies.Bubble,true);
        async Task Setup()
        {
            preparing=true;ready=false;window.Width=cases[index].Width;window.Height=900;workspace.ReducedMotion=false;workspace.SetLanguage(cases[index].Id.StartsWith("en-",StringComparison.Ordinal));
            workspace.SetScope(new[]{"data","schema","access","objects","trial"},new[]{"demo.customers","demo.orders","demo.line_items"});
            workspace.SetTrial(new(workspace.English,"trial.example.invalid","migration_test","1","-1",false,false,""));workspace.Navigate("trial");
            await Task.Delay(450);await Dispatcher.UIThread.InvokeAsync(()=>{},DispatcherPriority.Background);window.UpdateLayout();events.Clear();if(index==0&&resumeFirstWheel){workspace.Viewport.Offset=100;window.UpdateLayout();}preparing=false;ready=true;Record();
        }
        EventHandler<KeyEventArgs> next=async(_,e)=>{if(e.Key!=Key.F8||preparing)return;if(index+1>=cases.Length)return;index++;await Setup();e.Handled=true;};
        window.KeyUp+=next;var timer=new DispatcherTimer{Interval=TimeSpan.FromMilliseconds(100)};timer.Tick+=(_,_)=>Record();
        window.Closed+=(_,_)=>{timer.Stop();window.KeyUp-=next;workspace.RemoveHandler(InputElement.PointerWheelChangedEvent,Observe);workspace.RemoveHandler(InputElement.PointerPressedEvent,Observe);};
        await Setup();timer.Start();
        File.WriteAllText(Path.Combine(output,"main-padding-native-ready.json"),JsonSerializer.Serialize(new{phase="NATIVE_READY",cases,fixture="F8 advances only original literal Trial setup; no production input proof",resumeFirstWheel, resumeScope="Optional case0 offset100 fixture API reuses saved actual native wheel, not new wheel proof",scope="Blank left padding guarded trusted wheel/click external actor; no product changes"}));
    }
}
