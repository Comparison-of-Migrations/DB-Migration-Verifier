using System.Text.Json;
using Avalonia;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Input;
using Avalonia.Input.Raw;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Verifier.Desktop;

/// <summary>New Trial parent scope in the real App; LOCK child evidence is reused.</summary>
internal static class PageIntegrationTrialProbe
{
    private static readonly string[] Names=["disabled","inputs-priority","invalid-empty","host-empty","db-empty","same-source","same-target","exact-whitespace","success","stale-success","edit-reset","consent-reset","return-reopen","language"];
    private static readonly string[] BoundaryNames=["bom-valid","nel-valid","bom-host","nel-host"];
    private static readonly JsonSerializerOptions Json=new(){WriteIndented=true};
    private static WebTrialView Seed(ReplicaWorkspace w,string name)
    {
        var v=new WebTrialView(w.English,"trial.example.invalid","migration_test","1","-1",name!="disabled",false,"");
        if(name=="inputs-priority")v=v with{ValidInput=" ",Host=""};
        if(name=="invalid-empty")v=v with{InvalidInput=" "};
        if(name=="host-empty")v=v with{Host=" "};if(name=="db-empty")v=v with{Database=" "};
        if(name=="same-source")v=v with{Host=w.Connection.Source.Host,Database=w.Connection.Source.Database};
        if(name=="same-target")v=v with{Host=w.Connection.Target.Host,Database=w.Connection.Target.Database};
        if(name=="exact-whitespace")v=v with{Host=" "+w.Connection.Source.Host,Database=w.Connection.Source.Database};
        if(name is "stale-success" or "edit-reset" or "return-reopen" or "language")v=v with{Error="owned-stale-error"};
        if(name is "edit-reset" or "consent-reset" or "return-reopen" or "language")v=v with{Ready=true};
        if(name=="bom-valid")v=v with{ValidInput="\uFEFF"};if(name=="nel-valid")v=v with{ValidInput="\u0085"};
        if(name=="bom-host")v=v with{Host="\uFEFF"};if(name=="nel-host")v=v with{Host="\u0085"};
        return v;
    }
    private static string Action(Control c)=>(AutomationProperties.GetAutomationId(c)??"").Replace("Footer-","");
    private static string Focus(Window window)
    {
        var c=window.FocusManager?.GetFocusedElement()as Control;
        if(c is null||c is ReplicaWorkspace)return "BODY";var id=AutomationProperties.GetAutomationId(c)??"";
        if(id=="LanguageButton")return "language";
        if(c is WebActionButton&&id.StartsWith("Footer-"))return Action(c);
        if(id.StartsWith("trial-"))return id;
        if(id=="screen-title")return id;
        if(id.Contains("consent"))return "trial.consent";
        return id;
    }
    /// <summary>Only newly changed parent boundaries; passed layout/flow/child scopes are reused.</summary>
    internal static async Task CheckChanged(ReplicaWorkspace w,Window window,string output)
    {
        var checks=new List<object>();var flows=new List<object>();var neutral=new List<object>();
        void Save(string file,object value)=>File.WriteAllText(Path.Combine(output,file),JsonSerializer.Serialize(value,Json));
        void Assert(string id,bool pass,object state){checks.Add(new{id,pass,state});Save("pageintegration-trial-changed-checks.json",checks);if(!pass)throw new InvalidOperationException(id);}
        async Task Flush(){await Dispatcher.UIThread.InvokeAsync(()=>{},DispatcherPriority.Background);window.UpdateLayout();AvaloniaHeadlessPlatform.ForceRenderTimerTick(1);}
        void Click(Control c){window.UpdateLayout();var p=c.TranslatePoint(new(c.Bounds.Width/2,c.Bounds.Height/2),window)??throw new InvalidOperationException("Attached parent target required");window.MouseMove(p);window.MouseDown(p,MouseButton.Left);window.MouseUp(p,MouseButton.Left);}
        w.ReducedMotion=true;window.Width=1280;window.Height=900;window.Activate();
        foreach(var english in new[]{false,true})foreach(var name in BoundaryNames)
        {
            w.SetLanguage(english);w.Navigate("trial");w.SetTrial(Seed(w,name));await Flush();Click(w.Footer.ActionButtons.Single());await Flush();
            var id=(english?"en-":"ko-")+name;var expected=name.StartsWith("nel-");
            Assert(id+"-trim",w.Trial.Ready==expected&&w.Trial.Consent&&w.Screen=="trial"&&(expected?w.Trial.Error=="":w.Trial.Error.Length>0),new{w.Trial});
            object? extra=null;if(w.Trial.Ready){var toast=w.Children.OfType<WebSurfaceBox>().Select(s=>s.Child).OfType<WebSystemTextBlock>().LastOrDefault()?.Text;extra=new{toast};}
            flows.Add(Snapshot(w,window,id,extra));Save("pageintegration-trial-validation-flows.json",new{flows});
        }
        foreach(var english in new[]{false,true})
        {
            w.SetLanguage(english);w.Navigate("trial");w.SetTrial(Seed(w,"disabled"));await Flush();var page=w.CurrentPage;var before=w.Trial;Click(w.Footer.ActionButtons.Single());await Flush();
            var id=(english?"en-":"ko-")+"disabled";
            Assert(id+"-announcer",w.Announcement==((WebTrialPage)w.CurrentPage!).Heading.TitleText.Text&&AutomationProperties.GetLiveSetting(w.AnnouncementRegion)==AutomationLiveSetting.Polite&&w.AnnouncementRegion.Bounds.Width>0&&w.AnnouncementRegion.Bounds.Height>0,new{w.Announcement,box=w.AnnouncementRegion.Bounds.ToString(),live=AutomationProperties.GetLiveSetting(w.AnnouncementRegion).ToString()});
            Assert(id+"-body",ReferenceEquals(window.FocusManager?.GetFocusedElement(),w)&&!KeyboardNavigation.GetIsTabStop(w)&&w.Trial==before&&ReferenceEquals(page,w.CurrentPage),new{state=Snapshot(w,window,id),rawFocus=AutomationProperties.GetAutomationId(w)});
            foreach(var key in new[]{Key.Space,Key.Enter}){window.KeyPress(key,RawInputModifiers.None,key==Key.Space?PhysicalKey.Space:PhysicalKey.Enter,key==Key.Space?" ":"Enter");window.KeyRelease(key,RawInputModifiers.None,key==Key.Space?PhysicalKey.Space:PhysicalKey.Enter,key==Key.Space?" ":"Enter");await Flush();Assert(id+"-neutral-"+key,w.Trial==before&&ReferenceEquals(page,w.CurrentPage)&&ReferenceEquals(window.FocusManager?.GetFocusedElement(),w),Snapshot(w,window,id));}
            neutral.Add(new{id,state=Snapshot(w,window,id),rawFocus=AutomationProperties.GetAutomationId(w),mapping="Managed ReplicaWorkspace neutral focus corresponds to source BODY; raw unnamed ID is preserved."});Save("pageintegration-trial-neutral.json",neutral);
        }
    }
    private static object Snapshot(ReplicaWorkspace w,Window window,string id,object? extra=null)
    {
        var p=w.Footer.ActionButtons.SingleOrDefault();var v=w.Trial;
        return new{id,screen=w.Screen,english=w.English,host=v.Host,database=v.Database,validInput=v.ValidInput,invalidInput=v.InvalidInput,consent=v.Consent,ready=v.Ready,error=v.Error,primary=p is null?null:new{action=Action(p),label=p.Content as string,enabled=p.IsEffectivelyEnabled},focus=Focus(window),announcement=w.Announcement,extra};
    }
    internal static async Task Check(ReplicaWorkspace w,Window window,string output)
    {
        if(Environment.GetEnvironmentVariable("DBMV_ADVANCED_RESIZE_ONLY")=="1"){await PageIntegrationConnectionAdvancedResizeProbe.Check(w,window,output);return;}
        if(Environment.GetEnvironmentVariable("DBMV_CONNECTION_RESIZE_ONLY")=="1"){await PageIntegrationConnectionResizeProbe.Check(w,window,output);return;}
        if(Environment.GetEnvironmentVariable("DBMV_TRIAL_STOPPED_ONLY")=="1"){await PageIntegrationTrialResizeProbe.CheckStopped(w,window,output);return;}
        if(Environment.GetEnvironmentVariable("DBMV_TRIAL_RESIZE_ONLY")=="1"){await PageIntegrationTrialResizeProbe.Check(w,window,output);return;}
        if(Environment.GetEnvironmentVariable("DBMV_TRIAL_OBSERVER_SMOKE") is "success" or "validation"){await CheckObserver(w,window,output);return;}
        if(Environment.GetEnvironmentVariable("DBMV_TRIAL_CHANGED")=="1"){await CheckChanged(w,window,output);return;}
        var checks=new List<object>();var flows=new List<object>();var rows=new List<object>();
        void Save(string file,object value)=>File.WriteAllText(Path.Combine(output,file),JsonSerializer.Serialize(value,Json));
        void Assert(string id,bool pass,object state){checks.Add(new{id,pass,state});Save("pageintegration-trial-checks.json",checks);if(!pass)throw new InvalidOperationException(id);}
        async Task Flush(){await Dispatcher.UIThread.InvokeAsync(()=>{},DispatcherPriority.Background);window.UpdateLayout();AvaloniaHeadlessPlatform.ForceRenderTimerTick(1);}
        void Click(Control c){c.BringIntoView();window.UpdateLayout();var p=c.TranslatePoint(new(c.Bounds.Width/2,c.Bounds.Height/2),window)??throw new InvalidOperationException("Attached target required");window.MouseMove(p);window.MouseDown(p,MouseButton.Left);window.MouseUp(p,MouseButton.Left);}
        object Box(Control c){var p=c.TranslatePoint(default,w)??throw new InvalidOperationException("Attached geometry required");return new{x=p.X,y=p.Y,width=c.Bounds.Width,height=c.Bounds.Height};}
        object Button(WebActionButton p)=>new{action=Action(p),label=p.Content as string,enabled=p.IsEffectivelyEnabled,box=Box(p)};
        w.ReducedMotion=true;window.Activate();window.Height=900;
        foreach(var english in new[]{false,true})foreach(var width in new[]{960,1180,1280,1440})foreach(var state in new[]{"default","consent","ready"})
        {
            window.Width=width;w.SetLanguage(english);w.Navigate("trial");w.SetTrial(new(english,"trial.example.invalid","migration_test","1","-1",state!="default",state=="ready",""));await Flush();
            Assert($"trial-connected-{english}-{width}-{state}",w.CurrentPage is WebTrialPage,new{type=w.CurrentPage?.GetType().Name});
            rows.Add(new{id=$"trial-{(english?"en":"ko")}-{width}-{state}",viewport=new{width=window.ClientSize.Width,height=window.ClientSize.Height},topbar=Box(w.TopBar),sidebar=Box(w.Sidebar),strip=Box(w.SampleStripBox),content=Box(w.Viewport),page=Box(w.CurrentPage!),footer=Box(w.Footer),hint=new{text=w.Footer.Hint.Text,box=Box(w.Footer.Hint)},back=Button(w.Footer.BackButton!),primary=Button(w.Footer.ActionButtons.Single())});
        }
        Save("pageintegration-trial-layout.json",new{dpr=window.RenderScaling,rows});
        if(Environment.GetEnvironmentVariable("DBMV_TRIAL_LAYOUT_ONLY")=="1")return;
        window.Width=1280;
        foreach(var english in new[]{false,true})foreach(var name in Names)
        {
            w.SetLanguage(english);w.Navigate("trial");w.SetTrial(Seed(w,name));await Flush();
            var page=(WebTrialPage)w.CurrentPage!;var id=(english?"en":"ko")+"-"+name;object? extra=null;
            if(name=="edit-reset")
            {
                var field=page.Inputs[2];var primary=w.Footer.ActionButtons.Single();field.BringIntoView();field.Focus(NavigationMethod.Tab);field.Text="owned-parent-value";field.CaretIndex=field.Text.Length;await Flush();
                extra=new{pageRetained=ReferenceEquals(page,w.CurrentPage),editorRetained=ReferenceEquals(field,((WebTrialPage)w.CurrentPage!).Inputs[2]),primaryRetained=ReferenceEquals(primary,w.Footer.ActionButtons.Single()),readyNotice=page.ReadyNotice is not null};
                Assert(id+"-identity",ReferenceEquals(page,w.CurrentPage)&&ReferenceEquals(field,page.Inputs[2])&&ReferenceEquals(primary,w.Footer.ActionButtons.Single())&&field.IsFocused&&!w.Trial.Ready&&!w.Trial.Consent&&page.ReadyNotice is null&&w.Trial.Error=="owned-stale-error",extra);
            }
            else if(name=="consent-reset"){Click(page.ConsentChoice.Checkbox);await Flush();Assert(id+"-ready-reset",!w.Trial.Ready&&!w.Trial.Consent,new{w.Trial.Ready,w.Trial.Consent});}
            else if(name=="return-reopen")
            {
                Click(w.Footer.ActionButtons.Single());await Flush();var returned=w.Screen=="select";var returnError=w.Trial.Error;var returnOffset=w.Viewport.Offset;await w.DispatchAsync("trial");await Flush();
                extra=new{returned,returnError,returnOffset,reopenOffset=w.Viewport.Offset};Assert(id+"-state",returned&&returnError==""&&w.Trial.Ready&&w.Trial.Consent&&((WebTrialPage)w.CurrentPage!).Heading.FocusTarget.IsFocused&&w.Viewport.Offset==0,extra);
            }
            else if(name=="language"){Click(w.TopBar.LanguageButton);await Flush();Assert(id+"-preserved",w.English!=english&&w.Trial.Ready&&w.Trial.Consent&&w.Trial.Error=="owned-stale-error"&&w.TopBar.LanguageButton.IsFocused,new{w.English,w.Trial});}
            else
            {
                var before=w.Trial;if(name=="disabled")await w.DispatchAsync("trial-check");else Click(w.Footer.ActionButtons.Single());await Flush();
                if(name=="disabled")Assert(id+"-ignored",w.Trial==before&&ReferenceEquals(page,w.CurrentPage),new{w.Trial});
                if(w.Trial.Ready){var toast=w.Children.OfType<WebSurfaceBox>().Select(s=>s.Child).OfType<WebSystemTextBlock>().LastOrDefault()?.Text;extra=new{toast};}
            }
            flows.Add(Snapshot(w,window,id,extra));Save("pageintegration-trial-flows.json",new{flows});
        }
        foreach(var english in new[]{false,true})
        {
            w.SetLanguage(english);w.SetScope(["trial"],w.Tables);w.Navigate("select");w.SetTrial(new(english,"trial.example.invalid","migration_test","1","-1",false,false,""));await w.DispatchAsync("review");await Flush();Assert($"review-unprepared-{english}",w.Screen=="select",new{w.Screen});
            w.SetTrial(w.Trial with{Ready=true,Consent=true});await w.DispatchAsync("review");await Flush();Assert($"review-prepared-{english}",w.Screen=="review",new{w.Screen});
        }
    }
    /// <summary>Actual OS input remains external; F8/F7 are explicitly setup-only utilities.</summary>
    private static async Task CheckObserver(ReplicaWorkspace w,Window window,string output)
    {
        var validation=Environment.GetEnvironmentVariable("DBMV_TRIAL_OBSERVER_SMOKE")=="validation";
        await PrepareNative(w,window,output,true,validation);
        window.UpdateLayout();var target=w.Footer.ActionButtons.Single();var p=target.TranslatePoint(new(target.Bounds.Width/2,target.Bounds.Height/2),window)??throw new InvalidOperationException("Attached observer target required");
        var before=JsonDocument.Parse(File.ReadAllText(Path.Combine(output,"pageintegration-trial-actual-state.json"))).RootElement.GetProperty("snapshot").GetInt32();
        window.MouseMove(p);window.MouseDown(p,MouseButton.Left);window.MouseUp(p,MouseButton.Left);
        await Task.Delay(250);await Dispatcher.UIThread.InvokeAsync(()=>{},DispatcherPriority.Background);window.UpdateLayout();
        var observed=JsonDocument.Parse(File.ReadAllText(Path.Combine(output,"pageintegration-trial-actual-state.json"))).RootElement;
        var pass=observed.GetProperty("snapshot").GetInt32()>before&&observed.GetProperty("setupReady").GetBoolean()&&observed.GetProperty("trace").GetArrayLength()>=2&&!File.Exists(Path.Combine(output,"pageintegration-trial-observer-error.json"))&&!File.Exists(Path.Combine(output,"pageintegration-trial-setup-error.json"));
        pass&=validation?observed.GetProperty("state").GetProperty("error").GetString()=="Confirm trial inputs and expected outcomes.":observed.GetProperty("state").GetProperty("ready").GetBoolean();
        File.WriteAllText(Path.Combine(output,"pageintegration-trial-observer-smoke.json"),JsonSerializer.Serialize(new{phase=pass?"HEADLESS_INSTRUMENTATION_PASSED":"FAILED",validation,before,observed,scope="New observer setup/timer/after-input boundary only; no actualOS evidence"},Json));
        if(!pass)throw new InvalidOperationException("Observer instrumentation smoke failed");
    }

    internal static async Task PrepareNative(ReplicaWorkspace w,Window window,string output,bool skipLayout=false,bool validationOnly=false)
    {
        if(Environment.GetEnvironmentVariable("DBMV_ADVANCED_RESIZE_ONLY")=="1"){await PageIntegrationConnectionAdvancedResizeProbe.PrepareNative(w,window,output);return;}
        if(Environment.GetEnvironmentVariable("DBMV_CONNECTION_RESIZE_ONLY")=="1"){await PageIntegrationConnectionResizeProbe.PrepareNative(w,window,output);return;}
        if(Environment.GetEnvironmentVariable("DBMV_TRIAL_RESIZE_ONLY")=="1"){await PageIntegrationTrialResizeProbe.PrepareNative(w,window,output);return;}
        w.ReducedMotion=true;window.Height=900;
        object Box(Control c){var p=c.TranslatePoint(default,w)??throw new InvalidOperationException("Attached native target required");return new{x=p.X,y=p.Y,width=c.Bounds.Width,height=c.Bounds.Height};}
        object Button(WebActionButton p)=>new{action=Action(p),label=p.Content as string,enabled=p.IsEffectivelyEnabled,box=Box(p)};
        var rows=new List<object>();
        if(!skipLayout) foreach(var english in new[]{false,true})foreach(var width in new[]{960,1180,1280,1440})foreach(var state in new[]{"default","consent","ready"})
        {
            window.Width=width;w.SetLanguage(english);w.Navigate("trial");w.SetTrial(new(english,"trial.example.invalid","migration_test","1","-1",state!="default",state=="ready",""));
            await Dispatcher.UIThread.InvokeAsync(()=>{},DispatcherPriority.Background);window.UpdateLayout();
            rows.Add(new{id=$"trial-{(english?"en":"ko")}-{width}-{state}",viewport=new{width=window.ClientSize.Width,height=window.ClientSize.Height},topbar=Box(w.TopBar),sidebar=Box(w.Sidebar),strip=Box(w.SampleStripBox),content=Box(w.Viewport),page=Box(w.CurrentPage!),footer=Box(w.Footer),hint=new{text=w.Footer.Hint.Text,box=Box(w.Footer.Hint)},back=Button(w.Footer.BackButton!),primary=Button(w.Footer.ActionButtons.Single())});
        }
        if(!skipLayout)File.WriteAllText(Path.Combine(output,"pageintegration-trial-layout.json"),JsonSerializer.Serialize(new{dpr=window.RenderScaling,rows},Json));
        var names=validationOnly?BoundaryNames:Names;
        window.Width=1280;var firstIndex=int.TryParse(Environment.GetEnvironmentVariable("DBMV_TRIAL_NATIVE_INDEX"),out var configuredIndex)?configuredIndex:0;if(firstIndex<0||firstIndex>=names.Length*2)throw new InvalidOperationException("Exact owned native seed index required");var index=firstIndex-1;int revision=0,snapshot=0;bool setupReady=false;string observerStage="not-started",setupStage="not-started";WebTrialPage? seededPage=null;WebTextInput? seededEditor=null;WebActionButton? seededPrimary=null;
        var trace=new List<object>();
        void Record()
        {
            if(!setupReady)return;
            try
            {
            observerStage="controls";
            var controls=w.GetVisualDescendants().OfType<Control>().Where(c=>!string.IsNullOrEmpty(AutomationProperties.GetAutomationId(c))).Select(c=>new{id=AutomationProperties.GetAutomationId(c),name=AutomationProperties.GetName(c),box=Box(c),enabled=c.IsEffectivelyEnabled,visible=c.IsEffectivelyVisible,focused=c.IsFocused}).ToArray();
            observerStage="live-peer";var current=w.CurrentPage as WebTrialPage;
            var livePeer=Avalonia.Automation.Peers.ControlAutomationPeer.CreatePeerForElement(w.AnnouncementRegion)!;
            var announcementPeer=new{name=livePeer.GetName(),type=livePeer.GetAutomationControlType().ToString(),live=livePeer.GetLiveSetting().ToString(),control=livePeer.IsControlElement(),content=livePeer.IsContentElement(),children=livePeer.GetChildren().Count,box=Box(w.AnnouncementRegion)};
            observerStage="snapshot";var state=new{snapshot=++snapshot,revision,index,setupReady,caseId=index<0?"":(index<names.Length?"ko-":"en-")+names[index%names.Length],state=Snapshot(w,window,"actual"),announcementPeer,rawFocus=window.FocusManager?.GetFocusedElement()is Control focused?AutomationProperties.GetAutomationId(focused):null,rawFocusType=window.FocusManager?.GetFocusedElement()?.GetType().FullName,rawFocusPath=window.FocusManager?.GetFocusedElement()is Control active?active.GetVisualAncestors().OfType<Control>().Select(c=>new{type=c.GetType().FullName,id=AutomationProperties.GetAutomationId(c)}).ToArray():[],offset=w.Viewport.Offset,documentOffset=new{x=w.DocumentHorizontal?.Offset??0,y=w.DocumentVertical?.Offset??0},controls,pageRetained=ReferenceEquals(seededPage,w.CurrentPage),editorRetained=current is not null&&ReferenceEquals(seededEditor,current.Inputs[2]),primaryRetained=w.Footer.ActionButtons.Count==1&&ReferenceEquals(seededPrimary,w.Footer.ActionButtons[0]),readyNotice=current?.ReadyNotice is not null,trace,dpr=window.RenderScaling,headless=ReplicaPlatform.IsHeadless};
            observerStage="serialize-and-atomic-save";var file=Path.Combine(output,"pageintegration-trial-actual-state.json");File.WriteAllText(file+".tmp",JsonSerializer.Serialize(state,Json));File.Move(file+".tmp",file,true);observerStage="complete";
            }
            catch(Exception ex){File.WriteAllText(Path.Combine(output,"pageintegration-trial-observer-error.json"),JsonSerializer.Serialize(new{phase="UNVERIFIED_OBSERVER_FAILURE",index,revision,snapshot,setupReady,observerStage,setupStage,error=ex.ToString()},Json));}
        }
        async Task Next()
        {
            if(index>=names.Length*2-1)return;setupReady=false;++index;++revision;setupStage="language";w.SetLanguage(index>=names.Length);setupStage="navigate";w.Navigate("trial");setupStage="seed";w.SetTrial(Seed(w,names[index%names.Length]));setupStage="await-layout";
            await Dispatcher.UIThread.InvokeAsync(()=>{},DispatcherPriority.Background);window.UpdateLayout();setupStage="references";seededPage=(WebTrialPage)w.CurrentPage!;seededEditor=seededPage.Inputs[2];seededPrimary=w.Footer.ActionButtons.Single();if(names[index%names.Length]=="edit-reset")seededEditor.BringIntoView();if(names[index%names.Length]=="consent-reset")seededPage.ConsentChoice.Checkbox.BringIntoView();window.UpdateLayout();trace.Clear();setupStage="complete";setupReady=true;Record();
        }
        window.AddHandler(InputElement.PointerPressedEvent,(_,e)=>trace.Add(new{kind="down",revision,source=e.Source?.GetType().Name,point=e.GetPosition(window).ToString(),focus=Focus(window)}),Avalonia.Interactivity.RoutingStrategies.Tunnel,true);
        window.AddHandler(InputElement.PointerReleasedEvent,(_,e)=>trace.Add(new{kind="up",revision,source=e.Source?.GetType().Name,focus=Focus(window)}),Avalonia.Interactivity.RoutingStrategies.Bubble,true);
        window.KeyDown+=async(_,e)=>{if(e.Key==Key.F8){e.Handled=true;try{await Next();}catch(Exception ex){setupReady=false;File.WriteAllText(Path.Combine(output,"pageintegration-trial-setup-error.json"),JsonSerializer.Serialize(new{phase="UNVERIFIED_SETUP_FAILURE",index,revision,snapshot,setupStage,error=ex.ToString()},Json));}}else if(e.Key==Key.F7){e.Handled=true;await w.DispatchAsync("trial");await Dispatcher.UIThread.InvokeAsync(()=>{},DispatcherPriority.Background);window.UpdateLayout();Record();}};
        var observer=new DispatcherTimer{Interval=TimeSpan.FromMilliseconds(100)};observer.Tick+=(_,_)=>Record();observer.Start();window.Closed+=(_,_)=>observer.Stop();
        await Next();File.WriteAllText(Path.Combine(output,"pageintegration-trial-actual-ready.json"),JsonSerializer.Serialize(new{phase="ACTUAL_OS_READY",firstIndex,cases=names.Length*2-firstIndex,scope="Parent24 recorded; actual footer/field/language input external. F8 seeds next case, F7 API reopen is setup only; not user flow evidence."},Json));
    }

}
