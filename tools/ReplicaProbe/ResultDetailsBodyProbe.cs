using System.Text.Json;
using Avalonia;
using Avalonia.Automation.Peers;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Verifier.Desktop;

internal sealed class ResultDetailsBodyProbeApplication:Application
{
    internal sealed record Case(string Id,string Lang,double Width,string Key,string[] Items,bool HasGroup,string Status,int Done,int Total);
    public override void OnFrameworkInitializationCompleted()
    {
        var life=(IClassicDesktopStyleApplicationLifetime)ApplicationLifetime!;var args=life.Args!;var cases=JsonSerializer.Deserialize<Case[]>(File.ReadAllText(args[1]),new JsonSerializerOptions{PropertyNameCaseInsensitive=true})!;var output=args[2];Directory.CreateDirectory(output);
        var n=new WebResultDetailsBody{ReducedMotion=true,Width=570};var canvas=new Canvas{Width=760,Height=550,UseLayoutRounding=false,Background=Brush.Parse("#2b2d31")};canvas.Children.Add(n);Canvas.SetLeft(n,30);Canvas.SetTop(n,30);var w=new Window{Title="Replica ResultDetailsBody verification",Width=800,Height=620,Content=canvas,UseLayoutRounding=false};life.MainWindow=w;
        object? Box(Control? e){if(e is null)return null;var p=e.TranslatePoint(default,n)??default;return new{x=p.X,y=p.Y,width=e.Bounds.Width,height=e.Bounds.Height};}
        void Present(Case c){n.Width=c.Width;n.SetPresentation(c.Items,c.Key=="trial",c.HasGroup?new(c.Done,c.Total,c.Status):null,c.Lang=="en");w.UpdateLayout();}
        object Read(Case c)=>new{id=c.Id,width=n.Bounds.Width,height=n.Bounds.Height,list=Box(n.CheckList),scope=Box(n.ScopeNotice),cleanup=Box(n.CleanupNotice),privacy=Box(n.PrivacyText),scopeTitle=n.ScopeNotice?.Title,scopeText=n.ScopeNotice?.Text,cleanupTitle=n.CleanupNotice?.Title,cleanupText=n.CleanupNotice?.Text,privacyText=n.PrivacyText.Text,items=n.CheckList.Items.Select(x=>x.Text.Text).ToArray()};
        w.Opened+=(_,_)=>Dispatcher.UIThread.Post(async()=>
        {
            var rows=new List<object>();var peerStates=new List<object>();foreach(var c in cases){Present(c);await Dispatcher.UIThread.InvokeAsync(()=>{},DispatcherPriority.Background);w.UpdateLayout();rows.Add(Read(c));var peer=ControlAutomationPeer.CreatePeerForElement(n.CheckList)!;peerStates.Add(new{id=c.Id,listRole=peer.GetAutomationControlType().ToString(),items=peer.GetChildren()?.Count??0,children=n.Children.Count,scopeAttached=n.ScopeNotice?.IsAttachedToVisualTree()??false,cleanupAttached=n.CleanupNotice?.IsAttachedToVisualTree()??false});}
            File.WriteAllText(Path.Combine(output,"detailsbody-native.json"),JsonSerializer.Serialize(new{dpr=w.RenderScaling,rows},new JsonSerializerOptions{WriteIndented=true}));File.WriteAllText(Path.Combine(output,"detailsbody-peer.json"),JsonSerializer.Serialize(new{capturedAtUtc=DateTime.UtcNow,rows=peerStates},new JsonSerializerOptions{WriteIndented=true}));Present(cases.First(c=>c.Id=="ko-570-trial-completed"));using var bmp=new RenderTargetBitmap(new PixelSize(1520,1100),new Vector(192,192));bmp.Render(canvas);bmp.Save(Path.Combine(output,"detailsbody.png"),new PngBitmapEncoderOptions());
            var lifecycle=new List<object>();void State(string label){w.UpdateLayout();lifecycle.Add(new{label,scope=n.ScopeNotice is not {} a?null:new{attached=a.IsAttachedToVisualTree(),opacity=a.Opacity,y=(a.RenderTransform as TranslateTransform)?.Y??0},cleanup=n.CleanupNotice is not {} b?null:new{attached=b.IsAttachedToVisualTree(),opacity=b.Opacity,y=(b.RenderTransform as TranslateTransform)?.Y??0},children=n.Children.Count});}
            n.ReducedMotion=false;Present(cases.First(c=>c.Id=="en-570-trial-completed"));State("normal-mount");await Task.Delay(260);State("normal-end");var old=n.CleanupNotice??throw new InvalidOperationException("Cleanup required");Present(cases.First(c=>c.Id=="en-570-data-absent"));State("without-notices");lifecycle.Add(new{label="removed-cleanup",attached=old.IsAttachedToVisualTree(),opacity=old.Opacity,y=(old.RenderTransform as TranslateTransform)?.Y??0});n.ReducedMotion=true;Present(cases.First(c=>c.Id=="ko-570-trial-pending"));State("reduced-pending");File.WriteAllText(Path.Combine(output,"detailsbody-lifecycle.json"),JsonSerializer.Serialize(new{capturedAtUtc=DateTime.UtcNow,rows=lifecycle},new JsonSerializerOptions{WriteIndented=true}));w.Close();Console.WriteLine($"Recorded {rows.Count} ResultDetailsBody layout/update/peer and {lifecycle.Count} lifecycle states");
        },DispatcherPriority.Background);base.OnFrameworkInitializationCompleted();
    }
}
