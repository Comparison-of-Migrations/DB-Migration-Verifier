using System.Text.Json;
using Avalonia;
using Avalonia.Automation;
using Avalonia.Automation.Peers;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Media;
using Avalonia.Threading;
using Verifier.Desktop;
internal sealed class HeadingSemanticsProbeApplication:Application
{
    public override void OnFrameworkInitializationCompleted()
    {
        var life=(IClassicDesktopStyleApplicationLifetime)ApplicationLifetime!;var args=life.Args!;var output=args[2];Directory.CreateDirectory(output);var canvas=new Canvas{Background=Brush.Parse("#313338"),UseLayoutRounding=false};
        var definitions=new[]{("plain","Ordinary sample text",0,14d),("paper","Research behind this app",2,13d),("step","Connect two databases",3,14d),("chart","Comparison by category",2,16d)};
        var texts=definitions.Select(d=>{var t=WebTypography.CreateCandidate(d.Item2,d.Item4,d.Item4*1.6,weight:d.Item3==0?400:600);t.Width=440;AutomationProperties.SetAutomationId(t,"HeadingSemantics-"+d.Item1);AutomationProperties.SetHeadingLevel(t,d.Item3);return t;}).ToArray();for(var i=0;i<texts.Length;i++){canvas.Children.Add(texts[i]);Canvas.SetLeft(texts[i],30);Canvas.SetTop(texts[i],30+i*70);}
        var window=new Window{Title="Replica Heading semantics verification",Width=600,Height=400,Content=canvas,UseLayoutRounding=false};life.MainWindow=window;
        window.Opened+=(_,_)=>Dispatcher.UIThread.Post(()=>{window.UpdateLayout();var rows=texts.Select((t,i)=>{var peer=ControlAutomationPeer.CreatePeerForElement(t)!;return new{id=definitions[i].Item1,text=t.Text,level=AutomationProperties.GetHeadingLevel(t),type=peer.GetAutomationControlType().ToString(),name=peer.GetName(),children=peer.GetChildren()?.Count??0,x=t.Bounds.X,y=t.Bounds.Y,width=t.Bounds.Width,height=t.Bounds.Height};}).ToArray();File.WriteAllText(Path.Combine(output,"headingsemantics-native.json"),JsonSerializer.Serialize(new{dpr=window.RenderScaling,rows},new JsonSerializerOptions{WriteIndented=true}));if(args[0]!="--headingsemantics-window")window.Close();},DispatcherPriority.Background);base.OnFrameworkInitializationCompleted();
    }
}
