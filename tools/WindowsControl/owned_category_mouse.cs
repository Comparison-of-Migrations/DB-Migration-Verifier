using System;using System.Runtime.InteropServices;
public static class OwnedCategoryMouse {
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

public static class OwnedCategoryFixture {
 public static string Run(string mode) {
  if(mode!="button-click"&&mode!="choice-click"&&mode!="button-hold")throw new InvalidOperationException("Fixed header operation required");
  var dpi=OwnedCategoryMouse.SetThreadDpiAwarenessContext(new IntPtr(-4));bool down=false;var previous=new OwnedCategoryMouse.POINT();OwnedCategoryMouse.GetPhysicalCursorPos(out previous);System.Windows.Automation.AutomationElement w=null;
  try{
   string path=System.IO.Path.Combine(System.Environment.GetEnvironmentVariable("TEMP"),"DBMV-CategoryHeader-20261007","app","ReplicaProbe.exe");
   foreach(System.Windows.Automation.AutomationElement x in System.Windows.Automation.AutomationElement.RootElement.FindAll(System.Windows.Automation.TreeScope.Children,System.Windows.Automation.Condition.TrueCondition)){if(x.Current.Name!="Replica CategoryHeader verification"||!string.Equals(System.Diagnostics.Process.GetProcessById(x.Current.ProcessId).MainModule.FileName,path,StringComparison.OrdinalIgnoreCase))continue;if(w!=null)throw new InvalidOperationException("Not unique");w=x;}
   if(w==null)throw new InvalidOperationException("Owned category missing");
   var b=w.FindFirst(System.Windows.Automation.TreeScope.Descendants,new System.Windows.Automation.PropertyCondition(System.Windows.Automation.AutomationElement.AutomationIdProperty,"CategoryToggle"));var c=w.FindFirst(System.Windows.Automation.TreeScope.Descendants,new System.Windows.Automation.PropertyCondition(System.Windows.Automation.AutomationElement.AutomationIdProperty,"CategoryCheck"));if(b==null||c==null)throw new InvalidOperationException("Exact category controls missing");b.SetFocus();var br=b.Current.BoundingRectangle;var cr=c.Current.BoundingRectangle;double scale=br.Width/858;if(Math.Abs(br.Height/scale-64)>.01||Math.Abs(cr.Width/scale-20)>.01)throw new InvalidOperationException("Geometry mismatch");
   int px=(int)Math.Round(mode=="choice-click"?cr.Left-8*scale:br.Left+br.Width*.7),py=(int)Math.Round(br.Top+br.Height/2);if(!br.Contains(new System.Windows.Point(px,py))||!w.Current.BoundingRectangle.Contains(new System.Windows.Point(px,py))||OwnedCategoryMouse.GetForegroundWindow().ToInt64()!=w.Current.NativeWindowHandle)throw new InvalidOperationException("Wrong foreground/frame");
   OwnedCategoryMouse.Move(px,py);System.Threading.Thread.Sleep(150);var actual=new OwnedCategoryMouse.POINT();OwnedCategoryMouse.GetPhysicalCursorPos(out actual);if(Math.Abs(actual.x-px)>1||Math.Abs(actual.y-py)>1||System.Windows.Automation.AutomationElement.FromPoint(new System.Windows.Point(px,py)).Current.ProcessId!=w.Current.ProcessId)throw new InvalidOperationException("Wrong pointer hit");
   OwnedCategoryMouse.Button(true);down=true;System.Threading.Thread.Sleep(mode=="button-hold"?200:80);string dir=System.IO.Path.GetDirectoryName(path);if(mode=="button-hold")System.IO.File.Copy(System.IO.Path.Combine(dir,"categoryheader-input.json"),System.IO.Path.Combine(dir,"actual-held.json"),true);
   OwnedCategoryMouse.Button(false);down=false;if(OwnedCategoryMouse.GetForegroundWindow().ToInt64()!=w.Current.NativeWindowHandle)throw new InvalidOperationException("Lost foreground");System.Threading.Thread.Sleep(320);string output=System.IO.File.ReadAllText(System.IO.Path.Combine(dir,"categoryheader-input.json"));System.IO.File.WriteAllText(System.IO.Path.Combine(dir,"actual-"+mode+".json"),output);return output;
  }finally{if(down)OwnedCategoryMouse.Button(false);if(w!=null&&OwnedCategoryMouse.GetForegroundWindow().ToInt64()==w.Current.NativeWindowHandle)OwnedCategoryMouse.Move(previous.x,previous.y);if(dpi!=IntPtr.Zero)OwnedCategoryMouse.SetThreadDpiAwarenessContext(dpi);}
 }
}
