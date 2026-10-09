using Avalonia;
using Avalonia.Automation;
using Avalonia.Automation.Peers;
using Avalonia.Controls;
using Avalonia.VisualTree;

namespace Verifier.Desktop;

public sealed partial class WebSelectionPage
{
    // The locked MappingRow paints/layouts all four cells. Its table parent supplies the logical hierarchy.
    private sealed class MappingRowHost : Panel
    {
        private readonly WebTableMappingRow _row;
        private readonly CellMarker[] _cells;
        private readonly ChoiceMarker _choice;
        public MappingRowHost(WebTableMappingRow row)
        {
            _row = row; UseLayoutRounding = false; Children.Add(row); AutomationProperties.SetAccessibilityView(row, AccessibilityView.Raw);
            _choice = new(row); _cells = [new(() => [ControlAutomationPeer.CreatePeerForElement(_choice)], () => row.SourceName, () => [ControlAutomationPeer.CreatePeerForElement(row.Checkbox)]),
                new(() => [ControlAutomationPeer.CreatePeerForElement(row.TargetText)], () => row.TargetName, () => []), new(() => [ControlAutomationPeer.CreatePeerForElement(row.MethodText)], () => row.MethodText.Text ?? "", () => []),
                new(() => [ControlAutomationPeer.CreatePeerForElement(row.ChangeButton)], () => row.ChangeButton.Content?.ToString() ?? "")];
            _cells[0].Children.Add(_choice); foreach (var c in _cells) Children.Add(c);
        }
        protected override Size MeasureOverride(Size size) { _row.Measure(size); _choice.Measure(_row.Choice.DesiredSize); foreach (var c in _cells) c.Measure(new(_row.ColumnWidths[Array.IndexOf(_cells, c)], _row.DesiredSize.Height)); return _row.DesiredSize; }
        protected override Size ArrangeOverride(Size size)
        {
            _row.Arrange(new Rect(size)); double x = 0; for (var i = 0; i < 4; i++) { _cells[i].Arrange(new(x, 0, _row.ColumnWidths[i], size.Height)); x += _row.ColumnWidths[i]; }
            var p = _row.Choice.TranslatePoint(default, _cells[0]) ?? default; _choice.Arrange(new(p, _row.Choice.Bounds.Size)); return size;
        }
        protected override AutomationPeer OnCreateAutomationPeer() => new RowPeer(this);
        private sealed class RowPeer(MappingRowHost owner) : ControlAutomationPeer(owner)
        {
            protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.DataItem;
            protected override string GetClassNameCore() => "Row";
            protected override string GetLocalizedControlTypeCore() => "row";
            protected override AutomationPeer? GetParentCore() => owner.GetVisualAncestors().OfType<SelectionTable>().Select(CreatePeerForElement).FirstOrDefault();
            protected override IReadOnlyList<AutomationPeer>? GetChildrenCore() => owner._cells.Select(CreatePeerForElement).ToArray();
        }
    }
    private sealed class CellMarker(Func<IReadOnlyList<AutomationPeer>> children, Func<string>? name = null, Func<IReadOnlyList<AutomationPeer>>? windowsChildren = null) : Panel
    {
        private readonly Func<IReadOnlyList<AutomationPeer>> _children = children;
        private readonly Func<IReadOnlyList<AutomationPeer>> _windowsChildren = windowsChildren ?? children;
        private readonly Func<string>? _name = name;
        public CellMarker() : this(() => Array.Empty<AutomationPeer>()) { }
        protected override Size MeasureOverride(Size size) { UseLayoutRounding = false; IsHitTestVisible = false; return size; }
        protected override AutomationPeer OnCreateAutomationPeer() => new CellPeer(this);
        private sealed class CellPeer(CellMarker owner) : ControlAutomationPeer(owner)
        {
            private static AutomationPeer ConnectOriginal(AutomationPeer peer) {
                // Finish stock visual connection before GetOrCreateChildrenCore assigns this logical cell.
                // Returning the cached child peer also preserves the root provider's focused identity.
                _ = peer.GetParent();
                return peer;
            }
            protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.DataItem;
            protected override string GetClassNameCore() => "Cell";
            protected override string GetLocalizedControlTypeCore() => "cell";
            protected override string? GetNameCore() => OperatingSystem.IsWindows() ? owner._name?.Invoke() ?? "" : base.GetNameCore();
            protected override IReadOnlyList<AutomationPeer>? GetChildrenCore() => OperatingSystem.IsWindows() ? owner._windowsChildren().Select(ConnectOriginal).ToArray() : owner._children();
        }
    }
    private sealed class ChoiceMarker(WebTableMappingRow row) : Control
    {
        private readonly WebTableMappingRow _row = row;
        protected override Size MeasureOverride(Size size) { UseLayoutRounding = false; IsHitTestVisible = false; return size; }
        protected override AutomationPeer OnCreateAutomationPeer() => new ChoicePeer(this);
        private sealed class ChoicePeer(ChoiceMarker owner) : ControlAutomationPeer(owner)
        {
            protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.Group;
            protected override IReadOnlyList<AutomationPeer>? GetChildrenCore()
            {
                var children = new[] { CreatePeerForElement(owner._row.Checkbox), CreatePeerForElement(owner._row.SourceText) };
                if (OperatingSystem.IsMacOS())
                    foreach (var child in children) _ = child.GetParent();
                // Stock assignment follows this return: finish original visual connection first.
                return children;
            }
        }
    }
}
