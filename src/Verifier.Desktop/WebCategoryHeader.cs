using System.Diagnostics;
using Avalonia;
using Avalonia.Automation;
using Avalonia.Automation.Peers;
using Avalonia.Automation.Provider;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.VisualTree;

namespace Verifier.Desktop;

/// <summary>Original header: an expansion button and a separate checkbox label hit area.</summary>
public sealed class WebCategoryHeader : UserControl
{
    private readonly HeaderPanel _panel;
    private readonly Border _choice;
    private readonly Border _head;
    private readonly Track<Rgba> _selectedBackground=new(Rgba.Mix,160,WebMotion.CssEase().Ease);
    private bool _reduced,_armed;
    private long _revision;
    public WebCheckbox Checkbox {get;}=new();
    public CategoryButton ToggleButton {get;}=new();
    public event EventHandler? SelectionChanged;
    public event EventHandler? ExpandedChanged;
    public string SelectionLabel {get=>AutomationProperties.GetName(Checkbox)??"";set=>AutomationProperties.SetName(Checkbox,value);}
    public string Title {get=>ToggleButton.Row.Title.Text??"";set{ToggleButton.Row.Title.Text=value;UpdateName();}}
    public string Description {get=>ToggleButton.Row.Description.Text??"";set{ToggleButton.Row.Description.Text=value;UpdateName();}}
    public string IconName {get=>ToggleButton.Row.Icon.IconName;set=>ToggleButton.Row.Icon.IconName=value;}
    public string StatusText {get=>ToggleButton.Row.Pill.Text;set{ToggleButton.Row.Pill.Text=value;UpdateName();}}
    public string StatusKind {get=>ToggleButton.Row.Pill.Kind;set=>ToggleButton.Row.Pill.Kind=value;}
    public bool IsSelected {get=>Checkbox.IsChecked==true;set{Checkbox.IsChecked=value;PaintSelection();}}
    public bool IsExpanded {get=>ToggleButton.Expanded;set=>ToggleButton.Expanded=value;}
    public bool ReducedMotion {get=>_reduced;set{_reduced=value;Checkbox.ReducedMotion=value;ToggleButton.ReducedMotion=value;PaintSelection();}}
    public Border Choice=>_choice;
    public Border Head=>_head;
    public WebCategoryHeader()
    {
        UseLayoutRounding=false;ClipToBounds=false;Focusable=false;IsTabStop=false;
        Checkbox.HorizontalAlignment=HorizontalAlignment.Center;Checkbox.VerticalAlignment=VerticalAlignment.Center;
        _choice=new(){Width=44,Height=44,CornerRadius=new(7),Background=Brushes.Transparent,Child=Checkbox,UseLayoutRounding=false,Cursor=new Cursor(StandardCursorType.Hand)};
        _panel=new(ToggleButton,_choice);_head=new(){Child=_panel,UseLayoutRounding=false};Content=_head;
        ToggleButton.UserExpanded+=(_,_)=>ExpandedChanged?.Invoke(this,EventArgs.Empty);
        Checkbox.Click+=(_,_)=>SelectionChanged?.Invoke(this,EventArgs.Empty);
        Checkbox.PropertyChanged+=(_,e)=>{if(e.Property==Avalonia.Controls.Primitives.ToggleButton.IsCheckedProperty)PaintSelection();if(e.Property==WebCheckbox.FocusVisibleProperty)PaintChoice();};
        _choice.PointerEntered+=(_,_)=>PaintChoice();_choice.PointerExited+=(_,_)=>PaintChoice();
        _choice.PointerPressed+=(_,e)=>
        {
            if(!e.GetCurrentPoint(_choice).Properties.IsLeftButtonPressed||e.Source==Checkbox||IsCheckboxDescendant(e.Source))return;
            TopLevel.GetTopLevel(this)?.FocusManager?.Focus(null,NavigationMethod.Pointer);_armed=true;e.Pointer.Capture(_choice);e.Handled=true;
        };
        _choice.PointerReleased+=(_,e)=>
        {
            if(!_armed||e.InitialPressMouseButton!=MouseButton.Left)return;_armed=false;e.Pointer.Capture(null);e.Handled=true;
            if(new Rect(_choice.Bounds.Size).Contains(e.GetPosition(_choice))&&Checkbox.IsEffectivelyEnabled){Checkbox.Focus(NavigationMethod.Pointer);Checkbox.IsChecked=Checkbox.IsChecked!=true;SelectionChanged?.Invoke(this,EventArgs.Empty);}
        };
        _choice.PointerCaptureLost+=(_,_)=>_armed=false;PaintSelection();
    }
    private bool IsCheckboxDescendant(object? source)=>source is Visual v&&v.GetVisualAncestors().Contains(Checkbox);
    private void UpdateName(){AutomationProperties.SetName(ToggleButton,WebTextWhitespace.Collapse(Title+" "+Description+" "+StatusText));InvalidateMeasure();}
    private void PaintChoice()=>_choice.Background=Brush.Parse(_choice.IsPointerOver||Checkbox.FocusVisible?"#16a7b1ff":"#00000000");
    private void PaintSelection()
    {
        var value=Rgba.Parse(IsSelected?"#343640":"#00000000");var revision=++_revision;
        if(_reduced||TopLevel.GetTopLevel(this) is not{} top){_selectedBackground.Snap(value);_head.Background=new SolidColorBrush(value.Color);return;}
        _selectedBackground.SetTarget(value);void Frame(TimeSpan _){if(revision!=_revision||!this.IsAttachedToVisualTree())return;_head.Background=new SolidColorBrush(_selectedBackground.Current.Color);if(_selectedBackground.Running)top.RequestAnimationFrame(Frame);}top.RequestAnimationFrame(Frame);
    }
    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e){++_revision;base.OnDetachedFromVisualTree(e);}
    private sealed class HeaderPanel(CategoryButton button,Border choice):Panel
    {
        public HeaderPanel():this(new(),new()){}
        private bool _initialized;
        protected override Size MeasureOverride(Size available){if(!_initialized){Children.Add(button);Children.Add(choice);_initialized=true;}button.Measure(available);choice.Measure(new(44,44));return button.DesiredSize;}
        protected override Size ArrangeOverride(Size final){button.Arrange(new(default,final));choice.Arrange(new(6,(final.Height-44)/2,44,44));return final;}
    }
    public sealed class CategoryButton:Button
    {
        private readonly Border _background=new(){UseLayoutRounding=false};
        private readonly InsetFocus _focus;
        private readonly ScaleTransform _scale=new(1,1);
        private readonly RotateTransform _rotation=new();
        private readonly Track<Rgba> _hover=new(Rgba.Mix,120,WebMotion.SourceEase().Ease);
        private readonly Track<double> _press=new((a,b,t)=>a+(b-a)*t,80,WebMotion.SourceEase().Ease),_angle=new((a,b,t)=>a+(b-a)*t,200,WebMotion.SourceEase().Ease);
        private bool _expanded,_reduced;
        private long _revision;
        private WebMotionPlayback? _click;
        internal HeaderRow Row {get;}=new();
        internal event EventHandler? UserExpanded;
        public bool Expanded {get=>_expanded;set{_expanded=value;ApplyState();}}
        public bool ReducedMotion {get=>_reduced;set{_reduced=value;if(value){_click?.Dispose();_click=null;}ApplyState();}}
        public CategoryButton()
        {
            UseLayoutRounding=false;ClipToBounds=false;MinHeight=64;Padding=new(58,9,16,9);BorderThickness=new(0);FocusAdorner=null;HorizontalAlignment=HorizontalAlignment.Stretch;VerticalAlignment=VerticalAlignment.Top;
            _background.Padding=Padding;_background.Child=Row;_focus=new(){Child=_background,UseLayoutRounding=false};Template=new FuncControlTemplate<CategoryButton>((_,_)=>_focus);
            RenderTransform=_scale;RenderTransformOrigin=new RelativePoint(.5,.5,RelativeUnit.Relative);Row.Chevron.RenderTransform=_rotation;Row.Chevron.RenderTransformOrigin=new RelativePoint(.5,.5,RelativeUnit.Relative);ApplyState();
        }
        protected override AutomationPeer OnCreateAutomationPeer()=>new CategoryPeer(this);
        private sealed class CategoryPeer(CategoryButton owner):ButtonAutomationPeer(owner),IExpandCollapseProvider
        {
            public ExpandCollapseState ExpandCollapseState=>owner.Expanded?ExpandCollapseState.Expanded:ExpandCollapseState.Collapsed;
            public bool ShowsMenu=>false;
            public void Expand(){if(!owner.Expanded)owner.OnClick();}
            public void Collapse(){if(owner.Expanded)owner.OnClick();}
        }
        protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change){base.OnPropertyChanged(change);if(_focus is null)return;if(change.Property==IsPointerOverProperty||change.Property==IsPressedProperty||change.Property==IsEffectivelyEnabledProperty)ApplyState();}
        protected override void OnGotFocus(FocusChangedEventArgs e){base.OnGotFocus(e);_focus.Visible=e.NavigationMethod!=NavigationMethod.Pointer;}
        protected override void OnLostFocus(FocusChangedEventArgs e){base.OnLostFocus(e);_focus.Visible=false;}
        protected override void OnKeyDown(KeyEventArgs e){if(e.Key is Key.Enter or Key.Space)_focus.Visible=true;base.OnKeyDown(e);}
        protected override void OnClick(){if(!IsEffectivelyEnabled)return;if(!_reduced&&TopLevel.GetTopLevel(this) is not null){_click?.Dispose();_click=WebMotion.Play(this,WebMotionKind.Press);}Expanded=!Expanded;UserExpanded?.Invoke(this,EventArgs.Empty);base.OnClick();}
        private void ApplyState()
        {
            if(_focus is null)return;var bg=Rgba.Parse(!IsEffectivelyEnabled?"#3b3d44":IsPointerOver?"#3e414c":"#00000000");var scale=IsPressed&&IsEffectivelyEnabled?.98:1;var angle=Expanded?90:0;var revision=++_revision;
            Cursor=new Cursor(IsEffectivelyEnabled?StandardCursorType.Hand:StandardCursorType.No);
            void Paint(){_background.Background=new SolidColorBrush(_hover.Current.Color);_scale.ScaleX=_scale.ScaleY=_press.Current;_rotation.Angle=_angle.Current;}
            if(_reduced||TopLevel.GetTopLevel(this) is not{} top){_hover.Snap(bg);_press.Snap(scale);_angle.Snap(angle);Paint();return;}
            _hover.SetTarget(bg);_press.SetTarget(scale);_angle.SetTarget(angle);void Frame(TimeSpan _){if(revision!=_revision||!this.IsAttachedToVisualTree())return;Paint();if(_hover.Running||_press.Running||_angle.Running)top.RequestAnimationFrame(Frame);}top.RequestAnimationFrame(Frame);
        }
        protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e){++_revision;_click?.Dispose();_click=null;base.OnDetachedFromVisualTree(e);}
    }
    internal sealed class HeaderRow:Panel
    {
        public SvgIcon Icon {get;}=new(){Width=19,Height=19,Stroke=Brush.Parse("#b5bac1")};
        public Border IconBox {get;}
        public WebSystemTextBlock Title {get;}=WebTypography.CreateCandidate("",14,22,Brush.Parse("#f2f3f5"),700);
        public WebSystemTextBlock Description {get;}=WebTypography.CreateCandidate("",11,18,Brush.Parse("#b5bac1"));
        public WebStatusPill Pill {get;}=new(){CategoryContext=true};
        public SvgIcon Chevron {get;}=new(){IconName="chevron",Width=18,Height=18,Stroke=Brush.Parse("#b5bac1")};
        private double _textWidth,_textHeight,_pillWidth;
        public HeaderRow(){UseLayoutRounding=false;ClipToBounds=false;Icon.HorizontalAlignment=HorizontalAlignment.Center;Icon.VerticalAlignment=VerticalAlignment.Center;IconBox=new(){Width=34,Height=34,CornerRadius=new(8),Background=Brush.Parse("#353740"),Child=Icon,UseLayoutRounding=false};Children.Add(IconBox);Children.Add(Title);Children.Add(Description);Children.Add(Pill);Children.Add(Chevron);}
        protected override Size MeasureOverride(Size available)
        {
            IconBox.Measure(new(34,34));Pill.Measure(Size.Infinity);Chevron.Measure(new(18,18));
            var unit=(TopLevel.GetTopLevel(this)?.RenderScaling??2)*64;_pillWidth=Math.Ceiling(Pill.DesiredSize.Width*unit)/unit;_textWidth=Math.Max(0,Math.Floor((available.Width-34-36-_pillWidth-18)*unit)/unit);
            Title.Measure(new(_textWidth,double.PositiveInfinity));Description.Measure(new(_textWidth,double.PositiveInfinity));_textHeight=Title.DesiredSize.Height+1+Description.DesiredSize.Height;
            return new(34+36+_pillWidth+18+_textWidth,Math.Max(34,Math.Max(_textHeight,Pill.DesiredSize.Height)));
        }
        protected override Size ArrangeOverride(Size final)
        {
            var unit=(TopLevel.GetTopLevel(this)?.RenderScaling??2)*64;double Y(double h)=>Math.Floor((final.Height-h)*unit/2)/unit;
            IconBox.Arrange(new(0,Y(34),34,34));var y=Y(_textHeight);Title.Arrange(new(46,y,_textWidth,Title.DesiredSize.Height));Description.Arrange(new(46,y+Title.DesiredSize.Height+1,_textWidth,Description.DesiredSize.Height));
            var x=46+_textWidth+12;Pill.HorizontalAlignment=HorizontalAlignment.Stretch;Pill.Arrange(new(x,Y(Pill.DesiredSize.Height),_pillWidth,Pill.DesiredSize.Height));Chevron.Arrange(new(x+_pillWidth+12,Y(18),18,18));return final;
        }
    }
    private sealed class InsetFocus:Decorator
    {
        private bool _visible;
        public bool Visible{get=>_visible;set{_visible=value;InvalidateVisual();}}
        public override void Render(DrawingContext context){if(_visible)context.DrawRectangle(null,new Pen(Brush.Parse("#a7b1ff"),3),new Rect(Bounds.Size).Deflate(1.5));}
    }
    private readonly record struct Rgba(double A,double R,double G,double B)
    {
        public static Rgba Parse(string s){var c=Color.Parse(s);return new(c.A,c.R,c.G,c.B);}
        public Color Color=>Color.FromArgb(Byte(A),Byte(R),Byte(G),Byte(B));
        private static byte Byte(double v)=>(byte)Math.Clamp(Math.Round(v,MidpointRounding.AwayFromZero),0,255);
        public static Rgba Mix(Rgba a,Rgba b,double t){var alpha=a.A+(b.A-a.A)*t;double C(double x,double y)=>alpha==0?0:(x*a.A*(1-t)+y*b.A*t)/alpha;return new(alpha,C(a.R,b.R),C(a.G,b.G),C(a.B,b.B));}
    }
    private sealed class Track<T>(Func<T,T,double,T> mix,double duration,Func<double,double> easing,Func<double>? clock=null)
    {
        private T _from=default!,_target=default!,_reversing=default!;
        private double _duration,_started,_factor=1;
        private double Now=>clock?.Invoke()??Stopwatch.GetTimestamp()*(1000d/Stopwatch.Frequency);
        private double Progress=>_duration<=0?1:Math.Clamp((Now-_started)/_duration,0,1);
        public bool Running=>Progress<1;
        public T Current=>mix(_from,_target,easing(Progress));
        public void Snap(T value){_from=_target=_reversing=value;_duration=0;_factor=1;}
        public void SetTarget(T target){if(EqualityComparer<T>.Default.Equals(_target,target))return;var p=Progress;var value=mix(_from,_target,easing(p));if(p<1&&EqualityComparer<T>.Default.Equals(_reversing,target)){_factor=Math.Clamp(Math.Abs(easing(p)*_factor+1-_factor),0,1);_reversing=_target;}else{_factor=1;_reversing=value;}_from=value;_target=target;_duration=duration*_factor;_started=Now;}
    }
}
