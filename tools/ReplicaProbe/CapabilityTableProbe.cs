using System.Text.Json;
using Avalonia;
using Avalonia.Automation.Peers;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Headless;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Verifier.Desktop;
internal sealed class CapabilityTableProbeApplication:Application
{
    private sealed record Case(string Id,bool English,double Width);
    public override void OnFrameworkInitializationCompleted()
    {
        var life=(IClassicDesktopStyleApplicationLifetime)ApplicationLifetime!;var args=life.Args!;var cases=JsonSerializer.Deserialize<Case[]>(File.ReadAllText(args[1]),new JsonSerializerOptions{PropertyNameCaseInsensitive=true})!;var output=args[2];Directory.CreateDirectory(output);
        var table=new WebCapabilityTable();Avalonia.Automation.AutomationProperties.SetAutomationId(table.TableControl,"CapabilityTable-root");var canvas=new Canvas{Width=1200,Height=900,HorizontalAlignment=Avalonia.Layout.HorizontalAlignment.Left,VerticalAlignment=Avalonia.Layout.VerticalAlignment.Top,Background=Brush.Parse("#2b2d31"),UseLayoutRounding=false};canvas.Children.Add(table);Canvas.SetLeft(table,30);Canvas.SetTop(table,40);
        var window=new Window{Title="Replica CapabilityTable verification",Width=1250,Height=950,Content=canvas,UseLayoutRounding=false};life.MainWindow=window;var permissions=0;table.PermissionsRequested+=(_,_)=>permissions++;
        void Set(Case c){table.SetEnglish(c.English);table.Width=c.Width+2;window.UpdateLayout();}
        object Box(Control c){var p=c.TranslatePoint(default,table)??throw new InvalidOperationException("Detached table geometry");return new{x=p.X,y=p.Y,width=c.Bounds.Width,height=c.Bounds.Height};}
        object State(string id)=>new{id,shell=Box(table),table=new{x=1-table.Viewport.Offset,y=1,width=table.ColumnWidths.Sum(),height=table.Header.Bounds.Height+table.Rows.Sum(r=>r.Bounds.Height)},headers=table.Header.Cells.Select(Box).ToArray(),rows=table.Rows.Select(r=>new{box=Box(r),cells=r.Cells.Select(Box).ToArray()}).ToArray(),columns=table.ColumnWidths,minColumns=table.MinimumColumns,maxColumns=table.MaximumColumns,clientWidth=Math.Round(table.Viewport.ClientWidth,MidpointRounding.AwayFromZero),clientHeight=Math.Round(table.Viewport.ClientHeight,MidpointRounding.AwayFromZero),scrollWidth=table.Viewport.ScrollWidth,scrollHeight=Math.Round(table.Viewport.ClientHeight,MidpointRounding.AwayFromZero),scrollLeft=table.Viewport.Offset,gutter=table.Viewport.GutterHeight,permissions,english=table.English,attached=table.Parent is not null,permissionsButton=Box(table.Rows.Single(r=>r.Key=="access").PermissionsButton),buttonFocused=table.Rows.Single(r=>r.Key=="access").PermissionsButton.IsFocused,viewportFocused=table.Viewport.IsFocused};
        if(args[0]=="--capabilitytable-window")
        {
            var trace=new List<object>();void Trace(string phase,PointerEventArgs e){var v=e.Source as Control;var point=e.GetPosition(window);var focused=window.FocusManager?.GetFocusedElement() as Control;trace.Add(new{phase,x=point.X,y=point.Y,handled=e.Handled,sourceType=v?.GetType().Name,sourceId=v is null?null:Avalonia.Automation.AutomationProperties.GetAutomationId(v),ancestors=v?.GetVisualAncestors().OfType<Control>().Select(c=>c.GetType().Name).ToArray(),focusedType=focused?.GetType().Name,focusedId=focused is null?null:Avalonia.Automation.AutomationProperties.GetAutomationId(focused)});File.WriteAllText(Path.Combine(output,"capabilitytable-routed-input.json"),JsonSerializer.Serialize(trace));}
            window.AddHandler(InputElement.PointerPressedEvent,(_,e)=>Trace("down-tunnel",e),Avalonia.Interactivity.RoutingStrategies.Tunnel,true);window.AddHandler(InputElement.PointerPressedEvent,(_,e)=>Trace("down-bubble",e),Avalonia.Interactivity.RoutingStrategies.Bubble,true);window.AddHandler(InputElement.PointerReleasedEvent,(_,e)=>Trace("up-bubble",e),Avalonia.Interactivity.RoutingStrategies.Bubble,true);
            var index=0;var timer=new DispatcherTimer{Interval=TimeSpan.FromMilliseconds(32)};void Record()=>File.WriteAllText(Path.Combine(output,"capabilitytable-input.json"),JsonSerializer.Serialize(State(cases[index].Id)));
            void Utility(string name,double left,Action a){var b=new Button{Content=name,Focusable=false,IsTabStop=false,MinHeight=44};Avalonia.Automation.AutomationProperties.SetAutomationId(b,"CapabilityTable-"+name);Canvas.SetLeft(b,left);b.Click+=(_,_)=>{a();Record();};canvas.Children.Add(b);}
            Utility("next",30,()=>{index=(index+1)%cases.Length;Set(cases[index]);});Utility("mount",160,()=>{if(canvas.Children.Contains(table))canvas.Children.Remove(table);else canvas.Children.Add(table);window.UpdateLayout();});Utility("record",280,Record);Set(cases[0]);timer.Tick+=(_,_)=>Record();timer.Start();window.Closed+=(_,_)=>timer.Stop();
        }
        else Dispatcher.UIThread.Post(async()=>
        {
            if(args.Contains("--group-semantic-only"))
            {
                var records=new List<object>();
                foreach(var c in new[]{new Case("ko",false,904),new Case("en",true,904)})
                {
                    Set(c);await Flush();var peer=ControlAutomationPeer.CreatePeerForElement(table.TableControl);var groups=peer.GetChildren()??throw new InvalidOperationException("Table groups missing");
                    var expected=!OperatingSystem.IsWindows();
                    if(groups.Count!=2||groups.Any(p=>p.IsControlElement()!=expected||p.IsContentElement()!=expected)||groups[0].GetChildren()?.Count!=1||groups[1].GetChildren()?.Count!=4)throw new InvalidOperationException("Table group filtering mismatch");
                    records.Add(new{language=c.Id,table=peer.GetAutomationControlType().ToString(),groups=groups.Select(p=>new{type=p.GetAutomationControlType().ToString(),control=p.IsControlElement(),content=p.IsContentElement(),bounds=p.GetBoundingRectangle(),rows=(p.GetChildren()??[]).Select(x=>new{type=x.GetAutomationControlType().ToString(),name=x.GetName(),control=x.IsControlElement(),content=x.IsContentElement(),bounds=x.GetBoundingRectangle(),cells=x.GetChildren()?.Count}).ToArray()}).ToArray()});
                }
                File.WriteAllText(Path.Combine(output,"capabilitytable-group-semantic-headless.json"),JsonSerializer.Serialize(records));window.Close();return;
            }
            if(args.Contains("--passive-only")){Set(new("passive",false,904));await Flush();var access=table.Rows.Single(r=>r.Key=="access");access.PermissionsButton.Focus(NavigationMethod.Tab);await Flush();var before=access.PermissionsButton.IsFocused;var point=table.TranslatePoint(new Point(61,71),window)??throw new InvalidOperationException("Passive point missing");window.MouseMove(point);await Task.Delay(200);window.MouseDown(point,MouseButton.Left);window.MouseUp(point,MouseButton.Left);await Flush();var focused=access.PermissionsButton.IsFocused;var hover=table.Rows[0].RowBackground?.ToString();if(!before||focused||permissions!=0||hover!="#ff3a3c44")throw new InvalidOperationException("Neutral row full hit rectangle missing "+hover);File.WriteAllText(Path.Combine(output,"capabilitytable-passive-headless.json"),JsonSerializer.Serialize(new{before,focused,permissions,hover,point}));window.Close();return;}
            if(args.Contains("--ax-only")){Set(new("ax-before",false,904));await Flush();object Peer(AutomationPeer p,int depth){var c=(p as ControlAutomationPeer)?.Owner;return new{type=p.GetAutomationControlType().ToString(),id=p.GetAutomationId(),name=p.GetName(),className=p.GetClassName(),bounds=p.GetBoundingRectangle(),ownerBounds=c?.Bounds,topLevel=c is not null&&TopLevel.GetTopLevel(c) is not null,transform=c?.TransformToVisual(window)?.ToString(),parent=p.GetParent()?.GetClassName(),children=depth<10?(p.GetChildren()??[]).Select(x=>Peer(x,depth+1)).ToArray():[]};}File.WriteAllText(Path.Combine(output,"capabilitytable-ax-diagnostic.json"),JsonSerializer.Serialize(Peer(ControlAutomationPeer.CreatePeerForElement(table.TableControl),0)));window.Close();return;}
            if(args.Contains("--cell-intrinsic-only")){await Flush();CapabilityTableIntrinsicDiagnostic.Run(table,window,output);window.Close();return;}
            if(args.Contains("--intrinsic-only")){await Flush();var metrics=new List<object>();foreach(var c in new[]{new Case("ko",false,904),new Case("en",true,904)}){Set(c);foreach(var row in table.Rows)foreach(var item in new[]{("title",row.TitleText),("description",row.DescriptionText),("detail",row.DetailText)}){var text=WebTypography.CreateCandidate(item.Item2.Text??"",item.Item2.FontSize,item.Item2.LineHeight,weight:(int)item.Item2.FontWeight);text.Measure(Size.Infinity);var max=text.SystemLineWidths.ToArray();text.Measure(new Size(1/128d,double.PositiveInfinity));metrics.Add(new{language=c.Id,row=row.Key,role=item.Item1,text=text.Text,max,min=text.SystemLineWidths.ToArray(),tokens=text.SystemLineTexts.ToArray(),standalone=text.SystemLineTexts.Select(token=>{var word=WebTypography.CreateCandidate(token,text.FontSize,text.LineHeight,weight:(int)text.FontWeight);word.TextWrapping=TextWrapping.NoWrap;word.Measure(Size.Infinity);return word.SystemLineWidths.DefaultIfEmpty(0).Max();}).ToArray()});}}File.WriteAllText(Path.Combine(output,"capabilitytable-intrinsic-raw.json"),JsonSerializer.Serialize(metrics));window.Close();return;}
            var samples=new List<object>();foreach(var c in cases){Set(c);await Flush();samples.Add(State(c.Id));if(c.Id is "ko-478"or"en-478"or"ko-904"){using Bitmap image=ReplicaPlatform.IsHeadless?window.CaptureRenderedFrame()??throw new InvalidOperationException("Real table frame absent"):new RenderTargetBitmap(new PixelSize(2500,1900),new Vector(192,192));if(!ReplicaPlatform.IsHeadless)((RenderTargetBitmap)image).Render(window);image.Save(Path.Combine(output,"capabilitytable-"+c.Id+".png"),new PngBitmapEncoderOptions());}}
            File.WriteAllText(Path.Combine(output,"capabilitytable-native.json"),JsonSerializer.Serialize(new{dpr=window.RenderScaling,tables=samples}));
            if(ReplicaPlatform.IsHeadless&&!args.Contains("--geometry-only"))
            {
                Set(new("parent-input",false,904));await Flush();var access=table.Rows.Single(r=>r.Key=="access");var button=access.PermissionsButton;var point=button.TranslatePoint(new Point(button.Bounds.Width/2,button.Bounds.Height/2),window)??throw new InvalidOperationException("Permissions point missing");window.MouseDown(point,MouseButton.Left);window.MouseUp(point,MouseButton.Left);await Flush();if(permissions!=1)throw new InvalidOperationException("Table permission callback missing");button.Focus(NavigationMethod.Tab);window.KeyPress(Key.Enter,RawInputModifiers.None,PhysicalKey.None,null);window.KeyRelease(Key.Enter,RawInputModifiers.None,PhysicalKey.None,null);await Flush();if(permissions!=2)throw new InvalidOperationException("Table keyboard callback missing");
                canvas.Children.Remove(table);window.UpdateLayout();canvas.Children.Add(table);Set(new("parent-remount",true,478));await Flush();if(table.Viewport.Offset!=0||!table.English)throw new InvalidOperationException("Table remount presentation mismatch");
                var peer=ControlAutomationPeer.CreatePeerForElement(table.TableControl)??throw new InvalidOperationException("Table peer missing");var groups=peer.GetChildren()??throw new InvalidOperationException("Table groups missing");if(peer.GetAutomationControlType()!=AutomationControlType.Table||groups.Count!=2||groups[0].GetChildren()?.Count!=1||groups[1].GetChildren()?.Count!=4)throw new InvalidOperationException("Table hierarchy mismatch");
                File.WriteAllText(Path.Combine(output,"capabilitytable-headless-input.json"),JsonSerializer.Serialize(new{permissions,remountOffset=table.Viewport.Offset,english=table.English,groups=groups.Count,headerRows=groups[0].GetChildren()?.Count,bodyRows=groups[1].GetChildren()?.Count}));
            }
            window.Close();Console.WriteLine("Recorded "+samples.Count+" shared-column tables");
        },DispatcherPriority.Background);
        base.OnFrameworkInitializationCompleted();
    }
    private static async Task Flush(){for(var i=0;i<4;i++){Dispatcher.UIThread.RunJobs();await Task.Delay(16);}}
}
