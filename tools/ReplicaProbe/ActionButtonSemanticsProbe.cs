using System.Text.Json;
using Avalonia;
using Avalonia.Automation;
using Avalonia.Automation.Peers;
using Avalonia.Automation.Provider;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Media;
using Avalonia.Threading;
using Verifier.Desktop;
internal sealed class ActionButtonSemanticsProbeApplication:Application
{
 public override void OnFrameworkInitializationCompleted()
 {
  var life=(IClassicDesktopStyleApplicationLifetime)ApplicationLifetime!;var args=life.Args!;var output=args[2];Directory.CreateDirectory(output);
  var canvas=new Canvas{Background=Brush.Parse("#313338"),UseLayoutRounding=false};
  var buttons=new List<WebActionButton>();
  foreach(var en in new[]{false,true})foreach(var kind in new[]{"base","disabled","loading","selected"}) {
   var b=new WebActionButton{Content=en?"Required permissions":"필요한 권한 보기",Variant=kind=="loading"?"primary":"base",IsEnabled=kind!="disabled",IsLoading=kind=="loading",Filter=kind=="selected",Selected=kind=="selected",ReducedMotion=true};
   AutomationProperties.SetAutomationId(b,"ButtonSemantics-"+(en?"en":"ko")+"-"+kind);Canvas.SetLeft(b,en?350:30);Canvas.SetTop(b,30+buttons.Count%4*70);canvas.Children.Add(b);buttons.Add(b);
  }
  var window=new Window{Title="Replica Action button semantics verification",Width=700,Height=370,Content=canvas,UseLayoutRounding=false};life.MainWindow=window;
  object[] Read()=>buttons.Select(b=>{var peer=ControlAutomationPeer.CreatePeerForElement(b)!;return (object)new{id=AutomationProperties.GetAutomationId(b),name=peer.GetName(),type=peer.GetAutomationControlType().ToString(),children=peer.GetChildren()?.Count??0,enabled=peer.IsEnabled(),invoke=peer.GetProvider<IInvokeProvider>() is not null};}).ToArray();
  window.Opened+=(_,_)=>Dispatcher.UIThread.Post(()=>{
   window.UpdateLayout();var initial=Read();canvas.Children.Clear();foreach(var b in buttons)canvas.Children.Add(b);window.UpdateLayout();var remount=Read();
   File.WriteAllText(Path.Combine(output,"actionbuttonsemantic-native.json"),JsonSerializer.Serialize(new{dpr=window.RenderScaling,initial,remount},new JsonSerializerOptions{WriteIndented=true}));if(args[0]!="--actionbuttonsemantic-window")window.Close();
  },DispatcherPriority.Background);base.OnFrameworkInitializationCompleted();
 }
}
