using System.Text.Json;
using Avalonia;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using Verifier.Desktop;

internal sealed class SaveOptionsProbeApplication:Application
{
    private sealed record Case(string Id,string Lang,double Width,string[] Selected);
    public override void OnFrameworkInitializationCompleted()
    {
        var life=(IClassicDesktopStyleApplicationLifetime)ApplicationLifetime!;var args=life.Args!;
        var cases=JsonSerializer.Deserialize<Case[]>(File.ReadAllText(args[1]),new JsonSerializerOptions{PropertyNameCaseInsensitive=true})!;
        var output=args[2];Directory.CreateDirectory(output);var n=new WebSaveOptions{Width=656,ReducedMotion=true};
        var canvas=new Canvas{Width=1100,Height=650,UseLayoutRounding=false,Background=Brush.Parse("#313338")};canvas.Children.Add(n);Canvas.SetLeft(n,30);Canvas.SetTop(n,100);
        var w=new Window{Title="Replica SaveOptions verification",Width=1130,Height=750,UseLayoutRounding=false,Content=canvas};life.MainWindow=w;
        object Box(Control x){var p=x.TranslatePoint(default,n)??default;return new{x=p.X,y=p.Y,width=x.Bounds.Width,height=x.Bounds.Height};}
        object Read(Case c)=>new{id=c.Id,width=n.Bounds.Width,height=n.Bounds.Height,options=n.Options.Select(o=>new{format=o.Format,selected=o.IsSelected,card=Box(o),checkbox=Box(o.Checkbox),body=Box(o.TextGroup),title=Box(o.TitleText)}).ToArray()};
        if(args[0]=="--saveoptions-window")
        {
            n.ReducedMotion=false;var changes=0;var english=false;var text=new TextBlock{Width=1000,TextWrapping=TextWrapping.Wrap};Canvas.SetLeft(text,30);Canvas.SetTop(text,300);canvas.Children.Add(text);
            void State(){w.UpdateLayout();var focused=n.Options.FirstOrDefault(o=>o.Checkbox.IsFocused);var state=new{selected=n.SelectedFormats,changes,focused=focused?.Format,focusVisible=focused?.Checkbox.FocusVisible??false,english,downloadEnabled=n.SelectedFormats.Count>0};var json=JsonSerializer.Serialize(state,new JsonSerializerOptions{WriteIndented=true});text.Text=json;File.WriteAllText(Path.Combine(output,"saveoptions-input.json"),json);}
            n.SelectionChanged+=(_,change)=>{if(n.SelectedFormats.Contains(change.Format)!=change.Selected)throw new InvalidOperationException("Selection request disagrees with aggregate");File.WriteAllText(Path.Combine(output,"saveoptions-request.json"),JsonSerializer.Serialize(change));changes++;Dispatcher.UIThread.Post(State,DispatcherPriority.Background);};
            w.AddHandler(InputElement.GotFocusEvent,(_,_)=>Dispatcher.UIThread.Post(State),RoutingStrategies.Bubble,true);
            w.AddHandler(InputElement.KeyUpEvent,(_,_)=>Dispatcher.UIThread.Post(State),RoutingStrategies.Bubble,true);
            var b=new Button{Content="Language",Name="SaveOptions-language"};AutomationProperties.SetAutomationId(b,b.Name);b.Click+=(_,_)=>{english=!english;n.SetPresentation(n.SelectedFormats,english);Dispatcher.UIThread.Post(State,DispatcherPriority.Background);};Canvas.SetLeft(b,600);Canvas.SetTop(b,20);canvas.Children.Add(b);
            w.Opened+=(_,_)=>State();
        }
        else w.Opened+=(_,_)=>Dispatcher.UIThread.Post(async()=>
        {
            var rows=new List<object>();foreach(var c in cases){n.Width=c.Width;n.SetPresentation(c.Selected,c.Lang=="en");await Dispatcher.UIThread.InvokeAsync(()=>{},DispatcherPriority.Background);n.InvalidateMeasure();w.UpdateLayout();rows.Add(Read(c));}
            using var bmp=new RenderTargetBitmap(new PixelSize(2200,1300),new Vector(192,192));bmp.Render(canvas);bmp.Save(Path.Combine(output,"saveoptions.png"),new PngBitmapEncoderOptions());File.WriteAllText(Path.Combine(output,"saveoptions-native.json"),JsonSerializer.Serialize(new{dpr=w.RenderScaling,rows},new JsonSerializerOptions{WriteIndented=true}));w.Close();Console.WriteLine("Recorded "+rows.Count+" SaveOptions cases");
        },DispatcherPriority.Background);
        base.OnFrameworkInitializationCompleted();
    }
}
