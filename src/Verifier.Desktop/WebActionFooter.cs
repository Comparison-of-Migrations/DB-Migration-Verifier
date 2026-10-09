using Avalonia;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Media;

namespace Verifier.Desktop;

/// <summary>Original .footer layout. Pages supply their branch's hint and permitted actions.</summary>
public sealed class WebActionFooter : Panel
{
    private readonly WebSurfaceBox _surface=WebSurface.Create(Brush.Parse("#2b2d31"),Brush.Parse("#41434a"),new(0,1,0,0),default,
        new(new BoxShadow{OffsetY=-4,Blur=16,Color=Color.FromArgb(24,0,0,0)}));
    private readonly FooterFlex _row=new();
    public WebSystemTextBlock Hint => _row.Hint;
    public WebActionButton? BackButton => _row.Back;
    public IReadOnlyList<WebActionButton> ActionButtons => _row.Actions.Buttons;
    public Control? ActionsBox => _row.Actions.Buttons.Count==0?null:_row.Actions;
    public event EventHandler<string>? ActionRequested;
    public double ShellViewportWidth {get; private set;}=1280;
    public WebActionFooter()
    {
        UseLayoutRounding=false;ClipToBounds=false;MinHeight=84;
        _surface.Child=_row;Children.Add(_surface);SetViewportWidth(1280);
    }
    public void SetViewportWidth(double width)
    {
        ShellViewportWidth=width;
        _surface.Padding=new(width<=1180?28:40,16);
        InvalidateMeasure();
    }
    public void SetContent(string hint,WebFooterAction? back,IReadOnlyList<WebFooterAction> actions,bool reducedMotion=false)
    {
        _row.Children.Clear();_row.Back=null;_row.Actions.SetButtons(Array.Empty<WebActionButton>());
        _row.Hint.Text=hint;
        WebActionButton Create(WebFooterAction action)
        {
            var b=new WebActionButton{Variant=action.Variant,Content=action.Label,IconName=action.IconName,
                IsEnabled=action.Enabled,IsLoading=action.Loading,ReducedMotion=reducedMotion};
            AutomationProperties.SetAutomationId(b,"Footer-"+action.Action);
            b.Click+=(_,_)=>{if(b.IsEffectivelyEnabled)ActionRequested?.Invoke(this,action.Action);};return b;
        }
        if(back is not null){_row.Back=Create(back);_row.Children.Add(_row.Back);}
        _row.Children.Add(_row.Hint);
        if(actions.Count>0){_row.Actions.SetButtons(actions.Select(Create).ToArray());_row.Children.Add(_row.Actions);}
        InvalidateMeasure();
    }
    protected override Size MeasureOverride(Size availableSize)
    {
        var units=(TopLevel.GetTopLevel(this)?.RenderScaling??2)*64;
        _row.Hint.LineHeight=Math.Floor(12*1.6*units)/units;
        _surface.Measure(availableSize);return _surface.DesiredSize;
    }
    protected override Size ArrangeOverride(Size finalSize){_surface.Arrange(new Rect(finalSize));return finalSize;}
    private sealed class FooterFlex : Panel
    {
        public WebSystemTextBlock Hint{get;}=WebTypography.CreateCandidate("",12,19.2,Brush.Parse("#b5bac1"));
        public WebActionButton? Back;
        public FooterActions Actions{get;}=new();
        private double _backWidth,_actionWidth,_hintWidth;
        public FooterFlex(){UseLayoutRounding=false;ClipToBounds=false;Children.Add(Hint);}
        protected override Size MeasureOverride(Size availableSize)
        {
            Back?.Measure(Size.Infinity);Actions.Measure(Size.Infinity);
            _backWidth=Back?.DesiredSize.Width??0;_actionWidth=Actions.DesiredSize.Width;
            var gaps=(Back is null?0:12)+(Actions.Buttons.Count==0?0:12);
            var finite=double.IsFinite(availableSize.Width);
            _hintWidth=finite?Math.Max(0,availableSize.Width-_backWidth-_actionWidth-gaps):double.PositiveInfinity;
            Hint.Measure(new(_hintWidth,double.PositiveInfinity));
            if(!finite)_hintWidth=Hint.DesiredSize.Width;
            return new(finite?availableSize.Width:_backWidth+_actionWidth+gaps+_hintWidth,Math.Max(Hint.DesiredSize.Height,Math.Max(Back?.DesiredSize.Height??0,Actions.DesiredSize.Height)));
        }
        protected override Size ArrangeOverride(Size finalSize)
        {
            var units=(TopLevel.GetTopLevel(this)?.RenderScaling??2)*64;
            double Center(Control c)=>Math.Floor((finalSize.Height-c.DesiredSize.Height)*units/2)/units;
            var x=0d;
            if(Back is not null){Back.Arrange(new(x,Center(Back),_backWidth,Back.DesiredSize.Height));x+=_backWidth+12;}
            Hint.Arrange(new(x,Center(Hint),_hintWidth,Hint.DesiredSize.Height));
            if(Actions.Buttons.Count>0)Actions.Arrange(new(finalSize.Width-_actionWidth,Center(Actions),_actionWidth,Actions.DesiredSize.Height));
            return finalSize;
        }
    }
    private sealed class FooterActions:Panel
    {
        public IReadOnlyList<WebActionButton> Buttons{get;private set;}=Array.Empty<WebActionButton>();
        public FooterActions(){UseLayoutRounding=false;ClipToBounds=false;}
        public void SetButtons(IReadOnlyList<WebActionButton> buttons){Children.Clear();Buttons=buttons;foreach(var b in buttons)Children.Add(b);InvalidateMeasure();}
        protected override Size MeasureOverride(Size availableSize)
        {
            foreach(var b in Buttons)b.Measure(Size.Infinity);
            return new(Buttons.Sum(b=>b.DesiredSize.Width)+Math.Max(0,Buttons.Count-1)*10,Buttons.Select(b=>b.DesiredSize.Height).DefaultIfEmpty(0).Max());
        }
        protected override Size ArrangeOverride(Size finalSize)
        {
            var x=0d;foreach(var b in Buttons){b.Arrange(new(x,(finalSize.Height-b.DesiredSize.Height)/2,b.DesiredSize.Width,b.DesiredSize.Height));x+=b.DesiredSize.Width+10;}return finalSize;
        }
    }
}
public sealed record WebFooterAction(string Action,string Label,string Variant,string IconName,bool Enabled=true,bool Loading=false);
