using System.Reflection;
using System.Text.Json;
using Avalonia.Media;
using Verifier.Desktop;
internal static class CapabilityRowTimingProbe
{
 internal static void Run(string output)
 {
  const BindingFlags f=BindingFlags.NonPublic|BindingFlags.Public|BindingFlags.Instance;var type=typeof(WebCapabilityRow).GetNestedType("BackgroundTrack",BindingFlags.NonPublic)!;double clock=0;var rows=new List<object>();foreach(var scenario in new[]{"hover","unhover","hover-reverse","unhover-reverse"}){clock=0;var initial=Color.Parse(scenario.StartsWith("unhover")?"#3a3c44":"#00000000");var target=Color.Parse(scenario.StartsWith("hover")?"#3a3c44":"#00000000");var t=Activator.CreateInstance(type,f,null,new object[]{(Func<double>)(()=>clock)},null)!;type.GetMethod("Snap",f)!.Invoke(t,new object[]{initial});type.GetMethod("Set",f)!.Invoke(t,new object[]{target});var reverse=scenario.EndsWith("reverse");if(reverse){clock=30;type.GetMethod("Set",f)!.Invoke(t,new object[]{initial});}foreach(var ms in new[]{0d,.5,8,16,30,40,60,80,120,160,200,240}){clock=ms+(reverse?30:0);rows.Add(new{scenario,ms,value=type.GetProperty("Current",f)!.GetValue(t)!.ToString(),reverseDuration=reverse?(double?)type.GetField("_duration",f)!.GetValue(t):null});}}
  File.WriteAllText(output,JsonSerializer.Serialize(new{rows},new JsonSerializerOptions{WriteIndented=true}));Console.WriteLine("Recorded 48 production capability row timing samples.");
 }
}
