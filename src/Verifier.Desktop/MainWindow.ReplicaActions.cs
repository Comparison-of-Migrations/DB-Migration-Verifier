using Avalonia.Platform.Storage;
using Avalonia.Controls;
using Verifier.Core;

namespace Verifier.Desktop;

public sealed partial class MainWindow
{
    private async Task HandleReplicaAction(string action)
    {
        if(ReplicaPreview is not{}preview)return;
        if(!preview.IsExample){await HandleActualResultAction(preview,action);return;}
        if(action=="open")
        {
            var files=await StorageProvider.OpenFilePickerAsync(new(){Title=preview.English?"Open a sample result":"예시 결과 열기",AllowMultiple=false,FileTypeFilter=[new("JSON"){Patterns=["*.json"]}]});
            if(files.Count==0)return;
            try
            {
                await using var stream=await files[0].OpenReadAsync();
                using var bytes=new MemoryStream();var buffer=new byte[8192];int count;
                while((count=await stream.ReadAsync(buffer))>0){if(bytes.Length+count>1024*1024)throw new InvalidDataException("Invalid sample file");bytes.Write(buffer,0,count);}
                preview.LoadSampleReport(ReplicaSampleRun.Read(bytes.ToArray()));
                preview.ShowToast(preview.English?"Saved sample result opened. No checks were rerun.":"저장한 예시 결과를 열었습니다. 재검사는 하지 않았습니다.");
            }
            catch(Exception e) when(e is System.Text.Json.JsonException or InvalidDataException or KeyNotFoundException or InvalidOperationException or IOException or UnauthorizedAccessException or FormatException)
            {
                // Source invalid-file feedback stays in the application; never expose file contents.
                await ReplicaFileMessage(preview.English?"This is not a valid sample JSON saved by this preview.":"이 시안에서 저장한 정상 예시 JSON이 아닙니다.");
            }
            return;
        }
        if(action=="download")
        {
            if(preview.SampleRun is not{}run||preview.Formats.Count==0)return;
            foreach(var format in preview.Formats.ToArray())
            {
                var destination=await StorageProvider.SaveFilePickerAsync(new(){Title=preview.English?"Save sample result":"예시 결과 저장",SuggestedFileName=run.Id+"-sample."+format,DefaultExtension=format,FileTypeChoices=[new(format.ToUpperInvariant()){Patterns=["*."+format]}]});
                if(destination is null)continue;
                try
                {
                    var body=ReplicaSampleExport.Create(run,format,preview.English,preview.SafeFindingProvider?.Invoke(run,preview.English)??[]);
                    await using var stream=await destination.OpenWriteAsync();stream.SetLength(0);await stream.WriteAsync(body);await stream.FlushAsync();
                    preview.SetSaved(true);
                }
                catch(Exception e) when(e is IOException or UnauthorizedAccessException or InvalidOperationException)
                {await ReplicaFileMessage(preview.English?"Could not save the sample file.":"예시 파일을 저장하지 못했습니다.");}
            }
        }
    }
    private bool _actualResultFileBusy;
    private async Task HandleActualResultAction(ReplicaWorkspace preview,string action)
    {
        if(_actualResultFileBusy||preview.LiveBusy||preview.IsDisposed)return;
        _actualResultFileBusy=true;
        var fileAction=action is "open" or "download";var enabled=preview.IsEnabled;
        var opener=FocusManager?.GetFocusedElement() as Control;
        // Keep the current run/settings fixed while its native dialog and file
        // operation are active. The window may still close; workers own leases.
        if(fileAction)preview.IsEnabled=false;
        try{await HandleActualResultFile(preview,action);}
        catch(Exception e) when(e is VerificationException or IOException or InvalidDataException or UnauthorizedAccessException or ArgumentException or InvalidOperationException)
        {if(!preview.IsDisposed)preview.ShowToast(preview.English?"Could not open the file dialog. The result is retained.":"파일 대화상자를 열지 못했습니다. 결과는 유지됩니다.");}
        finally
        {
            _actualResultFileBusy=false;
            if(fileAction&&!preview.IsDisposed)
            {
                preview.IsEnabled=enabled;
                if(opener!=null&&TopLevel.GetTopLevel(opener)==this&&opener.IsEffectivelyEnabled)opener.Focus();
                else if(preview.CurrentPage is WebResultsPage page)page.Heading.FocusTitle(false);
            }
        }
    }
    private async Task HandleActualResultFile(ReplicaWorkspace preview,string action)
    {
        if(action is "history" or "history-refresh" or "history-next"||action.StartsWith("history-open:",StringComparison.Ordinal)||action.StartsWith("history-remove:",StringComparison.Ordinal)){await HandleHistoryAction(preview,action);return;}
        if(action=="open")
        {
            var files=await StorageProvider.OpenFilePickerAsync(new(){Title=preview.English?"Open inspection result":"검사 결과 열기",AllowMultiple=false,FileTypeFilter=[new("DMV JSON"){Patterns=["*.json"]}]});
            if(files.Count==0)return;
            using var openedFile=files[0];
            if(preview.IsDisposed)return;
            RunReport? loaded=null;
            try
            {
                var path=openedFile.TryGetLocalPath()??throw new InvalidDataException();
                loaded=await Task.Run(()=>ReportFile.LoadAsync(path));
                if(preview.IsDisposed)return;
                preview.LoadLiveReport(loaded);
                var recorded=await RecordHistoryAsync(path,loaded);
                if(!preview.IsDisposed)preview.ShowToast(recorded?(preview.English?"Saved result opened. No checks were rerun.":"저장한 결과를 열었습니다. 재검사는 하지 않았습니다."):(preview.English?"Result opened, but local history could not be recorded.":"결과는 열었지만 로컬 이력을 기록하지 못했습니다."));
            }
            catch(Exception e) when(e is VerificationException or IOException or InvalidDataException or UnauthorizedAccessException or ArgumentException or System.Text.Json.JsonException or FormatException or InvalidOperationException)
            {if(!preview.IsDisposed)preview.ShowToast(preview.English?"Could not validate this result file. The previous result is retained.":"결과 파일을 검증하지 못했습니다. 기존 결과는 유지됩니다.");}
            finally{if(loaded!=null&&!ReferenceEquals(preview.LiveReport,loaded))loaded.Dispose();}
            return;
        }
        if(action!="download"||preview.LiveReport is not{}report)return;
        using var lease=report.AcquireStorage();
        var formats=preview.Formats.ToArray();var completed=new List<string>();var historyFailed=false;preview.SetSaved(false);
        try
        {
            foreach(var extension in formats)
            {
                if(!Enum.TryParse<ReportFormat>(extension,true,out var format))throw new InvalidDataException();
                using var file=await StorageProvider.SaveFilePickerAsync(new(){Title=preview.English?"Save inspection result":"검사 결과 저장",SuggestedFileName="dmv-"+report.RunId.ToString("N")+"."+extension,DefaultExtension=extension,FileTypeChoices=[new("DMV "+extension.ToUpperInvariant()){Patterns=["*."+extension]}]});
                if(preview.IsDisposed)return;
                if(file==null){preview.ShowToast((preview.English?"Save cancelled. Saved formats: ":"저장을 취소했습니다. 저장된 형식: ")+string.Join(", ",completed));return;}
                var path=file.TryGetLocalPath()??throw new InvalidDataException();var english=preview.English;
                await Task.Run(()=>ReportExport.SaveAsync(path,report,format,english));
                completed.Add(extension);
                if(format==ReportFormat.Json&&!await RecordHistoryAsync(file.TryGetLocalPath()!,report))historyFailed=true;
                if(preview.IsDisposed)return;
            }
            preview.SetSaved(formats.Length>0&&completed.Count==formats.Length);
            if(historyFailed)preview.ShowToast(preview.English?"Result saved, but local history could not be recorded.":"결과는 저장했지만 로컬 이력을 기록하지 못했습니다.");
        }
        catch(Exception e) when(e is VerificationException or IOException or InvalidDataException or UnauthorizedAccessException or ArgumentException or System.Text.Json.JsonException or InvalidOperationException)
        {if(!preview.IsDisposed)preview.ShowToast((preview.English?"Save failed. Result retained. Saved formats: ":"저장에 실패했습니다. 결과는 유지됩니다. 저장된 형식: ")+string.Join(", ",completed));}
    }
    private Task ReplicaFileMessage(string text)
    {
        ReplicaPreview?.ShowToast(text);return Task.CompletedTask;
    }
}
