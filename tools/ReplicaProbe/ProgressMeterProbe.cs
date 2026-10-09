using System.Reflection;
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

internal sealed class ProgressMeterProbeApplication:Application
{
    private sealed record Case(string Id,string Lang,double Width,long Done,long Total);
    public override void OnFrameworkInitializationCompleted()
    {
        var life=(IClassicDesktopStyleApplicationLifetime)ApplicationLifetime!;var args=life.Args!;
        var cases=JsonSerializer.Deserialize<Case[]>(File.ReadAllText(args[1]),new JsonSerializerOptions{PropertyNameCaseInsensitive=true})!;var output=args[2];Directory.CreateDirectory(output);
        var canvas=new Canvas{Width=1000,Height=500,Background=Brush.Parse("#313338"),UseLayoutRounding=false};var meters=new List<WebProgressMeter>();
        foreach(var c in cases){var n=new WebProgressMeter{Width=c.Width,Height=6};n.SetPresentation(c.Done,c.Total,c.Lang=="en",false);AutomationProperties.SetAutomationId(n,c.Id);Canvas.SetLeft(n,30);Canvas.SetTop(n,30+14*meters.Count);canvas.Children.Add(n);meters.Add(n);}
        var w=new Window{Title="Replica ProgressMeter verification",Width=1020,Height=620,Content=canvas,UseLayoutRounding=false};life.MainWindow=w;
        string Rgb(IBrush? brush){var c=((ISolidColorBrush)brush!).Color;return $"rgb({c.R}, {c.G}, {c.B})";}
        object Read(int i){var n=meters[i];var c=cases[i];return new{id=c.Id,height=n.Bounds.Height,width=n.Bounds.Width,barWidth=n.Bar.Bounds.Width*n.VisibleFraction,background=Rgb(((WebSurfaceBox)n.Children[0]).Background),barBackground=Rgb(n.Bar.Background),radius="8px",overflow=n.Clip is null?"visible":"hidden",origin="0px 3px",label=AutomationProperties.GetName(n),minimum=0,maximum=n.Total,value=n.Processed,focusable=n.Focusable};}
        var lifecycle=new List<object>();var timing=new List<object>();
        void Timings()
        {
            var type=typeof(WebProgressMeter).GetNestedType("Transition",BindingFlags.NonPublic)!;double clock=0;var track=Activator.CreateInstance(type,BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic,null,new object[]{(Func<double>)(()=>clock)},null)!;
            void Snap(double v){type.GetMethod("Snap")!.Invoke(track,new object[]{v});}void Target(double v){type.GetMethod("SetTarget")!.Invoke(track,new object[]{v});}
            void Sample(string label,double t){clock=t;timing.Add(new{label,time=t,duration=(double)type.GetField("_duration",BindingFlags.NonPublic|BindingFlags.Instance)!.GetValue(track)!,fraction=(double)type.GetProperty("Current")!.GetValue(track)!});}
            foreach(var tuple in new[]{(0d,1d,"forward"),(1d,0d,"reverse")}){clock=0;Snap(tuple.Item1);Target(tuple.Item2);foreach(var t in new[]{0d,50,100,150,200})Sample(tuple.Item3+"-"+t,t);}
            clock=0;Snap(0);Target(1);Sample("retarget-before",70);Target(.5);foreach(var t in new[]{0d,50,200}){clock=70+t;timing.Add(new{label="retarget-"+t,time=t,duration=(double)type.GetField("_duration",BindingFlags.NonPublic|BindingFlags.Instance)!.GetValue(track)!,fraction=(double)type.GetProperty("Current")!.GetValue(track)!});}
            clock=0;Snap(0);Target(1);Sample("reversal-before",80);Target(0);foreach(var t in new[]{0d,40,80}){clock=80+t;timing.Add(new{label="reversal-"+t,time=t,duration=(double)type.GetField("_duration",BindingFlags.NonPublic|BindingFlags.Instance)!.GetValue(track)!,fraction=(double)type.GetProperty("Current")!.GetValue(track)!});}
        }
        w.Opened+=(_,_)=>Dispatcher.UIThread.Post(async()=>
        {
            w.UpdateLayout();var rows=cases.Select((_,i)=>Read(i)).ToArray();using(var bitmap=new RenderTargetBitmap(new(2000,1000),new(192,192))){bitmap.Render(canvas);bitmap.Save(Path.Combine(output,"progressmeter.png"),new PngBitmapEncoderOptions());}
            var n=meters[0];void Record(string label){w.UpdateLayout();lifecycle.Add(new{label,processed=n.Processed,total=n.Total,fraction=n.VisibleFraction,width=n.Bounds.Width,name=AutomationProperties.GetName(n),attached=n.IsAttachedToVisualTree(),focusable=n.Focusable});}
            Record("initial-no-entrance");n.SetPresentation(50,50);Record("normal-start");await Task.Delay(240);Record("normal-end");n.ReducedMotion=true;n.SetPresentation(25,50,true);Record("reduced-update-language");n.Width=656;Record("resize");canvas.Children.Remove(n);Record("detach");canvas.Children.Add(n);Record("remount-no-entrance");Timings();
            File.WriteAllText(Path.Combine(output,"progressmeter-native.json"),JsonSerializer.Serialize(new{dpr=w.RenderScaling,rows},new JsonSerializerOptions{WriteIndented=true}));File.WriteAllText(Path.Combine(output,"progressmeter-lifecycle.json"),JsonSerializer.Serialize(lifecycle,new JsonSerializerOptions{WriteIndented=true}));File.WriteAllText(Path.Combine(output,"progressmeter-timing.json"),JsonSerializer.Serialize(timing,new JsonSerializerOptions{WriteIndented=true}));
            if(args[0]!="--progressmeter-window"){w.Close();Console.WriteLine("Recorded 30 ProgressMeter endpoints,18 transition samples,7 attached lifecycle states");}
        },DispatcherPriority.Background);base.OnFrameworkInitializationCompleted();
    }
}
