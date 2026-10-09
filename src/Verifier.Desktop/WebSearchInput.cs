using Avalonia;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;

namespace Verifier.Desktop;

public enum WebSearchContext { Tables, Matching }

/// <summary>Source search-field; filtering and selection remain the parent's policy.</summary>
public sealed class WebSearchInput : UserControl
{
    public WebTextInput Editor { get; } = new() { SearchMode = true };
    public WebActionButton ClearButton { get; } = WebIconButton.Create("");
    private WebSearchContext _context;
    public string Text { get => Editor.Text ?? ""; set => Editor.Text = value; }
    public string Placeholder { get => Editor.PlaceholderText ?? ""; set => Editor.PlaceholderText = value; }
    public string AccessibleName { get => AutomationProperties.GetName(Editor) ?? ""; set => AutomationProperties.SetName(Editor,value); }
    public string ClearAccessibleName { get => AutomationProperties.GetName(ClearButton) ?? ""; set => WebIconButton.SetAccessibleName(ClearButton,value); }
    public bool ReducedMotion { get => Editor.ReducedMotion; set { Editor.ReducedMotion=value; ClearButton.ReducedMotion=value; } }
    public WebSearchContext Context
    {
        get => _context;
        set { _context=value; MaxWidth=value==WebSearchContext.Tables?380:double.PositiveInfinity; Margin=value==WebSearchContext.Tables?default:new Thickness(0,10,0,14); ClearButton.IsVisible=value==WebSearchContext.Tables; }
    }
    public event EventHandler? QueryChanged;
    public event EventHandler? ClearInvoked;
    public WebSearchInput()
    {
        UseLayoutRounding=false;Focusable=false;IsTabStop=false;MinWidth=180;
        var search=new SvgIcon{IconName="search",Width=18,Height=18,Stroke=Brush.Parse("#b5bac1"),
            HorizontalAlignment=HorizontalAlignment.Left,VerticalAlignment=VerticalAlignment.Top,Margin=new Thickness(13,13,0,0),IsHitTestVisible=false};
        ClearButton.HorizontalAlignment=HorizontalAlignment.Right;ClearButton.VerticalAlignment=VerticalAlignment.Top;
        ClearButton.LabelFontSize=12;
        var grid=new Grid{UseLayoutRounding=false};grid.Children.Add(Editor);grid.Children.Add(search);grid.Children.Add(ClearButton);Content=grid;
        Context=WebSearchContext.Tables;ClearButton.IsEnabled=false;
        Editor.PropertyChanged+=(_,e)=>
        {
            if(e.Property!=TextBox.TextProperty)return;
            ClearButton.IsEnabled=!string.IsNullOrEmpty(Editor.Text);QueryChanged?.Invoke(this,EventArgs.Empty);
        };
        ClearButton.Click+=(_,_)=>{Text="";ClearInvoked?.Invoke(this,EventArgs.Empty);Editor.Focus(NavigationMethod.Pointer);};
        // Chromium search-input consumes Escape only while it has an editable
        // value. Empty Escape remains available to the containing dialog.
        Editor.AddHandler(KeyDownEvent,(_,e)=>
        {
            if(e.Key!=Key.Escape||Editor.IsReadOnly||!Editor.IsEffectivelyEnabled||string.IsNullOrEmpty(Editor.Text))return;
            Text="";e.Handled=true;
        },RoutingStrategies.Tunnel);
    }
}
