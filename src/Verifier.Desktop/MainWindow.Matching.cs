using Avalonia;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Threading;
using Verifier.Core;

namespace Verifier.Desktop;

public sealed partial class MainWindow
{
    private const string AutomaticChoice="\0automatic";
    private sealed record MatchingChoice(string? Value,string Label,bool Enabled,string? Hint=null,bool Current=false);

    private async Task MatchingDialog(MatchedScope scope)
    {
        if(scope.Source==null||sourceInfo==null||targetInfo==null)return;
        var caller=FocusManager?.GetFocusedElement() as Control;var left=scope.Source;
        var previous=tableMappings.SingleOrDefault(m=>m.Source==left.Qualified);
        var automatic=previous==null;var destination=scope.Target;
        var working=(previous?.Columns??[]).ToList();
        var dialog=new Window { Title=T("테이블·컬럼 대응","Table and column matching"),Width=840,Height=640,MinWidth=700,MinHeight=500,WindowStartupLocation=WindowStartupLocation.CenterOwner,Background=BrushOf("#2b2d31") };
        var grid=new Grid { RowDefinitions=new("Auto,*,Auto") };var heading=Stack(8);heading.Margin=new(24,20);
        heading.Children.Add(Text(T("원본을 어느 대상과 비교할까요?","Which target should this source match?"),22,Brushes.White));heading.Children.Add(Text(left.Qualified,13,Accent));
        heading.Children.Add(Text(T("값을 변환하지 않습니다. 모든 컬럼을 대응하고 이름·타입·순서의 차이도 결과에 남깁니다.","Values are not transformed. Map every column; name, type and order differences remain in results."),12,Muted));Put(grid,heading);
        var scroll=new ScrollViewer();Put(grid,scroll,0,1);
        var footer=Columns("*,Auto,Auto",12);footer.Margin=new(24,16);var status=Text("",12,Warn);Put(footer,status);
        var cancel=Action(T("취소","Cancel"),()=>{dialog.Close();return Task.CompletedTask;},"quiet");Put(footer,cancel,1);
        var apply=Action(T("대응 적용","Apply matching"),()=>
        {
            var candidates=Candidate();var after=ScopeMatching.Resolve(sourceInfo,targetInfo,candidates);var all=chosenTables.SetEquals(MatchedScopes().Select(s=>s.Key));
            tableMappings.Clear();tableMappings.AddRange(candidates);
            if(all){chosenTables.Clear();chosenTables.UnionWith(after.Select(s=>s.Key));}else chosenTables.IntersectWith(after.Select(s=>s.Key));
            reviewConfirmed=false;dialog.Close();ShowMessage(T("대응을 적용했습니다. 검사 대상과 제외 범위를 다시 확인하세요.","Matching applied. Review the selected and excluded scope again."),MessageTone.Information);focusAfterRender=caller;Render();return Task.CompletedTask;
        },"primary",164);Put(footer,apply,2);Put(grid,footer,0,2);
        dialog.Content=grid;dialog.KeyDown+=(_,e)=>{if(e.Key==Key.Escape){e.Handled=true;dialog.Close();}};
        void Draw(string? restoreFocus=null)
        {
            Control? focus=null;
            var offset=scroll.Offset;var body=Stack(16);body.Margin=new(24,8,24,24);
            var choose=WithIcon(Action(T("대상 테이블 선택","Choose target table"),async()=>
            {
                var owners=MatchedScopes().Where(s=>s.Source!=null&&s.Source.Qualified!=left.Qualified&&s.Target!=null).ToDictionary(s=>s.Target!.Qualified,s=>s.Source!.Qualified,StringComparer.Ordinal);
                var choices=new List<MatchingChoice> {new(AutomaticChoice,T("동일 이름 자동 대응","Automatic same-name matching"),true,Current:automatic),new(null,T("대응 없음","Unmatched"),true,T("데이터는 판정 불가, 구조 차이는 보존합니다.","Data remains indeterminate; schema differences are preserved."),!automatic&&destination==null)};
                foreach(var table in targetInfo.Tables.OrderBy(t=>t.Qualified,StringComparer.Ordinal))
                { var taken=owners.TryGetValue(table.Qualified,out var owner);choices.Add(new(table.Qualified,table.Qualified,!taken,taken?T("다른 원본과 대응 중: ","Paired with another source: ")+owner:null,!automatic&&destination?.Qualified==table.Qualified)); }
                var chosen=await PickMatching(dialog,T("대상 테이블 선택","Choose target table"),choices);
                if(!chosen.Accepted)return;
                automatic=chosen.Value==AutomaticChoice;working.Clear();
                destination=automatic?ScopeMatching.Resolve(sourceInfo,targetInfo,tableMappings.Where(m=>m.Source!=left.Qualified).ToArray()).Single(s=>s.Source?.Qualified==left.Qualified).Target:targetInfo.Tables.SingleOrDefault(t=>t.Qualified==chosen.Value);
                Draw("mapping-target");_=Enter(scroll,160);
            },"",null),"db");choose.Name="mapping-target";choose.HorizontalAlignment=HorizontalAlignment.Stretch;choose.HorizontalContentAlignment=HorizontalAlignment.Stretch;
            body.Children.Add(choose);body.Children.Add(Text(T("현재 대상: ","Current target: ")+(destination?.Qualified??T("대응 없음","Unmatched"))+" · "+(automatic?T("자동","Automatic"):T("수동","Manual")),13,destination==null?Warn:Accent));
            if(restoreFocus==choose.Name)focus=choose;
            var columns=ScopeMatching.ResolveColumns(left,destination,working);
            var list=Stack(0);list.Children.Add(TableLine(Row("*,*,Auto",new Control[]{Text(T("원본 컬럼 / 타입","Source column / type"),12,Muted),Text(T("대상 컬럼 / 타입","Target column / type"),12,Muted),Text("",12)}),true));
            foreach(var column in left.Columns)
            {
                var current=columns.Single(c=>c.Source==column.Name);var targetColumn=destination?.Columns.SingleOrDefault(c=>c.Name==current.Target);
                var sourceLabel=Stack(2);sourceLabel.Children.Add(Text(column.Name,13,Brushes.White));sourceLabel.Children.Add(Text(column.Type,12,Muted));
                var targetLabel=Stack(2);targetLabel.Children.Add(Text(current.Target??T("대응 없음","Unmatched"),13,current.Target==null?Warn:Muted));targetLabel.Children.Add(Text(targetColumn?.Type??"—",12,Muted));
                var change=Action(T("컬럼 대응 선택 · ","Match column · ")+column.Name,async()=>
                {
                    if(destination==null)return;
                    var owners=ScopeMatching.ResolveColumns(left,destination,working).Where(c=>c.Source!=null&&c.Source!=column.Name&&c.Target!=null).ToDictionary(c=>c.Target!,c=>c.Source!,StringComparer.Ordinal);
                    var choices=new List<MatchingChoice>{new(null,T("대응 없음","Unmatched"),true,T("해당 컬럼을 제외해 전체 일치로 표시하지 않습니다.","An unmatched column prevents full data equality."),current.Target==null)};
                    foreach(var target in destination.Columns)
                    { var taken=owners.TryGetValue(target.Name,out var owner);choices.Add(new(target.Name,target.Name,!taken,target.Type+(taken?T(" · 다른 컬럼과 대응: "," · already paired: ")+owner:""),target.Name==current.Target)); }
                    var chosen=await PickMatching(dialog,T("대상 컬럼 선택 · ","Choose target column · ")+column.Name,choices);if(!chosen.Accepted)return;
                    automatic=false;working.RemoveAll(c=>c.Source==column.Name);working.Add(new(column.Name,chosen.Value));Draw("mapping-column-"+column.Name);_=Enter(scroll,160);
                },"quiet");change.Content=UiIcon("refresh");change.Width=44;change.IsEnabled=destination!=null;change.Name="mapping-column-"+column.Name;
                list.Children.Add(TableLine(Row("*,*,Auto",new Control[]{sourceLabel,targetLabel,change})));
                if(restoreFocus==change.Name)focus=change;
            }
            if(left.Columns.Count==0)list.Children.Add(TableLine(Text(T("컬럼이 없는 테이블도 전체 행 개수를 포함해 비교합니다.","Zero-column tables are still compared including row multiplicity."),12,Muted)));
            body.Children.Add(Panel(list,new(0)));
            var extra=columns.Where(c=>c.Source==null).Select(c=>c.Target).ToArray();
            if(extra.Length>0)body.Children.Add(Notice(T("대응되지 않은 대상 컬럼: ","Unmatched target columns: ")+string.Join(" · ",extra)));
            status.Text=destination!=null&&columns.All(c=>c.Source!=null&&c.Target!=null)?T("모든 컬럼 대응됨 · 아직 검사하지 않았습니다.","All columns mapped · not yet verified."):T("대응되지 않은 범위는 데이터 판정 불가로 남습니다.","Unmatched scope remains indeterminate for data.");
            scroll.Content=body;scroll.Offset=offset;
            if(focus!=null)Dispatcher.UIThread.Post(()=>focus.Focus());
        }
        List<TableMapping> Candidate()
        { var list=tableMappings.Where(m=>m.Source!=left.Qualified).ToList();if(!automatic)list.Add(new(left.Qualified,destination?.Qualified,working.ToArray()));return list; }
        Draw();dialog.Opened+=(_,_)=>{apply.Focus();_=Enter(grid,280);};await dialog.ShowDialog(this);caller?.Focus();
    }

    private async Task<(bool Accepted,string? Value)> PickMatching(Window owner,string title,IReadOnlyList<MatchingChoice> choices)
    {
        (bool Accepted,string? Value) result=(false,null);
        var window=new Window { Title=title,Width=640,Height=520,MinWidth=500,MinHeight=400,WindowStartupLocation=WindowStartupLocation.CenterOwner,Background=BrushOf("#2b2d31") };
        var grid=new Grid { RowDefinitions=new("Auto,Auto,*,Auto"),Margin=new(24),RowSpacing=16 };Put(grid,Text(title,20,Brushes.White));
        var search=new TextBox { Name="matching-search",PlaceholderText=T("이름으로 검색","Search by name") };AutomationProperties.SetName(search,T("대응 대상 검색","Search matching targets"));Put(grid,search,0,1);
        var list=new ListBox { Background=Brushes.Transparent,BorderThickness=new(0),Padding=new(0) };list.Classes.Add("matching-list");Put(grid,list,0,2);
        // ListBox focuses its row on Tab/arrows; Enter/Space must activate the
        // selected enabled choice through the same action and feedback as a click.
        list.AddHandler(InputElement.KeyDownEvent,(_,e)=>
        {
            if(e.Key is Key.Enter or Key.Space)e.Handled=true;
        },RoutingStrategies.Tunnel);
        list.AddHandler(InputElement.KeyUpEvent,(_,e)=>
        {
            if(e.Key is not (Key.Enter or Key.Space))return;
            e.Handled=true;
            if(list.SelectedItem is Button { IsEnabled:true } button)button.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        },RoutingStrategies.Tunnel);
        var close=Action(T("취소","Cancel"),()=>{window.Close();return Task.CompletedTask;},"quiet");close.HorizontalAlignment=HorizontalAlignment.Right;Put(grid,close,0,3);
        void Draw()
        {
            var rows=new List<Control>();
            Button? currentChoice=null;
            foreach(var choice in choices.Where(c=>c.Label.Contains(search.Text??"",StringComparison.OrdinalIgnoreCase)))
            {
                var body=Stack(3);body.Children.Add(Text(choice.Label,13,choice.Enabled?Brushes.White:Muted));if(choice.Hint!=null)body.Children.Add(Text(choice.Hint,12,choice.Enabled?Muted:Warn));
                var button=Action(choice.Label,()=>{result=(true,choice.Value);window.Close();return Task.CompletedTask;},choice.Current?"selected-filter":"quiet");button.IsEnabled=choice.Enabled;button.HorizontalAlignment=HorizontalAlignment.Stretch;button.HorizontalContentAlignment=HorizontalAlignment.Stretch;button.MinHeight=52;button.Padding=new(12);button.Content=Row("*,Auto",new Control[]{body,UiIcon(choice.Current?"check":"chevron",choice.Current?Accent:Muted)});rows.Add(button);
                if(choice.Current&&choice.Enabled)currentChoice=button;
            }
            if(rows.Count==0)rows.Add(Text(T("검색 결과가 없습니다. 검색어를 바꾸세요.","No matches. Change your search."),13,Muted));list.ItemsSource=rows;
            list.SelectedItem=currentChoice??rows.OfType<Button>().FirstOrDefault(b=>b.IsEnabled);
        }
        search.TextChanged+=(_,_)=>Draw();Draw();window.Content=grid;window.KeyDown+=(_,e)=>{if(e.Key==Key.Escape){e.Handled=true;window.Close();}};window.Opened+=(_,_)=>{search.Focus();_=Enter(grid,280);};await window.ShowDialog(owner);return result;
    }
}
