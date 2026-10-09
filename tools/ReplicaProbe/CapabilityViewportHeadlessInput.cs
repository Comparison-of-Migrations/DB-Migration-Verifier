using System.Text.Json;
using Avalonia;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using Verifier.Desktop;
internal static class CapabilityViewportHeadlessInput
{
 internal static async Task Run(Window window,Canvas canvas,WebCapabilityViewport viewport,Panel content,string output,bool childOnly)
 {
  // New scope only: previous72 geometry/key9/lifecycle3 are retained unchanged.
  var host=new Canvas{Width=800,Height=120,UseLayoutRounding=false,Background=Brush.Parse("#232428")};viewport.Content=host;viewport.Width=478;viewport.LogicalWidth=800;viewport.OverflowWidth=800;window.Activate();window.UpdateLayout();viewport.Focus(NavigationMethod.Pointer);
  var states=new List<object>();async Task Flush(){await Task.Delay(280);window.UpdateLayout();AvaloniaHeadlessPlatform.ForceRenderTimerTick(1);}
  void Save(string id,double expected){if(viewport.Offset!=expected)throw new InvalidOperationException(id+" offset="+viewport.Offset+" expected="+expected);states.Add(new{id,offset=viewport.Offset,maximum=viewport.MaximumOffset,focusable=viewport.Focusable,tabStop=viewport.IsTabStop,focusVisible=viewport.IsFocusVisible,attached=canvas.Children.Contains(viewport)});File.WriteAllText(Path.Combine(output,"capabilityviewport-extra-headless.json"),JsonSerializer.Serialize(new{states},new JsonSerializerOptions{WriteIndented=true}));Console.WriteLine(id+":"+viewport.Offset);}
  if(!childOnly){viewport.Offset=100;window.KeyPress(Key.Space,RawInputModifiers.Shift,PhysicalKey.Space," ");window.KeyRelease(Key.Space,RawInputModifiers.Shift,PhysicalKey.Space," ");await Flush();Save("shift-space",100);
  var point=viewport.TranslatePoint(new Point(70,55),window)!.Value;
  viewport.Offset=0;window.MouseWheel(point,new Vector(OperatingSystem.IsWindows()?-1:-2,0));await Flush();Save("wheel-x",100);
  viewport.Offset=0;window.MouseWheel(point,new Vector(0,OperatingSystem.IsWindows()?-1:-2));await Flush();Save("wheel-y",0);
  viewport.Offset=0;window.MouseWheel(point,new Vector(0,OperatingSystem.IsWindows()?-1:-2),RawInputModifiers.Shift);await Flush();Save("wheel-shift-y",OperatingSystem.IsWindows()?100:0);
  viewport.Offset=300;viewport.Width=850;window.UpdateLayout();Save("resize-clamp",0);viewport.Width=478;window.UpdateLayout();await Flush();Save("restore-no-resume",0);}
  var button=new Button{Content="Required permissions",Width=150,Height=44};AutomationProperties.SetAutomationId(button,"CapabilityViewport-child");Canvas.SetLeft(button,630);Canvas.SetTop(button,35);host.Children.Add(button);window.UpdateLayout();if(viewport.IsTabStop)throw new InvalidOperationException("Horizontal child creates extra parent Tab stop");states.Add(new{id="child-no-parent-tab",tabStop=viewport.IsTabStop});button.Focus(NavigationMethod.Tab);await Flush();Save("child-bring-into-view",322);if(!button.IsFocused)throw new InvalidOperationException("Child actual routed focus lost");
  button.Focus(NavigationMethod.Tab);window.KeyPress(Key.Right,RawInputModifiers.None,PhysicalKey.ArrowRight,null);canvas.Children.Remove(viewport);window.KeyRelease(Key.Right,RawInputModifiers.None,PhysicalKey.ArrowRight,null);await Flush();Save("detach-pending-key",0);canvas.Children.Add(viewport);window.UpdateLayout();await Flush();Save("remount-no-pending",0);
  host.Children.Clear();viewport.Content=host;viewport.Width=478;viewport.LogicalWidth=800;viewport.OverflowWidth=800;window.UpdateLayout();
  using(var image=window.CaptureRenderedFrame()??throw new InvalidOperationException("Real Skia extra frame missing"))image.Save(Path.Combine(output,"capabilityviewport-extra-headless.png"),new PngBitmapEncoderOptions());File.WriteAllText(Path.Combine(output,"capabilityviewport-extra-headless.json"),JsonSerializer.Serialize(new{scope="New finite scopes only; actual OS source/native verification remains",states},new JsonSerializerOptions{WriteIndented=true}));
 }
}
