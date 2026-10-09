using System.Text.Json;
using Avalonia;
using Avalonia.VisualTree;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Input;
using Avalonia.Threading;
using Verifier.Desktop;

/// <summary>Only the new connected Trial resize identity/caret/entry concern. No actualOS input claim.</summary>
internal static class PageIntegrationTrialResizeProbe
{
    internal static async Task Check(ReplicaWorkspace workspace,Window window,string output)
    {
        var rows=new List<object>();
        async Task Flush(){await Dispatcher.UIThread.InvokeAsync(()=>{},DispatcherPriority.Background);window.UpdateLayout();AvaloniaHeadlessPlatform.ForceRenderTimerTick(1);}
        async Task Settle(){await Task.Delay(400);await Flush();}
        workspace.ReducedMotion=false;window.Height=900;window.Activate();
        foreach(var english in new[]{false,true})
        {
            window.Width=1280;workspace.SetLanguage(english);workspace.Navigate("trial");
            workspace.SetTrial(new(english,"trial.example.invalid","migration_test","1","-1",false,false,""));
            await Flush();await Settle();
            var page=(WebTrialPage)workspace.CurrentPage!;var editor=page.Inputs[0];var field=page.Fields[0];
            editor.Focus(NavigationMethod.Tab);editor.SelectionStart=3;editor.SelectionEnd=8;var expected=editor.Text;
            var step=0;
            foreach(var width in new[]{1280,1100,960,1280})
            {
                window.Width=width;await Flush();
                var immediatePage=(WebTrialPage)workspace.CurrentPage!;
                var immediate=new{opacity=immediatePage.Opacity,translateY=immediatePage.RenderTransform?.Value.M32??0,
                    newPageAnimationObserved=!ReferenceEquals(page,immediatePage)&&(immediatePage.Opacity<.999||Math.Abs(immediatePage.RenderTransform?.Value.M32??0)>.001)};
                await Settle();var current=(WebTrialPage)workspace.CurrentPage!;var input=current.Inputs[0];
                object Box(Control control){var position=control.TranslatePoint(default,current)??throw new InvalidOperationException("Attached resize geometry required");return new{x=position.X,y=position.Y,width=control.Bounds.Width,height=control.Bounds.Height};}
                var geometry=new{root=Box(current),split=Box(current.ConnectionSplit),panels=current.Panels.Take(2).Select(Box).ToArray(),fields=current.Fields.Select(Box).ToArray(),inputs=current.Inputs.Select(Box).ToArray(),valueSplit=Box(current.ValueSplit)};
                rows.Add(new{id=(english?"en-":"ko-")+step++,language=english?"en":"ko",width,actualWidth=window.ClientSize.Width,
                    pageRetained=ReferenceEquals(page,current),editorRetained=ReferenceEquals(editor,input),fieldRetained=ReferenceEquals(field,current.Fields[0]),
                    focused=input.IsFocused,value=input.Text,expectedValue=expected,selectionStart=input.SelectionStart,selectionEnd=input.SelectionEnd,
                    caretIndex=input.CaretIndex,geometry,immediate,newPageAnimations=immediate.newPageAnimationObserved?1:0});
                File.WriteAllText(Path.Combine(output,"pageintegration-trial-resize.json"),JsonSerializer.Serialize(new{phase="HEADLESS_RECORDED",scope="New connected Trial resize identity/caret/public entry state; actualOS unverified",rows},new JsonSerializerOptions{WriteIndented=true}));
            }
        }
    }
    internal static async Task CheckStopped(ReplicaWorkspace workspace,Window window,string output)
    {
        var rows=new List<object>();
        async Task Flush(){await Dispatcher.UIThread.InvokeAsync(()=>{},DispatcherPriority.Background);window.UpdateLayout();AvaloniaHeadlessPlatform.ForceRenderTimerTick(1);}
        object State()=>new{stopped=workspace.Stopped,ready=workspace.Trial.Ready,consent=workspace.Trial.Consent,connected=workspace.Connection.Connected,visited=workspace.Visited.ToArray()};
        window.Width=1280;window.Height=900;workspace.ReducedMotion=true;workspace.SetLanguage(false);
        foreach(var mode in new[]{"endpoint","dbms"})
        {
            await workspace.DispatchAsync("start");workspace.SetTrial(new(false,"trial.example.invalid","migration_test","1","-1",true,true,""));
            workspace.Navigate("review");await Flush();((WebReviewPage)workspace.CurrentPage!).StoppedChoice.Checkbox.IsChecked=true;
            workspace.Navigate("connect");await Flush();await workspace.DispatchAsync("connect");await Flush();
            if(!workspace.Stopped||!workspace.Trial.Ready||!workspace.Trial.Consent||!workspace.Connection.Connected)
                throw new InvalidOperationException("Stopped concern setup was not ready: "+mode);
            var before=State();var connection=(WebConnectionPage)workspace.CurrentPage!;
            if(mode=="endpoint")connection.Source.HostInput.Text="changed.example.invalid";
            else
            {
                var radio=connection.Picker.Options.Single(o=>o.EngineName=="PostgreSQL").Radio;
                radio.BringIntoView();await Flush();var point=radio.TranslatePoint(new(radio.Bounds.Width/2,radio.Bounds.Height/2),window)??throw new InvalidOperationException("Attached DBMS required");
                window.MouseMove(point);window.MouseDown(point,MouseButton.Left);window.MouseUp(point,MouseButton.Left);
            }
            await Flush();rows.Add(new{id=mode,before,after=State()});
            File.WriteAllText(Path.Combine(output,"pageintegration-trial-stopped.json"),JsonSerializer.Serialize(new{phase="HEADLESS_RECORDED",scope="New Connection endpoint/dbms stopped state after Review confirmation; public model setup/headless input, actualOS unverified",rows},new JsonSerializerOptions{WriteIndented=true}));
        }
    }

    internal static async Task PrepareNative(ReplicaWorkspace workspace,Window window,string output)
    {
        var json=new JsonSerializerOptions{WriteIndented=true};WebTrialPage? seedPage=null;WebTextInput? seedEditor=null;WebField? seedField=null;
        var english=Environment.GetEnvironmentVariable("DBMV_TRIAL_RESIZE_EN_ONLY")=="1";var ready=false;var snapshot=0;var recording=false;
        async Task Flush(){await Dispatcher.UIThread.InvokeAsync(()=>{},DispatcherPriority.Background);window.UpdateLayout();}
        void Record()
        {
            if(!ready||recording||workspace.CurrentPage is not WebTrialPage page)return;recording=true;
            try
            {
                window.UpdateLayout();var editor=page.Inputs[0];
                object Box(Control control){var p=control.TranslatePoint(default,page)??throw new InvalidOperationException("Attached native resize geometry required");return new{x=p.X,y=p.Y,width=control.Bounds.Width,height=control.Bounds.Height};}
                var geometry=new{root=Box(page),split=Box(page.ConnectionSplit),panels=page.Panels.Take(2).Select(Box).ToArray(),fields=page.Fields.Select(Box).ToArray(),inputs=page.Inputs.Select(Box).ToArray(),valueSplit=Box(page.ValueSplit)};
                var value=new{snapshot=++snapshot,setupReady=ready,language=english?"en":"ko",width=window.ClientSize.Width,height=window.ClientSize.Height,dpr=window.RenderScaling,
                    pageRetained=ReferenceEquals(seedPage,page),editorRetained=ReferenceEquals(seedEditor,editor),fieldRetained=ReferenceEquals(seedField,page.Fields[0]),
                    focused=editor.IsFocused,value=editor.Text,expectedValue="trial.example.invalid",selectionStart=editor.SelectionStart,selectionEnd=editor.SelectionEnd,caretIndex=editor.CaretIndex,
                    immediate=new{opacity=page.Opacity,translateY=page.RenderTransform?.Value.M32??0},newPageAnimations=!ReferenceEquals(seedPage,page)&&(page.Opacity<.999||Math.Abs(page.RenderTransform?.Value.M32??0)>.001)?1:0,
                    geometry,scope="Actual native window externally resized by guarded UIA/AX/Win32 actor; F8 baseline KOEN seed only"};
                var file=Path.Combine(output,$"pageintegration-trial-resize-state-{snapshot:D6}.json");File.WriteAllText(file+".tmp",JsonSerializer.Serialize(value,json));File.Move(file+".tmp",file);
            }
            catch(Exception e){File.WriteAllText(Path.Combine(output,"pageintegration-trial-resize-observer-error.json"),JsonSerializer.Serialize(new{phase="UNVERIFIED",exception=e.ToString()},json));}
            finally{recording=false;}
        }
        async Task Seed()
        {
            ready=false;workspace.ReducedMotion=false;workspace.SetLanguage(english);workspace.Navigate("trial");workspace.SetTrial(new(english,"trial.example.invalid","migration_test","1","-1",false,false,""));
            await Flush();await Task.Delay(400);await Flush();seedPage=(WebTrialPage)workspace.CurrentPage!;seedEditor=seedPage.Inputs[0];seedField=seedPage.Fields[0];
            seedEditor.Focus(NavigationMethod.Tab);seedEditor.SelectionStart=3;seedEditor.SelectionEnd=8;ready=true;Record();
        }
        window.Width=1280;window.Height=900;window.Activate();await Seed();
        window.KeyDown+=async(_,e)=>{if(e.Key!=Key.F8)return;e.Handled=true;if(english||!ready)return;english=true;await Seed();};
        window.SizeChanged+=(_,_)=>Dispatcher.UIThread.Post(Record,DispatcherPriority.Loaded);
        var observer=new DispatcherTimer{Interval=TimeSpan.FromMilliseconds(100)};observer.Tick+=(_,_)=>Record();observer.Start();window.Closed+=(_,_)=>observer.Stop();
        File.WriteAllText(Path.Combine(output,"pageintegration-trial-resize-actual-ready.json"),JsonSerializer.Serialize(new{phase="ACTUAL_OS_READY",dpr=window.RenderScaling,scope="Only new resize KOEN8/geometry. External actor must resize physical native window; no APISetViewport shortcut. F8 seeds nextlanguage once."},json));
    }

}
