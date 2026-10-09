using System.Text.Json;
using System.Reflection;
using Avalonia;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Verifier.Desktop;

internal static class IconButtonProbe
{
    internal sealed record Case(string Id,string Context,string Label,bool Disabled,bool Reduced);
    internal static void Run(string input,string output,bool interactive)
    {
        Directory.CreateDirectory(output);
        var cases=JsonSerializer.Deserialize<Case[]>(File.ReadAllText(input),new JsonSerializerOptions{PropertyNameCaseInsensitive=true})!;
        var canvas=new Canvas{Width=600,Height=cases.Length*60+60,Background=Brush.Parse("#313338"),UseLayoutRounding=false};
        var buttons=new List<WebActionButton>();
        foreach(var (c,i) in cases.Select((c,i)=>(c,i)))
        {
            var b=WebIconButton.Create(c.Label,c.Reduced);b.IsEnabled=!c.Disabled;b.LabelFontSize=c.Context=="search"?12:13;
            AutomationProperties.SetAutomationId(b,"IconButton-"+c.Id);
            Canvas.SetLeft(b,30);Canvas.SetTop(b,30+i*60);canvas.Children.Add(b);buttons.Add(b);
        }
        var window=new Window{Title="Replica IconButton verification",Width=600,Height=600,Content=canvas};
        ((IClassicDesktopStyleApplicationLifetime)Application.Current!.ApplicationLifetime!).MainWindow=window;
        var flags=BindingFlags.NonPublic|BindingFlags.Instance;var type=typeof(WebActionButton);
        var resolve=type.GetMethod("Resolve",flags,[typeof(bool),typeof(bool)])!;var paint=type.GetMethod("Paint",flags)!;var outlineField=type.GetField("_outline",flags)!;
        object Box(Rect r)=>new{x=r.X,y=r.Y,width=r.Width,height=r.Height};
        object Read(WebActionButton b,string id,string mode)
        {
            var surface=b.GetVisualDescendants().OfType<WebSurfaceBox>().Single();var icon=b.GetVisualDescendants().OfType<SvgIcon>().Single();var label=b.GetVisualDescendants().OfType<WebSystemTextBlock>().Single();
            var p=icon.TranslatePoint(default,b)??default;
            var outline=outlineField.GetValue(b)!;var visible=(bool)outline.GetType().GetProperty("Visible")!.GetValue(outline)!;
            return new{id,mode,label=AutomationProperties.GetName(b),width=b.Bounds.Width,height=b.Bounds.Height,padding=b.Padding.ToString(),radius=surface.CornerRadius.TopLeft,borderWidth=surface.BorderThickness.Left,bg=((ISolidColorBrush)surface.Background!).Color.ToString(),color=((ISolidColorBrush)icon.Stroke!).Color.ToString(),border=((ISolidColorBrush)surface.BorderBrush!).Color.ToString(),
                shadow=Enumerable.Range(0,surface.BoxShadow.Count).Select(i=>new{x=surface.BoxShadow[i].OffsetX,y=surface.BoxShadow[i].OffsetY,blur=surface.BoxShadow[i].Blur,color=surface.BoxShadow[i].Color.ToString()}).ToArray(),
                scale=((ScaleTransform)b.RenderTransform!).ScaleX,focus=visible,icon=Box(new(p,icon.Bounds.Size)),fontSize=label.FontSize,text=label.Text,disabled=!b.IsEnabled,reduced=b.ReducedMotion};
        }
        var clicks=0;var events=new List<object>();
        void Evidence(WebActionButton b,string kind)
        {events.Add(new{kind,id=AutomationProperties.GetAutomationId(b),pressed=b.IsPressed,focused=b.IsFocused,clicks});File.WriteAllText(Path.Combine(output,"iconbutton-input.json"),JsonSerializer.Serialize(new{clicks,events},new JsonSerializerOptions{WriteIndented=true}));}
        foreach(var b in buttons)
        {
            b.Click+=(_,_)=>{clicks++;Evidence(b,"click");};b.GotFocus+=(_,_)=>Evidence(b,"focus");b.LostFocus+=(_,_)=>Evidence(b,"blur");
            b.PropertyChanged+=(_,e)=>{if(e.Property==Button.IsPressedProperty)Evidence(b,"pressed-changed");};
            b.KeyDown+=(_,e)=>Evidence(b,"key-down-"+e.Key);b.KeyUp+=(_,e)=>Evidence(b,"key-up-"+e.Key);
        }
        void Record()
        {
            window.UpdateLayout();var rows=new List<object>();
            foreach(var (b,i) in buttons.Select((b,i)=>(b,i)))
            foreach(var mode in new[]{"normal","hover","pressed","focus"})
            {
                if(!b.IsEnabled&&mode=="focus")continue;
                paint.Invoke(b,[resolve.Invoke(b,[mode is "hover" or "pressed",mode=="pressed"])]);
                var outline=outlineField.GetValue(b)!;outline.GetType().GetProperty("Visible")!.SetValue(outline,mode=="focus");
                rows.Add(Read(b,cases[i].Id,mode));
            }
            foreach(var b in buttons){paint.Invoke(b,[resolve.Invoke(b,[false,false])]);var outline=outlineField.GetValue(b)!;outline.GetType().GetProperty("Visible")!.SetValue(outline,false);}
            using(var bitmap=new RenderTargetBitmap(new PixelSize(1200,(int)canvas.Height*2),new Vector(192,192))){bitmap.Render(canvas);bitmap.Save(Path.Combine(output,"iconbutton-native.png"),new PngBitmapEncoderOptions());}
            var changes=new List<object>();foreach(var c in cases){WebIconButton.SetAccessibleName(buttons[0],c.Label);changes.Add(new{expected=c.Label,actual=AutomationProperties.GetName(buttons[0])});}WebIconButton.SetAccessibleName(buttons[0],cases[0].Label);
            File.WriteAllText(Path.Combine(output,"iconbutton-native.json"),JsonSerializer.Serialize(new{scale=window.RenderScaling,rows,changes},new JsonSerializerOptions{WriteIndented=true}));
            Console.WriteLine($"Recorded {rows.Count} IconButton states.");
            if(!interactive)window.Close();
        }
        window.Opened+=(_,_)=>Dispatcher.UIThread.Post(Record,DispatcherPriority.Background);
    }
}
internal sealed class IconButtonProbeApplication : Application
{
    public override void Initialize(){Styles.Add(new Avalonia.Themes.Fluent.FluentTheme());}
    public override void OnFrameworkInitializationCompleted(){var args=((IClassicDesktopStyleApplicationLifetime)ApplicationLifetime!).Args!;IconButtonProbe.Run(args[1],args[2],args[0]=="--iconbutton-window");base.OnFrameworkInitializationCompleted();}
}
