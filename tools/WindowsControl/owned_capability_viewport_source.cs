using System;using System.Runtime.InteropServices;using System.Windows.Automation;
public static class OwnedCapabilityViewportSource {
 [StructLayout(LayoutKind.Sequential)]public struct POINT{public int x,y;}
 [StructLayout(LayoutKind.Sequential)]public struct MOUSE{public int x,y;public uint data,flags,time;public UIntPtr extra;}
 [StructLayout(LayoutKind.Sequential)]public struct KEY{public ushort vk,scan;public uint flags,time;public UIntPtr extra;}
 [StructLayout(LayoutKind.Explicit,Size=32)]public struct UNION{[FieldOffset(0)]public MOUSE mouse;[FieldOffset(0)]public KEY key;}
 [StructLayout(LayoutKind.Sequential)]public struct INPUT{public uint type;public UNION data;}
 [DllImport("user32.dll",SetLastError=true)]static extern uint SendInput(uint n,INPUT[] events,int size);
 [DllImport("user32.dll")]static extern int GetSystemMetrics(int n);
 [DllImport("user32.dll")]static extern bool GetCursorPos(out POINT p);
 [DllImport("user32.dll")]static extern IntPtr GetForegroundWindow();
 [DllImport("user32.dll")]static extern bool SetForegroundWindow(IntPtr w);
 static void Emit(INPUT i){if(SendInput(1,new[]{i},Marshal.SizeOf<INPUT>())!=1)throw new InvalidOperationException("SendInput failed");}
 static void KeyEvent(ushort vk,bool down){Emit(new INPUT{type=1,data=new UNION{key=new KEY{vk=vk,flags=down?0u:2u}}});}
 static void Mouse(uint flags,uint data){Emit(new INPUT{type=0,data=new UNION{mouse=new MOUSE{flags=flags,data=data}}});}
 static void Move(int x,int y){int l=GetSystemMetrics(76),t=GetSystemMetrics(77),w=GetSystemMetrics(78),h=GetSystemMetrics(79);if(w<=1||h<=1||x<l||x>=l+w||y<t||y>=t+h)throw new InvalidOperationException("Display bounds failed");Emit(new INPUT{type=0,data=new UNION{mouse=new MOUSE{x=(int)Math.Round((x-l)*65535d/(w-1)),y=(int)Math.Round((y-t)*65535d/(h-1)),flags=0xC001}}});}
 public static void Batch(string nonce){Run(nonce,false);}
 public static void Extra(string nonce){Run(nonce,true);}
 public static void Focus(string nonce,string app){Run(nonce,true,true,app);}
 static void Run(string nonce,bool extra,bool focusOnly=false,string app=null){
  if(!System.Text.RegularExpressions.Regex.IsMatch(nonce,@"^[0-9a-f]{32}$"))throw new InvalidOperationException("Own batch identity required");AutomationElement w=null;
  foreach(AutomationElement a in AutomationElement.RootElement.FindAll(TreeScope.Children,Condition.TrueCondition)){if(!a.Current.Name.StartsWith("CapabilityViewport Input · original isolation",StringComparison.Ordinal))continue;var path=System.Diagnostics.Process.GetProcessById(a.Current.ProcessId).MainModule.FileName;if(!path.EndsWith(@"Microsoft\Edge\Application\msedge.exe",StringComparison.OrdinalIgnoreCase))continue;if(w!=null)throw new InvalidOperationException("Owned source not unique");w=a;}if(w==null)throw new InvalidOperationException("Owned source missing");
  var docs=w.FindAll(TreeScope.Descendants,new PropertyCondition(AutomationElement.ControlTypeProperty,ControlType.Document));if(docs.Count!=1)throw new InvalidOperationException("One source document required");var doc=docs[0];var frame=w.Current.BoundingRectangle;var area=doc.Current.BoundingRectangle;const double scale=2;if(area.Width<960*scale||area.Height<720*scale)throw new InvalidOperationException("Source minimum viewport unavailable");
  Func<string,AutomationElement> find=id=>doc.FindFirst(TreeScope.Descendants,new PropertyCondition(AutomationElement.AutomationIdProperty,id));
  var middle=find("CapabilityViewport-middle");var reset=find("CapabilityViewport-reset");var record=find("CapabilityViewport-record");if(middle==null||reset==null||record==null)throw new InvalidOperationException("Exact source controls required");SetForegroundWindow(new IntPtr(w.Current.NativeWindowHandle));System.Threading.Thread.Sleep(100);POINT previous;if(!GetCursorPos(out previous))throw new InvalidOperationException("Cursor read failed");bool mouseDown=false,shiftDown=false,keyDown=false;ushort key=0;
  Action guard=()=>{if(GetForegroundWindow().ToInt64()!=w.Current.NativeWindowHandle||AutomationElement.FocusedElement.Current.ProcessId!=w.Current.ProcessId)throw new InvalidOperationException("Owned foreground/focus lost");};
  Action<int,int> move=(x,y)=>{if(!frame.Contains(new System.Windows.Point(x,y)))throw new InvalidOperationException("Owned point outside window");Move(x,y);System.Threading.Thread.Sleep(40);guard();var hit=AutomationElement.FromPoint(new System.Windows.Point(x,y));if(hit==null||hit.Current.ProcessId!=w.Current.ProcessId)throw new InvalidOperationException("Owned pointer hit failed");};
  Action<int,int> click=(x,y)=>{move(x,y);Mouse(2,0);mouseDown=true;System.Threading.Thread.Sleep(25);Mouse(4,0);mouseDown=false;System.Threading.Thread.Sleep(100);};Action<AutomationElement> button=b=>{var r=b.Current.BoundingRectangle;if(r.Width<=0||r.Height<=0||!frame.Contains(r))throw new InvalidOperationException("Owned button geometry");click((int)Math.Round(r.Left+r.Width/2),(int)Math.Round(r.Top+r.Height/2));};
  int bx=(int)Math.Round(area.Left+100*scale),by=(int)Math.Round(area.Top+125*scale);
  Action<string> ready=label=>{System.Threading.Thread.Sleep(800);button(record);System.Threading.Thread.Sleep(1000);guard();using(var client=new System.Net.WebClient()){client.Headers["Content-Type"]="application/json";client.UploadString("http://10.211.55.2:8768/evidence/preflight-capabilityviewport-windows-control-ready.json","POST","{\"nonce\":\""+nonce+"\",\"label\":\""+label+"\"}");}System.Threading.Thread.Sleep(250);};
  try{
   if(focusOnly){AutomationElement other=null;foreach(AutomationElement a in AutomationElement.RootElement.FindAll(TreeScope.Children,Condition.TrueCondition)){if(a.Current.Name!="Replica CapabilityViewport verification")continue;var op=System.Diagnostics.Process.GetProcessById(a.Current.ProcessId).MainModule.FileName;if(!string.Equals(op,System.IO.Path.Combine(app,"ReplicaProbe.exe"),StringComparison.OrdinalIgnoreCase))continue;if(other!=null)throw new InvalidOperationException("Opposite owned restore window not unique");other=a;}if(other==null)throw new InvalidOperationException("Opposite owned restore window missing");
    Action restore=()=>{SetForegroundWindow(new IntPtr(other.Current.NativeWindowHandle));System.Threading.Thread.Sleep(150);if(GetForegroundWindow().ToInt64()!=other.Current.NativeWindowHandle)throw new InvalidOperationException("Exact opposite activation failed");SetForegroundWindow(new IntPtr(w.Current.NativeWindowHandle));System.Threading.Thread.Sleep(150);guard();};
    Action tab=()=>{KeyEvent(9,true);key=9;keyDown=true;System.Threading.Thread.Sleep(25);KeyEvent(9,false);keyDown=false;};
    Action<string> focusReady=label=>{System.Threading.Thread.Sleep(650);guard();using(var client=new System.Net.WebClient()){client.Headers["Content-Type"]="application/json";client.UploadString("http://10.211.55.2:8768/evidence/preflight-capabilityviewport-windows-control-ready.json","POST","{\"nonce\":\""+nonce+"\",\"label\":\""+label+"\"}");}System.Threading.Thread.Sleep(250);};
    Action<string> peerLabel=label=>{var c=find("CapabilityViewport-child");if(c==null||c.Current.Name!="Required permissions"||c.Current.ControlType!=ControlType.Button||!c.Current.IsControlElement||!c.Current.IsContentElement)throw new InvalidOperationException("Visible named child semantic peer absent");Console.WriteLine("PEER:{\"label\":\""+label+"\",\"name\":\"Required permissions\",\"control\":true,\"content\":true,\"offscreen\":"+c.Current.IsOffscreen.ToString().ToLowerInvariant()+"}");};
    button(reset);click(bx,by);restore();focusReady("pointer-restore");
    button(find("CapabilityViewport-before"));tab();focusReady("tab-viewport");restore();focusReady("keyboard-restore");
    button(find("CapabilityViewport-child-toggle"));System.Threading.Thread.Sleep(200);peerLabel("child-clipped");button(find("CapabilityViewport-before"));tab();focusReady("tab-child");peerLabel("child-visible");return;
   }
   if(extra){
    string[] ids={"reset","fit","fit","child-toggle","child-focus","mount","mount"};string[] stages={"reset-before-fit","fit-no-overflow","fit","child-toggle","child-focus","mount-detached","mount"};
    for(int i=0;i<ids.Length;i++){button(find("CapabilityViewport-"+ids[i]));ready(stages[i]);}
    button(reset);click(bx,by);click((int)Math.Round(area.Left+430*scale),(int)Math.Round(area.Top+197.5*scale));ready("track");
    button(middle);click(bx,by);move((int)Math.Round(area.Left+39*scale),(int)Math.Round(area.Top+197.5*scale));Mouse(2,0);mouseDown=true;System.Threading.Thread.Sleep(450);Mouse(4,0);mouseDown=false;ready("hold-left");
    button(reset);click(bx,by);Mouse(0x1000u,120);System.Threading.Thread.Sleep(150);move((int)Math.Round(area.Left+130*scale),(int)Math.Round(area.Top+197.5*scale));Mouse(2,0);mouseDown=true;System.Threading.Thread.Sleep(60);move((int)Math.Round(area.Left+480*scale),(int)Math.Round(area.Top+197.5*scale));System.Threading.Thread.Sleep(60);Mouse(4,0);mouseDown=false;ready("drag");
    return;
   }
   string[] labels={"right","left","down","up","page-down","page-up","home","end","space","shift-space"};ushort[] keys={39,37,40,38,34,33,36,35,32,32};
   for(int i=0;i<labels.Length;i++){button(middle);click(bx,by);guard();if(i==9){KeyEvent(16,true);shiftDown=true;}key=keys[i];KeyEvent(key,true);keyDown=true;System.Threading.Thread.Sleep(25);KeyEvent(key,false);keyDown=false;if(shiftDown){KeyEvent(16,false);shiftDown=false;}ready(labels[i]);}
   foreach(string label in new[]{"wheel-x","wheel-y","wheel-shift-y"}){button(reset);click(bx,by);guard();if(label=="wheel-shift-y"){KeyEvent(16,true);shiftDown=true;}Mouse(label=="wheel-x"?0x1000u:0x800u,label=="wheel-x"?120u:unchecked((uint)-120));if(shiftDown){KeyEvent(16,false);shiftDown=false;}ready(label);}
  }finally{if(keyDown)KeyEvent(key,false);if(shiftDown)KeyEvent(16,false);if(mouseDown)Mouse(4,0);if(GetForegroundWindow().ToInt64()==w.Current.NativeWindowHandle)Move(previous.x,previous.y);}
 }
}
