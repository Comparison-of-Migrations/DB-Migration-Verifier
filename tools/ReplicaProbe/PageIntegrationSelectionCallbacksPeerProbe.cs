using System.Text.Json;
using Avalonia.Automation.Peers;
using Avalonia.Automation.Provider;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Threading;
using Verifier.Desktop;

internal static class PageIntegrationSelectionCallbacksPeerProbe
{
    internal static async Task Check(ReplicaWorkspace workspace, Window window, string output, string oracle)
    {
        if (!ReplicaPlatform.IsHeadless) throw new InvalidOperationException("Peer diagnostic headless scope only");
        using var original = JsonDocument.Parse(File.ReadAllText(oracle));
        if (original.RootElement.GetProperty("records").GetArrayLength() != 2) throw new InvalidOperationException("Exact original KOEN oracle required");
        async Task Flush() { await Dispatcher.UIThread.InvokeAsync(() => { }, DispatcherPriority.Loaded); window.UpdateLayout(); await Task.Delay(45); }
        var records = new List<object>();
        foreach (var english in new[] { false, true })
        {
            workspace.Dispose(); workspace = new ReplicaWorkspace { ReducedMotion = true }; window.Content = workspace; window.Width = 1280; window.Height = 900;
            workspace.SetLanguage(english); await workspace.DispatchAsync("start"); await Flush(); await workspace.DispatchAsync("connect"); await Flush();
            workspace.Navigate("review"); await Flush(); var review = (WebReviewPage)workspace.CurrentPage!;
            ((IToggleProvider)ControlAutomationPeer.CreatePeerForElement(review.StoppedChoice.Checkbox)!).Toggle(); workspace.Navigate("select"); await Flush();
            var page = (WebSelectionPage)workspace.CurrentPage!;
            ((IToggleProvider)ControlAutomationPeer.CreatePeerForElement(page.Categories[0].Header.Checkbox)!).Toggle();
            workspace.Navigate("review"); await Flush(); review = (WebReviewPage)workspace.CurrentPage!;
            ((IToggleProvider)ControlAutomationPeer.CreatePeerForElement(review.StoppedChoice.Checkbox)!).Toggle(); workspace.Navigate("select"); await Flush();
            page = (WebSelectionPage)workspace.CurrentPage!;
            var expand = (IExpandCollapseProvider)ControlAutomationPeer.CreatePeerForElement(page.Categories[0].Header.ToggleButton)!;
            expand.Expand(); await Flush(); expand.Collapse(); await Flush();
            // Explicit handler fixture restores the saved query checkpoint; no physical action pass inferred.
            page.Search.Text = "orders"; await Flush(); var row = page.Rows.Single();
            var root = ControlAutomationPeer.CreatePeerForElement(window)!;
            var seen = new HashSet<AutomationPeer>();
            void Walk(AutomationPeer peer, int depth) { if (depth > 50 || !seen.Add(peer)) return; foreach (var child in peer.GetChildren()) Walk(child, depth + 1); }
            Walk(root, 0);
            var checkbox = ControlAutomationPeer.CreatePeerForElement(row.Checkbox)!;
            var sourceText = ControlAutomationPeer.CreatePeerForElement(row.SourceText)!;
            object CheckLeaf(AutomationPeer peer, string id)
            {
                var chain = new List<string>(); var visited = new HashSet<AutomationPeer>(); AutomationPeer? current = peer;
                while (current is not null && visited.Add(current) && chain.Count < 40) { chain.Add(current.GetType().FullName!); if (ReferenceEquals(current, root)) break; current = current.GetParent(); }
                if (!ReferenceEquals(current, root)) throw new InvalidOperationException("Current original leaf must reach real root: " + id);
                if (!ReferenceEquals(ControlAutomationPeer.CreatePeerForElement(((ControlAutomationPeer)peer).Owner), peer)) throw new InvalidOperationException("Cached original peer identity changed");
                var bounds = peer.GetBoundingRectangle(); if (bounds.Width <= 0 || bounds.Height <= 0) throw new InvalidOperationException("Current original leaf bounds missing");
                return new { id, peer = peer.GetType().FullName, chain, bounds = new { bounds.X, bounds.Y, bounds.Width, bounds.Height }, name = peer.GetName() };
            }
            var checkboxProof = CheckLeaf(checkbox, "Selection-table-demo.orders");
            var sourceProof = CheckLeaf(sourceText, "SourceText-demo.orders");
            if (checkbox is not IToggleProvider toggle) throw new InvalidOperationException("Original Toggle provider retained");
            toggle.Toggle(); await Flush();
            if (!workspace.Tables.SequenceEqual(new[] { "demo.customers", "demo.line_items" }) || workspace.Stopped) throw new InvalidOperationException("Original parent callback after queried row Toggle");
            records.Add(new { english, checkboxProof, sourceProof, reason = page.LastChangeReason, state = PageIntegrationFlowSnapshot.Read("peer-query-toggle", workspace, window), scope = "Fixture/API peer ancestry; native physical continuation separate" });
        }
        File.WriteAllText(Path.Combine(output, "selectioncallbacks-peer-comparison.json"), JsonSerializer.Serialize(new { records, passed = true, scope = "New Mac original leaf connection order only; old geometry and physical checkpoints reused" }, new JsonSerializerOptions { WriteIndented = true }));
    }
}
