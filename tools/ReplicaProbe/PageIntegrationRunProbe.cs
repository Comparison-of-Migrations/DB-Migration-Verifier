using System.Text.Json;
using Avalonia.Controls;
using Avalonia.Threading;
using Verifier.Desktop;

internal static class PageIntegrationRunProbe
{
    internal static async Task Check(ReplicaWorkspace workspace,Window window,string output,string oracle)
    {
        using var document=JsonDocument.Parse(File.ReadAllText(oracle));
        var rows=new List<object>();
        void Assert(string id,bool passed,object state){rows.Add(new{id,passed,state});File.WriteAllText(Path.Combine(output,"pageintegration-run-checks.json"),JsonSerializer.Serialize(rows,new JsonSerializerOptions{WriteIndented=true}));if(!passed)throw new InvalidOperationException(id);}
        object State(ReplicaSampleRun run)=>new{groups=run.Groups.Select(g=>new{key=g.Key,total=g.Total,done=g.Done,status=g.Status,equal=g.Equal,different=g.Different,unknown=g.Unknown}),interrupted=run.Interrupted,complete=run.Complete};
        foreach(var test in document.RootElement.GetProperty("rows").EnumerateArray())
        {
            var id=test.GetProperty("id").GetString()!;
            var specification=JsonDocument.Parse(File.ReadAllText(Path.Combine(Path.GetDirectoryName(oracle)!,"root-pageintegration-run-cases.json")));
            var c=specification.RootElement.EnumerateArray().First(x=>x.GetProperty("id").GetString()==id);
            var categories=c.GetProperty("categories").EnumerateArray().Select(x=>x.GetString()!).ToArray();var tables=c.GetProperty("tables").EnumerateArray().Select(x=>x.GetString()!).ToArray();
            var maps=ReplicaSampleRun.AllowedTables.ToDictionary(x=>x,x=>x);if(c.TryGetProperty("renamed",out var renamed)&&renamed.GetBoolean())maps["demo.orders"]="archive.orders";
            var run=ReplicaSampleRun.Create(categories,tables,maps);var frames=new List<object>{State(run)};
            var cancelAt=c.TryGetProperty("cancelAt",out var cancel)?cancel.GetInt32():-1;
            int iterations=0;
            while(!run.Complete)
            {
                if(cancelAt>=0&&run.Groups.Sum(g=>g.Done)>=cancelAt){run=run.Cancel();frames.Add(State(run));break;}
                run=run.Tick();frames.Add(State(run));if(++iterations>200)throw new InvalidOperationException("Unbounded sample run");
            }
            static string Canonical(JsonElement element)=>element.ValueKind switch{JsonValueKind.Object=>"{"+string.Join(",",element.EnumerateObject().OrderBy(p=>p.Name).Select(p=>JsonSerializer.Serialize(p.Name)+":"+Canonical(p.Value)))+"}",JsonValueKind.Array=>"["+string.Join(",",element.EnumerateArray().Select(Canonical))+"]",_=>element.GetRawText()};
            using var actual=JsonDocument.Parse(JsonSerializer.Serialize(frames));
            Assert("original-frames-"+id,Canonical(actual.RootElement)==Canonical(test.GetProperty("frames")),new{frames=frames.Count,expected=test.GetProperty("frames").GetArrayLength()});specification.Dispose();
        }
        await workspace.DispatchAsync("start");workspace.Navigate("review");await workspace.DispatchAsync("run");
        Assert("run-requires-stopped",workspace.SampleRun is null&&workspace.Screen=="review",new{workspace.Screen});
        workspace.SetScope(["data","schema"],["demo.orders"]);workspace.Navigate("review");await Dispatcher.UIThread.InvokeAsync(()=>{},DispatcherPriority.Background);window.UpdateLayout();
        ((WebReviewPage)workspace.CurrentPage!).StoppedChoice.Checkbox.IsChecked=true;
        await workspace.DispatchAsync("run");Assert("run-starts-source-state",workspace.Screen=="running"&&workspace.SampleRun is{Complete:false,Interrupted:false}&&workspace.SampleRun.Groups.All(g=>g.Done==0),new{workspace.Screen});
        await Task.Delay(465);await workspace.DispatchAsync("cancel");var cancelled=workspace.SampleRun!;
        Assert("clock-cancel-retains-partial",cancelled.Interrupted&&cancelled.Groups.Sum(g=>g.Done) is>=1 and<=4&&cancelled.Groups[0].Status=="interrupted",State(cancelled));
        await Task.Delay(180);Assert("cancel-stops-clock",ReferenceEquals(cancelled,workspace.SampleRun),State(workspace.SampleRun!));
        await workspace.DispatchAsync("show-results");Assert("partial-results-navigation",workspace.Screen=="results"&&ReferenceEquals(cancelled,workspace.SampleRun),new{workspace.Screen});
        await workspace.DispatchAsync("restart");Assert("restart-discards-partial",workspace.SampleRun is null&&!workspace.Stopped&&workspace.Screen=="connect",new{workspace.Screen});
        workspace.SetScope(["data"],[]);workspace.Navigate("review");await Dispatcher.UIThread.InvokeAsync(()=>{},DispatcherPriority.Background);((WebReviewPage)workspace.CurrentPage!).StoppedChoice.Checkbox.IsChecked=true;await workspace.DispatchAsync("run");
        await Task.Delay(315);Assert("zero-total-completes-next-tick",workspace.SampleRun is{Complete:true,Interrupted:false}&&workspace.SampleRun.Groups[0].Status=="completed",State(workspace.SampleRun!));
    }
}
