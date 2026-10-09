using System.Text.Json;
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

internal static class SearchInputProbe
{
    internal sealed record Case(string Id,string Context,string Lang,double Width,string Value,string Label,string Placeholder,string Clear);
    internal static void Run(string input,string output,bool interactive)
    {
        Directory.CreateDirectory(output);
        var cases=JsonSerializer.Deserialize<Case[]>(File.ReadAllText(input),new JsonSerializerOptions{PropertyNameCaseInsensitive=true})!;
        if(interactive)cases=cases.Where(c=>new[]{"tables-ko-380-empty","tables-ko-380-value","tables-ko-380-space","matching-ko-452-value"}.Contains(c.Id)).ToArray();
        var canvas=new Canvas{Width=600,Height=cases.Length*76+70,Background=Brush.Parse("#313338"),UseLayoutRounding=false};
        var fields=new List<WebSearchInput>();
        foreach(var (c,i) in cases.Select((c,i)=>(c,i)))
        {
            var f=new WebSearchInput{Context=c.Context=="tables"?WebSearchContext.Tables:WebSearchContext.Matching,Width=c.Width,
                AccessibleName=c.Label,Placeholder=c.Placeholder,ClearAccessibleName=c.Clear,Text=c.Value};
            AutomationProperties.SetAutomationId(f.Editor,"SearchInput-"+c.Id);AutomationProperties.SetAutomationId(f.ClearButton,"SearchClear-"+c.Id);
            Canvas.SetLeft(f,30);Canvas.SetTop(f,30+i*76);canvas.Children.Add(f);fields.Add(f);
        }
        var window=new Window{Title="Replica SearchInput verification",Width=600,Height=700,Content=canvas};
        ((IClassicDesktopStyleApplicationLifetime)Application.Current!.ApplicationLifetime!).MainWindow=window;
        object Box(Control child,WebSearchInput parent){var point=child.TranslatePoint(default,parent)??default;return new{x=point.X,y=point.Y,width=child.Bounds.Width,height=child.Bounds.Height};}
        object Read(WebSearchInput f,string id)
        {
            var icon=f.GetVisualDescendants().OfType<SvgIcon>().First(i=>i.IconName=="search");
            var surface=f.Editor.GetVisualDescendants().OfType<WebSurfaceBox>().Single();
            return new{id,width=f.Bounds.Width,height=f.Bounds.Height,margin=new{left=f.Margin.Left,top=f.Margin.Top,right=f.Margin.Right,bottom=f.Margin.Bottom},input=Box(f.Editor,f),icon=Box(icon,f),clear=f.ClearButton.IsVisible?Box(f.ClearButton,f):null,
                value=f.Text,placeholder=f.Placeholder,label=f.AccessibleName,clearName=f.ClearButton.IsVisible?f.ClearAccessibleName:null,clearDisabled=f.ClearButton.IsVisible?(bool?)!f.ClearButton.IsEnabled:null,
                style=new{padding=f.Editor.Padding.ToString(),fontSize=f.Editor.FontSize,lineHeight=f.Editor.LineHeight,bg=((ISolidColorBrush)surface.Background!).Color.ToString(),color=((ISolidColorBrush)f.Editor.Foreground!).Color.ToString(),border=((ISolidColorBrush)surface.BorderBrush!).Color.ToString(),radius=surface.CornerRadius.TopLeft}};
        }
        var events=new List<object>();
        void Evidence(WebSearchInput f,string kind)
        {events.Add(new{id=AutomationProperties.GetAutomationId(f.Editor),kind,value=f.Text,focused=f.Editor.IsFocused,clearDisabled=!f.ClearButton.IsEnabled});File.WriteAllText(Path.Combine(output,"searchinput-input.json"),JsonSerializer.Serialize(new{events},new JsonSerializerOptions{WriteIndented=true}));}
        foreach(var f in fields){f.QueryChanged+=(_,_)=>Evidence(f,"input");f.ClearInvoked+=(_,_)=>Dispatcher.UIThread.Post(()=>Evidence(f,"clear"),DispatcherPriority.Background);f.Editor.KeyUp+=(_,e)=>Evidence(f,"keyup-"+e.Key);f.Editor.GotFocus+=(_,_)=>Evidence(f,"focus");}
        window.KeyDown+=(_,e)=>
        {
            if(e.Key==Key.Escape&&fields.FirstOrDefault(f=>f.Editor.IsFocused) is {} f)Evidence(f,"parent-escape");
        };
        void Record()
        {
            window.UpdateLayout();var rows=fields.Select((f,i)=>Read(f,cases[i].Id)).ToArray();
            using(var bitmap=new RenderTargetBitmap(new PixelSize(1200,(int)canvas.Height*2),new Vector(192,192))){bitmap.Render(canvas);bitmap.Save(Path.Combine(output,"searchinput-native.png"),new PngBitmapEncoderOptions());}
            var updates=new List<object>();var target=fields[0];
            foreach(var value in new[]{"orders",""," ","ARCHIVE","한글값",new string('x',300),""}){target.Text=value;window.UpdateLayout();updates.Add(new{value=target.Text,disabled=!target.ClearButton.IsEnabled});}target.Text=cases[0].Value;
            File.WriteAllText(Path.Combine(output,"searchinput-native.json"),JsonSerializer.Serialize(new{scale=window.RenderScaling,rows,updates},new JsonSerializerOptions{WriteIndented=true}));
            Console.WriteLine($"Recorded {rows.Length} SearchInput layouts and {updates.Count} value updates.");
            if(interactive){events.Clear();File.WriteAllText(Path.Combine(output,"searchinput-input.json"),JsonSerializer.Serialize(new{events}));}else window.Close();
        }
        window.Opened+=(_,_)=>Dispatcher.UIThread.Post(Record,DispatcherPriority.Background);
    }
}
internal sealed class SearchInputProbeApplication : Application
{
    public override void Initialize(){Styles.Add(new Avalonia.Themes.Fluent.FluentTheme());}
    public override void OnFrameworkInitializationCompleted(){var args=((IClassicDesktopStyleApplicationLifetime)ApplicationLifetime!).Args!;SearchInputProbe.Run(args[1],args[2],args[0]=="--searchinput-window");base.OnFrameworkInitializationCompleted();}
}
