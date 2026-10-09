using System;using System.Runtime.InteropServices;
public static class OwnedSidebarMouse {
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

public static class OwnedSidebarFixture {
 public static string Run(string mode) {
  if(mode!="blocked-connect")throw new InvalidOperationException("Fixed header operation required");
  var dpi=OwnedSidebarMouse.SetThreadDpiAwarenessContext(new IntPtr(-4));bool down=false;var previous=new OwnedSidebarMouse.POINT();OwnedSidebarMouse.GetPhysicalCursorPos(out previous);System.Windows.Automation.AutomationElement w=null;
  try{
   string path=System.IO.Path.Combine(System.Environment.GetEnvironmentVariable("TEMP"),"DBMV-Sidebar-20261007","app","ReplicaProbe.exe");
   foreach(System.Windows.Automation.AutomationElement x in System.Windows.Automation.AutomationElement.RootElement.FindAll(System.Windows.Automation.TreeScope.Children,System.Windows.Automation.Condition.TrueCondition)){if(x.Current.Name!="Replica Sidebar verification"||!string.Equals(System.Diagnostics.Process.GetProcessById(x.Current.ProcessId).MainModule.FileName,path,StringComparison.OrdinalIgnoreCase))continue;if(w!=null)throw new InvalidOperationException("Not unique");w=x;}
   if(w==null)throw new InvalidOperationException("Owned category missing");
   var b=w.FindFirst(System.Windows.Automation.TreeScope.Descendants,new System.Windows.Automation.PropertyCondition(System.Windows.Automation.AutomationElement.AutomationIdProperty,"Sidebar-connect"));var anchor=w.FindFirst(System.Windows.Automation.TreeScope.Descendants,new System.Windows.Automation.PropertyCondition(System.Windows.Automation.AutomationElement.AutomationIdProperty,"Sidebar-locale"));if(b==null||anchor==null||b.Current.IsEnabled)throw new InvalidOperationException("Expected owned disabled sidebar item");anchor.SetFocus();System.Threading.Thread.Sleep(150);var br=b.Current.BoundingRectangle;double scale=br.Height/52;if(Math.Abs(scale-2)>.01||Math.Abs(br.Width/scale-177)>.01)throw new InvalidOperationException("Owned Sidebar geometry mismatch");
   int px=(int)Math.Round(br.Left+br.Width/2),py=(int)Math.Round(br.Top+br.Height/2);if(!br.Contains(new System.Windows.Point(px,py))||!w.Current.BoundingRectangle.Contains(new System.Windows.Point(px,py))||OwnedSidebarMouse.GetForegroundWindow().ToInt64()!=w.Current.NativeWindowHandle)throw new InvalidOperationException("Wrong foreground/frame");
   OwnedSidebarMouse.Move(px,py);System.Threading.Thread.Sleep(150);var actual=new OwnedSidebarMouse.POINT();OwnedSidebarMouse.GetPhysicalCursorPos(out actual);if(Math.Abs(actual.x-px)>1||Math.Abs(actual.y-py)>1||System.Windows.Automation.AutomationElement.FromPoint(new System.Windows.Point(px,py)).Current.ProcessId!=w.Current.ProcessId)throw new InvalidOperationException("Wrong pointer hit");
   OwnedSidebarMouse.Button(true);down=true;System.Threading.Thread.Sleep(80);string dir=System.IO.Path.GetDirectoryName(path);
   OwnedSidebarMouse.Button(false);down=false;if(OwnedSidebarMouse.GetForegroundWindow().ToInt64()!=w.Current.NativeWindowHandle)throw new InvalidOperationException("Lost foreground");System.Threading.Thread.Sleep(320);var record=w.FindFirst(System.Windows.Automation.TreeScope.Descendants,new System.Windows.Automation.PropertyCondition(System.Windows.Automation.AutomationElement.AutomationIdProperty,"Sidebar-record"));if(record==null)throw new InvalidOperationException("Owned record missing");((System.Windows.Automation.InvokePattern)record.GetCurrentPattern(System.Windows.Automation.InvokePattern.Pattern)).Invoke();System.Threading.Thread.Sleep(100);string output=System.IO.File.ReadAllText(System.IO.Path.Combine(dir,"sidebar-input.json"));System.IO.File.WriteAllText(System.IO.Path.Combine(dir,"actual-"+mode+".json"),output);return output;
  }finally{if(down)OwnedSidebarMouse.Button(false);if(w!=null&&OwnedSidebarMouse.GetForegroundWindow().ToInt64()==w.Current.NativeWindowHandle)OwnedSidebarMouse.Move(previous.x,previous.y);if(dpi!=IntPtr.Zero)OwnedSidebarMouse.SetThreadDpiAwarenessContext(dpi);}
 }
}
