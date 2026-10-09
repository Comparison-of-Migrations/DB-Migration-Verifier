using System.Text.Json;
using Verifier.Desktop;

internal static class NumberRulesProbe
{
    public static void Run(string sourceDirectory,string output)
    {
        var rows=new List<object>();var failures=new List<object>();
        void Check(string id,string raw,JsonElement expected)
        {
            var state=WebNumberRules.Read(raw);
            var actual=new Dictionary<string,object?>{{"value",state.Value},{"number",state.Number},{"bad",state.BadInput},{"under",state.RangeUnderflow},{"over",state.RangeOverflow},{"step",state.StepMismatch},{"valid",state.Valid}};
            actual["badInput"]=state.BadInput;actual["rangeUnderflow"]=state.RangeUnderflow;actual["rangeOverflow"]=state.RangeOverflow;actual["stepMismatch"]=state.StepMismatch;
            var differences=expected.EnumerateObject().Where(p=>actual.ContainsKey(p.Name)&&!Equivalent(p.Value,actual[p.Name])).Select(p=>p.Name).ToArray();
            var row=new{id,raw,expected=expected.Clone(),actual,differences};rows.Add(row);if(differences.Length>0)failures.Add(row);
        }
        foreach(var name in new[]{"numberinput-mac-step-source.json","numberinput-mac-precision-step-source.json","numberinput-mac-leadingzero-step-source.json"}.Where(n=>File.Exists(Path.Combine(sourceDirectory,n))))
        using(var steps=JsonDocument.Parse(File.ReadAllText(Path.Combine(sourceDirectory,name))))
        foreach(var (e,i) in steps.RootElement.EnumerateArray().Select((e,i)=>(e,i)))
        {
            var raw=WebNumberRules.SanitizeAssignedValue(e.GetProperty("value").GetString()!);
            Check($"{name}-{i}-before",raw,e.GetProperty("before"));
            Check($"{name}-{i}-after",WebNumberRules.Step(raw,e.GetProperty("key").GetString()=="ArrowUp"?1:-1),e.GetProperty("after"));
        }
        foreach(var file in new[]{"typing","lexical"})
        {
            using var cases=JsonDocument.Parse(File.ReadAllText(Path.Combine(sourceDirectory,$"numberinput-mac-{file}-source.json")));
            foreach(var (e,i) in cases.RootElement.EnumerateArray().Select((e,i)=>(e,i)))
            {
                var raw="";
                foreach(var c in e.GetProperty("text").GetString()!)raw+=WebNumberRules.FilterInsertion(raw,raw.Length,raw.Length,c.ToString());
                Check($"{file}-{i}",raw,e.TryGetProperty("state",out var state)?state:e);
            }
        }
        foreach(var file in Directory.GetFiles(sourceDirectory,"numberinput-windows-step-*.json"))
        {
            using var source=JsonDocument.Parse(File.ReadAllText(file));
            var key=source.RootElement.GetProperty("events").EnumerateArray().Last(e=>e.GetProperty("event").GetString()=="keydown"&&e.GetProperty("key").GetString() is "ArrowUp" or "ArrowDown");
            var raw=WebNumberRules.SanitizeAssignedValue(key.GetProperty("value").GetString()!);
            Check(Path.GetFileName(file),WebNumberRules.Step(raw,key.GetProperty("key").GetString()=="ArrowUp"?1:-1),source.RootElement.GetProperty("rows")[0]);
        }
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(output))!);
        File.WriteAllText(output,JsonSerializer.Serialize(new{scope="Recorded source numeric value/validity rules; no paint or UI completion claim",checks=rows.Count,failed=failures.Count,rows},new JsonSerializerOptions{WriteIndented=true}));
        Console.WriteLine($"Number rules: {rows.Count-failures.Count}/{rows.Count} source states agree.");
        if(failures.Count>0)Environment.ExitCode=1;
    }
    private static bool Equivalent(JsonElement expected,object? actual)=>expected.ValueKind switch
    {
        JsonValueKind.Null=>actual is null,
        JsonValueKind.String=>actual is string s&&s==expected.GetString(),
        JsonValueKind.True or JsonValueKind.False=>actual is bool b&&b==expected.GetBoolean(),
        JsonValueKind.Number=>actual is double n&&n==expected.GetDouble(),
        _=>false
    };
}
