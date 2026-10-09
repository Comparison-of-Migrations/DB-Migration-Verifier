using System.Text.Json;
using Avalonia;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Headless;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Verifier.Desktop;
internal sealed class CapabilityViewportProbeApplication:Application
{
 private sealed record Case(string Id,double Width,string Mode,double Height,double LogicalWidth,double ExtentWidth);
 private sealed class Stripes:Control
 {
  public override void Render(DrawingContext context){for(var x=0d;x<Bounds.Width;x+=21){context.DrawRectangle(Brush.Parse("#232428"),null,new Rect(x,0,Math.Min(20,Bounds.Width-x),Bounds.Height));if(x+20<Bounds.Width)context.DrawRectangle(Brush.Parse("#41434a"),null,new Rect(x+20,0,Math.Min(1,Bounds.Width-x-20),Bounds.Height));}}
 }
 private sealed class ContentBox:Panel
 {
  public double ContentWidth{get;set;}public double ContentHeight{get;set;}public double ExtentWidth{get;set;}
  public readonly Stripes Plane=new(){UseLayoutRounding=false};
  public ContentBox(){UseLayoutRounding=false;Children.Add(Plane);}
  protected override Size MeasureOverride(Size s){foreach(var child in Children)child.Measure(child==Plane?new Size(ExtentWidth,ContentHeight):new Size(150,44));return new(ContentWidth,ContentHeight);}
  protected override Size ArrangeOverride(Size s){foreach(var child in Children)child.Arrange(child==Plane?new Rect(0,0,ExtentWidth,ContentHeight):new Rect(630,35,150,44));return s;}
  public override string ToString()=>"Source diagnostic stripes";
 }
 public override void OnFrameworkInitializationCompleted()
 {
  var life=(IClassicDesktopStyleApplicationLifetime)ApplicationLifetime!;var args=life.Args!;var cases=JsonSerializer.Deserialize<Case[]>(File.ReadAllText(args[1]),new JsonSerializerOptions{PropertyNameCaseInsensitive=true})!;var output=args[2];Directory.CreateDirectory(output);
  var content=new ContentBox();var viewport=new WebCapabilityViewport{Content=content,Background=Brush.Parse("#2b2d31"),UseLayoutRounding=false};AutomationProperties.SetAutomationId(viewport,"CapabilityViewport-box");
  var canvas=new Canvas{Width=1200,Height=800,HorizontalAlignment=Avalonia.Layout.HorizontalAlignment.Left,VerticalAlignment=Avalonia.Layout.VerticalAlignment.Top,Background=Brush.Parse("#2b2d31"),UseLayoutRounding=false};canvas.Children.Add(viewport);Canvas.SetLeft(viewport,30);Canvas.SetTop(viewport,70);
  var window=new Window{Title="Replica CapabilityViewport verification",Width=1250,Height=850,Content=canvas,UseLayoutRounding=false};life.MainWindow=window;
  void Set(Case c){content.ContentWidth=c.LogicalWidth;content.ContentHeight=c.Height;content.ExtentWidth=c.ExtentWidth;content.InvalidateMeasure();viewport.LogicalWidth=c.LogicalWidth;viewport.OverflowWidth=c.ExtentWidth;viewport.Width=c.Width;viewport.InvalidateMeasure();window.UpdateLayout();}
  object State(string id,string label)=>new{id,label,width=viewport.Width,height=content.ContentHeight,logicalWidth=viewport.LogicalWidth,extentWidth=viewport.OverflowWidth,shell=new{x=0,y=0,width=viewport.Bounds.Width,height=viewport.Bounds.Height},content=new{x=content.Bounds.X,y=content.Bounds.Y,width=content.Bounds.Width,height=content.Bounds.Height},extent=new{x=content.Bounds.X,y=content.Bounds.Y,width=content.ExtentWidth,height=content.ContentHeight},clientWidth=viewport.ClientWidth,clientHeight=viewport.ClientHeight,scrollWidth=viewport.ScrollWidth,scrollHeight=Math.Round(viewport.ClientHeight,MidpointRounding.AwayFromZero),offset=viewport.Offset,maximum=viewport.MaximumOffset,gutter=viewport.GutterHeight,focusable=viewport.Focusable,tabStop=viewport.IsTabStop,focusVisible=viewport.IsFocusVisible};
  if(args[0]=="--capabilityviewport-window"){
   var child=false;var fit=false;var frames=new List<object>();var keys=new List<object>();var focusEvents=new List<object>();var epoch=0;var timer=new DispatcherTimer{Interval=TimeSpan.FromMilliseconds(32)};
   void Configure(){epoch++;frames.Clear();keys.Clear();content.Children.Clear();content.Children.Add(content.Plane);if(child){var button=new Button{Content="Required permissions",MinHeight=44};AutomationProperties.SetAutomationId(button,"CapabilityViewport-child");content.Children.Add(button);}Set(new("input",478,fit?"fit":"intrinsic",120,fit?458:800,fit?458:800));viewport.Offset=0;}
   void Record(){window.UpdateLayout();var focus=window.FocusManager?.GetFocusedElement()as Control;var state=new{dpr=window.RenderScaling,epoch,attached=canvas.Children.Contains(viewport),child,fit,offset=viewport.Offset,top=0,width=viewport.Bounds.Width,height=viewport.Bounds.Height,clientWidth=viewport.ClientWidth,clientHeight=viewport.ClientHeight,scrollWidth=viewport.ScrollWidth,scrollHeight=Math.Round(viewport.ClientHeight),active=focus is null?"":AutomationProperties.GetAutomationId(focus),focusVisible=viewport.IsFocusVisible,maximum=viewport.MaximumOffset,focusable=viewport.Focusable,tabStop=viewport.IsTabStop,contentFocusables=content.GetVisualDescendants().Prepend(content).OfType<InputElement>().Where(x=>x.Focusable).Select(x=>new{type=x.GetType().Name,tabStop=x.IsTabStop,visible=x.IsEffectivelyVisible}).ToArray(),focusEvents=focusEvents.TakeLast(30).ToArray(),frames=frames.ToArray(),keys=keys.ToArray()};File.WriteAllText(Path.Combine(output,"capabilityviewport-input.json"),JsonSerializer.Serialize(state));}
   void Utility(string id,double x,Action a){var b=new Button{Content=id,MinHeight=44,Padding=new(12,8),Focusable=false,IsTabStop=false};AutomationProperties.SetAutomationId(b,"CapabilityViewport-"+id);b.Click+=(_,_)=>{a();Dispatcher.UIThread.Post(Record);};Canvas.SetLeft(b,x);canvas.Children.Add(b);}
   if(args.Contains("--focus-only")){var before=new Button{Content="Focus before viewport",Width=180,Height=44};AutomationProperties.SetAutomationId(before,"CapabilityViewport-before");Canvas.SetLeft(before,600);canvas.Children.Insert(0,before);}
   Utility("reset",0,()=>{child=false;fit=false;Configure();});Utility("middle",90,()=>viewport.Offset=100);Utility("child-toggle",190,()=>{child=!child;Configure();});Utility("fit",310,()=>{fit=!fit;Configure();});Utility("mount",400,()=>{if(canvas.Children.Contains(viewport))canvas.Children.Remove(viewport);else canvas.Children.Add(viewport);});Utility("record",510,Record);
   window.AddHandler(InputElement.GotFocusEvent,(_,e)=>focusEvents.Add(new{id=e.Source is Control c?AutomationProperties.GetAutomationId(c):null,type=e.Source?.GetType().Name,method=e.NavigationMethod.ToString()}),Avalonia.Interactivity.RoutingStrategies.Bubble,true);
   window.AddHandler(InputElement.KeyDownEvent,(_,e)=>keys.Add(new{key=e.Key.ToString(),shift=e.KeyModifiers.HasFlag(KeyModifiers.Shift),handled=e.Handled}),Avalonia.Interactivity.RoutingStrategies.Bubble,true);
   timer.Tick+=(_,_)=>{frames.Add(new{t=DateTime.UtcNow.Ticks,offset=viewport.Offset});if(frames.Count>180)frames.RemoveAt(0);Record();};window.Opened+=(_,_)=>{Configure();timer.Start();};window.Closed+=(_,_)=>timer.Stop();
  }else window.Opened+=(_,_)=>Dispatcher.UIThread.Post(async()=>{
   var samples=new List<object>();foreach(var c in (args.Contains("--extra-only")?Array.Empty<Case>():cases)){Set(c);foreach(var (label,value) in new[]{("zero",0d),("fractional",123.456),("maximum",viewport.MaximumOffset)}){viewport.Offset=value;window.UpdateLayout();await Dispatcher.UIThread.InvokeAsync(()=>{},DispatcherPriority.Background);window.UpdateLayout();samples.Add(State(c.Id,label));}if(c.Id is "478-intrinsic-120" or "478.25-descendant-684.6796875"){using var image=new RenderTargetBitmap(new PixelSize(2400,1600),new Vector(192,192));image.Render(canvas);image.Save(Path.Combine(output,"capabilityviewport-"+c.Id+".png"),new PngBitmapEncoderOptions());}}
   File.WriteAllText(Path.Combine(output,"capabilityviewport-native.json"),JsonSerializer.Serialize(new{dpr=window.RenderScaling,samples},new JsonSerializerOptions{WriteIndented=true}));
   if(ReplicaPlatform.IsHeadless&&args.Contains("--extra-only")){if(args.Contains("--focus-only")){Set(new("input",478,"intrinsic",120,800,800));await CapabilityViewportFocusInput.Run(window,canvas,viewport,content,output,args.Contains("--clip-only"));}else await CapabilityViewportHeadlessInput.Run(window,canvas,viewport,content,output,args.Contains("--child-only"));window.Close();Console.WriteLine("Recorded new horizontal input scope only");return;}
   if(ReplicaPlatform.IsHeadless){var states=new List<object>();Set(new("input",478,"intrinsic",120,800,800));window.Activate();window.UpdateLayout();viewport.Focus(NavigationMethod.Pointer);
    async Task Flush(){await Task.Delay(270);window.UpdateLayout();AvaloniaHeadlessPlatform.ForceRenderTimerTick(1);}
    foreach(var (key,label,expected) in new[]{(Key.Right,"right",140d),(Key.Left,"left",60d),(Key.Down,"down",100d),(Key.Up,"up",100d),(Key.PageDown,"page-down",100d),(Key.PageUp,"page-up",100d),(Key.Home,"home",100d),(Key.End,"end",100d),(Key.Space,"space",100d)}){viewport.Offset=100;window.KeyPress(key,RawInputModifiers.None,PhysicalKey.None,null);window.KeyRelease(key,RawInputModifiers.None,PhysicalKey.None,null);await Flush();if(viewport.Offset!=expected)throw new InvalidOperationException("Horizontal source key mismatch "+label+":"+viewport.Offset);states.Add(State("input",label));}
    viewport.Offset=100;canvas.Children.Remove(viewport);window.UpdateLayout();if(viewport.Offset!=0)throw new InvalidOperationException("Detached horizontal offset retained");states.Add(new{id="detach",offset=viewport.Offset});canvas.Children.Add(viewport);window.UpdateLayout();states.Add(State("input","remount"));Set(new("fit",478,"fit",120,458,458));if(viewport.MaximumOffset!=0||viewport.IsTabStop)throw new InvalidOperationException("Fit viewport has extra tab stop");states.Add(State("input","fit"));
    using(var image=window.CaptureRenderedFrame()??throw new InvalidOperationException("Real rendered viewport frame missing"))image.Save(Path.Combine(output,"capabilityviewport-headless-input.png"),new PngBitmapEncoderOptions());File.WriteAllText(Path.Combine(output,"capabilityviewport-headless-input.json"),JsonSerializer.Serialize(new{states}));}
   window.Close();Console.WriteLine("Recorded "+samples.Count+" horizontal viewport states");},DispatcherPriority.Background);
  base.OnFrameworkInitializationCompleted();
 }
}
