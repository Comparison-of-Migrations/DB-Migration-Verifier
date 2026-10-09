using System.Text.Json;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using Verifier.Desktop;

/// <summary>Readonly isolated plain-text boundary diagnostics; no text/font substitution.</summary>
internal static class PageIntegrationSlashProbe
{
    private sealed record Case(string Id,string Text,double Width);
    internal static async Task Check(Window window,string output,string casesPath)
    {
        var cases=JsonSerializer.Deserialize<Case[]>(File.ReadAllText(casesPath),new JsonSerializerOptions{PropertyNameCaseInsensitive=true})!;
        var panel=new StackPanel{UseLayoutRounding=false,Spacing=4,HorizontalAlignment=Avalonia.Layout.HorizontalAlignment.Left};
        var texts=cases.Select(c=>WebTypography.CreateCandidate(c.Text,14,22.4)).ToArray();
        for(var i=0;i<cases.Length;i++){texts[i].Width=cases[i].Width;texts[i].HorizontalAlignment=Avalonia.Layout.HorizontalAlignment.Left;panel.Children.Add(texts[i]);}
        window.Content=panel;window.Width=1280;window.Height=900;
        await Dispatcher.UIThread.InvokeAsync(()=>{},DispatcherPriority.Loaded);window.UpdateLayout();
        var rows=cases.Select((c,i)=>new{id=c.Id,text=c.Text,width=texts[i].Bounds.Width,height=texts[i].Bounds.Height,
            lines=texts[i].SystemLineTexts,widths=texts[i].SystemLineWidths,font=new{family=texts[i].FontFamily.Name,size=texts[i].FontSize,weight=(int)texts[i].FontWeight,lineHeight=texts[i].LineHeight}}).ToArray();
        File.WriteAllText(Path.Combine(output,"pageintegration-slash-boundaries.json"),JsonSerializer.Serialize(new
        {scope="Original CSS plain-text slash boundaries; existing protected Typography implementation, no actual input claim",dpr=window.RenderScaling,headless=ReplicaPlatform.IsHeadless,rows},new JsonSerializerOptions{WriteIndented=true}));
        using var bitmap=new RenderTargetBitmap(new((int)(texts[0].Bounds.Width*2),(int)Math.Ceiling(texts[0].Bounds.Height*2)),new(192,192));
        bitmap.Render(texts[0]);bitmap.Save(Path.Combine(output,"original-paragraph-candidate.png"),new PngBitmapEncoderOptions());
    }
}
