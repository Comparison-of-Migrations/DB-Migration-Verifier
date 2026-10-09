using Avalonia;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.VisualTree;
namespace Verifier.Desktop;

/// <summary>Literal plain label.choice; parent owns margins and replacement/focus identity.</summary>
public sealed class WebChoice : Panel
{
    public WebCheckbox Checkbox { get; } = new();
    public WebSystemTextBlock Label { get; } = WebTypography.CreateCandidate("",14,22.4,Brush.Parse("#dbdee1"));
    public event EventHandler? SelectionRequested;
    private bool _setting, _armed; private double _width;
    public bool IsSelected { get => Checkbox.IsChecked == true; set { _setting=true;try { Checkbox.SetCurrentValue(WebCheckbox.IsCheckedProperty,value); } finally { _setting=false; } } }
    public bool ReducedMotion { get => Checkbox.ReducedMotion; set => Checkbox.ReducedMotion=value; }
    public WebChoice() {
        UseLayoutRounding=false;Focusable=false;IsTabStop=false;Cursor=new(StandardCursorType.Hand);Children.Add(Checkbox);Children.Add(Label);
        Checkbox.PropertyChanged+=(_,e)=>{if(e.Property==WebCheckbox.IsCheckedProperty&&!_setting)SelectionRequested?.Invoke(this,EventArgs.Empty);};
    }
    public void SetPresentation(string text,string flag,bool selected,bool enabled=true) {
        Label.Text=text;IsSelected=selected;Checkbox.IsEnabled=enabled;AutomationProperties.SetAutomationId(Checkbox,"flag-"+flag);AutomationProperties.SetName(Checkbox,WebTextWhitespace.Collapse(text));InvalidateMeasure();
    }
    private bool FromInput(object? source)=>source is Visual v&&(v==Checkbox||Checkbox.IsVisualAncestorOf(v));
    protected override void OnPointerPressed(PointerPressedEventArgs e) { base.OnPointerPressed(e);if(FromInput(e.Source)||!Checkbox.IsEffectivelyEnabled||!e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)return;_armed=true;e.Pointer.Capture(this);e.Handled=true; }
    protected override void OnPointerReleased(PointerReleasedEventArgs e) { base.OnPointerReleased(e);if(!_armed||e.InitialPressMouseButton!=MouseButton.Left)return;_armed=false;e.Pointer.Capture(null);if(Checkbox.IsEffectivelyEnabled&&new Rect(Bounds.Size).Contains(e.GetPosition(this))){Checkbox.FocusFromPointer();Checkbox.SetCurrentValue(WebCheckbox.IsCheckedProperty,!IsSelected);}e.Handled=true; }
    protected override void OnPointerCaptureLost(PointerCaptureLostEventArgs e) { _armed=false;base.OnPointerCaptureLost(e); }
    protected override Size MeasureOverride(Size available) {
        var units=(TopLevel.GetTopLevel(this)?.RenderScaling??2)*64;Label.LineHeight=Math.Floor(22.4*units)/units;
        Checkbox.Measure(new(20,20));Label.Measure(Size.Infinity);var maximum=Math.Ceiling(Label.DesiredSize.Width*units)/units;
        Label.Measure(new(1/units,double.PositiveInfinity));var minimum=Math.Ceiling(Label.SystemLineWidths.DefaultIfEmpty().Max()*units)/units;
        _width=Math.Max(minimum,Math.Min(maximum,Math.Max(0,available.Width-32)));Label.Measure(new(_width,double.PositiveInfinity));
        return new(double.IsPositiveInfinity(available.Width)?_width+32:available.Width,Math.Max(48,Math.Max(20,Label.DesiredSize.Height)+16));
    }
    protected override Size ArrangeOverride(Size final) {
        var units=(TopLevel.GetTopLevel(this)?.RenderScaling??2)*64;double Center(double height)=>Math.Floor((final.Height-height)*units/2)/units;
        Checkbox.Arrange(new(0,Center(20),20,20));Label.Arrange(new(32,Center(Label.DesiredSize.Height),_width,Label.DesiredSize.Height));return final;
    }
}
