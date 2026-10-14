using Avalonia.Automation.Peers;
namespace Verifier.Desktop;

// Preserve the completed input renderer while protecting accessibility values/events.
public sealed class WebPasswordInput : WebTextInput
{
    protected override AutomationPeer OnCreateAutomationPeer()=>new PasswordTextBox.PasswordPeer(this);
}
