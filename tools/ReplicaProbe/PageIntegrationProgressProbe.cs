using System.Text.Json;
using Avalonia;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Threading;
using Verifier.Desktop;

internal static class PageIntegrationProgressProbe
{
    internal static async Task CheckLayout(ReplicaWorkspace workspace,Window window,string output,string cases,string oracle)
    {
        using var specs=JsonDocument.Parse(File.ReadAllText(cases));using var source=JsonDocument.Parse(File.ReadAllText(oracle));var checks=new List<object>();
        void Assert(string id,bool passed,object value){checks.Add(new{id,passed,value});File.WriteAllText(Path.Combine(output,"progress-parent-checks.json"),JsonSerializer.Serialize(checks,new JsonSerializerOptions{WriteIndented=true}));if(!passed)throw new InvalidOperationException(id);}
        object Box(Control c){var p=c.TranslatePoint(default,workspace)??default;return new{x=p.X,y=p.Y,width=c.Bounds.Width,height=c.Bounds.Height};}
        workspace.ReducedMotion=true;
        foreach(var spec in specs.RootElement.EnumerateArray())
        {
            var id=spec.GetProperty("id").GetString()!;var expected=source.RootElement.GetProperty("rows").EnumerateArray().Single(x=>x.GetProperty("id").GetString()==id);
            window.Width=spec.GetProperty("width").GetDouble();window.Height=spec.GetProperty("height").GetDouble();workspace.SetLanguage(spec.GetProperty("english").GetBoolean());
            var run=JsonSerializer.Deserialize<ReplicaSampleRun>(spec.GetProperty("run").GetRawText(),new JsonSerializerOptions{PropertyNameCaseInsensitive=true})!;
            workspace.LoadSampleReport(run);workspace.Navigate("running");await Dispatcher.UIThread.InvokeAsync(()=>{},DispatcherPriority.Background);window.UpdateLayout();var page=(WebProgressPage)workspace.CurrentPage!;
            foreach(var pair in new (string key,Control control)[]{("topbar",workspace.TopBar),("page",page),("heading",page.Heading),("panel",page.ProgressPanel),("table",page.TableShell),("content",workspace.Viewport),("footer",workspace.Footer)})
            {
                using var actual=JsonDocument.Parse(JsonSerializer.Serialize(Box(pair.control)));var box=expected.GetProperty(pair.key);
                Assert(id+"-"+pair.key,box.EnumerateObject().All(p=>Math.Abs(p.Value.GetDouble()-actual.RootElement.GetProperty(p.Name).GetDouble())<.02),Box(pair.control));
            }
            var action=expected.GetProperty("footerActions").EnumerateArray().Single().GetProperty("action").GetString();
            Assert(id+"-footer",AutomationProperties.GetAutomationId(workspace.Footer.ActionButtons.Single())=="Footer-"+action,workspace.Footer.ActionButtons.Single().Content!);
            Assert(id+"-announcer",workspace.Announcement==expected.GetProperty("announcement").GetString(),workspace.Announcement);
            Assert(id+"-state",page.Note.Text==expected.GetProperty("note").GetString()&&(page.InterruptedNotice is not null)==expected.GetProperty("interrupted").GetBoolean()&&workspace.Viewport.Offset==expected.GetProperty("scroll").GetDouble(),new{page.Note.Text,workspace.Viewport.Offset});
        }
    }
    internal static async Task Check(ReplicaWorkspace workspace,Window window,string output)
    {
        var checks=new List<object>();
        void Assert(string id,bool passed,object value){checks.Add(new{id,passed,value});File.WriteAllText(Path.Combine(output,"progress-connected-checks.json"),JsonSerializer.Serialize(checks,new JsonSerializerOptions{WriteIndented=true}));if(!passed)throw new InvalidOperationException(id);}
        async Task Flush(){await Dispatcher.UIThread.InvokeAsync(()=>{},DispatcherPriority.Background);window.UpdateLayout();}
        async Task Start(string[] categories){workspace.SetScope(categories,ReplicaSampleRun.AllowedTables);workspace.Navigate("review");await Flush();((WebReviewPage)workspace.CurrentPage!).StoppedChoice.Checkbox.IsChecked=true;await workspace.DispatchAsync("run");await Flush();}
        workspace.ReducedMotion=true;window.Width=1280;window.Height=900;window.Activate();
        await Start(["data","schema"]);var page=(WebProgressPage)workspace.CurrentPage!;
        Assert("navigation-page-heading",page.Run==workspace.SampleRun&&page.Heading.FocusTarget.IsFocused&&workspace.Announcement==page.Heading.TitleText.Text&&workspace.Viewport.Offset==0,new{workspace.Screen,workspace.Announcement});
        Assert("initial-cancel-footer",AutomationProperties.GetAutomationId(workspace.Footer.ActionButtons.Single())=="Footer-cancel",workspace.Footer.ActionButtons.Single().Content!);
        await Task.Delay(325);await Flush();
        Assert("same-page-live-assembly",ReferenceEquals(page,workspace.CurrentPage)&&ReferenceEquals(page.Run,workspace.SampleRun)&&page.Number.Large.Text==workspace.SampleRun!.Groups.Sum(g=>g.Done).ToString()&&page.Table.Rows[0].Status==workspace.SampleRun.Groups[0].Status,new{count=page.Number.Large.Text,status=page.Table.Rows[0].Status});
        workspace.SetLanguage(true);await Flush();var english=(WebProgressPage)workspace.CurrentPage!;
        Assert("language-preserves-live-run",!ReferenceEquals(page,english)&&english.English&&ReferenceEquals(english.Run,workspace.SampleRun)&&english.Number.Large.Text==workspace.SampleRun!.Groups.Sum(g=>g.Done).ToString(),new{workspace.Screen,english.English});
        await workspace.DispatchAsync("cancel");await Flush();var cancelled=(WebProgressPage)workspace.CurrentPage!;
        Assert("cancel-connected-page",cancelled.Run.Interrupted&&cancelled.InterruptedNotice is not null&&AutomationProperties.GetAutomationId(workspace.Footer.ActionButtons.Single())=="Footer-show-results"&&workspace.Announcement=="Checks interrupted. Partial results are available.",new{workspace.Announcement,cancelled.Run.Interrupted});
        await workspace.DispatchAsync("show-results");await Flush();Assert("cancel-result-assembly",workspace.CurrentPage is WebResultsPage&&workspace.SampleRun!.Interrupted,new{workspace.Screen});
        await workspace.DispatchAsync("restart");await Flush();Assert("restart-connected",workspace.CurrentPage is WebConnectionPage&&workspace.SampleRun is null,new{workspace.Screen});
        workspace.SetLanguage(false);await Start(["access"]);workspace.Footer.ActionButtons.Single().Focus(NavigationMethod.Tab);var deadline=DateTime.UtcNow.AddSeconds(5);
        while(workspace.SampleRun is{Complete:false}&&DateTime.UtcNow<deadline)await Task.Delay(50);await Flush();
        var complete=(WebProgressPage)workspace.CurrentPage!;
        Assert("complete-render-footer-focus",complete.Run.Complete&&complete.Number.Large.Text==complete.Run.Groups.Sum(g=>g.Total).ToString()&&workspace.Footer.ActionButtons.Single().IsFocused&&AutomationProperties.GetAutomationId(workspace.Footer.ActionButtons.Single())=="Footer-show-results",new{workspace.Screen,count=complete.Number.Large.Text});
        Assert("complete-announcement",workspace.Announcement=="예시 검사 완료. 결과 보기 버튼을 사용하세요.",workspace.Announcement);
        await workspace.DispatchAsync("show-results");await Flush();Assert("complete-result-assembly",workspace.CurrentPage is WebResultsPage&&workspace.SampleRun!.Complete,new{workspace.Screen});
    }
}
