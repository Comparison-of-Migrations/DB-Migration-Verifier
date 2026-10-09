using System.Reflection;
using System.Text.Json;
using Avalonia.Media;
using Verifier.Desktop;
internal static class ResultRowTimingProbe
{
 internal static void Run(string output)
 {
  const BindingFlags flags=BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Static|BindingFlags.Instance;var owner=typeof(WebResultSummaryRow);var track=owner.GetNestedType("Track`1",flags)!;var mix=owner.GetMethod("Mix",flags)!.CreateDelegate(typeof(Func<Color,Color,double,Color>));double clock=0;var rows=new List<object>();
  foreach(var scenario in new[]{"hover","hover-reverse","press","press-reverse","chevron","chevron-reverse"})
  {
   clock=0;bool numeric=!scenario.StartsWith("hover"),reverse=scenario.EndsWith("reverse");double duration=scenario.StartsWith("press")?80:120;
   object initial=numeric?(scenario.StartsWith("press")?1d:0d):Color.Parse("#00000000"),target=numeric?(scenario.StartsWith("press")?.98d:2d):Color.Parse("#353740");
   object fn=numeric?(Func<double,double,double,double>)((a,b,t)=>a+(b-a)*t):mix;var t=Activator.CreateInstance(track.MakeGenericType(numeric?typeof(double):typeof(Color)),flags,null,new object[]{fn,duration,(Func<double>)(()=>clock)},null)!;var type=t.GetType();type.GetMethod("Snap",flags)!.Invoke(t,new[]{initial});type.GetMethod("Set",flags)!.Invoke(t,new[]{target});if(reverse){clock=30;type.GetMethod("Set",flags)!.Invoke(t,new[]{initial});}
   foreach(var ms in new[]{0d,.5,8,16,30,40,60,80,120,160,200,240}){clock=ms+(reverse?30:0);var value=type.GetProperty("Current",flags)!.GetValue(t)!;rows.Add(new{scenario,ms,value=numeric?value:value.ToString(),reverseDuration=reverse?(double?)type.GetField("_duration",flags)!.GetValue(t):null});}
  }
  File.WriteAllText(output,JsonSerializer.Serialize(new{rows},new JsonSerializerOptions{WriteIndented=true}));Console.WriteLine($"Recorded {rows.Count} production result row transitions.");
 }
}
