using System.Text.Json;
using Avalonia;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using Verifier.Desktop;

internal sealed class ConnectionPanelProbeApplication:Application
{
    public override void OnFrameworkInitializationCompleted()
    {
        var lifetime=(IClassicDesktopStyleApplicationLifetime)ApplicationLifetime!;var args=lifetime.Args!;
        var cases=JsonSerializer.Deserialize<PanelCase[]>(File.ReadAllText(args[1]),new JsonSerializerOptions{PropertyNameCaseInsensitive=true})!;
        var output=args[2];Directory.CreateDirectory(output);var panel=new WebConnectionPanel{Width=328};
        panel.HostInput.Text="source.example.invalid";panel.PortInput.Value="1433";panel.DatabaseInput.Text="migration_sample";panel.UserInput.Text="demo_reader";
        var canvas=new Canvas{Width=1100,Height=680,UseLayoutRounding=false,Background=Brush.Parse("#313338")};canvas.Children.Add(panel);Canvas.SetLeft(panel,30);Canvas.SetTop(panel,30);
        var w=new Window{Title="Replica ConnectionPanel verification",Width=1130,Height=780,Content=canvas,UseLayoutRounding=false};lifetime.MainWindow=w;
        object Box(Control c){var p=c.TranslatePoint(default,panel)??default;return new{x=p.X,y=p.Y,width=c.Bounds.Width,height=c.Bounds.Height};}
        string Shadow(){var s=((WebSurfaceBox)panel.Children[0]).BoxShadow[0];return $"rgba({s.Color.R}, {s.Color.G}, {s.Color.B}, {Math.Round(s.Color.A/255d,3).ToString(System.Globalization.CultureInfo.InvariantCulture)}) {s.OffsetX}px {s.OffsetY}px {s.Blur}px {s.Spread}px";}
        object Snapshot(PanelCase c)=>new{id=c.Id,width=panel.Bounds.Width,height=panel.Bounds.Height,shadow=Shadow(),header=Box(panel.Header),mark=Box(panel.RoleMark),icon=Box(panel.RoleIcon),titles=Box(panel.TitleGroup),title=Box(panel.Title),subtitle=Box(panel.Subtitle),status=Box(panel.Status),statusText=panel.Status.Text,serverRow=Box(panel.ServerRow),fields=panel.Fields.Select(f=>new{box=Box(f),label=Box(f.LabelHitArea),input=Box(f.Input!),hint=f.Hint is null?null:Box(f.HintText),labelText=f.Label,value=f.Input is WebNumberInput n?n.Value:((WebTextInput)f.Input!).Text??"",disabled=!f.Input!.IsEnabled}).ToArray()};
        if(args[0]=="--connectionpanel-window")
        {
            var events=new List<object>();
            void Record(){var f=Path.Combine(output,"connectionpanel-input.json");File.WriteAllText(f+".next",JsonSerializer.Serialize(new{capturedAtUtc=DateTime.UtcNow,dpr=w.RenderScaling,events,snapshot=Snapshot(cases[0])},new JsonSerializerOptions{WriteIndented=true}));File.Move(f+".next",f,true);}
            panel.InputChanged+=(_,key)=>{events.Add(new{key,value=key switch{"host"=>panel.HostInput.Text,"port"=>panel.PortInput.Value,"db"=>panel.DatabaseInput.Text,_=>panel.UserInput.Text}});w.UpdateLayout();Record();};
            string[] keys=["host","port","db","user","password"];for(var i=0;i<keys.Length;i++)AutomationProperties.SetAutomationId(panel.Fields[i].Input is WebNumberInput n?n.Editor:panel.Fields[i].Input!,"ConnectionPanel-"+keys[i]);
            var close=new WebActionButton{Content="Close fixture",Variant="quiet"};AutomationProperties.SetAutomationId(close,"ConnectionPanel-close");canvas.Children.Add(close);Canvas.SetLeft(close,600);Canvas.SetTop(close,30);close.Click+=(_,_)=>w.Close();
            var record=new WebActionButton{Content="Record",Variant="quiet",Focusable=false,IsTabStop=false};AutomationProperties.SetAutomationId(record,"ConnectionPanel-record");canvas.Children.Add(record);Canvas.SetLeft(record,600);Canvas.SetTop(record,100);record.Click+=(_,_)=>Record();
            w.Opened+=(_,_)=>{w.UpdateLayout();Record();};base.OnFrameworkInitializationCompleted();return;
        }
        w.Opened+=(_,_)=>Dispatcher.UIThread.Post(()=>
        {
            var rows=new List<object>();foreach(var c in cases){panel.Width=c.Width;panel.SetPresentation(c.Lang,c.Side,c.Connected);panel.HostInput.Text=c.Side+".example.invalid";w.UpdateLayout();rows.Add(Snapshot(c));}
            using var bitmap=new RenderTargetBitmap(new PixelSize(2200,1360),new Vector(192,192));bitmap.Render(canvas);bitmap.Save(Path.Combine(output,"connectionpanel.png"),new PngBitmapEncoderOptions());
            File.WriteAllText(Path.Combine(output,"connectionpanel-native.json"),JsonSerializer.Serialize(new{dpr=w.RenderScaling,rows},new JsonSerializerOptions{WriteIndented=true}));w.Close();Console.WriteLine("Recorded "+rows.Count+" attached ConnectionPanel presentations.");
        },DispatcherPriority.Background);base.OnFrameworkInitializationCompleted();
    }
    internal sealed record PanelCase(string Id,string Lang,string Side,double Width,bool Connected);
}
