using System.Text;
using System.Text.Json;
using Verifier.Desktop;
internal static class PageIntegrationExportProbe
{
    internal static void Check(string output,string oracle)
    {
        using var document=JsonDocument.Parse(File.ReadAllText(oracle));var checks=new List<object>();
        void Assert(string id,bool passed,object state){checks.Add(new{id,passed,state});File.WriteAllText(Path.Combine(output,"pageintegration-export-checks.json"),JsonSerializer.Serialize(checks,new JsonSerializerOptions{WriteIndented=true}));if(!passed)throw new InvalidOperationException(id);}
        static string Canonical(JsonElement element)=>element.ValueKind switch{JsonValueKind.Object=>"{"+string.Join(",",element.EnumerateObject().OrderBy(p=>p.Name).Select(p=>JsonSerializer.Serialize(p.Name)+":"+Canonical(p.Value)))+"}",JsonValueKind.Array=>"["+string.Join(",",element.EnumerateArray().Select(Canonical))+"]",_=>element.ValueKind==JsonValueKind.String?JsonSerializer.Serialize(element.GetString()):element.GetRawText()};
        foreach(var c in document.RootElement.GetProperty("rows").EnumerateArray())
        {
            var report=c.GetProperty("report");var run=ReplicaSampleRun.Read(Encoding.UTF8.GetBytes(report.GetRawText()));var findings=report.GetProperty("findings").EnumerateArray().Select(x=>(object)x.Clone()).ToArray();var english=c.GetProperty("english").GetBoolean();
            foreach(var expected in c.GetProperty("exports").EnumerateArray())
            {
                var name=expected.GetProperty("name").GetString()!;var format=Path.GetExtension(name)[1..];var body=Encoding.UTF8.GetString(ReplicaSampleExport.Create(run,format,english,findings));
                File.WriteAllText(Path.Combine(output,c.GetProperty("id").GetString()+"."+format),body,new UTF8Encoding(false));
                bool same;
                if(format=="json"){using var actual=JsonDocument.Parse(body);using var original=JsonDocument.Parse(expected.GetProperty("body").GetString()!);same=Canonical(actual.RootElement)==Canonical(original.RootElement);}
                else same=body==expected.GetProperty("body").GetString();
                Assert(c.GetProperty("id").GetString()+"-"+format,same,new{format,bytes=Encoding.UTF8.GetByteCount(body),expectedBytes=Encoding.UTF8.GetByteCount(expected.GetProperty("body").GetString()!)});
            }
        }
        var runForEscaping=ReplicaSampleRun.Create(["objects"],[],ReplicaSampleRun.AllowedTables.ToDictionary(x=>x,x=>x)).CompleteAll();
        var cells=document.RootElement.GetProperty("cells").EnumerateArray().Select(x=>x.GetString()!).ToArray();var csv=document.RootElement.GetProperty("csv").EnumerateArray().Select(x=>x.GetString()!).ToArray();var escaped=document.RootElement.GetProperty("escaped").EnumerateArray().Select(x=>x.GetString()!).ToArray();
        for(var i=0;i<cells.Length;i++)
        {
            object[] synthetic=[new{id="SYNTHETIC",category="objects",kind="unknown",table="",column="",reason=cells[i]}];
            var candidateCsv=Encoding.UTF8.GetString(ReplicaSampleExport.Create(runForEscaping,"csv",false,synthetic));var candidateHtml=Encoding.UTF8.GetString(ReplicaSampleExport.Create(runForEscaping,"html",false,synthetic));
            Assert("escaping-"+i,candidateCsv.EndsWith(","+csv[i],StringComparison.Ordinal)&&candidateHtml.Contains("<br>"+escaped[i]+"</p>",StringComparison.Ordinal),new{i});
        }
    }
}
