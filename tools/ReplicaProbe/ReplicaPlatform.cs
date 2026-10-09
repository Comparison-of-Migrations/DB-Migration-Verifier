using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using System.Globalization;

internal static class ReplicaPlatform
{
    public static bool IsHeadless=>Environment.GetEnvironmentVariable("DBMV_HEADLESS")=="1";
    public static AppBuilder Configure<T>() where T:Application,new()
    {
        var builder=AppBuilder.Configure<T>();
        if(!IsHeadless)return builder.UsePlatformDetect();
        var scale=double.Parse(Environment.GetEnvironmentVariable("DBMV_HEADLESS_SCALE")??"2",CultureInfo.InvariantCulture);
        if(!double.IsFinite(scale)||scale<=0)throw new ArgumentOutOfRangeException("DBMV_HEADLESS_SCALE");
        return builder.UseSkia().UseHeadless(new AvaloniaHeadlessPlatformOptions{UseHeadlessDrawing=false,ShouldRenderOnUIThread=true})
            .AfterSetup(builderContext=>Window.IsVisibleProperty.Changed.AddClassHandler<Window>((window,eventArgs)=>{if(window.IsVisible)window.SetRenderScaling(scale);}));
    }
}
