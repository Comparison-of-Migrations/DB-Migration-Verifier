using Avalonia;
using Avalonia.Automation;
using Avalonia.Automation.Peers;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;

namespace Verifier.Desktop;

/// <summary>Original .notice: passive SVG and inline-title/paragraph content.</summary>
public sealed class WebNotice : UserControl
{
    private readonly WebSurfaceBox _surface;
    private readonly SvgIcon _icon;
    private readonly NoticeText _body;
    private WebMotionPlayback? _entry;
    private string _title="",_text="",_kind="";
    public string Title {get=>_title;set{_title=value??"";Apply();}}
    public string Text {get=>_text;set{_text=value??"";Apply();}}
    public string Kind {get=>_kind;set{_kind=value??"";Apply();}}
    public bool ReducedMotion {get;set;}
    public WebNotice()
    {
        UseLayoutRounding=false;Focusable=false;IsTabStop=false;Margin=new(0,20,0,0);
        _icon=new(){Width=18,Height=18,UseLayoutRounding=false,IsHitTestVisible=false};
        _body=new(){UseLayoutRounding=false};
        var row=new NoticeRow(_icon,_body){UseLayoutRounding=false};
        _surface=WebSurface.Create(Brushes.Transparent,Brushes.Transparent,new(1),new(8));
        _surface.Padding=new(16,14);_surface.Child=row;Content=_surface;Apply();
    }
    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {base.OnAttachedToVisualTree(e);_entry?.Dispose();_entry=WebMotion.Play(this,WebMotionKind.Page,ReducedMotion);}
    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {_entry?.Dispose();_entry=null;base.OnDetachedFromVisualTree(e);}
    protected override AutomationPeer OnCreateAutomationPeer()=>new NoticePeer(this);
    private sealed class NoticePeer(WebNotice owner):ControlAutomationPeer(owner)
    {
        protected override AutomationControlType GetAutomationControlTypeCore()=>AutomationControlType.Group;
        protected override string GetClassNameCore()=>owner.Kind=="error"?"Alert":"Note";
    }
    private void Apply()
    {
        var (bg,border,ink)=_kind switch{
            "warn"=>("#39332b","#6b573b","#f0c47c"),"error"=>("#3c2a31","#73515b","#ff959d"),
            "ok"=>("#263d33","#3d6350","#7ee4b0"),_=>("#2b2d31","#41434a","#b5bac1")};
        _surface.Background=Brush.Parse(bg);_surface.BorderBrush=Brush.Parse(border);
        _icon.IconName=_kind is "warn" or "error"?"warning":"info";_icon.Stroke=Brush.Parse(ink);
        _body.Set(_title,_text,Brush.Parse(ink));
        AutomationProperties.SetName(this,WebTextWhitespace.Collapse(_title+" "+_text));
        AutomationProperties.SetLiveSetting(this,_kind=="error"?AutomationLiveSetting.Assertive:AutomationLiveSetting.Off);
    }
    private sealed class NoticeRow : Panel
    {
        private readonly SvgIcon _icon;private readonly NoticeText _body;
        public NoticeRow(SvgIcon icon,NoticeText body){_icon=icon;_body=body;Children.Add(icon);Children.Add(body);}
        protected override Size MeasureOverride(Size available)
        {_icon.Measure(new(18,18));_body.Measure(new(Math.Max(0,available.Width-30),available.Height));return new(30+_body.DesiredSize.Width,Math.Max(18,_body.DesiredSize.Height));}
        protected override Size ArrangeOverride(Size final)
        {
            var units=(TopLevel.GetTopLevel(this)?.RenderScaling??2)*64;
            double Center(double height)=>Math.Floor((final.Height-height)*units/2)/units;
            _icon.Arrange(new(0,Center(18),18,18));_body.Arrange(new(30,Center(_body.DesiredSize.Height),_body.ContentWidth,_body.DesiredSize.Height));return final;
        }
    }
    private sealed class NoticeText : Panel
    {
        private readonly WebSystemTextBlock _title=WebTypography.CreateCandidate("",13,22.4,Brush.Parse("#f2f3f5"),600);
        private readonly WebSystemTextBlock _text=WebTypography.CreateCandidate("",12,19.2);
        private readonly WebSystemTextBlock _strut=WebTypography.CreateCandidate("M",14,22.4);
        public double ContentWidth {get;private set;}
        public NoticeText()
        {
            Children.Add(_title);Children.Add(_text);Children.Add(_strut);
            _strut.Opacity=0;_strut.IsHitTestVisible=false;
            AutomationProperties.SetAccessibilityView(_strut,AccessibilityView.Raw);
        }
        public void Set(string title,string text,IBrush ink){_title.Text=title;_text.Text=text;_text.Foreground=ink;InvalidateMeasure();}
        protected override Size MeasureOverride(Size available)
        {
            var units=(TopLevel.GetTopLevel(this)?.RenderScaling??2)*64;
            _title.LineHeight=_strut.LineHeight=Math.Floor(14*1.6*units)/units;
            _text.LineHeight=Math.Floor(12*1.6*units)/units;
            _strut.Measure(Size.Infinity);_title.Measure(Size.Infinity);_text.Measure(Size.Infinity);
            var maximum=Math.Ceiling(Math.Max(_title.DesiredSize.Width,_text.DesiredSize.Width)*units)/units;
            _title.Measure(new(1/units,double.PositiveInfinity));_text.Measure(new(1/units,double.PositiveInfinity));
            var minimum=Math.Ceiling(Math.Max(_title.SystemLineWidths.DefaultIfEmpty().Max(),_text.SystemLineWidths.DefaultIfEmpty().Max())*units)/units;
            var width=ContentWidth=Math.Max(minimum,Math.Min(available.Width,maximum));
            _title.Measure(new(width,double.PositiveInfinity));_text.Measure(new(width,double.PositiveInfinity));
            _title.RenderTransform=new TranslateTransform(0,_strut.SystemBaseline-_title.SystemBaseline);
            return new(width,_title.DesiredSize.Height+_text.DesiredSize.Height);
        }
        protected override Size ArrangeOverride(Size final)
        {_title.Arrange(new(0,0,final.Width,_title.DesiredSize.Height));_text.Arrange(new(0,_title.DesiredSize.Height,final.Width,_text.DesiredSize.Height));_strut.Arrange(new(0,0,0,0));return final;}
    }
}
