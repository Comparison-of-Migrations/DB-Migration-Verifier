using Avalonia;
using Avalonia.Automation;
using Avalonia.Automation.Peers;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Threading;
using Avalonia.VisualTree;

namespace Verifier.Desktop;

public sealed record WebPreflightView(bool English);

/// <summary>Literal capabilities() page. Navigation and the permissions modal belong to the application.</summary>
public sealed class WebPreflightPage : Panel
{
    public WebPreflightView View { get; private set; } = new(false);
    public WebPageHeading Heading { get; private set; } = new();
    public WebCapabilityTable Table { get; private set; } = new();
    public WebNotice WarningNotice { get; private set; } = new();
    public Control OptionalSection => _optional;
    public WebSystemTextBlock OptionalTitle => _optional.Title;
    public WebSystemTextBlock OptionalDescription => _optional.Description;
    public bool ReducedMotion { get; set; }
    public event EventHandler<string>? ActionRequested;
    private OptionalTrial _optional = new();
    private WebMotionPlayback? _entry;
    private bool _attached, _initialized, _keyboardFocus;
    private long _revision;

    public WebPreflightPage()
    {
        UseLayoutRounding = false;
        Background = Brushes.Transparent;
        AddHandler(GotFocusEvent, (_, e) => _keyboardFocus = e.NavigationMethod != NavigationMethod.Pointer, RoutingStrategies.Bubble, true);
        AddHandler(PointerPressedEvent, (_, e) =>
        {
            if (!e.GetCurrentPoint(this).Properties.IsLeftButtonPressed || e.Source is not Visual visual) return;
            if (visual is WebActionButton || visual.GetVisualAncestors().OfType<WebActionButton>().Any()) return;
            TopLevel.GetTopLevel(this)?.FocusManager?.Focus(null, NavigationMethod.Pointer);
        }, RoutingStrategies.Tunnel, true);
        SetPresentation(View, 1280);
    }

    public void SetPresentation(WebPreflightView view, double viewportWidth)
    {
        // A viewport resize does not invoke original render() or replace its DOM.
        if (_initialized && View == view) { Table.ReducedMotion = ReducedMotion; WarningNotice.ReducedMotion = ReducedMotion; InvalidateMeasure(); return; }
        var restoreTitle = Heading.FocusTarget.IsFocused;
        var restorePermission = Table.Rows.Single(r => r.Key == "access").PermissionsButton.IsFocused;
        var keyboard = _keyboardFocus;
        View = view;
        var revision = ++_revision;
        string T(string ko, string en) => view.English ? en : ko;
        Children.Clear();
        Heading = new();
        Heading.SetPresentation(T("앱이 확인할 수 있는 범위입니다", "What the app can verify"),
            T("권한 이름을 미리 알 필요는 없습니다. 가능한 검사와 확인하지 못한 부분을 먼저 보여드립니다.", "You do not need to know permission names. The app first shows available checks and visibility gaps."));
        Table = new() { ReducedMotion = ReducedMotion };
        Table.SetEnglish(view.English);
        Table.PermissionsRequested += (_, _) => ActionRequested?.Invoke(this, "permissions");
        WarningNotice = new() { ReducedMotion = ReducedMotion, Kind = "warn",
            Title = T("보이지 않는 객체를 “없음”으로 처리하지 않습니다", "Hidden objects are not treated as absent"),
            Text = T("읽지 못한 항목은 검사 후에도 확인 불가로 남습니다. 읽을 수 있는 부분의 차이는 계속 확인할 수 있습니다.", "Unreadable items remain unverified. Differences in readable areas can still be checked.") };
        _optional = new() { Margin = new(0, 30, 0, 0) };
        _optional.Title.Text = T("별도 DB 동작 시험은 선택입니다", "Behavior tests are optional");
        _optional.Description.Text = T("기본 비교는 읽기 전용입니다. 입력·수정·삭제 시험은 다음 화면에서 별도 시험 DB를 준비한 경우에만 선택합니다.", "The default comparison is read-only. Inserts, updates and deletes require a separate prepared test DB.");
        Children.Add(Heading); Children.Add(Table); Children.Add(WarningNotice); Children.Add(_optional);
        _initialized = true;
        InvalidateMeasure();
        if (_attached) PlayEntry();
        if (restoreTitle || restorePermission) Dispatcher.UIThread.Post(() =>
        {
            if (!_attached || revision != _revision) return;
            if (restoreTitle) Heading.FocusTitle(keyboard);
            else { Table.Rows.Single(r => r.Key == "access").PermissionsButton.Focus(keyboard ? NavigationMethod.Tab : NavigationMethod.Pointer); Table.Viewport.Offset = 0; }
        }, DispatcherPriority.Loaded);
    }

    public void SetFooter(WebActionFooter footer)
    {
        string T(string ko, string en) => View.English ? en : ko;
        footer.SetContent(T("미조회 영역은 결과에도 남습니다.", "Visibility gaps stay in the result."),
            new("back", T("뒤로", "Back"), "quiet", "back"),
            [new("choose", T("검사 범위 선택", "Choose checks"), "primary", "arrow")], ReducedMotion);
    }

    private void PlayEntry() { _entry?.Dispose(); _entry = WebMotion.Play(this, WebMotionKind.Page, ReducedMotion); }
    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e) { base.OnAttachedToVisualTree(e); _attached = true; PlayEntry(); }
    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e) { _attached = false; ++_revision; _entry?.Dispose(); _entry = null; base.OnDetachedFromVisualTree(e); }
    protected override Size MeasureOverride(Size available)
    {
        double y = 0;
        foreach (var child in Children) { child.Measure(new(available.Width, double.PositiveInfinity)); y += child.DesiredSize.Height; }
        return new(available.Width, y);
    }
    protected override Size ArrangeOverride(Size final)
    {
        double y = 0;
        foreach (var child in Children) { child.Arrange(new(0, y, final.Width, child.DesiredSize.Height)); y += child.DesiredSize.Height; }
        return final;
    }

    // Original section has no independent state, controls, action or extra paragraph gap.
    private sealed class OptionalTrial : Panel
    {
        public WebSystemTextBlock Title { get; } = WebTypography.CreateCandidate("", 18, 27, Brush.Parse("#f2f3f5"), 700, -.27);
        public WebSystemTextBlock Description { get; } = WebTypography.CreateCandidate("", 14, 22.4, Brush.Parse("#b5bac1"));
        private readonly Control _titleHost, _descriptionHost;
        public OptionalTrial()
        {
            UseLayoutRounding = false; Background = Brushes.Transparent;
            AutomationProperties.SetHeadingLevel(Title, 2);
            _titleHost = OperatingSystem.IsWindows() ? new OptionalText(Title) : Title;
            _descriptionHost = OperatingSystem.IsWindows() ? new OptionalText(Description) : Description;
            if (OperatingSystem.IsWindows()) AutomationProperties.SetHeadingLevel(_titleHost, 2);
            Children.Add(_titleHost); Children.Add(_descriptionHost);
        }
        protected override Size MeasureOverride(Size available)
        {
            var units = (TopLevel.GetTopLevel(this)?.RenderScaling ?? 2) * 64;
            Description.LineHeight = Math.Floor(22.4 * units) / units;
            _titleHost.Measure(new(available.Width, double.PositiveInfinity)); _descriptionHost.Measure(new(available.Width, double.PositiveInfinity));
            return new(available.Width, _titleHost.DesiredSize.Height + _descriptionHost.DesiredSize.Height);
        }
        protected override Size ArrangeOverride(Size final) { _titleHost.Arrange(new(0, 0, final.Width, _titleHost.DesiredSize.Height)); _descriptionHost.Arrange(new(0, _titleHost.DesiredSize.Height, final.Width, _descriptionHost.DesiredSize.Height)); return final; }
    }

    // These Windows-only hosts expose each optional text once in the original ControlView.
    private sealed class OptionalText : Decorator
    {
        private readonly WebSystemTextBlock _text;
        public OptionalText(WebSystemTextBlock text) { _text = text; Child = text; UseLayoutRounding = false; }
        protected override AutomationPeer OnCreateAutomationPeer() => new OptionalTextPeer(this);
        private sealed class OptionalTextPeer(OptionalText owner) : ControlAutomationPeer(owner)
        {
            protected override string GetNameCore() => owner._text.Text ?? "";
            protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.Text;
            protected override IReadOnlyList<AutomationPeer>? GetChildrenCore() => Array.Empty<AutomationPeer>();
        }
    }
}
