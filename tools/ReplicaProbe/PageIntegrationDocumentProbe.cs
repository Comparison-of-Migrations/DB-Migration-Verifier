using System.Text.Json;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Input;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using Verifier.Desktop;

/// <summary>Only the newly observed Windows document parent, not locked page re-verification.</summary>
internal static class PageIntegrationDocumentProbe
{
    internal static async Task Check(ReplicaWorkspace workspace,MainWindow window,string output,string source)
    {
        using var oracle=JsonDocument.Parse(File.ReadAllText(source));var checks=new List<object>();
        void Assert(string id,bool passed,object value){checks.Add(new{id,passed,value});File.WriteAllText(Path.Combine(output,"document-checks.json"),JsonSerializer.Serialize(checks,new JsonSerializerOptions{WriteIndented=true}));if(!passed)throw new InvalidOperationException(id);}
        async Task Flush(){await Dispatcher.UIThread.InvokeAsync(()=>{},DispatcherPriority.Background);window.UpdateLayout();AvaloniaHeadlessPlatform.ForceRenderTimerTick(1);}
        bool Equal(double actual,double expected)=>Math.Abs(actual-expected)<.01;
        var vertical=workspace.DocumentVertical??throw new InvalidOperationException("Windows document required");
        var horizontal=workspace.DocumentHorizontal!;workspace.ReducedMotion=true;
        foreach(var row in oracle.RootElement.GetProperty("rows").EnumerateArray())
        {
            var width=row.GetProperty("inner").GetProperty("width").GetDouble();var height=row.GetProperty("inner").GetProperty("height").GetDouble();
            window.Width=width;window.Height=height;workspace.Navigate("home");vertical.Offset=horizontal.Offset=0;await Flush();
            var doc=row.GetProperty("document");var bar=row.GetProperty("topbar");
            Assert(width+"-document-client",Equal(vertical.ClientWidth,doc.GetProperty("clientWidth").GetDouble())&&Equal(vertical.ClientHeight,doc.GetProperty("clientHeight").GetDouble()),new{vertical.ClientWidth,vertical.ClientHeight});
            Assert(width+"-document-extent",Equal(vertical.ScrollHeight,doc.GetProperty("scrollHeight").GetDouble())&&Equal(horizontal.LogicalWidth,doc.GetProperty("scrollWidth").GetDouble()),new{vertical.ScrollHeight,horizontal.LogicalWidth});
            Assert(width+"-shell",Equal(workspace.TopBar.Bounds.Width,bar.GetProperty("width").GetDouble())&&Equal(workspace.TopBar.Bounds.Height,60),workspace.TopBar.Bounds.ToString());
            Assert(width+"-media",Equal(workspace.ShellViewportWidth,width)&&Equal(workspace.Viewport.Padding.Left,row.GetProperty("matches1180").GetBoolean()?28:40),new{workspace.ShellViewportWidth,padding=workspace.Viewport.Padding.ToString()});
            Assert(width+"-scroll-focus",!KeyboardNavigation.GetIsTabStop(vertical)&&!KeyboardNavigation.GetIsTabStop(horizontal),new{verticalTabStop=vertical.IsTabStop,horizontalTabStop=horizontal.IsTabStop});
            using var image=new RenderTargetBitmap(new((int)width*2,(int)height*2),new(192,192));image.Render(workspace);image.Save(Path.Combine(output,"document-"+width+".png"),new PngBitmapEncoderOptions());
            vertical.Offset=vertical.MaximumOffset;horizontal.Offset=horizontal.MaximumOffset;await Flush();
            var point=workspace.TopBar.TranslatePoint(default,workspace)??default;
            Assert(width+"-background-offset",Equal(point.X,-horizontal.Offset)&&Equal(point.Y,-vertical.Offset),new{x=point.X,y=point.Y,horizontal.Offset,verticalMaximum=vertical.MaximumOffset});
            vertical.Offset=horizontal.Offset=0;await Flush();
        }
    }
}
