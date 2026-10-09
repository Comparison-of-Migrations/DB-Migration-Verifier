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
internal sealed class DbmsPickerProbeApplication:Application
{
 private sealed record Case(string Id,double Width,double Viewport,bool English,string Engine);
 public override void OnFrameworkInitializationCompleted()
 {
  var life=(IClassicDesktopStyleApplicationLifetime)ApplicationLifetime!;var args=life.Args!;var cases=JsonSerializer.Deserialize<Case[]>(File.ReadAllText(args[1]),new JsonSerializerOptions{PropertyNameCaseInsensitive=true})!;var output=args[2];Directory.CreateDirectory(output);
  var n=new WebDbmsPicker{Width=904,ReducedMotion=true};var canvas=new Canvas{Width=1150,Height=700,Background=Brush.Parse("#313338"),UseLayoutRounding=false};canvas.Children.Add(n);Canvas.SetLeft(n,30);Canvas.SetTop(n,100);var w=new Window{Title="Replica DbmsPicker verification",Width=1180,Height=770,Content=canvas,UseLayoutRounding=false};life.MainWindow=w;
  object Box(Control c){var p=c.TranslatePoint(default,n)??default;return new{x=p.X,y=p.Y,width=c.Bounds.Width,height=c.Bounds.Height};}
  if(args[0]=="--dbmspicker-window") {
   n.ReducedMotion=false;n.SetPresentation(false,"SQL Server",1280);var changes=new List<string>();var text=new TextBlock{Width=1000,TextWrapping=TextWrapping.Wrap};Canvas.SetLeft(text,30);Canvas.SetTop(text,450);canvas.Children.Add(text);
   void State(){w.UpdateLayout();var json=JsonSerializer.Serialize(new{engine=n.SelectedEngine,changes=changes.ToArray(),checkedEngines=n.Options.Where(x=>x.IsSelected).Select(x=>x.EngineName).ToArray(),focused=n.Options.Select((x,i)=>(x,i)).Where(x=>x.x.Radio.IsFocused).Select(x=>x.i).ToArray(),focusVisible=n.Options.Select(x=>x.Radio.FocusVisible).ToArray(),english=n.English,attached=canvas.Children.Contains(n)});text.Text=json;File.WriteAllText(Path.Combine(output,"dbmspicker-input.json"),json);}
   n.EngineChanged+=(_,engine)=>{changes.Add(engine);Dispatcher.UIThread.Post(State);};
   void Button(string id,Action action,double x){var b=new Button{Content=id};AutomationProperties.SetAutomationId(b,"DbmsPicker-"+id);b.Click+=(_,_)=>{action();Dispatcher.UIThread.Post(State);};Canvas.SetLeft(b,x);canvas.Children.Add(b);}
   Button("language",()=>n.SetPresentation(!n.English,n.SelectedEngine,1280),400);Button("update",()=>n.SetPresentation(n.English,"MySQL",1000),530);Button("mount",()=>{if(canvas.Children.Contains(n))canvas.Children.Remove(n);else canvas.Children.Add(n);},660);
   w.AddHandler(InputElement.GotFocusEvent,(_,_)=>Dispatcher.UIThread.Post(State),RoutingStrategies.Bubble,true);w.AddHandler(InputElement.KeyUpEvent,(_,_)=>Dispatcher.UIThread.Post(State),RoutingStrategies.Bubble,true);w.Opened+=(_,_)=>State();
  } else w.Opened+=(_,_)=>Dispatcher.UIThread.Post(async()=>{var rows=new List<object>();foreach(var c in cases){n.Width=c.Width;n.SetPresentation(c.English,c.Engine,c.Viewport);await Dispatcher.UIThread.InvokeAsync(()=>{},DispatcherPriority.Background);w.UpdateLayout();rows.Add(new{id=c.Id,root=Box(n),legend=Box(n.LegendBox),grid=Box(n.Cards),note=Box(n.Note),legendText=n.Legend.Text,noteText=n.Note.Text,options=n.Options.Select(x=>new{box=Box(x),engine=x.EngineName,description=x.Description,selected=x.IsSelected}).ToArray()});if(c.Id is "compact-ko-904-SQL Server" or "wide-en-904-MariaDB"){using var sample=new RenderTargetBitmap(new PixelSize(2300,1400),new Vector(192,192));sample.Render(canvas);sample.Save(Path.Combine(output,"dbmspicker-"+c.Id+".png"),new PngBitmapEncoderOptions());}}using var bmp=new RenderTargetBitmap(new PixelSize(2300,1400),new Vector(192,192));bmp.Render(canvas);bmp.Save(Path.Combine(output,"dbmspicker.png"),new PngBitmapEncoderOptions());File.WriteAllText(Path.Combine(output,"dbmspicker-native.json"),JsonSerializer.Serialize(new{dpr=w.RenderScaling,rows},new JsonSerializerOptions{WriteIndented=true}));w.Close();Console.WriteLine("Recorded "+rows.Count+" DbmsPicker cases");},DispatcherPriority.Background);
  base.OnFrameworkInitializationCompleted();
 }
}
