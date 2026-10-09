using System.Text.Json;
using Avalonia;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Headless;
using Avalonia.Input;
using Avalonia.Input.Raw;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Verifier.Desktop;
internal sealed class HomePageProbeApplication:Application
{
    private sealed record Case(string Id,double Width,double ViewportWidth,bool English);
    public override void OnFrameworkInitializationCompleted()
    {
        var life=(IClassicDesktopStyleApplicationLifetime)ApplicationLifetime!;var args=life.Args!;var cases=JsonSerializer.Deserialize<Case[]>(File.ReadAllText(args[1]),new JsonSerializerOptions{PropertyNameCaseInsensitive=true})!;var output=args[2];Directory.CreateDirectory(output);
        var page=new WebHomePage{Width=980,ReducedMotion=true};var canvas=new Canvas{Width=1200,Height=1100,Background=Brush.Parse("#313338"),UseLayoutRounding=false};canvas.Children.Add(page);Canvas.SetLeft(page,30);Canvas.SetTop(page,60);var w=new Window{Title="Replica HomePage verification",Width=1240,Height=1180,Content=canvas,UseLayoutRounding=false};life.MainWindow=w;
        object Box(Control c){var p=c.TranslatePoint(default,page)??default;return new{x=p.X,y=p.Y,width=c.Bounds.Width,height=c.Bounds.Height};}
        string Color(IBrush b){var c=((ISolidColorBrush)b).Color;return c.A==0?"rgba(0, 0, 0, 0)":$"rgb({c.R}, {c.G}, {c.B})";}
        object Text(WebSystemTextBlock c)=>new{box=Box(c),text=c.Text,color=Color(c.Foreground!)};
        object Button(WebActionButton b){var s=b.GetVisualDescendants().OfType<WebSurfaceBox>().Single();return new{box=Box(b),text=b.Content,enabled=b.IsEnabled,background=Color(s.Background!),borderColor=Color(s.BorderBrush!),color=Color(b.GetVisualDescendants().OfType<WebSystemTextBlock>().Single().Foreground!)};}
        object Read(string id)=>new{id,root=Box(page),heading=Box(page.Heading),title=Text(page.Heading.TitleText),description=Text(page.Heading.DescriptionText),hero=Box(page.Hero),helpBox=Box(page.HelpBox),help=Text(page.HelpText),report=new{visible=page.Report.IsVisible,box=Box(page.Report)},entry=Box(page.Entry),actions=Box(page.Actions),mainActions=Box(page.MainActions),buttons=new[]{Button(page.StartButton),Button(page.OpenButton),Button(page.SampleButton)},hint=new{root=Box(page.Hint),icon=Box(page.Hint.InfoIcon),text=Text(page.Hint.HintText)},steps=new{root=Box(page.Steps),articles=page.Steps.Steps.Select(s=>new{root=Box(s),number=Text(s.NumberText),title=Text(s.TitleText),body=Text(s.BodyText)}).ToArray()},paper=new{root=Box(page.Paper),icon=Box(page.Paper.FileIcon),content=Box(page.Paper.ResearchContent),label=Text(page.Paper.ResearchContent.LabelText),title=Text(page.Paper.ResearchContent.TitleText),body=Text(page.Paper.ResearchContent.BodyText),button=Button(page.Paper.ResearchButton)}};
        string currentId="ko-1280";void Present(Case c){currentId=c.Id;page.Width=c.Width;page.SetPresentation(new(c.English),c.ViewportWidth);w.UpdateLayout();}
        void Capture(string name){using var bmp=new RenderTargetBitmap(new PixelSize(2400,2200),new Vector(192,192));bmp.Render(canvas);bmp.Save(Path.Combine(output,name),new PngBitmapEncoderOptions());}
        var actions=new List<string>();page.ActionRequested+=(_,action)=>actions.Add(action);
        if(args.Length>3 && args[3] is "input-scope" or "neutral-scope")
        {
            w.Opened+=(_,_)=>Dispatcher.UIThread.Post(async()=>{
                var states=new List<object>();foreach(var id in new[]{"ko-960","en-960"}){
                    Present(cases.Single(x=>x.Id==id));if(args[3]=="input-scope")foreach(var b in new[]{page.StartButton,page.OpenButton,page.SampleButton,page.Paper.ResearchButton}){var identity=AutomationProperties.GetAutomationId(b);var point=b.TranslatePoint(new(b.Bounds.Width/2,b.Bounds.Height/2),w)!.Value;w.MouseMove(point);w.MouseDown(point,MouseButton.Left);w.MouseUp(point,MouseButton.Left);states.Add(new{id,method="pointer",target=identity,count=actions.Count,action=actions.Last()});foreach(var key in new[]{Key.Enter,Key.Space}){b.Focus(NavigationMethod.Tab);w.KeyPress(key,RawInputModifiers.None,PhysicalKey.None,"");w.KeyRelease(key,RawInputModifiers.None,PhysicalKey.None,"");states.Add(new{id,method=key.ToString(),target=identity,count=actions.Count,action=actions.Last()});}}
                    foreach(var target in new[]{"help","hint","gap"}){page.StartButton.Focus(NavigationMethod.Tab);var control=target=="help"?(Control)page.HelpText:target=="hint"?page.Hint.HintText:page.Entry;var local=target=="gap"?new Point(control.Bounds.Width/2,control.Bounds.Height-12):new Point(control.Bounds.Width/2,control.Bounds.Height/2);var point=control.TranslatePoint(local,w)!.Value;var before=actions.Count;w.MouseMove(point);w.MouseDown(point,MouseButton.Left);w.MouseUp(point,MouseButton.Left);await Dispatcher.UIThread.InvokeAsync(()=>{},DispatcherPriority.Background);states.Add(new{id,method="passive",target,count=actions.Count,unchanged=actions.Count==before,focused=w.FocusManager?.GetFocusedElement() is Control f?AutomationProperties.GetAutomationId(f):""});}
                    var neutralBefore=actions.Count;foreach(var key in new[]{Key.Enter,Key.Space}){w.KeyPress(key,RawInputModifiers.None,PhysicalKey.None,"");w.KeyRelease(key,RawInputModifiers.None,PhysicalKey.None,"");states.Add(new{id,method="neutral-key",target=key.ToString(),count=actions.Count,unchanged=actions.Count==neutralBefore,focused=w.FocusManager?.GetFocusedElement() is Control neutralFocus?AutomationProperties.GetAutomationId(neutralFocus):""});}
                    canvas.Children.Remove(page);await Dispatcher.UIThread.InvokeAsync(()=>{},DispatcherPriority.Background);states.Add(new{id,method="neutral-detached",target="BODY",count=actions.Count,unchanged=actions.Count==neutralBefore,focused=w.FocusManager?.GetFocusedElement() is Control detachedFocus?AutomationProperties.GetAutomationId(detachedFocus):""});canvas.Children.Add(page);Present(cases.Single(x=>x.Id==id));page.Focus(NavigationMethod.Pointer);states.Add(new{id,method="neutral-reattach",target="BODY",count=actions.Count,unchanged=actions.Count==neutralBefore,focused=w.FocusManager?.GetFocusedElement() is Control reattachedFocus?AutomationProperties.GetAutomationId(reattachedFocus):""});page.SetPresentation(new(page.View.English),cases.Single(x=>x.Id==id).ViewportWidth,"start",true);await Dispatcher.UIThread.InvokeAsync(()=>{},DispatcherPriority.Loaded);states.Add(new{id,method="neutral-restore",target="start",count=actions.Count,unchanged=actions.Count==neutralBefore,focused=w.FocusManager?.GetFocusedElement() is Control restoredFocus?AutomationProperties.GetAutomationId(restoredFocus):""});page.Focus(NavigationMethod.Pointer);Capture("homepage-scope-"+id+".png");
                }File.WriteAllText(Path.Combine(output,"homepage-input-scope.json"),JsonSerializer.Serialize(states,new JsonSerializerOptions{WriteIndented=true}));w.Close();
            },DispatcherPriority.Background);base.OnFrameworkInitializationCompleted();return;
        }
        if(args[0]=="--homepage-window")
        {
            var toggle=new Button{Content="Toggle Home fixture",Width=145,Height=24};AutomationProperties.SetAutomationId(toggle,"HomePage-toggle");canvas.Children.Add(toggle);toggle.Click+=(_,_)=>Present(cases.Single(x=>x.Id==(page.View.English?"ko-960":"en-960")));
            var pointerEvents=new List<object>();
            void RecordPointer(string phase,PointerEventArgs e){var wp=e.GetPosition(w);var pp=e.GetPosition(page);pointerEvents.Add(new{phase,source=e.Source?.GetType().FullName,window=new{x=wp.X,y=wp.Y},page=new{x=pp.X,y=pp.Y},left=e.GetCurrentPoint(w).Properties.IsLeftButtonPressed,handled=e.Handled,scaling=w.RenderScaling,client=w.ClientSize,screenOrigin=w.PointToScreen(default),pageOrigin=page.TranslatePoint(default,w),focused=w.FocusManager?.GetFocusedElement() is Control c?AutomationProperties.GetAutomationId(c):""});}
            w.AddHandler(InputElement.PointerPressedEvent,(_,e)=>RecordPointer("down",e),RoutingStrategies.Tunnel,true);w.AddHandler(InputElement.PointerReleasedEvent,(_,e)=>RecordPointer("up",e),RoutingStrategies.Tunnel,true);
            var timer=new DispatcherTimer{Interval=TimeSpan.FromMilliseconds(60)};timer.Tick+=(_,_)=>{w.UpdateLayout();File.WriteAllText(Path.Combine(output,"homepage-input.json"),JsonSerializer.Serialize(new{actions,pointerEvents,english=page.View.English,focused=w.FocusManager?.GetFocusedElement() is Control f?AutomationProperties.GetAutomationId(f):"",row=Read(currentId)}));};w.Closed+=(_,_)=>timer.Stop();w.Opened+=(_,_)=>{Present(cases.Single(x=>x.Id=="ko-960"));timer.Start();};base.OnFrameworkInitializationCompleted();return;
        }
        w.Opened+=(_,_)=>Dispatcher.UIThread.Post(async()=>{
            var rows=new List<object>();foreach(var c in cases){Present(c);await Dispatcher.UIThread.InvokeAsync(()=>{},DispatcherPriority.Background);w.UpdateLayout();rows.Add(Read(c.Id));if(c.Id is "ko-1280" or "en-960")Capture("homepage-"+c.Id+".png");}
            var lifecycle=new List<object>();foreach(var id in new[]{"ko-1280","en-960","ko-1280"}){canvas.Children.Remove(page);await Dispatcher.UIThread.InvokeAsync(()=>{},DispatcherPriority.Background);Present(cases.Single(x=>x.Id==id));canvas.Children.Add(page);await Dispatcher.UIThread.InvokeAsync(()=>{},DispatcherPriority.Background);w.UpdateLayout();lifecycle.Add(new{attached=canvas.Children.Contains(page),children=page.Children.Count,row=Read(id)});}
            var inputs=new List<object>();if(ReplicaPlatform.IsHeadless){foreach(var b in new[]{page.StartButton,page.OpenButton,page.SampleButton,page.Paper.ResearchButton}){var p=b.TranslatePoint(new(b.Bounds.Width/2,b.Bounds.Height/2),w)!.Value;w.MouseMove(p);w.MouseDown(p,MouseButton.Left);w.MouseUp(p,MouseButton.Left);inputs.Add(new{method="pointer",action=actions.Last(),count=actions.Count});foreach(var key in new[]{Key.Enter,Key.Space}){b.Focus(NavigationMethod.Tab);w.KeyPress(key,RawInputModifiers.None,PhysicalKey.None,"");w.KeyRelease(key,RawInputModifiers.None,PhysicalKey.None,"");inputs.Add(new{method=key.ToString(),action=actions.Last(),count=actions.Count});}}}
            Capture("homepage.png");File.WriteAllText(Path.Combine(output,"homepage-native.json"),JsonSerializer.Serialize(new{dpr=w.RenderScaling,rows,lifecycle,inputs},new JsonSerializerOptions{WriteIndented=true}));if(args[0]!="--homepage-window")w.Close();Console.WriteLine("Recorded "+rows.Count+" HomePage cases");
        },DispatcherPriority.Background);base.OnFrameworkInitializationCompleted();
    }
}
