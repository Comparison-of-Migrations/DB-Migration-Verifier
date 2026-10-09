using Avalonia;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Layout;

namespace Verifier.Desktop;

/// <summary>Original trial-preparation block. Preparation is a parent-page action, never a DB operation here.</summary>
public sealed class WebTrialPreparation : Panel
{
    public WebNotice? Notice {get;private set;}
    public WebActionButton? PrepareButton {get;private set;}
    public WebPreparedDb? PreparedDb {get;private set;}
    public bool ReducedMotion {get;set;}
    public double ViewportWidth {get;set;}=1280;
    public event EventHandler? PreparationRequested;
    private double _rowHeight;
    public WebTrialPreparation(){UseLayoutRounding=false;AutomationProperties.SetAutomationId(this,"TrialPreparation");}
    public void SetPresentation(bool selected,bool ready,string dbms,string database,string host,bool english)
    {
        // Source updateScopeSelection replaces this block's children, including their mount animations.
        Children.Clear();Notice=null;PrepareButton=null;PreparedDb=null;Margin=selected?new(0,20,0,0):default;
        if(!selected){InvalidateMeasure();return;}
        string T(string ko,string en)=>english?en:ko;
        Notice=new WebNotice{Margin=default,ReducedMotion=ReducedMotion,Kind=ready?"ok":"warn",
            Title=ready?T("시험 DB 준비가 확인됐습니다 · 예시","Test DB preparation confirmed · Sample"):T("동작 시험에 사용할 공간을 준비하세요","Prepare a space for behavior tests"),
            Text=ready?T("확인한 예시 DB에서만 동작 시험을 진행합니다. 원본·대상 비교는 읽기 전용입니다.","Behavior tests use only the confirmed sample test DB. Source and target comparison stays read-only."):T("원본·대상과 분리된 시험 DB와 값·기대 결과를 확인해야 합니다.","Confirm a separate test DB, trial inputs and expected outcomes.")};
        PrepareButton=new WebActionButton{IconName="db",ReducedMotion=ReducedMotion,Content=ready?T("시험 준비 수정","Edit test setup"):T("시험 DB 준비","Prepare a test DB")};
        AutomationProperties.SetAutomationId(PrepareButton,"TrialPreparation-prepare");PrepareButton.Click+=(_,_)=>PreparationRequested?.Invoke(this,EventArgs.Empty);
        Children.Add(Notice);Children.Add(PrepareButton);
        if(ready){PreparedDb=new(){ReducedMotion=ReducedMotion};PreparedDb.SetPresentation(dbms,database,host,english);Children.Add(PreparedDb);}
        InvalidateMeasure();
    }
    protected override Size MeasureOverride(Size available)
    {
        if(Notice is null||PrepareButton is null)return new(double.IsFinite(available.Width)?available.Width:0,0);
        Notice.Measure(new(available.Width,double.PositiveInfinity));PrepareButton.Measure(Size.Infinity);
        if(PreparedDb is not null){var units=(TopLevel.GetTopLevel(this)?.RenderScaling??2)*64;PreparedDb.MaxWidth=Math.Floor((ViewportWidth<=1180?.66:.70)*available.Width*units)/units;PreparedDb.Measure(new(available.Width,double.PositiveInfinity));}
        _rowHeight=Math.Max(PrepareButton.DesiredSize.Height,PreparedDb?.DesiredSize.Height??0);
        return new(available.Width,Notice.DesiredSize.Height+16+_rowHeight);
    }
    protected override Size ArrangeOverride(Size final)
    {
        if(Notice is null||PrepareButton is null)return final;
        Notice.Arrange(new(0,0,final.Width,Notice.DesiredSize.Height));var y=Notice.DesiredSize.Height+16;var units=(TopLevel.GetTopLevel(this)?.RenderScaling??2)*64;
        double Center(double h)=>Math.Floor((_rowHeight-h)*units/2)/units;
        PrepareButton.Arrange(new(0,y+Center(PrepareButton.DesiredSize.Height),PrepareButton.DesiredSize.Width,PrepareButton.DesiredSize.Height));
        if(PreparedDb is not null)PreparedDb.Arrange(new(final.Width-PreparedDb.DesiredSize.Width,y+Center(PreparedDb.DesiredSize.Height),PreparedDb.DesiredSize.Width,PreparedDb.DesiredSize.Height));
        return final;
    }
}
