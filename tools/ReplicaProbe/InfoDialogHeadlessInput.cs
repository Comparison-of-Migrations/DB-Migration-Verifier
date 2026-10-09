using System.Text.Json;
using Avalonia;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Input;
using Avalonia.Input.Raw;
using Avalonia.Threading;
using Verifier.Desktop;

internal static class InfoDialogHeadlessInput
{
    public static async Task Run(Window w,Canvas canvas,WebInfoDialog n,string output)
    {
        if(!ReplicaPlatform.IsHeadless)return;
        var records=new List<object>();
        var background=new Canvas{Width=1280,Height=720};
        var opener=new WebActionButton{Content="Open"};
        AutomationProperties.SetAutomationId(opener,"InfoDialog-opener");
        background.Children.Add(opener);canvas.Children.Insert(0,background);
        void Record(string label){w.UpdateLayout();records.Add(new{label,visible=n.IsVisible,closing=n.IsClosing,backgroundEnabled=background.IsEnabled,focus=w.FocusManager?.GetFocusedElement() is Control c?AutomationProperties.GetAutomationId(c):null,bodyOffset=n.BodyViewport.Offset,outerOffset=n.OuterViewport.Offset,english=n.Body.English,permissions=n.Body.Permissions});}
        void Send(Key key){w.KeyPress(key,RawInputModifiers.None,PhysicalKey.None,"");w.KeyRelease(key,RawInputModifiers.None,PhysicalKey.None,"");}
        n.Width=1280;n.Height=720;n.ReducedMotion=true;
        n.Open(false,false,false,opener,background,NavigationMethod.Tab);w.UpdateLayout();
        if(background.IsEnabled||!n.CloseButton.IsFocused)throw new InvalidOperationException("Initial modal isolation/focus");Record("open");
        w.MouseDown(new Point(10,10),MouseButton.Left);w.MouseUp(new Point(10,10),MouseButton.Left);
        if(!n.IsVisible)throw new InvalidOperationException("Backdrop closed unexpectedly");Record("backdrop");
        n.CloseButton.Focus(NavigationMethod.Tab);Send(Key.Tab);
        if(!n.FooterCloseButton.IsFocused)throw new InvalidOperationException("Footer Tab order");Record("footer-focus");
        Send(Key.Escape);await Dispatcher.UIThread.InvokeAsync(()=>{},DispatcherPriority.Background);
        if(n.IsVisible||!background.IsEnabled||!opener.IsFocused)throw new InvalidOperationException("Escape/focus restoration");Record("escape");
        n.Open(true,true,false,opener,background,NavigationMethod.Pointer);w.UpdateLayout();Record("language-kind");
        canvas.Children.Remove(n);if(!background.IsEnabled)throw new InvalidOperationException("Detach restored background");Record("detached");
        canvas.Children.Add(n);n.Open(false,false,false,opener,background,NavigationMethod.Tab);w.UpdateLayout();Record("remounted");
        await n.CloseAsync();if(!background.IsEnabled||!opener.IsFocused)throw new InvalidOperationException("Reopened restoration");Record("close");
        canvas.Children.Remove(background);
        File.WriteAllText(Path.Combine(output,"infodialog-headless-input.json"),JsonSerializer.Serialize(records,new JsonSerializerOptions{WriteIndented=true}));
    }
}
