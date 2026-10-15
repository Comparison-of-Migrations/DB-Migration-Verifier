using System.Diagnostics;
using Avalonia;
using Avalonia.Automation;
using Avalonia.Automation.Peers;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Threading;
namespace Verifier.Desktop;

public sealed record WebTrialView(bool English,string Host,string Database,string ValidInput,string InvalidInput,bool Consent,bool Ready,string Error);

/// <summary>Literal trial() content. The application owns the sample preparation check and navigation.</summary>
public sealed class WebTrialPage:Panel
{
    public WebTrialView View{get;private set;}=new(false,"trial.example.invalid","migration_test","1","-1",false,false,"");
    public bool ReducedMotion{get;set;}
    public WebPageHeading Heading{get;private set;}=new();
    public WebField[] Fields{get;private set;}=[];
    public WebTextInput[] Inputs{get;private set;}=[];
    public WebChoice ConsentChoice{get;private set;}=new();
    public Control ConnectionSplit{get;private set;}=new Panel();
    public Control PlanSection{get;private set;}=new Panel();
    public Control[] Panels{get;private set;}=[];
    public WebSystemTextBlock[] Subheadings{get;private set;}=[];
    public Control IsolationNotice{get;private set;}=new Panel();
    public Control Guidance{get;private set;}=new Panel();
    public Control PlanTable{get;private set;}=new Panel();
    public Control ValueSplit{get;private set;}=new Panel();
    public WebSystemTextBlock GuidanceNote{get;private set;}=Text("",12,19.2);
    public WebSystemTextBlock ValueNote{get;private set;}=Text("",12,19.2);
    public WebNotice? ErrorNotice{get;private set;}
    public WebNotice? ReadyNotice{get;private set;}
    public event EventHandler<WebTrialView>? Changed;
    private double _viewport=1280;private bool _setting;private long _revision;
    private static WebSystemTextBlock Text(string value,double size,double line,int weight=400,string color="#b5bac1",double spacing=0)=>WebTypography.CreateCandidate(value,size,line,Brush.Parse(color),weight,spacing);
    private static WebSystemTextBlock H2(string value){var h=Text(value,18,27,700,"#f2f3f5",-.27);AutomationProperties.SetHeadingLevel(h,2);return h;}
    private static WebSurfaceBox PanelBox(Control child)=>new(){Background=Brush.Parse("#2b2d31"),BorderBrush=Brush.Parse("#41434a"),BorderThickness=new(1),CornerRadius=new(10),Padding=new(26),UseLayoutRounding=false,Child=child,BoxShadow=new(new BoxShadow{OffsetY=2,Blur=4,Color=Color.FromArgb(16,0,0,0)})};
    public WebTrialPage(){UseLayoutRounding=false;SetPresentation(View,_viewport);}
    /// <summary>CSS-only viewport updates preserve the existing inputs and their selection.</summary>
    public void SetViewport(double viewportWidth)
    {
        if(_viewport==viewportWidth)return;
        _viewport=viewportWidth;var gap=viewportWidth<=1100?16:20;
        if(ConnectionSplit is Split connection)connection.SetGap(gap);
        if(ValueSplit is Split values)values.SetGap(gap);
        InvalidateMeasure();
    }
    public void SetPresentation(WebTrialView view,double viewportWidth)
    {
        _setting=true;try{
        View=view;_viewport=viewportWidth;var revision=++_revision;Children.Clear();ErrorNotice=ReadyNotice=null;
        string T(string ko,string en)=>view.English?en:ko;
        Heading=new();Heading.SetPresentation(T("동작 시험은 별도 DB에서 합니다","Behavior tests use a separate database"),T("앱이 원본·대상을 복제하거나 초기화하지 않습니다. 변경 가능한 시험 DB를 사용자가 준비합니다.","Prepare your own mutable test DB. The app does not clone or reset the source or target."));Children.Add(Heading);
        Subheadings=[H2(T("시험 DB 연결","Test DB connection")),H2(T("DBMS별 준비 안내","DBMS preparation guidance")),H2(T("기본 시험안 · 예시","Generated trial plan · Example"))];
        string[] labels=[T("서버 주소","Server"),T("시험 데이터베이스","Test database"),T("정상 입력 시험값 · 기대: 허용","Valid input · Expected: accepted"),T("제약 위반 시험값 · 기대: 거부","Constraint-violating input · Expected: rejected")];
        string[] values=[view.Host,view.Database,view.ValidInput,view.InvalidInput],ids=["host","db","validInput","invalidInput"];
        Inputs=values.Select(value=>new WebTextInput{Text=value,ReducedMotion=ReducedMotion}).ToArray();Fields=Enumerable.Range(0,4).Select(i=>new WebField{Label=labels[i],Input=Inputs[i]}).ToArray();
        for(var i=0;i<4;i++){var index=i;AutomationProperties.SetAutomationId(Inputs[i],"trial-"+ids[i]);Inputs[i].PropertyChanged+=(_,e)=>{if(e.Property==TextBox.TextProperty&&!_setting&&revision==_revision)InputChanged(index);};}
        IsolationNotice=new IsolationBox(T("분리 확인","Isolation check"),T("실제 앱은 연결 경로와 DB 식별을 확인합니다. 이 시안은 같은 예시 DB를 지정하는 오류만 재현합니다.","The actual app must verify routing and DB identity. This preview only demonstrates rejecting the same sample DB."),ReducedMotion);
        var connection=PanelBox(new Flow(Subheadings[0],Fields[0],Fields[1],IsolationNotice));
        Guidance=new OrderedList([T("변경 가능한 별도 시험 DB를 준비합니다.","Prepare a separate database that may be modified."),T("대상 구조·필요 계정·권한을 갖춥니다.","Provide target schema and required test accounts/permissions."),T("동작별 시험 값과 기대 결과를 확인합니다.","Confirm test inputs and expected outcomes.")]);
        GuidanceNote=Text(T("백엔드 업무 환경은 이 기본 시험의 필수 준비물이 아닙니다.","A backend business environment is not required for these basic tests."),12,19.2);
        var guide=PanelBox(new Flow(Subheadings[1],Guidance,GuidanceNote));
        ConnectionSplit=new Split(connection,guide,viewportWidth<=1100?16:20);Children.Add(ConnectionSplit);
        PlanTable=new TrialTable(view.English,ReducedMotion){Margin=new(0,12,0,0)};
        ValueNote=Text(T("합성 시험 제약: amount ≥ 0 · 아래 값은 예시이며 실제 데이터가 아닙니다.","Synthetic trial constraint: amount ≥ 0 · These are example values, not real data."),12,19.2);
        ValueSplit=new Split(Fields[2],Fields[3],viewportWidth<=1100?16:20);var valuesPanel=PanelBox(new Flow(ValueNote,ValueSplit));valuesPanel.Margin=new(0,16,0,0);Panels=[connection,guide,valuesPanel];
        ConsentChoice=new(){Margin=new(0,16,0,0),ReducedMotion=ReducedMotion};ConsentChoice.SetPresentation(T("예시 시험안과 기대 결과를 확인했습니다.","I have reviewed the sample trial plan and expected results."),"trial.consent",view.Consent);
        ConsentChoice.SelectionRequested+=(_,_)=>{if(revision!=_revision)return;var selected=ConsentChoice.IsSelected;View=View with{Consent=selected,Ready=selected&&View.Ready};if(!View.Ready&&ReadyNotice is{}notice){Children.Remove(notice);ReadyNotice=null;InvalidateMeasure();}Changed?.Invoke(this,View);};
        PlanSection=new Flow(Subheadings[2],PlanTable,valuesPanel,ConsentChoice){Margin=new(0,30,0,0)};Children.Add(PlanSection);
        if(view.Error.Length>0){ErrorNotice=new(){Kind="error",Title=T("시험 준비를 확인하세요","Check test preparation"),Text=view.Error,ReducedMotion=ReducedMotion};Children.Add(ErrorNotice);}
        if(view.Ready){ReadyNotice=new(){Kind="ok",Title=T("예시 시험 준비가 확인됐습니다","Sample trial setup is confirmed"),Text=T("실제 쓰기나 DB 분리 검증을 수행한 것은 아닙니다.","No actual writes or database-isolation checks have been performed."),ReducedMotion=ReducedMotion};Children.Add(ReadyNotice);}
        InvalidateMeasure();
        }finally{_setting=false;}
    }
    private void InputChanged(int index)
    {
        var text=Inputs[index].Text??"";View=index switch{0=>View with{Host=text},1=>View with{Database=text},2=>View with{ValidInput=text},_=>View with{InvalidInput=text}};
        View=View with{Consent=false,Ready=false};ConsentChoice.IsSelected=false;if(ReadyNotice is not null){Children.Remove(ReadyNotice);ReadyNotice=null;InvalidateMeasure();}Changed?.Invoke(this,View);
    }
    protected override Size MeasureOverride(Size available){double height=0;foreach(var c in Children){c.Measure(new(available.Width,double.PositiveInfinity));height+=c.DesiredSize.Height;}return new(available.Width,height);}
    protected override Size ArrangeOverride(Size final){double y=0;foreach(var c in Children){c.Arrange(new(0,y,final.Width,c.DesiredSize.Height));y+=c.DesiredSize.Height;}return final;}
    private sealed class Flow:Panel
    {
        public Flow(params Control[] controls){UseLayoutRounding=false;Children.AddRange(controls);}
        protected override Size MeasureOverride(Size available){double h=0;foreach(var c in Children){if(c is WebSystemTextBlock text){var u=(TopLevel.GetTopLevel(this)?.RenderScaling??2)*64;text.LineHeight=Math.Floor(text.LineHeight*u)/u;}c.Measure(new(available.Width,double.PositiveInfinity));h+=c.DesiredSize.Height;}return new(available.Width,h);}
        protected override Size ArrangeOverride(Size final){double y=0;foreach(var c in Children){c.Arrange(new(0,y,final.Width,c.DesiredSize.Height));y+=c.DesiredSize.Height;}return final;}
    }
    private sealed class Split:Panel
    {
        private double _gap;private double _column;
        public Split(Control left,Control right,double gap){UseLayoutRounding=false;_gap=gap;Children.Add(left);Children.Add(right);}
        public void SetGap(double gap){if(_gap==gap)return;_gap=gap;InvalidateMeasure();}
        protected override Size MeasureOverride(Size available){var u=(TopLevel.GetTopLevel(this)?.RenderScaling??2)*64;_column=Math.Floor((available.Width-_gap)*u/2)/u;Children[0].Measure(new(_column,double.PositiveInfinity));Children[1].Measure(new(available.Width-_gap-_column,double.PositiveInfinity));return new(available.Width,Children.Max(c=>c.DesiredSize.Height));}
        protected override Size ArrangeOverride(Size final){Children[0].Arrange(new(0,0,_column,final.Height));Children[1].Arrange(new(_column+_gap,0,final.Width-_gap-_column,final.Height));return final;}
    }
    private sealed class IsolationBox:Panel
    {
        private readonly WebSurfaceBox _surface;private readonly bool _reduced;private WebMotionPlayback? _motion;
        public IsolationBox(string title,string body,bool reduced){UseLayoutRounding=false;Margin=new(0,20,0,0);_reduced=reduced;_surface=WebSurface.Create(Brush.Parse("#2b2d31"),Brush.Parse("#41434a"),new(1),new(8));_surface.Padding=new(16,14);_surface.Child=new IsolationText(title,body);Children.Add(_surface);}
        protected override Size MeasureOverride(Size available){_surface.Measure(available);return _surface.DesiredSize;}
        protected override Size ArrangeOverride(Size final){_surface.Arrange(new Rect(final));return final;}
        protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e){base.OnAttachedToVisualTree(e);_motion=WebMotion.Play(this,WebMotionKind.Page,_reduced);}
        protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e){_motion?.Dispose();_motion=null;base.OnDetachedFromVisualTree(e);}
    }
    private sealed class IsolationText:Panel
    {
        private readonly WebSystemTextBlock _title,_body,_strut=Text("M",14,22.4);private double _width;
        public IsolationText(string title,string body){UseLayoutRounding=false;_title=Text(title,13,22.4,600,"#f2f3f5");_body=Text(body,12,19.2);Children.Add(_title);Children.Add(_body);Children.Add(_strut);_strut.Opacity=0;_strut.IsHitTestVisible=false;AutomationProperties.SetAccessibilityView(_strut,AccessibilityView.Raw);}
        protected override Size MeasureOverride(Size available){var u=(TopLevel.GetTopLevel(this)?.RenderScaling??2)*64;_title.LineHeight=_strut.LineHeight=Math.Floor(22.4*u)/u;_body.LineHeight=Math.Floor(19.2*u)/u;_strut.Measure(Size.Infinity);_title.Measure(Size.Infinity);_body.Measure(Size.Infinity);_width=Math.Min(available.Width,Math.Ceiling(Math.Max(_title.DesiredSize.Width,_body.DesiredSize.Width)*u)/u);_title.Measure(new(_width,double.PositiveInfinity));_body.Measure(new(_width,double.PositiveInfinity));_title.RenderTransform=new TranslateTransform(0,_strut.SystemBaseline-_title.SystemBaseline);return new(available.Width,_title.DesiredSize.Height+_body.DesiredSize.Height);}
        protected override Size ArrangeOverride(Size final){_title.Arrange(new(0,0,_width,_title.DesiredSize.Height));_body.Arrange(new(0,_title.DesiredSize.Height,_width,_body.DesiredSize.Height));return final;}
    }
    private sealed class OrderedList:Panel
    {
        public OrderedList(string[] values){UseLayoutRounding=false;Margin=new(0,14,0,14);for(var i=0;i<values.Length;i++)Children.Add(new OrderedItem((i+1)+".",values[i]));}
        protected override Size MeasureOverride(Size available){double h=0;foreach(var c in Children){c.Measure(new(Math.Max(0,available.Width-20),double.PositiveInfinity));h+=c.DesiredSize.Height;}return new(available.Width,h);}
        protected override Size ArrangeOverride(Size final){double y=0;foreach(var c in Children){c.Arrange(new(20,y,Math.Max(0,final.Width-20),c.DesiredSize.Height));y+=c.DesiredSize.Height;}return final;}
        protected override AutomationPeer OnCreateAutomationPeer()=>new OrderedPeer(this);
        private sealed class OrderedPeer(OrderedList owner):ControlAutomationPeer(owner){protected override AutomationControlType GetAutomationControlTypeCore()=>AutomationControlType.List;}
    }
    private sealed class OrderedItem:Panel
    {
        private readonly WebSystemTextBlock _marker,_text,_spacing=Text("M M",14,22.4),_withoutSpace=Text("MM",14,22.4);private double _space;
        public OrderedItem(string marker,string text){UseLayoutRounding=false;_marker=Text(marker,14,22.4);_text=Text(text,14,22.4);Children.Add(_text);Children.Add(_marker);AutomationProperties.SetAccessibilityView(_marker,AccessibilityView.Raw);}
        protected override Size MeasureOverride(Size available){var u=(TopLevel.GetTopLevel(this)?.RenderScaling??2)*64;_text.LineHeight=_marker.LineHeight=Math.Floor(22.4*u)/u;_marker.Measure(Size.Infinity);_spacing.Measure(Size.Infinity);_withoutSpace.Measure(Size.Infinity);_space=_spacing.DesiredSize.Width-_withoutSpace.DesiredSize.Width;_text.Measure(available);return new(available.Width,_text.DesiredSize.Height);}
        protected override Size ArrangeOverride(Size final){_text.Arrange(new Rect(final));_marker.Arrange(new(-_space-_marker.DesiredSize.Width,0,_marker.DesiredSize.Width,_marker.DesiredSize.Height));return final;}
        protected override AutomationPeer OnCreateAutomationPeer()=>new ItemPeer(this);
        private sealed class ItemPeer(OrderedItem owner):ControlAutomationPeer(owner){protected override AutomationControlType GetAutomationControlTypeCore()=>AutomationControlType.ListItem;}
    }
    private sealed class TrialTable:Panel
    {
        private readonly WebSurfaceBox _surface=WebSurface.Create(Brush.Parse("#2b2d31"),Brush.Parse("#41434a"),new(1),new(10));
        public TrialTable(bool english,bool reduced){UseLayoutRounding=false;_surface.Child=new TableRows(english,reduced);Children.Add(_surface);}
        protected override Size MeasureOverride(Size available){_surface.Measure(available);return _surface.DesiredSize;}
        protected override Size ArrangeOverride(Size final){_surface.Arrange(new Rect(final));var table=_surface.Child!;table.Clip=new RectangleGeometry{Rect=new Rect(table.Bounds.Size),RadiusX=9,RadiusY=9};return final;}
        protected override AutomationPeer OnCreateAutomationPeer()=>new TablePeer(this);
        private sealed class TablePeer(TrialTable owner):ControlAutomationPeer(owner){protected override bool IsControlElementCore()=>false;protected override bool IsContentElementCore()=>false;}
    }
    private sealed class TableRows:Panel
    {
        private readonly Cell[] _cells;private readonly TableRow[] _rows;private readonly double[] _columns=new double[3],_heights=new double[4];
        public TableRows(bool en,bool reduced){UseLayoutRounding=false;string[][] text=en?[["Order","Action","Expected observation"],["1","Insert and read app-owned test data","Valid input accepted; automatic values checked"],["2","Update and read app-owned data","Valid and invalid input outcomes distinguished"],["3","Delete app-owned data and check cleanup","Existing data untouched; remaining changes reported"]]:[["순서","시험","확인할 결과"],["1","앱 생성 자료 입력·조회","정상 입력 허용·자동값 확인"],["2","앱 생성 자료 수정·조회","제약 충족·위반 입력의 결과 구분"],["3","앱 생성 자료 삭제·정리 확인","기존 자료 불변·남은 변경 별도 표시"]];_cells=text.SelectMany((row,r)=>row.Select((value,c)=>new Cell(value,r==0,r==3))).ToArray();_rows=Enumerable.Range(0,4).Select(i=>new TableRow(_cells.Skip(i*3).Take(3).ToArray(),i>0,reduced)).ToArray();Children.Add(new TableGroup([_rows[0]]));Children.Add(new TableGroup(_rows.Skip(1).ToArray()));}
        protected override Size MeasureOverride(Size available){var u=(TopLevel.GetTopLevel(this)?.RenderScaling??2)*64;var max=new double[3];var min=new double[3];for(var c=0;c<3;c++){foreach(var cell in _cells.Where((_,i)=>i%3==c)){cell.Text.Measure(Size.Infinity);max[c]=Math.Max(max[c],Math.Ceiling(cell.Text.DesiredSize.Width*u)/u+36);cell.Text.Measure(new(1/u,double.PositiveInfinity));min[c]=Math.Max(min[c],Math.Ceiling(cell.Text.SystemLineWidths.DefaultIfEmpty().Max()*u)/u+36);}}double sumMax=max.Sum(),sumMin=min.Sum();var width=Math.Max(available.Width,sumMin);for(var c=0;c<2;c++){var value=width>=sumMax?max[c]*width/sumMax:min[c]+(max[c]-min[c])*(width-sumMin)/(sumMax-sumMin);_columns[c]=Math.Floor(value*u)/u;}_columns[2]=width-_columns[0]-_columns[1];double height=0;for(var row=0;row<4;row++){for(var c=0;c<3;c++)_cells[row*3+c].Measure(new(_columns[c],double.PositiveInfinity));_heights[row]=_cells.Skip(row*3).Take(3).Max(x=>x.DesiredSize.Height);height+=_heights[row];_rows[row].Columns=_columns;_rows[row].RowHeight=_heights[row];}foreach(var g in Children)g.Measure(new(width,double.PositiveInfinity));return new(width,height);}
        protected override Size ArrangeOverride(Size final){double y=0;foreach(var g in Children){g.Arrange(new(0,y,final.Width,g.DesiredSize.Height));y+=g.DesiredSize.Height;}return final;}
        protected override AutomationPeer OnCreateAutomationPeer()=>new ContentPeer(this);
        private sealed class ContentPeer(TableRows owner):ControlAutomationPeer(owner){protected override AutomationControlType GetAutomationControlTypeCore()=>AutomationControlType.Table;protected override string GetClassNameCore()=>"Table";}
    }
    private sealed class TableGroup:Panel
    {
        public TableGroup(TableRow[] rows){UseLayoutRounding=false;Children.AddRange(rows);}
        protected override Size MeasureOverride(Size available){foreach(var c in Children)c.Measure(available);return new(available.Width,Children.Sum(c=>c.DesiredSize.Height));}
        protected override Size ArrangeOverride(Size final){double y=0;foreach(var c in Children){c.Arrange(new(0,y,final.Width,c.DesiredSize.Height));y+=c.DesiredSize.Height;}return final;}
        protected override AutomationPeer OnCreateAutomationPeer()=>new GroupPeer(this);
        private sealed class GroupPeer(TableGroup owner):ControlAutomationPeer(owner){protected override bool IsControlElementCore()=>!OperatingSystem.IsWindows();protected override bool IsContentElementCore()=>!OperatingSystem.IsWindows();protected override AutomationControlType GetAutomationControlTypeCore()=>AutomationControlType.Group;protected override string GetClassNameCore()=>"RowGroup";}
    }
    private sealed class TableRow:Panel
    {
        public double[] Columns=[0,0,0];public double RowHeight;
        private readonly bool _body,_reduced;private bool _hover;private long _revision;private readonly TrialBackgroundTrack _track=new();
        public TableRow(Cell[] cells,bool body,bool reduced){UseLayoutRounding=false;_body=body;_reduced=reduced;Background=Brushes.Transparent;Children.AddRange(cells);PointerEntered+=(_,_)=>Hover(true);PointerExited+=(_,_)=>Hover(false);}
        private void Hover(bool value){if(!_body||_hover==value)return;_hover=value;var color=Color.Parse(value?"#3a3c44":"#00000000");var revision=++_revision;if(_reduced||TopLevel.GetTopLevel(this) is not{}top){_track.Snap(color);Background=new SolidColorBrush(color);return;}_track.Set(color);void Frame(TimeSpan _){if(revision!=_revision||TopLevel.GetTopLevel(this) is null)return;Background=new SolidColorBrush(_track.Current);if(_track.Running)top.RequestAnimationFrame(Frame);}top.RequestAnimationFrame(Frame);}
        protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e){++_revision;_hover=false;_track.Snap(default);Background=Brushes.Transparent;base.OnDetachedFromVisualTree(e);}
        protected override Size MeasureOverride(Size available)=>new(Columns.Sum(),RowHeight);
        protected override Size ArrangeOverride(Size final){double x=0;for(var i=0;i<3;i++){Children[i].Arrange(new(x,0,Columns[i],RowHeight));x+=Columns[i];}return final;}
        protected override AutomationPeer OnCreateAutomationPeer()=>new RowPeer(this);
        private sealed class RowPeer(TableRow owner):ControlAutomationPeer(owner){protected override AutomationControlType GetAutomationControlTypeCore()=>AutomationControlType.DataItem;protected override string GetClassNameCore()=>"Row";protected override string GetLocalizedControlTypeCore()=>"row";}
    }
    private sealed class Cell:Panel
    {
        public WebSystemTextBlock Text{get;}private readonly WebSurfaceBox _paint;private readonly bool _header,_last;
        public Cell(string value,bool header,bool last){UseLayoutRounding=false;_header=header;_last=last;Text=WebTrialPage.Text(value,header?12:13,header?19.2:20.8,header?600:400,header?"#b5bac1":"#dbdee1");_paint=WebSurface.Create(header?Brush.Parse("#232428"):Brushes.Transparent,Brush.Parse("#41434a"),new(0,0,0,last?0:1),default);Children.Add(_paint);Children.Add(Text);}
        protected override AutomationPeer OnCreateAutomationPeer()=>new CellPeer(this);
        private sealed class CellPeer(Cell owner):ControlAutomationPeer(owner){protected override AutomationControlType GetAutomationControlTypeCore()=>owner._header?AutomationControlType.HeaderItem:OperatingSystem.IsWindows()?AutomationControlType.DataItem:AutomationControlType.Custom;protected override string GetClassNameCore()=>owner._header?"ColumnHeader":"Cell";protected override string GetLocalizedControlTypeCore()=>owner._header?"column header":"cell";protected override string? GetNameCore()=>OperatingSystem.IsWindows()?owner.Text.Text??"":base.GetNameCore();protected override IReadOnlyList<AutomationPeer>? GetChildrenCore()=>OperatingSystem.IsWindows()?Array.Empty<AutomationPeer>():[CreatePeerForElement(owner.Text)];}
        protected override Size MeasureOverride(Size available){var u=(TopLevel.GetTopLevel(this)?.RenderScaling??2)*64;Text.LineHeight=Math.Floor((_header?19.2:20.8)*u)/u;Text.Measure(new(Math.Max(0,available.Width-36),double.PositiveInfinity));return new(available.Width,24+Text.DesiredSize.Height+(_header?.5:_last?.5:1));}
        protected override Size ArrangeOverride(Size final){_paint.Arrange(new Rect(final));Text.Arrange(new(18,(final.Height-Text.DesiredSize.Height)/2,Math.Max(0,final.Width-36),Text.DesiredSize.Height));return final;}
    }
    private sealed class TrialBackgroundTrack(Func<double>? clock=null)
    {
        private double Now=>clock?.Invoke()??Stopwatch.GetTimestamp()*1000d/Stopwatch.Frequency;private Color _from,_to,_reverse;private double _start,_duration,_factor=1;private double P=>_duration<=0?1:Math.Clamp((Now-_start)/_duration,0,1);public bool Running=>P<1;private static double Ease(double p)=>WebMotion.CssEase().Ease(p);
        public Color Current{get{var t=Ease(P);var a=_from.A+(_to.A-_from.A)*t;byte B(double n)=>(byte)Math.Clamp(Math.Round(n,MidpointRounding.AwayFromZero),0,255);byte C(byte x,byte y)=>a==0?(byte)0:B((x*_from.A*(1-t)+y*_to.A*t)/a);return Color.FromArgb(B(a),C(_from.R,_to.R),C(_from.G,_to.G),C(_from.B,_to.B));}}
        public void Snap(Color c){_from=_to=_reverse=c;_duration=0;_factor=1;}public void Set(Color c){if(c==_to)return;var p=P;var current=Current;if(p<1&&c==_reverse){_factor=Math.Clamp(Math.Abs(Ease(p)*_factor+1-_factor),0,1);_reverse=_to;}else{_factor=1;_reverse=current;}_from=current;_to=c;_start=Now;_duration=160*_factor;}
    }
}
