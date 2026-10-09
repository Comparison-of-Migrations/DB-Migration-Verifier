using Avalonia;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Media;

namespace Verifier.Desktop;

/// <summary>Original topAndRail header, including its explicit .language button overrides.</summary>
public sealed class WebTopBar:Panel
{
    private readonly WebSurfaceBox _surface=WebSurface.Create(Brush.Parse("#1e1f22"),Brush.Parse("#393b41"),new(0,0,0,1),default);
    private readonly HeaderFlex _row=new();
    public Control BrandBox=>_row.Brand;
    public Control BrandIconBox=>_row.Brand.IconBox;
    public SvgIcon BrandIcon=>_row.Brand.Icon;
    public WebSystemTextBlock BrandText=>_row.Brand.Text;
    public Control PreviewBox=>_row.PreviewBox;
    public WebSystemTextBlock PreviewText=>_row.PreviewText;
    public WebActionButton LanguageButton=>_row.Language;
    public event EventHandler<string>? LanguageRequested;
    public WebTopBar()
    {
        UseLayoutRounding=false;ClipToBounds=false;Height=60;
        _surface.Padding=new(24,0);_surface.Child=_row;Children.Add(_surface);SetLanguage("ko");
    }
    public void SetLanguage(string language,bool reducedMotion=false)
    {
        var korean=language=="ko";
        _row.PreviewText.Text=korean?"설계 시안 · 실제 DB 연결 없음":"Design preview · No real database connection";
        var b=new WebActionButton{Variant="base",Content=korean?"한국어 / EN":"English / KO",Padding=new(12,0),LabelFontSize=12,LabelWeight=400,
            RestingBackgroundOverride=Colors.Transparent,BorderColorOverride=Colors.Transparent,HoverBackgroundOverride=Color.Parse("#383a40"),ReducedMotion=reducedMotion};
        b.RefreshParentStyle();AutomationProperties.SetName(b,korean?"영어로 변경":"Switch to Korean");
        b.Click+=(_,_)=>LanguageRequested?.Invoke(this,korean?"en":"ko");
        _row.SetLanguageButton(b);InvalidateMeasure();
    }
    protected override Size MeasureOverride(Size availableSize){_surface.Measure(availableSize);return _surface.DesiredSize;}
    protected override Size ArrangeOverride(Size finalSize){_surface.Arrange(new Rect(finalSize));return finalSize;}
    private sealed class HeaderFlex:Panel
    {
        public HeaderBrand Brand{get;}=new();
        public WebSystemTextBlock PreviewText{get;}=WebTypography.CreateCandidate("",12,19.2,Brush.Parse("#b5bac1"));
        public WebSurfaceBox PreviewBox{get;}=WebSurface.Create(Brushes.Transparent,Brush.Parse("#41434a"),new(1,0,0,0),default);
        public WebActionButton Language{get;private set;}=null!;
        private double _brandWidth,_previewWidth,_buttonWidth;
        public HeaderFlex(){UseLayoutRounding=false;ClipToBounds=false;PreviewBox.Padding=new(16,0,0,0);PreviewBox.Child=PreviewText;Children.Add(Brand);Children.Add(PreviewBox);}
        public void SetLanguageButton(WebActionButton button){if(Language is not null)Children.Remove(Language);Language=button;Children.Add(button);}
        protected override Size MeasureOverride(Size availableSize)
        {
            var units=(TopLevel.GetTopLevel(this)?.RenderScaling??2)*64;
            PreviewText.LineHeight=Math.Floor(12*1.6*units)/units;
            Brand.Measure(Size.Infinity);PreviewBox.Measure(Size.Infinity);Language.Measure(Size.Infinity);
            _brandWidth=Brand.DesiredSize.Width;_previewWidth=Math.Ceiling(PreviewBox.DesiredSize.Width*units)/units;_buttonWidth=Language.DesiredSize.Width;
            return new(double.IsFinite(availableSize.Width)?availableSize.Width:_brandWidth+_previewWidth+_buttonWidth+40,Math.Max(Brand.DesiredSize.Height,Math.Max(PreviewBox.DesiredSize.Height,Language.DesiredSize.Height)));
        }
        protected override Size ArrangeOverride(Size finalSize)
        {
            var units=(TopLevel.GetTopLevel(this)?.RenderScaling??2)*64;
            double Center(Control c)=>Math.Floor((finalSize.Height-c.DesiredSize.Height)*units/2)/units;
            Brand.Arrange(new(0,Center(Brand),_brandWidth,Brand.DesiredSize.Height));
            Language.Arrange(new(finalSize.Width-_buttonWidth,Center(Language),_buttonWidth,Language.DesiredSize.Height));
            PreviewBox.Arrange(new(finalSize.Width-_buttonWidth-20-_previewWidth,Center(PreviewBox),_previewWidth,PreviewBox.DesiredSize.Height));return finalSize;
        }
    }
    private sealed class HeaderBrand:Panel
    {
        public WebSystemTextBlock Text{get;}=WebTypography.CreateCandidate("DB-Migration-Verifier",14,22.4,Brush.Parse("#f2f3f5"),650,-.35);
        public SvgIcon Icon{get;}=new(){IconName="shield",Width=20,Height=20,Stroke=Brushes.White};
        public WebSurfaceBox IconBox{get;}=WebSurface.Create(Brush.Parse("#5865f2"),Brushes.Transparent,default,new(8));
        private double _textWidth;
        public HeaderBrand(){UseLayoutRounding=false;ClipToBounds=false;IconBox.Padding=new(5);IconBox.Child=Icon;Children.Add(IconBox);Children.Add(Text);}
        protected override Size MeasureOverride(Size availableSize)
        {
            var units=(TopLevel.GetTopLevel(this)?.RenderScaling??2)*64;Text.LineHeight=Math.Floor(14*1.6*units)/units;
            IconBox.Measure(new(30,30));Text.Measure(Size.Infinity);_textWidth=Math.Ceiling(Text.DesiredSize.Width*units)/units;
            return new(42+_textWidth,Math.Max(30,Text.DesiredSize.Height));
        }
        protected override Size ArrangeOverride(Size finalSize)
        {
            var units=(TopLevel.GetTopLevel(this)?.RenderScaling??2)*64;IconBox.Arrange(new(0,(finalSize.Height-30)/2,30,30));
            Text.Arrange(new(42,Math.Floor((finalSize.Height-Text.DesiredSize.Height)*units/2)/units,_textWidth,Text.DesiredSize.Height));return finalSize;
        }
    }
}
