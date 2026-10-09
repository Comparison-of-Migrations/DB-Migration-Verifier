using System.Text.Json;
using Avalonia;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using Verifier.Desktop;

internal sealed class DownloadOptionProbeApplication:Application
{
    private sealed record Case(string Id,string Lang,double Width,string Format,bool Selected);
    public override void OnFrameworkInitializationCompleted()
    {
        var life=(IClassicDesktopStyleApplicationLifetime)ApplicationLifetime!;var args=life.Args!;
        var cases=JsonSerializer.Deserialize<Case[]>(File.ReadAllText(args[1]),new JsonSerializerOptions{PropertyNameCaseInsensitive=true})!;
        var output=args[2];Directory.CreateDirectory(output);var n=new WebDownloadOption{Width=209.328125,ReducedMotion=true};
        var canvas=new Canvas{Width=1100,Height=600,UseLayoutRounding=false,Background=Brush.Parse("#313338")};canvas.Children.Add(n);Canvas.SetLeft(n,30);Canvas.SetTop(n,100);
        var w=new Window{Title="Replica DownloadOption verification",Width=1130,Height=700,UseLayoutRounding=false,Content=canvas};life.MainWindow=w;
        object Box(Control x){var p=x.TranslatePoint(default,n)??default;return new{x=p.X,y=p.Y,width=x.Bounds.Width,height=x.Bounds.Height};}
        string Rgb(IBrush? b){var c=((ISolidColorBrush)b!).Color;return $"rgb({c.R}, {c.G}, {c.B})";}
        object Read(Case c){var s=(WebSurfaceBox)n.Content!;var p=n.TextGroup.TranslatePoint(default,n)??default;var units=w.RenderScaling*64;return new{id=c.Id,width=n.Bounds.Width,height=n.Bounds.Height,checkbox=Box(n.Checkbox),body=Box(n.TextGroup),title=Box(n.TitleText),titleText=n.TitleText.Text,descriptionText=n.DescriptionText.Text,descriptionBaseline=p.Y+n.DescriptionBaseline,descriptionLines=n.DescriptionText.SystemLineWidths.Select(width=>new{x=p.X,width=Math.Ceiling(width*units)/units}),selected=n.IsSelected,bg=Rgb(s.Background),border=Rgb(s.BorderBrush),shadow=n.IsSelected?"rgb(139, 148, 255) 0px 0px 0px 1px inset":"none",titleColor=Rgb(n.TitleText.Foreground),descriptionColor=Rgb(n.DescriptionText.Foreground)};}
        void Set(Case c){n.Width=c.Width;n.SetPresentation(c.Format,c.Lang=="en");n.IsSelected=c.Selected;n.InvalidateMeasure();w.UpdateLayout();}
        if(args[0]=="--downloadoption-window")
        {
            n.ReducedMotion=false;var changes=0;var text=new TextBlock{Text="DownloadOption input diagnostics",Width=580,TextWrapping=TextWrapping.Wrap};Canvas.SetLeft(text,400);Canvas.SetTop(text,100);canvas.Children.Add(text);
            void State(){w.UpdateLayout();var state=new{selected=n.IsSelected,changes,focused=n.Checkbox.IsFocused,focusVisible=n.Checkbox.FocusVisible,format=n.Format,title=n.TitleText.Text,reduced=n.ReducedMotion};var json=JsonSerializer.Serialize(state,new JsonSerializerOptions{WriteIndented=true});text.Text=json;File.WriteAllText(Path.Combine(output,"downloadoption-input.json"),json);}
            n.SelectionRequested+=(_,_)=>{changes++;State();};n.Checkbox.GotFocus+=(_,_)=>Dispatcher.UIThread.Post(State);n.Checkbox.LostFocus+=(_,_)=>Dispatcher.UIThread.Post(State);
            n.Checkbox.PropertyChanged+=(_,e)=>{if(e.Property==WebCheckbox.FocusVisibleProperty)Dispatcher.UIThread.Post(State);};
            w.AddHandler(Avalonia.Input.InputElement.KeyUpEvent,(_,_)=>Dispatcher.UIThread.Post(State),Avalonia.Interactivity.RoutingStrategies.Bubble,true);
            var record=new Button{Content="Record",Name="DownloadOption-record"};AutomationProperties.SetAutomationId(record,record.Name);record.Click+=(_,_)=>State();Canvas.SetLeft(record,400);Canvas.SetTop(record,440);canvas.Children.Add(record);
            var en=false;var b=new Button{Content="Language",Name="DownloadOption-language"};AutomationProperties.SetAutomationId(b,b.Name);b.Click+=(_,_)=>{en=!en;n.SetPresentation(n.Format,en);State();};Canvas.SetLeft(b,400);Canvas.SetTop(b,260);canvas.Children.Add(b);
            var html=new Button{Content="HTML / JSON",Name="DownloadOption-format"};AutomationProperties.SetAutomationId(html,html.Name);html.Click+=(_,_)=>{n.SetPresentation(n.Format=="json"?"html":"json",en);State();};Canvas.SetLeft(html,400);Canvas.SetTop(html,320);canvas.Children.Add(html);
            var reduced=new Button{Content="Reduced motion",Name="DownloadOption-reduced"};AutomationProperties.SetAutomationId(reduced,reduced.Name);reduced.Click+=(_,_)=>{n.ReducedMotion=!n.ReducedMotion;State();};Canvas.SetLeft(reduced,400);Canvas.SetTop(reduced,380);canvas.Children.Add(reduced);
            w.Opened+=(_,_)=>State();
        }
        else w.Opened+=(_,_)=>Dispatcher.UIThread.Post(async()=>
        {
            var rows=new List<object>();foreach(var c in cases){Set(c);await Dispatcher.UIThread.InvokeAsync(()=>{},DispatcherPriority.Background);w.UpdateLayout();rows.Add(Read(c));}
            using var bmp=new RenderTargetBitmap(new PixelSize(2200,1200),new Vector(192,192));bmp.Render(canvas);bmp.Save(Path.Combine(output,"downloadoption.png"),new PngBitmapEncoderOptions());File.WriteAllText(Path.Combine(output,"downloadoption-native.json"),JsonSerializer.Serialize(new{dpr=w.RenderScaling,rows},new JsonSerializerOptions{WriteIndented=true}));w.Close();Console.WriteLine("Recorded "+rows.Count+" DownloadOption cases");
        },DispatcherPriority.Background);
        base.OnFrameworkInitializationCompleted();
    }
}
