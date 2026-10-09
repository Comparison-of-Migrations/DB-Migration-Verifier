using System.Diagnostics;
using SkiaSharp;
using Avalonia;
using Avalonia.Automation;
using Avalonia.Automation.Peers;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.VisualTree;
namespace Verifier.Desktop;

/// <summary>Literal running()/updateProgress() assembly. The workspace owns the 140ms sample clock.</summary>
public sealed class WebProgressPage : Panel
{
    public static readonly (string Key,string Ko,string En)[] Categories=[("data","데이터","Data"),("schema","구조·제약","Schema & constraints"),("access","사용자·권한","Users & permissions"),("objects","관련 객체","Related objects"),("trial","별도 DB 동작 시험","Behavior tests in a separate DB")];
    public WebPageHeading Heading {get;}=new();
    public WebProgressMeter Meter {get;}=new();
    public WebCapabilityViewport Viewport {get;}=new();
    public WebSystemTextBlock Identifier {get;}=WebTypography.CreateCandidate("",12,19.2,monospaced:true,tabularNumbers:true);
    public WebNotice? InterruptedNotice {get;private set;}
    public ReplicaSampleRun Run {get;private set;}=ReplicaSampleRun.Create(["data","schema","access","objects"],ReplicaSampleRun.AllowedTables,ReplicaSampleRun.AllowedTables.ToDictionary(x=>x,x=>x));
    public bool English {get;private set;}
    public bool ReducedMotion {get;set;}
    public Control ProgressPanel=>_panel;
    public Control Main=>_main;
    public MixedCount Number {get;}=new();
    public InlineNote Note {get;}=new();
    public Control TableShell=>_tableShell;
    public ProgressTable Table {get;}=new();
    private readonly WebSurfaceBox _panel=WebSurface.Create(Brush.Parse("#2b2d31"),Brush.Parse("#41434a"),new(1),new(10),new(new BoxShadow{Color=Color.Parse("#00000010"),OffsetY=2,Blur=4}));
    private readonly MainPanel _main;
    private readonly WebSurfaceBox _tableShell=WebSurface.Create(Brush.Parse("#2b2d31"),Brush.Parse("#41434a"),new(1),new(10));
    private WebMotionPlayback? _entry;
    private string? _noteGroup;
    private double Units=>(TopLevel.GetTopLevel(this)?.RenderScaling??2)*64;
    public WebProgressPage()
    {
        UseLayoutRounding=false;_main=new(Number,Note,Identifier);Viewport.Content=Table;
        Children.Add(Heading);Children.Add(_panel);Children.Add(_main);Children.Add(Meter);Children.Add(_tableShell);Children.Add(Viewport);
        SetPresentation(Run,false);
    }
    private string T(string ko,string en)=>English?en:ko;
    private string Category(string key){var c=Categories.Single(x=>x.Key==key);return T(c.Ko,c.En);}
    public void SetPresentation(ReplicaSampleRun run,bool english,bool liveTick=false)
    {
        Run=run;English=english;_noteGroup=null;
        Heading.SetPresentation(run.Interrupted?T("검사가 중단됐습니다","The check was interrupted"):run.Complete?T("예시 검사가 끝났습니다","Sample checks are finished"):T("선택한 범위를 비교하고 있습니다","Comparing the selected scope"),T("현재 화면의 진행과 결과는 예시입니다. DB 쿼리를 실행하지 않습니다.","Progress and outcomes here are samples. No database queries are being executed."));
        Identifier.Text=run.Id;
        var current=run.Groups.FirstOrDefault(g=>g.Status=="running");
        Note.SetPresentation(current is not null?Category(current.Key)+" · "+T("예시 대조 중","Sample comparison"):run.Interrupted?T("완료·중단·미시작 범위를 나눠 보관합니다.","Completed, interrupted and pending scopes remain separate."):T("불일치와 확인 불가 영역을 결과에서 확인하세요.","Review differences and unverified areas in the results."),current is not null,ReducedMotion);
        Table.SetPresentation(run.Groups,english,false,ReducedMotion);
        if(InterruptedNotice is not null){Children.Remove(InterruptedNotice);InterruptedNotice=null;}
        if(run.Interrupted){InterruptedNotice=new(){Kind="warn",Title=T("전체 재검사만 가능합니다","Only a full new check is available"),Text=T("부분 결과를 확인할 수 있습니다. 중단 자료로 이어서 검사하거나 새 결과와 합치지 않습니다.","You can review the partial result. Interrupted data is not resumed or merged into a new run."),ReducedMotion=ReducedMotion};Children.Add(InterruptedNotice);}
        Counts(false);if(liveTick)Update(run);InvalidateMeasure();
    }
    private void Counts(bool animate){Number.SetPresentation(Run.Groups.Sum(g=>g.Done),Run.Groups.Sum(g=>g.Total),English);Meter.ReducedMotion=ReducedMotion;Meter.SetPresentation(Run.Groups.Sum(g=>g.Done),Run.Groups.Sum(g=>g.Total),English,animate);}
    public void Update(ReplicaSampleRun run)
    {
        if(run.Interrupted!=Run.Interrupted||run.Complete!=Run.Complete||!run.Groups.Select(g=>g.Key).SequenceEqual(Run.Groups.Select(g=>g.Key))){SetPresentation(run,English);return;}
        Run=run;Counts(true);var current=run.Groups.FirstOrDefault(g=>g.Status=="running");var key=current?.Key??"";
        if(_noteGroup!=key){_noteGroup=key;Note.SetPresentation(current is null?T("다음 분류를 준비하고 있습니다.","Preparing the next category."):Category(current.Key)+" · "+T("예시 대조 중","Sample comparison"),current is not null,ReducedMotion);}
        Table.SetPresentation(run.Groups,English,true,ReducedMotion);InvalidateMeasure();
    }
    public void SetFooter(WebActionFooter footer)
    {
        var done=Run.Interrupted||Run.Complete;
        footer.SetContent(Run.Interrupted?T("중단 이유: 사용자 취소 · 새 전체 검사만 가능","Reason: user cancellation · Full new check only"):T("화면 시안의 예시 검사입니다. 실제 DB 읽기·쓰기는 없습니다.","Simulated preview checks; no real DB reads or writes."),null,[done?new("show-results",T("결과 보기","View results"),"primary","arrow"):new("cancel",T("검사 취소","Cancel checks"),"danger","stop")],ReducedMotion);
    }
    protected override Size MeasureOverride(Size s)
    {
        Heading.Measure(new(s.Width,double.PositiveInfinity));var inner=Math.Max(0,s.Width-54);_main.Measure(new(inner,double.PositiveInfinity));Meter.Measure(new(inner,6));
        var panelHeight=54+_main.DesiredSize.Height+16+6;_panel.Measure(new(s.Width,panelHeight));Table.Measure(new(Math.Max(0,s.Width-2),double.PositiveInfinity));Viewport.LogicalWidth=Table.LogicalWidth;Viewport.OverflowWidth=Table.LogicalWidth;Viewport.Measure(new(Math.Max(0,s.Width-2),double.PositiveInfinity));_tableShell.Measure(new(s.Width,Viewport.DesiredSize.Height+2));
        InterruptedNotice?.Measure(new(s.Width,double.PositiveInfinity));return new(s.Width,Heading.DesiredSize.Height+panelHeight+30+Viewport.DesiredSize.Height+2+(InterruptedNotice?.DesiredSize.Height??0));
    }
    protected override Size ArrangeOverride(Size s)
    {
        Heading.Arrange(new(0,0,s.Width,Heading.DesiredSize.Height));var y=Heading.DesiredSize.Height;var h=54+_main.DesiredSize.Height+22;_panel.Arrange(new(0,y,s.Width,h));_main.Arrange(new(27,y+27,Math.Max(0,s.Width-54),_main.DesiredSize.Height));Meter.Arrange(new(27,y+27+_main.DesiredSize.Height+16,Math.Max(0,s.Width-54),6));y+=h+30;
        _tableShell.Arrange(new(0,y,s.Width,Viewport.DesiredSize.Height+2));Viewport.Arrange(new(1,y+1,Math.Max(0,s.Width-2),Viewport.DesiredSize.Height));Viewport.Clip=new RectangleGeometry(new(Viewport.Bounds.Size),9,9);y+=Viewport.DesiredSize.Height+2;InterruptedNotice?.Arrange(new(0,y,s.Width,InterruptedNotice.DesiredSize.Height));return s;
    }
    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e){base.OnAttachedToVisualTree(e);_entry?.Dispose();_entry=WebMotion.Play(this,WebMotionKind.Page,ReducedMotion);}
    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e){_entry?.Dispose();_entry=null;base.OnDetachedFromVisualTree(e);}
    private sealed class MainPanel(MixedCount count,InlineNote note,WebSystemTextBlock id):Panel
    {
        private double _left,_right;
        public MainPanel():this(new(),new(),WebTypography.CreateCandidate("",12,19.2)){}
        protected override Size MeasureOverride(Size s)
        {
            if(Children.Count==0){UseLayoutRounding=false;Children.Add(count);Children.Add(note);Children.Add(id);}var u=(TopLevel.GetTopLevel(this)?.RenderScaling??2)*64;id.LineHeight=Math.Floor(19.2*u)/u;id.Measure(Size.Infinity);_right=Math.Ceiling(id.DesiredSize.Width*u)/u;_left=Math.Max(count.MinimumWidth,Math.Max(0,s.Width-_right-18));count.Measure(new(_left,double.PositiveInfinity));note.Measure(new(_left,double.PositiveInfinity));return new(s.Width,Math.Max(count.DesiredSize.Height+8+note.DesiredSize.Height,id.DesiredSize.Height));
        }
        protected override Size ArrangeOverride(Size s){count.Arrange(new(0,0,_left,count.DesiredSize.Height));note.Arrange(new(0,count.DesiredSize.Height+8,_left,note.DesiredSize.Height));id.Arrange(new(_left+18,s.Height-id.DesiredSize.Height,_right,id.DesiredSize.Height));return s;}
    }
    // These inline fragments are stateless layout details of this page, not independent components.
    public sealed class MixedCount:Panel
    {
        public WebSystemTextBlock Large{get;}=WebTypography.CreateCandidate("0",38,45.6,Brush.Parse("#f2f3f5"),600,-1.33,tabularNumbers:true);
        private readonly List<WebSystemTextBlock> _small=[];
        public IReadOnlyList<WebSystemTextBlock> Small=>_small;
        private string _suffix="/ 0 항목";private double _line,_baseline,_largeWidth;
        private readonly List<(WebSystemTextBlock Text,Rect Box)> _fragments=[];
        private double Units=>(TopLevel.GetTopLevel(this)?.RenderScaling??2)*64;
        public double MinimumWidth {get{Large.Measure(Size.Infinity);return Math.Ceiling(Large.DesiredSize.Width*Units)/Units;}}
        public MixedCount(){UseLayoutRounding=false;Children.Add(Large);for(var i=0;i<3;i++){var t=SmallText("");_small.Add(t);Children.Add(t);}}
        private double SmallLineHeight
        {
            get
            {
                if(!OperatingSystem.IsWindows())return 19;
                var scale=TopLevel.GetTopLevel(this)?.RenderScaling??2;
                using var face=SKTypeface.FromFamilyName(WebTypography.SystemFontFamily.Name,new SKFontStyle(400,5,SKFontStyleSlant.Upright));
                using var font=new SKFont(face,16);var metrics=font.Metrics;
                return (Math.Floor(-metrics.Ascent*scale+.5)+Math.Floor(metrics.Descent*scale+.5))/scale;
            }
        }
        private WebSystemTextBlock SmallText(string text)=>WebTypography.CreateCandidate(text,16,SmallLineHeight,Brush.Parse("#b5bac1"),400,-1.33,tabularNumbers:true);
        private double WidthOf(string text){var t=SmallText(text);t.Measure(Size.Infinity);return t.SystemLineWidths.FirstOrDefault();}
        public void SetPresentation(int done,int total,bool english){Large.Text=done.ToString();_suffix="/ "+total+" "+(english?"checks":"항목");InvalidateMeasure();}
        protected override Size MeasureOverride(Size s)
        {
            _fragments.Clear();var fragmentIndex=0;_line=Math.Floor(45.6*Units)/Units;Large.LineHeight=_line;Large.Measure(Size.Infinity);_baseline=Large.SystemBaseline;_largeWidth=Math.Ceiling(Large.SystemLineWidths.FirstOrDefault()*Units)/Units;
            var tokens=_suffix.Split(' ');var line=0;var x=_largeWidth;var text="";var start=x;var lead=true;
            void Flush(){if(text.Length==0)return;var t=_small[fragmentIndex++];t.Text=text;t.LineHeight=SmallLineHeight;t.IsVisible=true;t.Measure(Size.Infinity);var w=Math.Ceiling(t.SystemLineWidths.FirstOrDefault()*Units)/Units;_fragments.Add((t,new(start,line*_line+_baseline-t.SystemBaseline,w,SmallLineHeight)));text="";}
            foreach(var token in tokens){var trial=text+(lead?"\u00a0":text.Length>0?" ":"")+token;if(x+WidthOf(trial)>s.Width&&x>0){Flush();line++;x=0;start=0;lead=false;trial=token;}text=trial;lead=false;}
            Flush();for(var i=fragmentIndex;i<_small.Count;i++)_small[i].IsVisible=false;return new(s.Width,(line+1)*_line);
        }
        protected override Size ArrangeOverride(Size s){Large.Arrange(new(0,0,_largeWidth,_line));foreach(var f in _fragments)f.Text.Arrange(f.Box);return s;}
    }
    public sealed class InlineNote:Panel
    {
        public WebSystemTextBlock First{get;}=WebTypography.CreateCandidate("",14,22.4,Brush.Parse("#b5bac1"));
        public WebSystemTextBlock Rest{get;}=WebTypography.CreateCandidate("",14,22.4,Brush.Parse("#b5bac1"));
        public WebRunningDot Dot{get;private set;}=new();
        public string Text{get;private set;}="";private bool _dot;private double _line;private readonly WebSystemTextBlock _measure=WebTypography.CreateCandidate("",14,22.4);
        public InlineNote(){UseLayoutRounding=false;Children.Add(First);Children.Add(Rest);Children.Add(Dot);}
        public void SetPresentation(string text,bool dot,bool reduced){Children.Remove(Dot);Dot=new(){IsVisible=dot,ReducedMotion=reduced};Children.Add(Dot);Text=text;_dot=dot;Dot.IsVisible=dot;Dot.ReducedMotion=reduced;InvalidateMeasure();}
        protected override Size MeasureOverride(Size s)
        {
            var u=(TopLevel.GetTopLevel(this)?.RenderScaling??2)*64;_line=Math.Floor(22.4*u)/u;First.LineHeight=Rest.LineHeight=_measure.LineHeight=_line;_measure.Text=Text;_measure.Measure(new(Math.Max(0,s.Width-(_dot?14:0)),double.PositiveInfinity));
            if(!_dot){First.Text=Text;First.Measure(new(s.Width,double.PositiveInfinity));Rest.IsVisible=false;return new(s.Width,First.DesiredSize.Height);}
            var first=_measure.SystemLineTexts.FirstOrDefault()??"";Rest.Text=Text[first.Length..].TrimStart();Rest.IsVisible=Rest.Text.Length>0;First.Text=first;First.Measure(new(Math.Max(0,s.Width-14),double.PositiveInfinity));Rest.Measure(new(s.Width,double.PositiveInfinity));Dot.Measure(Size.Infinity);return new(s.Width,_line+(Rest.IsVisible?Rest.DesiredSize.Height:0));
        }
        protected override Size ArrangeOverride(Size s){First.Arrange(new(_dot?14:0,0,Math.Max(0,s.Width-(_dot?14:0)),First.DesiredSize.Height));if(Rest.IsVisible)Rest.Arrange(new(0,_line,s.Width,Rest.DesiredSize.Height));if(_dot)Dot.Arrange(new(0,First.SystemBaseline-7,14,7));return s;}
    }
    public sealed class ProgressTable:Panel
    {
        public IReadOnlyList<TableRow> Rows=>_rows;public TableRow Header{get;}=new(true,false);
        private readonly List<TableRow> _rows=[];private readonly RowGroup _headGroup=new(),_bodyGroup=new();public double[] Columns{get;private set;}=[36,36,36];public double LogicalWidth{get;private set;}public double[] MinimumColumns{get;private set;}=[36,36,36];public double[] MaximumColumns{get;private set;}=[36,36,36];
        private double Units=>(TopLevel.GetTopLevel(this)?.RenderScaling??2)*64;
        public ProgressTable(){UseLayoutRounding=false;_headGroup.Children.Add(Header);Children.Add(_headGroup);Children.Add(_bodyGroup);}
        public void SetPresentation(IReadOnlyList<ReplicaSampleGroup> groups,bool english,bool animate,bool reduced)
        {
            if(!_rows.Select(r=>r.Key).SequenceEqual(groups.Select(g=>g.Key))){foreach(var row in _rows)_bodyGroup.Children.Remove(row);_rows.Clear();foreach(var g in groups){var r=new TableRow(false,g==groups.Last()){Key=g.Key};_rows.Add(r);_bodyGroup.Children.Add(r);}}
            var labels=english?new[]{"Category","Execution","Processed"}:new[]{"검사 분류","실행 상태","처리 항목"};for(var i=0;i<3;i++)Header.Labels[i].Text=labels[i];
            for(var i=0;i<groups.Count;i++){var g=groups[i];var row=_rows[i];var d=Categories.Single(c=>c.Key==g.Key);row.Labels[0].Text=english?d.En:d.Ko;row.Labels[2].Text=g.Done+" / "+g.Total;row.SetStatus(g.Status,english,animate,reduced);}InvalidateMeasure();
        }
        private (double Min,double Max) Intrinsic(WebSystemTextBlock t,bool spacedCount=false){t.Measure(Size.Infinity);var max=Math.Ceiling(t.SystemLineWidths.DefaultIfEmpty(0).Max()*Units)/Units;if(spacedCount){var min=(t.Text??"").Split(' ',StringSplitOptions.RemoveEmptyEntries).Select(token=>{var part=WebTypography.CreateCandidate(token,12,19.2,monospaced:true,tabularNumbers:true);part.Measure(Size.Infinity);return Math.Ceiling(part.SystemLineWidths.DefaultIfEmpty(0).Max()*Units)/Units;}).DefaultIfEmpty(0).Max();return(min,max);}t.Measure(new(1/Units,double.PositiveInfinity));return(Math.Ceiling(t.SystemLineWidths.DefaultIfEmpty(0).Max()*Units)/Units,max);}
        protected override Size MeasureOverride(Size s)
        {
            var min=new double[3];var max=new double[3];foreach(var row in new[]{Header}.Concat(_rows))for(var i=0;i<3;i++){var v=i==1&&!row.IsHeader?(row.PillWidth(),row.PillWidth()):Intrinsic(row.Labels[i],!row.IsHeader&&i==2);min[i]=Math.Max(min[i],v.Item1+36);max[i]=Math.Max(max[i],v.Item2+36);}
            MinimumColumns=min;MaximumColumns=max;var width=Math.Max(s.Width,min.Sum());LogicalWidth=width;var cols=new double[3];for(var i=0;i<2;i++)cols[i]=Math.Floor((width>=max.Sum()?max[i]*width/max.Sum():min[i]+(width-min.Sum())*(max[i]-min[i])/(max.Sum()-min.Sum()))*Units)/Units;cols[2]=width-cols[0]-cols[1];Columns=cols;
            foreach(var row in new[]{Header}.Concat(_rows)){row.Columns=cols;row.Measure(new(width,double.PositiveInfinity));}_headGroup.Measure(new(width,Header.DesiredSize.Height));_bodyGroup.Measure(new(width,_rows.Sum(r=>r.DesiredSize.Height)));return new(width,Header.DesiredSize.Height+_rows.Sum(r=>r.DesiredSize.Height));
        }
        protected override Size ArrangeOverride(Size s){_headGroup.Arrange(new(0,0,LogicalWidth,Header.DesiredSize.Height));_bodyGroup.Arrange(new(0,Header.DesiredSize.Height,LogicalWidth,_rows.Sum(r=>r.DesiredSize.Height)));return s;}
        protected override AutomationPeer OnCreateAutomationPeer()=>new TablePeer(this);
        private sealed class TablePeer(ProgressTable t):ControlAutomationPeer(t){protected override AutomationControlType GetAutomationControlTypeCore()=>AutomationControlType.Table;}
    }
    private sealed class RowGroup:Panel
    {
        public RowGroup(){UseLayoutRounding=false;}
        protected override Size MeasureOverride(Size s){foreach(var r in Children)r.Measure(new(s.Width,double.PositiveInfinity));return new(s.Width,Children.Sum(r=>r.DesiredSize.Height));}
        protected override Size ArrangeOverride(Size s){var y=0d;foreach(var r in Children){r.Arrange(new(0,y,s.Width,r.DesiredSize.Height));y+=r.DesiredSize.Height;}return s;}
        protected override AutomationPeer OnCreateAutomationPeer()=>new GroupPeer(this);
        private sealed class GroupPeer(RowGroup g):ControlAutomationPeer(g){protected override AutomationControlType GetAutomationControlTypeCore()=>AutomationControlType.Group;protected override string GetClassNameCore()=>"RowGroup";protected override bool IsControlElementCore()=>!OperatingSystem.IsWindows()&&base.IsControlElementCore();protected override bool IsContentElementCore()=>!OperatingSystem.IsWindows()&&base.IsContentElementCore();}
    }
    public sealed class TableRow:Panel
    {
        public bool IsHeader{get;}public string Key{get;set;}="";public string Status{get;private set;}="";public double[] Columns{get;set;}=[36,36,36];public WebSystemTextBlock[] Labels{get;}
        public IReadOnlyList<Cell> Cells=>_cells;private readonly List<Cell> _cells=[];
        public WebStatusPill Pill{get;}=new(){ShowDot=true};private readonly bool _last;private WebMotionPlayback? _enter;private bool _hasLiveStatus;private double _share;
        private readonly RowPaint _paint=new(){IsHitTestVisible=false};
        private readonly BackgroundTrack _background=new();private bool _reduced;private int _hoverRevision;
        public TableRow(bool header,bool last){if(!header)Background=Brushes.Transparent;UseLayoutRounding=false;IsHeader=header;_last=last;_paint.Header=header;_paint.Last=last;Children.Add(_paint);AutomationProperties.SetAccessibilityView(_paint,AccessibilityView.Raw);Labels=Enumerable.Range(0,3).Select(i=>WebTypography.CreateCandidate("",header?12:i==2?12:13,header||i==2?19.2:20.8,Brush.Parse(header?"#b5bac1":"#dbdee1"),header?600:i==0?700:400,monospaced:!header&&i==2,tabularNumbers:!header&&i==2)).ToArray();for(var i=0;i<3;i++){var cell=new Cell(header);cell.Children.Add(!header&&i==1?Pill:Labels[i]);if(header&&OperatingSystem.IsWindows())AutomationProperties.SetAccessibilityView(Labels[i],AccessibilityView.Raw);_cells.Add(cell);Children.Add(cell);}_share=header?.5:last?.5:1;PointerEntered+=(_,_)=>{if(!header)Hover(true);};PointerExited+=(_,_)=>{if(!header)Hover(false);};}
        public double PillWidth(){var text=WebTypography.CreateCandidate(Pill.Text,12,19.2);text.Measure(Size.Infinity);return Math.Ceiling(text.SystemLineWidths.FirstOrDefault()*(TopLevel.GetTopLevel(this)?.RenderScaling??2)*64)/((TopLevel.GetTopLevel(this)?.RenderScaling??2)*64)+28;}
        public void SetStatus(string status,bool english,bool animate,bool reduced)
        {
            _reduced=reduced;var changed=Status!=status;Status=status;
            Pill.Kind=status switch{"completed"=>"ok","running"=>"accent","interrupted"=>"warn",_=>""};
            Pill.Text=(english,status) switch{(true,"completed")=>"Completed",(true,"running")=>"Running",(true,"interrupted")=>"Interrupted",(true,_)=>"Not started",(false,"completed")=>"완료",(false,"running")=>"진행 중",(false,"interrupted")=>"중단",_=>"미시작"};
            if(!animate){_enter?.Dispose();_enter=null;_hasLiveStatus=false;}
            else
            {
                // The source's newly rendered td has no dataset.status until its first live update.
                if(changed||!_hasLiveStatus){_enter?.Dispose();_enter=WebMotion.Play(_cells[1],WebMotionKind.Page,reduced);}
                _hasLiveStatus=true;
            }
            InvalidateMeasure();
        }
        protected override Size MeasureOverride(Size s){var u=(TopLevel.GetTopLevel(this)?.RenderScaling??2)*64;var h=0d;for(var i=0;i<3;i++){Labels[i].LineHeight=Math.Floor(Labels[i].FontSize*1.6*u)/u;if(IsHeader||i!=1){Labels[i].Measure(new(Math.Max(0,Columns[i]-36),double.PositiveInfinity));h=Math.Max(h,Labels[i].DesiredSize.Height);}}if(!IsHeader){Pill.Measure(Size.Infinity);h=Math.Max(h,Pill.DesiredSize.Height);}var height=h+24+_share;for(var i=0;i<3;i++)_cells[i].Measure(new(Columns[i],height));return new(Columns.Sum(),height);}
        protected override Size ArrangeOverride(Size s){_paint.Arrange(new(s));var u=(TopLevel.GetTopLevel(this)?.RenderScaling??2)*64;double x=0;for(var i=0;i<3;i++){var c=i==1&&!IsHeader?(Control)Pill:Labels[i];var w=i==1&&!IsHeader?PillWidth():Math.Max(0,Columns[i]-36);var top=IsHeader?0:.5;_cells[i].Arrange(new(x,0,Columns[i],s.Height));c.Arrange(new(18,12+top+Math.Floor((s.Height-24-_share-c.DesiredSize.Height)*u/2)/u,w,c.DesiredSize.Height));x+=Columns[i];}return s;}
        private void Hover(bool hover){var color=Color.Parse(hover?"#3a3c44":"#00000000");var rev=++_hoverRevision;if(_reduced||TopLevel.GetTopLevel(this) is not{}top){_background.Snap(color);Background=new SolidColorBrush(color);return;}_background.Set(color);void Frame(TimeSpan _){if(rev!=_hoverRevision||!this.IsAttachedToVisualTree())return;Background=new SolidColorBrush(_background.Current);if(_background.Running)top.RequestAnimationFrame(Frame);}top.RequestAnimationFrame(Frame);}
        private sealed class BackgroundTrack(Func<double>? clock=null)
    {
        private double Now=>clock?.Invoke()??Stopwatch.GetTimestamp()*1000d/Stopwatch.Frequency;private Color _from,_to,_reverse;private double _start,_duration,_factor=1;private double P=>_duration<=0?1:Math.Clamp((Now-_start)/_duration,0,1);public bool Running=>P<1;private static double Ease(double p)=>WebMotion.CssEase().Ease(p);
        public Color Current{get{var t=Ease(P);var a=_from.A+(_to.A-_from.A)*t;byte B(double n)=>(byte)Math.Clamp(Math.Round(n,MidpointRounding.AwayFromZero),0,255);byte C(byte x,byte y)=>a==0?(byte)0:B((x*_from.A*(1-t)+y*_to.A*t)/a);return Color.FromArgb(B(a),C(_from.R,_to.R),C(_from.G,_to.G),C(_from.B,_to.B));}}
        public void Snap(Color c){_from=_to=_reverse=c;_duration=0;_factor=1;}public void Set(Color c){if(c==_to)return;var p=P;var current=Current;if(p<1&&c==_reverse){_factor=Math.Clamp(Math.Abs(Ease(p)*_factor+1-_factor),0,1);_reverse=_to;}else{_factor=1;_reverse=current;}_from=current;_to=c;_start=Now;_duration=160*_factor;}
    }
        public sealed class Cell(bool header):Panel
        {
            protected override Size MeasureOverride(Size s)=>s;
            protected override Size ArrangeOverride(Size s)=>s;
            protected override AutomationPeer OnCreateAutomationPeer()=>new CellPeer(this,header);
            private sealed class CellPeer(Cell cell,bool header):ControlAutomationPeer(cell)
            {
                protected override AutomationControlType GetAutomationControlTypeCore()=>OperatingSystem.IsWindows()?AutomationControlType.DataItem:header?AutomationControlType.HeaderItem:AutomationControlType.Custom;
                protected override string GetClassNameCore()=>header?"ColumnHeader":"Cell";
                protected override string GetLocalizedControlTypeCore()=>header?"column header":"cell";
                protected override string? GetNameCore()=>OperatingSystem.IsWindows()?cell.Children[0] switch{WebSystemTextBlock text=>text.Text??"",WebStatusPill pill=>pill.Text,_=>base.GetNameCore()}:base.GetNameCore();
                protected override IReadOnlyList<AutomationPeer>? GetChildrenCore()=>OperatingSystem.IsWindows()?Array.Empty<AutomationPeer>():base.GetChildrenCore();
            }
        }
        private sealed class RowPaint:Control{public bool Header,Last;public override void Render(DrawingContext c){base.Render(c);if(Header)c.DrawRectangle(Brush.Parse("#232428"),null,new(Bounds.Size));if(!Last)c.DrawLine(new Pen(Brush.Parse("#41434a"),1),new(0,Bounds.Height),new(Bounds.Width,Bounds.Height));}}
        protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e){_enter?.Dispose();_enter=null;++_hoverRevision;_background.Snap(default);Background=Brushes.Transparent;base.OnDetachedFromVisualTree(e);}
        protected override AutomationPeer OnCreateAutomationPeer()=>new RowPeer(this);
        private sealed class RowPeer(TableRow row):ControlAutomationPeer(row){protected override AutomationControlType GetAutomationControlTypeCore()=>AutomationControlType.DataItem;protected override string GetClassNameCore()=>"Row";}
    }
}
