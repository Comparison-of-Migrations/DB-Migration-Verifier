using System.Globalization;
using System.Diagnostics;
using Avalonia;
using Avalonia.Automation;
using Avalonia.Automation.Peers;
using Avalonia.Automation.Provider;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Threading;
using Avalonia.Rendering.SceneGraph;
using Avalonia.Skia;
using SkiaSharp;

namespace Verifier.Desktop;

/// <summary>Source port input, including its editing value and platform UA spinner.</summary>
public sealed class WebNumberInput : UserControl
{
    public static readonly StyledProperty<string> ValueProperty=
        AvaloniaProperty.Register<WebNumberInput,string>(nameof(Value),"");
    private readonly NumberEditor _editor;
    private readonly NumberSpin _spin;
    private bool _assigning,_fromEditor,_readOnly;
    private string _focusValue="";
    public string Value {get=>GetValue(ValueProperty);set=>AssignValue(value);}
    public string RawText=>_editor.Text??"";
    public WebNumberRules.State Validity=>WebNumberRules.Read(RawText);
    public bool IsReadOnly {get=>_readOnly;set{_readOnly=value;_editor.IsReadOnly=value;UpdateSpin();}}
    public bool ReducedMotion {get=>_editor.ReducedMotion;set=>_editor.ReducedMotion=value;}
    public WebTextInput Editor=>_editor;
    public event EventHandler? Input;
    public event EventHandler? Change;
    public WebNumberInput()
    {
        _editor=new(this);
        Width=112;MinHeight=44;UseLayoutRounding=false;ClipToBounds=false;
        Focusable=false;IsTabStop=false;FocusAdorner=null;
        var spinWidth=OperatingSystem.IsWindows()?15:11;
        _editor.Padding=new(12,9,12+spinWidth,9);
        _editor.InsertionFilter=s=>WebNumberRules.FilterInsertion(RawText,_editor.SelectionStart,_editor.SelectionEnd,s);
        _spin=new(this){Width=spinWidth,Height=22,HorizontalAlignment=HorizontalAlignment.Right,
            VerticalAlignment=VerticalAlignment.Center,Margin=new(0,0,13,0),UseLayoutRounding=false};
        var grid=new Grid{UseLayoutRounding=false};grid.Children.Add(_editor);grid.Children.Add(_spin);Content=grid;
        _editor.PropertyChanged+=(_,e)=>
        {
            if(e.Property==TextBox.TextProperty&&!_assigning)
            {
                _fromEditor=true;
                try{SetCurrentValue(ValueProperty,Validity.Value);}finally{_fromEditor=false;}
                Input?.Invoke(this,EventArgs.Empty);
            }
            if(e.Property==InputElement.IsFocusedProperty)UpdateSpin();
        };
        _editor.GotFocus+=(_,_)=>{_focusValue=Value;};
        _editor.LostFocus+=(_,_)=>{CommitChange();UpdateSpin();};
        AddHandler(KeyDownEvent,NumberKey,RoutingStrategies.Tunnel);
        AddHandler(PointerWheelChangedEvent,NumberWheel,RoutingStrategies.Tunnel);
        UpdateSpin();
    }
    protected override void OnGotFocus(FocusChangedEventArgs e)
    {base.OnGotFocus(e);_editor.Focus(e.NavigationMethod,e.KeyModifiers);}
    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs e)
    {
        base.OnPropertyChanged(e);
        if(_spin is null)return;
        if(e.Property==ValueProperty&&!_fromEditor&&!_assigning)AssignValue(Value??"");
        if(e.Property==IsPointerOverProperty||e.Property==IsEffectivelyEnabledProperty)UpdateSpin();
    }
    private void AssignValue(string value)
    {
        var sanitized=WebNumberRules.SanitizeAssignedValue(value??"");
        _assigning=true;
        try
        {
            SetCurrentValue(ValueProperty,sanitized);_editor.Text=sanitized;
            _editor.CaretIndex=_editor.SelectionStart=_editor.SelectionEnd=sanitized.Length;
            _focusValue=sanitized;
        }
        finally{_assigning=false;}
    }
    private void SetAccessibleValue(string value)
    {
        if(IsReadOnly||!IsEffectivelyEnabled)throw new InvalidOperationException("Number input is not editable.");
        var before=Value;AssignValue(value);
        if(before==Value)return;
        Input?.Invoke(this,EventArgs.Empty);Change?.Invoke(this,EventArgs.Empty);
    }
    private void UpdateSpin()
    {
        _spin.IsVisible=IsEffectivelyEnabled&&!IsReadOnly&&(_editor.IsFocused||IsPointerOver);
        if(!_spin.IsVisible)_spin.Stop();
    }
    private void NumberKey(object? sender,KeyEventArgs e)
    {
        if(e.Key is Key.Up or Key.Down && IsEffectivelyEnabled&&!IsReadOnly && !(e.Key==Key.Down&&e.KeyModifiers.HasFlag(KeyModifiers.Alt)))
        {Step(e.Key==Key.Up?1:-1);e.Handled=true;}
    }
    private void NumberWheel(object? sender,PointerWheelEventArgs e)
    {
        if(_editor.IsFocused&&IsEffectivelyEnabled&&!IsReadOnly&&e.Delta.Y!=0)
        {Step(e.Delta.Y>0?1:-1,false);e.Handled=true;}
    }
    public void Step(int direction,bool commitChange=true)
    {
        if(!IsEffectivelyEnabled||IsReadOnly)return;
        var next=WebNumberRules.Step(RawText,direction);
        if(next!=RawText)
        {
            _editor.SetCurrentValue(TextBox.TextProperty,next);
            _editor.CaretIndex=_editor.SelectionStart=_editor.SelectionEnd=next.Length;
        }
        if(commitChange)CommitChange();
    }
    private void CommitChange(){if(_focusValue==Value)return;_focusValue=Value;Change?.Invoke(this,EventArgs.Empty);}
    private void FocusForSpin(){_editor.Focus(NavigationMethod.Pointer);_editor.SelectAll();}
    protected override AutomationPeer OnCreateAutomationPeer()=>new ContainerPeer(this);
    private sealed class NumberEditor(WebNumberInput owner):WebTextInput
    {protected override AutomationPeer OnCreateAutomationPeer()=>new NumberPeer(owner,this);}
    private sealed class ContainerPeer(WebNumberInput owner):ControlAutomationPeer(owner)
    {
        protected override string? GetAutomationIdCore()=>null;
        protected override string? GetNameCore()=>null;
        protected override bool IsControlElementCore()=>false;
        protected override bool IsContentElementCore()=>false;
        protected override AutomationControlType GetAutomationControlTypeCore()=>AutomationControlType.Pane;
    }
    private sealed class NumberPeer:ControlAutomationPeer,IValueProvider,IRangeValueProvider
    {
        private readonly WebNumberInput owner;
        public NumberPeer(WebNumberInput host,WebTextInput editor):base(editor)
        {
            owner=host;
            editor.PropertyChanged+=(_,e)=>
            {
                if(e.Property!=TextBox.TextProperty)return;
                RaisePropertyChangedEvent(ValuePatternIdentifiers.ValueProperty,e.OldValue,e.NewValue);
                var before=WebNumberRules.Read(e.OldValue as string??"").Number??double.NaN;
                var after=WebNumberRules.Read(e.NewValue as string??"").Number??double.NaN;
                RaisePropertyChangedEvent(RangeValuePatternIdentifiers.ValueProperty,before,after);
            };
        }
        bool IValueProvider.IsReadOnly=>owner.IsReadOnly;
        string IValueProvider.Value=>owner.RawText;
        void IValueProvider.SetValue(string? value)=>owner.SetAccessibleValue(value??"");
        bool IRangeValueProvider.IsReadOnly=>owner.IsReadOnly;
        double IRangeValueProvider.Value=>owner.Validity.Number??double.NaN;
        double IRangeValueProvider.Minimum=>1;
        double IRangeValueProvider.Maximum=>65535;
        double IRangeValueProvider.SmallChange=>1;
        double IRangeValueProvider.LargeChange=>1;
        void IRangeValueProvider.SetValue(double value)=>owner.SetAccessibleValue(value.ToString("R",CultureInfo.InvariantCulture));
        protected override AutomationControlType GetAutomationControlTypeCore()=>AutomationControlType.Spinner;
        protected override string? GetAutomationIdCore()=>AutomationProperties.GetAutomationId(owner);
        protected override string? GetNameCore()=>AutomationProperties.GetName(owner);
        protected override IReadOnlyList<AutomationPeer>? GetChildrenCore()=>Array.Empty<AutomationPeer>();
        protected override bool HasKeyboardFocusCore()=>owner._editor.IsFocused;
        protected override bool IsKeyboardFocusableCore()=>owner.IsEffectivelyEnabled;
        protected override void SetFocusCore()=>owner._editor.Focus();
    }
    private sealed class NumberSpin(WebNumberInput owner):Control
    {
        private readonly DispatcherTimer _repeat=new();
        private TimeSpan _repeatPeriod=TimeSpan.FromMilliseconds(50);
        private long _repeatStarted;
        private double _nextRepeat;
        private int _direction=1;
        private bool _pressed;
        protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs e)
        {base.OnPropertyChanged(e);if(e.Property==IsPointerOverProperty)InvalidateVisual();}
        public void Stop(){_repeat.Stop();_pressed=false;InvalidateVisual();}
        private void Position(Point p){_direction=Math.Floor(p.Y+.5)<Bounds.Height/2?1:-1;InvalidateVisual();}
        protected override void OnPointerMoved(PointerEventArgs e){base.OnPointerMoved(e);Position(e.GetPosition(this));}
        protected override void OnPointerEntered(PointerEventArgs e){base.OnPointerEntered(e);Position(e.GetPosition(this));}
        protected override void OnPointerExited(PointerEventArgs e){base.OnPointerExited(e);InvalidateVisual();}
        protected override void OnPointerPressed(PointerPressedEventArgs e)
        {
            if(!e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)return;
            Position(e.GetPosition(this));owner.FocusForSpin();_pressed=true;
            var timing=NumberSpinTiming.Read();_repeatPeriod=timing.Period;
            _repeatStarted=Stopwatch.GetTimestamp();_nextRepeat=timing.Initial.TotalMilliseconds;
            _repeat.Tick-=Repeat;_repeat.Tick+=Repeat;_repeat.Interval=timing.Initial;_repeat.Start();
            owner.Step(_direction,false);e.Pointer.Capture(this);e.Handled=true;InvalidateVisual();
        }
        private void Repeat(object? sender,EventArgs e)
        {
            owner.Step(_direction,false);
            if(!_repeat.IsEnabled)return;
            var elapsed=Stopwatch.GetElapsedTime(_repeatStarted).TotalMilliseconds;
            if(_repeatPeriod<=TimeSpan.Zero){_repeat.Interval=TimeSpan.Zero;return;}
            do{_nextRepeat+=_repeatPeriod.TotalMilliseconds;}while(_nextRepeat<=elapsed);
            _repeat.Interval=TimeSpan.FromMilliseconds(_nextRepeat-elapsed);
        }
        protected override void OnPointerReleased(PointerReleasedEventArgs e)
        {Stop();e.Pointer.Capture(null);owner.CommitChange();e.Handled=true;}
        protected override void OnPointerCaptureLost(PointerCaptureLostEventArgs e){base.OnPointerCaptureLost(e);Stop();owner.CommitChange();}
        protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e){Stop();base.OnDetachedFromVisualTree(e);}
        public override void Render(DrawingContext context)
        {
            var windows=OperatingSystem.IsWindows();
            for(var i=0;i<2;i++)
            {
                var direction=i==0?1:-1;var active=direction==_direction;
                var state=active&&_pressed?2:active&&IsPointerOver?1:0;
                var rect=new Rect(0,i*11,Bounds.Width,11);
                var background=windows?"#2c2c2c":state switch{2=>"#b1b1b1",1=>"#4f4f4f",_=>"#424242"};
                var foreground=windows?state==0?"#9f9f9f":"#d1d1d1":state==2?"#000000":"#ffffff";
                context.DrawRectangle(Brush.Parse(background),null,rect);
                if(windows)PaintFluent(context,rect,direction,state,Brush.Parse(foreground));
                else context.Custom(new MacArrow(rect,direction,Color.Parse(foreground),Color.Parse(background)));
            }
        }
        private static void PaintFluent(DrawingContext context,Rect rect,int direction,int state,IBrush foreground)
        {
            var character=direction>0?'\ueddb':'\ueddc';
            // Measured live Edge151 port glyphs (the generic HEAD scrollbar
            // arrow calculation does not reproduce this inner spin control).
            var side=state==2?5.5:6;
            var x=rect.X+(rect.Width-side)/2;
            context.Custom(new FluentGlyph(rect,character,side,new(x,rect.Y+8.5),((ISolidColorBrush)foreground).Color));
        }
        private sealed class FluentGlyph(Rect rect,char character,double size,Point baseline,Color color):ICustomDrawOperation
        {
            public Rect Bounds=>rect;
            public bool HitTest(Point point)=>false;
            public bool Equals(ICustomDrawOperation? other)=>false;
            public void Dispose(){}
            public void Render(ImmediateDrawingContext context)
            {
                var feature=context.TryGetFeature<ISkiaSharpApiLeaseFeature>();if(feature is null)return;
                using var lease=feature.Lease();
                using var face=SKTypeface.FromFamilyName("Segoe Fluent Icons");
                using var font=new SKFont(face,(float)size){Edging=SKFontEdging.Antialias,Subpixel=true};
                using var paint=new SKPaint{IsAntialias=true,Color=new SKColor(color.R,color.G,color.B,(byte)(color.A*lease.CurrentOpacity))};
                using var blob=SKTextBlob.Create(character.ToString(),font);
                lease.SkCanvas.DrawText(blob,(float)baseline.X,(float)baseline.Y,paint);
            }
        }
        private sealed class MacArrow(Rect rect,int direction,Color foreground,Color background):ICustomDrawOperation
        {
            public Rect Bounds=>rect;
            public bool HitTest(Point point)=>false;
            public bool Equals(ICustomDrawOperation? other)=>false;
            public void Dispose(){}
            public void Render(ImmediateDrawingContext context)
            {
                var feature=context.TryGetFeature<ISkiaSharpApiLeaseFeature>();if(feature is null)return;
                using var lease=feature.Lease();var canvas=lease.SkCanvas;
                var matrix=canvas.TotalMatrix;var scale=Math.Abs(matrix.ScaleX);
                if(scale<=0||Math.Abs(Math.Abs(matrix.ScaleY)-scale)>.001)return;
                var width=(int)Math.Round(rect.Width*scale);var height=(int)Math.Round(rect.Height*scale);
                var inset=(Math.Max(width,height)+3)/4;
                var side=Math.Min(Math.Min(width,height),Math.Max(width,height)-2*inset);
                var left=(width-side+1)/2;var top=(height-side+1)/2;
                var arrowHeight=side/2+1;var bottom=top+side-arrowHeight/2+1;
                var center=top+side/2d;
                bool Inside(double x,double y)
                {
                    if(direction<0)y=2*center-y;
                    return y<=bottom&&y>=bottom-arrowHeight&&
                        Math.Abs(x-(left+side/2d))<=side/2d*(y-(bottom-arrowHeight))/arrowHeight;
                }
                // Live Chromium's four-sample GPU coverage, rather than analytic AA.
                ReadOnlySpan<Point> samples=[new(.375,.125),new(.875,.375),new(.125,.625),new(.625,.875)];
                using var paint=new SKPaint{IsAntialias=false};
                for(var y=0;y<height;y++)for(var x=0;x<width;x++)
                {
                    var count=0;foreach(var sample in samples)if(Inside(x+sample.X,y+sample.Y))count++;
                    if(count==0)continue;
                    byte Mix(byte a,byte b)=>(byte)Math.Round((a*count+b*(4-count))/4d,MidpointRounding.AwayFromZero);
                    paint.Color=new SKColor(Mix(foreground.R,background.R),Mix(foreground.G,background.G),Mix(foreground.B,background.B),(byte)(255*lease.CurrentOpacity));
                    canvas.DrawRect((float)(rect.X+x/scale),(float)(rect.Y+y/scale),1/scale,1/scale,paint);
                }
            }
        }
    }
}
