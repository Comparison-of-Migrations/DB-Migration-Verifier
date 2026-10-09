using System.Text.Json;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Input;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Verifier.Desktop;

/// <summary>Only protected Advanced viewport/gap API; no Connection or Workspace resize caller.</summary>
internal static class PageIntegrationConnectionAdvancedResizeProbe
{
    private sealed class Oracle{public Row[] Rows{get;set;}=[];}
    private sealed class Row{public string Id{get;set;}="";public string Language{get;set;}="";public double Width{get;set;}public Allocation ParentAllocation{get;set;}=new();}
    private sealed class Allocation{public double PageWidth{get;set;}}
    private static Row[] ReadCases()=>JsonSerializer.Deserialize<Oracle>(File.ReadAllText(Environment.GetEnvironmentVariable("DBMV_ADVANCED_RESIZE_SOURCE")??throw new InvalidOperationException("Frozen original child source path required")),new JsonSerializerOptions{PropertyNameCaseInsensitive=true})!.Rows;
    private sealed class Host(WebAdvancedConnection child):Panel
    {
        public Row[] Cases{get;set;}=[];public string Language{get;set;}="ko";
        public Host(WebAdvancedConnection child,Row[] cases):this(child){Cases=cases;UseLayoutRounding=false;Children.Add(child);}
        protected override Size MeasureOverride(Size available)
        {
            var width=available.Width;
            var row=Cases.SingleOrDefault(c=>c.Language==Language&&Math.Abs(c.Width-width)<.01);if(row is not null){child.SetViewport(width);child.Measure(new(row.ParentAllocation.PageWidth,double.PositiveInfinity));}
            return new(available.Width,available.Height);
        }
        protected override Size ArrangeOverride(Size final){if(child.IsMeasureValid)child.Arrange(new(40,40,child.DesiredSize.Width,child.DesiredSize.Height));return final;}
    }
    private static void Capture(Window window,Control host,string output,string name)
    {
        var scale=window.RenderScaling;using var bitmap=new RenderTargetBitmap(new PixelSize((int)Math.Ceiling(window.ClientSize.Width*scale),(int)Math.Ceiling(window.ClientSize.Height*scale)),new Vector(96*scale,96*scale));bitmap.Render(host);bitmap.Save(Path.Combine(output,name),new PngBitmapEncoderOptions());
    }
    private static object Geometry(WebAdvancedConnection child)
    {
        object Box(Control control){var p=control.TranslatePoint(default,child)??throw new InvalidOperationException("Attached Advanced child geometry required");return new{x=p.X,y=p.Y,width=control.Bounds.Width,height=control.Bounds.Height};}
        var split=(Panel)child.Explanation!.Child!;return new{root=Box(child),toggle=Box(child.ToggleButton),panel=Box(child.Explanation),split=Box(split),columns=split.Children.Select(Box).ToArray(),titles=child.Titles.Select(Box).ToArray(),descriptions=child.Descriptions.Select(Box).ToArray()};
    }
    internal static async Task Check(ReplicaWorkspace unused,Window window,string output)
    {
        var cases=ReadCases();var child=new WebAdvancedConnection{ReducedMotion=false};var host=new Host(child,cases);window.Content=host;window.Height=900;window.Activate();var rows=new List<object>();
        async Task Flush(){await Dispatcher.UIThread.InvokeAsync(()=>{},DispatcherPriority.Background);window.UpdateLayout();AvaloniaHeadlessPlatform.ForceRenderTimerTick(1);}
        foreach(var lang in (Environment.GetEnvironmentVariable("DBMV_ADVANCED_RESIZE_DIAGNOSTIC_ONLY")=="1"?new[]{"en"}:new[]{"ko","en"}))
        {
            window.Width=1280;host.Language=lang;child.SetPresentation(lang=="en",true,1280);host.InvalidateMeasure();await Flush();await Task.Delay(400);await Flush();
            var toggle=child.ToggleButton;var panel=child.Explanation;var column=child.Titles[0].Parent;
            foreach(var row in cases.Where(r=>r.Language==lang&&(Environment.GetEnvironmentVariable("DBMV_ADVANCED_RESIZE_DIAGNOSTIC_ONLY")!="1"||r.Width==1280)))
            {
                window.Width=row.Width;await Flush();await Task.Delay(400);await Flush();if(lang=="en"&&row.Width==1280)Capture(window,host,output,"advanced-resize-en-1280.png");rows.Add(new{id=row.Id,language=lang,width=row.Width,actualWidth=window.ClientSize.Width,toggleRetained=ReferenceEquals(toggle,child.ToggleButton),explanationRetained=ReferenceEquals(panel,child.Explanation),columnRetained=ReferenceEquals(column,child.Titles[0].Parent),newPageAnimations=!ReferenceEquals(panel,child.Explanation)&&(child.Explanation!.Opacity<.999||Math.Abs(child.Explanation.RenderTransform?.Value.M32??0)>.001)?1:0,geometry=Geometry(child),textDiagnostic=child.Descriptions.Select(t=>new{text=t.Text,fontFamily=t.FontFamily.Name,fontSize=t.FontSize,fontWeight=(int)t.FontWeight,letterSpacing=t.LetterSpacing,lineHeight=t.LineHeight,textWrapping=t.TextWrapping.ToString(),lines=t.SystemLineTexts,widths=t.SystemLineWidths}).ToArray()});
            }
        }
        File.WriteAllText(Path.Combine(output,"pageintegration-advanced-resize.json"),JsonSerializer.Serialize(new{phase="HEADLESS_RECORDED",scope="Only standalone Advanced child viewport/gap4; original parent allocation supplied, Connection/Workspace resize API not invoked; actualOS unverified",rows},new JsonSerializerOptions{WriteIndented=true}));
    }
    internal static async Task PrepareNative(ReplicaWorkspace unused,Window window,string output)
    {
        var cases=ReadCases();var child=new WebAdvancedConnection{ReducedMotion=false};var host=new Host(child,cases);window.Content=host;window.Title="Replica Advanced resize verification";window.Height=900;window.Width=1280;window.Activate();var english=false;var ready=false;var snapshot=0;
        object? toggle=null,panel=null,column=null;var captured=new HashSet<string>();
        void Record(){if(!ready)return;try{window.UpdateLayout();var pictureId=(english?"en":"ko")+"-"+window.ClientSize.Width;if(ready&&english&&Math.Abs(window.ClientSize.Width-1280)<.01&&captured.Add(pictureId))Capture(window,host,output,"advanced-resize-en-1280.png");var v=new{snapshot=++snapshot,setupReady=ready,language=english?"en":"ko",width=window.ClientSize.Width,height=window.ClientSize.Height,dpr=window.RenderScaling,toggleRetained=ReferenceEquals(toggle,child.ToggleButton),explanationRetained=ReferenceEquals(panel,child.Explanation),columnRetained=ReferenceEquals(column,child.Titles[0].Parent),newPageAnimations=!ReferenceEquals(panel,child.Explanation)&&(child.Explanation!.Opacity<.999||Math.Abs(child.Explanation.RenderTransform?.Value.M32??0)>.001)?1:0,geometry=Geometry(child)};var f=Path.Combine(output,$"pageintegration-advanced-resize-state-{snapshot:D6}.json");File.WriteAllText(f+".tmp",JsonSerializer.Serialize(v,new JsonSerializerOptions{WriteIndented=true}));File.Move(f+".tmp",f);}catch(Exception e){File.WriteAllText(Path.Combine(output,"pageintegration-advanced-resize-observer-error.json"),JsonSerializer.Serialize(new{phase="UNVERIFIED",exception=e.ToString()}));}}
        async Task Seed(){ready=false;host.Language=english?"en":"ko";child.SetPresentation(english,true,1280);host.InvalidateMeasure();await Dispatcher.UIThread.InvokeAsync(()=>{},DispatcherPriority.Background);window.UpdateLayout();await Task.Delay(400);window.UpdateLayout();toggle=child.ToggleButton;panel=child.Explanation;column=child.Titles[0].Parent;ready=true;Record();}
        await Seed();window.KeyDown+=async(_,e)=>{if(e.Key!=Key.F8)return;e.Handled=true;if(english||!ready)return;english=true;await Seed();};window.SizeChanged+=(_,_)=>Dispatcher.UIThread.Post(Record,DispatcherPriority.Loaded);var observer=new DispatcherTimer{Interval=TimeSpan.FromMilliseconds(100)};observer.Tick+=(_,_)=>Record();observer.Start();window.Closed+=(_,_)=>observer.Stop();File.WriteAllText(Path.Combine(output,"pageintegration-advanced-resize-actual-ready.json"),JsonSerializer.Serialize(new{phase="ACTUAL_OS_READY",dpr=window.RenderScaling,scope="Only Advanced child4 external native resize; frozen original parent allocation, no Connection/Workspace SetViewport caller"}));
    }
}
