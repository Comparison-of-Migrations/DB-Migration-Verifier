using System.Reflection;
using System.Text.Json;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Media;
using Verifier.Desktop;
internal sealed class DbmsOptionLifecycleProbeApplication : Application
{
    public override void Initialize(){Styles.Add(new Avalonia.Themes.Fluent.FluentTheme());}
    public override void OnFrameworkInitializationCompleted()
    {
        var a=((IClassicDesktopStyleApplicationLifetime)ApplicationLifetime!).Args!;Directory.CreateDirectory(a[1]);var n=new WebDbmsOption{Width=240,EngineName="SQL Server",Description="Microsoft · 1433",GroupName="lifecycle"};var canvas=new Canvas{Background=Brush.Parse("#313338")};Canvas.SetLeft(n,30);Canvas.SetTop(n,30);canvas.Children.Add(n);var w=new Window{Title="Replica DbmsOption lifecycle verification",Width=400,Height=240,Content=canvas};((IClassicDesktopStyleApplicationLifetime)ApplicationLifetime!).MainWindow=w;
        const BindingFlags f=BindingFlags.NonPublic|BindingFlags.Instance;var rows=new List<object>();var changes=0;n.SelectionChanged+=(_,_)=>changes++;
        object Read(string stage){var surface=(WebSurfaceBox)typeof(WebDbmsOption).GetField("_surface",f)!.GetValue(n)!;var track=typeof(WebDbmsOption).GetField("_background",f)!.GetValue(n)!;return new{stage,selected=n.IsSelected,reduced=n.ReducedMotion,running=(bool)track.GetType().GetProperty("Running")!.GetValue(track)!,bg=((ISolidColorBrush)surface.Background!).Color.ToString(),changes};}
        w.Opened+=async(_,_)=>{w.UpdateLayout();rows.Add(Read("attached"));n.IsSelected=true;rows.Add(Read("select-start"));n.ReducedMotion=true;rows.Add(Read("reduced"));n.ReducedMotion=false;n.IsSelected=false;rows.Add(Read("reverse-start"));canvas.Children.Remove(n);rows.Add(Read("detached"));await Task.Delay(220);rows.Add(Read("detached-later"));canvas.Children.Add(n);w.UpdateLayout();rows.Add(Read("remounted"));n.Radio.SetCurrentValue(WebRadio.IsCheckedProperty,true);rows.Add(Read("radio-true"));n.Radio.SetCurrentValue(WebRadio.IsCheckedProperty,false);rows.Add(Read("radio-false"));File.WriteAllText(Path.Combine(a[1],"dbmsoption-lifecycle.json"),JsonSerializer.Serialize(new{rows},new JsonSerializerOptions{WriteIndented=true}));w.Close();Console.WriteLine("Recorded DBMS lifecycle and change semantics.");};base.OnFrameworkInitializationCompleted();
    }
}
