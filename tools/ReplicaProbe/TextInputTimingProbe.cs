using System.Reflection;
using System.Text.Json;
using Verifier.Desktop;
internal static class TextInputTimingProbe
{
    internal static void Run(string output)
    {
        var flags=BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic;
        var owner=typeof(WebTextInput);var rgba=owner.GetNestedType("Rgba",BindingFlags.NonPublic)!;
        var type=owner.GetNestedType("FocusTrack",BindingFlags.NonPublic)!;
        object Value(double a,double r,double g,double b)=>Activator.CreateInstance(rgba,a,r,g,b)!;
        var unfocused=Value(255,124,131,140);var focused=Value(255,167,177,255);
        var clear=Value(0,0,0,0);var glow=Value(34,167,177,255);
        var rows=new List<object>();
        foreach(var reverse in new[]{false,true})
        {
            double now=0;
            object Track(object initial)=>type.GetConstructors(flags).Single().Invoke(new[]{initial,(object)(Func<double>)(()=>now)});
            var a=reverse?focused:unfocused;var b=reverse?unfocused:focused;
            var c=reverse?glow:clear;var d=reverse?clear:glow;
            var border=Track(a);var shadow=Track(c);
            void Target(object track,object value)=>type.GetMethod("Target")!.Invoke(track,new[]{value});
            void Snap(object track,object value)=>type.GetMethod("Snap")!.Invoke(track,new[]{value});
            object Current(object track)=>type.GetProperty("Current")!.GetValue(track)!;
            object Read(double time){var g=Current(shadow);return new{time,border=Current(border),glow=g,spread=3*(double)rgba.GetProperty("A")!.GetValue(g)!/34};}
            Target(border,b);Target(shadow,d);
            var times=new[]{0d,.5,15.125,40.125,75.375,80,105.625,120,160};
            var samples=new List<object>();foreach(var t in times){now=t;samples.Add(Read(t));}
            now=0;Snap(border,a);Snap(shadow,c);Target(border,b);Target(shadow,d);
            now=40.125;Target(border,a);Target(shadow,c);
            var duration=(double)type.GetField("_duration",flags)!.GetValue(border)!;
            var back=new List<object>();foreach(var t in times){now=40.125+t;back.Add(Read(t));}
            rows.Add(new{name=reverse?"blur":"focus",samples,reversed=back,duration});
        }
        File.WriteAllText(output,JsonSerializer.Serialize(rows,new JsonSerializerOptions{WriteIndented=true}));
        Console.WriteLine("TextInput production focus tracks:36 fractional samples");
    }
}
