using System.Text.Json;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Automation;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.VisualTree;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Threading;
using Avalonia.Input;
using Avalonia.Interactivity;
using Verifier.Desktop;

internal static class ActionButtonProbe
{
    internal static void States(string casesPath, string output)
    {
        var (canvas, cases, buttons) = Build(casesPath);
        canvas.Measure(new(1000, canvas.Height)); canvas.Arrange(new(0, 0, 1000, canvas.Height));
        var type = typeof(WebActionButton);
        var flags = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;
        var resolve = type.GetMethod("Resolve", flags, [typeof(bool), typeof(bool)])!;
        var paint = type.GetMethod("Paint", flags)!;
        var outlineField = type.GetField("_outline", flags)!;
        var rows = new List<object>();
        foreach (var (button, i) in buttons.Select((button, i) => (button, i)))
        foreach (var mode in new[] { "normal", "hover", "pressed", "focus" })
        {
            if (!button.IsEnabled && mode == "focus") continue;
            var state = resolve.Invoke(button, [mode is "hover" or "pressed", mode == "pressed"]);
            paint.Invoke(button, [state]);
            var outline = outlineField.GetValue(button)!;
            outline.GetType().GetProperty("Visible")!.SetValue(outline, mode == "focus");
            var surface = button.GetVisualDescendants().OfType<WebSurfaceBox>().Single();
            var label = button.GetVisualDescendants().OfType<WebSystemTextBlock>().Single();
            static string ColorOf(IBrush? brush) => ((ISolidColorBrush)brush!).Color.ToString();
            var matrix = button.RenderTransform!.Value;
            rows.Add(new { id = cases[i].Id, mode, bg = ColorOf(surface.Background), color = ColorOf(label.Foreground), border = ColorOf(surface.BorderBrush),
                shadow = Enumerable.Range(0, surface.BoxShadow.Count).Select(index => { var s = surface.BoxShadow[index]; return new { x = s.OffsetX, y = s.OffsetY, blur = s.Blur, color = s.Color.ToString() }; }).ToArray(),
                scale = matrix.M11, focus = mode == "focus" });
        }
        File.WriteAllText(output, JsonSerializer.Serialize(new { fixture = "Production Resolve/Paint, explicit pseudo-state inputs; actual input evidence is separate", rows }, new JsonSerializerOptions { WriteIndented = true }));
    }

    internal static void Timing(string output)
    {
        var flags = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance;
        var colorType = typeof(WebActionButton).GetNestedType("CssColor", System.Reflection.BindingFlags.NonPublic)!;
        var type = typeof(WebActionButton).GetNestedType("CssTransition`1", System.Reflection.BindingFlags.NonPublic)!.MakeGenericType(colorType);
        var delegateType = typeof(Func<,,,>).MakeGenericType(colorType, colorType, typeof(double), colorType);
        var mix = typeof(WebActionButton).GetMethod("MixPrecise", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static)!.CreateDelegate(delegateType);
        object CssColor(string hex) => colorType.GetMethod("FromColor")!.Invoke(null, [Color.Parse(hex)])!;
        double now = 0;
        var track = type.GetConstructors(flags).Single().Invoke([mix, 120d, (Func<double>)(() => now)]);
        var snap = type.GetMethod("Snap")!; var target = type.GetMethod("SetTarget")!; var current = type.GetProperty("Current")!;
        string CurrentColor() => colorType.GetMethod("ToColor")!.Invoke(current.GetValue(track), null)!.ToString()!;
        snap.Invoke(track, [CssColor("#5865f2")]); target.Invoke(track, [CssColor("#4752c4")]);
        var samples = new List<object>();
        foreach (var time in new[] { 0d, 15.125, 40.125, 75.375, 105.625, 120, 121 })
        { now = time; samples.Add(new { time, color = CurrentColor() }); }
        now = 0; snap.Invoke(track, [CssColor("#5865f2")]); target.Invoke(track, [CssColor("#4752c4")]);
        now = 40.125; target.Invoke(track, [CssColor("#5865f2")]);
        var duration = (double)type.GetField("_duration", flags)!.GetValue(track)!;
        var reversed = new List<object>();
        foreach (var time in new[] { 0d, 15.125, 30.375, 60.625, 90.875, 120d })
        { now = 40.125 + time; reversed.Add(new { time, color = CurrentColor() }); }
        File.WriteAllText(output, JsonSerializer.Serialize(new { samples, reversedDuration = duration, reversed, tracks = TrackTiming() }, new JsonSerializerOptions { WriteIndented = true }));
    }
    private static object[] TrackTiming()
    {
        var flags = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance;
        var owner = typeof(WebActionButton); var nested = System.Reflection.BindingFlags.NonPublic;
        var colorType = owner.GetNestedType("CssColor", nested)!; var shadowType = owner.GetNestedType("CssShadow", nested)!;
        var resolve = owner.GetMethod("Resolve", flags, [typeof(bool), typeof(bool)])!;
        var output = new List<object>();
        foreach (var (name, variant, over, pressed) in new[] { ("primary-hover", "primary", true, false), ("quiet-hover", "quiet", true, false), ("primary-key-pressed", "primary", false, true) })
        {
            var button = new WebActionButton { Variant = variant };
            var start = resolve.Invoke(button, [false, false])!; var end = resolve.Invoke(button, [over, pressed])!;
            object Property(object appearance, string key) => appearance.GetType().GetProperty(key)!.GetValue(appearance)!;
            double now = 0;
            var tracks = new Dictionary<string, (object Track, Type Type, object From, object To)>();
            void Add(string key, Type valueType, string mixName, double duration, object from, object to)
            {
                var type = owner.GetNestedType("CssTransition`1", nested)!.MakeGenericType(valueType);
                Delegate mix = valueType == typeof(double) ? (Func<double, double, double, double>)((a, b, t) => a + (b - a) * t)
                    : owner.GetMethod(mixName, System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static)!.CreateDelegate(typeof(Func<,,,>).MakeGenericType(valueType, valueType, typeof(double), valueType));
                var track = type.GetConstructors(flags).Single().Invoke([mix, duration, (Func<double>)(() => now)]);
                type.GetMethod("Snap")!.Invoke(track, [from]); type.GetMethod("SetTarget")!.Invoke(track, [to]);
                tracks.Add(key, (track, type, from, to));
            }
            object AsColor(object x) => colorType.GetMethod("FromColor")!.Invoke(null, [x])!;
            object AsShadow(object x) => shadowType.GetMethod("FromShadow")!.Invoke(null, [x])!;
            Add("background-color", colorType, "MixPrecise", 120, AsColor(Property(start, "Background")), AsColor(Property(end, "Background")));
            Add("color", colorType, "MixPrecise", 120, AsColor(Property(start, "Color")), AsColor(Property(end, "Color")));
            Add("box-shadow", shadowType, "MixShadow", 120, AsShadow(Property(start, "Shadow")), AsShadow(Property(end, "Shadow")));
            Add("transform", typeof(double), "", 80, Property(start, "Scale"), Property(end, "Scale"));
            object Current(string key) { var t = tracks[key]; return t.Type.GetProperty("Current")!.GetValue(t.Track)!; }
            object Read(double time)
            {
                string ColorValue(string key) => colorType.GetMethod("ToColor")!.Invoke(Current(key), null)!.ToString()!;
                var shadow = (BoxShadows)shadowType.GetMethod("ToShadow")!.Invoke(Current("box-shadow"), null)!;
                return new { time, bg = ColorValue("background-color"), color = ColorValue("color"), scale = Current("transform"), shadow = Enumerable.Range(0, shadow.Count).Select(i => new { x = shadow[i].OffsetX, y = shadow[i].OffsetY, blur = shadow[i].Blur, color = shadow[i].Color.ToString() }).ToArray() };
            }
            var samples = new List<object>();
            foreach (var time in new[] { 0d, 15.125, 40.125, 75.375, 105.625, 120, 121 }) { now = time; samples.Add(Read(time)); }
            now = 0; foreach (var t in tracks.Values) { t.Type.GetMethod("Snap")!.Invoke(t.Track, [t.From]); t.Type.GetMethod("SetTarget")!.Invoke(t.Track, [t.To]); }
            now = 40.125; foreach (var t in tracks.Values) t.Type.GetMethod("SetTarget")!.Invoke(t.Track, [t.From]);
            var durations = tracks.ToDictionary(t => t.Key, t => (double)t.Value.Type.GetField("_duration", flags)!.GetValue(t.Value.Track)!);
            var reversed = new List<object>();
            foreach (var time in new[] { 0d, 15.125, 30.375, 60.625, 90.875, 120d }) { now = 40.125 + time; reversed.Add(Read(time)); }
            output.Add(new { name, samples, reversed, durations });
        }
        return output.ToArray();
    }
    internal sealed record Case(string Id, string Text, string Variant, string Icon, bool Disabled, bool Filter, bool Selected, double? Width,
        double? Height, double? MinWidth, double? PaddingX, double? PaddingY, double? FontSize, int? Weight, bool AbsoluteTrailing, bool FileEntry, bool Loading, bool LoadingReduced);
    internal static (Canvas Canvas, Case[] Cases, WebActionButton[] Buttons) Build(string casesPath, bool loadingOnly = false)
    {
        var cases = JsonSerializer.Deserialize<Case[]>(File.ReadAllText(casesPath), new JsonSerializerOptions { PropertyNameCaseInsensitive = true })!;
        if (loadingOnly) cases = cases.Where(c => c.Loading).ToArray();
        var canvas = new Canvas { Width = 1000, Height = cases.Length * 82 + 40, Background = Brush.Parse("#313338"), UseLayoutRounding = false };
        var buttons = cases.Select(c => new WebActionButton { Content = c.Text, Variant = c.Variant, IconName = c.Icon, Filter = c.Filter, Selected = c.Selected, IsEnabled = !c.Disabled, ReducedMotion = true }).ToArray();
        foreach (var (b, i) in buttons.Select((b, i) => (b, i)))
        {
            AutomationProperties.SetAutomationId(b, "ActionButton-" + cases[i].Id);
            if (cases[i].Width is double width) b.Width = width;
            if (cases[i].Height is double height) b.Height = b.MinHeight = height;
            if (cases[i].MinWidth is double minWidth) b.MinWidth = minWidth;
            if (cases[i].PaddingX is double padX) b.Padding = new(padX, cases[i].PaddingY ?? 10);
            if (cases[i].FontSize is double fontSize) b.LabelFontSize = fontSize;
            if (cases[i].Weight is int weight) b.LabelWeight = weight;
            b.IsLoading = cases[i].Loading;
            if (cases[i].Loading) b.ReducedMotion = cases[i].LoadingReduced;
            b.AbsoluteTrailingIcon = cases[i].AbsoluteTrailing;
            if (cases[i].FileEntry)
            { b.RestingBackgroundOverride = Colors.Transparent; b.BorderColorOverride = Color.Parse("#686e7b"); b.HoverBackgroundOverride = Color.Parse("#3c3f46"); b.ParentHoverIncludesDisabled = true; b.RefreshParentStyle(); }
            Canvas.SetLeft(b, 32); Canvas.SetTop(b, 28 + i * 82); canvas.Children.Add(b);
        }
        return (canvas, cases, buttons);
    }
    internal static void Run(string casesPath, string output)
    {
        Directory.CreateDirectory(output);
        var (canvas, cases, buttons) = Build(casesPath);
        canvas.Measure(new(1000, canvas.Height)); canvas.Arrange(new(0, 0, 1000, canvas.Height));
        Record(canvas, cases, buttons, output);
        using var bitmap = new RenderTargetBitmap(new PixelSize(2000, (int)canvas.Height * 2), new Vector(192, 192));
        bitmap.Render(canvas); bitmap.Save(Path.Combine(output, "action-button-native-2x.png"), new PngBitmapEncoderOptions());
        Console.WriteLine($"Rendered {cases.Length} ActionButton cases.");
    }
    internal static void Record(Canvas canvas, Case[] cases, WebActionButton[] buttons, string output)
    {
        var rows = buttons.Select((button, i) =>
        {
            var text = button.GetVisualDescendants().OfType<WebSystemTextBlock>().Single();
            var icon = button.GetVisualDescendants().OfType<SvgIcon>().Single();
            var surface = button.GetVisualDescendants().OfType<WebSurfaceBox>().Single();
            var spinner = button.GetVisualDescendants().OfType<WebSpinner>().FirstOrDefault();
            object Rect(Control control)
            { var p = control.TranslatePoint(default, canvas)!.Value; return new { x = p.X, y = p.Y, width = control.Bounds.Width, height = control.Bounds.Height }; }
            return new { id = cases[i].Id, rect = Rect(button), text = Rect(text), icon = icon.IsVisible ? Rect(icon) : null, spinner = spinner is null ? null : Rect(spinner),
                bg = surface.Background?.ToString(), color = text.Foreground?.ToString(), border = surface.BorderBrush?.ToString(),
                shadow = surface.BoxShadow.ToString(), widths = text.SystemLineWidths, lineHeight = text.LineHeight, enabled = button.IsEffectivelyEnabled };
        }).ToArray();
        File.WriteAllText(Path.Combine(output, "action-button-native-layout.json"), JsonSerializer.Serialize(rows, new JsonSerializerOptions { WriteIndented = true }));
    }
}

internal sealed class ActionButtonProbeApplication : Application
{
    private static void Evidence(string output, string name, object data)
    {
        var json = JsonSerializer.Serialize(data, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(Path.Combine(output, name), json);
        var endpoint = Path.Combine(output, "evidence-endpoint.txt");
        if (!File.Exists(endpoint)) return;
        var url = File.ReadAllText(endpoint).Trim() + "/" + name;
        _ = Task.Run(async () =>
        {
            using var client = new System.Net.Http.HttpClient();
            using var content = new System.Net.Http.StringContent(json, System.Text.Encoding.UTF8, "application/json");
            try { using var response = await client.PostAsync(url, content); response.EnsureSuccessStatusCode(); }
            catch (Exception error) { File.WriteAllText(Path.Combine(output, "action-button-evidence-error.txt"), error.Message); }
        });
    }
    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var args = desktop.Args!;
            var output = args[2]; Directory.CreateDirectory(output);
            var loadingOnly = args[0] == "--action-busy-window";
            var (canvas, cases, buttons) = ActionButtonProbe.Build(args[1], loadingOnly);
            canvas.HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Left;
            canvas.VerticalAlignment = Avalonia.Layout.VerticalAlignment.Top;
            foreach (var (button, i) in buttons.Select((b, i) => (b, i))) button.ReducedMotion = loadingOnly && cases[i].LoadingReduced;
            var window = new Window { Title = loadingOnly ? "Replica ActionButton busy verification" : "Replica ActionButton verification", Width = 1000, Height = 1040,
                Background = Brush.Parse("#313338"), Content = canvas, UseLayoutRounding = false, WindowStartupLocation = WindowStartupLocation.CenterScreen };
            desktop.MainWindow = window;
            window.Opened += (_, _) => Dispatcher.UIThread.Post(() => ActionButtonProbe.Record(canvas, cases, buttons, output), DispatcherPriority.Render);
            if (loadingOnly)
            {
                window.Opened += (_, _) =>
                {
                    var samples = new List<object>(); var stage = 0;
                    var firstSpinner = buttons[0].GetVisualDescendants().OfType<WebSpinner>().Single();
                    object Read(WebActionButton b) { var spinner = b.GetVisualDescendants().OfType<WebSpinner>().FirstOrDefault(); return new { label = b.Content, loading = b.IsLoading, enabled = b.IsEffectivelyEnabled, spinner = spinner is null ? null : new { width = spinner.Bounds.Width, height = spinner.Bounds.Height, color = spinner.CurrentColor.ToString(), opacity = spinner.Opacity, matrix = spinner.RenderTransform?.Value.ToString() } }; }
                    var timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(250) };
                    timer.Tick += (_, _) =>
                    {
                        samples.Add(new { stage, rows = buttons.Select(Read).ToArray() });
                        if (stage == 0) { buttons[0].IsLoading = false; buttons[0].Content = "조회 범위 확인"; buttons[0].IconName = "arrow"; buttons[0].IsEnabled = true; }
                        else { timer.Stop(); File.WriteAllText(Path.Combine(output, "action-button-busy-window.json"), JsonSerializer.Serialize(new { utc = DateTime.UtcNow, samples, detachedSpinnerMatrix = firstSpinner.RenderTransform?.Value.ToString(), detached = firstSpinner.GetVisualParent() is null }, new JsonSerializerOptions { WriteIndented = true })); window.Title += " · completed"; }
                        stage++;
                    }; timer.Start();
                };
            }
            var clicks = 0;
            window.AddHandler(InputElement.KeyDownEvent, (_, e) =>
            {
                if (e.Key != Key.M) return;
                foreach (var b in buttons) b.ReducedMotion = true;
                window.Title = "Replica ActionButton verification · reduced";
                Evidence(output, "action-button-reduced.json", new { reduced = true, clicks }); e.Handled = true;
            }, RoutingStrategies.Tunnel);
            foreach (var button in buttons)
            {
                button.PropertyChanged += (_, change) =>
                {
                    if (change.Property != Button.IsPressedProperty && change.Property != InputElement.IsPointerOverProperty) return;
                    var timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(150) };
                    timer.Tick += (_, _) =>
                    {
                        timer.Stop();
                        var surface = button.GetVisualDescendants().OfType<WebSurfaceBox>().Single();
                        var label = button.GetVisualDescendants().OfType<WebSystemTextBlock>().Single();
                        Evidence(output, "action-button-input-state.json", new { utc = DateTime.UtcNow, name = button.Content, pressed = button.IsPressed,
                            hover = button.IsPointerOver, focused = button.IsFocused, enabled = button.IsEffectivelyEnabled,
                            bg = surface.Background?.ToString(), color = label.Foreground?.ToString(), border = surface.BorderBrush?.ToString(),
                            shadow = surface.BoxShadow.ToString(), scale = button.RenderTransform?.Value.M11, clicks });
                    };
                    timer.Start();
                };
                button.Click += (_, _) =>
                {
                    var clicked = ++clicks;
                    window.Title = "Replica ActionButton verification · clicked";
                    var samples = new List<object>();
                    var start = System.Diagnostics.Stopwatch.GetTimestamp();
                    var timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(16) };
                    timer.Tick += (_, _) =>
                    {
                        var elapsed = System.Diagnostics.Stopwatch.GetElapsedTime(start).TotalMilliseconds;
                        samples.Add(new { elapsed, opacity = button.Opacity, transform = button.RenderTransform?.Value.ToString(), pressed = button.IsPressed, focused = button.IsFocused });
                        if (elapsed < 240) return;
                        timer.Stop();
                        Evidence(output, "action-button-click.json", new { clicked, reduced = button.ReducedMotion, name = button.Content, samples });
                    };
                    timer.Start();
                };
            }
        }
        base.OnFrameworkInitializationCompleted();
    }
}
