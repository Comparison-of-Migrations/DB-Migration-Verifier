using System.Text.Json;
using Avalonia;
using Avalonia.Automation;
using Avalonia.Automation.Peers;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using Verifier.Desktop;
internal sealed class CapabilityRowProbeApplication:Application
{
 private sealed record Case(string Id,bool English,string Key,bool Last,double[] Columns);
 public override void OnFrameworkInitializationCompleted()
 {
  var life=(IClassicDesktopStyleApplicationLifetime)ApplicationLifetime!;var args=life.Args!;if(args.Contains("--table-semantic-only")){CapabilityRowTableSemantics.Start(life,args);base.OnFrameworkInitializationCompleted();return;}var cases=JsonSerializer.Deserialize<Case[]>(File.ReadAllText(args[1]),new JsonSerializerOptions{PropertyNameCaseInsensitive=true})!;var output=args[2];Directory.CreateDirectory(output);
  var n=new WebCapabilityRow{ReducedMotion=true};var canvas=new Canvas{Width=1200,Height=800,Background=Brush.Parse("#2b2d31"),UseLayoutRounding=false};canvas.Children.Add(n);Canvas.SetLeft(n,30);Canvas.SetTop(n,60);var w=new Window{Title="Replica CapabilityRow verification",Width=1250,Height=850,Content=canvas,UseLayoutRounding=false};life.MainWindow=w;
  object Box(Control c){var p=c.TranslatePoint(default,n)??default;return new{x=p.X,y=p.Y,width=c.Bounds.Width,height=c.Bounds.Height};}
  if(args[0]=="--capabilityrow-window") {
   n.SetPresentation("access",false,false);n.ColumnWidths=cases.Single(c=>c.Id=="ko-904-2").Columns;n.ReducedMotion=false;var requests=0;var text=new TextBlock{Width=1100,TextWrapping=TextWrapping.Wrap};Canvas.SetLeft(text,30);Canvas.SetTop(text,600);canvas.Children.Add(text);
   void State(){w.UpdateLayout();var json=JsonSerializer.Serialize(new{requests,english=n.English,key=n.Key,focused=n.PermissionsButton.IsFocused,attached=canvas.Children.Contains(n),background=n.RowBackground?.ToString(),opacity=n.Opacity,button=n.PermissionsButton.Content,visible=n.PermissionsButton.IsVisible});text.Text=json;File.WriteAllText(Path.Combine(output,"capabilityrow-input.json"),json);}
   n.PointerEntered+=(_,_)=>DispatcherTimer.RunOnce(State,TimeSpan.FromMilliseconds(200));n.PointerExited+=(_,_)=>DispatcherTimer.RunOnce(State,TimeSpan.FromMilliseconds(200));
   n.PermissionsRequested+=(_,_)=>{requests++;Dispatcher.UIThread.Post(State,DispatcherPriority.Background);};
   void Button(string id,Action action,double x){var b=new Button{Content=id};AutomationProperties.SetAutomationId(b,"CapabilityRow-"+id);b.Click+=(_,_)=>{action();Dispatcher.UIThread.Post(State,DispatcherPriority.Background);};Canvas.SetLeft(b,x);canvas.Children.Add(b);}
   Button("language",()=>n.SetPresentation(n.Key,!n.English,false),400);Button("reduced",()=>n.ReducedMotion=!n.ReducedMotion,530);Button("mount",()=>{if(canvas.Children.Contains(n))canvas.Children.Remove(n);else canvas.Children.Add(n);},660);Button("data",()=>n.SetPresentation(n.Key=="access"?"data":"access",n.English,false),780);
   w.AddHandler(InputElement.GotFocusEvent,(_,_)=>Dispatcher.UIThread.Post(State),RoutingStrategies.Bubble,true);w.AddHandler(InputElement.KeyUpEvent,(_,_)=>Dispatcher.UIThread.Post(State),RoutingStrategies.Bubble,true);w.AddHandler(InputElement.PointerMovedEvent,(_,_)=>Dispatcher.UIThread.Post(State),RoutingStrategies.Bubble,true);w.Opened+=(_,_)=>Dispatcher.UIThread.Post(State);
  } else w.Opened+=(_,_)=>Dispatcher.UIThread.Post(async()=>{var rows=new List<object>();var peers=new List<object>();foreach(var c in cases){n.ColumnWidths=c.Columns;n.SetPresentation(c.Key,c.English,c.Last);await Dispatcher.UIThread.InvokeAsync(()=>{},DispatcherPriority.Background);w.UpdateLayout();var peer=ControlAutomationPeer.CreatePeerForElement(n)!;var cells=peer.GetChildren()!;if(peer.GetClassName()!="Row"||cells.Count!=4||cells.Any(p=>p.GetClassName()!="Cell")||(OperatingSystem.IsWindows()?cells.Where((_,i)=>i!=3).Any(p=>(p.GetChildren()?.Count??0)!=0)||(cells[3].GetChildren()?.Count??0)!=(n.Key=="access"?1:0):(cells[0].GetChildren()?.Count??0)!=2||(cells[3].GetChildren()?.Count??0)!=1||(cells[3].GetChildren()![0].GetChildren()?.Count??0)!=(n.Key=="access"?2:1)))throw new InvalidOperationException("Literal row/four-cell peer tree missing: "+c.Id+" "+JsonSerializer.Serialize(cells.Select(p=>new{role=p.GetClassName(),children=p.GetChildren()?.Select(x=>new{role=x.GetClassName(),type=x.GetAutomationControlType().ToString(),children=x.GetChildren()?.Count??0})}).ToArray()));peers.Add(new{id=c.Id,type=peer.GetAutomationControlType().ToString(),role=peer.GetClassName(),detailLineCount=n.DetailText.SystemLineTexts.Count,detailLines=n.DetailText.SystemLineTexts.ToArray(),cells=cells.Select(p=>new{role=p.GetClassName(),children=p.GetChildren()?.Count??0,contentChildren=p.GetChildren()?.Select(x=>x.GetChildren()?.Count??0).ToArray()}).ToArray()});rows.Add(new{id=c.Id,root=Box(n),description=Box(n.DescriptionText),connected=Box(n.ConnectedPill),capability=Box(n.CapabilityPill),details=Box(n.Details),detailText=Box(n.DetailSpan),button=n.PermissionsButton.IsVisible?Box(n.PermissionsButton):null,columns=n.ColumnWidths,texts=new string?[]{n.TitleText.Text,n.DescriptionText.Text,n.ConnectedPill.Text,n.CapabilityPill.Text,n.DetailText.Text,n.PermissionsButton.IsVisible?n.PermissionsButton.Content as string:null}});if(c.Id is "ko-478-2" or "ko-656-2" or "en-904-2"){using var b=new RenderTargetBitmap(new PixelSize(2400,1600),new Vector(192,192));b.Render(canvas);b.Save(Path.Combine(output,"capabilityrow-"+c.Id+".png"),new PngBitmapEncoderOptions());}}File.WriteAllText(Path.Combine(output,"capabilityrow-native.json"),JsonSerializer.Serialize(new{dpr=w.RenderScaling,rows,peers},new JsonSerializerOptions{WriteIndented=true}));CapabilityRowTimingProbe.Run(Path.Combine(output,"capabilityrow-timing.json"));if(ReplicaPlatform.IsHeadless)await CapabilityRowHeadlessInput.Run(w,canvas,n,cases.Single(c=>c.Id=="ko-904-2").Columns,output);w.Close();Console.WriteLine("Recorded "+rows.Count+" Capability rows");},DispatcherPriority.Background);
  base.OnFrameworkInitializationCompleted();
 }
}
