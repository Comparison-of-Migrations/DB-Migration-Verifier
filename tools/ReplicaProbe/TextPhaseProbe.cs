using System.Text.Json;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Verifier.Desktop;

internal static class TextPhaseProbe
{
    public static void Run(string input,string output)
    {
        var cases=JsonSerializer.Deserialize<Case[]>(File.ReadAllText(input),new JsonSerializerOptions{PropertyNameCaseInsensitive=true})!;
        var root=new Canvas {Width=960,Height=490,Background=Brush.Parse("#313338"),UseLayoutRounding=false};
        foreach(var (c,i) in cases.Select((c,i)=>(c,i)))
        {
            var text=WebTypography.CreateCandidate(c.Text,c.Size,c.LineHeight,Brush.Parse("#f2f3f5"),c.Weight,0,c.Mono);
            text.Width=300;
            Canvas.SetLeft(text,i%3*320+10+c.Offset);Canvas.SetTop(text,i/3*70+10+c.Offset);root.Children.Add(text);
        }
        root.Measure(new(960,490));root.Arrange(new(0,0,960,490));
        var measurements=root.Children.Cast<WebSystemTextBlock>().Select((x,i)=>new {id=cases[i].Id,x=x.Bounds.X,y=x.Bounds.Y,width=x.Bounds.Width,height=x.Bounds.Height,baseline=x.SystemBaseline,lines=x.SystemLineWidths});
        File.WriteAllText(Path.Combine(output,"layout.json"),JsonSerializer.Serialize(measurements,new JsonSerializerOptions{WriteIndented=true}));
        using var bitmap=new RenderTargetBitmap(new PixelSize(1920,980),new Vector(192,192));bitmap.Render(root);
        using var stream=File.Create(Path.Combine(output,"native.png"));bitmap.Save(stream,new PngBitmapEncoderOptions());
    }
    private sealed record Case(string Id,string Text,double Size,double LineHeight,int Weight,bool Mono,double Offset);
}
