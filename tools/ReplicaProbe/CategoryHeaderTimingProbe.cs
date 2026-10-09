using System.Reflection;
using System.Text.Json;
using Avalonia.Media;
using Verifier.Desktop;
internal static class CategoryHeaderTimingProbe
{
 internal static void Run(string output)
 {
  const BindingFlags flags=BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Static|BindingFlags.Instance;var owner=typeof(WebCategoryHeader);var rgba=owner.GetNestedType("Rgba",flags)!;var track=owner.GetNestedType("Track`1",flags)!;var mix=rgba.GetMethod("Mix",flags)!.CreateDelegate(typeof(Func<,,,>).MakeGenericType(rgba,rgba,typeof(double),rgba));object C(string v)=>rgba.GetMethod("Parse",flags)!.Invoke(null,new object[]{v})!;double clock=0;
  var rows=new List<object>();foreach(var scenario in new[]{"select","unselect","hover","hover-reverse","press","press-reverse","expand","expand-reverse"})
  {
   clock=0;bool numeric=scenario.StartsWith("press")||scenario.StartsWith("expand"),reverse=scenario.EndsWith("reverse");double duration=scenario.StartsWith("press")?80:scenario.StartsWith("expand")?200:scenario is "select" or "unselect"?160:120;var easing=scenario is "select" or "unselect"?WebMotion.CssEase():WebMotion.SourceEase();object initial=numeric?(scenario.StartsWith("press")?1d:0d):C(scenario=="unselect"?"#343640":"#00000000");object target=numeric?(scenario.StartsWith("press")?.98d:90d):C(scenario=="unselect"?"#00000000":scenario=="select"?"#343640":"#3e414c");
   object fn=numeric?(Func<double,double,double,double>)((a,b,t)=>a+(b-a)*t):mix;var t=Activator.CreateInstance(track.MakeGenericType(numeric?typeof(double):rgba),flags,null,new object[]{fn,duration,(Func<double,double>)easing.Ease,(Func<double>)(()=>clock)},null)!;var type=t.GetType();type.GetMethod("Snap",flags)!.Invoke(t,new[]{initial});type.GetMethod("SetTarget",flags)!.Invoke(t,new[]{target});if(reverse){clock=30;type.GetMethod("SetTarget",flags)!.Invoke(t,new[]{initial});}
   foreach(var ms in new[]{0d,.5,8,16,30,40,60,80,120,160,200,240}){clock=ms+(reverse?30:0);var value=type.GetProperty("Current",flags)!.GetValue(t)!;rows.Add(new{scenario,ms,value=numeric?value:rgba.GetProperty("Color",flags)!.GetValue(value)!.ToString(),reverseDuration=reverse?(double?)type.GetField("_duration",flags)!.GetValue(t):null});}
  }
  File.WriteAllText(output,JsonSerializer.Serialize(new{rows},new JsonSerializerOptions{WriteIndented=true}));Console.WriteLine($"Recorded {rows.Count} production header transition samples.");
 }
}
