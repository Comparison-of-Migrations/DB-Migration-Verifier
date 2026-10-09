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

internal sealed class PreparedDbProbeApplication:Application
{
    internal sealed record Case(string Id,string Lang,double ParentWidth,double ViewportWidth,string Dbms,string Database,string Host);
    public override void OnFrameworkInitializationCompleted()
    {
        var life=(IClassicDesktopStyleApplicationLifetime)ApplicationLifetime!;var args=life.Args!;var cases=JsonSerializer.Deserialize<Case[]>(File.ReadAllText(args[1]),new JsonSerializerOptions{PropertyNameCaseInsensitive=true})!;var output=args[2];Directory.CreateDirectory(output);
        var n=new WebPreparedDb{ReducedMotion=true};var canvas=new Canvas{Width=1100,Height=650,UseLayoutRounding=false,Background=Brush.Parse("#313338")};canvas.Children.Add(n);Canvas.SetLeft(n,30);Canvas.SetTop(n,30);
        var w=new Window{Title="Replica PreparedDb verification",Width=1130,Height=750,Content=canvas,UseLayoutRounding=false};life.MainWindow=w;
        object Box(Control e){var p=e.TranslatePoint(default,n)??default;return new{x=p.X,y=p.Y,width=e.Bounds.Width,height=e.Bounds.Height};}
        string Rgb(IBrush? brush){var c=((ISolidColorBrush)brush!).Color;return $"rgb({c.R}, {c.G}, {c.B})";}
        string Radius(){var c=((WebSurfaceBox)n.Child!).CornerRadius;return $"{c.TopLeft}px {c.TopRight}px {c.BottomRight}px {c.BottomLeft}px";}
        object Read(Case c)=>new{id=c.Id,width=n.Bounds.Width,height=n.Bounds.Height,parts=new{dbIcon=Box(n.DatabaseIcon),body=Box(n.TextGroup),name=Box(n.NameText),meta=Box(n.MetaText),hint=Box(n.HintText),checkIcon=Box(n.CheckIcon)},texts=new{name=n.NameText.Text,meta=n.MetaText.Text,hint=n.HintText.Text},palette=new{background=Rgb(((WebSurfaceBox)n.Child!).Background),border=Rgb(((WebSurfaceBox)n.Child!).BorderBrush),name=Rgb(n.NameText.Foreground),meta=Rgb(n.MetaText.Foreground),dbIcon=Rgb(n.DatabaseIcon.Stroke),checkIcon=Rgb(n.CheckIcon.Stroke)},radius=Radius(),role=AutomationProperties.GetLiveSetting(n)==AutomationLiveSetting.Polite?"status":null};
        w.Opened+=(_,_)=>Dispatcher.UIThread.Post(async()=>
        {
            var rows=new List<object>();foreach(var c in cases){n.MaxWidth=Math.Floor((c.ViewportWidth<=1180?.66:.70)*c.ParentWidth*w.RenderScaling*64)/(w.RenderScaling*64);n.SetPresentation(c.Dbms,c.Database,c.Host,c.Lang=="en");await Dispatcher.UIThread.InvokeAsync(()=>{},DispatcherPriority.Background);n.InvalidateMeasure();w.UpdateLayout();rows.Add(Read(c));}
            using var bmp=new RenderTargetBitmap(new PixelSize(2200,1300),new Vector(192,192));bmp.Render(canvas);bmp.Save(Path.Combine(output,"prepareddb.png"),new PngBitmapEncoderOptions());File.WriteAllText(Path.Combine(output,"prepareddb-native.json"),JsonSerializer.Serialize(new{dpr=w.RenderScaling,rows},new JsonSerializerOptions{WriteIndented=true}));var lifecycle=new List<object>();
            void State(string label){w.UpdateLayout();lifecycle.Add(new{label,attached=n.IsAttachedToVisualTree(),reduced=n.ReducedMotion,opacity=n.Opacity,y=(n.RenderTransform as TranslateTransform)?.Y??0,name=n.NameText.Text,live=AutomationProperties.GetLiveSetting(n).ToString()});}
            canvas.Children.Remove(n);n.ReducedMotion=false;n.SetPresentation("SQL Server","migration_test","trial.example.invalid",false);canvas.Children.Add(n);State("normal-begin");await Task.Delay(260);State("normal-end");
            n.SetPresentation("PostgreSQL","sample_updated","db.example.invalid",true);State("attached-update");canvas.Children.Remove(n);State("detached-end");
            n.ReducedMotion=true;canvas.Children.Add(n);State("reduced-begin");canvas.Children.Remove(n);State("reduced-detached");
            n.ReducedMotion=false;canvas.Children.Add(n);State("reattach-begin");canvas.Children.Remove(n);State("detach-during-motion");
            canvas.Children.Add(n);State("second-reattach-begin");n.ReducedMotion=true;State("reduce-during-motion");
            File.WriteAllText(Path.Combine(output,"prepareddb-lifecycle.json"),JsonSerializer.Serialize(new{capturedAtUtc=DateTime.UtcNow,dpr=w.RenderScaling,rows=lifecycle},new JsonSerializerOptions{WriteIndented=true}));
            w.Close();Console.WriteLine("Recorded "+rows.Count+" PreparedDb states and "+lifecycle.Count+" attached lifecycle states");
        },DispatcherPriority.Background);base.OnFrameworkInitializationCompleted();
    }
}
