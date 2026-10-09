using System.Text.Json;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Automation;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using Verifier.Desktop;
internal sealed class WelcomeEntryHintProbeApplication:Application
{
    private sealed record Case(string Id,double Width,bool English);
    private sealed class Host:Panel
    {
        public WebWelcomeEntryHint Hint{get;}=new();
        public Host(){UseLayoutRounding=false;Children.Add(Hint);}
        protected override Size MeasureOverride(Size available){Hint.Measure(available);return Hint.DesiredSize;}
        protected override Size ArrangeOverride(Size final){Hint.Arrange(new Rect(final));return final;}
    }
    public override void OnFrameworkInitializationCompleted()
    {
        var life=(IClassicDesktopStyleApplicationLifetime)ApplicationLifetime!;var args=life.Args!;var cases=JsonSerializer.Deserialize<Case[]>(File.ReadAllText(args[1]),new JsonSerializerOptions{PropertyNameCaseInsensitive=true})!;var output=args[2];Directory.CreateDirectory(output);
        var host=new Host{Width=438};var hint=host.Hint;var canvas=new Canvas{Width=1240,Height=420,Background=Brush.Parse("#313338"),UseLayoutRounding=false};canvas.Children.Add(host);Canvas.SetLeft(host,30);Canvas.SetTop(host,60);var w=new Window{Title="Replica WelcomeEntryHint verification",Width=1280,Height=500,Content=canvas,UseLayoutRounding=false};life.MainWindow=w;if(args[0]=="--welcomeentryhint-window"){var toggle=new Button{Content="Toggle hint fixture",Width=145,Height=24};AutomationProperties.SetAutomationId(toggle,"WelcomeEntryHint-toggle");toggle.Click+=(_,_)=>hint.SetPresentation(!hint.English);canvas.Children.Add(toggle);Canvas.SetLeft(toggle,0);Canvas.SetTop(toggle,0);}
        object Box(Control c){var p=c.TranslatePoint(default,host)??default;return new{x=p.X,y=p.Y,width=c.Bounds.Width,height=c.Bounds.Height};}
        object Row(string id){var c=((ISolidColorBrush)hint.HintText.Foreground!).Color;var stroke=((ISolidColorBrush)hint.InfoIcon.Stroke!).Color;return new{id,root=Box(hint),icon=new{box=Box(hint.InfoIcon),stroke=$"rgb({stroke.R}, {stroke.G}, {stroke.B})",strokeWidth="1.7px"},text=new{box=Box(hint.HintText),text=hint.HintText.Text,color=$"rgb({c.R}, {c.G}, {c.B})"}};}
        void Capture(string name){using var bmp=new RenderTargetBitmap(new PixelSize(2480,840),new Vector(192,192));bmp.Render(canvas);bmp.Save(Path.Combine(output,name),new PngBitmapEncoderOptions());}
        w.Opened+=(_,_)=>Dispatcher.UIThread.Post(async()=>{
            var rows=new List<object>();foreach(var c in cases){host.Width=c.Width;hint.SetPresentation(c.English);await Dispatcher.UIThread.InvokeAsync(()=>{},DispatcherPriority.Background);w.UpdateLayout();rows.Add(Row(c.Id));if(c.Id is "ko-438" or "en-438")Capture("welcomeentryhint-"+c.Id+".png");}
            host.Width=600;var lifecycle=new List<object>();foreach(var english in new[]{false,true,false}){canvas.Children.Remove(host);await Dispatcher.UIThread.InvokeAsync(()=>{},DispatcherPriority.Background);hint.SetPresentation(english);canvas.Children.Add(host);await Dispatcher.UIThread.InvokeAsync(()=>{},DispatcherPriority.Background);w.UpdateLayout();lifecycle.Add(new{english,attached=canvas.Children.Contains(host),children=hint.Children.Count,row=Row(english?"en-600":"ko-600")});}
            Capture("welcomeentryhint.png");File.WriteAllText(Path.Combine(output,"welcomeentryhint-native.json"),JsonSerializer.Serialize(new{dpr=w.RenderScaling,rows,lifecycle},new JsonSerializerOptions{WriteIndented=true}));if(args[0]!="--welcomeentryhint-window")w.Close();Console.WriteLine("Recorded "+rows.Count+" WelcomeEntryHint cases");
        },DispatcherPriority.Background);base.OnFrameworkInitializationCompleted();
    }
}
