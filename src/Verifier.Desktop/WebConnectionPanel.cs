using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;

namespace Verifier.Desktop;

/// <summary>Original connectionSide panel; connection/validation policy belongs to the page.</summary>
public sealed class WebConnectionPanel:Panel
{
    private readonly WebSurfaceBox _surface=WebSurface.Create(Brush.Parse("#2b2d31"),Brush.Parse("#41434a"),new(1),new(10));
    private readonly ConnectionFlow _flow=new();
    public Control Header=>_flow.Header;
    public Control RoleMark=>_flow.Header.Mark;
    public SvgIcon RoleIcon=>_flow.Header.Icon;
    public Control TitleGroup=>_flow.Header.Titles;
    public WebSystemTextBlock Title=>_flow.Header.Titles.Title;
    public WebSystemTextBlock Subtitle=>_flow.Header.Titles.Subtitle;
    public WebStatusPill Status=>_flow.Header.Status;
    public Control ServerRow=>_flow.ServerRow;
    public WebField[] Fields=>_flow.Fields;
    public WebTextInput HostInput{get;}=new();
    public WebNumberInput PortInput{get;}=new();
    public WebTextInput DatabaseInput{get;}=new();
    public WebTextInput UserInput{get;}=new();
    public WebTextInput PasswordInput{get;}=new(){IsEnabled=false,PasswordChar='•'};
    public event EventHandler<string>? InputChanged;
    public WebConnectionPanel()
    {
        UseLayoutRounding=false;ClipToBounds=false;_surface.Padding=new(26);_surface.Child=_flow;
        _surface.BoxShadow= new(new BoxShadow{OffsetY=2,Blur=4,Color=Color.FromArgb(16,0,0,0)});
        Children.Add(_surface);
        Control[] inputs=[HostInput,PortInput,DatabaseInput,UserInput,PasswordInput];
        for(var i=0;i<inputs.Length;i++)_flow.Fields[i].Input=inputs[i];
        foreach(var (input,key) in new[]{(HostInput,"host"),(DatabaseInput,"db"),(UserInput,"user")})
            input.PropertyChanged+=(_,change)=>{if(change.Property==TextBox.TextProperty)InputChanged?.Invoke(this,key);};
        PortInput.Input+=(_,_)=>InputChanged?.Invoke(this,"port");
        SetPresentation("ko","source",false);
    }
    public void SetPresentation(string language,string side,bool connected)
    {
        bool ko=language=="ko",source=side=="source";
        Title.Text=source?(ko?"원본 DB":"Source DB"):(ko?"대상 DB":"Target DB");
        Subtitle.Text=source?(ko?"마이그레이션 전 상태":"Before migration"):(ko?"마이그레이션 후 상태":"After migration");
        Status.Text=connected?(ko?"예시 연결 확인":"Sample connected"):(ko?"미확인":"Not checked");Status.Kind=connected?"ok":"";
        string[] labels=ko?["서버 주소","포트","데이터베이스","조회 계정","비밀번호"]:["Server","Port","Database","Reader account","Password"];
        for(var i=0;i<labels.Length;i++)Fields[i].Label=labels[i];
        PasswordInput.PlaceholderText=ko?"시안에서는 실제 비밀번호를 입력하지 않습니다":"Do not enter a real password in this preview";
        Fields[4].Hint=ko?"실제 앱에서는 기본 미저장, 선택 시 OS 보안 저장소를 사용합니다.":"The actual app will not save credentials by default; OS vault storage is optional.";
        InvalidateMeasure();
    }
    protected override Size MeasureOverride(Size available){_surface.Measure(available);return _surface.DesiredSize;}
    protected override Size ArrangeOverride(Size final){_surface.Arrange(new Rect(final));return final;}
    private sealed class ConnectionFlow:Panel
    {
        public ConnectionHeader Header{get;}=new();
        public PortRow ServerRow{get;}=new();
        public WebField[] Fields{get;}=Enumerable.Range(0,5).Select(_=>new WebField()).ToArray();
        public ConnectionFlow(){UseLayoutRounding=false;Children.Add(Header);ServerRow.Children.Add(Fields[0]);ServerRow.Children.Add(Fields[1]);Children.Add(ServerRow);foreach(var f in Fields.Skip(2))Children.Add(f);}
        protected override Size MeasureOverride(Size available)
        {double h=0;foreach(var c in Children){c.Measure(new(available.Width,double.PositiveInfinity));h+=c.DesiredSize.Height;}return new(available.Width,h);}
        protected override Size ArrangeOverride(Size final)
        {double y=0;foreach(var c in Children){c.Arrange(new(0,y,final.Width,c.DesiredSize.Height));y+=c.DesiredSize.Height;}return final;}
    }
    private sealed class PortRow:Panel
    {
        public PortRow(){UseLayoutRounding=false;}
        protected override Size MeasureOverride(Size available)
        {Children[0].Measure(new(Math.Max(0,available.Width-128),double.PositiveInfinity));Children[1].Measure(new(112,double.PositiveInfinity));return new(available.Width,Children.Max(c=>c.DesiredSize.Height));}
        protected override Size ArrangeOverride(Size final)
        {Children[0].Arrange(new(0,0,Math.Max(0,final.Width-128),final.Height));Children[1].Arrange(new(final.Width-112,0,112,final.Height));return final;}
    }
    private sealed class ConnectionHeader:Panel
    {
        public WebSurfaceBox Mark{get;}=WebSurface.Create(Brush.Parse("#353740"),Brush.Parse("#4c505a"),new(1),new(8));
        public SvgIcon Icon{get;}=new(){IconName="db",Width=22,Height=22,Stroke=Brush.Parse("#b5bac1"),HorizontalAlignment=Avalonia.Layout.HorizontalAlignment.Center,VerticalAlignment=Avalonia.Layout.VerticalAlignment.Center};
        public HeaderTitles Titles{get;}=new();
        public WebStatusPill Status{get;}=new();
        private readonly WebSurfaceBox _line=WebSurface.Create(Brushes.Transparent,Brush.Parse("#41434a"),new(0,0,0,1),default);
        private double _markWidth,_titleWidth,_pillWidth,_rowHeight;
        public ConnectionHeader(){UseLayoutRounding=false;Mark.Child=new MarkCenter(Icon);Status.HorizontalAlignment=Avalonia.Layout.HorizontalAlignment.Stretch;Children.Add(_line);Children.Add(Mark);Children.Add(Titles);Children.Add(Status);}
        protected override Size MeasureOverride(Size available)
        {
            Titles.Measure(Size.Infinity);Status.Measure(Size.Infinity);
            var units=(TopLevel.GetTopLevel(this)?.RenderScaling??2)*64;
            _titleWidth=Math.Ceiling(Titles.DesiredSize.Width*units)/units;_pillWidth=Math.Ceiling(Status.DesiredSize.Width*units)/units;_markWidth=40;
            var overflow=_markWidth+_titleWidth+_pillWidth+24-available.Width;
            if(overflow>0){var titleBase=_titleWidth;var shrinkMark=Math.Min(16,overflow*38/(38+titleBase));_markWidth=Math.Floor((40-shrinkMark)*units+.5)/units;_titleWidth=available.Width-24-_pillWidth-_markWidth;}
            Mark.Measure(new(_markWidth,40));Titles.Measure(new(_titleWidth,double.PositiveInfinity));
            _rowHeight=Math.Max(40,Math.Max(Titles.DesiredSize.Height,Status.DesiredSize.Height));return new(available.Width,_rowHeight+21);
        }
        protected override Size ArrangeOverride(Size final)
        {
            var units=(TopLevel.GetTopLevel(this)?.RenderScaling??2)*64;double Center(double h)=>Math.Floor((_rowHeight-h)*units/2)/units;
            _line.Arrange(new(0,0,final.Width,final.Height));Mark.Arrange(new(0,Center(40),_markWidth,40));
            Titles.Arrange(new(_markWidth+12,Center(Titles.DesiredSize.Height),_titleWidth,Titles.DesiredSize.Height));
            Status.Arrange(new(final.Width-_pillWidth,Center(Status.DesiredSize.Height),_pillWidth,Status.DesiredSize.Height));return final;
        }
    }
    private sealed class MarkCenter:Panel
    {
        public MarkCenter(SvgIcon icon){UseLayoutRounding=false;Children.Add(icon);}
        protected override Size MeasureOverride(Size available){Children[0].Measure(new(22,22));return new(22,22);}
        protected override Size ArrangeOverride(Size final)
        {var units=(TopLevel.GetTopLevel(this)?.RenderScaling??2)*64;Children[0].Arrange(new(Math.Floor((final.Width-22)*units/2)/units,Math.Floor((final.Height-22)*units/2)/units,22,22));return final;}
    }
    private sealed class HeaderTitles:Panel
    {
        public WebSystemTextBlock Title{get;}=WebTypography.CreateCandidate("",18,27,Brush.Parse("#f2f3f5"),700,-.27);
        public WebSystemTextBlock Subtitle{get;}=WebTypography.CreateCandidate("",12,19.2,Brush.Parse("#b5bac1"));
        public HeaderTitles(){UseLayoutRounding=false;Children.Add(Title);Children.Add(Subtitle);}
        protected override Size MeasureOverride(Size available)
        {var units=(TopLevel.GetTopLevel(this)?.RenderScaling??2)*64;Subtitle.LineHeight=Math.Floor(19.2*units)/units;Title.Measure(available);Subtitle.Measure(available);return new(Math.Max(Title.DesiredSize.Width,Subtitle.DesiredSize.Width),Title.DesiredSize.Height+Subtitle.DesiredSize.Height);}
        protected override Size ArrangeOverride(Size final)
        {Title.Arrange(new(0,0,final.Width,Title.DesiredSize.Height));Subtitle.Arrange(new(0,Title.DesiredSize.Height,final.Width,Subtitle.DesiredSize.Height));return final;}
    }
}
