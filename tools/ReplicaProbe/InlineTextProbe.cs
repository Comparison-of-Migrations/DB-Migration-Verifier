using System.Text.Json;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using Verifier.Desktop;
internal sealed class InlineTextProbeApplication:Application
{
 private sealed record Case(string Id,string Prefix,string Suffix,double Width);
 public override void OnFrameworkInitializationCompleted()
 {
  var life=(IClassicDesktopStyleApplicationLifetime)ApplicationLifetime!;var args=life.Args!;var cases=JsonSerializer.Deserialize<Case[]>(File.ReadAllText(args[1]),new JsonSerializerOptions{PropertyNameCaseInsensitive=true})!;var output=args[2];Directory.CreateDirectory(output);
  var n=new WebInlineText{Width=412};var canvas=new Canvas{Width=1100,Height=650,Background=Brush.Parse("#313338"),UseLayoutRounding=false};canvas.Children.Add(n);Canvas.SetLeft(n,30);Canvas.SetTop(n,100);var w=new Window{Title="Replica InlineText verification",Width=1130,Height=750,Content=canvas,UseLayoutRounding=false};life.MainWindow=w;
  w.Opened+=(_,_)=>Dispatcher.UIThread.Post(async()=>{var rows=new List<object>();foreach(var c in cases){n.Width=c.Width;n.SetPresentation(c.Prefix,c.Suffix);await Dispatcher.UIThread.InvokeAsync(()=>{},DispatcherPriority.Background);w.UpdateLayout();rows.Add(new{id=c.Id,width=n.Bounds.Width,height=n.Bounds.Height,text=n.TextControl.Text,regions=n.MutedRegions.Select(q=>new{x=q.X,y=q.Y}).ToArray(),color="rgb(219, 222, 225)",suffixColor="rgb(181, 186, 193)",lines=n.TextControl.SystemLineTexts.ToArray(),lineWidths=n.TextControl.SystemLineWidths.ToArray()});if(c.Width==412){using var sample=new RenderTargetBitmap(new PixelSize(2200,1300),new Vector(192,192));sample.Render(canvas);sample.Save(Path.Combine(output,"inlinetext-"+c.Id+".png"),new PngBitmapEncoderOptions());}}using var bmp=new RenderTargetBitmap(new PixelSize(2200,1300),new Vector(192,192));bmp.Render(canvas);bmp.Save(Path.Combine(output,"inlinetext.png"),new PngBitmapEncoderOptions());File.WriteAllText(Path.Combine(output,"inlinetext-native.json"),JsonSerializer.Serialize(new{dpr=w.RenderScaling,rows},new JsonSerializerOptions{WriteIndented=true}));w.Close();Console.WriteLine("Recorded "+rows.Count+" inline text cases");},DispatcherPriority.Background);
  base.OnFrameworkInitializationCompleted();
 }
}
