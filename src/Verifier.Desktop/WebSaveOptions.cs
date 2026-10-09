using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Threading;

namespace Verifier.Desktop;

public sealed record WebFormatSelection(string Format,bool Selected,bool FocusVisible);

public sealed class WebSaveOptions:Panel
{
    private static readonly string[] Formats=["json","csv","html"];
    private readonly List<string> _selected=["json"];
    private bool _english,_reduced;
    private long _revision;
    public IReadOnlyList<WebDownloadOption> Options=>Children.OfType<WebDownloadOption>().ToArray();
    public IReadOnlyList<string> SelectedFormats=>_selected.ToArray();
    public event EventHandler<WebFormatSelection>? SelectionChanged;
    public bool ReducedMotion{get=>_reduced;set{_reduced=value;foreach(var child in Options)child.ReducedMotion=value;}}
    public WebSaveOptions(){UseLayoutRounding=false;Rebuild();}
    public void SetPresentation(IEnumerable<string> selected,bool english)
    {
        var values=selected.ToArray();if(values.Any(v=>!Formats.Contains(v)))throw new ArgumentOutOfRangeException(nameof(selected));
        var focus=Options.FirstOrDefault(o=>o.Checkbox.IsFocused);var identity=focus?.Format;var keyboard=focus?.Checkbox.FocusVisible??false;
        _selected.Clear();_selected.AddRange(values.Distinct());_english=english;Rebuild(identity,keyboard);
    }
    private void Rebuild(string? focus=null,bool keyboard=false)
    {
        var revision=++_revision;Children.Clear();
        foreach(var format in Formats)
        {
            var option=new WebDownloadOption{ReducedMotion=_reduced,IsSelected=_selected.Contains(format)};option.SetPresentation(format,_english);
            option.SelectionRequested+=(_,_)=>{var visible=option.Checkbox.FocusVisible;var selected=option.IsSelected;if(selected)_selected.Add(format);else _selected.Remove(format);Rebuild(format,visible);SelectionChanged?.Invoke(this,new(format,selected,visible));};
            Children.Add(option);
        }
        InvalidateMeasure();
        if(focus is not null)Dispatcher.UIThread.Post(()=>{if(revision!=_revision)return;var option=Options.First(o=>o.Format==focus);if(keyboard)option.Checkbox.Focus(NavigationMethod.Tab);else option.Checkbox.FocusFromPointer();},DispatcherPriority.Loaded);
    }
    private double Units=>(TopLevel.GetTopLevel(this)?.RenderScaling??2)*64;
    private double Edge(int index,double width)=>Math.Floor(index*(double)(float)(Math.Max(0,width-28)/3)*Units)/Units+index*14;
    protected override Size MeasureOverride(Size available)
    {
        double height=0;for(var i=0;i<Children.Count;i++){var width=Edge(i+1,available.Width)-Edge(i,available.Width)-14;Children[i].Measure(new(Math.Max(0,width),double.PositiveInfinity));height=Math.Max(height,Children[i].DesiredSize.Height);}
        return new(available.Width,height);
    }
    protected override Size ArrangeOverride(Size final)
    {
        for(var i=0;i<Children.Count;i++){var x=Edge(i,final.Width);var width=Edge(i+1,final.Width)-x-14;Children[i].Arrange(new(x,0,Math.Max(0,width),final.Height));}return final;
    }
}
