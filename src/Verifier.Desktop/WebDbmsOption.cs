using System.Diagnostics;
using Avalonia;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.VisualTree;

namespace Verifier.Desktop;

/// <summary>Original DBMS label card. Connection state and port changes belong to its parent.</summary>
public sealed class WebDbmsOption : UserControl
{
    private readonly WebSurfaceBox _surface;
    private readonly DbmsFocus _outline;
    private readonly DbmsRow _row;
    private readonly CssTransition<CssColor> _background=new(Mix,160),_border=new(Mix,160),_shadowColor=new(Mix,160);
    private readonly CssTransition<double> _spread=new((a,b,t)=>a+(b-a)*t,160);
    private bool _compact,_reduced,_setting,_armed;
    private long _revision;
    public WebRadio Radio {get;}=new();
    public string EngineName {get=>_row.Body.Title.Text??"";set{_row.Body.Title.Text=value??"";AutomationProperties.SetName(Radio,value??"");}}
    public string Description {get=>_row.Body.Description.Text??"";set=>_row.Body.Description.Text=value??"";}
    public string GroupName {get=>Radio.GroupName??"";set=>Radio.GroupName=value;}
    public bool IsSelected {get=>Radio.IsChecked==true;set{_setting=true;try{Radio.SetCurrentValue(WebRadio.IsCheckedProperty,value);}finally{_setting=false;}}}
    public bool Compact {get=>_compact;set{_compact=value;_surface.Padding=new(value?14:16);_row.Compact=value;InvalidateMeasure();}}
    public bool ReducedMotion {get=>_reduced;set{_reduced=value;Radio.ReducedMotion=value;ApplyState(false);}}
    public event EventHandler? SelectionChanged;
    public WebDbmsOption()
    {
        UseLayoutRounding=false;ClipToBounds=false;Focusable=false;IsTabStop=false;Cursor=new(StandardCursorType.Hand);
        _row=new(Radio);_surface=WebSurface.Create(Brushes.Transparent,Brushes.Transparent,new(1),new(10));
        _surface.Padding=new(16);_surface.MinHeight=88;_surface.Child=_row;
        _outline=new(){UseLayoutRounding=false,Child=_surface};Content=_outline;
        Radio.PropertyChanged+=(_,e)=>
        {
            if(e.Property==WebRadio.IsCheckedProperty){ApplyState();if(!_setting&&Radio.IsChecked==true)SelectionChanged?.Invoke(this,EventArgs.Empty);}
            if(e.Property==WebRadio.FocusVisibleProperty)_outline.Visible=Radio.FocusVisible;
        };
        ApplyState(false);
    }
    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {base.OnPropertyChanged(change);if(_surface is not null&&change.Property==IsPointerOverProperty)ApplyState();}
    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e){base.OnAttachedToVisualTree(e);ApplyState(false);}
    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e){_revision++;base.OnDetachedFromVisualTree(e);}
    private bool FromRadio(object? source)=>source is Visual v&&(v==Radio||Radio.IsVisualAncestorOf(v));
    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        base.OnPointerPressed(e);if(FromRadio(e.Source)||!IsEffectivelyEnabled||!e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)return;
        _armed=true;e.Pointer.Capture(this);e.Handled=true;
    }
    protected override void OnPointerReleased(PointerReleasedEventArgs e)
    {
        base.OnPointerReleased(e);if(!_armed||e.InitialPressMouseButton!=MouseButton.Left)return;
        _armed=false;e.Pointer.Capture(null);
        if(new Rect(Bounds.Size).Contains(e.GetPosition(this))&&Radio.IsEffectivelyEnabled){Radio.FocusFromPointer();Radio.SetCurrentValue(WebRadio.IsCheckedProperty,true);}e.Handled=true;
    }
    protected override void OnPointerCaptureLost(PointerCaptureLostEventArgs e){_armed=false;base.OnPointerCaptureLost(e);}
    private readonly record struct Appearance(Color Background,Color Border,double Spread,Color Shadow);
    private Appearance Resolve(bool hover)
    {
        var bg=IsSelected?"#3b3d52":"#2b2d31";var border=IsSelected?"#8b94ff":"#7c838c";
        if(hover){bg="#383b44";border="#a7b1ff";}
        return new(Color.Parse(bg),Color.Parse(border),IsSelected?1:0,IsSelected?Color.Parse("#8b94ff"):Colors.Transparent);
    }
    private void Paint(Appearance state)
    {
        _surface.Background=new SolidColorBrush(state.Background);_surface.BorderBrush=new SolidColorBrush(state.Border);
        _surface.BoxShadow=state.Spread==0&&state.Shadow.A==0?default:new BoxShadows(new BoxShadow{IsInset=true,Spread=state.Spread,Color=state.Shadow});
        _row.Icon.Stroke=Brush.Parse(IsSelected?"#b4bdff":"#b5bac1");
    }
    private void ApplyState(bool animate=true)
    {
        if(_surface is null)return;var state=Resolve(IsPointerOver);var top=TopLevel.GetTopLevel(this);var animated=animate&&!_reduced&&top is not null;
        void Set<T>(CssTransition<T> track,T target){if(animated)track.SetTarget(target);else track.Snap(target);}
        Set(_background,CssColor.FromColor(state.Background));Set(_border,CssColor.FromColor(state.Border));Set(_shadowColor,CssColor.FromColor(state.Shadow));Set(_spread,state.Spread);
        void Current()=>Paint(new(_background.Current.ToColor(),_border.Current.ToColor(),_spread.Current,_shadowColor.Current.ToColor()));
        Current();var revision=++_revision;if(!animated)return;
        void Frame(TimeSpan _){if(revision!=_revision||!this.IsAttachedToVisualTree())return;Current();if(_background.Running||_border.Running||_shadowColor.Running||_spread.Running)top!.RequestAnimationFrame(Frame);}
        top!.RequestAnimationFrame(Frame);
    }
    private sealed class DbmsRow : Panel
    {
        private readonly WebRadio _radio;
        public SvgIcon Icon {get;}=new(){IconName="db",Width=25,Height=25,UseLayoutRounding=false,IsHitTestVisible=false};
        public DbmsText Body {get;}=new(){UseLayoutRounding=false};
        private bool _compact;
        public bool Compact {get=>_compact;set{_compact=value;Icon.IsVisible=!value;Body.Compact=value;InvalidateMeasure();}}
        public DbmsRow(WebRadio radio){_radio=radio;UseLayoutRounding=false;Children.Add(Icon);Children.Add(Body);Children.Add(radio);}
        protected override Size MeasureOverride(Size available)
        {
            var prefix=_compact?0:42;var gap=_compact?8:12;Icon.Measure(new(25,25));_radio.Measure(new(20,20));Body.Measure(new(Math.Max(0,available.Width-prefix-gap-20),available.Height));
            return new(prefix+Body.ContentWidth+gap+20,Math.Max(_compact?20:25,Body.DesiredSize.Height));
        }
        protected override Size ArrangeOverride(Size final)
        {
            var units=(TopLevel.GetTopLevel(this)?.RenderScaling??2)*64;double Center(double h)=>Math.Floor((final.Height-h)*units/2)/units;
            var prefix=_compact?0:42;var gap=_compact?8:12;
            if(!_compact)Icon.Arrange(new(0,Center(25),25,25));Body.Arrange(new(prefix,Center(Body.DesiredSize.Height),Body.ContentWidth,Body.DesiredSize.Height));_radio.Arrange(new(prefix+Body.ContentWidth+gap,Center(20),20,20));return final;
        }
    }
    private sealed class DbmsText : Panel
    {
        public WebSystemTextBlock Title {get;}=WebTypography.CreateCandidate("",13,20.8,Brush.Parse("#f2f3f5"),700);
        public WebSystemTextBlock Description {get;}=WebTypography.CreateCandidate("",11,17.6,Brush.Parse("#b5bac1"));
        private bool _compact;
        public bool Compact {get=>_compact;set{_compact=value;Description.FontSize=value?10:11;InvalidateMeasure();}}
        public double ContentWidth {get;private set;}
        public DbmsText(){Children.Add(Title);Children.Add(Description);Description.TextWrapping=TextWrapping.NoWrap;}
        protected override Size MeasureOverride(Size available)
        {
            var units=(TopLevel.GetTopLevel(this)?.RenderScaling??2)*64;Title.LineHeight=Math.Floor(13*1.6*units)/units;Description.LineHeight=Math.Floor(Description.FontSize*1.6*units)/units;
            Title.Measure(new(1/units,double.PositiveInfinity));Description.Measure(Size.Infinity);
            var minimum=Math.Ceiling(Math.Max(Title.SystemLineWidths.DefaultIfEmpty().Max(),Description.DesiredSize.Width)*units)/units;
            ContentWidth=Math.Max(available.Width,minimum);Title.Measure(new(ContentWidth,double.PositiveInfinity));Description.Measure(new(ContentWidth,double.PositiveInfinity));return new(ContentWidth,Title.DesiredSize.Height+Description.DesiredSize.Height);
        }
        protected override Size ArrangeOverride(Size final){Title.Arrange(new(0,0,final.Width,Title.DesiredSize.Height));Description.Arrange(new(0,Title.DesiredSize.Height,final.Width,Description.DesiredSize.Height));return final;}
    }
    private sealed class DbmsFocus : Decorator
    {
        private bool _visible;
        public bool Visible {get=>_visible;set{_visible=value;InvalidateVisual();}}
        public override void Render(DrawingContext context){if(_visible)context.DrawRectangle(null,new Pen(Brush.Parse("#a7b1ff"),3),new Rect(Bounds.Size).Inflate(4.5),14.5,14.5);}
    }
    private static CssColor Mix(CssColor a,CssColor b,double t)
    {
        var alpha=a.A+(b.A-a.A)*t;double Channel(double x,double y)=>alpha==0?0:(x*a.A*(1-t)+y*b.A*t)/alpha;
        return new(alpha,Channel(a.R,b.R),Channel(a.G,b.G),Channel(a.B,b.B));
    }
    private readonly record struct CssColor(double A,double R,double G,double B)
    {
        public static CssColor FromColor(Color c)=>new(c.A,c.R,c.G,c.B);
        public Color ToColor(){static byte Byte(double x)=>(byte)Math.Clamp(Math.Round(x,MidpointRounding.AwayFromZero),0,255);return Color.FromArgb(Byte(A),Byte(R),Byte(G),Byte(B));}
    }
    private sealed class CssTransition<T>(Func<T,T,double,T> interpolate,double duration,Func<double>? clock=null)
    {
        private T _from=default!,_target=default!,_reversingStart=default!;
        private double _factor=1,_duration,_started;
        private double Now=>clock?.Invoke()??Stopwatch.GetTimestamp()*(1000d/Stopwatch.Frequency);
        private double Progress=>_duration<=0?1:Math.Min(1,(Now-_started)/_duration);
        public bool Running=>Progress<1;
        public T Current=>interpolate(_from,_target,WebMotion.CssEase().Ease(Progress));
        public void Snap(T target){_from=_target=_reversingStart=target;_duration=0;_factor=1;}
        public void SetTarget(T target)
        {
            if(EqualityComparer<T>.Default.Equals(_target,target))return;var progress=Progress;var value=interpolate(_from,_target,WebMotion.CssEase().Ease(progress));
            if(progress<1&&EqualityComparer<T>.Default.Equals(_reversingStart,target)){_factor=Math.Clamp(Math.Abs(WebMotion.CssEase().Ease(progress)*_factor+1-_factor),0,1);_reversingStart=_target;}
            else{_factor=1;_reversingStart=value;}
            _from=value;_target=target;_duration=duration*_factor;_started=Now;
        }
    }
}
