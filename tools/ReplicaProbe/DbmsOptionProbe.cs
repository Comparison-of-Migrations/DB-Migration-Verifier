using System.Reflection;
using System.Text.Json;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Verifier.Desktop;

internal static class DbmsOptionProbe
{
    internal sealed record Case(string Id,string Lang,bool Compact,double Width,string Name,string Description,bool Selected,bool Hover,bool Focus);
    private const BindingFlags Flags=BindingFlags.NonPublic|BindingFlags.Instance;
    internal static void Run(string input,string output)
    {
        Directory.CreateDirectory(output);var cases=JsonSerializer.Deserialize<Case[]>(File.ReadAllText(input),new JsonSerializerOptions{PropertyNameCaseInsensitive=true})!;
        var canvas=new Canvas{Width=1080,Height=Math.Ceiling(cases.Length/3d)*120+60,Background=Brush.Parse("#313338"),UseLayoutRounding=false};var controls=new List<WebDbmsOption>();
        void Apply(WebDbmsOption n,Case c){n.Width=c.Width;n.EngineName=c.Name;n.Description=c.Description;n.Compact=c.Compact;n.IsSelected=c.Selected;}
        void Endpoint(WebDbmsOption n,Case c)
        {
            var value=typeof(WebDbmsOption).GetMethod("Resolve",Flags)!.Invoke(n,new object[]{c.Hover});typeof(WebDbmsOption).GetMethod("Paint",Flags)!.Invoke(n,new[]{value});
            var outline=typeof(WebDbmsOption).GetField("_outline",Flags)!.GetValue(n)!;outline.GetType().GetProperty("Visible")!.SetValue(outline,c.Focus);
            typeof(WebRadio).GetMethod("SetFocusVisible",Flags)!.Invoke(n.Radio,new object[]{c.Focus});
        }
        foreach(var(c,i)in cases.Select((c,i)=>(c,i))){var n=new WebDbmsOption{ReducedMotion=true,GroupName=c.Id};Apply(n,c);Canvas.SetLeft(n,30+i%3*360);Canvas.SetTop(n,30+i/3*120);canvas.Children.Add(n);controls.Add(n);}
        var window=new Window{Title="Replica DbmsOption verification",Width=1080,Height=720,Content=canvas};((IClassicDesktopStyleApplicationLifetime)Application.Current!.ApplicationLifetime!).MainWindow=window;
        object Read(WebDbmsOption n,Case c)
        {
            var s=(WebSurfaceBox)typeof(WebDbmsOption).GetField("_surface",Flags)!.GetValue(n)!;var row=(Control)typeof(WebDbmsOption).GetField("_row",Flags)!.GetValue(n)!;var body=(Control)row.GetType().GetProperty("Body")!.GetValue(row)!;
            var text=body.GetVisualDescendants().OfType<WebSystemTextBlock>().ToArray();var icon=n.GetVisualDescendants().OfType<SvgIcon>().Single();
            object Box(Control x){var p=x.TranslatePoint(default,n)??default;return new{x=p.X,y=p.Y,width=x.Bounds.Width,height=x.Bounds.Height};}
            string Color(IBrush? b)=>((ISolidColorBrush)b!).Color.ToString();
            return new{id=c.Id,rect=new{width=n.Bounds.Width,height=n.Bounds.Height},body=Box(body),icon=c.Compact?null:Box(icon),radio=Box(n.Radio),title=Box(text[0]),description=Box(text[1]),outlineDiagnostics=new{rootClip=n.ClipToBounds,bounds=Box((Control)typeof(WebDbmsOption).GetField("_outline",Flags)!.GetValue(n)!),visible=typeof(WebDbmsOption).GetField("_outline",Flags)!.GetValue(n)!.GetType().GetProperty("Visible")!.GetValue(typeof(WebDbmsOption).GetField("_outline",Flags)!.GetValue(n)),clips=n.GetVisualDescendants().Select(v=>new{type=v.GetType().Name,clip=v.ClipToBounds,bounds=v.Bounds.ToString()})},style=new{bg=Color(s.Background),border=Color(s.BorderBrush),radius=s.CornerRadius.TopLeft,padding=s.Padding.Left,gap=c.Compact?8:12,shadowSpread=s.BoxShadow.Count>0?s.BoxShadow[0].Spread:0,shadowColor=s.BoxShadow.Count>0?s.BoxShadow[0].Color.ToString():"#00000000",outline=(bool)typeof(WebDbmsOption).GetField("_outline",Flags)!.GetValue(n)!.GetType().GetProperty("Visible")!.GetValue(typeof(WebDbmsOption).GetField("_outline",Flags)!.GetValue(n))!,iconColor=Color(icon.Stroke),titleSize=text[0].FontSize,titleWeight=(int)text[0].FontWeight,titleHeight=text[0].LineHeight,descriptionSize=text[1].FontSize,descriptionHeight=text[1].LineHeight,titleColor=Color(text[0].Foreground),descriptionColor=Color(text[1].Foreground)}};
        }
        void Record()
        {
            window.UpdateLayout();foreach(var(n,i)in controls.Select((n,i)=>(n,i)))Endpoint(n,cases[i]);window.UpdateLayout();var rows=controls.Select((n,i)=>Read(n,cases[i])).ToArray();
            using(var bitmap=new RenderTargetBitmap(new PixelSize(2160,(int)canvas.Height*2),new Vector(192,192))){bitmap.Render(canvas);bitmap.Save(Path.Combine(output,"dbmsoption-native.png"),new PngBitmapEncoderOptions());}
            var updates=new List<object>();foreach(var c in cases){Apply(controls[0],c);window.UpdateLayout();Endpoint(controls[0],c);updates.Add(Read(controls[0],c));}
            File.WriteAllText(Path.Combine(output,"dbmsoption-native.json"),JsonSerializer.Serialize(new{scale=window.RenderScaling,rows,updates},new JsonSerializerOptions{WriteIndented=true}));window.Close();Console.WriteLine($"Recorded {rows.Length} DbmsOption endpoints and {updates.Count} updates.");
        }
        window.Opened+=(_,_)=>Dispatcher.UIThread.Post(Record,DispatcherPriority.Background);
    }
}
internal sealed class DbmsOptionProbeApplication : Application
{
    public override void Initialize(){Styles.Add(new Avalonia.Themes.Fluent.FluentTheme());}
    public override void OnFrameworkInitializationCompleted(){var args=((IClassicDesktopStyleApplicationLifetime)ApplicationLifetime!).Args!;DbmsOptionProbe.Run(args[1],args[2]);base.OnFrameworkInitializationCompleted();}
}
