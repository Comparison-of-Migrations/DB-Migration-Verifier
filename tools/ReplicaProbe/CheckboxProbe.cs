using System.Reflection;
using System.Text.Json;
using Avalonia;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using Verifier.Desktop;

internal static class CheckboxProbe
{
    private const BindingFlags Private = BindingFlags.Instance|BindingFlags.NonPublic;
    internal sealed record Case(string Id,string State,bool Hover,bool Pressed,bool Focused,bool FocusVisible,bool Disabled,bool Reduced);
    internal static void Run(string input,string output)
    {
        Directory.CreateDirectory(output);
        var cases=JsonSerializer.Deserialize<Case[]>(File.ReadAllText(input),new JsonSerializerOptions{PropertyNameCaseInsensitive=true})!;
        var canvas=new Canvas { Width=720,Height=624,UseLayoutRounding=false,Background=Brush.Parse("#313338") };
        var rows=new List<object>();
        foreach(var (c,i) in cases.Select((c,i)=>(c,i)))
        {
            var checkbox=new WebCheckbox { IsChecked=c.State=="mixed" ? null : c.State=="checked",IsEnabled=!c.Disabled,ReducedMotion=c.Reduced };
            Canvas.SetLeft(checkbox,30+i/12*240);Canvas.SetTop(checkbox,16+i%12*52);canvas.Children.Add(checkbox);
            var appearance=typeof(WebCheckbox).GetMethod("Resolve",Private)!.Invoke(checkbox,new object[]{c.Hover,c.Pressed,c.Focused,c.FocusVisible})!;
            typeof(WebCheckbox).GetMethod("Paint",Private)!.Invoke(checkbox,new[]{appearance});
            var outline=typeof(WebCheckbox).GetField("_outline",Private)!.GetValue(checkbox)!;
            outline.GetType().GetProperty("Visible")!.SetValue(outline,c.FocusVisible);
            rows.Add(new { id=c.Id,checkedState=checkbox.IsChecked,disabled=!checkbox.IsEffectivelyEnabled,appearance=appearance });
        }
        canvas.Measure(new(720,624));canvas.Arrange(new(0,0,720,624));
        File.WriteAllText(Path.Combine(output,"checkbox-native.json"),JsonSerializer.Serialize(rows,new JsonSerializerOptions{WriteIndented=true,IncludeFields=true}));
        foreach(var scale in new[]{1,2})
        {
            using var bitmap=new RenderTargetBitmap(new PixelSize(720*scale,624*scale),new Vector(96*scale,96*scale));
            bitmap.Render(canvas);bitmap.Save(Path.Combine(output,$"checkbox-native-{scale}x.png"),new PngBitmapEncoderOptions());
        }
        Console.WriteLine($"Rendered {cases.Length} source-derived checkbox states.");
    }
    internal static void Timing(string output)
    {
        var owner=typeof(WebCheckbox);var flags=BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Instance;
        var result=new List<object>();
        foreach(var (name,fromState,toState,hover,pressed) in new[]{("check","unchecked","checked",false,false),("mixed","unchecked","mixed",false,false),("mixed-check","mixed","checked",false,false),("uncheck","checked","unchecked",false,false),("hover","checked","checked",true,false),("pressed","checked","checked",false,true)})
        {
            var c=new WebCheckbox();bool? State(string state)=>state=="mixed"?null:state=="checked";
            c.IsChecked=State(fromState);var from=owner.GetMethod("Resolve",Private)!.Invoke(c,new object[]{false,false,false,false})!;
            c.IsChecked=State(toState);var to=owner.GetMethod("Resolve",Private)!.Invoke(c,new object[]{hover,pressed,false,false})!;
            double now=0;var tracks=new Dictionary<string,(object Track,Type Type,object From,object To)>();
            foreach(var key in new[]{"Background","Border","Glow","Scale","Pose"})
            {
                var property=from.GetType().GetProperty(key)!;var valueType=property.PropertyType;
                Delegate mix=valueType==typeof(double)?(Func<double,double,double,double>)((a,b,t)=>a+(b-a)*t)
                    : valueType.GetMethod("Mix",BindingFlags.Public|BindingFlags.Static)!.CreateDelegate(typeof(Func<,,,>).MakeGenericType(valueType,valueType,typeof(double),valueType));
                var duration=key=="Pose"?160d:key=="Scale"?80d:120d;
                var easing=key=="Pose"?(Func<double,double>)WebMotion.SourceEase().Ease:WebMotion.CssEase().Ease;
                var type=owner.GetNestedType("Track`1",BindingFlags.NonPublic)!.MakeGenericType(valueType);
                var track=type.GetConstructors(flags).Single().Invoke(new object[]{mix,duration,easing,(Func<double>)(()=>now)});
                var a=property.GetValue(from)!;var b=property.GetValue(to)!;
                type.GetMethod("Snap")!.Invoke(track,new[]{a});type.GetMethod("SetTarget")!.Invoke(track,new[]{b});tracks.Add(key,(track,type,a,b));
            }
            object Read(double time)=>new {time,appearance=tracks.ToDictionary(x=>x.Key,x=>x.Value.Type.GetProperty("Current")!.GetValue(x.Value.Track))};
            var times=new[]{0d,.5,15.125,40.125,75.375,80,105.625,120,160};
            var samples=new List<object>();foreach(var time in times){now=time;samples.Add(Read(time));}
            now=0;foreach(var t in tracks.Values){t.Type.GetMethod("Snap")!.Invoke(t.Track,new[]{t.From});t.Type.GetMethod("SetTarget")!.Invoke(t.Track,new[]{t.To});}
            now=40.125;foreach(var t in tracks.Values)t.Type.GetMethod("SetTarget")!.Invoke(t.Track,new[]{t.From});
            var durations=tracks.ToDictionary(x=>x.Key,x=>(double)x.Value.Type.GetField("_duration",flags)!.GetValue(x.Value.Track)!);
            var reversed=new List<object>();foreach(var time in times){now=40.125+time;reversed.Add(Read(time));}
            result.Add(new{name,samples,reversed,durations});
        }
        File.WriteAllText(output,JsonSerializer.Serialize(result,new JsonSerializerOptions{WriteIndented=true,IncludeFields=true}));
        Console.WriteLine("Checkbox production tracks sampled:108 forward/reverse rows");
    }
    internal static object Read(WebCheckbox c)
    {
        var appearance=typeof(WebCheckbox).GetField("_paint",Private)!.GetValue(c);
        return new { checkedState=c.IsChecked,enabled=c.IsEffectivelyEnabled,focused=c.IsFocused,focusVisible=c.FocusVisible,hover=c.IsPointerOver,pressed=c.IsPressed,reduced=c.ReducedMotion,
            width=c.Bounds.Width,height=c.Bounds.Height,appearance };
    }
}
internal sealed class CheckboxProbeApplication : Application
{
    public override void OnFrameworkInitializationCompleted()
    {
        var desktop=(IClassicDesktopStyleApplicationLifetime)ApplicationLifetime!;
        var output=desktop.Args![1];Directory.CreateDirectory(output);
        var canvas=new Canvas{Background=Brush.Parse("#313338")};
        var boxes=new[] {new WebCheckbox(),new WebCheckbox{IsChecked=true},new WebCheckbox{IsChecked=null},new WebCheckbox{IsChecked=true,IsEnabled=false},new WebCheckbox{ReducedMotion=true}};
        var names=new[]{"검사 선택","Checked input","Mixed input","Disabled input","Reduced input"};
        for(var i=0;i<boxes.Length;i++)
        {
            AutomationProperties.SetName(boxes[i],names[i]);AutomationProperties.SetAutomationId(boxes[i],"ReplicaCheckbox"+i);
            Canvas.SetLeft(boxes[i],32);Canvas.SetTop(boxes[i],32+i*52);canvas.Children.Add(boxes[i]);
            var label=WebTypography.CreateCandidate(names[i],13,20.8);label.Foreground=Brush.Parse("#dbdee1");
            Canvas.SetLeft(label,72);Canvas.SetTop(label,31.6+i*52);canvas.Children.Add(label);
        }
        var changes=0;
        var window=new Window{Title="Replica Checkbox verification",Width=500,Height=370,CanResize=false,Content=canvas};desktop.MainWindow=window;
        void Record(string reason)
        {
            File.WriteAllText(Path.Combine(output,"checkbox-input.json"),JsonSerializer.Serialize(new {utc=DateTime.UtcNow,reason,changes,rows=boxes.Select((c,i)=>new{name=names[i],state=CheckboxProbe.Read(c)})},new JsonSerializerOptions{WriteIndented=true,IncludeFields=true}));
            window.Title="Replica Checkbox verification · input "+changes;
        }
        void Later(string reason) {var timer=new DispatcherTimer{Interval=TimeSpan.FromMilliseconds(250)};timer.Tick+=(_,_)=>{timer.Stop();Record(reason);};timer.Start();}
        foreach(var c in boxes)c.IsCheckedChanged+=(_,_)=>{changes++;Later("changed");};
        window.AddHandler(InputElement.KeyUpEvent,(_,e)=>Later(e.Key+"-released"),RoutingStrategies.Tunnel,true);
        window.Opened+=(_,_)=>Later("opened");
        base.OnFrameworkInitializationCompleted();
    }
}
