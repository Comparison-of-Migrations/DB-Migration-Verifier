using System.Reflection;
using System.Text.Json;
using Verifier.Desktop;

internal static class CategorySectionTimingProbe
{
    internal static void Run(string nativeGeometry,string output)
    {
        using var geometry=JsonDocument.Parse(File.ReadAllText(nativeGeometry));
        var row=geometry.RootElement.GetProperty("rows").EnumerateArray().First(r=>r.GetProperty("id").GetString()=="ko-676-data-00");
        double naturalHeight=28+row.GetProperty("items").EnumerateArray().Sum(i=>i.GetProperty("box").GetProperty("height").GetDouble());
        const BindingFlags flags=BindingFlags.NonPublic|BindingFlags.Public|BindingFlags.Instance;
        var type=typeof(WebCategorySection).GetNestedType("ScalarTrack",BindingFlags.NonPublic)!;double clock=0;
        object New(double duration,Func<double,double> easing)=>Activator.CreateInstance(type,flags,null,new object[]{duration,easing,(Func<double>)(()=>clock)},null)!;
        void Snap(object t,double v)=>type.GetMethod("Snap",flags)!.Invoke(t,new object[]{v});
        void Set(object t,double v)=>type.GetMethod("SetTarget",flags)!.Invoke(t,new object[]{v});
        double Current(object t)=>(double)type.GetProperty("Current",flags)!.GetValue(t)!;
        var rows=new List<object>();var durations=new List<object>();
        foreach(var scenario in new[]{"expand","collapse","expand-reverse","select","unselect","select-reverse"})
        {
            clock=0;bool expansion=scenario.StartsWith("expand")||scenario=="collapse",reverse=scenario.EndsWith("reverse");double initial=scenario is "collapse" or "unselect"?1:0;
            var open=New(200,WebMotion.SourceEase().Ease);var visibility=New(200,WebMotion.CssEase().Ease);var selected=New(160,WebMotion.CssEase().Ease);
            Snap(open,expansion?initial:1);Snap(visibility,expansion?initial:1);Snap(selected,expansion?0:initial);
            if(expansion){Set(open,1-initial);Set(visibility,1-initial);}else Set(selected,1-initial);
            if(reverse){clock=30;if(expansion){Set(open,initial);Set(visibility,initial);}else Set(selected,initial);}
            durations.Add(new{scenario,open=(double)type.GetField("_duration",flags)!.GetValue(open)!,visibility=(double)type.GetField("_duration",flags)!.GetValue(visibility)!,selected=(double)type.GetField("_duration",flags)!.GetValue(selected)!});
            foreach(var ms in new[]{0d,.5,8,16,30,40,60,80,120,160,200,240})
            {
                clock=ms+(reverse?30:0);var t=Current(selected);byte Mix(int a,int b)=>(byte)Math.Round(a+(b-a)*t,MidpointRounding.AwayFromZero);
                bool targetSelected=!expansion&&(reverse?initial:1-initial)==1;
                rows.Add(new{scenario,ms,height=Math.Floor(naturalHeight*Current(open)*128)/128,visibility=Current(visibility)>0?"visible":"hidden",border=$"rgb({Mix(65,115)}, {Mix(67,124)}, {Mix(74,184)})",shadow=targetSelected?"rgb(139, 148, 255) 3px 0px 0px 0px inset":"rgba(0, 0, 0, 0.02) 0px 1px 2px 0px"});
            }
        }
        File.WriteAllText(output,JsonSerializer.Serialize(new{rows,durations},new JsonSerializerOptions{WriteIndented=true}));Console.WriteLine("Recorded72 production category transition samples.");
    }
}
