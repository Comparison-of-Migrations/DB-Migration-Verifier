using System.Text.Json;
using Avalonia;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using Verifier.Desktop;
internal sealed class ResultChartProbeApplication:Application
{
 public override void Initialize()=>Styles.Add(new Avalonia.Themes.Fluent.FluentTheme());
 public override void OnFrameworkInitializationCompleted()
 {
  var args=((IClassicDesktopStyleApplicationLifetime)ApplicationLifetime!).Args!;var output=args[2];Directory.CreateDirectory(output);
  var cases=JsonSerializer.Deserialize<ChartCase[]>(File.ReadAllText(args[1]),new JsonSerializerOptions{PropertyNameCaseInsensitive=true})!;
  var chart=new WebResultChart{Width=980};var canvas=new Canvas{Width=1160,Height=650,UseLayoutRounding=false,Background=Brush.Parse("#313338")};canvas.Children.Add(chart);Canvas.SetLeft(chart,30);Canvas.SetTop(chart,30);
  var w=new Window{Title="Replica ResultChart verification",Width=1200,Height=760,Content=canvas,UseLayoutRounding=false};((IClassicDesktopStyleApplicationLifetime)ApplicationLifetime!).MainWindow=w;
  object Box(Control c){var p=c.TranslatePoint(default,chart)??default;return new{x=p.X,y=p.Y,width=c.Bounds.Width,height=c.Bounds.Height};}
  w.Opened+=(_,_)=>Dispatcher.UIThread.Post(()=>{var rows=new List<object>();foreach(var c in cases){chart.Width=c.Width;chart.SetGroups(c.Groups,c.Lang=="en",c.Compact);w.UpdateLayout();if(!chart.IsVisible){rows.Add(new{id=c.Id,present=false});continue;}
   var first=chart.UnitLines[0].TranslatePoint(default,chart)!.Value;var unit=new{x=first.X,y=first.Y,width=chart.UnitLines[0].Bounds.Width,height=34};
   rows.Add(new{id=c.Id,present=true,width=chart.Bounds.Width,height=chart.Bounds.Height,header=Box(chart.Header),heading=Box(chart.Heading),help=Box(chart.Help),unit,columns=Box(chart.Columns),labels=chart.ColumnLabels.Select(Box).ToArray(),rows=chart.Rows.Select(r=>new{box=Box(r),name=AutomationProperties.GetName(r),numbers=r.Counts.Select(n=>n.Count).ToArray()}).ToArray(),captionBox=Box(chart.CaptionBox),unitText=string.Join("\n",chart.UnitLines.Select(x=>x.Text)),headingText=chart.Heading.Text,helpText=chart.Help.Text,captionText=chart.Caption.Text,maximum=chart.Maximum});
   if(c.Id is "0" or "4" or "8" or "20"){using var image=new RenderTargetBitmap(new PixelSize(2320,1300),new Vector(192,192));image.Render(canvas);image.Save(Path.Combine(output,"resultchart-"+c.Id+".png"),new PngBitmapEncoderOptions());}
  }File.WriteAllText(Path.Combine(output,"resultchart-native.json"),JsonSerializer.Serialize(new{scale=w.RenderScaling,rows},new JsonSerializerOptions{WriteIndented=true}));w.Close();Console.WriteLine($"Recorded {rows.Count} native chart layout/update cases; entrance/input pending.");},DispatcherPriority.Background);
  base.OnFrameworkInitializationCompleted();
 }
 internal sealed record ChartCase(string Id,string Lang,bool Compact,double Width,WebResultGroup[] Groups);
}
