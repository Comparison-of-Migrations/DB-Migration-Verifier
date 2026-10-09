using System.Text.Json;
using Avalonia;
using Avalonia.Automation;
using Avalonia.Automation.Peers;
using Avalonia.Automation.Provider;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Headless;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Verifier.Desktop;

internal sealed class SelectionPageProbeApplication : Application
{
    private sealed record Case(string Id, double Width, double Viewport, bool English, string Variant);
    public override void OnFrameworkInitializationCompleted()
    {
        var life = (IClassicDesktopStyleApplicationLifetime)ApplicationLifetime!; var args = life.Args!;
        var cases = JsonSerializer.Deserialize<Case[]>(File.ReadAllText(args[1]), new JsonSerializerOptions { PropertyNameCaseInsensitive = true })!;
        var output = args[2]; Directory.CreateDirectory(output);
        var page = new WebSelectionPage { ReducedMotion = true, Width = cases[0].Width }; var footer = new WebActionFooter();
        var canvas = new Canvas { Width = 1500, Height = 1800, UseLayoutRounding = false, Background = Brush.Parse("#313338"), HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Left, VerticalAlignment = Avalonia.Layout.VerticalAlignment.Top };
        canvas.Children.Add(page); canvas.Children.Add(footer); Canvas.SetLeft(page, 40); Canvas.SetTop(page, 50); Canvas.SetTop(footer, 1560);
        var window = new Window { Title = "Replica SelectionPage verification", Width = 1540, Height = 1860, UseLayoutRounding = false, Content = canvas }; life.MainWindow = window;
        var requests = new List<string>(); var changes = new List<object>(); var reasons = new List<string>();
        WebScrollViewport? inputScroll = null;
        page.ActionRequested += (_, action) => requests.Add(action); footer.ActionRequested += (_, action) => requests.Add(action);
        page.ViewChanged += (_, view) => { reasons.Add(page.LastChangeReason); changes.Add(new { reason = page.LastChangeReason, view.Categories, view.Tables, view.Expanded, view.Query }); page.SetFooter(footer); };
        void Set(Case c) { page.Width = c.Width; page.SetPresentation(Presentation(c), c.Viewport); footer.Width = c.Viewport - 220; footer.SetViewportWidth(c.Viewport); page.SetFooter(footer); window.UpdateLayout(); }
        object Box(Control c, Control? origin = null) { var p = c.TranslatePoint(default, origin ?? page) ?? throw new InvalidOperationException("Detached geometry"); return new { x = p.X, y = p.Y, width = c.Bounds.Width, height = c.Bounds.Height }; }
        object Text(WebSystemTextBlock c) => new { box = Box(c), text = c.Text, fontSize = c.FontSize, lineHeight = c.LineHeight, weight = (int)c.FontWeight, spacing = c.LetterSpacing, color = c.Foreground?.ToString(), lines = c.SystemLineTexts };
        object Check(WebCheckbox c) => new { box = Box(c), @checked = c.IsChecked == true, mixed = c.IsChecked is null, disabled = !c.IsEnabled };
        object Button(WebActionButton c) => new { box = Box(c), text = c.Content?.ToString(), disabled = !c.IsEnabled };
        object Diagnostic(Control control) {
            var peer = ControlAutomationPeer.CreatePeerForElement(control);
            var t = control.GetTransformedBounds();
            return new { type = control.GetType().Name, bounds = control.Bounds.ToString(), desired = control.DesiredSize.ToString(), visible = control.IsVisible, attached = control.IsAttachedToVisualTree(), transform = t?.Transform.ToString(), transformedBounds = t?.Bounds.ToString(), clip = t?.Clip.ToString(), peerBounds = peer.GetBoundingRectangle().ToString(), offscreen = peer.IsOffscreen() };
        }
        object State(string id) => new {
            id, empty = page.TableControl.GetVisualDescendants().OfType<WebSystemTextBlock>().Where(t => t.Text?.StartsWith("검색된 테이블이 없습니다.") == true || t.Text?.StartsWith("No tables match.") == true).Select(t => new { text = Text(t), cell = Box((Control)t.GetVisualParent()!), baseline = t.SystemBaseline, paint = ((Panel)t.GetVisualParent()!).Children.OfType<WebSurfaceBox>().First().Background?.ToString() }).ToArray(), tablePeerDiagnostic = inputScroll is null ? null : ControlAutomationPeer.CreatePeerForElement(page.TableControl).GetChildren().OfType<ControlAutomationPeer>().Select(p => Diagnostic(p.Owner)).ToArray(), diagnostic = inputScroll is null ? null : page.Rows.Select(r => new { row = Diagnostic(r), button = Diagnostic(r.ChangeButton), ancestors = r.ChangeButton.GetVisualAncestors().OfType<Control>().Select(Diagnostic).ToArray() }).ToArray(), root = Box(page), heading = Box(page.Heading), title = Text(page.Heading.TitleText), description = Text(page.Heading.DescriptionText), scope = Box(page.Scope),
            categories = page.Categories.Select(c => new { root = Box(c), head = Box(c.Header.Head), toggle = Box(c.Header.ToggleButton), checkbox = Check(c.Header.Checkbox), body = Box(c.Body), expanded = c.IsExpanded }).ToArray(),
            preparation = Box(page.Preparation), preparationIdentity = page.Preparation.Notice is null ? 0 : System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(page.Preparation.Notice), section = Box(page.Section), sectionHeading = Box(page.SectionHeading), sectionTitle = Text(page.SectionTitle), selectionCount = Text(page.SelectionCount),
            toolbar = Box(page.Toolbar), searchField = Box(page.Search), search = new { box = Box(page.Search.Editor), value = page.Search.Text, placeholder = page.Search.Placeholder, name = page.Search.AccessibleName }, clearSearch = Button(page.Search.ClearButton),
            bulk = Box(page.BulkChoice), bulkCheckbox = Check(page.BulkCheckbox), bulkLabel = Text(page.BulkLabel), deselect = Button(page.DeselectButton), meta = Box(page.Meta), searchCount = Text(page.SearchCount), searchHint = Text(page.SearchHint),
            tableShell = Box(page.TableShell), table = Box(page.TableControl), headers = page.HeaderLabels.Select((t, i) => new { box = new { x = page.TableControl.TranslatePoint(default, page)!.Value.X + page.ColumnWidths.Take(i).Sum(), y = page.TableControl.TranslatePoint(default, page)!.Value.Y, width = page.ColumnWidths[i], height = page.HeaderLabels.Max(x => x.Bounds.Height) + 24.5 }, text = t.Text }).ToArray(),
            rows = page.Rows.Select(r => new { root = Box(r), cells = r.ColumnWidths.Select((w, i) => new { x = r.TranslatePoint(default, page)!.Value.X + r.ColumnWidths.Take(i).Sum(), y = r.TranslatePoint(default, page)!.Value.Y, width = w, height = r.Bounds.Height }).ToArray(), checkbox = Check(r.Checkbox), change = Button(r.ChangeButton), source = Text(r.SourceText), target = Text(r.TargetText), method = Text(r.MethodText) }).ToArray(),
            caption = Text(page.Caption), scroll = inputScroll is null ? null : new { offsetY = inputScroll.Offset, extentHeight = inputScroll.ScrollHeight, viewportHeight = inputScroll.ClientHeight }, footer = new { root = Box(footer, footer), hint = new { box = Box(footer.Hint, footer), text = footer.Hint.Text }, back = new { box = Box(footer.BackButton!, footer), text = footer.BackButton!.Content }, forward = new { box = Box(footer.ActionButtons[0], footer), text = footer.ActionButtons[0].Content }, enabled = footer.ActionButtons[0].IsEnabled },
            intrinsic = new { min = page.MinimumColumns, max = page.MaximumColumns, columns = page.ColumnWidths }, selection = new { categories = page.View.Categories, tables = page.View.Tables, maps = page.View.Maps, query = page.View.Query, ready = page.View.TrialReady }, english = page.View.English, lastChangeReason = page.LastChangeReason, requests = requests.ToArray(), changes = changes.ToArray(), headingFocused = page.Heading.FocusTarget.IsFocused, searchFocused = page.Search.Editor.IsFocused, opacity = page.Opacity, attached = page.Parent is not null };
        if (args[0] == "--selectionpage-window")
        {
            window.Content = null;
            canvas.Children.Remove(footer); Canvas.SetTop(page, 10);
            canvas.Width = 984; canvas.Height = 1800;
            var tools = new Canvas { Height = 44, UseLayoutRounding = false };
            var scroll = new WebScrollViewport { Content = canvas };
            inputScroll = scroll;
            var inputRoot = new Grid { RowDefinitions = new("44,*,84"), Background = Brush.Parse("#313338") };
            inputRoot.Children.Add(tools); inputRoot.Children.Add(scroll); Grid.SetRow(scroll, 1); inputRoot.Children.Add(footer); Grid.SetRow(footer, 2);
            window.Content = inputRoot; window.Width = 1000; window.Height = 840; window.Position = new(20, 50);
            var index = 0; var timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(50) }; var motion = new List<object>();
            void Record() { window.UpdateLayout(); if (page.Parent is not null) { canvas.Height = page.Bounds.Height + 30; window.UpdateLayout(); } object state = page.Parent is not null ? State(cases[index].Id) : new { attached = false, english = page.View.English, requests = requests.ToArray(), reduced = page.ReducedMotion }; File.WriteAllText(Path.Combine(output, "selectionpage-input.json"), JsonSerializer.Serialize(state)); if (motion.Count < 512) { motion.Add(new { ticks = DateTime.UtcNow.Ticks, opacity = page.Opacity, attached = page.Parent is not null, reduced = page.ReducedMotion, english = page.View.English, translationY = page.RenderTransform?.Value.M32 ?? 0 }); File.WriteAllText(Path.Combine(output, "selectionpage-motion.json"), JsonSerializer.Serialize(motion)); } }
            void Utility(string name, double left, Action action) { var b = new Button { Content = name, Focusable = false, IsTabStop = false, MinHeight = 44 }; AutomationProperties.SetAutomationId(b, "SelectionPage-" + name); Canvas.SetLeft(b, left); b.Click += (_, _) => { action(); Dispatcher.UIThread.Post(Record, DispatcherPriority.Background); }; tools.Children.Add(b); }
            Utility("next", 40, () => { index = (index + 1) % cases.Length; Set(cases[index]); }); Utility("language", 180, () => { page.SetPresentation(page.View with { English = !page.View.English }, cases[index].Viewport); page.SetFooter(footer); window.UpdateLayout(); });
            Utility("mount", 330, () => { if (canvas.Children.Contains(page)) canvas.Children.Remove(page); else canvas.Children.Add(page); }); Utility("motion", 460, () => { page.ReducedMotion = !page.ReducedMotion; motion.Clear(); });
            Utility("focus-search", 570, () => page.Search.Editor.Focus(NavigationMethod.Tab)); Utility("focus-trial", 740, () => page.Categories[4].Header.Checkbox.Focus(NavigationMethod.Tab));
            Utility("scroll-top", 890, () => scroll.Offset = default);
            Utility("scroll-table", 890, () => scroll.Offset = Math.Max(0, page.Search.TranslatePoint(default, page)!.Value.Y - 100));
            Utility("scroll-bottom", 890, () => scroll.Offset = scroll.MaximumOffset);
            Set(cases[0]);
            if (args.Contains("--resume-after-bulk") || args.Contains("--resume-after-clear") || args.Contains("--resume-empty")) { page.SetPresentation(page.View with { Categories = ["schema", "access", "objects"], Expanded = ["data"], Tables = ["demo.customers", "demo.line_items", "demo.orders"], Query = args.Contains("--resume-empty") ? "missing-table" : args.Contains("--resume-after-clear") ? "" : "orders" }, cases[0].Viewport); page.SetFooter(footer); }
            window.Show(); timer.Tick += (_, _) => Record(); timer.Start(); window.Closed += (_, _) => timer.Stop(); base.OnFrameworkInitializationCompleted(); return;
        }
        window.Show();
        Dispatcher.UIThread.Post(async () => {
            try {
                if (args.Contains("--reason-only")) {
                    var states = new List<object>();
                    foreach (var english in new[] { false, true }) {
                        Set(new(english ? "en-reasons" : "ko-reasons", 904, 1180, english, "default")); await Flush(); reasons.Clear();
                        void ClickAt(Control control, Point point) { var p = control.TranslatePoint(point, window)!.Value; window.MouseMove(p); window.MouseDown(p, MouseButton.Left); window.MouseUp(p, MouseButton.Left); }
                        void Click(Control control) => ClickAt(control, new(control.Bounds.Width / 2, control.Bounds.Height / 2));
                        async Task Expect(string reason, Action action) { var before = reasons.Count; action(); await Flush(); if (reasons.Count != before + 1 || reasons.Last() != reason || page.LastChangeReason != reason) throw new InvalidOperationException("Metadata callback missing/duplicate/wrong reason " + reason); states.Add(new { english, reason, count = reasons.Count, tables = page.View.Tables, categories = page.View.Categories, query = page.View.Query }); }
                        await Expect("bulk", () => ClickAt(page.BulkChoice, new(40, 22)));
                        await Expect("bulk", () => Click(page.BulkCheckbox));
                        await Expect("clear-tables", () => Click(page.DeselectButton));
                        await Expect("category:data", () => Click(page.Categories[0].Header.Checkbox));
                        await Expect("expand:data", () => Click(page.Categories[0].Header.ToggleButton));
                        await Expect("query", () => page.Search.Text = "orders");
                        await Expect("table:demo.orders", () => ControlAutomationPeer.CreatePeerForElement(page.Rows[0].Checkbox).GetProvider<IToggleProvider>()!.Toggle());
                        await Expect("query", () => Click(page.Search.ClearButton));
                        var count = reasons.Count; var last = page.LastChangeReason; page.SetPresentation(page.View, 1180); await Flush(); if (reasons.Count != count || page.LastChangeReason != last) throw new InvalidOperationException("Presentation echo invented metadata callback");
                    }
                    File.WriteAllText(Path.Combine(output, "selectionpage-reasons.json"), JsonSerializer.Serialize(states)); Console.WriteLine("Recorded source change reasons KOEN16; external echo silent"); return;
                }
                if (args.Contains("--projected-only")) {
                    var states = new List<object>();
                    foreach (var english in new[] { false, true }) {
                        Set(new(english ? "en-projected" : "ko-projected", 904, 1180, english, "default")); await Flush();
                        var tablePeer = ControlAutomationPeer.CreatePeerForElement(page.TableControl); var rowPeer = tablePeer.GetChildren()[1]; var cells = rowPeer.GetChildren();
                        var checkPeer = cells[0].GetChildren()[0]; var buttonPeer = cells[3].GetChildren()[0];
                        if (!ReferenceEquals(checkPeer, ControlAutomationPeer.CreatePeerForElement(page.Rows[0].Checkbox)) || !ReferenceEquals(buttonPeer, ControlAutomationPeer.CreatePeerForElement(page.Rows[0].ChangeButton))) throw new InvalidOperationException("Root focused control provider identity differs from logical child");
                        if (!ReferenceEquals(rowPeer.GetParent(), tablePeer) || !ReferenceEquals(checkPeer.GetParent(), cells[0]) || !ReferenceEquals(buttonPeer.GetParent(), cells[3])) throw new InvalidOperationException("Projected parent chain differs from declared children");
                        var toggle = checkPeer.GetProvider<IToggleProvider>() ?? throw new InvalidOperationException("Original Toggle provider lost"); var invoke = buttonPeer.GetProvider<IInvokeProvider>() ?? throw new InvalidOperationException("Original Invoke provider lost");
                        var events = 0; checkPeer.PropertyChanged += (_, e) => { if (e.Property == TogglePatternIdentifiers.ToggleStateProperty) events++; };
                        var source = page.Rows[0].SourceName; var before = requests.Count; toggle.Toggle(); invoke.Invoke(); await Flush();
                        if (page.View.Tables.Contains(source) || requests.Count != before + 1 || requests.Last() != "mapping:" + source || events == 0) throw new InvalidOperationException("Projected provider callback/state/notification absent");
                        states.Add(new { english, parent = true, name = checkPeer.GetName(), buttonName = buttonPeer.GetName(), checkType = checkPeer.GetAutomationControlType().ToString(), buttonType = buttonPeer.GetAutomationControlType().ToString(), checkChildren = checkPeer.GetChildren().Count, buttonChildren = buttonPeer.GetChildren().Count, notificationCount = events, toggleState = toggle.ToggleState.ToString(), request = requests.Last(), originalOwner = ReferenceEquals(((ControlAutomationPeer)checkPeer).Owner, page.Rows[0].Checkbox) });
                    }
                    File.WriteAllText(Path.Combine(output, "selectionpage-projected.json"), JsonSerializer.Serialize(states)); Console.WriteLine("Recorded projected original providers2"); return;
                }
                if (args.Contains("--empty-only")) {
                    var states = new List<object>();
                    foreach (var english in new[] { false, true }) {
                        Set(new(english ? "en-empty" : "ko-empty", 904, 1180, english, "query-none")); await Flush(); states.Add(State("empty-before"));
                        if (Environment.GetEnvironmentVariable("DBMV_HEADLESS") == "1") {
                            var text = page.TableControl.GetVisualDescendants().OfType<WebSystemTextBlock>().First(t => t.Text?.StartsWith("검색된 테이블이 없습니다.") == true || t.Text?.StartsWith("No tables match.") == true);
                            var cell = (Control)text.GetVisualParent()!; window.MouseMove(cell.TranslatePoint(new(80, cell.Bounds.Height / 2), window)!.Value); await Flush(); states.Add(State("empty-hover"));
                            window.CaptureRenderedFrame()?.Save(Path.Combine(output, "selectionpage-empty-" + (english ? "en" : "ko") + ".png"), new PngBitmapEncoderOptions());
                            window.MouseMove(new(4, 4)); await Flush(); states.Add(State("empty-leave"));
                        }
                    }
                    File.WriteAllText(Path.Combine(output, "selectionpage-empty.json"), JsonSerializer.Serialize(states)); Console.WriteLine("Recorded Selection empty scope2"); return;
                }
                if (args.Contains("--preparation-only"))
                {
                    var states = new List<object>();
                    foreach (var english in new[] { false, true })
                    {
                        Set(new(english ? "en-preparation" : "ko-preparation", 904, 1180, english, "trial-unready")); await Flush();
                        var before = page.Preparation.Notice!; var button = page.Preparation.PrepareButton!;
                        var category = page.Categories[1].Header.Checkbox;
                        var p = category.TranslatePoint(new(category.Bounds.Width / 2, category.Bounds.Height / 2), window)!.Value;
                        window.MouseMove(p); window.MouseDown(p, MouseButton.Left); window.MouseUp(p, MouseButton.Left); await Flush();
                        if (ReferenceEquals(before, page.Preparation.Notice) || before.Parent is not null || ReferenceEquals(button, page.Preparation.PrepareButton)) throw new InvalidOperationException("Category change retained preparation children");
                        if (!category.IsFocused || !page.View.Categories.Contains("trial")) throw new InvalidOperationException("Preparation remount changed category focus/selection");
                        var after = page.Preparation.Notice;
                        page.SetPresentation(page.View, 1180); await Flush();
                        if (!ReferenceEquals(after, page.Preparation.Notice)) throw new InvalidOperationException("Application echo replaced preparation twice");
                        page.Search.Text = "orders"; await Flush();
                        if (!ReferenceEquals(after, page.Preparation.Notice)) throw new InvalidOperationException("Query unexpectedly replaced preparation");
                        states.Add(new { english, replaced = true, oldDetached = before.Parent is null, echoPreserved = true, queryPreserved = true, categoryFocusedBeforeQuery = true, categories = page.View.Categories });
                    }
                    File.WriteAllText(Path.Combine(output, "selectionpage-preparation.json"), JsonSerializer.Serialize(states)); Console.WriteLine("Recorded SelectionPage preparation2"); return;
                }
                if (args.Contains("--peer-only"))
                {
                    var trees = new List<object>();
                    object Peer(AutomationPeer peer, int depth) => new { name = peer.GetName(), type = peer.GetAutomationControlType().ToString(), @class = peer.GetClassName(), localized = peer.GetLocalizedControlType(), control = peer.IsControlElement(), content = peer.IsContentElement(), children = depth < 8 ? peer.GetChildren()?.Select(c => Peer(c, depth + 1)).ToArray() : [] };
                    foreach (var english in new[] { false, true })
                    {
                        Set(new(english ? "en-peer" : "ko-peer", 904, 1180, english, "default")); await Flush();
                        var tablePeer = ControlAutomationPeer.CreatePeerForElement(page.TableControl); var rowsPeer = tablePeer.GetChildren()!;
                        if (rowsPeer.Count > 1 && !ReferenceEquals(rowsPeer[1].GetParent(), tablePeer)) throw new InvalidOperationException("Logical table row parent differs from Table children");
                        if (tablePeer.GetAutomationControlType() != AutomationControlType.Table) throw new InvalidOperationException("Table peer absent");
                        if (OperatingSystem.IsMacOS() && (rowsPeer.Count != 4 || rowsPeer.Any(r => r.GetChildren()?.Count != 4))) throw new InvalidOperationException("Mac primary row/cell tree absent");
                        trees.Add(new { english, tree = Peer(tablePeer, 0), searchName = ControlAutomationPeer.CreatePeerForElement(page.Search.Editor).GetName() });
                        if (OperatingSystem.IsMacOS())
                        {
                            foreach (var query in new[] { "orders", "missing-table", "" })
                            {
                                page.Search.Text = query; await Flush(); var currentRows = tablePeer.GetChildren()!;
                                var expectedRows = query.Length == 0 ? 4 : 2;
                                if (currentRows.Count != expectedRows) throw new InvalidOperationException("Stale table AX row cache after query " + query);
                                if (query == "missing-table" && currentRows[1].GetChildren()?.Count != 1) throw new InvalidOperationException("Empty colspan cell missing");
                                trees.Add(new { english, query, tree = Peer(tablePeer, 0) });
                            }
                        }
                    }
                    File.WriteAllText(Path.Combine(output, "selectionpage-peer.json"), JsonSerializer.Serialize(trees)); Console.WriteLine("Recorded SelectionPage peer2"); return;
                }
                var rows = new List<object>();
                foreach (var c in cases) { Set(c); await Flush(); rows.Add(State(c.Id)); }
                File.WriteAllText(Path.Combine(output, "selectionpage-layout.json"), JsonSerializer.Serialize(new { dpr = window.RenderScaling, rows }));
                Set(cases.FirstOrDefault(c => c.Variant == "default" && !c.English) ?? cases[0]); await Flush();
                if (Environment.GetEnvironmentVariable("DBMV_HEADLESS") == "1") window.CaptureRenderedFrame()?.Save(Path.Combine(output, "selectionpage-rendered.png"), new PngBitmapEncoderOptions());
                else { using var bitmap = new RenderTargetBitmap(new(1540 * 2, 1860 * 2), new(192, 192)); bitmap.Render(canvas); bitmap.Save(Path.Combine(output, "selectionpage-rendered.png"), new PngBitmapEncoderOptions()); }
                if (args.Contains("--geometry-only")) { Console.WriteLine("Recorded SelectionPage geometry " + rows.Count); return; }
                var lifeRows = new List<object>();
                Set(cases[0]); await Flush(); lifeRows.Add(State("initial")); page.Search.Editor.Focus(NavigationMethod.Tab);
                page.SetPresentation(page.View with { English = true }, cases[0].Viewport); await Flush(); if (!page.Search.Editor.IsFocused) throw new InvalidOperationException("Language focus restoration missing"); lifeRows.Add(State("language-focused"));
                canvas.Children.Remove(page); await Flush(); canvas.Children.Add(page); await Flush(); lifeRows.Add(State("remount"));
                File.WriteAllText(Path.Combine(output, "selectionpage-lifecycle.json"), JsonSerializer.Serialize(lifeRows));
                if (Environment.GetEnvironmentVariable("DBMV_HEADLESS") == "1")
                {
                    Set(cases[0]); await Flush();
                    void Click(Control c) { var p = c.TranslatePoint(new(c.Bounds.Width / 2, c.Bounds.Height / 2), window)!.Value; window.MouseMove(p); window.MouseDown(p, MouseButton.Left); window.MouseUp(p, MouseButton.Left); }
                    var input = new List<object>();
                    Click(page.Categories[0].Header.ToggleButton); await Flush(); if (!page.View.Expanded.Contains("data") || !page.View.Categories.Contains("data")) throw new InvalidOperationException("Expansion changed selection"); input.Add(State("expand-independent"));
                    Click(page.Categories[0].Header.Checkbox); await Flush(); if (page.View.Categories.Contains("data") || !page.View.Expanded.Contains("data")) throw new InvalidOperationException("Category changed expansion"); input.Add(State("category-independent"));
                    page.Search.Editor.Focus(NavigationMethod.Tab); window.KeyTextInput("orders"); await Flush(); if (page.Rows.Count != 1 || page.View.Tables.Count != 3) throw new InvalidOperationException("Filter changed selection"); input.Add(State("query-preserves-selection"));
                    Click(page.DeselectButton); await Flush(); if (page.View.Tables.Contains("demo.orders") || page.View.Tables.Count != 2) throw new InvalidOperationException("Deselect removed hidden rows"); input.Add(State("deselect-shown-only"));
                    Click(page.BulkChoice); await Flush(); if (!page.View.Tables.Contains("demo.orders")) throw new InvalidOperationException("Bulk label missing activation"); input.Add(State("bulk-shown-only"));
                    Click(page.Rows[0].ChangeButton); await Flush(); if (requests.LastOrDefault() != "mapping:demo.orders") throw new InvalidOperationException("Mapping identity missing"); input.Add(State("mapping-request"));
                    Click(page.Search.ClearButton); await Flush(); if (page.View.Query != "" || !page.Search.Editor.IsFocused || page.Rows.Count != 3) throw new InvalidOperationException("Clear query/focus mismatch"); input.Add(State("clear-restores-editor"));
                    page.Search.Editor.Focus(NavigationMethod.Tab); window.KeyTextInput("missing-table"); await Flush(); if (page.BulkCheckbox.IsEnabled || page.DeselectButton.IsEnabled || page.View.Tables.Count != 3) throw new InvalidOperationException("Empty filter state mismatch"); input.Add(State("empty-filter"));
                    window.KeyPress(Key.Escape, RawInputModifiers.None, PhysicalKey.Escape, null); window.KeyRelease(Key.Escape, RawInputModifiers.None, PhysicalKey.Escape, null); await Flush(); if (page.View.Query != "" || !page.Search.Editor.IsFocused) throw new InvalidOperationException("Search Escape mismatch"); input.Add(State("escape-clear"));
                    page.Heading.FocusTitle(true); Click(page.Caption); await Flush(); if (page.Heading.FocusTarget.IsFocused || requests.Count != 1) throw new InvalidOperationException("Passive caption invoked or retained focus"); input.Add(State("passive-caption"));
                    File.WriteAllText(Path.Combine(output, "selectionpage-headless-input.json"), JsonSerializer.Serialize(input));
                }
                Console.WriteLine("Recorded SelectionPage " + rows.Count);
            } catch (Exception error) { File.WriteAllText(Path.Combine(output, "selectionpage-failure.json"), JsonSerializer.Serialize(new { error = error.ToString() })); Console.Error.WriteLine(error); Environment.ExitCode = 1; }
            finally { window.Close(); }
        }, DispatcherPriority.Background);
        base.OnFrameworkInitializationCompleted();
    }
    private static async Task Flush() { for (var i = 0; i < 5; i++) { Dispatcher.UIThread.RunJobs(); await Task.Delay(16); } }
    private static WebSelectionView Presentation(Case c)
    {
        var categories = new List<string> { "data", "schema", "access", "objects" }; var tables = new List<string> { "demo.customers", "demo.orders", "demo.line_items" }; var maps = tables.ToDictionary(x => x, x => x); var expanded = new List<string>(); var query = "";
        if (c.Variant == "expanded") expanded.AddRange([.. categories, "trial"]);
        if (c.Variant is "trial-unready" or "trial-ready") categories.Add("trial"); if (c.Variant == "no-categories") categories.Clear();
        if (c.Variant is "no-tables" or "access-only-no-tables") { tables.Clear(); if (c.Variant == "access-only-no-tables") categories = ["access"]; }
        if (c.Variant == "partial-tables") tables = ["demo.customers"]; if (c.Variant == "query-orders") query = "orders"; if (c.Variant == "query-none") query = "missing-table";
        if (c.Variant == "manual-target-query") { maps["demo.orders"] = "archive.orders"; query = "archive"; }
        return new(c.English, categories, tables, maps, expanded, query, c.Variant == "trial-ready", "SQL Server", "migration_test", "trial.example.invalid");
    }
}
