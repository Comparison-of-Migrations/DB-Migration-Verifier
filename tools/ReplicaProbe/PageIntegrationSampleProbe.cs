using System.Text;
using System.Text.Json;
using Verifier.Desktop;
internal static class PageIntegrationSampleProbe
{
    internal static void Check(string output,string oracleFile)
    {
        using var document=JsonDocument.Parse(File.ReadAllText(oracleFile));var rows=new List<object>();
        foreach(var test in document.RootElement.EnumerateArray())
        {
            var id=test.GetProperty("id").GetString()!;var expected=test.GetProperty("expected").GetBoolean();var raw=test.TryGetProperty("oversize",out var over)&&over.GetBoolean()?new string(' ',1024*1024+1):test.GetProperty("raw").GetString()!;
            bool accepted;string? error=null;ReplicaSampleRun? run=null;
            try{run=ReplicaSampleRun.Read(Encoding.UTF8.GetBytes(raw));accepted=true;}
            catch(Exception e)when(e is JsonException or InvalidDataException or KeyNotFoundException or InvalidOperationException or IOException or FormatException or ArgumentException){accepted=false;error=e.GetType().Name;}
            var safe=run?.WriteJson([]);var echoed=safe is not null&&Encoding.UTF8.GetString(safe).Contains("SYNTHETIC-NEVER-ECHO");
            var roundTrip=run is null||ReplicaSampleRun.Read(safe!).Groups.SequenceEqual(run.Groups);
            rows.Add(new{id,expected,accepted,error,echoed,roundTrip,passed=expected==accepted&&!echoed&&roundTrip});
        }
        File.WriteAllText(Path.Combine(output,"pageintegration-file-checks.json"),JsonSerializer.Serialize(rows,new JsonSerializerOptions{WriteIndented=true}));
        using var results=JsonDocument.Parse(JsonSerializer.Serialize(rows));if(results.RootElement.EnumerateArray().Any(x=>!x.GetProperty("passed").GetBoolean()))throw new InvalidOperationException("Original sample-reader comparison failed");
    }
}
