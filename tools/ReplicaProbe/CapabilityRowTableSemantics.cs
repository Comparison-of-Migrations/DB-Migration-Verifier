using System.Text.Json;
using Avalonia;
using Avalonia.Automation;
using Avalonia.Automation.Peers;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Media;
using Avalonia.Threading;
using Verifier.Desktop;

internal static class CapabilityRowTableSemantics
{
    private sealed record CellSource(string Name,string Type,CellSource[] Children);
    private sealed record RowSource(string Id,string Key,bool English,CellSource[] Cells);
    private sealed record Source(RowSource[] Rows);
    internal static void Start(IClassicDesktopStyleApplicationLifetime life,string[] args)
    {
        var source=JsonSerializer.Deserialize<Source>(File.ReadAllText(args[1]),new JsonSerializerOptions{PropertyNameCaseInsensitive=true})!;
        var output=args[2];Directory.CreateDirectory(output);
        var rows=WebCapabilityRow.Definitions.Select(d=>new WebCapabilityRow{ReducedMotion=true,ColumnWidths=[270,145,165,324]}).ToArray();
        var body=new StackPanel{Width=904,Spacing=0,UseLayoutRounding=false};
        foreach(var row in rows){body.Children.Add(row);AutomationProperties.SetAutomationId(row,"CapabilityRow-semantic-"+WebCapabilityRow.Definitions[Array.IndexOf(rows,row)].Key);}
        var canvas=new Canvas{Width=1050,Height=780,Background=Brush.Parse("#2b2d31"),UseLayoutRounding=false};
        canvas.Children.Add(body);Canvas.SetLeft(body,30);Canvas.SetTop(body,70);
        var w=new Window{Title="Replica CapabilityRow semantic verification",Width=1050,Height=780,Content=canvas,UseLayoutRounding=false};life.MainWindow=w;
        var english=false;
        void Set(bool en){english=en;for(var i=0;i<4;i++)rows[i].SetPresentation(WebCapabilityRow.Definitions[i].Key,en,i==3);w.UpdateLayout();}
        object Peer(AutomationPeer peer,int depth=0)=>new{type=peer.GetAutomationControlType().ToString(),name=peer.GetName(),id=peer.GetAutomationId(),role=peer.GetClassName(),parent=peer.GetParent()?.GetClassName(),bounds=peer.GetBoundingRectangle(),children=depth<6?(peer.GetChildren()??[]).Select(p=>Peer(p,depth+1)).ToArray():[]};
        object Record()=>new{english,attached=canvas.Children.Contains(body),rows=canvas.Children.Contains(body)?rows.Select(row=>new{key=row.Key,peer=Peer(ControlAutomationPeer.CreatePeerForElement(row)!),box=row.Bounds}).ToArray():[]};
        void Save()=>File.WriteAllText(Path.Combine(output,"capabilityrow-semantic-input.json"),JsonSerializer.Serialize(Record(),new JsonSerializerOptions{WriteIndented=true}));
        if(args[0]=="--capabilityrow-window")
        {
            void Utility(string id,Action action,double left){var b=new Button{Content=id};AutomationProperties.SetAutomationId(b,"CapabilityRow-semantic-"+id);b.Click+=(_,_)=>{action();Dispatcher.UIThread.Post(Save,DispatcherPriority.Background);};canvas.Children.Add(b);Canvas.SetLeft(b,left);Canvas.SetTop(b,10);}
            Utility("language",()=>Set(!english),30);Utility("mount",()=>{if(canvas.Children.Contains(body))canvas.Children.Remove(body);else canvas.Children.Add(body);},170);
            w.Opened+=(_,_)=>{Set(args.Contains("--semantic-lifecycle-only"));DispatcherTimer.Run(()=>{Save();return w.IsVisible;},TimeSpan.FromMilliseconds(80));};
        }
        else w.Opened+=(_,_)=>Dispatcher.UIThread.Post(async()=>
        {
            var records=new List<object>();
            async Task Check(bool en,string scope)
            {
                Set(en);await Dispatcher.UIThread.InvokeAsync(()=>{},DispatcherPriority.Background);w.UpdateLayout();
                for(var i=0;i<4;i++)
                {
                    var row=rows[i];var expected=source.Rows.Single(r=>r.English==en&&r.Key==row.Key);
                    var peer=ControlAutomationPeer.CreatePeerForElement(row)!;var cells=peer.GetChildren()!;
                    if(cells.Count!=4)throw new InvalidOperationException("Four cells required");
                    for(var c=0;c<4;c++)
                    {
                        var p=cells[c];var children=p.GetChildren()??[];
                        if(OperatingSystem.IsWindows())
                        {
                            if(p.GetName()!=expected.Cells[c].Name||p.GetAutomationControlType()!=AutomationControlType.DataItem||children.Count!=expected.Cells[c].Children.Length)throw new InvalidOperationException("Source cell semantics mismatch: "+expected.Id+"/"+c+" "+JsonSerializer.Serialize(Peer(p)));
                            if(children.Count==1&&(children[0].GetName()!=expected.Cells[c].Children[0].Name||children[0].GetAutomationControlType()!=AutomationControlType.Button||children[0].GetParent()!=p))throw new InvalidOperationException("Permission must be a direct named child of its Cell");
                        }
                        else if(!string.IsNullOrEmpty(p.GetName())||p.GetAutomationControlType()!=AutomationControlType.Custom||children.Count!=(c==0?2:1))throw new InvalidOperationException("Mac existing Cell contract changed");
                    }
                    records.Add(new{scope,id=expected.Id,peer=Peer(peer),box=row.Bounds});
                }
            }
            await Check(false,"initial");await Check(true,"language");
            canvas.Children.Remove(body);await Dispatcher.UIThread.InvokeAsync(()=>{},DispatcherPriority.Background);
            var detached=rows.All(r=>TopLevel.GetTopLevel(r)==null);if(!detached)throw new InvalidOperationException("Row remained attached");
            canvas.Children.Add(body);await Check(false,"remount");
            File.WriteAllText(Path.Combine(output,"capabilityrow-semantic-native.json"),JsonSerializer.Serialize(new{os=OperatingSystem.IsWindows()?"windows":"mac",dpr=w.RenderScaling,records,detached,buttonChildrenPendingRoot=true},new JsonSerializerOptions{WriteIndented=true}));w.Close();Console.WriteLine("CapabilityRow semantic-only: 32 original cells, 16 remount cells, no geometry/input replay");
        },DispatcherPriority.Background);
    }
}
