using System.Text.Json;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Input;
using Avalonia.Threading;
using Verifier.Desktop;

/// <summary>Original six Review resize histories only; no flow/input/child matrix replay.</summary>
internal static class PageIntegrationViewportHistoryProbe
{
    private static readonly double[][] Paths=[[1280,960],[1280,1100,960],[1280,1180,960],[1280,1181,960],[1280,1179,960],[1280,960,1100,960]];
    private static readonly JsonSerializerOptions JsonOptions=new(){WriteIndented=true};
    private static int StartIndex
    {
        get
        {
            const string prefix="--parent-resize-history-start=";
            var argument=Environment.GetCommandLineArgs().SingleOrDefault(a=>a.StartsWith(prefix,StringComparison.Ordinal));
            if(argument is null)return 0;
            if(argument!=prefix+"3")throw new InvalidOperationException("Only assigned missing paths3..5 resume allowed");
            return 3;
        }
    }
    private static void Validate(string oracle)
    {
        using var doc=JsonDocument.Parse(File.ReadAllText(oracle));var source=doc.RootElement;
        if(source.GetProperty("scope").GetString()!="parent-resize-history6"||source.GetProperty("rows").GetArrayLength()!=Paths.Length)throw new InvalidOperationException("Exact six original histories required");
        foreach(var pair in source.GetProperty("rows").EnumerateArray().Zip(Paths))
            if(!pair.First.GetProperty("widths").EnumerateArray().Select(x=>x.GetDouble()).SequenceEqual(pair.Second))throw new InvalidOperationException("Immutable original width order required");
    }
    private static async Task Flush(Window window)
    {await Dispatcher.UIThread.InvokeAsync(()=>{},DispatcherPriority.Loaded);window.UpdateLayout();}
    private static async Task Seed(ReplicaWorkspace w,Window window)
    {
        window.Width=1280;window.Height=900;w.ReducedMotion=false;
        // Public sample action only. No DB, private setter, report or synthetic run.
        await w.DispatchAsync("start");w.SetLanguage(false);await Flush(window);
        await w.DispatchAsync("connect");w.Navigate("review");await Flush(window);
        var deadline=DateTime.UtcNow.AddSeconds(2);
        while(w.CurrentPage!.Opacity!=1&&DateTime.UtcNow<deadline){await Task.Delay(20);await Flush(window);}
        if(w.CurrentPage is not WebReviewPage||w.CurrentPage.Opacity!=1||!w.Connection.Connected||w.SampleRun is not null)throw new InvalidOperationException("Default connected Review seed must settle with no run");
        w.Viewport.Offset=0;await Flush(window);
    }
    private static object Read(ReplicaWorkspace w,Window window,WebReviewPage initialPage,Control initialHeading,WebSummaryList initialSummary)
    {
        var page=w.CurrentPage as WebReviewPage??throw new InvalidOperationException("Current default Review required");
        object Box(Control c){var p=c.TranslatePoint(default,w)??throw new InvalidOperationException("Attached history control required");return new{x=p.X,y=p.Y,width=c.Bounds.Width,height=c.Bounds.Height};}
        var padding=w.Viewport.Padding;
        var items=page.Summary.Rows.Select(row=>
        {
            var texts=row.InlineValue is{}inline?new[]{inline.TextControl}:row.ValueLines;
            return new{row=Box(row),label=new{text=row.LabelText.Text,box=Box(row.LabelText),lines=row.LabelText.SystemLineTexts.Select(t=>t.TrimEnd()).ToArray()},
                value=new{text=string.Concat(texts.Select(t=>t.Text)),box=Box(row.ValueControl),lines=texts.SelectMany(t=>t.SystemLineTexts).Select(t=>t.TrimEnd()).ToArray()}};
        }).ToArray();
        return new{width=window.ClientSize.Width,height=window.ClientSize.Height,dpr=window.RenderScaling,
            pageSame=ReferenceEquals(page,initialPage),headingSame=ReferenceEquals(page.Heading.FocusTarget,initialHeading),summarySame=ReferenceEquals(page.Summary,initialSummary),
            pageOpacity=page.Opacity,noEntry=ReferenceEquals(page,initialPage)&&page.Opacity==1,
            media=new{small=window.ClientSize.Width<=1180,compact=window.ClientSize.Width<=1100},
            main=new{box=Box(w.Viewport),clientWidth=w.Viewport.ClientWidth,clientHeight=w.Viewport.ClientHeight,scrollHeight=w.Viewport.ScrollHeight,scrollTop=w.Viewport.Offset,gutter=w.Viewport.GutterWidth,padding=new{top=padding.Top,right=padding.Right,bottom=padding.Bottom,left=padding.Left}},
            page=Box(page),heading=Box(page.Heading),summary=Box(page.Summary),choice=Box(page.StoppedChoice.Checkbox),items,
            publicState=new{screen=w.Screen,connected=w.Connection.Connected,selected=w.Categories,tables=w.Tables,stopped=w.Stopped,run=w.SampleRun},
            headless=ReplicaPlatform.IsHeadless,scope="Prior gutter history, public connected default Review; original reference identity and geometry, no trusted user input claim"};
    }
    internal static async Task Check(ReplicaWorkspace w,Window window,string output,string oracle)
    {
        Validate(oracle);var rows=new List<object>();
        foreach(var widths in Paths.Skip(StartIndex))
        {
            await Seed(w,window);var page=(WebReviewPage)w.CurrentPage!;var heading=page.Heading.FocusTarget;var summary=page.Summary;var states=new List<object>();
            foreach(var width in widths)
            {window.Width=width;await Flush(window);AvaloniaHeadlessPlatform.ForceRenderTimerTick(1);states.Add(Read(w,window,page,heading,summary));}
            rows.Add(new{id=string.Join("-",widths),widths,states});
            File.WriteAllText(Path.Combine(output,"pageintegrationviewporthistory-records.json"),JsonSerializer.Serialize(new{scope="parent-resize-history6",phase="HEADLESS_RECORDED",rows},JsonOptions));
        }
    }
    internal static async Task PrepareNative(ReplicaWorkspace w,Window window,string output,string oracle)
    {
        Validate(oracle);var index=StartIndex-1;var snapshot=0;var ready=false;var advancing=false;WebReviewPage? page=null;Control? heading=null;WebSummaryList? summary=null;
        void Record()
        {
            if(!ready||page is null||heading is null||summary is null)return;
            window.UpdateLayout();var state=Read(w,window,page,heading,summary);var payload=new{index,snapshot=++snapshot,observedUtc=DateTimeOffset.UtcNow.ToString("O"),id=string.Join("-",Paths[index]),widths=Paths[index],ready,state};
            var path=Path.Combine(output,"pageintegrationviewporthistory-actual-state.json");File.WriteAllText(path+".tmp",JsonSerializer.Serialize(payload,JsonOptions));File.Move(path+".tmp",path,true);
        }
        async Task Next()
        {
            if(advancing||index>=Paths.Length-1)return;advancing=true;ready=false;
            try{await Seed(w,window);index++;page=(WebReviewPage)w.CurrentPage!;heading=page.Heading.FocusTarget;summary=page.Summary;ready=true;Record();}
            finally{advancing=false;}
        }
        window.KeyDown+=async(_,e)=>{if(e.Key==Key.F8){e.Handled=true;await Next();}};
        var timer=new DispatcherTimer{Interval=TimeSpan.FromMilliseconds(100)};timer.Tick+=(_,_)=>Record();window.Closed+=(_,_)=>timer.Stop();await Next();timer.Start();
    }
}
