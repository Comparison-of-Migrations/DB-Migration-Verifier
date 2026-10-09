using System.Text.Json;
using Avalonia;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Threading;
using Verifier.Desktop;

/// <summary>Only connected parent CSS resize identity/layout, no child matrix or timer flow replay.</summary>
internal static class PageIntegrationParentResizeProbe
{
    private static readonly string[] Screens=["home","capabilities","review","running","results","save"];
    private static readonly double[] Widths=[1280,1100,960,1280];
    private sealed record Context(string Id,string Screen,bool English);
    private static IEnumerable<Context> Contexts()
    {
        var contexts=new[]{false,true}.SelectMany(english=>Screens.Select(screen=>new Context(screen+"-"+(english?"en":"ko"),screen,english)));
        var target=Environment.GetCommandLineArgs().SingleOrDefault(a=>a.StartsWith("--parent-resize-target=",StringComparison.Ordinal))?[23..];
        if(target is not null){var targets=target.Split(',');if(targets.Distinct().Count()!=targets.Length||targets.Any(t=>!contexts.Any(c=>c.Id==t)))throw new InvalidOperationException("Exact assigned parent targets required");return contexts.Where(c=>targets.Contains(c.Id));}
        return contexts;
    }
    private static bool LayoutDiagnostic=>Environment.GetCommandLineArgs().Contains("--parent-resize-layout-diagnostic");
    private static async Task Flush(Window window,bool settle=false)
    {await Dispatcher.UIThread.InvokeAsync(()=>{},DispatcherPriority.Loaded);window.UpdateLayout();if(settle)await Task.Delay(360);window.UpdateLayout();}
    private static WebPageHeading Heading(ReplicaWorkspace w)=>w.CurrentPage switch
    {WebHomePage p=>p.Heading,WebPreflightPage p=>p.Heading,WebReviewPage p=>p.Heading,WebProgressPage p=>p.Heading,WebResultsPage p=>p.Heading,WebSavePage p=>p.Heading,_=>throw new InvalidOperationException("Expected assigned connected parent")};
    private static Control Target(ReplicaWorkspace w)=>w.CurrentPage switch
    {WebHomePage p=>p.StartButton,WebPreflightPage p=>p.Table.Rows.Single(r=>r.Key=="access").PermissionsButton,WebReviewPage p=>p.StoppedChoice.Checkbox,WebProgressPage=>w.Footer.ActionButtons.Single(),WebResultsPage p=>p.FilterButtons.Single(b=>AutomationProperties.GetAutomationId(b)=="Results-filter-all"),WebSavePage p=>p.Options.Options.Single(o=>o.Format=="json").Checkbox,_=>throw new InvalidOperationException("Expected scoped parent focus target")};
    private static bool Selected(Control target)=>target switch{WebCheckbox b=>b.IsChecked==true,WebActionButton b=>b.Selected,_=>false};
    private static async Task Seed(ReplicaWorkspace workspace,Window window,Context context)
    {
        window.Width=1280;window.Height=900;workspace.ReducedMotion=false;workspace.SetLanguage(context.English);
        var run=ReplicaSampleRun.Create(["data","schema","access","objects"],ReplicaSampleRun.AllowedTables,ReplicaSampleRun.AllowedTables.ToDictionary(x=>x,x=>x)) with{Id="UI-PARENTRESIZE"};
        if(context.Screen!="running")run=run.CompleteAll();
        // LoadSampleReport stops the real timer. Pending Progress remains an explicit synthetic fixture.
        workspace.LoadSampleReport(run);workspace.Navigate(context.Screen);await Flush(window,true);
        // The source waits the original page's entrance to finish before any resize.
        // Observe the public current-page opacity rather than comparing during a seed animation.
        var deadline=DateTime.UtcNow.AddSeconds(2);
        while(workspace.CurrentPage!.Opacity!=1&&DateTime.UtcNow<deadline){await Task.Delay(20);window.UpdateLayout();}
        if(workspace.CurrentPage!.Opacity!=1)throw new InvalidOperationException("Initial parent entrance did not settle; resize not started");
        Target(workspace).Focus(NavigationMethod.Tab);workspace.Viewport.Offset=0;await Flush(window);
    }
    private static object Capture(ReplicaWorkspace w,Window window,Control initialPage,Control initialFocus,Control initialHeading)
    {
        object? Box(Control? c){if(c is null)return null;var p=c.TranslatePoint(default,w)??throw new InvalidOperationException("Attached current control required");return new{x=p.X,y=p.Y,width=c.Bounds.Width,height=c.Bounds.Height};}
        var page=w.CurrentPage!;var target=Target(w);var summary=page switch{WebReviewPage p=>p.Summary,WebSavePage p=>p.Summary,_=>null};var row=summary?.Rows.FirstOrDefault();
        var table=(page as WebPreflightPage)?.Table;
        object TextLayout(WebSystemTextBlock text)=>new{text=text.Text,box=Box(text),desired=new{width=text.DesiredSize.Width,height=text.DesiredSize.Height},lines=text.SystemLineTexts,widths=text.SystemLineWidths};
        return new{width=window.ClientSize.Width,height=window.ClientSize.Height,pageSame=ReferenceEquals(page,initialPage),headingSame=ReferenceEquals(Heading(w).FocusTarget,initialHeading),focusTargetSame=ReferenceEquals(target,initialFocus),focusTargetFocused=ReferenceEquals(window.FocusManager?.GetFocusedElement(),initialFocus),selected=Selected(target),screen=w.Screen,english=w.English,announcement=w.Announcement,pageOpacity=page.Opacity,noEntry=ReferenceEquals(page,initialPage)&&page.Opacity==1,mainScroll=w.Viewport.Offset,documentScroll=new{x=w.DocumentHorizontal?.Offset??0,y=w.DocumentVertical?.Offset??0},boxes=new{topbar=Box(w.TopBar),content=Box(w.Viewport),page=Box(page),heading=Box(Heading(w)),focus=Box(target),footer=Box(w.Footer),summaryLabel=Box(row?.LabelText),summaryValue=Box(row?.ValueControl),metrics=Box((page as WebResultsPage)?.Metrics)},footerActions=new[]{w.Footer.BackButton}.Concat(w.Footer.ActionButtons).Where(b=>b is not null).Select(b=>new{action=AutomationProperties.GetAutomationId(b!)![7..],disabled=!b!.IsEnabled}).ToArray(),formats=w.Formats,stopped=w.Stopped,visited=w.Visited.Order().ToArray(),dpr=window.RenderScaling,tableDiagnostics=table is null?null:new{box=Box(table),table.ColumnWidths,table.MinimumColumns,table.MaximumColumns,header=new{box=Box(table.Header),labels=table.Header.Labels.Select(TextLayout).ToArray()},rows=table.Rows.Select(r=>new{key=r.Key,box=Box(r),title=TextLayout(r.TitleText),description=TextLayout(r.DescriptionText),details=TextLayout(r.DetailText),permissions=Box(r.PermissionsButton)}).ToArray()},scope="Connected parent resize only; pending Progress has no timer, public programmatic focus seed only"};
    }
    internal static async Task Check(ReplicaWorkspace workspace,Window window,string output,string sourceOracle)
    {
        if(Environment.GetCommandLineArgs().Contains("--parent-resize-history-only")){await PageIntegrationViewportHistoryProbe.Check(workspace,window,output,sourceOracle);return;}
        using var oracle=JsonDocument.Parse(File.ReadAllText(sourceOracle));if(oracle.RootElement.GetProperty("scope").GetString()!="parent-resize-only6")throw new InvalidOperationException("Exact new source parent resize oracle required");
        if(Environment.GetCommandLineArgs().Contains("--parent-resize-headerallocation-only"))
        {
            await Seed(workspace,window,new("capabilities-ko","capabilities",false));
            var header=new WebCapabilityHeader{Opacity=0};workspace.Children.Add(header);var layouts=new List<object>();
            try{foreach(var width in new[]{80.4296875,80.421875,80.4375,80.4140625,80.4453125})
            {
                header.ColumnWidths=[244.4296875,116.21875,36+width,329.921875];header.Measure(new(header.ColumnWidths.Sum(),double.PositiveInfinity));header.Arrange(new Rect(0,0,header.ColumnWidths.Sum(),header.DesiredSize.Height));
                var label=header.Labels[2];layouts.Add(new{width,columns=header.ColumnWidths,headerHeight=header.Bounds.Height,lines=label.SystemLineTexts.ToArray(),advances=label.SystemLineWidths.ToArray(),labelHeight=label.Bounds.Height,dpr=window.RenderScaling,headless=ReplicaPlatform.IsHeadless});
            }}finally{workspace.Children.Remove(header);}
            File.WriteAllText(Path.Combine(output,"pageintegrationparentresize-headerallocation.json"),JsonSerializer.Serialize(new{scope="parent-resize-headerallocation5",rows=layouts},new JsonSerializerOptions{WriteIndented=true}));return;
        }
        if(Environment.GetCommandLineArgs().Contains("--parent-resize-headerprecision-only"))
        {
            await Seed(workspace,window,new("capabilities-ko","capabilities",false));
            var text=WebTypography.CreateCandidate("검사 가능 범위",12,19.2,Avalonia.Media.Brush.Parse("#b5bac1"),600);
            text.Opacity=0;workspace.Children.Add(text);
            var layouts=new List<object>();
            try{foreach(var width in new[]{80.4296875,80.421875,80.4375,80.4140625,80.4453125})
            {
                text.Measure(new(width,double.PositiveInfinity));text.Arrange(new Rect(0,0,text.DesiredSize.Width,text.DesiredSize.Height));
                var stock=text.Child as Avalonia.Controls.TextBlock;
                layouts.Add(new{width,text=text.Text,fontFamily=text.FontFamily.Name,fontSize=text.FontSize,fontWeight=(int)text.FontWeight,lineHeight=text.LineHeight,wrapping=text.TextWrapping.ToString(),desired=new{width=text.DesiredSize.Width,height=text.DesiredSize.Height},lines=text.SystemLineTexts.ToArray(),advances=text.SystemLineWidths.ToArray(),stockType=stock?.GetType().Name,stockLines=stock?.TextLayout.TextLines.Select(l=>new{l.Width,l.WidthIncludingTrailingWhitespace,l.Length,l.Start}).ToArray(),dpr=window.RenderScaling});
            }}finally{workspace.Children.Remove(text);}
            File.WriteAllText(Path.Combine(output,"pageintegrationparentresize-headerprecision.json"),JsonSerializer.Serialize(new{scope="parent-resize-headerprecision5",rows=layouts},new JsonSerializerOptions{WriteIndented=true}));return;
        }
        var records=new List<object>();
        foreach(var context in Contexts())
        {
            await Seed(workspace,window,context);var page=workspace.CurrentPage!;var focus=Target(workspace);var heading=Heading(workspace).FocusTarget;var rows=new List<object>();
            foreach(var width in LayoutDiagnostic?new double[]{1280,1100}:Widths)
            {
                window.Width=width;await Flush(window);rows.Add(Capture(workspace,window,page,focus,heading));
                if(LayoutDiagnostic&&width==1100){await Task.Delay(100);window.UpdateLayout();File.WriteAllText(Path.Combine(output,"pageintegrationparentresize-late-layout-diagnostic.json"),JsonSerializer.Serialize(Capture(workspace,window,page,focus,heading),new JsonSerializerOptions{WriteIndented=true}));}
            }
            records.Add(new{id=context.Id,rows});File.WriteAllText(Path.Combine(output,"pageintegrationparentresize-records.json"),JsonSerializer.Serialize(new{dpr=window.RenderScaling,rows=records},new JsonSerializerOptions{WriteIndented=true}));
        }
    }
    internal static async Task PrepareNative(ReplicaWorkspace workspace,Window window,string output,string sourceOracle)
    {
        if(Environment.GetCommandLineArgs().Contains("--parent-resize-history-only")){await PageIntegrationViewportHistoryProbe.PrepareNative(workspace,window,output,sourceOracle);return;}
        using var oracle=JsonDocument.Parse(File.ReadAllText(sourceOracle));if(oracle.RootElement.GetProperty("scope").GetString()!="parent-resize-only6")throw new InvalidOperationException("Exact source resize oracle required");
        var contexts=Contexts().ToArray();var index=-1;var snapshot=0;var ready=false;Control? page=null,focus=null,heading=null;
        void Record()
        {
            if(!ready||page is null||focus is null||heading is null)return;window.UpdateLayout();var payload=new{index,snapshot=++snapshot,id=contexts[index].Id,ready,state=Capture(workspace,window,page,focus,heading)};var path=Path.Combine(output,"pageintegrationparentresize-actual-state.json");File.WriteAllText(path+".tmp",JsonSerializer.Serialize(payload,new JsonSerializerOptions{WriteIndented=true}));File.Move(path+".tmp",path,true);
        }
        async Task Next(){if(index>=contexts.Length-1)return;ready=false;await Seed(workspace,window,contexts[++index]);page=workspace.CurrentPage!;focus=Target(workspace);heading=Heading(workspace).FocusTarget;ready=true;Record();}
        window.KeyDown+=async(_,e)=>{if(e.Key==Key.F8){e.Handled=true;await Next();}};
        var timer=new DispatcherTimer{Interval=TimeSpan.FromMilliseconds(100)};timer.Tick+=(_,_)=>Record();window.Closed+=(_,_)=>timer.Stop();await Next();timer.Start();
    }
}
