using System.Text.Json;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using Verifier.Desktop;
internal sealed class SummaryItemProbeApplication:Application
{
 private sealed record Case(string Id,double Width,bool Compact,bool Last,string Label,string[] Lines,bool Strong,bool Mono,string Suffix);
 public override void OnFrameworkInitializationCompleted()
 {
  var life=(IClassicDesktopStyleApplicationLifetime)ApplicationLifetime!;var args=life.Args!;var cases=JsonSerializer.Deserialize<Case[]>(File.ReadAllText(args[1]),new JsonSerializerOptions{PropertyNameCaseInsensitive=true})!;var output=args[2];Directory.CreateDirectory(output);
  var n=new WebSummaryItem{Width=602};var canvas=new Canvas{Width=1100,Height=650,Background=Brush.Parse("#313338"),UseLayoutRounding=false};canvas.Children.Add(n);Canvas.SetLeft(n,30);Canvas.SetTop(n,100);var w=new Window{Title="Replica SummaryItem verification",Width=1130,Height=750,Content=canvas,UseLayoutRounding=false};life.MainWindow=w;
  object Box(Control c){var p=c.TranslatePoint(default,n)??default;return new{x=p.X,y=p.Y,width=c.Bounds.Width,height=c.Bounds.Height};}
  w.Opened+=(_,_)=>Dispatcher.UIThread.Post(async()=>{var rows=new List<object>();foreach(var c in cases){n.Width=c.Width;n.SetPresentation(new(c.Label,c.Lines,c.Strong,c.Mono,c.Suffix),c.Last,c.Compact);await Dispatcher.UIThread.InvokeAsync(()=>{},DispatcherPriority.Background);w.UpdateLayout();rows.Add(new{id=c.Id,width=n.Bounds.Width,height=n.Bounds.Height,label=Box(n.LabelText),value=Box(n.ValueControl),labelText=n.LabelText.Text,valueText=c.Mono?n.InlineValue!.TextControl.Text:string.Concat(n.ValueLines.Select(x=>x.Text)),borderBottom=(c.Last?0:1)+"px",labelColor="rgb(181, 186, 193)",valueColor="rgb(219, 222, 225)"});if(c.Id is "wide-ko-1-602-false" or "wide-en-10-602-false"){using var sample=new RenderTargetBitmap(new PixelSize(2200,1300),new Vector(192,192));sample.Render(canvas);sample.Save(Path.Combine(output,"summaryitem-"+c.Id+".png"),new PngBitmapEncoderOptions());}}using var bmp=new RenderTargetBitmap(new PixelSize(2200,1300),new Vector(192,192));bmp.Render(canvas);bmp.Save(Path.Combine(output,"summaryitem.png"),new PngBitmapEncoderOptions());File.WriteAllText(Path.Combine(output,"summaryitem-native.json"),JsonSerializer.Serialize(new{dpr=w.RenderScaling,rows},new JsonSerializerOptions{WriteIndented=true}));w.Close();Console.WriteLine("Recorded "+rows.Count+" summary item cases");},DispatcherPriority.Background);
  base.OnFrameworkInitializationCompleted();
 }
}
