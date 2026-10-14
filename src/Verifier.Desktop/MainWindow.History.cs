using Verifier.Core;

namespace Verifier.Desktop;

public sealed partial class MainWindow
{
    private ResultHistory _resultHistory=new(ResultHistory.DefaultDirectory);
    private HistoryPage _lastHistoryPage=new([],null,false);
    private HistoryCursor? _historyBefore;
    private async Task<bool> RecordHistoryAsync(string path,RunReport report)
    {
        try{await Task.Run(()=>_resultHistory.RecordAsync(path,report));return true;}
        catch(Exception e)when(e is IOException or InvalidDataException or UnauthorizedAccessException or ArgumentException or VerificationException or System.Text.Json.JsonException){return false;}
    }
    private async Task HandleHistoryAction(ReplicaWorkspace preview,string action)
    {
        var status="";preview.ShowHistory(_lastHistoryPage,busy:true);
        try
        {
            if(action.StartsWith("history-open:",StringComparison.Ordinal))
            {
                try{var report=await Task.Run(()=>_resultHistory.OpenAsync(action[13..]));if(preview.IsDisposed){report.Dispose();return;}preview.ShowHistory(_lastHistoryPage);preview.LoadLiveReport(report);preview.ShowToast(preview.English?"Recorded result verified and opened. No checks were rerun.":"기록한 결과를 검증해 열었습니다. 재검사는 하지 않았습니다.");return;}
                catch(Exception e)when(e is IOException or InvalidDataException or UnauthorizedAccessException or ArgumentException or VerificationException or System.Text.Json.JsonException or InvalidOperationException){status="open-failed";}
            }
            else if(action.StartsWith("history-remove:",StringComparison.Ordinal))
            {
                try{await Task.Run(()=>_resultHistory.RemoveAsync(action[15..]));}
                catch(Exception e)when(e is IOException or InvalidDataException or UnauthorizedAccessException or ArgumentException or VerificationException){status="remove-failed";}
            }
            else _historyBefore=action=="history-next"?preview.HistoryNext:null;
            _lastHistoryPage=await Task.Run(()=>_resultHistory.ReadPageAsync(_historyBefore));
        }
        catch(Exception e)when(e is IOException or InvalidDataException or UnauthorizedAccessException or ArgumentException or VerificationException or System.Text.Json.JsonException){status="read-failed";}
        finally{if(!preview.IsDisposed&&preview.Screen=="history")preview.ShowHistory(_lastHistoryPage,status);}
    }
}
