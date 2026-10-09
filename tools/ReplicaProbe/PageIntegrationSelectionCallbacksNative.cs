using Avalonia;
using System.Text.Json;
using Avalonia.Automation;
using Avalonia.Automation.Peers;
using Avalonia.Automation.Provider;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Verifier.Desktop;

/// <summary>Read-only native callback observer; fixture preparation is labeled separately from external input.</summary>
internal static class PageIntegrationSelectionCallbacksNative
{
    internal static async Task Prepare(ReplicaWorkspace workspace,Window window,string output,string oracle,bool english)
    {
        using var source=JsonDocument.Parse(File.ReadAllText(oracle));if(source.RootElement.GetProperty("records").GetArrayLength()!=2)throw new InvalidOperationException("Exact callback oracle required");
        window.Width=1280;window.Height=900;workspace.ReducedMotion=false;var sequence=0;var changes=0;var ready=false;Control? page=null,heading=null,search=null,category=null,tableCheckbox=null;
        async Task Flush(){await Dispatcher.UIThread.InvokeAsync(()=>{},DispatcherPriority.Loaded);window.UpdateLayout();await Task.Delay(60);}
        async Task StopSetup()
        {
            ready=false;workspace.Navigate("review");await Flush();var p=(WebReviewPage)workspace.CurrentPage!;
            if(p.StoppedChoice.Checkbox.IsChecked!=true)((IToggleProvider)ControlAutomationPeer.CreatePeerForElement(p.StoppedChoice.Checkbox)!).Toggle();
            workspace.Navigate("select");await Flush();await Task.Delay(360);var selection=(WebSelectionPage)workspace.CurrentPage!;
            selection.ViewChanged+=(_,_)=>changes++;page=selection;heading=selection.Heading.FocusTarget;search=selection.Search.Editor;category=selection.Categories[0].Header.Checkbox;tableCheckbox=selection.Rows.Single(r=>r.SourceName=="demo.customers").Checkbox;ready=true;
        }
        workspace.SetLanguage(english);await workspace.DispatchAsync("start");await Flush();await workspace.DispatchAsync("connect");await StopSetup();
        var resumeFile=Path.Combine(output,"selectioncallbacks-resume.json");
        if(File.Exists(resumeFile))
        {
            using var resume=JsonDocument.Parse(File.ReadAllText(resumeFile));
            var checkpoint=resume.RootElement.GetProperty("checkpoint").GetString();
            if(checkpoint is not ("after-query-orders" or "after-mapping-draft-cancel" or "after-mapping-open-removal" or "after-resize1280"))throw new InvalidOperationException("Known saved callback checkpoint required");
            var selection=(WebSelectionPage)workspace.CurrentPage!;
            ((IToggleProvider)ControlAutomationPeer.CreatePeerForElement(selection.Categories[0].Header.Checkbox)!).Toggle();await Flush();await StopSetup();
            selection=(WebSelectionPage)workspace.CurrentPage!;var expansion=(IExpandCollapseProvider)ControlAutomationPeer.CreatePeerForElement(selection.Categories[0].Header.ToggleButton)!;
            expansion.Expand();await Flush();expansion.Collapse();await Flush();selection.Search.Text="orders";await Flush();
            if(checkpoint is "after-mapping-draft-cancel" or "after-mapping-open-removal" or "after-resize1280")
            {
                ((IToggleProvider)ControlAutomationPeer.CreatePeerForElement(selection.Rows.Single().Checkbox)!).Toggle();await Flush();
                ((IToggleProvider)ControlAutomationPeer.CreatePeerForElement(selection.BulkCheckbox)!).Toggle();await Flush();
                selection.DeselectButton.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));await Flush();
                selection.Rows.Single().ChangeButton.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));await Flush();
                workspace.MatchingDialog!.Options.Single(o=>o.TargetName=="archive.orders").Radio.IsChecked=true;await Flush();
                if(checkpoint is "after-mapping-open-removal" or "after-resize1280")
                {
                    workspace.MatchingDialog.ApplyButton.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));await Flush();await Task.Delay(360);
                    selection=(WebSelectionPage)workspace.CurrentPage!;selection.Search.Text="archive";await Flush();
                    selection.Rows.Single().ChangeButton.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));await Flush();await Task.Delay(360);
                    if(workspace.MatchingDialog?.DraftTarget!="archive.orders"||selection.Search.Text!="archive"||workspace.Stopped)throw new InvalidOperationException("Exact saved removal modal required");
                    if(checkpoint=="after-resize1280")
                    {
                        workspace.MatchingDialog.Options.Single(o=>o.TargetName=="demo.orders").Radio.IsChecked=true;await Flush();
                        workspace.MatchingDialog.ApplyButton.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));await Flush();await Task.Delay(360);
                        selection=(WebSelectionPage)workspace.CurrentPage!;selection.Search.Text="";workspace.Viewport.Offset=0;await Flush();
                        if(selection.Rows.Count!=3||workspace.Maps["demo.orders"]!="demo.orders"||workspace.MatchingDialog is not null)throw new InvalidOperationException("Exact saved resize checkpoint required");
                    }
                }
            }

            File.WriteAllText(Path.Combine(output,"resume-fixture-proof.json"),JsonSerializer.Serialize(new{checkpoint,scope="Explicit provider/handler fixture restoration of already-passed physical checkpoints; no physical pass claimed",state=PageIntegrationFlowSnapshot.Read("resume",workspace,window)}));
        }

        object Box(Control c){var p=c.TranslatePoint(default,workspace)??throw new InvalidOperationException("Attached observer target required");return new{x=p.X,y=p.Y,width=c.Bounds.Width,height=c.Bounds.Height};}
        void Record()
        {
            if(!ready)return;window.UpdateLayout();var selection=workspace.CurrentPage as WebSelectionPage;var modal=workspace.MatchingDialog;var focused=window.FocusManager?.GetFocusedElement() as Control;var name=focused is null?"":ControlAutomationPeer.CreatePeerForElement(focused)?.GetName()??"";
            var current=new{sequence=sequence++,ready,changes,state=PageIntegrationFlowSnapshot.Read("callback-native",workspace,window),reason=selection?.LastChangeReason??"",shown=selection?.Rows.Select(r=>r.SourceName).ToArray()??[],modal=modal is null?null:new{source=modal.SourceName,draft=modal.DraftTarget,query=modal.Search.Text},focusedName=name,focusedType=focused?.GetType().Name??"",identity=new{pageSame=ReferenceEquals(workspace.CurrentPage,page),headingSame=ReferenceEquals(selection?.Heading.FocusTarget,heading),searchSame=ReferenceEquals(selection?.Search.Editor,search),categorySame=ReferenceEquals(selection?.Categories[0].Header.Checkbox,category),tableCheckboxSame=ReferenceEquals(selection?.Rows.FirstOrDefault(r=>r.SourceName=="demo.customers")?.Checkbox,tableCheckbox)},width=window.ClientSize.Width,height=window.ClientSize.Height,controls=workspace.GetVisualDescendants().OfType<Control>().Where(c=>c is Button or WebTextInput).Select(c=>new{id=AutomationProperties.GetAutomationId(c),name=ControlAutomationPeer.CreatePeerForElement(c)?.GetName(),enabled=c.IsEffectivelyEnabled,visible=c.IsEffectivelyVisible,box=Box(c)}).ToArray()};
            File.WriteAllText(Path.Combine(output,$"selectioncallback-state-{current.sequence:D5}.json"),JsonSerializer.Serialize(current,new JsonSerializerOptions{WriteIndented=true}));
        }
        void PeerDiagnostic()
        {
            var parentField=typeof(ControlAutomationPeer).GetField("_parent",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance);
            var validField=typeof(ControlAutomationPeer).GetField("_parentValid",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance);
            object[] CachedChain(AutomationPeer initial)
            {
                var list=new List<object>();var seen=new HashSet<AutomationPeer>();AutomationPeer? peer=initial;
                while(peer is not null&&seen.Add(peer)&&list.Count<30)
                {
                    var owner=(peer as ControlAutomationPeer)?.Owner;list.Add(new{peer=peer.GetType().FullName,owner=owner?.GetType().FullName,id=owner is null?null:AutomationProperties.GetAutomationId(owner),attached=owner is not null&&TopLevel.GetTopLevel(owner)is not null,bounds=peer.GetBoundingRectangle(),parentValid=validField?.GetValue(peer),cachedParent=(parentField?.GetValue(peer)as AutomationPeer)?.GetType().FullName});
                    peer=parentField?.GetValue(peer)as AutomationPeer;
                }
                return list.ToArray();
            }
            var p=(WebSelectionPage)workspace.CurrentPage!;
            File.WriteAllText(Path.Combine(output,"managed-cached-peer-diagnostic.json"),JsonSerializer.Serialize(new{scope="Read-only cached parent fields and bounds, no GetParent/EnsureConnected repair calls",rows=p.Rows.Select(r=>new{r.SourceName,checkbox=CachedChain(ControlAutomationPeer.CreatePeerForElement(r.Checkbox)!),button=CachedChain(ControlAutomationPeer.CreatePeerForElement(r.ChangeButton)!)}),table=CachedChain(ControlAutomationPeer.CreatePeerForElement(p.TableControl)!)},new JsonSerializerOptions{WriteIndented=true}));
        }
        // Fixture utilities prepare contexts without claiming a user action. No hidden product mutation.
        window.KeyDown+=async(_,e)=>{
            if(e.Key==Key.F5){e.Handled=true;PeerDiagnostic();}
            else if(e.Key==Key.F6){e.Handled=true;await StopSetup();Record();}
            else if(e.Key==Key.F7){e.Handled=true;if(workspace.CurrentPage is WebSelectionPage p){page=p;heading=p.Heading.FocusTarget;search=p.Search.Editor;category=p.Categories[0].Header.Checkbox;tableCheckbox=p.Rows.FirstOrDefault(r=>r.SourceName=="demo.customers")?.Checkbox;p.Search.Editor.Focus(NavigationMethod.Pointer);workspace.Viewport.Offset=0;await Flush();Record();}}
            else if(e.Key==Key.F8){e.Handled=true;window.Width=window.ClientSize.Width==1280?960:window.ClientSize.Width==960?1180:1280;await Flush();Record();}
            else if(e.Key==Key.F9){e.Handled=true;workspace.SetLanguage(!workspace.English);await Flush();Record();}
        };
        var timer=new DispatcherTimer{Interval=TimeSpan.FromMilliseconds(100)};timer.Tick+=(_,_)=>Record();window.Closed+=(_,_)=>timer.Stop();Record();timer.Start();
        File.WriteAllText(Path.Combine(output,"selectioncallbacks-native-ready.json"),JsonSerializer.Serialize(new{ready=true,english,dpr=window.RenderScaling,headless=ReplicaPlatform.IsHeadless,scope="Actual parent observation; external guarded input still required. F6 Stopped fixture setup, F7 resize identity seed, F8 resize, F9 source language handler API only."}));
    }
}
