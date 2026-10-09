using System.Text.Json;
using Avalonia;
using Avalonia.Automation;
using Avalonia.Automation.Peers;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Media;
using Avalonia.Threading;
using Verifier.Desktop;
internal sealed class PageHeadingSemanticsProbeApplication:Application
{
    public override void OnFrameworkInitializationCompleted()
    {
        var life=(IClassicDesktopStyleApplicationLifetime)ApplicationLifetime!;var args=life.Args!;var output=args[2];Directory.CreateDirectory(output);
        var heading=new WebPageHeading{Width=656};heading.SetPresentation("비교할 두 DB를 연결하세요","같은 DBMS의 원본과 대상을 지정합니다. 접속 정보는 결과 파일에 넣지 않습니다.");
        var canvas=new Canvas{Background=Brush.Parse("#313338"),UseLayoutRounding=false};canvas.Children.Add(heading);Canvas.SetLeft(heading,30);Canvas.SetTop(heading,30);
        var window=new Window{Title="Replica Page heading semantics verification",Width=760,Height=320,Content=canvas,UseLayoutRounding=false};life.MainWindow=window;
        window.Opened+=(_,_)=>Dispatcher.UIThread.Post(()=>{window.UpdateLayout();var peer=ControlAutomationPeer.CreatePeerForElement(heading.FocusTarget)!;File.WriteAllText(Path.Combine(output,"pageheadingsemantic-native.json"),JsonSerializer.Serialize(new{dpr=window.RenderScaling,name=peer.GetName(),type=peer.GetAutomationControlType().ToString(),level=AutomationProperties.GetHeadingLevel(heading.FocusTarget),children=peer.GetChildren()?.Count??0,focusable=heading.FocusTarget.Focusable,tabstop=heading.FocusTarget.IsTabStop},new JsonSerializerOptions{WriteIndented=true}));if(args[0]!="--pageheadingsemantic-window")window.Close();},DispatcherPriority.Background);
        base.OnFrameworkInitializationCompleted();
    }
}
