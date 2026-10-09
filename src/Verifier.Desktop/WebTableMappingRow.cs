using System.Diagnostics;
using Avalonia;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.VisualTree;

namespace Verifier.Desktop;

/// <summary>Original tableRows tr. The table parent supplies shared, auto-calculated column widths.</summary>
public sealed class WebTableMappingRow : UserControl
{
    private readonly RowPanel _row;
    private readonly WebSurfaceBox _surface;
    private bool _reduced, _armed, _hover;
    private long _revision;
    private readonly BackgroundTrack _background = new();
    public WebCheckbox Checkbox { get; } = new();
    public WebActionButton ChangeButton { get; } = new() { Variant="quiet",IconName="swap",LabelFontSize=12 };
    public WebSystemTextBlock SourceText { get; } = WebTypography.CreateCandidate("",12,19.2,monospaced:true,tabularNumbers:true);
    public WebSystemTextBlock TargetText { get; } = WebTypography.CreateCandidate("",12,19.2,monospaced:true,tabularNumbers:true);
    public WebSystemTextBlock MethodText { get; } = WebTypography.CreateCandidate("",12,19.2,Brush.Parse("#b5bac1"));
    public SvgIcon MethodIcon { get; } = new() {IconName="check",Width=14,Height=14,Stroke=Brush.Parse("#b5bac1")};
    public Border Choice { get; }
    public Panel Method { get; }
    public event EventHandler? SelectionChanged;
    public event EventHandler? ChangeRequested;
    public string SourceName {get=>SourceText.Text??"";set{SourceText.Text=value;AutomationProperties.SetName(Checkbox,value);InvalidateMeasure();}}
    public string TargetName {get=>TargetText.Text??"";set{TargetText.Text=value;InvalidateMeasure();}}
    public bool Selected {get=>Checkbox.IsChecked==true;set{Checkbox.IsChecked=value;Paint();}}
    public bool Manual {get=>MethodIcon.IconName=="swap";set{MethodIcon.IconName=value?"swap":"check";MethodText.Foreground=MethodIcon.Stroke=Brush.Parse(value?"#f0c47c":"#b5bac1");}}
    public bool ReducedMotion {get=>_reduced;set{_reduced=value;Checkbox.ReducedMotion=value;ChangeButton.ReducedMotion=value;Paint();}}
    public double[] ColumnWidths {get=>_row.Columns.ToArray();set{if(value.Length!=4||value.Any(x=>!double.IsFinite(x)||x<36))throw new ArgumentException("Four measured table column widths required");_row.Columns=value.ToArray();Width=value.Sum();_row.InvalidateMeasure();InvalidateMeasure();}}
    public double TopBorderShare {get=>_row.TopBorder;set{_row.TopBorder=value;_surface.BorderThickness=new(0,value,0,_row.BottomBorder);InvalidateMeasure();}}
    public double BottomBorderShare {get=>_row.BottomBorder;set{_row.BottomBorder=value;_surface.BorderThickness=new(0,_row.TopBorder,0,value);InvalidateMeasure();}}
    public IBrush? RowBackground=>_surface.Background;
    public bool PointerWithinRow=>_hover;
    public WebTableMappingRow()
    {
        UseLayoutRounding=false;Focusable=false;IsTabStop=false;ClipToBounds=false;
        Choice=new(){Background=Brushes.Transparent,UseLayoutRounding=false,Cursor=new Cursor(StandardCursorType.Arrow)};
        Choice.Child=new ChoicePanel(Checkbox,SourceText);Method=new MethodPanel(MethodIcon,MethodText);
        _row=new(Choice,TargetText,Method,ChangeButton);_surface=WebSurface.Create(Brushes.Transparent,Brush.Parse("#41434a"),new(0,.5,0,.5),new(0));_surface.Child=_row;Content=_surface;
        Checkbox.Click+=(_,_)=>SelectionChanged?.Invoke(this,EventArgs.Empty);
        Checkbox.PropertyChanged+=(_,e)=>{if(e.Property==Avalonia.Controls.Primitives.ToggleButton.IsCheckedProperty)Paint();};
        ChangeButton.Click+=(_,_)=>ChangeRequested?.Invoke(this,EventArgs.Empty);
        // HTML non-focusable cells blur a focused input; Avalonia needs the parent to do so.
        AddHandler(InputElement.PointerPressedEvent,(_,e)=>{
            if(!e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)return;
            var v=e.Source as Visual;
            if(v==Checkbox||v==ChangeButton||v?.GetVisualAncestors().Any(x=>x==Checkbox||x==ChangeButton)==true)return;
            TopLevel.GetTopLevel(this)?.FocusManager?.Focus(null,NavigationMethod.Pointer);
        },RoutingStrategies.Tunnel,true);
        // Child pointer capture must not clear DOM-like row :hover while still inside.
        void TrackPointer(PointerEventArgs e){var inside=new Rect(Bounds.Size).Contains(e.GetPosition(this));if(inside==_hover)return;_hover=inside;Paint();}
        PointerEntered+=(_,e)=>TrackPointer(e);
        PointerExited+=(_,e)=>{
            var captured=e.Pointer.Captured as Visual;
            if(_armed||captured==this||captured?.GetVisualAncestors().Contains(this)==true){TrackPointer(e);return;}
            // Windows leave events can expose the previous position; an uncaptured exit is definitive.
            if(!_hover)return;_hover=false;Paint();
        };
        AddHandler(InputElement.PointerMovedEvent,(_,e)=>TrackPointer(e),RoutingStrategies.Tunnel,true);
        Choice.PointerPressed+=(_,e)=>{if(!e.GetCurrentPoint(Choice).Properties.IsLeftButtonPressed||e.Source==Checkbox||e.Source is Visual v&&v.GetVisualAncestors().Contains(Checkbox))return;TopLevel.GetTopLevel(this)?.FocusManager?.Focus(null,NavigationMethod.Pointer);_armed=true;e.Pointer.Capture(Choice);e.Handled=true;};
        Choice.PointerReleased+=(_,e)=>{if(!_armed||e.InitialPressMouseButton!=MouseButton.Left)return;_armed=false;e.Pointer.Capture(null);e.Handled=true;if(new Rect(Choice.Bounds.Size).Contains(e.GetPosition(Choice))&&Checkbox.IsEffectivelyEnabled){Checkbox.Focus(NavigationMethod.Pointer);Checkbox.IsChecked=Checkbox.IsChecked!=true;SelectionChanged?.Invoke(this,EventArgs.Empty);}};
        Choice.PointerCaptureLost+=(_,_)=>_armed=false;Paint();
    }
    private Color Resolve(bool hover)=>Color.Parse(Selected?"#343640":hover?"#3a3c44":"#00000000");
    private void Paint()
    {
        var target=Resolve(_hover);var revision=++_revision;
        if(_reduced||TopLevel.GetTopLevel(this) is not{} top){_background.Snap(target);_surface.Background=new SolidColorBrush(target);return;}
        _background.Set(target);void Frame(TimeSpan _){if(revision!=_revision||!this.IsAttachedToVisualTree())return;_surface.Background=new SolidColorBrush(_background.Current);if(_background.Running)top.RequestAnimationFrame(Frame);}top.RequestAnimationFrame(Frame);
    }
    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e){++_revision;base.OnDetachedFromVisualTree(e);}
    private sealed class ChoicePanel(WebCheckbox check,WebSystemTextBlock text):Panel
    {
        private bool _added;private double _textWidth;
        protected override Size MeasureOverride(Size size){if(!_added){Children.Add(check);Children.Add(text);_added=true;}var units=64*(TopLevel.GetTopLevel(this)?.RenderScaling??2);text.LineHeight=Math.Floor(text.FontSize*1.6*units)/units;check.Measure(new(20,20));text.Measure(Size.Infinity);_textWidth=Math.Min(Math.Ceiling(text.DesiredSize.Width*units)/units,Math.Max(0,size.Width-32));text.Measure(new(_textWidth,double.PositiveInfinity));return new(size.Width,Math.Max(44,text.DesiredSize.Height));}
        protected override Size ArrangeOverride(Size size){var units=64*(TopLevel.GetTopLevel(this)?.RenderScaling??2);double Y(double h)=>Math.Floor((size.Height-h)*units/2)/units;check.Arrange(new(0,Y(20),20,20));text.Arrange(new(32,Y(text.DesiredSize.Height),_textWidth,text.DesiredSize.Height));return size;}
    }
    private sealed class MethodPanel(SvgIcon icon,WebSystemTextBlock text):Panel
    {
        private bool _added;private double _iconWidth,_textWidth;
        protected override Size MeasureOverride(Size size){if(!_added){Children.Add(icon);Children.Add(text);_added=true;}var units=64*(TopLevel.GetTopLevel(this)?.RenderScaling??2);text.LineHeight=Math.Floor(text.FontSize*1.6*units)/units;text.Measure(Size.Infinity);var intrinsic=Math.Ceiling(text.DesiredSize.Width*units)/units;_iconWidth=14;_textWidth=intrinsic;if(intrinsic+20>size.Width){_iconWidth=Math.Floor(14*Math.Max(0,size.Width-6)/(14+intrinsic)*units+.5)/units;_textWidth=Math.Max(0,size.Width-6-_iconWidth);}icon.Width=_iconWidth;icon.Measure(new(_iconWidth,14));text.Measure(new(_textWidth,double.PositiveInfinity));return new(size.Width,Math.Max(14,text.DesiredSize.Height));}
        protected override Size ArrangeOverride(Size size){var units=64*(TopLevel.GetTopLevel(this)?.RenderScaling??2);double Y(double h)=>Math.Floor((size.Height-h)*units/2)/units;icon.Arrange(new(0,Y(14),_iconWidth,14));text.Arrange(new(_iconWidth+6,Y(text.DesiredSize.Height),_textWidth,text.DesiredSize.Height));return size;}
    }
    private sealed class RowPanel(Border choice,WebSystemTextBlock target,Panel method,WebActionButton button):Panel
    {
        public double[] Columns=[298.046875,243.96875,170.5,239.484375];
        public double TopBorder=.5,BottomBorder=.5;
        private bool _added;
        protected override Size MeasureOverride(Size size){if(!_added){Children.Add(choice);Children.Add(target);Children.Add(method);Children.Add(button);_added=true;}var units=64*(TopLevel.GetTopLevel(this)?.RenderScaling??2);target.LineHeight=Math.Floor(target.FontSize*1.6*units)/units;var items=new Control[]{choice,target,method,button};for(var i=0;i<4;i++)items[i].Measure(new(Math.Max(0,Columns[i]-36),double.PositiveInfinity));return new(Columns.Sum(),items.Max(x=>x.DesiredSize.Height)+24);}
        protected override Size ArrangeOverride(Size size){double x=0;var items=new Control[]{choice,target,method,button};var h=size.Height-24;var units=64*(TopLevel.GetTopLevel(this)?.RenderScaling??2);for(var i=0;i<4;i++){var e=items[i];var width=i==3?e.DesiredSize.Width:Math.Max(0,Columns[i]-36);e.Arrange(new(x+18,12+Math.Floor((h-e.DesiredSize.Height)*units/2)/units,width,e.DesiredSize.Height));x+=Columns[i];}return size;}
    }
    private sealed class BackgroundTrack(Func<double>? clock=null)
    {
        private double Now=>clock?.Invoke()??Stopwatch.GetTimestamp()*1000d/Stopwatch.Frequency;
        private Color _from,_target,_reverse;
        private double _started,_duration,_factor=1;
        private double P=>_duration<=0?1:Math.Clamp((Now-_started)/_duration,0,1);
        private static double Ease(double p)=>WebMotion.CssEase().Ease(p);
        public bool Running=>P<1;
        public Color Current=>Mix(_from,_target,Ease(P));
        public void Snap(Color value){_from=_target=_reverse=value;_duration=0;_factor=1;}
        public void Set(Color value){if(value==_target)return;var p=P;var current=Current;if(p<1&&value==_reverse){_factor=Math.Clamp(Math.Abs(Ease(p)*_factor+1-_factor),0,1);_reverse=_target;}else{_factor=1;_reverse=current;}_from=current;_target=value;_started=Now;_duration=160*_factor;}
        private static Color Mix(Color a,Color b,double t){double alpha=a.A+(b.A-a.A)*t;byte Round(double x)=>(byte)Math.Clamp(Math.Round(x,MidpointRounding.AwayFromZero),0,255);byte C(byte x,byte y)=>alpha==0?(byte)0:Round((x*a.A*(1-t)+y*b.A*t)/alpha);return Color.FromArgb(Round(alpha),C(a.R,b.R),C(a.G,b.G),C(a.B,b.B));}
    }
}
