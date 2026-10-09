using System.Text.Json;
using Avalonia;
using Avalonia.Automation.Peers;
using Avalonia.Automation;
using Avalonia.VisualTree;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Input;
using Avalonia.Threading;
using Verifier.Desktop;
internal static class CapabilityViewportFocusInput
{
 internal static async Task Run(Window window,Canvas canvas,WebCapabilityViewport viewport,Panel content,string output,bool clipOnly)
 {
  viewport.Width=478;viewport.LogicalWidth=800;viewport.OverflowWidth=800;
  var host=content;
  var before=new Button{Content="Before viewport",Width=180,Height=44};Canvas.SetLeft(before,600);canvas.Children.Insert(0,before);
  var states=new List<object>();
  async Task Flush(){await Task.Delay(60);window.UpdateLayout();AvaloniaHeadlessPlatform.ForceRenderTimerTick(1);}
  void Save(string id){states.Add(new{id,offset=viewport.Offset,visible=viewport.IsFocusVisible,focused=viewport.IsFocused,tabStop=viewport.IsTabStop});File.WriteAllText(Path.Combine(output,"capabilityviewport-focus-headless.json"),JsonSerializer.Serialize(new{states},new JsonSerializerOptions{WriteIndented=true}));}
  if(!clipOnly){window.UpdateLayout();viewport.Focus(NavigationMethod.Pointer);await Flush();before.Focus();viewport.Focus();await Flush();if(viewport.IsFocusVisible)throw new InvalidOperationException("Pointer restore invented a keyboard ring");Save("pointer-restore");
  before.Focus();window.KeyPress(Key.Tab,RawInputModifiers.None,PhysicalKey.Tab,null);window.KeyRelease(Key.Tab,RawInputModifiers.None,PhysicalKey.Tab,null);await Flush();if(!viewport.IsFocused||!viewport.IsFocusVisible)throw new InvalidOperationException("Raw Tab failed to reach viewport with keyboard ring");Save("tab-viewport");
  before.Focus();viewport.Focus();await Flush();if(!viewport.IsFocusVisible)throw new InvalidOperationException("Keyboard restore lost ring");Save("keyboard-restore");}
  window.UpdateLayout();
  var child=new Button{Content="Required permissions",Width=150,Height=44};Canvas.SetLeft(child,630);Canvas.SetTop(child,35);host.Children.Add(child);await Flush();
  var peer=ControlAutomationPeer.CreatePeerForElement(child);states.Add(new{id="clip-diagnostic",behavior=AutomationProperties.GetIsOffscreenBehavior(child).ToString(),clip=child.GetTransformedBounds()?.Clip.ToString(),bounds=child.Bounds.ToString()});File.WriteAllText(Path.Combine(output,"capabilityviewport-focus-headless.json"),JsonSerializer.Serialize(new{states},new JsonSerializerOptions{WriteIndented=true}));if(OperatingSystem.IsWindows()&&!peer.IsOffscreen())throw new InvalidOperationException("FromClip peer must expose clipped child offscreen");states.Add(new{id="clipped-child",offscreen=peer.IsOffscreen(),name=peer.GetName(),tabStop=viewport.IsTabStop});
  before.Focus();window.KeyPress(Key.Tab,RawInputModifiers.None,PhysicalKey.Tab,null);window.KeyRelease(Key.Tab,RawInputModifiers.None,PhysicalKey.Tab,null);await Flush();if(!child.IsFocused||viewport.Offset!=322||viewport.IsTabStop)throw new InvalidOperationException("Raw child Tab focus/bring boundary failed");if(peer.IsOffscreen())throw new InvalidOperationException("Visible focused child remained offscreen");Save("tab-child");
  states.Add(new{id="visible-child",offscreen=peer.IsOffscreen(),name=peer.GetName()});File.WriteAllText(Path.Combine(output,"capabilityviewport-focus-headless.json"),JsonSerializer.Serialize(new{states},new JsonSerializerOptions{WriteIndented=true}));
 }
}
