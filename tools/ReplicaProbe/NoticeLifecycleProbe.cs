using System.Text.Json;
using System.Reflection;
using Avalonia;
using Avalonia.Automation.Peers;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Media;
using Avalonia.Threading;
using Verifier.Desktop;

internal static class NoticeLifecycleProbe
{
    internal static void Run(string output)
    {
        Directory.CreateDirectory(output);var canvas=new Canvas{Background=Brush.Parse("#313338"),UseLayoutRounding=false};
        var window=new Window{Title="Replica Notice lifecycle verification",Width=760,Height=420,Content=canvas};((IClassicDesktopStyleApplicationLifetime)Application.Current!.ApplicationLifetime!).MainWindow=window;
        const BindingFlags hidden=BindingFlags.NonPublic|BindingFlags.Instance;
        WebMotionPlayback Entry(WebNotice n)=>(WebMotionPlayback)typeof(WebNotice).GetField("_entry",hidden)!.GetValue(n)!;
        object Read(WebNotice n)=>new{opacity=n.Opacity,y=(n.RenderTransform as TranslateTransform)?.Y??0,completion=Entry(n).Completion.Status.ToString(),name=ControlAutomationPeer.CreatePeerForElement(n)!.GetName(),live=ControlAutomationPeer.CreatePeerForElement(n)!.GetLiveSetting().ToString(),role=ControlAutomationPeer.CreatePeerForElement(n)!.GetClassName(),focusable=n.Focusable,tabStop=n.IsTabStop};
        window.Opened+=async(_,_)=>
        {
            var n=new WebNotice{Width=700,Title="보이지 않는 객체를 “없음”으로 처리하지 않습니다",Text="읽지 못한 항목은 검사 후에도 확인 불가로 남습니다.",Kind="warn"};Canvas.SetLeft(n,30);Canvas.SetTop(n,30);canvas.Children.Add(n);window.UpdateLayout();
            var initial=Read(n);var first=Entry(n);n.Kind="error";n.Text="예시 오류입니다.";window.UpdateLayout();var update=Read(n);var samePlayback=ReferenceEquals(first,Entry(n));
            await first.Completion.WaitAsync(TimeSpan.FromSeconds(5));var complete=Read(n);
            canvas.Children.Remove(n);var disposed=first.Completion.Status.ToString();canvas.Children.Add(n);window.UpdateLayout();var remount=Read(n);var second=Entry(n);var replaced=!ReferenceEquals(first,second);
            canvas.Children.Remove(n);var cancelled=second.Completion.Status.ToString();var restored=new{opacity=n.Opacity,y=(n.RenderTransform as TranslateTransform)?.Y??0};
            n.ReducedMotion=true;canvas.Children.Add(n);window.UpdateLayout();var reduced=Read(n);
            File.WriteAllText(Path.Combine(output,"notice-lifecycle.json"),JsonSerializer.Serialize(new{scale=window.RenderScaling,initial,update,samePlayback,complete,disposed,remount,replaced,cancelled,restored,reduced},new JsonSerializerOptions{WriteIndented=true}));
            window.Close();Console.WriteLine("Verified owned Notice actual mount/update/completion/cancel/remount/reduced lifecycle.");
        };
    }
}
internal sealed class NoticeLifecycleProbeApplication:Application
{
    public override void Initialize(){Styles.Add(new Avalonia.Themes.Fluent.FluentTheme());}
    public override void OnFrameworkInitializationCompleted(){var args=((IClassicDesktopStyleApplicationLifetime)ApplicationLifetime!).Args!;NoticeLifecycleProbe.Run(args[1]);base.OnFrameworkInitializationCompleted();}
}
