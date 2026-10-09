using Avalonia;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.VisualTree;

namespace Verifier.Desktop;

/// <summary>Original label.download-option. Format aggregation and page re-render belong to SaveOptions/Save.</summary>
public sealed class WebDownloadOption : UserControl
{
    private readonly WebSurfaceBox _surface=WebSurface.Create(Brush.Parse("#2b2d31"),Brush.Parse("#41434a"),new(1),new(8));
    private readonly OptionRow _row;
    private bool _setting,_armed;
    public WebCheckbox Checkbox{get;}=new();
    public Control TextGroup=>_row.Body;
    public WebSystemTextBlock TitleText=>_row.Body.Title;
    public WebSystemTextBlock DescriptionText=>_row.Body.Description;
    public double DescriptionBaseline=>_row.Body.InlineBaseline;
    public string Format{get;private set;}="json";
    public bool IsSelected{get=>Checkbox.IsChecked==true;set{_setting=true;try{Checkbox.SetCurrentValue(WebCheckbox.IsCheckedProperty,value);}finally{_setting=false;}}}
    public bool ReducedMotion{get=>Checkbox.ReducedMotion;set=>Checkbox.ReducedMotion=value;}
    public event EventHandler? SelectionRequested;
    public WebDownloadOption()
    {
        UseLayoutRounding=false;Focusable=false;IsTabStop=false;Cursor=new(StandardCursorType.Hand);
        _row=new(Checkbox);_surface.Padding=new(22);_surface.Child=_row;Content=_surface;
        Checkbox.PropertyChanged+=(_,e)=>{if(e.Property==WebCheckbox.IsCheckedProperty){Paint();if(!_setting)SelectionRequested?.Invoke(this,EventArgs.Empty);}};
        SetPresentation("json",false);Paint();
    }
    public void SetPresentation(string format,bool english)
    {
        Format=format;
        (TitleText.Text,DescriptionText.Text)=format switch{
            "json"=>english?("JSON for reopening","Scope, states and safe findings"):("다시 열기용 JSON","검사 범위·상태·안전한 결과"),
            "csv"=>("CSV",english?"Category outcomes and safe findings":"분류별 판정과 안전한 차이 목록"),
            "html"=>("HTML",english?"Readable sample report":"브라우저에서 읽는 예시 보고서"),
            _=>throw new ArgumentOutOfRangeException(nameof(format))};
        AutomationProperties.SetAutomationId(Checkbox,"format-"+format);
        AutomationProperties.SetName(Checkbox,TitleText.Text+" "+DescriptionText.Text);
        InvalidateMeasure();
    }
    private void Paint()
    {
        // :has(input:checked) wins background/border; later :has(:checked) supplies the inset shadow.
        _surface.Background=Brush.Parse(IsSelected?"#3a3e58":"#2b2d31");_surface.BorderBrush=Brush.Parse(IsSelected?"#5865f2":"#41434a");
        _surface.BoxShadow=IsSelected?new(new BoxShadow{IsInset=true,Spread=1,Color=Color.Parse("#8b94ff")}):default;
    }
    private bool FromCheckbox(object? source)=>source is Visual v&&(v==Checkbox||Checkbox.IsVisualAncestorOf(v));
    protected override void OnPointerPressed(PointerPressedEventArgs e){base.OnPointerPressed(e);if(FromCheckbox(e.Source)||!IsEffectivelyEnabled||!e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)return;_armed=true;e.Pointer.Capture(this);e.Handled=true;}
    protected override void OnPointerReleased(PointerReleasedEventArgs e){base.OnPointerReleased(e);if(!_armed||e.InitialPressMouseButton!=MouseButton.Left)return;_armed=false;e.Pointer.Capture(null);if(IsEffectivelyEnabled&&new Rect(Bounds.Size).Contains(e.GetPosition(this))){Checkbox.FocusFromPointer();Checkbox.SetCurrentValue(WebCheckbox.IsCheckedProperty,!IsSelected);}e.Handled=true;}
    protected override void OnPointerCaptureLost(PointerCaptureLostEventArgs e){_armed=false;base.OnPointerCaptureLost(e);}
    private sealed class OptionRow:Panel
    {
        public OptionText Body{get;}=new();
        private readonly WebCheckbox _checkbox;
        public OptionRow(WebCheckbox checkbox){_checkbox=checkbox;UseLayoutRounding=false;Children.Add(_checkbox);Children.Add(Body);}
        protected override Size MeasureOverride(Size available)
        {
            _checkbox.Measure(new(20,20));Body.Measure(new(Math.Max(0,available.Width-32),double.PositiveInfinity));
            return new(double.IsPositiveInfinity(available.Width)?Body.DesiredSize.Width+32:available.Width,Math.Max(20,Body.DesiredSize.Height));
        }
        protected override Size ArrangeOverride(Size final)
        {
            var units=(TopLevel.GetTopLevel(this)?.RenderScaling??2)*64;
            _checkbox.Arrange(new(0,Math.Floor((final.Height-20)*units/2)/units,20,20));
            Body.Arrange(new(32,0,Body.ContentWidth,final.Height));return final;
        }
    }
    private sealed class OptionText:Panel
    {
        public WebSystemTextBlock Title{get;}=WebTypography.CreateCandidate("",14,22.4,Brush.Parse("#f2f3f5"),700);
        public WebSystemTextBlock Description{get;}=WebTypography.CreateCandidate("",12,22.4,Brush.Parse("#b5bac1"));
        private readonly WebSystemTextBlock _strut=WebTypography.CreateCandidate("M",14,22.4);
        public double ContentWidth{get;private set;}
        public double InlineBaseline{get;private set;}
        public OptionText(){UseLayoutRounding=false;Children.Add(Title);Children.Add(Description);Children.Add(_strut);_strut.Opacity=0;_strut.IsHitTestVisible=false;AutomationProperties.SetAccessibilityView(_strut,AccessibilityView.Raw);}
        protected override Size MeasureOverride(Size available)
        {
            var units=(TopLevel.GetTopLevel(this)?.RenderScaling??2)*64;
            Title.LineHeight=Description.LineHeight=_strut.LineHeight=Math.Floor(22.4*units)/units;
            _strut.Measure(Size.Infinity);Title.Measure(Size.Infinity);Description.Measure(Size.Infinity);
            var maximum=Math.Ceiling(Math.Max(Title.DesiredSize.Width,Description.DesiredSize.Width)*units)/units;
            Title.Measure(new(1/units,double.PositiveInfinity));Description.Measure(new(1/units,double.PositiveInfinity));
            var minimum=Math.Ceiling(Math.Max(Title.SystemLineWidths.DefaultIfEmpty().Max(),Description.SystemLineWidths.DefaultIfEmpty().Max())*units)/units;
            var width=ContentWidth=Math.Max(minimum,Math.Min(available.Width,maximum));
            Title.Measure(new(width,double.PositiveInfinity));Description.Measure(new(width,double.PositiveInfinity));
            InlineBaseline=Title.DesiredSize.Height+4+_strut.SystemBaseline;
            Description.RenderTransform=new TranslateTransform(0,_strut.SystemBaseline-Description.SystemBaseline);
            return new(width,Math.Max(64,Title.DesiredSize.Height+4+Description.DesiredSize.Height));
        }
        protected override Size ArrangeOverride(Size final){Title.Arrange(new(0,0,final.Width,Title.DesiredSize.Height));Description.Arrange(new(0,Title.DesiredSize.Height+4,final.Width,Description.DesiredSize.Height));_strut.Arrange(new(0,0,0,0));return final;}
    }
}
