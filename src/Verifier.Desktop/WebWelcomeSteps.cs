using Avalonia;
using Avalonia.Controls;
namespace Verifier.Desktop;

/// <summary>Original Home three-column grid with three shared row tracks.</summary>
public sealed class WebWelcomeSteps:Panel
{
    public WebWelcomeStep[] Steps{get;}=[new(),new(),new()];
    public bool English{get;private set;}
    public WebWelcomeRowTracks RowTracks{get;private set;}
    private readonly double[] _widths=new double[3];
    private readonly double[] _left=new double[3];
    public WebWelcomeSteps(){UseLayoutRounding=false;Margin=new(0,24,0,0);foreach(var step in Steps)Children.Add(step);SetPresentation(false);}
    public void SetPresentation(bool english)
    {
        English=english;
        for(var i=0;i<3;i++){Steps[i].SetSharedTracks(null);Steps[i].SetPresentation(i,english);}
        InvalidateMeasure();
    }
    protected override Size MeasureOverride(Size available)
    {
        var units=(TopLevel.GetTopLevel(this)?.RenderScaling??2)*64;
        double Q(double value)=>Math.Floor(value*units)/units;
        var space=Math.Max(0,available.Width-64);
        double fraction=(float)(space/3);
        for(var i=0;i<3;i++)
        {
            _left[i]=Q(fraction*i)+32*i;
            _widths[i]=Q(fraction*(i+1))-Q(fraction*i);
            Steps[i].Measure(new(_widths[i],double.PositiveInfinity));
        }
        RowTracks=new(Steps.Max(x=>x.NaturalTracks.Number),Steps.Max(x=>x.NaturalTracks.Title),Steps.Max(x=>x.NaturalTracks.Body));
        for(var i=0;i<3;i++){Steps[i].SetSharedTracks(RowTracks);Steps[i].Measure(new(_widths[i],double.PositiveInfinity));}
        return new(available.Width,RowTracks.Height);
    }
    protected override Size ArrangeOverride(Size final)
    {
        for(var i=0;i<3;i++)Steps[i].Arrange(new(_left[i],0,_widths[i],final.Height));
        return final;
    }
}
