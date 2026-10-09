using Avalonia;
using Avalonia.Automation;
using Avalonia.Automation.Peers;
using Avalonia.Controls;
using Avalonia.Media;

namespace Verifier.Desktop;

/// <summary>The original DBMS fieldset; connection resets and sample port updates belong to Connection.</summary>
public sealed class WebDbmsPicker:Panel
{
    private static readonly (string Engine,string Description)[] Definitions=[("SQL Server","Microsoft · 1433"),("PostgreSQL","PostgreSQL · 5432"),("MySQL","MySQL · 3306"),("MariaDB","MariaDB · 3306")];
    private readonly PickerGrid _grid=new();
    private bool _setting,_reduced;
    public WebSystemTextBlock Legend{get;}=WebTypography.CreateCandidate("",12,19.2,Brush.Parse("#f2f3f5"),600);
    public Border LegendBox{get;}=new(){Padding=new(2,0),UseLayoutRounding=false,HorizontalAlignment=Avalonia.Layout.HorizontalAlignment.Left};
    public WebSystemTextBlock Note{get;}=WebTypography.CreateCandidate("",12,19.2,Brush.Parse("#b5bac1"));
    public IReadOnlyList<WebDbmsOption> Options=>_grid.Children.Cast<WebDbmsOption>().ToArray();
    public Control Cards=>_grid;
    public string SelectedEngine{get;private set;}="SQL Server";
    public bool English{get;private set;}
    public bool ReducedMotion{get=>_reduced;set{_reduced=value;foreach(var option in Options)option.ReducedMotion=value;}}
    public event EventHandler<string>? EngineChanged;
    public WebDbmsPicker()
    {
        UseLayoutRounding=false;ClipToBounds=false;var group="dbms-"+Guid.NewGuid().ToString("N");
        foreach(var (entry,index) in Definitions.Select((d,i)=>(d,i)))
        {
            var option=new WebDbmsOption{EngineName=entry.Engine,Description=entry.Description,GroupName=group};AutomationProperties.SetAutomationId(option.Radio,"dbms-"+index);
            option.SelectionChanged+=(_,_)=>{if(_setting)return;SelectedEngine=option.EngineName;EngineChanged?.Invoke(this,SelectedEngine);};_grid.Children.Add(option);
        }
        LegendBox.Child=Legend;Children.Add(LegendBox);Children.Add(_grid);Children.Add(Note);SetPresentation(false,"SQL Server",1280);
    }
    public void SetPresentation(bool english,string engine,double viewportWidth)
    {
        English=english;SelectedEngine=engine;Legend.Text=english?"Database engine to compare":"비교할 데이터베이스 종류";Note.Text=english?"Source and target use the same engine. Selecting one updates both sample ports.":"원본과 대상은 같은 종류로 비교합니다. 선택하면 양쪽 예시 포트도 바뀝니다.";
        AutomationProperties.SetName(this,Legend.Text);_setting=true;
        try{foreach(var option in Options){option.Compact=viewportWidth<=1180;option.IsSelected=option.EngineName==engine;}}finally{_setting=false;}InvalidateMeasure();
    }
    protected override Size MeasureOverride(Size available)
    {
        var units=(TopLevel.GetTopLevel(this)?.RenderScaling??2)*64;Legend.LineHeight=Note.LineHeight=Math.Floor(19.2*units)/units;
        Legend.Measure(new(Math.Max(0,available.Width-4),double.PositiveInfinity));LegendBox.Width=Math.Ceiling((Legend.DesiredSize.Width+4)*units)/units;LegendBox.Measure(new(available.Width,double.PositiveInfinity));_grid.Measure(new(available.Width,double.PositiveInfinity));Note.Measure(new(available.Width,double.PositiveInfinity));
        return new(available.Width,Legend.DesiredSize.Height+10+_grid.DesiredSize.Height+10+Note.DesiredSize.Height);
    }
    protected override Size ArrangeOverride(Size final)
    {
        LegendBox.Arrange(new(0,0,LegendBox.DesiredSize.Width,LegendBox.DesiredSize.Height));var y=Legend.DesiredSize.Height+10;_grid.Arrange(new(0,y,final.Width,_grid.DesiredSize.Height));y+=_grid.DesiredSize.Height+10;Note.Arrange(new(0,y,final.Width,Note.DesiredSize.Height));return final;
    }
    protected override AutomationPeer OnCreateAutomationPeer()=>new PickerPeer(this);
    private sealed class PickerPeer(WebDbmsPicker picker):ControlAutomationPeer(picker)
    {protected override AutomationControlType GetAutomationControlTypeCore()=>AutomationControlType.Group;}
    private sealed class PickerGrid:Panel
    {
        public PickerGrid(){UseLayoutRounding=false;ClipToBounds=false;}
        private double Units=>(TopLevel.GetTopLevel(this)?.RenderScaling??2)*64;
        private double Edge(int i,double width)=>Math.Floor(i*(double)(float)(Math.Max(0,width-30)/4)*Units)/Units+i*10;
        protected override Size MeasureOverride(Size available)
        {
            var height=0d;for(var i=0;i<Children.Count;i++){Children[i].Measure(new(Math.Max(0,Edge(i+1,available.Width)-Edge(i,available.Width)-10),double.PositiveInfinity));height=Math.Max(height,Children[i].DesiredSize.Height);}return new(available.Width,height);
        }
        protected override Size ArrangeOverride(Size final)
        {
            for(var i=0;i<Children.Count;i++){var x=Edge(i,final.Width);Children[i].Arrange(new(x,0,Math.Max(0,Edge(i+1,final.Width)-x-10),final.Height));}return final;
        }
    }
}
