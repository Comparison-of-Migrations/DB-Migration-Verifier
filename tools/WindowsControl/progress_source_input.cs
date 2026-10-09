using System;using System.Diagnostics;using System.Linq;using System.Runtime.InteropServices;using System.Threading;using System.Windows.Automation;
public static class ProgressSourceInput {
 [DllImport("user32.dll")]static extern IntPtr SetThreadDpiAwarenessContext(IntPtr c);
 [DllImport("user32.dll")]static extern bool SetForegroundWindow(IntPtr w);
 [DllImport("user32.dll")]static extern IntPtr GetForegroundWindow();
 [DllImport("user32.dll")]static extern IntPtr WindowFromPoint(POINT p);
 [DllImport("user32.dll")]static extern IntPtr GetAncestor(IntPtr w,uint f);
 [DllImport("user32.dll")]static extern bool SetCursorPos(int x,int y);
 [DllImport("user32.dll")]static extern uint SendInput(uint n,INPUT[] v,int size);
 [StructLayout(LayoutKind.Sequential)]struct POINT{public int X,Y;public POINT(int x,int y){X=x;Y=y;}}
 [StructLayout(LayoutKind.Sequential)]struct MOUSEINPUT{public int dx,dy;public uint data,flags,time;public IntPtr extra;}
 [StructLayout(LayoutKind.Sequential)]struct INPUT{public uint type;public MOUSEINPUT mouse;}
 public static void Run(string mode){var dpi=SetThreadDpiAwarenessContext(new IntPtr(-4));bool held=false;try{
 var wins=AutomationElement.RootElement.FindAll(TreeScope.Children,Condition.TrueCondition).Cast<AutomationElement>().Where(w=>w.Current.Name.StartsWith("Progress input · original isolation")&&Process.GetProcessById(w.Current.ProcessId).MainModule.FileName.EndsWith("Microsoft\\Edge\\Application\\msedge.exe",StringComparison.OrdinalIgnoreCase)).ToArray();if(wins.Length!=1)throw new Exception("Exact own source window required");var win=wins[0];var hwnd=new IntPtr(win.Current.NativeWindowHandle);SetForegroundWindow(hwnd);if(GetForegroundWindow()!=hwnd)throw new Exception("Own foreground required");
 AutomationElement target=null;
 if(mode=="hover"){var table=win.FindFirst(TreeScope.Descendants,new PropertyCondition(AutomationElement.ControlTypeProperty,ControlType.Table));if(table==null)throw new Exception("Source table missing");var walker=TreeWalker.ControlViewWalker;for(var row=walker.GetFirstChild(table);row!=null;row=walker.GetNextSibling(row)){var cell=walker.GetFirstChild(row);if(cell!=null&&cell.Current.Name=="데이터"){target=row;break;}}}
 else{var name=mode=="live"?"Record first live update":mode=="language"?"Language":"Progress input recorded";target=win.FindFirst(TreeScope.Descendants,new AndCondition(new PropertyCondition(AutomationElement.ControlTypeProperty,ControlType.Button),new PropertyCondition(AutomationElement.NameProperty,name)));}
 if(target==null)throw new Exception("Exact source target missing "+mode);var b=target.Current.BoundingRectangle;if(b.IsEmpty||b.Width<1||b.Height<1)throw new Exception("Invalid own source bounds");var point=new POINT((int)(b.X+b.Width/2),(int)(b.Y+b.Height/2));if(GetAncestor(WindowFromPoint(point),2)!=hwnd)throw new Exception("Physical source hit guard");SetCursorPos(point.X,point.Y);
 if(mode=="live"||mode=="language"){if(SendInput(1,new[]{new INPUT{type=0,mouse=new MOUSEINPUT{flags=2}}},Marshal.SizeOf(typeof(INPUT)))!=1)throw new Exception("Mouse down refused");held=true;if(SendInput(1,new[]{new INPUT{type=0,mouse=new MOUSEINPUT{flags=4}}},Marshal.SizeOf(typeof(INPUT)))!=1)throw new Exception("Mouse up refused");held=false;}
 Console.WriteLine("OWNED_SOURCE_INPUT "+mode);
 }finally{if(held)SendInput(1,new[]{new INPUT{type=0,mouse=new MOUSEINPUT{flags=4}}},Marshal.SizeOf(typeof(INPUT)));if(dpi!=IntPtr.Zero)SetThreadDpiAwarenessContext(dpi);}}
}
