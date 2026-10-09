using System.Reflection;
using System.Text.Json;
using Avalonia.Media;
using Verifier.Desktop;

internal static class SidebarItemTimingProbe
{
    internal static void Run(string output)
    {
        const BindingFlags hidden=BindingFlags.NonPublic|BindingFlags.Public|BindingFlags.Static|BindingFlags.Instance;
        var type=typeof(WebSidebarItem);var colorType=type.GetNestedType("CssColor",hidden)!;var openTrack=type.GetNestedType("CssTransition`1",hidden)!;
        var delegateType=typeof(Func<,,,>).MakeGenericType(colorType,colorType,typeof(double),colorType);var interpolate=type.GetMethod("MixPrecise",hidden)!.CreateDelegate(delegateType);
        var makeColor=colorType.GetMethod("FromColor",hidden)!;var toColor=colorType.GetMethod("ToColor",hidden)!;
        double clock=0;Func<double> now=()=>clock;
        object Track(Type valueType,object function,double duration,object start,object end)
        {
            var closed=openTrack.MakeGenericType(valueType);var track=Activator.CreateInstance(closed,hidden,null,new object[]{function,duration,now},null)!;
            closed.GetMethod("Snap",hidden)!.Invoke(track,new[]{start});closed.GetMethod("SetTarget",hidden)!.Invoke(track,new[]{end});return track;
        }
        object ColorValue(string text)=>makeColor.Invoke(null,new object[]{Color.Parse(text)})!;
        object ColorTrack(string start,string end)=>Track(colorType,interpolate,120,ColorValue(start),ColorValue(end));
        object ScalarTrack(double start,double end,double duration)=>Track(typeof(double),(Func<double,double,double,double>)((a,b,t)=>a+(b-a)*t),duration,start,end);
        object Current(object track)=>track.GetType().GetProperty("Current",hidden)!.GetValue(track)!;
        string ReadColor(object track)=>((Color)toColor.Invoke(Current(track),null)!).ToString();
        var rows=new List<object>();
        foreach(var scenario in new[]{"normal-hover","active-hover","disable-color","pressed-scale","hover-reverse"})
        {
            clock=0;var active=scenario=="active-hover";var disable=scenario=="disable-color";var pressed=scenario=="pressed-scale";
            var bg=ColorTrack(active?"#3c3f4b":"#00000000",disable||pressed?"#00000000":"#35373c");
            var color=ColorTrack(active?"#f2f3f5":"#cdd0d6",active?"#f2f3f5":disable?"#b5bac1":"#cdd0d6");
            var shadow=ColorTrack(active?"#8b94ff":"#00000000","#00000000");var x=ScalarTrack(active?3:0,0,120);var scale=ScalarTrack(1,pressed?.98:1,80);
            if(scenario=="hover-reverse")
            {clock=30;bg.GetType().GetMethod("SetTarget",hidden)!.Invoke(bg,new[]{ColorValue("#00000000")});}
            var duration=(double)bg.GetType().GetField("_duration",hidden)!.GetValue(bg)!;
            foreach(var ms in new[]{0d,8,16,30,40,60,80,100,120,160})
            {
                clock=ms+(scenario=="hover-reverse"?30:0);
                rows.Add(new{scenario,ms,bg=ReadColor(bg),color=ReadColor(color),badgeColor=active?"#ffffffff":ReadColor(color),shadowX=(double)Current(x),shadowColor=ReadColor(shadow),scale=(double)Current(scale),reverseDuration=scenario=="hover-reverse"?duration:(double?)null});
            }
        }
        File.WriteAllText(output,JsonSerializer.Serialize(new{scope="Actual private SidebarItem transition algorithm with injected clock; actual control input/lifecycle proved separately",rows},new JsonSerializerOptions{WriteIndented=true}));Console.WriteLine($"Recorded {rows.Count} SidebarItem transition samples.");
    }
}
