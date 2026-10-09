using System.Text.Json;
using Avalonia;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Input;
using Avalonia.Input.Raw;
using Avalonia.Interactivity;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Verifier.Desktop;

internal static class PageIntegrationProbe
{
    private sealed record Case(string Id,string Screen,bool English,double Width,double Height);
    public static void Run(string[] args)
    {
        var output=args[2];Directory.CreateDirectory(output);
        var native=args[0]=="--pageintegration-window";
        var modalOnly=args.Contains("--modal-only");var runOnly=args.Contains("--run-state-only");var exportOnly=args.Contains("--export-only");var fileOnly=args.Contains("--file-only")||args.Contains("--file-window-only");
        var homeOnly=args.Contains("--home-only");
        var trialOnly=args.Contains("--trial-only")||args.Contains("--trial-changed-only")||args.Contains("--trial-validation-only");
        var cases=JsonSerializer.Deserialize<Case[]>(File.ReadAllText(args[1]),new JsonSerializerOptions{PropertyNameCaseInsensitive=true})!;
        var started=new HashSet<MainWindow>();
        Window.IsVisibleProperty.Changed.AddClassHandler<MainWindow>((w,_)=>{
            if(!w.IsVisible||!started.Add(w))return;
            if(w.ReplicaPreview is not{}workspace)throw new InvalidOperationException("Expected the real application's preview route");
            w.Opened+=(_,_)=>Dispatcher.UIThread.Post(async()=>{
                try
                {
                    var rows=new List<object>();workspace.ReducedMotion=true;
                    if(native)w.Title="Replica PageIntegration application verification";
                    if(args.Contains("--slash-boundary-only"))
                    {
                        await PageIntegrationSlashProbe.Check(w,output,args[3]);w.Close();return;
                    }
                    if(args.Contains("--parent-resize-only"))
                    {
                        if(native)await PageIntegrationParentResizeProbe.PrepareNative(workspace,w,output,args[3]);
                        else {await PageIntegrationParentResizeProbe.Check(workspace,w,output,args[3]);w.Close();}
                        return;
                    }
                    if(args.Contains("--summary-content-only"))
                    {
                        if(native)await PageIntegrationSummaryContentProbe.PrepareNative(workspace,w,output);
                        else {await PageIntegrationSummaryContentProbe.Check(workspace,w,output);w.Close();}
                        return;
                    }
                    if(args.Contains("--main-padding-only"))
                    {
                        if(native)await PageIntegrationMainPaddingNative.Prepare(workspace,w,output,args[3]);
                        else {await PageIntegrationMainPaddingProbe.Check(workspace,w,output,args[3]);w.Close();}
                        return;
                    }
                    if(args.Contains("--toast-only"))
                    {
                        if(native)await PageIntegrationToastProbe.PrepareNative(workspace,w,output);
                        else {await PageIntegrationToastProbe.Check(workspace,w,output);w.Close();}
                        return;
                    }
                    if(args.Contains("--selection-callbacks-peer-only"))
                    {
                        await PageIntegrationSelectionCallbacksPeerProbe.Check(workspace,w,output,args[3]);w.Close();return;
                    }
                    if(args.Contains("--selection-callbacks-only"))
                    {
                        if(native)await PageIntegrationSelectionCallbacksNative.Prepare(workspace,w,output,args[3],args.Contains("--selection-callbacks-english"));
                        else {await PageIntegrationSelectionCallbacksProbe.Check(workspace,w,output,args[3]);w.Close();}
                        return;
                    }
                    if(args.Contains("--flow-only")||args.Contains("--flow-window-only"))
                    {
                        if(native)await PageIntegrationFlowProbe.PrepareNative(workspace,w,output,args.Contains("--flow-english"),args.Contains("--flow-resume-trial"));
                        else {await PageIntegrationFlowProbe.Check(workspace,w,output);w.Close();}
                        return;
                    }
                    if(args.Contains("--modal-bounds-only"))
                    {
                        await PageIntegrationModalBoundsProbe.Check(workspace,w,output,args[3]);w.Close();return;
                    }
                    if(args.Contains("--preflight-connected-only"))
                    {
                        await PageIntegrationPreflightProbe.Check(workspace,w,output,args[1],args[3]);w.Close();return;
                    }
                    if(args.Contains("--document-only"))
                    {
                        await PageIntegrationDocumentProbe.Check(workspace,w,output,args[3]);w.Close();return;
                    }
                    if(args.Contains("--progress-connected-only"))
                    {
                        await PageIntegrationProgressProbe.Check(workspace,w,output);w.Close();return;
                    }
                    if(args.Contains("--progress-parent-only"))
                    {
                        await PageIntegrationProgressProbe.CheckLayout(workspace,w,output,args[1],args[3]);w.Close();return;
                    }
                    if(args.Contains("--results-only"))
                    {
                        if(native)await PageIntegrationResultsProbe.PrepareNative(workspace,w,output);
                        else {await PageIntegrationResultsProbe.Check(workspace,w,output,args[3]);w.Close();}
                        return;
                    }
                    object Box(Control c){var p=c.TranslatePoint(default,workspace)??default;return new{x=p.X,y=p.Y,width=c.Bounds.Width,height=c.Bounds.Height};}
                    foreach(var c in (trialOnly||modalOnly||runOnly||exportOnly||fileOnly||args.Contains("--home-actions-only")?Array.Empty<Case>():cases))
                    {
                        w.Width=c.Width;w.Height=c.Height;workspace.SetLanguage(c.English);workspace.Navigate(c.Screen);
                        await Dispatcher.UIThread.InvokeAsync(()=>{},DispatcherPriority.Background);w.ApplyTemplate();w.UpdateLayout();
                        rows.Add(new{c.Id,viewport=new{width=w.ClientSize.Width,height=w.ClientSize.Height},topbar=Box(workspace.TopBar),sidebar=Box(workspace.Sidebar),strip=Box(workspace.SampleStripBox),content=Box(workspace.Viewport),page=Box(workspace.CurrentPage!),footer=Box(workspace.Footer),workspace.Screen,workspace.English,padding=workspace.Viewport.Padding.ToString(),pageType=workspace.CurrentPage!.GetType().Name});
                        if(c.Id=="connect-ko-1280") {using var bmp=new RenderTargetBitmap(new((int)c.Width*2,(int)c.Height*2),new(192,192));bmp.Render(workspace);bmp.Save(Path.Combine(output,"connected-pages.png"),new PngBitmapEncoderOptions());}
                    }
                    File.WriteAllText(Path.Combine(output,"pageintegration-layout.json"),JsonSerializer.Serialize(new{dpr=w.RenderScaling,rows},new JsonSerializerOptions{WriteIndented=true}));
                    if(native)
                    {
                        if(trialOnly){await PageIntegrationTrialProbe.PrepareNative(workspace,w,output,args.Contains("--trial-actions-only")||args.Contains("--trial-validation-only"),args.Contains("--trial-validation-only"));return;}
                        w.Width=1280;w.Height=900;if(homeOnly)workspace.Navigate("home");else await workspace.DispatchAsync("start");
                        if(args.Contains("--file-window-only")){await workspace.DispatchAsync("sample");workspace.Navigate("save");}
                        await Dispatcher.UIThread.InvokeAsync(()=>{},DispatcherPriority.Background);w.UpdateLayout();
                        int snapshot=0;
                        void Record()
                        {
                            var focused=w.FocusManager?.GetFocusedElement() as Control;
                            var buttons=workspace.GetVisualDescendants().OfType<Control>().Where(c=>!string.IsNullOrEmpty(AutomationProperties.GetAutomationId(c))).Select(c=>new{id=AutomationProperties.GetAutomationId(c),name=AutomationProperties.GetName(c),box=Box(c),enabled=c.IsEffectivelyEnabled,visible=c.IsVisible,focused=c.IsFocused}).ToArray();
                            var data=new{snapshot=++snapshot,workspace.Screen,workspace.English,workspace.Stopped,workspace.Saved,workspace.Connection,formats=workspace.Formats,visited=workspace.Visited.ToArray(),offset=workspace.Viewport.Offset,focus=focused is null?null:AutomationProperties.GetAutomationId(focused),buttons,dpr=w.RenderScaling,pageType=workspace.CurrentPage?.GetType().Name,pageOpacity=workspace.CurrentPage?.Opacity,dialog=workspace.InfoDialog is not null,sampleRun=workspace.SampleRun};
                            var statePath=Path.Combine(output,"pageintegration-actual-state.json");File.WriteAllText(statePath+".tmp",JsonSerializer.Serialize(data,new JsonSerializerOptions{WriteIndented=true}));File.Move(statePath+".tmp",statePath,true);
                        }
                        w.KeyDown+=async(_,e)=>
                        {
                            if(e.Key==Key.F12){e.Handled=true;await Dispatcher.UIThread.InvokeAsync(()=>{},DispatcherPriority.Background);w.UpdateLayout();Record();}
                            if(e.Key is Key.F11 or Key.F10 or Key.F9)
                            {e.Handled=true;workspace.Navigate(e.Key==Key.F11?"connect":e.Key==Key.F10?"review":"save");await Dispatcher.UIThread.InvokeAsync(()=>{},DispatcherPriority.Background);w.UpdateLayout();Record();}
                        };
                        var observer=new DispatcherTimer{Interval=TimeSpan.FromMilliseconds(200)};observer.Tick+=(_,_)=>Record();observer.Start();w.Closed+=(_,_)=>observer.Stop();
                        Record();File.WriteAllText(Path.Combine(output,"pageintegration-actual-ready.json"),JsonSerializer.Serialize(new{phase="ACTUAL_OS_READY",dpr=w.RenderScaling,rows=rows.Count,headless=ReplicaPlatform.IsHeadless,scope="Actual App/MainWindow; F11/F10/F9 probe preparation only; F12 readonly capture"}));
                        return;
                    }
                    if(trialOnly){if(args.Contains("--trial-changed-only"))await PageIntegrationTrialProbe.CheckChanged(workspace,w,output);else await PageIntegrationTrialProbe.Check(workspace,w,output);w.Close();return;}
                    if(homeOnly){await PageIntegrationHomeProbe.Check(workspace,w,output);w.Close();return;}
                    if(args.Length>3&&!modalOnly&&!runOnly&&!exportOnly)PageIntegrationSampleProbe.Check(output,args[3]);
                    if(fileOnly){w.Close();return;}
                    var checks=new List<object>();
                    void Check(string id,bool passed,object state){checks.Add(new{id,passed,state});File.WriteAllText(Path.Combine(output,"pageintegration-checks.json"),JsonSerializer.Serialize(checks));if(!passed)throw new InvalidOperationException(id);}
                    if(exportOnly){PageIntegrationExportProbe.Check(output,args[3]);w.Close();return;}
                    if(runOnly){await PageIntegrationRunProbe.Check(workspace,w,output,args[3]);w.Close();return;}
                    if(!modalOnly){
                    w.Width=1280;w.Height=900;await workspace.DispatchAsync("start");w.UpdateLayout();
                    Check("start-connect",workspace.Screen=="connect"&&workspace.CurrentPage is WebConnectionPage,new{workspace.Screen});
                    var connection=(WebConnectionPage)workspace.CurrentPage!;
                    await Dispatcher.UIThread.InvokeAsync(()=>{},DispatcherPriority.Background);
                    var originalCheck=workspace.Footer.ActionButtons[0];w.Activate();originalCheck.Focus(NavigationMethod.Tab);
                    connection.Source.HostInput.Text="bad.example.invalid";await workspace.DispatchAsync("connect");
                    Check("invalid-stays-connect",!workspace.Connection.Connected&&workspace.Screen=="connect"&&connection.ErrorNotice is not null,new{workspace.Screen,workspace.Connection.Connected});
                    Check("invalid-restores-action-focus",AutomationProperties.GetAutomationId(w.FocusManager?.GetFocusedElement() as Control ?? workspace)=="Footer-connect",new{focus=AutomationProperties.GetAutomationId(w.FocusManager?.GetFocusedElement() as Control ?? workspace)});
                    connection.Source.HostInput.Text="source.example.invalid";await workspace.DispatchAsync("connect");
                    Check("sample-check-footer",workspace.Connection.Connected&&AutomationProperties.GetAutomationId(workspace.Footer.ActionButtons[0])=="Footer-preflight",new{workspace.Connection.Connected});
                    await workspace.DispatchAsync("preflight");Check("preflight-partial-route",workspace.Screen=="capabilities",new{workspace.Screen,page=workspace.CurrentPage!.GetType().Name});
                    await workspace.DispatchAsync("back");Check("back-retains-endpoints",workspace.Screen=="connect"&&workspace.Connection.Connected,new{workspace.Screen});
                    connection=(WebConnectionPage)workspace.CurrentPage!;connection.Source.HostInput.Text="source2.example.invalid";
                    Check("input-invalidates-visits",!workspace.Connection.Connected&&workspace.Visited.SetEquals(new[]{"connect"}),new{connected=workspace.Connection.Connected,visited=workspace.Visited.ToArray()});
                    workspace.Navigate("review");w.UpdateLayout();
                    Check("review-run-disabled",!workspace.Footer.ActionButtons[0].IsEnabled,new{enabled=workspace.Footer.ActionButtons[0].IsEnabled});
                    var review=(WebReviewPage)workspace.CurrentPage!;await Dispatcher.UIThread.InvokeAsync(()=>{},DispatcherPriority.Background);w.Activate();var focused=review.StoppedChoice.Checkbox.Focus(NavigationMethod.Tab);w.KeyPress(Key.Space,RawInputModifiers.None,PhysicalKey.Space," ");w.KeyRelease(Key.Space,RawInputModifiers.None,PhysicalKey.Space," ");
                    await Dispatcher.UIThread.InvokeAsync(()=>{},DispatcherPriority.Background);
                    w.UpdateLayout();Check("review-stopped-propagates",workspace.Stopped&&workspace.Footer.ActionButtons[0].IsEnabled,new{workspace.Stopped,focused,keyboardFocused=review.StoppedChoice.Checkbox.IsFocused,checkedValue=review.StoppedChoice.Checkbox.IsChecked,currentSame=ReferenceEquals(workspace.CurrentPage,review),root=Box(review),checkbox=Box(review.StoppedChoice.Checkbox)});
                    workspace.Navigate("save");w.UpdateLayout();
                    await Dispatcher.UIThread.InvokeAsync(()=>{},DispatcherPriority.Background);
                    var save=(WebSavePage)workspace.CurrentPage!;save.Options.Options.First(x=>x.Format=="json").Checkbox.Focus(NavigationMethod.Tab);w.KeyPress(Key.Space,RawInputModifiers.None,PhysicalKey.Space," ");w.KeyRelease(Key.Space,RawInputModifiers.None,PhysicalKey.Space," ");
                    await Dispatcher.UIThread.InvokeAsync(()=>{},DispatcherPriority.Background);
                    w.UpdateLayout();Check("save-empty-disabled",workspace.Formats.Count==0&&!workspace.Footer.ActionButtons[0].IsEnabled,new{formats=workspace.Formats.ToArray()});
                    workspace.SetLanguage(true);Check("language-retains-formats",workspace.English&&workspace.Formats.Count==0,new{workspace.English});
                    await workspace.DispatchAsync("restart");Check("full-restart-connect",workspace.Screen=="connect"&&!workspace.Connection.Connected&&!workspace.Stopped,new{workspace.Screen});
                    await workspace.DispatchAsync("start");
                    var busyTask=workspace.DispatchAsync("connect");workspace.SetLanguage(!workspace.English);await busyTask;
                    Check("language-during-connect",workspace.Connection.Connected&&!workspace.Connection.Busy&&workspace.CurrentPage is WebConnectionPage current&&current.View.Connected&&!current.View.Busy,new{workspace.Connection.Connected,workspace.Connection.Busy,workspace.English});
                    }
                    workspace.Navigate("connect");await Dispatcher.UIThread.InvokeAsync(()=>{},DispatcherPriority.Background);w.UpdateLayout();
                    var cachedPeer=Avalonia.Automation.Peers.ControlAutomationPeer.CreatePeerForElement(workspace)!;var priorChildren=cachedPeer.GetChildren().Count;
                    var opener=((WebConnectionPage)workspace.CurrentPage!).Advanced.ToggleButton;opener.Focus(NavigationMethod.Tab);workspace.ShowInfo(false,opener);w.UpdateLayout();
                    Check("research-dialog-parent-inert",workspace.InfoDialog is{IsVisible:true} info&&!opener.IsEffectivelyEnabled&&info.CloseButton.IsFocused,new{visible=workspace.InfoDialog?.IsVisible,enabled=opener.IsEffectivelyEnabled,focus=workspace.InfoDialog?.CloseButton.IsFocused});
                    Check("modal-peer-excludes-background",cachedPeer.GetChildren().Count==1,new{children=cachedPeer.GetChildren().Count,priorChildren});
                    await workspace.InfoDialog!.CloseAsync();await Dispatcher.UIThread.InvokeAsync(()=>{},DispatcherPriority.Background);
                    Check("research-close-restores-opener",workspace.InfoDialog is null&&opener.IsFocused&&opener.IsEffectivelyEnabled&&cachedPeer.GetChildren().Count==priorChildren,new{opener.IsFocused,opener.IsEffectivelyEnabled});
                    workspace.ShowInfo(true,opener);w.UpdateLayout();Check("permissions-dialog-parent",workspace.InfoDialog!.Body.DialogTitle==(workspace.English?"To establish visibility":"조회 범위를 확인하려면"),new{title=workspace.InfoDialog!.Body.DialogTitle});
                    await workspace.InfoDialog!.CloseAsync();
                    File.WriteAllText(Path.Combine(output,"pageintegration-native.json"),JsonSerializer.Serialize(new{dpr=w.RenderScaling,rows,checks,scope="Real App/MainWindow preview constructor; incomplete route navigation is not completed page flow"},new JsonSerializerOptions{WriteIndented=true}));
                    w.Close();
                }
                catch(Exception e){File.WriteAllText(Path.Combine(output,"failure.txt"),e.ToString());((Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime)Application.Current!.ApplicationLifetime!).Shutdown(1);}
            },DispatcherPriority.Background);
        });
        ReplicaPlatform.Configure<App>().StartWithClassicDesktopLifetime(["--replica-preview"]);
    }
}
