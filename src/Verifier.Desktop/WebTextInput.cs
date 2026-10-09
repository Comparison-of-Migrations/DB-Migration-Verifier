using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Templates;
using Avalonia.Data;
using Avalonia.Input;
using Avalonia.Input.Platform;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.VisualTree;
using System.Diagnostics;
using Avalonia.Interactivity;
using System.Runtime.InteropServices;

namespace Verifier.Desktop;

/// <summary>Source single-line input. Labels, icons, clear buttons and number steppers belong to parents.</summary>
public class WebTextInput : TextBox
{
    protected override Type StyleKeyOverride=>typeof(TextBox);
    private readonly WebSurfaceBox _surface;
    private readonly TextPresenter _presenter;
    private readonly WebInputCaret _caret;
    private readonly ScrollViewer _scroll;
    private readonly WebSystemTextBlock _placeholder;
    private readonly FocusTrack _border = new(new(255,124,131,140));
    private readonly FocusTrack _glow = new(default);
    private long _revision;
    private bool _search, _reduced;
    public IBrush NativeCaretBrush=>Brushes.Transparent;
    // A number-input parent filters both typing and paste at the current selection.
    // Null preserves the locked plain text/search insertion contract.
    public Func<string,string>? InsertionFilter {get;set;}
    private string FilterInsertion(string text)
    {var line=SingleLine(text);return InsertionFilter?.Invoke(line)??line;}
    public bool SearchMode
    {
        get => _search;
        set { _search=value;FontSize=value?13:14;LineHeight=value?20.8:22.4;Padding=value?new(40,9,44,9):new(12,9);Sync(); }
    }
    public bool ReducedMotion { get=>_reduced;set {_reduced=value;UpdateFocus(false);} }
    public WebTextInput()
    {
        UseLayoutRounding=false;ClipToBounds=false;Opacity=1;FocusAdorner=null;
        MinHeight=44;Padding=new(12,9);Margin=default;BorderThickness=new(1);CornerRadius=new(8);
        FontFamily=WebTypography.SystemFontFamily;FontSize=14;FontWeight=FontWeight.Normal;LineHeight=22.4;
        TextWrapping=TextWrapping.NoWrap;AcceptsReturn=false;AcceptsTab=false;
        // HTML inputs preserve their selection range on blur; Avalonia defaults to clearing it.
        ClearSelectionOnLostFocus=false;
        HorizontalContentAlignment=HorizontalAlignment.Left;VerticalContentAlignment=VerticalAlignment.Center;
        Foreground=Brush.Parse("#dbdee1");Background=Brush.Parse("#1e1f22");BorderBrush=Brush.Parse("#7c838c");
        PlaceholderForeground=Brush.Parse("#a3a9b2");CaretBrush=Brush.Parse("#dbdee1");
        if(OperatingSystem.IsMacOS())CaretBlinkInterval=MacEditorSystemColors.CaretBlinkInterval();
        else if(OperatingSystem.IsWindows())
        {var period=GetCaretBlinkTime();CaretBlinkInterval=period==uint.MaxValue?TimeSpan.Zero:TimeSpan.FromMilliseconds(period==0?500:period);}
        if(OperatingSystem.IsMacOS())
        {SelectionBrush=new SolidColorBrush(MacEditorSystemColors.SelectedBackground());SelectionForegroundBrush=Brushes.White;}
        else if(OperatingSystem.IsWindows())
        {
            // Recorded from the actual reference Edge profile, not Fluent's editor accent.
            SelectionBrush=Brush.Parse("#063aa4");SelectionForegroundBrush=Brushes.White;
        }
        _presenter=new WebEditorTextPresenter {Name="PART_TextPresenter",UseLayoutRounding=false,TextWrapping=TextWrapping.NoWrap,
            HorizontalAlignment=HorizontalAlignment.Left,VerticalAlignment=VerticalAlignment.Center};
        _placeholder=WebTypography.CreateCandidate("",14,22.4,PlaceholderForeground);
        _placeholder.TextWrapping=TextWrapping.NoWrap;_placeholder.IsHitTestVisible=false;_placeholder.VerticalAlignment=VerticalAlignment.Center;
        _caret=new WebInputCaret(this,_presenter);
        var content=new EditorContentPanel{UseLayoutRounding=false,ClipToBounds=true};content.Children.Add(_placeholder);content.Children.Add(_presenter);content.Children.Add(_caret);
        var scroll=new ScrollViewer{Name="PART_ScrollViewer",Content=content,UseLayoutRounding=false,
            HorizontalScrollBarVisibility=ScrollBarVisibility.Hidden,VerticalScrollBarVisibility=ScrollBarVisibility.Disabled};
        _scroll=scroll;
        scroll.PropertyChanged+=(_,change)=>
        {
            if(change.Property==ScrollViewer.OffsetProperty && IsFocused)
            {
                var scale=TopLevel.GetTopLevel(this)?.RenderScaling??1;
                var x=Math.Floor(scroll.Offset.X*scale+.5)/scale;
                if(Math.Abs(x-scroll.Offset.X)>1e-9)scroll.Offset=new Vector(x,scroll.Offset.Y);
            }
        };
        _surface=WebSurface.Create(Background,BorderBrush,new(1),new(8));_surface.Child=scroll;_surface.Padding=Padding;
        Template=new FuncControlTemplate<WebTextInput>((_,scope)=>
        {
            scope.Register("PART_TextPresenter",_presenter);scope.Register("PART_ScrollViewer",scroll);
            foreach(var pair in new (AvaloniaProperty,string)[]{(TextPresenter.TextProperty,"Text"),(TextPresenter.CaretIndexProperty,"CaretIndex"),
                (TextPresenter.SelectionStartProperty,"SelectionStart"),(TextPresenter.SelectionEndProperty,"SelectionEnd"),
                (TextPresenter.PasswordCharProperty,"PasswordChar"),(TextPresenter.RevealPasswordProperty,"RevealPassword"),
                (TextPresenter.CaretBlinkIntervalProperty,"CaretBlinkInterval"),(TextPresenter.ShowSelectionHighlightProperty,"IsFocused"),(TextPresenter.SelectionBrushProperty,"SelectionBrush"),
                (TextPresenter.SelectionForegroundBrushProperty,"SelectionForegroundBrush"),(TextPresenter.CaretBrushProperty,"NativeCaretBrush")})
                _presenter.Bind(pair.Item1,new Binding(pair.Item2){Source=this,Mode=pair.Item2=="Text"?BindingMode.TwoWay:BindingMode.OneWay});
            return _surface;
        });
        _presenter.PropertyChanged+=(_,c)=>{if(c.Property==TextPresenter.PreeditTextProperty){Sync();_caret.Refresh();}};
        PastingFromClipboard+=PasteSingleLine;
        Sync();
    }
    [DllImport("user32.dll")]private static extern uint GetCaretBlinkTime();
    private static string SingleLine(string text)=>text.Replace("\r","").Replace("\n","");
    private async void PasteSingleLine(object? sender,RoutedEventArgs e)
    {
        e.Handled=true;
        if(IsReadOnly || !IsEffectivelyEnabled || TopLevel.GetTopLevel(this)?.Clipboard is not { } clipboard)return;
        string? input;
        try{input=await clipboard.TryGetTextAsync();}
        catch(Exception error)when(error is TimeoutException or OperationCanceledException or UnauthorizedAccessException or COMException){return;}
        if(!string.IsNullOrEmpty(input) && IsEffectivelyEnabled && !IsReadOnly)
        {
            var value=FilterInsertion(input);
            if(value.Length>0)SelectedText=value;
        }
    }
    protected override void OnTextInput(TextInputEventArgs e)
    {
        if(e.Text is not null)e.Text=FilterInsertion(e.Text);
        base.OnTextInput(e);
    }
    private void Sync()
    {
        if(_surface is null)return;
        _surface.Background=Background;_surface.Padding=Padding;
        _presenter.FontFamily=FontFamily;_presenter.FontSize=FontSize;_presenter.FontWeight=FontWeight;
        _presenter.LineHeight=LineHeight;_presenter.Foreground=Foreground;
        _placeholder.FontFamily=FontFamily;_placeholder.FontSize=FontSize;_placeholder.LineHeight=LineHeight;
        _placeholder.Foreground=PlaceholderForeground;_placeholder.Text=PlaceholderText;
        _placeholder.IsVisible=string.IsNullOrEmpty(Text)&&string.IsNullOrEmpty(_presenter.PreeditText);
        Cursor=new Cursor(IsEffectivelyEnabled?StandardCursorType.Ibeam:StandardCursorType.Arrow);
    }
    private void UpdateFocus(bool animate=true)
    {
        if(_surface is null)return;
        var border=IsFocused?new Rgba(255,167,177,255):new Rgba(255,124,131,140);
        var glow=IsFocused?new Rgba(34,167,177,255):default;
        var revision=++_revision;
        if(!animate || _reduced || TopLevel.GetTopLevel(this) is not { } top)
        {_border.Snap(border);_glow.Snap(glow);PaintFocus();return;}
        _border.Target(border);_glow.Target(glow);
        void Frame(TimeSpan _)
        {
            if(revision!=_revision || !this.IsAttachedToVisualTree())return;
            PaintFocus();
            if(_border.Running || _glow.Running)top.RequestAnimationFrame(Frame);
        }
        top.RequestAnimationFrame(Frame);
    }
    private void PaintFocus()
    {
        var glow=_glow.Current;
        _surface.BorderBrush=new SolidColorBrush(_border.Current.Color);
        // The transparent 'none' endpoint also has zero spread in CSS.
        _surface.BoxShadow=glow.A<=0?default:new BoxShadows(new BoxShadow
        {Spread=3*glow.A/34,Color=glow.Color});
    }
    private sealed class EditorContentPanel:Panel
    {
        protected override Size MeasureOverride(Size availableSize)
        {
            var desired=base.MeasureOverride(availableSize);
            var scale=TopLevel.GetTopLevel(this)?.RenderScaling??1;
            return new Size(Math.Floor(desired.Width*scale+.5)/scale,desired.Height);
        }
    }
    private readonly record struct Rgba(double A,double R,double G,double B)
    {
        public Color Color=>Color.FromArgb(Byte(A),Byte(R),Byte(G),Byte(B));
        private static byte Byte(double v)=>(byte)Math.Clamp(Math.Round(v,MidpointRounding.AwayFromZero),0,255);
        public static Rgba Mix(Rgba a,Rgba b,double t)
        {
            var alpha=a.A+(b.A-a.A)*t;
            double Channel(double x,double y)=>alpha==0?0:(x*a.A*(1-t)+y*b.A*t)/alpha;
            return new(alpha,Channel(a.R,b.R),Channel(a.G,b.G),Channel(a.B,b.B));
        }
    }
    private sealed class FocusTrack(Rgba initial,Func<double>? clock=null)
    {
        private Rgba _from=initial,_target=initial,_reversingStart=initial;
        private double _factor=1,_duration,_started;
        private double Now=>clock?.Invoke()??Stopwatch.GetTimestamp()*(1000d/Stopwatch.Frequency);
        private double Progress=>_duration<=0?1:Math.Clamp((Now-_started)/_duration,0,1);
        public bool Running=>Progress<1;
        public Rgba Current=>Rgba.Mix(_from,_target,WebMotion.CssEase().Ease(Progress));
        public void Snap(Rgba value){_from=_target=_reversingStart=value;_duration=0;_factor=1;}
        public void Target(Rgba value)
        {
            if(_target==value)return;
            var progress=Progress;var eased=WebMotion.CssEase().Ease(progress);var current=Rgba.Mix(_from,_target,eased);
            if(progress<1 && _reversingStart==value)
            {_factor=Math.Clamp(Math.Abs(eased*_factor+1-_factor),0,1);_reversingStart=_target;}
            else{_factor=1;_reversingStart=current;}
            _from=current;_target=value;_duration=120*_factor;_started=Now;
        }
    }
    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if(change.Property==TextProperty || change.Property==CaretIndexProperty || change.Property==SelectionStartProperty || change.Property==SelectionEndProperty || change.Property==IsEffectivelyEnabledProperty)_caret?.Refresh();
        if(change.Property==TextProperty || change.Property==PlaceholderTextProperty || change.Property==PlaceholderForegroundProperty ||
            change.Property==FontFamilyProperty || change.Property==FontSizeProperty || change.Property==FontWeightProperty ||
            change.Property==LineHeightProperty || change.Property==PaddingProperty || change.Property==BackgroundProperty ||
            change.Property==ForegroundProperty || change.Property==IsEffectivelyEnabledProperty)Sync();
    }
    protected override void OnGotFocus(FocusChangedEventArgs e)
    {base.OnGotFocus(e);UpdateFocus();_caret.Refresh();
        if(e.NavigationMethod==NavigationMethod.Tab)_scroll.Offset=default;
        else if(_presenter.IsAttachedToVisualTree())
        {var rect=_presenter.TextLayout.HitTestTextPosition(CaretIndex);_presenter.BringIntoView(new Rect(rect.X,rect.Y,1,rect.Height));}}
    protected override void OnLostFocus(FocusChangedEventArgs e)
    {base.OnLostFocus(e);UpdateFocus();_caret.Refresh();_scroll.Offset=default;}
    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {++_revision;base.OnDetachedFromVisualTree(e);UpdateFocus(false);}
    protected override void OnKeyDown(KeyEventArgs e)
    {
        _caret.Refresh();
        if(_search && e.Key==Key.Escape && !string.IsNullOrEmpty(Text))
        {SetCurrentValue(TextProperty,"");CaretIndex=SelectionStart=SelectionEnd=0;e.Handled=true;return;}
        base.OnKeyDown(e);
    }
}
