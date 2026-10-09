using System.Globalization;
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

internal sealed class InfoDialogProbeApplication:Application
{
    internal sealed record Case(string Id,string Lang,double Width,double Height,string Key,string Title);
    public override void OnFrameworkInitializationCompleted()
    {
        var life=(IClassicDesktopStyleApplicationLifetime)ApplicationLifetime!;var args=life.Args!;var cases=JsonSerializer.Deserialize<Case[]>(File.ReadAllText(args[1]),new JsonSerializerOptions{PropertyNameCaseInsensitive=true})!;var output=args[2];Directory.CreateDirectory(output);
        var n=new WebInfoDialog{Width=1280,Height=720,ReducedMotion=true};var canvas=new Canvas{Width=1280,Height=960,UseLayoutRounding=false,Background=Brush.Parse("#313338")};canvas.Children.Add(n);var w=new Window{Title="Replica InfoDialog verification",Width=1280,Height=1000,UseLayoutRounding=false,Content=canvas};life.MainWindow=w;
        object Box(Control e){var p=e.TranslatePoint(default,n.DialogBox)??default;return new{x=p.X,y=p.Y,width=e.Bounds.Width,height=e.Bounds.Height};}
        string Rgb(IBrush? brush){var c=((ISolidColorBrush)brush!).Color;return $"rgb({c.R}, {c.G}, {c.B})";}
        string Shadow(){var s=n.DialogBox.BoxShadow[0];return $"rgba({s.Color.R}, {s.Color.G}, {s.Color.B}, {Math.Round(s.Color.A/255d,2).ToString(CultureInfo.InvariantCulture)}) {s.OffsetX}px {s.OffsetY}px {s.Blur}px {s.Spread}px";}
        void Present(Case c,Control? opener=null,Control? background=null,NavigationMethod method=NavigationMethod.Unspecified){n.Width=c.Width;n.Height=c.Height;n.Open(c.Key=="permissions",c.Lang=="en",c.Width<=1100,opener,background,method);}
        string BodyText()=>string.Concat(n.Body.Children.Select(child=>child switch {
            WebSystemTextBlock text=>text.Text,
            WebNotice notice=>notice.Title+notice.Text,
            WebSummaryList list=>string.Concat(list.Rows.Select(row=>row.LabelText.Text+string.Concat(row.ValueLines.Select(line=>line.Text)))),
            _=>""}));
        object Read(Case c){var p=n.DialogBox.TranslatePoint(default,n)??default;var content=n.Body.Children.First();return new{id=c.Id,x=p.X,y=p.Y,width=n.DialogBox.Bounds.Width,height=n.DialogBox.Bounds.Height,parts=new{head=Box(n.HeaderRegion),title=Box(n.TitleText),close=Box(n.CloseButton),body=Box(n.BodyViewport),content=Box(content),footer=Box(n.FooterRegion),footerClose=Box(n.FooterCloseButton)},texts=new{title=n.TitleText.Text,close=AutomationProperties.GetName(n.CloseButton),footerClose=n.FooterCloseButton.Content},bodyScrollHeight=n.BodyViewport.ScrollHeight,bodyScrollTop=n.BodyViewport.Offset,dialogScrollHeight=n.OuterViewport.ScrollHeight,dialogScrollTop=n.OuterViewport.Offset,shadow=Shadow(),palette=new{background=Rgb(n.DialogBox.Background),border=Rgb(n.DialogBox.BorderBrush),radius=n.DialogBox.CornerRadius.TopLeft.ToString(CultureInfo.InvariantCulture)+"px",footer=Rgb(((WebSurfaceBox)n.FooterRegion).Background)},bodyText=BodyText()};}
        if(args[0]=="--infodialog-window")
        {
            n.ReducedMotion=false;var current=cases.First(c=>c.Id=="ko-1280-720-research");var background=new Canvas{Width=1280,Height=720,UseLayoutRounding=false,Background=Brush.Parse("#313338")};canvas.Children.Clear();canvas.Children.Add(background);var opener=new WebActionButton{Content="Open details",Variant="primary"};AutomationProperties.SetAutomationId(opener,"InfoDialog-open");background.Children.Add(opener);Canvas.SetLeft(opener,30);Canvas.SetTop(opener,30);canvas.Children.Add(n);var events=new List<string>();
            void Record(){w.UpdateLayout();var focused=w.FocusManager?.GetFocusedElement() as Control;var data=new{capturedAtUtc=DateTime.UtcNow,dpr=w.RenderScaling,visible=n.IsVisible,closing=n.IsClosing,reduced=n.ReducedMotion,backgroundEnabled=background.IsEnabled,backgroundBlur=(background.Effect as BlurEffect)?.Radius,focus=focused is null?null:AutomationProperties.GetAutomationId(focused),focusVisible=focused is WebActionButton button?(bool?)typeof(WebActionButton).GetField("_focusVisible",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic)!.GetValue(button):null,opacity=n.DialogBox.Opacity,events,row=n.IsVisible?Read(current):null};var f=Path.Combine(output,"infodialog-input.json");File.WriteAllText(f+".next",JsonSerializer.Serialize(data,new JsonSerializerOptions{WriteIndented=true}));File.Move(f+".next",f,true);}
            var openMethod=NavigationMethod.Unspecified;opener.AddHandler(InputElement.PointerPressedEvent,(_,_)=>openMethod=NavigationMethod.Pointer,RoutingStrategies.Tunnel,true);opener.AddHandler(InputElement.KeyDownEvent,(_,_)=>openMethod=NavigationMethod.Tab,RoutingStrategies.Tunnel,true);
            opener.Click+=(_,_)=>{Present(current,opener,background,openMethod);Record();};n.Closed+=(_,_)=>{events.Add("close");Record();};
            var record=new WebActionButton{Content="Record",Variant="quiet",Focusable=false,IsTabStop=false};AutomationProperties.SetAutomationId(record,"InfoDialog-record");record.Click+=(_,_)=>Record();canvas.Children.Add(record);Canvas.SetLeft(record,30);Canvas.SetTop(record,760);
            var reduced=new WebActionButton{Content="Reduced motion",Variant="quiet"};AutomationProperties.SetAutomationId(reduced,"InfoDialog-reduced");reduced.Click+=(_,_)=>{n.ReducedMotion=true;Record();};canvas.Children.Add(reduced);Canvas.SetLeft(reduced,180);Canvas.SetTop(reduced,760);
            var trial=new WebActionButton{Content="Research / small",Variant="quiet"};AutomationProperties.SetAutomationId(trial,"InfoDialog-small");trial.Click+=(_,_)=>{current=cases.First(c=>c.Id=="en-1280-400-research");Present(current,opener,background);Record();};canvas.Children.Add(trial);Canvas.SetLeft(trial,380);Canvas.SetTop(trial,760);
            var close=new WebActionButton{Content="Close fixture",Variant="quiet"};AutomationProperties.SetAutomationId(close,"InfoDialog-fixture-close");close.Click+=(_,_)=>w.Close();canvas.Children.Add(close);Canvas.SetLeft(close,580);Canvas.SetTop(close,760);w.Opened+=(_,_)=>Record();base.OnFrameworkInitializationCompleted();return;
        }
        w.Opened+=(_,_)=>Dispatcher.UIThread.Post(async()=>
        {
            var rows=new List<object>();foreach(var c in cases){Present(c);await Dispatcher.UIThread.InvokeAsync(()=>{},DispatcherPriority.Background);n.InvalidateMeasure();n.Measure(new(c.Width,c.Height));n.Arrange(new(0,0,c.Width,c.Height));w.UpdateLayout();rows.Add(Read(c));}
            var lifecycle=new List<object>();var closes=0;n.Closed+=(_,_)=>closes++;n.ReducedMotion=false;Present(cases.First());w.UpdateLayout();lifecycle.Add(new{state="open-start",visible=n.IsVisible,opacity=n.DialogBox.Opacity});await Task.Delay(340);lifecycle.Add(new{state="open-end",visible=n.IsVisible,opacity=n.DialogBox.Opacity});var closing=n.CloseAsync();_ = n.CloseAsync();lifecycle.Add(new{state="close-start",visible=n.IsVisible,closing=n.IsClosing});await closing;lifecycle.Add(new{state="close-end",visible=n.IsVisible,closes});n.ReducedMotion=true;Present(cases.First());w.UpdateLayout();lifecycle.Add(new{state="reduced-open",visible=n.IsVisible,opacity=n.DialogBox.Opacity});await n.CloseAsync();lifecycle.Add(new{state="reduced-close",visible=n.IsVisible,closes});await InfoDialogHeadlessInput.Run(w,canvas,n,output);File.WriteAllText(Path.Combine(output,"infodialog-lifecycle.json"),JsonSerializer.Serialize(lifecycle,new JsonSerializerOptions{WriteIndented=true}));
            File.WriteAllText(Path.Combine(output,"infodialog-native.json"),JsonSerializer.Serialize(new{dpr=w.RenderScaling,rows},new JsonSerializerOptions{WriteIndented=true}));Present(cases.First(c=>c.Id=="ko-1280-720-permissions"));w.UpdateLayout();using var bmp=new RenderTargetBitmap(new PixelSize(2560,1920),new Vector(192,192));bmp.Render(canvas);bmp.Save(Path.Combine(output,"infodialog.png"),new PngBitmapEncoderOptions());w.Close();Console.WriteLine("Recorded "+rows.Count+" InfoDialog modal states");
        },DispatcherPriority.Background);base.OnFrameworkInitializationCompleted();
    }
}
