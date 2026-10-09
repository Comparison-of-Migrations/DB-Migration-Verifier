using Avalonia;
using Avalonia.Automation;
using Avalonia.Automation.Peers;
using Avalonia.Automation.Provider;
using Avalonia.Controls;

namespace Verifier.Desktop;

// TextBox's value provider returns the actual Text, even with PasswordChar.
// Expose an editable password field with a masked value and no text children.
public sealed class PasswordTextBox : TextBox
{
    // Keep the native TextBox template/styles while replacing only its value peer.
    protected override Type StyleKeyOverride=>typeof(TextBox);
    protected override AutomationPeer OnCreateAutomationPeer()=>new PasswordPeer(this);
    private sealed class PasswordPeer : ControlAutomationPeer,IValueProvider
    {
        private readonly PasswordTextBox owner;
        public PasswordPeer(PasswordTextBox owner) : base(owner)
        {
            this.owner=owner;
            owner.PropertyChanged+=(_,e)=>
            {
                if(e.Property==TextProperty) RaisePropertyChangedEvent(ValuePatternIdentifiers.ValueProperty,
                    string.IsNullOrEmpty(e.OldValue as string)?"":"••••••••",string.IsNullOrEmpty(e.NewValue as string)?"":"••••••••");
            };
        }
        public bool IsReadOnly=>owner.IsReadOnly;
        public string Value=>string.IsNullOrEmpty(owner.Text)?"":"••••••••";
        public void SetValue(string? value)=>owner.Text=value;
        protected override AutomationControlType GetAutomationControlTypeCore()=>AutomationControlType.Edit;
        protected override string GetLocalizedControlTypeCore()=>"Password";
        protected override IReadOnlyList<AutomationPeer> GetOrCreateChildrenCore()=>Array.Empty<AutomationPeer>();
    }
}
