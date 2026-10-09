using System.Text.Json;
using Avalonia;
using Avalonia.Automation;
using Avalonia.Automation.Peers;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Headless;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using Verifier.Desktop;

internal sealed class PreflightPageProbeApplication : Application
{
    private sealed record Case(string Id, double Width, double Viewport, bool English);
    public override void OnFrameworkInitializationCompleted()
    {
        var life = (IClassicDesktopStyleApplicationLifetime)ApplicationLifetime!;
        var args = life.Args!;
        var cases = JsonSerializer.Deserialize<Case[]>(File.ReadAllText(args[1]), new JsonSerializerOptions { PropertyNameCaseInsensitive = true })!;
        var output = args[2]; Directory.CreateDirectory(output);
        var page = new WebPreflightPage { ReducedMotion = true, Width = cases[0].Width };
        var footer = new WebActionFooter();
        var canvas = new Canvas { Width = 1500, Height = 1200, UseLayoutRounding = false, Background = Brush.Parse("#313338"), HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Left, VerticalAlignment = Avalonia.Layout.VerticalAlignment.Top };
        canvas.Children.Add(page); canvas.Children.Add(footer); Canvas.SetLeft(page, 40); Canvas.SetTop(page, 50); Canvas.SetTop(footer, 1030);
        var window = new Window { Title = "Replica PreflightPage verification", Width = 1540, Height = 1260, UseLayoutRounding = false, Content = canvas }; life.MainWindow = window;
        var requests = new List<string>(); page.ActionRequested += (_, action) => requests.Add(action); footer.ActionRequested += (_, action) => requests.Add(action);
        void Set(Case c) { page.Width = c.Width; page.SetPresentation(new(c.English), c.Viewport); footer.Width = c.Viewport - 220; footer.SetViewportWidth(c.Viewport); page.SetFooter(footer); window.UpdateLayout(); }
        object Box(Control c, Control? origin = null) { var p = c.TranslatePoint(default, origin ?? page) ?? throw new InvalidOperationException("Detached geometry"); return new { x = p.X, y = p.Y, width = c.Bounds.Width, height = c.Bounds.Height }; }
        object Text(WebSystemTextBlock c) => new { box = Box(c), text = c.Text, fontSize = c.FontSize, lineHeight = c.LineHeight, weight = (int)c.FontWeight, spacing = c.LetterSpacing, color = c.Foreground?.ToString(), lines = c.SystemLineTexts };
        object State(string id) => new { id, root = Box(page), heading = Box(page.Heading), title = Text(page.Heading.TitleText), description = Text(page.Heading.DescriptionText), tableShell = Box(page.Table), table = Box(page.Table.TableControl), notice = new { root = Box(page.WarningNotice), title = page.WarningNotice.Title, body = page.WarningNotice.Text, kind = page.WarningNotice.Kind }, optional = new { root = Box(page.OptionalSection), title = Text(page.OptionalTitle), body = Text(page.OptionalDescription) }, permission = new { box = Box(page.Table.Rows[2].PermissionsButton), text = page.Table.Rows[2].PermissionsButton.Content }, footer = new { root = Box(footer, footer), hint = new { box = Box(footer.Hint, footer), text = footer.Hint.Text }, back = new { box = Box(footer.BackButton!, footer), text = footer.BackButton!.Content }, forward = new { box = Box(footer.ActionButtons[0], footer), text = footer.ActionButtons[0].Content }, enabled = footer.ActionButtons[0].IsEnabled }, english = page.View.English, requests = requests.ToArray(), permissionFocused = page.Table.Rows[2].PermissionsButton.IsFocused, headingFocused = page.Heading.FocusTarget.IsFocused, opacity = page.Opacity, attached = page.Parent is not null, offset = page.Table.Viewport.Offset };
        if (args[0] == "--preflightpage-window")
        {
            var index = 0; var timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(50) };
            var motion = new List<object>();
            void Record() { window.UpdateLayout(); File.WriteAllText(Path.Combine(output, "preflightpage-input.json"), JsonSerializer.Serialize(State(cases[index].Id))); if (motion.Count < 512) { motion.Add(new { ticks = DateTime.UtcNow.Ticks, opacity = page.Opacity, attached = page.Parent is not null, reduced = page.ReducedMotion, english = page.View.English, translationY = page.RenderTransform?.Value.M32 ?? 0 }); File.WriteAllText(Path.Combine(output, "preflightpage-motion.json"), JsonSerializer.Serialize(motion)); } }
            void Utility(string name, double left, Action action) { var b = new Button { Content = name, Focusable = false, IsTabStop = false, MinHeight = 44 }; AutomationProperties.SetAutomationId(b, "PreflightPage-" + name); Canvas.SetLeft(b, left); b.Click += (_, _) => { action(); Dispatcher.UIThread.Post(Record, DispatcherPriority.Background); }; canvas.Children.Add(b); }
            Utility("next", 40, () => { index = (index + 1) % cases.Length; Set(cases[index]); });
            Utility("language", 180, () => { Set(cases[index] with { English = !page.View.English }); });
            Utility("mount", 330, () => { if (canvas.Children.Contains(page)) canvas.Children.Remove(page); else canvas.Children.Add(page); });
            Utility("reduced", 480, () => { page.ReducedMotion = !page.ReducedMotion; Set(cases[index] with { English = !page.View.English }); });
            Utility("focus-title", 630, () => page.Heading.FocusTitle(false));
            Utility("focus-permission", 790, () => page.Table.Rows[2].PermissionsButton.Focus(NavigationMethod.Tab));
            Canvas.SetTop(footer, 760);
            page.ReducedMotion = false; Set(cases[0]); timer.Tick += (_, _) => Record(); timer.Start(); window.Closed += (_, _) => timer.Stop();
        }
        else Dispatcher.UIThread.Post(async () =>
        {
            if (args.Contains("--optional-peer-only"))
            {
                var results = new List<object>();
                foreach (var english in new[] { false, true })
                {
                    Set(new("optional-peer", 904, 1180, english)); await Flush();
                    var titleHost = OperatingSystem.IsWindows() ? (Control)page.OptionalTitle.Parent! : page.OptionalTitle; var bodyHost = OperatingSystem.IsWindows() ? (Control)page.OptionalDescription.Parent! : page.OptionalDescription;
                    object Peer(Control host, WebSystemTextBlock text)
                    {
                        var peer = ControlAutomationPeer.CreatePeerForElement(host);
                        if (OperatingSystem.IsWindows() && (peer is null || peer.GetName() != text.Text || peer.GetAutomationControlType() != AutomationControlType.Text || peer.GetChildren().Count != 0)) throw new InvalidOperationException("Optional named leaf mismatch");
                        if (host.Bounds.Size != text.Bounds.Size) throw new InvalidOperationException("Optional host geometry changed");
                        return new { name = peer?.GetName(), role = peer?.GetAutomationControlType().ToString(), children = peer?.GetChildren().Count, host = Box(host), text = Text(text), headingLevel = AutomationProperties.GetHeadingLevel(host), promotedChildName = ControlAutomationPeer.CreatePeerForElement(text).GetName() };
                    }
                    results.Add(new { english, title = Peer(titleHost, page.OptionalTitle), body = Peer(bodyHost, page.OptionalDescription) });
                }
                File.WriteAllText(Path.Combine(output, "preflightpage-optional-peer-headless.json"), JsonSerializer.Serialize(results)); window.Close(); return;
            }
            if (args.Contains("--passive-only"))
            {
                Set(new("passive", 904, 1180, false)); await Flush(); page.Heading.FocusTitle(true); await Flush();
                var before = page.Heading.FocusTarget.IsFocused;
                var p = page.OptionalDescription.TranslatePoint(new Point(10, page.OptionalDescription.Bounds.Height / 2), window) ?? throw new InvalidOperationException("Missing optional hit point");
                window.MouseMove(p); window.MouseDown(p, MouseButton.Left); window.MouseUp(p, MouseButton.Left); await Flush();
                if (!before || page.Heading.FocusTarget.IsFocused || requests.Count != 0) throw new InvalidOperationException("Page optional passive blur mismatch");
                File.WriteAllText(Path.Combine(output, "preflightpage-passive-headless.json"), JsonSerializer.Serialize(new { before, after = page.Heading.FocusTarget.IsFocused, requests, point = p })); window.Close(); return;
            }
            var rows = new List<object>();
            foreach (var c in cases)
            {
                Set(c); await Flush(); rows.Add(State(c.Id));
                if (c.Id is "ko-960" or "en-960" or "en-1440")
                {
                    using Bitmap image = ReplicaPlatform.IsHeadless ? window.CaptureRenderedFrame() ?? throw new InvalidOperationException("Real frame absent") : new RenderTargetBitmap(new PixelSize(3080, 2520), new Vector(192, 192));
                    if (!ReplicaPlatform.IsHeadless) ((RenderTargetBitmap)image).Render(window);
                    image.Save(Path.Combine(output, "preflightpage-" + c.Id + ".png"), new PngBitmapEncoderOptions());
                }
            }
            File.WriteAllText(Path.Combine(output, "preflightpage-native.json"), JsonSerializer.Serialize(new { dpr = window.RenderScaling, rows }));
            if (ReplicaPlatform.IsHeadless && !args.Contains("--geometry-only"))
            {
                Set(new("input", 904, 1180, false)); await Flush();
                void Click(Control control) { var p = control.TranslatePoint(new Point(control.Bounds.Width / 2, control.Bounds.Height / 2), window) ?? throw new InvalidOperationException("Missing hit point"); window.MouseMove(p); window.MouseDown(p, MouseButton.Left); window.MouseUp(p, MouseButton.Left); }
                Click(page.Table.Rows[2].PermissionsButton); await Flush();
                page.Table.Rows[2].PermissionsButton.Focus(NavigationMethod.Tab); window.KeyPress(Key.Enter, RawInputModifiers.None, PhysicalKey.Enter, null); window.KeyRelease(Key.Enter, RawInputModifiers.None, PhysicalKey.Enter, null); await Flush();
                if (!requests.SequenceEqual(new[] { "permissions", "permissions" })) throw new InvalidOperationException("Permission dispatch mismatch");
                Set(new("language", 904, 1180, true)); await Flush();
                if (!page.Table.Rows[2].PermissionsButton.IsFocused || page.Table.Viewport.Offset != 0) throw new InvalidOperationException("Language identity restore mismatch");
                Click(footer.BackButton!); Click(footer.ActionButtons[0]); await Flush();
                if (!requests.SequenceEqual(new[] { "permissions", "permissions", "back", "choose" })) throw new InvalidOperationException("Footer dispatch mismatch");
                page.Heading.FocusTitle(true); Set(new("heading-language", 904, 1180, false)); await Flush(); if (!page.Heading.FocusTarget.IsFocused) throw new InvalidOperationException("Heading language restore mismatch");
                canvas.Children.Remove(page); await Flush(); canvas.Children.Add(page); await Flush();
                var h2 = ControlAutomationPeer.CreatePeerForElement(page.OptionalTitle);
                if (h2.GetHeadingLevel() != 2 || page.Opacity != 1 || page.Children.Count != 4) throw new InvalidOperationException("Lifecycle/parent hierarchy mismatch");
                File.WriteAllText(Path.Combine(output, "preflightpage-headless-input.json"), JsonSerializer.Serialize(new { requests, headingRestored = true, permissionRestored = true, remounted = true, optionalHeading = h2.GetHeadingLevel(), opacity = page.Opacity }));
            }
            window.Close(); Console.WriteLine("Recorded PreflightPage " + rows.Count);
        }, DispatcherPriority.Background);
        base.OnFrameworkInitializationCompleted();
    }
    private static async Task Flush() { for (var i = 0; i < 4; i++) { Dispatcher.UIThread.RunJobs(); await Task.Delay(16); } }
}
