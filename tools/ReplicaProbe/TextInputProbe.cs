using System.Text.Json;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Automation;
using Avalonia.Input;
using Avalonia.Input.TextInput;
using Avalonia.Controls.Presenters;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.VisualTree;
using Avalonia.Threading;
using Verifier.Desktop;

internal static class TextInputProbe
{
    internal sealed record Case(string Id,string Text,string Placeholder,bool Search,bool Disabled,double Width);
    internal static void Run(string input,string output)
    {
        Application.Current!.Styles.Add(new Avalonia.Themes.Fluent.FluentTheme());
        Directory.CreateDirectory(output);
        var cases=JsonSerializer.Deserialize<Case[]>(File.ReadAllText(input),new JsonSerializerOptions{PropertyNameCaseInsensitive=true})!;
        var canvas=new Canvas{Width=460,Height=cases.Length*68+20,Background=Brush.Parse("#313338"),UseLayoutRounding=false};
        var controls=new List<(Case Case,WebTextInput Input)>();
        foreach(var (c,i) in cases.Select((c,i)=>(c,i)))
        {
            var field=new WebTextInput{Width=c.Width,Text=c.Text,PlaceholderText=c.Placeholder,SearchMode=c.Search,IsEnabled=!c.Disabled};
            field.ApplyTemplate();
            Canvas.SetLeft(field,30);Canvas.SetTop(field,20+i*68);canvas.Children.Add(field);controls.Add((c,field));
        }
        var host=new Window{Title="Replica TextInput render verification",Width=460,Height=canvas.Height,Content=canvas,ShowInTaskbar=false};
        host.Show();host.UpdateLayout();
        canvas.Measure(new(460,canvas.Height));canvas.Arrange(new(0,0,460,canvas.Height));
        object RectRecord(Rect r)=>new{x=r.X,y=r.Y,width=r.Width,height=r.Height};
        var rows=controls.Select(x=>{var p=x.Input.GetVisualDescendants().OfType<TextPresenter>().Single();var c=x.Input.GetVisualDescendants().Single(v=>v.GetType().Name=="WebInputCaret");var caret=(Rect)c.GetType().GetProperty("CaretRect")!.GetValue(c)!;return new {id=x.Case.Id,family=p.FontFamily.Name,fontSize=p.FontSize,fontWeight=(int)p.FontWeight,box=RectRecord(x.Input.Bounds),presenter=RectRecord(p.Bounds),textWidth=p.TextLayout.WidthIncludingTrailingWhitespace,baseline=p.TextLayout.TextLines.FirstOrDefault()?.Baseline,presenterHeight=p.TextLayout.Height,caret=RectRecord(caret),blinkInterval=x.Input.CaretBlinkInterval.TotalMilliseconds,placeholder=RectRecord(x.Input.GetVisualDescendants().OfType<WebSystemTextBlock>().Single().Bounds)};});
        foreach(var scale in new[]{1,2})
        {using var b=new RenderTargetBitmap(new PixelSize(460*scale,(int)canvas.Height*scale),new Vector(96*scale,96*scale));b.Render(canvas);b.Save(Path.Combine(output,$"textinput-native-{scale}x.png"),new PngBitmapEncoderOptions());}
        File.WriteAllText(Path.Combine(output,"textinput-native.json"),JsonSerializer.Serialize(rows,new JsonSerializerOptions{WriteIndented=true}));
        Console.WriteLine($"Rendered {cases.Length} text input candidates");
        host.Close();
    }
}
internal sealed class TextInputProbeApplication:Application
{
    public override void Initialize(){Styles.Add(new Avalonia.Themes.Fluent.FluentTheme());}
    public override void OnFrameworkInitializationCompleted()
    {
        var desktop=(IClassicDesktopStyleApplicationLifetime)ApplicationLifetime!;var output=desktop.Args![1];Directory.CreateDirectory(output);
        var text=new WebTextInput{Width=366,Text="source.example.invalid"};
        var search=new WebTextInput{Width=380,SearchMode=true,PlaceholderText="Search tables"};
        var disabled=new WebTextInput{Width=366,IsEnabled=false,PlaceholderText="Do not enter a real password in this preview",PasswordChar='●'};
        var fields=new[]{text,search,disabled};var canvas=new Canvas{Background=Brush.Parse("#313338")};
        for(var i=0;i<fields.Length;i++){Canvas.SetLeft(fields[i],32);Canvas.SetTop(fields[i],32+i*80);canvas.Children.Add(fields[i]);AutomationProperties.SetName(fields[i],new[]{"Sample server","Sample search","Disabled preview password"}[i]);AutomationProperties.SetAutomationId(fields[i],"ReplicaTextInput"+i);}
        var w=new Window{Title="Replica TextInput verification",Width=500,Height=330,Content=canvas};desktop.MainWindow=w;
        var keys=new List<object>();var changes=new List<object>();
        object? Geometry(WebTextInput f)
        {
            var p=f.GetVisualDescendants().OfType<TextPresenter>().SingleOrDefault();var scroll=f.GetVisualDescendants().OfType<ScrollViewer>().SingleOrDefault();
            var surface=f.GetVisualDescendants().OfType<WebSurfaceBox>().SingleOrDefault();
            var caret=f.GetVisualDescendants().SingleOrDefault(v=>v.GetType().Name=="WebInputCaret");
            var rect=caret?.GetType().GetProperty("CaretRect")?.GetValue(caret) is Rect r?r:default;
            if(p is null || scroll is null || surface is null)return null;
            return new{width=f.Bounds.Width,textWidth=p.TextLayout.WidthIncludingTrailingWhitespace,preedit=p.PreeditText,showSelection=p.ShowSelectionHighlight,caretRect=new{x=rect.X,y=rect.Y,width=rect.Width,height=rect.Height},blinkInterval=p.CaretBlinkInterval.TotalMilliseconds,offset=scroll.Offset.X,viewport=scroll.Viewport.Width,extent=scroll.Extent.Width,border=(surface.BorderBrush as ISolidColorBrush)?.Color.ToString(),selectionBrush=(f.SelectionBrush as ISolidColorBrush)?.Color.ToString(),reduced=f.ReducedMotion};
        }
        void Record(string file="textinput-input.json"){File.WriteAllText(Path.Combine(output,file),JsonSerializer.Serialize(new{utc=DateTime.UtcNow,keys,changes,selectAll=PlatformSettings!.HotkeyConfiguration.SelectAll.Select(g=>g.ToString()),rows=fields.Select((f,i)=>new{id=i,text=i==2?null:f.Text,enabled=f.IsEnabled,focused=f.IsFocused,selectionStart=f.SelectionStart,selectionEnd=f.SelectionEnd,caret=f.CaretIndex,geometry=Geometry(f),editorAttached=typeof(TextBox).GetField("_presenter",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic)!.GetValue(f) is not null})},new JsonSerializerOptions{WriteIndented=true}));}
        void Key(object? _,KeyEventArgs e){keys.Add(new{key=e.Key.ToString(),modifiers=e.KeyModifiers.ToString(),eventType=e.RoutedEvent!.Name,source=e.Source?.GetType().Name,handled=e.Handled});Dispatcher.UIThread.Post(()=>
            {
                w.UpdateLayout();
                if(e.RoutedEvent==InputElement.KeyUpEvent && e.Key==Avalonia.Input.Key.Tab)
                {
                    using var b=new RenderTargetBitmap(new PixelSize(1000,660),new Vector(192,192));
                    b.Render(canvas);b.Save(Path.Combine(output,"textinput-keyup-Tab-"+e.KeyModifiers+".png"),new PngBitmapEncoderOptions());
                    Record("textinput-keyup-Tab-"+e.KeyModifiers+".json");
                }
                Record();
            },DispatcherPriority.Background);}
        w.AddHandler(InputElement.KeyDownEvent,Key,Avalonia.Interactivity.RoutingStrategies.Bubble,true);
        w.AddHandler(InputElement.KeyUpEvent,Key,Avalonia.Interactivity.RoutingStrategies.Bubble,true);
        w.KeyDown+=(_,e)=>
        {
            if(e.Key==Avalonia.Input.Key.F12)
            {
                var caret=text.GetVisualDescendants().Single(v=>v.GetType().Name=="WebInputCaret");
                var timer=(DispatcherTimer)caret.GetType().GetField("_timer",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic)!.GetValue(caret)!;
                var visible=caret.GetType().GetField("_visible",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic)!;
                var samples=new List<object>();var watch=System.Diagnostics.Stopwatch.StartNew();
                EventHandler? observe=null;observe=(_,_)=>
                {
                    samples.Add(new{elapsed=watch.Elapsed.TotalMilliseconds,visible=(bool)visible.GetValue(caret)!,focused=text.IsFocused});
                    if(samples.Count==6)
                    {
                        timer.Tick-=observe;
                        File.WriteAllText(Path.Combine(output,"textinput-blink.json"),JsonSerializer.Serialize(new{utc=DateTime.UtcNow,interval=text.CaretBlinkInterval.TotalMilliseconds,samples},new JsonSerializerOptions{WriteIndented=true}));
                    }
                };
                timer.Tick+=observe;e.Handled=true;
            }
            if(e.Key==Avalonia.Input.Key.F6){text.Width=238;e.Handled=true;}
            if(e.Key==Avalonia.Input.Key.F7){text.Width=366;e.Handled=true;}
            if(e.Key==Avalonia.Input.Key.F8){foreach(var f in fields)f.ReducedMotion=!f.ReducedMotion;e.Handled=true;}
            if(e.Key is Avalonia.Input.Key.F9 or Avalonia.Input.Key.F10 or Avalonia.Input.Key.F11)
            {
                var request=new TextInputMethodClientRequestedEventArgs{RoutedEvent=InputElement.TextInputMethodClientRequestedEvent};
                text.RaiseEvent(request);
                if(request.Client is null)throw new InvalidOperationException("Owned native IME client was not attached.");
                if(e.Key==Avalonia.Input.Key.F11)
                {request.Client.SetPreeditText(null);text.RaiseEvent(new TextInputEventArgs{RoutedEvent=InputElement.TextInputEvent,Text="가"});}
                else request.Client.SetPreeditText(e.Key==Avalonia.Input.Key.F9?"ㄱ":"가",1);
                e.Handled=true;
            }
            Dispatcher.UIThread.Post(()=>
            {
                w.UpdateLayout();Record();
                if(e.Key is Avalonia.Input.Key.F6 or Avalonia.Input.Key.F7 or Avalonia.Input.Key.F8 or Avalonia.Input.Key.F9 or Avalonia.Input.Key.F10 or Avalonia.Input.Key.F11)
                {
                    using var b=new RenderTargetBitmap(new PixelSize(1000,660),new Vector(192,192));
                    b.Render(canvas);b.Save(Path.Combine(output,"textinput-state-"+e.Key+".png"),new PngBitmapEncoderOptions());
                    Record("textinput-state-"+e.Key+".json");
                }
            },DispatcherPriority.Background);
        };
        foreach(var f in fields)f.PropertyChanged+=(_,e)=>{if(e.Property==TextBox.TextProperty || e.Property==TextBox.SelectionStartProperty || e.Property==TextBox.SelectionEndProperty || e.Property==TextBox.CaretIndexProperty || e.Property==InputElement.IsFocusedProperty){changes.Add(new{id=Array.IndexOf(fields,f),property=e.Property.Name,start=f.SelectionStart,end=f.SelectionEnd,caret=f.CaretIndex});Record();}};
        w.Opened+=(_,_)=>Record();
        base.OnFrameworkInitializationCompleted();
    }
}
