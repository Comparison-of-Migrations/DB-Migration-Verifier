using Avalonia;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Threading;
namespace Verifier.Desktop;

public sealed record WebSaveView(bool English, bool Saved, IReadOnlyList<string> Formats);

/// <summary>Literal save() body. The application owns file operations, scroll and the common footer.</summary>
public sealed class WebSavePage : Panel
{
    public WebPageHeading Heading { get; private set; } = new();
    public WebSaveOptions Options { get; private set; } = new();
    public WebSummaryList Summary { get; private set; } = new();
    public WebNotice? SavedNotice { get; private set; }
    public IReadOnlyList<WebActionButton> ActionButtons => _actions.Children.OfType<WebActionButton>().ToArray();
    public WebSaveView View { get; private set; } = new(false, false, ["json"]);
    public event EventHandler<WebFormatSelection>? FormatChanged;
    public event EventHandler<string>? ActionRequested;
    public bool ReducedMotion { get; set; }
    private double _viewport = 1280;
    private Actions _actions = new();
    private WebMotionPlayback? _entry;
    private long _revision;
    private bool _attached;
    public WebSavePage() { UseLayoutRounding = false; SetPresentation(View, _viewport); }
    public void SetPresentation(WebSaveView view, double viewportWidth, string? focusFormat = null, bool focusVisible = false)
    {
        View = view with { Formats = view.Formats.ToArray() }; _viewport = viewportWidth;
        var revision = ++_revision; Children.Clear(); SavedNotice = null;
        string T(string ko, string en) => view.English ? en : ko;
        Heading = new();
        Heading.SetPresentation(T("확인한 범위를 결과 파일로 남기세요", "Save the scope you have verified"), T("실제 값·행 키·계정명·접속 정보는 결과에 넣지 않습니다. 예시 결과 파일은 이 시안에서 다시 열 수 있습니다.", "No row values, keys, principal names or connection details are included. Sample JSON files can be reopened in this preview."));
        Children.Add(Heading);
        Options = new() { ReducedMotion = ReducedMotion }; Options.SetPresentation(view.Formats, view.English);
        Options.SelectionChanged += (_, change) => {
            var selected = Options.SelectedFormats.ToArray();
            SetPresentation(View with { Formats = selected }, _viewport, change.Format, change.FocusVisible);
            FormatChanged?.Invoke(this, change);
        };
        Children.Add(Options);
        Summary = new(); Summary.SetPresentation([
            new(T("포함", "Included"), [T("예시 run ID, 선택 범위, 완료·중단·미시작, 판정과 확인 불가 이유, 안전한 finding 위치", "Sample run ID, scope, completed/interrupted/pending states, outcomes, gap reasons and safe finding locations")]),
            new(T("제외", "Excluded"), [T("실제 값·PK·비밀번호·토큰·원 SQL 정의·접속 설정", "Values, PKs, passwords, tokens, raw SQL definitions and connection settings")])
        ], viewportWidth <= 1100, true);
        var section = new Decorator { Child = Summary, Margin = new(0, 30, 0, 0), UseLayoutRounding = false }; Children.Add(section);
        if (view.Saved) {
            SavedNotice = new() { Kind = "ok", ReducedMotion = ReducedMotion, Title = T("예시 파일 다운로드를 요청했습니다", "Sample file downloads were requested"), Text = T("브라우저의 다운로드 결과를 확인하세요. 디스크 저장 성공을 이 화면만으로 확정하지 않습니다.", "Check your browser downloads. This screen alone does not establish a successful disk save.") };
            Children.Add(SavedNotice);
        }
        _actions = new() { Margin = new(0, 24, 0, 0) };
        foreach (var action in new[] { new WebFooterAction("open", T("저장한 결과 다시 열기", "Reopen a saved result"), "base", "file"), new WebFooterAction("restart", T("전체 재검사", "Start a full new check"), "quiet", "refresh") }) {
            var button = new WebActionButton { Content = action.Label, Variant = action.Variant, IconName = action.IconName, ReducedMotion = ReducedMotion };
            AutomationProperties.SetAutomationId(button, "Save-" + action.Action);
            button.Click += (_, _) => ActionRequested?.Invoke(this, action.Action); _actions.Children.Add(button);
        }
        Children.Add(_actions); InvalidateMeasure();
        if (_attached) PlayEntry();
        if (focusFormat is not null) Dispatcher.UIThread.Post(() => {
            if (revision != _revision || !_attached) return;
            var box = Options.Options.First(o => o.Format == focusFormat).Checkbox;
            if (focusVisible) box.Focus(NavigationMethod.Tab); else box.FocusFromPointer();
        }, DispatcherPriority.Loaded);
    }
    public void SetFooter(WebActionFooter footer)
    {
        string T(string ko, string en) => View.English ? en : ko;
        footer.SetContent(View.Formats.Count > 0 ? T("브라우저 다운로드로 예시 파일을 만듭니다.", "Create sample files via browser downloads.") : T("저장할 형식을 선택하세요.", "Choose a file format."),
            new("back", T("결과로 돌아가기", "Back to results"), "quiet", "back"),
            [new("download", T("선택 형식 저장", "Save selected formats"), "primary", "save", View.Formats.Count > 0)], ReducedMotion);
    }
    private void PlayEntry() { _entry?.Dispose(); _entry = WebMotion.Play(this, WebMotionKind.Page, ReducedMotion); }
    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e) { base.OnAttachedToVisualTree(e); _attached = true; PlayEntry(); }
    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e) { _attached = false; _entry?.Dispose(); _entry = null; base.OnDetachedFromVisualTree(e); }
    protected override Size MeasureOverride(Size available) { double y = 0; foreach (var child in Children) { child.Measure(new(available.Width, double.PositiveInfinity)); y += child.DesiredSize.Height; } return new(available.Width, y); }
    protected override Size ArrangeOverride(Size final) { double y = 0; foreach (var child in Children) { child.Arrange(new(0, y, final.Width, child.DesiredSize.Height)); y += child.DesiredSize.Height; } return final; }
    private sealed class Actions : Panel
    {
        private readonly List<Rect> _boxes = [];
        public Actions() { UseLayoutRounding = false; }
        protected override Size MeasureOverride(Size available) {
            _boxes.Clear(); double x = 0, y = 0, line = 0;
            foreach (var c in Children) { c.Measure(Size.Infinity); var s = c.DesiredSize; if (x > 0 && x + s.Width > available.Width) { y += line + 10; x = 0; line = 0; } _boxes.Add(new(x, y, s.Width, s.Height)); x += s.Width + 10; line = Math.Max(line, s.Height); }
            return new(available.Width, y + line);
        }
        protected override Size ArrangeOverride(Size final) { for (var i = 0; i < Children.Count; i++) Children[i].Arrange(_boxes[i]); return final; }
    }
}
