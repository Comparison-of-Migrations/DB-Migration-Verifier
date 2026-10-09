using System.Text.Json;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Input;
using Avalonia.Input.Raw;
using Avalonia.Interactivity;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Verifier.Desktop;

/// <summary>New connected Selection callback scopes; prior standalone geometry is not repeated.</summary>
internal static class PageIntegrationSelectionCallbacksProbe
{
    internal static async Task Check(ReplicaWorkspace workspace,Window window,string output,string oracle)
    {
        if(!ReplicaPlatform.IsHeadless)throw new InvalidOperationException("This callback handler oracle is headless only; physical OS input is external");
        var source=JsonDocument.Parse(File.ReadAllText(oracle));
        if(source.RootElement.GetProperty("records").GetArrayLength()!=2)throw new InvalidOperationException("Fresh KOEN original callback oracle required");
        Directory.CreateDirectory(output);var records=new List<object>();
        Control? initialPage=null,initialHeading=null,initialSearch=null,initialCategory=null,initialCheckbox=null;
        async Task Flush(int delay=35){await Task.Delay(delay);await Dispatcher.UIThread.InvokeAsync(()=>{},DispatcherPriority.Loaded);window.UpdateLayout();}
        async Task Until(Func<bool> condition,string label){var limit=DateTime.UtcNow.AddSeconds(8);while(!condition()){if(DateTime.UtcNow>limit)throw new InvalidOperationException(label);await Flush(15);}}
        WebSelectionPage Selection()=>workspace.CurrentPage as WebSelectionPage??throw new InvalidOperationException("Expected connected Selection");
        async Task Click(Button button){if(!button.IsEffectivelyEnabled)throw new InvalidOperationException("Enabled source-shaped button required");button.Focus(NavigationMethod.Pointer);if(button is WebCategoryHeader.CategoryButton){window.KeyPress(Key.Space,RawInputModifiers.None,PhysicalKey.Space," ");window.KeyRelease(Key.Space,RawInputModifiers.None,PhysicalKey.Space," ");}else button.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));await Flush();}
        async Task Footer(string action){var buttons=workspace.Footer.ActionButtons.Prepend(workspace.Footer.BackButton).OfType<WebActionButton>();await Click(buttons.Single(b=>AutomationProperties.GetAutomationId(b)=="Footer-"+action));}
        async Task Toggle(WebCheckbox checkbox){if(!checkbox.IsEffectivelyEnabled)throw new InvalidOperationException("Enabled original-shaped checkbox required");checkbox.Focus(NavigationMethod.Pointer);window.KeyPress(Key.Space,RawInputModifiers.None,PhysicalKey.Space," ");window.KeyRelease(Key.Space,RawInputModifiers.None,PhysicalKey.Space," ");await Flush();}
        async Task Query(string text){Selection().Search.Editor.Focus(NavigationMethod.Pointer);Selection().Search.Text=text;await Flush();}
        async Task Mapping(){await Click(Selection().Rows.Single(r=>r.SourceName=="demo.orders").ChangeButton);await Until(()=>workspace.MatchingDialog is not null,"Matching open");}
        async Task Radio(string target){var option=workspace.MatchingDialog!.Options.Single(r=>r.TargetName==target);option.Radio.Focus(NavigationMethod.Pointer);option.Radio.IsChecked=true;await Flush();}
        async Task Close(bool apply){await Click(apply?workspace.MatchingDialog!.ApplyButton:workspace.MatchingDialog!.CloseButton);await Until(()=>workspace.MatchingDialog is null,"Matching close");await Flush();}
        async Task StoppedSetup(){workspace.Navigate("review");await Flush();var p=(WebReviewPage)workspace.CurrentPage!;if(!p.StoppedChoice.Checkbox.IsChecked.GetValueOrDefault())await Toggle(p.StoppedChoice.Checkbox);workspace.Navigate("select");await Flush();}
        foreach(var english in new[]{false,true})
        {
            workspace.Dispose();workspace=new ReplicaWorkspace{ReducedMotion=true};window.Content=workspace;window.Width=1280;window.Height=900;
            workspace.SetLanguage(english);await workspace.DispatchAsync("start");await Flush();await workspace.DispatchAsync("connect");await Until(()=>workspace.Connection.Connected&&!workspace.Connection.Busy,"Sample setup connection");await StoppedSetup();
            var rows=new List<object>();
            string Focus()
            {
                var c=window.FocusManager?.GetFocusedElement() as Control;if(c is null||c==workspace)return "BODY";
                if(c==PageIntegrationFlowSnapshot.Heading(workspace.CurrentPage)?.FocusTarget)return "screen-title";
                if(workspace.MatchingDialog is{} d){if(c==d.CloseButton||c==d.CancelButton)return "modal-close";if(c==d.ApplyButton)return "mapping-apply";if(c==d.Search.Editor)return "match-search";var options=d.Options.ToArray();var optionIndex=Array.FindIndex(options,o=>o.Radio==c);if(optionIndex>=0)return "match-option-"+optionIndex;}
                if(workspace.CurrentPage is WebSelectionPage p){if(c==p.Search.Editor)return "table-search";if(c==p.BulkCheckbox)return "table-all";for(var i=0;i<p.Categories.Count;i++){if(c==p.Categories[i].Header.Checkbox)return "category:"+ReplicaSampleRun.CategoryKeys[i];if(c==p.Categories[i].Header.ToggleButton)return "expand:"+ReplicaSampleRun.CategoryKeys[i];}foreach(var row in p.Rows){if(c==row.Checkbox)return "table:"+row.SourceName;if(c==row.ChangeButton)return "mapping:"+row.SourceName;}}
                var id=AutomationProperties.GetAutomationId(c)??"";return id.StartsWith("Footer-",StringComparison.Ordinal)?id[7..]:id.Length==0?"BODY":id;
            }
            void Record(string id)
            {
                var p=workspace.CurrentPage as WebSelectionPage;var d=workspace.MatchingDialog;var shown=workspace.Maps.Keys.Where(n=>workspace.TableQuery.Trim().Length==0||n.Contains(workspace.TableQuery.Trim(),StringComparison.OrdinalIgnoreCase)||workspace.Maps[n].Contains(workspace.TableQuery.Trim(),StringComparison.OrdinalIgnoreCase)).ToArray();
                var footer=workspace.Footer.ActionButtons.Prepend(workspace.Footer.BackButton).OfType<WebActionButton>().Select(b=>new{action=(AutomationProperties.GetAutomationId(b)??"").Replace("Footer-",""),disabled=!b.IsEnabled}).ToArray();
                rows.Add(new{id,english=workspace.English,screen=workspace.Screen,error=workspace.Connection.Error,categories=workspace.Categories.ToArray(),tables=workspace.Tables.ToArray(),maps=new Dictionary<string,string>(workspace.Maps),expanded=workspace.ExpandedCategories.ToArray(),query=workspace.TableQuery,stopped=workspace.Stopped,announcement=workspace.Announcement,focus=Focus(),modal=d is null?null:new{type="mapping",payload=d.SourceName,draft=d.DraftTarget,query=d.Search.Text},shown,bulk=p is null?null:new{ @checked=p.BulkCheckbox.IsChecked==true,mixed=p.BulkCheckbox.IsChecked is null,disabled=!p.BulkCheckbox.IsEnabled},footer,width=window.ClientSize.Width,pageSame=ReferenceEquals(p,initialPage),headingSame=ReferenceEquals(p?.Heading.FocusTarget,initialHeading),searchSame=ReferenceEquals(p?.Search.Editor,initialSearch),categorySame=ReferenceEquals(p?.Categories[0].Header.Checkbox,initialCategory),tableCheckboxSame=ReferenceEquals(p?.Rows.FirstOrDefault(r=>r.SourceName=="demo.customers")?.Checkbox,initialCheckbox),mainScroll=workspace.Viewport.Offset});
                File.WriteAllText(Path.Combine(output,"pageintegration-selectioncallbacks.json"),JsonSerializer.Serialize(new{scope="new real Workspace Selection handler callbacks; headless input, not physical OS input",nonce=source.RootElement.GetProperty("nonce").GetString(),dpr=window.RenderScaling,records=records.Append(new{language=english?"en":"ko",rows})},new JsonSerializerOptions{WriteIndented=true}));
            }
            Record("initial");await Toggle(Selection().Categories[0].Header.Checkbox);Record("category-data");await StoppedSetup();
            await Click(Selection().Categories[0].Header.ToggleButton);Record("expand-data");await Click(Selection().Categories[0].Header.ToggleButton);Record("collapse-data");
            await Query("orders");Record("query-orders");await Toggle(Selection().Rows.Single().Checkbox);Record("table-orders");await Toggle(Selection().BulkCheckbox);Record("filtered-bulk");await Click(Selection().DeselectButton);Record("filtered-deselect");await Query("");Record("query-clear");
            await Mapping();Record("mapping-open");await Radio("archive.orders");Record("mapping-draft");await Close(false);Record("mapping-cancel");await Mapping();await Radio("archive.orders");await Close(true);Record("mapping-applied");
            await Query("archive");Record("query-target");await Mapping();await Radio("demo.orders");await Close(true);Record("mapping-removes-opener");await Query("");
            await Mapping();workspace.MatchingDialog!.Search.Text="archive";await Flush();await Radio("archive.orders");Record("modal-before-language");workspace.SetLanguage(!workspace.English);await Flush();Record("modal-language-reopened");await Close(false);Record("modal-language-cancel");if(workspace.English!=english)await Click(workspace.TopBar.LanguageButton);
            var page=Selection();initialPage=page;initialHeading=page.Heading.FocusTarget;initialSearch=page.Search.Editor;initialCategory=page.Categories[0].Header.Checkbox;initialCheckbox=page.Rows.Single(r=>r.SourceName=="demo.customers").Checkbox;page.Search.Editor.Focus(NavigationMethod.Pointer);Record("resize-ready");
            foreach(var width in new[]{960,1180,1280}){window.Width=width;await Flush();Record("resize-"+width);}
            await Footer("back");await Footer("back");var connect=(WebConnectionPage)workspace.CurrentPage!;connect.Source.HostInput.Focus(NavigationMethod.Pointer);connect.Source.HostInput.Text="";await Flush();await Footer("connect");await Flush();Record("invalid-connect");await Footer("back");Record("error-back-home");await Click(((WebHomePage)workspace.CurrentPage!).StartButton);Record("error-start-connect");
            records.Add(new{language=english?"en":"ko",rows});
        }
    }
}
