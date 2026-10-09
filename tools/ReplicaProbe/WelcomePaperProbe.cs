using System.Text.Json;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Automation;
using Avalonia.Headless;
using Avalonia.Input;
using Avalonia.Input.Raw;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Verifier.Desktop;
internal sealed class WelcomePaperProbeApplication:Application
{
    private sealed record Case(string Id,double Width,double ViewportWidth,bool English);
    private sealed class Host:Panel
    {
        public WebWelcomePaper Paper{get;}=new();
        public Host(){UseLayoutRounding=false;Children.Add(Paper);}
        protected override Size MeasureOverride(Size a){Paper.Measure(new(a.Width,double.PositiveInfinity));return new(a.Width,Paper.DesiredSize.Height);}
        protected override Size ArrangeOverride(Size f){Paper.Arrange(new(0,0,f.Width,f.Height));return f;}
    }
    public override void OnFrameworkInitializationCompleted()
    {
        var life=(IClassicDesktopStyleApplicationLifetime)ApplicationLifetime!;var args=life.Args!;var cases=JsonSerializer.Deserialize<Case[]>(File.ReadAllText(args[1]),new JsonSerializerOptions{PropertyNameCaseInsensitive=true})!;var output=args[2];Directory.CreateDirectory(output);
        var host=new Host{Width=680};var paper=host.Paper;var canvas=new Canvas{Width=1500,Height=500,Background=Brush.Parse("#313338"),UseLayoutRounding=false};canvas.Children.Add(host);Canvas.SetLeft(host,30);Canvas.SetTop(host,60);var w=new Window{Title="Replica WelcomePaper verification",Width=1540,Height=580,Content=canvas,UseLayoutRounding=false};life.MainWindow=w;
        object Box(Control c,Control rel){var p=c.TranslatePoint(default,rel)??default;return new{x=p.X,y=p.Y,width=c.Bounds.Width,height=c.Bounds.Height};}
        string Color(IBrush brush){var c=((ISolidColorBrush)brush).Color;return $"rgb({c.R}, {c.G}, {c.B})";}
        object Text(WebSystemTextBlock t)=>new{box=Box(t,paper),text=t.Text,color=Color(t.Foreground!)};
        object Read(string id)=>new{id,root=Box(paper,host),icon=new{box=Box(paper.FileIcon,paper),stroke=Color(paper.FileIcon.Stroke!),strokeWidth=paper.FileIcon.StrokeWidth+"px"},content=new{box=Box(paper.ResearchContent,paper),label=Text(paper.ResearchContent.LabelText),title=Text(paper.ResearchContent.TitleText),body=Text(paper.ResearchContent.BodyText)},button=new{box=Box(paper.ResearchButton,paper),text=paper.ResearchButton.Content,color=Color(paper.ResearchButton.GetVisualDescendants().OfType<WebSystemTextBlock>().Single().Foreground!),enabled=paper.ResearchButton.IsEnabled}};
        void Capture(string name){using var bmp=new RenderTargetBitmap(new PixelSize(3000,1000),new Vector(192,192));bmp.Render(canvas);bmp.Save(Path.Combine(output,name),new PngBitmapEncoderOptions());}
        void Present(Case c){host.Width=c.Width;paper.SetPresentation(c.English,c.ViewportWidth,true);w.UpdateLayout();}
        if(args[0]=="--welcomepaper-passive-headless")
        {
            if(!ReplicaPlatform.IsHeadless)throw new InvalidOperationException("Finite raw passive check requires headless");
            w.Opened+=(_,_)=>Dispatcher.UIThread.Post(async()=>{Present(cases.Single(x=>x.Id=="en-1101"));var states=new List<object>();foreach(var target in new[]{"body","gap"}){paper.ResearchButton.Focus();var c=target=="body"?(Control)paper.ResearchContent.BodyText:paper;var point=c.TranslatePoint(target=="body"?new(c.Bounds.Width/2,c.Bounds.Height/2):new(300,12),w)!.Value;w.MouseMove(point);w.MouseDown(point,MouseButton.Left);w.MouseUp(point,MouseButton.Left);await Dispatcher.UIThread.InvokeAsync(()=>{},DispatcherPriority.Background);states.Add(new{target,focusedResearch=paper.ResearchButton.IsFocused,focusedType=w.FocusManager?.GetFocusedElement()?.GetType().Name??""});}File.WriteAllText(Path.Combine(output,"welcomepaper-passive.json"),JsonSerializer.Serialize(states));w.Close();},DispatcherPriority.Background);base.OnFrameworkInitializationCompleted();return;
        }
        if(args[0]=="--welcomepaper-window")
        {
            var count=0;AutomationProperties.SetAutomationId(paper,"WelcomePaper-root");AutomationProperties.SetAutomationId(paper.ResearchContent.BodyText,"WelcomePaper-body");AutomationProperties.SetAutomationId(paper.ResearchButton,"WelcomePaper-research");var change=new Button{Content="Toggle paper fixture",Width=145,Height=24};AutomationProperties.SetAutomationId(change,"WelcomePaper-language");canvas.Children.Add(change);Canvas.SetLeft(change,30);Canvas.SetTop(change,10);
            change.Click+=(_,_)=>{Present(cases.Single(x=>x.Id==(paper.English?"ko-960":"en-1101")));};paper.ResearchRequested+=(_,_)=>count++;
            w.Opened+=(_,_)=>{Present(cases.Single(x=>x.Id=="ko-960"));var timer=new DispatcherTimer{Interval=TimeSpan.FromMilliseconds(50)};timer.Tick+=(_,_)=>{w.UpdateLayout();File.WriteAllText(Path.Combine(output,"welcomepaper-input.json"),JsonSerializer.Serialize(new{requests=count,english=paper.English,viewportWidth=paper.ViewportWidth,focused=paper.ResearchButton.IsFocused,row=Read(paper.English?"en-1101":"ko-960")}));};timer.Start();w.Closed+=(_,_)=>timer.Stop();};base.OnFrameworkInitializationCompleted();return;
        }
        w.Opened+=(_,_)=>Dispatcher.UIThread.Post(async()=>{
            var rows=new List<object>();foreach(var c in cases){Present(c);await Dispatcher.UIThread.InvokeAsync(()=>{},DispatcherPriority.Background);w.UpdateLayout();rows.Add(Read(c.Id));if(c.Id is "ko-960" or "en-1101")Capture("welcomepaper-"+c.Id+".png");}
            var lifecycle=new List<object>();foreach(var id in new[]{"ko-960","en-1101","ko-960"}){canvas.Children.Remove(host);await Dispatcher.UIThread.InvokeAsync(()=>{},DispatcherPriority.Background);Present(cases.Single(x=>x.Id==id));canvas.Children.Add(host);await Dispatcher.UIThread.InvokeAsync(()=>{},DispatcherPriority.Background);w.UpdateLayout();lifecycle.Add(new{attached=canvas.Children.Contains(host),children=paper.Children.Count,row=Read(id)});}
            var requests=0;paper.ResearchRequested+=(_,_)=>requests++;var input=new List<object>();if(ReplicaPlatform.IsHeadless){var b=paper.ResearchButton;var p=b.TranslatePoint(new(b.Bounds.Width/2,b.Bounds.Height/2),w)!.Value;w.MouseMove(p);w.MouseDown(p,MouseButton.Left);w.MouseUp(p,MouseButton.Left);input.Add(new{state="pointer",requests,focused=b.IsFocused});foreach(var key in new[]{Key.Enter,Key.Space}){b.Focus();w.KeyPress(key,RawInputModifiers.None,PhysicalKey.None,"");w.KeyRelease(key,RawInputModifiers.None,PhysicalKey.None,"");input.Add(new{state=key.ToString(),requests,focused=b.IsFocused});}}
            Capture("welcomepaper.png");File.WriteAllText(Path.Combine(output,"welcomepaper-native.json"),JsonSerializer.Serialize(new{dpr=w.RenderScaling,rows,lifecycle,input},new JsonSerializerOptions{WriteIndented=true}));w.Close();Console.WriteLine("Recorded "+rows.Count+" WelcomePaper cases");
        },DispatcherPriority.Background);base.OnFrameworkInitializationCompleted();
    }
}
