using System.Text.Json;
using Avalonia;
using Avalonia.Automation;
using Avalonia.Automation.Peers;
using Avalonia.Automation.Provider;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Input;
using Avalonia.Input.Raw;
using Avalonia.Interactivity;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Verifier.Desktop;

/// <summary>Literal source handler sequence. Physical OS input is verified separately.</summary>
internal static class PageIntegrationFlowProbe
{
    internal static async Task PrepareNative(ReplicaWorkspace workspace,Window window,string output,bool english,bool resumeTrial=false)
    {
        window.Width=1280;window.Height=900;workspace.ReducedMotion=false;
        workspace.SetLanguage(english);workspace.Navigate("home");
        await Dispatcher.UIThread.InvokeAsync(()=>{},DispatcherPriority.Loaded);window.UpdateLayout();
        if(resumeTrial)
        {
            // Explicit fixture restoration of the already recorded Trial milestone.
            // These handler/provider calls are setup, never additional physical input proof.
            async Task Flush(){await Dispatcher.UIThread.InvokeAsync(()=>{},DispatcherPriority.Loaded);window.UpdateLayout();await Task.Delay(60);}
            await workspace.DispatchAsync("start");await Flush();
            ((WebConnectionPage)workspace.CurrentPage!).Source.HostInput.Text="source.example.invalid";
            await workspace.DispatchAsync("connect");await Flush();
            if(!workspace.Connection.Connected||workspace.Connection.Busy)throw new InvalidOperationException("Saved Trial fixture connection required");
            await workspace.DispatchAsync("preflight");await Flush();
            await workspace.DispatchAsync("choose");await Flush();
            var selection=(WebSelectionPage)workspace.CurrentPage!;
            selection.Rows.Single(x=>x.SourceName=="demo.orders").ChangeButton.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));await Flush();
            workspace.MatchingDialog!.Options.Single(x=>x.TargetName=="archive.orders").Radio.IsChecked=true;
            workspace.MatchingDialog.ApplyButton.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            var limit=DateTime.UtcNow.AddSeconds(4);
            while(workspace.MatchingDialog is not null){if(DateTime.UtcNow>limit)throw new InvalidOperationException("Saved Trial matching fixture close required");await Flush();}
            selection=(WebSelectionPage)workspace.CurrentPage!;
            ((IToggleProvider)ControlAutomationPeer.CreatePeerForElement(selection.Categories[4].Header.Checkbox)!).Toggle();await Flush();
            await workspace.DispatchAsync("trial");await Flush();await Task.Delay(400);
            if(workspace.Screen!="trial"||workspace.Trial.Consent||workspace.Trial.Ready||workspace.Stopped||workspace.Maps["demo.orders"]!="archive.orders"||workspace.Categories.Count!=5)
                throw new InvalidOperationException("Exact saved Trial17 fixture required");
            File.WriteAllText(Path.Combine(output,"flow-resume-fixture-proof.json"),JsonSerializer.Serialize(new{scope="Explicit public handler/provider restoration of already-passed Trial17; not physical input proof",state=PageIntegrationFlowSnapshot.Read("resume-trial17",workspace,window)},new JsonSerializerOptions{WriteIndented=true}));
        }
        var sequence=0;
        void Record()
        {
            var state=PageIntegrationFlowSnapshot.Read("native-observation-"+sequence,workspace,window);
            object Box(Control c){var p=c.TranslatePoint(default,workspace)??default;return new{x=p.X,y=p.Y,width=c.Bounds.Width,height=c.Bounds.Height};}
            var controls=workspace.GetVisualDescendants().OfType<Control>()
                .Where(c=>c is Button or WebTextInput)
                .Select(c=>new{id=AutomationProperties.GetAutomationId(c),name=AutomationProperties.GetName(c),type=c.GetType().Name,
                    text=c is Button b?b.Content?.ToString():c is WebTextInput t?t.Text:null,
                    enabled=c.IsEffectivelyEnabled,visible=c.IsEffectivelyVisible,attached=c.IsAttachedToVisualTree(),focused=c.IsFocused,box=Box(c)}).ToArray();
            var viewportPoint=workspace.Viewport.TranslatePoint(default,workspace)??default;
            var viewportRect=new Rect(viewportPoint,workspace.Viewport.Bounds.Size);
            var passiveWheelTargets=new List<object>();
            if(workspace.InfoDialog is null&&workspace.MatchingDialog is null&&workspace.ResultDetails is null)
            foreach(var text in workspace.GetVisualDescendants().OfType<WebSystemTextBlock>())
            {
                if(!text.IsEffectivelyVisible||!text.IsAttachedToVisualTree())continue;
                var p=text.TranslatePoint(default,workspace)??default;
                var clipped=new Rect(p,text.Bounds.Size).Intersect(viewportRect);
                if(clipped.Width<4||clipped.Height<4)continue;
                var point=clipped.Center;
                if(workspace.InputHitTest(point) is not Control hit)continue;
                var ancestors=hit.GetVisualAncestors().Prepend(hit).ToArray();
                if(!ancestors.Contains(workspace.Viewport)||ancestors.Any(c=>c is Button or WebTextInput or CheckBox or RadioButton))continue;
                passiveWheelTargets.Add(new{x=point.X,y=point.Y,text=text.Text,hit=hit.GetType().Name,ancestry=ancestors.Select(c=>c.GetType().Name).ToArray(),mainViewport=true});
            }
            var data=JsonSerializer.Serialize(new{sequence,state,controls,passiveWheelTargets,viewport=Box(workspace.Viewport),footer=Box(workspace.Footer),clientSize=new{width=window.ClientSize.Width,height=window.ClientSize.Height},dpr=window.RenderScaling,headless=ReplicaPlatform.IsHeadless},new JsonSerializerOptions{WriteIndented=true});
            // Immutable observations avoid rewriting a guest file concurrently with its reader.
            File.WriteAllText(Path.Combine(output,$"flow-state-{sequence++:D5}.json"),data);
        }
        var observer=new DispatcherTimer{Interval=TimeSpan.FromMilliseconds(100)};
        observer.Tick+=(_,_)=>Record();window.Closed+=(_,_)=>observer.Stop();Record();observer.Start();
        File.WriteAllText(Path.Combine(output,"pageintegration-flow-native-ready.json"),JsonSerializer.Serialize(new
        {phase="ACTUAL_OS_READY",english,resumeTrial,dpr=window.RenderScaling,headless=ReplicaPlatform.IsHeadless,scope="Read-only actual App/MainWindow observer; physical input must come from a guarded external actor"}));
    }

    internal static async Task Check(ReplicaWorkspace workspace, Window window, string output)
    {
        if(!ReplicaPlatform.IsHeadless)throw new InvalidOperationException("Handler checkbox simulation requires the isolated headless process; native input uses the external actor");
        window.Width=1280;window.Height=900;
        var rows=new List<object>();
        async Task Flush(int delay=35)
        {
            await Task.Delay(delay);
            await Dispatcher.UIThread.InvokeAsync(()=>{},DispatcherPriority.Background);
            window.UpdateLayout();
        }
        async Task Until(Func<bool> condition,string label)
        {
            var end=DateTime.UtcNow.AddSeconds(12);
            while(!condition())
            {
                if(DateTime.UtcNow>end)throw new InvalidOperationException(label);
                await Flush(10);
            }
        }
        void Record(string id)
        {
            rows.Add(PageIntegrationFlowSnapshot.Read(id,workspace,window));
            File.WriteAllText(Path.Combine(output,"pageintegration-flow.json"),JsonSerializer.Serialize(new
            {
                scope="Actual App/MainWindow handler sequence; programmatic focus/event and headless checkbox input, not physical OS input",
                environment=new{viewport=new{width=window.ClientSize.Width,height=window.ClientSize.Height},dpr=window.RenderScaling,headless=ReplicaPlatform.IsHeadless,motion="reduced",data="synthetic initial/sample"},rows
            },new JsonSerializerOptions{WriteIndented=true}));
        }
        Button Resolve(string action)
        {
            var footer=new List<WebActionButton>();
            if(workspace.Footer.BackButton is{} back)footer.Add(back);
            footer.AddRange(workspace.Footer.ActionButtons);
            var b=footer.FirstOrDefault(x=>AutomationProperties.GetAutomationId(x)=="Footer-"+action);
            if(b is not null)return b;
            return action switch
            {
                "start" => ((WebHomePage)workspace.CurrentPage!).StartButton,
                "restart" => workspace.GetVisualDescendants().OfType<Button>().Single(x=>AutomationProperties.GetAutomationId(x)=="Save-restart"),
                "permissions" => ((WebPreflightPage)workspace.CurrentPage!).Table.Rows.Single(x=>x.Key=="access").PermissionsButton,
                "clear-tables" => ((WebSelectionPage)workspace.CurrentPage!).DeselectButton,
                "trial" => ((WebSelectionPage)workspace.CurrentPage!).Preparation.PrepareButton!,
                "mapping:demo.orders" => ((WebSelectionPage)workspace.CurrentPage!).Rows.Single(x=>x.SourceName=="demo.orders").ChangeButton,
                "mapping-apply" => workspace.GetVisualDescendants().OfType<WebMatchingDialog>().Single(x=>x.IsVisible).ApplyButton,
                "modal-close" => (Button?)workspace.InfoDialog?.CloseButton??workspace.ResultDetails?.CloseButton??workspace.GetVisualDescendants().OfType<WebMatchingDialog>().Single(x=>x.IsVisible).CloseButton,
                "filter:unknown" => ((WebResultsPage)workspace.CurrentPage!).FilterButtons.Single(x=>AutomationProperties.GetAutomationId(x)=="Results-filter-unknown"),
                "detail:access" => ((WebResultsPage)workspace.CurrentPage!).Chart!.Rows.Single(x=>x.CategoryKey=="access"),
                _ => throw new InvalidOperationException("Source-shaped action absent: "+action)
            };
        }
        async Task Action(string action)
        {
            var button=Resolve(action);
            if(!button.IsEffectivelyEnabled)throw new InvalidOperationException("Source-shaped action disabled: "+action);
            button.Focus(NavigationMethod.Pointer);button.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            if(action is "modal-close" or "mapping-apply")
                await Until(()=>workspace.InfoDialog is null&&workspace.ResultDetails is null&&!workspace.GetVisualDescendants().OfType<WebMatchingDialog>().Any(x=>x.IsVisible),"Modal close completion");
            await Flush();
        }
        async Task Toggle(WebCheckbox checkbox)
        {
            if(!checkbox.IsEffectivelyEnabled)throw new InvalidOperationException("Source-shaped checkbox disabled");
            checkbox.Focus(NavigationMethod.Pointer);
            window.KeyPress(Key.Space,RawInputModifiers.None,PhysicalKey.Space," ");
            window.KeyRelease(Key.Space,RawInputModifiers.None,PhysicalKey.Space," ");
            await Flush();
        }
        async Task Field(WebTextInput field,string value)
        {field.Focus(NavigationMethod.Pointer);field.Text=value;await Flush();}
        async Task RunPath(string prefix,bool english,bool interrupted)
        {
            // Only initial setup resets state. All subsequent actions use the current live page.
            workspace.Dispose();workspace=new ReplicaWorkspace{ReducedMotion=true};window.Content=workspace;
            workspace.SetLanguage(english);workspace.Navigate("home");await Flush();Record(prefix+"home");
            await Action("start");Record(prefix+"connect");
            await Field(((WebConnectionPage)workspace.CurrentPage!).Source.HostInput,"");await Action("connect");Record(prefix+"invalid-connect");
            await Field(((WebConnectionPage)workspace.CurrentPage!).Source.HostInput,"source.example.invalid");await Action("connect");
            await Until(()=>workspace.Connection.Connected&&!workspace.Connection.Busy,"Connection completion");Record(prefix+"connected");
            await Action("preflight");Record(prefix+"preflight");await Action("permissions");Record(prefix+"permissions");await Action("modal-close");Record(prefix+"permissions-close");
            await Action("choose");
            if(workspace.CurrentPage is not WebSelectionPage)throw new InvalidOperationException("Selection is not connected; do not bypass its LOCK gate");
            Record(prefix+"selection");await Action("clear-tables");Record(prefix+"tables-empty");await Toggle(((WebSelectionPage)workspace.CurrentPage!).BulkCheckbox);Record(prefix+"tables-restored");
            await Action("mapping:demo.orders");
            var option=workspace.GetVisualDescendants().OfType<WebMatchingDialog>().Single(x=>x.IsVisible).Options.Single(x=>x.TargetName=="archive.orders");
            option.Radio.Focus(NavigationMethod.Pointer);option.Radio.IsChecked=true;await Flush();await Action("mapping-apply");Record(prefix+"mapped");
            await Toggle(((WebSelectionPage)workspace.CurrentPage!).Categories[4].Header.Checkbox);Record(prefix+"trial-required");
            await Action("trial");Record(prefix+"trial");await Toggle(((WebTrialPage)workspace.CurrentPage!).ConsentChoice.Checkbox);await Action("trial-check");Record(prefix+"trial-ready");
            await Action("trial-back");Record(prefix+"trial-return");await Action("review");Record(prefix+"review");
            await Toggle(((WebReviewPage)workspace.CurrentPage!).StoppedChoice.Checkbox);Record(prefix+"review-confirmed");await Action("run");Record(prefix+"running");
            if(interrupted){await Until(()=>workspace.SampleRun!.Groups.Any(g=>g.Done>0),"First sample tick");await Action("cancel");Record(prefix+"interrupted");}
            else {await Until(()=>workspace.SampleRun!.Complete,"Sample completion");Record(prefix+"completed");}
            await Action("show-results");Record(prefix+"results");await Action("filter:unknown");Record(prefix+"filtered");
            await Action("detail:access");Record(prefix+"detail");await Action("modal-close");Record(prefix+"detail-close");
            await Action("save");Record(prefix+"save");
            await Toggle(((WebSavePage)workspace.CurrentPage!).Options.Options.Single(x=>x.Format=="csv").Checkbox);
            await Toggle(((WebSavePage)workspace.CurrentPage!).Options.Options.Single(x=>x.Format=="html").Checkbox);Record(prefix+"formats");
            await Action("restart");Record(prefix+"restart");
        }
        await RunPath("ko-",false,false);await RunPath("en-",true,true);
    }
}
