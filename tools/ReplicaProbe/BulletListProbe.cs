using System.Globalization;
using System.Text.Json;
using Avalonia;
using Avalonia.Automation.Peers;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using Verifier.Desktop;

internal sealed class BulletListProbeApplication:Application
{
    internal sealed record Case(string Id,string Lang,double Width,string[] Items);
    public override void OnFrameworkInitializationCompleted()
    {
        var life=(IClassicDesktopStyleApplicationLifetime)ApplicationLifetime!;var args=life.Args!;var cases=JsonSerializer.Deserialize<Case[]>(File.ReadAllText(args[1]),new JsonSerializerOptions{PropertyNameCaseInsensitive=true})!;var output=args[2];Directory.CreateDirectory(output);
        var n=new WebBulletList();var canvas=new Canvas{Width=760,Height=450,UseLayoutRounding=false,Background=Brush.Parse("#2b2d31")};canvas.Children.Add(n);Canvas.SetLeft(n,30);Canvas.SetTop(n,30);
        var w=new Window{Title="Replica BulletList verification",Width=800,Height=520,Content=canvas,UseLayoutRounding=false};life.MainWindow=w;
        string Px(double value)=>value.ToString("0.###",CultureInfo.InvariantCulture)+"px";
        string Rgb(IBrush brush){var color=((ISolidColorBrush)brush).Color;return $"rgb({color.R}, {color.G}, {color.B})";}
        object Read(Case c)=>new{id=c.Id,x=n.Bounds.X-30,y=n.Bounds.Y-30,width=n.Bounds.Width,height=n.Bounds.Height,color=n.Items.Count==0?"rgb(181, 186, 193)":Rgb(n.Items[0].Text.Foreground!),margin=$"{n.Margin.Top}px {n.Margin.Right}px",padding="0px 0px 0px 20px",items=n.Items.Select(item=>new{text=item.Text.Text,x=item.Bounds.X,y=item.Bounds.Y,width=item.Bounds.Width,height=item.Bounds.Height,markerSize=Px(item.Marker.FontSize),markerLine=Px(item.Marker.FontSize*1.6),markerColor=((ISolidColorBrush)item.Marker.Foreground!).Color is var color?$"rgb({color.R}, {color.G}, {color.B})":"",type=item.Marker.Text=="•"?"disc":"different"}).ToArray()};
        w.Opened+=(_,_)=>Dispatcher.UIThread.Post(async()=>
        {
            var rows=new List<object>();var peerStates=new List<object>();foreach(var c in cases){n.Width=c.Width;n.SetItems(c.Items);await Dispatcher.UIThread.InvokeAsync(()=>{},DispatcherPriority.Background);w.UpdateLayout();rows.Add(Read(c));var peer=ControlAutomationPeer.CreatePeerForElement(n)!;peerStates.Add(new{id=c.Id,type=peer.GetAutomationControlType().ToString(),children=peer.GetChildren()?.Select(x=>x.GetAutomationControlType().ToString()).ToArray()??[]});}
            n.Width=570;n.SetItems(cases.First(c=>c.Id=="ko-570-trial").Items);w.UpdateLayout();using var bmp=new RenderTargetBitmap(new PixelSize(1520,900),new Vector(192,192));bmp.Render(canvas);bmp.Save(Path.Combine(output,"bulletlist.png"),new PngBitmapEncoderOptions());File.WriteAllText(Path.Combine(output,"bulletlist-native.json"),JsonSerializer.Serialize(new{dpr=w.RenderScaling,rows},new JsonSerializerOptions{WriteIndented=true}));File.WriteAllText(Path.Combine(output,"bulletlist-peer.json"),JsonSerializer.Serialize(new{capturedAtUtc=DateTime.UtcNow,rows=peerStates},new JsonSerializerOptions{WriteIndented=true}));w.Close();Console.WriteLine("Recorded "+rows.Count+" BulletList layout/update/peer states");
        },DispatcherPriority.Background);base.OnFrameworkInitializationCompleted();
    }
}
