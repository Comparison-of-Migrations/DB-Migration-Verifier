using Avalonia;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;

namespace Verifier.Desktop;

// Paging changes only the view. Counts, inspection scope and exported results stay intact.
public sealed class WebResultPager : WrapPanel
{
    public const int PageSize=25;
    public int PageIndex{get;}
    public int Total{get;}
    public WebActionButton PreviousButton{get;}
    public WebActionButton NextButton{get;}
    public WebSystemTextBlock RangeText{get;}
    public static int LastPage(int total)=>total<=0?0:(total-1)/PageSize;
    public WebResultPager(int total,int page,bool english,bool reduced,string id,Action<int> change)
    {
        Total=total;PageIndex=Math.Clamp(page,0,LastPage(total));Orientation=Orientation.Horizontal;
        VerticalAlignment=VerticalAlignment.Center;
        var first=total==0?0:PageIndex*PageSize+1;var last=Math.Min((long)first+PageSize-1,total);
        RangeText=WebTypography.CreateCandidate($"{first:N0}–{last:N0} / {total:N0}"+(english?" items":"개"),14,22.4,Brush.Parse("#b5bac1"));
        RangeText.VerticalAlignment=VerticalAlignment.Center;RangeText.Margin=new(0,0,16,0);
        AutomationProperties.SetLiveSetting(RangeText,AutomationLiveSetting.Polite);
        PreviousButton=Button(english?"Previous":"이전",PageIndex>0,"previous");NextButton=Button(english?"Next":"다음",PageIndex<LastPage(total),"next");
        PreviousButton.Click+=(_,_)=>{if(PreviousButton.IsEnabled)change(PageIndex-1);};NextButton.Click+=(_,_)=>{if(NextButton.IsEnabled)change(PageIndex+1);};
        Children.Add(RangeText);Children.Add(PreviousButton);Children.Add(NextButton);
        WebActionButton Button(string text,bool enabled,string suffix)
        {
            var button=new WebActionButton{Content=text,IsEnabled=enabled,ReducedMotion=reduced,Margin=new(0,0,8,0)};
            AutomationProperties.SetAutomationId(button,id+"-"+suffix);return button;
        }
    }
}
