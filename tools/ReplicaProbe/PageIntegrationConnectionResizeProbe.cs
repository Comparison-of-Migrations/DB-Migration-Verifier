using System.Text.Json;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Automation.Peers;
using Avalonia.Automation.Provider;
using Avalonia.Headless;
using Avalonia.Input;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Verifier.Desktop;

/// <summary>Only the new connected Connection resize identity/caret/entry scope.</summary>
internal static class PageIntegrationConnectionResizeProbe
{
    private static void Capture(Window window,Control visual,string output,string name)
    {
        var scale=window.RenderScaling;using var bitmap=new RenderTargetBitmap(new PixelSize((int)Math.Ceiling(window.ClientSize.Width*scale),(int)Math.Ceiling(window.ClientSize.Height*scale)),new Vector(96*scale,96*scale));bitmap.Render(visual);bitmap.Save(Path.Combine(output,name),new PngBitmapEncoderOptions());
    }
    internal static async Task Check(ReplicaWorkspace workspace,Window window,string output)
    {
        var rows=new List<object>();var expanded=Environment.GetEnvironmentVariable("DBMV_CONNECTION_RESIZE_ADVANCED_ONLY")=="1";
        async Task Flush(){await Dispatcher.UIThread.InvokeAsync(()=>{},DispatcherPriority.Background);window.UpdateLayout();AvaloniaHeadlessPlatform.ForceRenderTimerTick(1);}
        async Task Settle(){await Task.Delay(400);await Flush();}
        workspace.ReducedMotion=false;window.Height=900;window.Activate();
        foreach(var english in new[]{false,true})
        {
            window.Width=1280;workspace.SetLanguage(english);workspace.Navigate("connect");await Flush();
            if(expanded&&workspace.CurrentPage is WebConnectionPage setup&&!setup.View.Advanced){((IExpandCollapseProvider)ControlAutomationPeer.CreatePeerForElement(setup.Advanced.ToggleButton)!).Expand();await Flush();}
            await Settle();
            var page=(WebConnectionPage)workspace.CurrentPage!;var editor=page.Source.HostInput;var field=page.Source.Fields[0];var radios=page.Picker.Options.Select(o=>o.Radio).ToArray();var toggle=page.Advanced.ToggleButton;var explanation=page.Advanced.Explanation;var column=page.Advanced.Titles.FirstOrDefault()?.Parent;
            editor.Focus(NavigationMethod.Tab);editor.SelectionStart=3;editor.SelectionEnd=8;var expected=editor.Text;var step=0;
            foreach(var width in (expanded?new[]{1280,1100}:new[]{1280,1100,960,1280}))
            {
                window.Width=width;await Flush();var immediatePage=(WebConnectionPage)workspace.CurrentPage!;
                var immediate=new{opacity=immediatePage.Opacity,translateY=immediatePage.RenderTransform?.Value.M32??0,newPageAnimationObserved=!ReferenceEquals(page,immediatePage)&&(immediatePage.Opacity<.999||Math.Abs(immediatePage.RenderTransform?.Value.M32??0)>.001)};
                await Settle();if(english&&width==1280&&step==0)Capture(window,workspace,output,expanded?"connection-resize-expanded-en-1280.png":"connection-resize-en-1280.png");var current=(WebConnectionPage)workspace.CurrentPage!;var input=current.Source.HostInput;
                object Box(Control control){var p=control.TranslatePoint(default,current)??throw new InvalidOperationException("Attached Connection geometry required");return new{x=p.X,y=p.Y,width=control.Bounds.Width,height=control.Bounds.Height};}
                Control[] Inputs(WebConnectionPanel panel)=>[panel.HostInput,panel.PortInput,panel.DatabaseInput,panel.UserInput,panel.PasswordInput];
                var geometry=new Dictionary<string,object>{{"root",Box(current)},{"cards",Box(current.Picker.Cards)},{"options",current.Picker.Options.Select(Box).ToArray()},{"split",Box(current.Panels)},{"panels",new[]{Box(current.Source),Box(current.Target)}},{"fields",current.Source.Fields.Concat(current.Target.Fields).Select(Box).ToArray()},{"inputs",Inputs(current.Source).Concat(Inputs(current.Target)).Select(Box).ToArray()},{"advancedToggle",Box(current.Advanced.ToggleButton)}};
                if(expanded){var advanced=current.Advanced;var split=(Panel)advanced.Explanation!.Child!;geometry["advancedPanel"]=Box(advanced.Explanation);geometry["advancedSplit"]=Box(split);geometry["advancedColumns"]=split.Children.Select(Box).ToArray();geometry["advancedTitles"]=advanced.Titles.Select(Box).ToArray();geometry["advancedDescriptions"]=advanced.Descriptions.Select(Box).ToArray();}
                rows.Add(new{id=(english?"en-":"ko-")+(expanded?"advanced-":"")+step++,language=english?"en":"ko",width,actualWidth=window.ClientSize.Width,pageRetained=ReferenceEquals(page,current),editorRetained=ReferenceEquals(editor,input),fieldRetained=ReferenceEquals(field,current.Source.Fields[0]),pickerRetained=radios.Select((r,i)=>ReferenceEquals(r,current.Picker.Options[i].Radio)).All(v=>v),toggleRetained=ReferenceEquals(toggle,current.Advanced.ToggleButton),explanationRetained=ReferenceEquals(explanation,current.Advanced.Explanation),columnRetained=ReferenceEquals(column,current.Advanced.Titles.FirstOrDefault()?.Parent),focused=input.IsFocused,value=input.Text,expectedValue=expected,selectionStart=input.SelectionStart,selectionEnd=input.SelectionEnd,caretIndex=input.CaretIndex,geometry,immediate,newPageAnimations=immediate.newPageAnimationObserved?1:0});
                File.WriteAllText(Path.Combine(output,expanded?"pageintegration-connection-resize-expanded.json":"pageintegration-connection-resize.json"),JsonSerializer.Serialize(new{phase="HEADLESS_RECORDED",scope="New Connection resize identity/caret/entry8; actualOS unverified",rows},new JsonSerializerOptions{WriteIndented=true}));
            }
        }
    }
    internal static async Task PrepareNative(ReplicaWorkspace workspace,Window window,string output)
    {
        var json=new JsonSerializerOptions{WriteIndented=true};WebConnectionPage? seedPage=null;WebTextInput? seedEditor=null;WebField? seedField=null;WebRadio[] seedRadios=[];Control? seedToggle=null,seedExplanation=null;object? seedColumn=null;
        var phaseIndex=0;var ready=false;var snapshot=0;var recording=false;var captured=new HashSet<int>();
        bool English()=>phaseIndex%2==1;bool Expanded()=>phaseIndex>=2;
        async Task Flush(){await Dispatcher.UIThread.InvokeAsync(()=>{},DispatcherPriority.Background);window.UpdateLayout();}
        void Record()
        {
            if(!ready||recording||workspace.CurrentPage is not WebConnectionPage page)return;recording=true;
            try
            {
                window.UpdateLayout();if(English()&&Math.Abs(window.ClientSize.Width-1280)<.01&&captured.Add(phaseIndex))Capture(window,workspace,output,Expanded()?"connection-resize-expanded-en-1280.png":"connection-resize-en-1280.png");var editor=page.Source.HostInput;
                object Box(Control control){var p=control.TranslatePoint(default,page)??throw new InvalidOperationException("Attached native Connection geometry required");return new{x=p.X,y=p.Y,width=control.Bounds.Width,height=control.Bounds.Height};}
                Control[] Inputs(WebConnectionPanel panel)=>[panel.HostInput,panel.PortInput,panel.DatabaseInput,panel.UserInput,panel.PasswordInput];
                var geometry=new Dictionary<string,object>{{"root",Box(page)},{"cards",Box(page.Picker.Cards)},{"options",page.Picker.Options.Select(Box).ToArray()},{"split",Box(page.Panels)},{"panels",new[]{Box(page.Source),Box(page.Target)}},{"fields",page.Source.Fields.Concat(page.Target.Fields).Select(Box).ToArray()},{"inputs",Inputs(page.Source).Concat(Inputs(page.Target)).Select(Box).ToArray()},{"advancedToggle",Box(page.Advanced.ToggleButton)}};
                if(Expanded()){var advanced=page.Advanced;var split=(Panel)advanced.Explanation!.Child!;geometry["advancedPanel"]=Box(advanced.Explanation);geometry["advancedSplit"]=Box(split);geometry["advancedColumns"]=split.Children.Select(Box).ToArray();geometry["advancedTitles"]=advanced.Titles.Select(Box).ToArray();geometry["advancedDescriptions"]=advanced.Descriptions.Select(Box).ToArray();}
                var value=new{snapshot=++snapshot,setupReady=ready,phaseIndex,language=English()?"en":"ko",expanded=Expanded(),width=window.ClientSize.Width,height=window.ClientSize.Height,dpr=window.RenderScaling,pageRetained=ReferenceEquals(seedPage,page),editorRetained=ReferenceEquals(seedEditor,editor),fieldRetained=ReferenceEquals(seedField,page.Source.Fields[0]),pickerRetained=seedRadios.Select((r,i)=>ReferenceEquals(r,page.Picker.Options[i].Radio)).All(v=>v),toggleRetained=ReferenceEquals(seedToggle,page.Advanced.ToggleButton),explanationRetained=ReferenceEquals(seedExplanation,page.Advanced.Explanation),columnRetained=ReferenceEquals(seedColumn,page.Advanced.Titles.FirstOrDefault()?.Parent),focused=editor.IsFocused,value=editor.Text,selectionStart=editor.SelectionStart,selectionEnd=editor.SelectionEnd,caretIndex=editor.CaretIndex,immediate=new{opacity=page.Opacity,translateY=page.RenderTransform?.Value.M32??0},newPageAnimations=!ReferenceEquals(seedPage,page)&&(page.Opacity<.999||Math.Abs(page.RenderTransform?.Value.M32??0)>.001)?1:0,geometry,scope="Actual native externally resized; F8 only language/expanded baseline editor seed"};
                var file=Path.Combine(output,$"pageintegration-connection-resize-state-{snapshot:D6}.json");File.WriteAllText(file+".tmp",JsonSerializer.Serialize(value,json));File.Move(file+".tmp",file);
            }
            catch(Exception e){File.WriteAllText(Path.Combine(output,"pageintegration-connection-resize-observer-error.json"),JsonSerializer.Serialize(new{phase="UNVERIFIED",exception=e.ToString()},json));}
            finally{recording=false;}
        }
        async Task Seed()
        {
            ready=false;workspace.ReducedMotion=false;workspace.SetLanguage(English());workspace.Navigate("connect");await Flush();var current=(WebConnectionPage)workspace.CurrentPage!;
            var peer=(IExpandCollapseProvider)ControlAutomationPeer.CreatePeerForElement(current.Advanced.ToggleButton)!;
            if(Expanded()&&!current.View.Advanced)peer.Expand();else if(!Expanded()&&current.View.Advanced)peer.Collapse();
            await Flush();await Task.Delay(400);await Flush();seedPage=(WebConnectionPage)workspace.CurrentPage!;seedEditor=seedPage.Source.HostInput;seedField=seedPage.Source.Fields[0];seedRadios=seedPage.Picker.Options.Select(o=>o.Radio).ToArray();seedToggle=seedPage.Advanced.ToggleButton;seedExplanation=seedPage.Advanced.Explanation;seedColumn=seedPage.Advanced.Titles.FirstOrDefault()?.Parent;seedEditor.Focus(NavigationMethod.Tab);seedEditor.SelectionStart=3;seedEditor.SelectionEnd=8;ready=true;Record();
        }
        window.Width=1280;window.Height=900;window.Activate();await Seed();
        window.KeyDown+=async(_,e)=>{if(e.Key!=Key.F8)return;e.Handled=true;if(!ready||phaseIndex>=3)return;phaseIndex++;await Seed();};
        window.SizeChanged+=(_,_)=>Dispatcher.UIThread.Post(Record,DispatcherPriority.Loaded);var observer=new DispatcherTimer{Interval=TimeSpan.FromMilliseconds(100)};observer.Tick+=(_,_)=>Record();observer.Start();window.Closed+=(_,_)=>observer.Stop();
        File.WriteAllText(Path.Combine(output,"pageintegration-connection-resize-actual-ready.json"),JsonSerializer.Serialize(new{phase="ACTUAL_OS_READY",dpr=window.RenderScaling,scope="New Connection defaultKOEN8 +expandedKOEN4. External actual window resize only; F8 baseline phases."},json));
    }

}
