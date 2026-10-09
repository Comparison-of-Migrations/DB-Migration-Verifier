using Avalonia.Platform.Storage;

namespace Verifier.Desktop;

public sealed partial class MainWindow
{
    private async Task HandleReplicaAction(string action)
    {
        if(ReplicaPreview is not{}preview)return;
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
    private Task ReplicaFileMessage(string text)
    {
        ReplicaPreview?.ShowToast(text);return Task.CompletedTask;
    }
}
