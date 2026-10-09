using System.Text.Json;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Automation;
using System.Diagnostics;
using Verifier.Desktop;

internal sealed class ScrollViewportProbeApplication : Application
{
    public override void OnFrameworkInitializationCompleted()
    {
        var lifetime = (IClassicDesktopStyleApplicationLifetime)ApplicationLifetime!;
        var output = lifetime.Args![1];
        Directory.CreateDirectory(output);
        var child = new ScrollStripes { Height = 1140, UseLayoutRounding = false };
        var pane = new WebScrollViewport { Width = 220, Height = 400, Padding = new Thickness(14,30),
            Content = child, Background = Brush.Parse("#232428") };
        var canvas = new Canvas { Width = 700, Height = 1100, UseLayoutRounding = false };
        canvas.Children.Add(pane); Canvas.SetLeft(pane,30); Canvas.SetTop(pane,60);
        var window = new Window { Title = "Replica ScrollViewport verification", Width = 800, Height = 1120,
            UseLayoutRounding = false, Content = canvas };
        lifetime.MainWindow = window;
        if(lifetime.Args[0]=="--scrollviewport-window")
        {
            pane.Focusable=true;
            AutomationProperties.SetAutomationId(pane,"ScrollCandidate");
            AutomationProperties.SetName(pane,"Candidate scroll fixture");
            var events=new List<object>();var frames=new List<object>();var changes=new List<object>();
            var start=Stopwatch.GetTimestamp();double activeUntil=0,lastRecord=0;
            double Now()=>Stopwatch.GetElapsedTime(start).TotalMilliseconds;
            void Record()
            {
                var file=Path.Combine(output,"scrollviewport-input.json");
                File.WriteAllText(file+".next",JsonSerializer.Serialize(new{
                capturedAtUtc=DateTime.UtcNow,dpr=window.RenderScaling,offset=pane.Offset,maximum=pane.MaximumOffset,clientWidth=pane.ClientWidth,
                thumb=new {x=pane.ThumbBounds.X,y=pane.ThumbBounds.Y,width=pane.ThumbBounds.Width,height=pane.ThumbBounds.Height},
                activeControl=window.FocusManager?.GetFocusedElement() is Control active?AutomationProperties.GetAutomationId(active):null,focused=pane.IsFocused,focusVisible=pane.IsFocusVisible,tabStop=pane.IsTabStop,events,frames,changes},new JsonSerializerOptions{WriteIndented=true}));
                File.Move(file+".next",file,true);
            }
            pane.AddHandler(InputElement.PointerWheelChangedEvent,(_,e)=>{events.Add(new{kind="wheel",time=Now(),x=e.Delta.X,y=e.Delta.Y});activeUntil=Now()+900;},RoutingStrategies.Tunnel,handledEventsToo:true);
            pane.AddHandler(InputElement.KeyDownEvent,(_,e)=>{events.Add(new{kind="key",time=Now(),key=e.Key.ToString(),focused=pane.IsFocused,source=e.Source?.GetType().Name});activeUntil=Now()+900;},RoutingStrategies.Tunnel,handledEventsToo:true);
            pane.AddHandler(InputElement.PointerPressedEvent,(_,e)=>{var p=e.GetPosition(pane);events.Add(new{kind="pressed",time=Now(),x=p.X,y=p.Y});activeUntil=Now()+1500;},RoutingStrategies.Tunnel,handledEventsToo:true);
            pane.AddHandler(InputElement.PointerReleasedEvent,(_,e)=>{events.Add(new{kind="released",time=Now()});activeUntil=Now()+900;},RoutingStrategies.Tunnel,handledEventsToo:true);
            pane.OffsetChanged+=(_,_)=>changes.Add(new{time=Now(),offset=pane.Offset});
            pane.GotFocus+=(_,e)=>events.Add(new{kind="focus",time=Now(),focused=pane.IsFocused,source=e.Source?.GetType().Name});
            pane.LostFocus+=(_,e)=>events.Add(new{kind="blur",time=Now(),focused=pane.IsFocused,source=e.Source?.GetType().Name});
            var timer=new DispatcherTimer{Interval=TimeSpan.FromMilliseconds(16)};
            timer.Tick+=(_,_)=>{var now=Now();if(now<activeUntil)frames.Add(new{time=now,offset=pane.Offset});if(now-lastRecord>=200){Record();lastRecord=now;}};
            foreach(var entry in new[]{("ScrollReset","Reset to top",0d),("ScrollBottom","Go to bottom",1e9),("ScrollShort","No overflow",-1d),("ScrollLong","Restore overflow",-2d),("ScrollClose","Close fixture",-3d),("ScrollTall","Tall page test",-4d),("ScrollRecord","Record state",-5d),("ScrollFocusChild","Focus child test",-6d),("ScrollDetach","Detach viewport",-7d)})
            {
                var button=new Button{Content=entry.Item2,Width=180,Height=44,Focusable=entry.Item1!="ScrollRecord",IsTabStop=entry.Item1!="ScrollRecord"};AutomationProperties.SetAutomationId(button,entry.Item1);
                Canvas.SetLeft(button,300);Canvas.SetTop(button,60+canvas.Children.Count*56);canvas.Children.Add(button);
                button.Click+=(_,_)=>{if(entry.Item3==-7){canvas.Children.Remove(pane);Record();DispatcherTimer.RunOnce(()=>{canvas.Children.Insert(0,pane);window.UpdateLayout();Record();},TimeSpan.FromMilliseconds(200));return;}if(entry.Item3==-6){pane.Height=400;var body=new Canvas{Height=1140};var target=new Button{Width=100,Height=30,Content="Focus child"};AutomationProperties.SetAutomationId(target,"ScrollChild");body.Children.Add(target);Canvas.SetLeft(target,0);Canvas.SetTop(target,60);pane.Content=body;pane.Offset=0;window.UpdateLayout();Record();return;}if(entry.Item3==-5){Record();return;}if(entry.Item3==-3){window.Close();return;}if(entry.Item3==-4){pane.Height=660;child.Height=3000;pane.Offset=0;events.Clear();frames.Clear();changes.Clear();}else if(entry.Item3<0)child.Height=entry.Item3==-1?200:1140;else{pane.Content=child;child.Height=1140;window.UpdateLayout();pane.Offset=entry.Item3;events.Clear();frames.Clear();changes.Clear();}window.UpdateLayout();Record();};
            }
            window.Opened+=(_,_)=>{timer.Start();Record();};window.Closed+=(_,_)=>timer.Stop();
            base.OnFrameworkInitializationCompleted();return;
        }
        window.Opened += (_,_) => Dispatcher.UIThread.Post(() =>
        {
            if(lifetime.Args[0]=="--scrollviewport-minimum")
            {
                var minimums = new List<object>();
                foreach (var requestedHeight in new[] {60,36,20,0})
                {
                    pane.Height=requestedHeight; window.UpdateLayout(); pane.Offset=0; window.UpdateLayout();
                    var point=child.TranslatePoint(default,pane)!.Value;
                    minimums.Add(new { requestedHeight, width=pane.Bounds.Width,height=pane.Bounds.Height,
                        clientWidth=pane.ClientWidth,clientHeight=pane.ClientHeight,scrollWidth=pane.ClientWidth,
                        scrollHeight=pane.ScrollHeight,offset=pane.Offset,
                        content=new{x=point.X,y=point.Y,width=child.Bounds.Width,height=child.Bounds.Height}});
                }
                File.WriteAllText(Path.Combine(output,"scrollviewport-native-minimum.json"),JsonSerializer.Serialize(new {dpr=window.RenderScaling,rows=minimums},new JsonSerializerOptions{WriteIndented=true}));
                window.Close();Console.WriteLine($"Recorded {minimums.Count} minimum cases.");return;
            }
            if(lifetime.Args[0]=="--scrollviewport-boundaries")
            {
                var boundaries=new List<object>();
                void Take(string id)
                {
                    window.UpdateLayout();var current=pane.Content!;var point=current.TranslatePoint(default,pane)!.Value;
                    boundaries.Add(new{id,width=pane.Bounds.Width,height=pane.Bounds.Height,clientWidth=pane.ClientWidth,clientHeight=pane.ClientHeight,
                        scrollWidth=pane.ClientWidth,scrollHeight=pane.ScrollHeight,offset=pane.Offset,
                        content=new{x=point.X,y=point.Y,width=current.Bounds.Width,height=current.Bounds.Height}});
                }
                pane.Offset=500;Take("offset500");child.Height=200;Take("shrink");child.Height=1140;Take("grow");
                pane.Offset=1e9;Take("bottom400");pane.Height=660;Take("resize660");pane.Height=960;Take("resize960");
                child.Height=200;Take("short960");child.Height=1140;Take("grow960");
                pane.Offset=double.NaN;Take("nan");pane.Offset=double.PositiveInfinity;Take("infinity");pane.Offset=double.NegativeInfinity;Take("negative-infinity");
                pane.Height=400;child.Height=1140.375;window.UpdateLayout();pane.Offset=1e9;Take("fractional-bottom");child.Height=1140;Take("rounded-shrink");
                pane.Offset=0;var target=new Button{Width=80,Height=30,Content="Focus target"};var body=new Canvas{Height=1140};body.Children.Add(target);Canvas.SetLeft(target,0);Canvas.SetTop(target,1000);
                pane.Content=body;window.UpdateLayout();target.BringIntoView(new Rect(0,0,80,30));Take("bring-nearest");
                File.WriteAllText(Path.Combine(output,"scrollviewport-native-boundaries.json"),JsonSerializer.Serialize(new{dpr=window.RenderScaling,rows=boundaries},new JsonSerializerOptions{WriteIndented=true}));
                window.Close();Console.WriteLine($"Recorded {boundaries.Count} candidate boundary cases.");return;
            }
            var rows = new List<object>();
            foreach (var height in new[] { 400,660,960 })
            foreach (var extent in new[] { 200,1140 })
            foreach (var requested in new[] { 0d,123.456,1e9 })
            {
                pane.Height = height; child.Height = extent; window.UpdateLayout();
                pane.Offset = requested; window.UpdateLayout();
                var point = child.TranslatePoint(default,pane)!.Value;
                rows.Add(new { requested, width=pane.Bounds.Width, height=pane.Bounds.Height,
                    clientWidth=pane.ClientWidth,clientHeight=pane.ClientHeight, scrollWidth=pane.ClientWidth,
                    scrollHeight=pane.ScrollHeight,offset=pane.Offset,
                    content=new { x=point.X,y=point.Y,width=child.Bounds.Width,height=child.Bounds.Height } });
                if(height==400 && extent==1140 && requested==0)
                {
                    using var image = new RenderTargetBitmap(new PixelSize(440,800),new Vector(192,192));
                    image.Render(pane);image.Save(Path.Combine(output,"scrollviewport-native-top.png"),new PngBitmapEncoderOptions());
                }
            }
            File.WriteAllText(Path.Combine(output,"scrollviewport-native-layout.json"),JsonSerializer.Serialize(new { dpr=window.RenderScaling,rows },new JsonSerializerOptions { WriteIndented=true }));
            window.Close();
            Console.WriteLine($"Recorded {rows.Count} candidate scroll layout cases. Paint/input not yet complete.");
        },DispatcherPriority.Background);
        base.OnFrameworkInitializationCompleted();
    }
}

internal sealed class ScrollStripes : Control
{
    public override void Render(DrawingContext context)
    {
        for(var y=0;y<Bounds.Height;y+=40)
        {
            context.DrawRectangle(Brush.Parse(y%80==0 ? "#343640" : "#2b2d31"),null,new Rect(0,y,Bounds.Width,39));
            context.DrawRectangle(Brush.Parse("#41434a"),null,new Rect(0,y+39,Bounds.Width,1));
        }
    }
}
