using System.Text.Json;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;

using Verifier.Desktop;

// Verification harness only: labels and keys do not introduce product UI or parent components.
internal sealed class MotionProbeApplication : Application
{
    public override void OnFrameworkInitializationCompleted()
    {
        var lifetime=(IClassicDesktopStyleApplicationLifetime)ApplicationLifetime!;
        var output=lifetime.Args![1];
        Directory.CreateDirectory(output);
        var panel=new StackPanel {Spacing=14,Margin=new Thickness(24)};
        var status=new TextBlock {Text="R: replay · C: cancel · M: reduced motion",Foreground=Brushes.White,Focusable=true};
        Avalonia.Automation.AutomationProperties.SetName(status,"Replica verification keyboard target");
        panel.Children.Add(status);
        var shapes=new List<(WebMotionKind Kind,Border Shape)>();
        foreach(var kind in Enum.GetValues<WebMotionKind>())
        {
            var row=new Grid {ColumnDefinitions=new("180,*,130"),Height=48};
            row.Children.Add(new TextBlock {Text=kind.ToString(),Foreground=Brushes.White,VerticalAlignment=VerticalAlignment.Center});
            var shape=new Border {Width=100,Height=32,Background=Brush.Parse("#5865f2"),HorizontalAlignment=HorizontalAlignment.Left,VerticalAlignment=VerticalAlignment.Center,
                RenderTransformOrigin=WebMotion.OriginFor(kind)};
            Grid.SetColumn(shape,1);row.Children.Add(shape);
            var time=new TextBlock {Text=$"{WebMotion.Create(kind)!.Duration.TotalMilliseconds}ms",Foreground=Brushes.LightGray,VerticalAlignment=VerticalAlignment.Center};
            Grid.SetColumn(time,2);row.Children.Add(time);panel.Children.Add(row);shapes.Add((kind,shape));
        }
        var window=new Window {Title="Replica Motion verification",Width=650,Height=780,CanResize=false,Background=Brush.Parse("#313338"),Content=panel};
        CancellationTokenSource? cancellation=null;
        var generation=0;
        var playbacks=new List<(WebMotionKind Kind,WebMotionPlayback Playback)>();
        void DisposePlayback() {foreach(var (_,playback) in playbacks)playback.Dispose();playbacks.Clear();}
        async void Replay(bool reduced)
        {
            var current=++generation;
            cancellation?.Cancel();DisposePlayback();cancellation?.Dispose();cancellation=new();
            var token=cancellation.Token;
            foreach(var (_,shape) in shapes){shape.Styles.Clear();shape.RenderTransform=null;shape.Opacity=1;}
            status.Text=reduced?"Reduced motion: all 11 effects skipped":"Replaying 11 source effects";
            var tasks=new List<Task>();
            foreach(var (kind,shape) in shapes)
            {
                var playback=WebMotion.Play(shape,kind,reduced,token);
                playbacks.Add((kind,playback));
                if(kind is not (WebMotionKind.RunningDot or WebMotionKind.Spinner))tasks.Add(playback.Completion);
            }
            try
            {
                await Task.WhenAll(tasks);
                if(current!=generation)return;
                status.Text=reduced?"Reduced motion: all 11 effects skipped":"9 finite effects completed · 2 infinite effects running";
                window.Title="Replica Motion verification · "+(reduced?"reduced":"completed");
                Record(reduced?"reduced":"completed");
            }
            catch(Exception error)
            {
                if(current!=generation)return;
                status.Text="Animation error: "+error.GetType().Name;
                Record("error");
            }
        }
        void Record(string state)
        {
            File.WriteAllText(Path.Combine(output,$"motion-window-{state}.json"),JsonSerializer.Serialize(new {state,engine="public-render-playback",status=status.Text,scale=window.RenderScaling,completion=playbacks.Select(x=>new {kind=x.Kind.ToString(),state=x.Playback.Completion.Status.ToString()}),shapes=shapes.Select(x=>new {kind=x.Kind.ToString(),opacity=x.Shape.Opacity,matrix=x.Shape.RenderTransform?.Value??Matrix.Identity,origin=x.Shape.RenderTransformOrigin.ToString()})},new JsonSerializerOptions {WriteIndented=true}));
        }
        window.KeyDown+=(_,e)=>
        {
            if(e.Key==Key.R)Replay(false);
            else if(e.Key==Key.M)Replay(true);
            else if(e.Key==Key.C)
            {
                generation++;cancellation?.Cancel();
                foreach(var (kind,playback) in playbacks)if(kind is WebMotionKind.RunningDot or WebMotionKind.Spinner)playback.Dispose();
                status.Text="Effects cancelled · infinite effects disposed";window.Title="Replica Motion verification · cancelled";Record("cancelled");
            }
        };
        window.Opened+=(_,_)=>{WebSurfaceColorSpace.Apply(window);Replay(false);};
        window.Closed+=(_,_)=>{cancellation?.Cancel();DisposePlayback();cancellation?.Dispose();};
        lifetime.MainWindow=window;
        base.OnFrameworkInitializationCompleted();
    }
}
