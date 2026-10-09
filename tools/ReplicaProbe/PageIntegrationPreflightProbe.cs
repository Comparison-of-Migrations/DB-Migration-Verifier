using System.Text.Json;
using Avalonia;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Input;
using Avalonia.Threading;
using Verifier.Desktop;

internal static class PageIntegrationPreflightProbe
{
    internal static async Task Check(ReplicaWorkspace workspace,Window window,string output,string casesPath,string oraclePath)
    {
        using var cases=JsonDocument.Parse(File.ReadAllText(casesPath));
        using var oracle=JsonDocument.Parse(File.ReadAllText(oraclePath));
        using var flow=JsonDocument.Parse(File.ReadAllText(Path.Combine(Path.GetDirectoryName(oraclePath)! ,"root-appflow-mac-source.json")));
        var checks=new List<object>();
        void Assert(string id,bool passed,object value)
        {checks.Add(new{id,passed,value});File.WriteAllText(Path.Combine(output,"preflight-connected-checks.json"),JsonSerializer.Serialize(checks,new JsonSerializerOptions{WriteIndented=true}));if(!passed)throw new InvalidOperationException(id);}
        async Task Flush(){await Dispatcher.UIThread.InvokeAsync(()=>{},DispatcherPriority.Background);window.UpdateLayout();}
        object Box(Control c){var p=c.TranslatePoint(default,workspace)??default;return new{x=p.X,y=p.Y,width=c.Bounds.Width,height=c.Bounds.Height};}
        workspace.ReducedMotion=true;
        foreach(var spec in cases.RootElement.EnumerateArray())
        {
            var id=spec.GetProperty("id").GetString()!;
            var expected=oracle.RootElement.GetProperty("rows").EnumerateArray().Single(x=>x.GetProperty("id").GetString()==id);
            window.Width=spec.GetProperty("width").GetDouble();window.Height=spec.GetProperty("height").GetDouble();workspace.SetLanguage(spec.GetProperty("language").GetString()=="en");workspace.Navigate("capabilities");await Flush();
            var page=(WebPreflightPage)workspace.CurrentPage!;
            foreach(var pair in new(string key,Control control)[]{("topbar",workspace.TopBar),("sidebar",workspace.Sidebar),("content",workspace.Viewport),("page",page),("heading",page.Heading),("footer",workspace.Footer)})
            {
                using var actual=JsonDocument.Parse(JsonSerializer.Serialize(Box(pair.control)));var expectedBox=expected.GetProperty(pair.key);
                Assert(id+"-"+pair.key,expectedBox.EnumerateObject().All(p=>Math.Abs(p.Value.GetDouble()-actual.RootElement.GetProperty(p.Name).GetDouble())<.02),Box(pair.control));
            }
            Assert(id+"-navigation",page.Heading.TitleText.Text==expected.GetProperty("title").GetString()&&page.Heading.FocusTarget.IsFocused&&workspace.Announcement==expected.GetProperty("announcement").GetString()&&workspace.Viewport.Offset==expected.GetProperty("scroll").GetDouble(),new{workspace.Announcement,page.Heading.FocusTarget.IsFocused,workspace.Viewport.Offset});
            var primary=expected.GetProperty("footerActions").EnumerateArray().Last();
            Assert(id+"-footer",AutomationProperties.GetAutomationId(workspace.Footer.ActionButtons.Single())=="Footer-"+primary.GetProperty("action").GetString()&&Equals(workspace.Footer.ActionButtons.Single().Content,primary.GetProperty("label").GetString()),workspace.Footer.ActionButtons.Single().Content!);
        }
        // Only newly connected parent callbacks are checked; standalone page/row states stay reused.
        window.Width=1280;window.Height=900;
        foreach(var english in new[]{false,true})
        {
            var prefix=english?"en-":"ko-";workspace.SetLanguage(english);await workspace.DispatchAsync("start");await workspace.DispatchAsync("connect");await workspace.DispatchAsync("preflight");await Flush();
            var page=(WebPreflightPage)workspace.CurrentPage!;
            var source=flow.RootElement.GetProperty("rows").EnumerateArray().Single(x=>x.GetProperty("id").GetString()==prefix+"preflight");
            Assert(prefix+"route",workspace.Screen==source.GetProperty("screen").GetString()&&workspace.Connection.Connected&&workspace.Visited.Contains("capabilities")&&page.Heading.FocusTarget.IsFocused,workspace.Screen);
            var permission=page.Table.Rows.Single(x=>x.Key=="access").PermissionsButton;permission.Focus(NavigationMethod.Pointer);permission.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));await Flush();
            Assert(prefix+"permission-route",workspace.InfoDialog is{IsVisible:true} dialog&&dialog.CloseButton.IsFocused&&!permission.IsEffectivelyEnabled,workspace.InfoDialog?.Body.DialogTitle??"");
            await workspace.InfoDialog!.CloseAsync();await Flush();
            Assert(prefix+"permission-return",workspace.InfoDialog is null&&permission.IsFocused&&permission.IsEffectivelyEnabled&&ReferenceEquals(workspace.CurrentPage,page),new{permission.IsFocused});
            await workspace.DispatchAsync("back");await Flush();Assert(prefix+"back-connected",workspace.Screen=="connect"&&workspace.CurrentPage is WebConnectionPage&&workspace.Connection.Connected,workspace.Screen);
        }
    }
}
