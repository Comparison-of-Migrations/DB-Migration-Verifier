using Avalonia;
using Avalonia.Controls;
using Avalonia.Automation;
using Avalonia.Styling;
using Avalonia.Input;
using Avalonia.Media;
using System.Diagnostics;
using Avalonia.Rendering.SceneGraph;
using Avalonia.Skia;
using SkiaSharp;
using Avalonia.Threading;
using Avalonia.VisualTree;

namespace Verifier.Desktop;

/// <summary>Original table-shell horizontal overflow, with auto content height and separate painted descendant extent.</summary>
public sealed class WebCapabilityViewport : Panel
{
    private double _logicalWidth;
    private double _overflowWidth;
    public double LogicalWidth { get => _logicalWidth; set { _logicalWidth=Math.Max(0,value);InvalidateMeasure(); } }
    public double OverflowWidth { get => _overflowWidth; set { _overflowWidth=Math.Max(0,value);InvalidateMeasure(); } }
    private readonly ViewportContent _host;
    private readonly ScrollChrome _chrome;
    private double _offset;
    private double _extent;
    private WebCubicBezierEasing _scrollEase = new(.42,0,.58,1);
    private double _scrollFrom, _scrollDuration, _scrollInitialSlope;
    private long _scrollStarted;
    private int _scrollRevision;
    private bool _animating, _dragging, _focusVisible, _pointerScrollFocus;
    private double _scrollTarget, _dragY, _dragOffset;
    private ScrollPart _pressedPart;
    private int _holdRevision;
    private bool _autoScrolling;
    private Point _pointerPosition;
    private enum ScrollPart { None, Thumb, Up, Down, TrackAbove, TrackBelow }

    public WebCapabilityViewport()
    {
        UseLayoutRounding = false;
        ClipToBounds = false;
        Focusable = true;
        if(OperatingSystem.IsWindows())
            Styles.Add(new Style(x=>x.Is<Control>())
            {
                Setters={new Setter(AutomationProperties.IsOffscreenBehaviorProperty,IsOffscreenBehavior.FromClip)}
            });
        _host = new ViewportContent(this) { ClipToBounds = true, UseLayoutRounding = false };
        Children.Add(_host);
        _chrome = new ScrollChrome(this) { UseLayoutRounding = false };
        Children.Add(_chrome);
        LayoutUpdated+=(_,_)=>UpdateFocusEligibility();
        AddHandler(GotFocusEvent,(_,e)=>
        {
            if(e.NavigationMethod!=NavigationMethod.Pointer&&e.Source is Control target&&target!=this)target.BringIntoView();
        });
        AddHandler(RequestBringIntoViewEvent, (_,e) =>
        {
            if(e.TargetObject is not Visual target || target.TranslatePoint(e.TargetRect.Position,this) is not { } point) return;
            var right=point.X+e.TargetRect.Width;
            if(point.X<0||right>ClientWidth)
            {
                if(target is InputElement input&&input.IsFocused)Offset+=point.X+e.TargetRect.Width/2-ClientWidth/2;
                else if(point.X<0)Offset+=point.X;
                else Offset+=right-ClientWidth;
            }
            e.Handled=true;
        });
        DetachedFromVisualTree+=(_,_)=>
        {
            CancelScroll();StopHold();_dragging=false;_pressedPart=ScrollPart.None;
            // Removing/reinserting the live DOM scroll box resets scrollLeft.
            // Reset without DidScroll: a detached box must not restart chrome.
            if(_offset!=0){_offset=0;_host.InvalidateArrange();OffsetChanged?.Invoke(this,EventArgs.Empty);}
        };
    }

    public Control? Content
    {
        get => _host.Children.Count == 0 ? null : _host.Children[0];
        set
        {
            _host.Children.Clear();
            if (value is not null) _host.Children.Add(value);
            InvalidateMeasure();
        }
    }
    public double ClientWidth { get; private set; }
    public double ClientHeight { get; private set; }
    public double ScrollWidth => Math.Round(Math.Max(ClientWidth, _extent), MidpointRounding.AwayFromZero);
    public double MaximumOffset => Math.Max(0,RoundDevice(_extent)-RoundDevice(ClientWidth));
    public double GutterHeight { get; private set; }
    public double Offset
    {
        get => _offset;
        set
        {
            CancelScroll();SetOffset(value);
        }
    }
    private void SetOffset(double value)
    {
            var next = Math.Clamp(RoundDevice(double.IsFinite(value) ? value : 0), 0, MaximumOffset);
            if (next == _offset) return;
            _offset = next;
            _host.InvalidateArrange();
            _chrome.DidScroll();
            OffsetChanged?.Invoke(this, EventArgs.Empty);
    }
    private void CancelScroll(){_scrollRevision++;_animating=false;}
    private double PageStep
    {
        get
        {
            var scale=TopLevel.GetTopLevel(this)?.RenderScaling??1;
            var physical=Math.Floor(ClientWidth*scale);
            var minimum=Math.Truncate(physical*.875);
            return Math.Max(1,OperatingSystem.IsWindows()?minimum:Math.Max(minimum,physical-40))/scale;
        }
    }
    private void ScrollTo(double target, bool smooth, double duration=150,bool inverseDuration=false)
    {
        target=Math.Clamp(target,0,MaximumOffset);
        if(!smooth || TopLevel.GetTopLevel(this) is not { } top){Offset=target;return;}
        if(_animating && _scrollTarget==target)return;
        var from=Offset;var velocity=0d;
        if(_animating)
        {
            var phase=Math.Clamp(Stopwatch.GetElapsedTime(_scrollStarted).TotalMilliseconds/_scrollDuration,0,1);
            from=Math.Clamp(_scrollFrom+(_scrollTarget-_scrollFrom)*_scrollEase.Ease(phase),0,MaximumOffset);
            velocity=(_scrollTarget-_scrollFrom)/_scrollDuration*ScrollVelocityRatio(phase,_scrollInitialSlope);
        }
        if(inverseDuration)duration=Math.Clamp(1000d*14/60-Math.Abs(target-from)*top.RenderScaling/3.6,100,200);
        var bound=velocity==0?double.PositiveInfinity:(target-from)/velocity*2.5;
        if(bound>=0)duration=Math.Min(duration,bound);
        if(duration<10){Offset=target;return;}
        CancelScroll();var revision=_scrollRevision;_scrollTarget=target;
        if(from==target)return;
        _animating=true;_scrollFrom=from;_scrollDuration=duration;_scrollInitialSlope=Math.Clamp(velocity*duration/(target-from),-1000,1000);
        var ease=_scrollEase=new WebCubicBezierEasing(.42,.42*_scrollInitialSlope,.58,1);
        var started=_scrollStarted=Stopwatch.GetTimestamp();
        void Frame(TimeSpan _)
        {
            if(revision!=_scrollRevision||!this.IsAttachedToVisualTree())return;
            var progress=Math.Clamp(Stopwatch.GetElapsedTime(started).TotalMilliseconds/duration,0,1);
            SetOffset(from+(target-from)*ease.Ease(progress));
            if(revision!=_scrollRevision)return;
            if(progress<1)top.RequestAnimationFrame(Frame);else _animating=false;
        }
        top.RequestAnimationFrame(Frame);
    }
    private static double ScrollVelocityRatio(double phase,double slope)
    {
        if(phase>=1)return 0;
        double low=0,high=1;
        for(var i=0;i<40;i++)
        {
            var u=(low+high)/2;var x=3*(1-u)*(1-u)*u*.42+3*(1-u)*u*u*.58+u*u*u;
            if(x<phase)low=u;else high=u;
        }
        var t=(low+high)/2;var dx=3*.42*(1-t)*(1-t)+6*(.58-.42)*(1-t)*t+3*(1-.58)*t*t;
        var dy=3*.42*slope*(1-t)*(1-t)+6*(1-.42*slope)*(1-t)*t;
        return dy/dx;
    }
    public bool IsFocusVisible => _focusVisible;
    protected override void OnGotFocus(FocusChangedEventArgs e)
    {
        base.OnGotFocus(e);
        if(IsFocused)
        {
            // Platform activation restores focus with Unspecified navigation;
            // keep the last input modality instead of inventing a keyboard ring.
            if(e.NavigationMethod!=NavigationMethod.Unspecified)
                _pointerScrollFocus=e.NavigationMethod==NavigationMethod.Pointer;
            _focusVisible=!_pointerScrollFocus;
            _chrome.InvalidateVisual();
        }
    }
    protected override void OnLostFocus(FocusChangedEventArgs e)
    { base.OnLostFocus(e);_focusVisible=false;_chrome.InvalidateVisual(); }
    protected override void OnPointerWheelChanged(PointerWheelEventArgs e)
    {
        base.OnPointerWheelChanged(e);
        var delta=e.Delta.X;
        if(OperatingSystem.IsWindows()&&delta==0&&e.KeyModifiers.HasFlag(KeyModifiers.Shift))delta=e.Delta.Y;
        if(e.Handled||delta==0||MaximumOffset<=0)return;
        var current=_animating?_scrollTarget:Offset;
        var target=current-delta*(OperatingSystem.IsWindows()?100:50);
        if(Math.Clamp(target,0,MaximumOffset)==current)return;
        ScrollTo(target,OperatingSystem.IsWindows(),inverseDuration:true);
        e.Handled=true;
    }
    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);
        if(IsFocused && !_pointerScrollFocus){_focusVisible=true;_chrome.InvalidateVisual();}
        if(e.Handled||MaximumOffset<=0||e.KeyModifiers.HasFlag(KeyModifiers.Control)||e.KeyModifiers.HasFlag(KeyModifiers.Alt)||e.KeyModifiers.HasFlag(KeyModifiers.Meta))return;
        var current=_animating?_scrollTarget:Offset;
        double? target=e.Key switch {Key.Right=>current+40,Key.Left=>current-40,_=>null};
        if(target is null)return;
        ScrollTo(target.Value,true);e.Handled=true;
    }
    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        base.OnPointerPressed(e);
        if(e.Handled||!e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)return;
        var p=AxisPoint(e.GetPosition(this));
        if(_chrome.HitPart(p) is { } part && part!=ScrollPart.None)
        {
            CancelScroll();_pressedPart=part;e.Pointer.Capture(this);e.Handled=true;
            _dragging=part==ScrollPart.Thumb;_dragY=p.Y;_dragOffset=Offset;
            if(!_dragging)
            {
                var delta=part==ScrollPart.Up?-40:part==ScrollPart.Down?40:part==ScrollPart.TrackAbove?-PageStep:PageStep;
                ScrollTo(Offset+delta,true,inverseDuration:true);_pointerPosition=p;StartHold(delta);
            }
            _chrome.InvalidateVisual();
        }
        else if(e.Source is Visual source && !source.GetVisualAncestors().Prepend(source).OfType<InputElement>().TakeWhile(x=>x!=this).Any(x=>x.Focusable)){_pointerScrollFocus=true;_focusVisible=false;if(Focusable)Focus(NavigationMethod.Pointer);else TopLevel.GetTopLevel(this)?.FocusManager?.Focus(null);_chrome.InvalidateVisual();}
    }
    protected override void OnPointerMoved(PointerEventArgs e)
    {
        base.OnPointerMoved(e);
        _pointerPosition=AxisPoint(e.GetPosition(this));
        if(!_dragging)return;
        var travel=_chrome.ThumbTravel;
        if(travel>0)Offset=_dragOffset+(e.GetPosition(this).X-_dragY)*MaximumOffset/travel;
        e.Handled=true;
    }
    protected override void OnPointerReleased(PointerReleasedEventArgs e)
    {
        base.OnPointerReleased(e);
        if(_pressedPart==ScrollPart.None||e.InitialPressMouseButton!=MouseButton.Left)return;
        StopHold();_dragging=false;_pressedPart=ScrollPart.None;e.Pointer.Capture(null);_chrome.InvalidateVisual();e.Handled=true;
    }
    protected override void OnPointerCaptureLost(PointerCaptureLostEventArgs e)
    { StopHold();_dragging=false;_pressedPart=ScrollPart.None;_chrome.InvalidateVisual();base.OnPointerCaptureLost(e); }
    private void StopHold()
    { _holdRevision++;if(_autoScrolling)CancelScroll();_autoScrolling=false; }
    private void StartHold(double delta)
    {
        if(TopLevel.GetTopLevel(this) is not { } top)return;
        var revision=++_holdRevision;var started=Stopwatch.GetTimestamp();
        var baseOffset=_scrollTarget;var repeatedOffset=baseOffset;var last=250d;
        void Frame(TimeSpan _)
        {
            if(revision!=_holdRevision||!this.IsAttachedToVisualTree()||_pressedPart==ScrollPart.None)return;
            var elapsed=Stopwatch.GetElapsedTime(started).TotalMilliseconds;
            if(elapsed>=250)
            {
                // Chromium's compositor repeats continuously: initial250ms,
                // velocity = measured per-line/page delta ×20 (cc/input/scrollbar.h).
                var samePart=_chrome.HitPart(_pointerPosition)==_pressedPart;
                if(!_autoScrolling){CancelScroll();_autoScrolling=true;SetOffset(baseOffset);}
                if(samePart){repeatedOffset=Math.Clamp(repeatedOffset+delta*20*(elapsed-last)/1000,0,MaximumOffset);SetOffset(repeatedOffset);}
                last=elapsed;
            }
            top.RequestAnimationFrame(Frame);
        }
        top.RequestAnimationFrame(Frame);
    }
    public event EventHandler? OffsetChanged;
    private double RoundDevice(double value)
    {
        var scale = TopLevel.GetTopLevel(this)?.RenderScaling ?? 1;
        return Math.Round(value * scale, MidpointRounding.AwayFromZero) / scale;
    }

    private static Point AxisPoint(Point point)=>new(point.Y,point.X);
    private static Rect AxisRect(Rect rect)=>new(rect.Y,rect.X,rect.Height,rect.Width);
    protected override Size MeasureOverride(Size availableSize)
    {
        _host.Measure(new Size(LogicalWidth,double.PositiveInfinity));
        _extent=Math.Max(LogicalWidth,OverflowWidth);
        var width=double.IsPositiveInfinity(availableSize.Width)?LogicalWidth:availableSize.Width;
        GutterHeight=OperatingSystem.IsWindows()&&RoundDevice(_extent)>RoundDevice(width)?15:0;
        return new Size(width,_host.DesiredSize.Height+GutterHeight);
    }
    protected override Size ArrangeOverride(Size finalSize)
    {
        ClientWidth=finalSize.Width;
        ClientHeight=Math.Max(0,finalSize.Height-GutterHeight);
        UpdateFocusEligibility();
        if(_offset>MaximumOffset){CancelScroll();SetOffset(MaximumOffset);}
        _host.Arrange(new Rect(0,0,ClientWidth,ClientHeight));
        _chrome.Arrange(new Rect(finalSize));
        _chrome.InvalidateVisual();return finalSize;
    }
    private void UpdateFocusEligibility()
    {
        Focusable=MaximumOffset>0;
        IsTabStop=Focusable&&!(Content?.GetVisualDescendants().Prepend(Content).OfType<InputElement>()
            .Any(x=>x.Focusable&&x.IsTabStop&&x.IsEffectivelyEnabled&&x.IsEffectivelyVisible)??false);
    }
    public Rect ThumbBounds=>AxisRect(_chrome.ThumbBounds);

    private sealed class ScrollChrome : Control
    {
        private readonly WebCapabilityViewport _owner;
        private readonly WebCubicBezierEasing _ease = new(.42, 0, .58, 1);
        private double _thumbAlpha, _trackAlpha, _thumbBoxWidth = 12;
        private bool _inTrack, _hoverThumb, _scheduled;
        private ScrollPart _hoverPart;
        private int _generation;
        private long _lastScroll, _trackStarted, _expandStarted;
        private bool _trackAnimating, _expanding, _fading;
        public ScrollChrome(WebCapabilityViewport owner)
        {
            _owner = owner;
            owner.PointerMoved += PointerMovedInOwner;
            owner.PointerExited += (_,_) => { _inTrack = _hoverThumb = false; _hoverPart=ScrollPart.None; _lastScroll = Stopwatch.GetTimestamp(); Schedule(); InvalidateVisual(); };
            owner.DetachedFromVisualTree += (_,_) => { _generation++;_scheduled = false;_inTrack=_hoverThumb=false; _thumbAlpha = _trackAlpha = 0; _thumbBoxWidth = 12; _trackAnimating = _expanding = _fading = false; };
            IsHitTestVisible = false;
        }
        public Rect ThumbBounds
        {
            get
            {
                if (_owner.MaximumOffset <= 0) return default;
                var windows = OperatingSystem.IsWindows();
                var scale = TopLevel.GetTopLevel(_owner)?.RenderScaling ?? 1;
                var height = _owner.ClientWidth;
                var button = windows ? Math.Min(18, Math.Floor(height / 2)) : 0;
                var track = Math.Max(0, height - 2 * button);
                var length = Math.Min(track, Math.Max(windows ? 17 : 26,
                    Math.Round(height / _owner.ScrollWidth * track * scale, MidpointRounding.AwayFromZero) / scale));
                var position = _owner.Offset * (track - length) / _owner.MaximumOffset;
                position = position > 0 && position * scale < 1 ? 1 / scale : Math.Truncate(position * scale) / scale;
                return windows ? new Rect(_owner.Bounds.Height - 12, button + position, 9, length)
                    : new Rect(_owner.Bounds.Height - (int)_thumbBoxWidth + 3, position + 2, Math.Max(0, (int)_thumbBoxWidth - 5), Math.Max(0, length - 4));
            }
        }
        public double ThumbTravel => Math.Max(0,_owner.ClientWidth-(OperatingSystem.IsWindows()?2*Math.Min(18,Math.Floor(_owner.ClientWidth/2)):4)-ThumbBounds.Height);
        public ScrollPart HitPart(Point point)
        {
            if(_owner.MaximumOffset<=0||point.Y<0||point.Y>=Bounds.Width||point.X<Bounds.Height-(OperatingSystem.IsWindows()?15:16)||point.X>=Bounds.Height)return ScrollPart.None;
            if(!OperatingSystem.IsWindows()&&_thumbAlpha==0)return ScrollPart.None;
            var button=OperatingSystem.IsWindows()?Math.Min(18,Math.Floor(Bounds.Width/2)):0;
            if(point.Y<button)return ScrollPart.Up;
            if(point.Y>=Bounds.Width-button)return ScrollPart.Down;
            var thumb=ThumbBounds;
            if(point.Y>=thumb.Top-(OperatingSystem.IsWindows()?0:2)&&point.Y<=thumb.Bottom+(OperatingSystem.IsWindows()?0:2))return ScrollPart.Thumb;
            return point.Y<thumb.Top?ScrollPart.TrackAbove:ScrollPart.TrackBelow;
        }
        private void CancelFade()
        {
            if (!_fading) return;
            _fading = false;
            _thumbAlpha = 1;
            if (_trackAlpha > 0) _trackAlpha = 1;
        }
        public void DidScroll()
        {
            if (!OperatingSystem.IsWindows())
            {
                CancelFade();
                if (_thumbAlpha == 0)
                {
                    _thumbAlpha = 1; _thumbBoxWidth = 12;
                    var point = _owner._pointerPosition;
                    if (point.X >= _owner.Bounds.Height - 16 && point.X < _owner.Bounds.Height && point.Y >= 0 && point.Y < _owner.ClientWidth)
                    { _inTrack = true; _trackAlpha = 1; _thumbBoxWidth = 16; }
                }
                _lastScroll = Stopwatch.GetTimestamp();
                Schedule();
            }
            InvalidateVisual();
        }
        private void PointerMovedInOwner(object? sender, PointerEventArgs e)
        {
            var point = AxisPoint(e.GetPosition(_owner));
            var inTrack = _owner.MaximumOffset > 0 && point.X >= _owner.Bounds.Height - (OperatingSystem.IsWindows() ? 15 : 16) && point.Y >= 0 && point.Y < _owner.Bounds.Width;
            if (!OperatingSystem.IsWindows() && _thumbAlpha == 0) inTrack = false;
            if (inTrack != _inTrack)
            {
                _inTrack = inTrack;
                if (inTrack)
                {
                    CancelFade();
                    if (_trackAlpha != 1 && !_trackAnimating)
                    { _trackStarted = Stopwatch.GetTimestamp(); _trackAnimating = true; }
                    if (_thumbBoxWidth != 16 && !_expanding)
                    { _expandStarted = Stopwatch.GetTimestamp(); _expanding = true; }
                }
                else _lastScroll = Stopwatch.GetTimestamp();
                Schedule();
            }
            _hoverThumb = inTrack && ThumbBounds.Contains(point);
            _hoverPart=inTrack?HitPart(point):ScrollPart.None;
            InvalidateVisual();
        }
        private void Schedule()
        {
            if (OperatingSystem.IsWindows() || _scheduled || TopLevel.GetTopLevel(this) is not { } top) return;
            _scheduled = true;var generation=_generation;top.RequestAnimationFrame(t=>{if(generation==_generation)Frame(t);});
        }
        private void Frame(TimeSpan frameTime)
        {
            _scheduled = false;
            if (TopLevel.GetTopLevel(this) is null) return;
            // These UA animations continue after pointer exit. A later
            // re-entry cancels fade-out without restarting completed expansion.
            if (_trackAnimating)
            {
                var phase = Math.Clamp(Stopwatch.GetElapsedTime(_trackStarted).TotalMilliseconds / 250, 0, 1);
                _trackAlpha = _ease.Ease(phase);
                if (phase == 1) _trackAnimating = false;
            }
            if (_expanding)
            {
                var phase = Math.Clamp(Stopwatch.GetElapsedTime(_expandStarted).TotalMilliseconds / 250, 0, 1);
                _thumbBoxWidth = 12 + 4 * _ease.Ease(phase);
                if (phase == 1) _expanding = false;
            }
            if (!_inTrack && !_owner._dragging && _thumbAlpha > 0)
            {
                var elapsed = Stopwatch.GetElapsedTime(_lastScroll).TotalMilliseconds - 500;
                if (elapsed >= 0)
                {
                    _fading = true; _trackAnimating = false;
                    var phase = Math.Clamp(elapsed / 250, 0, 1);
                    var alpha = 1 - _ease.Ease(phase);
                    _thumbAlpha = alpha;
                    if (_trackAlpha > 0) _trackAlpha = alpha;
                    if (phase == 1)
                    { _thumbBoxWidth = 12; _trackAlpha = 0; _trackAnimating = _expanding = _fading = false; }
                }
            }
            if (_trackAnimating || _expanding || (!_inTrack && _thumbAlpha > 0)) Schedule();
            InvalidateVisual();
        }
        public override void Render(DrawingContext context)
        {
            if(_owner._focusVisible)context.DrawRectangle(null,new Pen(Brush.Parse("#5865f2"),3),new Rect(Bounds.Size).Inflate(4.5));
            using var horizontal=context.PushTransform(new Matrix(0,1,1,0,0,0));
            if (_owner.MaximumOffset <= 0) return;
            if (OperatingSystem.IsWindows())
            {
                var track = new Rect(Bounds.Height - 15, 0, 15, Bounds.Width);
                context.DrawRectangle(Brush.Parse("#2c2c2c"), null, track);
                var button = Math.Min(18, Math.Floor(Bounds.Width / 2));
                PaintArrow(context, new Rect(track.X, 0, 15, button), true,_owner.Offset>0 && _owner._pressedPart==ScrollPart.Up,_owner.Offset>0 && _hoverPart==ScrollPart.Up);
                PaintArrow(context, new Rect(track.X, Bounds.Width - button, 15, button), false,_owner.Offset<_owner.MaximumOffset && _owner._pressedPart==ScrollPart.Down,_owner.Offset<_owner.MaximumOffset && _hoverPart==ScrollPart.Down);
                var thumb = ThumbBounds;
                var channel=_hoverThumb || _owner._pressedPart==ScrollPart.Thumb ? (byte)209 : (byte)159;
                context.Custom(new CapsuleThumb(thumb,channel,255));
            }
            else if (_thumbAlpha > 0)
            {
                if (_trackAlpha > 0)
                {
                    var track = new Rect(Bounds.Height - 16, 0, 16, Bounds.Width);
                    var gradient = new LinearGradientBrush { StartPoint = new RelativePoint(0,0,RelativeUnit.Relative), EndPoint = new RelativePoint(0,1,RelativeUnit.Relative),
                        GradientStops = new GradientStops { new(Color.FromArgb(40,216,216,216),0), new(Color.FromArgb(38,204,204,204),1.0/3), new(Color.FromArgb(38,204,204,204),2.0/3), new(Color.FromArgb(38,204,204,204),1) } };
                    using (context.PushOpacity(_trackAlpha))
                    {
                        context.DrawRectangle(gradient, null, track);
                        context.DrawRectangle(new SolidColorBrush(Color.FromArgb(51,229,229,229)), null, new Rect(track.X,0,1,track.Height));
                        context.DrawRectangle(new SolidColorBrush(Color.FromArgb(40,216,216,216)),null,new Rect(track.Right-1,0,1,track.Height));
                    }
                }
                var thumb = ThumbBounds;
                using (context.PushOpacity(_thumbAlpha)) context.Custom(new CapsuleThumb(thumb,255,128));
            }
        }
        private static void PaintArrow(DrawingContext context, Rect button, bool up,bool pressed,bool hovered)
        {
            // Live Edge151 at the verified 18px button: main's 9px/-1/+1
            // geometry differs from the installed browser. See the preserved
            // source crops and scrollviewport-windows-glyph-fit.json.
            var factor = Math.Max(button.Width,button.Height) / 18;
            var side = (pressed?6.5:7.5) * factor;
            var x = button.X + (button.Width - side) / 2;
            var baseline = button.Y + (pressed?12.25:12.75) * factor;
            // Chromium paints a SkFont glyph at this baseline, not a text linebox.
            context.Custom(new FluentArrow(button,up ? '\uEDDB' : '\uEDDC',side,new Point(x,baseline),pressed||hovered));
        }
        private sealed class FluentArrow(Rect rect,char character,double size,Point baseline,bool highlighted) : ICustomDrawOperation
        {
            public Rect Bounds => rect;
            public bool HitTest(Point point) => false;
            public bool Equals(ICustomDrawOperation? other) => false;
            public void Dispose() { }
            public void Render(ImmediateDrawingContext context)
            {
                if(context.TryGetFeature<ISkiaSharpApiLeaseFeature>() is not { } feature) return;
                using var lease=feature.Lease();
                using var face=SKTypeface.FromFamilyName("Segoe Fluent Icons");
                using var font=new SKFont(face,(float)size) { Edging=SKFontEdging.Antialias,Subpixel=true,Hinting=SKFontHinting.None };
                byte channel=highlighted?(byte)209:(byte)159;
                using var paint=new SKPaint { IsAntialias=true,Color=new SKColor(channel,channel,channel,(byte)(255*lease.CurrentOpacity)) };
                using var blob=SKTextBlob.Create(character.ToString(),font);
                lease.SkCanvas.DrawText(blob,(float)baseline.X,(float)baseline.Y,paint);
            }
        }
        private sealed class CapsuleThumb(Rect rect,byte channel,byte alpha):ICustomDrawOperation
        {
            // The live Chromium cap uses GPU analytic distance coverage.
            // Preserve the same capsule geometry on Avalonia's CPU/GPU paths.
            private static readonly Lazy<SKRuntimeEffect> CapsuleEffect=new(()=>
                SKRuntimeEffect.CreateShader("""
                uniform float4 box;
                uniform float scale;
                uniform half4 ink;
                half4 main(float2 p) {
                    float radius=(box.z-box.x)*0.5;
                    float2 delta=p-(box.xy+box.zw)*0.5;
                    delta.y=max(abs(delta.y)-max(0.0,(box.w-box.y)*0.5-radius),0.0);
                    float coverage=clamp(radius*scale+0.5-length(delta)*scale,0.0,1.0);
                    return ink*coverage;
                }
                """,out var error) ?? throw new InvalidOperationException(error));
            public Rect Bounds=>rect;
            public bool HitTest(Point point)=>false;
            public bool Equals(ICustomDrawOperation? other)=>false;
            public void Dispose(){}
            public void Render(ImmediateDrawingContext context)
            {
                if(context.TryGetFeature<ISkiaSharpApiLeaseFeature>() is not {} feature)return;
                using var lease=feature.Lease();
                using var paint=new SKPaint{IsAntialias=true,Color=new SKColor(channel,channel,channel,(byte)(alpha*lease.CurrentOpacity))};
                if (OperatingSystem.IsMacOS())
                {
                    using var path = new SKPath();
                    path.AddRoundRect(new SKRect((float)rect.Left,(float)rect.Top,(float)rect.Right,(float)rect.Bottom), (float)rect.Width, (float)rect.Width);
                    lease.SkCanvas.DrawPath(path,paint);
                    return;
                }
                var matrix=lease.SkCanvas.TotalMatrix;
                var scale=Math.Sqrt(matrix.ScaleX*matrix.ScaleX+matrix.SkewY*matrix.SkewY);
                var opacity=(float)lease.CurrentOpacity*alpha/255f;
                using var uniforms=new SKRuntimeEffectUniforms(CapsuleEffect.Value)
                {
                    ["box"]=new float[]{(float)rect.Left,(float)rect.Top,(float)rect.Right,(float)rect.Bottom},
                    ["scale"]=(float)Math.Abs(scale),
                    ["ink"]=new float[]{channel/255f*opacity,channel/255f*opacity,channel/255f*opacity,opacity}
                };
                using var shader=CapsuleEffect.Value.ToShader(uniforms);
                paint.Color=SKColors.White;paint.Shader=shader;paint.IsAntialias=false;
                lease.SkCanvas.DrawRect(new SKRect((float)rect.Left,(float)rect.Top,(float)rect.Right,(float)rect.Bottom),paint);
            }
        }
    }

    private sealed class ViewportContent(WebCapabilityViewport owner):Panel
    {
        protected override Size MeasureOverride(Size availableSize)
        {
            if(owner.Content is not {} child)return default;
            child.Measure(new Size(owner.LogicalWidth,double.PositiveInfinity));
            return new Size(owner.LogicalWidth,child.DesiredSize.Height);
        }
        protected override Size ArrangeOverride(Size finalSize)
        {
            owner.Content?.Arrange(new Rect(-owner.Offset,0,owner.LogicalWidth,finalSize.Height));
            return finalSize;
        }
    }
}
