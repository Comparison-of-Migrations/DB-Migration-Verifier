using Avalonia;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.VisualTree;

namespace Verifier.Desktop;

/// <summary>Empty decorative inline pulse span; the parent owns its text baseline.</summary>
public sealed class WebRunningDot : Control
{
    private static readonly IBrush Color=Brush.Parse("#5865f2");
    private WebMotionPlayback? _playback;
    private bool _reduced;
    public WebRunningDot()
    {
        Width=Height=7;Margin=new(0,0,7,0);Focusable=false;IsTabStop=false;UseLayoutRounding=false;
        AutomationProperties.SetAccessibilityView(this,AccessibilityView.Raw);
    }
    public bool ReducedMotion
    {
        get=>_reduced;
        set{if(_reduced==value)return;_reduced=value;_playback?.Dispose();_playback=null;Opacity=1;if(this.IsAttachedToVisualTree())Start();}
    }
    private void Start(){_playback?.Dispose();_playback=WebMotion.Play(this,WebMotionKind.RunningDot,_reduced);}
    public override void Render(DrawingContext context){base.Render(context);context.DrawEllipse(Color,null,new(3.5,3.5),3.5,3.5);}
    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e){base.OnAttachedToVisualTree(e);Start();}
    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e){_playback?.Dispose();_playback=null;base.OnDetachedFromVisualTree(e);}
}
