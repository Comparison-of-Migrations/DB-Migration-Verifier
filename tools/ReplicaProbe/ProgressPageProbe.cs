using System.Text.Json;
using Avalonia;
using Avalonia.Automation.Peers;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Verifier.Desktop;
internal sealed class ProgressPageProbeApplication:Application
{
    private sealed record Case(string Id,bool English,double Width,double ViewportWidth,bool LiveTick,ReplicaSampleRun Run);
    public override void OnFrameworkInitializationCompleted()
    {
        var life=(IClassicDesktopStyleApplicationLifetime)ApplicationLifetime!;var args=life.Args!;var options=new JsonSerializerOptions{PropertyNameCaseInsensitive=true,WriteIndented=true};var cases=JsonSerializer.Deserialize<Case[]>(File.ReadAllText(args[1]),options)!;var output=args[2];Directory.CreateDirectory(output);
        var host=new Canvas{Background=Brush.Parse("#313338"),UseLayoutRounding=false};var page=new WebProgressPage{Width=980,ReducedMotion=true};host.Children.Add(page);Canvas.SetLeft(page,30);Canvas.SetTop(page,20);var w=new Window{Width=1500,Height=1000,Content=host,UseLayoutRounding=false,Title="Replica ProgressPage verification"};life.MainWindow=w;
        object Box(Control c){var p=c.TranslatePoint(default,page)??default;return new{x=p.X,y=p.Y,width=c.Bounds.Width,height=c.Bounds.Height};}
        object Item(Control c,string? text=null)=>new{box=Box(c),text=text??(c as WebSystemTextBlock)?.Text};
        object Read(Case c)=>new{id=c.Id,page=Box(page),heading=Item(page.Heading),title=Item(page.Heading.TitleText),panel=Item(page.ProgressPanel),main=Item(page.Main),number=Item(page.Number),small=page.Number.Small.Where(t=>t.IsVisible).Select(t=>Item(t)).ToArray(),note=Item(page.Note,page.Note.Text),idLabel=Item(page.Identifier),meter=Item(page.Meter),tableShell=Item(page.TableShell),table=Item(page.Table),columns=page.Table.Columns,minColumns=page.Table.MinimumColumns,maxColumns=page.Table.MaximumColumns,header=page.Table.Header.Cells.Select(t=>Item(t)).ToArray(),rows=page.Table.Rows.Select(r=>new{box=Box(r),cells=r.Cells.Select(t=>Item(t)).ToArray(),pill=Item(r.Pill,r.Pill.Text)}).ToArray(),notice=page.InterruptedNotice is null?null:Item(page.InterruptedNotice),dot=page.Note.Dot.IsVisible?Item(page.Note.Dot):null,clientWidth=page.Viewport.ClientWidth,scrollWidth=page.Table.LogicalWidth,run=page.Run};
        w.Opened+=(_,_)=>Dispatcher.UIThread.Post(async()=>
        {
            if(args.Contains("--hover-diagnostic-only")){await ProgressPageHeadlessInput.HoverDiagnostic(w,page,cases.First(c=>c.Id=="ko-980-pending").Run,output);w.Close();return;}
            if(args.Contains("--hover-only")){await ProgressPageHeadlessInput.Run(w,host,page,cases.First(c=>c.Id=="ko-980-pending").Run,output,true);w.Close();return;}
            if(args.Contains("--interaction-only")){await ProgressPageHeadlessInput.Run(w,host,page,cases.First(c=>c.Id=="ko-980-pending").Run,output);w.Close();return;}
            if(args[0]=="--progresspage-window"&&args.Contains("--native-input-only")){ProgressPageNativeScope.Attach(w,host,page,cases.First(c=>c.Id=="ko-980-pending").Run,output);return;}
            var peerOnly=args.Contains("--peer-only");var selected=peerOnly?cases.Where(c=>c.Id is "ko-980-pending" or "en-980-pending" or "ko-326-large-trial" or "en-326-large-trial").ToArray():cases;
            var rows=new List<object>();foreach(var c in args.Contains("--lifecycle-only")?Array.Empty<Case>():selected){page.Width=c.Width;page.SetPresentation(c.Run,c.English,c.LiveTick);w.UpdateLayout();await Task.Delay(15);w.UpdateLayout();rows.Add(Read(c));if(c.Id is "ko-980-mid" or "en-326-large-trial" or "en-684-interrupted"){using var image=new RenderTargetBitmap(new(3000,2000),new(192,192));image.Render(host);image.Save(Path.Combine(output,c.Id+".png"),new PngBitmapEncoderOptions());}}
            if(peerOnly)
            {
                File.WriteAllText(Path.Combine(output,"progresspage-native.json"),JsonSerializer.Serialize(new{dpr=w.RenderScaling,rows},options));
                var peers=new List<object>();foreach(var english in new[]{false,true}){page.SetPresentation(cases.First(c=>c.Id=="ko-980-pending").Run,english);w.UpdateLayout();var table=ControlAutomationPeer.CreatePeerForElement(page.Table)!;peers.Add(new{english,groups=table.GetChildren().Select(g=>new{name=g.GetName(),type=g.GetAutomationControlType().ToString(),control=g.IsControlElement(),content=g.IsContentElement(),rows=g.GetChildren().Select(row=>new{type=row.GetAutomationControlType().ToString(),cells=row.GetChildren().Select(cell=>new{type=cell.GetAutomationControlType().ToString(),name=cell.GetName(),children=cell.GetChildren().Count}).ToArray()}).ToArray()}).ToArray()});}
                File.WriteAllText(Path.Combine(output,"progresspage-peer.json"),JsonSerializer.Serialize(peers,options));
                if(args[0]=="--progresspage-window")ProgressPageNativeScope.Attach(w,host,page,cases.First(c=>c.Id=="ko-980-pending").Run,output);else w.Close();return;
            }
            var lifecycle=new List<object>();
            Case Find(string id)=>cases.First(c=>c.Id==id);
            void Present(string id,string label,bool update=false){var c=Find(id);page.Width=c.Width;if(update)page.Update(c.Run);else page.SetPresentation(c.Run,c.English,c.LiveTick);w.UpdateLayout();lifecycle.Add(new{label,frame=Read(c),attached=page.IsAttachedToVisualTree(),meterDone=page.Meter.Processed,meterTotal=page.Meter.Total,rows=page.Table.Rows.Count});}
            Present("ko-980-pending","initial");Present("ko-980-mid","live-update",true);Present("ko-980-tick-boundary","live-boundary",true);Present("en-684-boundary","language-resize");Present("ko-326-large-trial","large-trial");Present("ko-980-interrupted","interruption");host.Children.Remove(page);w.UpdateLayout();lifecycle.Add(new{label="detached",attached=page.IsAttachedToVisualTree()});host.Children.Add(page);w.UpdateLayout();lifecycle.Add(new{label="remounted",frame=Read(Find("ko-980-interrupted")),attached=page.IsAttachedToVisualTree()});Present("en-980-complete","complete");
            File.WriteAllText(Path.Combine(output,"progresspage-lifecycle.json"),JsonSerializer.Serialize(lifecycle,options));
            File.WriteAllText(Path.Combine(output,"progresspage-native.json"),JsonSerializer.Serialize(new{dpr=w.RenderScaling,rows},options));if(args[0]!="--progresspage-window")w.Close();else ProgressPageNativeScope.Attach(w,host,page,cases.First(c=>c.Id=="ko-980-pending").Run,output);
        },DispatcherPriority.Background);base.OnFrameworkInitializationCompleted();
    }
}
