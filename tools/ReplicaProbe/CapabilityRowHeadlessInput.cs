using System.Text.Json;
using Avalonia;
using Avalonia.Automation.Peers;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using Verifier.Desktop;
internal static class CapabilityRowHeadlessInput
{
 internal static async Task Run(Window window,Canvas canvas,WebCapabilityRow row,double[] columns,string output)
 {
  var requests=0;row.PermissionsRequested+=Requested;void Requested(object? _,EventArgs __)=>requests++;
  var states=new List<object>();
  async Task Flush(){window.UpdateLayout();await Dispatcher.UIThread.InvokeAsync(()=>{},DispatcherPriority.Background);AvaloniaHeadlessPlatform.ForceRenderTimerTick(1);}
  void Save(string id,int expected,bool? focused=null){if(requests!=expected||focused is{}f&&row.PermissionsButton.IsFocused!=f)throw new InvalidOperationException("Headless row input mismatch "+id+" requests="+requests+" focus="+row.PermissionsButton.IsFocused);states.Add(new{id,requests,focused=row.PermissionsButton.IsFocused,english=row.English,key=row.Key,visible=row.PermissionsButton.IsVisible,attached=canvas.Children.Contains(row),background=row.RowBackground?.ToString(),button=row.PermissionsButton.Content,detailChildren=ControlAutomationPeer.CreatePeerForElement(row.Cells[3])!.GetChildren()![0].GetChildren()?.Count??0});}
  Point Centre(Control c)=>c.TranslatePoint(new(c.Bounds.Width/2,c.Bounds.Height/2),window)??throw new InvalidOperationException("Attached target required");
  void Click(Point p){window.MouseMove(p);window.MouseDown(p,MouseButton.Left);window.MouseUp(p,MouseButton.Left);}
  row.ColumnWidths=columns;row.SetPresentation("access",false,false);row.ReducedMotion=true;window.Activate();await Flush();Save("initial",0);
  var button=Centre(row.PermissionsButton);var passive=Centre(row.Cells[0]);var outside=new Point(1150,740);
  Click(button);await Flush();Save("pointer-permissions",1,true);
  Click(passive);await Flush();Save("passive-cell-blur",1,false);
  window.MouseMove(button);window.MouseDown(button,MouseButton.Left);window.MouseMove(outside);window.MouseUp(outside,MouseButton.Left);await Flush();Save("cancel-outside",1,true);
  row.PermissionsButton.Focus(NavigationMethod.Tab);window.KeyPress(Key.Space,RawInputModifiers.None,PhysicalKey.Space," ");await Flush();Save("space-held",1,true);window.KeyRelease(Key.Space,RawInputModifiers.None,PhysicalKey.Space," ");await Flush();Save("space-release",2,true);
  window.KeyPress(Key.Enter,RawInputModifiers.None,PhysicalKey.Enter,null);window.KeyRelease(Key.Enter,RawInputModifiers.None,PhysicalKey.Enter,null);await Flush();Save("enter",3,true);
  row.SetPresentation("access",true,false);await Flush();Save("english",3,true);
  row.SetPresentation("data",true,false);await Flush();Save("hide-permissions",3);if(row.PermissionsButton.IsVisible)throw new InvalidOperationException("Data has no permissions button");
  row.SetPresentation("access",false,false);await Flush();Save("restore-permissions",3);if(!row.PermissionsButton.IsVisible)throw new InvalidOperationException("Access permissions button missing");
  row.ReducedMotion=false;window.MouseMove(Centre(row.Cells[0]));await Task.Delay(190);await Flush();Save("hover-complete",3);if((row.RowBackground as ISolidColorBrush)?.Color!=Color.Parse("#3a3c44"))throw new InvalidOperationException("Row hover background missing");
  canvas.Children.Remove(row);await Flush();Save("detach",3);if((row.RowBackground as ISolidColorBrush)?.Color.A!=0)throw new InvalidOperationException("Detached row retains hover");
  canvas.Children.Add(row);row.ReducedMotion=true;window.MouseMove(outside);window.MouseMove(Centre(row.Cells[0]));await Flush();Save("remount-reduced-hover",3);
  window.MouseMove(outside);await Flush();Save("reduced-leave",3);if((row.RowBackground as ISolidColorBrush)?.Color.A!=0)throw new InvalidOperationException("Reduced row leave background mismatch");
  button=Centre(row.PermissionsButton);window.MouseMove(button);window.MouseDown(button,MouseButton.Left);canvas.Children.Remove(row);window.MouseUp(outside,MouseButton.Left);await Flush();Save("held-detach-cancel",3);
  canvas.Children.Add(row);await Flush();Save("held-remount",3);
  using(var image=window.CaptureRenderedFrame()??throw new InvalidOperationException("Real rendered headless frame missing"))image.Save(Path.Combine(output,"capabilityrow-headless-input.png"),new PngBitmapEncoderOptions());
  File.WriteAllText(Path.Combine(output,"capabilityrow-headless-input.json"),JsonSerializer.Serialize(new{scope="Rendered headless routing/lifecycle; actual OS input verification remains required",states},new JsonSerializerOptions{WriteIndented=true}));row.PermissionsRequested-=Requested;
 }
}
