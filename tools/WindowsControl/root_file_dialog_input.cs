// Official Win32 input for the project's native file picker only. UIA in this
// guest exposes its native controls as patternless Pane nodes; evidence retained.
using System;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;

public static class RootFileDialogInput
{
    [StructLayout(LayoutKind.Sequential)] public struct POINT { public int X,Y; }
    [StructLayout(LayoutKind.Sequential)] public struct RECT { public int L,T,R,B; }
    [StructLayout(LayoutKind.Sequential)] struct MOUSE { public int X,Y;public uint Data,Flags,Time;public UIntPtr Extra; }
    [StructLayout(LayoutKind.Sequential)] struct KEY { public ushort Vk,Scan;public uint Flags,Time;public UIntPtr Extra; }
    [StructLayout(LayoutKind.Explicit)] struct UNION { [FieldOffset(0)]public MOUSE Mouse;[FieldOffset(0)]public KEY Key; }
    [StructLayout(LayoutKind.Sequential)] struct INPUT { public uint Type;public UNION Value; }
    [DllImport("user32.dll")] public static extern IntPtr GetWindow(IntPtr h,uint relation);
    [DllImport("user32.dll")] public static extern IntPtr GetAncestor(IntPtr h,uint flags);
    [DllImport("user32.dll")] public static extern uint GetWindowThreadProcessId(IntPtr h,out uint pid);
    [DllImport("user32.dll")] public static extern bool IsWindowVisible(IntPtr h);
    [DllImport("user32.dll")] static extern bool IsWindowEnabled(IntPtr h);
    [DllImport("user32.dll")] static extern bool GetWindowRect(IntPtr h,out RECT rect);
    [DllImport("user32.dll")] static extern IntPtr GetForegroundWindow();
    [DllImport("user32.dll")] static extern bool SetForegroundWindow(IntPtr h);
    [DllImport("user32.dll")] static extern IntPtr WindowFromPoint(POINT p);
    [DllImport("user32.dll")] static extern bool IsChild(IntPtr parent,IntPtr child);
    [DllImport("user32.dll")] static extern IntPtr SetThreadDpiAwarenessContext(IntPtr context);
    [DllImport("user32.dll")] static extern IntPtr GetThreadDpiAwarenessContext();
    [DllImport("user32.dll")] static extern int GetAwarenessFromDpiAwarenessContext(IntPtr context);
    [DllImport("user32.dll")] static extern bool GetPhysicalCursorPos(out POINT p);
    [DllImport("user32.dll")] static extern int GetSystemMetrics(int metric);
    [DllImport("user32.dll",CharSet=CharSet.Unicode)] static extern int GetWindowText(IntPtr h,StringBuilder text,int capacity);
    [DllImport("user32.dll",SetLastError=true)] static extern uint SendInput(uint count,INPUT[] inputs,int size);
    public static string Text(IntPtr h) {var b=new StringBuilder(1024);GetWindowText(h,b,b.Capacity);return b.ToString();}
    public static void Guard(IntPtr main,IntPtr dialog,uint pid,string title)
    {
        uint actual;GetWindowThreadProcessId(dialog,out actual);
        if(actual!=pid||GetWindow(dialog,4)!=main||!IsWindowVisible(dialog)||Text(dialog)!=title)
            throw new InvalidOperationException("Exact owned native file dialog guard");
    }
    static INPUT Keyboard(ushort vk,ushort scan,uint flags)
    {return new INPUT{Type=1,Value=new UNION{Key=new KEY{Vk=vk,Scan=scan,Flags=flags}}};}
    static void Send(params INPUT[] inputs)
    {if(SendInput((uint)inputs.Length,inputs,Marshal.SizeOf(typeof(INPUT)))!=inputs.Length)throw new InvalidOperationException("Input delivery incomplete; do not replay");}
    public static void Click(IntPtr main,IntPtr dialog,IntPtr control,uint pid,string title)
    {
        // Only this short-lived verification actor's thread uses physical DPI.
        SetThreadDpiAwarenessContext(new IntPtr(-4));
        if(GetAwarenessFromDpiAwarenessContext(GetThreadDpiAwarenessContext())!=2)throw new InvalidOperationException("Physical DPI context guard");
        Guard(main,dialog,pid,title);uint actual;GetWindowThreadProcessId(control,out actual);
        RECT r;if(actual!=pid||GetAncestor(control,2)!=dialog||!IsWindowVisible(control)||!IsWindowEnabled(control)||!GetWindowRect(control,out r)||r.R<=r.L||r.B<=r.T)
            throw new InvalidOperationException("Fresh owned file control guard");
        SetForegroundWindow(dialog);if(GetForegroundWindow()!=dialog)throw new InvalidOperationException("File picker foreground guard");
        int x=(r.L+r.R)/2,y=(r.T+r.B)/2;
        int left=GetSystemMetrics(76),top=GetSystemMetrics(77),width=GetSystemMetrics(78),height=GetSystemMetrics(79);
        Send(new INPUT{Type=0,Value=new UNION{Mouse=new MOUSE{X=(int)Math.Round((x-left)*65535.0/(width-1)),Y=(int)Math.Round((y-top)*65535.0/(height-1)),Flags=0x8001|0x4000}}});
        POINT point;if(!GetPhysicalCursorPos(out point)||Math.Abs(point.X-x)>1||Math.Abs(point.Y-y)>1||GetAncestor(WindowFromPoint(point),2)!=dialog||!(WindowFromPoint(point)==control||IsChild(control,WindowFromPoint(point))))
            throw new InvalidOperationException("Physical cursor/root hit guard");
        Guard(main,dialog,pid,title);
        try{Send(new INPUT{Type=0,Value=new UNION{Mouse=new MOUSE{Flags=2}}});Thread.Sleep(60);}
        finally{Send(new INPUT{Type=0,Value=new UNION{Mouse=new MOUSE{Flags=4}}});}
    }
    public static void FileName(IntPtr main,IntPtr dialog,IntPtr edit,uint pid,string title,string path)
    {
        Click(main,dialog,edit,pid,title);
        try
        {
            Send(Keyboard(0x11,0,0),Keyboard(0x41,0,0),Keyboard(0x41,0,2),Keyboard(0x11,0,2));
            foreach(char c in path)
            {
                Guard(main,dialog,pid,title);if(GetForegroundWindow()!=dialog)throw new InvalidOperationException("File typing focus lost; no replay");
                Send(Keyboard(0,c,4),Keyboard(0,c,6));
            }
        }
        finally{Send(Keyboard(0x11,0,2));}
        // GetWindowText cannot read another process's edit contents. The caller
        // verifies the fresh UIA ValuePattern before any accept operation.
    }
}
