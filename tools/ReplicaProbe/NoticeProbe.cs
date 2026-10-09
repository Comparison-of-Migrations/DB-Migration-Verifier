using System.Text.Json;
using System.Reflection;
using Avalonia;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Verifier.Desktop;

internal static class NoticeProbe
{
    internal sealed record Case(string Id,string Lang,double Width,string Title,string Text,string Kind);
    internal static void Run(string input,string output)
    {
        Directory.CreateDirectory(output);var cases=JsonSerializer.Deserialize<Case[]>(File.ReadAllText(input),new JsonSerializerOptions{PropertyNameCaseInsensitive=true})!;
        var canvas=new Canvas{Width=960,Height=cases.Length*260+60,Background=Brush.Parse("#313338"),UseLayoutRounding=false};var controls=new List<WebNotice>();
        void Apply(WebNotice n,Case c){n.Width=c.Width;n.Title=c.Title;n.Text=c.Text;n.Kind=c.Kind;}
        foreach(var(c,i) in cases.Select((c,i)=>(c,i)))
        {var n=new WebNotice{ReducedMotion=true};Apply(n,c);Canvas.SetLeft(n,30);Canvas.SetTop(n,30+i*260);canvas.Children.Add(n);controls.Add(n);}
        var window=new Window{Title="Replica Notice verification",Width=960,Height=720,Content=canvas};((IClassicDesktopStyleApplicationLifetime)Application.Current!.ApplicationLifetime!).MainWindow=window;
        object Read(WebNotice n,Case c)
        {
            var surface=n.GetVisualDescendants().OfType<WebSurfaceBox>().Single();var icon=n.GetVisualDescendants().OfType<SvgIcon>().Single();
            var body=(Control)typeof(WebNotice).GetField("_body",BindingFlags.NonPublic|BindingFlags.Instance)!.GetValue(n)!;
            var texts=body.GetVisualDescendants().OfType<WebSystemTextBlock>().ToArray();var title=texts[0];var text=texts[1];var strut=texts[2];
            object Box(Control control){var p=control.TranslatePoint(default,n)??default;return new{x=p.X,y=p.Y,width=control.Bounds.Width,height=control.Bounds.Height};}
            var titlePoint=title.TranslatePoint(default,n)??default;var baselines=title.SystemLineWidths.Select((_,i)=>titlePoint.Y+title.SystemBaseline+i*title.LineHeight).ToArray();
            return new{id=c.Id,rect=new{width=n.Bounds.Width,height=n.Bounds.Height},icon=Box(icon),body=Box(body),paragraph=Box(text),baseline=baselines.Length>0?(double?)baselines.Last():null,
                contentWidth=body.GetType().GetProperty("ContentWidth")!.GetValue(body),titleWidths=title.SystemLineWidths,paragraphWidths=text.SystemLineWidths,strutBaseline=strut.SystemBaseline,
                role=n.Kind=="error"?"alert":"note",live=AutomationProperties.GetLiveSetting(n).ToString(),
                style=new{bg=((ISolidColorBrush)surface.Background!).Color.ToString(),color=((ISolidColorBrush)text.Foreground!).Color.ToString(),border=((ISolidColorBrush)surface.BorderBrush!).Color.ToString(),radius=surface.CornerRadius.TopLeft,borderWidth=surface.BorderThickness.Left,padding=surface.Padding.ToString(),marginTop=n.Margin.Top,titleColor=((ISolidColorBrush)title.Foreground!).Color.ToString(),titleWeight=(int)title.FontWeight,titleSize=title.FontSize,lineHeight=title.LineHeight,paragraphHeight=text.LineHeight},focusable=n.Focusable,tabStop=n.IsTabStop};
        }
        void Record()
        {
            window.UpdateLayout();var rows=controls.Select((n,i)=>Read(n,cases[i])).ToArray();
            using(var bitmap=new RenderTargetBitmap(new PixelSize(1920,(int)canvas.Height*2),new Vector(192,192)))
            {bitmap.Render(canvas);bitmap.Save(Path.Combine(output,"notice-native.png"),new PngBitmapEncoderOptions());}
            var updates=new List<object>();foreach(var c in cases){Apply(controls[0],c);window.UpdateLayout();updates.Add(Read(controls[0],c));}
            File.WriteAllText(Path.Combine(output,"notice-native.json"),JsonSerializer.Serialize(new{scale=window.RenderScaling,rows,updates},new JsonSerializerOptions{WriteIndented=true}));window.Close();Console.WriteLine($"Recorded {rows.Length} Notice states and {updates.Count} updates.");
        }
        window.Opened+=(_,_)=>Dispatcher.UIThread.Post(Record,DispatcherPriority.Background);
    }
}
internal sealed class NoticeProbeApplication : Application
{
    public override void Initialize(){Styles.Add(new Avalonia.Themes.Fluent.FluentTheme());}
    public override void OnFrameworkInitializationCompleted(){var args=((IClassicDesktopStyleApplicationLifetime)ApplicationLifetime!).Args!;NoticeProbe.Run(args[1],args[2]);base.OnFrameworkInitializationCompleted();}
}
