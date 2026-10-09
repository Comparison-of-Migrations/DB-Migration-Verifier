using System;using System.Runtime.InteropServices;
public static class OwnedNumberMouse {
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
}public static class OwnedNumberFixture {
 static System.Windows.Automation.AutomationElement Find(string target) {
  var root=System.Windows.Automation.AutomationElement.RootElement;
  var windows=root.FindAll(System.Windows.Automation.TreeScope.Children,System.Windows.Automation.Condition.TrueCondition);
  System.Windows.Automation.AutomationElement result=null;
  foreach(System.Windows.Automation.AutomationElement window in windows) {
   var process=System.Diagnostics.Process.GetProcessById(window.Current.ProcessId);
   bool match=target=="source"?window.Current.Name.StartsWith("NumberInput · original isolation")&&process.ProcessName=="msedge":
    window.Current.Name=="Replica NumberInput verification"&&string.Equals(process.MainModule.FileName,System.IO.Path.Combine(System.IO.Path.Combine(System.IO.Path.Combine(System.Environment.GetEnvironmentVariable("TEMP"),"DBMV-Replica-Playback-20261006"),"app"),"ReplicaProbe.exe"),System.StringComparison.OrdinalIgnoreCase);
   if(match){if(result!=null)throw new System.InvalidOperationException("Owned window must be unique");result=window;}
  }
  if(result==null)throw new System.InvalidOperationException("Owned number fixture missing");return result;
 }
 static System.Windows.Automation.AutomationElement Element(System.Windows.Automation.AutomationElement window,string id) {
  var field=window.FindFirst(System.Windows.Automation.TreeScope.Descendants,new System.Windows.Automation.PropertyCondition(System.Windows.Automation.AutomationElement.AutomationIdProperty,id));
  if(field==null)throw new System.InvalidOperationException("Owned number element missing");return field;
 }
 static void Foreground(System.Windows.Automation.AutomationElement window) {
  if(OwnedNumberMouse.GetForegroundWindow().ToInt64()!=window.Current.NativeWindowHandle)throw new System.InvalidOperationException("Owned fixture lost foreground; input blocked");
 }
 static void Post(string nonce,string phase,System.Windows.Rect rect,double scale) {
  string body=string.Format(System.Globalization.CultureInfo.InvariantCulture,"{{\"nonce\":\"{0}\",\"phase\":\"{1}\",\"rect\":{{\"X\":{2},\"Y\":{3},\"Width\":{4},\"Height\":{5}}},\"scale\":{6}}}",nonce,phase,rect.X,rect.Y,rect.Width,rect.Height,scale);
  using(var client=new System.Net.WebClient()){client.Headers["Content-Type"]="application/json";client.UploadString("http://10.211.55.2:8768/evidence/control-ready.json",body);}
 }
 public static void Run(string target,string mode,string half,string nonce) {
  if((target!="source"&&target!="native")||(mode!="hold"&&mode!="click"&&mode!="hover")||(half!="up"&&half!="down")||!System.Text.RegularExpressions.Regex.IsMatch(nonce,"^[0-9a-f-]{36}$"))throw new System.InvalidOperationException("Unsupported fixture input");
  var window=Find(target);var field=Element(window,"number-default");
  if(target=="source"){Element(window,"record");Element(window,"RootWebArea");}
  var rect=field.Current.BoundingRectangle;double scale=rect.Width/112;
  if(scale<=0||System.Math.Abs(rect.Height/44-scale)>.01)throw new System.InvalidOperationException("Unexpected owned geometry");
  field.SetFocus();if(System.Windows.Automation.AutomationElement.FocusedElement.Current.ProcessId!=window.Current.ProcessId)throw new System.InvalidOperationException("Wrong owned focus");
  ((System.Windows.Automation.ValuePattern)field.GetCurrentPattern(System.Windows.Automation.ValuePattern.Pattern)).SetValue("3306");
  Foreground(window);var previous=new OwnedNumberMouse.POINT();OwnedNumberMouse.GetCursorPos(out previous);bool pressed=false;
  try {
   Foreground(window);OwnedNumberMouse.Move((int)System.Math.Round(rect.Right-20.5*scale),(int)System.Math.Round(rect.Top+(half=="up"?16.5:27.5)*scale));System.Threading.Thread.Sleep(350);
   if(mode=="hover"){Post(nonce,"hover",rect,scale);System.Threading.Thread.Sleep(2000);}
   else {Foreground(window);OwnedNumberMouse.Button(true);pressed=true;Post(nonce,"pressed",rect,scale);System.Threading.Thread.Sleep(mode=="hold"?1500:35);OwnedNumberMouse.Button(false);pressed=false;System.Threading.Thread.Sleep(350);}
   if(target=="source"){((System.Windows.Automation.InvokePattern)Element(window,"record").GetCurrentPattern(System.Windows.Automation.InvokePattern.Pattern)).Invoke();System.Threading.Thread.Sleep(100);}
  } finally {if(pressed)OwnedNumberMouse.Button(false);if(OwnedNumberMouse.GetForegroundWindow().ToInt64()==window.Current.NativeWindowHandle)OwnedNumberMouse.Move(previous.x,previous.y);}
 }
}
