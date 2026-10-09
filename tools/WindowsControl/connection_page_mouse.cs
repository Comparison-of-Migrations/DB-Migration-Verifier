using System;using System.Runtime.InteropServices;
public static class OwnedConnectionPageMouse {
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
public static class OwnedConnectionPageFixture {
 public static string Run(string path) {
  var temp=System.IO.Path.GetFullPath(System.Environment.GetEnvironmentVariable("TEMP"));var actual=System.IO.Path.GetFullPath(path);
  if(!actual.StartsWith(temp+System.IO.Path.DirectorySeparatorChar,StringComparison.OrdinalIgnoreCase)||!System.Text.RegularExpressions.Regex.IsMatch(actual.Substring(temp.Length+1),@"^DBMV-Isolated-connection-connectionpage-[0-9a-f]{32}\\app\\ReplicaProbe\.exe$"))throw new InvalidOperationException("Fixed isolated Connection path required");
  System.Windows.Automation.AutomationElement window=null;
  foreach(System.Windows.Automation.AutomationElement w in System.Windows.Automation.AutomationElement.RootElement.FindAll(System.Windows.Automation.TreeScope.Children,System.Windows.Automation.Condition.TrueCondition)) {
   if(w.Current.Name!="Replica ConnectionPage verification")continue;
   if(!string.Equals(System.Diagnostics.Process.GetProcessById(w.Current.ProcessId).MainModule.FileName,path,StringComparison.OrdinalIgnoreCase))continue;
   if(window!=null)throw new InvalidOperationException("Owned window not unique");window=w;
  }
  if(window==null)throw new InvalidOperationException("Owned window missing");
  var c=window.FindFirst(System.Windows.Automation.TreeScope.Descendants,new System.Windows.Automation.PropertyCondition(System.Windows.Automation.AutomationElement.AutomationIdProperty,"advanced"));
  if(c==null||!c.Current.IsEnabled)throw new InvalidOperationException("Advanced missing or disabled");var b=c.Current.BoundingRectangle;
  double scale=b.Height/44;if(scale<1||scale>3||b.Width/scale<130||b.Width/scale>180)throw new InvalidOperationException("Wrong advanced geometry");
  c.SetFocus();if(System.Windows.Automation.AutomationElement.FocusedElement.Current.ProcessId!=window.Current.ProcessId)throw new InvalidOperationException("Owned focus missing");
  int x=(int)Math.Round(b.Left+b.Width/2),y=(int)Math.Round(b.Top+b.Height/2);
  var previous=new OwnedConnectionPageMouse.POINT();OwnedConnectionPageMouse.GetCursorPos(out previous);bool down=false;
  try{OwnedConnectionPageMouse.Move(x,y);System.Threading.Thread.Sleep(150);if(OwnedConnectionPageMouse.GetForegroundWindow().ToInt64()!=window.Current.NativeWindowHandle)throw new InvalidOperationException("Owned foreground missing");var hit=System.Windows.Automation.AutomationElement.FromPoint(new System.Windows.Point(x,y));if(hit==null||hit.Current.ProcessId!=window.Current.ProcessId)throw new InvalidOperationException("Owned hit missing");OwnedConnectionPageMouse.Button(true);down=true;System.Threading.Thread.Sleep(25);OwnedConnectionPageMouse.Button(false);down=false;System.Threading.Thread.Sleep(300);}
  finally{if(down)OwnedConnectionPageMouse.Button(false);if(OwnedConnectionPageMouse.GetForegroundWindow().ToInt64()==window.Current.NativeWindowHandle)OwnedConnectionPageMouse.Move(previous.x,previous.y);}
  return System.IO.File.ReadAllText(System.IO.Path.Combine(System.IO.Path.GetDirectoryName(path),"connectionpage-input.json"));
 }
}
