using Avalonia;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Input;
using Avalonia.Threading;
using Verifier.Core;
using Verifier.Data;

namespace Verifier.Desktop;

public sealed partial class MainWindow
{
    private void Home()
    {
        Heading(T("마이그레이션 결과를 비교하세요","Verify your migration results"),T("옮긴 뒤의 두 DB를 비교하고, 확인한 범위까지 분명하게.","Compare the two databases and see exactly what has been verified."));
        var wide=Bounds.Width>1180;var welcome=Columns(wide?"1.3*,0.7*":"*",40);
        var help=Text(T("마이그레이션을 마친 개발자가 논문의 다축 검증 방법으로 두 DB의 데이터·구조·권한·객체를 비교하고, 차이와 확인하지 못한 영역을 찾도록 돕습니다.","Helps developers after migration compare data, schema, permissions and objects using the paper’s multi-axis approach, and identify differences and unverified areas."),15,Muted);help.LineHeight=28;help.MaxWidth=740;help.HorizontalAlignment=HorizontalAlignment.Left;help.VerticalAlignment=VerticalAlignment.Top;Put(welcome,help);
        var preview=Stack(8);var reportHead=Columns("*,Auto");Put(reportHead,Text(T("예시 결과 미리보기","Sample result preview"),12,Muted));Put(reportHead,UiIcon("file"),1);preview.Children.Add(reportHead);var headLine=Line();headLine.Margin=new(0,2);preview.Children.Add(headLine);var pt=Text(T("차이와 확인 범위를\n함께 봅니다.","Differences and verified\nscope, together."),15,Brushes.White);pt.LineHeight=23;pt.FontWeight=FontWeight.SemiBold;preview.Children.Add(pt);
        var metricLabels=english?new[]{"Different checks","Unverified checks","Equal checks"}:new[]{"불일치 항목","확인 불가·미검증","일치 항목"};var nums=new[]{3,2,45};var colors=new[]{Bad,Warn,Brushes.White};
        for(var i=0;i<3;i++){var r=Columns("*,Auto");Put(r,Text(metricLabels[i],12,Muted));Put(r,Text(nums[i].ToString(),17,colors[i]),1);preview.Children.Add(r);if(i<2){var rowLine=Line();rowLine.Margin=new(0);preview.Children.Add(rowLine);}}
        var previewNote=Text(T("위 수치는 웹 시안의 합성 설명 예시입니다.\n실제 예시 실행 결과와 구분합니다.","Static synthetic counts from the design preview.\nSeparate from the executed example result."),10,Muted);previewNote.LineHeight=15;preview.Children.Add(previewNote);if(wide)Put(welcome,Panel(preview,new(22,18)),1);page.Children.Add(welcome);
        var actions=Columns("Auto,Auto,*",12);
        var start=WithIcon(Action(T("새 검사 시작","Start a new check"),async()=>{await Reset();await Go(1);},"primary",224),"arrow",true);start.Height=52;Put(actions,start);
        var open=WithIcon(Action(T("검사 결과 파일로 보기","View a result file"),OpenReport,"",240),"file");open.Height=52;Put(actions,open,1);
        var demo=Action(T("예시 결과 보기","See sample results"),async()=>{await PrepareExample();await Run();},"quiet");demo.Height=52;Put(actions,demo,2);page.Children.Add(actions);
        var hint=Columns("Auto,*",10);Put(hint,UiIcon("info",Muted,16));Put(hint,Text(T("저장한 결과 JSON은 DB 연결이나 새 검사 없이 바로 열어 확인할 수 있습니다.","Open a saved result JSON without connecting to a DB or running a new check."),12,Muted),1);page.Children.Add(hint);page.Children.Add(Line());
        var steps=Columns("*,*,*",32);steps.RowDefinitions=new("Auto,Auto,Auto");steps.RowSpacing=8;
        var titles=english?new[]{"Connect two databases","Choose what to compare","Review differences and gaps"}:new[]{"두 DB를 연결합니다","범위를 고르고 비교합니다","차이와 미검증을 확인합니다"};
        var desc=english?new[]{"The app checks visibility and explains additional preparation.","Select categories and confirm how tables are matched.","Review verified outcomes and save a report without row values."}:new[]{"접속 후 앱이 조회 가능한 범위와 필요한 준비를 안내합니다.","데이터·구조·권한·객체를 선택하고 테이블 대응을 확인합니다.","확인한 결과를 구분해 보고, 안전한 결과 파일로 보관합니다."};
        for(var i=0;i<3;i++){var number=Text("0"+(i+1),12,Muted);number.VerticalAlignment=VerticalAlignment.Top;Put(steps,number,i);var title=Text(titles[i],14,Brushes.White);title.FontWeight=FontWeight.SemiBold;title.VerticalAlignment=VerticalAlignment.Top;Put(steps,title,i,1);var description=Text(desc[i],13,Muted);description.VerticalAlignment=VerticalAlignment.Top;Put(steps,description,i,2);}page.Children.Add(steps);page.Children.Add(Line());
        var paperRow=Columns("Auto,*,Auto",16);Put(paperRow,UiIcon("file",Muted,24));var paper=Stack(6);paper.Children.Add(Text(T("이 앱의 연구 근거","Research behind this app"),12,Muted));var paperTitle=Text(Paper,13,Brushes.White);paperTitle.FontWeight=FontWeight.SemiBold;paper.Children.Add(paperTitle);paper.Children.Add(Text(T("기본 기능 성공과 넓은 DB 계약 충족을 구분하는 연구를 검사 분류와 미검증 표시에 반영합니다.","The distinction between functional success and wider contracts informs check categories and unverified states."),12,Muted));Put(paperRow,paper,1);Put(paperRow,Action(T("연구 반영 보기","Research mapping"),ResearchDialog,"quiet"),2);page.Children.Add(paperRow);
    }
    private async Task PrepareExample()
    {
        await Reset();example=true;kind=DbKind.PostgreSql;source=new ExampleDatabase();target=new ExampleDatabase(true);sourceInfo=await source.InspectAsync(default);targetInfo=await target.InspectAsync(default);foreach(var scope in MatchedScopes())chosenTables.Add(scope.Key);connected=true;writesStopped=true;selected.Clear();selected.UnionWith([Category.Data,Category.Schema,Category.Access,Category.Objects]);visited.UnionWith([1,2]);
    }
    private void Connections()
    {
        Heading(T("비교할 두 DB를 연결하세요","Connect the two databases"),T("같은 DBMS의 원본과 대상을 지정합니다. 접속 정보는 결과 파일에 넣지 않습니다.","Choose source and target on the same DBMS. Credentials stay out of reports."));
        page.Children.Add(Text(T("비교할 데이터베이스 종류","Database type"),12,Brushes.White));var cards=Columns("*,*,*,*",10);
        var names=new[]{"SQL Server","PostgreSQL","MySQL","MariaDB"};var ports=new[]{1433,5432,3306,3306};
        for(var i=0;i<4;i++)
        {
            var index=i;var supported=i<2;var k=i==0?DbKind.SqlServer:DbKind.PostgreSql;
            var radio=new RadioButton { Name="dbms-"+i,GroupName="dbms",HorizontalAlignment=HorizontalAlignment.Stretch,IsChecked=supported&&kind==k,IsEnabled=!busy&&supported };radio.Classes.Add("db-card");AutomationProperties.SetName(radio,names[i]+(supported?"":T(" · 준비 중"," · pending")));
            var content=Columns("*,Auto",8);var labels=Stack(2);var title=Text(names[i],13,Brushes.White);title.FontWeight=FontWeight.SemiBold;labels.Children.Add(title);labels.Children.Add(Text(supported?(i==0?"Microsoft":"PostgreSQL")+" · "+ports[i]:T("준비 중 · ","Pending · ")+ports[i],11,Muted));Put(content,labels);
            var dot=new Border { VerticalAlignment=VerticalAlignment.Center,Width=18,Height=18,CornerRadius=new(9),BorderBrush=BrushOf(radio.IsChecked==true?"#5865f2":"#7c838c"),BorderThickness=new(2),Background=BrushOf(radio.IsChecked==true?"#5865f2":"#2b2d31"),Child=radio.IsChecked==true?new Border { Width=6,Height=6,Background=BrushOf("#232428"),CornerRadius=new(3),VerticalAlignment=VerticalAlignment.Center,HorizontalAlignment=HorizontalAlignment.Center }:null };Put(content,dot,1);radio.Content=content;
            void ChooseDatabase(int next)
            {
                var nextKind=next==0?DbKind.SqlServer:DbKind.PostgreSql;
                if(kind==nextKind)return;
                CaptureInputs();kind=nextKind;drafts[0][1]=drafts[1][1]=ports[next].ToString();connected=false;reviewConfirmed=false;visited.RemoveWhere(n=>n>1);
                focusAfterRender=cards.Children.OfType<RadioButton>().FirstOrDefault(x=>x.Name=="dbms-"+next)??radio;
                // Complete RadioButton group changes before replacing the visual tree.
                Dispatcher.UIThread.Post(Render);
            }
            radio.IsCheckedChanged+=(_,_)=>{if(radio.IsChecked==true)ChooseDatabase(index);};
            radio.KeyDown+=(_,e)=>{if(e.Key is not (Key.Left or Key.Right or Key.Up or Key.Down))return;e.Handled=true;ChooseDatabase(index==0?1:0);};
            Put(cards,radio,i);
        }
        page.Children.Add(cards);page.Children.Add(Text(T("양쪽은 같은 종류로 비교합니다. MySQL·MariaDB 연결은 아직 구현되지 않았습니다.","Both sides use the same type. MySQL/MariaDB connections are not implemented yet."),12,Muted));
        var forms=Columns("*,*",20);inputs=new TextBox[2][];
        for(var side=0;side<2;side++)
        {
            var panel=Stack(16);var head=Columns("Auto,*,Auto",14);Put(head,new Border { Child=UiIcon("db"),Padding=new(10),CornerRadius=new(8),Background=BrushOf("#34363c") });var namesPanel=Stack(2);var title=Text(side==0?T("원본 DB","Source DB"):T("대상 DB","Target DB"),18,Brushes.White);title.FontWeight=FontWeight.SemiBold;namesPanel.Children.Add(title);namesPanel.Children.Add(Text(side==0?T("마이그레이션 전 상태","Before migration"):T("마이그레이션 후 상태","After migration"),12,Muted));Put(head,namesPanel,1);Put(head,Pill(connected?T("연결 확인","Connected"):T("미확인","Not checked"),connected?Good:Muted),2);panel.Children.Add(head);panel.Children.Add(Line());
            inputs[side]=new TextBox[6];var fieldNames=english?new[]{"Host","Port","Database","Reader account","Password","CA certificate path (optional)"}:new[]{"서버 주소","포트","데이터베이스","조회 계정","비밀번호","CA 인증서 경로 (선택)"};
            for(var i=0;i<6;i++){var box=i==4?new PasswordTextBox { PasswordChar='●' }:new TextBox();box.Name="input-"+side+"-"+i;box.Text=drafts[side][i];box.IsEnabled=!busy&&(i!=5||kind==DbKind.PostgreSql);AutomationProperties.SetName(box,(side==0?T("원본 ","Source "):T("대상 ","Target "))+fieldNames[i]);inputs[side][i]=box;box.TextChanged+=(_,_)=>{if(!connected)return;connected=false;reviewConfirmed=false;visited.RemoveWhere(n=>n>1);};}
            var address=Columns("*,112",16);Put(address,Field(fieldNames[0],inputs[side][0]));Put(address,Field(fieldNames[1],inputs[side][1]),1);panel.Children.Add(address);
            for(var i=2;i<5;i++)panel.Children.Add(Field(fieldNames[i],inputs[side][i]));panel.Children.Add(Text(T("비밀번호는 기본 미저장입니다. OS 보안 저장소는 후속 구현입니다.","Passwords are not saved. OS credential storage is pending."),11,Muted));
            if(advanced)panel.Children.Add(Field(fieldNames[5],inputs[side][5]));Put(forms,Panel(panel,new(24)),side);
        }
        page.Children.Add(forms);
        Button? advancedToggle=null;
        advancedToggle=WithIcon(Action(T("고급 연결 설정","Advanced connection settings"),()=>{CaptureInputs();advanced=!advanced;focusAfterRender=advancedToggle;Render();_=Enter(page,160);return Task.CompletedTask;},"quiet"),advanced?"down":"chevron");advancedToggle.Name="advanced-settings";page.Children.Add(advancedToggle);
        if(advanced){var note=Columns("*,*",24);var tls=Stack(8);tls.Children.Add(Text("TLS · "+T("인증서 검증","certificate verification"),15,Brushes.White));tls.Children.Add(Text(T("인증서와 서버 이름을 검증합니다. 실패하면 보안을 낮춰 재시도하지 않습니다.","Certificates and host names are verified. No automatic security downgrade."),13,Muted));Put(note,tls);var auth=Stack(8);auth.Children.Add(Text(T("인증 · 제한 시간","Authentication · timeouts"),15,Brushes.White));auth.Children.Add(Text(T("DB 계정 인증 · 드라이버의 연결/명령 제한. MS SQL 실제 서버 검증과 다른 인증 방식은 아직 남아 있습니다.","DB-account authentication with connection/command limits. SQL Server validation and other auth modes remain pending."),13,Muted));Put(note,auth,1);page.Children.Add(Panel(note));}
        stopped=new AlignedCheckBox { Name="writes-stopped",Content=T("원본과 대상의 쓰기를 모두 멈췄습니다.","Writes are stopped on both databases."),IsChecked=writesStopped,IsEnabled=!busy };stopped.IsCheckedChanged+=(_,_)=>{writesStopped=stopped.IsChecked==true;_=Pulse(stopped);};page.Children.Add(stopped);
        page.Children.Add(Action(T("DB 없이 합성 예시 흐름 보기","Explore a synthetic flow without a DB"),async()=>{await PrepareExample();await Go(2);},"quiet"));
    }
    private static StackPanel Field(string label,Control input){var s=Stack(6);s.Children.Add(Text(label,12,Muted));s.Children.Add(input);return s;}
}
