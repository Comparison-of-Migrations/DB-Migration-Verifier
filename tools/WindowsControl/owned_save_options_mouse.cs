using System;using System.Runtime.InteropServices;
public static class OwnedSaveOptionsMouse {
 [StructLayout(LayoutKind.Sequential)] public struct POINT {public int x,y;}
 [StructLayout(LayoutKind.Sequential)] public struct MOUSEINPUT {public int dx,dy;public uint mouseData,dwFlags,time;public UIntPtr dwExtraInfo;}
 [StructLayout(LayoutKind.Sequential)] public struct INPUT {public uint type;public MOUSEINPUT mi;}
 [DllImport("user32.dll",SetLastError=true)] static extern uint SendInput(uint count,INPUT[] events,int size);
 [DllImport("user32.dll")] public static extern IntPtr GetForegroundWindow();
 [DllImport("user32.dll")] public static extern bool GetCursorPos(out POINT point);
 [DllImport("user32.dll")] static extern int GetSystemMetrics(int index);
 public static void Move(int x,int y) {
  int left=GetSystemMetrics(76),top=GetSystemMetrics(77),width=GetSystemMetrics(78),height=GetSystemMetrics(79);
  if(width<=1||height<=1||x<left||y<top||x>=left+width||y>=top+height)throw new InvalidOperationException("Target outside virtual display");
  Emit(new MOUSEINPUT{dx=(int)Math.Round((x-left)*65535.0/(width-1)),dy=(int)Math.Round((y-top)*65535.0/(height-1)),dwFlags=0x8000|0x4000|1});
 }
 public static void Button(bool down){Emit(new MOUSEINPUT{dwFlags=down?2u:4u});}
 static void Emit(MOUSEINPUT mouse){var input=new INPUT{type=0,mi=mouse};if(SendInput(1,new[]{input},Marshal.SizeOf<INPUT>())!=1)throw new InvalidOperationException("SendInput failed");}
}
public static class OwnedSaveOptionsFixture {
 public static string Run(int index,bool cancel) {
  if(index<0||index>2)throw new InvalidOperationException("DBMS index invalid");
  var windows=System.Windows.Automation.AutomationElement.RootElement.FindAll(System.Windows.Automation.TreeScope.Children,System.Windows.Automation.Condition.TrueCondition);System.Windows.Automation.AutomationElement window=null;
  string path=System.IO.Path.Combine(System.Environment.GetEnvironmentVariable("TEMP"),"DBMV-SaveOptions-20261008","app","ReplicaProbe.exe");
  foreach(System.Windows.Automation.AutomationElement w in windows){if(w.Current.Name!="Replica SaveOptions verification")continue;var process=System.Diagnostics.Process.GetProcessById(w.Current.ProcessId);if(!string.Equals(process.MainModule.FileName,path,StringComparison.OrdinalIgnoreCase))continue;if(window!=null)throw new InvalidOperationException("Owned DBMS window not unique");window=w;}
  if(window==null)throw new InvalidOperationException("Owned DBMS window missing");
  var radio=window.FindFirst(System.Windows.Automation.TreeScope.Descendants,new System.Windows.Automation.PropertyCondition(System.Windows.Automation.AutomationElement.AutomationIdProperty,"format-"+new[]{"json","csv","html"}[index]));if(radio==null)throw new InvalidOperationException("Owned radio missing");var r=radio.Current.BoundingRectangle;double scale=r.Width/20;if(scale<=0||Math.Abs(r.Height/20-scale)>.01)throw new InvalidOperationException("Wrong radio geometry");
  var ancestor=System.Windows.Automation.TreeWalker.ControlViewWalker.GetParent(radio);bool found=false;while(ancestor!=null&&ancestor!=window){var box=ancestor.Current.BoundingRectangle;if(Math.Abs(box.Width/scale-209.328125)<1/scale+.001&&box.Height/scale>=64&&box.Height/scale<300){found=true;break;}ancestor=System.Windows.Automation.TreeWalker.ControlViewWalker.GetParent(ancestor);}if(!found)throw new InvalidOperationException("Expected owned card geometry missing");
  var focusTarget=radio.Current.IsEnabled?radio:window.FindFirst(System.Windows.Automation.TreeScope.Descendants,new System.Windows.Automation.PropertyCondition(System.Windows.Automation.AutomationElement.AutomationIdProperty,"format-"+new[]{"json","csv","html"}[index]));if(focusTarget==null)throw new InvalidOperationException("Owned focus target missing");focusTarget.SetFocus();if(System.Windows.Automation.AutomationElement.FocusedElement.Current.ProcessId!=window.Current.ProcessId)throw new InvalidOperationException("Wrong focus");
  int x=(int)Math.Round(r.Left+100*scale),y=(int)Math.Round(r.Top+10*scale);var card=ancestor.Current.BoundingRectangle;if(!card.Contains(new System.Windows.Point(x,y)))throw new InvalidOperationException("Click outside card");if(OwnedSaveOptionsMouse.GetForegroundWindow().ToInt64()!=window.Current.NativeWindowHandle)throw new InvalidOperationException("Owned DBMS window not foreground");
  var previous=new OwnedSaveOptionsMouse.POINT();OwnedSaveOptionsMouse.GetCursorPos(out previous);bool down=false;
  try {OwnedSaveOptionsMouse.Move(x,y);System.Threading.Thread.Sleep(180);if(OwnedSaveOptionsMouse.GetForegroundWindow().ToInt64()!=window.Current.NativeWindowHandle)throw new InvalidOperationException("Lost foreground");var hit=System.Windows.Automation.AutomationElement.FromPoint(new System.Windows.Point(x,y));if(hit==null||hit.Current.ProcessId!=window.Current.ProcessId)throw new InvalidOperationException("Pointer hit outside owned app");OwnedSaveOptionsMouse.Button(true);down=true;System.Threading.Thread.Sleep(280);if(cancel){var w=window.Current.BoundingRectangle;OwnedSaveOptionsMouse.Move((int)(w.Right-30*scale),y);System.Threading.Thread.Sleep(50);}OwnedSaveOptionsMouse.Button(false);down=false;System.Threading.Thread.Sleep(350);}
  finally{if(down)OwnedSaveOptionsMouse.Button(false);if(OwnedSaveOptionsMouse.GetForegroundWindow().ToInt64()==window.Current.NativeWindowHandle)OwnedSaveOptionsMouse.Move(previous.x,previous.y);}
  return System.IO.File.ReadAllText(System.IO.Path.Combine(System.IO.Path.GetDirectoryName(path),"saveoptions-input.json"));
 }
}
