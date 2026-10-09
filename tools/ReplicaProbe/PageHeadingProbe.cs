using System.Text.Json;
using Avalonia;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Input;
using Avalonia.Headless;
using Avalonia.Input.Raw;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using Verifier.Desktop;

internal sealed class PageHeadingProbeApplication:Application
{
    private sealed record Case(string Id,string Lang,double Width,string Title,string Description,bool Home=false);
    public override void OnFrameworkInitializationCompleted()
    {
        var life=(IClassicDesktopStyleApplicationLifetime)ApplicationLifetime!;var args=life.Args!;
        var cases=JsonSerializer.Deserialize<Case[]>(File.ReadAllText(args[1]),new JsonSerializerOptions{PropertyNameCaseInsensitive=true})!;
        var output=args[2];Directory.CreateDirectory(output);var n=new WebPageHeading{Width=656};void Present(Case c){n.TitleText.FontSize=c.Home?36:28;n.TitleText.LetterSpacing=c.Home?-1.26:-.98;n.TitleLineHeight=c.Home?48.6:37.8;n.TitleMaximumWidth=c.Home?700:double.PositiveInfinity;n.SetPresentation(c.Title,c.Description);}Present(cases[0]);/* initial */ n.Width=cases[0].Width;
        var canvas=new Canvas{Width=1100,Height=650,UseLayoutRounding=false,Background=Brush.Parse("#313338")};canvas.Children.Add(n);Canvas.SetLeft(n,30);Canvas.SetTop(n,100);
        var w=new Window{Title="Replica PageHeading verification",Width=1130,Height=750,UseLayoutRounding=false,Content=canvas};life.MainWindow=w;
        object Box(Control x){var p=x.TranslatePoint(default,n)??default;return new{x=p.X,y=p.Y,width=x.Bounds.Width,height=x.Bounds.Height};}
        string Rgb(IBrush? b){var c=((ISolidColorBrush)b!).Color;return $"rgb({c.R}, {c.G}, {c.B})";}
        object Read(Case c)=>new{id=c.Id,width=n.Bounds.Width,height=n.Bounds.Height,title=Box(n.TitleText),description=Box(n.DescriptionText),titleText=n.TitleText.Text,descriptionText=n.DescriptionText.Text,marginBottom=n.Margin.Bottom+"px",titleColor=Rgb(n.TitleText.Foreground),descriptionColor=Rgb(n.DescriptionText.Foreground)};
        if(args[0]=="--pageheading-window")
        {
            var keyboard=true;var text=new TextBlock{Width=1000,TextWrapping=TextWrapping.Wrap};Canvas.SetLeft(text,30);Canvas.SetTop(text,360);canvas.Children.Add(text);
            void State(){w.UpdateLayout();var focused=w.FocusManager?.GetFocusedElement();var id=focused is Control c?AutomationProperties.GetAutomationId(c):null;var state=new{focused=n.FocusTarget.IsFocused,focusVisible=n.FocusVisible,focusId=id,headingLevel=AutomationProperties.GetHeadingLevel(n.FocusTarget),title=n.TitleText.Text};var json=JsonSerializer.Serialize(state,new JsonSerializerOptions{WriteIndented=true});text.Text=json;File.WriteAllText(Path.Combine(output,"pageheading-input.json"),json);}
            w.AddHandler(InputElement.PointerPressedEvent,(_,_)=>keyboard=false,RoutingStrategies.Tunnel,true);w.AddHandler(InputElement.KeyDownEvent,(_,_)=>keyboard=true,RoutingStrategies.Tunnel,true);
            w.AddHandler(InputElement.GotFocusEvent,(_,_)=>Dispatcher.UIThread.Post(State),RoutingStrategies.Bubble,true);w.AddHandler(InputElement.LostFocusEvent,(_,_)=>Dispatcher.UIThread.Post(State),RoutingStrategies.Bubble,true);w.AddHandler(InputElement.KeyUpEvent,(_,_)=>Dispatcher.UIThread.Post(State),RoutingStrategies.Bubble,true);
            var focus=new Button{Content="Focus title",Name="PageHeading-focus"};AutomationProperties.SetAutomationId(focus,"focus-title");focus.Click+=(_,_)=>{n.FocusTitle(keyboard);State();};Canvas.SetLeft(focus,500);Canvas.SetTop(focus,20);canvas.Children.Insert(0,focus);
            var after=new Button{Content="Next button",Name="PageHeading-next"};AutomationProperties.SetAutomationId(after,"after-title");Canvas.SetLeft(after,30);Canvas.SetTop(after,280);canvas.Children.Add(after);
            w.Opened+=(_,_)=>State();
        }
        else w.Opened+=(_,_)=>Dispatcher.UIThread.Post(async()=>
        {
            var rows=new List<object>();foreach(var c in cases){n.Width=c.Width;Present(c);await Dispatcher.UIThread.InvokeAsync(()=>{},DispatcherPriority.Background);n.InvalidateMeasure();w.UpdateLayout();rows.Add(Read(c));}
            if(ReplicaPlatform.IsHeadless)
            {
                var states=new List<object>();var after=new Button{Content="Next button"};AutomationProperties.SetAutomationId(after,"after-title");Canvas.SetLeft(after,30);Canvas.SetTop(after,500);canvas.Children.Add(after);w.UpdateLayout();
                void State(string label){w.UpdateLayout();var focused=w.FocusManager?.GetFocusedElement();states.Add(new{label,focused=n.FocusTarget.IsFocused,focusVisible=n.FocusVisible,focusId=focused is Control control?AutomationProperties.GetAutomationId(control):null,headingLevel=AutomationProperties.GetHeadingLevel(n.FocusTarget)});}
                void Send(Key key){w.KeyPress(key,RawInputModifiers.None,PhysicalKey.None,"");w.KeyRelease(key,RawInputModifiers.None,PhysicalKey.None,"");}
                n.FocusTitle(false);State("pointer-focus");Send(Key.Enter);State("enter");Send(Key.Tab);State("tab");n.FocusTitle(true);State("keyboard-focus");
                var json=JsonSerializer.Serialize(states,new JsonSerializerOptions{WriteIndented=true});File.WriteAllText(Path.Combine(output,"pageheading-headless-input.json"),json);
                var saved=states.Select(x=>JsonSerializer.SerializeToElement(x)).ToArray();if(!saved[0].GetProperty("focused").GetBoolean()||saved[0].GetProperty("focusVisible").GetBoolean()||!saved[1].GetProperty("focusVisible").GetBoolean()||saved[2].GetProperty("focusId").GetString()!="after-title"||!saved[3].GetProperty("focusVisible").GetBoolean())throw new InvalidOperationException("Headless heading input contract failed");
                canvas.Children.Remove(after);n.FocusTarget.Focus(NavigationMethod.Pointer);n.FocusTitle(false);w.UpdateLayout();
            }
            using var bmp=new RenderTargetBitmap(new PixelSize(2200,1300),new Vector(192,192));bmp.Render(canvas);bmp.Save(Path.Combine(output,"pageheading.png"),new PngBitmapEncoderOptions());File.WriteAllText(Path.Combine(output,"pageheading-native.json"),JsonSerializer.Serialize(new{dpr=w.RenderScaling,rows},new JsonSerializerOptions{WriteIndented=true}));w.Close();Console.WriteLine("Recorded "+rows.Count+" PageHeading cases");
        },DispatcherPriority.Background);
        base.OnFrameworkInitializationCompleted();
    }
}
