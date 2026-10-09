using System.Text.Json;
using Avalonia;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using Verifier.Desktop;
internal sealed class ChoiceProbeApplication:Application
{
 private sealed record Case(string Id,double Width,string Text,string Flag,bool Selected);
 public override void OnFrameworkInitializationCompleted()
 {
  var life=(IClassicDesktopStyleApplicationLifetime)ApplicationLifetime!;var args=life.Args!;var cases=JsonSerializer.Deserialize<Case[]>(File.ReadAllText(args[1]),new JsonSerializerOptions{PropertyNameCaseInsensitive=true})!;var output=args[2];Directory.CreateDirectory(output);
  var n=new WebChoice{Width=904,ReducedMotion=true};var canvas=new Canvas{Width=1100,Height=650,Background=Brush.Parse("#313338"),UseLayoutRounding=false};canvas.Children.Add(n);Canvas.SetLeft(n,30);Canvas.SetTop(n,100);var w=new Window{Title="Replica Choice verification",Width=1130,Height=750,Content=canvas,UseLayoutRounding=false};life.MainWindow=w;
  object Box(Control c){var p=c.TranslatePoint(default,n)??default;return new{x=p.X,y=p.Y,width=c.Bounds.Width,height=c.Bounds.Height};}
  if(args[0]=="--choice-window") {
   var first=cases[0];n.Width=first.Width;n.ReducedMotion=false;n.SetPresentation(first.Text,first.Flag,false);var changes=0;var text=new TextBlock{Width=1000,TextWrapping=TextWrapping.Wrap};Canvas.SetLeft(text,30);Canvas.SetTop(text,400);canvas.Children.Add(text);
   void State(){w.UpdateLayout();var json=JsonSerializer.Serialize(new{selected=n.IsSelected,changes,focused=n.Checkbox.IsFocused,focusVisible=n.Checkbox.FocusVisible,enabled=n.Checkbox.IsEnabled});text.Text=json;File.WriteAllText(Path.Combine(output,"choice-input.json"),json);}
   n.SelectionRequested+=(_,_)=>{changes++;Dispatcher.UIThread.Post(State);};w.AddHandler(InputElement.GotFocusEvent,(_,_)=>Dispatcher.UIThread.Post(State),RoutingStrategies.Bubble,true);w.AddHandler(InputElement.KeyUpEvent,(_,_)=>Dispatcher.UIThread.Post(State),RoutingStrategies.Bubble,true);
   var button=new Button{Content="Disabled"};AutomationProperties.SetAutomationId(button,"Choice-disabled");button.Click+=(_,_)=>{n.Checkbox.IsEnabled=!n.Checkbox.IsEnabled;State();};Canvas.SetLeft(button,600);Canvas.SetTop(button,20);canvas.Children.Add(button);w.Opened+=(_,_)=>State();
  } else w.Opened+=(_,_)=>Dispatcher.UIThread.Post(async()=>{var rows=new List<object>();foreach(var c in cases){n.Width=c.Width;n.SetPresentation(c.Text,c.Flag,c.Selected);await Dispatcher.UIThread.InvokeAsync(()=>{},DispatcherPriority.Background);w.UpdateLayout();rows.Add(new{id=c.Id,root=Box(n),input=Box(n.Checkbox),label=Box(n.Label),selected=n.IsSelected,text=n.Label.Text});}using var bmp=new RenderTargetBitmap(new PixelSize(2200,1300),new Vector(192,192));bmp.Render(canvas);bmp.Save(Path.Combine(output,"choice.png"),new PngBitmapEncoderOptions());File.WriteAllText(Path.Combine(output,"choice-native.json"),JsonSerializer.Serialize(new{dpr=w.RenderScaling,rows},new JsonSerializerOptions{WriteIndented=true}));w.Close();Console.WriteLine("Recorded "+rows.Count+" Choice cases");},DispatcherPriority.Background);
  base.OnFrameworkInitializationCompleted();
 }
}
