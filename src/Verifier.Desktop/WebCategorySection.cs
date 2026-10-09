using System.Diagnostics;
using Avalonia;
using Avalonia.Automation.Peers;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.VisualTree;

namespace Verifier.Desktop;

/// <summary>Original category border, list and grid-row expansion around the locked header.</summary>
public sealed class WebCategorySection:Panel
{
    private readonly WebSurfaceBox _surface=WebSurface.Create(Brush.Parse("#2b2d31"),Brush.Parse("#41434a"),new(1),new(10));
    private readonly SectionFlow _flow;
    private readonly ScalarTrack _open=new(200,WebMotion.SourceEase().Ease);
    private readonly ScalarTrack _visibility=new(200,WebMotion.CssEase().Ease);
    private readonly ScalarTrack _selected=new(160,WebMotion.CssEase().Ease);
    private bool _expanded,_selection,_reduced;
    private long _revision;
    public WebCategoryHeader Header{get;}=new();
    public DetailBody Body{get;}
    public double ExpansionProgress=>_open.Current;
    public bool BodyVisible=>_visibility.Current>0;
    public bool IsExpanded{get=>_expanded;set{_expanded=value;Header.IsExpanded=value;UpdateState();}}
    public bool IsSelected{get=>_selection;set{_selection=value;Header.IsSelected=value;UpdateState();}}
    public bool ReducedMotion{get=>_reduced;set{_reduced=value;Header.ReducedMotion=value;UpdateState();}}
    public event EventHandler<bool>? SelectionRequested;
    public event EventHandler<bool>? ExpansionRequested;
    public WebCategorySection()
    {
        UseLayoutRounding=false;ClipToBounds=false;Margin=new(0,0,0,12);Body=new(this);_flow=new(this);_surface.Child=_flow;Children.Add(_surface);
        Header.SelectionChanged+=(_,_)=>{_selection=Header.IsSelected;UpdateState();SelectionRequested?.Invoke(this,_selection);};
        Header.ExpandedChanged+=(_,_)=>{_expanded=Header.IsExpanded;UpdateState();ExpansionRequested?.Invoke(this,_expanded);};
        UpdateState();
    }
    public void SetDetails(IEnumerable<string> details)=>Body.SetDetails(details);
    private void UpdateState()
    {
        var revision=++_revision;Body.Open=_expanded;
        if(_reduced||TopLevel.GetTopLevel(this) is null){_open.Snap(_expanded?1:0);_visibility.Snap(_expanded?1:0);_selected.Snap(_selection?1:0);}
        else{_open.SetTarget(_expanded?1:0);_visibility.SetTarget(_expanded?1:0);_selected.SetTarget(_selection?1:0);}
        // CSS inset/outset shadow lists cannot interpolate with each other.
        _surface.BoxShadow=_selection?new(new BoxShadow{OffsetX=3,Color=Color.Parse("#8b94ff"),IsInset=true}):new(new BoxShadow{OffsetY=1,Blur=2,Color=Color.FromArgb(5,0,0,0)});
        Paint();
        if(TopLevel.GetTopLevel(this) is{} top)
        {void Frame(TimeSpan _){if(revision!=_revision||!this.IsAttachedToVisualTree())return;Paint();if(_open.Running||_visibility.Running||_selected.Running)top.RequestAnimationFrame(Frame);}if(_open.Running||_visibility.Running||_selected.Running)top.RequestAnimationFrame(Frame);}
    }
    private void Paint()
    {double t=_selected.Current;byte Mix(byte a,byte b)=>(byte)Math.Round(a+(b-a)*t,MidpointRounding.AwayFromZero);_surface.BorderBrush=new SolidColorBrush(Color.FromRgb(Mix(65,115),Mix(67,124),Mix(74,184)));Body.Opacity=BodyVisible?1:0;_flow.InvalidateMeasure();InvalidateMeasure();}
    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e){++_revision;base.OnDetachedFromVisualTree(e);}
    protected override Size MeasureOverride(Size available){_surface.Measure(available);return _surface.DesiredSize;}
    protected override Size ArrangeOverride(Size final){_surface.Arrange(new Rect(final));_flow.Clip=new RectangleGeometry(new Rect(_flow.Bounds.Size),9,9);return final;}
    private sealed class SectionFlow(WebCategorySection owner):Panel
    {
        private bool _initialized;
        protected override Size MeasureOverride(Size available)
        {
            UseLayoutRounding=false;if(!_initialized){Children.Add(owner.Header);Children.Add(owner.Body);_initialized=true;}
            owner.Header.Measure(new(available.Width,double.PositiveInfinity));owner.Body.Measure(new(available.Width,double.PositiveInfinity));
            var units=(TopLevel.GetTopLevel(this)?.RenderScaling??2)*64;var h=Math.Floor(owner.Body.DesiredSize.Height*owner._open.Current*units)/units;
            return new(available.Width,owner.Header.DesiredSize.Height+h);
        }
        protected override Size ArrangeOverride(Size final)
        {var h=owner.Header.DesiredSize.Height;owner.Header.Arrange(new(0,0,final.Width,h));owner.Body.Arrange(new(0,h,final.Width,Math.Max(0,final.Height-h)));return final;}
    }
    public sealed class DetailBody:Panel
    {
        private readonly WebCategorySection _owner;
        private readonly WebSurfaceBox _background=WebSurface.Create(Brush.Parse("#2b2d31"),Brushes.Transparent,default,default);
        private readonly List<DetailItem> _items=[];
        private bool _isOpen;
        private BodyPeer? _peer;
        public bool Open{get=>_isOpen;set{if(_isOpen==value)return;_isOpen=value;_peer?.Refresh();}}
        public IReadOnlyList<DetailItem> Items=>_items;
        public double NaturalHeight{get;private set;}
        internal DetailBody(WebCategorySection owner){_owner=owner;UseLayoutRounding=false;ClipToBounds=true;IsHitTestVisible=false;Children.Add(_background);}
        public void SetDetails(IEnumerable<string> details){foreach(var c in _items)Children.Remove(c);_items.Clear();foreach(var text in details){var c=new DetailItem(text);_items.Add(c);Children.Add(c);}InvalidateMeasure();}
        protected override Size MeasureOverride(Size available)
        {double h=28;foreach(var item in _items){item.Measure(new(Math.Max(0,available.Width-102),double.PositiveInfinity));h+=item.DesiredSize.Height;}NaturalHeight=h;return new(available.Width,h);}
        protected override Size ArrangeOverride(Size final)
        {
            _background.BoxShadow=Open?new(new BoxShadow{OffsetY=1,Color=Color.Parse("#41434a"),IsInset=true}):default;_background.Arrange(new Rect(final));
            double y=12;foreach(var c in _items){c.Arrange(new(82,y,Math.Max(0,final.Width-102),c.DesiredSize.Height));y+=c.DesiredSize.Height;}return final;
        }
        protected override AutomationPeer OnCreateAutomationPeer()=>_peer=new BodyPeer(this);
        private sealed class BodyPeer(DetailBody body):ControlAutomationPeer(body)
        {public void Refresh(){InvalidateChildren();}protected override AutomationControlType GetAutomationControlTypeCore()=>AutomationControlType.List;protected override IReadOnlyList<AutomationPeer>? GetChildrenCore()=>body.Open?base.GetChildrenCore():Array.Empty<AutomationPeer>();protected override bool IsControlElementCore()=>body.Open;protected override bool IsContentElementCore()=>body.Open;}
    }
    public sealed class DetailItem:Panel
    {
        public WebSystemTextBlock Text{get;}
        private readonly WebSystemTextBlock _marker=WebTypography.CreateCandidate("•",13,24.7,Brush.Parse("#b5bac1"));
        public DetailItem(string text){UseLayoutRounding=false;ClipToBounds=false;Text=WebTypography.CreateCandidate(text,13,24.7,Brush.Parse("#b5bac1"));Children.Add(Text);Children.Add(_marker);}
        protected override Size MeasureOverride(Size available)
        {var units=(TopLevel.GetTopLevel(this)?.RenderScaling??2)*64;Text.LineHeight=_marker.LineHeight=Math.Floor(24.7*units)/units;Text.Measure(available);_marker.Measure(Size.Infinity);return new(available.Width,Text.DesiredSize.Height);}
        protected override Size ArrangeOverride(Size final)
        {Text.Arrange(new Rect(final));_marker.Arrange(new(-7-_marker.DesiredSize.Width,0,_marker.DesiredSize.Width,_marker.DesiredSize.Height));return final;}
        protected override AutomationPeer OnCreateAutomationPeer()=>new ItemPeer(this);
        private sealed class ItemPeer(DetailItem item):ControlAutomationPeer(item)
        {protected override AutomationControlType GetAutomationControlTypeCore()=>AutomationControlType.ListItem;}
    }
    private sealed class ScalarTrack(double duration,Func<double,double> easing,Func<double>? clock=null)
    {
        private double _from,_target,_reversing,_duration,_started,_factor=1;
        private double Now=>clock?.Invoke()??Stopwatch.GetTimestamp()*(1000d/Stopwatch.Frequency);
        private double Progress=>_duration<=0?1:Math.Clamp((Now-_started)/_duration,0,1);
        public bool Running=>Progress<1;
        public double Current=>_from+(_target-_from)*easing(Progress);
        public void Snap(double value){_from=_target=_reversing=value;_duration=0;_factor=1;}
        public void SetTarget(double target){if(_target==target)return;var p=Progress;var value=Current;if(p<1&&_reversing==target){_factor=Math.Clamp(Math.Abs(easing(p)*_factor+1-_factor),0,1);_reversing=_target;}else{_factor=1;_reversing=value;}_from=value;_target=target;_duration=duration*_factor;_started=Now;}
    }
}
