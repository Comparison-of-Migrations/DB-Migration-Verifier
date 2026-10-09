using System.Text.Json;
using Avalonia;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Verifier.Desktop;

internal sealed class TrialPreparationProbeApplication:Application
{
    internal sealed record Case(string Id,string Lang,double ParentWidth,double ViewportWidth,bool Selected,bool Ready,string Dbms,string Database,string Host);
    public override void OnFrameworkInitializationCompleted()
    {
        var life=(IClassicDesktopStyleApplicationLifetime)ApplicationLifetime!;var args=life.Args!;var cases=JsonSerializer.Deserialize<Case[]>(File.ReadAllText(args[1]),new JsonSerializerOptions{PropertyNameCaseInsensitive=true})!;var output=args[2];Directory.CreateDirectory(output);bool interactive=args[0].EndsWith("-window");
        var n=new WebTrialPreparation{ReducedMotion=true};var canvas=new Canvas{Width=1100,Height=650,UseLayoutRounding=false,Background=Brush.Parse("#313338")};canvas.Children.Add(n);Canvas.SetLeft(n,30);Canvas.SetTop(n,30);
        var w=new Window{Title="Replica TrialPreparation verification",Width=1130,Height=750,Content=canvas,UseLayoutRounding=false};life.MainWindow=w;var events=new List<string>();n.PreparationRequested+=(_,_)=>events.Add("trial");
        object? Box(Control? e){if(e is null)return null;var p=e.TranslatePoint(default,n)??default;return new{x=p.X,y=p.Y,width=e.Bounds.Width,height=e.Bounds.Height};}
        void Present(Case c){n.Width=c.ParentWidth;n.ViewportWidth=c.ViewportWidth;n.SetPresentation(c.Selected,c.Ready,c.Dbms,c.Database,c.Host,c.Lang=="en");w.UpdateLayout();}
        object Read(Case c)=>new{id=c.Id,x=n.Bounds.X-30,y=n.Bounds.Y-30,width=n.Bounds.Width,height=n.Bounds.Height,notice=Box(n.Notice),button=Box(n.PrepareButton),card=Box(n.PreparedDb),title=n.Notice?.Title,text=n.Notice?.Text,buttonText=n.PrepareButton?.Content,cardText=n.PreparedDb is null?null:n.PreparedDb.NameText.Text+n.PreparedDb.MetaText.Text+n.PreparedDb.HintText.Text,kind=n.Notice is null?null:"notice "+n.Notice.Kind,buttonEnabled=n.PrepareButton?.IsEnabled};
        void Record(){w.UpdateLayout();File.WriteAllText(Path.Combine(output,"trialpreparation-input.json"),JsonSerializer.Serialize(new{capturedAtUtc=DateTime.UtcNow,events,visible=n.Children.Count>0,notice=Box(n.Notice),card=Box(n.PreparedDb),button=Box(n.PrepareButton),label=n.PrepareButton?.Content,focus=AutomationProperties.GetAutomationId(w.FocusManager?.GetFocusedElement() as Control??n)},new JsonSerializerOptions{WriteIndented=true}));}
        if(interactive){int i=0;foreach(var (id,label,action) in new(string,string,Action)[]{("unprepared","Unprepared",()=>Present(cases.First(c=>c.Id=="ko-1280-904-unprepared"))),("ready","Ready",()=>Present(cases.First(c=>c.Id=="ko-1280-904-ready"))),("hidden","Hidden",()=>Present(cases.First(c=>c.Id=="ko-1280-904-hidden"))),("english","English",()=>Present(cases.First(c=>c.Id=="en-1280-904-ready"))),("reduced","Toggle reduced",()=>{n.ReducedMotion=!n.ReducedMotion;Present(cases.First(c=>c.Id=="ko-1280-904-ready"));}),("record","Record",Record)}){var b=new Button{Content=label};AutomationProperties.SetAutomationId(b,"TrialPreparation-"+id);b.Click+=(_,_)=>action();canvas.Children.Add(b);Canvas.SetLeft(b,30+i++*160);Canvas.SetTop(b,580);}w.Opened+=(_,_)=>{Present(cases.First(c=>c.Id=="ko-1280-904-unprepared"));Record();};}
        else w.Opened+=(_,_)=>Dispatcher.UIThread.Post(async()=>
        {
            var rows=new List<object>();foreach(var c in cases){Present(c);await Dispatcher.UIThread.InvokeAsync(()=>{},DispatcherPriority.Background);w.UpdateLayout();rows.Add(Read(c));}
            File.WriteAllText(Path.Combine(output,"trialpreparation-native.json"),JsonSerializer.Serialize(new{dpr=w.RenderScaling,rows},new JsonSerializerOptions{WriteIndented=true}));Present(cases.First(c=>c.Id=="ko-1280-904-ready"));using var bmp=new RenderTargetBitmap(new PixelSize(2200,1300),new Vector(192,192));bmp.Render(canvas);bmp.Save(Path.Combine(output,"trialpreparation.png"),new PngBitmapEncoderOptions());
            var lifecycle=new List<object>();void State(string label){w.UpdateLayout();lifecycle.Add(new{label,children=n.Children.Count,height=n.Bounds.Height,notice=n.Notice is not {} note?null:new{attached=note.IsAttachedToVisualTree(),opacity=note.Opacity,y=(note.RenderTransform as TranslateTransform)?.Y??0},card=n.PreparedDb is not {} card?null:new{attached=card.IsAttachedToVisualTree(),opacity=card.Opacity,y=(card.RenderTransform as TranslateTransform)?.Y??0}});}
            n.ReducedMotion=false;Present(cases.First(c=>c.Id=="ko-1280-904-unprepared"));State("unprepared-mount");await Task.Delay(260);State("unprepared-end");Present(cases.First(c=>c.Id=="ko-1280-904-ready"));State("ready-mount");await Task.Delay(260);State("ready-end");var old=n.PreparedDb??throw new InvalidOperationException("Ready card required");Present(cases.First(c=>c.Id=="ko-1280-904-hidden"));State("hidden");lifecycle.Add(new{label="removed-card",attached=old.IsAttachedToVisualTree(),opacity=old.Opacity,y=(old.RenderTransform as TranslateTransform)?.Y??0});n.ReducedMotion=true;Present(cases.First(c=>c.Id=="en-1280-904-ready"));State("reduced-ready");
            File.WriteAllText(Path.Combine(output,"trialpreparation-lifecycle.json"),JsonSerializer.Serialize(new{capturedAtUtc=DateTime.UtcNow,dpr=w.RenderScaling,rows=lifecycle},new JsonSerializerOptions{WriteIndented=true}));w.Close();Console.WriteLine($"Recorded {rows.Count} TrialPreparation states and {lifecycle.Count} lifecycle states");
        },DispatcherPriority.Background);base.OnFrameworkInitializationCompleted();
    }
}
