using System.Reflection;
using System.Text;
using Verifier.Core;
using Verifier.Data;
using Xunit;

namespace Verifier.Tests;

public sealed class CoreTests
{
    private static readonly IReadOnlySet<Category> Selected=new HashSet<Category>{Category.Data,Category.Schema,Category.Access,Category.Objects};
    private static readonly Column[] Columns=[new("value","text",1,true,null,true)];
    [Theory]
    [InlineData("1","1.00")][InlineData("-0.00","0")][InlineData("999999999999999999999999999999999999999.0000","999999999999999999999999999999999999999")]
    public void ExactNumbersDoNotRound(string a,string b)=>Assert.Equal(ExactRow.CanonicalNumber(a),ExactRow.CanonicalNumber(b));
    [Fact] public void ArbitraryPrecisionLastDigitMatters()=>Assert.NotEqual(ExactRow.CanonicalNumber("0.000000000000000000000000000000000000001"),ExactRow.CanonicalNumber("0.000000000000000000000000000000000000002"));
    [Theory][InlineData(null,"")][InlineData("a","a ")][InlineData("a","A")][InlineData("é","é")]
    public void NoTextNormalization(string? a,string? b)=>Assert.False(ExactRow.Encode([a],Columns).SequenceEqual(ExactRow.Encode([b],Columns)));
    [Fact] public async Task OrderIsIgnoredButMultiplicityIsNot()
    {
        var same=await Run(new Fake("a",["A","B","B"]),new Fake("b",["B","A","B"]));
        Assert.Equal(Verdict.Equal,same.Checks[0].Verdict); Assert.False(same.AllEqual); Assert.True(same.HasUnknown);
        var different=await Run(new Fake("a",["A","B","B"]),new Fake("b",["A","A","B"]));
        Assert.Equal(Verdict.Different,different.Checks[0].Verdict); Assert.Equal(2,different.Checks[0].Findings.Sum(x=>x.Count));
    }
    [Fact] public async Task EqualCountsCannotHideDifferentValues()
    { var r=await Run(new Fake("a",["secret-marker-A"]),new Fake("b",["secret-marker-B"])); Assert.True(r.HasDifferences); Assert.True(r.HasUnknown); Assert.DoesNotContain("secret-marker",System.Text.Json.JsonSerializer.Serialize(r)); }
    [Fact] public async Task VisibilityUnknownCannotBecomeEqual()
    { var r=await Run(new Fake("a",["A"],complete:false),new Fake("b",["A"])); Assert.Equal(Verdict.Indeterminate,r.Checks[0].Verdict); Assert.False(r.AllEqual); }
    [Fact] public async Task ProgressNeverCertifiesPendingScopeOrExposesValues()
    {
        var events=new List<ProgressInfo>();
        var feedback=new CaptureProgress(events);
        var result=await new ComparisonEngine().RunAsync(new Fake("a",["PRIVATE-A"]),new Fake("b",["PRIVATE-B"]),Selected,null,"Synthetic-v1",true,feedback,default);
        Assert.Contains(events,p=>p.Checks?.Any(c=>c.Execution==Execution.Running)==true);
        var final=Assert.Single(events,p=>p.Stage=="Finalize").Checks!;
        Assert.Equal(result.Checks.Select(c=>(c.CheckId,c.Execution,c.Verdict)),final.Select(c=>(c.CheckId,c.Execution,c.Verdict)));
        Assert.All(final.Where(c=>c.Execution==Execution.Pending),c=>Assert.NotEqual(Verdict.Equal,c.Verdict));
        Assert.DoesNotContain("PRIVATE-",System.Text.Json.JsonSerializer.Serialize(events));
    }
    private sealed class CaptureProgress(List<ProgressInfo> events):IProgress<ProgressInfo>
    { public void Report(ProgressInfo value)=>events.Add(value); }
    [Fact] public async Task InterruptedRunsPreserveKnownDifferences()
    {
        using var cancellation=new CancellationTokenSource();
        var r=await new ComparisonEngine().RunAsync(new Fake("a",["A"],second:true,cancel:cancellation),new Fake("b",["B"],second:true),Selected,null,"Synthetic-v1",true,null,cancellation.Token);
        Assert.True(r.HasDifferences); Assert.True(r.HasUnknown); Assert.Equal(Execution.Interrupted,r.Checks[0].Execution); Assert.Equal(Reason.Cancelled,r.Checks[0].Reason);
        Assert.All(r.Checks.Where(c=>c.Category is Category.Access or Category.Objects),c=>Assert.Equal(Execution.Pending,c.Execution));
    }
    [Fact] public async Task SameDatabaseCannotBeComparedAsSuccess()
    { var r=await Run(new Fake("same",["A"]),new Fake("same",["A"])); Assert.False(r.AllEqual); Assert.All(r.Checks.Where(x=>x.Selected),x=>Assert.Equal(Reason.SameDatabase,x.Reason)); }
    [Fact] public void SpillSortUsesExactBytesAndMultipleMergePasses()
    {
        using var sort=new ProtectedSort(32);
        for(var i=200;i>=0;i--) sort.Add(Encoding.UTF8.GetBytes(i.ToString("D5")));
        var actual=sort.Finish(default).Select(Encoding.UTF8.GetString).ToArray();
        Assert.Equal(Enumerable.Range(0,201).Select(x=>x.ToString("D5")),actual);
    }
    [Theory][InlineData("flip")][InlineData("truncate")][InlineData("append")][InlineData("swap")][InlineData("delete")]
    public void DamagedTemporaryChunksNeverProduceEqual(string change)
    {
        using var sort=new ProtectedSort(16); sort.Add(Encoding.UTF8.GetBytes("SYNTHETIC-VALUE-A")); sort.Add(Encoding.UTF8.GetBytes("SYNTHETIC-VALUE-B"));
        var output=sort.Finish(default);
        var directory=(string)typeof(ProtectedSort).GetField("directory",BindingFlags.NonPublic|BindingFlags.Instance)!.GetValue(sort)!;
        var files=Directory.GetFiles(directory).Order().ToArray(); var bytes=File.ReadAllBytes(files[0]);
        Assert.DoesNotContain("SYNTHETIC",Encoding.UTF8.GetString(bytes));
        if(change=="flip") { bytes[^1]^=1; File.WriteAllBytes(files[0],bytes); }
        if(change=="truncate") File.WriteAllBytes(files[0],bytes[..^1]);
        if(change=="append") File.WriteAllBytes(files[0],[..bytes,0]);
        if(change=="swap") { var other=File.ReadAllBytes(files[1]); File.WriteAllBytes(files[0],other); File.WriteAllBytes(files[1],bytes); }
        if(change=="delete") File.Delete(files[0]);
        Assert.ThrowsAny<Exception>(()=>output.ToArray());
    }
    [Fact] public void OwnerOnlyPermissionsAndCleanup()
    {
        if(OperatingSystem.IsWindows()) return;
        var sort=new ProtectedSort(); sort.Add([1,2,3]); _=sort.Finish(default).ToArray();
        var dir=(string)typeof(ProtectedSort).GetField("directory",BindingFlags.NonPublic|BindingFlags.Instance)!.GetValue(sort)!;
        Assert.Equal(UnixFileMode.UserRead|UnixFileMode.UserWrite|UnixFileMode.UserExecute,File.GetUnixFileMode(dir));
        foreach(var f in Directory.GetFiles(dir)) Assert.Equal(UnixFileMode.UserRead|UnixFileMode.UserWrite,File.GetUnixFileMode(f));
        sort.Dispose(); Assert.False(Directory.Exists(dir));
    }
    [Fact] public async Task SafeReportRoundTripAndTamperRejection()
    {
        var dir=Path.Combine(Path.GetTempPath(),"dmv-report-test-"+Guid.NewGuid().ToString("N")); Directory.CreateDirectory(dir);
        try
        {
            var r=await Run(new Fake("a",["FAKE-SECRET-A"]),new Fake("b",["FAKE-SECRET-B"])); var path=Path.Combine(dir,"result.json");
            await ReportFile.SaveAsync(path,r); var restored=await ReportFile.LoadAsync(path);
            Assert.Equal(r.RunId,restored.RunId); Assert.True(restored.HasDifferences); Assert.True(restored.HasUnknown);
            Assert.Equal(r.Tables,restored.Tables);
            Assert.DoesNotContain("FAKE-SECRET",await File.ReadAllTextAsync(path));
            var node=System.Text.Json.Nodes.JsonNode.Parse(await File.ReadAllTextAsync(path))!; node["Sha256"]="00";
            await File.WriteAllTextAsync(path,node.ToJsonString()); await Assert.ThrowsAnyAsync<Exception>(()=>ReportFile.LoadAsync(path));
        } finally { Directory.Delete(dir,true); }
    }
    [Theory][InlineData("{}")] [InlineData("{\"Format\":\"DMV-RESULT-1\",\"Payload\":null,\"Sha256\":null}")]
    public async Task MalformedReportsAreRecoverable(string content)
    {
        var path=Path.Combine(Path.GetTempPath(),"dmv-invalid-"+Guid.NewGuid().ToString("N")+".json");
        try { await File.WriteAllTextAsync(path,content); await Assert.ThrowsAnyAsync<Exception>(()=>ReportFile.LoadAsync(path)); }
        finally { File.Delete(path); }
    }
    [Fact] public async Task UnknownSelectedTableCannotProduceEmptySuccess()
    {
        var r=await new ComparisonEngine().RunAsync(new Fake("a",["A"]),new Fake("b",["A"]),Selected,new HashSet<string>{"demo.missing"},"Synthetic-v1",true,null,default);
        Assert.False(r.AllEqual); Assert.Equal(Reason.InvalidInput,r.Checks[0].Reason);
    }
    [Fact] public async Task OversizedRowCannotPass()
    {
        var r=await Run(new Fake("a",[new string('a',ExactRow.MaxRowBytes+1)]),new Fake("b",["a"]));
        Assert.Equal(Reason.RowTooLarge,r.Checks[0].Reason); Assert.False(r.AllEqual);
    }
    [Fact] public void ProviderStringsRequireTlsAndNoPooling()
    {
        var s=new ConnectionSettings(DbKind.PostgreSql,"localhost",5432,"synthetic","observer","synthetic-only");
        var pg=new Npgsql.NpgsqlConnectionStringBuilder(s.PostgreSqlString()); Assert.Equal(Npgsql.SslMode.VerifyFull,pg.SslMode); Assert.False(pg.Pooling); Assert.False(pg.IncludeErrorDetail);
        var ms=new Microsoft.Data.SqlClient.SqlConnectionStringBuilder((s with { Kind=DbKind.SqlServer,Port=1433 }).SqlServerString());
        Assert.False(ms.TrustServerCertificate); Assert.Equal(Microsoft.Data.SqlClient.SqlConnectionEncryptOption.Mandatory,ms.Encrypt); Assert.False(ms.Pooling);
    }
    private static Task<RunReport> Run(IReadDatabase a,IReadDatabase b)=>new ComparisonEngine().RunAsync(a,b,Selected,null,"Synthetic-v1",true,null,default);
    private sealed class Fake(string id,string?[] values,bool complete=true,bool second=false,CancellationTokenSource? cancel=null) : IReadDatabase
    {
        public Task<Inventory> InspectAsync(CancellationToken token)=>Task.FromResult(new Inventory(id,second?[new("demo","a",Columns,true),new("demo","z",Columns,true)]:[new("demo","a",Columns,true)],complete,"Synthetic"));
        public async IAsyncEnumerable<byte[]> RowsAsync(Table table,[System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken token)
        { if(table.Name=="z") cancel?.Cancel(); foreach(var v in values) { token.ThrowIfCancellationRequested(); await Task.Yield(); yield return ExactRow.Encode([v],Columns); } }
        public ValueTask DisposeAsync()=>ValueTask.CompletedTask;
    }
}
