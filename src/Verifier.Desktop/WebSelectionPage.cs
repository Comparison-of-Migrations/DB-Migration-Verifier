using Avalonia;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Threading;
using Avalonia.VisualTree;

namespace Verifier.Desktop;

public sealed record WebSelectionView(bool English, IReadOnlyList<string> Categories, IReadOnlyList<string> Tables,
    IReadOnlyDictionary<string, string> Maps, IReadOnlyList<string> Expanded, string Query, bool TrialReady,
    string Dbms, string TrialDatabase, string TrialHost, string Error = "");

/// <summary>Literal select() parent. The application owns navigation, the matching modal and announcements.</summary>
public sealed partial class WebSelectionPage : Panel
{
    public WebSelectionView View { get; private set; } = new(false, ["data", "schema", "access", "objects"],
        ["demo.customers", "demo.orders", "demo.line_items"], new Dictionary<string, string> {
            ["demo.customers"] = "demo.customers", ["demo.orders"] = "demo.orders", ["demo.line_items"] = "demo.line_items" }, [], "", false, "SQL Server", "migration_test", "trial.example.invalid");
    public WebPageHeading Heading { get; private set; } = new();
    public IReadOnlyList<WebCategorySection> Categories => _categories;
    public WebTrialPreparation Preparation { get; private set; } = new();
    public WebSearchInput Search => _section.Search;
    public WebCheckbox BulkCheckbox => _section.Bulk.Checkbox;
    public WebActionButton DeselectButton => _section.Deselect;
    public Control Scope => _scope;
    public Control Section => _section;
    public Control SectionHeading => _section.Heading;
    public WebSystemTextBlock SectionTitle => _section.Heading.Left;
    public WebSystemTextBlock SelectionCount => _section.Heading.Right;
    public Control Toolbar => _section.Toolbar;
    public Control BulkChoice => _section.Bulk;
    public WebSystemTextBlock BulkLabel => _section.Bulk.Label;
    public Control Meta => _section.Meta;
    public WebSystemTextBlock SearchCount => _section.Meta.Left;
    public WebSystemTextBlock SearchHint => _section.Meta.Right;
    public Control TableShell => _section.Shell;
    public Control TableControl => _section.Table;
    public IReadOnlyList<WebSystemTextBlock> HeaderLabels => _section.Table.Header.Labels;
    public IReadOnlyList<WebTableMappingRow> Rows => _section.Table.Rows;
    public WebSystemTextBlock Caption => _section.Caption;
    public double[] ColumnWidths => _section.Table.Columns.ToArray();
    public double[] MinimumColumns => _section.Table.Minima.ToArray();
    public double[] MaximumColumns => _section.Table.Maxima.ToArray();
    public WebCapabilityViewport Viewport => _section.Viewport;
    public bool ReducedMotion { get; set; }
    public string LastChangeReason { get; private set; } = "";
    public event EventHandler<WebSelectionView>? ViewChanged;
    public event EventHandler<string>? ActionRequested;
    private readonly List<WebCategorySection> _categories = [];
    private PairLine _scope = new();
    private TableSection _section = null!;
    private WebNotice? _error;
    private bool _initialized, _updating, _attached, _keyboard, _replacePreparation;
    private long _revision;
    private WebMotionPlayback? _entry;
    private double _viewportWidth = 1280;
    private string T(string ko, string en) => View.English ? en : ko;

    public WebSelectionPage()
    {
        UseLayoutRounding = false; Background = Brushes.Transparent;
        AddHandler(GotFocusEvent, (_, e) => _keyboard = e.NavigationMethod != NavigationMethod.Pointer, RoutingStrategies.Bubble, true);
        AddHandler(PointerPressedEvent, (_, e) => {
            if (!e.GetCurrentPoint(this).Properties.IsLeftButtonPressed || e.Source is not Visual visual) return;
            if (visual.GetVisualAncestors().Prepend(visual).OfType<InputElement>().TakeWhile(x => x != this)
                .Any(x => x is WebActionButton or WebCheckbox or WebTextInput or WebCategoryHeader.CategoryButton)) return;
            TopLevel.GetTopLevel(this)?.FocusManager?.Focus(null, NavigationMethod.Pointer);
        }, RoutingStrategies.Tunnel, true);
        SetPresentation(View, _viewportWidth);
    }
    public void SetPresentation(WebSelectionView view, double viewportWidth)
    {
        _viewportWidth = viewportWidth;
        var old = View;
        if (!_initialized || old.English != view.English)
        {
            var focus = CaptureFocus(); View = view; _updating = true;
            Children.Clear(); _categories.Clear();
            Heading = new(); Heading.SetPresentation(T("무엇을 검사할지 선택하세요", "Choose what to verify"),
                T("읽을 수 있는 비교 분류는 기본 선택합니다. 분류 안의 세부 항목도 검사 대상으로 유지합니다.", "Readable comparison categories are selected by default, including their detailed checks."));
            _scope = new(); _scope.Left.Text = T("검사 분류", "Check categories");
            Children.Add(Heading); Children.Add(_scope);
            foreach (var d in Definitions)
            {
                var category = new WebCategorySection { ReducedMotion = ReducedMotion };
                category.Header.Title = d.Name[view.English ? 1 : 0]; category.Header.Description = d.Description[view.English ? 1 : 0];
                category.Header.IconName = d.Icon; category.Header.SelectionLabel = category.Header.Title + T(" 검사 선택", " check selection");
                category.SetDetails(view.English ? d.EnglishDetails : d.Details);
                category.SelectionRequested += (_, selected) => { if (_updating) return; Publish(View with { Categories = Toggle(View.Categories, d.Key, selected), Error = "" }, "category:" + d.Key, true); };
                category.ExpansionRequested += (_, expanded) => { if (_updating) return; Publish(View with { Expanded = Toggle(View.Expanded, d.Key, expanded) }, "expand:" + d.Key); };
                AutomationProperties.SetAutomationId(category, "Selection-category-" + d.Key);
                _categories.Add(category); Children.Add(category);
            }
            Preparation = new() { ReducedMotion = ReducedMotion };
            Preparation.PreparationRequested += (_, _) => ActionRequested?.Invoke(this, "trial");
            _section = new(this); Children.Add(Preparation); Children.Add(_section);
            _initialized = true; Sync(old, true); _updating = false;
            if (_attached) { PlayEntry(); RestoreFocus(focus); }
        }
        else
        {
            View = view; _updating = true; Sync(old, false); _updating = false;
        }
        Preparation.ViewportWidth = viewportWidth; InvalidateMeasure();
    }
    private void Sync(WebSelectionView old, bool rebuild)
    {
        _scope.Right.Text = $"{View.Categories.Count} / 5 " + T("선택", "selected");
        for (var i = 0; i < Definitions.Length; i++)
        {
            var d = Definitions[i]; var c = _categories[i]; c.ReducedMotion = ReducedMotion;
            c.IsSelected = View.Categories.Contains(d.Key); c.IsExpanded = View.Expanded.Contains(d.Key);
            c.Header.StatusText = d.Key == "trial" ? (View.TrialReady ? T("예시 준비됨", "Sample ready") : T("별도 DB 준비 필요", "Test DB required"))
                : d.Key == "access" ? T("일부 가능", "Partial") : T("가능", "Available");
            c.Header.StatusKind = d.Key == "trial" ? (View.TrialReady ? "ok" : "warn") : d.Key == "access" ? "warn" : "ok";
        }
        Preparation.ReducedMotion = ReducedMotion;
        if (rebuild || _replacePreparation || old.TrialReady != View.TrialReady || old.Categories.Contains("trial") != View.Categories.Contains("trial") || old.Dbms != View.Dbms || old.TrialDatabase != View.TrialDatabase || old.TrialHost != View.TrialHost)
            Preparation.SetPresentation(View.Categories.Contains("trial"), View.TrialReady, View.Dbms, View.TrialDatabase, View.TrialHost, View.English);
        _section.Sync(rebuild || old.Query != View.Query || !old.Maps.SequenceEqual(View.Maps));
        if (rebuild || old.Error != View.Error)
        {
            if (_error is not null) Children.Remove(_error); _error = null;
            if (View.Error.Length > 0) { _error = new() { Kind = "error", Title = T("선택을 확인하세요", "Check your selection"), Text = View.Error, ReducedMotion = ReducedMotion }; Children.Add(_error); }
        }
    }
    private void Publish(WebSelectionView view, string reason, bool replacePreparation = false)
    {
        _replacePreparation = replacePreparation;
        try { SetPresentation(view, _viewportWidth); }
        finally { _replacePreparation = false; }
        LastChangeReason = reason;
        ViewChanged?.Invoke(this, View);
    }
    private static string[] Toggle(IReadOnlyList<string> items, string key, bool selected) => selected ? items.Contains(key) ? items.ToArray() : [.. items, key] : items.Where(x => x != key).ToArray();
    private string[] Shown => View.Maps.Keys.Where(name => View.Query.Trim().Length == 0 || name.Contains(View.Query.Trim(), StringComparison.OrdinalIgnoreCase) || View.Maps[name].Contains(View.Query.Trim(), StringComparison.OrdinalIgnoreCase)).ToArray();
    private void Bulk(bool selected, string reason)
    {
        var tables = View.Tables.ToArray(); foreach (var name in Shown) tables = Toggle(tables, name, selected); Publish(View with { Tables = tables }, reason);
    }
    public void SetFooter(WebActionFooter footer)
    {
        var disabled = View.Categories.Count == 0 || (View.Categories.Contains("data") || View.Categories.Contains("schema")) && View.Tables.Count == 0 || View.Categories.Contains("trial") && !View.TrialReady;
        var hint = View.Categories.Count == 0 ? T("검사 분류를 하나 이상 선택하세요.", "Select at least one check category.")
            : (View.Categories.Contains("data") || View.Categories.Contains("schema")) && View.Tables.Count == 0 ? T("검사할 테이블을 하나 이상 선택하세요.", "Select at least one table to compare.")
            : View.Categories.Contains("trial") && !View.TrialReady ? T("동작 시험을 준비하거나 선택을 해제하세요.", "Prepare or deselect the behavior test.")
            : T("선택 범위와 제외 범위를 다음 화면에서 확인합니다.", "Review selected and excluded scope next.");
        footer.SetContent(hint, new("back", T("뒤로", "Back"), "quiet", "back"), [new("review", T("실행 전 확인", "Review before running"), "primary", "arrow", !disabled)], ReducedMotion);
    }
    private string? CaptureFocus()
    {
        var focus = TopLevel.GetTopLevel(this)?.FocusManager?.GetFocusedElement();
        if (focus == Heading.FocusTarget) return "heading";
        if (_section is not null && focus == Search.Editor) return "search";
        if (_section is not null && focus == BulkCheckbox) return "bulk";
        if (_section is not null && focus == DeselectButton) return "deselect";
        for (var i = 0; i < _categories.Count; i++) { if (focus == _categories[i].Header.ToggleButton) return "expand:" + i; if (focus == _categories[i].Header.Checkbox) return "category:" + i; }
        if (_section is not null) foreach (var r in Rows) { if (focus == r.Checkbox) return "table:" + r.SourceName; if (focus == r.ChangeButton) return "mapping:" + r.SourceName; }
        return null;
    }
    private void RestoreFocus(string? key)
    {
        var revision = ++_revision; var keyboard = _keyboard;
        Dispatcher.UIThread.Post(() => {
            if (!_attached || revision != _revision || key is null) return;
            if (key == "heading") { Heading.FocusTitle(keyboard); return; }
            InputElement? target = key switch { "search" => Search.Editor, "bulk" => BulkCheckbox, "deselect" => DeselectButton,
                _ when key.StartsWith("expand:") => _categories[int.Parse(key[7..])].Header.ToggleButton,
                _ when key.StartsWith("category:") => _categories[int.Parse(key[9..])].Header.Checkbox,
                _ when key.StartsWith("table:") => Rows.FirstOrDefault(r => r.SourceName == key[6..])?.Checkbox,
                _ when key.StartsWith("mapping:") => Rows.FirstOrDefault(r => r.SourceName == key[8..])?.ChangeButton, _ => null };
            target?.Focus(keyboard ? NavigationMethod.Tab : NavigationMethod.Pointer);
        }, DispatcherPriority.Loaded);
    }
    private void PlayEntry() { _entry?.Dispose(); _entry = WebMotion.Play(this, WebMotionKind.Page, ReducedMotion); }
    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e) { base.OnAttachedToVisualTree(e); _attached = true; PlayEntry(); }
    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e) { _attached = false; ++_revision; _entry?.Dispose(); _section.CancelFade(); base.OnDetachedFromVisualTree(e); }
    protected override Size MeasureOverride(Size available)
    {
        Heading.Measure(new(available.Width, double.PositiveInfinity)); _scope.Measure(new(available.Width, double.PositiveInfinity));
        var y = Heading.DesiredSize.Height + _scope.DesiredSize.Height + 12;
        foreach (var c in _categories) { c.Measure(new(available.Width, double.PositiveInfinity)); y += c.DesiredSize.Height; }
        y -= 12; Preparation.Measure(new(available.Width, double.PositiveInfinity));
        if (View.Categories.Contains("trial")) y += Preparation.DesiredSize.Height;
        _section.Measure(new(available.Width, double.PositiveInfinity)); y += 30 + _section.DesiredSize.Height;
        if (_error is not null) { _error.Measure(new(available.Width, double.PositiveInfinity)); y += _error.DesiredSize.Height; }
        return new(available.Width, y);
    }
    protected override Size ArrangeOverride(Size final)
    {
        double y = 0; void Place(Control c) { c.Arrange(new(0, y, final.Width, c.DesiredSize.Height)); y += c.DesiredSize.Height; }
        Place(Heading); Place(_scope); y += 12; foreach (var c in _categories) Place(c); y -= 12;
        if (View.Categories.Contains("trial")) Place(Preparation); else Preparation.Arrange(new(0, y + 12, final.Width, 0));
        y += 30; Place(_section); if (_error is not null) Place(_error); return final;
    }
    private sealed record CategoryDefinition(string Key, string[] Name, string[] Description, string[] Details, string[] EnglishDetails, string Icon);
    private static readonly CategoryDefinition[] Definitions = [
        new("data", ["데이터", "Data"], ["전체 값·누락·중복·참조 관계", "All values, missing rows, duplicates and relationships"], ["DATA-01 · 모든 선택 컬럼의 정확한 값과 중복 개수", "DATA-02 · 입증 가능한 키·참조·제약 위반"], ["DATA-01 · Exact values and duplicate multiplicities", "DATA-02 · Evidence of key, reference or constraint violations"], "db"),
        new("schema", ["구조·제약", "Schema & constraints"], ["컬럼·자료형·키·제약·인덱스·partition", "Columns, types, keys, constraints, indexes and partitions"], ["SCHEMA-01 · 컬럼 순서·타입·NULL·기본값·계산식", "SCHEMA-02 · PK·UNIQUE·FK·CHECK·활성/검증 상태", "SCHEMA-03 · 인덱스·partition 키와 경계"], ["SCHEMA-01 · Column order, type, NULL, default and expressions", "SCHEMA-02 · Keys, references, CHECKs and validation state", "SCHEMA-03 · Indexes, partition keys and bounds"], "grid"),
        new("access", ["사용자·권한", "Users & permissions"], ["사용자·역할·소유자·권한·행 정책", "Users, roles, ownership, grants and row policies"], ["ACCESS-01 · 같은 이름 principal·멤버십·권한 정의", "ACCESS-02 · RLS·DEFINER·실행 문맥 정의", "2개 예시 항목은 조회 범위를 확인하지 못했습니다."], ["ACCESS-01 · Same-name principals, membership and grants", "ACCESS-02 · RLS, DEFINER and execution context", "Full visibility is unconfirmed for 2 sample checks."], "shield"),
        new("objects", ["관련 객체", "Related objects"], ["view·함수·trigger·sequence·사용자 타입", "Views, routines, triggers, sequences and user types"], ["OBJECT-01 · 정의·signature·소유·참조 연결", "OBJECT-02 · 번호 발급 없는 sequence·identity 상태", "OBJECT-03 · 사용자 타입·enum·domain·외부 객체"], ["OBJECT-01 · Definitions, signatures, owners and references", "OBJECT-02 · Sequence and identity state without advancing values", "OBJECT-03 · User types, enums, domains and external objects"], "layers"),
        new("trial", ["별도 DB 동작 시험", "Behavior tests in a separate DB"], ["선택 기능 · 시험 DB에서만 입력·수정·삭제", "Optional · Insert, update and delete only in a test DB"], ["BEHAVIOR-01 · 확인한 값과 기대 결과의 기본 동작", "BEHAVIOR-02 · 선택 역할의 허용·거부·행 제한", "앱 소유 자료만 정리하며 남은 변경을 따로 표시합니다."], ["BEHAVIOR-01 · Basic behavior using confirmed values and expectations", "BEHAVIOR-02 · Allowed, denied and filtered actions of selected roles", "Only app-owned data is cleaned up. Remaining changes are separate."], "play"),
    ];
}
