using System.Reflection;
using System.Text.Json;
using Avalonia;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Threading;
using Verifier.Desktop;
internal static class DbmsOptionInputProbe
{
    internal static void Run(string output)
    {
        Directory.CreateDirectory(output);var canvas=new Canvas{Background=Brush.Parse("#313338"),UseLayoutRounding=false};var engines=new[]{("SQL Server","Microsoft · 1433"),("PostgreSQL","PostgreSQL · 5432"),("MySQL","MySQL · 3306"),("MariaDB","MariaDB · 3306")};var cards=new List<WebDbmsOption>();
        foreach(var((name,desc),i)in engines.Select((e,i)=>(e,i))){var n=new WebDbmsOption{Width=240,EngineName=name,Description=desc,GroupName="dbms-input",IsSelected=i==0};AutomationProperties.SetAutomationId(n.Radio,"DbmsRadio"+i);Canvas.SetLeft(n,30);Canvas.SetTop(n,30+i*110);canvas.Children.Add(n);cards.Add(n);}
        var window=new Window{Title="Replica DbmsOption input verification",Width=560,Height=540,Content=canvas};((IClassicDesktopStyleApplicationLifetime)Application.Current!.ApplicationLifetime!).MainWindow=window;var changes=0;var events=new List<object>();
        object State()=>new{changes,selected=cards.FindIndex(n=>n.IsSelected),focused=cards.FindIndex(n=>n.Radio.IsFocused),rows=cards.Select(n=>new{n.EngineName,n.IsSelected,n.Radio.FocusVisible,n.Radio.IsTabStop,n.Radio.IsPressed,n.IsPointerOver,scale=n.RenderTransform is ScaleTransform s?s.ScaleX:1})};
        void Record(string reason){File.WriteAllText(Path.Combine(output,"dbmsoption-input.json"),JsonSerializer.Serialize(new{reason,state=State(),events},new JsonSerializerOptions{WriteIndented=true}));}
        void Later(string reason){var timer=new DispatcherTimer{Interval=TimeSpan.FromMilliseconds(220)};timer.Tick+=(_,_)=>{timer.Stop();Record(reason);};timer.Start();}
        foreach(var n in cards){n.SelectionChanged+=(_,_)=>{changes++;events.Add(new{kind="change",target=cards.IndexOf(n),focused=cards.FindIndex(c=>c.Radio.IsFocused),selected=cards.FindIndex(c=>c.IsSelected)});Record("change");Later("settled");};n.Radio.GotFocus+=(_,_)=>{events.Add(new{kind="focus",target=cards.IndexOf(n)});Later("focus");};n.AddHandler(InputElement.PointerPressedEvent,(_,_)=>{events.Add(new{kind="pointer-down",target=cards.IndexOf(n),scale=n.RenderTransform is ScaleTransform s?s.ScaleX:1});Later("pointer");},RoutingStrategies.Tunnel,true);}
        window.AddHandler(InputElement.KeyUpEvent,(_,e)=>Later("key-"+e.Key),RoutingStrategies.Tunnel,true);window.Opened+=(_,_)=>Later("opened");window.Closed+=(_,_)=>Record("closed");
    }
}
internal sealed class DbmsOptionInputProbeApplication : Application
{
    public override void Initialize(){Styles.Add(new Avalonia.Themes.Fluent.FluentTheme());}
    public override void OnFrameworkInitializationCompleted(){var a=((IClassicDesktopStyleApplicationLifetime)ApplicationLifetime!).Args!;DbmsOptionInputProbe.Run(a[1]);base.OnFrameworkInitializationCompleted();}
}
