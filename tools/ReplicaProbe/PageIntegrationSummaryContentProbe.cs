using System.Text.Json;
using Avalonia;
using Avalonia.Automation.Peers;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using Verifier.Desktop;

/// <summary>New full static summary text/order/wrap/placement in the actual App parent.</summary>
internal static class PageIntegrationSummaryContentProbe
{
    private static readonly string[] Modes=["review-default","review-trial","save-completed","save-interrupted","save-pending"];
    private sealed record Context(string Mode,bool English);
    private static bool PeerOnly=>Environment.GetEnvironmentVariable("DBMV_SUMMARY_CONTENT_PEER_ONLY")=="1";
    private static Context[] Contexts()=>new[]{false,true}.SelectMany(en=>(PeerOnly?new[]{"review-default"}:Modes).Select(mode=>new Context(mode,en))).ToArray();
    private static async Task Flush(Window window,bool settle=false)
    {
        await Dispatcher.UIThread.InvokeAsync(()=>{},DispatcherPriority.Loaded);window.UpdateLayout();
        if(settle){await Task.Delay(400);window.UpdateLayout();}
    }
    private static async Task Seed(ReplicaWorkspace w,Window window,Context context)
    {
        window.Height=900;w.ReducedMotion=false;await w.DispatchAsync("start");w.SetLanguage(context.English);
        var categories=context.Mode=="review-trial"?ReplicaSampleRun.CategoryKeys:ReplicaSampleRun.CategoryKeys.Take(4).ToArray();
        var run=ReplicaSampleRun.Create(categories,ReplicaSampleRun.AllowedTables,ReplicaSampleRun.AllowedTables.ToDictionary(x=>x,x=>x)) with{Id="UI-SUMMARY"};
        if(context.Mode=="save-completed")run=run.CompleteAll();
        if(context.Mode=="save-interrupted")run=run with{Interrupted=true};
        // Public report loading stops the run timer; this is a bounded presentation seed.
        if(context.Mode.StartsWith("review",StringComparison.Ordinal))w.SetScope(categories,ReplicaSampleRun.AllowedTables);
        else w.LoadSampleReport(run);
        w.Navigate(context.Mode.StartsWith("review",StringComparison.Ordinal)?"review":"save");await Flush(window,true);
    }
    private static object Read(ReplicaWorkspace w,Window window,Context context)
    {
        var page=w.CurrentPage??throw new InvalidOperationException("Attached actual App page required");
        var summary=page switch{WebReviewPage p=>p.Summary,WebSavePage p=>p.Summary,_=>throw new InvalidOperationException("Exact Review or Save parent required")};
        object Box(Control c){var p=c.TranslatePoint(default,page)??throw new InvalidOperationException("Attached summary child required");return new{x=p.X,y=p.Y,width=c.Bounds.Width,height=c.Bounds.Height};}
        object Peer(Control control)
        {
            var peer=ControlAutomationPeer.CreatePeerForElement(control)??throw new InvalidOperationException("Attached summary text peer required");
            return new{name=peer.GetName(),type=peer.GetAutomationControlType().ToString(),children=peer.GetChildren()?.Count??0,control=peer.IsControlElement(),content=peer.IsContentElement(),box=peer.GetBoundingRectangle().ToString()};
        }
        var items=summary.Rows.Select(row=>
        {
            var inline=row.InlineValue;var lines=row.ValueLines;var value=inline?.TextControl.Text??string.Concat(lines.Select(t=>t.Text));
            var texts=inline is null?lines: new[]{inline.TextControl};
            var text=texts.First();
            var peer=inline is null?null:ControlAutomationPeer.CreatePeerForElement(inline);
            return new{label=row.LabelText.Text,value,strong=inline is null&&lines.All(t=>(int)t.FontWeight==700),mono=inline is not null,
                suffixPainted=inline is not null&&inline.MutedRegions.Count>0,
                logicalLines=inline is null?lines.Select(t=>t.Text).ToArray():new[]{inline.TextControl.Text},
                labelLines=row.LabelText.SystemLineTexts.Select(t=>t.TrimEnd()).ToArray(),valueLines=texts.SelectMany(t=>t.SystemLineTexts).Select(t=>t.TrimEnd()).ToArray(),
                labelFont=new{size=row.LabelText.FontSize,weight=(int)row.LabelText.FontWeight},valueFont=new{size=text.FontSize,weight=(int)text.FontWeight},
                publicReading=new{label=Peer(row.LabelText),values=texts.Select(t=>Peer(t)).ToArray()},
                publicInlineReading=peer is null?null:new{textControl=Peer(inline!.TextControl),children=peer.GetChildren().Select(child=>new{name=child.GetName(),type=child.GetAutomationControlType().ToString(),children=child.GetChildren().Count,control=child.IsControlElement(),content=child.IsContentElement(),box=child.GetBoundingRectangle().ToString()}).ToArray()},
                publicPaint=texts.Select(t=>new{t.Text,family=t.FontFamily.Name,t.FontSize,weight=(int)t.FontWeight,t.LineHeight,t.TabularNumbers,color=t.Foreground?.ToString(),widths=t.SystemLineWidths.ToArray()}).ToArray(),
                mutedRegions=inline?.MutedRegions.Select(r=>new{x=r.X,y=r.Y,width=r.Width,height=r.Height}).ToArray(),
                publicPeer=peer is null?null:new{name=peer.GetName(),type=peer.GetAutomationControlType().ToString(),children=peer.GetChildren()?.Count??0,box=peer.GetBoundingRectangle().ToString()},
                boxes=new{row=Box(row),label=Box(row.LabelText),value=Box(row.ValueControl)}};
        }).ToArray();
        return new{id=context.Mode+"-"+(context.English?"en":"ko")+"-"+window.ClientSize.Width,mode=context.Mode,language=context.English?"en":"ko",width=window.ClientSize.Width,actualWidth=window.ClientSize.Width,height=window.ClientSize.Height,dpr=window.RenderScaling,
            run=w.SampleRun is{}run?new{complete=run.Complete,interrupted=run.Interrupted,groups=run.Groups.Select(g=>new{key=g.Key,status=g.Status}).ToArray()}:null,
            parentDiagnostic=new{viewport=Box(w.Viewport),w.Viewport.ClientWidth,w.Viewport.ClientHeight,w.Viewport.ScrollHeight,w.Viewport.GutterWidth,padding=w.Viewport.Padding.ToString(),page=Box(page),pageDesiredHeight=page.DesiredSize.Height,footer=Box(w.Footer),footerDesiredHeight=w.Footer.DesiredSize.Height,documentGutterWidth=w.DocumentVertical?.GutterWidth,documentGutterHeight=w.DocumentHorizontal?.GutterHeight},
            summary=Box(summary),items,scope="Public full summary values/fonts/line wrapping/relative layout; Mono suffix literal in combined text and muted clips, protected InlineText paint contract reused"};
    }
    private static void Capture(ReplicaWorkspace w,Window window,string output,string name)
    {
        var s=window.RenderScaling;using var bitmap=new RenderTargetBitmap(new PixelSize((int)Math.Ceiling(window.ClientSize.Width*s),(int)Math.Ceiling(window.ClientSize.Height*s)),new Vector(96*s,96*s));
        bitmap.Render(w);bitmap.Save(Path.Combine(output,name),new PngBitmapEncoderOptions());
    }
    internal static async Task Check(ReplicaWorkspace w,Window window,string output)
    {
        var rows=new List<object>();
        var updatesOnly=Environment.GetEnvironmentVariable("DBMV_SUMMARY_CONTENT_UPDATES_ONLY")=="1";
        if(updatesOnly){window.Width=1280;await Seed(w,window,new("review-default",true));}
        foreach(var context in updatesOnly?Array.Empty<Context>():Contexts())
        {
            var target=Environment.GetEnvironmentVariable("DBMV_SUMMARY_CONTENT_TARGET");
            if(target is not null&&!target.StartsWith(context.Mode+"-"+(context.English?"en":"ko")+"-",StringComparison.Ordinal))continue;
            window.Width=1280;await Seed(w,window,context);
            foreach(var width in PeerOnly?new[]{960,1280}:new[]{960,1180,1280})
            {
                if(target is not null&&target!=context.Mode+"-"+(context.English?"en":"ko")+"-"+width)continue;
                window.Width=width;await Flush(window);AvaloniaHeadlessPlatform.ForceRenderTimerTick(1);rows.Add(Read(w,window,context));
                if(context.English&&width==960&&(context.Mode=="review-trial"||context.Mode=="save-completed"))Capture(w,window,output,"summary-content-"+context.Mode+"-en-960.png");
                File.WriteAllText(Path.Combine(output,"pageintegration-summary-content.json"),JsonSerializer.Serialize(new{phase="HEADLESS_RECORDED",scope="summary-content30",rows},new JsonSerializerOptions{WriteIndented=true}));
            }
        }
        if(PeerOnly&&OperatingSystem.IsWindows()&&Environment.GetEnvironmentVariable("DBMV_SUMMARY_CONTENT_INLINE_ONLY")!="1"&&w.CurrentPage is WebReviewPage plain)
        {
            var label=plain.Summary.Rows[0].LabelText;var original=label.Text;var peer=ControlAutomationPeer.CreatePeerForElement(label)!;var updates=new List<object>();
            void RecordPlain(string expected)=>updates.Add(new{expected,name=peer.GetName(),type=peer.GetAutomationControlType().ToString(),children=peer.GetChildren().Count,control=peer.IsControlElement(),content=peer.IsContentElement(),samePeer=ReferenceEquals(peer,ControlAutomationPeer.CreatePeerForElement(label))});
            AutomationProperties.SetName(label,"Named summary label");await Flush(window);RecordPlain("Named summary label");
            label.Text="Changed text under attached name";await Flush(window);RecordPlain("Named summary label");
            label.ClearValue(AutomationProperties.NameProperty);await Flush(window);RecordPlain("Changed text under attached name");
            label.Text=original;await Flush(window);RecordPlain(original??"");
            File.WriteAllText(Path.Combine(output,"pageintegration-summary-plain-peer-updates.json"),JsonSerializer.Serialize(new{scope="Windows generic text attachedName precedence/changedText/clearName/originalRestore, same publicpeer; headless only",rows=updates},new JsonSerializerOptions{WriteIndented=true}));
        }
        if(PeerOnly&&OperatingSystem.IsWindows()&&w.CurrentPage is WebReviewPage windowsInline)
        {
            var inline=windowsInline.Summary.Rows[1].InlineValue!;var peer=ControlAutomationPeer.CreatePeerForElement(inline)!;
            var original=inline.TextControl.Text??"";var prefix=windowsInline.View.SourceDatabase+" → "+windowsInline.View.TargetDatabase;var suffix=original[prefix.Length..];var originalChildren=peer.GetChildren().ToArray();var updates=new List<object>();
            foreach(var value in new[]{("updated_sample"," / safe suffix"),("","") ,(prefix,suffix)})
            {
                inline.SetPresentation(value.Item1,value.Item2);await Flush(window);var children=peer.GetChildren().ToArray();
                updates.Add(new{prefix=value.Item1,suffix=value.Item2,samePeer=ReferenceEquals(peer,ControlAutomationPeer.CreatePeerForElement(inline)),sameChildren=children.Length==originalChildren.Length&&children.Zip(originalChildren).All(pair=>ReferenceEquals(pair.First,pair.Second)),textControlName=ControlAutomationPeer.CreatePeerForElement(inline.TextControl)!.GetName(),children=children.Select(child=>new{name=child.GetName(),type=child.GetAutomationControlType().ToString(),control=child.IsControlElement(),content=child.IsContentElement(),children=child.GetChildren().Count,box=child.GetBoundingRectangle().ToString()}).ToArray()});
            }
            File.WriteAllText(Path.Combine(output,"pageintegration-summary-inline-windows-updates.json"),JsonSerializer.Serialize(new{scope="Windows public inline prefix/suffix samepeer/update-empty-restore; no privatepaint child reflection",rows=updates},new JsonSerializerOptions{WriteIndented=true}));
        }
        if(PeerOnly&&OperatingSystem.IsMacOS()&&w.CurrentPage is WebReviewPage current)
        {
            var inline=current.Summary.Rows[1].InlineValue??throw new InvalidOperationException("Current mono required");
            var peer=ControlAutomationPeer.CreatePeerForElement(inline)??throw new InvalidOperationException("Current inline peer required");
            var original=inline.TextControl.Text??"";var prefix=current.View.SourceDatabase+" → "+current.View.TargetDatabase;
            if(!original.StartsWith(prefix,StringComparison.Ordinal))throw new InvalidOperationException("Literal original prefix required");
            var suffix=original[prefix.Length..];var updates=new List<object>();
            foreach(var value in new[]{("updated_sample"," / safe suffix"),("","")})
            {
                inline.SetPresentation(value.Item1,value.Item2);await Flush(window);
                updates.Add(new{expected=value.Item1+value.Item2,name=peer.GetName(),children=peer.GetChildren()?.Count??0,samePeer=ReferenceEquals(peer,ControlAutomationPeer.CreatePeerForElement(inline))});
            }
            inline.SetPresentation(prefix,suffix);await Flush(window);
            updates.Add(new{expected=original,name=peer.GetName(),children=peer.GetChildren()?.Count??0,samePeer=ReferenceEquals(peer,ControlAutomationPeer.CreatePeerForElement(inline))});
            File.WriteAllText(Path.Combine(output,"pageintegration-summary-peer-updates.json"),JsonSerializer.Serialize(new{scope="Same public inline/peer name updates, empty value and original restore; headless-only diagnostic after finite4 capture",rows=updates},new JsonSerializerOptions{WriteIndented=true}));
        }
    }
    internal static async Task PrepareNative(ReplicaWorkspace w,Window window,string output)
    {
        var contexts=Contexts();var index=0;var snapshot=0;var ready=false;var recording=false;var captured=new HashSet<int>();var json=new JsonSerializerOptions{WriteIndented=true};
        void Record()
        {
            if(!ready||recording)return;recording=true;
            try
            {
                window.UpdateLayout();var state=Read(w,window,contexts[index]);
                if(contexts[index].English&&Math.Abs(window.ClientSize.Width-960)<.01&&captured.Add(index))Capture(w,window,output,"summary-content-"+contexts[index].Mode+"-en-960.png");
                var f=Path.Combine(output,$"pageintegration-summary-content-state-{++snapshot:D6}.json");File.WriteAllText(f+".tmp",JsonSerializer.Serialize(new{index,snapshot,setupReady=ready,state},json));File.Move(f+".tmp",f);
            }
            catch(Exception e){File.WriteAllText(Path.Combine(output,"pageintegration-summary-content-observer-error.json"),JsonSerializer.Serialize(new{phase="UNVERIFIED",exception=e.ToString()},json));}
            finally{recording=false;}
        }
        async Task Next(){ready=false;await Seed(w,window,contexts[index]);ready=true;Record();}
        window.Width=1280;window.Activate();await Next();
        window.KeyDown+=async(_,e)=>{if(e.Key!=Avalonia.Input.Key.F8||!ready||index>=contexts.Length-1)return;e.Handled=true;index++;await Next();};
        var timer=new DispatcherTimer{Interval=TimeSpan.FromMilliseconds(100)};timer.Tick+=(_,_)=>Record();timer.Start();window.Closed+=(_,_)=>timer.Stop();
        File.WriteAllText(Path.Combine(output,"pageintegration-summary-content-actual-ready.json"),JsonSerializer.Serialize(new{scope="summary-content30",contexts=contexts.Length,widths=new[]{960,1180,1280},dpr=window.RenderScaling},json));
    }
}
