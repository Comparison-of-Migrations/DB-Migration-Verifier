using System;using System.Runtime.InteropServices;using System.Windows.Automation;
public static class OwnedCapabilityViewportNative {
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
 public static void Batch(string nonce,string app){Run(nonce,app,false);}
 public static void Extra(string nonce,string app){Run(nonce,app,true);}
 public static void FitPointer(string nonce,string app){Run(nonce,app,true,true);}
 public static void Focus(string nonce,string app){Run(nonce,app,true,false,true);}
 static void Run(string nonce,string app,bool extra,bool fitOnly=false,bool focusOnly=false){
  if(!System.Text.RegularExpressions.Regex.IsMatch(nonce,@"^[0-9a-f]{32}$"))throw new InvalidOperationException("Own batch identity required");AutomationElement w=null;
  foreach(AutomationElement a in AutomationElement.RootElement.FindAll(TreeScope.Children,Condition.TrueCondition)){if(a.Current.Name!="Replica CapabilityViewport verification")continue;var path=System.Diagnostics.Process.GetProcessById(a.Current.ProcessId).MainModule.FileName;if(!string.Equals(path,System.IO.Path.Combine(app,"ReplicaProbe.exe"),StringComparison.OrdinalIgnoreCase))continue;if(w!=null)throw new InvalidOperationException("Owned source not unique");w=a;}if(w==null)throw new InvalidOperationException("Owned source missing");
  if(!System.Text.RegularExpressions.Regex.IsMatch(app,@"\\DBMV-Isolated-preflight-capabilityviewport-[0-9a-f]{32}\\app$"))throw new InvalidOperationException("Owned native path required");
  var frame=w.Current.BoundingRectangle;const double scale=2;
  Func<string,AutomationElement> find=id=>w.FindFirst(TreeScope.Descendants,new PropertyCondition(AutomationElement.AutomationIdProperty,id));
  var origin=find("CapabilityViewport-reset");if(origin==null)throw new InvalidOperationException("Owned Canvas origin missing");var anchor=origin.Current.BoundingRectangle;var area=new System.Windows.Rect(anchor.Left+30*scale,anchor.Top+70*scale,478*scale,135*scale);if(!frame.Contains(area))throw new InvalidOperationException("Native viewport parent geometry mismatch");
  var middle=find("CapabilityViewport-middle");var reset=find("CapabilityViewport-reset");var record=find("CapabilityViewport-record");if(middle==null||reset==null||record==null)throw new InvalidOperationException("Exact source controls required");SetForegroundWindow(new IntPtr(w.Current.NativeWindowHandle));System.Threading.Thread.Sleep(100);POINT previous;if(!GetCursorPos(out previous))throw new InvalidOperationException("Cursor read failed");bool mouseDown=false,shiftDown=false,keyDown=false;ushort key=0;
  Action guard=()=>{if(GetForegroundWindow().ToInt64()!=w.Current.NativeWindowHandle||AutomationElement.FocusedElement.Current.ProcessId!=w.Current.ProcessId)throw new InvalidOperationException("Owned foreground/focus lost");};
  Action<int,int> move=(x,y)=>{if(!frame.Contains(new System.Windows.Point(x,y)))throw new InvalidOperationException("Owned point outside window");Move(x,y);System.Threading.Thread.Sleep(40);guard();var hit=AutomationElement.FromPoint(new System.Windows.Point(x,y));if(hit==null||hit.Current.ProcessId!=w.Current.ProcessId)throw new InvalidOperationException("Owned pointer hit failed");};
  Action<int,int> click=(x,y)=>{move(x,y);Mouse(2,0);mouseDown=true;System.Threading.Thread.Sleep(25);Mouse(4,0);mouseDown=false;System.Threading.Thread.Sleep(100);};Action<AutomationElement> button=b=>{if(!b.Current.IsEnabled)throw new InvalidOperationException("Owned utility disabled");((InvokePattern)b.GetCurrentPattern(InvokePattern.Pattern)).Invoke();System.Threading.Thread.Sleep(100);};
  int bx=(int)Math.Round(area.Left+70*scale),by=(int)Math.Round(area.Top+55*scale);
  string recordPath=System.IO.Path.Combine(app,"capabilityviewport-input.json");
  Action<string> ready=label=>{System.Threading.Thread.Sleep(650);guard();if(!System.IO.File.Exists(recordPath)||System.IO.File.GetLastWriteTimeUtc(recordPath)<DateTime.UtcNow.AddSeconds(-1))throw new InvalidOperationException("Fresh native timer record missing");System.IO.File.Copy(recordPath,System.IO.Path.Combine(app,"native-"+label+".json"),false);};
  try{
   if(focusOnly){AutomationElement other=null;foreach(AutomationElement a in AutomationElement.RootElement.FindAll(TreeScope.Children,Condition.TrueCondition)){if(!a.Current.Name.StartsWith("CapabilityViewport Input · original isolation",StringComparison.Ordinal))continue;var op=System.Diagnostics.Process.GetProcessById(a.Current.ProcessId).MainModule.FileName;if(!op.EndsWith(@"Microsoft\Edge\Application\msedge.exe",StringComparison.OrdinalIgnoreCase))continue;if(other!=null)throw new InvalidOperationException("Opposite owned restore window not unique");other=a;}if(other==null)throw new InvalidOperationException("Opposite owned restore window missing");
    Action restore=()=>{SetForegroundWindow(new IntPtr(other.Current.NativeWindowHandle));System.Threading.Thread.Sleep(150);if(GetForegroundWindow().ToInt64()!=other.Current.NativeWindowHandle)throw new InvalidOperationException("Exact opposite activation failed");SetForegroundWindow(new IntPtr(w.Current.NativeWindowHandle));System.Threading.Thread.Sleep(150);guard();};
    Action tab=()=>{KeyEvent(9,true);key=9;keyDown=true;System.Threading.Thread.Sleep(25);KeyEvent(9,false);keyDown=false;};
    Action<string> focusReady=ready;
    Action<string> peerLabel=label=>{var c=find("CapabilityViewport-child");if(c==null||c.Current.Name!="Required permissions"||c.Current.ControlType!=ControlType.Button||!c.Current.IsControlElement||!c.Current.IsContentElement)throw new InvalidOperationException("Visible named child semantic peer absent");Console.WriteLine("PEER:{\"label\":\""+label+"\",\"name\":\"Required permissions\",\"control\":true,\"content\":true,\"offscreen\":"+c.Current.IsOffscreen.ToString().ToLowerInvariant()+"}");};
    button(reset);click(bx,by);restore();focusReady("pointer-restore");
    find("CapabilityViewport-before").SetFocus();tab();focusReady("tab-viewport");restore();focusReady("keyboard-restore");
    button(find("CapabilityViewport-child-toggle"));System.Threading.Thread.Sleep(200);peerLabel("child-clipped");find("CapabilityViewport-before").SetFocus();tab();focusReady("tab-child");peerLabel("child-visible");return;
   }
   if(extra){
    if(fitOnly){button(reset);click(bx,by);button(find("CapabilityViewport-fit"));ready("fit-no-overflow-pointer");button(find("CapabilityViewport-fit"));ready("fit-pointer");return;}
    string[] ids={"reset","fit","fit","child-toggle","child-focus","mount","mount"};string[] stages={"reset-before-fit","fit-no-overflow","fit","child-toggle","child-focus","mount-detached","mount"};
    for(int i=0;i<ids.Length;i++){if(ids[i]=="child-focus"){var child=find("CapabilityViewport-child");if(child==null)throw new InvalidOperationException("Exact native focused child missing");child.SetFocus();}else button(find("CapabilityViewport-"+ids[i]));ready(stages[i]);}
    button(reset);click(bx,by);click((int)Math.Round(area.Left+400*scale),(int)Math.Round(area.Top+127.5*scale));ready("track");
    button(middle);click(bx,by);move((int)Math.Round(area.Left+9*scale),(int)Math.Round(area.Top+127.5*scale));Mouse(2,0);mouseDown=true;System.Threading.Thread.Sleep(450);Mouse(4,0);mouseDown=false;ready("hold-left");
    button(reset);click(bx,by);Mouse(0x1000u,120);System.Threading.Thread.Sleep(150);move((int)Math.Round(area.Left+100*scale),(int)Math.Round(area.Top+127.5*scale));Mouse(2,0);mouseDown=true;System.Threading.Thread.Sleep(60);move((int)Math.Round(area.Left+450*scale),(int)Math.Round(area.Top+127.5*scale));System.Threading.Thread.Sleep(60);Mouse(4,0);mouseDown=false;ready("drag");
    return;
   }
   string[] labels={"right","left","down","up","page-down","page-up","home","end","space","shift-space"};ushort[] keys={39,37,40,38,34,33,36,35,32,32};
   for(int i=0;i<labels.Length;i++){button(middle);click(bx,by);guard();if(i==9){KeyEvent(16,true);shiftDown=true;}key=keys[i];KeyEvent(key,true);keyDown=true;System.Threading.Thread.Sleep(25);KeyEvent(key,false);keyDown=false;if(shiftDown){KeyEvent(16,false);shiftDown=false;}ready(labels[i]);}
   foreach(string label in new[]{"wheel-x","wheel-y","wheel-shift-y"}){button(reset);click(bx,by);guard();if(label=="wheel-shift-y"){KeyEvent(16,true);shiftDown=true;}Mouse(label=="wheel-x"?0x1000u:0x800u,label=="wheel-x"?120u:unchecked((uint)-120));if(shiftDown){KeyEvent(16,false);shiftDown=false;}ready(label);}
  }finally{if(keyDown)KeyEvent(key,false);if(shiftDown)KeyEvent(16,false);if(mouseDown)Mouse(4,0);if(GetForegroundWindow().ToInt64()==w.Current.NativeWindowHandle)Move(previous.x,previous.y);}
 }
}
