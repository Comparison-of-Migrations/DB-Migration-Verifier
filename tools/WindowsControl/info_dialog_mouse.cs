using System;using System.Runtime.InteropServices;using System.Windows.Automation;
public static class OwnedInfoDialogMouse {
 [StructLayout(LayoutKind.Sequential)] public struct POINT{public int x,y;}
 [StructLayout(LayoutKind.Sequential)] public struct MOUSEINPUT{public int dx,dy;public uint mouseData,dwFlags,time;public UIntPtr extra;}
 [StructLayout(LayoutKind.Sequential)] public struct INPUT{public uint type;public MOUSEINPUT mi;}
 [DllImport("user32.dll",SetLastError=true)] static extern uint SendInput(uint n,INPUT[] e,int size);
 [DllImport("user32.dll")] static extern int GetSystemMetrics(int n);
 [DllImport("user32.dll")] static extern bool GetCursorPos(out POINT p);
 [DllImport("user32.dll")] static extern IntPtr GetForegroundWindow();
 [DllImport("user32.dll")] static extern bool SetForegroundWindow(IntPtr window);
 [StructLayout(LayoutKind.Sequential)] public struct KEYBDINPUT{public ushort vk,scan;public uint flags,time;public UIntPtr extra;}
 [StructLayout(LayoutKind.Explicit,Size=32)] public struct UNION{[FieldOffset(0)]public MOUSEINPUT mouse;[FieldOffset(0)]public KEYBDINPUT key;}
 [StructLayout(LayoutKind.Sequential)] public struct KEYINPUT{public uint type;public UNION data;}
 [DllImport("user32.dll",EntryPoint="SendInput",SetLastError=true)] static extern uint SendKey(uint n,KEYINPUT[] e,int size);
 static bool InDocument(AutomationElement e,AutomationElement w){for(var x=e;x!=null&&!Automation.Compare(x,w);x=TreeWalker.ControlViewWalker.GetParent(x))if(x.Current.ControlType==ControlType.Document)return true;return false;}
 static void Emit(MOUSEINPUT m){if(SendInput(1,new[]{new INPUT{type=0,mi=m}},Marshal.SizeOf<INPUT>())!=1)throw new InvalidOperationException("SendInput failed");}
 static void Move(int x,int y){int l=GetSystemMetrics(76),t=GetSystemMetrics(77),w=GetSystemMetrics(78),h=GetSystemMetrics(79);if(w<=1||h<=1||x<l||y<t||x>=l+w||y>=t+h)throw new InvalidOperationException("Display bounds failed");Emit(new MOUSEINPUT{dx=(int)Math.Round((x-l)*65535.0/(w-1)),dy=(int)Math.Round((y-t)*65535.0/(h-1)),dwFlags=0x8000|0x4000|1});}
 public static void Run(string path,bool source,string target){
  var temp=System.IO.Path.GetFullPath(Environment.GetEnvironmentVariable("TEMP"));var actual=System.IO.Path.GetFullPath(path);
  if(!actual.StartsWith(temp+System.IO.Path.DirectorySeparatorChar,StringComparison.OrdinalIgnoreCase)||!System.Text.RegularExpressions.Regex.IsMatch(actual.Substring(temp.Length+1),@"^DBMV-Isolated-root-infodialog-[0-9a-f]{32}\\app\\ReplicaProbe\.exe$"))throw new InvalidOperationException("Fixed isolated path required");
  if(target!="open"&&target!="close"&&target!="footer-close")throw new InvalidOperationException("Fixed target required");
  AutomationElement window=null;
  foreach(AutomationElement w in AutomationElement.RootElement.FindAll(TreeScope.Children,Condition.TrueCondition)){
   if(source?!w.Current.Name.StartsWith("InfoDialog input · original isolation"):w.Current.Name!="Replica InfoDialog verification")continue;
   var process=System.Diagnostics.Process.GetProcessById(w.Current.ProcessId).MainModule.FileName;
   bool matched=source?w.Current.Name.StartsWith("InfoDialog input · original isolation")&&process.EndsWith(@"Microsoft\Edge\Application\msedge.exe",StringComparison.OrdinalIgnoreCase):w.Current.Name=="Replica InfoDialog verification"&&string.Equals(process,path,StringComparison.OrdinalIgnoreCase);
   if(!matched)continue;if(window!=null)throw new InvalidOperationException("Not unique");window=w;
  }
  if(window==null)throw new InvalidOperationException("Owned window absent");AutomationElement c=null;
  if(source){var buttons=window.FindAll(TreeScope.Descendants,new PropertyCondition(AutomationElement.ControlTypeProperty,ControlType.Button));int count=0;foreach(AutomationElement b in buttons){if(!InDocument(b,window)||(target=="open"?b.Current.Name!="Open details":b.Current.Name!="닫기"&&b.Current.Name!="Close"))continue;if(count==(target=="footer-close"?1:0))c=b;count++;}if(count!=(target=="open"?1:2))throw new InvalidOperationException("Source count mismatch");}
  else c=window.FindFirst(TreeScope.Descendants,new PropertyCondition(AutomationElement.AutomationIdProperty,"InfoDialog-"+target));
  if(c==null||!c.Current.IsEnabled)throw new InvalidOperationException("Owned enabled target missing");var rect=c.Current.BoundingRectangle;if(rect.Width<20||rect.Height<20||rect.Width>700||rect.Height>160)throw new InvalidOperationException("Target geometry rejected");
  SetForegroundWindow(new IntPtr(window.Current.NativeWindowHandle));System.Threading.Thread.Sleep(80);var focus=AutomationElement.FocusedElement;if(focus==null||focus.Current.ProcessId!=window.Current.ProcessId)throw new InvalidOperationException("Focus guard");int x=(int)Math.Round(rect.Left+rect.Width/2),y=(int)Math.Round(rect.Top+rect.Height/2);POINT previous;if(!GetCursorPos(out previous))throw new InvalidOperationException("Cursor guard");bool down=false;
  try{Move(x,y);System.Threading.Thread.Sleep(100);if(GetForegroundWindow().ToInt64()!=window.Current.NativeWindowHandle)throw new InvalidOperationException("Foreground guard");var hit=AutomationElement.FromPoint(new System.Windows.Point(x,y));if(hit==null||hit.Current.ProcessId!=window.Current.ProcessId)throw new InvalidOperationException("Hit guard");Emit(new MOUSEINPUT{dwFlags=2});down=true;System.Threading.Thread.Sleep(25);Emit(new MOUSEINPUT{dwFlags=4});down=false;System.Threading.Thread.Sleep(650);}
  finally{if(down)Emit(new MOUSEINPUT{dwFlags=4});if(GetForegroundWindow().ToInt64()==window.Current.NativeWindowHandle)Move(previous.x,previous.y);}
 }

 public static void Key(string path,bool source,string key,string target){
  var temp=System.IO.Path.GetFullPath(Environment.GetEnvironmentVariable("TEMP"));var actual=System.IO.Path.GetFullPath(path);
  if(!actual.StartsWith(temp+System.IO.Path.DirectorySeparatorChar,StringComparison.OrdinalIgnoreCase)||!System.Text.RegularExpressions.Regex.IsMatch(actual.Substring(temp.Length+1),@"^DBMV-Isolated-root-infodialog-[0-9a-f]{32}\\app\\ReplicaProbe\.exe$"))throw new InvalidOperationException("Fixed isolated path required");

  AutomationElement window=null;
  foreach(AutomationElement w in AutomationElement.RootElement.FindAll(TreeScope.Children,Condition.TrueCondition)){
   if(source?!w.Current.Name.StartsWith("InfoDialog input · original isolation"):w.Current.Name!="Replica InfoDialog verification")continue;
   var process=System.Diagnostics.Process.GetProcessById(w.Current.ProcessId).MainModule.FileName;
   bool matched=source?w.Current.Name.StartsWith("InfoDialog input · original isolation")&&process.EndsWith(@"Microsoft\Edge\Application\msedge.exe",StringComparison.OrdinalIgnoreCase):w.Current.Name=="Replica InfoDialog verification"&&string.Equals(process,path,StringComparison.OrdinalIgnoreCase);
   if(!matched)continue;if(window!=null)throw new InvalidOperationException("Not unique");window=w;
  }

  if(window==null)throw new InvalidOperationException("Owned window absent");
  if(target!="current"){if(source)throw new InvalidOperationException("Source keys use current actual focus");var c=window.FindFirst(TreeScope.Descendants,new PropertyCondition(AutomationElement.AutomationIdProperty,"InfoDialog-"+target));if(c==null||!c.Current.IsEnabled)throw new InvalidOperationException("Owned key target absent");c.SetFocus();}
  var focus=AutomationElement.FocusedElement;if(focus==null||focus.Current.ProcessId!=window.Current.ProcessId||GetForegroundWindow().ToInt64()!=window.Current.NativeWindowHandle)throw new InvalidOperationException("Owned key foreground guard");
  ushort vk=key=="tab"?(ushort)9:key=="escape"?(ushort)27:key=="end"?(ushort)35:key=="enter"?(ushort)13:key=="space"?(ushort)32:(ushort)0;if(vk==0)throw new InvalidOperationException("Fixed key required");
  bool down=false;try{if(SendKey(1,new[]{new KEYINPUT{type=1,data=new UNION{key=new KEYBDINPUT{vk=vk}}}},Marshal.SizeOf<KEYINPUT>())!=1)throw new InvalidOperationException("Key press failed");down=true;System.Threading.Thread.Sleep(25);}
  finally{if(down&&SendKey(1,new[]{new KEYINPUT{type=1,data=new UNION{key=new KEYBDINPUT{vk=vk,flags=2}}}},Marshal.SizeOf<KEYINPUT>())!=1)throw new InvalidOperationException("Key release failed");}
  System.Threading.Thread.Sleep(650);
 }
}
