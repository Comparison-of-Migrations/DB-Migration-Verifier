using Verifier.Core;
namespace Verifier.Desktop;

public sealed partial class ReplicaWorkspace
{
    private HistoryPage _historyPage=new([],null,false);
    private string _historyStatus="";
    private bool _historyBusy;
    public HistoryCursor? HistoryNext=>_historyPage.Next;
    public void ShowHistory(HistoryPage page,string status="",bool busy=false)
    {_historyPage=page;_historyStatus=status;_historyBusy=busy;Navigate("history");}
    private WebHistoryPage CreateHistoryPage()
    {
        var page=new WebHistoryPage(_historyPage,English,_historyBusy,ReducedMotion,_historyStatus);
        page.ActionRequested+=async(_,action)=>{if(ReferenceEquals(CurrentPage,page))await DispatchAsync(action);};
        Footer.SetContent(English?"Local references only. Saved results are verified again when opened.":"로컬 참조 기록입니다. 결과는 열 때 다시 검증합니다.",new("history-back",English?"Back to home":"홈으로","quiet","back",!_historyBusy),[],ReducedMotion);
        return page;
    }
}
