using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Media;
using Avalonia.Threading;
using Avalonia.VisualTree;
using SkiaSharp;
using System.Runtime.InteropServices;

namespace Verifier.Desktop;

// TextPresenter owns editing, shaping and IME. This overlay paints only the
// source browser's font-fragment caret, rather than Avalonia's full linebox bar.
internal sealed class WebInputCaret(WebTextInput owner,TextPresenter presenter):Control
{
    private readonly DispatcherTimer _timer=new();
    private bool _visible;
    private readonly System.Diagnostics.Stopwatch _clock=new();
    private (double Size,double Scale,string Family)? _key;
    private double _height,_top;
    public Rect CaretRect {get;private set;}
    internal void Refresh(bool reset=true)
    {
        if(!this.IsAttachedToVisualTree())return;
        var show=owner.IsFocused && owner.IsEffectivelyEnabled && (owner.SelectionStart==owner.SelectionEnd || !string.IsNullOrEmpty(presenter.PreeditText));
        if(!show){_timer.Stop();_visible=false;}
        else if(reset){_visible=true;_clock.Restart();_timer.Stop();if(owner.CaretBlinkInterval.TotalMilliseconds>0){_timer.Interval=owner.CaretBlinkInterval;_timer.Start();}}
        InvalidateVisual();
    }
    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);IsHitTestVisible=false;UseLayoutRounding=false;
        _timer.Tick+=Tick;presenter.CaretBoundsChanged+=CaretMoved;Refresh();
    }
    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {_timer.Stop();_timer.Tick-=Tick;presenter.CaretBoundsChanged-=CaretMoved;base.OnDetachedFromVisualTree(e);}
    private void Tick(object? sender,EventArgs e)
    {
        _visible=!_visible;
        // Blink TimerBase retains the original deadline and skips missed ticks,
        // rather than adding UI scheduler lateness to each subsequent period.
        var period=owner.CaretBlinkInterval.TotalMilliseconds;
        var now=_clock.Elapsed.TotalMilliseconds;
        if(period>0)_timer.Interval=TimeSpan.FromMilliseconds(Math.Ceiling((now+period/20)/period)*period-now);
        InvalidateVisual();
    }
    private void CaretMoved(object? sender,EventArgs e)=>Refresh();
    private void Metrics(double scale)
    {
        var key=(owner.FontSize,scale,owner.FontFamily.Name);if(_key==key)return;
        double ascent,descent;
        if(OperatingSystem.IsMacOS())
        {
            var font=CTFontCreateUIFontForLanguage(2,owner.FontSize,IntPtr.Zero);
            if(font==IntPtr.Zero)throw new InvalidOperationException("Cannot read the editor font metrics.");
            try{ascent=CTFontGetAscent(font);descent=CTFontGetDescent(font);}finally{CFRelease(font);}
        }
        else
        {
            using var face=SKTypeface.FromFamilyName(owner.FontFamily.Name);using var font=new SKFont(face,(float)owner.FontSize);
            ascent=-font.Metrics.Ascent;descent=font.Metrics.Descent;
        }
        ascent=Math.Floor(ascent*scale+.5)/scale;descent=Math.Floor(descent*scale+.5)/scale;
        _height=ascent+descent;_top=Math.Floor((owner.LineHeight-_height)*scale/2)/scale;_key=key;
    }
    public override void Render(DrawingContext context)
    {
        var scale=TopLevel.GetTopLevel(this)?.RenderScaling??1;Metrics(scale);
        var index=presenter.CaretIndex+(string.IsNullOrEmpty(presenter.PreeditText)?0:presenter.PreeditTextCursorPosition??presenter.PreeditText!.Length);
        var caret=presenter.TextLayout.HitTestTextPosition(index);
        var viewport=presenter.GetVisualAncestors().OfType<ScrollViewer>().FirstOrDefault()?.Viewport.Width??Bounds.Width;
        var limit=Math.Max(viewport,presenter.TextLayout.WidthIncludingTrailingWhitespace)-1;
        var x=Math.Round(Math.Clamp(caret.X-.5,0,Math.Max(0,limit)),MidpointRounding.AwayFromZero);
        var point=presenter.TranslatePoint(new Point(x,_top),this)??default;
        // Blink rounds the local inline position after subtracting half the bar.
        // Snap only its block origin to the display grid; keep scroll displacement.
        var origin=this.TranslatePoint(default,TopLevel.GetTopLevel(this)!)??default;
        CaretRect=new Rect(point.X,Math.Floor((point.Y+origin.Y)*scale+.5)/scale-origin.Y,1,_height);
        if(_visible && owner.CaretBrush is { } brush)context.FillRectangle(brush,CaretRect);
    }
    private const string Ct="/System/Library/Frameworks/CoreText.framework/CoreText";
    private const string Cf="/System/Library/Frameworks/CoreFoundation.framework/CoreFoundation";
    [DllImport(Ct)]private static extern IntPtr CTFontCreateUIFontForLanguage(int type,double size,IntPtr language);
    [DllImport(Ct)]private static extern double CTFontGetAscent(IntPtr font);
    [DllImport(Ct)]private static extern double CTFontGetDescent(IntPtr font);
    [DllImport(Cf)]private static extern void CFRelease(IntPtr font);
}
