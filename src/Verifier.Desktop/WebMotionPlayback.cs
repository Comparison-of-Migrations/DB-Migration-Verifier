using System.Diagnostics;
using Avalonia;
using Avalonia.Animation;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Media;
using Avalonia.Threading;
using Avalonia.Styling;
using Avalonia.VisualTree;

namespace Verifier.Desktop;

/// <summary>Source keyframes on the public render clock without integer-millisecond truncation.</summary>
public sealed class WebMotionPlayback : IDisposable
{
    private readonly Control _target;
    private readonly Animation? _animation;
    private readonly List<IDisposable> _bindings = [];
    private readonly List<(FrameValue Value, (double Cue, double Value)[] Frames)> _tracks = [];
    private readonly TaskCompletionSource _completion = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private CancellationTokenRegistration _registration;
    private readonly long _started = Stopwatch.GetTimestamp();
    private bool _stopped;
    private bool _disposed;
    public Task Completion => _completion.Task;

    internal WebMotionPlayback(Control target, Animation? animation, bool scheduleFrames, CancellationToken cancellation)
    {
        _target = target;
        _animation = animation;
        if (animation is null) { _stopped = true; _completion.SetResult(); return; }
        var topLevel = scheduleFrames ? TopLevel.GetTopLevel(target) ?? throw new InvalidOperationException("Motion needs an attached control.") : null;
        var properties = animation.Children.SelectMany(frame => frame.Setters.OfType<Setter>()).Select(setter => setter.Property!).Distinct().ToArray();
        Transform? transform = properties.Any(p => p.OwnerType == typeof(TranslateTransform)) ? new TranslateTransform() :
            properties.Any(p => p.OwnerType == typeof(ScaleTransform)) ? new ScaleTransform() :
            properties.Any(p => p.OwnerType == typeof(RotateTransform)) ? new RotateTransform() : null;
        if (transform is not null)
            _bindings.Add(target.Bind(Visual.RenderTransformProperty, new FrameValue(transform), BindingPriority.Animation));
        foreach (var property in properties)
        {
            var frames = animation.Children.SelectMany(frame => frame.Setters.OfType<Setter>().Where(s => s.Property == property)
                .Select(setter => (frame.Cue.CueValue, (double)setter.Value!))).OrderBy(frame => frame.CueValue).ToArray();
            var value = new FrameValue(frames[0].Item2);
            AvaloniaObject owner = property == Visual.OpacityProperty ? target : transform!;
            _bindings.Add(owner.Bind(property, value, BindingPriority.Animation));
            _tracks.Add((value, frames));
        }
        target.DetachedFromVisualTree += Detached;
        if (cancellation.CanBeCanceled)
            _registration = cancellation.Register(() =>
            {
                if (Dispatcher.UIThread.CheckAccess()) Cancel();
                else Dispatcher.UIThread.Post(Cancel);
            });
        if (scheduleFrames && !_stopped)
        {
            topLevel!.RequestAnimationFrame(Frame);
        }
    }

    private void Frame(TimeSpan frameTime)
    {
        if (_stopped) return;
        Advance(Stopwatch.GetElapsedTime(_started));
        if (!_stopped) TopLevel.GetTopLevel(_target)?.RequestAnimationFrame(Frame);
    }

    // One timeline for actual playback and the independent source-comparison harness.
    internal void Advance(TimeSpan elapsed)
    {
        if (_stopped || _animation is null) return;
        var progress = Math.Max(0, elapsed.TotalMilliseconds / _animation.Duration.TotalMilliseconds);
        var infinite = _animation.IterationCount.IsInfinite;
        var ended = !infinite && progress >= 1;
        progress = infinite ? progress - Math.Floor(progress) : Math.Min(1, progress);
        progress = _animation.Easing.Ease(progress);
        foreach (var (value, frames) in _tracks)
        {
            var end = Array.FindIndex(frames, frame => frame.Cue >= progress);
            if (end <= 0) value.Publish(end == 0 ? frames[0].Value : frames[^1].Value);
            else
            {
                var left = frames[end - 1]; var right = frames[end];
                var fraction = (progress - left.Cue) / (right.Cue - left.Cue);
                value.Publish(left.Value + (right.Value - left.Value) * fraction);
            }
        }
        if (!ended) return;
        _stopped = true;
        if (_animation.FillMode != FillMode.Forward) ClearBindings();
        _completion.TrySetResult();
    }

    private void Cancel()
    {
        if (_stopped) return;
        _stopped = true;
        if (_animation?.FillMode != FillMode.Forward) ClearBindings();
        _completion.TrySetCanceled();
    }
    private void Detached(object? sender, VisualTreeAttachmentEventArgs e) => Dispose();
    private void ClearBindings()
    {
        foreach (var binding in _bindings) binding.Dispose();
        _bindings.Clear();
    }
    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        Cancel(); ClearBindings();
        _registration.Dispose();
        _target.DetachedFromVisualTree -= Detached;
    }

    private sealed class FrameValue(object? initial) : IObservable<object?>
    {
        private object? _current = initial;
        private readonly List<IObserver<object?>> _observers = [];
        public IDisposable Subscribe(IObserver<object?> observer)
        {
            _observers.Add(observer); observer.OnNext(_current);
            return new Subscription(() => _observers.Remove(observer));
        }
        public void Publish(object? value)
        {
            _current = value;
            foreach (var observer in _observers.ToArray()) observer.OnNext(value);
        }
        private sealed class Subscription(Action remove) : IDisposable { public void Dispose() => remove(); }
    }
}
