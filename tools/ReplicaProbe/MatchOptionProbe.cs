using System.Reflection;
using System.Text.Json;
using Avalonia;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using Verifier.Desktop;

internal sealed class MatchOptionProbeApplication:Application
{
    internal sealed record Case(string Id,string Lang,double Width,string Name,string Note,string Badge,bool Unavailable,bool Selected,bool Hover,bool Focus);
    private const BindingFlags Flags=BindingFlags.Instance|BindingFlags.NonPublic;
    public override void OnFrameworkInitializationCompleted()
    {
        var life=(IClassicDesktopStyleApplicationLifetime)ApplicationLifetime!;var args=life.Args!;var cases=JsonSerializer.Deserialize<Case[]>(File.ReadAllText(args[1]),new JsonSerializerOptions{PropertyNameCaseInsensitive=true})!;var output=args[2];Directory.CreateDirectory(output);
        var n=new WebMatchOption{ReducedMotion=true,Width=cases[0].Width};var canvas=new Canvas{Width=1100,Height=450,UseLayoutRounding=false,Background=Brush.Parse("#313338")};canvas.Children.Add(n);Canvas.SetLeft(n,30);Canvas.SetTop(n,30);
        var w=new Window{Title="Replica MatchOption verification",Width=1130,Height=620,UseLayoutRounding=false,Content=canvas};life.MainWindow=w;
        void Set(Case c){n.Width=c.Width;n.TargetName=c.Name;n.Note=c.Note;n.Badge=c.Badge;n.Unavailable=c.Unavailable;n.GroupName=c.Id;n.IsSelected=c.Selected;var a=typeof(WebMatchOption).GetMethod("Resolve",Flags)!.Invoke(n,new object[]{c.Hover});typeof(WebMatchOption).GetMethod("Paint",Flags)!.Invoke(n,new[]{a});var o=typeof(WebMatchOption).GetField("_outline",Flags)!.GetValue(n)!;o.GetType().GetProperty("Visible")!.SetValue(o,c.Focus);typeof(WebRadio).GetMethod("SetFocusVisible",Flags)!.Invoke(n.Radio,new object[]{c.Focus});}
        object Box(Control c){var p=c.TranslatePoint(default,n)??default;return new{x=p.X,y=p.Y,width=c.Bounds.Width,height=c.Bounds.Height};}
        string Css(Color c)=>c.A==255?$"rgb({c.R}, {c.G}, {c.B})":$"rgba({c.R}, {c.G}, {c.B}, {Math.Round(c.A/255d,3).ToString(System.Globalization.CultureInfo.InvariantCulture)})";
        object Read(Case c){var s=(WebSurfaceBox)typeof(WebMatchOption).GetField("_surface",Flags)!.GetValue(n)!;return new{id=c.Id,width=n.Bounds.Width,height=n.Bounds.Height,radio=Box(n.Radio),body=Box(n.Body),name=Box(n.NameText),note=Box(n.NoteText),badge=Box(n.BadgeText),nameText=n.TargetName,noteText=n.Note,badgeText=n.Badge,selected=n.IsSelected,disabled=!n.Radio.IsEnabled,bg=Css(((ISolidColorBrush)s.Background!).Color),border=Css(((ISolidColorBrush)s.BorderBrush!).Color),shadow=s.BoxShadow.Count==0?"none":$"{Css(s.BoxShadow[0].Color)} 0px 0px 0px {s.BoxShadow[0].Spread}px inset",outline=c.Focus?"rgb(167, 177, 255) solid 3px":"rgb(219, 222, 225) none 3px",outlineOffset=c.Focus?"2px":"0px",nameColor=Css(((ISolidColorBrush)n.NameText.Foreground!).Color),badgeColor=Css(((ISolidColorBrush)n.BadgeText.Foreground!).Color)};}
        if(args[0]=="--matchoption-window")
        {
            canvas.Children.Clear();var presentations=cases.Where(c=>c.Lang=="ko"&&c.Width==442&&!c.Selected&&!c.Hover&&!c.Focus).ToArray();var cards=new List<WebMatchOption>();var events=new List<object>();
            for(int i=0;i<presentations.Length;i++){var c=presentations[i];n=new WebMatchOption();Set(c with{Selected=i==0,Badge=i==0?"선택":c.Badge});n.GroupName="MatchOption-actual";n.ReducedMotion=false;AutomationProperties.SetAutomationId(n.Radio,"MatchOption-choice-"+i);cards.Add(n);canvas.Children.Add(n);}
            void Record(){double y=30;foreach(var card in cards){card.Measure(new(442,double.PositiveInfinity));Canvas.SetLeft(card,30);Canvas.SetTop(card,y);y+=card.DesiredSize.Height+8;}w.UpdateLayout();var rows=new List<object>();for(int i=0;i<cards.Count;i++){n=cards[i];rows.Add(Read(presentations[i]with{Focus=n.Radio.FocusVisible}));}var f=Path.Combine(output,"matchoption-input.json");File.WriteAllText(f+".next",JsonSerializer.Serialize(new{capturedAtUtc=DateTime.UtcNow,dpr=w.RenderScaling,events,rows},new JsonSerializerOptions{WriteIndented=true}));File.Move(f+".next",f,true);}
            foreach(var card in cards)card.SelectionRequested+=(_,_)=>{events.Add(new{action="select",name=card.TargetName});foreach(var other in cards)other.Badge=other.Unavailable?"사용 중":other.IsSelected?"선택":"";Record();};
            var record=new WebActionButton{Content="Record",Variant="quiet",Focusable=false,IsTabStop=false};AutomationProperties.SetAutomationId(record,"MatchOption-record");canvas.Children.Add(record);Canvas.SetLeft(record,650);Canvas.SetTop(record,100);record.Click+=(_,_)=>Record();
            var close=new WebActionButton{Content="Close fixture",Variant="quiet"};AutomationProperties.SetAutomationId(close,"MatchOption-close");canvas.Children.Add(close);Canvas.SetLeft(close,650);Canvas.SetTop(close,30);close.Click+=(_,_)=>w.Close();
            var reduced=new WebActionButton{Content="Reduced motion",Variant="quiet"};AutomationProperties.SetAutomationId(reduced,"MatchOption-reduced");canvas.Children.Add(reduced);Canvas.SetLeft(reduced,650);Canvas.SetTop(reduced,170);reduced.Click+=(_,_)=>{foreach(var card in cards)card.ReducedMotion=true;Record();};
            w.Opened+=(_,_)=>Record();base.OnFrameworkInitializationCompleted();return;
        }
        w.Opened+=(_,_)=>Dispatcher.UIThread.Post(()=>{var rows=new List<object>();foreach(var c in cases){Set(c);w.UpdateLayout();rows.Add(Read(c));}using var bmp=new RenderTargetBitmap(new PixelSize(2200,900),new Vector(192,192));bmp.Render(canvas);bmp.Save(Path.Combine(output,"matchoption.png"),new PngBitmapEncoderOptions());File.WriteAllText(Path.Combine(output,"matchoption-native.json"),JsonSerializer.Serialize(new{dpr=w.RenderScaling,rows},new JsonSerializerOptions{WriteIndented=true}));w.Close();Console.WriteLine("Recorded "+rows.Count+" MatchOption states");},DispatcherPriority.Background);
        base.OnFrameworkInitializationCompleted();
    }
}
