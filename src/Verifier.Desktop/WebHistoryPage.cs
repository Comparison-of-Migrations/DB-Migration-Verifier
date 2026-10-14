using Avalonia;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Verifier.Core;

namespace Verifier.Desktop;

// A functional page made from the existing heading, text and action controls.
// The index is a reference to a saved result, never new comparison evidence.
public sealed class WebHistoryPage:StackPanel
{
    public event EventHandler<string>? ActionRequested;
    public IReadOnlyList<WebActionButton> OpenButtons=>_open;
    private readonly List<WebActionButton> _open=[];
    public WebHistoryPage(HistoryPage page,bool english,bool busy,bool reduced,string status)
    {
        Spacing=16;string T(string a,string b)=>english?b:a;
        var heading=new WebPageHeading();heading.SetPresentation(T("저장한 검사 이력","Saved inspection history"),T("이 기기에서 저장하거나 연 JSON 결과입니다. 다시 열어도 DB 검사를 실행하지 않습니다.","JSON results saved or opened on this device. Opening a record does not rerun database checks."));Children.Add(heading);
        Text(T("기록은 이 기기에만 보관합니다. 기록 삭제는 원래 결과 파일을 지우지 않습니다. 파일이 변경되었거나 검증되지 않으면 열지 않습니다.","Records stay on this device. Removing a record leaves the original result file intact. Changed or invalid files cannot be opened."));
        var message=status switch
        {
            "open-failed"=>T("기록한 결과 파일을 검증하지 못했습니다. 파일이 없거나 변경·손상됐을 수 있습니다. 기존 결과는 유지됩니다.","The recorded file could not be verified. It may be missing, changed or damaged. The previous result is retained."),
            "remove-failed"=>T("이력 기록을 삭제하지 못했습니다. 원래 결과 파일은 유지됩니다.","The history record could not be removed. The original result file is retained."),
            "read-failed"=>T("이력을 읽지 못했습니다. 다시 불러오기를 시도하세요.","History could not be read. Try refreshing."),
            _=>""
        };
        if(message.Length>0)Children.Add(new WebNotice{Title=T("이력 안내","History notice"),Text=message,Kind="warn",ReducedMotion=reduced});
        if(page.UnreadableEntries)Text(T("일부 이력 기록을 읽지 못했습니다. 읽을 수 있는 기록만 표시합니다.","Some records could not be read. Only readable records are shown."));
        if(busy)Text(T("처리 중…","Working…"));else if(page.Entries.Count==0)Text(T("표시할 저장 이력이 없습니다.","No saved history to display."));
        foreach(var entry in page.Entries)
        {
            var row=new StackPanel{Spacing=6};
            Text(T("검사 ","Run ")+entry.RunId.ToString("N"),row);
            Text(entry.Recorded.ToLocalTime().ToString("yyyy-MM-dd HH:mm:ss",System.Globalization.CultureInfo.InvariantCulture),row);
            var actions=new WrapPanel{Orientation=Orientation.Horizontal};
            var open=Button(T("결과 열기","Open result"),"history-open:"+entry.Id,"file");_open.Add(open);actions.Children.Add(open);
            actions.Children.Add(Button(T("기록만 삭제","Remove record only"),"history-remove:"+entry.Id,""));row.Children.Add(actions);Children.Add(row);
        }
        var navigation=new WrapPanel{Orientation=Orientation.Horizontal};navigation.Children.Add(Button(T("처음부터 새로 불러오기","Refresh from the beginning"),"history-refresh",""));if(page.Next!=null)navigation.Children.Add(Button(T("다음 기록","Next records"),"history-next","arrow"));Children.Add(navigation);
        void Text(string value,StackPanel? parent=null){var text=WebTypography.CreateCandidate(value,14,22.4,Brush.Parse("#dbdee1"));text.TextWrapping=TextWrapping.Wrap;(parent??this).Children.Add(text);}
        WebActionButton Button(string title,string action,string icon){var button=new WebActionButton{Content=title,IconName=icon,IsEnabled=!busy,ReducedMotion=reduced,Margin=new(0,0,8,0)};AutomationProperties.SetAutomationId(button,action);button.Click+=(_,_)=>ActionRequested?.Invoke(this,action);return button;}
    }
}
