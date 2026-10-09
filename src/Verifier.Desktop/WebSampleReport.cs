using Avalonia;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Media;

namespace Verifier.Desktop;

/// <summary>Literal passive home() aside; viewport visibility and page entrance belong to Home.</summary>
public sealed class WebSampleReport:Decorator
{
    private readonly Body _body=new();
    public WebSurfaceBox Surface{get;}=WebSurface.Create(Brush.Parse("#2b2d31"),Brush.Parse("#4b4f59"),new(1),new(12),BoxShadows.Parse("0 12 30 #25000000"));
    public Control Head=>_body.Head;
    public WebSystemTextBlock HeadText=>_body.Head.Label;
    public SvgIcon Icon=>_body.Head.Icon;
    public Control TitleBlock=>_body.Title;
    public IReadOnlyList<WebSystemTextBlock> TitleLines=>_body.Title.Lines;
    public IReadOnlyList<ReportLine> Lines=>_body.Lines;
    public Control FootBlock=>_body.Foot;
    public IReadOnlyList<WebSystemTextBlock> FootLines=>_body.Foot.Lines;
    public bool English{get;private set;}
    public WebSampleReport(){UseLayoutRounding=false;Surface.Padding=new(22,18);Surface.Child=_body;Child=Surface;AutomationProperties.SetAutomationId(this,"SampleReport");SetPresentation(false);}
    public void SetPresentation(bool english)
    {
        English=english;AutomationProperties.SetName(this,english?"Sample verification report":"예시 검사 보고서");
        HeadText.Text=english?"Sample result preview":"예시 결과 미리보기";
        _body.Title.Set(english?["Differences and verified","scope, together."]:["차이와 확인 범위를","함께 봅니다."]);
        var labels=english?new[]{"Different checks","Unverified checks","Equal checks"}:new[]{"불일치 항목","확인 불가·미검증","일치 항목"};
        for(var i=0;i<3;i++)Lines[i].Label.Text=labels[i];
        _body.Foot.Set(english?["These counts are samples.","No actual database connection."]:["이 수치는 예시입니다.","실제 DB에 접속하지 않습니다."]);InvalidateMeasure();
    }
    private static double Q(Control c,double v){var units=(TopLevel.GetTopLevel(c)?.RenderScaling??2)*64;return Math.Floor(v*units)/units;}
    private static double C(Control c,double v){var units=(TopLevel.GetTopLevel(c)?.RenderScaling??2)*64;return Math.Ceiling(v*units)/units;}
    private sealed class Body:Panel
    {
        public Header Head{get;}=new();public HardBreakBlock Title{get;}=new(15,23.25,"#f2f3f5",650,-.3);public HardBreakBlock Foot{get;}=new(10,17,"#b5bac1");
        public ReportLine[] Lines{get;}=[new("3","#ff959d"),new("2","#f0c47c"),new("45","#f2f3f5")];
        public Body(){UseLayoutRounding=false;Children.Add(Head);Children.Add(Title);foreach(var line in Lines)Children.Add(line);Children.Add(Foot);}
        protected override Size MeasureOverride(Size available){foreach(var c in Children)c.Measure(new(available.Width,double.PositiveInfinity));return new(available.Width,Children.Sum(x=>x.DesiredSize.Height)+12+6+12);}
        protected override Size ArrangeOverride(Size final){double y=0;void Put(Control c){c.Arrange(new(0,y,final.Width,c.DesiredSize.Height));y+=c.DesiredSize.Height;}Put(Head);y+=12;Put(Title);y+=6;foreach(var l in Lines)Put(l);y+=12;Put(Foot);return final;}
    }
    private sealed class Header:Panel
    {
        private readonly WebSurfaceBox _rule=new(){Background=Brush.Parse("#41434a"),UseLayoutRounding=false};
        public WebSystemTextBlock Label{get;}=WebTypography.CreateCandidate("",12,19.2,Brush.Parse("#b5bac1"));public SvgIcon Icon{get;}=new(){IconName="file",Width=20,Height=20,Stroke=Brush.Parse("#b5bac1")};
        public Header(){UseLayoutRounding=false;Children.Add(Label);Children.Add(Icon);Children.Add(_rule);}
        protected override Size MeasureOverride(Size available){Label.LineHeight=Q(this,19.2);Label.Measure(new(Math.Max(0,available.Width-20),double.PositiveInfinity));Icon.Measure(new(20,20));_rule.Measure(new(available.Width,1));return new(available.Width,Math.Max(20,Label.DesiredSize.Height)+11);}
        protected override Size ArrangeOverride(Size final){var h=final.Height-11;Label.Arrange(new(0,Q(this,(h-Label.DesiredSize.Height)/2),C(this,Label.DesiredSize.Width),Label.DesiredSize.Height));Icon.Arrange(new(final.Width-20,Q(this,(h-20)/2),20,20));_rule.Arrange(new(0,final.Height-1,final.Width,1));return final;}
    }
    public sealed class ReportLine:Panel
    {
        private readonly WebSurfaceBox _rule=new(){Background=Brush.Parse("#41434a"),UseLayoutRounding=false};
        public WebSystemTextBlock Label{get;}=WebTypography.CreateCandidate("",12,19.2,Brush.Parse("#b5bac1"));public WebSystemTextBlock Value{get;}
        internal ReportLine(string value,string color){UseLayoutRounding=false;Value=WebTypography.CreateCandidate(value,17,27.2,Brush.Parse(color),600,tabularNumbers:true);Children.Add(Label);Children.Add(Value);Children.Add(_rule);}
        protected override Size MeasureOverride(Size available){Label.LineHeight=Q(this,19.2);Value.LineHeight=Q(this,27.2);Value.Measure(Size.Infinity);_rule.Measure(new(available.Width,1));Label.Measure(new(Math.Max(0,available.Width-Value.DesiredSize.Width),double.PositiveInfinity));return new(available.Width,Math.Max(Label.DesiredSize.Height,Value.DesiredSize.Height)+17);}
        protected override Size ArrangeOverride(Size final){var h=final.Height-17;Label.Arrange(new(0,8+Q(this,(h-Label.DesiredSize.Height)/2),C(this,Label.DesiredSize.Width),Label.DesiredSize.Height));Value.Arrange(new(final.Width-C(this,Value.DesiredSize.Width),8+Q(this,(h-Value.DesiredSize.Height)/2),C(this,Value.DesiredSize.Width),Value.DesiredSize.Height));_rule.Arrange(new(0,final.Height-1,final.Width,1));return final;}
    }
    private sealed class HardBreakBlock:Panel
    {
        private readonly double _lineHeight;public WebSystemTextBlock[] Lines{get;}
        public HardBreakBlock(double size,double height,string color,int weight=400,double spacing=0){UseLayoutRounding=false;_lineHeight=height;Lines=[WebTypography.CreateCandidate("",size,height,Brush.Parse(color),weight,spacing),WebTypography.CreateCandidate("",size,height,Brush.Parse(color),weight,spacing)];foreach(var line in Lines)Children.Add(line);}
        public void Set(string[] values){for(var i=0;i<2;i++)Lines[i].Text=values[i];InvalidateMeasure();}
        protected override Size MeasureOverride(Size available){double h=0;foreach(var line in Lines){line.LineHeight=Q(this,_lineHeight);line.Measure(new(available.Width,double.PositiveInfinity));h+=line.DesiredSize.Height;}return new(available.Width,h);}
        protected override Size ArrangeOverride(Size final){double y=0;foreach(var line in Lines){line.Arrange(new(0,y,final.Width,line.DesiredSize.Height));y+=line.DesiredSize.Height;}return final;}
    }
}
