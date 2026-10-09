using System.Text.Json;
using Avalonia;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using Verifier.Desktop;

internal sealed class SidebarProbeApplication:Application
{
    public override void OnFrameworkInitializationCompleted()
    {
        var lifetime=(IClassicDesktopStyleApplicationLifetime)ApplicationLifetime!;var args=lifetime.Args!;
        var cases=JsonSerializer.Deserialize<SidebarCase[]>(File.ReadAllText(args[1]),new JsonSerializerOptions{PropertyNameCaseInsensitive=true})!;
        var output=args[2];Directory.CreateDirectory(output);
        var sidebar=new WebSidebar{Height=660};var canvas=new Canvas{Width=1400,Height=1020,UseLayoutRounding=false,Background=Brush.Parse("#313338")};
        canvas.Children.Add(sidebar);Canvas.SetLeft(sidebar,30);Canvas.SetTop(sidebar,30);
        var w=new Window{Title="Replica Sidebar verification",Width=1430,Height=1120,Content=canvas,UseLayoutRounding=false};lifetime.MainWindow=w;
        object Box(Control c){var p=c.TranslatePoint(default,sidebar)??default;return new{x=p.X,y=p.Y,width=c.Bounds.Width,height=c.Bounds.Height};}
        void Apply(SidebarCase c){sidebar.Height=c.Height;sidebar.SetState(c.Lang,c.Screen,c.Visited);w.UpdateLayout();}
        object Snapshot(SidebarCase c)=>new{id=c.Id,width=sidebar.Bounds.Width,height=sidebar.Bounds.Height,clientWidth=sidebar.Viewport.ClientWidth,clientHeight=sidebar.Viewport.ClientHeight,
            scrollHeight=sidebar.Viewport.ScrollHeight,scrollWidth=sidebar.Viewport.ClientWidth,title=Box(sidebar.TitleBox),nav=Box(sidebar.NavigationBox),navName=AutomationProperties.GetName(sidebar.NavigationBox),
            note=Box(sidebar.NoteBox),strong=Box(sidebar.NoteHeading),noteText=sidebar.NoteHeading.Text+"\n"+sidebar.NoteText.Text,titleText=sidebar.TitleText.Text,
            items=sidebar.Items.Select((b,i)=>new{box=Box(b),enabled=b.IsEnabled,current=b.State==WebStepBadgeState.Active?"step":"",done=b.State==WebStepBadgeState.Done,name=AutomationProperties.GetName(b),key=WebSidebar.StageKeys[i]}).ToArray()};
        if(args[0]=="--sidebar-window")
        {
            string lang="ko",screen="select";string[] visits=WebSidebar.StageKeys;var requests=new List<string>();
            SidebarCase Current()=>new("actual",lang,660,220,screen,visits);
            void Record(){var f=Path.Combine(output,"sidebar-input.json");File.WriteAllText(f+".next",JsonSerializer.Serialize(new{capturedAtUtc=DateTime.UtcNow,dpr=w.RenderScaling,lang,screen,requests,offset=sidebar.Viewport.Offset,snapshot=Snapshot(Current())},new JsonSerializerOptions{WriteIndented=true}));File.Move(f+".next",f,true);}
            void Update(){requests.Clear();Apply(Current());Record();}
            sidebar.NavigationRequested+=(_,key)=>{requests.Add(key);Record();};
            AutomationProperties.SetAutomationId(sidebar,"SidebarCandidate");
            var controls=new[]{("Sidebar-select","All visits"),("Sidebar-running","Running"),("Sidebar-reopened","Reopened"),("Sidebar-locale","KO / EN"),("Sidebar-record","Record"),("Sidebar-close","Close")};
            foreach(var (entry,index) in controls.Select((v,i)=>(v,i)))
            {
                var b=new WebActionButton{Content=entry.Item2,Variant="quiet",Width=140,Focusable=entry.Item1!="Sidebar-record",IsTabStop=entry.Item1!="Sidebar-record"};
                AutomationProperties.SetAutomationId(b,entry.Item1);canvas.Children.Add(b);Canvas.SetLeft(b,280);Canvas.SetTop(b,30+index*58);
                b.Click+=(_,_)=>{switch(entry.Item1){case "Sidebar-close":w.Close();return;case "Sidebar-record":Record();return;case "Sidebar-locale":lang=lang=="ko"?"en":"ko";break;case "Sidebar-running":screen="running";visits=WebSidebar.StageKeys;break;case "Sidebar-reopened":screen="results";visits=new[]{"results"};break;default:screen="select";visits=WebSidebar.StageKeys;break;}Update();};
            }
            w.Opened+=(_,_)=>Update();base.OnFrameworkInitializationCompleted();return;
        }
        w.Opened+=(_,_)=>Dispatcher.UIThread.Post(()=>
        {
            var rows=new List<object>();foreach(var c in cases){Apply(c);rows.Add(Snapshot(c));
                if(c.Id is "18" or "45"){using var bitmap=new RenderTargetBitmap(new PixelSize(2800,2040),new Vector(192,192));bitmap.Render(canvas);bitmap.Save(Path.Combine(output,"sidebar-"+c.Id+".png"),new PngBitmapEncoderOptions());}}
            File.WriteAllText(Path.Combine(output,"sidebar-native.json"),JsonSerializer.Serialize(new{dpr=w.RenderScaling,rows},new JsonSerializerOptions{WriteIndented=true}));w.Close();Console.WriteLine("Recorded "+rows.Count+" attached Sidebar presentations.");
        },DispatcherPriority.Background);base.OnFrameworkInitializationCompleted();
    }
    internal sealed record SidebarCase(string Id,string Lang,double Height,double Width,string Screen,string[] Visited);
}
