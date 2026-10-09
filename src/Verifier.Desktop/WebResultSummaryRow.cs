using System.Diagnostics;
using System.Runtime.InteropServices;
using Avalonia;
using Avalonia.Automation;
using Avalonia.Automation.Peers;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.VisualTree;

namespace Verifier.Desktop;

/// <summary>Plain button.result-row. Parent supplies result data/axis and handles the detail request.</summary>
public sealed class WebResultSummaryRow:Button
{
    private readonly RowGrid _grid=new();
    private readonly WebSurfaceBox _surface=WebSurface.Create(Brushes.Transparent,Brush.Parse("#41434a"),new(0,0,0,1),default);
    private readonly RowFocus _focus;
    private readonly ScaleTransform _scale=new(1,1);
    private readonly Track<Color> _background=new(Mix,120);
    private readonly Track<double> _scaleTrack=new((a,b,t)=>a+(b-a)*t,80),_chevron=new((a,b,t)=>a+(b-a)*t,120);
    private WebMotionPlayback? _press;
    private long _revision;
    private bool _reduced;
    public WebResultCount[] Counts=>_grid.Counts;
    public WebSystemTextBlock Title=>_grid.Category.Title;
    public WebSystemTextBlock Description=>_grid.Category.Description;
    public SvgIcon CategoryIcon=>_grid.Category.Icon;
    public Control Category=>_grid.Category;
    public Control Chevron=>_grid.Chevron;
    public SvgIcon ChevronIcon=>_grid.Chevron.Icon;
    public bool FocusVisible=>_focus.Visible;
    public IBrush? RowBackground=>_surface.Background;
    public string CategoryKey {get;private set;}="";
    public bool Compact{get=>_grid.Compact;set{_grid.Compact=value;Padding=new(value?16:20,12);_surface.Padding=Padding;InvalidateMeasure();}}
    public bool Last{get=>_surface.BorderThickness.Bottom==0;set{_surface.BorderThickness=new(0,0,0,value?0:1);InvalidateMeasure();}}
    public bool ReducedMotion{get=>_reduced;set{_reduced=value;if(value){_press?.Dispose();_press=null;}ApplyState(false);}}
    public WebResultSummaryRow()
    {
        UseLayoutRounding=false;ClipToBounds=false;MinHeight=82;Padding=new(20,12);BorderThickness=default;
        HorizontalAlignment=HorizontalAlignment.Stretch;VerticalAlignment=VerticalAlignment.Top;FocusAdorner=null;
        _surface.Padding=Padding;_surface.Child=_grid;_focus=new(){Child=_surface,UseLayoutRounding=false};
        Template=new FuncControlTemplate<WebResultSummaryRow>((_,_)=>_focus);
        RenderTransform=_scale;RenderTransformOrigin=WebMotion.OriginFor(WebMotionKind.Press);ApplyState(false);
    }
    public void SetResult(string key,string title,string description,string icon,long different,long unknown,long equal,double maximum,bool english=false)
    {
        CategoryKey=key;Title.Text=title;Description.Text=description;CategoryIcon.IconName=icon;
        var values=new[]{different,unknown,equal};var kinds=new[]{"different","unknown","equal"};
        for(var i=0;i<3;i++)Counts[i].SetValue(values[i],maximum,kinds[i]);
        var labels=english?new[]{"Different","Unverified","Equal"}:new[]{"불일치","미검증","일치"};
        AutomationProperties.SetName(this,title+": "+string.Join(", ",values.Select((n,i)=>labels[i]+" "+n))+(english?". View details":". 세부 보기"));
        InvalidateMeasure();
    }
    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs e){base.OnPropertyChanged(e);if(_focus is null)return;if(e.Property==IsPointerOverProperty||e.Property==IsPressedProperty||e.Property==IsEffectivelyEnabledProperty)ApplyState();}
    protected override void OnPointerPressed(PointerPressedEventArgs e){if(e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)_focus.Visible=false;base.OnPointerPressed(e);}
    protected override void OnGotFocus(FocusChangedEventArgs e){base.OnGotFocus(e);_focus.Visible=e.NavigationMethod!=NavigationMethod.Pointer;}
    protected override void OnLostFocus(FocusChangedEventArgs e){base.OnLostFocus(e);_focus.Visible=false;}
    protected override void OnKeyDown(KeyEventArgs e){if(e.Key is Key.Space or Key.Enter)_focus.Visible=true;base.OnKeyDown(e);}
    protected override void OnClick(){if(!IsEffectivelyEnabled)return;if(!_reduced&&TopLevel.GetTopLevel(this) is not null){_press?.Dispose();_press=WebMotion.Play(this,WebMotionKind.Press);}base.OnClick();}
    private void ApplyState(bool animate=true)
    {
        var bg=Color.Parse(!IsEffectivelyEnabled?"#3b3d44":IsPointerOver?"#353740":"#00000000");var scale=IsPressed&&IsEffectivelyEnabled?.98:1;var chevron=IsPointerOver?2d:0;
        Cursor=new(IsEffectivelyEnabled?StandardCursorType.Hand:StandardCursorType.No);var revision=++_revision;
        if(!animate||_reduced||TopLevel.GetTopLevel(this) is not{} top){_background.Snap(bg);_scaleTrack.Snap(scale);_chevron.Snap(chevron);Paint();return;}
        _background.Set(bg);_scaleTrack.Set(scale);_chevron.Set(chevron);
        void Frame(TimeSpan _){if(revision!=_revision||!this.IsAttachedToVisualTree())return;Paint();if(_background.Running||_scaleTrack.Running||_chevron.Running)top.RequestAnimationFrame(Frame);}top.RequestAnimationFrame(Frame);
    }
    private void Paint(){_surface.Background=new SolidColorBrush(_background.Current);_scale.ScaleX=_scale.ScaleY=_scaleTrack.Current;Chevron.RenderTransform=new TranslateTransform(_chevron.Current,0);}
    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e){++_revision;_press?.Dispose();_press=null;base.OnDetachedFromVisualTree(e);}
    private sealed class RowFocus:Decorator
    {
        private bool _visible;public bool Visible{get=>_visible;set{_visible=value;InvalidateVisual();}}
        public override void Render(DrawingContext c){if(_visible)c.DrawRectangle(null,new Pen(Brush.Parse("#a7b1ff"),3),new Rect(Bounds.Size).Deflate(1.5));}
    }
    private sealed class CategoryPanel:Panel
    {
        public WebSystemTextBlock Title{get;}=WebTypography.CreateCandidate("",13,20.8,Brush.Parse("#f2f3f5"),700);
        public WebSystemTextBlock Description{get;}=WebTypography.CreateCandidate("",10,17,Brush.Parse("#b5bac1"));
        public SvgIcon Icon{get;}=new(){Width=19,Height=19,Stroke=Brush.Parse("#b5bac1")};
        private readonly WebSurfaceBox _iconBox=WebSurface.Create(Brushes.Transparent,Brushes.Transparent,default,new(8));
        private double _textWidth;
        public bool Compact{get;set;}
        private double TextLeft=>Compact?38:42;
        public Control IconBox=>_iconBox;
        public CategoryPanel(){UseLayoutRounding=false;_iconBox.Child=Icon;Icon.HorizontalAlignment=HorizontalAlignment.Center;Icon.VerticalAlignment=VerticalAlignment.Center;Children.Add(_iconBox);Children.Add(Title);Children.Add(Description);}
        protected override Size MeasureOverride(Size size)
        {
            var units=64*(TopLevel.GetTopLevel(this)?.RenderScaling??2);Title.LineHeight=Math.Floor(20.8*units)/units;
            Title.Measure(Size.Infinity);Description.Measure(Size.Infinity);
            _textWidth=Math.Min(Math.Max(0,size.Width-TextLeft),Math.Ceiling(Math.Max(Title.DesiredSize.Width,Description.DesiredSize.Width)*units)/units);
            Title.Measure(new(_textWidth,double.PositiveInfinity));Description.Measure(new(_textWidth,double.PositiveInfinity));_iconBox.Measure(new(30,30));
            return new(size.Width,Math.Max(30,Title.DesiredSize.Height+3+Description.DesiredSize.Height));
        }
        protected override Size ArrangeOverride(Size size){_iconBox.Arrange(new(0,(size.Height-30)/2,30,30));var y=(size.Height-Title.DesiredSize.Height-3-Description.DesiredSize.Height)/2;Title.Arrange(new(TextLeft,y,_textWidth,Title.DesiredSize.Height));Description.Arrange(new(TextLeft,y+Title.DesiredSize.Height+3,_textWidth,Description.DesiredSize.Height));return size;}
    }
    private sealed class ChevronPanel:Panel
    {
        private readonly WebSystemTextBlock _strut=WebTypography.CreateCandidate("M",14,22.4);
        public SvgIcon Icon{get;}=new(){IconName="chevron",Width=18,Height=18,Stroke=Brush.Parse("#b5bac1")};
        public ChevronPanel(){UseLayoutRounding=false;_strut.Opacity=0;_strut.IsHitTestVisible=false;AutomationProperties.SetAccessibilityView(_strut,AccessibilityView.Raw);Children.Add(_strut);Children.Add(Icon);}
        protected override Size MeasureOverride(Size size){var units=64*(TopLevel.GetTopLevel(this)?.RenderScaling??2);_strut.LineHeight=Math.Floor(22.4*units)/units;_strut.Measure(Size.Infinity);Icon.Measure(new(18,18));return new(18,_strut.DesiredSize.Height);}
        protected override Size ArrangeOverride(Size size){var units=64*(TopLevel.GetTopLevel(this)?.RenderScaling??2);var halfXHeight=Math.Floor(XHeight()/2*units+.5)/units;var y=Math.Floor((_strut.SystemBaseline-halfXHeight-9)*units)/units;Icon.Arrange(new(0,y,18,18));_strut.Arrange(new(0,0,0,0));return size;}
        protected override AutomationPeer OnCreateAutomationPeer()=>new MetricPeer(this);
        private sealed class MetricPeer(ChevronPanel owner):ControlAutomationPeer(owner)
        {protected override IReadOnlyList<AutomationPeer>? GetChildrenCore()=>null;}
        private static double XHeight()
        {
            if(OperatingSystem.IsMacOS()){var font=CTFontCreateUIFontForLanguage(2,14,IntPtr.Zero);try{return CTFontGetXHeight(font);}finally{CFRelease(font);}}
            using var face=SkiaSharp.SKTypeface.FromFamilyName(WebTypography.SystemFontFamily.Name);using var font2=new SkiaSharp.SKFont(face,14);return font2.Metrics.XHeight;
        }
        [DllImport("/System/Library/Frameworks/CoreText.framework/CoreText")]private static extern IntPtr CTFontCreateUIFontForLanguage(uint type,double size,IntPtr language);
        [DllImport("/System/Library/Frameworks/CoreText.framework/CoreText")]private static extern double CTFontGetXHeight(IntPtr font);
        [DllImport("/System/Library/Frameworks/CoreFoundation.framework/CoreFoundation")]private static extern void CFRelease(IntPtr obj);
    }
    private sealed class RowGrid:Panel
    {
        public CategoryPanel Category{get;}=new();public ChevronPanel Chevron{get;}=new();
        public WebResultCount[] Counts{get;}=[new(),new(),new()];private double[] _columns=[170,90,90,90,18];
        public bool Compact{get=>Category.Compact;set{Category.Compact=value;InvalidateMeasure();}}
        public RowGrid(){UseLayoutRounding=false;Children.Add(Category);foreach(var c in Counts)Children.Add(c);Children.Add(Chevron);}
        private void Columns(double width)
        {
            var units=64*(TopLevel.GetTopLevel(this)?.RenderScaling??2);var minimum=Compact?155:170;var numberMin=Compact?75:90;var gap=Compact?16:20;var tail=Compact?16:18;var space=Math.Max(minimum+numberMin*3,width-gap*4-tail);
            // Blink expands flexible sets in raw LayoutUnits with a float fr size and carried remainder.
            var fr=(float)(space*units)/4.4f;var frozen=fr*1.4f<minimum*units;
            if(frozen)fr=(float)((space-minimum)*units)/3;
            float leftover=0;
            for(var i=0;i<4;i++)
            {
                if(i==0&&frozen){_columns[0]=minimum;continue;}
                var share=fr*(i==0?1.4f:1)+leftover;var raw=Math.Floor(share+1e-6);
                _columns[i]=raw/units;leftover=(float)Math.Max(0,share-raw);
            }
            _columns[4]=tail;
        }
        protected override Size MeasureOverride(Size size){var width=double.IsFinite(size.Width)?size.Width:538;Columns(width);for(var i=0;i<5;i++)Children[i].Measure(new(_columns[i],double.PositiveInfinity));return new(width,Children.Max(x=>x.DesiredSize.Height));}
        protected override Size ArrangeOverride(Size size){Columns(size.Width);var units=64*(TopLevel.GetTopLevel(this)?.RenderScaling??2);double x=0;for(var i=0;i<5;i++){var c=Children[i];c.Arrange(new(x,Math.Floor((size.Height-c.DesiredSize.Height)*units/2)/units,_columns[i],c.DesiredSize.Height));x+=_columns[i]+(Compact?16:20);}return size;}
    }
    private static Color Mix(Color a,Color b,double t){var alpha=a.A+(b.A-a.A)*t;byte R(double n)=>(byte)Math.Clamp(Math.Round(n,MidpointRounding.AwayFromZero),0,255);byte C(byte x,byte y)=>alpha==0?(byte)0:R((x*a.A*(1-t)+y*b.A*t)/alpha);return Color.FromArgb(R(alpha),C(a.R,b.R),C(a.G,b.G),C(a.B,b.B));}
    private sealed class Track<T>(Func<T,T,double,T> mix,double duration,Func<double>? clock=null)
    {
        private T _from=default!,_target=default!,_reverse=default!;private double _start,_duration,_factor=1;private double Now=>clock?.Invoke()??Stopwatch.GetTimestamp()*1000d/Stopwatch.Frequency;private double P=>_duration<=0?1:Math.Clamp((Now-_start)/_duration,0,1);public bool Running=>P<1;public T Current=>mix(_from,_target,WebMotion.SourceEase().Ease(P));
        public void Snap(T target){_from=_target=_reverse=target;_duration=0;_factor=1;}
        public void Set(T target){if(EqualityComparer<T>.Default.Equals(target,_target))return;var p=P;var current=Current;if(p<1&&EqualityComparer<T>.Default.Equals(target,_reverse)){_factor=Math.Clamp(Math.Abs(WebMotion.SourceEase().Ease(p)*_factor+1-_factor),0,1);_reverse=_target;}else{_factor=1;_reverse=current;}_from=current;_target=target;_start=Now;_duration=duration*_factor;}
    }
}
