using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Verifier.Core;
using Xunit;

namespace Verifier.Tests;

public sealed class ReportTests
{
    private const string Location="=SUM(1,2),\"<script>alert(1)</script>\n合成";
    private const string Column="＠SUM(1,2)";
    private static RunReport Mixed()
    {
        var checks=Checks.Initial(new HashSet<Category>{Category.Data,Category.Schema});
        checks[0]=checks[0] with { Execution=Execution.Completed,Verdict=Verdict.Different,Reason=Reason.UnsupportedType,
            Findings=[new(Guid.NewGuid().ToString("N"),Location,null,"SourceOnlyRow",2)],Issues=[new(Location,Column,Reason.UnsupportedType)] };
        checks[2]=checks[2] with { Execution=Execution.Completed,Verdict=Verdict.Equal,Reason=Reason.None };
        return new(1,Guid.NewGuid(),DateTimeOffset.UtcNow.AddSeconds(-1),DateTimeOffset.UtcNow,true,"Synthetic-v1",[Location],checks);
    }
    [Theory][InlineData(true,Reason.NoObjects)][InlineData(false,Reason.VisibilityUnknown)]
    public async Task EmptyInventoryCannotCertifyAnySelectedCheck(bool complete,Reason expected)
    {
        var report=await new ComparisonEngine().RunAsync(new EmptyDatabase("a",complete),new EmptyDatabase("b",complete),new HashSet<Category>{Category.Data},null,"Synthetic-v1",true,null,default);
        Assert.False(report.AllEqual);Assert.True(report.HasUnknown);
        Assert.All(report.Checks.Where(x=>x.Selected),x=>{Assert.Equal(expected,x.Reason);Assert.NotEqual(Verdict.Equal,x.Verdict);});
        var path=Path.GetTempFileName();try{await ReportFile.SaveAsync(path,report);Assert.False((await ReportFile.LoadAsync(path)).AllEqual);}finally{File.Delete(path);}
    }
    [Fact] public async Task UnknownIdentityCannotBecomeAnEqualComparison()
    {
        var report=await new ComparisonEngine().RunAsync(new EmptyDatabase("",true),new EmptyDatabase("b",true),new HashSet<Category>{Category.Data},null,"Synthetic-v1",true,null,default);
        Assert.Equal(Reason.IdentityUnknown,report.Checks[0].Reason);Assert.False(report.AllEqual);
    }
    private sealed class EmptyDatabase(string identity,bool complete):IReadDatabase
    {
        public Task<Inventory> InspectAsync(CancellationToken token)=>Task.FromResult(new Inventory(identity,[],complete,"Synthetic"));
        public IAsyncEnumerable<byte[]> RowsAsync(Table table,CancellationToken token)=>throw new InvalidOperationException("No row query should be made.");
        public ValueTask DisposeAsync()=>ValueTask.CompletedTask;
    }
    [Theory]
    [InlineData("equal-with-issue")][InlineData("excluded-completed")][InlineData("running")][InlineData("duplicate-finding")][InlineData("example-mismatch")][InlineData("missing-selected")][InlineData("duplicate-property")][InlineData("null-finding")][InlineData("out-of-scope")][InlineData("negative-count")]
    public async Task AValidChecksumDoesNotMakeContradictoryResultsValid(string mutation)
    {
        var dir=NewDirectory();
        try
        {
            var path=Path.Combine(dir,"result.json");await ReportFile.SaveAsync(path,Mixed());
            var envelope=JsonNode.Parse(await File.ReadAllTextAsync(path))!;
            var payload=JsonNode.Parse(Encoding.UTF8.GetString(Convert.FromBase64String(envelope["Payload"]!.GetValue<string>())))!;
            var check=payload["Checks"]![0]!;
            switch(mutation)
            {
                case "equal-with-issue":check["Verdict"]="Equal";check["Reason"]="None";check["Findings"]=new JsonArray();break;
                case "excluded-completed":payload["Checks"]![5]!["Execution"]="Completed";break;
                case "running":check["Execution"]="Running";break;
                case "duplicate-finding":check["Findings"]!.AsArray().Add(check["Findings"]![0]!.DeepClone());break;
                case "example-mismatch":payload["Example"]=false;break;
                case "missing-selected":check.AsObject().Remove("Selected");break;
                case "null-finding":check["Findings"]!.AsArray().Add((JsonNode?)null);break;
                case "out-of-scope":check["Findings"]![0]!["Table"]="not-in-scope";break;
                case "negative-count":check["Findings"]![0]!["Count"]=-1;break;
            }
            var json=payload.ToJsonString();if(mutation=="duplicate-property")json=json.Replace("\"Version\":1","\"Version\":1,\"Version\":1",StringComparison.Ordinal);
            var bytes=Encoding.UTF8.GetBytes(json);envelope["Payload"]=Convert.ToBase64String(bytes);envelope["Sha256"]=Convert.ToHexString(SHA256.HashData(bytes));
            await File.WriteAllTextAsync(path,envelope.ToJsonString());await Assert.ThrowsAnyAsync<Exception>(()=>ReportFile.LoadAsync(path));
        }
        finally { Directory.Delete(dir,true); }
    }
    [Fact] public async Task CsvPreservesMixedResultsAndProtectsEveryField()
    {
        var dir=NewDirectory();try
        {
            var report=Mixed();var path=Path.Combine(dir,"result.csv");await ReportExport.SaveAsync(path,report,ReportFormat.Csv);
            var rows=ParseCsv(await File.ReadAllTextAsync(path));Assert.All(rows,x=>Assert.Equal(25,x.Length));
            Assert.Equal(report.Checks.Count,rows.Count(x=>x[0]=="check"));
            Assert.Equal(report.Checks.Sum(x=>x.Issues.Count),rows.Count(x=>x[0]=="issue"));
            Assert.Equal(report.Checks.Sum(x=>x.Findings.Count),rows.Count(x=>x[0]=="finding"));
            Assert.Equal("'"+Location,Assert.Single(rows,x=>x[0]=="scope")[10]);
            Assert.Equal("'"+Column,Assert.Single(rows,x=>x[0]=="issue")[19]);
            var run=Assert.Single(rows,x=>x[0]=="run");Assert.Equal(new[]{"true","true","false"},run[7..10]);
            foreach(var check in report.Checks)
            {
                var record=Assert.Single(rows,x=>x[0]=="check"&&x[12]==check.CheckId);
                Assert.Equal(new[]{check.Category.ToString(),check.CheckId,check.ResearchId,check.Selected?"true":"false",check.Execution.ToString(),check.Verdict.ToString(),check.Reason.ToString()},record[11..18]);
            }
            var finding=Assert.Single(rows,x=>x[0]=="finding");Assert.Equal(report.Checks[0].Findings[0].Id,finding[18]);Assert.Equal("2",finding[21]);
        }finally{Directory.Delete(dir,true);}
    }
    [Theory][InlineData(false)][InlineData(true)]
    public async Task HtmlContainsEveryCheckAndEscapesInjectedMarkup(bool english)
    {
        var dir=NewDirectory();try
        {
            var report=Mixed();var path=Path.Combine(dir,"result.html");await ReportExport.SaveAsync(path,report,ReportFormat.Html,english);
            var html=await File.ReadAllTextAsync(path);Assert.DoesNotContain("<script>",html);Assert.Contains("&lt;script&gt;",html);
            Assert.Contains("default-src 'none'",html);Assert.DoesNotContain("<script src",html);Assert.DoesNotContain("<link",html);
            Assert.Contains("HasDifferences: true · HasUnknown: true · AllEqual: false",html);
            Assert.Contains(english?"Synthetic example":"합성 예시",html);Assert.Contains(english?"Excluded":"선택 제외",html);
            foreach(var check in report.Checks)Assert.Contains(check.CheckId+" · "+check.ResearchId,html);
            Assert.Contains(report.Checks[0].Findings[0].Id,html);Assert.Contains(Reason.UnsupportedType.ToString(),html);
        }finally{Directory.Delete(dir,true);}
    }
    [Theory][InlineData(ReportFormat.Json)][InlineData(ReportFormat.Csv)][InlineData(ReportFormat.Html)]
    public async Task FailedSaveLeavesOtherFilesAndNoTemporaryResidue(ReportFormat format)
    {
        var dir=NewDirectory();try
        {
            var old=Path.Combine(dir,"old-result");await File.WriteAllTextAsync(old,"KEEP-EXISTING");
            await Assert.ThrowsAsync<InvalidDataException>(()=>ReportExport.SaveAsync(old,Mixed() with { Example=false },format));
            Assert.Equal("KEEP-EXISTING",await File.ReadAllTextAsync(old));
            var destination=Path.Combine(dir,"existing-directory");Directory.CreateDirectory(destination);await File.WriteAllTextAsync(Path.Combine(destination,"keep"),"KEEP-NEIGHBOR");
            await Assert.ThrowsAnyAsync<IOException>(()=>ReportExport.SaveAsync(destination,Mixed(),format));
            Assert.Empty(Directory.GetFiles(dir,"*.tmp"));Assert.Equal("KEEP-NEIGHBOR",await File.ReadAllTextAsync(Path.Combine(destination,"keep")));
            var good=Path.Combine(dir,"report");await ReportExport.SaveAsync(good,Mixed(),format);
            if(!OperatingSystem.IsWindows())Assert.Equal(UnixFileMode.UserRead|UnixFileMode.UserWrite,File.GetUnixFileMode(good));
        }finally{Directory.Delete(dir,true);}
    }
    private static string NewDirectory(){var path=Path.Combine(Path.GetTempPath(),"dmv-export-test-"+Guid.NewGuid().ToString("N"));Directory.CreateDirectory(path);return path;}
    internal static List<string[]> ParseCsv(string text)
    {
        var records=new List<string[]>();var fields=new List<string>();var field=new StringBuilder();var quoted=false;
        for(var i=0;i<text.Length;i++)
        {
            var c=text[i];if(c=='"'){if(quoted&&i+1<text.Length&&text[i+1]=='"'){field.Append('"');i++;}else quoted=!quoted;}
            else if(!quoted&&c==','){fields.Add(field.ToString());field.Clear();}
            else if(!quoted&&c=='\n'){fields.Add(field.ToString().TrimEnd('\r'));field.Clear();records.Add(fields.ToArray());fields.Clear();}
            else field.Append(c);
        }
        Assert.False(quoted);Assert.Empty(fields);Assert.Equal(0,field.Length);return records;
    }
}
