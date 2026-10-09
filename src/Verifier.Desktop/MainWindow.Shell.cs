using Avalonia;
using Avalonia.Animation;
using Avalonia.Animation.Easings;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Threading;
using Avalonia.Styling;
using Avalonia.VisualTree;
using Verifier.Core;
using Verifier.Data;
using Path = Avalonia.Controls.Shapes.Path;

namespace Verifier.Desktop;

public sealed partial class MainWindow
{
    private readonly HashSet<int> visited=[];
    private readonly HashSet<Category> expanded=[];
    private bool connected,advanced,reviewConfirmed;
    private string tableQuery="",resultFilter="all";
    private ScrollViewer? bodyScroll;
    private int renderedStep=-1;
    private Control? focusAfterRender;
    private static IBrush BrushOf(string value)=>Brush.Parse(value);
    private static Border Panel(Control child,Thickness? padding=null,string background="#2b2d31")=>new()
    { Child=child,Padding=padding??new(24),Background=BrushOf(background),BorderBrush=BrushOf("#41434a"),BorderThickness=new(1),CornerRadius=new(10) };
    private static Grid Columns(string definitions,double gap=16)=>new() { ColumnDefinitions=new(definitions),ColumnSpacing=gap };
    private static void Put(Grid grid,Control child,int column=0,int row=0) { Grid.SetColumn(child,column); Grid.SetRow(child,row); grid.Children.Add(child); }
    private static Control UiIcon(string name,IBrush? color=null,double size=20)
    {
        var originalName=name switch { "structure"=>"grid", "warn"=>"warning", "down"=>"chevron", _=>name };
        var icon=new SvgIcon { IconName=originalName,Stroke=color??Muted,Width=size,Height=size,VerticalAlignment=VerticalAlignment.Center };
        if(name=="down")icon.RenderTransform=new RotateTransform(90);
        return icon;
    }
    private Button WithIcon(Button button,string icon,bool right=false)
    {
        var label=button.Content?.ToString()??"";
        button.HorizontalContentAlignment=HorizontalAlignment.Stretch;var row=Columns(right?"*,Auto":"Auto,*",10);
        var text=Text(label,13,button.Classes.Contains("primary")?Brushes.White:Muted); text.FontWeight=FontWeight.SemiBold;if(right)text.TextAlignment=TextAlignment.Center;
        Put(row,text,right?0:1);Put(row,UiIcon(icon,button.Classes.Contains("primary")?Brushes.White:Muted,18),right?1:0);
        row.VerticalAlignment=VerticalAlignment.Center;button.Content=row;return button;
    }
    private static Border Pill(string label,IBrush? ink=null)
    { var text=Text(label,12,ink??Muted);return new() { Child=text,Padding=new(8,4),CornerRadius=new(5),Background=BrushOf(ink==Bad?"#422b34":ink==Warn?"#3c3529":ink==Good?"#253d33":"#34363c"),VerticalAlignment=VerticalAlignment.Center,HorizontalAlignment=HorizontalAlignment.Left }; }
    private Border Notice(string content,MessageTone tone=MessageTone.Warning)
    {
        var ink=tone switch { MessageTone.Success=>Good,MessageTone.Error=>Bad,MessageTone.Information=>Muted,_=>Warn };
        var background=tone switch { MessageTone.Success=>"#253d33",MessageTone.Error=>"#422b34",MessageTone.Information=>"#2b2d31",_=>"#383329" };
        var border=tone switch { MessageTone.Success=>"#39664e",MessageTone.Error=>"#75444d",MessageTone.Information=>"#41434a",_=>"#705a37" };
        var g=Columns("Auto,*",12);Put(g,UiIcon(tone==MessageTone.Success?"check":tone==MessageTone.Information?"info":"warn",ink));Put(g,Text(content,13,ink),1);
        return new() { Child=g,Padding=new(16),Background=BrushOf(background),BorderBrush=BrushOf(border),BorderThickness=new(1),CornerRadius=new(8) };
    }
    private void Heading(string title,string subtitle)
    { var h=Stack(8);var name=Text(title,step==0?36:28,Brushes.White);name.FontWeight=FontWeight.Bold;h.Children.Add(name);h.Children.Add(Text(subtitle,14,Muted));h.Margin=new(0,0,8,0);page.Children.Add(h); }
    private Task Back()
    {
        CaptureInputs();message="";detail=null;
        if(step==5)return Task.CompletedTask;
        if(step==8)return Go(3);
        if(step==6)return Go(0);
        return Go(Math.Max(0,step-1));
    }
    private void Render()
    {
        var samePage=renderedStep==step;
        var offset=samePage?bodyScroll?.Offset??default:default;
        var previousName=samePage?(FocusManager?.GetFocusedElement() as Control)?.Name:null;
        renderedStep=step;
        root.Children.Clear();root.RowDefinitions=new("60,36,*,84");root.ColumnDefinitions=new("220,*");
        var top=Columns("Auto,*,Auto,Auto",12);top.Margin=new(24,0);top.VerticalAlignment=VerticalAlignment.Center;
        var logo=new Border { Background=BrushOf("#5865f2"),CornerRadius=new(8),Padding=new(5),Width=32,Height=32,VerticalAlignment=VerticalAlignment.Center,Child=UiIcon("shield",Brushes.White,22) };
        Put(top,logo);var brand=Text("DB-Migration-Verifier",14,Brushes.White);brand.FontWeight=FontWeight.Bold;Put(top,brand,1);
        var mode=Text(example?T("합성 예시 · 실제 DB 아님","Synthetic example · no actual DB"):T("읽기 전용 비교","Read-only comparison"),12,Muted);mode.Margin=new(16,0);Put(top,mode,2);
        var lang=Action(T("한국어 / EN","한국어 / EN"),()=>{ CaptureInputs();english=!english;message="";Render();_=Enter(page,160);return Task.CompletedTask;},"quiet");lang.Name="language";Put(top,lang,3);
        var header=new Border { Child=top,Background=BrushOf("#1e1f22"),BorderBrush=BrushOf("#41434a"),BorderThickness=new(0,0,0,1) };Grid.SetColumnSpan(header,2);root.Children.Add(header);
        var railGrid=new Grid { RowDefinitions=new("Auto,*,Auto"),Margin=new(14,30) };
        var nav=Stack(8);var title=Text(T("마이그레이션 결과 비교","Migration comparison"),11,Muted);title.Margin=new(14,0,0,28);nav.Children.Add(title);
        var labels=english?new[]{"Connect databases","Preflight","Select checks","Review","Progress","Results","Save results"}:new[]{"DB 연결","사전 확인","검사 선택","실행 전 확인","검사 진행","결과 확인","결과 저장"};
        for(var i=1;i<=7;i++)
        {
            var value=i;var active=step==i || step==8&&i==3;var done=visited.Contains(i)&&!active;
            var row=Columns("Auto,*",12);row.VerticalAlignment=VerticalAlignment.Center;
            var badge=new Border { VerticalAlignment=VerticalAlignment.Center, Width=24,Height=24,CornerRadius=new(5),BorderThickness=new(1),BorderBrush=BrushOf(active?"#5865f2":done?"#697185":"#7c838c"),Background=BrushOf(active?"#5865f2":done?"#363846":"#232428") };
            badge.Child=done?UiIcon("check",BrushOf("#c9cfdb"),15):new TextBlock { Text=i.ToString(),HorizontalAlignment=HorizontalAlignment.Center,VerticalAlignment=VerticalAlignment.Center,Foreground=Brushes.White,FontSize=12 };
            Put(row,badge);Put(row,Text(labels[i-1],13,active?Brushes.White:Muted),1);
            var b=Action(labels[i-1],()=>Go(value),"nav");b.Content=row;b.Height=52;b.HorizontalAlignment=HorizontalAlignment.Stretch;b.IsEnabled=!busy && (i==1 || visited.Contains(i)) && i!=5 && (i is <2 or >4 || connected) && (i<6 || report!=null);
            if(active)b.Classes.Add("active");nav.Children.Add(b);
        }
        Put(railGrid,nav);
        var note=Stack(8);note.Children.Add(Line());var bold=Text(T("비교는 읽기 전용","Read-only comparison"),13,Brushes.White);bold.FontWeight=FontWeight.SemiBold;note.Children.Add(bold);note.Children.Add(Text(T("원본·대상에는 쓰지 않습니다.\n동작 시험은 별도 시험 DB에서만 합니다.","No source/target writes.\nBehavior tests require a separate test DB."),12,Muted));
        var motion=new AlignedCheckBox { Name="motion",Content=T("동작 줄이기","Reduce motion"),IsChecked=reducedMotion,IsEnabled=!busy };motion.IsCheckedChanged+=(_,_)=>{reducedMotion=motion.IsChecked==true;if(!reducedMotion)_=Pulse(motion);};note.Children.Add(motion);Put(railGrid,note,0,2);
        var rail=new Border { Child=railGrid,Background=BrushOf("#232428") };Grid.SetRow(rail,1);Grid.SetRowSpan(rail,3);root.Children.Add(rail);
        var strip=Columns("Auto,*",10);strip.Margin=new(Bounds.Width<=1180?28:40,0);strip.VerticalAlignment=VerticalAlignment.Center;Put(strip,UiIcon("info",Muted,17));Put(strip,Text(example?T("합성 예시 · 실제 DB 검사 결과와 구분합니다.","Synthetic example · separate from actual DB results."):T("원본과 대상을 읽어 비교합니다. 확인하지 못한 부분은 일치로 표시하지 않습니다.","Read-only source/target comparison. Unverified is never equal."),12,Muted),1);
        var stripBorder=new Border { Child=strip,Background=BrushOf("#2b2d31"),BorderBrush=BrushOf("#41434a"),BorderThickness=new(0,0,0,1) };Put(root,stripBorder,1,1);
        page=Stack(24);page.Margin=Bounds.Width<=1180?new(28):new(40,36);page.MaxWidth=1120;
        inputs=null;stopped=null;
        if(message.Length>0){var notice=Notice(message,messageTone);page.Children.Add(notice);_=Enter(notice,160);}
        switch(step) { case 0:Home();break;case 1:Connections();break;case 2:Preflight();break;case 3:SelectChecks();break;case 4:Review();break;case 5:Progress();break;case 6:Results();break;case 7:Save();break;case 8:TrialPreparation();break; }
        bodyScroll=new ScrollViewer { Content=page,HorizontalScrollBarVisibility=Avalonia.Controls.Primitives.ScrollBarVisibility.Disabled,Offset=offset };Put(root,bodyScroll,1,2);
        var footer=Columns("Auto,*,Auto",16);footer.Margin=new(Bounds.Width<=1180?28:40,16);
        if(step>0 && step is not 5 and not 6)Put(footer,WithIcon(Action(step==7?T("결과로 돌아가기","Back to results"):T("뒤로","Back"),Back,"quiet"),"back"));
        if(step==6)Put(footer,WithIcon(Action(T("전체 재검사","Full new check"),async()=>{await Reset();await Go(1);},"quiet"),"refresh"));
        var hint=Text(FooterHint(),12,Muted);Put(footer,hint,1);
        Button? next=step switch {
            1=>WithIcon(Action(T("두 연결 확인","Check both connections"),Connect,"primary"),"db"),
            2=>WithIcon(Action(T("검사 범위 선택","Select check scope"),()=>Go(3),"primary"),"arrow"),
            3=>WithIcon(Action(T("실행 전 확인","Review selection"),()=>{ if(chosenTables.Count==0||selected.Count==0){message=T("검사 분류와 테이블을 하나 이상 선택하세요.","Select at least one category and table.");Render();return Task.CompletedTask;}reviewConfirmed=false;return Go(4);},"primary"),"arrow"),
            4=>WithIcon(Action(T("비교 시작","Start comparison"),Run,"primary"),"play"),
            5=>WithIcon(Action(cancelRequested?T("중단 처리 중","Stopping"):T("검사 취소","Cancel checks"),()=>{cancelRequested=true;cancellation?.Cancel();message=T("중단 요청됨 · 부분 결과를 정리합니다.","Stop requested · finalizing partial results.");Render();return Task.CompletedTask;},"danger",duringBusy:true),"stop"),
            6=>WithIcon(Action(T("결과 저장","Save results"),()=>Go(7),"primary"),"save"),
            7=>WithIcon(Action(T("선택 형식 저장","Save selected format"),SaveReport,"primary"),"save"),
            8=>WithIcon(Action(T("검사 선택으로 돌아가기","Back to selection"),()=>Go(3),"primary"),"back"),_=>null };
        if(next!=null){if(step==4)next.IsEnabled=!busy&&reviewConfirmed;if(step==5)next.IsEnabled=!cancelRequested;if(step==7)next.IsEnabled=!busy&&selectedFormats.Count>0;Put(footer,next,2);}
        var footerBorder=new Border { Child=footer,Background=BrushOf("#2b2d31"),BorderBrush=BrushOf("#41434a"),BorderThickness=new(0,1,0,0) };Put(root,footerBorder,1,3);
        if(!samePage)_=Enter(page);
        Dispatcher.UIThread.Post(()=>{
            if(focusAfterRender!=null){var name=focusAfterRender.Name;var target=name==null?null:root.GetVisualDescendants().OfType<Control>().FirstOrDefault(c=>c.Name==name);target?.Focus();if(target!=null)_=Pulse(target);focusAfterRender=null;}
            else if(previousName!=null) root.GetVisualDescendants().OfType<Control>().FirstOrDefault(c=>c.Name==previousName)?.Focus();
        });
    }
    private string FooterHint()=>step switch {
        0=>T("로그인 없이 합성 예시 흐름을 살펴볼 수 있습니다.","Explore a synthetic flow without signing in."),
        1=>busy?T("연결·조회 범위 확인 중","Checking connections and visibility"):T("연결 정보를 바꾸면 다시 확인합니다.","Changed connections must be checked again."),
        3=>T("선택 범위와 제외 범위를 다음 화면에서 확인합니다.","Review selected and excluded scope next."),
        4=>reviewConfirmed?T("원본과 대상에는 쓰지 않습니다.","No source or target writes."):T("검사 범위와 쓰기 정지 조건을 확인하세요.","Confirm scope and stopped writes."),
        5=>T("처리 중 · 취소하면 부분 결과와 중단 이유를 보관합니다.","Working · cancellation preserves partial results and reason."),
        _=>T("확인한 범위와 확인하지 못한 이유를 함께 보관합니다.","Keep verified scope and reasons for gaps together.") };
    private Task Enter(Control target,int duration=200)
    {
        if(reducedMotion)return Task.CompletedTask;
        return new Animation { Duration=TimeSpan.FromMilliseconds(duration),Easing=new CubicEaseOut(),Children={new KeyFrame { Cue=new(0),Setters={new Setter(OpacityProperty,.35)}},new KeyFrame { Cue=new(1),Setters={new Setter(OpacityProperty,1d)}}} }.RunAsync(target);
    }
    private async Task InfoDialog(string title,string description)
    {
        var caller=FocusManager?.GetFocusedElement() as Control;
        var w=new Window { Title=title,Width=620,Height=460,MinWidth=480,MinHeight=320,WindowStartupLocation=WindowStartupLocation.CenterOwner,Background=BrushOf("#2b2d31") };
        var grid=new Grid { RowDefinitions=new("Auto,*,Auto") };var head=Columns("*,Auto");head.Margin=new(24,16);Put(head,Text(title,18,Brushes.White));
        var close=new Button { Content=UiIcon("close"),Width=44,Height=44 };AutomationProperties.SetName(close,T("닫기","Close"));close.Click+=async(_,_)=>{await Pulse(close);w.Close();};Put(head,close,1);Put(grid,head);
        var body=Text(description,14);body.Margin=new(24);body.VerticalAlignment=VerticalAlignment.Top;Put(grid,new ScrollViewer { Content=body },0,1);
        var done=new Button { Content=T("닫기","Close"),Margin=new(24,16),HorizontalAlignment=HorizontalAlignment.Right };done.Click+=async(_,_)=>{await Pulse(done);w.Close();};Put(grid,done,0,2);
        w.Content=grid;w.KeyDown+=async(_,e)=>{if(e.Key==Key.Escape){e.Handled=true;await Pulse(body);w.Close();}};w.Opened+=(_,_)=>{close.Focus();_=Enter(grid,280);};await w.ShowDialog(this);caller?.Focus();
    }
    private Task ResearchDialog()=>InfoDialog(T("연구 결과 → 검사 → 화면","Research → checks → screens"),
        T("기본 기능 성공과 넓은 DB 계약 충족을 구분한 논문의 결과를 앱에 반영합니다.\n\nRP-D01 · 전체 값·중복·누락 대조 → 데이터 판정\nRP-C01/C02 · 구조·제약·인덱스 → 구현된 속성과 미검증 구분\nRP-A01/A02 · 사용자·권한 → 현재 미구현 이유 표시\nRP-O01 · 관련 객체 → 현재 미검증 표시\nRP-B01 · 별도 시험 DB → 현재 준비 기능 미구현\nRP-S01 · 중단·권한·관측 한계 → 일치로 처리하지 않음\n\n현재 관측한 일부 속성이 같아도 전체 계약의 일치를 선언하지 않습니다.",
        "The paper distinguishes functional success from wider database contracts.\n\nRP-D01 · Exact values, duplicates and missing rows → data verdicts\nRP-C01/C02 · Schema, constraints and indexes → observed vs unverified scope\nRP-A01/A02 · Access → current implementation gaps\nRP-O01 · Objects → explicit unverified states\nRP-B01 · Separate test DB → preparation pending\nRP-S01 · Interruption and observation limits → never equal\n\nEqual observed attributes do not certify all contracts."));
    private string StageText(string stage)=>stage switch {"Inspect"=>T("조회 범위 확인","Inspecting scope"),"Finalize"=>T("결과 정리","Finalizing"),"Source"=>T("원본 읽기","Reading source"),"Target"=>T("대상 읽기","Reading target"),_=>stage};
}
