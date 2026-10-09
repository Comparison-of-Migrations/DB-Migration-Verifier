using System.Text.Json;
using Avalonia;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Interactivity;
using Avalonia.Threading;
using Verifier.Desktop;
internal static class FieldProbe
{
    internal sealed record Case(string Id,string Lang,double Width,string Label,string Value,string Type,bool Disabled,string? Hint,string Placeholder);
    internal static void Run(string input,string output,bool interactive)
    {
        Directory.CreateDirectory(output);var cases=JsonSerializer.Deserialize<Case[]>(File.ReadAllText(input),new JsonSerializerOptions{PropertyNameCaseInsensitive=true})!;if(interactive)cases=new[]{cases[0],cases[3],cases.First(c=>c.Type=="number")};
        var canvas=new Canvas{Width=interactive?560:1120,Height=interactive?600:Math.Ceiling(cases.Length/2d)*190+60,UseLayoutRounding=false,Background=Brush.Parse("#313338")};var fields=new List<WebField>();
        Control Editor(Case c)=>c.Type=="number"?new WebNumberInput{Value=c.Value,ReducedMotion=true}:new WebTextInput{Text=c.Value,PasswordChar=c.Type=="password"?'●':'\0',PlaceholderText=c.Placeholder,IsEnabled=!c.Disabled,ReducedMotion=true};
        void Apply(WebField f,Case c){f.Width=c.Width;f.Label=c.Label;f.Hint=c.Hint;f.Input=Editor(c);}
        foreach(var(c,i)in cases.Select((c,i)=>(c,i))){var f=new WebField();Apply(f,c);Canvas.SetLeft(f,30+(interactive?0:i%2*550));Canvas.SetTop(f,30+(interactive?i*150:i/2*190));AutomationProperties.SetAutomationId(f.LabelText,"FieldLabel-"+i);if(f.Input is WebNumberInput number)AutomationProperties.SetAutomationId(number.Editor,"FieldEditor-"+i);else AutomationProperties.SetAutomationId(f.Input!,"FieldEditor-"+i);canvas.Children.Add(f);fields.Add(f);}
        var w=new Window{Title="Replica Field verification",Width=interactive?560:1120,Height=700,Content=canvas};((IClassicDesktopStyleApplicationLifetime)Application.Current!.ApplicationLifetime!).MainWindow=w;
        object Read(WebField f,Case c){object Box(Control x){var p=x.TranslatePoint(default,f)??default;return new{x=p.X,y=p.Y,width=x.Bounds.Width,height=x.Bounds.Height};}return new{id=c.Id,rect=new{width=f.Bounds.Width,height=f.Bounds.Height},label=Box(f.LabelText),input=Box(f.Input!),hint=f.Hint is null?null:Box(f.HintText),marginTop=f.Margin.Top,labelSize=f.LabelText.FontSize,labelWeight=(int)f.LabelText.FontWeight,labelColor=((ISolidColorBrush)f.LabelText.Foreground!).Color.ToString(),labelHeight=f.LabelText.LineHeight,hintColor=f.Hint is null?null:((ISolidColorBrush)f.HintText.Foreground!).Color.ToString(),disabled=!f.Input!.IsEffectivelyEnabled,name=AutomationProperties.GetName(f.Input!)};}
        void InputRecord(){var rows=fields.Select(f=>new{f.Label,focused=f.Input is WebNumberInput n?n.Editor.IsFocused:f.Input!.IsFocused,enabled=f.Input!.IsEffectivelyEnabled});File.WriteAllText(Path.Combine(output,"field-input.json"),JsonSerializer.Serialize(rows,new JsonSerializerOptions{WriteIndented=true}));}
        void Record(){w.UpdateLayout();var rows=fields.Select((f,i)=>Read(f,cases[i])).ToArray();using(var image=new RenderTargetBitmap(new PixelSize(2240,(int)canvas.Height*2),new Vector(192,192))){image.Render(canvas);image.Save(Path.Combine(output,"field-native.png"),new PngBitmapEncoderOptions());}var updates=new List<object>();if(!interactive)foreach(var c in cases){Apply(fields[0],c);w.UpdateLayout();updates.Add(Read(fields[0],c));}File.WriteAllText(Path.Combine(output,"field-native.json"),JsonSerializer.Serialize(new{scale=w.RenderScaling,rows,updates},new JsonSerializerOptions{WriteIndented=true}));if(!interactive)w.Close();else InputRecord();Console.WriteLine($"Recorded {rows.Length} Field layouts.");}
        if(interactive)
        {
            var events=new List<object>();
            void Event(string name,int index)
            {
                var active=fields.FindIndex(f=>f.Input is WebNumberInput n?n.Editor.IsFocused:f.Input!.IsFocused);
                events.Add(new{ @event=name,id=cases[index].Id,active=active<0?"":cases[active].Id });
                File.WriteAllText(Path.Combine(output,"field-events.json"),JsonSerializer.Serialize(events,new JsonSerializerOptions{WriteIndented=true}));
                Dispatcher.UIThread.Post(InputRecord,DispatcherPriority.Background);
            }
            foreach(var(f,i)in fields.Select((f,i)=>(f,i)))
            {
                f.LabelHitArea.AddHandler(InputElement.PointerPressedEvent,(_,_)=>Event("mousedown",i),RoutingStrategies.Tunnel);
                f.LabelHitArea.AddHandler(InputElement.PointerReleasedEvent,(_,_)=>Event("mouseup",i),RoutingStrategies.Tunnel);
                f.Input!.GotFocus+=(_,_)=>Event("focusin",i);
                f.Input!.LostFocus+=(_,_)=>Event("focusout",i);
            }
        }
        w.Opened+=(_,_)=>Dispatcher.UIThread.Post(Record,DispatcherPriority.Background);
    }
}
internal sealed class FieldProbeApplication : Application
{
    public override void Initialize(){Styles.Add(new Avalonia.Themes.Fluent.FluentTheme());}
    public override void OnFrameworkInitializationCompleted(){var a=((IClassicDesktopStyleApplicationLifetime)ApplicationLifetime!).Args!;FieldProbe.Run(a[1],a[2],a[0]=="--field-window");base.OnFrameworkInitializationCompleted();}
}
