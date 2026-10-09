using System.Text;
using System.Text.Json;
using Avalonia;
using Avalonia.Automation;
using Avalonia.Automation.Peers;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Input;
using Avalonia.Input.Raw;
using Avalonia.Media.Imaging;
using Avalonia.Media.TextFormatting;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Verifier.Desktop;

/// <summary>Only new Results application wiring. The locked child matrices are reused.</summary>
internal static class PageIntegrationResultsProbe
{
    internal static async Task Check(ReplicaWorkspace workspace,Window window,string output,string sourceOracle)
    {
        using var oracle=JsonDocument.Parse(File.ReadAllText(sourceOracle));
        var records=new List<object>();var checks=new List<object>();
        void Assert(string id,bool passed,object state)
        {
            checks.Add(new{id,passed,state});File.WriteAllText(Path.Combine(output,"pageintegrationresults-checks.json"),JsonSerializer.Serialize(checks,new JsonSerializerOptions{WriteIndented=true}));
            if(!passed)throw new InvalidOperationException(id);
        }
        async Task Flush(bool settle=true)
        {
            await Dispatcher.UIThread.InvokeAsync(()=>{},DispatcherPriority.Loaded);window.UpdateLayout();
            if(settle)await Task.Delay(360);
            AvaloniaHeadlessPlatform.ForceRenderTimerTick(1);window.UpdateLayout();
        }
        void Activate(Control button)
        {
            if(!button.IsEffectivelyEnabled)throw new InvalidOperationException("Enabled original action required");
            var scroll=workspace.Viewport.Offset;button.Focus(NavigationMethod.Tab);workspace.Viewport.Offset=scroll;
            window.KeyPress(Key.Space,RawInputModifiers.None,PhysicalKey.Space," ");window.KeyRelease(Key.Space,RawInputModifiers.None,PhysicalKey.Space," ");
        }
        void Escape(){window.KeyPress(Key.Escape,RawInputModifiers.None,PhysicalKey.Escape,"");window.KeyRelease(Key.Escape,RawInputModifiers.None,PhysicalKey.Escape,"");}
        object Box(Control? c)
        {
            if(c is null)return null!;var p=c.TranslatePoint(default,workspace)??default;
            return new{x=p.X,y=p.Y,width=c.Bounds.Width,height=c.Bounds.Height};
        }
        string? Identity(Control? focused)
        {
            if(focused is null)return null;
            if(workspace.CurrentPage is WebResultsPage p)
            {
                if(ReferenceEquals(focused,p.Heading.FocusTarget))return "screen-title";
                if(ReferenceEquals(focused,p.TrialButton))return "detail:trial";
                if(ReferenceEquals(focused,p.ResearchButton))return "research";
                var row=p.Chart?.Rows.FirstOrDefault(x=>ReferenceEquals(x,focused));if(row is not null)return "detail:"+row.CategoryKey;
            }
            if(workspace.CurrentPage is WebSavePage save&&ReferenceEquals(focused,save.Heading.FocusTarget))return "screen-title";
            var id=AutomationProperties.GetAutomationId(focused);
            if(id?.StartsWith("Results-filter-")==true)return "filter:"+id[15..];
            if(id?.StartsWith("Footer-")==true)return id[7..];
            if(id is "ResultDetails-close" or "ResultDetails-footer-close" or "InfoDialog-close" or "InfoDialog-footer-close")return "modal-close";
            return id;
        }
        object? Modal()
        {
            var detail=workspace.GetVisualDescendants().OfType<WebResultDetails>().FirstOrDefault(x=>x.IsVisible);
            if(detail is not null)
            {
                var key=detail.Body.CleanupNotice is not null?"trial":detail.TitleText.Text switch{"데이터" or "Data"=>"data","구조·제약" or "Schema & constraints"=>"schema","사용자·권한" or "Users & permissions"=>"access",_=>"objects"};
                var notices=new[]{detail.Body.ScopeNotice,detail.Body.CleanupNotice}.Where(x=>x is not null).Select(x=>new{title=x!.Title,text=x.Text}).ToArray();
                return new{type="detail",payload=key,title=detail.TitleText.Text,checks=detail.Body.CheckList.Items.Select(x=>x.Text.Text).ToArray(),notices,open=true};
            }
            return workspace.InfoDialog is{} info?new{type="research",payload="",title=info.TitleText.Text,checks=Array.Empty<string>(),notices=Array.Empty<object>(),open=true}:null;
        }
        JsonElement Report(ReplicaSampleRun run)
        {
            var findings=workspace.SafeFindingProvider?.Invoke(run,workspace.English)??throw new InvalidOperationException("Root's unfiltered safe finding provider must be connected");
            return JsonSerializer.Deserialize<JsonElement>((run with{Id="UI-FLOWRESULTS"}).WriteJson(findings));
        }
        foreach(var context in oracle.RootElement.GetProperty("rows").EnumerateArray())
        {
            var id=context.GetProperty("id").GetString()!;var viewport=context.GetProperty("viewport");
            window.Width=viewport.GetProperty("width").GetDouble();window.Height=viewport.GetProperty("height").GetDouble();
            workspace.ReducedMotion=context.GetProperty("reduced").GetBoolean();await workspace.DispatchAsync("start");workspace.SetLanguage(id.StartsWith("en-"));
            workspace.SetScope(ReplicaSampleRun.CategoryKeys,ReplicaSampleRun.AllowedTables);workspace.Navigate("home");await Flush();
            var rows=new List<object>();
            void State(string label)
            {
                window.UpdateLayout();var p=workspace.CurrentPage as WebResultsPage;var heading=p?.Heading??(workspace.CurrentPage as WebSavePage)?.Heading;
                var focus=Identity(window.FocusManager?.GetFocusedElement() as Control);
                rows.Add(new{label,screen=workspace.Screen,english=workspace.English,announcement=workspace.Announcement,filter=p?.Filter,focus,scroll=workspace.Viewport.Offset,visited=workspace.Visited.ToArray(),title=heading?.TitleText.Text,articleIds=p?.Articles.Select(a=>a.IdentifierText.Text).ToArray()??[],pageOpacity=p?.Opacity,modal=Modal(),report=workspace.SampleRun is{}run?Report(run):(object?)null,boxes=new{content=Box(workspace.Viewport),page=Box(workspace.CurrentPage),footer=Box(workspace.Footer),heading=Box(heading),filter=Box(p?.FilterRow),findings=Box(p?.FindingList)},footer=new[]{workspace.Footer.BackButton}.Concat(workspace.Footer.ActionButtons).Where(x=>x is not null).Select(b=>new{action=AutomationProperties.GetAutomationId(b!)?[7..],text=b!.Content,disabled=!b.IsEnabled,box=Box(b)}).ToArray(),backgroundEnabled=workspace.CurrentPage?.IsEffectivelyEnabled,peerChildren=ControlAutomationPeer.CreatePeerForElement(workspace)!.GetChildren().Count});
            }
            if(oracle.RootElement.TryGetProperty("targetedRootBoundary",out var rootOnly)&&rootOnly.GetBoolean())
            {
                await workspace.DispatchAsync("sample");await Flush();
                var nodes=new List<object>();
                void PeerNodes(AutomationPeer peer,int depth)
                {
                    if(depth>4||nodes.Count>=20)throw new InvalidOperationException("Bounded announcement tree required");
                    var children=peer.GetChildren();nodes.Add(new{type="ControlType."+peer.GetAutomationControlType(),name=peer.GetName(),control=peer.IsControlElement(),content=peer.IsContentElement(),children=children.Count});
                    foreach(var child in children)PeerNodes(child,depth+1);
                }
                var announcementPeer=ControlAutomationPeer.CreatePeerForElement(workspace.AnnouncementRegion)!;PeerNodes(announcementPeer,0);
                var announcement=workspace.Announcement;var live=announcementPeer.GetLiveSetting().ToString();
                var modalRows=oracle.RootElement.TryGetProperty("rootBoundaryPeerOnly",out var boundaryPeerOnly)&&boundaryPeerOnly.GetBoolean()?new List<object>():await CaptureRootBoundaryModals(workspace,window,id);
                records.Add(new{id,announcement,live,peerNodes=nodes,modalRows,scope="Narrow source-derived root marker+modal boxes only; prior callbacks30/parent4 retained"});
                File.WriteAllText(Path.Combine(output,"pageintegrationresults-root-boundary.json"),JsonSerializer.Serialize(records,new JsonSerializerOptions{WriteIndented=true}));continue;
            }
            if(oracle.RootElement.TryGetProperty("targetedEntryObserver",out var entryOnly)&&entryOnly.GetBoolean())
            {
                var started=System.Diagnostics.Stopwatch.StartNew();var samples=new List<object>();var observed=new HashSet<WebResultsPage>();
                void Observe(WebResultsPage page,string stage)=>samples.Add(new{stage,elapsedMs=started.Elapsed.TotalMilliseconds,opacity=page.Opacity,current=ReferenceEquals(workspace.CurrentPage,page)});
                void WatchCurrent()
                {
                    if(workspace.CurrentPage is not WebResultsPage page||!observed.Add(page))return;
                    Observe(page,"current-page");page.PropertyChanged+=(_,e)=>{if(e.Property==Visual.OpacityProperty)Observe(page,"opacity-changed");};
                }
                EventHandler layout=(_,_)=>WatchCurrent();window.LayoutUpdated+=layout;
                try
                {
                    Activate(((WebHomePage)workspace.CurrentPage!).SampleButton);WatchCurrent();
                    await Dispatcher.UIThread.InvokeAsync(()=>{},DispatcherPriority.Loaded);WatchCurrent();window.UpdateLayout();
                    if(workspace.CurrentPage is WebResultsPage p)Observe(p,"after-loaded-before-render-tick");
                    AvaloniaHeadlessPlatform.ForceRenderTimerTick(1);window.UpdateLayout();await Task.Delay(360);
                    if(workspace.CurrentPage is WebResultsPage settled)Observe(settled,"settled");
                    records.Add(new{id,samples,scope="Narrow public Opacity observer around trusted headless Space/Loaded; ordinary full68 retained"});
                    File.WriteAllText(Path.Combine(output,"pageintegrationresults-entry-trace.json"),JsonSerializer.Serialize(records,new JsonSerializerOptions{WriteIndented=true}));
                }
                finally{window.LayoutUpdated-=layout;}
                continue;
            }
            if(oracle.RootElement.TryGetProperty("targetedAnnouncementPeer",out var peerOnly)&&peerOnly.GetBoolean())
            {
                await workspace.DispatchAsync("sample");await Flush();
                var peer=ControlAutomationPeer.CreatePeerForElement(workspace.AnnouncementRegion)!;
                var value=new{id,name=peer.GetName(),type=peer.GetAutomationControlType().ToString(),live=peer.GetLiveSetting().ToString(),control=peer.IsControlElement(),content=peer.IsContentElement(),children=peer.GetChildren().Count,box=Box(workspace.AnnouncementRegion)};
                Assert(id+"-announcement-peer",peer.GetName()==workspace.Announcement&&peer.GetAutomationControlType()==AutomationControlType.Text&&peer.GetLiveSetting()==AutomationLiveSetting.Polite&&peer.IsControlElement()&&peer.IsContentElement()&&peer.GetChildren().Count==0,value);
                records.Add(value);File.WriteAllText(Path.Combine(output,"pageintegrationresults-announcement-peer.json"),JsonSerializer.Serialize(records,new JsonSerializerOptions{WriteIndented=true}));continue;
            }
            if(oracle.RootElement.TryGetProperty("targetedSavedObserver",out var savedOnly)&&savedOnly.GetBoolean())
            {
                await workspace.DispatchAsync("sample");await Flush();workspace.Navigate("save");await Flush();workspace.SetSaved(true);await Flush();State("download-requested");
                var savedRun=workspace.SampleRun!;var safe=workspace.SafeFindingProvider!(savedRun,workspace.English);
                var savedExports=new[]{"json","csv","html"}.Select(format=>new{name="UI-FLOWRESULTS-sample."+format,body=Encoding.UTF8.GetString(ReplicaSampleExport.Create(savedRun with{Id="UI-FLOWRESULTS"},format,workspace.English,safe))}).ToArray();
                records.Add(new{id,viewport=new{width=window.ClientSize.Width,height=window.ClientSize.Height},rows,exports=savedExports});
                File.WriteAllText(Path.Combine(output,"pageintegrationresults-native.json"),JsonSerializer.Serialize(new{dpr=window.RenderScaling,rows=records,scope="Targeted saved observer4 after deferred layout/focus; prior64 retained"},new JsonSerializerOptions{WriteIndented=true}));continue;
            }
            Activate(((WebHomePage)workspace.CurrentPage!).SampleButton);await Flush(false);
            Assert(id+"-results-connected",workspace.CurrentPage is WebResultsPage,new{workspace.Screen,type=workspace.CurrentPage?.GetType().Name});
            State("sample-go-start");await Flush();State("sample-go");
            var page=(WebResultsPage)workspace.CurrentPage!;
            Assert(id+"-go-heading-scroll",page.Heading.FocusTarget.IsFocused&&workspace.Viewport.Offset==0,new{page.Heading.FocusTarget.IsFocused,workspace.Viewport.Offset});
            workspace.Viewport.Offset=100;Activate(page.FilterButtons[2]);await Flush();State("filter-unknown");
            page=(WebResultsPage)workspace.CurrentPage!;var filteredRun=workspace.SampleRun;
            workspace.SetLanguage(workspace.English);await Flush();State("ordinary-filter-render");
            Activate(workspace.TopBar.LanguageButton);await Flush();State("language-filtered");
            page=(WebResultsPage)workspace.CurrentPage!;workspace.Viewport.Offset=0;Activate(page.Chart!.Rows.First(r=>r.CategoryKey=="data"));await Flush();State("detail-data-open");
            Assert(id+"-detail-parent-isolation",Modal() is not null&&!page.IsEffectivelyEnabled&&ControlAutomationPeer.CreatePeerForElement(workspace)!.GetChildren().Count==1,new{page.IsEffectivelyEnabled,peerChildren=ControlAutomationPeer.CreatePeerForElement(workspace)!.GetChildren().Count});
            Escape();await Flush();State("detail-data-close");
            page=(WebResultsPage)workspace.CurrentPage!;Activate(page.TrialButton!);await Flush();State("detail-trial-open");Escape();await Flush();State("detail-trial-close");
            page=(WebResultsPage)workspace.CurrentPage!;Activate(page.ResearchButton);await Flush();State("research-open");Escape();await Flush();State("research-close");
            page=(WebResultsPage)workspace.CurrentPage!;Activate(page.Chart!.Rows.First(r=>r.CategoryKey=="data"));await Flush();workspace.SetLanguage(!workspace.English);await Flush();State("modal-language-recreated");Escape();await Flush();State("modal-language-close");
            Activate(workspace.Footer.ActionButtons.First(b=>AutomationProperties.GetAutomationId(b)=="Footer-save"));await Flush();State("save-go");Activate(workspace.Footer.BackButton!);await Flush();State("save-back-results");
            page=(WebResultsPage)workspace.CurrentPage!;Activate(page.FilterButtons[2]);await Flush();
            Activate(workspace.Footer.ActionButtons.First(b=>AutomationProperties.GetAutomationId(b)=="Footer-save"));await Flush();
            var run=workspace.SampleRun!;var findings=workspace.SafeFindingProvider!(run,workspace.English);
            var exports=new[]{"json","csv","html"}.Select(format=>new{name="UI-FLOWRESULTS-sample."+format,body=Encoding.UTF8.GetString(ReplicaSampleExport.Create(run with{Id="UI-FLOWRESULTS"},format,workspace.English,findings))}).ToArray();
            workspace.SetSaved(true);await Flush();State("download-requested");
            using var report=JsonDocument.Parse((run with{Id="UI-FLOWRESULTS"}).WriteJson(findings));workspace.LoadSampleReport(ReplicaSampleRun.Read(Encoding.UTF8.GetBytes(report.RootElement.GetRawText())));await Flush();State("reopen-results");
            Assert(id+"-filter-preserved-run",filteredRun is not null&&workspace.SampleRun!.Groups.SequenceEqual(filteredRun.Groups),new{groups=workspace.SampleRun!.Groups});
            using(var bitmap=new RenderTargetBitmap(new((int)window.Width*2,(int)window.Height*2),new(192,192)))
            {bitmap.Render(workspace);bitmap.Save(Path.Combine(output,"pageintegrationresults-"+id+".png"),new PngBitmapEncoderOptions());}
            records.Add(new{id,viewport=new{width=window.ClientSize.Width,height=window.ClientSize.Height},rows,exports});
            File.WriteAllText(Path.Combine(output,"pageintegrationresults-native.json"),JsonSerializer.Serialize(new{dpr=window.RenderScaling,rows=records,scope="Real App/MainWindow parent wiring; existing Results child matrices reused"},new JsonSerializerOptions{WriteIndented=true}));
        }
    }
    private static async Task<List<object>> CaptureRootBoundaryModals(ReplicaWorkspace workspace,Window window,string id)
    {
        var rows=new List<object>();var prefix=id.StartsWith("ko-")?"ko-":"en-";
        async Task Settle(){await Dispatcher.UIThread.InvokeAsync(()=>{},DispatcherPriority.Loaded);window.UpdateLayout();await Task.Delay(360);if(ReplicaPlatform.IsHeadless)AvaloniaHeadlessPlatform.ForceRenderTimerTick(1);window.UpdateLayout();}
        object Box(Control control){var p=control.TranslatePoint(default,workspace)??throw new InvalidOperationException("Attached root modal required");return new{x=p.X,y=p.Y,width=control.Bounds.Width,height=control.Bounds.Height};}
        workspace.SetLanguage(!id.StartsWith("en-"));await workspace.DispatchAsync("filter:unknown");await Settle();
        foreach(var key in new[]{"data","trial","research"})
        {
            workspace.Viewport.Offset=key=="data"?0:400;
            var page=(WebResultsPage)workspace.CurrentPage!;
            if(key=="research")workspace.ShowInfo(false,page.ResearchButton);else workspace.ShowResultDetails(key,key=="trial"?page.TrialButton:page.Chart!.Rows.First(r=>r.CategoryKey==key));
            await Settle();
            var detail=workspace.GetVisualDescendants().OfType<WebResultDetails>().FirstOrDefault(d=>d.IsVisible);var info=workspace.InfoDialog;
            rows.Add(new{id=prefix+(key=="research"?"research":"detail-"+key)+"-open",modalBox=Box(detail?.DialogBox??info!.DialogBox),scope="APIsetup geometry only; actual callback/input proof retained"});
            var closing=detail is not null?detail.CloseAsync():info!.CloseAsync();await Settle();await closing;window.UpdateLayout();
        }
        return rows;
    }
    private static object CaptureMarkerLayout(ReplicaWorkspace workspace,string id)
    {
        var text=workspace.AnnouncementRegion.GetVisualDescendants().OfType<TextBlock>().Single();
        var lines=text.TextLayout.TextLines;
        return new{id,text=text.Text,lineTexts=lines.Select(line=>string.Concat(line.TextRuns.OfType<ShapedTextRun>().Select(run=>run.Text.ToString())).TrimEnd('\n',' ')).ToArray(),lineWidths=lines.Select(line=>line.WidthIncludingTrailingWhitespace).ToArray(),parentBox=new{width=workspace.AnnouncementRegion.Bounds.Width,height=workspace.AnnouncementRegion.Bounds.Height},controlBox=new{width=text.Bounds.Width,height=text.Bounds.Height},fontFamily=text.FontFamily.Name,fontSize=text.FontSize,lineHeight=text.LineHeight,textWrapping=text.TextWrapping.ToString(),scope="Public actual TextBlock layout only; .sr clip remains1x1; native Control boundingbox is not browser inline inkRange"};
    }
    /// <summary>Actual actions are external. F8 only seeds the next original parent context.</summary>
    internal static async Task PrepareNative(ReplicaWorkspace workspace,Window window,string output)
    {
        if(ReplicaPlatform.IsHeadless)throw new InvalidOperationException("Actual OS observer requires a native platform");
        var options=new JsonSerializerOptions{WriteIndented=true};var index=-1;var revision=0;var snapshot=0;
        var layoutContexts=new[]{(Id:"ko-1000",English:false,Width:1000),(Id:"ko-1280",English:false,Width:1280),(Id:"en-1000",English:true,Width:1000),(Id:"en-1280",English:true,Width:1280)};var contexts=layoutContexts.Where(c=>c.Width==1280).ToArray();
        var resumePath=Path.Combine(output,"pageintegrationresults-resume.json");
        var resumeSeed=File.Exists(resumePath)?JsonDocument.Parse(File.ReadAllText(resumePath)).RootElement.GetProperty("seed").GetString():null;var resumeData=resumeSeed=="post-language";var resumeCapture=resumeSeed=="capture-caller";var skipLayout=resumeSeed=="skip-layout";var markerLayoutOnly=resumeSeed=="marker-layout-only";var markerLayoutRows=new List<object>();var markerLayoutReady=false;var rootBoundaryResume=resumeSeed=="root-boundary-resume";var rootBoundary=resumeSeed=="root-boundary"||rootBoundaryResume;var rootBoundaryRows=new List<object>();var rootBoundaryReady=false;
        if(rootBoundaryResume)
        {
            var retained=JsonDocument.Parse(File.ReadAllText(Path.Combine(output,"pageintegrationresults-retained-modal-boxes.json"))).RootElement;
            if(retained.GetArrayLength()!=3||retained.EnumerateArray().Any(r=>!r.GetProperty("id").GetString()!.StartsWith("ko-")))throw new InvalidOperationException("Exactly retained KO modal3 required");
            rootBoundaryRows.AddRange(retained.EnumerateArray().Select(r=>(object)r.Clone()));
        }
        var trace=new List<object>();
        object? Box(Control? c)
        {
            if(c is null)return null;var p=c.TranslatePoint(default,workspace);
            return p is null?null:new{x=p.Value.X,y=p.Value.Y,width=c.Bounds.Width,height=c.Bounds.Height};
        }
        string? Identity(Control? c)
        {
            if(c is null)return null;
            if(workspace.CurrentPage is WebResultsPage p)
            {
                if(ReferenceEquals(c,p.Heading.FocusTarget))return "screen-title";
                if(ReferenceEquals(c,p.TrialButton))return "detail:trial";
                if(ReferenceEquals(c,p.ResearchButton))return "research";
                var row=p.Chart?.Rows.FirstOrDefault(r=>ReferenceEquals(r,c));if(row is not null)return "detail:"+row.CategoryKey;
            }
            if(workspace.CurrentPage is WebSavePage save&&ReferenceEquals(c,save.Heading.FocusTarget))return "screen-title";
            var id=AutomationProperties.GetAutomationId(c);
            if(id?.StartsWith("Results-filter-")==true)return "filter:"+id[15..];
            if(id?.StartsWith("Footer-")==true)return id[7..];
            if(id is "ResultDetails-close" or "ResultDetails-footer-close" or "InfoDialog-close" or "InfoDialog-footer-close")return "modal-close";
            return id;
        }
        void Record()
        {
            window.UpdateLayout();var p=workspace.CurrentPage as WebResultsPage;var heading=p?.Heading??(workspace.CurrentPage as WebSavePage)?.Heading;
            var detail=workspace.GetVisualDescendants().OfType<WebResultDetails>().FirstOrDefault(d=>d.IsVisible);
            var payload=detail is null?null:detail.Body.CleanupNotice is not null?"trial":detail.TitleText.Text switch{"데이터" or "Data"=>"data","구조·제약" or "Schema & constraints"=>"schema","사용자·권한" or "Users & permissions"=>"access",_=>"objects"};
            object? modal=detail is not null?new{type="detail",payload,title=detail.TitleText.Text,checks=detail.Body.CheckList.Items.Select(x=>x.Text.Text).ToArray(),notices=new[]{detail.Body.ScopeNotice,detail.Body.CleanupNotice}.Where(x=>x is not null).Select(x=>new{title=x!.Title,text=x.Text}).ToArray(),open=true}:workspace.InfoDialog is{} info?new{type="research",payload="",title=info.TitleText.Text,checks=Array.Empty<string>(),notices=Array.Empty<object>(),open=true}:null;
            var controls=workspace.GetVisualDescendants().OfType<Control>().Where(c=>!string.IsNullOrEmpty(AutomationProperties.GetAutomationId(c))||Identity(c)?.StartsWith("detail:")==true).Select(c=>new{id=AutomationProperties.GetAutomationId(c),identity=Identity(c),name=ControlAutomationPeer.CreatePeerForElement(c)?.GetName(),type=c.GetType().Name,box=Box(c),visible=c.IsEffectivelyVisible,enabled=c.IsEffectivelyEnabled,focused=c.IsFocused}).ToArray();
            object? report=workspace.SampleRun is{} run&&workspace.SafeFindingProvider is{} provider?JsonSerializer.Deserialize<JsonElement>((run with{Id="UI-FLOWRESULTS"}).WriteJson(provider(run,workspace.English))):null;
            var focused=window.FocusManager?.GetFocusedElement() as Control;
            bool? focusVisible=focused switch{WebResultsTrialButton b=>b.FocusVisible,WebResultSummaryRow r=>r.FocusVisible,_=>heading is not null&&ReferenceEquals(focused,heading.FocusTarget)?heading.FocusVisible:null};
            var state=new{markerLayoutReady,markerLayoutCount=markerLayoutRows.Count,rootBoundaryReady,rootBoundaryCount=rootBoundaryRows.Count,modalBox=Box(detail?.DialogBox??workspace.InfoDialog?.DialogBox),focusVisible,snapshot=++snapshot,revision,index,caseId=index<0?null:contexts[index].Id,viewport=new{width=window.ClientSize.Width,height=window.ClientSize.Height},dpr=window.RenderScaling,headless=ReplicaPlatform.IsHeadless,screen=workspace.Screen,english=workspace.English,announcement=workspace.Announcement,filter=p?.Filter,focus=Identity(window.FocusManager?.GetFocusedElement() as Control),scroll=workspace.Viewport.Offset,visited=workspace.Visited.ToArray(),title=heading?.TitleText.Text,articleIds=p?.Articles.Select(a=>a.IdentifierText.Text).ToArray()??[],pageOpacity=p?.Opacity,pageType=workspace.CurrentPage?.GetType().Name,modal,backgroundEnabled=workspace.CurrentPage?.IsEffectivelyEnabled,peerChildren=ControlAutomationPeer.CreatePeerForElement(workspace)!.GetChildren().Count,report,boxes=new{content=Box(workspace.Viewport),page=Box(workspace.CurrentPage),footer=Box(workspace.Footer),heading=Box(heading),filter=Box(p?.FilterRow),findings=Box(p?.FindingList)},footer=new[]{workspace.Footer.BackButton}.Concat(workspace.Footer.ActionButtons).Where(x=>x is not null).Select(b=>new{action=Identity(b),text=b!.Content,disabled=!b.IsEnabled,box=Box(b)}).ToArray(),controls,trace};
            var path=Path.Combine(output,"pageintegrationresults-actual-state.json");File.WriteAllText(path+".tmp",JsonSerializer.Serialize(state,options));File.Move(path+".tmp",path,true);
        }
        async Task Next()
        {
            if(index>=contexts.Length-1)return;++index;++revision;var c=contexts[index];window.Width=c.Width;window.Height=900;workspace.ReducedMotion=false;
            await workspace.DispatchAsync("start");workspace.SetLanguage(c.English);workspace.SetScope(ReplicaSampleRun.CategoryKeys,ReplicaSampleRun.AllowedTables);workspace.Navigate("home");
            if((index==0&&resumeData)||resumeCapture)
            {
                await workspace.DispatchAsync("sample");await Dispatcher.UIThread.InvokeAsync(()=>{},DispatcherPriority.Loaded);window.UpdateLayout();await Task.Delay(360);
                workspace.SetLanguage(!c.English);await Dispatcher.UIThread.InvokeAsync(()=>{},DispatcherPriority.Loaded);await workspace.DispatchAsync("filter:unknown");await Dispatcher.UIThread.InvokeAsync(()=>{},DispatcherPriority.Loaded);window.UpdateLayout();
                workspace.TopBar.LanguageButton.Focus(NavigationMethod.Pointer);workspace.Viewport.Offset=0;
                if(resumeCapture&&workspace.CurrentPage is WebResultsPage capturePage){workspace.Viewport.Offset=400;capturePage.ResearchButton.Focus(NavigationMethod.Pointer);workspace.ShowInfo(false,capturePage.ResearchButton);}
            }
            if(markerLayoutOnly)
            {
                markerLayoutReady=false;await workspace.DispatchAsync("sample");await Dispatcher.UIThread.InvokeAsync(()=>{},DispatcherPriority.Loaded);window.UpdateLayout();await Task.Delay(360);window.UpdateLayout();
                markerLayoutRows.Add(CaptureMarkerLayout(workspace,c.Id));File.WriteAllText(Path.Combine(output,"pageintegrationresults-marker-layout.json"),JsonSerializer.Serialize(markerLayoutRows,options));markerLayoutReady=true;
            }
            if(rootBoundary)
            {
                rootBoundaryReady=false;await workspace.DispatchAsync("sample");await Dispatcher.UIThread.InvokeAsync(()=>{},DispatcherPriority.Loaded);window.UpdateLayout();await Task.Delay(360);
                if(!rootBoundaryResume||index!=0)rootBoundaryRows.AddRange(await CaptureRootBoundaryModals(workspace,window,c.Id));
                File.WriteAllText(Path.Combine(output,"pageintegrationresults-root-modal-boxes.json"),JsonSerializer.Serialize(rootBoundaryRows,options));rootBoundaryReady=true;
            }
            await Dispatcher.UIThread.InvokeAsync(()=>{},DispatcherPriority.Loaded);window.UpdateLayout();trace.Clear();Record();
        }
        window.AddHandler(InputElement.PointerPressedEvent,(_,e)=>trace.Add(new{kind="down",revision,source=e.Source?.GetType().Name,point=new{x=e.GetPosition(window).X,y=e.GetPosition(window).Y},focus=Identity(window.FocusManager?.GetFocusedElement() as Control)}),Avalonia.Interactivity.RoutingStrategies.Tunnel,true);
        window.AddHandler(InputElement.PointerReleasedEvent,(_,e)=>trace.Add(new{kind="up",revision,source=e.Source?.GetType().Name,focus=Identity(window.FocusManager?.GetFocusedElement() as Control)}),Avalonia.Interactivity.RoutingStrategies.Bubble,true);
        window.KeyDown+=async(_,e)=>{if(e.Key==Key.F8){e.Handled=true;await Next();}else if(e.Key==Key.F12){e.Handled=true;Record();}else if(e.Key is Key.F5 or Key.F6 or Key.F7){e.Handled=true;if(workspace.CurrentPage is WebResultsPage){workspace.Viewport.Offset=e.Key==Key.F5?0:400;await Dispatcher.UIThread.InvokeAsync(()=>{},DispatcherPriority.Loaded);Record();}}};
        var observer=new DispatcherTimer{Interval=TimeSpan.FromMilliseconds(100)};observer.Tick+=(_,_)=>Record();observer.Start();window.Closed+=(_,_)=>observer.Stop();
        observer.Stop();var layouts=new List<object>();
        foreach(var c in resumeData||resumeCapture||skipLayout||rootBoundary||markerLayoutOnly?Array.Empty<(string Id,bool English,int Width)>():layoutContexts)
        {
            window.Width=c.Width;window.Height=900;workspace.ReducedMotion=false;await workspace.DispatchAsync("start");workspace.SetLanguage(c.English);workspace.SetScope(ReplicaSampleRun.CategoryKeys,ReplicaSampleRun.AllowedTables);workspace.Navigate("home");await workspace.DispatchAsync("sample");
            await Dispatcher.UIThread.InvokeAsync(()=>{},DispatcherPriority.Loaded);window.UpdateLayout();await Task.Delay(360);window.UpdateLayout();
            if(workspace.CurrentPage is not WebResultsPage p)throw new InvalidOperationException("Connected Results required for new native parent allocations");
            layouts.Add(new{id=c.Id,viewport=new{width=window.ClientSize.Width,height=window.ClientSize.Height},boxes=new{content=Box(workspace.Viewport),page=Box(p),footer=Box(workspace.Footer),heading=Box(p.Heading),filter=Box(p.FilterRow),findings=Box(p.FindingList)},footer=new[]{workspace.Footer.BackButton}.Concat(workspace.Footer.ActionButtons).Where(x=>x is not null).Select(b=>new{action=Identity(b),text=b!.Content,disabled=!b.IsEnabled,box=Box(b)}).ToArray(),focus=Identity(window.FocusManager?.GetFocusedElement() as Control),scroll=workspace.Viewport.Offset,announcement=workspace.Announcement,title=p.Heading.TitleText.Text,articleIds=p.Articles.Select(a=>a.IdentifierText.Text).ToArray(),pageOpacity=p.Opacity,scope="Passive native newApp parent allocation4; API setup is not actual input evidence"});
        }
        File.WriteAllText(Path.Combine(output,"pageintegrationresults-actual-layout.json"),JsonSerializer.Serialize(new{dpr=window.RenderScaling,rows=layouts},options));observer.Start();
        await Next();File.WriteAllText(Path.Combine(output,"pageintegrationresults-actual-ready.json"),JsonSerializer.Serialize(new{phase="ACTUAL_OS_READY",contexts=contexts.Select(x=>x.Id),passiveLayoutContexts=layoutContexts.Select(x=>x.Id),scope="RealApp parent only; F8 setup; F12 manual debugging excluded from focus-visible proof. Actual source/native pointer/key must be external. Existing locked child matrices reused."},options));
    }

}
