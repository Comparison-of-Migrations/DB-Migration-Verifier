using System;using System.Runtime.InteropServices;
public static class OwnedChoiceMouse {
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
public static class OwnedChoiceFixture {
 public static string Run(int index,bool cancel) {
  if(index<0||index>2)throw new InvalidOperationException("DBMS index invalid");
  var windows=System.Windows.Automation.AutomationElement.RootElement.FindAll(System.Windows.Automation.TreeScope.Children,System.Windows.Automation.Condition.TrueCondition);System.Windows.Automation.AutomationElement window=null;
  string path=System.IO.Path.Combine(System.Environment.GetEnvironmentVariable("TEMP"),"DBMV-Choice-20261008","app","ReplicaProbe.exe");
  foreach(System.Windows.Automation.AutomationElement w in windows){if(w.Current.Name!="Replica Choice verification")continue;var process=System.Diagnostics.Process.GetProcessById(w.Current.ProcessId);if(!string.Equals(process.MainModule.FileName,path,StringComparison.OrdinalIgnoreCase))continue;if(window!=null)throw new InvalidOperationException("Owned DBMS window not unique");window=w;}
  if(window==null)throw new InvalidOperationException("Owned DBMS window missing");
  var radio=window.FindFirst(System.Windows.Automation.TreeScope.Descendants,new System.Windows.Automation.PropertyCondition(System.Windows.Automation.AutomationElement.AutomationIdProperty,"flag-stopped"));if(radio==null)throw new InvalidOperationException("Owned radio missing");var r=radio.Current.BoundingRectangle;double scale=r.Width/20;if(scale<=0||Math.Abs(r.Height/20-scale)>.01)throw new InvalidOperationException("Wrong radio geometry");
  var label=window.FindFirst(System.Windows.Automation.TreeScope.Descendants,new System.Windows.Automation.AndCondition(new System.Windows.Automation.PropertyCondition(System.Windows.Automation.AutomationElement.ControlTypeProperty,System.Windows.Automation.ControlType.Text),new System.Windows.Automation.PropertyCondition(System.Windows.Automation.AutomationElement.NameProperty,radio.Current.Name)));if(label==null)throw new InvalidOperationException("Owned label missing");var card=label.Current.BoundingRectangle;if(Math.Abs((card.Left-r.Right)/scale-12)>.02||card.Width/scale<30||card.Width/scale>200)throw new InvalidOperationException("Expected owned label geometry missing");
  var focusTarget=radio.Current.IsEnabled?radio:window.FindFirst(System.Windows.Automation.TreeScope.Descendants,new System.Windows.Automation.PropertyCondition(System.Windows.Automation.AutomationElement.AutomationIdProperty,"Choice-disabled"));if(focusTarget==null)throw new InvalidOperationException("Owned focus target missing");focusTarget.SetFocus();if(System.Windows.Automation.AutomationElement.FocusedElement.Current.ProcessId!=window.Current.ProcessId)throw new InvalidOperationException("Wrong focus");
  int x=(int)Math.Round(card.Left+30*scale),y=(int)Math.Round(card.Top+10*scale);if(!card.Contains(new System.Windows.Point(x,y)))throw new InvalidOperationException("Click outside card");if(OwnedChoiceMouse.GetForegroundWindow().ToInt64()!=window.Current.NativeWindowHandle)throw new InvalidOperationException("Owned DBMS window not foreground");
  var previous=new OwnedChoiceMouse.POINT();OwnedChoiceMouse.GetCursorPos(out previous);bool down=false;
  try {OwnedChoiceMouse.Move(x,y);System.Threading.Thread.Sleep(180);if(OwnedChoiceMouse.GetForegroundWindow().ToInt64()!=window.Current.NativeWindowHandle)throw new InvalidOperationException("Lost foreground");var hit=System.Windows.Automation.AutomationElement.FromPoint(new System.Windows.Point(x,y));if(hit==null||hit.Current.ProcessId!=window.Current.ProcessId)throw new InvalidOperationException("Pointer hit outside owned app");OwnedChoiceMouse.Button(true);down=true;System.Threading.Thread.Sleep(280);if(cancel){var w=window.Current.BoundingRectangle;OwnedChoiceMouse.Move((int)(w.Right-30*scale),y);System.Threading.Thread.Sleep(50);}OwnedChoiceMouse.Button(false);down=false;System.Threading.Thread.Sleep(350);}
  finally{if(down)OwnedChoiceMouse.Button(false);if(OwnedChoiceMouse.GetForegroundWindow().ToInt64()==window.Current.NativeWindowHandle)OwnedChoiceMouse.Move(previous.x,previous.y);}
  return System.IO.File.ReadAllText(System.IO.Path.Combine(System.IO.Path.GetDirectoryName(path),"choice-input.json"));
 }
}
