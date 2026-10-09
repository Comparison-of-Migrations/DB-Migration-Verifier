using System.Text.Json;
using System.Diagnostics;
using Avalonia;
using Avalonia.Automation;
using Avalonia.Automation.Peers;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Controls.Presenters;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Verifier.Desktop;

internal sealed class NumberInputProbeApplication:Application
{
    public override void Initialize(){Styles.Add(new Avalonia.Themes.Fluent.FluentTheme());}
    public override void OnFrameworkInitializationCompleted()
    {
        var desktop=(IClassicDesktopStyleApplicationLifetime)ApplicationLifetime!;
        var output=desktop.Args![1];Directory.CreateDirectory(output);
        var canvas=new Canvas{Background=Brush.Parse("#313338"),UseLayoutRounding=false};
        var ids=new[]{"number-default","number-empty","number-min","number-max","number-fraction","number-exponent"};
        var values=new[]{"3306","","1","65535","1.5","1e3"};
        var controls=new List<WebNumberInput>();var events=new List<object>();
        var clock=Stopwatch.StartNew();
        var w=new Window{Title="Replica NumberInput verification",Width=460,Height=450,Content=canvas};desktop.MainWindow=w;
        object Rectangle(Rect r)=>new{x=r.X,y=r.Y,width=r.Width,height=r.Height};
        void Record(string name="numberinput-input.json",bool capture=false)
        {
            w.UpdateLayout();
            if(capture)
            {using var b=new RenderTargetBitmap(new PixelSize(920,900),new Vector(192,192));b.Render(canvas);b.Save(Path.Combine(output,Path.GetFileNameWithoutExtension(name)+".png"),new PngBitmapEncoderOptions());}
            var rows=controls.Select((n,i)=>
            {
                var state=n.Validity;var p=n.Editor.GetVisualDescendants().OfType<TextPresenter>().SingleOrDefault();
                var spin=n.GetVisualDescendants().Single(v=>v.GetType().Name=="NumberSpin");
                var scroll=n.Editor.GetVisualDescendants().OfType<ScrollViewer>().SingleOrDefault();
                var caret=n.Editor.GetVisualDescendants().SingleOrDefault(v=>v.GetType().Name=="WebInputCaret");
                var c=caret?.GetType().GetProperty("CaretRect")?.GetValue(caret) is Rect r?r:default;
                var peer=ControlAutomationPeer.CreatePeerForElement(n.Editor);
                return new{id=ids[i],raw=n.RawText,value=n.Value,number=state.Number,badInput=state.BadInput,rangeUnderflow=state.RangeUnderflow,rangeOverflow=state.RangeOverflow,stepMismatch=state.StepMismatch,valid=state.Valid,
                    focused=n.Editor.IsFocused,rect=Rectangle(n.Bounds),peerRect=Rectangle(peer.GetBoundingRectangle()),peerOffscreen=peer.IsOffscreen(),peerParent=peer.GetParent()?.GetClassName(),spinVisible=spin.IsVisible,spin=Rectangle(spin.Bounds),presenter=p is null?null:Rectangle(p.Bounds),textWidth=p?.TextLayout.WidthIncludingTrailingWhitespace,caret=capture?Rectangle(c):null,paintObserved=capture,offset=scroll?.Offset.X};
            }).ToArray();
            File.WriteAllText(Path.Combine(output,name),JsonSerializer.Serialize(new{utc=DateTime.UtcNow,events,rows},new JsonSerializerOptions{WriteIndented=true}));
        }
        foreach(var i in Enumerable.Range(0,ids.Length))
        {
            var index=i;var n=new WebNumberInput{Value=values[i]};controls.Add(n);
            Canvas.SetLeft(n,30);Canvas.SetTop(n,20+i*68);canvas.Children.Add(n);
            AutomationProperties.SetName(n,ids[i]);AutomationProperties.SetAutomationId(n,ids[i]);
            n.Input+=(_,_)=>{events.Add(new{id=ids[index],type="input",timeStamp=clock.Elapsed.TotalMilliseconds,raw=n.RawText,value=n.Value});Dispatcher.UIThread.Post(()=>Record());};
            n.Change+=(_,_)=>{events.Add(new{id=ids[index],type="change",timeStamp=clock.Elapsed.TotalMilliseconds,raw=n.RawText,value=n.Value});Dispatcher.UIThread.Post(()=>Record());};
            n.AddHandler(InputElement.PointerPressedEvent,(_,e)=>events.Add(new{id=ids[index],type="pointerdown",timeStamp=clock.Elapsed.TotalMilliseconds,raw=n.RawText,value=n.Value}),RoutingStrategies.Tunnel,true);
            n.AddHandler(InputElement.PointerReleasedEvent,(_,e)=>events.Add(new{id=ids[index],type="pointerup",timeStamp=clock.Elapsed.TotalMilliseconds,raw=n.RawText,value=n.Value}),RoutingStrategies.Tunnel,true);
        }
        w.KeyUp+=(_,e)=>
        {
            var key=e.Key.ToString();
            events.Add(new{type="keyup",key});
            Dispatcher.UIThread.Post(()=>Record("numberinput-keyup-"+key+".json",true),DispatcherPriority.Background);
        };
        w.AddHandler(InputElement.PointerReleasedEvent,(_,_)=>Dispatcher.UIThread.Post(()=>Record("numberinput-pointer.json",true),DispatcherPriority.Background),RoutingStrategies.Bubble,true);
        w.Opened+=(_,_)=>Dispatcher.UIThread.Post(()=>Record("numberinput-initial.json",true),DispatcherPriority.Background);
        base.OnFrameworkInitializationCompleted();
    }
}
