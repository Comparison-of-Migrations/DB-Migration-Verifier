using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.LogicalTree;
using Avalonia.Media;
using Verifier.Core;

namespace Verifier.Desktop;

public sealed partial class MainWindow
{
    private readonly HashSet<ReportFormat> selectedFormats=[ReportFormat.Json];
    private static bool Unknown(CheckResult c)=>c.Selected&&(c.Execution!=Execution.Completed||c.Reason!=Reason.None||c.Issues.Count>0||c.Verdict is Verdict.Unverified or Verdict.Indeterminate);
    private void Results()
    {
        if(report==null)return;
        Heading(report.HasDifferences?T("불일치가 발견됐습니다","Differences found"):report.AllEqual?T("선택 범위 내 일치","Selected scope is equal"):T("확인하지 못한 영역이 있습니다","Some scope remains unverified"),T("값·정의의 차이와 확인하지 못한 부분을 함께 봅니다. 선택하지 않은 범위는 검증한 것으로 표시하지 않습니다.","Review differences and gaps together. Excluded scope is never marked as verified."));
        if(report.Example)page.Children.Add(Notice(T("합성 예시 실행 결과입니다. 실제 사용자 DB의 검사 결과가 아닙니다.","Executed synthetic example. Not an actual user database result.")));
        var selectedChecks=report.Checks.Where(x=>x.Selected).ToArray();var different=selectedChecks.Count(c=>c.Verdict==Verdict.Different);var unknown=selectedChecks.Count(Unknown);var equal=selectedChecks.Count(c=>c.Verdict==Verdict.Equal);
        var metrics=Columns("Auto,Auto,Auto,*",40);
        var labels=english?new[]{"Different checks","Unknown / unverified","Equal checks"}:new[]{"불일치 검사","확인 불가·미검증 검사","일치 검사"};var values=new[]{different,unknown,equal};var colors=new[]{Bad,Warn,Good};
        for(var i=0;i<3;i++){var s=Stack(4);s.Children.Add(Text(labels[i],12,Muted));var n=Text(values[i].ToString(),32,colors[i]);n.FontWeight=FontWeight.SemiBold;s.Children.Add(n);Put(metrics,s,i);_=Enter(n,220);}var run=Stack(2);run.HorizontalAlignment=HorizontalAlignment.Right;run.Children.Add(Text("Run ID",12,Muted));run.Children.Add(Text(report.RunId.ToString("N")[..12],12,Muted));Put(metrics,run,3);page.Children.Add(metrics);page.Children.Add(Line());
        page.Children.Add(Text(T("분류별 비교 요약","Comparison by category"),18,Brushes.White));
        var columns="1.4*,*,*,*,Auto";
        var summaries=Stack(0);summaries.Children.Add(TableLine(Row(columns,new Control[]{Text(T("분류","Category"),12,Muted),Text(T("불일치","Different"),12,Bad),Text(T("확인 불가·미검증","Unknown / unverified"),12,Warn),Text(T("일치","Equal"),12,Good),Text("",12)}),true));
        var max=Math.Max(1,report.Checks.GroupBy(c=>c.Category).Max(g=>g.Count()));
        foreach(var c in new[]{Category.Data,Category.Schema,Category.Access,Category.Objects})
        {
            var key=c;var entries=report.Checks.Where(x=>x.Category==c&&x.Selected).ToArray();var names=Stack(3);var title=Text(CategoryText(c),14,Brushes.White);title.FontWeight=FontWeight.SemiBold;names.Children.Add(title);names.Children.Add(Text(entries.Length==0?T("선택 제외","Not selected"):entries.Any(e=>e.Execution==Execution.Interrupted)?T("중단·부분 결과","Interrupted / partial"):entries.All(e=>e.Execution==Execution.Pending)?T("미시작·미검증","Not started / unverified"):T("실행·미검증 범위 포함","Executed and unverified scope"),11,Muted));
            var b=Action(CategoryText(c)+T(" 세부 보기"," details"),()=>{detail=detail==key?null:key;Render();Avalonia.Threading.Dispatcher.UIThread.Post(()=>{var heading=page.GetLogicalDescendants().OfType<Control>().FirstOrDefault(c=>c.Name=="result-detail");if(heading!=null&&bodyScroll!=null){bodyScroll.Offset=new(0,heading.TranslatePoint(default,page)?.Y??0);heading.Focus();}},Avalonia.Threading.DispatcherPriority.Loaded);return Task.CompletedTask;},"result-row");b.Content=Row(columns,new Control[]{names,CountBar(entries.Count(x=>x.Verdict==Verdict.Different),max,Bad),CountBar(entries.Count(Unknown),max,Warn),CountBar(entries.Count(x=>x.Verdict==Verdict.Equal),max,Good),UiIcon(detail==c?"down":"chevron")});summaries.Children.Add(b);
        }
        page.Children.Add(Panel(summaries,new(0)));page.Children.Add(Text(T("같은 길이는 같은 검사 ID 수입니다. 차이와 확인 불가가 공존하면 양쪽에 포함됩니다. 세 막대는 독립적이며 합계·DB 성공률이 아닙니다.","Equal lengths mean equal numbers of check IDs. A check may have both differences and unknown scope. Bars are independent, not a total or DB success rate."),12,Muted));
        var excluded=report.Checks.Where(x=>!x.Selected).Select(x=>x.Category).Distinct().Select(CategoryText);page.Children.Add(Text(T("선택 제외: ","Excluded: ")+string.Join(" · ",excluded),12,Muted));
        var trial=report.Checks.Where(x=>x.Category==Category.Behavior).ToArray();page.Children.Add(Panel(Row("*,Auto",new Control[]{Text(T("별도 DB 동작 시험","Separate DB behavior test"),14,Brushes.White),Pill(trial.Any(x=>x.Selected)?T("미검증","Unverified"):T("선택 안 함 · 미구현","Not selected · pending"),Warn)}),new(18)));
        var filter=Columns("*,Auto,Auto,Auto",8);var detailTitle=Text(detail==null?T("차이와 확인 불가 이유","Differences and visibility limits"):CategoryText(detail.Value)+T(" · 세부 검사"," · detailed checks"),18,Brushes.White);detailTitle.Name="result-detail";detailTitle.Focusable=true;Put(filter,detailTitle);
        var options=new[]{("all",T("전체","All")),("different",T("불일치","Different")),("unknown",T("확인 불가","Unverified"))};
        for(var i=0;i<options.Length;i++){var item=options[i];var btn=Action(item.Item2,()=>{resultFilter=item.Item1;Render();return Task.CompletedTask;},resultFilter==item.Item1?"selected-filter":"quiet");Put(filter,btn,i+1);}page.Children.Add(filter);
        var checks=report.Checks.Where(x=>x.Selected&&(detail==null||x.Category==detail)&&(resultFilter=="all"||resultFilter=="different"&&x.Verdict==Verdict.Different||resultFilter=="unknown"&&Unknown(x))).ToArray();
        var items=new List<Control>();
        foreach(var entry in checks)
        {
            var h=Columns("*,Auto");Put(h,Text(entry.CheckId+" · "+entry.ResearchId,13,Brushes.White));Put(h,Pill(VerdictText(entry),entry.Verdict==Verdict.Different?Bad:entry.Verdict==Verdict.Equal?Good:Warn),1);var content=Stack(8);content.Children.Add(h);
            if(entry.Reason!=Reason.None)content.Children.Add(Text(ReasonText(entry.Reason),12,Muted));
            foreach(var issue in entry.Issues)content.Children.Add(Text(issue.Table+(issue.Column==null?"":" · "+issue.Column)+" · "+ReasonText(issue.Reason),12,Warn));
            foreach(var f in entry.Findings){var finding=Stack(3);finding.Children.Add(Text((f.Table??"")+(f.Column==null?"":" · "+f.Column)+" · "+FindingText(f.Kind)+" × "+f.Count,12,Muted));finding.Children.Add(Text(T("내부 ID: ","Internal ID: ")+f.Id,11,Muted));content.Children.Add(finding);}
            items.Add(Panel(content,new(18)));
        }
        if(items.Count==0)items.Add(Panel(Text(T("이 필터에 해당하는 항목이 없습니다.","No checks match this filter."),13,Muted)));
        var list=new ListBox { ItemsSource=items,MaxHeight=420,MinHeight=80,Padding=new(0),Background=Brushes.Transparent,BorderThickness=new(0) };list.Classes.Add("detail-list");page.Children.Add(list);
        page.Children.Add(Text(T("실제 값·행 키·계정 이름은 표시하지 않습니다. 선택 테이블: ","Row values, keys and principal names are excluded. Selected tables: ")+string.Join(" · ",report.Tables),12,Muted));page.Children.Add(Action(T("연구 반영 보기","Research mapping"),ResearchDialog,"quiet"));
        if(report.Version==2)
            page.Children.Add(Action(T("검사에 사용한 대응 보기","View executed matching"),()=>InfoDialog(T("실제로 비교한 테이블·컬럼 대응","Executed table and column matching"),string.Join("\n\n",report.Mappings.Select(m=>MappingLabel(m)+"\n"+string.Join("\n",m.Columns.Select(c=>(c.Source??"—")+" → "+(c.Target??"—")))))),"quiet"));
        else page.Children.Add(Text(T("형식 1 결과 · 별도 대응 기록이 없는 기존 파일입니다.","Version 1 result · legacy file without a separate mapping record."),12,Muted));
    }
    private Control CountBar(int count,int max,IBrush color)
    {
        var s=Stack(6);s.Children.Add(Text(count.ToString(),18,color));var bar=Columns(count+"*,"+Math.Max(0,max-count)+"*",0);bar.Height=4;bar.Background=BrushOf("#41434a");var fill=new Border { Background=color,Height=4,CornerRadius=new(2) };Put(bar,fill);if(count==0)fill.IsVisible=false;s.Children.Add(bar);_=Enter(fill,320);return s;
    }
    private void Save()
    {
        Heading(T("확인한 결과를 저장합니다","Save verified results"),T("검사 범위·판정·미검증 이유를 보관합니다. 실제 값·키·접속 정보는 넣지 않습니다.","Keep scope, verdicts and unverified reasons. No values, row keys or credentials."));
        var summary=Stack(0);summary.Children.Add(TableLine(Row("170,*",new Control[]{Text(T("검사 ID","Run ID"),13,Muted),Text(report?.RunId.ToString()??"",13)},alignment:VerticalAlignment.Top)));summary.Children.Add(TableLine(Row("170,*",new Control[]{Text(T("포함 항목","Included"),13,Muted),Text(T("선택 범위·실행 상태·판정·이유·위치·내부 번호","Scope · execution · verdicts · reasons · locations · internal IDs"),13)},alignment:VerticalAlignment.Top)));summary.Children.Add(TableLine(Row("170,*",new Control[]{Text(T("제외 항목","Excluded"),13,Muted),Text(T("실제 값·행 키·비밀번호·연결 문자열","Values · row keys · passwords · connection strings"),13)},alignment:VerticalAlignment.Top)));page.Children.Add(Panel(summary,new(0)));
        page.Children.Add(Text(T("저장할 형식","Report formats"),18,Brushes.White));var formats=Columns("*,*,*",12);
        foreach(var format in Enum.GetValues<ReportFormat>())
        {
            var name=format.ToString().ToUpperInvariant();var labels=Stack(6);labels.MinHeight=64;labels.VerticalAlignment=VerticalAlignment.Center;labels.Children.Add(Text(name,17,Brushes.White));
            labels.Children.Add(Text(format switch { ReportFormat.Json=>T("앱에서 다시 열기","Reopen in the app"),ReportFormat.Csv=>T("스프레드시트로 읽기","Read in a spreadsheet"),_=>T("브라우저로 읽기","Read in a browser") },12,Muted));
            var box=new AlignedCheckBox { Name="format-"+name,Content=labels,IsChecked=selectedFormats.Contains(format),IsEnabled=!busy,HorizontalAlignment=HorizontalAlignment.Stretch,HorizontalContentAlignment=HorizontalAlignment.Stretch,MinHeight=100 };
            Avalonia.Automation.AutomationProperties.SetName(box,name+T(" 저장 형식"," report format"));var panel=Panel(box,new(20));
            box.IsCheckedChanged+=(_,_)=>{if(box.IsChecked==true)selectedFormats.Add(format);else selectedFormats.Remove(format);focusAfterRender=box;Render();};Put(formats,panel,(int)format);
        }
        page.Children.Add(formats);
        page.Children.Add(Text(T("선택한 형식마다 저장 위치를 확인합니다. CSV·HTML에도 필터와 관계없이 모든 검사·차이·미검증·선택 제외를 담습니다. 앱에서 다시 열기는 JSON만 가능합니다.","Choose a destination for each selected format. CSV and HTML include every check, difference, unknown area and exclusion, regardless of the screen filter. Only JSON reopens in the app."),12,Muted));
        page.Children.Add(Text(T("JSON checksum은 우발적 손상 확인용이며 작성자 인증 서명이 아닙니다. CSV의 수식처럼 보이는 이름은 작은따옴표로 보호하며, 원래 이름은 JSON에 보존합니다.","The JSON checksum detects accidental corruption; it is not an author signature. Formula-like names in CSV are guarded with an apostrophe; JSON preserves original names."),12,Muted));
        var actions=Columns("Auto,Auto,*",12);Put(actions,WithIcon(Action(T("검사 결과 파일로 보기","View a result file"),OpenReport,""),"file"));Put(actions,WithIcon(Action(T("새 검사 시작","Start a new check"),async()=>{await Reset();await Go(1);},"quiet"),"refresh"),1);page.Children.Add(actions);
    }
}
