using System.Reflection;
using System.Text.Json;
using Avalonia;
using Avalonia.Animation;
using Avalonia.Controls;
using Avalonia.Media.Transformation;
using Verifier.Desktop;

internal static class MotionProbe
{
    public static void Run(string output, bool integerMilliseconds = false)
    {
        // Development-only access to the pinned engine's controlled clock, never used by the product.
        var clockType = typeof(Animation).Assembly.GetType("Avalonia.Animation.Clock", true)!;
        var pulse = clockType.GetMethod("Pulse", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)!;
        var run = typeof(Animation).GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
            .Single(m => m.Name == "RunAsync" && m.GetParameters().Length == 3);
        var apply = typeof(Animation).GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
            .Single(m => m.Name == "Apply" && m.GetParameters().Length == 5);
        var rows = new List<object>();
        foreach (var kind in Enum.GetValues<WebMotionKind>())
        {
            var root = new Canvas {Width=200,Height=100,UseLayoutRounding=false};
            var target = new Border { Width = 100, Height = 40, Opacity = 1, RenderTransformOrigin = WebMotion.OriginFor(kind) };
            root.Children.Add(target);root.Measure(new(200,100));root.Arrange(new(0,0,200,100));
            var clock = Activator.CreateInstance(clockType, nonPublic: true)!;
            var animation = WebMotion.Create(kind)!;
            using var cancellation = new CancellationTokenSource();
            Task? task = null;
            IDisposable? infinite = null;
            if (animation.IterationCount.IsInfinite)
                infinite = (IDisposable)apply.Invoke(animation, [target, clock, new AlwaysMatch(), null, true])!;
            else task = (Task)run.Invoke(animation, [target, clock, cancellation.Token])!;
            var samples = new List<object>();
            foreach (var fraction in new[] { 0d, .125, .25, .375, .5, .625, .75, .875, 1, 1.001, 1.25, 1.5 })
            {
                var milliseconds = fraction * animation.Duration.TotalMilliseconds;
                if (integerMilliseconds) milliseconds = Math.Floor(milliseconds);
                pulse.Invoke(clock, [TimeSpan.FromMilliseconds(milliseconds)]);
                var matrix = target.RenderTransform?.Value ?? Matrix.Identity;
                var corners=new[]{new Point(0,0),new Point(100,0),new Point(0,40),new Point(100,40)}.Select(p=>target.TranslatePoint(p,root)!.Value).ToArray();
                var bounds=new[]{corners.Min(p=>p.X),corners.Min(p=>p.Y),corners.Max(p=>p.X)-corners.Min(p=>p.X),corners.Max(p=>p.Y)-corners.Min(p=>p.Y)};
                samples.Add(new { fraction, milliseconds, opacity = target.Opacity, matrix = new[] {matrix.M11,matrix.M12,matrix.M21,matrix.M22,matrix.M31,matrix.M32},bounds,completed=task?.IsCompleted ?? false });
            }
            if (task is not null) SpinWait.SpinUntil(() => task.IsCompleted, 200);
            var finished = task?.Status.ToString();
            cancellation.Cancel();
            infinite?.Dispose();
            // A new target checks cancellation while an effect is still active.
            var cancelTarget = new Border { Opacity = 1 };
            var cancelClock = Activator.CreateInstance(clockType, nonPublic:true)!;
            using var midCancellation = new CancellationTokenSource();
            Task? cancelTask = null; IDisposable? cancelInfinite = null;
            if (animation.IterationCount.IsInfinite)
                cancelInfinite = (IDisposable)apply.Invoke(animation,[cancelTarget,cancelClock,new AlwaysMatch(),null,true])!;
            else cancelTask=(Task)run.Invoke(animation,[cancelTarget,cancelClock,midCancellation.Token])!;
            pulse.Invoke(cancelClock,[TimeSpan.Zero]);
            pulse.Invoke(cancelClock,[TimeSpan.FromMilliseconds(animation.Duration.TotalMilliseconds/4)]);
            var beforeCancel=cancelTarget.Opacity;
            midCancellation.Cancel();cancelInfinite?.Dispose();
            if(cancelTask is not null) SpinWait.SpinUntil(()=>cancelTask.IsCompleted,200);
            rows.Add(new {kind=kind.ToString(),duration=animation.Duration.TotalMilliseconds,fill=animation.FillMode.ToString(),infinite=animation.IterationCount.IsInfinite,reducedSkipped=WebMotion.Create(kind,true)is null,samples,finished,cancelledOpacity=target.Opacity,midCancellation=new {beforeCancel,afterCancel=cancelTarget.Opacity,matrix=cancelTarget.RenderTransform?.Value??Matrix.Identity,status=cancelTask?.Status.ToString()}});
        }
        File.WriteAllText(output, JsonSerializer.Serialize(rows,new JsonSerializerOptions {WriteIndented=true}));
    }
    private sealed class AlwaysMatch : IObservable<bool>
    {
        public IDisposable Subscribe(IObserver<bool> observer) { observer.OnNext(true); return new EmptyDisposable(); }
    }
    private sealed class EmptyDisposable : IDisposable { public void Dispose() { } }
}
