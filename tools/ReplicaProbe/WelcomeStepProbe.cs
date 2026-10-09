using System.Text.Json;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using Verifier.Desktop;
internal sealed class WelcomeStepProbeApplication:Application
{
    private sealed record Case(string Id,double Width,bool English);
    private sealed class Fixture:Panel
    {
        public WebWelcomeStep[] Steps{get;}=[new(),new(),new()];private readonly double[] _widths=new double[3];private readonly double[] _left=new double[3];
        public Fixture(){UseLayoutRounding=false;foreach(var s in Steps)Children.Add(s);}
        public void Set(bool english){for(var i=0;i<3;i++){Steps[i].SetSharedTracks(null);Steps[i].SetPresentation(i,english);}InvalidateMeasure();}
        protected override Size MeasureOverride(Size available)
        {
            var units=(TopLevel.GetTopLevel(this)?.RenderScaling??2)*64;double Q(double v)=>Math.Floor(v*units)/units;var remaining=Math.Max(0,available.Width-64);
            for(var i=0;i<3;i++){_left[i]=Q(remaining*i/3)+32*i;_widths[i]=Q(remaining*(i+1)/3)-Q(remaining*i/3);Steps[i].Measure(new(_widths[i],double.PositiveInfinity));}
            var shared=new WebWelcomeRowTracks(Steps.Max(x=>x.NaturalTracks.Number),Steps.Max(x=>x.NaturalTracks.Title),Steps.Max(x=>x.NaturalTracks.Body));
            for(var i=0;i<3;i++){Steps[i].SetSharedTracks(shared);Steps[i].Measure(new(_widths[i],double.PositiveInfinity));}return new(available.Width,shared.Height);
        }
        protected override Size ArrangeOverride(Size final){for(var i=0;i<3;i++)Steps[i].Arrange(new(_left[i],0,_widths[i],final.Height));return final;}
    }
    public override void OnFrameworkInitializationCompleted()
    {
        var life=(IClassicDesktopStyleApplicationLifetime)ApplicationLifetime!;var args=life.Args!;var cases=JsonSerializer.Deserialize<Case[]>(File.ReadAllText(args[1]),new JsonSerializerOptions{PropertyNameCaseInsensitive=true})!;var output=args[2];Directory.CreateDirectory(output);
        var fixture=new Fixture{Width=684};var canvas=new Canvas{Width=1240,Height=420,Background=Brush.Parse("#313338"),UseLayoutRounding=false};canvas.Children.Add(fixture);Canvas.SetLeft(fixture,30);Canvas.SetTop(fixture,60);var w=new Window{Title="Replica WelcomeStep verification",Width=1280,Height=500,Content=canvas,UseLayoutRounding=false};life.MainWindow=w;
        object Box(Control c,Control relative){var p=c.TranslatePoint(default,relative)??default;return new{x=p.X,y=p.Y,width=c.Bounds.Width,height=c.Bounds.Height};}
        object Text(WebSystemTextBlock t,Control relative){var c=((ISolidColorBrush)t.Foreground!).Color;return new{box=Box(t,relative),text=t.Text,color=$"rgb({c.R}, {c.G}, {c.B})"};}
        void Capture(string name){using var bmp=new RenderTargetBitmap(new PixelSize(2480,840),new Vector(192,192));bmp.Render(canvas);bmp.Save(Path.Combine(output,name),new PngBitmapEncoderOptions());}
        w.Opened+=(_,_)=>Dispatcher.UIThread.Post(async()=>{
            var rows=new List<object>();foreach(var c in cases){fixture.Width=c.Width;fixture.Set(c.English);await Dispatcher.UIThread.InvokeAsync(()=>{},DispatcherPriority.Background);w.UpdateLayout();for(var i=0;i<3;i++){var n=fixture.Steps[i];rows.Add(new{id=c.Id+"-"+i,root=Box(n,n),number=Text(n.NumberText,n),title=Text(n.TitleText,n),body=Text(n.BodyText,n)});}if(c.Id is "ko-684" or "en-980")Capture("welcomestep-"+c.Id+".png");}
            var lifecycle=new List<object>();foreach(var english in new[]{false,true,false}){canvas.Children.Remove(fixture);await Dispatcher.UIThread.InvokeAsync(()=>{},DispatcherPriority.Background);fixture.Set(english);canvas.Children.Add(fixture);await Dispatcher.UIThread.InvokeAsync(()=>{},DispatcherPriority.Background);w.UpdateLayout();lifecycle.Add(new{english,attached=canvas.Children.Contains(fixture),height=fixture.Bounds.Height,tracks=fixture.Steps[0].SharedTracks,numbers=fixture.Steps.Select(x=>x.NumberText.Text).ToArray(),titles=fixture.Steps.Select(x=>x.TitleText.Text).ToArray()});}
            Capture("welcomestep.png");File.WriteAllText(Path.Combine(output,"welcomestep-native.json"),JsonSerializer.Serialize(new{dpr=w.RenderScaling,rows,lifecycle},new JsonSerializerOptions{WriteIndented=true}));w.Close();Console.WriteLine("Recorded "+rows.Count+" WelcomeStep cases");
        },DispatcherPriority.Background);
        base.OnFrameworkInitializationCompleted();
    }
}
