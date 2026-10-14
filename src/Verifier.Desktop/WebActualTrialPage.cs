using Avalonia;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Verifier.Core;
using Verifier.Data;
namespace Verifier.Desktop;

// Same page heading, panels, fields, choices and action controls as the completed UI.
// Actual schema-driven fields replace the preview's fixed amount/example inputs.
public sealed class WebActualTrialPage:StackPanel
{
    public WebPageHeading Heading {get;}=new();
    public WebConnectionPanel ConnectionPanel {get;}=new();
    public WebTextInput CertificateInput {get;}=new();
    public WebTextInput TableInput {get;}=new();
    public WebTextInput RoleInput {get;}=new();
    public WebChoice ConsentChoice {get;}=new();
    public WebActionButton PrepareButton {get;}=new();
    public WebNotice? ErrorNotice {get;private set;}
    public event EventHandler<string>? ActionRequested;
    internal event EventHandler<bool>? Edited;
    private readonly LiveTrialSetup _state;
    private readonly bool _english,_reduced;
    private string T(string ko,string en)=>_english?en:ko;
    internal WebActualTrialPage(LiveTrialSetup state,bool english,bool reduced,IReadOnlyList<string> suggestions,DbKind kind=DbKind.PostgreSql)
    {
        _state=state;_english=english;_reduced=reduced;Spacing=20;UseLayoutRounding=false;
        Heading.SetPresentation(T("동작 시험은 별도 DB에서 합니다","Behavior tests use a separate database"),T("연결과 분리를 확인한 뒤, 실제 구조에서 만든 시험안을 확인하세요. 이 단계에서는 읽기만 합니다.","Verify connection and isolation, then review a plan generated from the actual schema. This preparation only reads."));Children.Add(Heading);
        ConnectionPanel.SetPresentation(english?"en":"ko","target",state.Prepared!=null);
        ConnectionPanel.Title.Text=T("별도 시험 DB","Separate test DB");ConnectionPanel.Subtitle.Text=T("이 DB에만 확인한 시험 쓰기를 실행합니다","Confirmed test writes run only in this DB");
        ConnectionPanel.Status.Text=state.Prepared!=null?T("분리 확인","Isolation verified"):T("미확인","Not checked");
        ConnectionPanel.HostInput.Text=state.Host;ConnectionPanel.PortInput.Value=state.Port;ConnectionPanel.DatabaseInput.Text=state.Database;ConnectionPanel.UserInput.Text=state.User;
        ConnectionPanel.Fields[3].Label=T("시험 준비 계정","Test preparation account");if(kind is DbKind.MySql or DbKind.MariaDb)ConnectionPanel.Fields[3].Hint=T("시험 객체의 재생성 여부를 확인하려면 PROCESS 조회 권한도 필요합니다. 앱이 권한을 변경하지 않습니다.","PROCESS visibility is also required to detect replacement of the prepared table. The app does not change grants.");ConnectionPanel.PasswordInput.IsEnabled=true;ConnectionPanel.PasswordInput.Text=state.Password;
        ConnectionPanel.PasswordInput.PlaceholderText=T("비밀번호 · 미저장","Password · not saved");ConnectionPanel.Fields[4].Hint=T("접속에만 사용하며 결과·로그에 저장하지 않습니다.","Used only to connect; excluded from results/logs.");Children.Add(ConnectionPanel);
        CertificateInput.Text=state.Ca;TableInput.Text=state.Table;
        AutomationProperties.SetAutomationId(CertificateInput,"trial-ca");AutomationProperties.SetAutomationId(TableInput,"trial-table");
        Children.Add(new WebField{Label=kind==DbKind.SqlServer?T("서버 인증서 경로 (선택 · 공개 leaf)","Server certificate path (optional · public leaf)"):T("CA 인증서 경로 (선택)","CA certificate path (optional)"),Input=CertificateInput,Hint=T("인증서·서버 이름 검증을 유지합니다.","Certificate and server-name verification remain enabled.")});
        Children.Add(new WebField{Label=T("시험 테이블 · schema.table","Test table · schema.table"),Input=TableInput,Hint=T("대상과 같은 구조를 준비하세요. 다르면 결과에 차이를 남깁니다.","Prepare the target schema. Any difference remains in the results.")});
        var quick=new WrapPanel{Orientation=Orientation.Horizontal};foreach(var name in suggestions.Take(8)){var b=Button(name,"Trial-table-option-"+name);b.Margin=new(0,0,8,8);b.Click+=(_,_)=>TableInput.Text=name;quick.Children.Add(b);}Children.Add(quick);
        PrepareButton.Content=T("실제 연결 · 분리 확인 · 시험안 만들기","Verify connection/isolation · Generate plan");PrepareButton.Variant="primary";PrepareButton.IconName="db";PrepareButton.ReducedMotion=reduced;PrepareButton.IsLoading=state.Busy;
        AutomationProperties.SetAutomationId(PrepareButton,"Trial-prepare-actual");PrepareButton.Click+=(_,_)=>ActionRequested?.Invoke(this,"trial-prepare-actual");Children.Add(PrepareButton);
        Children.Add(new WebNotice{Kind="info",ReducedMotion=reduced,Title=T("DB 준비 방법","Prepare the test DB"),Text=T("별도 DB에 대상의 구조·필요 계정·권한을 준비하고 여기에 접속하세요. 앱은 원본·대상 복제나 전체 초기화를 하지 않습니다. 정상 시험은 마지막에 rollback하며 시퀀스·트리거·외부 부작용의 완전 복구를 보장하지 않습니다.","Prepare a separate DB with the target schema and required roles/grants, then connect here. The app does not clone or reset databases. Normal trials end with rollback; full restoration of sequences, triggers and external effects is not guaranteed.")});
        if(state.Prepared is{} prepared)BuildPlan(prepared);
        var error=state.ErrorText(english);if(error.Length>0){ErrorNotice=new WebNotice{Kind="error",ReducedMotion=reduced,Title=T("시험 준비를 확인하세요","Check trial preparation"),Text=error};Children.Add(ErrorNotice);}
        ConnectionPanel.InputChanged+=(_,_)=>ConnectionEdited();ConnectionPanel.PasswordInput.PropertyChanged+=(_,e)=>{if(e.Property==TextBox.TextProperty)ConnectionEdited();};
        CertificateInput.PropertyChanged+=(_,e)=>{if(e.Property==TextBox.TextProperty){state.Ca=CertificateInput.Text??"";ConnectionEdited();}};
        TableInput.PropertyChanged+=(_,e)=>{if(e.Property==TextBox.TextProperty){state.Table=TableInput.Text??"";ConnectionEdited();}};
        IsEnabled=!state.Busy;
    }
    private void ConnectionEdited()
    {
        _state.Host=ConnectionPanel.HostInput.Text??"";_state.Port=ConnectionPanel.PortInput.RawText;_state.Database=ConnectionPanel.DatabaseInput.Text??"";_state.User=ConnectionPanel.UserInput.Text??"";_state.Password=ConnectionPanel.PasswordInput.Text??"";
        _state.ClearPreparation();ConnectionPanel.Status.Text=T("미확인","Not checked");ConnectionPanel.Status.Kind="";ConsentChoice.IsSelected=false;_planPanel?.IsEnabled=false;Edited?.Invoke(this,true);
    }
    private StackPanel? _planPanel;
    private event Action? ReferenceSelectionChanged;
    private void PlanEdited(){_state.Reviewed=false;ConsentChoice.IsSelected=false;Edited?.Invoke(this,false);}
    private WebActionButton Button(string text,string id){var button=new WebActionButton{Content=text,ReducedMotion=_reduced};AutomationProperties.SetAutomationId(button,id);return button;}
    private Control Surface(Control content)=>new WebSurfaceBox{Background=Brush.Parse("#2b2d31"),BorderBrush=Brush.Parse("#41434a"),BorderThickness=new(1),CornerRadius=new(10),Padding=new(26),Child=content};
    private WebSystemTextBlock Label(string value,double size=14)=>WebTypography.CreateCandidate(value,size,size*1.6,Brush.Parse("#dbdee1"),size>14?700:400);
    private void BuildPlan(IPreparedTrial prepared)
    {
        _planPanel=new StackPanel{Spacing=16};Children.Add(_planPanel);
        _planPanel.Children.Add(new WebNotice{Kind=prepared.TargetStructure==Verdict.Equal?"ok":"warn",ReducedMotion=_reduced,Title=T("실제 DB 분리 확인 · 입력→조회→수정→조회→삭제→rollback","Actual DB isolation verified · Insert→read→update→read→delete→rollback"),Text=T("대상과 시험 구조: ","Target vs trial structure: ")+prepared.TargetStructure+T(" · 아래 값과 부모 자료까지 확인한 뒤 실행하세요. 입력을 바꾸면 확인은 취소됩니다."," · Review all values and dependency rows before execution. Editing invalidates confirmation.")});
        if(prepared.Kind is DbKind.MySql or DbKind.MariaDb)_planPanel.Children.Add(Label(T("기존 행과 겹치지 않는 입력만 시험합니다. 키가 없으면 입력값을 잠금 조회해 앱이 생성한 한 행인지 확인하며, 수정값 충돌 시 변경 전에 중단합니다. 잠금은 다른 쓰기를 기다리게 할 수 있습니다.","Only inputs that do not match existing rows are tested. Without a key, a locking read verifies one app-created row; conflicting updates stop before changes. Locks can delay other writes.")));
        foreach(var p in prepared.Dependencies.Append(prepared))
        {
            var block=new StackPanel{Spacing=10};block.Children.Add(Label((p.Id==prepared.Id?T("시험 행 · ","Trial row · "):T("합성 부모 행 · ","Synthetic dependency row · "))+p.Table,18));
            if(p.Id!=prepared.Id)
            {
                var include=new WebChoice{ReducedMotion=_reduced};include.SetPresentation(T("이 부모 자료도 생성합니다","Create this dependency row"),"trial-parent-"+p.Id,_state.IncludedDependencies.Contains(p.Id));include.SelectionRequested+=(_,_)=>{if(include.IsSelected)_state.IncludedDependencies.Add(p.Id);else _state.IncludedDependencies.Remove(p.Id);ReferenceSelectionChanged?.Invoke();PlanEdited();};block.Children.Add(include);
            }
            AddInputs(block,p,_state.Inserts[p.Id],"insert");
            foreach(var issue in p.Draft.NeedsInput)
            {
                var explanation=prepared.Kind==DbKind.SqlServer?issue.Kind switch
                {
                    "GeneratedParentKey"=>T("이 참조는 새 부모 행에 자동 연결할 수 없습니다. 기존 부모의 참조값을 확인해 입력하고 해당 부모 생성 선택을 해제하세요.","This reference cannot be bound to a new parent row automatically. Confirm an existing parent reference value, enter it, and deselect creation of that parent."),
                    "ForeignKeyCycle"=>T("순환 참조는 자동 구성하지 않습니다. 허용되는 NULL 또는 기존 부모 키를 직접 확인하세요. 제약을 끄거나 기존 행을 수정하지 않습니다.","Cyclic references require confirmed NULL values where allowed, or existing parent keys. Constraints stay enabled and existing rows are preserved."),
                    _=>null
                }:null;
                block.Children.Add(Label(T("추가 확인 · ","Review needed · ")+(issue.Column??p.Table)+" · "+(explanation??issue.Kind+" / "+issue.Reason)));
            }
            _planPanel.Children.Add(Surface(block));
        }
        var update=new StackPanel{Spacing=10};update.Children.Add(Label(T("수정할 값 · 선택한 컬럼만 변경","Update values · only selected columns change"),18));AddInputs(update,prepared,_state.Update,"update");_planPanel.Children.Add(Surface(update));
        var rejection=new StackPanel{Spacing=10};var reject=new WebChoice{ReducedMotion=_reduced};reject.SetPresentation(T("제약 위반 거부도 시험합니다","Test a constraint rejection too"),"trial-reject",_state.Reject);reject.SelectionRequested+=(_,_)=>{_state.Reject=reject.IsSelected;PlanEdited();};rejection.Children.Add(reject);
        rejection.Children.Add(Label(T("정상 행을 만든 다음 아래 입력이 지정한 단계·오류 코드로 거부되는지 확인합니다.",prepared.Kind==DbKind.SqlServer?"After inserting a valid baseline, verify rejection at the selected stage with the expected native error.":"After inserting a valid baseline, verify that the following input is rejected at the selected stage with the expected SQLSTATE.")));
        var stages=new WrapPanel();foreach(var stage in new[]{TrialStage.RejectInsert,TrialStage.RejectUpdate,TrialStage.RejectCommit}.Where(s=>prepared.Kind==DbKind.PostgreSql||s!=TrialStage.RejectCommit))
        {var button=Button(stage.ToString(),"Trial-rejection-"+stage);button.Filter=true;button.Selected=_state.RejectionStage==stage;button.Margin=new(0,0,8,0);button.Click+=(_,_)=>{_state.RejectionStage=stage;foreach(var b in stages.Children.OfType<WebActionButton>())b.Selected=ReferenceEquals(b,button);PlanEdited();};stages.Children.Add(button);}rejection.Children.Add(stages);
        var code=new WebTextInput{Text=_state.ExpectedSqlState,ReducedMotion=_reduced};AutomationProperties.SetAutomationId(code,"trial-sqlstate");code.PropertyChanged+=(_,e)=>{if(e.Property==TextBox.TextProperty){_state.ExpectedSqlState=code.Text??"";PlanEdited();}};if(prepared.Kind!=DbKind.SqlServer)rejection.Children.Add(new WebField{Label=prepared.Kind==DbKind.PostgreSql?T("기대 SQLSTATE · 23502/23503/23505/23514/23P01/22001/22003/22P02","Expected SQLSTATE · 23502/23503/23505/23514/23P01/22001/22003/22P02"):T("기대 SQLSTATE · 23000/22001/22003/22007/22018/22032/HY000","Expected SQLSTATE · 23000/22001/22003/22007/22018/22032/HY000"),Input=code});var constraint=new WebTextInput{Text=_state.ExpectedConstraint,ReducedMotion=_reduced};AutomationProperties.SetAutomationId(constraint,"trial-constraint");constraint.PropertyChanged+=(_,e)=>{if(e.Property==TextBox.TextProperty){_state.ExpectedConstraint=constraint.Text??"";PlanEdited();}};rejection.Children.Add(new WebField{Label=T("기대 제약 이름 (선택)","Expected constraint name (optional)"),Input=constraint,Hint=T("지정하면 다른 제약의 오류는 기대 충족으로 처리하지 않습니다.","When specified, an error from another constraint cannot meet the expectation.")});if(prepared.Kind is DbKind.MySql or DbKind.MariaDb or DbKind.SqlServer)
        {
            var native=new WebTextInput{Text=_state.ExpectedNativeError,ReducedMotion=_reduced};AutomationProperties.SetAutomationId(native,"trial-native-error");native.PropertyChanged+=(_,e)=>{if(e.Property==TextBox.TextProperty){_state.ExpectedNativeError=native.Text??"";PlanEdited();}};
            rejection.Children.Add(new WebField{Label=prepared.Kind==DbKind.SqlServer?T("기대 DB 오류 번호 · 예: NULL 515, 제약 547, 중복 2627","Expected native error · e.g. NULL 515, constraint 547, duplicate 2627"):T("기대 DB 오류 번호 · 예: NULL 1048, 중복 1062","Expected native error · e.g. NULL 1048, duplicate 1062"),Input=native,Hint=prepared.Kind==DbKind.SqlServer?T("오류 번호와 지정한 제약 이름이 일치해야 합니다. 오류에서 제약을 확인할 수 없으면 기대 충족으로 처리하지 않습니다.","The native number and any specified constraint must match. An unidentified constraint never meets the expectation."):T("SQLSTATE와 오류 번호 모두 일치해야 합니다. 제약 이름이 공개되지 않으면 확인 불가입니다. 이 DBMS는 지연 commit 제약 시험을 제공하지 않습니다.","Both SQLSTATE and native code must match. Hidden constraint names remain unverified. This DBMS has no deferred commit-constraint trial.")});
        }
        AddInputs(rejection,prepared,_state.Rejected,"rejected");_planPanel.Children.Add(Surface(rejection));
        var role=new StackPanel{Spacing=10};RoleInput.Text=_state.Role;RoleInput.ReducedMotion=_reduced;AutomationProperties.SetAutomationId(RoleInput,"trial-role");RoleInput.PropertyChanged+=(_,e)=>{if(e.Property==TextBox.TextProperty){_state.Role=RoleInput.Text??"";PlanEdited();}};
        role.Children.Add(new WebField{Label=prepared.Kind==DbKind.SqlServer?T("시험할 DB 사용자 (선택)","DB user to test (optional)"):T("시험할 역할 (선택)","Role to test (optional)"),Input=RoleInput,Hint=prepared.Kind==DbKind.SqlServer?T("EXECUTE AS USER로 사용자의 역할·권한이 적용된 동작을 확인합니다. 준비 계정에 해당 사용자 IMPERSONATE 권한이 필요합니다. 비밀번호·로그인 성공은 검사하지 않습니다.","Tests operations via EXECUTE AS USER with the user's roles/grants. The preparation account needs IMPERSONATE permission. This does not test passwords/logins."):T("SET ROLE로 실제 동작을 확인합니다. 이 역할의 비밀번호·로그인 성공을 검증하는 것은 아닙니다. 비우면 BEHAVIOR-02는 미실행입니다.","Tests actual operations via SET ROLE, not password/login success. Leave empty to keep BEHAVIOR-02 unexecuted.")});
        if(prepared.Kind==DbKind.MySql)
        {var host=new WebTextInput{Text=_state.RoleHost,ReducedMotion=_reduced};AutomationProperties.SetAutomationId(host,"trial-role-host");host.PropertyChanged+=(_,e)=>{if(e.Property==TextBox.TextProperty){_state.RoleHost=host.Text??"";PlanEdited();}};role.Children.Add(new WebField{Label=T("역할의 계정 host","Role account host"),Input=host,Hint=T("DB 접속 주소가 아닌 역할 식별자의 host입니다. 기본값은 %입니다.","Host part of the role identifier, not the DB connection address. Defaults to %. ")});}
        foreach(var operation in _state.RoleExpectations.Keys.ToArray())
        {
            role.Children.Add(Label(operation.ToString()));var choices=new WrapPanel();
            foreach(var expectation in new[]{TrialExpectation.Allowed,TrialExpectation.PermissionDenied,TrialExpectation.NoRow}.Where(e=>operation!=TrialStage.RoleInsert||e!=TrialExpectation.NoRow))
            {var b=Button(expectation switch{TrialExpectation.Allowed=>T("1행 허용","One row allowed"),TrialExpectation.NoRow=>T("0행 조회·변경","Zero visible/affected rows"),_=>T("권한 오류로 거부","Permission error")},"Trial-role-"+operation+"-"+expectation);b.Filter=true;b.Selected=_state.RoleExpectations[operation]==expectation;b.Margin=new(0,0,8,0);b.Click+=(_,_)=>{_state.RoleExpectations[operation]=expectation;foreach(var button in choices.Children.OfType<WebActionButton>())button.Selected=ReferenceEquals(button,b);PlanEdited();};choices.Children.Add(b);}role.Children.Add(choices);
        }
        if(prepared.Kind is DbKind.MySql or DbKind.MariaDb)role.Children.Add(Label(T("선택 역할과 접속 계정의 직접 권한을 합친 실제 동작입니다. 역할 단독 권한을 인증하지 않습니다. MySQL은 역할 이름과 계정 host를 함께 확인합니다.","Tests the selected role together with the login account's direct grants, not isolated role privileges. MySQL verifies both the role name and account host.")));
        _planPanel.Children.Add(Surface(role));ConsentChoice.ReducedMotion=_reduced;ConsentChoice.SetPresentation(T("별도 DB·위 입력·부모 자료·기대 결과·정리 한계를 확인했습니다","I reviewed the separate DB, inputs, dependency rows, expectations and cleanup limits"),"trial-actual-consent",_state.Reviewed);ConsentChoice.SelectionRequested+=(_,_)=>{_state.Reviewed=ConsentChoice.IsSelected;Edited?.Invoke(this,false);};_planPanel.Children.Add(ConsentChoice);
    }
    private void AddInputs(StackPanel block,IPreparedTrial prepared,Dictionary<string,string?> values,string phase)
    {
        foreach(var column in prepared.Columns.Where(c=>!c.Generated))
        {
            var reference=phase=="insert"?column.GeneratedReference:null;
            var input=new WebTextInput{Text=values.GetValueOrDefault(column.Name)??"",ReducedMotion=_reduced};var use=new WebChoice{ReducedMotion=_reduced};var nil=new WebChoice{ReducedMotion=_reduced};
            var key=phase+"-"+prepared.Table+"-"+column.Name;
            use.SetPresentation(T("사용","Use"),"trial-use-"+key,values.ContainsKey(column.Name));nil.SetPresentation("NULL","trial-null-"+key,values.TryGetValue(column.Name,out var v)&&v==null);
            AutomationProperties.SetAutomationId(input,"trial-value-"+key);AutomationProperties.SetName(input,column.Name+" · "+column.Type+" · "+phase);
            var row=new Grid{ColumnDefinitions=new("*,100,100"),ColumnSpacing=12};row.Children.Add(new WebField{Label=column.Name+" · "+column.Type+(column.HasDefault?T(" · 기본값 있음"," · default available"):""),Input=input,Hint=reference!=null?(reference.Generated?T("부모 생성 선택 시 DB가 생성한 키를 자동 연결합니다. 해제하면 기존 부모 키를 직접 입력하세요. ","When creating the parent, its generated key is bound automatically. Otherwise enter an existing key. "):T("부모 생성 선택 시 그 부모의 확인된 참조값을 연결합니다. 해제하면 기존 부모의 값을 입력하세요. ","When creating the parent, its confirmed reference value is bound automatically. Otherwise enter the existing parent value. "))+reference.Table+" · "+reference.Column:column.InputFormat switch{"sql-spatial-wkt"=>T("SRID=0;POINT (1 2) · geography는 SRID=4326을 사용합니다. 좌표 원문을 그대로 전달합니다.","SRID=0;POINT (1 2); use SRID=4326 for geography. Coordinates are passed unchanged."),"hierarchyid"=>T("계층 경로 · 예: /1/2/","Hierarchy path, e.g. /1/2/"),"sql_variant"=>T("타입·값 JSON · 예: {\"type\":\"decimal\",\"precision\":38,\"scale\":10,\"value\":\"1.0000000001\"}","Typed JSON, e.g. {\"type\":\"decimal\",\"precision\":38,\"scale\":10,\"value\":\"1.0000000001\"}"),"geometry-wkb"=>T("SRID=0;0x… 형식의 OGC WKB · 좌표는 반올림하지 않습니다.","OGC WKB as SRID=0;0x…; coordinates are never rounded."),"hex"=>T("바이트를 0x00ff 형식으로 입력하세요. 고정 BINARY는 선언된 전체 길이가 필요합니다.","Enter bytes as 0x00ff. Fixed BINARY needs the full declared length."),"bit"=>T("0b0101 형식 · 선언한 비트 수 이내","Use 0b0101, within the declared bit width"),"json" or "json-text"=>T("JSON 문서 · 예: {\"항목\":1}. 숫자 정밀도 손실은 불일치입니다.","JSON document, e.g. {\"item\":1}. Numeric precision loss is a mismatch."),"uuid"=>"00112233-4455-6677-8899-aabbccddeeff","inet4"=>"192.0.2.1","inet6"=>"2001:db8::1","time"=>"01:02:03.123456","datetime" or "timestamp"=>"2026-01-02 03:04:05.123456","date"=>"2026-01-02",_=>null}});Grid.SetColumn(use,1);Grid.SetColumn(nil,2);use.VerticalAlignment=nil.VerticalAlignment=VerticalAlignment.Center;row.Children.Add(use);row.Children.Add(nil);block.Children.Add(row);
            void Apply(bool notify=true)
            {
                var automatic=reference!=null&&_state.IncludedDependencies.Contains(reference.DependencyId);
                use.IsEnabled=!automatic;input.IsEnabled=!automatic&&use.IsSelected&&!nil.IsSelected;nil.IsEnabled=!automatic&&use.IsSelected;
                input.PlaceholderText=automatic?reference!.Generated?T("새 부모의 생성 키 · 실행 중 자동 연결","New parent generated key · bound during execution"):T("새 부모의 확인 값 · 실행 중 자동 연결","New parent confirmed value · bound during execution"):"";
                if(automatic||!use.IsSelected)values.Remove(column.Name);else values[column.Name]=nil.IsSelected?null:input.Text??"";
                if(notify)PlanEdited();
            }
            Apply(false);if(reference!=null)ReferenceSelectionChanged+=()=>Apply(false);
            input.PropertyChanged+=(_,e)=>{if(e.Property==TextBox.TextProperty)Apply();};use.SelectionRequested+=(_,_)=>Apply();nil.SelectionRequested+=(_,_)=>Apply();
        }
    }
}
