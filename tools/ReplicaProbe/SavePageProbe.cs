using System.Text.Json;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Automation;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using Verifier.Desktop;
internal sealed class SavePageProbeApplication:Application
{
 private sealed record Case(string Id,double Width,double Viewport,bool English,bool Saved,string[] Formats);
 public override void OnFrameworkInitializationCompleted()
 {
  var life=(IClassicDesktopStyleApplicationLifetime)ApplicationLifetime!;var args=life.Args!;var cases=JsonSerializer.Deserialize<Case[]>(File.ReadAllText(args[1]),new JsonSerializerOptions{PropertyNameCaseInsensitive=true})!;var output=args[2];Directory.CreateDirectory(output);
  var n=new WebSavePage{Width=656,ReducedMotion=true};var canvas=new Canvas{Width=1150,Height=950,Background=Brush.Parse("#313338"),UseLayoutRounding=false};canvas.Children.Add(n);Canvas.SetLeft(n,30);Canvas.SetTop(n,40);var w=new Window{Title="Replica SavePage verification",Width=1180,Height=1000,Content=canvas,UseLayoutRounding=false};life.MainWindow=w;
  object Box(Control c){var p=c.TranslatePoint(default,n)??default;return new{x=p.X,y=p.Y,width=c.Bounds.Width,height=c.Bounds.Height};}
  if(args[0]=="--savepage-window") {
   n.ReducedMotion=false;n.Width=904;n.SetPresentation(new(false,false,["json","csv","html"]),1280);
   var footer=new WebActionFooter{Width=1050};Canvas.SetLeft(footer,0);Canvas.SetTop(footer,780);canvas.Children.Add(footer);n.SetFooter(footer);
   var changes=0;var requests=new List<string>();var stateText=new TextBlock{Width=1000,TextWrapping=TextWrapping.Wrap,FontSize=11};Canvas.SetLeft(stateText,30);Canvas.SetTop(stateText,680);canvas.Children.Add(stateText);
   void State(){w.UpdateLayout();var focused=n.Options.Options.FirstOrDefault(o=>o.Checkbox.IsFocused);var value=new{formats=n.View.Formats,changes,focused=focused?.Format,focusVisible=focused?.Checkbox.FocusVisible??false,english=n.View.English,saved=n.View.Saved,downloadEnabled=footer.ActionButtons[0].IsEnabled,hint=footer.Hint.Text,requests=requests.ToArray(),opacity=n.Opacity,attached=canvas.Children.Contains(n)};var json=JsonSerializer.Serialize(value);stateText.Text=json;File.WriteAllText(Path.Combine(output,"savepage-input.json"),json);}
   n.FormatChanged+=(_,change)=>{if(n.View.Formats.Contains(change.Format)!=change.Selected)throw new InvalidOperationException("Wrong page selection");changes++;n.SetFooter(footer);Dispatcher.UIThread.Post(State,DispatcherPriority.Background);};
   n.ActionRequested+=(_,action)=>{requests.Add(action);Dispatcher.UIThread.Post(State,DispatcherPriority.Background);};
   footer.ActionRequested+=(_,action)=>{requests.Add(action);if(action=="download"){n.SetPresentation(n.View with{Saved=true},1280);n.SetFooter(footer);}Dispatcher.UIThread.Post(State,DispatcherPriority.Background);};
   void Button(string id,string label,Action action,double x){var b=new Button{Content=label};AutomationProperties.SetAutomationId(b,"SavePage-"+id);b.Click+=(_,_)=>{action();Dispatcher.UIThread.Post(State,DispatcherPriority.Background);};Canvas.SetLeft(b,x);Canvas.SetTop(b,0);canvas.Children.Add(b);}
   Button("language","Language",()=>{n.SetPresentation(n.View with{English=!n.View.English},1280);n.SetFooter(footer);},400);
   Button("reduced","Reduced",()=>{n.ReducedMotion=!n.ReducedMotion;n.SetPresentation(n.View,1280);n.SetFooter(footer);},530);
   Button("mount","Mount",()=>{if(canvas.Children.Contains(n))canvas.Children.Remove(n);else canvas.Children.Add(n);},660);
   w.AddHandler(InputElement.GotFocusEvent,(_,_)=>Dispatcher.UIThread.Post(State),RoutingStrategies.Bubble,true);w.AddHandler(InputElement.KeyUpEvent,(_,_)=>Dispatcher.UIThread.Post(State),RoutingStrategies.Bubble,true);
   w.Opened+=(_,_)=>Dispatcher.UIThread.Post(State,DispatcherPriority.Background);
  }
  else w.Opened+=(_,_)=>Dispatcher.UIThread.Post(async()=>{var rows=new List<object>();foreach(var c in cases){n.Width=c.Width;n.SetPresentation(new(c.English,c.Saved,c.Formats),c.Viewport);await Dispatcher.UIThread.InvokeAsync(()=>{},DispatcherPriority.Background);w.UpdateLayout();rows.Add(new{id=c.Id,root=Box(n),heading=Box(n.Heading),options=Box(n.Options),summary=Box(n.Summary),notice=n.SavedNotice is {}notice?Box(notice):null,buttons=n.ActionButtons.Select(b=>new{box=Box(b),label=b.Content}).ToArray(),title=n.Heading.TitleText.Text,description=n.Heading.DescriptionText.Text, @checked=n.Options.SelectedFormats});if(c.Id is "compact-ko-true-0-656" or "wide-en-true-3-904"){using var sample=new RenderTargetBitmap(new PixelSize(2300,1900),new Vector(192,192));sample.Render(canvas);sample.Save(Path.Combine(output,"savepage-"+c.Id+".png"),new PngBitmapEncoderOptions());}}using var bmp=new RenderTargetBitmap(new PixelSize(2300,1900),new Vector(192,192));bmp.Render(canvas);bmp.Save(Path.Combine(output,"savepage.png"),new PngBitmapEncoderOptions());File.WriteAllText(Path.Combine(output,"savepage-native.json"),JsonSerializer.Serialize(new{dpr=w.RenderScaling,rows},new JsonSerializerOptions{WriteIndented=true}));w.Close();Console.WriteLine("Recorded "+rows.Count+" Save page cases");},DispatcherPriority.Background);
  base.OnFrameworkInitializationCompleted();
 }
}
