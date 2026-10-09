using System.Text;
using System.Text.Json;

namespace Verifier.Desktop;

public static class ReplicaSampleExport
{
    public static byte[] Create(ReplicaSampleRun run,string format,bool english,object[] findings)
    {
        if(format=="json")return run.WriteJson(findings);
        if(format=="csv")
        {
            static string Cell(string? value){var s=value??"";return "\""+(s.Length>0&&"=+@-\t\r".Contains(s[0])?"'":"")+s.Replace("\"","\"\"")+"\"";}
            List<string[]> rows=[["SAMPLE ONLY","run","category","execution","equal","different","unknown"]];
            rows.AddRange(run.Groups.Select(g=>new[]{"sample",run.Id,g.Key,g.Status,g.Equal.ToString(System.Globalization.CultureInfo.InvariantCulture),g.Different.ToString(System.Globalization.CultureInfo.InvariantCulture),(g.Total-g.Equal-g.Different).ToString(System.Globalization.CultureInfo.InvariantCulture)}));
            rows.Add([]);rows.Add(["finding","category","kind","table","column","reason"]);
            foreach(var f in JsonSerializer.SerializeToElement(findings).EnumerateArray())rows.Add(new[]{"id","category","kind","table","column","reason"}.Select(k=>f.TryGetProperty(k,out var v)?v.GetString()??"":"").ToArray());
            return Encoding.UTF8.GetBytes("\uFEFF"+string.Join("\r\n",rows.Select(r=>string.Join(',',r.Select(Cell)))));
        }
        if(format=="html")
        {
            static string E(string? s)=>(s??"").Replace("&","&amp;").Replace("<","&lt;").Replace(">","&gt;").Replace("\"","&quot;").Replace("'","&#39;");
            string T(string ko,string en)=>english?en:ko;
            string Name(string key)=>key switch{"data"=>T("데이터","Data"),"schema"=>T("구조·제약","Schema & constraints"),"access"=>T("사용자·권한","Users & permissions"),"objects"=>T("관련 객체","Related objects"),"trial"=>T("별도 DB 동작 시험","Behavior tests in a separate DB"),_=>key};
            var rows=string.Concat(run.Groups.Select(g=>$"<tr><td>{E(Name(g.Key))}</td><td>{E(g.Status)}</td><td>{g.Equal} / {g.Different} / {g.Total-g.Equal-g.Different}</td></tr>"));
            var entries=string.Concat(JsonSerializer.SerializeToElement(findings).EnumerateArray().Select(f=>{
                string V(string key)=>f.TryGetProperty(key,out var value)?E(value.GetString()):"";
                return $"<p><strong>{V("id")}</strong> · {V("table")} · {V("column")}<br>{V("reason")}</p>";
            }));
            return Encoding.UTF8.GetBytes($"<!doctype html><html lang=\"{(english?"en":"ko")}\"><meta charset=\"utf-8\"><title>Sample verification report</title><style>body{{font:14px/1.6 system-ui;max-width:960px;margin:40px auto;padding:20px;color:#14213d}}table{{border-collapse:collapse;width:100%}}td,th{{border:1px solid #dce4f0;padding:12px;text-align:left}}</style><h1>{T("예시 검사 보고서","Sample verification report")}</h1><p>{T("화면 시안의 예시입니다. 실제 DB 검사 결과가 아닙니다.","Design preview sample. Not a real DB verification result.")}</p><p>{E(run.Id)} · {(run.Interrupted?T("중단","Interrupted"):T("완료","Completed"))}</p><table><tr><th>{T("분류","Category")}</th><th>{T("실행","Execution")}</th><th>{T("일치 / 불일치 / 확인 불가","Equal / different / unknown")}</th></tr>{rows}</table><h2>{T("차이와 미검증 이유","Differences and gaps")}</h2>{entries}</html>");
        }
        throw new InvalidOperationException("Unsupported sample format");
    }
}
