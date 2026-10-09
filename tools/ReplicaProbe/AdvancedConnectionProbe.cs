using System.Text.Json;
using Avalonia;
using Avalonia.Automation;
using Avalonia.Automation.Peers;
using Avalonia.Automation.Provider;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using Verifier.Desktop;
internal sealed class AdvancedConnectionProbeApplication:Application
{
 private sealed record Case(string Id,double Width,double Viewport,bool English,bool Expanded);
 public override void OnFrameworkInitializationCompleted()
 {
  var life=(IClassicDesktopStyleApplicationLifetime)ApplicationLifetime!;var args=life.Args!;var cases=JsonSerializer.Deserialize<Case[]>(File.ReadAllText(args[1]),new JsonSerializerOptions{PropertyNameCaseInsensitive=true})!;var output=args[2];Directory.CreateDirectory(output);
  var n=new WebAdvancedConnection{Width=904,ReducedMotion=true};var canvas=new Canvas{Width=1150,Height=700,Background=Brush.Parse("#313338"),UseLayoutRounding=false};canvas.Children.Add(n);Canvas.SetLeft(n,30);Canvas.SetTop(n,100);var w=new Window{Title="Replica AdvancedConnection verification",Width=1180,Height=770,Content=canvas,UseLayoutRounding=false};life.MainWindow=w;
  object Box(Control c){var p=c.TranslatePoint(default,n)??default;return new{x=p.X,y=p.Y,width=c.Bounds.Width,height=c.Bounds.Height};}
  ExpandCollapseState Peer()=>((IExpandCollapseProvider)ControlAutomationPeer.CreatePeerForElement(n.ToggleButton)!).ExpandCollapseState;
  if(args[0]=="--advancedconnection-window") {
   n.ReducedMotion=false;n.SetPresentation(false,false,1280);var changes=new List<bool>();var text=new TextBlock{Width=1000,TextWrapping=TextWrapping.Wrap};Canvas.SetLeft(text,30);Canvas.SetTop(text,480);canvas.Children.Add(text);
   void State(){w.UpdateLayout();var json=JsonSerializer.Serialize(new{expanded=n.Expanded,peer=Peer().ToString(),changes=changes.ToArray(),focused=n.ToggleButton.IsFocused,focusVisible=n.ToggleButton.FocusVisible,english=n.English,opacity=n.Explanation?.Opacity,attached=canvas.Children.Contains(n)});text.Text=json;File.WriteAllText(Path.Combine(output,"advancedconnection-input.json"),json);}
   n.ExpansionRequested+=(_,change)=>{changes.Add(change.Expanded);n.SetPresentation(n.English,change.Expanded,1280,true,change.FocusVisible);Dispatcher.UIThread.Post(State,DispatcherPriority.Background);};
   void Button(string id,Action action,double x){var b=new Button{Content=id};AutomationProperties.SetAutomationId(b,"AdvancedConnection-"+id);b.Click+=(_,_)=>{action();Dispatcher.UIThread.Post(State,DispatcherPriority.Background);};Canvas.SetLeft(b,x);canvas.Children.Add(b);}
   Button("language",()=>n.SetPresentation(!n.English,n.Expanded,1280),400);Button("reduced",()=>{n.ReducedMotion=!n.ReducedMotion;n.SetPresentation(n.English,n.Expanded,1280);},530);Button("mount",()=>{if(canvas.Children.Contains(n))canvas.Children.Remove(n);else canvas.Children.Add(n);},660);
   w.AddHandler(InputElement.GotFocusEvent,(_,_)=>Dispatcher.UIThread.Post(State),RoutingStrategies.Bubble,true);w.AddHandler(InputElement.KeyUpEvent,(_,_)=>Dispatcher.UIThread.Post(State),RoutingStrategies.Bubble,true);w.Opened+=(_,_)=>State();
  } else w.Opened+=(_,_)=>Dispatcher.UIThread.Post(async()=>{var rows=new List<object>();foreach(var c in cases){n.Width=c.Width;n.SetPresentation(c.English,c.Expanded,c.Viewport);await Dispatcher.UIThread.InvokeAsync(()=>{},DispatcherPriority.Background);w.UpdateLayout();rows.Add(new{id=c.Id,root=Box(n),button=Box(n.ToggleButton),panel=n.Explanation is{}panel?Box(panel):null,titles=n.Titles.Select(x=>new{box=Box(x),text=x.Text}).ToArray(),descriptions=n.Descriptions.Select(x=>new{box=Box(x),text=x.Text}).ToArray(),label=n.ToggleButton.Content,expanded=Peer()==ExpandCollapseState.Expanded});if(c.Id is "compact-ko-904-true" or "wide-en-904-true"){using var sample=new RenderTargetBitmap(new PixelSize(2300,1400),new Vector(192,192));sample.Render(canvas);sample.Save(Path.Combine(output,"advancedconnection-"+c.Id+".png"),new PngBitmapEncoderOptions());}}using var bmp=new RenderTargetBitmap(new PixelSize(2300,1400),new Vector(192,192));bmp.Render(canvas);bmp.Save(Path.Combine(output,"advancedconnection.png"),new PngBitmapEncoderOptions());File.WriteAllText(Path.Combine(output,"advancedconnection-native.json"),JsonSerializer.Serialize(new{dpr=w.RenderScaling,rows},new JsonSerializerOptions{WriteIndented=true}));w.Close();Console.WriteLine("Recorded "+rows.Count+" AdvancedConnection cases");},DispatcherPriority.Background);
  base.OnFrameworkInitializationCompleted();
 }
}
