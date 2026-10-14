using Verifier.Data;
using Verifier.Core;

namespace Verifier.Desktop;

public sealed partial class ReplicaWorkspace
{
    private LocalConnectionStore? _connectionStore;
    private bool _savePasswords;
    private string _localStorageStatus="";
    private string LocalStorageMessage()=>_localStorageStatus switch
    {
        "saved"=>English?"Connection settings saved on this device. Passwords were not saved.":"이 기기에 접속 설정을 저장했습니다. 비밀번호는 저장하지 않았습니다.",
        "saved-secret"=>English?"Connection settings saved; passwords are in the OS credential store only.":"접속 설정을 저장했습니다. 비밀번호는 OS 보안 저장소에만 보관합니다.",
        "loaded"=>English?"Saved settings loaded. Confirm stopped writes and check both connections again.":"저장한 설정을 불러왔습니다. 쓰기 정지 확인 후 양쪽 연결을 다시 확인하세요.",
        "missing-secret"=>English?"Settings loaded, but a saved password is missing or unavailable. Enter it again; no connection was verified.":"설정은 불러왔지만 저장한 비밀번호가 없거나 읽을 수 없습니다. 직접 입력하세요. 연결은 미확인입니다.",
        "empty"=>English?"No saved connections on this device.":"이 기기에 저장한 접속 설정이 없습니다.",
        "forgotten"=>English?"Saved settings and their OS credential entries were deleted. Current in-memory inputs remain until you clear them or close the app.":"저장한 설정과 해당 OS 비밀번호를 삭제했습니다. 현재 입력은 지우거나 앱을 종료할 때까지 메모리에 남습니다.",
        "failed"=>English?"Local storage could not complete. Existing connection verification is unchanged. No passwords were saved to ordinary files; failed deletion may need a retry.":"로컬 보관 작업을 완료하지 못했습니다. 실제 연결 확인 상태는 유지됩니다. 비밀번호를 일반 파일에 저장하지 않으며 삭제 실패 시 다시 삭제해야 합니다.",
        _=>""
    };
    private async Task DispatchLocalStorageAsync(string action)
    {
        if(Screen!="connect"||CurrentPage is not WebConnectionPage page||LiveBusy)return;
        CaptureLiveConnection(page);
        if(action=="save-local"&&(!Connection.Connected||_pair==null))return;
        var revision=_connectionRevision;var wasConnected=Connection.Connected;
        var source=action=="save-local"?LiveSettings(Connection.Source,true):null;
        var target=action=="save-local"?LiveSettings(Connection.Target,false):null;
        var remember=_savePasswords;
        Connection=Connection with{Busy=true};Render(false);
        try
        {
            _connectionStore??=new(LocalConnectionStore.DefaultDirectory,CredentialVault.Create());
            if(action=="save-local")
            {await Task.Run(()=>_connectionStore.SaveAsync(source!,target!,remember));_localStorageStatus=remember?"saved-secret":"saved";}
            else if(action=="forget-local")
            {await Task.Run(()=>_connectionStore.ForgetAsync());_localStorageStatus="forgotten";}
            else
            {
                var restored=await Task.Run(()=>_connectionStore.RestoreAsync());
                if(_disposed||revision!=_connectionRevision)return;
                if(restored==null)_localStorageStatus="empty";
                else
                {
                    InvalidateLiveConnection();revision=_connectionRevision;wasConnected=false;
                    _visited.Clear();_visited.Add("connect");Sidebar.SetState(English?"en":"ko",Screen,_visited,ReducedMotion);
                    var p=restored.Settings;
                    static WebConnectionEndpoint Endpoint(SavedEndpoint e)=>new(e.Host,e.Port.ToString(System.Globalization.CultureInfo.InvariantCulture),e.Database,e.User);
                    Connection=Connection with{Dbms=p.Kind switch{DbKind.PostgreSql=>"PostgreSQL",DbKind.MySql=>"MySQL",DbKind.MariaDb=>"MariaDB",_=>"SQL Server"},Source=Endpoint(p.Source),Target=Endpoint(p.Target),Error=""};
                    _sourcePassword=restored.SourcePassword??"";_targetPassword=restored.TargetPassword??"";_sourceCa=p.Source.RootCertificate??"";_targetCa=p.Target.RootCertificate??"";
                    _writesStopped=false;_savePasswords=false;_localStorageStatus=restored.CredentialsUnavailable?"missing-secret":"loaded";
                }
            }
        }
        catch(Exception e) when(e is CredentialVaultException or IOException or InvalidDataException or UnauthorizedAccessException or System.Text.Json.JsonException or ArgumentException or Verifier.Core.VerificationException)
        {_localStorageStatus="failed";}
        finally
        {
            if(!_disposed){Connection=Connection with{Busy=false,Connected=revision==_connectionRevision&&wasConnected};Render(false);Announce(LocalStorageMessage());}
        }
    }
}
