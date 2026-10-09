using System.Text.Json;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Verifier.Desktop;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Threading;
using Avalonia.VisualTree;

internal static class SpinnerProbe
{
    internal sealed record Case(string Id, string Color, double Angle, bool Reduced);
    internal static void Run(string casesPath, string output)
    {
        Directory.CreateDirectory(output);
        var cases = JsonSerializer.Deserialize<Case[]>(File.ReadAllText(casesPath), new JsonSerializerOptions { PropertyNameCaseInsensitive = true })!;
        var canvas = new Canvas { Width = 400, Height = cases.Length * 48 + 32, Background = Brush.Parse("#313338"), UseLayoutRounding = false };
        foreach (var (c, i) in cases.Select((c, i) => (c, i)))
        {
            var spinner = new WebSpinner { CurrentColor = Brush.Parse(c.Color), ReducedMotion = c.Reduced };
            spinner.RenderTransform = new RotateTransform(c.Angle);
            Canvas.SetLeft(spinner, 32); Canvas.SetTop(spinner, 16 + i * 48); canvas.Children.Add(spinner);
        }
        canvas.Measure(new(400, canvas.Height)); canvas.Arrange(new(0, 0, 400, canvas.Height));
        foreach (var scale in new[] { 1, 2 })
        {
            using var bitmap = new RenderTargetBitmap(new PixelSize(400 * scale, (int)canvas.Height * scale), new Vector(96 * scale, 96 * scale));
            bitmap.Render(canvas); bitmap.Save(Path.Combine(output, $"spinner-native-{scale}x.png"), new PngBitmapEncoderOptions());
        }
        File.WriteAllText(Path.Combine(output, "spinner-native.json"), JsonSerializer.Serialize(canvas.Children.Cast<WebSpinner>().Select((s, i) => new
        { id = cases[i].Id, x = Canvas.GetLeft(s), y = Canvas.GetTop(s), width = s.Bounds.Width, height = s.Bounds.Height, opacity = s.Opacity, color = s.CurrentColor.ToString(), transform = s.RenderTransform?.Value.ToString() }), new JsonSerializerOptions { WriteIndented = true }));
        Console.WriteLine($"Rendered {cases.Length} Spinner states.");
    }
}

internal sealed class SpinnerProbeApplication : Application
{
    public override void OnFrameworkInitializationCompleted()
    {
        var desktop = (IClassicDesktopStyleApplicationLifetime)ApplicationLifetime!;
        var output = desktop.Args![1]; Directory.CreateDirectory(output);
        var canvas = new Canvas { Background = Brush.Parse("#313338") };
        var normal = new WebSpinner { CurrentColor = Brush.Parse("#b5bac1") };
        var switching = new WebSpinner { CurrentColor = Brushes.White };
        var reduced = new WebSpinner { CurrentColor = Brush.Parse("#b5bac1"), ReducedMotion = true };
        foreach (var (s, i) in new[] { normal, switching, reduced }.Select((s, i) => (s, i)))
        { Canvas.SetLeft(s, 32 + i * 80); Canvas.SetTop(s, 32); canvas.Children.Add(s); }
        var window = new Window { Title = "Replica Spinner verification", Width = 320, Height = 180, CanResize = false, Content = canvas };
        desktop.MainWindow = window;
        window.Opened += (_, _) =>
        {
            var samples = new List<object>(); var stage = 0;
            var playbackField = typeof(WebSpinner).GetField("_playback", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!;
            var initialPlayback = (WebMotionPlayback)playbackField.GetValue(normal)!;
            var timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(200) };
            object Read(WebSpinner s) => new { width = s.Bounds.Width, height = s.Bounds.Height, s.Opacity, reduced = s.ReducedMotion, matrix = s.RenderTransform?.Value.ToString(), attached = s.GetVisualParent() is not null };
            timer.Tick += (_, _) =>
            {
                if (stage == 0) samples.Add(new { stage = "playing", normal = Read(normal), switching = Read(switching), reduced = Read(reduced), completed = initialPlayback.Completion.IsCompleted });
                if (stage == 1) { switching.ReducedMotion = true; canvas.Children.Remove(normal); }
                if (stage == 2)
                {
                    samples.Add(new { stage = "reduced-and-detached", normal = Read(normal), switching = Read(switching), reduced = Read(reduced), completed = initialPlayback.Completion.IsCompleted });
                    switching.ReducedMotion = false;
                }
                if (stage == 3)
                {
                    samples.Add(new { stage = "resumed", switching = Read(switching) }); timer.Stop();
                    File.WriteAllText(Path.Combine(output, "spinner-window.json"), JsonSerializer.Serialize(new { utc = DateTime.UtcNow, samples }, new JsonSerializerOptions { WriteIndented = true }));
                    window.Title = "Replica Spinner verification · completed";
                }
                stage++;
            };
            timer.Start();
        };
        base.OnFrameworkInitializationCompleted();
    }
}
