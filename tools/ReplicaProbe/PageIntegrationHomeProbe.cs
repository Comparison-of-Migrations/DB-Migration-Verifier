using System.Text.Json;
using Avalonia;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Input;
using Avalonia.Threading;
using Verifier.Desktop;

internal static class PageIntegrationHomeProbe
{
    internal static async Task Check(ReplicaWorkspace workspace,Window window,string output)
    {
        var checks=new List<object>();var external=new List<string>();workspace.ExternalActionRequested+=(_,action)=>external.Add(action);
        void Assert(string id,bool pass,object state){checks.Add(new{id,pass,state});File.WriteAllText(Path.Combine(output,"pageintegration-home-checks.json"),JsonSerializer.Serialize(checks,new JsonSerializerOptions{WriteIndented=true}));if(!pass)throw new InvalidOperationException(id);}
        async Task Flush(){await Dispatcher.UIThread.InvokeAsync(()=>{},DispatcherPriority.Background);window.UpdateLayout();AvaloniaHeadlessPlatform.ForceRenderTimerTick(1);}
        Point Centre(Control c)=>c.TranslatePoint(new(c.Bounds.Width/2,c.Bounds.Height/2),window)??throw new InvalidOperationException("Attached target required");
        void Click(Control c){c.BringIntoView();window.UpdateLayout();var p=Centre(c);window.MouseMove(p);window.MouseDown(p,MouseButton.Left);window.MouseUp(p,MouseButton.Left);}
        window.Activate();window.Width=1280;window.Height=900;workspace.SetLanguage(false);workspace.Navigate("home");await Flush();
        Assert("home-connected",workspace.CurrentPage is WebHomePage,new{type=workspace.CurrentPage?.GetType().Name});
        var home=(WebHomePage)workspace.CurrentPage!;Assert("home-go-heading-focus",home.Heading.FocusTarget.IsFocused&&workspace.Viewport.Offset==0,new{home.Heading.FocusTarget.IsFocused,workspace.Viewport.Offset});
        Click(home.StartButton);await Flush();Assert("home-start-connect",workspace.Screen=="connect"&&workspace.CurrentPage is WebConnectionPage,new{workspace.Screen});
        Click(workspace.Footer.BackButton!);await Flush();Assert("connect-back-home",workspace.Screen=="home"&&((WebHomePage)workspace.CurrentPage!).Heading.FocusTarget.IsFocused,new{workspace.Screen});
        home=(WebHomePage)workspace.CurrentPage!;Click(home.Paper.ResearchButton);await Flush();Assert("home-research-modal",workspace.InfoDialog is not null&&!home.Paper.ResearchButton.IsEffectivelyEnabled&&workspace.InfoDialog.CloseButton.IsFocused,new{visible=workspace.InfoDialog is not null});
        await workspace.InfoDialog!.CloseAsync();await Flush();Assert("home-research-restore",home.Paper.ResearchButton.IsFocused&&home.Paper.ResearchButton.IsEffectivelyEnabled,new{home.Paper.ResearchButton.IsFocused});
        workspace.Viewport.Offset=40;Click(workspace.TopBar.LanguageButton);await Flush();Assert("home-language-identity",workspace.English&&workspace.CurrentPage is WebHomePage en&&en.View.English&&workspace.TopBar.LanguageButton.IsFocused,new{workspace.English,focus=AutomationProperties.GetAutomationId(window.FocusManager?.GetFocusedElement()as Control??workspace)});
        home=(WebHomePage)workspace.CurrentPage!;Click(home.OpenButton);await Flush();Assert("home-open-external",external.SequenceEqual(["open"])&&workspace.Screen=="home",new{external,workspace.Screen});
        Click(home.SampleButton);await Flush();Assert("home-sample-state",workspace.Screen=="results"&&workspace.SampleRun is{Complete:true,Interrupted:false},new{workspace.Screen,complete=workspace.SampleRun?.Complete,scope="Results route itself remains unverified until page LOCK"});
        await workspace.DispatchAsync("start");await Flush();Assert("home-start-reset",workspace.SampleRun is null&&workspace.Screen=="connect"&&!workspace.Connection.Connected,new{workspace.Screen});
        workspace.Navigate("home");await Flush();home=(WebHomePage)workspace.CurrentPage!;Click(home.HelpText);await Flush();Assert("home-neutral-parent",home.IsFocused,new{focus=AutomationProperties.GetAutomationId(window.FocusManager?.GetFocusedElement()as Control??workspace)});
        workspace.SetLanguage(false);await Flush();home=(WebHomePage)workspace.CurrentPage!;Assert("home-neutral-language-recovery",home.IsFocused,new{focus=AutomationProperties.GetAutomationId(window.FocusManager?.GetFocusedElement()as Control??workspace)});
        workspace.ReducedMotion=false;workspace.Navigate("home");await Flush();home=(WebHomePage)workspace.CurrentPage!;
        var field=typeof(ReplicaWorkspace).GetField("_homeEntry",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic)!;var entry=(WebMotionPlayback?)field.GetValue(workspace);
        Assert("home-parent-entry-created",entry is not null,new{entry=entry is not null});
        typeof(WebMotionPlayback).GetMethod("Advance",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic)!.Invoke(entry,new object[]{TimeSpan.FromMilliseconds(80)});
        Assert("home-parent-entry-value",Math.Abs(home.Opacity-(.35+.65*WebMotion.SourceEase().Ease(.4)))<1e-9,new{home.Opacity});
        workspace.SetLanguage(true);await Flush();Assert("home-replacement-entry",!ReferenceEquals(field.GetValue(workspace),entry)&&home.Opacity==1,new{oldOpacity=home.Opacity});
    }
}
