using System.Reflection;
using System.Text.Json;
using Avalonia.Media;
using Verifier.Desktop;
internal static class DbmsOptionTimingProbe
{
    internal static void Run(string output)
    {
        const BindingFlags flags=BindingFlags.NonPublic|BindingFlags.Public|BindingFlags.Instance|BindingFlags.Static;
        var owner=typeof(WebDbmsOption);var color=owner.GetNestedType("CssColor",flags)!;var trackType=owner.GetNestedType("CssTransition`1",flags)!;var mix=owner.GetMethod("Mix",flags)!.CreateDelegate(typeof(Func<,,,>).MakeGenericType(color,color,typeof(double),color));
        object C(string text)=>color.GetMethod("FromColor",flags)!.Invoke(null,new object[]{Color.Parse(text)})!;double clock=0;
        object Track(Type type,object fn,object a,object b){var t=Activator.CreateInstance(trackType.MakeGenericType(type),flags,null,new object[]{fn,160d,(Func<double>)(()=>clock)},null)!;t.GetType().GetMethod("Snap",flags)!.Invoke(t,new[]{a});t.GetType().GetMethod("SetTarget",flags)!.Invoke(t,new[]{b});return t;}
        object CT(string a,string b)=>Track(color,mix,C(a),C(b));object Value(object t)=>t.GetType().GetProperty("Current",flags)!.GetValue(t)!;string ColorValue(object t)=>((Color)color.GetMethod("ToColor",flags)!.Invoke(Value(t),null)!).ToString();
        var rows=new List<object>();foreach(var scenario in new[]{"normal-hover","selected-hover","select","unselect","hover-reverse"})
        {
            clock=0;var selected=scenario is "selected-hover" or "unselect";var endSelected=scenario is "selected-hover" or "select";var hover=scenario is "normal-hover" or "selected-hover" or "hover-reverse";
            var bg=CT(selected?"#3b3d52":"#2b2d31",hover?"#383b44":endSelected?"#3b3d52":"#2b2d31");var border=CT(selected?"#8b94ff":"#7c838c",hover?"#a7b1ff":endSelected?"#8b94ff":"#7c838c");var shadow=CT(selected?"#8b94ff":"#00000000",endSelected?"#8b94ff":"#00000000");var spread=Track(typeof(double),(Func<double,double,double,double>)((a,b,t)=>a+(b-a)*t),selected?1d:0d,endSelected?1d:0d);
            if(scenario=="hover-reverse"){clock=30;bg.GetType().GetMethod("SetTarget",flags)!.Invoke(bg,new[]{C("#2b2d31")});border.GetType().GetMethod("SetTarget",flags)!.Invoke(border,new[]{C("#7c838c")});}
            var duration=(double)bg.GetType().GetField("_duration",flags)!.GetValue(bg)!;foreach(var ms in new[]{0d,.5,8,16,30,40,60,80,120,160}){clock=ms+(scenario=="hover-reverse"?30:0);rows.Add(new{scenario,ms,bg=ColorValue(bg),border=ColorValue(border),shadowColor=ColorValue(shadow),shadowSpread=(double)Value(spread),reverseDuration=scenario=="hover-reverse"?(double?)duration:null});}
        }
        File.WriteAllText(output,JsonSerializer.Serialize(new{rows},new JsonSerializerOptions{WriteIndented=true}));Console.WriteLine($"Recorded {rows.Count} DbmsOption production transition samples.");
    }
}
