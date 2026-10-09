using System.Text.Json;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using Verifier.Desktop;
internal sealed class WelcomeStepsProbeApplication:Application
{
    private sealed record Case(string Id,double Width,bool English);
    private sealed class Host:Panel
    {
        public WebWelcomeSteps Group{get;}=new();
        public Host(){UseLayoutRounding=false;Children.Add(Group);}
        protected override Size MeasureOverride(Size available){Group.Measure(new(available.Width,double.PositiveInfinity));return new(available.Width,Group.DesiredSize.Height);}
        protected override Size ArrangeOverride(Size final){Group.Arrange(new(0,0,final.Width,final.Height));return final;}
    }
    public override void OnFrameworkInitializationCompleted()
    {
        var life=(IClassicDesktopStyleApplicationLifetime)ApplicationLifetime!;var args=life.Args!;
        var cases=JsonSerializer.Deserialize<Case[]>(File.ReadAllText(args[1]),new JsonSerializerOptions{PropertyNameCaseInsensitive=true})!;var output=args[2];Directory.CreateDirectory(output);
        var host=new Host{Width=684};var group=host.Group;
        var canvas=new Canvas{Width=1240,Height=420,Background=Brush.Parse("#313338"),UseLayoutRounding=false};canvas.Children.Add(host);Canvas.SetLeft(host,30);Canvas.SetTop(host,60);
        var w=new Window{Title="Replica WelcomeSteps verification",Width=1280,Height=500,Content=canvas,UseLayoutRounding=false};life.MainWindow=w;
        object Box(Control c,Control relative){var p=c.TranslatePoint(default,relative)??default;return new{x=p.X,y=p.Y,width=c.Bounds.Width,height=c.Bounds.Height};}
        object Text(WebSystemTextBlock t,Control relative){var c=((ISolidColorBrush)t.Foreground!).Color;return new{box=Box(t,relative),text=t.Text,color=$"rgb({c.R}, {c.G}, {c.B})"};}
        object Article(WebWelcomeStep step){var p=step.TranslatePoint(default,group)??default;return new{x=p.X,y=p.Y,root=Box(step,step),number=Text(step.NumberText,step),title=Text(step.TitleText,step),body=Text(step.BodyText,step)};}
        void Capture(string name){using var bmp=new RenderTargetBitmap(new PixelSize(2480,840),new Vector(192,192));bmp.Render(canvas);bmp.Save(Path.Combine(output,name),new PngBitmapEncoderOptions());}
        w.Opened+=(_,_)=>Dispatcher.UIThread.Post(async()=>{
            var rows=new List<object>();foreach(var c in cases){host.Width=c.Width;group.SetPresentation(c.English);await Dispatcher.UIThread.InvokeAsync(()=>{},DispatcherPriority.Background);w.UpdateLayout();rows.Add(new{id=c.Id,root=Box(group,host),articles=group.Steps.Select(Article).ToArray()});if(c.Id is "ko-684" or "en-980" or "ko-824" or "en-824")Capture("welcomesteps-"+c.Id+".png");}
            var lifecycle=new List<object>();foreach(var english in new[]{false,true,false}){canvas.Children.Remove(host);await Dispatcher.UIThread.InvokeAsync(()=>{},DispatcherPriority.Background);group.SetPresentation(english);canvas.Children.Add(host);await Dispatcher.UIThread.InvokeAsync(()=>{},DispatcherPriority.Background);w.UpdateLayout();lifecycle.Add(new{english,attached=canvas.Children.Contains(host),root=Box(group,host),tracks=group.RowTracks,numbers=group.Steps.Select(x=>x.NumberText.Text).ToArray(),titles=group.Steps.Select(x=>x.TitleText.Text).ToArray(),children=group.Children.Count});}
            Capture("welcomesteps.png");File.WriteAllText(Path.Combine(output,"welcomesteps-native.json"),JsonSerializer.Serialize(new{dpr=w.RenderScaling,rows,lifecycle},new JsonSerializerOptions{WriteIndented=true}));w.Close();Console.WriteLine("Recorded "+rows.Count+" WelcomeSteps cases");
        },DispatcherPriority.Background);
        base.OnFrameworkInitializationCompleted();
    }
}
