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

internal sealed class RunningDotProbeApplication:Application
{
    public override void OnFrameworkInitializationCompleted()
    {
        var life=(IClassicDesktopStyleApplicationLifetime)ApplicationLifetime!;var args=life.Args!;var output=args[2];Directory.CreateDirectory(output);
        var source=JsonDocument.Parse(File.ReadAllText(args[1]));var canvas=new Canvas{Width=1000,Height=500,Background=Brush.Parse("#313338"),UseLayoutRounding=false};var dots=new List<WebRunningDot>();var ids=new List<string>();
        foreach(var c in source.RootElement.GetProperty("rows").EnumerateArray()){var n=new WebRunningDot{ReducedMotion=c.GetProperty("reduced").GetBoolean()};var id=c.GetProperty("id").GetString()!;AutomationProperties.SetAutomationId(n,id);Canvas.SetLeft(n,30);Canvas.SetTop(n,39+dots.Count*44);canvas.Children.Add(n);dots.Add(n);ids.Add(id);}
        var window=new Window{Title="Replica RunningDot verification",Width=1020,Height=620,Content=canvas,UseLayoutRounding=false};life.MainWindow=window;
        bool Playing(WebRunningDot n)=>typeof(WebRunningDot).GetField("_playback",BindingFlags.Instance|BindingFlags.NonPublic)!.GetValue(n)is WebMotionPlayback playback&&!playback.Completion.IsCompleted;
        object Read(int i){var n=dots[i];return new{id=ids[i],x=n.Bounds.X-30,y=n.Bounds.Y-(30+i*44),width=n.Bounds.Width,height=n.Bounds.Height,marginRight=n.Margin.Right,color="rgb(88, 101, 242)",opacity=n.Opacity,reduced=n.ReducedMotion,playing=Playing(n),focusable=n.Focusable,accessibility=AutomationProperties.GetAccessibilityView(n).ToString(),desiredWidth=n.DesiredSize.Width};}
        window.Opened+=(_,_)=>Dispatcher.UIThread.Post(async()=>
        {
            window.UpdateLayout();var rows=dots.Select((_,i)=>Read(i)).ToArray();var n=dots[0];var records=new List<object>();void Record(string label){window.UpdateLayout();records.Add(new{label,opacity=n.Opacity,playing=Playing(n),reduced=n.ReducedMotion,attached=n.IsAttachedToVisualTree(),width=n.Bounds.Width,height=n.Bounds.Height});}
            Record("attached-normal");await Task.Delay(150);Record("pulse-advanced");n.ReducedMotion=true;Record("reduced-restores-one");canvas.Children.Remove(n);Record("detached-reduced");canvas.Children.Add(n);Record("remount-reduced");n.ReducedMotion=false;Record("normal-restart");await Task.Delay(150);Record("restarted-advances");canvas.Children.Remove(n);Record("detach-restores-one");canvas.Children.Add(n);n.ReducedMotion=true;window.UpdateLayout();
            using(var png=new RenderTargetBitmap(new(2000,1000),new(192,192))){png.Render(canvas);png.Save(Path.Combine(output,"runningdot.png"),new PngBitmapEncoderOptions());}
            File.WriteAllText(Path.Combine(output,"runningdot-native.json"),JsonSerializer.Serialize(new{dpr=window.RenderScaling,rows,lifecycle=records},new JsonSerializerOptions{WriteIndented=true}));
            if(args[0]!="--runningdot-window")window.Close();Console.WriteLine("Recorded RunningDot8/lifecycle8");
        },DispatcherPriority.Background);base.OnFrameworkInitializationCompleted();
    }
}
