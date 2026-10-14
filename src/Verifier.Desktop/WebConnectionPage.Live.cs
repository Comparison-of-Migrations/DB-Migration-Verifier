using Avalonia.Controls;
namespace Verifier.Desktop;

public sealed partial class WebConnectionPage
{
    public bool LiveMode{get;init;}
    private string _sourcePassword="",_targetPassword="",_sourceCa="",_targetCa="";
    private bool _writesStopped;
    private bool _savePasswords;
    private string _storageNotice="";
    public WebCheckbox? SavePasswordsInput{get;private set;}
    public WebActionButton? SaveConnectionButton{get;private set;}
    public WebActionButton? RestoreConnectionButton{get;private set;}
    public WebActionButton? ForgetConnectionButton{get;private set;}
    public event EventHandler<string>? LocalStorageActionRequested;
    internal bool SavePasswords=>_savePasswords;
    internal void SetLocalStorage(bool savePasswords,string notice){_savePasswords=savePasswords;_storageNotice=notice;}
    public WebCheckbox? WritesStoppedInput{get;private set;}
    internal (string SourcePassword,string TargetPassword,string SourceCa,string TargetCa,bool WritesStopped) LiveCredentials =>
        (_sourcePassword,_targetPassword,_sourceCa,_targetCa,_writesStopped);
    internal void SetLiveCredentials(string a,string b,string ca,string cb,bool stopped)
    {_sourcePassword=a;_targetPassword=b;_sourceCa=ca;_targetCa=cb;_writesStopped=stopped;}
    private void ConfigureLiveInputs()
    {
        string T(string a,string b)=>View.English?b:a;
        foreach(var (panel,side,secret) in new[]{(Source,"source",_sourcePassword),(Target,"target",_targetPassword)})
        {
            panel.PasswordInput.IsEnabled=!View.Busy;panel.PasswordInput.Text=secret;panel.PasswordInput.PlaceholderText=T("비밀번호 · 기본 미저장","Password · not saved");
            Avalonia.Automation.AutomationProperties.SetHelpText(panel.PasswordInput,T("접속에만 사용하는 비밀번호입니다.","Password used only to connect."));
            panel.Fields[4].Hint=T("접속에만 사용하며 결과·로그에 저장하지 않습니다.","Used only to connect; excluded from results and logs.");
            panel.Status.Text=View.Connected?T("연결 확인","Connected"):T("미확인","Not checked");
            foreach(var input in new Control[]{panel.HostInput,panel.PortInput,panel.DatabaseInput,panel.UserInput})input.IsEnabled=!View.Busy;
            panel.PasswordInput.PropertyChanged+=(_,e)=>{if(e.Property!=TextBox.TextProperty)return;if(side=="source")_sourcePassword=panel.PasswordInput.Text??"";else _targetPassword=panel.PasswordInput.Text??"";InvalidateLiveInput(side+"-pass");};
        }
        var caPair=new PanelPair{Gap=_viewport<=1100?16:20,Margin=new(0,12,0,0)};
        foreach(var (side,value) in new[]{("source",_sourceCa),("target",_targetCa)})
        {
            var input=new WebTextInput{Text=value,IsEnabled=!View.Busy};
            Avalonia.Automation.AutomationProperties.SetAutomationId(input,side+"-ca");
            var sql=View.Dbms=="SQL Server";
            var label=sql?(side=="source"?T("원본 서버 인증서 경로 (선택)","Source server certificate path (optional)"):T("대상 서버 인증서 경로 (선택)","Target server certificate path (optional)")):
                side=="source"?T("원본 CA 인증서 경로 (선택)","Source CA certificate path (optional)"):T("대상 CA 인증서 경로 (선택)","Target CA certificate path (optional)");
            var field=new WebField{Label=label,Input=input,Hint=sql?T("비우면 OS 인증서 신뢰를 사용합니다. 지정하면 서버 인증서와 정확히 대조합니다(CA 파일 아님).","Uses OS certificate trust when empty. An entered server certificate must match exactly (not a CA file)."):T("인증서·서버 이름 검증을 유지합니다.","Certificate and server-name verification remain enabled.")};caPair.Children.Add(field);
            input.PropertyChanged+=(_,e)=>{if(e.Property!=TextBox.TextProperty)return;if(side=="source")_sourceCa=input.Text??"";else _targetCa=input.Text??"";InvalidateLiveInput(side+"-ca");};
        }
        Children.Add(caPair);
        var stopped=new WebChoice();stopped.SetPresentation(T("양쪽 DB의 쓰기를 정지했습니다","Writes to both databases are stopped"),"writes-stopped",_writesStopped,!View.Busy);
        WritesStoppedInput=stopped.Checkbox;
        // Existing Choice owns the same accessible checkbox and whole-label input interaction.
        stopped.SelectionRequested+=(_,_)=>{_writesStopped=stopped.IsSelected;InvalidateLiveInput("writes-stopped");};Children.Add(stopped);
        var storage=new StackPanel{Spacing=8,Margin=new(0,16,0,0)};
        var scope=WebTypography.CreateCandidate(T("선택 저장 · 서버·포트·DB·접속 계정·CA 경로를 이 기기에만 저장합니다. 저장한 설정도 다시 연결을 확인합니다.","Optional local storage · server, port, database, account and CA path stay on this device. Restored settings require a fresh connection check."),13,20.8,Avalonia.Media.Brush.Parse("#b5bac1"));scope.TextWrapping=Avalonia.Media.TextWrapping.Wrap;storage.Children.Add(scope);
        var remember=new WebChoice{ReducedMotion=ReducedMotion};remember.SetPresentation(T("비밀번호도 OS 보안 저장소에 저장 (기본 미저장)","Also save passwords in the OS credential store (off by default)"),"save-passwords",_savePasswords,!View.Busy);SavePasswordsInput=remember.Checkbox;
        remember.SelectionRequested+=(_,_)=>{_savePasswords=remember.IsSelected;Changed("storage",null);};storage.Children.Add(remember);
        var actions=new WrapPanel{Orientation=Avalonia.Layout.Orientation.Horizontal};
        SaveConnectionButton=Button("save-local",T("접속 설정 저장","Save connection settings"),"save",View.Connected&&!View.Busy);
        RestoreConnectionButton=Button("restore-local",T("저장한 접속 불러오기","Load saved connections"),"file",!View.Busy);
        ForgetConnectionButton=Button("forget-local",T("저장한 접속 삭제","Forget saved connections"),"",!View.Busy);
        foreach(var b in new[]{SaveConnectionButton,RestoreConnectionButton,ForgetConnectionButton})actions.Children.Add(b);storage.Children.Add(actions);
        if(_storageNotice.Length>0){var text=WebTypography.CreateCandidate(_storageNotice,13,20.8,Avalonia.Media.Brush.Parse("#dbdee1"));text.TextWrapping=Avalonia.Media.TextWrapping.Wrap;Avalonia.Automation.AutomationProperties.SetLiveSetting(text,Avalonia.Automation.AutomationLiveSetting.Polite);storage.Children.Add(text);}
        Children.Add(storage);
        WebActionButton Button(string action,string title,string icon,bool enabled)
        {var button=new WebActionButton{Content=title,IconName=icon,IsEnabled=enabled,ReducedMotion=ReducedMotion,Margin=new(0,0,8,8)};Avalonia.Automation.AutomationProperties.SetAutomationId(button,"Connection-"+action);button.Click+=(_,_)=>LocalStorageActionRequested?.Invoke(this,action);return button;}
        if(ConnectedNotice is{}notice){notice.Title=T("양쪽 DB의 실제 연결을 확인했습니다","Both database connections are verified");notice.Text=T("조회 범위와 읽지 못한 항목을 다음 화면에서 확인합니다.","Review visibility and unreadable scopes on the next page.");}
    }
    private void RefreshLiveHints()
    {foreach(var panel in new[]{Source,Target}){panel.PasswordInput.IsEnabled=!View.Busy;panel.PasswordInput.PlaceholderText=View.English?"Password · not saved":"비밀번호 · 기본 미저장";Avalonia.Automation.AutomationProperties.SetHelpText(panel.PasswordInput,View.English?"Password used only to connect.":"접속에만 사용하는 비밀번호입니다.");panel.Fields[4].Hint=View.English?"Used only to connect; excluded from results and logs.":"접속에만 사용하며 결과·로그에 저장하지 않습니다.";}}
    private void InvalidateLiveInput(string field)
    {View=View with{Connected=false,Error=""};if(SaveConnectionButton!=null)SaveConnectionButton.IsEnabled=false;Source.Status.Text=Target.Status.Text=View.English?"Not checked":"미확인";Source.Status.Kind=Target.Status.Kind="";if(ConnectedNotice is{}n){Children.Remove(n);ConnectedNotice=null;}Changed("input",field);InvalidateMeasure();}
}
