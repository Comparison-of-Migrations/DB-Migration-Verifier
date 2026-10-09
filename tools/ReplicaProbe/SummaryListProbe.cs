using System.Text.Json;
using System.Globalization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using Verifier.Desktop;
internal sealed class SummaryListProbeApplication:Application
{
 private sealed record Item(string Label,string[] Lines,bool Strong,bool Mono,string Suffix);
 private sealed record Case(string Id,double Width,bool Compact,bool Panel,Item[] Items);
 public override void OnFrameworkInitializationCompleted()
 {
  var life=(IClassicDesktopStyleApplicationLifetime)ApplicationLifetime!;var args=life.Args!;var cases=JsonSerializer.Deserialize<Case[]>(File.ReadAllText(args[1]),new JsonSerializerOptions{PropertyNameCaseInsensitive=true})!;var output=args[2];Directory.CreateDirectory(output);
  var n=new WebSummaryList{Width=656};var canvas=new Canvas{Width=1100,Height=650,Background=Brush.Parse("#313338"),UseLayoutRounding=false};canvas.Children.Add(n);Canvas.SetLeft(n,30);Canvas.SetTop(n,100);var w=new Window{Title="Replica SummaryList verification",Width=1130,Height=750,Content=canvas,UseLayoutRounding=false};life.MainWindow=w;
  object Box(Control c){var p=c.TranslatePoint(default,n)??default;return new{x=p.X,y=p.Y,width=c.Bounds.Width,height=c.Bounds.Height};}
  string Rgb(IBrush? b){if(b is not ISolidColorBrush brush)return "rgba(0, 0, 0, 0)";var c=brush.Color;return $"rgb({c.R}, {c.G}, {c.B})";}
  string Shadow(){if(n.Surface.BoxShadow.Count==0)return "none";var b=n.Surface.BoxShadow[0];return $"rgba({b.Color.R}, {b.Color.G}, {b.Color.B}, {Math.Round(b.Color.A/255d,3).ToString(CultureInfo.InvariantCulture)}) {b.OffsetX}px {b.OffsetY}px {b.Blur}px {b.Spread}px";}
  w.Opened+=(_,_)=>Dispatcher.UIThread.Post(async()=>{var rows=new List<object>();foreach(var c in cases){n.Width=c.Width;n.SetPresentation(c.Items.Select(x=>new WebSummaryValue(x.Label,x.Lines,x.Strong,x.Mono,x.Suffix)),c.Compact,c.Panel);await Dispatcher.UIThread.InvokeAsync(()=>{},DispatcherPriority.Background);w.UpdateLayout();rows.Add(new{id=c.Id,width=n.Bounds.Width,height=n.Bounds.Height,items=n.Rows.Select(x=>new{row=Box(x),label=Box(x.LabelText),value=Box(x.ValueControl),labelText=x.LabelText.Text,valueText=x.InlineValue is{}inline?inline.TextControl.Text:string.Concat(x.ValueLines.Select(v=>v.Text))}).ToArray(),background=Rgb(n.Surface.Background),padding=n.Surface.Padding.Top+"px",borderWidth=n.Surface.BorderThickness.Top+"px",radius=n.Surface.CornerRadius.TopLeft+"px",shadow=Shadow()});if(c.Id is "compact-ko-0-656" or "wide-en-3-656"){using var sample=new RenderTargetBitmap(new PixelSize(2200,1300),new Vector(192,192));sample.Render(canvas);sample.Save(Path.Combine(output,"summarylist-"+c.Id+".png"),new PngBitmapEncoderOptions());}}using var bmp=new RenderTargetBitmap(new PixelSize(2200,1300),new Vector(192,192));bmp.Render(canvas);bmp.Save(Path.Combine(output,"summarylist.png"),new PngBitmapEncoderOptions());File.WriteAllText(Path.Combine(output,"summarylist-native.json"),JsonSerializer.Serialize(new{dpr=w.RenderScaling,rows},new JsonSerializerOptions{WriteIndented=true}));w.Close();Console.WriteLine("Recorded "+rows.Count+" summary list cases");},DispatcherPriority.Background);
  base.OnFrameworkInitializationCompleted();
 }
}
