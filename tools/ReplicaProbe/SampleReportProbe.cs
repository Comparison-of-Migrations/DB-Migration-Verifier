using System.Text.Json;
using Avalonia;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using Verifier.Desktop;
internal sealed class SampleReportProbeApplication:Application
{
    private sealed record Case(string Id,double Width,bool English);
    public override void OnFrameworkInitializationCompleted()
    {
        var life=(IClassicDesktopStyleApplicationLifetime)ApplicationLifetime!;var args=life.Args!;var cases=JsonSerializer.Deserialize<Case[]>(File.ReadAllText(args[1]),new JsonSerializerOptions{PropertyNameCaseInsensitive=true})!;var output=args[2];Directory.CreateDirectory(output);
        var n=new WebSampleReport{Width=270};var canvas=new Canvas{Width=900,Height=520,Background=Brush.Parse("#313338"),UseLayoutRounding=false};canvas.Children.Add(n);Canvas.SetLeft(n,30);Canvas.SetTop(n,60);var w=new Window{Title="Replica SampleReport verification",Width=940,Height=590,Content=canvas,UseLayoutRounding=false};life.MainWindow=w;
        object Box(Control c){var p=c.TranslatePoint(default,n)??default;return new{x=p.X,y=p.Y,width=c.Bounds.Width,height=c.Bounds.Height};}
        string Color(IBrush? b){var c=((ISolidColorBrush)b!).Color;return $"rgb({c.R}, {c.G}, {c.B})";}
        object Text(WebSystemTextBlock t)=>new{box=Box(t),text=t.Text,color=Color(t.Foreground)};
        object Block(Control b,IReadOnlyList<WebSystemTextBlock> lines)=>new{box=Box(b),text=string.Join("\n",lines.Select(x=>x.Text)),color=Color(lines[0].Foreground)};
        void Capture(string name){using var bmp=new RenderTargetBitmap(new PixelSize(1800,1040),new Vector(192,192));bmp.Render(canvas);bmp.Save(Path.Combine(output,name),new PngBitmapEncoderOptions());}
        w.Opened+=(_,_)=>Dispatcher.UIThread.Post(async()=>{
            var rows=new List<object>();foreach(var c in cases){n.Width=Math.Floor(c.Width*w.RenderScaling*64)/(w.RenderScaling*64);n.SetPresentation(c.English);await Dispatcher.UIThread.InvokeAsync(()=>{},DispatcherPriority.Background);w.UpdateLayout();rows.Add(new{id=c.Id,root=Box(n),head=Box(n.Head),headText=Text(n.HeadText),icon=Box(n.Icon),title=Block(n.TitleBlock,n.TitleLines),lines=n.Lines.Select(x=>new{box=Box(x),label=Text(x.Label),value=Text(x.Value)}).ToArray(),foot=Block(n.FootBlock,n.FootLines),name=AutomationProperties.GetName(n),background=Color(n.Surface.Background),border=Color(n.Surface.BorderBrush),radius=n.Surface.CornerRadius.TopLeft});if(c.Id is "ko-270" or "en-375.2")Capture("samplereport-"+c.Id+".png");}
            var lifecycle=new List<object>();foreach(var english in new[]{false,true,false}){canvas.Children.Remove(n);await Dispatcher.UIThread.InvokeAsync(()=>{},DispatcherPriority.Background);n.SetPresentation(english);canvas.Children.Add(n);await Dispatcher.UIThread.InvokeAsync(()=>{},DispatcherPriority.Background);w.UpdateLayout();lifecycle.Add(new{english,attached=canvas.Children.Contains(n),name=AutomationProperties.GetName(n),height=n.Bounds.Height,opacity=n.Opacity,transform=n.RenderTransform?.ToString()});}
            Capture("samplereport.png");File.WriteAllText(Path.Combine(output,"samplereport-native.json"),JsonSerializer.Serialize(new{dpr=w.RenderScaling,rows,lifecycle},new JsonSerializerOptions{WriteIndented=true}));w.Close();Console.WriteLine("Recorded "+rows.Count+" SampleReport cases");
        },DispatcherPriority.Background);
        base.OnFrameworkInitializationCompleted();
    }
}
