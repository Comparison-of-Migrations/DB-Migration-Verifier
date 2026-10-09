using System.Text.Json;
using Avalonia;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using Verifier.Desktop;

internal sealed class ActionFooterProbeApplication:Application
{
    public override void OnFrameworkInitializationCompleted()
    {
        var lifetime=(IClassicDesktopStyleApplicationLifetime)ApplicationLifetime!;var args=lifetime.Args!;
        var cases=JsonSerializer.Deserialize<FooterCase[]>(File.ReadAllText(args[1]),new JsonSerializerOptions{PropertyNameCaseInsensitive=true})!;
        var output=args[2];Directory.CreateDirectory(output);
        var foot=new WebActionFooter{Width=cases[0].Viewport-220};var canvas=new Canvas{Width=1200,Height=300,UseLayoutRounding=false,Background=Brush.Parse("#313338")};
        canvas.Children.Add(foot);Canvas.SetLeft(foot,30);Canvas.SetTop(foot,60);
        var w=new Window{Title="Replica ActionFooter verification",Width=1240,Height=440,Content=canvas,UseLayoutRounding=false};lifetime.MainWindow=w;
        object Box(Control c){var p=c.TranslatePoint(default,foot)??default;return new{x=p.X,y=p.Y,width=c.Bounds.Width,height=c.Bounds.Height};}
        if(args[0]=="--actionfooter-window")
        {
            string lang="ko",name="connect";double viewport=1000;var requests=new List<object>();
            void Record()
            {
                var file=Path.Combine(output,"actionfooter-input.json");
                var buttons=(foot.BackButton is null?Array.Empty<WebActionButton>():new[]{foot.BackButton}).Concat(foot.ActionButtons);
                File.WriteAllText(file+".next",JsonSerializer.Serialize(new{capturedAtUtc=DateTime.UtcNow,dpr=w.RenderScaling,id=lang+"-"+name,viewport,requests,
                    width=foot.Bounds.Width,height=foot.Bounds.Height,hint=foot.Hint.Text,
                    buttons=buttons.Select(b=>new{action=AutomationProperties.GetAutomationId(b)?.Replace("Footer-",""),label=b.Content as string,enabled=b.IsEnabled,focused=b.IsFocused}).ToArray()},new JsonSerializerOptions{WriteIndented=true}));
                File.Move(file+".next",file,true);
            }
            void Apply()
            {
                var c=cases.Single(x=>x.Id==lang+"-"+name&&x.Viewport==viewport);
                foot.Width=viewport-220;foot.SetViewportWidth(viewport);foot.SetContent(c.Hint,c.Back,c.Actions);w.UpdateLayout();Record();
            }
            foot.ActionRequested+=(_,action)=>{requests.Add(new{action,time=DateTime.UtcNow});Record();};
            AutomationProperties.SetAutomationId(foot,"FooterCandidate");AutomationProperties.SetName(foot,"Candidate footer");
            Canvas.SetTop(foot,100);
            var entries=new[]{("FooterCase-connect","Connect"),("FooterCase-connect-busy","Busy"),("FooterCase-select-none","No checks"),("FooterCase-trial-ready","Trial ready"),("FooterLocale","KO / EN"),("FooterRecord","Record state"),("FooterClose","Close fixture")};
            foreach(var (entry,index) in entries.Select((item,index)=>(item,index)))
            {
                var button=new Button{Content=entry.Item2,Width=120,Height=44,Focusable=entry.Item1!="FooterRecord",IsTabStop=entry.Item1!="FooterRecord"};
                AutomationProperties.SetAutomationId(button,entry.Item1);canvas.Children.Add(button);Canvas.SetLeft(button,30+index*130);Canvas.SetTop(button,10);
                button.Click+=(_,_)=>{if(entry.Item1=="FooterClose"){w.Close();return;}if(entry.Item1=="FooterRecord"){Record();return;}
                    if(entry.Item1=="FooterLocale")lang=lang=="ko"?"en":"ko";else name=entry.Item1.Replace("FooterCase-","");
                    requests.Clear();Apply();};
            }
            w.Opened+=(_,_)=>Apply();base.OnFrameworkInitializationCompleted();return;
        }
        w.Opened+=(_,_)=>Dispatcher.UIThread.Post(()=>
        {
            var rows=new List<object>();
            foreach(var c in cases)
            {
                foot.Width=c.Viewport-220;foot.SetViewportWidth(c.Viewport);foot.SetContent(c.Hint,c.Back,c.Actions);w.UpdateLayout();
                var buttons=(foot.BackButton is null?Array.Empty<WebActionButton>():new[]{foot.BackButton}).Concat(foot.ActionButtons).ToArray();
                rows.Add(new{id=c.Id,viewport=c.Viewport,width=foot.Bounds.Width,height=foot.Bounds.Height,paddingX=c.Viewport<=1180?28:40,
                    hint=new{box=Box(foot.Hint),text=foot.Hint.Text},actions=foot.ActionsBox is null?null:Box(foot.ActionsBox),
                    buttons=buttons.Select(b=>new{box=Box(b),action=AutomationProperties.GetAutomationId(b)?.Replace("Footer-",""),enabled=b.IsEnabled,label=b.Content as string,name=AutomationProperties.GetName(b),variant=b.Variant,iconName=b.IconName,loading=b.IsLoading}).ToArray()});
                if(c.Viewport==1000&&c.Id is "ko-connect" or "en-select-none" or "en-trial-ready")
                {using var image=new RenderTargetBitmap(new PixelSize(2400,600),new Vector(192,192));image.Render(canvas);image.Save(Path.Combine(output,"actionfooter-"+c.Id+".png"),new PngBitmapEncoderOptions());}
            }
            File.WriteAllText(Path.Combine(output,"actionfooter-native.json"),JsonSerializer.Serialize(new{scale=w.RenderScaling,rows},new JsonSerializerOptions{WriteIndented=true}));
            w.Close();Console.WriteLine($"Recorded {rows.Count} attached footer layout/update cases.");
        },DispatcherPriority.Background);
        base.OnFrameworkInitializationCompleted();
    }
    internal sealed record FooterCase(string Id,double Viewport,string Hint,WebFooterAction? Back,WebFooterAction[] Actions);
}
