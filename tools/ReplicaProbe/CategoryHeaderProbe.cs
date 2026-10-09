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
internal static class CategoryHeaderProbe
{
 internal sealed record Case(string Id,string Lang,double Width,string Title,string Description,string Icon,string StatusText,string StatusKind,string SelectionLabel,bool Selected,bool Expanded);
 internal static void Run(string input,string output,bool interactive)
 {
  Directory.CreateDirectory(output);var cases=JsonSerializer.Deserialize<Case[]>(File.ReadAllText(input),new JsonSerializerOptions{PropertyNameCaseInsensitive=true})!;
  var n=new WebCategoryHeader{ReducedMotion=!interactive,HorizontalAlignment=Avalonia.Layout.HorizontalAlignment.Left,VerticalAlignment=Avalonia.Layout.VerticalAlignment.Top};
  var panel=new Canvas{Width=1120,Height=300,Background=Brush.Parse("#313338"),UseLayoutRounding=false};var parent=WebSurface.Create(Brush.Parse("#2b2d31"),Brush.Parse("#41434a"),new(1),new(10));parent.Child=n;parent.UseLayoutRounding=false;panel.Children.Add(parent);Canvas.SetLeft(parent,30);Canvas.SetTop(parent,30);
  var window=new Window{Title="Replica CategoryHeader verification",Width=1120,Height=350,Content=panel,UseLayoutRounding=false};((IClassicDesktopStyleApplicationLifetime)Application.Current!.ApplicationLifetime!).MainWindow=window;
  void Apply(Case c){n.Title=c.Title;n.Description=c.Description;n.IconName=c.Icon;n.StatusText=c.StatusText;n.StatusKind=c.StatusKind;n.SelectionLabel=c.SelectionLabel;n.IsSelected=c.Selected;n.IsExpanded=c.Expanded;n.Width=c.Width;parent.Width=c.Width+2;parent.Height=double.NaN;}
  object Read(Case c)
  {
   var children=n.ToggleButton.GetVisualDescendants().ToArray();var title=children.OfType<WebSystemTextBlock>().Single(t=>t.FontSize==14);var desc=children.OfType<WebSystemTextBlock>().Single(t=>t.FontSize==11);var icon=children.OfType<SvgIcon>().Single(s=>s.IconName==c.Icon);var arrow=children.OfType<SvgIcon>().Single(s=>s.IconName=="chevron");var box=(Border)icon.Parent!;var pill=children.OfType<WebStatusPill>().Single();
   object Box(Control e){var ps=new[]{default(Point),new Point(e.Bounds.Width,0),new Point(0,e.Bounds.Height),new Point(e.Bounds.Width,e.Bounds.Height)}.Select(p=>e.TranslatePoint(p,n)??default).ToArray();return new{x=ps.Min(p=>p.X),y=ps.Min(p=>p.Y),width=ps.Max(p=>p.X)-ps.Min(p=>p.X),height=ps.Max(p=>p.Y)-ps.Min(p=>p.Y)};}
   string Color(IBrush? b)=>b is ISolidColorBrush s?s.Color.ToString():"#00000000";
   return new{id=c.Id,rect=new{width=n.Bounds.Width,height=n.Bounds.Height},button=Box(n.ToggleButton),choice=Box(n.Choice),checkbox=Box(n.Checkbox),icon=Box(box),svg=Box(icon),title=Box(title),description=Box(desc),pill=Box(pill),chevron=Box(arrow),name=AutomationProperties.GetName(n.ToggleButton),selectionName=n.SelectionLabel,selected=n.IsSelected,expanded=n.IsExpanded,style=new{bg=Color(n.Head.Background),padding=n.ToggleButton.Padding.ToString(),minHeight=n.ToggleButton.MinHeight,gap=12,iconBg=Color(box.Background),iconRadius=box.CornerRadius.TopLeft,titleSize=title.FontSize,titleWeight=(int)title.FontWeight,titleHeight=title.LineHeight,titleColor=Color(title.Foreground),descriptionSize=desc.FontSize,descriptionHeight=desc.LineHeight,descriptionColor=Color(desc.Foreground),chevronAngle=(arrow.RenderTransform as RotateTransform)?.Angle}};
  }
  int changes=0,expands=0;void Live(){window.UpdateLayout();var c=cases.First(c=>c.Lang=="ko"&&c.Width==858&&!c.Selected&&!c.Expanded);File.WriteAllText(Path.Combine(output,"categoryheader-input.json"),JsonSerializer.Serialize(new{selected=n.IsSelected,expanded=n.IsExpanded,changes,expands,checkboxFocus=n.Checkbox.IsFocused,checkboxFocusVisible=n.Checkbox.FocusVisible,buttonFocus=n.ToggleButton.IsFocused,buttonPressed=n.ToggleButton.IsPressed,buttonHover=n.ToggleButton.IsPointerOver,buttonScale=n.ToggleButton.RenderTransform?.Value.M11,headBackground=(n.Head.Background as ISolidColorBrush)?.Color.ToString(),reduced=n.ReducedMotion,clickActive=typeof(WebCategoryHeader.CategoryButton).GetField("_click",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic)!.GetValue(n.ToggleButton) is WebMotionPlayback playback&&!playback.Completion.IsCompleted,choiceBackground=(n.Choice.Background as ISolidColorBrush)?.Color.ToString()},new JsonSerializerOptions{WriteIndented=true}));}
  n.SelectionChanged+=(_,_)=>{changes++;Dispatcher.UIThread.Post(Live,DispatcherPriority.Background);};n.ExpandedChanged+=(_,_)=>{expands++;Dispatcher.UIThread.Post(Live,DispatcherPriority.Background);if(interactive){var timer=new DispatcherTimer{Interval=TimeSpan.FromMilliseconds(40)};var tick=0;timer.Tick+=(_,_)=>{Live();File.Copy(Path.Combine(output,"categoryheader-input.json"),Path.Combine(output,"categoryheader-click-"+expands+"-"+tick+".json"),true);if(++tick>=7)timer.Stop();};timer.Start();}};n.Checkbox.GotFocus+=(_,_)=>Dispatcher.UIThread.Post(Live,DispatcherPriority.Background);n.Checkbox.LostFocus+=(_,_)=>Dispatcher.UIThread.Post(Live,DispatcherPriority.Background);n.ToggleButton.GotFocus+=(_,_)=>Dispatcher.UIThread.Post(Live,DispatcherPriority.Background);
  n.Checkbox.PropertyChanged+=(_,e)=>{if(e.Property==WebCheckbox.FocusVisibleProperty)Dispatcher.UIThread.Post(Live,DispatcherPriority.Background);};
  window.KeyDown+=(_,e)=>{if(e.Key==Avalonia.Input.Key.M){n.ReducedMotion=!n.ReducedMotion;e.Handled=true;}Dispatcher.UIThread.Post(Live,DispatcherPriority.Background);};
  window.AddHandler(InputElement.PointerPressedEvent,(_,_)=>{Dispatcher.UIThread.Post(Live,DispatcherPriority.Background);var timer=new DispatcherTimer{Interval=TimeSpan.FromMilliseconds(40)};var ticks=0;timer.Tick+=(_,_)=>{Live();if(++ticks>=6)timer.Stop();};timer.Start();},RoutingStrategies.Tunnel,true);window.AddHandler(InputElement.PointerReleasedEvent,(_,_)=>Dispatcher.UIThread.Post(Live,DispatcherPriority.Background),RoutingStrategies.Tunnel,true);
  window.KeyUp+=(_,_)=>Dispatcher.UIThread.Post(Live,DispatcherPriority.Background);
  AutomationProperties.SetAutomationId(n.ToggleButton,"CategoryToggle");AutomationProperties.SetAutomationId(n.Checkbox,"CategoryCheck");AutomationProperties.SetAutomationId(n.Choice,"CategoryChoice");
  Apply(cases[0]);
  window.Opened+=(_,_)=>Dispatcher.UIThread.Post(()=>{
   if(interactive){Apply(cases.First(c=>c.Lang=="ko"&&c.Width==858&&!c.Selected&&!c.Expanded));Live();return;}
   var rows=new List<object>();foreach(var(c,i)in cases.Select((c,i)=>(c,i))){Apply(c);window.UpdateLayout();rows.Add(Read(c));if(i is 0 or 24 or 48 or 96 or 120 or 144){using var image=new RenderTargetBitmap(new PixelSize(2240,600),new Vector(192,192));image.Render(panel);image.Save(Path.Combine(output,"categoryheader-"+c.Id+".png"),new PngBitmapEncoderOptions());}}
   File.WriteAllText(Path.Combine(output,"categoryheader-native.json"),JsonSerializer.Serialize(new{scale=window.RenderScaling,rows},new JsonSerializerOptions{WriteIndented=true}));Console.WriteLine($"Recorded {rows.Count} CategoryHeader state replacements.");window.Close();
  },DispatcherPriority.Background);
 }
}
internal sealed class CategoryHeaderProbeApplication:Application
{
 public override void Initialize()=>Styles.Add(new Avalonia.Themes.Fluent.FluentTheme());
 public override void OnFrameworkInitializationCompleted(){var a=((IClassicDesktopStyleApplicationLifetime)ApplicationLifetime!).Args!;CategoryHeaderProbe.Run(a[1],a[2],a[0]=="--categoryheader-window");base.OnFrameworkInitializationCompleted();}
}
