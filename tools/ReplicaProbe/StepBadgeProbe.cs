using System.Text.Json;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Verifier.Desktop;

internal static class StepBadgeProbe
{
    internal sealed record Case(string Id,string Lang,string State,bool Disabled,int Number,string? InheritedColor=null);
    internal static void Run(string input,string output)
    {
        Directory.CreateDirectory(output);var cases=JsonSerializer.Deserialize<Case[]>(File.ReadAllText(input),new JsonSerializerOptions{PropertyNameCaseInsensitive=true})!;
        var canvas=new Canvas{Width=600,Height=cases.Length*60+70,Background=Brush.Parse("#313338"),UseLayoutRounding=false};var badges=new List<WebStepBadge>();
        void Apply(WebStepBadge badge,Case c){badge.Index=c.Number;badge.State=Enum.Parse<WebStepBadgeState>(c.State,true);badge.ParentDisabled=c.Disabled;badge.InheritedForeground=c.InheritedColor is null?null:Brush.Parse(c.InheritedColor); }
        foreach(var(c,i) in cases.Select((c,i)=>(c,i)))
        {
            var badge=new WebStepBadge{HorizontalAlignment=HorizontalAlignment.Left,VerticalAlignment=VerticalAlignment.Center};Apply(badge,c);
            var parent=new Border{Width=196,Height=52,BorderThickness=new(1),Padding=new(14,12),BorderBrush=Brushes.Transparent,UseLayoutRounding=false,Child=badge};
            Canvas.SetLeft(parent,30);Canvas.SetTop(parent,30+i*60);canvas.Children.Add(parent);badges.Add(badge);
        }
        var window=new Window{Title="Replica StepBadge verification",Width=600,Height=660,Content=canvas};((IClassicDesktopStyleApplicationLifetime)Application.Current!.ApplicationLifetime!).MainWindow=window;
        object Read(WebStepBadge badge,Case c)
        {
            var surface=badge.GetVisualDescendants().OfType<WebSurfaceBox>().Single();var label=badge.GetVisualDescendants().OfType<WebSystemTextBlock>().Single();var icon=badge.GetVisualDescendants().OfType<SvgIcon>().Single();
            Control child=label.IsVisible?label:icon;var p=badge.TranslatePoint(default,(Visual)badge.Parent!)??default;var q=child.TranslatePoint(default,badge)??default;var parent=(Control)badge.Parent!;
            return new{id=c.Id,rect=new{width=badge.Bounds.Width,height=badge.Bounds.Height},parent=new{x=p.X,y=p.Y,width=parent.Bounds.Width,height=parent.Bounds.Height},child=new{x=q.X,y=q.Y,width=child.Bounds.Width,height=child.Bounds.Height},kind=label.IsVisible?"digit":"check",text=label.IsVisible?label.Text:"",style=new{bg=((ISolidColorBrush)surface.Background!).Color.ToString(),color=((ISolidColorBrush)label.Foreground!).Color.ToString(),border=((ISolidColorBrush)surface.BorderBrush!).Color.ToString(),radius=surface.CornerRadius.TopLeft,borderWidth=surface.BorderThickness.Left,fontSize=label.FontSize,fontWeight=(int)label.FontWeight,lineHeight=label.LineHeight},focusable=badge.Focusable,tabStop=badge.IsTabStop};
        }
        void Record()
        {
            window.UpdateLayout();var rows=badges.Select((b,i)=>Read(b,cases[i])).ToArray();
            using(var bitmap=new RenderTargetBitmap(new PixelSize(1200,(int)canvas.Height*2),new Vector(192,192))){bitmap.Render(canvas);bitmap.Save(Path.Combine(output,"stepbadge-native.png"),new PngBitmapEncoderOptions());}
            var updates=new List<object>();foreach(var c in cases){Apply(badges[0],c);window.UpdateLayout();updates.Add(Read(badges[0],c));}
            File.WriteAllText(Path.Combine(output,"stepbadge-native.json"),JsonSerializer.Serialize(new{scale=window.RenderScaling,rows,updates},new JsonSerializerOptions{WriteIndented=true}));window.Close();Console.WriteLine($"Recorded {rows.Length} StepBadge states and {updates.Count} same-object updates.");
        }
        window.Opened+=(_,_)=>Dispatcher.UIThread.Post(Record,DispatcherPriority.Background);
    }
}
internal sealed class StepBadgeProbeApplication : Application
{
    public override void Initialize(){Styles.Add(new Avalonia.Themes.Fluent.FluentTheme());}
    public override void OnFrameworkInitializationCompleted(){var args=((IClassicDesktopStyleApplicationLifetime)ApplicationLifetime!).Args!;StepBadgeProbe.Run(args[1],args[2]);base.OnFrameworkInitializationCompleted();}
}
