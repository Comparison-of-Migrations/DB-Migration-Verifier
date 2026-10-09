using System;using System.Runtime.InteropServices;
public static class OwnedTableRowMouse {
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

public static class OwnedTableRowFixture {
 static string Read(string path){for(int i=0;i<20;i++){try{return System.IO.File.ReadAllText(path);}catch(System.IO.IOException){if(i==19)throw;System.Threading.Thread.Sleep(10);}}throw new InvalidOperationException("No stable fixture record");}
 public static string Run(string mode) {
  if(mode!="change-click"&&mode!="choice-click"&&mode!="choice-hold"&&mode!="target-click"&&mode!="hover"&&mode!="leave"&&mode!="choice-cancel")throw new InvalidOperationException("Fixed row operation required");
  var dpi=OwnedTableRowMouse.SetThreadDpiAwarenessContext(new IntPtr(-4));bool down=false;var previous=new OwnedTableRowMouse.POINT();OwnedTableRowMouse.GetPhysicalCursorPos(out previous);System.Windows.Automation.AutomationElement w=null;
  try{
   string path=System.IO.Path.Combine(System.Environment.GetEnvironmentVariable("TEMP"),"DBMV-TableMappingRow-20261007","app","ReplicaProbe.exe");
   foreach(System.Windows.Automation.AutomationElement x in System.Windows.Automation.AutomationElement.RootElement.FindAll(System.Windows.Automation.TreeScope.Children,System.Windows.Automation.Condition.TrueCondition)){if(x.Current.Name!="Replica TableMappingRow verification"||!string.Equals(System.Diagnostics.Process.GetProcessById(x.Current.ProcessId).MainModule.FileName,path,StringComparison.OrdinalIgnoreCase))continue;if(w!=null)throw new InvalidOperationException("Not unique");w=x;}
   if(w==null)throw new InvalidOperationException("Owned table row missing");
   var b=w.FindFirst(System.Windows.Automation.TreeScope.Descendants,new System.Windows.Automation.PropertyCondition(System.Windows.Automation.AutomationElement.AutomationIdProperty,"TableRowChange"));var c=w.FindFirst(System.Windows.Automation.TreeScope.Descendants,new System.Windows.Automation.PropertyCondition(System.Windows.Automation.AutomationElement.AutomationIdProperty,"TableRowCheck"));if(b==null||c==null)throw new InvalidOperationException("Exact table row controls missing");b.SetFocus();var br=b.Current.BoundingRectangle;var cr=c.Current.BoundingRectangle;double scale=cr.Width/20;if(Math.Abs(br.Height/scale-44)>.01||Math.Abs(cr.Height/scale-20)>.01||Math.Abs(br.Left-cr.Left-629.1015625*scale)>1)throw new InvalidOperationException("Geometry mismatch");
   int px=(int)Math.Round(mode=="choice-click"||mode=="choice-hold"||mode=="choice-cancel"?cr.Right+40*scale:mode=="target-click"||mode=="hover"?cr.Left+266.1015625*scale:mode=="leave"?w.Current.BoundingRectangle.Right-20*scale:br.Left+br.Width/2),py=(int)Math.Round(mode=="leave"?w.Current.BoundingRectangle.Bottom-40*scale:cr.Top+cr.Height/2);if(!w.Current.BoundingRectangle.Contains(new System.Windows.Point(px,py))||OwnedTableRowMouse.GetForegroundWindow().ToInt64()!=w.Current.NativeWindowHandle)throw new InvalidOperationException("Wrong foreground/frame");
   OwnedTableRowMouse.Move(px,py);System.Threading.Thread.Sleep(150);var actual=new OwnedTableRowMouse.POINT();OwnedTableRowMouse.GetPhysicalCursorPos(out actual);if(Math.Abs(actual.x-px)>1||Math.Abs(actual.y-py)>1||System.Windows.Automation.AutomationElement.FromPoint(new System.Windows.Point(px,py)).Current.ProcessId!=w.Current.ProcessId)throw new InvalidOperationException("Wrong pointer hit");
   string dir=System.IO.Path.GetDirectoryName(path);if(mode!="hover"&&mode!="leave"){OwnedTableRowMouse.Button(true);down=true;System.Threading.Thread.Sleep(mode=="choice-hold"||mode=="choice-cancel"?200:80);if(mode=="choice-hold")System.IO.File.WriteAllText(System.IO.Path.Combine(dir,"actual-held.json"),Read(System.IO.Path.Combine(dir,"tablemappingrow-input.json")));
   if(mode=="choice-cancel"){System.IO.File.WriteAllText(System.IO.Path.Combine(dir,"cancel-held-inside.json"),Read(System.IO.Path.Combine(dir,"tablemappingrow-input.json")));px=(int)Math.Round(w.Current.BoundingRectangle.Right-20*scale);py=(int)Math.Round(w.Current.BoundingRectangle.Bottom-40*scale);OwnedTableRowMouse.Move(px,py);System.Threading.Thread.Sleep(200);System.IO.File.WriteAllText(System.IO.Path.Combine(dir,"cancel-held-outside.json"),Read(System.IO.Path.Combine(dir,"tablemappingrow-input.json")));}OwnedTableRowMouse.Button(false);down=false;}if(OwnedTableRowMouse.GetForegroundWindow().ToInt64()!=w.Current.NativeWindowHandle)throw new InvalidOperationException("Lost foreground");System.Threading.Thread.Sleep(350);string output=Read(System.IO.Path.Combine(dir,"tablemappingrow-input.json"));System.IO.File.WriteAllText(System.IO.Path.Combine(dir,"actual-"+mode+".json"),output);return output;
  }finally{if(down)OwnedTableRowMouse.Button(false);if(w!=null&&OwnedTableRowMouse.GetForegroundWindow().ToInt64()==w.Current.NativeWindowHandle)OwnedTableRowMouse.Move(previous.x,previous.y);if(dpi!=IntPtr.Zero)OwnedTableRowMouse.SetThreadDpiAwarenessContext(dpi);}
 }
}
