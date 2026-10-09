using System.Text.Json;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Verifier.Desktop;
internal sealed class InfoDialogBodyProbeApplication:Application
{
    private sealed record Case(string Id,double Width,bool English,bool Compact,bool Permissions,string Title);
    public override void OnFrameworkInitializationCompleted()
    {
        var life=(IClassicDesktopStyleApplicationLifetime)ApplicationLifetime!;var args=life.Args!;var cases=JsonSerializer.Deserialize<Case[]>(File.ReadAllText(args[1]),new JsonSerializerOptions{PropertyNameCaseInsensitive=true})!;var output=args[2];Directory.CreateDirectory(output);
        var n=new WebInfoDialogBody{ReducedMotion=true,Width=570};var canvas=new Canvas{Width=900,Height=800,Background=Brush.Parse("#2b2d31"),UseLayoutRounding=false};canvas.Children.Add(n);Canvas.SetLeft(n,30);Canvas.SetTop(n,100);var w=new Window{Title="Replica InfoDialogBody verification",Width=1000,Height=900,Content=canvas,UseLayoutRounding=false};life.MainWindow=w;
        object? Box(Control? c){if(c is null)return null;var p=c.TranslatePoint(default,n)??default;return new{x=p.X,y=p.Y,width=c.Bounds.Width,height=c.Bounds.Height};}
        void Present(Case c){n.Width=c.Width;n.SetPresentation(c.Permissions,c.English,c.Compact);w.UpdateLayout();}
        object Read(Case c)=>new{id=c.Id,root=Box(n),paragraphs=n.Paragraphs.Select(p=>new{box=Box(p),text=p.Text}).ToArray(),summary=Box(n.Summary),items=n.Summary?.Rows.Select(x=>new{row=Box(x),label=Box(x.LabelText),value=Box(x.ValueControl),labelText=x.LabelText.Text,valueText=string.Concat(x.ValueLines.Select(y=>y.Text))}).Cast<object>().ToArray()??[],notice=Box(n.Notice),noticeTitle=n.Notice?.Title,noticeText=n.Notice?.Text};
        w.Opened+=(_,_)=>Dispatcher.UIThread.Post(async()=>{
            var rows=new List<object>();foreach(var c in cases){Present(c);await Dispatcher.UIThread.InvokeAsync(()=>{},DispatcherPriority.Background);w.UpdateLayout();rows.Add(Read(c));if(c.Id is "compact-ko-570-permissions" or "wide-en-570-research"){using var bmp=new RenderTargetBitmap(new PixelSize(1800,1600),new Vector(192,192));bmp.Render(canvas);bmp.Save(Path.Combine(output,"infodialogbody-"+c.Id+".png"),new PngBitmapEncoderOptions());}}
            File.WriteAllText(Path.Combine(output,"infodialogbody-native.json"),JsonSerializer.Serialize(new{dpr=w.RenderScaling,rows},new JsonSerializerOptions{WriteIndented=true}));
            var states=new List<object>();void State(string label){w.UpdateLayout();states.Add(new{label,permissions=n.Permissions,english=n.English,attached=n.IsAttachedToVisualTree(),notice=n.Notice is{} notice?new{attached=notice.IsAttachedToVisualTree(),opacity=notice.Opacity,y=(notice.RenderTransform as TranslateTransform)?.Y??0}:null,paragraphCount=n.Paragraphs.Count,summaryCount=n.Summary?.Rows.Count??0});}
            n.ReducedMotion=false;Present(cases.First(c=>c.Id=="wide-en-570-permissions"));State("normal-mount");await Task.Delay(260);State("normal-end");var old=n.Notice!;Present(cases.First(c=>c.Id=="wide-en-570-research"));State("research-replaces-permissions");states.Add(new{label="removed-notice",attached=old.IsAttachedToVisualTree(),opacity=old.Opacity,y=(old.RenderTransform as TranslateTransform)?.Y??0});canvas.Children.Remove(n);State("body-detached");canvas.Children.Add(n);State("body-reattached");n.ReducedMotion=true;Present(cases.First(c=>c.Id=="compact-ko-570-permissions"));State("reduced-permissions");
            File.WriteAllText(Path.Combine(output,"infodialogbody-lifecycle.json"),JsonSerializer.Serialize(new{capturedAtUtc=DateTime.UtcNow,rows=states},new JsonSerializerOptions{WriteIndented=true}));w.Close();Console.WriteLine($"Recorded {rows.Count} InfoDialogBody cases and {states.Count} lifecycle states");
        },DispatcherPriority.Background);base.OnFrameworkInitializationCompleted();
    }
}
