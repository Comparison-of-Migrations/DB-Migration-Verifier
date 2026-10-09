using System.Text.Json;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using Verifier.Desktop;
internal sealed class WelcomePaperContentProbeApplication:Application
{
    private sealed record Case(string Id,double Width,bool English);
    public override void OnFrameworkInitializationCompleted()
    {
        var life=(IClassicDesktopStyleApplicationLifetime)ApplicationLifetime!;var args=life.Args!;var cases=JsonSerializer.Deserialize<Case[]>(File.ReadAllText(args[1]),new JsonSerializerOptions{PropertyNameCaseInsensitive=true})!;var output=args[2];Directory.CreateDirectory(output);
        var content=new WebWelcomePaperContent{Width=438};var canvas=new Canvas{Width=1240,Height=420,Background=Brush.Parse("#313338"),UseLayoutRounding=false};canvas.Children.Add(content);Canvas.SetLeft(content,30);Canvas.SetTop(content,60);var w=new Window{Title="Replica WelcomePaperContent verification",Width=1280,Height=500,Content=canvas,UseLayoutRounding=false};life.MainWindow=w;
        object Box(Control c){var p=c.TranslatePoint(default,content)??default;return new{x=p.X,y=p.Y,width=c.Bounds.Width,height=c.Bounds.Height};}
        object Text(WebSystemTextBlock t){var c=((ISolidColorBrush)t.Foreground!).Color;return new{box=Box(t),text=t.Text,color=$"rgb({c.R}, {c.G}, {c.B})"};}
        void Capture(string name){using var bmp=new RenderTargetBitmap(new PixelSize(2480,840),new Vector(192,192));bmp.Render(canvas);bmp.Save(Path.Combine(output,name),new PngBitmapEncoderOptions());}
        w.Opened+=(_,_)=>Dispatcher.UIThread.Post(async()=>{
            var rows=new List<object>();foreach(var c in cases){content.Width=c.Width;content.SetPresentation(c.English);await Dispatcher.UIThread.InvokeAsync(()=>{},DispatcherPriority.Background);w.UpdateLayout();rows.Add(new{id=c.Id,root=Box(content),label=Text(content.LabelText),title=Text(content.TitleText),body=Text(content.BodyText)});if(c.Id is "ko-438" or "en-744")Capture("welcomepapercontent-"+c.Id+".png");}
            content.Width=600;var lifecycle=new List<object>();foreach(var english in new[]{false,true,false}){canvas.Children.Remove(content);await Dispatcher.UIThread.InvokeAsync(()=>{},DispatcherPriority.Background);content.SetPresentation(english);canvas.Children.Add(content);await Dispatcher.UIThread.InvokeAsync(()=>{},DispatcherPriority.Background);w.UpdateLayout();lifecycle.Add(new{english,attached=canvas.Children.Contains(content),root=Box(content),label=Text(content.LabelText),title=Text(content.TitleText),body=Text(content.BodyText),children=content.Children.Count});}
            Capture("welcomepapercontent.png");File.WriteAllText(Path.Combine(output,"welcomepapercontent-native.json"),JsonSerializer.Serialize(new{dpr=w.RenderScaling,rows,lifecycle},new JsonSerializerOptions{WriteIndented=true}));w.Close();Console.WriteLine("Recorded "+rows.Count+" WelcomePaperContent cases");
        },DispatcherPriority.Background);base.OnFrameworkInitializationCompleted();
    }
}
