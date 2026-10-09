using System;using System.Runtime.InteropServices;
public static class OwnedFieldMouse {
 [StructLayout(LayoutKind.Sequential)] public struct POINT {public int x,y;}
 [StructLayout(LayoutKind.Sequential)] public struct MOUSEINPUT {public int dx,dy;public uint mouseData,dwFlags,time;public UIntPtr dwExtraInfo;}
 [StructLayout(LayoutKind.Sequential)] public struct INPUT {public uint type;public MOUSEINPUT mi;}
 [DllImport("user32.dll",SetLastError=true)] static extern uint SendInput(uint count,INPUT[] events,int size);
 [DllImport("user32.dll")] public static extern IntPtr GetForegroundWindow();
 [DllImport("user32.dll")] public static extern bool GetCursorPos(out POINT point);
 [DllImport("user32.dll")] public static extern bool GetPhysicalCursorPos(out POINT point);
 [DllImport("user32.dll")] public static extern IntPtr SetThreadDpiAwarenessContext(IntPtr context);
 [DllImport("user32.dll")] static extern int GetSystemMetrics(int index);
 public static void Move(int x,int y) {
  int left=GetSystemMetrics(76),top=GetSystemMetrics(77),width=GetSystemMetrics(78),height=GetSystemMetrics(79);
  if(width<=1||height<=1||x<left||y<top||x>=left+width||y>=top+height)throw new InvalidOperationException("Target outside virtual display");
  Emit(new MOUSEINPUT{dx=(int)Math.Round((x-left)*65535.0/(width-1)),dy=(int)Math.Round((y-top)*65535.0/(height-1)),dwFlags=0x8000|0x4000|1});
 }
 public static void Button(bool down){Emit(new MOUSEINPUT{dwFlags=down?2u:4u});}
 static void Emit(MOUSEINPUT mouse){var input=new INPUT{type=0,mi=mouse};if(SendInput(1,new[]{input},Marshal.SizeOf<INPUT>())!=1)throw new InvalidOperationException("SendInput failed");}
}
// Explicit project verification control approval, 2026-10-07. Exact Field fixture only.
public static class OwnedFieldFixture {
 public static string Run(int index) {
  var priorDpi=OwnedFieldMouse.SetThreadDpiAwarenessContext(new IntPtr(-4));
  try{return RunAware(index);}finally{if(priorDpi!=IntPtr.Zero)OwnedFieldMouse.SetThreadDpiAwarenessContext(priorDpi);}
 }
 static string RunAware(int index) {
  if(index<0||index>2)throw new InvalidOperationException("Field index invalid");
  var windows=System.Windows.Automation.AutomationElement.RootElement.FindAll(System.Windows.Automation.TreeScope.Children,System.Windows.Automation.Condition.TrueCondition);System.Windows.Automation.AutomationElement window=null;
  string path=System.IO.Path.Combine(System.Environment.GetEnvironmentVariable("TEMP"),"DBMV-Field-20261007","app","ReplicaProbe.exe");
  foreach(System.Windows.Automation.AutomationElement w in windows){if(w.Current.Name!="Replica Field verification")continue;var process=System.Diagnostics.Process.GetProcessById(w.Current.ProcessId);if(!string.Equals(process.MainModule.FileName,path,StringComparison.OrdinalIgnoreCase))continue;if(window!=null)throw new InvalidOperationException("Owned Field window not unique");window=w;}
  if(window==null)throw new InvalidOperationException("Owned Field window missing");
  var label=window.FindFirst(System.Windows.Automation.TreeScope.Descendants,new System.Windows.Automation.PropertyCondition(System.Windows.Automation.AutomationElement.AutomationIdProperty,"FieldLabel-"+index));if(label==null)throw new InvalidOperationException("Owned label missing");
  var r=label.Current.BoundingRectangle;double width=index==2?112:180,scale=r.Width/width;if(scale<=0||Math.Abs(r.Height/scale-19.1953125)>.6)throw new InvalidOperationException("Wrong label geometry");
  var preFocus=window.FindFirst(System.Windows.Automation.TreeScope.Descendants,new System.Windows.Automation.PropertyCondition(System.Windows.Automation.AutomationElement.AutomationIdProperty,"FieldEditor-0"));if(preFocus==null||!preFocus.Current.IsEnabled)throw new InvalidOperationException("Owned pre-focus editor missing");preFocus.SetFocus();if(System.Windows.Automation.AutomationElement.FocusedElement.Current.ProcessId!=window.Current.ProcessId)throw new InvalidOperationException("Wrong focus");
  int x=(int)Math.Round(r.Left+r.Width*.7),y=(int)Math.Round(r.Top+r.Height*.5);var frame=window.Current.BoundingRectangle;if(!r.Contains(new System.Windows.Point(x,y))||!frame.Contains(new System.Windows.Point(x,y)))throw new InvalidOperationException("Click outside owned label");if(OwnedFieldMouse.GetForegroundWindow().ToInt64()!=window.Current.NativeWindowHandle)throw new InvalidOperationException("Owned Field window not foreground");
  var previous=new OwnedFieldMouse.POINT();OwnedFieldMouse.GetCursorPos(out previous);bool down=false;string result=null;
  try {OwnedFieldMouse.Move(x,y);System.Threading.Thread.Sleep(100);if(OwnedFieldMouse.GetForegroundWindow().ToInt64()!=window.Current.NativeWindowHandle)throw new InvalidOperationException("Lost foreground");var actual=new OwnedFieldMouse.POINT();if(!OwnedFieldMouse.GetPhysicalCursorPos(out actual)||Math.Abs(actual.x-x)>1||Math.Abs(actual.y-y)>1)throw new InvalidOperationException("Physical pointer does not match label coordinates: "+actual.x+","+actual.y+" expected "+x+","+y);var hit=System.Windows.Automation.AutomationElement.FromPoint(new System.Windows.Point(actual.x,actual.y));if(hit==null||hit.Current.ProcessId!=window.Current.ProcessId)throw new InvalidOperationException("Pointer is not over the owned fixture");OwnedFieldMouse.Button(true);down=true;System.Threading.Thread.Sleep(120);if(OwnedFieldMouse.GetForegroundWindow().ToInt64()!=window.Current.NativeWindowHandle)throw new InvalidOperationException("Lost foreground before release");OwnedFieldMouse.Button(false);down=false;System.Threading.Thread.Sleep(250);var directory=System.IO.Path.GetDirectoryName(path);System.IO.File.Copy(System.IO.Path.Combine(directory,"field-events.json"),System.IO.Path.Combine(directory,"field-pointer-events-"+index+".json"),true);result=System.IO.File.ReadAllText(System.IO.Path.Combine(directory,"field-input.json"));}
  finally{if(down)OwnedFieldMouse.Button(false);if(OwnedFieldMouse.GetForegroundWindow().ToInt64()==window.Current.NativeWindowHandle)OwnedFieldMouse.Move(previous.x,previous.y);}
  return result;
 }
}
public static class OwnedFieldSource {
 public static string Run(int index) {
  if(index<0||index>2)throw new InvalidOperationException("Field index invalid");
  var windows=System.Windows.Automation.AutomationElement.RootElement.FindAll(System.Windows.Automation.TreeScope.Children,System.Windows.Automation.Condition.TrueCondition);System.Windows.Automation.AutomationElement window=null;
  foreach(System.Windows.Automation.AutomationElement w in windows){if(!w.Current.Name.StartsWith("Field · original isolation")||System.Diagnostics.Process.GetProcessById(w.Current.ProcessId).ProcessName!="msedge")continue;if(window!=null)throw new InvalidOperationException("Owned source not unique");window=w;}
  if(window==null)throw new InvalidOperationException("Owned source missing");
  var condition=new System.Windows.Automation.PropertyCondition(System.Windows.Automation.AutomationElement.AutomationIdProperty,"ko-180-0");var first=window.FindFirst(System.Windows.Automation.TreeScope.Descendants,condition);if(first==null)throw new InvalidOperationException("Owned source first input missing");first.SetFocus();var deadline=DateTime.UtcNow.AddSeconds(2);while(System.Windows.Automation.AutomationElement.FocusedElement.Current.AutomationId!="ko-180-0"&&DateTime.UtcNow<deadline)System.Threading.Thread.Sleep(30);if(System.Windows.Automation.AutomationElement.FocusedElement.Current.AutomationId!="ko-180-0")throw new InvalidOperationException("Owned source pre-focus missing");
  string id=new[]{"ko-180-0","ko-180-3","ko-port"}[index];var target=window.FindFirst(System.Windows.Automation.TreeScope.Descendants,new System.Windows.Automation.PropertyCondition(System.Windows.Automation.AutomationElement.AutomationIdProperty,id));if(target==null)throw new InvalidOperationException("Owned target missing");((System.Windows.Automation.ScrollItemPattern)target.GetCurrentPattern(System.Windows.Automation.ScrollItemPattern.Pattern)).ScrollIntoView();System.Threading.Thread.Sleep(120);
  var dpi=OwnedFieldMouse.SetThreadDpiAwarenessContext(new IntPtr(-4));bool down=false;var previous=new OwnedFieldMouse.POINT();OwnedFieldMouse.GetPhysicalCursorPos(out previous);
  try {
   var r=target.Current.BoundingRectangle;double scale=r.Width/(index==2?112:180);if(scale<=0||Math.Abs(r.Height/scale-44)>1)throw new InvalidOperationException("Wrong source geometry");int x=(int)Math.Round(r.Left+r.Width*.7),y=(int)Math.Round(r.Top-(26.1953125-19.1953125/2)*scale);if(!window.Current.BoundingRectangle.Contains(new System.Windows.Point(x,y))||OwnedFieldMouse.GetForegroundWindow().ToInt64()!=window.Current.NativeWindowHandle)throw new InvalidOperationException("Wrong source foreground/frame");
   OwnedFieldMouse.Move(x,y);System.Threading.Thread.Sleep(100);var actual=new OwnedFieldMouse.POINT();if(!OwnedFieldMouse.GetPhysicalCursorPos(out actual)||Math.Abs(actual.x-x)>1||Math.Abs(actual.y-y)>1)throw new InvalidOperationException("Source pointer mismatch");var hit=System.Windows.Automation.AutomationElement.FromPoint(new System.Windows.Point(x,y));if(hit.Current.ProcessId!=target.Current.ProcessId)throw new InvalidOperationException("Source hit not owned");
   OwnedFieldMouse.Button(true);down=true;System.Threading.Thread.Sleep(120);if(OwnedFieldMouse.GetForegroundWindow().ToInt64()!=window.Current.NativeWindowHandle)throw new InvalidOperationException("Source lost foreground");OwnedFieldMouse.Button(false);down=false;System.Threading.Thread.Sleep(180);string active=System.Windows.Automation.AutomationElement.FocusedElement.Current.AutomationId;if(active!="ko-180-0"&&active!="ko-180-3"&&active!="ko-port")active="";return "{\"target\":\""+id+"\",\"active\":\""+active+"\",\"position\":["+x+","+y+"]}";
  }finally{if(down)OwnedFieldMouse.Button(false);if(OwnedFieldMouse.GetForegroundWindow().ToInt64()==window.Current.NativeWindowHandle)OwnedFieldMouse.Move(previous.x,previous.y);if(dpi!=IntPtr.Zero)OwnedFieldMouse.SetThreadDpiAwarenessContext(dpi);}
 }
}
