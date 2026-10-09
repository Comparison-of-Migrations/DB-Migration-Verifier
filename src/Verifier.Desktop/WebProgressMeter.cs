using System.Diagnostics;
using Avalonia;
using Avalonia.Automation;
using Avalonia.Automation.Peers;
using Avalonia.Automation.Provider;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.VisualTree;

namespace Verifier.Desktop;

/// <summary>The source readonly progress meter; its parent owns the processing schedule.</summary>
public sealed class WebProgressMeter : Panel
{
    private readonly WebSurfaceBox _track=WebSurface.Create(Brush.Parse("#202125"),Brushes.Transparent,default,new(8));
    private readonly ScaleTransform _scale=new(0,1);
    private readonly Transition _transition=new();
    private int _revision;
    private bool _reduced;
    private MeterPeer? _peer;
    public WebSurfaceBox Bar{get;}=WebSurface.Create(Brush.Parse("#5865f2"),Brushes.Transparent,default,default);
    public long Processed{get;private set;}
    public long Total{get;private set;}
    public double VisibleFraction=>_transition.Current;
    public bool ReducedMotion{get=>_reduced;set{if(_reduced==value)return;_reduced=value;if(value){_revision++;_transition.Snap(Total==0?0:Processed/(double)Total);Paint();}}}
    public WebProgressMeter()
    {
        UseLayoutRounding=false;Focusable=false;IsTabStop=false;
        Bar.RenderTransform=_scale;Bar.RenderTransformOrigin=new(0,.5,RelativeUnit.Relative);
        AutomationProperties.SetAccessibilityView(_track,AccessibilityView.Raw);AutomationProperties.SetAccessibilityView(Bar,AccessibilityView.Raw);
        Children.Add(_track);Children.Add(Bar);SetPresentation(0,0);
        DetachedFromVisualTree+=(_,_)=>{_revision++;_transition.Snap(Total==0?0:Processed/(double)Total);Paint();};
    }
    public void SetPresentation(long processed,long total,bool english=false,bool animate=true)
    {
        if(processed<0||total<0||processed>total)throw new ArgumentOutOfRangeException(nameof(processed));
        var before=Processed;var oldTotal=Total;Processed=processed;Total=total;
        AutomationProperties.SetName(this,english?"Sample processed checks":"예시 처리 항목");
        _peer?.Changed(before,oldTotal);
        var fraction=total==0?0:processed/(double)total;
        if(!animate||_reduced||!this.IsAttachedToVisualTree()){_revision++;_transition.Snap(fraction);Paint();return;}
        _transition.SetTarget(fraction);Paint();var revision=++_revision;
        void Frame(TimeSpan _){if(revision!=_revision||!this.IsAttachedToVisualTree())return;Paint();if(_transition.Running)TopLevel.GetTopLevel(this)?.RequestAnimationFrame(Frame);}
        TopLevel.GetTopLevel(this)?.RequestAnimationFrame(Frame);
    }
    private void Paint(){_scale.ScaleX=_transition.Current;InvalidateVisual();}
    protected override Size MeasureOverride(Size a){var width=double.IsFinite(a.Width)?a.Width:0;_track.Measure(new(width,6));Bar.Measure(new(width,6));return new(width,6);}
    protected override Size ArrangeOverride(Size s){_track.Arrange(new(0,0,s.Width,6));Bar.Arrange(new(0,0,s.Width,6));Clip=new RectangleGeometry(new(0,0,s.Width,6),Math.Min(3,s.Width/2),3);return new(s.Width,6);}
    protected override AutomationPeer OnCreateAutomationPeer()=>_peer??=new MeterPeer(this);
    private sealed class MeterPeer(WebProgressMeter owner):ControlAutomationPeer(owner),IRangeValueProvider
    {
        public void Changed(long before,long oldTotal){RaisePropertyChangedEvent(RangeValuePatternIdentifiers.ValueProperty,(double)before,(double)owner.Processed);RaisePropertyChangedEvent(RangeValuePatternIdentifiers.MaximumProperty,(double)oldTotal,(double)owner.Total);}
        protected override AutomationControlType GetAutomationControlTypeCore()=>AutomationControlType.ProgressBar;
        protected override IReadOnlyList<AutomationPeer> GetChildrenCore()=>Array.Empty<AutomationPeer>();
        bool IRangeValueProvider.IsReadOnly=>true;
        double IRangeValueProvider.Value=>owner.Processed;
        double IRangeValueProvider.Minimum=>0;
        double IRangeValueProvider.Maximum=>owner.Total;
        double IRangeValueProvider.SmallChange=>double.NaN;
        double IRangeValueProvider.LargeChange=>double.NaN;
        void IRangeValueProvider.SetValue(double value)=>throw new InvalidOperationException("This source progressbar is readonly.");
    }
    private sealed class Transition(Func<double>? clock=null)
    {
        private double _from,_target,_reversing,_duration,_started,_factor=1;
        private double Now=>clock?.Invoke()??Stopwatch.GetTimestamp()*(1000d/Stopwatch.Frequency);
        private double Progress=>_duration<=0?1:Math.Clamp((Now-_started)/_duration,0,1);
        public bool Running=>Progress<1;
        public double Current=>_from+(_target-_from)*Progress;
        public void Snap(double value){_from=_target=_reversing=value;_duration=0;_factor=1;}
        public void SetTarget(double target){if(_target==target)return;var progress=Progress;var current=Current;if(progress<1&&_reversing==target){_factor=Math.Clamp(Math.Abs(progress*_factor+(1-_factor)),0,1);_reversing=_target;}else{_factor=1;_reversing=current;}_from=current;_target=target;_duration=200*_factor;_started=Now;}
    }
}
