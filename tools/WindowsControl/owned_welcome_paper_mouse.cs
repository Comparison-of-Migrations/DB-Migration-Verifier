using System;using System.Runtime.InteropServices;
public static class OwnedWelcomePaperMouse {
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
public static class OwnedWelcomePaperFixture {
 public static string Run(string app,bool source,int target) {
  var windows=System.Windows.Automation.AutomationElement.RootElement.FindAll(System.Windows.Automation.TreeScope.Children,System.Windows.Automation.Condition.TrueCondition);System.Windows.Automation.AutomationElement w=null;
  string path=System.IO.Path.Combine(app,"ReplicaProbe.exe");
  foreach(System.Windows.Automation.AutomationElement x in windows){if(source?!x.Current.Name.StartsWith("WelcomePaper · original isolation",StringComparison.Ordinal):x.Current.Name!="Replica WelcomePaper verification")continue;var p=System.Diagnostics.Process.GetProcessById(x.Current.ProcessId);if(source?!p.ProcessName.Equals("msedge",StringComparison.OrdinalIgnoreCase):!string.Equals(p.MainModule.FileName,path,StringComparison.OrdinalIgnoreCase))continue;if(source&&!p.MainModule.FileName.EndsWith(@"\Microsoft\Edge\Application\msedge.exe",StringComparison.OrdinalIgnoreCase))continue;if(w!=null)throw new InvalidOperationException("Owned paper window not unique");w=x;}if(w==null)throw new InvalidOperationException("Owned paper window missing");
  bool language=target==1;string id=language?"WelcomePaper-language":"WelcomePaper-research";var b=w.FindFirst(System.Windows.Automation.TreeScope.Descendants,new System.Windows.Automation.PropertyCondition(System.Windows.Automation.AutomationElement.AutomationIdProperty,id));if(b==null)throw new InvalidOperationException("Owned paper button missing");var rect=b.Current.BoundingRectangle;double scale=rect.Height/(language?24:44);if(scale<=0||rect.Width/scale<100||rect.Width/scale>170)throw new InvalidOperationException("Wrong button geometry");
  b.SetFocus();if(System.Windows.Automation.AutomationElement.FocusedElement.Current.ProcessId!=w.Current.ProcessId||System.Windows.Automation.AutomationElement.FocusedElement.Current.AutomationId!=id||OwnedWelcomePaperMouse.GetForegroundWindow().ToInt64()!=w.Current.NativeWindowHandle)throw new InvalidOperationException("Owned paper foreground/focus required");
  if(target==2||target==3){var body=w.FindFirst(System.Windows.Automation.TreeScope.Descendants,new System.Windows.Automation.PropertyCondition(System.Windows.Automation.AutomationElement.AutomationIdProperty,"WelcomePaper-body"));if(body==null && source)body=w.FindFirst(System.Windows.Automation.TreeScope.Descendants,new System.Windows.Automation.AndCondition(new System.Windows.Automation.PropertyCondition(System.Windows.Automation.AutomationElement.ControlTypeProperty,System.Windows.Automation.ControlType.Text),new System.Windows.Automation.PropertyCondition(System.Windows.Automation.AutomationElement.NameProperty,"The research distinction between functional success and wider contracts informs the check categories and unverified states.")));if(body==null)throw new InvalidOperationException("Owned passive body missing");rect=body.Current.BoundingRectangle;if(rect.Width/scale<100||rect.Height/scale<18||rect.Height/scale>45)throw new InvalidOperationException("Wrong passive body geometry");if(target==3)rect=new System.Windows.Rect(rect.Left+rect.Width/2,rect.Top-2*scale,scale,scale);}
  int x0=(int)Math.Round(rect.Left+rect.Width/2),y0=(int)Math.Round(rect.Top+rect.Height/2);if(!w.Current.BoundingRectangle.Contains(new System.Windows.Point(x0,y0)))throw new InvalidOperationException("Outside owned frame");var previous=new OwnedWelcomePaperMouse.POINT();OwnedWelcomePaperMouse.GetCursorPos(out previous);bool down=false;
  try{OwnedWelcomePaperMouse.Move(x0,y0);System.Threading.Thread.Sleep(180);if(OwnedWelcomePaperMouse.GetForegroundWindow().ToInt64()!=w.Current.NativeWindowHandle)throw new InvalidOperationException("Lost owned foreground");var hit=System.Windows.Automation.AutomationElement.FromPoint(new System.Windows.Point(x0,y0));if(hit==null||hit.Current.ProcessId!=w.Current.ProcessId)throw new InvalidOperationException("Pointer hit another process");OwnedWelcomePaperMouse.Button(true);down=true;System.Threading.Thread.Sleep(80);OwnedWelcomePaperMouse.Button(false);down=false;System.Threading.Thread.Sleep(300);}
  finally{if(down)OwnedWelcomePaperMouse.Button(false);if(OwnedWelcomePaperMouse.GetForegroundWindow().ToInt64()==w.Current.NativeWindowHandle)OwnedWelcomePaperMouse.Move(previous.x,previous.y);}
  return source?"Owned source pointer complete":System.IO.File.ReadAllText(System.IO.Path.Combine(app,"welcomepaper-input.json"));
 }
}
