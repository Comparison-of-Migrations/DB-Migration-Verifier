using System.Text.Json;
using Avalonia;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using Verifier.Desktop;

internal sealed class TopBarProbeApplication:Application
{
    public override void OnFrameworkInitializationCompleted()
    {
        var lifetime=(IClassicDesktopStyleApplicationLifetime)ApplicationLifetime!;var args=lifetime.Args!;
        var cases=JsonSerializer.Deserialize<TopCase[]>(File.ReadAllText(args[1]),new JsonSerializerOptions{PropertyNameCaseInsensitive=true})!;var output=args[2];Directory.CreateDirectory(output);
        var top=new WebTopBar{Width=960};var canvas=new Canvas{Width=1400,Height=300,UseLayoutRounding=false,Background=Brush.Parse("#313338")};canvas.Children.Add(top);Canvas.SetLeft(top,30);Canvas.SetTop(top,30);
        var w=new Window{Title="Replica TopBar verification",Width=1430,Height=440,Content=canvas,UseLayoutRounding=false};lifetime.MainWindow=w;
        object Box(Control c){var p=c.TranslatePoint(default,top)??default;return new{x=p.X,y=p.Y,width=c.Bounds.Width,height=c.Bounds.Height};}
        object Snapshot(TopCase c)=>new{id=c.Id,width=top.Bounds.Width,height=top.Bounds.Height,brand=Box(top.BrandBox),brandIcon=Box(top.BrandIconBox),brandText=Box(top.BrandText),preview=Box(top.PreviewBox),previewText=top.PreviewText.Text,language=Box(top.LanguageButton),languageText=top.LanguageButton.Content as string,languageName=AutomationProperties.GetName(top.LanguageButton)};
        if(args[0]=="--topbar-window")
        {
            string lang="ko";var requests=new List<string>();
            void Record(){var f=Path.Combine(output,"topbar-input.json");File.WriteAllText(f+".next",JsonSerializer.Serialize(new{capturedAtUtc=DateTime.UtcNow,dpr=w.RenderScaling,lang,requests,snapshot=Snapshot(new(lang+"-960",lang,960))},new JsonSerializerOptions{WriteIndented=true}));File.Move(f+".next",f,true);}
            top.LanguageRequested+=(_,next)=>{requests.Add(next);lang=next;top.SetLanguage(lang);AutomationProperties.SetAutomationId(top.LanguageButton,"TopBar-language");w.UpdateLayout();Record();};
            var close=new WebActionButton{Content="Close fixture",Variant="quiet"};AutomationProperties.SetAutomationId(close,"TopBar-close");canvas.Children.Add(close);Canvas.SetLeft(close,30);Canvas.SetTop(close,150);close.Click+=(_,_)=>w.Close();
            var record=new WebActionButton{Content="Record",Variant="quiet",Focusable=false,IsTabStop=false};AutomationProperties.SetAutomationId(record,"TopBar-record");canvas.Children.Add(record);Canvas.SetLeft(record,200);Canvas.SetTop(record,150);record.Click+=(_,_)=>Record();
            w.Opened+=(_,_)=>{top.SetLanguage(lang);AutomationProperties.SetAutomationId(top.LanguageButton,"TopBar-language");w.UpdateLayout();Record();};base.OnFrameworkInitializationCompleted();return;
        }
        w.Opened+=(_,_)=>Dispatcher.UIThread.Post(()=>
        {
            var rows=new List<object>();foreach(var c in cases){top.Width=c.Width;top.SetLanguage(c.Lang);w.UpdateLayout();rows.Add(Snapshot(c));}
            using var bitmap=new RenderTargetBitmap(new PixelSize(2800,600),new Vector(192,192));bitmap.Render(canvas);bitmap.Save(Path.Combine(output,"topbar.png"),new PngBitmapEncoderOptions());
            File.WriteAllText(Path.Combine(output,"topbar-native.json"),JsonSerializer.Serialize(new{dpr=w.RenderScaling,rows},new JsonSerializerOptions{WriteIndented=true}));w.Close();Console.WriteLine("Recorded "+rows.Count+" attached TopBar presentations.");
        },DispatcherPriority.Background);base.OnFrameworkInitializationCompleted();
    }
    internal sealed record TopCase(string Id,string Lang,double Width);
}
