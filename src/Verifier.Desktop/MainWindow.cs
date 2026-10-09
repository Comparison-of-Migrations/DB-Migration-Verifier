using Avalonia;
using Avalonia.Animation;
using Avalonia.Animation.Easings;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Platform.Storage;
using Avalonia.Styling;
using Avalonia.Threading;
using Verifier.Core;
using Verifier.Data;

namespace Verifier.Desktop;

public sealed partial class MainWindow : Window
{
    private const string Paper="Functional Success versus Database-State, Behavioral, and Operational Contracts in AI-Generated PostgreSQL Schema Migrations";
    private readonly Grid root=new();
    private StackPanel page=new();
    private bool english,busy,example,reducedMotion;
    private int step;
    private enum MessageTone { Warning, Information, Success, Error }
    private MessageTone messageTone;
    private string storedMessage="";
    private string message { get=>storedMessage; set { storedMessage=value;messageTone=MessageTone.Warning; } }
    private void ShowMessage(string value,MessageTone tone) { message=value;messageTone=tone; }
    private IReadDatabase? source,target;
    private Inventory? sourceInfo,targetInfo;
    private RunReport? report;
    private CancellationTokenSource? cancellation;
    private DbKind kind=DbKind.PostgreSql;
    private readonly HashSet<Category> selected=[Category.Data,Category.Schema,Category.Access,Category.Objects];
    private readonly HashSet<string> chosenTables=new(StringComparer.Ordinal);
    private readonly List<TableMapping> tableMappings=[];
    private string[][] drafts=[["localhost","5432","","","",""],["localhost","5432","","","",""]];
    private TextBox[][]? inputs;
    private CheckBox? stopped;
    private bool writesStopped;
    private Category? detail;
    private TextBlock? progressText;
    private ProgressInfo? latestProgress;
    private readonly Dictionary<Category,TextBlock> progressStates=[];
    private bool cancelRequested;
    public MainWindow(bool replicaPreview=false)
    {
        Title="DB-Migration-Verifier"; Width=1180; Height=860; MinWidth=960; MinHeight=720;
        if(replicaPreview)
        {
            ReplicaPreview=new ReplicaWorkspace();Content=ReplicaPreview;
            ReplicaPreview.ExternalActionRequested+=async(_,action)=>await HandleReplicaAction(action);
            Closed+=(_,_)=>ReplicaPreview.Dispose();
            Opened+=(_,_)=>WebSurfaceColorSpace.Apply(this);
            return;
        }
        Content=root; Render();
        var narrow=Width<=1180;SizeChanged+=(_,_)=>{var next=Bounds.Width<=1180;if(next!=narrow){CaptureInputs();narrow=next;Render();}};
        Closing+=(_,e)=> { if(busy) { e.Cancel=true; cancellation?.Cancel(); message=T("중단 처리 중입니다. 결과를 확인한 뒤 닫아 주세요.","Stopping. Review the interrupted result before closing."); Render(); } };
        Closed+=async(_,_)=>await CloseConnections();
    }
    public ReplicaWorkspace? ReplicaPreview {get;}
    private string T(string kr,string en)=>english?en:kr;
    private static readonly IBrush Muted=Brush.Parse("#b5bac1"),Accent=Brush.Parse("#b4bdff"),Warn=Brush.Parse("#f0c47c"),Bad=Brush.Parse("#ff959d"),Good=Brush.Parse("#6bdda3");
    private static TextBlock Text(string value,double size=14,IBrush? color=null)
    {
        // Existing parent metrics are retained until that parent's replica pass supplies exact CSS values.
        var text=WebTypography.Create(value,size,size<=12?18:size<=14?22:size*1.3,color);
        text.VerticalAlignment=VerticalAlignment.Center;
        return text;
    }
    private static StackPanel Stack(double gap=16)=>new() { Spacing=gap };
    private static Border Line()=>new() { Height=1,Background=Brush.Parse("#41434a"),Margin=new(0,8) };
    private Button Action(string label,Func<Task> execute,string style="",double? width=null,bool duringBusy=false)
    {
        var button=new Button { Content=label,IsEnabled=!busy || duringBusy };
        if(style.Length>0) button.Classes.Add(style);
        if(width!=null) button.Width=width.Value;
        AutomationProperties.SetName(button,label);
        button.Click+=async(_,_)=>
        {
            if(busy && !duringBusy) return;
            _=Pulse(button);
            try { await execute(); }
            catch(Exception e) when(e is VerificationException or IOException or InvalidDataException or System.Text.Json.JsonException or FormatException or UnauthorizedAccessException or ArgumentException or OperationCanceledException)
            { ShowMessage(e is VerificationException ve ? ReasonText(ve.Reason) : e is OperationCanceledException ? ReasonText(Reason.Cancelled) : T("입력·파일을 처리하지 못했습니다. 입력값, 파일 형식과 저장 위치를 확인하세요.","Could not process input or file. Check values, format and location."),MessageTone.Error); busy=false; Render(); }
        };
        return button;
    }
    private Task Pulse(Control control)
    {
        if(reducedMotion) return Task.CompletedTask;
        var animation=new Animation { Duration=TimeSpan.FromMilliseconds(180), Easing=new CubicEaseOut(), Children=
        {new KeyFrame { Cue=new(0),Setters={new Setter(OpacityProperty,0.62)} },new KeyFrame { Cue=new(1),Setters={new Setter(OpacityProperty,1d)} } } };
        return animation.RunAsync(control);
    }

    private async Task Go(int value) { CaptureInputs(); if(value==1 && step>1){await CloseConnections();example=false;visited.RemoveWhere(n=>n>1);} step=value; visited.Add(value); message=""; detail=null; Render(); }
    private void CaptureInputs()
    {
        if(inputs==null) return;
        for(var s=0;s<2;s++) for(var i=0;i<6;i++) drafts[s][i]=inputs[s][i].Text??"";
        writesStopped=stopped?.IsChecked==true;
    }
    private ConnectionSettings Settings(int side)
    {
        var d=drafts[side]; if(!int.TryParse(d[1],out var port)) throw new VerificationException(Reason.InvalidInput);
        return new(kind,d[0],port,d[2],d[3],d[4],d[5].Length==0?null:d[5]);
    }
    private async Task Connect()
    {
        CaptureInputs();
        if(!writesStopped) { message=T("양쪽 DB의 쓰기 정지 확인이 필요합니다.","Confirm writes are stopped on both databases."); Render(); return; }
        var a=Settings(0); var b=Settings(1); a.Validate(); b.Validate(); example=false;
        await CloseConnections(); cancellation=new(); busy=true; message=T("연결과 조회 범위를 확인하고 있습니다.","Checking connections and visibility."); Render();
        try
        {
            source=DatabaseFactory.Create(a); target=DatabaseFactory.Create(b);
            sourceInfo=await source.InspectAsync(cancellation.Token); targetInfo=await target.InspectAsync(cancellation.Token);
            if(sourceInfo.Identity==targetInfo.Identity) throw new VerificationException(Reason.SameDatabase);
            tableMappings.Clear();chosenTables.Clear();foreach(var scope in MatchedScopes())chosenTables.Add(scope.Key);
            busy=false; connected=true; await Go(2);
        }
        catch { busy=false; await CloseConnections(); throw; }
    }

    private string CategoryText(Category c)=>c switch { Category.Data=>T("데이터","Data"),Category.Schema=>T("구조·제약","Schema & constraints"),Category.Access=>T("사용자·권한","Users & access"),Category.Objects=>T("관련 객체","Related objects"),_=>T("별도 DB 동작 시험","Separate DB behavior") };




    private async Task Run()
    {
        if(source==null || target==null) throw new VerificationException(Reason.InvalidInput);
        busy=true; cancelRequested=false; latestProgress=null; step=5; message=""; cancellation?.Dispose(); cancellation=new(); Render();
        var feedback=new Progress<ProgressInfo>(p=> { if(step==5)UpdateProgress(p); });
        var categories=selected.ToHashSet();var tables=chosenTables.ToHashSet(StringComparer.Ordinal);var mappings=tableMappings.ToArray();
        try { report=await Task.Run(()=>new ComparisonEngine().RunAsync(source,target,categories,tables,example?"Synthetic-v1":kind+"-v1",example,feedback,cancellation.Token,mappings)); }
        finally { busy=false; await CloseConnections(); }
        step=6; visited.Add(6); message=""; detail=null; Render();
    }

    private void UpdateProgress(ProgressInfo update)
    {
        latestProgress=update with { Checks=update.Checks??latestProgress?.Checks };
        if(progressText!=null)progressText.Text=T("현재: ","Current: ")+StageText(update.Stage)+T(" · 읽은 행 "," · observed rows ")+update.Rows;
        if(latestProgress.Checks==null)return;
        foreach(var (category,state) in progressStates)
        {
            var checks=latestProgress.Checks.Where(c=>c.Category==category&&c.Selected).ToArray();
            state.Text=checks.Length==0?T("미시작","Not started"):string.Join(" · ",checks.GroupBy(c=>c.Execution).Select(g=>g.Key switch { Execution.Running=>T("진행 ","Running ")+g.Count(),Execution.Completed=>T("실행 완료 ","Executed ")+g.Count(),Execution.Interrupted=>T("중단 ","Interrupted ")+g.Count(),_=>T("미구현/대기 ","Pending ")+g.Count()}));
        }
    }

    private string VerdictText(CheckResult x)=>(x.Execution==Execution.Pending?T("미시작 · ","Not started · "):x.Execution==Execution.Interrupted?T("중단 · ","Interrupted · "):"")+(x.Verdict switch { Verdict.Equal=>T("일치","Equal"),Verdict.Different=>T("불일치","Different"),Verdict.Indeterminate=>T("판정 불가","Indeterminate"),_=>T("미검증","Unverified") });
    private string FindingText(string kind)=>CatalogComparison.Fields.TryGetValue(kind,out var field)?T(field.Korean+" 차이",field.English+" difference"):kind switch { "SourceOnlyRow"=>T("원본에만 관측된 행","Source-only row"),"TargetOnlyRow"=>T("대상에만 관측된 행","Target-only row"),"ColumnDefinition"=>T("컬럼 정의 차이","Column definition difference"),"TableName"=>T("테이블 이름 차이","Table name difference"),_=>T("테이블 유무 차이","Table presence difference") };
    private string ReasonText(Reason reason)=>reason switch
    {
        Reason.NotImplemented=>T("해당 상세 검사는 아직 구현되지 않았습니다. 관측한 일부 속성이 같아도 전체 일치가 아닙니다.","This detailed check is not fully implemented. Equal observed attributes do not certify the whole scope."),
        Reason.UnsupportedType=>T("미지원 타입 또는 다른 컬럼 구성으로 전체 값을 비교하지 못했습니다.","Unsupported types or different column layouts prevent complete value comparison."),
        Reason.RowFiltered=>T("행 정책·조회 권한·테이블 종류로 전체 행 관측을 확인하지 못했습니다.","Policies, read permissions or table kinds prevent confirmed full-row observation."),
        Reason.VisibilityUnknown=>T("객체 목록의 완전한 가시성을 확인하지 못했습니다.","Complete inventory visibility could not be confirmed."),
        Reason.Cancelled=>T("사용자 요청으로 중단했습니다. 전체 재검사만 가능합니다.","Cancelled by the user. Start a full new run."),
        Reason.SameDatabase=>T("원본과 대상이 동일한 DB입니다. 서로 다른 DB를 연결하세요.","Source and target are the same database. Connect different databases."),
        Reason.InvalidInput=>T("주소·포트·DB 이름·계정을 확인하세요.","Check host, port, database and username."),
        Reason.NoObjects=>T("검사할 테이블이 없습니다. DB와 선택 범위를 확인하세요. 빈 목록은 일치 결과가 아닙니다.","No tables to compare. Check the databases and selected scope. An empty inventory is not an equal result."),
        Reason.AmbiguousScope=>T("구분할 수 없는 테이블·컬럼 식별이 있습니다. 모호한 대상을 임의로 선택해 비교하지 않습니다.","Table or column identifiers are ambiguous. No arbitrary object is selected for comparison."),
        Reason.MappingIncomplete=>T("대응되지 않은 컬럼이 있습니다. 일부 컬럼만 비교해 전체 데이터 일치로 표시하지 않습니다.","Some columns are not mapped. A partial column comparison never certifies all data."),
        Reason.IdentityUnknown=>T("두 DB의 실제 식별을 확인하지 못했습니다. 접속 경로와 식별 조회 권한을 확인하세요.","Could not confirm database identities. Check connection routes and identity read permissions."),
        Reason.PermissionDenied=>T("조회 권한이 부족합니다. PostgreSQL 서버 식별에는 pg_control_system 조회 권한도 필요합니다.","Read permission is insufficient. PostgreSQL server identification also needs pg_control_system access."),
        Reason.ConnectionLost=>T("연결·인증·TLS 검증에 실패하거나 연결이 끊겼습니다. 접속 정보와 인증서를 확인하세요. 보안을 낮춰 재시도하지 않습니다.","Connection, authentication or TLS verification failed, or the connection was lost. Check credentials and certificates. No security downgrade."),
        Reason.RowTooLarge=>T("첫 실행판의 1 MiB 행 관측 한계를 초과했습니다. 대형 셀 스트림 구현 전에는 판정하지 않습니다.","The first implementation's 1 MiB row observation limit was exceeded. Large-cell streaming is pending."),
        Reason.PlatformNotVerified=>T("이 OS 또는 DB 버전의 해당 검사 경로는 아직 검증되지 않았습니다.","This check path has not been verified for this OS or database version."),
        Reason.TemporaryIntegrity=>T("임시 자료의 무결성·완전성 확인에 실패했습니다. 일치로 판정하지 않습니다.","Temporary integrity/completion validation failed. No equal verdict."),
        Reason.Timeout=>T("시간 제한으로 중단했습니다.","Stopped after a timeout."),
        _=>T("관측 또는 저장을 완료하지 못했습니다. 전체 재검사가 필요합니다.","Observation or storage could not complete. A full new run is required.")
    };

    private async Task SaveReport()
    {
        if(report==null || selectedFormats.Count==0) return;
        var snapshot=report;var formats=selectedFormats.Order().ToArray();var completed=new List<string>();
        busy=true;ShowMessage(T("형식별 저장 위치를 선택하세요.","Choose a destination for each format."),MessageTone.Information);Render();
        try
        {
            foreach(var format in formats)
            {
                var extension=format.ToString().ToLowerInvariant();var name=extension.ToUpperInvariant();
                var file=await StorageProvider.SaveFilePickerAsync(new() { Title=T("검사 결과 저장 · ","Save result · ")+name,SuggestedFileName="dmv-"+snapshot.RunId.ToString("N")+"."+extension,DefaultExtension=extension,FileTypeChoices=[new("DMV "+name){Patterns=["*."+extension]}] });
                if(file==null){message=T("저장을 취소했습니다. 미저장 형식: ","Save cancelled. Not saved: ")+string.Join(" · ",formats.Where(f=>!completed.Contains(f.ToString().ToUpperInvariant())).Select(f=>f.ToString().ToUpperInvariant()))+Saved();return;}
                ShowMessage(name+T(" 파일을 쓰고 검증하고 있습니다."," file is being written and validated."),MessageTone.Information);Render();
                await ReportExport.SaveAsync(file.TryGetLocalPath() ?? throw new InvalidDataException(),snapshot,format,english);
                completed.Add(name);
            }
            ShowMessage(T("선택 형식 저장·다시 읽기 검증 완료: ","Selected formats saved and read-back validated: ")+string.Join(" · ",completed),MessageTone.Success);
        }
        catch(Exception e) when(e is IOException or InvalidDataException or UnauthorizedAccessException or ArgumentException or System.Text.Json.JsonException)
        { ShowMessage(T("파일 저장에 실패했습니다. 저장 위치·쓰기 권한·남은 공간을 확인하고 다시 저장하세요. 검사 결과는 화면에 유지됩니다.","Could not save the file. Check destination, write permission and free space, then retry. The result remains on screen.")+Saved(),MessageTone.Error); }
        finally { busy=false; Render(); }
        string Saved()=>completed.Count==0?"":T(" 이미 저장·검증된 형식: "," Already saved and validated: ")+string.Join(" · ",completed);
    }
    private async Task OpenReport()
    {
        CaptureInputs();busy=true;ShowMessage(T("다시 열 JSON 결과 파일을 선택하세요.","Choose a JSON result to reopen."),MessageTone.Information);Render();
        try
        {
            var files=await StorageProvider.OpenFilePickerAsync(new() { Title=T("검사 결과 파일 열기","Open result file"),AllowMultiple=false,FileTypeFilter=[new("DMV JSON"){Patterns=["*.json"]}] });
            if(files.Count==0) { message=T("파일 열기를 취소했습니다.","Open cancelled."); return; }
            ShowMessage(T("파일의 무결성과 검사 상태를 확인하고 있습니다.","Checking file integrity and verification states."),MessageTone.Information);Render();
            var loaded=await ReportFile.LoadAsync(files[0].TryGetLocalPath() ?? throw new InvalidDataException());
            await Reset();report=loaded;example=report.Example;step=6;visited.Add(6);
        }
        catch(Exception e) when(e is IOException or InvalidDataException or UnauthorizedAccessException or ArgumentException or System.Text.Json.JsonException or FormatException)
        { ShowMessage(T("결과 파일을 열지 못했습니다. 앱이 저장한 JSON인지, 손상되지 않았는지, 읽기 권한이 있는지 확인하세요. 기존 화면의 결과·입력은 유지됩니다.","Could not open the result. Check that it is an intact JSON saved by this app and that you have read permission. Existing results and inputs are kept."),MessageTone.Error); }
        finally { busy=false;Render(); }
    }
    private async Task Reset()
    { await CloseConnections(); report=null; sourceInfo=targetInfo=null; chosenTables.Clear();tableMappings.Clear(); example=false; detail=null; message=""; connected=false; visited.Clear(); expanded.Clear(); tableQuery=""; resultFilter="all"; }
    private async Task CloseConnections()
    { var a=source; var b=target; source=target=null; connected=false; try { if(a!=null) await a.DisposeAsync(); } finally { if(b!=null) await b.DisposeAsync(); } }
}
