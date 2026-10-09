using System.Text.Json;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Threading;
using Ellipse = Avalonia.Controls.Shapes.Ellipse;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.VisualTree;
using Verifier.Desktop;

internal static class StatusPillProbe
{
    internal sealed record Case(string Id,string Text,string Kind,bool Category,bool Dot);
    internal static void Run(string input,string output)
    {
        Directory.CreateDirectory(output);
        var cases=JsonSerializer.Deserialize<Case[]>(File.ReadAllText(input),new JsonSerializerOptions{PropertyNameCaseInsensitive=true})!;
        var canvas=new Canvas{Width=660,Height=cases.Length*48+60,Background=Brush.Parse("#313338"),UseLayoutRounding=false};
        var pills=new List<WebStatusPill>();
        foreach(var (c,i) in cases.Select((c,i)=>(c,i)))
        {
            var pill=new WebStatusPill{Text=c.Text,Kind=c.Kind,CategoryContext=c.Category,ShowDot=c.Dot};
            Canvas.SetLeft(pill,30);Canvas.SetTop(pill,30+i*48);canvas.Children.Add(pill);pills.Add(pill);
        }
        var window=new Window{Title="Replica StatusPill verification",Width=660,Height=660,Content=canvas};
        ((IClassicDesktopStyleApplicationLifetime)Application.Current!.ApplicationLifetime!).MainWindow=window;
        void Layout(){window.UpdateLayout();}
        object Box(Rect r)=>new{x=r.X,y=r.Y,width=r.Width,height=r.Height};
        object State(WebStatusPill pill,Case c)
        {
            var surface=pill.GetVisualDescendants().OfType<Border>().Single();
            var dot=pill.GetVisualDescendants().OfType<Ellipse>().Single();
            var text=pill.GetVisualDescendants().OfType<WebSystemTextBlock>().Single();
            var point=dot.TranslatePoint(default,pill)??default;var label=text.TranslatePoint(default,pill)??default;
            return new{id=c.Id,text=pill.Text,kind=pill.Kind,category=pill.CategoryContext,dot=pill.ShowDot,
                rect=Box(new(0,0,pill.Bounds.Width,pill.Bounds.Height)),dotRect=dot.IsVisible?Box(new(point,dot.Bounds.Size)):null,
                labelRect=text.IsVisible?Box(new(label,text.Bounds.Size)):null,foreground=((ISolidColorBrush)text.Foreground!).Color.ToString(),background=((ISolidColorBrush)surface.Background!).Color.ToString(),
                padding=surface.Padding.ToString(),radius=surface.CornerRadius.TopLeft,border=surface.BorderThickness.Left,fontSize=text.FontSize,lineHeight=text.LineHeight,fontWeight=(int)text.FontWeight,
                focusable=pill.Focusable,tabStop=pill.IsTabStop};
        }
        void Record()
        {
        Layout();
        var rows=pills.Select((p,i)=>State(p,cases[i])).ToArray();
        using(var bitmap=new RenderTargetBitmap(new PixelSize(1320,(int)canvas.Height*2),new Vector(192,192)))
        {bitmap.Render(canvas);bitmap.Save(Path.Combine(output,"statuspill-native.png"),new PngBitmapEncoderOptions());}
        // Reuse one attached object. No forced invalidation, so missed state propagation remains visible.
        var changing=pills[0];var updates=new List<object>();
        foreach(var c in cases)
        {changing.Text=c.Text;changing.Kind=c.Kind;changing.CategoryContext=c.Category;changing.ShowDot=c.Dot;Layout();updates.Add(State(changing,c));}
        File.WriteAllText(Path.Combine(output,"statuspill-native.json"),JsonSerializer.Serialize(new{scale=window.RenderScaling,rows,updates},new JsonSerializerOptions{WriteIndented=true}));
        window.Close();
        Console.WriteLine($"Rendered {cases.Length} passive pills and {updates.Count} updates.");
        }
        window.Opened+=(_,_)=>Dispatcher.UIThread.Post(Record,DispatcherPriority.Background);
    }
}

internal sealed class StatusPillProbeApplication : Application
{
    public override void Initialize(){Styles.Add(new Avalonia.Themes.Fluent.FluentTheme());}
    public override void OnFrameworkInitializationCompleted()
    {var args=((IClassicDesktopStyleApplicationLifetime)ApplicationLifetime!).Args!;StatusPillProbe.Run(args[1],args[2]);base.OnFrameworkInitializationCompleted();}
}
