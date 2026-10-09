using System.Diagnostics;
using System.Collections.Specialized;
using System.Text.Json;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Automation;
using Avalonia.Automation.Peers;
using Avalonia.Headless;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Verifier.Desktop;

/// <summary>Existing MainContent toast: parent allocation/replacement/timer/lifetime only.</summary>
internal static class PageIntegrationToastProbe
{
    private static readonly JsonSerializerOptions Json=new(){WriteIndented=true};
    private static WebSurfaceBox? Toast(ReplicaWorkspace w)=>w.Children.OfType<WebSurfaceBox>().SingleOrDefault(t=>t.Child is WebSystemTextBlock);
    private static string Short(bool en)=>en?"Sample trial setup confirmed. No actual DB was verified.":"예시 시험 준비를 확인했습니다. 실제 DB 검증은 아닙니다.";
    private static string Long(bool en)=>string.Join(" ",Enumerable.Repeat(Short(en),4));
    private static async Task Flush(Window window)
    {
        await Dispatcher.UIThread.InvokeAsync(()=>{},DispatcherPriority.Background);window.UpdateLayout();
        if(Environment.GetEnvironmentVariable("DBMV_HEADLESS")=="1")AvaloniaHeadlessPlatform.ForceRenderTimerTick(1);
    }
    private static object Box(Control c,ReplicaWorkspace w)
    {var p=c.TranslatePoint(default,w)??throw new InvalidOperationException("Attached toast required");return new{x=p.X,y=p.Y,width=c.Bounds.Width,height=c.Bounds.Height};}
    private static object Read(ReplicaWorkspace w,Window window,string id)
    {
        var toast=Toast(w)??throw new InvalidOperationException("Current toast absent");var label=(WebSystemTextBlock)toast.Child!;var labelPeer=ControlAutomationPeer.CreatePeerForElement(label);var toastPeer=ControlAutomationPeer.CreatePeerForElement(toast);
        return new{id,width=window.ClientSize.Width,height=window.ClientSize.Height,dpr=window.RenderScaling,text=label.Text,
            parent=new{finalWidth=w.Bounds.Width,finalHeight=w.Bounds.Height,documentGutterWidth=w.DocumentVertical?.GutterWidth??0,documentGutterHeight=w.DocumentHorizontal?.GutterHeight??0,documentClientWidth=w.Bounds.Width-(w.DocumentVertical?.GutterWidth??0),documentClientHeight=w.Bounds.Height-(w.DocumentHorizontal?.GutterHeight??0)},
            box=Box(toast,w),labelBox=Box(label,w),lines=label.SystemLineTexts.Select(t=>t.TrimEnd()).ToArray(),
            style=new{background=toast.Background?.ToString(),borderColor=toast.BorderBrush?.ToString(),borderWidth=toast.BorderThickness.ToString(),radius=toast.CornerRadius.ToString(),padding=toast.Padding.ToString(),shadow=toast.BoxShadow.ToString(),fontSize=label.FontSize,lineHeight=label.LineHeight,color=label.Foreground?.ToString(),maxWidth=toast.MaxWidth,hitTest=toast.IsHitTestVisible},
            accessibility=new{toastAttachedLive=AutomationProperties.GetLiveSetting(toast).ToString(),labelAttachedLive=AutomationProperties.GetLiveSetting(label).ToString(),labelName=labelPeer?.GetName(),labelType=labelPeer?.GetAutomationControlType().ToString(),labelLive=labelPeer?.GetLiveSetting().ToString(),labelControl=labelPeer?.IsControlElement(),labelContent=labelPeer?.IsContentElement(),labelChildren=labelPeer?.GetChildren().Count,toastPeerType=toastPeer?.GetAutomationControlType().ToString(),toastPeerLive=toastPeer?.GetLiveSetting().ToString(),toastPeerName=toastPeer?.GetName(),toastPeerControl=toastPeer?.IsControlElement(),toastPeerContent=toastPeer?.IsContentElement(),toastPeerChildren=toastPeer?.GetChildren().Count},
            focus=window.FocusManager?.GetFocusedElement()?.GetType().Name,opacity=toast.Opacity,scope="Parent settled box/style/text; existing Motion200ms contract reused"};
    }
    private static void Capture(ReplicaWorkspace w,Window window,string output,string id)
    {
        var s=window.RenderScaling;using var bitmap=new RenderTargetBitmap(new PixelSize((int)Math.Ceiling(window.ClientSize.Width*s),(int)Math.Ceiling(window.ClientSize.Height*s)),new Vector(96*s,96*s));
        bitmap.Render(w);bitmap.Save(Path.Combine(output,id+".png"),new PngBitmapEncoderOptions());
    }
    private static async Task Until(Stopwatch clock,int ms)
    {var remaining=ms-clock.Elapsed.TotalMilliseconds;if(remaining>0)await Task.Delay(TimeSpan.FromMilliseconds(remaining));}
    private static async Task Run(ReplicaWorkspace w,Window window,string output,bool native)
    {
        if(Environment.GetEnvironmentVariable("DBMV_TOAST_KO_TIMER_ONLY")=="1"){await RunKoTimerOnly(w,window,output,native);return;}
        var geometryOnly=Environment.GetEnvironmentVariable("DBMV_TOAST_GEOMETRY_ONLY")=="1";
        var rows=new List<object>();var lifetimes=new List<object>();var errors=new List<string>();
        foreach(var en in new[]{false,true})foreach(var width in new[]{960,1280})foreach(var reduced in new[]{false,true})
        {
            var id=(en?"en":"ko")+"-"+width+"-"+(reduced?"reduced":"normal");window.Width=width;window.Height=900;w.SetLanguage(en);w.ReducedMotion=reduced;await Flush(window);
            w.ShowToast(Short(en));await Task.Delay(240);await Flush(window);rows.Add(Read(w,window,id+"-short"));
            w.ShowToast(Long(en));var oldClock=Stopwatch.StartNew();var old=Toast(w)!;await Task.Delay(240);await Flush(window);rows.Add(Read(w,window,id+"-long"));Capture(w,window,output,id+"-long");
            // The timer has no width branch. Geometry16 is full; each language
            // and motion mode gets one real-time lifecycle at960 only.
            if(width!=960||geometryOnly)continue;
            await Until(oldClock,1200);var focus=window.FocusManager?.GetFocusedElement();w.ShowToast(Short(en));var newClock=Stopwatch.StartNew();await Flush(window);var replacement=Toast(w)!;
            var replaced=old!=replacement&&!w.Children.Contains(old)&&Toast(w)?.Child is WebSystemTextBlock text&&text.Text==Short(en);
            await Until(oldClock,4300);await Flush(window);var survivesOldDeadline=ReferenceEquals(Toast(w),replacement);var oldDeadlineMs=oldClock.Elapsed.TotalMilliseconds;
            await Until(newClock,4100);await Flush(window);var beforeNewDeadline=ReferenceEquals(Toast(w),replacement);var beforeMs=newClock.Elapsed.TotalMilliseconds;
            await Until(newClock,4400);await Flush(window);var removed=Toast(w) is null;var afterMs=newClock.Elapsed.TotalMilliseconds;
            var focusSame=ReferenceEquals(focus,window.FocusManager?.GetFocusedElement());
            lifetimes.Add(new{id,replaced,survivesOldDeadline,beforeNewDeadline,removed,focusSame,oldDeadlineMs,beforeMs,afterMs,attachedOld=old.IsAttachedToVisualTree()});
            if(!replaced||!survivesOldDeadline||!beforeNewDeadline||!removed||!focusSame)errors.Add(id+" parent replacement/timer/focus failed");
            File.WriteAllText(Path.Combine(output,"pageintegration-toast.json"),JsonSerializer.Serialize(new{phase=native?"ACTUAL_OS_RECORDED":"HEADLESS_RECORDED",rows,lifetimes,errors},Json));
        }
        if(geometryOnly)
        {
            File.WriteAllText(Path.Combine(output,"pageintegration-toast.json"),JsonSerializer.Serialize(new{phase=native?"ACTUAL_OS_RECORDED":"HEADLESS_RECORDED",geometryOnly=true,rows,lifetimes,errors},Json));return;
        }
        // Teardown exercises the real public parent disposal after a new timer starts.
        w.ShowToast(Long(true));await Flush(window);var disposalToast=Toast(w)!;var disposalClock=Stopwatch.StartNew();w.Dispose();await Flush(window);
        var disposedRemoved=!w.Children.Contains(disposalToast)&&Toast(w) is null;await Until(disposalClock,4400);await Flush(window);
        var oldTimerNoReappearance=Toast(w) is null;
        File.WriteAllText(Path.Combine(output,"pageintegration-toast.json"),JsonSerializer.Serialize(new{phase=native?"ACTUAL_OS_RECORDED":"HEADLESS_RECORDED",rows,lifetimes,disposal=new{disposedRemoved,oldTimerNoReappearance,elapsedMs=disposalClock.Elapsed.TotalMilliseconds},errors},Json));
        if(!disposedRemoved||!oldTimerNoReappearance||errors.Count>0)throw new InvalidOperationException("Toast parent lifecycle differences; retained scoped evidence");
    }
    private static async Task RunKoTimerOnly(ReplicaWorkspace w,Window window,string output,bool native)
    {
        var records=new List<object>();var errors=new List<string>();
        foreach(var reduced in new[]{false,true})
        {
            window.Width=960;window.Height=900;w.SetLanguage(false);w.ReducedMotion=reduced;await Flush(window);
            var clock=Stopwatch.StartNew();var events=new List<(string Kind,int Id,double Time)>();var ids=new Dictionary<WebSurfaceBox,int>();
            void Observe(object? sender,NotifyCollectionChangedEventArgs e)
            {
                void Record(System.Collections.IList? list,string kind){if(list is null)return;foreach(var item in list)if(item is WebSurfaceBox surface&&surface.Child is WebSystemTextBlock){if(!ids.TryGetValue(surface,out var id)){id=ids.Count+1;ids.Add(surface,id);}events.Add((kind,id,clock.Elapsed.TotalMilliseconds));}}
                Record(e.OldItems,"removed");Record(e.NewItems,"added");
            }
            w.Children.CollectionChanged+=Observe;
            try
            {
                w.ShowToast(Long(false));var old=Toast(w)!;await Flush(window);var peer=Read(w,window,"ko-960-"+(reduced?"reduced":"normal"));
                await Until(clock,1200);var focus=window.FocusManager?.GetFocusedElement();w.ShowToast(Short(false));var current=Toast(w)!;var newClock=Stopwatch.StartNew();
                await Until(clock,4300);var survivesOldDeadline=ReferenceEquals(Toast(w),current);var oldDeadlineMs=clock.Elapsed.TotalMilliseconds;
                // No layout/renderer work between deadline wake-up and logical sample.
                await Until(newClock,3800);var beforePresent=ReferenceEquals(Toast(w),current);var beforeMs=newClock.Elapsed.TotalMilliseconds;
                await Until(newClock,4400);var removed=Toast(w) is null;var afterMs=newClock.Elapsed.TotalMilliseconds;
                var add=events.Single(v=>v.Kind=="added"&&v.Id==ids[current]);var removals=events.Where(v=>v.Kind=="removed"&&v.Id==ids[current]).ToArray();
                double? removalAge=removals.Length==1?removals[0].Time-add.Time:null;
                var uninterrupted=events.Count==4&&events.Count(v=>v.Kind=="added"&&v.Id==ids[current])==1&&removals.Length==1&&removalAge>=4200;
                var good=old!=current&&survivesOldDeadline&&beforePresent&&beforeMs<4200&&removed&&uninterrupted&&ReferenceEquals(focus,window.FocusManager?.GetFocusedElement());
                records.Add(new{id="ko-960-"+(reduced?"reduced":"normal"),survivesOldDeadline,oldDeadlineMs,beforePresent,beforeMs,removed,afterMs,removalAgeMs=removalAge,uninterruptedUntil4200=uninterrupted,focusSame=ReferenceEquals(focus,window.FocusManager?.GetFocusedElement()),events=events.Select(v=>new{kind=v.Kind,id=v.Id,timeMs=v.Time}).ToArray(),publicPeer=peer});
                if(!good)errors.Add("KO timer continuation failed: "+reduced);
            }
            finally{w.Children.CollectionChanged-=Observe;}
        }
        File.WriteAllText(Path.Combine(output,"pageintegration-toast.json"),JsonSerializer.Serialize(new{phase=native?"ACTUAL_OS_RECORDED":"HEADLESS_RECORDED",targetedKoTimerOnly=true,lifetimes=records,errors,scope="Only failed KO2 late observers: synchronous child transition times prove continuous replacement through4200; unchanged geometry16/EN2/Dispose reused"},Json));
        if(errors.Count>0)throw new InvalidOperationException("Targeted KO timer differences");
    }
    internal static Task Check(ReplicaWorkspace w,Window window,string output)=>Run(w,window,output,false);
    internal static async Task PrepareNative(ReplicaWorkspace w,Window window,string output)
    {
        try{await Run(w,window,output,true);}
        catch(Exception e){File.WriteAllText(Path.Combine(output,"pageintegration-toast-error.json"),JsonSerializer.Serialize(new{phase="UNVERIFIED",exception=e.ToString()},Json));}
        finally{window.Close();}
    }
}
