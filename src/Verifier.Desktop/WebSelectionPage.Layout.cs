using Avalonia;
using Avalonia.Automation;
using Avalonia.Automation.Peers;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.VisualTree;

namespace Verifier.Desktop;

public sealed partial class WebSelectionPage
{
    private static WebSystemTextBlock Small(string text = "") => WebTypography.CreateCandidate(text, 12, 19.2, Brush.Parse("#b5bac1"));
    private sealed class PairLine : Panel
    {
        public WebSystemTextBlock Left { get; }
        public WebSystemTextBlock Right { get; } = Small();
        public PairLine(bool heading = false) { UseLayoutRounding = false; Left = heading ? WebTypography.CreateCandidate("", 18, 27, Brush.Parse("#f2f3f5"), 700, -.27) : Small(); if (heading) AutomationProperties.SetHeadingLevel(Left, 2); Children.Add(Left); Children.Add(Right); }
        protected override Size MeasureOverride(Size size)
        {
            var units = 64 * (TopLevel.GetTopLevel(this)?.RenderScaling ?? 2); Right.LineHeight = Math.Floor(19.2 * units) / units;
            if (Left.FontSize == 12) Left.LineHeight = Right.LineHeight;
            Right.Measure(Size.Infinity); Left.Measure(new(Math.Max(0, size.Width - Right.DesiredSize.Width - 16), double.PositiveInfinity));
            return new(size.Width, Math.Max(Left.DesiredSize.Height, Right.DesiredSize.Height));
        }
        protected override Size ArrangeOverride(Size size)
        {
            var units = 64 * (TopLevel.GetTopLevel(this)?.RenderScaling ?? 2); double Center(double h) => Math.Floor((size.Height - h) * units / 2) / units;
            Left.Arrange(new(0, Center(Left.DesiredSize.Height), Math.Ceiling(Left.DesiredSize.Width * units) / units, Left.DesiredSize.Height));
            var rightWidth = Math.Ceiling(Right.DesiredSize.Width * units) / units; Right.Arrange(new(size.Width - rightWidth, Center(Right.DesiredSize.Height), rightWidth, Right.DesiredSize.Height)); return size;
        }
    }
    private sealed class BulkChoicePanel : Panel
    {
        public WebCheckbox Checkbox { get; } = new() { IsThreeState = false };
        public WebSystemTextBlock Label { get; } = Small();
        private bool _armed;
        public event EventHandler? Requested;
        public BulkChoicePanel()
        {
            UseLayoutRounding = false; Background = Brushes.Transparent; Cursor = new(StandardCursorType.Hand); Children.Add(Checkbox); Children.Add(Label);
            Checkbox.Click += (_, _) => Requested?.Invoke(this, EventArgs.Empty);
            PointerPressed += (_, e) => { if (!e.GetCurrentPoint(this).Properties.IsLeftButtonPressed || e.Source == Checkbox || e.Source is Visual v && v.GetVisualAncestors().Contains(Checkbox)) return; TopLevel.GetTopLevel(this)?.FocusManager?.Focus(null, NavigationMethod.Pointer); _armed = true; e.Pointer.Capture(this); e.Handled = true; };
            PointerReleased += (_, e) => { if (!_armed || e.InitialPressMouseButton != MouseButton.Left) return; _armed = false; e.Pointer.Capture(null); e.Handled = true; if (!new Rect(Bounds.Size).Contains(e.GetPosition(this)) || !Checkbox.IsEffectivelyEnabled) return; Checkbox.Focus(NavigationMethod.Pointer); Checkbox.IsChecked = Checkbox.IsChecked != true; Requested?.Invoke(this, EventArgs.Empty); };
            PointerCaptureLost += (_, _) => _armed = false;
        }
        protected override Size MeasureOverride(Size size) { var units = 64 * (TopLevel.GetTopLevel(this)?.RenderScaling ?? 2); Label.LineHeight = Math.Floor(19.2 * units) / units; Label.Measure(Size.Infinity); Checkbox.Measure(new(20, 20)); return new(29 + Math.Ceiling(Label.DesiredSize.Width * units) / units, 44); }
        protected override Size ArrangeOverride(Size size) { var units = 64 * (TopLevel.GetTopLevel(this)?.RenderScaling ?? 2); Checkbox.Arrange(new(0, 12, 20, 20)); Label.Arrange(new(29, Math.Floor((44 - Label.DesiredSize.Height) * units / 2) / units, size.Width - 29, Label.DesiredSize.Height)); return size; }
    }
    private sealed class ToolbarPanel(WebSearchInput search, BulkChoicePanel bulk, WebActionButton deselect) : Panel
    {
        private double _searchWidth;
        private readonly List<(Control Control, Rect Box)> _boxes = [];
        public void Initialize() { UseLayoutRounding = false; Children.Add(search); Children.Add(bulk); Children.Add(deselect); }
        protected override Size MeasureOverride(Size size)
        {
            bulk.Measure(Size.Infinity); deselect.Measure(Size.Infinity);
            _searchWidth = Math.Clamp(size.Width - bulk.DesiredSize.Width - deselect.DesiredSize.Width - 32, 180, 380);
            search.Measure(new(_searchWidth, double.PositiveInfinity)); _boxes.Clear(); double x = 0, y = 0, h = 0;
            foreach (var c in Children) { var w = c == search ? _searchWidth : c.DesiredSize.Width; if (x > 0 && x + w > size.Width) { y += h + 16; x = h = 0; } _boxes.Add((c, new(x, y, w, c.DesiredSize.Height))); x += w + 16; h = Math.Max(h, c.DesiredSize.Height); }
            return new(size.Width, y + h);
        }
        protected override Size ArrangeOverride(Size size) { foreach (var pair in _boxes) pair.Control.Arrange(pair.Box); return size; }
    }
    private sealed class TableSection : Panel
    {
        public PairLine Heading { get; } = new(true);
        public WebSearchInput Search { get; } = new();
        public BulkChoicePanel Bulk { get; } = new();
        public WebActionButton Deselect { get; } = new() { Variant = "quiet", LabelFontSize = 12 };
        public ToolbarPanel Toolbar { get; }
        public PairLine Meta { get; } = new();
        public SelectionTable Table { get; }
        public WebCapabilityViewport Viewport { get; } = new();
        public Panel Shell { get; }
        public WebSystemTextBlock Caption { get; } = Small();
        private readonly WebSelectionPage _page;
        private WebMotionPlayback? _fade;
        public TableSection(WebSelectionPage page)
        {
            _page = page; UseLayoutRounding = false; Background = Brushes.Transparent;
            Heading.Left.Text = page.T("검사할 테이블과 대응", "Tables and matching"); Search.Context = WebSearchContext.Tables;
            Search.Placeholder = page.T("테이블 이름으로 검색", "Search by table name"); Search.AccessibleName = page.T("테이블 검색", "Search tables"); Search.ClearAccessibleName = page.T("테이블 검색 지우기", "Clear table search");
            Bulk.Label.Text = page.T("검색 결과 전체 선택", "Select all matching tables"); AutomationProperties.SetName(Bulk.Checkbox, Bulk.Label.Text);
            Deselect.Content = page.T("선택 해제", "Deselect matches"); AutomationProperties.SetAutomationId(Deselect, "Selection-clear-tables"); AutomationProperties.SetAutomationId(Bulk.Checkbox, "table-all"); AutomationProperties.SetAutomationId(Search.Editor, "table-search");
            Meta.Right.Text = page.T("검색은 선택 범위를 바꾸지 않습니다.", "Search does not change your selection.");
            Caption.Text = page.T("사용자·역할·소유자는 같은 이름으로 비교합니다. 수동 대응 후에도 이름·값·정의 차이를 보고합니다.", "Users, roles and owners are compared by the same name. Manual matching still reports name, value and definition differences.");
            Toolbar = new(Search, Bulk, Deselect); Toolbar.Initialize(); Table = new(page); Viewport.Content = Table; Shell = new TableShellPanel(Viewport, Table);
            Children.Add(Heading); Children.Add(Toolbar); Children.Add(Meta); Children.Add(Shell); Children.Add(Caption);
            Search.QueryChanged += (_, _) => { if (!page._updating) page.Publish(page.View with { Query = Search.Text }, "query"); };
            Search.ClearInvoked += (_, _) => { CancelFade(); _fade = WebMotion.Play(Table.Body, WebMotionKind.SearchFade, page.ReducedMotion); };
            Bulk.Requested += (_, _) => { if (!page._updating) page.Bulk(Bulk.Checkbox.IsChecked == true, "bulk"); };
            Deselect.Click += (_, _) => page.Bulk(false, "clear-tables");
        }
        public void CancelFade() { _fade?.Dispose(); _fade = null; }
        public void Sync(bool replaceRows)
        {
            Search.ReducedMotion = Bulk.Checkbox.ReducedMotion = Deselect.ReducedMotion = _page.ReducedMotion;
            if (Search.Text != _page.View.Query) Search.Text = _page.View.Query;
            var shown = _page.Shown; var count = shown.Count(_page.View.Tables.Contains);
            Heading.Right.Text = $"{_page.View.Tables.Count} / {_page.View.Maps.Count} " + _page.T("개 선택", "selected");
            Meta.Left.Text = $"{shown.Length} " + _page.T("개 표시 · 전체", "shown · of") + $" {_page.View.Maps.Count} " + _page.T("개", "tables");
            Bulk.Checkbox.IsChecked = count > 0 && count < shown.Length ? null : shown.Length > 0 && count == shown.Length; Bulk.Checkbox.IsEnabled = shown.Length > 0; Deselect.IsEnabled = count > 0;
            if (replaceRows) { CancelFade(); Table.ReplaceRows(shown); }
            foreach (var row in Table.Rows) { row.Selected = _page.View.Tables.Contains(row.SourceName); row.ReducedMotion = _page.ReducedMotion; }
            InvalidateMeasure();
        }
        protected override Size MeasureOverride(Size size)
        {
            foreach (var c in Children) c.Measure(new(size.Width, double.PositiveInfinity));
            var units = 64 * (TopLevel.GetTopLevel(this)?.RenderScaling ?? 2); Caption.LineHeight = Math.Floor(19.2 * units) / units; Caption.Measure(new(size.Width, double.PositiveInfinity));
            return new(size.Width, Heading.DesiredSize.Height + 12 + Toolbar.DesiredSize.Height + 12 + Meta.DesiredSize.Height + 10 + Shell.DesiredSize.Height + 12 + Caption.DesiredSize.Height);
        }
        protected override Size ArrangeOverride(Size size)
        {
            double y = 0; void Put(Control c, double gap) { c.Arrange(new(0, y, size.Width, c.DesiredSize.Height)); y += c.DesiredSize.Height + gap; }
            Put(Heading, 12); Put(Toolbar, 12); Put(Meta, 10); Put(Shell, 12); Put(Caption, 0); return size;
        }
    }
    private sealed class TableShellPanel : Panel
    {
        private readonly WebCapabilityViewport _viewport;
        private readonly SelectionTable _table;
        private readonly WebSurfaceBox _surface = WebSurface.Create(Brush.Parse("#2b2d31"), Brush.Parse("#41434a"), new(1), new(10));
        public TableShellPanel(WebCapabilityViewport viewport, SelectionTable table) { UseLayoutRounding = false; Background = Brushes.Transparent; _viewport = viewport; _table = table; _surface.IsHitTestVisible = false; AutomationProperties.SetAccessibilityView(_surface, AccessibilityView.Raw); Children.Add(_surface); Children.Add(viewport); }
        protected override Size MeasureOverride(Size size)
        {
            var inner = Math.Max(0, size.Width - 2); _table.Allocate(inner); _viewport.LogicalWidth = _table.Columns.Sum(); _viewport.OverflowWidth = _viewport.LogicalWidth;
            _viewport.Measure(new(inner, double.PositiveInfinity)); _surface.Measure(new(size.Width, _viewport.DesiredSize.Height + 2)); return new(size.Width, _viewport.DesiredSize.Height + 2);
        }
        protected override Size ArrangeOverride(Size size) { _surface.Arrange(new Rect(size)); _viewport.Arrange(new(1, 1, Math.Max(0, size.Width - 2), Math.Max(0, size.Height - 2))); _viewport.Clip = new RectangleGeometry(new Rect(_viewport.Bounds.Size), 9, 9); return size; }
        protected override AutomationPeer OnCreateAutomationPeer() => new ShellPeer(this);
        private sealed class ShellPeer(TableShellPanel owner) : ControlAutomationPeer(owner) { protected override bool IsControlElementCore() => false; protected override bool IsContentElementCore() => false; protected override IReadOnlyList<AutomationPeer>? GetChildrenCore() => [CreatePeerForElement(owner._viewport)]; }
    }
    private sealed class SelectionTable : Panel
    {
        public SelectionHeader Header { get; }
        public RowsPanel Body { get; } = new();
        public List<WebTableMappingRow> Rows { get; } = [];
        public double[] Columns { get; private set; } = [36, 36, 36, 36];
        public double[] Minima { get; private set; } = [36, 36, 36, 36];
        public double[] Maxima { get; private set; } = [36, 36, 36, 36];
        private readonly WebSelectionPage _page;
        private TablePeer? _peer;
        private double Units => 64 * (TopLevel.GetTopLevel(this)?.RenderScaling ?? 2);
        private double Ceil(double n) => Math.Ceiling(n * Units) / Units;
        private (double Min, double Max) Intrinsic(WebSystemTextBlock text)
        {
            text.Measure(Size.Infinity); var max = Ceil(text.SystemLineWidths.DefaultIfEmpty(text.DesiredSize.Width).Max());
            text.Measure(new(1 / Units, double.PositiveInfinity)); return (Ceil(text.SystemLineWidths.DefaultIfEmpty(text.DesiredSize.Width).Max()), max);
        }
        public SelectionTable(WebSelectionPage page) { _page = page; UseLayoutRounding = false; Header = new(page.View.English); Children.Add(Header); Children.Add(Body); }
        public void ReplaceRows(string[] shown)
        {
            Rows.Clear(); Body.Children.Clear();
            foreach (var name in shown)
            {
                var row = new WebTableMappingRow { SourceName = name, TargetName = _page.View.Maps[name], Manual = _page.View.Maps[name] != name, ReducedMotion = _page.ReducedMotion, BottomBorderShare = name == shown.Last() ? 0 : .5 };
                row.MethodText.Text = row.Manual ? _page.T("수동 · 이름 차이 유지", "Manual · Name difference retained") : _page.T("동일 이름", "Same name"); row.ChangeButton.Content = _page.T("대응 변경", "Change match");
                AutomationProperties.SetAutomationId(row.Checkbox, "Selection-table-" + name); AutomationProperties.SetAutomationId(row.ChangeButton, "Selection-mapping-" + name);
                row.SelectionChanged += (_, _) => { if (!_page._updating) _page.Publish(_page.View with { Tables = Toggle(_page.View.Tables, name, row.Selected) }, "table:" + name); };
                row.ChangeRequested += (_, _) => _page.ActionRequested?.Invoke(_page, "mapping:" + name);
                Rows.Add(row); Body.Children.Add(new MappingRowHost(row));
            }
            if (shown.Length == 0) Body.Children.Add(new EmptyRow(_page.T("검색된 테이블이 없습니다. 검색어를 바꿔 보세요.", "No tables match. Try a different search.")));
            _peer?.Refresh();
            InvalidateMeasure();
        }
        public void Allocate(double inner)
        {
            var min = new double[4]; var max = new double[4]; void Add(int i, double a, double b) { min[i] = Math.Max(min[i], a + 36); max[i] = Math.Max(max[i], b + 36); }
            for (var i = 0; i < 4; i++) { var t = Intrinsic(Header.Labels[i]); Add(i, t.Min, t.Max); }
            foreach (var row in Rows)
            {
                var source = Intrinsic(row.SourceText).Max; var target = Intrinsic(row.TargetText).Max; Add(0, source + 32, source + 32); Add(1, target, target);
                var method = Intrinsic(row.MethodText); Add(2, method.Min + 20, method.Max + 20);
                var label = WebTypography.CreateCandidate(row.ChangeButton.Content?.ToString() ?? "", 12, 19.2, weight: 600); var button = Intrinsic(label); Add(3, button.Min + 61, button.Max + 61);
            }
            // Empty colspan4 intrinsic constraints measured from the literal KO/EN sentence,
            // macOS default system-ui at DPR2; they are content constraints, independent of viewport.
            if (Rows.Count == 0 && (OperatingSystem.IsMacOS() || OperatingSystem.IsWindows()))
            {
                min = _page.View.English ? [77.5, 73.8828125, 90.9609375, 36] : [46.3828125, 46.3828125, 46.3828125, 36];
                max = _page.View.English ? [110.4921875, 106.875, 90.9609375, 36.0078125] : [93.3671875, 93.3671875, 82.7265625, 36.90625];
                if (OperatingSystem.IsWindows()) { min = _page.View.English ? [74.640625, 71.765625, 89.8828125, 36] : [48, 48, 48, 36]; max = _page.View.English ? [107.390625, 104.515625, 89.8828125, 36.0078125] : [106.3515625, 106.3515625, 93.6171875, 38.21875]; }
            }
            Minima = min; Maxima = max; var width = Math.Max(inner, min.Sum()); var minSum = min.Sum(); var maxSum = max.Sum(); var columns = new double[4];
            for (var i = 0; i < 3; i++) columns[i] = Math.Floor((width >= maxSum ? max[i] * width / maxSum : min[i] + (maxSum == minSum ? 0 : (width - minSum) * (max[i] - min[i]) / (maxSum - minSum))) * Units) / Units;
            columns[3] = width - columns.Take(3).Sum(); Columns = columns; Header.Columns = columns; foreach (var row in Rows) if (!row.ColumnWidths.SequenceEqual(columns)) row.ColumnWidths = columns;
        }
        protected override Size MeasureOverride(Size size) { Header.Measure(new(size.Width, double.PositiveInfinity)); Body.Measure(new(size.Width, double.PositiveInfinity)); return new(size.Width, Header.DesiredSize.Height + Body.DesiredSize.Height); }
        protected override Size ArrangeOverride(Size size) { Header.Arrange(new(0, 0, size.Width, Header.DesiredSize.Height)); Body.Arrange(new(0, Header.DesiredSize.Height, size.Width, Body.DesiredSize.Height)); return size; }
        protected override AutomationPeer OnCreateAutomationPeer() => _peer = new TablePeer(this);
        private sealed class TablePeer(SelectionTable owner) : ControlAutomationPeer(owner) { public void Refresh() => InvalidateChildren(); protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.Table; protected override string GetClassNameCore() => "Table"; protected override IReadOnlyList<AutomationPeer>? GetChildrenCore() => new[] { CreatePeerForElement(owner.Header) }.Concat(owner.Body.Children.Select(CreatePeerForElement)).ToArray(); }
    }
    private sealed class SelectionHeader : Panel
    {
        public WebSystemTextBlock[] Labels { get; }
        private double[] _columns = [36, 36, 36, 36];
        public double[] Columns { get => _columns; set { if (_columns.SequenceEqual(value)) return; _columns = value.ToArray(); InvalidateMeasure(); } }
        private readonly WebSurfaceBox _surface = WebSurface.Create(Brush.Parse("#232428"), Brush.Parse("#41434a"), new(0, 0, 0, .5), default);
        private readonly HeaderCell[] _cells;
        public SelectionHeader(bool english) { UseLayoutRounding = false; _surface.IsHitTestVisible = false; AutomationProperties.SetAccessibilityView(_surface, AccessibilityView.Raw); Children.Add(_surface); Labels = (english ? new[] { "Source table", "Target table", "Matching", "" } : ["원본 테이블", "대상 테이블", "대응 방식", ""]).Select(t => WebTypography.CreateCandidate(t, 12, 19.2, Brush.Parse("#b5bac1"), 600)).ToArray(); _cells = Labels.Select(t => new HeaderCell(t)).ToArray(); foreach (var c in _cells) Children.Add(c); }
        protected override Size MeasureOverride(Size size) { var units = 64 * (TopLevel.GetTopLevel(this)?.RenderScaling ?? 2); double h = 0; for (var i = 0; i < 4; i++) { Labels[i].LineHeight = Math.Floor(19.2 * units) / units; Labels[i].Measure(new(Math.Max(0, Columns[i] - 36), double.PositiveInfinity)); h = Math.Max(h, Labels[i].DesiredSize.Height); } foreach (var c in _cells) c.Measure(new(Columns[Array.IndexOf(_cells, c)], h + 24.5)); return new(Columns.Sum(), h + 24.5); }
        protected override Size ArrangeOverride(Size size) { _surface.Arrange(new Rect(size)); double x = 0; for (var i = 0; i < 4; i++) { _cells[i].Arrange(new(x, 0, Columns[i], size.Height)); x += Columns[i]; } return size; }
        protected override AutomationPeer OnCreateAutomationPeer() => new HeaderRowPeer(this);
        private sealed class HeaderRowPeer(SelectionHeader owner) : ControlAutomationPeer(owner) { protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.DataItem; protected override string GetClassNameCore() => "Row"; protected override string GetLocalizedControlTypeCore() => "row"; protected override IReadOnlyList<AutomationPeer>? GetChildrenCore() => owner._cells.Select(CreatePeerForElement).ToArray(); }
        private sealed class HeaderCell : Panel
        {
            public WebSystemTextBlock Label { get; }
            public HeaderCell(WebSystemTextBlock label) { UseLayoutRounding = false; Label = label; Children.Add(label); }
            protected override Size MeasureOverride(Size size) => size;
            protected override Size ArrangeOverride(Size size) { Label.Arrange(new(18, 12, Math.Max(0, size.Width - 36), Label.DesiredSize.Height)); return size; }
            protected override AutomationPeer OnCreateAutomationPeer() => new CellPeer(this);
            private sealed class CellPeer(HeaderCell owner) : ControlAutomationPeer(owner) { protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.DataItem; protected override string GetClassNameCore() => "Cell"; protected override string GetLocalizedControlTypeCore() => "cell"; protected override IReadOnlyList<AutomationPeer>? GetChildrenCore() => OperatingSystem.IsWindows() || string.IsNullOrEmpty(owner.Label.Text) ? [] : [CreatePeerForElement(owner.Label)]; protected override string? GetNameCore() => OperatingSystem.IsWindows() ? owner.Label.Text ?? "" : base.GetNameCore(); protected override bool IsControlElementCore() => !OperatingSystem.IsWindows() || !string.IsNullOrEmpty(owner.Label.Text); protected override bool IsContentElementCore() => !OperatingSystem.IsWindows() || !string.IsNullOrEmpty(owner.Label.Text); }
        }
    }
    private sealed class RowsPanel : Panel
    {
        public RowsPanel() { UseLayoutRounding = false; }
        protected override Size MeasureOverride(Size size) { foreach (var c in Children) c.Measure(new(size.Width, double.PositiveInfinity)); return new(size.Width, Children.Sum(x => x.DesiredSize.Height)); }
        protected override Size ArrangeOverride(Size size) { double y = 0; foreach (var c in Children) { c.Arrange(new(0, y, size.Width, c.DesiredSize.Height)); y += c.DesiredSize.Height; } return size; }
    }
    private sealed class EmptyRow : Panel
    {
        private readonly EmptyCell _cell;
        public EmptyRow(string text) { UseLayoutRounding = false; _cell = new(text); Children.Add(_cell); }
        protected override Size MeasureOverride(Size size) { _cell.Measure(size); return _cell.DesiredSize; }
        protected override Size ArrangeOverride(Size size) { _cell.Arrange(new Rect(size)); return size; }
        protected override AutomationPeer OnCreateAutomationPeer() => new RowPeer(this);
        private sealed class RowPeer(EmptyRow owner) : ControlAutomationPeer(owner) { protected override AutomationPeer? GetParentCore() => owner.GetVisualAncestors().OfType<SelectionTable>().Select(CreatePeerForElement).FirstOrDefault(); protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.DataItem; protected override string GetClassNameCore() => "Row"; protected override string GetLocalizedControlTypeCore() => "row"; protected override IReadOnlyList<AutomationPeer>? GetChildrenCore() => [CreatePeerForElement(owner._cell)]; }
        private sealed class EmptyCell : Panel
        {
        private readonly WebSystemTextBlock _text;
        private readonly WebSurfaceBox _surface = WebSurface.Create(Brushes.Transparent, Brush.Parse("#41434a"), new(0, .5, 0, 0), default);
        private bool _hover;
        private Color _from, _target, _reverse;
        private long _started;
        private double _duration, _factor = 1;
        private int _paintRevision;
        private double Progress => _duration <= 0 ? 1 : Math.Clamp(System.Diagnostics.Stopwatch.GetElapsedTime(_started).TotalMilliseconds / _duration, 0, 1);
        private Color Current {
            get { var t = WebMotion.CssEase().Ease(Progress); var alpha = _from.A + (_target.A - _from.A) * t; byte Round(double n) => (byte)Math.Clamp(Math.Round(n, MidpointRounding.AwayFromZero), 0, 255); byte C(byte a, byte b) => alpha == 0 ? (byte)0 : Round((a * _from.A * (1 - t) + b * _target.A * t) / alpha); return Color.FromArgb(Round(alpha), C(_from.R, _target.R), C(_from.G, _target.G), C(_from.B, _target.B)); }
        }
        private void Paint(bool hover) {
            if (_hover == hover) return; _hover = hover; var value = hover ? Color.Parse("#3a3c44") : Colors.Transparent; var revision = ++_paintRevision;
            if (this.GetVisualAncestors().OfType<WebSelectionPage>().FirstOrDefault()?.ReducedMotion != false || TopLevel.GetTopLevel(this) is not { } top) { _from = _target = _reverse = value; _duration = 0; _surface.Background = new SolidColorBrush(value); return; }
            var p = Progress; var current = Current; if (p < 1 && value == _reverse) { _factor = Math.Clamp(Math.Abs(WebMotion.CssEase().Ease(p) * _factor + 1 - _factor), 0, 1); _reverse = _target; } else { _factor = 1; _reverse = current; } _from = current; _target = value; _started = System.Diagnostics.Stopwatch.GetTimestamp(); _duration = 160 * _factor;
            void Frame(TimeSpan _) { if (revision != _paintRevision || !this.IsAttachedToVisualTree()) return; _surface.Background = new SolidColorBrush(Current); if (Progress < 1) top.RequestAnimationFrame(Frame); } top.RequestAnimationFrame(Frame);
        }
        public EmptyCell(string text) { UseLayoutRounding = false; Background = Brushes.Transparent; _surface.IsHitTestVisible = false; AutomationProperties.SetAccessibilityView(_surface, AccessibilityView.Raw); Children.Add(_surface); PointerEntered += (_, _) => Paint(true); PointerExited += (_, _) => Paint(false); DetachedFromVisualTree += (_, _) => ++_paintRevision; _text = WebTypography.CreateCandidate(text, 13, 20.8, Brush.Parse("#b5bac1")); _text.TextAlignment = TextAlignment.Center; Children.Add(_text); }
        protected override Size MeasureOverride(Size size) { var units = 64 * (TopLevel.GetTopLevel(this)?.RenderScaling ?? 2); _text.LineHeight = Math.Floor(20.8 * units) / units; _text.Measure(new(Math.Max(0, size.Width - 56), double.PositiveInfinity)); return new(size.Width, _text.DesiredSize.Height + 56.5); }
        protected override Size ArrangeOverride(Size size) { _surface.Arrange(new Rect(size)); _text.Arrange(new(28, 28.5, size.Width - 56, _text.DesiredSize.Height)); return size; }
        protected override AutomationPeer OnCreateAutomationPeer() => new CellPeer(this);
        private sealed class CellPeer(EmptyCell owner) : ControlAutomationPeer(owner) { protected override string? GetNameCore() => OperatingSystem.IsWindows() ? owner._text.Text ?? "" : base.GetNameCore(); protected override IReadOnlyList<AutomationPeer>? GetChildrenCore() => OperatingSystem.IsWindows() ? [] : [CreatePeerForElement(owner._text)]; protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.DataItem; protected override string GetClassNameCore() => "Cell"; protected override string GetLocalizedControlTypeCore() => "cell"; }
        }
    }
}
