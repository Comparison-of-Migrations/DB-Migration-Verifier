using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;

namespace Verifier.Desktop;

public sealed record WebDetailScope(int Done,int Total,string Status);

/// <summary>Original detail modal content; its parent owns body padding, scrolling and the modal lifecycle.</summary>
public sealed class WebResultDetailsBody : Panel
{
    public WebBulletList CheckList{get;}=new(){Margin=default};
    public WebNotice? ScopeNotice{get;private set;}
    public WebNotice? CleanupNotice{get;private set;}
    public WebSystemTextBlock PrivacyText{get;}=WebTypography.CreateCandidate("",12,19.2,Brush.Parse("#b5bac1"));
    public bool ReducedMotion{get;set;}
    public int PageIndex{get;private set;}
    public WebResultPager? Pager{get;private set;}
    public event Action<int>? PageRequested;
    public WebResultDetailsBody(){UseLayoutRounding=false;}
    public void SetPresentation(IEnumerable<string> checks,bool behavior,WebDetailScope? scope,bool english,int pageIndex=0)
    {
        Children.Clear();ScopeNotice=null;CleanupNotice=null;Pager=null;
        var items=checks as IReadOnlyList<string>??checks.ToArray();PageIndex=Math.Clamp(pageIndex,0,WebResultPager.LastPage(items.Count));
        if(items.Count>WebResultPager.PageSize)
        {
            Pager=new(items.Count,PageIndex,english,ReducedMotion,"ResultDetails-page",index=>
            {
                var next=index>PageIndex;SetPresentation(items,behavior,scope,english,index);PageRequested?.Invoke(index);
                var button=next?Pager?.NextButton:Pager?.PreviousButton;var fallback=next?Pager?.PreviousButton:Pager?.NextButton;
                (button?.IsEnabled==true?button:fallback)?.Focus(Avalonia.Input.NavigationMethod.Tab);
            });Children.Add(Pager);
        }
        var start=PageIndex*WebResultPager.PageSize;CheckList.SetItems(Enumerable.Range(start,Math.Min(WebResultPager.PageSize,items.Count-start)).Select(i=>items[i]));Children.Add(CheckList);
        string T(string ko,string en)=>english?en:ko;
        if(scope is not null){ScopeNotice=new(){Margin=default,ReducedMotion=ReducedMotion,Title=T("실행 범위","Execution scope"),Text=$"{scope.Done} / {scope.Total} "+T("항목 처리","checks processed")+" · "+(scope.Status=="completed"?T("완료","Completed"):scope.Status=="interrupted"?T("중단","Interrupted"):T("미시작","Not started"))};Children.Add(ScopeNotice);}
        if(behavior){CleanupNotice=new(){Margin=default,Kind="warn",ReducedMotion=ReducedMotion,Title=T("예시 정리 상태","Sample cleanup state"),Text=scope?.Status=="completed"?T("정리 실패 1건 · 앱 자료 T-001 잔여 · 부작용 확인 불가. 실제 자료 변경은 없습니다.","One cleanup failure · Artifact T-001 remains · Side effects unconfirmed. No real data was modified."):T("시험 미완료. 정리 완료를 확인하지 않았습니다.","Trial incomplete. Cleanup has not been established.")};Children.Add(CleanupNotice);}
        PrivacyText.Text=T("실제 값·행 키·계정명은 표시하지 않습니다.","Row values, keys and principal names are not displayed.");Children.Add(PrivacyText);InvalidateMeasure();
    }
    protected override Size MeasureOverride(Size available)
    {
        var units=(TopLevel.GetTopLevel(this)?.RenderScaling??2)*64;PrivacyText.LineHeight=Math.Floor(19.2*units)/units;
        CheckList.Measure(new(available.Width,double.PositiveInfinity));double h=14+CheckList.DesiredSize.Height;
        if(Pager is not null){Pager.Measure(new(available.Width,double.PositiveInfinity));h+=Pager.DesiredSize.Height+16;}
        foreach(var notice in new[]{ScopeNotice,CleanupNotice})if(notice is not null){notice.Measure(new(available.Width,double.PositiveInfinity));h+=20+notice.DesiredSize.Height;}
        PrivacyText.Measure(new(available.Width,double.PositiveInfinity));h+=16+PrivacyText.DesiredSize.Height;
        return new(available.Width,h);
    }
    protected override Size ArrangeOverride(Size final)
    {
        double y=14;if(Pager is not null){Pager.Arrange(new(0,y,final.Width,Pager.DesiredSize.Height));y+=Pager.DesiredSize.Height+16;}CheckList.Arrange(new(0,y,final.Width,CheckList.DesiredSize.Height));y+=CheckList.DesiredSize.Height;
        foreach(var notice in new[]{ScopeNotice,CleanupNotice})if(notice is not null){y+=20;notice.Arrange(new(0,y,final.Width,notice.DesiredSize.Height));y+=notice.DesiredSize.Height;}
        y+=16;PrivacyText.Arrange(new(0,y,final.Width,PrivacyText.DesiredSize.Height));return final;
    }
}
