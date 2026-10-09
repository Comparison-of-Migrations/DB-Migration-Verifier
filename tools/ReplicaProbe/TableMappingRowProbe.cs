using System.Reflection;
using System.Text.Json;
using Avalonia;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Media;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Verifier.Desktop;
internal static class TableMappingRowProbe
{
 internal sealed record Case(string Id,string Lang,string Source,string Target,bool Selected,bool Manual,string Method,string Button,bool Hover,bool Last,double Width);
 internal static void Run(string input,string source,string output,bool interactive)
 {
  Directory.CreateDirectory(output);var opts=new JsonSerializerOptions{PropertyNameCaseInsensitive=true};var cases=JsonSerializer.Deserialize<Case[]>(File.ReadAllText(input),opts)!;using var src=JsonDocument.Parse(File.ReadAllText(source));var widths=src.RootElement.GetProperty("rows").EnumerateArray().ToDictionary(x=>x.GetProperty("id").GetString()!,x=>x.GetProperty("columns").EnumerateArray().Select(v=>v.GetDouble()).ToArray());
  var n=new WebTableMappingRow{ReducedMotion=!interactive,HorizontalAlignment=Avalonia.Layout.HorizontalAlignment.Left,VerticalAlignment=Avalonia.Layout.VerticalAlignment.Top};var canvas=new Canvas{Width=1160,Height=250,Background=Brush.Parse("#2b2d31"),UseLayoutRounding=false};canvas.Children.Add(n);Canvas.SetLeft(n,30);Canvas.SetTop(n,30);var w=new Window{Title="Replica TableMappingRow verification",Width=1160,Height=300,Content=canvas,UseLayoutRounding=false};((IClassicDesktopStyleApplicationLifetime)Application.Current!.ApplicationLifetime!).MainWindow=w;
  void Apply(Case c){n.SourceName=c.Source;n.TargetName=c.Target;n.Selected=c.Selected;n.Manual=c.Manual;n.MethodText.Text=c.Method;n.ChangeButton.Content=c.Button;n.ColumnWidths=widths[c.Id];n.TopBorderShare=.5;n.BottomBorderShare=c.Last?0:.5;}
  void Endpoint(Case c){var f=BindingFlags.Instance|BindingFlags.NonPublic;var color=(Color)typeof(WebTableMappingRow).GetMethod("Resolve",f)!.Invoke(n,new object[]{c.Hover})!;var s=(WebSurfaceBox)typeof(WebTableMappingRow).GetField("_surface",f)!.GetValue(n)!;s.Background=new SolidColorBrush(color);}
  object Read(Case c){object Box(Control e){var p=e.TranslatePoint(default,n)??default;return new{x=p.X,y=p.Y,width=e.Bounds.Width,height=e.Bounds.Height};}var desc=n.ChangeButton.GetVisualDescendants();return new{id=c.Id,width=n.Bounds.Width,height=n.Bounds.Height,columns=n.ColumnWidths,choice=Box(n.Choice),checkbox=Box(n.Checkbox),source=Box(n.SourceText),targetBox=Box(n.TargetText),method=Box(n.Method),methodIcon=Box(n.MethodIcon),methodText=Box(n.MethodText),button=Box(n.ChangeButton),buttonText=Box(desc.OfType<WebSystemTextBlock>().Single()),buttonIcon=Box(desc.OfType<SvgIcon>().Single()),selected=n.Selected,manual=n.Manual,selectionName=AutomationProperties.GetName(n.Checkbox),buttonName=AutomationProperties.GetName(n.ChangeButton),style=new{background=(n.RowBackground as ISolidColorBrush)?.Color.ToString(),methodColor=(n.MethodText.Foreground as ISolidColorBrush)?.Color.ToString(),choiceCursor=n.Choice.Cursor?.ToString()}};}
  int selected=0,changes=0;void Live(){w.UpdateLayout();File.WriteAllText(Path.Combine(output,"tablemappingrow-input.json"),JsonSerializer.Serialize(new{selected=n.Selected,target=n.TargetName,manual=n.Manual,selections=selected,changes,checkboxFocus=n.Checkbox.IsFocused,checkboxFocusVisible=n.Checkbox.FocusVisible,buttonFocus=n.ChangeButton.IsFocused,reduced=n.ReducedMotion,hover=n.PointerWithinRow,background=(n.RowBackground as ISolidColorBrush)?.Color.ToString()},new JsonSerializerOptions{WriteIndented=true}));}
  void Sample(){Dispatcher.UIThread.Post(Live,DispatcherPriority.Background);if(interactive){var timer=new DispatcherTimer{Interval=TimeSpan.FromMilliseconds(40)};var tick=0;timer.Tick+=(_,_)=>{Live();if(++tick>=8)timer.Stop();};timer.Start();}}
  n.PointerEntered+=(_,_)=>Sample();n.PointerExited+=(_,_)=>Sample();
  w.AddHandler(InputElement.PointerPressedEvent,(_,_)=>Sample(),RoutingStrategies.Tunnel,true);w.AddHandler(InputElement.PointerReleasedEvent,(_,_)=>Sample(),RoutingStrategies.Tunnel,true);w.AddHandler(InputElement.KeyDownEvent,(_,e)=>{if(e.Key==Key.M){n.ReducedMotion=!n.ReducedMotion;e.Handled=true;}Sample();},RoutingStrategies.Tunnel,true);
  n.SelectionChanged+=(_,_)=>{selected++;Dispatcher.UIThread.Post(Live,DispatcherPriority.Background);};n.ChangeRequested+=(_,_)=>{changes++;Dispatcher.UIThread.Post(Live,DispatcherPriority.Background);};w.AddHandler(InputElement.KeyUpEvent,(_,_)=>Sample(),RoutingStrategies.Tunnel,true);n.Checkbox.GotFocus+=(_,_)=>Dispatcher.UIThread.Post(Live,DispatcherPriority.Background);n.ChangeButton.GotFocus+=(_,_)=>Dispatcher.UIThread.Post(Live,DispatcherPriority.Background);
  AutomationProperties.SetAutomationId(n.Checkbox,"TableRowCheck");AutomationProperties.SetAutomationId(n.ChangeButton,"TableRowChange");AutomationProperties.SetAutomationId(n.Choice,"TableRowChoice");Apply(cases[0]);
  w.Opened+=(_,_)=>Dispatcher.UIThread.Post(()=>{if(interactive){Apply(cases.First(c=>c.Lang=="ko"&&c.Width==858&&!c.Manual&&!c.Last&&!c.Selected));Sample();return;}var rows=new List<object>();foreach(var(c,i)in cases.Select((c,i)=>(c,i))){Apply(c);w.UpdateLayout();Endpoint(c);rows.Add(Read(c));if(i is 0 or 4 or 24 or 28){using var b=new RenderTargetBitmap(new PixelSize(2320,500),new Vector(192,192));b.Render(canvas);b.Save(Path.Combine(output,"tablemappingrow-"+c.Id+".png"),new PngBitmapEncoderOptions());}}File.WriteAllText(Path.Combine(output,"tablemappingrow-native.json"),JsonSerializer.Serialize(new{scale=w.RenderScaling,rows},new JsonSerializerOptions{WriteIndented=true}));w.Close();Console.WriteLine($"Recorded {rows.Count} atomic table rows using original parent column widths.");},DispatcherPriority.Background);
 }
}
internal sealed class TableMappingRowProbeApplication:Application
{
 public override void Initialize()=>Styles.Add(new Avalonia.Themes.Fluent.FluentTheme());
 public override void OnFrameworkInitializationCompleted(){var a=((IClassicDesktopStyleApplicationLifetime)ApplicationLifetime!).Args!;TableMappingRowProbe.Run(a[1],a[2],a[3],a[0]=="--tablerow-window");base.OnFrameworkInitializationCompleted();}
}
