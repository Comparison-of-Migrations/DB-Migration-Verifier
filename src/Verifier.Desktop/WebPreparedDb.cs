using Avalonia;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.VisualTree;

namespace Verifier.Desktop;

/// <summary>The original passive prepared-db status card. Its parent supplies the original responsive 70%/66% width cap.</summary>
public sealed class WebPreparedDb : Decorator
{
    private readonly WebSurfaceBox _surface=WebSurface.Create(Brush.Parse("#2b3330"),Brush.Parse("#6bdda3"),new(2,0,0,0),new(0,8,8,0));
    private readonly CardRow _row=new();
    private WebMotionPlayback? _entry;
    private bool _reduced;
    public SvgIcon DatabaseIcon=>_row.DatabaseIcon;
    public SvgIcon CheckIcon=>_row.CheckIcon;
    public Control TextGroup=>_row.Body;
    public WebSystemTextBlock NameText=>_row.Body.NameText;
    public WebSystemTextBlock MetaText=>_row.Body.MetaText;
    public WebSystemTextBlock HintText=>_row.Body.HintText;
    public bool ReducedMotion{get=>_reduced;set{if(_reduced==value)return;_reduced=value;if(this.IsAttachedToVisualTree())StartEntry();}}
    public WebPreparedDb()
    {
        UseLayoutRounding=false;_surface.Padding=new(16,10);_surface.MinHeight=64;_surface.Child=_row;Child=_surface;
        AutomationProperties.SetAutomationId(this,"PreparedDb");AutomationProperties.SetLiveSetting(this,AutomationLiveSetting.Polite);
        AttachedToVisualTree+=(_,_)=>StartEntry();DetachedFromVisualTree+=(_,_)=>{_entry?.Dispose();_entry=null;};
        SetPresentation("SQL Server","migration_test","trial.example.invalid",false);
    }
    private void StartEntry(){_entry?.Dispose();_entry=WebMotion.Play(this,WebMotionKind.Page,ReducedMotion);}
    public void SetPresentation(string dbms,string database,string host,bool english)
    {
        NameText.Text=database;MetaText.Text=dbms+" · "+host;HintText.Text=english?"Preparation confirmed · Sample details":"준비 확인 완료 · 예시 정보";
        AutomationProperties.SetName(this,string.Join(" ",new[]{NameText.Text,MetaText.Text,HintText.Text}));InvalidateMeasure();
    }
    protected override Size MeasureOverride(Size available)
    {
        _row.Measure(Size.Infinity);var units=(TopLevel.GetTopLevel(this)?.RenderScaling??2)*64;
        var natural=Math.Ceiling((_row.DesiredSize.Width+34)*units)/units;
        _surface.Measure(new(Math.Min(natural,available.Width),double.PositiveInfinity));return _surface.DesiredSize;
    }
    protected override Size ArrangeOverride(Size final){_surface.Arrange(new Rect(final));return final;}
    private sealed class CardRow:Panel
    {
        public SvgIcon DatabaseIcon{get;}=new(){IconName="db",Width=20,Height=20,Stroke=Brush.Parse("#6bdda3")};
        public SvgIcon CheckIcon{get;}=new(){IconName="check",Width=20,Height=20,Stroke=Brush.Parse("#6bdda3")};
        public CardText Body{get;}=new();
        public CardRow(){UseLayoutRounding=false;Children.Add(DatabaseIcon);Children.Add(Body);Children.Add(CheckIcon);}
        protected override Size MeasureOverride(Size available)
        {
            DatabaseIcon.Measure(new(20,20));CheckIcon.Measure(new(20,20));Body.Measure(new(Math.Max(0,available.Width-64),double.PositiveInfinity));
            return new(double.IsPositiveInfinity(available.Width)?Body.DesiredSize.Width+64:available.Width,Math.Max(20,Body.DesiredSize.Height));
        }
        protected override Size ArrangeOverride(Size final)
        {
            var units=(TopLevel.GetTopLevel(this)?.RenderScaling??2)*64;double Center(double h)=>Math.Floor((final.Height-h)*units/2)/units;
            DatabaseIcon.Arrange(new(0,Center(20),20,20));Body.Arrange(new(32,Center(Body.DesiredSize.Height),Math.Max(0,final.Width-64),Body.DesiredSize.Height));CheckIcon.Arrange(new(final.Width-20,Center(20),20,20));return final;
        }
    }
    private sealed class CardText:Panel
    {
        public WebSystemTextBlock NameText{get;}=WebTypography.CreateCandidate("",13,20.8,Brush.Parse("#f2f3f5"),700);
        public WebSystemTextBlock MetaText{get;}=WebTypography.CreateCandidate("",11,17.6,Brush.Parse("#b5bac1"));
        public WebSystemTextBlock HintText{get;}=WebTypography.CreateCandidate("",11,17.6,Brush.Parse("#b5bac1"));
        public CardText(){UseLayoutRounding=false;Children.Add(NameText);Children.Add(MetaText);Children.Add(HintText);foreach(var text in Children.OfType<WebSystemTextBlock>())text.TextWrapping=TextWrapping.Wrap;}
        private static bool Empty(WebSystemTextBlock text)=>string.IsNullOrWhiteSpace(text.Text);
        protected override Size MeasureOverride(Size available)
        {
            var units=(TopLevel.GetTopLevel(this)?.RenderScaling??2)*64;NameText.LineHeight=Math.Floor(20.8*units)/units;MetaText.LineHeight=HintText.LineHeight=Math.Floor(17.6*units)/units;
            double width=0,height=0;foreach(var text in Children.OfType<WebSystemTextBlock>()){text.Measure(available);if(Empty(text))continue;width=Math.Max(width,text.DesiredSize.Width);height+=text.DesiredSize.Height;}
            return new(double.IsPositiveInfinity(available.Width)?width:available.Width,height);
        }
        protected override Size ArrangeOverride(Size final)
        {double y=0;foreach(var text in Children.OfType<WebSystemTextBlock>()){var height=Empty(text)?0:text.DesiredSize.Height;text.Arrange(new(0,y,final.Width,height));y+=height;}return final;}
    }
}
