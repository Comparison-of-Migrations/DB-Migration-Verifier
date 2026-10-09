using System.Reflection;
using System.Text.Json;
using Avalonia;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Verifier.Desktop;

internal static class SidebarItemProbe
{
    internal sealed record Case(string Id,string Lang,string Label,int Index,double Width,string State,bool Disabled,string Interaction);
    internal static void Run(string input,string output,bool interactive)
    {
        Directory.CreateDirectory(output);var cs=JsonSerializer.Deserialize<Case[]>(File.ReadAllText(input),new JsonSerializerOptions{PropertyNameCaseInsensitive=true})!;
        if(interactive)cs=cs.Where(c=>new[]{"ko-normal-0-idle","ko-active-0-idle","ko-normal-1-idle","en-done-0-idle"}.Contains(c.Id)).ToArray();
        var canvas=new Canvas{Width=600,Height=cs.Length*110+80,Background=Brush.Parse("#313338"),UseLayoutRounding=false};var items=new List<WebSidebarItem>();
        foreach(var(c,i)in cs.Select((c,i)=>(c,i))){var item=new WebSidebarItem{Width=c.Width,Content=c.Label,Index=c.Index,State=Enum.Parse<WebStepBadgeState>(c.State,true),IsEnabled=!c.Disabled,ReducedMotion=interactive&&c.Id=="en-done-0-idle"};AutomationProperties.SetAutomationId(item,"SidebarItem-"+c.Id);Canvas.SetLeft(item,30);Canvas.SetTop(item,30+i*110);canvas.Children.Add(item);items.Add(item);}
        var window=new Window{Title="Replica SidebarItem verification",Width=600,Height=700,Content=canvas};((IClassicDesktopStyleApplicationLifetime)Application.Current!.ApplicationLifetime!).MainWindow=window;
        const BindingFlags flags=BindingFlags.NonPublic|BindingFlags.Instance;
        void Endpoint(WebSidebarItem b,Case c)
        {
            var value=typeof(WebSidebarItem).GetMethod("Resolve",flags)!.Invoke(b,new object[]{c.Interaction=="hover",c.Interaction=="pressed"});typeof(WebSidebarItem).GetMethod("Paint",flags)!.Invoke(b,new[]{value});
            var outline=typeof(WebSidebarItem).GetField("_outline",flags)!.GetValue(b)!;outline.GetType().GetProperty("Visible")!.SetValue(outline,c.Interaction=="focus-visible");
        }
        object Read(WebSidebarItem b,Case c)
        {
            var s=(WebSurfaceBox)typeof(WebSidebarItem).GetField("_surface",flags)!.GetValue(b)!;object Box(Control x){var p=x.TranslatePoint(default,b)??default;return new{x=p.X,y=p.Y,width=x.Bounds.Width,height=x.Bounds.Height};}
            var transform=(ScaleTransform)b.RenderTransform!;
            return new{id=c.Id,width=b.Bounds.Width,height=b.Bounds.Height,badge=Box(b.Badge),label=Box(b.Label),style=new{bg=((ISolidColorBrush)s.Background!).Color.ToString(),color=((ISolidColorBrush)b.Label.Foreground!).Color.ToString(),border=((ISolidColorBrush)s.BorderBrush!).Color.ToString(),radius=s.CornerRadius.TopLeft,padding=s.Padding.ToString(),fontSize=b.Label.FontSize,fontWeight=(int)b.Label.FontWeight,lineHeight=b.Label.LineHeight,shadowX=s.BoxShadow.Count>0?s.BoxShadow[0].OffsetX:0,shadowColor=s.BoxShadow.Count>0?s.BoxShadow[0].Color.ToString():"#00000000",shadowInset=s.BoxShadow.Count>0&&s.BoxShadow[0].IsInset,scale=transform.ScaleX},name=AutomationProperties.GetName(b),itemStatus=AutomationProperties.GetItemStatus(b),lineWidths=b.Label.SystemLineWidths};
        }
        var clicks=0;var events=new List<object>();
        void Evidence(WebSidebarItem b,string kind){var playback=(WebMotionPlayback?)typeof(WebSidebarItem).GetField("_press",flags)!.GetValue(b);events.Add(new{id=AutomationProperties.GetAutomationId(b),kind,pressed=b.IsPressed,focused=b.IsFocused,reduced=b.ReducedMotion,motionPending=playback is not null&&!playback.Completion.IsCompleted,scale=(b.RenderTransform as ScaleTransform)?.ScaleX,clicks});File.WriteAllText(Path.Combine(output,"sidebaritem-input.json"),JsonSerializer.Serialize(new{clicks,events},new JsonSerializerOptions{WriteIndented=true}));}
        foreach(var b in items){b.Click+=(_,_)=>{clicks++;Evidence(b,"click");var playback=(WebMotionPlayback?)typeof(WebSidebarItem).GetField("_press",flags)!.GetValue(b);if(playback is not null)_=playback.Completion.ContinueWith(task=>Dispatcher.UIThread.Post(()=>Evidence(b,task.IsCanceled?"motion-canceled":"motion-ended")));};b.GotFocus+=(_,_)=>Evidence(b,"focus");b.LostFocus+=(_,_)=>Evidence(b,"blur");b.PropertyChanged+=(_,e)=>{if(e.Property==Button.IsPressedProperty)Evidence(b,"pressed-changed");};b.KeyDown+=(_,e)=>Evidence(b,"keydown-"+e.Key);b.KeyUp+=(_,e)=>Evidence(b,"keyup-"+e.Key);}
        void Record(){window.UpdateLayout();foreach(var(b,i)in items.Select((b,i)=>(b,i)))Endpoint(b,cs[i]);window.UpdateLayout();var rows=items.Select((b,i)=>Read(b,cs[i])).ToArray();using(var png=new RenderTargetBitmap(new PixelSize(1200,(int)canvas.Height*2),new Vector(192,192))){png.Render(canvas);png.Save(Path.Combine(output,"sidebaritem-native.png"),new PngBitmapEncoderOptions());}var updates=new List<object>();if(!interactive){var b=items[0];foreach(var c in cs){b.Width=c.Width;b.Content=c.Label;b.Index=c.Index;b.IsEnabled=!c.Disabled;b.State=Enum.Parse<WebStepBadgeState>(c.State,true);window.UpdateLayout();Endpoint(b,c);updates.Add(Read(b,c));}}File.WriteAllText(Path.Combine(output,"sidebaritem-native.json"),JsonSerializer.Serialize(new{scale=window.RenderScaling,rows,updates},new JsonSerializerOptions{WriteIndented=true}));if(!interactive)window.Close();else{events.Clear();File.WriteAllText(Path.Combine(output,"sidebaritem-input.json"),JsonSerializer.Serialize(new{clicks,events}));}Console.WriteLine($"Recorded {rows.Length} SidebarItem endpoint layouts.");}
        window.Opened+=(_,_)=>Dispatcher.UIThread.Post(Record,DispatcherPriority.Background);
    }
}
internal sealed class SidebarItemProbeApplication : Application
{
    public override void Initialize(){Styles.Add(new Avalonia.Themes.Fluent.FluentTheme());}
    public override void OnFrameworkInitializationCompleted(){var args=((IClassicDesktopStyleApplicationLifetime)ApplicationLifetime!).Args!;SidebarItemProbe.Run(args[1],args[2],args[0]=="--sidebaritem-window");base.OnFrameworkInitializationCompleted();}
}
