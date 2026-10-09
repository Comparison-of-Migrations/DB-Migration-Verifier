using System;using System.Runtime.InteropServices;
public static class OwnedResultRowMouse {
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


public static class OwnedResultRowFixture {
 static string Read(string p){for(int i=0;i<20;i++){try{return System.IO.File.ReadAllText(p);}catch(System.IO.IOException){if(i==19)throw;System.Threading.Thread.Sleep(10);}}throw new InvalidOperationException("No record");}
 public static string Run(string mode){
  if(mode!="title-click"&&mode!="icon-click"&&mode!="count-click"&&mode!="chevron-click"&&mode!="hold"&&mode!="cancel"&&mode!="hover"&&mode!="leave")throw new InvalidOperationException("Fixed row mode required");
  var dpi=OwnedResultRowMouse.SetThreadDpiAwarenessContext(new IntPtr(-4));bool down=false;var previous=new OwnedResultRowMouse.POINT();OwnedResultRowMouse.GetPhysicalCursorPos(out previous);System.Windows.Automation.AutomationElement w=null;
  try{string app=System.IO.Path.Combine(System.Environment.GetEnvironmentVariable("TEMP"),"DBMV-ResultRow-20261007","app"),path=System.IO.Path.Combine(app,"ReplicaProbe.exe");
   foreach(System.Windows.Automation.AutomationElement x in System.Windows.Automation.AutomationElement.RootElement.FindAll(System.Windows.Automation.TreeScope.Children,System.Windows.Automation.Condition.TrueCondition)){if(x.Current.Name!="Replica ResultSummaryRow verification"||!string.Equals(System.Diagnostics.Process.GetProcessById(x.Current.ProcessId).MainModule.FileName,path,StringComparison.OrdinalIgnoreCase))continue;if(w!=null)throw new InvalidOperationException("Not unique");w=x;}
   if(w==null)throw new InvalidOperationException("Owned row missing");
   var b=w.FindFirst(System.Windows.Automation.TreeScope.Descendants,new System.Windows.Automation.PropertyCondition(System.Windows.Automation.AutomationElement.AutomationIdProperty,"ResultRowTarget"));var peer=w.FindFirst(System.Windows.Automation.TreeScope.Descendants,new System.Windows.Automation.PropertyCondition(System.Windows.Automation.AutomationElement.AutomationIdProperty,"ResultRowPeer"));if(b==null||peer==null)throw new InvalidOperationException("Owned rows missing");
   if(b.Current.IsEnabled)b.SetFocus();else peer.SetFocus();var br=b.Current.BoundingRectangle;var pr=peer.Current.BoundingRectangle;double scale=br.Width/858;if(Math.Abs(br.Height/scale-82)>.01||Math.Abs((pr.Top-br.Top)/scale-112)>.01||Math.Abs(pr.Width-br.Width)>.01)throw new InvalidOperationException("Row geometry mismatch");
   int px=(int)Math.Round(mode=="leave"?w.Current.BoundingRectangle.Right-20*scale:br.Left+(mode=="icon-click"?35:mode=="title-click"?100:mode=="chevron-click"?830:440)*scale),py=(int)Math.Round(mode=="leave"?w.Current.BoundingRectangle.Bottom-40*scale:br.Top+41*scale);
   if(!w.Current.BoundingRectangle.Contains(new System.Windows.Point(px,py))||OwnedResultRowMouse.GetForegroundWindow().ToInt64()!=w.Current.NativeWindowHandle)throw new InvalidOperationException("Wrong foreground/frame");OwnedResultRowMouse.Move(px,py);System.Threading.Thread.Sleep(180);var actual=new OwnedResultRowMouse.POINT();OwnedResultRowMouse.GetPhysicalCursorPos(out actual);if(Math.Abs(actual.x-px)>1||Math.Abs(actual.y-py)>1||System.Windows.Automation.AutomationElement.FromPoint(new System.Windows.Point(px,py)).Current.ProcessId!=w.Current.ProcessId)throw new InvalidOperationException("Wrong hit");
   string input=System.IO.Path.Combine(app,"resultrow-input.json");if(mode!="hover"&&mode!="leave"){OwnedResultRowMouse.Button(true);down=true;System.Threading.Thread.Sleep(mode=="hold"||mode=="cancel"?200:80);if(mode=="hold"||mode=="cancel")System.IO.File.WriteAllText(System.IO.Path.Combine(app,mode+"-inside.json"),Read(input));
    if(mode=="cancel"){px=(int)Math.Round(w.Current.BoundingRectangle.Right-20*scale);py=(int)Math.Round(w.Current.BoundingRectangle.Bottom-40*scale);OwnedResultRowMouse.Move(px,py);System.Threading.Thread.Sleep(200);System.IO.File.WriteAllText(System.IO.Path.Combine(app,"cancel-outside.json"),Read(input));}OwnedResultRowMouse.Button(false);down=false;
   }if(OwnedResultRowMouse.GetForegroundWindow().ToInt64()!=w.Current.NativeWindowHandle)throw new InvalidOperationException("Lost foreground");System.Threading.Thread.Sleep(350);return Read(input);
  }finally{if(down)OwnedResultRowMouse.Button(false);if(w!=null&&OwnedResultRowMouse.GetForegroundWindow().ToInt64()==w.Current.NativeWindowHandle)OwnedResultRowMouse.Move(previous.x,previous.y);if(dpi!=IntPtr.Zero)OwnedResultRowMouse.SetThreadDpiAwarenessContext(dpi);}
 }
}
