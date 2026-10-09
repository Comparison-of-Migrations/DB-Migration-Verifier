using System.Runtime.CompilerServices;
using Verifier.Core;
using Xunit;

namespace Verifier.Tests;

public sealed class CatalogTests
{
    public static IEnumerable<object[]> Attributes=>CatalogComparison.Fields.Select(f=>new object[]{f.Key,f.Value.Check});
    private static readonly Table Base=new("demo","table",[new("column","text",1,true,null,true)],true);
    private static Table With(string check,IReadOnlyList<CatalogFact> facts,Reason reason=Reason.None)=>Base with { Catalog=[new(check,facts,reason)] };
    private static Task<RunReport> Run(Table left,Table right,bool complete=true,IReadOnlySet<Category>? selected=null)=>new ComparisonEngine().RunAsync(new Database("a",left,complete),new Database("b",right,true),selected??new HashSet<Category>{Category.Schema},null,"Synthetic-v1",true,null,default,[]);
    [Theory][MemberData(nameof(Attributes))] public async Task EveryDeclaredAttributeDifferenceHasItsOwnSafeKind(string attribute,string check)
    {
        var column=attribute.StartsWith("Column",StringComparison.Ordinal)?"column":null;
        var a=With(check,[new("INTERNAL-OBJECT-NAME-MARKER",column,attribute,"SYNTHETIC-PRIVATE-EXPRESSION-A")]);var b=With(check,[new("INTERNAL-OBJECT-NAME-MARKER",column,attribute,"SYNTHETIC-PRIVATE-EXPRESSION-B")]);
        var report=await Run(a,b,selected:new HashSet<Category>{Checks.Definitions.Single(d=>d.Id==check).Category});var result=Assert.Single(report.Checks,c=>c.CheckId==check);
        Assert.Equal(Verdict.Different,result.Verdict);var finding=Assert.Single(result.Findings);Assert.Equal(attribute,finding.Kind);Assert.Equal(column,finding.Column);Assert.Equal(Base.Qualified,finding.Table);
        var path=Path.GetTempFileName();try
        {
            await ReportFile.SaveAsync(path,report);var restored=await ReportFile.LoadAsync(path);Assert.Equal(attribute,restored.Checks.Single(c=>c.CheckId==check).Findings.Single().Kind);
            foreach(var format in new[]{ReportFormat.Csv,ReportFormat.Html})
            {await ReportExport.SaveAsync(path,report,format);var contents=await File.ReadAllTextAsync(path);Assert.DoesNotContain("SYNTHETIC-PRIVATE",contents);Assert.DoesNotContain("INTERNAL-OBJECT-NAME-MARKER",contents);}
        }finally{File.Delete(path);}
    }
    [Fact] public async Task EmptyFullyObservedConstraintSectionsAreEqual()
    {var report=await Run(With("SCHEMA-02",[]),With("SCHEMA-02",[]));Assert.Equal(Verdict.Equal,report.Checks.Single(c=>c.CheckId=="SCHEMA-02").Verdict);Assert.False(report.AllEqual);}
    [Fact] public async Task AbsentAccessAndObjectCollectorsStayUnstarted()
    {
        var report=await Run(Base,Base,selected:new HashSet<Category>{Category.Access,Category.Objects});
        Assert.All(report.Checks.Where(c=>c.Selected),c=>
        {Assert.Equal(Execution.Pending,c.Execution);Assert.Equal(Verdict.Unverified,c.Verdict);Assert.Equal(Reason.NotImplemented,c.Reason);});
        Assert.False(report.AllEqual);
    }
    [Fact] public async Task AccessObservedOnOnlyOneSideStillReportsIncompleteComparison()
    {
        var report=await Run(Base,With("ACCESS-01",[new("owner",null,"TableOwner","SYNTHETIC-OWNER")]),selected:new HashSet<Category>{Category.Access});
        var check=report.Checks.Single(c=>c.CheckId=="ACCESS-01");
        Assert.Equal(Execution.Completed,check.Execution);Assert.Equal(Verdict.Indeterminate,check.Verdict);
        Assert.Equal(Reason.NotImplemented,check.Reason);Assert.Empty(check.Findings);Assert.NotEmpty(check.Issues);
    }
    [Fact] public async Task MissingOrDeniedSectionsDoNotInventObjectAbsence()
    {
        var populated=With("SCHEMA-02",[new("constraint",null,"ConstraintDefinition","definition")]);
        foreach(var missing in new[]{Base,With("SCHEMA-02",[],Reason.PermissionDenied)})
        {var r=await Run(missing,populated);var c=r.Checks.Single(c=>c.CheckId=="SCHEMA-02");Assert.Equal(Verdict.Indeterminate,c.Verdict);Assert.Empty(c.Findings);Assert.NotEmpty(c.Issues);}
        var limited=await Run(With("SCHEMA-02",[]),populated,false);Assert.Empty(limited.Checks.Single(c=>c.CheckId=="SCHEMA-02").Findings);
    }
    [Fact] public async Task KnownDifferencesCoexistWithIncompleteMetadata()
    {
        var report=await Run(With("SCHEMA-02",[new("same",null,"ConstraintValidated","false")],Reason.PermissionDenied),With("SCHEMA-02",[new("same",null,"ConstraintValidated","true"),new("unseen",null,"ConstraintDefinition","unknown")]));
        var check=report.Checks.Single(c=>c.CheckId=="SCHEMA-02");Assert.Equal(Verdict.Different,check.Verdict);Assert.Equal(Reason.PermissionDenied,check.Reason);Assert.Single(check.Findings);Assert.Single(check.Issues);Assert.True(report.HasDifferences);Assert.True(report.HasUnknown);
    }
    [Fact] public async Task DuplicateFactsCannotSelectAnArbitraryDefinition()
    {
        var report=await Run(With("SCHEMA-03",[new("i",null,"IndexDefinition","A"),new("i",null,"IndexDefinition","B")]),With("SCHEMA-03",[new("i",null,"IndexDefinition","B")]));
        var check=report.Checks.Single(c=>c.CheckId=="SCHEMA-03");Assert.Equal(Reason.AmbiguousScope,check.Reason);Assert.Empty(check.Findings);Assert.NotEqual(Verdict.Equal,check.Verdict);
    }
    [Fact] public async Task UnsupportedFieldsCannotCertifyEquality()
    {var t=With("SCHEMA-01",[new("x",null,"UnrecognizedAttribute","value")]);var r=await Run(t,t);Assert.Equal(Reason.NotImplemented,r.Checks[2].Reason);Assert.NotEqual(Verdict.Equal,r.Checks[2].Verdict);}
    [Fact] public async Task RenamedColumnsMatchFactsButKeepNameAndOrderDifferences()
    {
        var a=With("SCHEMA-01",[new("column","column","ColumnName","column"),new("column","column","ColumnPosition","1"),new("column","column","ColumnDefault","same")]);
        var b=Base with { Columns=[Base.Columns[0] with { Name="new_column",Position=2 }],Catalog=[new("SCHEMA-01",[new("new_column","new_column","ColumnName","new_column"),new("new_column","new_column","ColumnPosition","2"),new("new_column","new_column","ColumnDefault","same")],Reason.None)] };
        var r=await new ComparisonEngine().RunAsync(new Database("a",a,true),new Database("b",b,true),new HashSet<Category>{Category.Schema},null,"Synthetic-v1",true,null,default,[new(a.Qualified,b.Qualified,[new("column","new_column")])]);
        Assert.Equal(new[]{"ColumnName","ColumnPosition"},r.Checks[2].Findings.Select(f=>f.Kind).Order());Assert.All(r.Checks[2].Findings,f=>Assert.Equal("column",f.Column));
    }
    [Fact] public async Task UnselectedMetadataDoesNotAddFindingsOrIssues()
    {var r=await Run(With("SCHEMA-02",[new("x",null,"ConstraintDefinition","A")]),With("SCHEMA-02",[],Reason.PermissionDenied),selected:new HashSet<Category>{Category.Data});Assert.All(r.Checks.Where(c=>c.Category==Category.Schema),c=>{Assert.False(c.Selected);Assert.Empty(c.Findings);Assert.Empty(c.Issues);});}
    [Fact] public async Task AReportedAttributeCannotBeAssignedToTheWrongCheck()
    {
        var r=await Run(With("SCHEMA-02",[new("x",null,"ConstraintDefinition","A")]),With("SCHEMA-02",[new("x",null,"ConstraintDefinition","B")]));
        var checks=r.Checks.ToArray();var index=Array.FindIndex(checks,c=>c.CheckId=="SCHEMA-02");checks[index]=checks[index] with { Findings=[checks[index].Findings.Single() with { Kind="IndexDefinition" }] };
        var path=Path.GetTempFileName();try{await Assert.ThrowsAsync<InvalidDataException>(()=>ReportFile.SaveAsync(path,r with { Checks=checks }));}finally{File.Delete(path);}
    }
    [Fact] public async Task SchemaEqualityCannotContradictRecordedNames()
    {
        var a=With("SCHEMA-01",[]);var b=a with { Name="renamed" };var r=await new ComparisonEngine().RunAsync(new Database("a",a,true),new Database("b",b,true),new HashSet<Category>{Category.Schema},null,"Synthetic-v1",true,null,default,[new(a.Qualified,b.Qualified,[new("column","column")])]);
        var checks=r.Checks.ToArray();checks[2]=checks[2] with { Verdict=Verdict.Equal,Reason=Reason.None,Findings=[],Issues=[] };
        var path=Path.GetTempFileName();try{await Assert.ThrowsAsync<InvalidDataException>(()=>ReportFile.SaveAsync(path,r with { Checks=checks }));}finally{File.Delete(path);}
    }
    private sealed class Database(string identity,Table table,bool complete):IReadDatabase
    {
        public Task<Inventory> InspectAsync(CancellationToken token)=>Task.FromResult(new Inventory(identity,[table],complete,"Synthetic"));
        public async IAsyncEnumerable<byte[]> RowsAsync(Table ignored,[EnumeratorCancellation]CancellationToken token){await Task.CompletedTask;yield break;}
        public ValueTask DisposeAsync()=>ValueTask.CompletedTask;
    }
}
