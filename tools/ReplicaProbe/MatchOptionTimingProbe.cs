using System.Reflection;
using System.Text.Json;
using Avalonia.Media;
using Verifier.Desktop;

internal static class MatchOptionTimingProbe
{
    internal static void Run(string output)
    {
        const BindingFlags flags=BindingFlags.Instance|BindingFlags.Static|BindingFlags.Public|BindingFlags.NonPublic;
        double clock=0;var colorType=typeof(WebMatchOption).GetNestedType("ColorTrack",flags)!;var scalarType=typeof(WebMatchOption).GetNestedType("ScalarTrack",flags)!;
        object Track(Type type,object from,object to){var t=Activator.CreateInstance(type,flags,null,new object[]{(Func<double>)(()=>clock)},null)!;type.GetMethod("Snap",flags)!.Invoke(t,new[]{from});type.GetMethod("Set",flags)!.Invoke(t,new[]{to});return t;}
        object CT(string from,string to)=>Track(colorType,Color.Parse(from),Color.Parse(to));object Value(object t)=>t.GetType().GetProperty("Current",flags)!.GetValue(t)!;string CV(object t)=>((Color)Value(t)).ToString();
        var rows=new List<object>();foreach(var scenario in new[]{"normal-hover","selected-hover","select","unselect","hover-reverse"})
        {
            clock=0;bool selected=scenario is "selected-hover" or "unselect",endSelected=scenario is "selected-hover" or "select",hover=scenario is "normal-hover" or "selected-hover" or "hover-reverse";
            var bg=CT(selected?"#3b3d52":"#2b2d31",hover?"#383b44":endSelected?"#3b3d52":"#2b2d31");var border=CT(selected?"#8b94ff":"#7c838c",hover?"#a7b1ff":endSelected?"#8b94ff":"#7c838c");var shadow=CT(selected?"#8b94ff":"#00000000",endSelected?"#8b94ff":"#00000000");var spread=Track(scalarType,selected?1d:0d,endSelected?1d:0d);
            if(scenario=="hover-reverse"){clock=30;colorType.GetMethod("Set",flags)!.Invoke(bg,new object[]{Color.Parse("#2b2d31")});colorType.GetMethod("Set",flags)!.Invoke(border,new object[]{Color.Parse("#7c838c")});}
            foreach(var ms in new[]{0d,.5,8,16,30,40,60,80,120,160}){clock=ms+(scenario=="hover-reverse"?30:0);rows.Add(new{scenario,ms,bg=CV(bg),border=CV(border),shadowColor=CV(shadow),shadowSpread=(double)Value(spread),reverseDuration=scenario=="hover-reverse"?(double?)colorType.GetProperty("Duration",flags)!.GetValue(bg):null});}
        }
        File.WriteAllText(output,JsonSerializer.Serialize(new{rows},new JsonSerializerOptions{WriteIndented=true}));Console.WriteLine("Recorded50 production MatchOption transitions");
    }
}
