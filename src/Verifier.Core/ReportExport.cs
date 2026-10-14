using System.Globalization;
using System.Net;
using System.Text;

namespace Verifier.Core;

public enum ReportFormat { Json, Csv, Html }

public static class ReportExport
{
    public static async Task SaveAsync(string path,RunReport report,ReportFormat format,bool english=false)
    {
        if(!Enum.IsDefined(format)) throw new ArgumentOutOfRangeException(nameof(format));
        if(format==ReportFormat.Json){await ReportFile.SaveAsync(path,report);return;}
        using var lease=report.AcquireStorage();
        report=ReportFile.Snapshot(report);
        ReportFile.Validate(report);
        await AtomicFile.WriteAsync(path,async stream=>
        {
            await using var writer=new StreamWriter(stream,new UTF8Encoding(format==ReportFormat.Csv),leaveOpen:true);
            if(format==ReportFormat.Csv) await Csv(writer,report);
            else await Html(writer,report,english);
        });
    }

    private static async Task Csv(TextWriter writer,RunReport report)
    {
        await Row("RecordType","Version","RunId","Started","Finished","Example","Adapter","HasDifferences","HasUnknown","AllEqual","Table","Category","CheckId","ResearchId","Selected","Execution","Verdict","Reason","FindingId","Column","Kind","Count","TargetTable","SourceColumn","TargetColumn");
        await Row("run",report.Version.ToString(CultureInfo.InvariantCulture),report.RunId.ToString(),report.Started.ToString("O"),report.Finished.ToString("O"),B(report.Example),report.Adapter,B(report.HasDifferences),B(report.HasUnknown),B(report.AllEqual));
        if(report.DatabaseScope)await Row("database-scope");
        if(report.DatabaseIsNamespace)await Row("database-namespace");
        foreach(var table in report.Tables) await Row("scope",null,null,null,null,null,null,null,null,null,table);
        foreach(var mapping in report.Mappings)
        {
            await MappingRow("table-map",mapping,null);
            foreach(var column in mapping.Columns)await MappingRow("column-map",mapping,column);
        }
        foreach(var check in report.Checks)
        {
            var metadata=new string?[]{check.Category.ToString(),check.CheckId,check.ResearchId,B(check.Selected),check.Execution.ToString(),check.Verdict.ToString(),check.Reason.ToString()};
            await Row(["check",null,null,null,null,null,null,null,null,null,null,..metadata]);
            foreach(var issue in check.Issues)
                await Row("issue",null,null,null,null,null,null,null,null,null,issue.Table,check.Category.ToString(),check.CheckId,check.ResearchId,B(check.Selected),check.Execution.ToString(),check.Verdict.ToString(),issue.Reason.ToString(),null,issue.Column,issue.Attribute);
            foreach(var limit in check.PairingLimits)
                await Row("pairing-limit",null,null,null,null,null,null,null,null,null,limit.Table,check.Category.ToString(),check.CheckId,check.ResearchId,B(check.Selected),check.Execution.ToString(),check.Verdict.ToString(),limit.Reason.ToString(),null,limit.Column);
            foreach(var finding in check.Findings)
                await Row(["finding",null,null,null,null,null,null,null,null,null,finding.Table,..metadata,finding.Id,finding.Column,finding.Kind,finding.Count.ToString(CultureInfo.InvariantCulture)]);
        }
        foreach(var request in report.BehaviorRequests)await Row("trial-request",null,null,null,null,null,null,null,null,null,request.Table,"Behavior",request.RoleOperation==null?"BEHAVIOR-01":"BEHAVIOR-02",null,null,null,null,null,request.Id.ToString(),null,request.RoleOperation?.ToString()??"CRUD");
        foreach(var trial in report.BehaviorResults)
        {
            foreach(var step in trial.Steps)await Row("trial-step",null,null,null,null,null,null,null,null,null,trial.Table,"Behavior",trial.RoleOperation==null?"BEHAVIOR-01":"BEHAVIOR-02",null,null,step.Stage.ToString(),step.Outcome.ToString(),step.Reason.ToString(),trial.Id.ToString(),null,step.Expected.ToString());
            await Row("trial-cleanup",null,null,null,null,null,null,null,null,null,trial.Table,"Behavior",null,null,null,null,trial.Cleanup.ToString(),null,trial.Id.ToString());
            await Row("trial-environment",null,null,null,null,null,null,null,null,null,trial.Table,"Behavior",null,null,null,null,trial.TargetStructure.ToString(),null,trial.Id.ToString());
            foreach(var parent in trial.DependencyTables)await Row("trial-dependency",null,null,null,null,null,null,null,null,null,parent,"Behavior",null,null,null,null,null,null,trial.Id.ToString());
            foreach(var sequence in trial.SequenceObservations)await Row("trial-sequence",null,null,null,null,null,null,null,null,null,sequence.Table,"Behavior",null,null,null,null,null,null,trial.Id.ToString(),null,sequence.Effect.ToString());
            foreach(var effect in trial.Effects)await Row("trial-effect",null,null,null,null,null,null,null,null,null,trial.Table,"Behavior",null,null,null,null,null,null,trial.Id.ToString(),null,effect.ToString());
        }
        await Row("note",null,null,null,null,null,null,null,null,null,"All checks, excluded scope, issues and findings are included. Formula-like identifiers use a protective apostrophe; JSON preserves the original identifier. Not a database data export.");
        async Task Row(params string?[] values)
        {
            var fields=new string[25];
            for(var i=0;i<fields.Length;i++) fields[i]=CsvField(i<values.Length?values[i]:null);
            await writer.WriteLineAsync(string.Join(',',fields));
        }
        Task MappingRow(string kind,TableMapping mapping,ColumnMapping? column)
        { var fields=new string?[25];fields[0]=kind;fields[10]=mapping.Source;fields[22]=mapping.Target;fields[23]=column?.Source;fields[24]=column?.Target;return Row(fields); }
    }
    private static string B(bool value)=>value?"true":"false";
    private static string CsvField(string? value)
    {
        value??="";
        // Quote every cell and guard formula markers even after leading whitespace.
        // CSV is for reading; the JSON result remains the lossless interchange file.
        var first=value.AsSpan().TrimStart();
        if(first.Length>0 && "=+-@＝＋－＠".Contains(first[0]) || value.Length>0&&value[0] is '\t' or '\r' or '\n') value="'"+value;
        return "\""+value.Replace("\"","\"\"")+"\"";
    }

    private static async Task Html(TextWriter writer,RunReport report,bool english)
    {
        string L(string kr,string en)=>english?en:kr;
        string E(string? value)=>WebUtility.HtmlEncode(value??"—");
        string Location(string? table)=>E(table??(report.DatabaseScope?L("DB 범위","Database scope"):"—"));
        string CategoryName(Category value)=>value switch { Category.Data=>L("데이터","Data"),Category.Schema=>L("구조·제약","Schema & constraints"),Category.Access=>L("사용자·권한","Users & access"),Category.Objects=>L("관련 객체","Related objects"),_=>L("별도 DB 동작 시험","Separate DB behavior") };
        string ExecutionName(Execution value)=>value switch { Execution.Completed=>L("실행 완료","Completed"),Execution.Interrupted=>L("중단","Interrupted"),Execution.Running=>L("진행 중","Running"),_=>L("미시작","Not started") };
        string VerdictName(Verdict value)=>value switch { Verdict.Equal=>L("일치","Equal"),Verdict.Different=>L("불일치","Different"),Verdict.Indeterminate=>L("판정 불가","Indeterminate"),_=>L("미검증","Unverified") };
        await writer.WriteLineAsync("<!doctype html><html lang=\""+(english?"en":"ko")+"\"><meta charset=\"utf-8\"><meta name=\"viewport\" content=\"width=device-width,initial-scale=1\"><meta http-equiv=\"Content-Security-Policy\" content=\"default-src 'none'; style-src 'unsafe-inline'; base-uri 'none'; form-action 'none'\"><title>DB-Migration-Verifier · "+report.RunId+"</title>");
        await writer.WriteLineAsync("<style>body{margin:0;background:#313338;color:#dbdee1;font:14px/1.6 system-ui,sans-serif}main{max-width:1100px;margin:auto;padding:40px}h1{color:#f2f3f5;font-size:30px;margin:0}h2{font-size:18px;color:#f2f3f5}small,.muted{color:#b5bac1}header,section{margin-bottom:24px}section{border:1px solid #41434a;border-radius:10px;padding:24px;background:#2b2d31}.metrics{display:flex;gap:40px;margin:24px 0}.number{font-size:32px;font-weight:600;font-variant-numeric:tabular-nums}.bad{color:#ff959d}.warn{color:#f0c47c}.good{color:#6bdda3}table{border-collapse:collapse;width:100%;table-layout:fixed}th,td{text-align:left;vertical-align:top;padding:12px;border-bottom:1px solid #41434a;overflow-wrap:anywhere;white-space:pre-wrap}th{color:#b5bac1;font-weight:500}code{overflow-wrap:anywhere;white-space:pre-wrap}ul{padding-left:20px}.scope{overflow-wrap:anywhere;white-space:pre-wrap}@media print{body{background:white;color:#111}main{padding:0}section{background:white;break-inside:avoid}.muted,small,th{color:#333}}</style><main>");
        await writer.WriteLineAsync("<header><small>DB-Migration-Verifier</small><h1>"+L("마이그레이션 비교 결과","Migration comparison report")+"</h1><p>"+L("선택한 범위의 차이와 확인하지 못한 영역을 함께 보관합니다.","Differences and unverified areas of the selected scope are preserved together.")+"</p><p class=\""+(report.Example?"warn":"muted")+"\">"+L(report.Example?"합성 예시 결과 · 실제 사용자 DB 검사가 아닙니다.":"사용자 DB 비교 결과",report.Example?"Synthetic example · not an actual user database verification.":"User database comparison result")+"</p></header>");
        await writer.WriteLineAsync("<section><h2>"+L("검사 요약","Run summary")+"</h2><p>Run ID: <code>"+report.RunId+"</code><br>"+L("시작 / 종료","Started / finished")+": "+E(report.Started.ToString("O"))+" / "+E(report.Finished.ToString("O"))+"<br>Adapter: "+E(report.Adapter)+" · Format: "+report.Version+"</p>");
        var checks=report.Checks.Where(x=>x.Selected).ToArray();
        await writer.WriteLineAsync("<div class=\"metrics\"><div class=\"bad\">"+L("불일치·위반 검사","Different / violated checks")+"<div class=\"number\">"+checks.Count(x=>x.Verdict==Verdict.Different)+"</div></div><div class=\"warn\">"+L("확인 불가·미검증 검사","Unknown / unverified")+"<div class=\"number\">"+checks.Count(Unknown)+"</div></div><div class=\"good\">"+L("일치 검사","Equal checks")+"<div class=\"number\">"+checks.Count(x=>x.Verdict==Verdict.Equal)+"</div></div></div>");
        await writer.WriteLineAsync("<p>"+L("차이와 확인 불가가 같은 검사에 공존할 수 있습니다. 수치는 검사 ID 개수이며 전체 DB의 성공률이 아닙니다.","Differences and unknown scope may coexist in a check. Numbers count check IDs, not a whole-database success rate.")+"</p><p>HasDifferences: "+B(report.HasDifferences)+" · HasUnknown: "+B(report.HasUnknown)+" · AllEqual: "+B(report.AllEqual)+" · HasIntegrityViolations: "+B(report.HasIntegrityViolations)+"</p><h2>"+L("선택 테이블","Selected tables")+"</h2><ul class=\"scope\">");
        if(report.DatabaseScope)await writer.WriteLineAsync("<li>"+L("DB 범위 · 빈 스키마·독립 객체·DB 권한","Database scope · empty schemas, standalone objects and database access")+"</li>");
        foreach(var table in report.Tables) await writer.WriteLineAsync("<li>"+E(table)+"</li>");
        await writer.WriteLineAsync("</ul>"+(report.Tables.Count==0&&!report.DatabaseScope?"<p class=\"warn\">"+L("확정된 검사 대상이 없습니다. 일치 결과가 아닙니다.","No confirmed scope. This is not an equal result.")+"</p>":"")+"</section>");
        if(report.Version>=2)
        {
            await writer.WriteLineAsync("<section><h2>"+L("실행한 테이블·컬럼 대응","Executed table and column mapping")+"</h2><p>"+L("대응은 값 변환 승인이 아닙니다. 이름·타입·순서의 차이는 구조 검사에 남습니다.","Mapping does not authorize value transformations. Name, type and order differences remain in schema checks.")+"</p>");
            if(report.DatabaseIsNamespace)await writer.WriteLineAsync("<p>"+L("선택한 MySQL DB 이름은 마이그레이션의 양쪽 공간입니다. DB 이름만 다른 경우 테이블 이름 변경으로 판정하지 않으며, 아래 실제 DB·테이블 위치는 그대로 보존합니다.","Selected MySQL database names identify the two migration namespaces. A database-name difference alone is not a table rename; actual database-qualified locations are preserved below.")+"</p>");
            foreach(var mapping in report.Mappings)
            {
                await writer.WriteLineAsync("<h2>"+E(mapping.Source)+" → "+E(mapping.Target)+"</h2><table><tr><th>"+L("원본 컬럼","Source column")+"</th><th>"+L("대상 컬럼","Target column")+"</th></tr>");
                foreach(var column in mapping.Columns)await writer.WriteLineAsync("<tr><td>"+E(column.Source)+"</td><td>"+E(column.Target)+"</td></tr>");
                await writer.WriteLineAsync("</table>");
            }
            await writer.WriteLineAsync("</section>");
        }
        foreach(var check in report.Checks)
        {
            await writer.WriteLineAsync("<section><h2>"+E(check.CheckId)+" · "+E(check.ResearchId)+"</h2><p>"+CategoryName(check.Category)+" · "+L(check.Selected?"선택됨":"선택 제외",check.Selected?"Selected":"Excluded")+" · "+ExecutionName(check.Execution)+" · <strong class=\""+(check.Verdict==Verdict.Different?"bad":check.Verdict==Verdict.Equal?"good":"warn")+"\">"+VerdictName(check.Verdict)+"</strong> · "+E(ReasonName(check.Reason,english))+"</p><small>"+E(check.Category.ToString())+" / "+E(check.Execution.ToString())+" / "+E(check.Verdict.ToString())+" / "+E(check.Reason.ToString())+"</small>");
            if(check.Issues.Count>0)
            {
                await writer.WriteLineAsync("<h2>"+L("확인하지 못한 범위","Unverified scope")+"</h2><table><tr><th>"+L("테이블","Table")+"</th><th>"+L("컬럼","Column")+"</th><th>"+L("이유","Reason")+"</th></tr>");
                foreach(var issue in check.Issues) await writer.WriteLineAsync("<tr><td>"+Location(issue.Table)+"</td><td>"+E(issue.Column)+"</td><td>"+(issue.Attribute!=null&&CatalogComparison.Fields.TryGetValue(issue.Attribute,out var field)?E(english?field.English:field.Korean)+"<br>":"")+E(ReasonName(issue.Reason,english))+"<br><small>"+E(issue.Reason.ToString())+(issue.Attribute==null?"":" · "+E(issue.Attribute))+"</small></td></tr>");
                await writer.WriteLineAsync("</table>");
            }
            if(check.Findings.Count>0)
            {
                await writer.WriteLineAsync("<h2>"+L("차이의 위치와 종류","Locations and kinds of differences")+"</h2><table><tr><th>"+L("테이블 / 컬럼","Table / column")+"</th><th>"+L("종류 / 건수","Kind / count")+"</th><th>"+L("앱 내부 번호","Internal ID")+"</th></tr>");
                foreach(var finding in check.Findings)
                {
                    var kind=CatalogComparison.Fields.TryGetValue(finding.Kind,out var field)?(english?field.English:field.Korean)+" ("+finding.Kind+")":finding.Kind;
                    await writer.WriteLineAsync("<tr><td>"+Location(finding.Table)+"<br>"+E(finding.Column)+"</td><td>"+E(kind)+" · "+finding.Count.ToString(CultureInfo.InvariantCulture)+"</td><td><code>"+E(finding.Id)+"</code></td></tr>");
                }
                await writer.WriteLineAsync("</table>");
            }
            if(check.PairingLimits.Count>0)
            {
                await writer.WriteLineAsync("<h2>"+L("행 대응 제한 · 값 비교 판정과 별도","Row pairing limits · separate from the value verdict")+"</h2><ul>");
                foreach(var limit in check.PairingLimits)await writer.WriteLineAsync("<li>"+E(limit.Table)+" · "+E(ReasonName(limit.Reason,english))+"</li>");
                await writer.WriteLineAsync("</ul>");
            }
            await writer.WriteLineAsync("</section>");
        }
        if(report.BehaviorRequests.Count>0)
        {
            await writer.WriteLineAsync("<section><h2>"+L("별도 시험 DB · 실행 범위와 정리","Separate test DB · requested scope and cleanup")+"</h2><p>"+L("원본·대상 운영 동작을 보장하지 않습니다. 기대 결과, 자료 정리, 연결된 시퀀스 관측을 구분합니다. 트리거·외부 부작용의 완전한 복구는 확인하지 않았습니다.","This does not guarantee source/target operations. Expectations, cleanup and linked-sequence observations are separate. Full restoration of trigger/external effects was not verified.")+"</p>");
            foreach(var request in report.BehaviorRequests)
            {
                var trial=report.BehaviorResults.SingleOrDefault(t=>t.Id==request.Id);
                await writer.WriteLineAsync("<h2>"+E(request.Table)+" · "+E(request.RoleOperation?.ToString()??"CRUD")+"</h2><p>"+request.Id+"</p>");
                if(trial==null){await writer.WriteLineAsync("<p class=\"warn\">"+L("실행 결과 없음 · 미검증","No execution result · unverified")+"</p>");continue;}
                await writer.WriteLineAsync("<table><tr><th>"+L("단계","Stage")+"</th><th>"+L("기대","Expectation")+"</th><th>"+L("결과 / 이유","Outcome / reason")+"</th></tr>");
                foreach(var step in trial.Steps)await writer.WriteLineAsync("<tr><td>"+E(step.Stage.ToString())+"</td><td>"+E(step.Expected.ToString())+"</td><td>"+E(step.Outcome.ToString())+" / "+E(ReasonName(step.Reason,english))+"</td></tr>");
                await writer.WriteLineAsync("</table><p>"+L("정리","Cleanup")+": "+E(trial.Cleanup.ToString())+" · "+L("대상과 시험 환경의 구조 비교","Target vs trial structure")+": "+E(trial.TargetStructure.ToString())+"</p><ul>");
                foreach(var parent in trial.DependencyTables)await writer.WriteLineAsync("<li>"+L("확인한 부모 자료 범위","Confirmed dependency scope")+": "+E(parent)+"</li>");
                foreach(var observation in trial.SequenceObservations)await writer.WriteLineAsync("<li>"+E(observation.Table)+" · "+E(observation.Effect.ToString())+"</li>");
                foreach(var effect in trial.Effects)await writer.WriteLineAsync("<li>"+E(effect.ToString())+"</li>");
                await writer.WriteLineAsync("</ul>");
            }
            await writer.WriteLineAsync("</section>");
        }
        await writer.WriteLineAsync("<footer><p>"+L("원본값·행 키·접속 정보·계정 이름·원 SQL 정의는 포함하지 않습니다. 다시 열기에는 JSON 파일을 사용하세요.","Values, row keys, connection details, principal names and SQL definitions are excluded. Use JSON to reopen in the app.")+"</p><small>Functional Success versus Database-State, Behavioral, and Operational Contracts in AI-Generated PostgreSQL Schema Migrations</small></footer></main></html>");
    }
    private static bool Unknown(CheckResult check)=>check.Execution!=Execution.Completed || check.Verdict is Verdict.Unverified or Verdict.Indeterminate || check.Reason!=Reason.None || check.Issues.Count>0;
    private static string ReasonName(Reason reason,bool english)
    {
        // Localized reading labels; stable codes are shown alongside them.
        var labels=reason switch
        {
            Reason.None=>("추가 미확인 사유 없음","No additional unknown reason"),
            Reason.NotImplemented=>("상세 검사 미구현","Detailed check not implemented"),
            Reason.UnsafeObservation=>("안전한 관측 방법 미확인","Safe observation method unverified"),
            Reason.UnsupportedType=>("미지원 타입·컬럼 구성","Unsupported type or column layout"),
            Reason.MissingDependency=>("참조된 객체 정의 누락","Referenced object definition missing"),
            Reason.VisibilityUnknown=>("목록 가시성 미확인","Inventory visibility not confirmed"),
            Reason.PermissionDenied=>("조회 권한 부족","Insufficient read permission"),
            Reason.Unpopulated=>("구체화된 뷰 미적재 · 행 조회 불가","Unpopulated materialized view · stored rows unreadable"),
            Reason.RowFiltered=>("전체 행 관측 미확인","Full-row observation not confirmed"),
            Reason.Cancelled=>("사용자 중단","Cancelled by the user"),
            Reason.Timeout=>("시간 제한으로 중단","Timed out"),
            Reason.ConnectionLost=>("연결·인증·TLS 실패","Connection, authentication or TLS failure"),
            Reason.TemporaryIntegrity=>("임시 자료 무결성·완전성 실패","Temporary integrity/completion failure"),
            Reason.TemporaryStorage=>("임시 저장·정리 실패","Temporary storage or cleanup failure"),
            Reason.RowTooLarge=>("현재 행 크기 관측 한계 초과","Current row observation limit exceeded"),
            Reason.IdentityUnknown=>("DB 식별 미확인","Database identity not confirmed"),
            Reason.SameDatabase=>("원본과 대상이 같은 DB","Source and target are the same DB"),
            Reason.InvalidInput=>("접속 입력·선택 범위 오류","Invalid connection input or selection"),
            Reason.ObservationChanged=>("관측을 완료하지 못함","Observation could not complete"),
            Reason.PlatformNotVerified=>("OS·DB 버전 경로 미검증","OS or database version path not verified"),
            Reason.NoObjects=>("검사할 테이블 없음","No tables to compare"),
            Reason.AmbiguousScope=>("테이블·컬럼 식별이 모호함","Ambiguous table or column identifiers"),
            Reason.MappingIncomplete=>("대응되지 않은 컬럼 있음","Some columns are not mapped"),
            Reason.AmbiguousRows=>("키 중복·NULL로 행 대응 불가 · 전체 값 다중집합 비교 사용","Duplicate or NULL keys prevent row pairing; full-value multiset comparison used"),
            _=>("미확인","Unknown")
        };
        return english?labels.Item2:labels.Item1;
    }
}
