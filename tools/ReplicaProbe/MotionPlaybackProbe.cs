using System.Reflection;
using System.Text.Json;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Verifier.Desktop;

internal static class MotionPlaybackProbe
{
    private static readonly ConstructorInfo Constructor = typeof(WebMotionPlayback).GetConstructors(BindingFlags.Instance|BindingFlags.NonPublic).Single();
    private static readonly MethodInfo Advance = typeof(WebMotionPlayback).GetMethod("Advance",BindingFlags.Instance|BindingFlags.NonPublic)!;
    private static WebMotionPlayback Create(Control target, WebMotionKind kind, CancellationToken cancellation=default, bool reduced=false) =>
        (WebMotionPlayback)Constructor.Invoke([target,WebMotion.Create(kind,reduced),false,cancellation]);
    public static void Run(string output)
    {
        var rows=new List<object>();
        foreach(var kind in Enum.GetValues<WebMotionKind>())
        {
            var root=new Canvas {Width=200,Height=100,UseLayoutRounding=false};
            var target=new Border {Width=100,Height=40,RenderTransformOrigin=WebMotion.OriginFor(kind)};
            root.Children.Add(target);root.Measure(new(200,100));root.Arrange(new(0,0,200,100));
            var definition=WebMotion.Create(kind)!;
            using var playback=Create(target,kind);
            var samples=new List<object>();
            foreach(var fraction in new[]{0d,.125,.25,.375,.5,.625,.75,.875,1,1.001,1.25,1.5})
            {
                var milliseconds=fraction*definition.Duration.TotalMilliseconds;
                Advance.Invoke(playback,[TimeSpan.FromMilliseconds(milliseconds)]);
                var matrix=target.RenderTransform?.Value??Matrix.Identity;
                var corners=new[]{new Point(0,0),new Point(100,0),new Point(0,40),new Point(100,40)}.Select(p=>target.TranslatePoint(p,root)!.Value).ToArray();
                samples.Add(new {fraction,milliseconds,opacity=target.Opacity,matrix=new[]{matrix.M11,matrix.M12,matrix.M21,matrix.M22,matrix.M31,matrix.M32},bounds=new[]{corners.Min(p=>p.X),corners.Min(p=>p.Y),corners.Max(p=>p.X)-corners.Min(p=>p.X),corners.Max(p=>p.Y)-corners.Min(p=>p.Y)},completed=playback.Completion.IsCompleted});
            }
            using var cancellation=new CancellationTokenSource();
            var cancelTarget=new Border {Opacity=.7};
            using var cancelPlayback=Create(cancelTarget,kind,cancellation.Token);
            Advance.Invoke(cancelPlayback,[definition.Duration/4]);
            var before=cancelTarget.Opacity;
            cancellation.Cancel();
            var after=cancelTarget.Opacity;
            cancelPlayback.Dispose();
            var underlying=new Border {Opacity=.7,RenderTransform=new TranslateTransform(9,3)};
            using(var restore=Create(underlying,kind))
            {
                Advance.Invoke(restore,[definition.Duration/4]);
                underlying.SetValue(Visual.OpacityProperty,.4);
                restore.Dispose();
            }
            var reducedTarget=new Border {Opacity=.7};
            using var reducedPlayback=Create(reducedTarget,kind,reduced:true);
            rows.Add(new {kind=kind.ToString(),duration=definition.Duration.TotalMilliseconds,infinite=definition.IterationCount.IsInfinite,fill=definition.FillMode.ToString(),samples,midCancellation=new {before,after,afterDispose=cancelTarget.Opacity,status=cancelPlayback.Completion.Status.ToString()},restoreCurrentUnderlying=underlying.Opacity==.4&&underlying.RenderTransform!.Value.M31==9,reducedSkipped=reducedTarget.Opacity==.7&&reducedPlayback.Completion.IsCompletedSuccessfully});
        }
        File.WriteAllText(output,JsonSerializer.Serialize(rows,new JsonSerializerOptions{WriteIndented=true}));
    }
}
