using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Verifier.Core;

namespace Verifier.Desktop;

public sealed partial class MainWindow
{
    private string CategoryDescription(Category c)=>c switch {
        Category.Data=>T("전체 값·누락·중복·참조 관계","All values · missing rows · duplicates · references"),
        Category.Schema=>T("컬럼·자료형·키·제약·인덱스·partition","Columns · types · keys · constraints · indexes · partitions"),
        Category.Access=>T("사용자·역할·소유자·권한·행 정책","Users · roles · ownership · grants · row policies"),
        Category.Objects=>T("view·함수·trigger·sequence·사용자 타입","Views · routines · triggers · sequences · custom types"),
        _=>T("선택 기능 · 시험 DB에만 입력·수정·삭제","Optional · writes only to a separate test DB") };
    private string CategoryIcon(Category c)=>c switch {Category.Data=>"db",Category.Schema=>"structure",Category.Access=>"shield",Category.Objects=>"layers",_=>"play"};
    private string Capability(Category c)=>c switch {
        Category.Data=>kind==DbKind.PostgreSql?T("일부 가능","Partially available"):T("미구현","Not implemented"),
        Category.Schema=>SchemaFullyObserved?T("정의 비교 가능","Definitions available"):T("일부 가능","Partially available"),_=>T("미구현","Not implemented") };
    private bool SchemaFullyObserved=>sourceInfo is { Complete:true,Tables.Count:>0 }&&targetInfo is { Complete:true,Tables.Count:>0 }&&sourceInfo.Tables.Concat(targetInfo.Tables).All(t=>new[]{"SCHEMA-01","SCHEMA-02","SCHEMA-03"}.All(id=>t.Catalog.Count(s=>s.CheckId==id&&s.Reason==Reason.None)==1));
    private string StructureScope(string check)
    {
        string Counts(Inventory? inventory)=>inventory==null?"—":inventory.Tables.Count(t=>t.Catalog.Any(s=>s.CheckId==check&&s.Reason==Reason.None))+" / "+inventory.Tables.Count;
        return T("정의 조회 완료 테이블 · 원본 ","Tables with definitions observed · source ")+Counts(sourceInfo)+T(" · 대상 "," · target ")+Counts(targetInfo);
    }
    private Grid Row(string defs,IEnumerable<Control> cells,double gap=12,VerticalAlignment alignment=VerticalAlignment.Center){var g=Columns(defs,gap);var i=0;foreach(var c in cells){c.VerticalAlignment=alignment;Put(g,c,i++);}return g;}
    private Border TableLine(Control content,bool header=false)=>new(){Child=content,Padding=new(18,14),Background=BrushOf(header?"#232428":"#2b2d31"),BorderBrush=BrushOf("#41434a"),BorderThickness=new(0,0,0,1)};
    private void Preflight()
    {
        Heading(T("앱이 확인할 수 있는 범위입니다","Here is what the app can inspect"),T("권한 이름을 미리 알 필요는 없습니다. 가능한 검사와 확인하지 못할 부분을 먼저 보여줍니다.","You do not need to know permission names. Review available checks and visibility limits first."));
        var table=Stack(0);table.Children.Add(TableLine(Row("1.25*,.6*,1*,1.4*",new[]{Text(T("검사 분류","Category"),12,Muted),Text(T("원본 / 대상","Source / target"),12,Muted),Text(T("검사 가능 범위","Available scope"),12,Muted),Text(T("안내","Guidance"),12,Muted)}),true));
        foreach(var c in new[]{Category.Data,Category.Schema,Category.Access,Category.Objects})
        {
            var category=Stack(4);var name=Text(CategoryText(c),14,Brushes.White);name.FontWeight=FontWeight.SemiBold;category.Children.Add(name);category.Children.Add(Text(CategoryDescription(c),12,Muted));
            var status=Stack(4);status.Children.Add(Pill(Capability(c),Warn));status.Children.Add(Text(c==Category.Data?T("기본 타입 전체 행 / 제한은 별도 표시","Built-in complete rows; limits listed separately"):c==Category.Schema?T("테이블·컬럼·제약·인덱스·partition 정의 / 범위별 제한","Table, column, constraint, index and partition definitions; scoped limits"):T("실제 상세 검사 미구현","Detailed inspection pending"),12,Muted));
            var guidance=Stack(4);guidance.Children.Add(Text(T("선택 화면에서 범위를 조정할 수 있습니다.","Adjust scope on the selection screen."),12,Muted));
            if(c==Category.Access)guidance.Children.Add(Action(T("필요한 권한 보기","View required access"),PermissionsDialog,"quiet"));
            table.Children.Add(TableLine(Row("1.25*,.6*,1*,1.4*",new Control[]{category,Pill(T("연결 확인","Connected"),Good),status,guidance})));
        }
        page.Children.Add(Panel(table,new(0)));
        page.Children.Add(Notice(T("보이지 않는 객체를 ‘없음’으로 처리하지 않습니다. 읽지 못한 항목은 검사 후에도 확인 불가로 남습니다.","Invisible objects are not treated as absent. Unread items remain unverified after inspection.")));
        if(sourceInfo?.Complete!=true||targetInfo?.Complete!=true)page.Children.Add(Notice(T("객체 목록 가시성이 불완전합니다. 누락 테이블을 확정하지 않습니다.","Inventory visibility is incomplete. Missing tables cannot be confirmed.")));
        page.Children.Add(Text(T("발견된 테이블: ","Discovered tables: ")+(sourceInfo?.Tables.Count??0)+" / "+(targetInfo?.Tables.Count??0),12,Muted));
        var optional=Stack(6);optional.Children.Add(Text(T("별도 DB 동작 시험은 선택입니다","Separate DB behavior testing is optional"),18,Brushes.White));optional.Children.Add(Text(T("기본 비교는 읽기 전용입니다. 시험 준비·쓰기 기능은 아직 구현되지 않았으며 현재 실행할 수 없습니다.","Default comparison is read-only. Test preparation and writes are pending and cannot run yet."),13,Muted));page.Children.Add(optional);
        page.Children.Add(Text(T("양쪽 snapshot은 독립적이며 같은 시점이나 모든 동시 변경 탐지를 보장하지 않습니다.","Both snapshots are independent; same-time observation and all concurrent changes are not guaranteed."),12,Muted));
    }
    private Task PermissionsDialog()=>InfoDialog(T("조회 범위를 확인하려면","To establish visibility"),T("현재 계정의 관측 결과에 따라 숨겨진 범위를 구분합니다. 관리자 전체 권한을 자동 부여하지 않습니다.\n\nPostgreSQL: 서버 식별은 pg_control_system() 조회, 선택 테이블은 SELECT 권한이 필요합니다. RLS·외부/분할 테이블·계산 컬럼·미지원 타입은 전체 값 관측을 제한할 수 있습니다.\n\nSQL Server: 정의 목록의 완전성에는 VIEW DEFINITION이 필요합니다. 실제 값 검사와 서버 검증은 아직 구현/확인되지 않았습니다.\n\n권한을 준비한 뒤 연결·사전 확인부터 새 전체 검사를 합니다.","The observer account's visibility determines unknown scope. Administrator access is never granted automatically.\n\nPostgreSQL: server identity needs pg_control_system(); selected tables need SELECT. RLS, foreign/partitioned tables, computed columns and unsupported types may prevent full observation.\n\nSQL Server: complete metadata visibility needs VIEW DEFINITION. Actual value comparison/server validation remain pending.\n\nAfter preparing access, start a new full check from connection and preflight."));
    private TextBlock? selectionCount;
    private ListBox? tableList;
    private CheckBox? bulk;
    private bool updatingBulk;
    private IReadOnlyList<MatchedScope> MatchedScopes()=>sourceInfo==null||targetInfo==null?[]:ScopeMatching.Resolve(sourceInfo,targetInfo,tableMappings);
    private string[] TableNames()=>MatchedScopes().Select(s=>s.Key).ToArray();
    private string MappingLabel(TableMapping mapping)=>(mapping.Source??T("원본 미관측","Not observed in source"))+" → "+(mapping.Target??T("대상 미관측","Not observed in target"));
    private void SelectChecks()
    {
        Heading(T("무엇을 검사할지 선택하세요","Choose what to inspect"),T("읽기 비교 분류는 기본 선택입니다. 분류 안의 세부 항목도 검사 대상으로 유지합니다.","Read-only categories are selected by default, including their detailed checks."));
        var labelRow=Columns("*,Auto");Put(labelRow,Text(T("검사 분류","Categories"),12,Muted));var categoryCount=Text(selected.Count+T(" / 5 선택"," / 5 selected"),12,Muted);Put(labelRow,categoryCount,1);page.Children.Add(labelRow);
        var categories=Stack(12);
        foreach(var c in Enum.GetValues<Category>())
        {
            var key=c;var cardBody=Stack(8);foreach(var check in Checks.Definitions.Where(x=>x.Category==c))cardBody.Children.Add(Text(check.Id+" · "+check.Research+" · "+(c==Category.Data&&check.Id=="DATA-01"?T("지원 조건에서 전체 행 비교","Full rows under supported conditions"):c==Category.Schema?StructureScope(check.Id):T("상세 구현·관측 제한은 미검증으로 표시","Implementation/observation gaps remain unverified")),12,Muted));cardBody.Margin=new(64,16,20,16);cardBody.IsVisible=expanded.Contains(c);
            var checkbox=new AlignedCheckBox { Name="category-"+c,IsChecked=selected.Contains(c),IsEnabled=!busy&&c!=Category.Behavior };Avalonia.Automation.AutomationProperties.SetName(checkbox,CategoryText(c)+T(" 검사 선택"," selected"));
            var toggle=Action(CategoryText(c)+T(" 세부 검사 펼침"," detailed checks"),()=>{if(!expanded.Add(key))expanded.Remove(key);cardBody.IsVisible=expanded.Contains(key);if(cardBody.IsVisible)_=Enter(cardBody);return Task.CompletedTask;},"category-toggle");
            var text=Stack(2);text.VerticalAlignment=VerticalAlignment.Center;var title=Text(CategoryText(c),14,Brushes.White);title.FontWeight=FontWeight.SemiBold;text.Children.Add(title);text.Children.Add(Text(CategoryDescription(c),12,Muted));var face=Columns("Auto,*,Auto,Auto",16);Put(face,UiIcon(CategoryIcon(c)));Put(face,text,1);Put(face,Pill(Capability(c),Warn),2);var chevron=UiIcon(expanded.Contains(c)?"down":"chevron");Put(face,chevron,3);toggle.Content=face;
            toggle.Click+=(_,_)=>{face.Children.Remove(chevron);chevron=UiIcon(expanded.Contains(c)?"down":"chevron");Put(face,chevron,3);};
            var head=Columns("Auto,*",16);head.Margin=new(16,0);Put(head,checkbox);Put(head,toggle,1);var contents=Stack(0);contents.Children.Add(head);contents.Children.Add(cardBody);
            var panel=Panel(contents,new(0),selected.Contains(c)?"#353740":"#2b2d31");panel.BorderBrush=BrushOf(selected.Contains(c)?"#8b94ff":"#41434a");
            checkbox.IsCheckedChanged+=(_,_)=>{if(checkbox.IsChecked==true)selected.Add(key);else selected.Remove(key);categoryCount.Text=selected.Count+T(" / 5 선택"," / 5 selected");panel.Background=BrushOf(selected.Contains(key)?"#353740":"#2b2d31");panel.BorderBrush=BrushOf(selected.Contains(key)?"#8b94ff":"#41434a");_=Pulse(checkbox);reviewConfirmed=false;};categories.Children.Add(panel);
        }
        page.Children.Add(categories);
        var trialRow=Columns("Auto,*",16);Put(trialRow,WithIcon(Action(T("시험 DB 준비 안내","Test DB preparation guide"),()=>Go(8),""),"db"));Put(trialRow,Text(T("준비·쓰기 기능 미구현 · 읽기 비교만 실행할 수 있습니다.","Preparation/writes pending · only read-only comparison can run."),12,Warn),1);page.Children.Add(trialRow);
        var scope=Columns("*,Auto");Put(scope,Text(T("검사할 테이블과 대응","Tables and matching"),18,Brushes.White));selectionCount=Text("",12,Muted);Put(scope,selectionCount,1);page.Children.Add(scope);
        var toolbar=Columns("*,Auto,Auto",16);var searchGrid=Columns("Auto,*,Auto",8);Put(searchGrid,UiIcon("search"));var search=new TextBox { Name="table-search",Text=tableQuery,PlaceholderText=T("테이블 이름으로 검색","Search table names"),BorderThickness=new(0),Background=Brushes.Transparent };Avalonia.Automation.AutomationProperties.SetName(search,T("테이블 검색","Search tables"));Put(searchGrid,search,1);
        var clear=Action(T("검색 지우기","Clear search"),()=>{search.Text="";search.Focus();return Task.CompletedTask;},"quiet",44);clear.Content=UiIcon("close",Muted,16);Put(searchGrid,clear,2);Put(toolbar,Panel(searchGrid,new(8,0)));
        bulk=new AlignedCheckBox { Name="table-bulk",Content=T("검색 결과 전체 선택","Select all visible"),IsThreeState=true };bulk.IsCheckedChanged+=(_,_)=>{if(updatingBulk)return;foreach(var name in VisibleTables()){if(bulk.IsChecked==true)chosenTables.Add(name);else chosenTables.Remove(name);}reviewConfirmed=false;UpdateTables();_=Pulse(bulk);};Put(toolbar,bulk,1);Put(toolbar,Action(T("선택 해제","Clear selection"),()=>{foreach(var n in VisibleTables())chosenTables.Remove(n);reviewConfirmed=false;UpdateTables();return Task.CompletedTask;},"quiet"),2);page.Children.Add(toolbar);
        page.Children.Add(Text(T("검색은 선택 범위를 바꾸지 않습니다. 일괄 조작은 검색 결과에만 적용합니다.","Searching preserves selection. Bulk actions affect visible results only."),12,Muted));
        var table=Stack(0);table.Children.Add(TableLine(Row("1.1*,1.1*,.7*,Auto",new Control[]{Text(T("원본 테이블","Source table"),12,Muted),Text(T("대상 테이블","Target table"),12,Muted),Text(T("대응 방식","Matching"),12,Muted),Text("",12)}),true));
        tableList=new ListBox { MaxHeight=360,MinHeight=80,Background=Brushes.Transparent,BorderThickness=new(0),Padding=new(0) };tableList.Classes.Add("table-list");table.Children.Add(tableList);page.Children.Add(Panel(table,new(0)));search.TextChanged+=(_,_)=>{tableQuery=search.Text??"";UpdateTables();};UpdateTables();
        page.Children.Add(Text(T("동일 이름은 자동 대응합니다. ‘대응 변경’에서 테이블·컬럼을 일대일로 지정하세요. 대응은 값을 변환하지 않으며 구조 차이도 결과에 남습니다.","Same names match automatically. Use Change matching to pair tables and columns one to one. Values are not transformed and schema differences remain in the result."),12,Muted));
    }
    private string[] VisibleTables()=>MatchedScopes().Where(s=>(s.Source?.Qualified??"").Contains(tableQuery,StringComparison.OrdinalIgnoreCase)||(s.Target?.Qualified??"").Contains(tableQuery,StringComparison.OrdinalIgnoreCase)).Select(s=>s.Key).ToArray();
    private void UpdateTables()
    {
        if(tableList==null||bulk==null)return;var scopes=MatchedScopes().ToDictionary(s=>s.Key,StringComparer.Ordinal);var names=VisibleTables();var rows=new List<Control>();
        foreach(var name in names)
        {
            var scope=scopes[name];var sourceName=scope.Source?.Qualified;var targetName=scope.Target?.Qualified;
            var box=new AlignedCheckBox { Name="table-"+name,Content=Text(sourceName??T("원본에서 미관측","Not observed in source"),12,Brushes.White),IsChecked=chosenTables.Contains(name) };
            var mapping=WithIcon(Action(T("대응 변경","Change matching"),()=>MatchingDialog(scope),"quiet"),"refresh");mapping.Name="mapping-"+name;mapping.IsEnabled=scope.Source!=null&&!busy;
            var label=sourceName==null?T("대상에만 관측","Target only"):targetName==null?T("대응 없음","Unmatched"):tableMappings.Any(m=>m.Source==sourceName)?T("수동 대응","Manual"):T("동일 이름","Same name");
            var g=Row("1.1*,1.1*,.7*,Auto",new Control[]{box,Text(targetName??T("대상에서 미관측","Not observed in target"),12,Muted),Text(label,12,Muted),mapping});
            var row=TableLine(g);row.Background=BrushOf(chosenTables.Contains(name)?"#353740":"#2b2d31");box.IsCheckedChanged+=(_,_)=>{if(box.IsChecked==true)chosenTables.Add(name);else chosenTables.Remove(name);row.Background=BrushOf(chosenTables.Contains(name)?"#353740":"#2b2d31");reviewConfirmed=false;UpdateBulk();_=Pulse(box);};rows.Add(row);
        }
        if(names.Length==0)rows.Add(TableLine(Text(T("검색 결과가 없습니다. 검색어를 바꿔 보세요.","No results. Change your search."),13,Muted)));tableList.ItemsSource=rows;UpdateBulk();
    }
    private void UpdateBulk()
    {
        if(bulk==null)return;var visible=VisibleTables();var count=visible.Count(chosenTables.Contains);updatingBulk=true;bulk.IsChecked=count==0?false:count==visible.Length?true:null;bulk.IsEnabled=visible.Length>0;updatingBulk=false;if(selectionCount!=null)selectionCount.Text=chosenTables.Count+T(" / "," / ")+TableNames().Length+T("개 선택 · "," selected · ")+visible.Length+T("개 표시"," visible");
    }
    private void TrialPreparation()
    {
        Heading(T("별도 시험 공간을 준비합니다","Prepare a separate test space"),T("원본·대상에는 쓰지 않습니다. 시험 준비·쓰기 기능은 현재 미구현입니다.","No source/target writes. Preparation and behavior tests are not implemented yet."));
        page.Children.Add(Notice(T("이 화면은 준비 방법 안내입니다. 실제 시험 DB를 연결하거나 준비 완료를 판정하지 않습니다.","This screen is preparation guidance. It does not connect or certify a test DB.")));
        var forms=Columns("*,*",20);var left=Stack(16);left.Children.Add(Text(T("별도 시험 DB","Separate test DB"),18,Brushes.White));left.Children.Add(Field(T("서버 / DB","Host / database"),new TextBox { IsEnabled=false,PlaceholderText=T("준비 기능 구현 후 입력","Available when preparation is implemented") }));left.Children.Add(Field(T("시험 계정","Test account"),new TextBox { IsEnabled=false }));Put(forms,Panel(left));var guide=Stack(10);guide.Children.Add(Text(T("준비 확인 순서","Preparation checklist"),18,Brushes.White));guide.Children.Add(Text(T("1. 원본·대상과 분리된 변경 가능한 DB\n2. 시험할 동작·값·기대 결과 확인\n3. 앱 소유 자료의 정리 결과·남은 변경 확인\n\n삭제·롤백만으로 모든 부작용의 원상복구를 보장하지 않습니다.","1. A writable DB separate from source/target\n2. Confirm actions, values and expectations\n3. Review cleanup and remaining changes\n\nDeletion/rollback cannot guarantee reversal of every side effect."),13,Muted));Put(forms,Panel(guide),1);page.Children.Add(forms);
        var actions=Columns("Auto,*",16);var prepare=WithIcon(Action(T("시험 준비 확인 · 미구현","Confirm test setup · pending"),()=>Task.CompletedTask,""),"db");prepare.IsEnabled=false;Put(actions,prepare);Put(actions,Text(T("준비 완료 DB 없음 · 실제 동작 시험 미검증","No prepared DB · behavior remains unverified"),12,Warn),1);page.Children.Add(actions);
    }
    private void Review()
    {
        Heading(T("확인한 범위로 검사를 시작합니다","Review before comparison"),T("선택과 제외, 확인하지 못할 부분을 마지막으로 확인하세요.","Confirm selected, excluded and unverified scope before running."));
        var summary=Stack(0);
        void Add(string label,string value)=>summary.Children.Add(TableLine(Row("170,*",new Control[]{Text(label,13,Muted),Text(value,13,Brushes.White)},alignment:VerticalAlignment.Top)));
        Add(T("원본 / 대상","Source / target"),example?T("합성 원본 / 합성 대상","Synthetic source / target"):drafts[0][2]+" / "+drafts[1][2]);
        Add("DBMS",example?"Synthetic · PostgreSQL":kind.ToString());Add(T("선택 분류","Selected categories"),string.Join(" · ",selected.Select(CategoryText)));Add(T("선택 테이블·대응","Selected tables / matching"),string.Join("\n",MatchedScopes().Where(s=>chosenTables.Contains(s.Key)).Select(s=>MappingLabel(s.Snapshot))));Add(T("제외 범위","Excluded scope"),string.Join(" · ",Enum.GetValues<Category>().Where(c=>!selected.Contains(c)).Select(CategoryText)));Add(T("변경 여부","Write boundary"),T("원본·대상 읽기 전용 / 별도 동작 시험 미구현","Read-only source/target / behavior testing pending"));page.Children.Add(Panel(summary,new(0)));
        foreach(var scope in MatchedScopes().Where(s=>chosenTables.Contains(s.Key)&&s.Source!=null&&s.Target!=null))
            page.Children.Add(Text(MappingLabel(scope.Snapshot)+"\n"+string.Join(" · ",scope.Columns.Select(c=>(c.Source??"—")+" → "+(c.Target??"—"))),12,scope.Complete?Muted:Warn));
        page.Children.Add(Notice(T("구조 검사는 관측한 정의를 비교합니다. 실제 제약 위반·로그인·실행 동작은 별도 검사이며 미구현 범위가 남아 있습니다. 미지원 타입·조회 불가·큰 행은 판정 불가로 표시합니다.","Schema checks compare observed definitions. Constraint violations, logins and execution behavior are separate checks with implementation gaps. Unsupported types and unreadable or oversized rows remain indeterminate.")));
        var confirm=new AlignedCheckBox { Name="review-confirm",Content=T("선택 범위와 제한을 확인했고 양쪽 쓰기를 멈췄습니다.","I confirmed scope/limits and stopped writes on both databases."),IsChecked=reviewConfirmed };confirm.IsCheckedChanged+=(_,_)=>{reviewConfirmed=confirm.IsChecked==true;focusAfterRender=confirm;Render();};page.Children.Add(confirm);
    }
    private void Progress()
    {
        Heading(T("선택한 범위를 비교하고 있습니다","Comparing the selected scope"),T("원본과 대상의 값을 읽고 정확히 대조합니다. 완료 전에는 전체 일치를 판정하지 않습니다.","Reading and comparing exact values. Full equality is never declared before completion."));
        var main=Stack(16);main.Children.Add(Text(T("현재 처리량","Observed work"),12,Muted));progressText=Text(T("읽기 시작을 기다립니다.","Waiting for reading to start."),24,Brushes.White);main.Children.Add(progressText);main.Children.Add(new ProgressBar { IsIndeterminate=!reducedMotion,Value=0,Height=6 });main.Children.Add(Text(T("전체 행 수를 모르면 완료 백분율이나 잔여 시간을 만들지 않습니다.","No invented percentage or ETA when total rows are unknown."),12,Muted));page.Children.Add(Panel(main));
        progressStates.Clear();var table=Stack(0);table.Children.Add(TableLine(Row("*,*,*",new Control[]{Text(T("분류","Category"),12,Muted),Text(T("실행 범위","Scope"),12,Muted),Text(T("상태","State"),12,Muted)}),true));foreach(var c in Enum.GetValues<Category>()){var state=Text(selected.Contains(c)?T("조회 대기 · 미구현 항목 별도","Awaiting inspection · pending checks separate"):T("미시작","Not started"),12,selected.Contains(c)?Warn:Muted);progressStates[c]=state;table.Children.Add(TableLine(Row("*,*,*",new Control[]{Text(CategoryText(c),14),Text(selected.Contains(c)?T("선택됨","Selected"):T("선택 제외","Excluded"),12,Muted),state})));}page.Children.Add(Panel(table,new(0)));
        page.Children.Add(Text(T("중단 요청은 현재 작업을 종료하고 이미 확인한 차이와 미검증 이유를 보관합니다. 이어하기 대신 전체 재검사만 제공합니다.","Stopping finalizes known differences and unknown scope. Only full new runs are available."),13,Muted));
        if(latestProgress!=null)UpdateProgress(latestProgress);
    }
}
