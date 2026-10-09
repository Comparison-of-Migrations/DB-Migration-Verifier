using System;using System.Runtime.InteropServices;using System.Windows.Automation;
public static class OwnedProgressInput {
 [StructLayout(LayoutKind.Sequential)]struct MOUSE{public int x,y;public uint data,flags,time;public UIntPtr extra;}
 [StructLayout(LayoutKind.Sequential)]struct KEY{public ushort vk,scan;public uint flags,time;public UIntPtr extra;}
 [StructLayout(LayoutKind.Explicit,Size=32)]struct UNION{[FieldOffset(0)]public MOUSE mouse;[FieldOffset(0)]public KEY key;}
 [StructLayout(LayoutKind.Sequential)]struct INPUT{public uint type;public UNION data;}
 [StructLayout(LayoutKind.Sequential)]struct POINT{public int x,y;}
 [DllImport("user32.dll",SetLastError=true)]static extern uint SendInput(uint n,INPUT[] input,int size);
 [DllImport("user32.dll")]static extern int GetSystemMetrics(int n);
 [DllImport("user32.dll")]static extern bool GetPhysicalCursorPos(out POINT p);
 [DllImport("user32.dll")]static extern IntPtr SetThreadDpiAwarenessContext(IntPtr c);
 [DllImport("user32.dll")]static extern IntPtr GetForegroundWindow();
 [DllImport("user32.dll")]static extern bool SetForegroundWindow(IntPtr w);
 static void Emit(INPUT i){if(SendInput(1,new[]{i},Marshal.SizeOf<INPUT>())!=1)throw new InvalidOperationException("SendInput failed");}
 static void Key(ushort code,bool down){Emit(new INPUT{type=1,data=new UNION{key=new KEY{vk=code,flags=down?0u:2u}}});}
 static void Move(int x,int y){int l=GetSystemMetrics(76),t=GetSystemMetrics(77),w=GetSystemMetrics(78),h=GetSystemMetrics(79);if(w<=1||h<=1||x<l||x>=l+w||y<t||y>=t+h)throw new InvalidOperationException("Physical screen point invalid");Emit(new INPUT{type=0,data=new UNION{mouse=new MOUSE{x=(int)Math.Round((x-l)*65535d/(w-1)),y=(int)Math.Round((y-t)*65535d/(h-1)),flags=0xC001}}});}
 public static string Run(string app,string mode){
  if(!System.Text.RegularExpressions.Regex.IsMatch(app,@"\\DBMV-Isolated-root-progresspage-[0-9a-f]{32}\\app$"))throw new InvalidOperationException("Owned frozen Progress app required");
  if(mode!="hover"&&mode!="leave"&&mode!="F5"&&mode!="F6"&&mode!="F7"&&mode!="F8"&&mode!="F9"&&mode!="F10"&&mode!="F11")throw new InvalidOperationException("Fixed project scope required");
  var old=SetThreadDpiAwarenessContext(new IntPtr(-4));try{
   AutomationElement window=null;foreach(var process in System.Diagnostics.Process.GetProcessesByName("ReplicaProbe")){if(!process.MainModule.FileName.Equals(System.IO.Path.Combine(app,"ReplicaProbe.exe"),StringComparison.OrdinalIgnoreCase)||process.MainWindowHandle==IntPtr.Zero)continue;var w=AutomationElement.FromHandle(process.MainWindowHandle);if(w.Current.Name!="Replica ProgressPage verification")continue;if(window!=null)throw new InvalidOperationException("Duplicate own window");window=w;}if(window==null)throw new InvalidOperationException("Own window absent");
   SetForegroundWindow(new IntPtr(window.Current.NativeWindowHandle));System.Threading.Thread.Sleep(80);Action guard=()=>{if(GetForegroundWindow().ToInt64()!=window.Current.NativeWindowHandle)throw new InvalidOperationException("Own foreground lost");};guard();
   if(mode.StartsWith("F",StringComparison.Ordinal)){ushort vk=(ushort)(0x70+int.Parse(mode.Substring(1))-1);bool pressed=false;try{Key(vk,true);pressed=true;System.Threading.Thread.Sleep(30);guard();Key(vk,false);pressed=false;}finally{if(pressed)Key(vk,false);}return "One guarded diagnostic "+mode;}
   var frame=window.Current.BoundingRectangle;double x,y;if(mode=="hover"){var row=window.FindFirst(TreeScope.Descendants,new PropertyCondition(AutomationElement.AutomationIdProperty,"Progress-row-data"));if(row==null)throw new InvalidOperationException("Owned row peer absent");var box=row.Current.BoundingRectangle;if(box.Width<200||box.Height<40||!frame.Contains(box))throw new InvalidOperationException("Owned row geometry rejected");x=box.Left+box.Width*.27;y=box.Top+box.Height/2;}else{x=frame.Right-80;y=frame.Top+180;}int px=(int)Math.Round(x),py=(int)Math.Round(y);if(!frame.Contains(new System.Windows.Point(px,py)))throw new InvalidOperationException("Point outside owned frame");Move(px,py);System.Threading.Thread.Sleep(40);guard();POINT actual;if(!GetPhysicalCursorPos(out actual)||Math.Abs(actual.x-px)>1||Math.Abs(actual.y-py)>1)throw new InvalidOperationException("Physical point mismatch");var hit=AutomationElement.FromPoint(new System.Windows.Point(actual.x,actual.y));if(hit==null||hit.Current.ProcessId!=window.Current.ProcessId)throw new InvalidOperationException("Hit outside owned process");return "Pointer "+mode+" at "+actual.x+","+actual.y+" hit "+hit.Current.ControlType.ProgrammaticName;
  }finally{if(old!=IntPtr.Zero)SetThreadDpiAwarenessContext(old);}
 }
}
