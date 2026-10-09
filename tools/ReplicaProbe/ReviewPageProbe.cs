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
internal sealed class ReviewPageProbeApplication:Application
{
 private sealed record Case(string Id,double Width,double Viewport,bool English,string Dbms,string SourceDatabase,string TargetDatabase,string[] Categories,int TableCount,bool Stopped);
 public override void OnFrameworkInitializationCompleted()
 {
  var life=(IClassicDesktopStyleApplicationLifetime)ApplicationLifetime!;var args=life.Args!;var cases=JsonSerializer.Deserialize<Case[]>(File.ReadAllText(args[1]),new JsonSerializerOptions{PropertyNameCaseInsensitive=true})!;var output=args[2];Directory.CreateDirectory(output);
  var n=new WebReviewPage{Width=656,ReducedMotion=true};var canvas=new Canvas{Width=1150,Height=1050,Background=Brush.Parse("#313338"),UseLayoutRounding=false};canvas.Children.Add(n);Canvas.SetLeft(n,30);Canvas.SetTop(n,40);var footer=new WebActionFooter{Width=1050};Canvas.SetLeft(footer,0);Canvas.SetTop(footer,900);canvas.Children.Add(footer);var w=new Window{Title="Replica ReviewPage verification",Width=1180,Height=1120,Content=canvas,UseLayoutRounding=false};life.MainWindow=w;
  object Box(Control c){var p=c.TranslatePoint(default,n)??default;return new{x=p.X,y=p.Y,width=c.Bounds.Width,height=c.Bounds.Height};}
  if(args[0]=="--reviewpage-window") {
   n.Width=904;n.ReducedMotion=false;n.SetPresentation(n.View,1280);n.SetFooter(footer);var changes=0;var requests=new List<string>();var text=new TextBlock{Width=1000,TextWrapping=TextWrapping.Wrap};Canvas.SetLeft(text,30);Canvas.SetTop(text,810);canvas.Children.Add(text);
   void State(){w.UpdateLayout();var json=JsonSerializer.Serialize(new{stopped=n.View.Stopped,changes,focused=n.StoppedChoice.Checkbox.IsFocused,focusVisible=n.StoppedChoice.Checkbox.FocusVisible,english=n.View.English,enabled=footer.ActionButtons[0].IsEnabled,hint=footer.Hint.Text,requests=requests.ToArray(),opacity=n.Opacity,attached=canvas.Children.Contains(n)});text.Text=json;File.WriteAllText(Path.Combine(output,"reviewpage-input.json"),json);}
   n.StoppedChanged+=(_,change)=>{if(n.View.Stopped!=change.Selected)throw new InvalidOperationException("Wrong stopped flag");changes++;n.SetFooter(footer);Dispatcher.UIThread.Post(State,DispatcherPriority.Background);};footer.ActionRequested+=(_,action)=>{requests.Add(action);Dispatcher.UIThread.Post(State,DispatcherPriority.Background);};
   void Button(string id,Action action,double x){var b=new Button{Content=id};AutomationProperties.SetAutomationId(b,"ReviewPage-"+id);b.Click+=(_,_)=>{action();Dispatcher.UIThread.Post(State,DispatcherPriority.Background);};Canvas.SetLeft(b,x);canvas.Children.Add(b);}
   Button("language",()=>{n.SetPresentation(n.View with{English=!n.View.English},1280);n.SetFooter(footer);},400);Button("reduced",()=>{n.ReducedMotion=!n.ReducedMotion;n.SetPresentation(n.View,1280);n.SetFooter(footer);},530);Button("mount",()=>{if(canvas.Children.Contains(n))canvas.Children.Remove(n);else canvas.Children.Add(n);},660);
   w.AddHandler(InputElement.GotFocusEvent,(_,_)=>Dispatcher.UIThread.Post(State),RoutingStrategies.Bubble,true);w.AddHandler(InputElement.KeyUpEvent,(_,_)=>Dispatcher.UIThread.Post(State),RoutingStrategies.Bubble,true);w.Opened+=(_,_)=>Dispatcher.UIThread.Post(State);
  } else w.Opened+=(_,_)=>Dispatcher.UIThread.Post(async()=>{var rows=new List<object>();foreach(var c in cases){n.Width=c.Width;n.SetPresentation(new(c.English,c.Dbms,c.SourceDatabase,c.TargetDatabase,c.Categories,c.TableCount,c.Stopped),c.Viewport);n.SetFooter(footer);await Dispatcher.UIThread.InvokeAsync(()=>{},DispatcherPriority.Background);w.UpdateLayout();rows.Add(new{id=c.Id,root=Box(n),heading=Box(n.Heading),summary=Box(n.Summary),notice=n.PermissionNotice is{}notice?Box(notice):null,choice=Box(n.StoppedChoice),disclaimer=Box(n.Disclaimer),title=n.Heading.TitleText.Text,description=n.Heading.DescriptionText.Text,selected=n.View.Stopped,summaryValues=n.Summary.Rows.Select(x=>(x.LabelText.Text??"")+(x.InlineValue is{}i?i.TextControl.Text:string.Concat(x.ValueLines.Select(v=>v.Text)))).ToArray(),footer=new{hint=footer.Hint.Text,enabled=footer.ActionButtons[0].IsEnabled,label=footer.ActionButtons[0].Content}});if(c.Id is "compact-ko-0-false-656" or "wide-en-1-true-904"){using var sample=new RenderTargetBitmap(new PixelSize(2300,2100),new Vector(192,192));sample.Render(canvas);sample.Save(Path.Combine(output,"reviewpage-"+c.Id+".png"),new PngBitmapEncoderOptions());}}using var bmp=new RenderTargetBitmap(new PixelSize(2300,2100),new Vector(192,192));bmp.Render(canvas);bmp.Save(Path.Combine(output,"reviewpage.png"),new PngBitmapEncoderOptions());File.WriteAllText(Path.Combine(output,"reviewpage-native.json"),JsonSerializer.Serialize(new{dpr=w.RenderScaling,rows},new JsonSerializerOptions{WriteIndented=true}));w.Close();Console.WriteLine("Recorded "+rows.Count+" Review page cases");},DispatcherPriority.Background);
  base.OnFrameworkInitializationCompleted();
 }
}
