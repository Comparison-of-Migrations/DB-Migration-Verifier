using System.Diagnostics;
using Avalonia;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.VisualTree;

namespace Verifier.Desktop;

/// <summary>Literal matchingOptions label card; matching policy and badge text belong to the parent.</summary>
public sealed class WebMatchOption : UserControl
{
    private readonly WebSurfaceBox _surface=WebSurface.Create(Brushes.Transparent,Brushes.Transparent,new(1),new(8));
    private readonly MatchOutline _outline=new();
    private readonly MatchRow _row;
    private readonly ColorTrack _background=new(),_border=new(),_shadow=new();
    private readonly ScalarTrack _spread=new();
    private bool _unavailable,_reduced,_setting,_armed;
    private long _revision;
    public WebRadio Radio{get;}=new();
    public WebSystemTextBlock NameText=>_row.Title;
    public WebSystemTextBlock NoteText=>_row.Note;
    public WebSystemTextBlock BadgeText=>_row.Badge;
    public Control Body=>_row.Body;
    public string TargetName{get=>NameText.Text??"";set{NameText.Text=value;UpdateName();}}
    public string Note{get=>NoteText.Text??"";set{NoteText.Text=value;UpdateName();}}
    public string Badge{get=>BadgeText.Text??"";set{BadgeText.Text=value;UpdateName();InvalidateMeasure();}}
    public string GroupName{get=>Radio.GroupName??"";set=>Radio.GroupName=value;}
    public bool IsSelected{get=>Radio.IsChecked==true;set{_setting=true;try{Radio.SetCurrentValue(WebRadio.IsCheckedProperty,value);}finally{_setting=false;}}}
    public bool Unavailable{get=>_unavailable;set{_unavailable=value;Radio.IsEnabled=!value;Cursor=new(value?StandardCursorType.No:StandardCursorType.Hand);ApplyState();}}
    public bool ReducedMotion{get=>_reduced;set{_reduced=value;Radio.ReducedMotion=value;ApplyState(false);}}
    public event EventHandler? SelectionRequested;
    public WebMatchOption()
    {
        UseLayoutRounding=false;ClipToBounds=false;Focusable=false;IsTabStop=false;
        _row=new(Radio);_surface.Padding=new(14,13);_surface.MinHeight=64;_surface.Child=_row;_outline.Child=_surface;Content=_outline;
        Radio.PropertyChanged+=(_,e)=>{if(e.Property==WebRadio.IsCheckedProperty){ApplyState();if(!_setting&&IsSelected)SelectionRequested?.Invoke(this,EventArgs.Empty);}if(e.Property==WebRadio.FocusVisibleProperty)_outline.Visible=Radio.FocusVisible;};
        Unavailable=false;ApplyState(false);
    }
    private void UpdateName()=>AutomationProperties.SetName(Radio,string.Join(" ",new[]{TargetName,Note,Badge}.Where(s=>!string.IsNullOrEmpty(s))));
    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs e){base.OnPropertyChanged(e);if(_row is not null&&e.Property==IsPointerOverProperty)ApplyState();}
    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e){base.OnAttachedToVisualTree(e);ApplyState(false);}
    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e){++_revision;base.OnDetachedFromVisualTree(e);}
    private bool FromRadio(object? source)=>source is Visual v&&(v==Radio||Radio.IsVisualAncestorOf(v));
    protected override void OnPointerPressed(PointerPressedEventArgs e){base.OnPointerPressed(e);if(FromRadio(e.Source)||Unavailable||!e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)return;_armed=true;e.Pointer.Capture(this);e.Handled=true;}
    protected override void OnPointerReleased(PointerReleasedEventArgs e){base.OnPointerReleased(e);if(!_armed||e.InitialPressMouseButton!=MouseButton.Left)return;_armed=false;e.Pointer.Capture(null);if(!Unavailable&&new Rect(Bounds.Size).Contains(e.GetPosition(this))){Radio.FocusFromPointer();Radio.SetCurrentValue(WebRadio.IsCheckedProperty,true);}e.Handled=true;}
    protected override void OnPointerCaptureLost(PointerCaptureLostEventArgs e){_armed=false;base.OnPointerCaptureLost(e);}
    private readonly record struct Appearance(Color Background,Color Border,Color Shadow,double Spread);
    private Appearance Resolve(bool hover)
    {
        var bg=Unavailable?"#26272d":IsSelected?"#3b3d52":"#2b2d31";var border=Unavailable?"#535963":IsSelected?"#8b94ff":"#7c838c";
        if(hover&&!Unavailable){bg="#383b44";border="#a7b1ff";}
        return new(Color.Parse(bg),Color.Parse(border),IsSelected?Color.Parse("#8b94ff"):Colors.Transparent,IsSelected?1:0);
    }
    private void Paint(Appearance a)
    {
        _surface.Background=new SolidColorBrush(a.Background);_surface.BorderBrush=new SolidColorBrush(a.Border);_surface.BoxShadow=a.Spread==0&&a.Shadow.A==0?default:new(new BoxShadow{IsInset=true,Spread=a.Spread,Color=a.Shadow});
        NameText.Foreground=Brush.Parse(Unavailable?"#adb3bd":"#dbdee1");BadgeText.Foreground=Brush.Parse(Unavailable?"#adb3bd":"#b4bdff");
    }
    private void ApplyState(bool animate=true)
    {
        if(_row is null)return;var a=Resolve(IsPointerOver);var top=TopLevel.GetTopLevel(this);var animation=animate&&!_reduced&&top is not null;
        if(animation){_background.Set(a.Background);_border.Set(a.Border);_shadow.Set(a.Shadow);_spread.Set(a.Spread);}else{_background.Snap(a.Background);_border.Snap(a.Border);_shadow.Snap(a.Shadow);_spread.Snap(a.Spread);}
        void Current()=>Paint(new(_background.Current,_border.Current,_shadow.Current,_spread.Current));Current();var revision=++_revision;
        if(animation){void Frame(TimeSpan _){if(revision!=_revision||!this.IsAttachedToVisualTree())return;Current();if(_background.Running||_border.Running||_shadow.Running||_spread.Running)top!.RequestAnimationFrame(Frame);}top!.RequestAnimationFrame(Frame);}
    }
    private sealed class MatchRow:Panel
    {
        private readonly WebRadio _radio;
        public MatchBody Body{get;}=new();
        public WebSystemTextBlock Title=>Body.Title;
        public WebSystemTextBlock Note=>Body.Note;
        public WebSystemTextBlock Badge{get;}=WebTypography.CreateCandidate("",11,17.6,Brush.Parse("#b4bdff"),600);
        private double _badgeWidth,_badgeHeight;
        public MatchRow(WebRadio radio){_radio=radio;UseLayoutRounding=false;Children.Add(radio);Children.Add(Body);Children.Add(Badge);Badge.TextWrapping=TextWrapping.NoWrap;}
        protected override Size MeasureOverride(Size available)
        {var units=(TopLevel.GetTopLevel(this)?.RenderScaling??2)*64;Badge.LineHeight=Math.Floor(17.6*units)/units;Badge.Measure(Size.Infinity);_badgeWidth=string.IsNullOrEmpty(Badge.Text)?0:Math.Ceiling(Badge.DesiredSize.Width*units)/units;_badgeHeight=_badgeWidth==0?0:Badge.DesiredSize.Height;_radio.Measure(new(20,20));Body.Measure(new(Math.Max(0,available.Width-44-_badgeWidth),double.PositiveInfinity));return new(double.IsPositiveInfinity(available.Width)?Body.DesiredSize.Width+44+_badgeWidth:available.Width,Math.Max(20,Math.Max(Body.DesiredSize.Height,_badgeHeight)));}
        protected override Size ArrangeOverride(Size final)
        {var units=(TopLevel.GetTopLevel(this)?.RenderScaling??2)*64;double Center(double h)=>Math.Floor((final.Height-h)*units/2)/units;_radio.Arrange(new(0,Center(20),20,20));Body.Arrange(new(32,Center(Body.DesiredSize.Height),Math.Max(0,final.Width-44-_badgeWidth),Body.DesiredSize.Height));Badge.Arrange(new(final.Width-_badgeWidth,Center(_badgeHeight),_badgeWidth,_badgeHeight));return final;}
    }
    private sealed class MatchBody:Panel
    {
        public WebSystemTextBlock Title{get;}=WebTypography.CreateCandidate("",12,19.2,Brush.Parse("#dbdee1"),monospaced:true);
        public WebSystemTextBlock Note{get;}=WebTypography.CreateCandidate("",11,17.6,Brush.Parse("#b5bac1"));
        public MatchBody(){UseLayoutRounding=false;Title.TextWrapping=TextWrapping.Wrap;Children.Add(Title);Children.Add(Note);}
        protected override Size MeasureOverride(Size available){var units=(TopLevel.GetTopLevel(this)?.RenderScaling??2)*64;Title.LineHeight=Math.Floor(19.2*units)/units;Note.LineHeight=Math.Floor(17.6*units)/units;Title.Measure(available);Note.Measure(available);return new(double.IsPositiveInfinity(available.Width)?Math.Max(Title.DesiredSize.Width,Note.DesiredSize.Width):available.Width,Title.DesiredSize.Height+3+Note.DesiredSize.Height);}
        protected override Size ArrangeOverride(Size final){Title.Arrange(new(0,0,final.Width,Title.DesiredSize.Height));Note.Arrange(new(0,Title.DesiredSize.Height+3,final.Width,Note.DesiredSize.Height));return final;}
    }
    private sealed class MatchOutline:Decorator
    {private bool _visible;public bool Visible{get=>_visible;set{_visible=value;InvalidateVisual();}}public override void Render(DrawingContext c){if(_visible)c.DrawRectangle(null,new Pen(Brush.Parse("#a7b1ff"),3),new Rect(Bounds.Size).Inflate(3.5),11.5,11.5);}}
    private class Track<T>(Func<T,T,double,T> mix,Func<double>? clock=null)
    {
        private T _from=default!,_target=default!,_reverse=default!;private double _factor=1,_duration,_start;
        private double Now=>clock?.Invoke()??Stopwatch.GetTimestamp()*1000d/Stopwatch.Frequency;
        private double P=>_duration==0?1:Math.Clamp((Now-_start)/_duration,0,1);
        public double Duration=>_duration;public bool Running=>P<1;public T Current=>mix(_from,_target,WebMotion.CssEase().Ease(P));
        public void Snap(T v){_from=_target=_reverse=v;_duration=0;_factor=1;}
        public void Set(T v){if(EqualityComparer<T>.Default.Equals(v,_target))return;var p=P;var current=Current;if(p<1&&EqualityComparer<T>.Default.Equals(v,_reverse)){_factor=Math.Clamp(Math.Abs(WebMotion.CssEase().Ease(p)*_factor+1-_factor),0,1);_reverse=_target;}else{_factor=1;_reverse=current;}_from=current;_target=v;_duration=160*_factor;_start=Now;}
    }
    private sealed class ScalarTrack(Func<double>? clock=null):Track<double>((a,b,t)=>a+(b-a)*t,clock);
    private sealed class ColorTrack(Func<double>? clock=null)
    {
        private readonly Track<Rgba> _track=new(Rgba.Mix,clock);
        public bool Running=>_track.Running;public double Duration=>_track.Duration;public Color Current=>_track.Current.ToColor();
        public void Snap(Color c)=>_track.Snap(Rgba.From(c));public void Set(Color c)=>_track.Set(Rgba.From(c));
    }
    private readonly record struct Rgba(double A,double R,double G,double B)
    {
        public static Rgba From(Color c)=>new(c.A,c.R,c.G,c.B);
        public static Rgba Mix(Rgba a,Rgba b,double t){double alpha=a.A+(b.A-a.A)*t;double Channel(double x,double y)=>alpha==0?0:(x*a.A*(1-t)+y*b.A*t)/alpha;return new(alpha,Channel(a.R,b.R),Channel(a.G,b.G),Channel(a.B,b.B));}
        public Color ToColor(){static byte Byte(double x)=>(byte)Math.Clamp(Math.Round(x,MidpointRounding.AwayFromZero),0,255);return Color.FromArgb(Byte(A),Byte(R),Byte(G),Byte(B));}
    }
}
