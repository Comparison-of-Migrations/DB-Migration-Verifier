using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.VisualTree;
namespace Verifier.Desktop;

/// <summary>The original Home research grid, with the viewport-based 1100px breakpoint.</summary>
public sealed class WebWelcomePaper:Panel
{
    private readonly WebSurfaceBox _rule=WebSurface.Create(Brush.Parse("#41434a"),Brushes.Transparent,default,default);
    public SvgIcon FileIcon{get;}=new(){IconName="file",Width=22,Height=22,Stroke=Brush.Parse("#b5bac1")};
    public WebWelcomePaperContent ResearchContent{get;}=new();
    public WebActionButton ResearchButton{get;}=new(){Variant="quiet"};
    public event EventHandler? ResearchRequested;
    public bool English{get;private set;}
    public double ViewportWidth{get;private set;}=1280;
    private double _contentWidth,_rowHeight,_buttonWidth,_buttonHeight;
    public WebWelcomePaper(){UseLayoutRounding=false;Background=Brushes.Transparent;Margin=new(0,30,0,0);Children.Add(_rule);Children.Add(FileIcon);Children.Add(ResearchContent);Children.Add(ResearchButton);ResearchButton.Click+=(_,_)=>ResearchRequested?.Invoke(this,EventArgs.Empty);AddHandler(InputElement.PointerPressedEvent,(_,e)=>{if(e.GetCurrentPoint(this).Properties.IsLeftButtonPressed && e.Source is Visual v && v!=ResearchButton && !v.GetVisualAncestors().Contains(ResearchButton))TopLevel.GetTopLevel(this)?.FocusManager?.Focus(null,NavigationMethod.Pointer);},RoutingStrategies.Tunnel,true);SetPresentation(false,1280);}
    public void SetPresentation(bool english,double viewportWidth,bool reducedMotion=false)
    {
        English=english;ViewportWidth=viewportWidth;ResearchContent.SetPresentation(english);ResearchButton.Content=english?"Research mapping":"연구 반영 보기";ResearchButton.ReducedMotion=reducedMotion;InvalidateMeasure();
    }
    protected override Size MeasureOverride(Size available)
    {
        ResearchButton.Measure(new(double.PositiveInfinity,double.PositiveInfinity));_buttonWidth=ResearchButton.DesiredSize.Width;_buttonHeight=ResearchButton.DesiredSize.Height;
        _contentWidth=Math.Max(0,available.Width-(ViewportWidth<=1100?36:50+_buttonWidth));ResearchContent.Measure(new(_contentWidth,double.PositiveInfinity));FileIcon.Measure(new(22,22));
        _rowHeight=Math.Max(22,ResearchContent.DesiredSize.Height);if(ViewportWidth>1100)_rowHeight=Math.Max(_rowHeight,_buttonHeight);
        return new(available.Width,49+_rowHeight+(ViewportWidth<=1100?14+_buttonHeight:0));
    }
    protected override Size ArrangeOverride(Size final)
    {
        var units=(TopLevel.GetTopLevel(this)?.RenderScaling??2)*64;double Q(double n)=>Math.Floor(n*units)/units;
        _rule.Arrange(new(0,0,final.Width,1));FileIcon.Arrange(new(0,Q(27+(_rowHeight-22)/2),22,22));ResearchContent.Arrange(new(36,Q(27+(_rowHeight-ResearchContent.DesiredSize.Height)/2),_contentWidth,ResearchContent.DesiredSize.Height));
        ResearchButton.Arrange(new(ViewportWidth<=1100?36:50+_contentWidth,ViewportWidth<=1100?27+_rowHeight+14:Q(27+(_rowHeight-_buttonHeight)/2),_buttonWidth,_buttonHeight));return final;
    }
}
