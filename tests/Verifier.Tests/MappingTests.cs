using System.Runtime.CompilerServices;
using Verifier.Core;
using Xunit;

namespace Verifier.Tests;

public sealed class MappingTests
{
    private static readonly Column[] LeftColumns=[new("id","numeric",1,false,null,true),new("label","text",2,true,null,true)];
    private static readonly Column[] RightColumns=[new("title","text",1,true,null,true),new("identifier","numeric",2,false,null,true)];
    private static readonly Table Left=new("demo","old",LeftColumns,true),Right=new("demo","new",RightColumns,true);
    private static readonly TableMapping Mapping=new(Left.Qualified,Right.Qualified,[new("id","identifier"),new("label","title")]);
    private static readonly IReadOnlySet<Category> Selected=new HashSet<Category>{Category.Data,Category.Schema};
    private static Task<RunReport> Run(string?[][] left,string?[][] right)=>new ComparisonEngine().RunAsync(new Database("a",[Left],left),new Database("b",[Right],right),Selected,null,"Synthetic-v1",true,null,default,[Mapping]);
    [Fact] public async Task RenamedAndReorderedColumnsCompareValuesWithoutHidingSchemaDifferences()
    {
        var report=await Run([["100000000000000000000000000.000001","A "],["2",null],["2",null]],[[null,"2.0"],["A ","100000000000000000000000000.000001"],[null,"2"]]);
        Assert.Equal(Verdict.Equal,report.Checks[0].Verdict);Assert.Equal(Verdict.Different,report.Checks[2].Verdict);
        Assert.Contains(report.Checks[2].Findings,f=>f.Kind=="TableName");Assert.Equal(2,report.Checks[2].Findings.Count(f=>f.Kind=="ColumnDefinition"));
        Assert.Equal(2,report.Version);Assert.Equal(Mapping.Source,report.Mappings[0].Source);Assert.Equal(Mapping.Target,report.Mappings[0].Target);Assert.Equal(Mapping.Columns,report.Mappings[0].Columns);Assert.Equal(new[]{Left.Qualified,Right.Qualified}.Order(StringComparer.Ordinal),report.Tables);
        Assert.False(report.AllEqual);
    }
    [Fact] public async Task MappingNeverTrimsOrTransformsValues()
    {
        var report=await Run([["1","A "]],[["A","1"]]);
        Assert.Equal(Verdict.Different,report.Checks[0].Verdict);Assert.Equal(2,report.Checks[0].Findings.Sum(f=>f.Count));
    }
    [Fact] public async Task UnmappedColumnsCannotProducePartialEquality()
    {
        var report=await new ComparisonEngine().RunAsync(new Database("a",[Left],[["1","A"]]),new Database("b",[Right],[["A","1"]]),Selected,null,"Synthetic-v1",true,null,default,[Mapping with { Columns=[new("id","identifier")] }]);
        Assert.Equal(Reason.MappingIncomplete,report.Checks[0].Reason);Assert.NotEqual(Verdict.Equal,report.Checks[0].Verdict);Assert.Equal(2,report.Checks[0].Issues.Count);
    }
    [Theory][InlineData("duplicate-table")][InlineData("missing-table")][InlineData("duplicate-column")][InlineData("missing-column")]
    public async Task InvalidMappingIsRejectedBeforeReadingValues(string mode)
    {
        IReadOnlyList<TableMapping> mappings=mode switch { "duplicate-table"=>[Mapping,Mapping],"missing-table"=>[Mapping with { Target="demo.missing" }],"duplicate-column"=>[Mapping with { Columns=[new("id","identifier"),new("label","identifier")] }],_=>[Mapping with { Columns=[new("absent","identifier")] }] };
        var left=new Database("a",[Left],[]);var right=new Database("b",[Right],[]);
        var report=await new ComparisonEngine().RunAsync(left,right,Selected,null,"Synthetic-v1",true,null,default,mappings);
        Assert.Equal(Reason.InvalidInput,report.Checks[0].Reason);Assert.False(report.AllEqual);Assert.Equal(0,left.Reads+right.Reads);
    }
    [Fact] public void AManualPairDoesNotHideAnUnmatchedTargetWithTheSameSourceName()
    {
        var plan=ScopeMatching.Resolve(new("a",[Left],true,"Synthetic"),new("b",[Right,Left],true,"Synthetic"),[Mapping]);
        Assert.Equal(2,plan.Count);Assert.Contains(plan,p=>p.Key=="source:demo.old"&&p.Target==Right);Assert.Contains(plan,p=>p.Key=="target:demo.old"&&p.Source==null);
    }
    [Fact] public void ExplicitUnmatchedTableAllowsReassigningAnAutomaticDestination()
    {
        var plan=ScopeMatching.Resolve(new("a",[Left,Right],true,"Synthetic"),new("b",[Right],true,"Synthetic"),[Mapping,new(Right.Qualified,null,[])]);
        Assert.Single(plan,p=>p.Target==Right);Assert.Single(plan,p=>p.Source==Right&&p.Target==null);
    }
    [Fact] public async Task OnlySelectedMappingsAreExecutedAndPersisted()
    {
        var source=new Database("a",[Left,Right],[["1","A"]]);var target=new Database("b",[Right,Left],[["A","1"]]);
        var report=await new ComparisonEngine().RunAsync(source,target,Selected,new HashSet<string>{"source:demo.old"},"Synthetic-v1",true,null,default,[Mapping,new(Right.Qualified,null,[])]);
        Assert.Single(report.Mappings);Assert.Equal(1,source.Reads);Assert.Equal(1,target.Reads);Assert.Equal(Verdict.Equal,report.Checks[0].Verdict);
        var path=Path.GetTempFileName();try
        {
            await ReportFile.SaveAsync(path,report);var restored=await ReportFile.LoadAsync(path);
            Assert.Equal(report.Mappings[0].Source,restored.Mappings[0].Source);Assert.Equal(report.Mappings[0].Target,restored.Mappings[0].Target);Assert.Equal(report.Mappings[0].Columns,restored.Mappings[0].Columns);
            var csv=path+".csv";var html=path+".html";
            try
            {
                await ReportExport.SaveAsync(csv,restored,ReportFormat.Csv);await ReportExport.SaveAsync(html,restored,ReportFormat.Html);
                var rows=ReportTests.ParseCsv(await File.ReadAllTextAsync(csv));var tableRow=Assert.Single(rows,r=>r[0]=="table-map");Assert.Equal(Left.Qualified,tableRow[10]);Assert.Equal(Right.Qualified,tableRow[22]);
                foreach(var column in report.Mappings[0].Columns){var row=Assert.Single(rows,r=>r[0]=="column-map"&&r[23]==column.Source);Assert.Equal(column.Target,row[24]);Assert.Equal(Left.Qualified,row[10]);Assert.Equal(Right.Qualified,row[22]);}
                var htmlText=await File.ReadAllTextAsync(html);Assert.Contains("demo.old → demo.new",htmlText);Assert.Contains("identifier",htmlText);Assert.Contains("title",htmlText);
            }
            finally{File.Delete(csv);File.Delete(html);}
        }finally{File.Delete(path);}
    }
    [Fact] public async Task AmbiguousMetadataIsAnUnknownResultInsteadOfACrash()
    {
        var duplicate=new Database("a",[Left,Left],[]);var report=await new ComparisonEngine().RunAsync(duplicate,new Database("b",[Left],[]),Selected,null,"Synthetic-v1",true,null,default);
        Assert.Equal(Reason.AmbiguousScope,report.Checks[0].Reason);Assert.False(report.AllEqual);Assert.Equal(0,duplicate.Reads);
    }
    [Fact] public async Task FileCannotCertifyEqualDataWithUnmatchedMapping()
    {
        var report=await new ComparisonEngine().RunAsync(new Database("a",[Left],[]),new Database("b",[Right],[]),Selected,null,"Synthetic-v1",true,null,default,[Mapping with { Columns=[] }]);
        var checks=report.Checks.ToArray();checks[0]=checks[0] with { Verdict=Verdict.Equal,Reason=Reason.None,Issues=[] };
        var path=Path.GetTempFileName();try{await Assert.ThrowsAsync<InvalidDataException>(()=>ReportFile.SaveAsync(path,report with { Checks=checks }));}finally{File.Delete(path);}
    }
    [Fact] public async Task DottedIdentifiersAreDistinctObjectsAndRemainComparable()
    {
        var tables=new[]{Left with { Schema="a.b",Name="c" },Left with { Schema="a",Name="b.c" }};
        Assert.NotEqual(tables[0].Qualified,tables[1].Qualified);
        var report=await new ComparisonEngine().RunAsync(new Database("a",tables,[["1","A"]]),new Database("b",tables,[["1","A"]]),Selected,null,"Synthetic-v1",true,null,default);
        Assert.Equal(Verdict.Equal,report.Checks[0].Verdict);Assert.Equal(2,report.Tables.Count);
    }
    private sealed class Database(string identity,IReadOnlyList<Table> tables,string?[][] values):IReadDatabase
    {
        public int Reads { get; private set; }
        public Task<Inventory> InspectAsync(CancellationToken token)=>Task.FromResult(new Inventory(identity,tables,true,"Synthetic"));
        public async IAsyncEnumerable<byte[]> RowsAsync(Table table,[EnumeratorCancellation]CancellationToken token)
        { Reads++;foreach(var row in values){token.ThrowIfCancellationRequested();await Task.Yield();yield return ExactRow.Encode(row,table.Columns);} }
        public ValueTask DisposeAsync()=>ValueTask.CompletedTask;
    }
}
