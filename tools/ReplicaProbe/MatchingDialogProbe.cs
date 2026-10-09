using System.Reflection;
using System.Text.Json;
using Avalonia;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using Verifier.Desktop;
using Avalonia.VisualTree;

internal sealed class MatchingDialogProbeApplication:Application
{
    internal sealed record Case(string Id,string Lang,double Width,double Height,string Query);
    public override void OnFrameworkInitializationCompleted()
    {
        var life=(IClassicDesktopStyleApplicationLifetime)ApplicationLifetime!;var args=life.Args!;var cases=JsonSerializer.Deserialize<Case[]>(File.ReadAllText(args[1]),new JsonSerializerOptions{PropertyNameCaseInsensitive=true})!;var output=args[2];Directory.CreateDirectory(output);
        var maps=new Dictionary<string,string>{{"demo.customers","demo.customers"},{"demo.orders","demo.orders"},{"demo.line_items","demo.line_items"}};string[] targets=[..maps.Keys,"archive.orders"];
        var c=cases[0];var n=new WebMatchingDialog{Width=c.Width,Height=c.Height,ReducedMotion=true};var canvas=new Canvas{Width=1280,Height=960,UseLayoutRounding=false,Background=Brush.Parse("#313338")};canvas.Children.Add(n);
        var w=new Window{Title="Replica MatchingDialog verification",Width=1280,Height=1000,UseLayoutRounding=false,Content=canvas};life.MainWindow=w;
        var flags=BindingFlags.Instance|BindingFlags.NonPublic;var surface=(Control)typeof(WebMatchingDialog).GetField("_surface",flags)!.GetValue(n)!;var flow=typeof(WebMatchingDialog).GetField("_flow",flags)!.GetValue(n)!;
        Control FlowPart(string field)=>(Control)flow.GetType().GetField(field,flags)!.GetValue(flow)!;
        object Box(Control e){var p=e.TranslatePoint(default,n)??default;return new{x=p.X,y=p.Y,width=e.Bounds.Width,height=e.Bounds.Height};}
        string Shadow(){var s=((WebSurfaceBox)surface).BoxShadow[0];return $"rgba({s.Color.R}, {s.Color.G}, {s.Color.B}, {Math.Round(s.Color.A/255d,2).ToString(System.Globalization.CultureInfo.InvariantCulture)}) {s.OffsetX}px {s.OffsetY}px {s.Blur}px {s.Spread}px";}
        object Read(Case item)=>new{id=item.Id,parts=new{dialog=Box(surface),head=Box(FlowPart("_head")),title=Box(n.TitleText),close=Box(n.CloseButton),body=Box(n.BodyViewport),source=Box(n.SourceBox),sourceLabel=Box(n.SourceLabel),targetLabel=Box(n.TargetLabel),search=Box(n.Search.Editor),list=Box(n.OptionsViewport),note=Box(n.QueryNote),policy=Box(n.PolicyText),footer=Box(FlowPart("_footer")),cancel=Box(n.CancelButton),apply=Box(n.ApplyButton)},cards=n.Options.Select(o=>new{name=o.TargetName,selected=o.IsSelected,disabled=o.Unavailable,box=Box(o),attached=o.IsAttachedToVisualTree(),parents=o.GetVisualAncestors().Select(v=>v.GetType().Name).ToArray()}).ToArray(),texts=new{title=n.TitleText.Text,sourceLabel=n.SourceLabel.Text,targetLabel=n.TargetLabel.Text,note=n.QueryNote.Text,policy=n.PolicyText.Text,cancel=n.CancelButton.Content,apply=n.ApplyButton.Content},bodyScrollHeight=n.BodyViewport.ScrollHeight,listScrollHeight=n.OptionsViewport.ScrollHeight,bodyScrollTop=n.BodyViewport.Offset,listScrollTop=n.OptionsViewport.Offset,draft=n.DraftTarget,shadow=Shadow(),empty=n.Options.Count==0?(n.OptionsViewport.Content as StackPanel)?.Children.OfType<Decorator>().FirstOrDefault()?.Child is WebSystemTextBlock t?t.Text:null:null};
        if(args[0]=="--matchingdialog-window")
        {
            n.ReducedMotion=false;var background=new Canvas{UseLayoutRounding=false,Background=Brush.Parse("#313338")};canvas.Children.Clear();canvas.Children.Add(background);background.Width=1280;background.Height=720;
            var opener=new WebActionButton{Content="Open matching",Variant="primary"};AutomationProperties.SetAutomationId(opener,"MatchingDialog-open");background.Children.Add(opener);Canvas.SetLeft(opener,30);Canvas.SetTop(opener,30);canvas.Children.Add(n);n.Width=1280;n.Height=720;
            var events=new List<object>();void Record(){w.UpdateLayout();var focused=w.FocusManager?.GetFocusedElement() as Control;var value=new{capturedAtUtc=DateTime.UtcNow,dpr=w.RenderScaling,visible=n.IsVisible,closing=n.IsClosing,reduced=n.ReducedMotion,backgroundEnabled=background.IsEnabled,backgroundBlur=(background.Effect as BlurEffect)?.Radius,focus=focused is null?null:AutomationProperties.GetAutomationId(focused),events,row=n.IsVisible?Read(c with{Width=1280,Height=720}):null};var f=Path.Combine(output,"matchingdialog-input.json");File.WriteAllText(f+".next",JsonSerializer.Serialize(value,new JsonSerializerOptions{WriteIndented=true}));File.Move(f+".next",f,true);}
            opener.Click+=(_,_)=>{n.Open("demo.customers",maps,targets,opener:opener,backgroundContent:background);Record();};n.DraftChanged+=(_,_)=>{events.Add(new{action="draft",value=n.DraftTarget});Record();};n.Applied+=(_,value)=>{maps["demo.customers"]=value;events.Add(new{action="apply",value});};n.Closed+=(_,_)=>{events.Add(new{action="close"});Record();};
            var record=new WebActionButton{Content="Record",Variant="quiet",Focusable=false,IsTabStop=false};AutomationProperties.SetAutomationId(record,"MatchingDialog-record");canvas.Children.Add(record);Canvas.SetLeft(record,30);Canvas.SetTop(record,760);record.Click+=(_,_)=>Record();
            var reduced=new WebActionButton{Content="Reduced motion",Variant="quiet"};AutomationProperties.SetAutomationId(reduced,"MatchingDialog-reduced");canvas.Children.Add(reduced);Canvas.SetLeft(reduced,180);Canvas.SetTop(reduced,760);reduced.Click+=(_,_)=>{n.ReducedMotion=true;Record();};
            var collision=new WebActionButton{Content="Example collision",Variant="quiet"};AutomationProperties.SetAutomationId(collision,"MatchingDialog-collision");canvas.Children.Add(collision);Canvas.SetLeft(collision,550);Canvas.SetTop(collision,760);collision.Click+=(_,_)=>{maps["demo.orders"]="archive.orders";Record();};
            var close=new WebActionButton{Content="Close fixture",Variant="quiet"};AutomationProperties.SetAutomationId(close,"MatchingDialog-fixture-close");canvas.Children.Add(close);Canvas.SetLeft(close,400);Canvas.SetTop(close,760);close.Click+=(_,_)=>w.Close();w.Opened+=(_,_)=>Record();base.OnFrameworkInitializationCompleted();return;
        }
        w.Opened+=(_,_)=>Dispatcher.UIThread.Post(async()=>
        {
            var rows=new List<object>();foreach(var item in cases){n.Width=item.Width;n.Height=item.Height;n.Open("demo.customers",maps,targets,item.Lang=="en");n.Search.Text=item.Query;await Dispatcher.UIThread.InvokeAsync(()=>{},DispatcherPriority.Background);n.InvalidateMeasure();n.Measure(new(item.Width,item.Height));n.Arrange(new(0,0,item.Width,item.Height));w.UpdateLayout();rows.Add(Read(item));}
            using var bmp=new RenderTargetBitmap(new PixelSize(2560,1920),new Vector(192,192));bmp.Render(canvas);bmp.Save(Path.Combine(output,"matchingdialog.png"),new PngBitmapEncoderOptions());File.WriteAllText(Path.Combine(output,"matchingdialog-native.json"),JsonSerializer.Serialize(new{dpr=w.RenderScaling,rows},new JsonSerializerOptions{WriteIndented=true}));w.Close();Console.WriteLine("Recorded "+rows.Count+" MatchingDialog states");
        },DispatcherPriority.Background);base.OnFrameworkInitializationCompleted();
    }
}
