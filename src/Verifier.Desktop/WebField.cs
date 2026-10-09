using Avalonia;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;

namespace Verifier.Desktop;

/// <summary>Original label/input/help block; connection and validation actions belong to its parent.</summary>
public sealed class WebField : UserControl
{
    private readonly FieldStack _stack=new();
    private bool _armed;
    public WebSystemTextBlock LabelText=>_stack.Label;
    public Border LabelHitArea=>_stack.LabelBox;
    public WebSystemTextBlock HintText=>_stack.Hint;
    public string Label {get=>LabelText.Text??"";set{LabelText.Text=value??"";NameInput();}}
    public string? Hint {get=>HintText.IsVisible?HintText.Text:null;set{HintText.Text=value??"";HintText.IsVisible=value is not null;InvalidateMeasure();}}
    public Control? Input {get=>_stack.Input;set{_stack.Input=value;NameInput();}}
    public WebField()
    {
        UseLayoutRounding=false;ClipToBounds=false;Focusable=false;IsTabStop=false;Margin=new(0,16,0,0);Content=_stack;Hint=null;
        LabelHitArea.PointerPressed+=(_,e)=>{if(!e.GetCurrentPoint(LabelHitArea).Properties.IsLeftButtonPressed)return;TopLevel.GetTopLevel(this)?.FocusManager?.Focus(null,NavigationMethod.Pointer);_armed=true;e.Pointer.Capture(LabelHitArea);};
        LabelHitArea.PointerReleased+=(_,e)=>{if(!_armed||e.InitialPressMouseButton!=MouseButton.Left)return;_armed=false;e.Pointer.Capture(null);if(new Rect(LabelHitArea.Bounds.Size).Contains(e.GetPosition(LabelHitArea))){var target=Target();if(target?.IsEffectivelyEnabled==true)target.Focus(NavigationMethod.Pointer);else TopLevel.GetTopLevel(this)?.FocusManager?.Focus(null,NavigationMethod.Pointer);}};
        LabelHitArea.PointerCaptureLost+=(_,_)=>_armed=false;
    }
    private Control? Target()=>Input is WebNumberInput number?number.Editor:Input;
    private void NameInput(){if(Input is{} input){AutomationProperties.SetName(input,Label);AutomationProperties.SetLabeledBy(input,LabelText);}if(Target() is{} target)AutomationProperties.SetName(target,Label);}
    private sealed class FieldStack : Panel
    {
        public WebSystemTextBlock Label {get;}=WebTypography.CreateCandidate("",12,19.2,Brush.Parse("#dbdee1"),600);
        public Border LabelBox {get;}
        public WebSystemTextBlock Hint {get;}=WebTypography.CreateCandidate("",12,19.2,Brush.Parse("#b5bac1"));
        private Control? _input;
        public Control? Input {get=>_input;set{if(_input is not null)Children.Remove(_input);_input=value;if(value is not null)Children.Add(value);InvalidateMeasure();}}
        public FieldStack(){UseLayoutRounding=false;ClipToBounds=false;LabelBox=new(){Background=Brushes.Transparent,Child=Label,UseLayoutRounding=false};Children.Add(LabelBox);Children.Add(Hint);}
        protected override Size MeasureOverride(Size available)
        {
            var unit=(TopLevel.GetTopLevel(this)?.RenderScaling??2)*64;Label.LineHeight=Hint.LineHeight=Math.Floor(12*1.6*unit)/unit;LabelBox.Measure(new(available.Width,double.PositiveInfinity));_input?.Measure(new(available.Width,double.PositiveInfinity));if(Hint.IsVisible)Hint.Measure(new(available.Width,double.PositiveInfinity));
            return new(Math.Max(Label.DesiredSize.Width,Math.Max(_input?.DesiredSize.Width??0,Hint.IsVisible?Hint.DesiredSize.Width:0)),Label.DesiredSize.Height+7+(_input?.DesiredSize.Height??0)+(Hint.IsVisible?5+Hint.DesiredSize.Height:0));
        }
        protected override Size ArrangeOverride(Size final)
        {
            LabelBox.Arrange(new(0,0,final.Width,Label.DesiredSize.Height));var y=Label.DesiredSize.Height+7;if(_input is not null){_input.Arrange(new(0,y,final.Width,_input.DesiredSize.Height));y+=_input.DesiredSize.Height;}if(Hint.IsVisible)Hint.Arrange(new(0,y+5,final.Width,Hint.DesiredSize.Height));return final;
        }
    }
}
