using System;using System.Runtime.InteropServices;
public static class OwnedCapabilityRowMouse {
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
public static class OwnedCapabilityRowFixture {
 public static string Run(int index,bool cancel,string path) {
  if(index<0||index>1)throw new InvalidOperationException("Fixed capability target required");
  var windows=System.Windows.Automation.AutomationElement.RootElement.FindAll(System.Windows.Automation.TreeScope.Children,System.Windows.Automation.Condition.TrueCondition);System.Windows.Automation.AutomationElement window=null;
  string temp=System.IO.Path.GetFullPath(System.Environment.GetEnvironmentVariable("TEMP"));string actual=System.IO.Path.GetFullPath(path);if(!actual.StartsWith(temp+System.IO.Path.DirectorySeparatorChar,StringComparison.OrdinalIgnoreCase)||!System.Text.RegularExpressions.Regex.IsMatch(actual.Substring(temp.Length+1),@"^DBMV-Isolated-preflight-capabilityrow-[0-9a-f]{32}\\app\\ReplicaProbe\.exe$"))throw new InvalidOperationException("Fixed isolated row path required");
  foreach(System.Windows.Automation.AutomationElement w in windows){if(w.Current.Name!="Replica CapabilityRow verification")continue;var process=System.Diagnostics.Process.GetProcessById(w.Current.ProcessId);if(!string.Equals(process.MainModule.FileName,path,StringComparison.OrdinalIgnoreCase))continue;if(window!=null)throw new InvalidOperationException("Owned window not unique");window=w;}
  if(window==null)throw new InvalidOperationException("Owned window missing");
  var button=window.FindFirst(System.Windows.Automation.TreeScope.Descendants,new System.Windows.Automation.PropertyCondition(System.Windows.Automation.AutomationElement.AutomationIdProperty,"CapabilityRow-permissions"));if(button==null)throw new InvalidOperationException("Owned permissions missing");var b=button.Current.BoundingRectangle;double scale=b.Height/44;if(scale<=0||b.Width/scale<90||b.Width/scale>170)throw new InvalidOperationException("Wrong button geometry");
  var cell=window.FindFirst(System.Windows.Automation.TreeScope.Descendants,new System.Windows.Automation.PropertyCondition(System.Windows.Automation.AutomationElement.AutomationIdProperty,"CapabilityRow-cell-0"));if(cell==null)throw new InvalidOperationException("Owned cell missing");var c=cell.Current.BoundingRectangle;if(c.Width/scale<270||c.Width/scale>310||c.Height/scale<65||c.Height/scale>150)throw new InvalidOperationException("Wrong cell geometry");
  button.SetFocus();if(System.Windows.Automation.AutomationElement.FocusedElement.Current.ProcessId!=window.Current.ProcessId)throw new InvalidOperationException("Wrong focus");var r=index==0?b:c;
  int x=(int)Math.Round(r.Left+r.Width/2),y=(int)Math.Round(r.Top+r.Height/2);if(!r.Contains(new System.Windows.Point(x,y)))throw new InvalidOperationException("Click outside target");if(OwnedCapabilityRowMouse.GetForegroundWindow().ToInt64()!=window.Current.NativeWindowHandle)throw new InvalidOperationException("Owned window not foreground");
  var previous=new OwnedCapabilityRowMouse.POINT();OwnedCapabilityRowMouse.GetCursorPos(out previous);bool down=false;
  try {OwnedCapabilityRowMouse.Move(x,y);System.Threading.Thread.Sleep(180);if(OwnedCapabilityRowMouse.GetForegroundWindow().ToInt64()!=window.Current.NativeWindowHandle)throw new InvalidOperationException("Lost foreground");var hit=System.Windows.Automation.AutomationElement.FromPoint(new System.Windows.Point(x,y));if(hit==null||hit.Current.ProcessId!=window.Current.ProcessId)throw new InvalidOperationException("Pointer hit outside owned app");OwnedCapabilityRowMouse.Button(true);down=true;System.Threading.Thread.Sleep(100);if(cancel){var w=window.Current.BoundingRectangle;OwnedCapabilityRowMouse.Move((int)(w.Right-30*scale),(int)(w.Bottom-30*scale));System.Threading.Thread.Sleep(50);}OwnedCapabilityRowMouse.Button(false);down=false;System.Threading.Thread.Sleep(250);}
  finally{if(down)OwnedCapabilityRowMouse.Button(false);if(OwnedCapabilityRowMouse.GetForegroundWindow().ToInt64()==window.Current.NativeWindowHandle)OwnedCapabilityRowMouse.Move(previous.x,previous.y);}
  return System.IO.File.ReadAllText(System.IO.Path.Combine(System.IO.Path.GetDirectoryName(path),"capabilityrow-input.json"));
 }
}

public static class OwnedCapabilityRowSource {
 public static string Run(int index,bool cancel) {
  if(index<0||index>1)throw new InvalidOperationException("Fixed source target required");
  var ws=System.Windows.Automation.AutomationElement.RootElement.FindAll(System.Windows.Automation.TreeScope.Children,System.Windows.Automation.Condition.TrueCondition);System.Windows.Automation.AutomationElement w=null;
  foreach(System.Windows.Automation.AutomationElement x in ws){if(!x.Current.Name.StartsWith("CapabilityRow Input · original isolation",StringComparison.Ordinal))continue;var p=System.Diagnostics.Process.GetProcessById(x.Current.ProcessId);if(!p.ProcessName.Equals("msedge",StringComparison.OrdinalIgnoreCase)||!p.MainModule.FileName.EndsWith(@"\Microsoft\Edge\Application\msedge.exe",StringComparison.OrdinalIgnoreCase))continue;if(w!=null)throw new InvalidOperationException("Owned source not unique");w=x;}if(w==null)throw new InvalidOperationException("Owned source missing");
  Func<string,System.Windows.Automation.AutomationElement> find=id=>w.FindFirst(System.Windows.Automation.TreeScope.Descendants,new System.Windows.Automation.PropertyCondition(System.Windows.Automation.AutomationElement.AutomationIdProperty,id));
  var button=find("CapabilityRow-permissions");var cell=find("CapabilityRow-cell-0");var record=find("capabilityrow-input-record");if(button==null||cell==null||record==null)throw new InvalidOperationException("Exact source controls missing");
  var b=button.Current.BoundingRectangle;var c=cell.Current.BoundingRectangle;var fr=w.Current.BoundingRectangle;double scale=b.Height/44;if(scale<=0||b.Width/scale<90||b.Width/scale>170||c.Width/scale<100||c.Width/scale>450||c.Height/scale<65||c.Height/scale>160)throw new InvalidOperationException("Source geometry mismatch");
  button.SetFocus();if(System.Windows.Automation.AutomationElement.FocusedElement.Current.AutomationId!="CapabilityRow-permissions"||OwnedCapabilityRowMouse.GetForegroundWindow().ToInt64()!=w.Current.NativeWindowHandle)throw new InvalidOperationException("Exact source foreground/focus missing");
  var previous=new OwnedCapabilityRowMouse.POINT();OwnedCapabilityRowMouse.GetCursorPos(out previous);bool down=false;
  Action<System.Windows.Rect> move=r=>{int x=(int)Math.Round(r.Left+r.Width/2),y=(int)Math.Round(r.Top+r.Height/2);if(!fr.Contains(new System.Windows.Point(x,y)))throw new InvalidOperationException("Owned target outside frame");OwnedCapabilityRowMouse.Move(x,y);System.Threading.Thread.Sleep(180);if(OwnedCapabilityRowMouse.GetForegroundWindow().ToInt64()!=w.Current.NativeWindowHandle)throw new InvalidOperationException("Source lost foreground");var hit=System.Windows.Automation.AutomationElement.FromPoint(new System.Windows.Point(x,y));if(hit==null||hit.Current.ProcessId!=w.Current.ProcessId)throw new InvalidOperationException("Source hit other process");};
  try{move(index==0?b:c);OwnedCapabilityRowMouse.Button(true);down=true;System.Threading.Thread.Sleep(80);if(cancel){OwnedCapabilityRowMouse.Move((int)(fr.Right-30*scale),(int)(fr.Bottom-30*scale));System.Threading.Thread.Sleep(80);}OwnedCapabilityRowMouse.Button(false);down=false;System.Threading.Thread.Sleep(350);if(cancel){move(record.Current.BoundingRectangle);OwnedCapabilityRowMouse.Button(true);down=true;System.Threading.Thread.Sleep(50);OwnedCapabilityRowMouse.Button(false);down=false;System.Threading.Thread.Sleep(350);}}
  finally{if(down)OwnedCapabilityRowMouse.Button(false);if(OwnedCapabilityRowMouse.GetForegroundWindow().ToInt64()==w.Current.NativeWindowHandle)OwnedCapabilityRowMouse.Move(previous.x,previous.y);}
  return "Owned source pointer completed: "+index+" cancel="+cancel+" cell="+c+" button="+b;
 }
}
