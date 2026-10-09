using System.Text.Json;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Threading;
using Verifier.Desktop;

/// <summary>Only the new small-viewport root modal placement. Locked child proofs are reused.</summary>
internal static class PageIntegrationModalBoundsProbe
{
    internal static async Task Check(ReplicaWorkspace workspace,Window window,string output,string source)
    {
        using var oracle=JsonDocument.Parse(File.ReadAllText(source));
        var checks=new List<object>();var rows=new List<object>();workspace.ReducedMotion=true;
        object Box(Control control)
        {var point=control.TranslatePoint(default,workspace)??throw new InvalidOperationException("Attached modal required");return new{x=point.X,y=point.Y,width=control.Bounds.Width,height=control.Bounds.Height};}
        void Compare(string id,Control control,JsonElement expected)
        {
            var actual=JsonSerializer.SerializeToElement(Box(control));
            foreach(var key in new[]{"x","y","width","height"})
            {
                var a=actual.GetProperty(key).GetDouble();var e=expected.GetProperty(key).GetDouble();
                checks.Add(new{id=id+"-"+key,passed=Math.Abs(a-e)<=1.0/64,actual=a,expected=e});
            }
        }
        async Task Flush()
        {await Dispatcher.UIThread.InvokeAsync(()=>{},DispatcherPriority.Loaded);window.UpdateLayout();if(ReplicaPlatform.IsHeadless)AvaloniaHeadlessPlatform.ForceRenderTimerTick(1);window.UpdateLayout();}
        foreach(var row in oracle.RootElement.GetProperty("rows").EnumerateArray())
        {
            var id=row.GetProperty("id").GetString()!;
            var first=row.GetProperty("states")[0].GetProperty("inner");
            window.Width=first.GetProperty("width").GetDouble();window.Height=first.GetProperty("height").GetDouble();
            workspace.SetLanguage(row.GetProperty("english").GetBoolean());workspace.SetScope(["data","schema","access","objects","trial"],ReplicaSampleRun.AllowedTables);
            await workspace.DispatchAsync("sample");await Flush();
            foreach(var state in row.GetProperty("states").EnumerateArray())
            {
                var type=state.GetProperty("type").GetString();var key=state.GetProperty("key");
                if(type=="detail")workspace.ShowResultDetails(key.GetString()!);else workspace.ShowInfo(type=="permissions");
                await Flush();
                var dialog=(Control?)workspace.ResultDetails??workspace.InfoDialog??throw new InvalidOperationException("Expected modal");
                var surface=workspace.ResultDetails?.DialogBox??workspace.InfoDialog!.DialogBox;
                var body=workspace.ResultDetails?.BodyViewport??workspace.InfoDialog!.BodyViewport;
                var scope=id+"-"+type+"-"+(key.ValueKind==JsonValueKind.String?key.GetString():"info");
                Compare(scope,surface,state.GetProperty("box"));Compare(scope+"-body",body,state.GetProperty("body"));
                rows.Add(new{id=scope,modal=Box(surface),body=Box(body),inner=new{width=window.ClientSize.Width,height=window.ClientSize.Height},document=new{vertical=workspace.DocumentVertical?.GutterWidth??0,horizontal=workspace.DocumentHorizontal?.GutterHeight??0}});
                if(workspace.ResultDetails is{} details)await details.CloseAsync();else await workspace.InfoDialog!.CloseAsync();await Flush();
            }
        }
        File.WriteAllText(Path.Combine(output,"pageintegration-modal-bounds.json"),JsonSerializer.Serialize(new{phase=checks.All(x=>JsonSerializer.SerializeToElement(x).GetProperty("passed").GetBoolean())?"PASSED":"DIFFERENCES",headless=ReplicaPlatform.IsHeadless,scope="Only new KOEN small viewport modal/body bounds; prior callbacks/input/child proofs reused",rows,checks},new JsonSerializerOptions{WriteIndented=true}));
        if(checks.Any(x=>!JsonSerializer.SerializeToElement(x).GetProperty("passed").GetBoolean()))throw new InvalidOperationException("Small viewport modal differs from source");
    }
}
