using System.Text.Json;
using Avalonia;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using Verifier.Desktop;

internal sealed class CategorySectionProbeApplication:Application
{
    public override void OnFrameworkInitializationCompleted()
    {
        var lifetime=(IClassicDesktopStyleApplicationLifetime)ApplicationLifetime!;var args=lifetime.Args!;
        var cases=JsonSerializer.Deserialize<SectionCase[]>(File.ReadAllText(args[1]),new JsonSerializerOptions{PropertyNameCaseInsensitive=true})!;
        var output=args[2];Directory.CreateDirectory(output);var section=new WebCategorySection{Width=cases[0].Width,ReducedMotion=true};
        void Set(SectionCase c){section.Width=c.Width;var h=section.Header;h.Title=c.Title;h.Description=c.Description;h.IconName=c.Icon;h.StatusText=c.StatusText;h.StatusKind=c.StatusKind;h.SelectionLabel=c.SelectionLabel;section.SetDetails(c.Details);section.IsSelected=c.Selected;section.IsExpanded=c.Expanded;}
        Set(cases[0]);var canvas=new Canvas{Width=1200,Height=750,UseLayoutRounding=false,Background=Brush.Parse("#313338")};canvas.Children.Add(section);Canvas.SetLeft(section,30);Canvas.SetTop(section,30);
        var w=new Window{Title="Replica CategorySection verification",Width=1240,Height=850,Content=canvas,UseLayoutRounding=false};lifetime.MainWindow=w;
        object Box(Control c){var p=c.TranslatePoint(default,section)??default;return new{x=p.X,y=p.Y,width=c.Bounds.Width,height=c.Bounds.Height};}
        string Css(Color c)=>c.A==255?$"rgb({c.R}, {c.G}, {c.B})":$"rgba({c.R}, {c.G}, {c.B}, {Math.Round(c.A/255d,2).ToString(System.Globalization.CultureInfo.InvariantCulture)})";
        string Shadow(){var s=((WebSurfaceBox)section.Children[0]).BoxShadow[0];return $"{Css(s.Color)} {s.OffsetX}px {s.OffsetY}px {s.Blur}px {s.Spread}px"+(s.IsInset?" inset":"");}
        object Snapshot(SectionCase c)=>new{id=c.Id,width=section.Bounds.Width,height=section.Bounds.Height,header=Box(section.Header),wrapper=Box(section.Body),body=Box(section.Body),items=section.Body.Items.Select(i=>new{box=Box(i),text=i.Text.Text}).ToArray(),selected=section.IsSelected,expanded=section.IsExpanded,hidden=!section.IsExpanded,inert=!section.IsExpanded,border=Css(((SolidColorBrush)((WebSurfaceBox)section.Children[0]).BorderBrush!).Color),shadow=Shadow(),visibility=section.BodyVisible?"visible":"hidden"};
        if(args[0]=="--categorysection-window")
        {
            section.ReducedMotion=false;AutomationProperties.SetAutomationId(section.Header.ToggleButton,"CategorySection-expand");AutomationProperties.SetAutomationId(section.Header.Checkbox,"CategorySection-select");
            var events=new List<object>();
            void Record(){w.UpdateLayout();var f=Path.Combine(output,"categorysection-input.json");File.WriteAllText(f+".next",JsonSerializer.Serialize(new{capturedAtUtc=DateTime.UtcNow,dpr=w.RenderScaling,events,progress=section.ExpansionProgress,snapshot=Snapshot(cases[0])},new JsonSerializerOptions{WriteIndented=true}));File.Move(f+".next",f,true);}
            section.ExpansionRequested+=(_,value)=>{events.Add(new{action="expand",value});Record();};section.SelectionRequested+=(_,value)=>{events.Add(new{action="select",value});Record();};
            var close=new WebActionButton{Content="Close fixture",Variant="quiet"};AutomationProperties.SetAutomationId(close,"CategorySection-close");canvas.Children.Add(close);Canvas.SetLeft(close,700);Canvas.SetTop(close,500);close.Click+=(_,_)=>w.Close();
            var record=new WebActionButton{Content="Record",Variant="quiet",Focusable=false,IsTabStop=false};AutomationProperties.SetAutomationId(record,"CategorySection-record");canvas.Children.Add(record);Canvas.SetLeft(record,850);Canvas.SetTop(record,500);record.Click+=(_,_)=>Record();
            var reduced=new WebActionButton{Content="Reduced motion",Variant="quiet"};AutomationProperties.SetAutomationId(reduced,"CategorySection-reduced");canvas.Children.Add(reduced);Canvas.SetLeft(reduced,1000);Canvas.SetTop(reduced,500);reduced.Click+=(_,_)=>{section.ReducedMotion=true;Record();};
            w.Opened+=(_,_)=>Record();base.OnFrameworkInitializationCompleted();return;
        }
        w.Opened+=(_,_)=>Dispatcher.UIThread.Post(()=>
        {
            var rows=new List<object>();foreach(var c in cases){Set(c);w.UpdateLayout();rows.Add(Snapshot(c));}
            using var bitmap=new RenderTargetBitmap(new PixelSize(2400,1500),new Vector(192,192));bitmap.Render(canvas);bitmap.Save(Path.Combine(output,"categorysection.png"),new PngBitmapEncoderOptions());
            File.WriteAllText(Path.Combine(output,"categorysection-native.json"),JsonSerializer.Serialize(new{dpr=w.RenderScaling,rows},new JsonSerializerOptions{WriteIndented=true}));w.Close();Console.WriteLine("Recorded "+rows.Count+" attached CategorySection presentations.");
        },DispatcherPriority.Background);base.OnFrameworkInitializationCompleted();
    }
    internal sealed record SectionCase(string Id,string Key,string Lang,double Width,string Title,string Description,string Icon,string StatusText,string StatusKind,string SelectionLabel,bool Selected,bool Expanded,bool Ready,string[] Details);
}
