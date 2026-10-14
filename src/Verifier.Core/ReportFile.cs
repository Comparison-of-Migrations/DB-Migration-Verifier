using System.Security.Cryptography;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Verifier.Core;

public static partial class ReportFile
{
    private static readonly JsonSerializerOptions Options=new() { WriteIndented=true, UnmappedMemberHandling=JsonUnmappedMemberHandling.Disallow, RespectRequiredConstructorParameters=true, RespectNullableAnnotations=true, AllowDuplicateProperties=false, Converters={new ExactJsonStringConverter(),new JsonStringEnumConverter(allowIntegerValues:false)} };
    public static async Task SaveAsync(string path,RunReport report)
    {
        using var lease=report.AcquireStorage();
        // Reuse owned immutable metadata views; copy mutable inputs into private
        // arrays, then validate exactly what is written.
        // This avoids reconstructing a second report from its serialized file.
        report=Snapshot(report);
        Validate(report);
        await AtomicFile.WriteAsync(path,async stream=>
        {
            await stream.WriteAsync("{\"Format\":\"DMV-RESULT-1\",\"Payload\":\""u8.ToArray());
            using var hash=IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
            using var transform=new ToBase64Transform();
            await using(var base64=new CryptoStream(stream,transform,CryptoStreamMode.Write,leaveOpen:true))
            {
                await using(var hashing=new HashingStream(base64,hash))
                {await JsonSerializer.SerializeAsync(hashing,report,Options);}
                await base64.FlushFinalBlockAsync();
            }
            await stream.WriteAsync(System.Text.Encoding.ASCII.GetBytes("\",\"Sha256\":\""+Convert.ToHexString(hash.GetHashAndReset())+"\"}"));
        });
    }
    internal static RunReport Snapshot(RunReport report)
    {
        if(report.Tables==null||report.Checks==null||report.Mappings==null||report.BehaviorRequests==null||report.BehaviorResults==null||
           report.Checks.Any(c=>c==null||c.Findings==null||c.Issues==null||c.PairingLimits==null)||report.Mappings.Any(m=>m==null||m.Columns==null)||
           report.BehaviorResults.Any(r=>r==null||r.Steps==null||r.Effects==null||r.DependencyTables==null||r.SequenceObservations==null))throw new InvalidDataException();
        return report with
        {
            Tables=report.Tables is IFrozenMetadata<string>?report.Tables:report.Tables.ToArray(),
            Checks=report.Checks.Select(c=>c with{Findings=FindingBuffer.Freeze(c.Findings),Issues=IssueBuffer.Freeze(c.Issues),PairingLimits=IssueBuffer.Freeze(c.PairingLimits)}).ToArray(),
            Mappings=report.Mappings is IFrozenMetadata<TableMapping>?report.Mappings:report.Mappings.Select(m=>m with{Columns=m.Columns is IFrozenMetadata<ColumnMapping>?m.Columns:m.Columns.ToArray()}).ToArray(),
            BehaviorRequests=report.BehaviorRequests.ToArray(),
            BehaviorResults=report.BehaviorResults.Select(r=>r with{Steps=r.Steps.ToArray(),Effects=r.Effects.ToArray(),DependencyTables=r.DependencyTables.ToArray(),SequenceObservations=r.SequenceObservations.ToArray()}).ToArray()
        };
    }
    public static async Task<RunReport> LoadAsync(string path)
    {
        await using var stream=File.OpenRead(path);
        return await ReadEnvelope(stream);
    }
    // History records identify the exact saved file. Parse and hash the same
    // open stream, so replacement between two path opens cannot mix evidence.
    public static async Task<RunReport> LoadRecordedAsync(string path,string sha256,Guid runId)
    {
        byte[] expected;try{expected=Convert.FromHexString(sha256);}catch(FormatException){throw new InvalidDataException();}
        if(expected.Length!=32||runId==Guid.Empty)throw new InvalidDataException();
        await using var stream=File.OpenRead(path);using var hash=IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
        await using var hashing=new HashingStream(stream,hash);var report=await ReadEnvelope(hashing);
        if(!CryptographicOperations.FixedTimeEquals(expected,hash.GetHashAndReset())||report.RunId!=runId||report.Example){report.Dispose();throw new InvalidDataException();}
        return report;
    }
    internal static void Validate(RunReport report)
    {
        if(report.Version is not (1 or 2 or 3) || report.RunId==Guid.Empty || report.Started==default || report.Finished<report.Started || report.Checks==null || report.Checks.Count!=Checks.Definitions.Length || report.Tables==null || report.Tables.Any(string.IsNullOrWhiteSpace) || report.Adapter is not ("Synthetic-v1" or "PostgreSql-v1" or "PostgreSql-v2" or "PostgreSql-v3" or "SqlServer-v1" or "MySql-v1" or "MariaDb-v1") || report.Example!=(report.Adapter=="Synthetic-v1") || report.Mappings==null) throw new InvalidDataException();
        using var namesOwner=new RunReportStorage();
        var tableNames=TableNameBuffer.Unique(report.Tables,namesOwner);
        bool HasTable(string? name)=>TableNameBuffer.Contains(tableNames,name);
        var incompleteTables=false;var incompleteColumns=false;var renamedScope=false;
        if(report.Version==1&&report.Mappings.Count>0)throw new InvalidDataException();
        if(report.DatabaseIsNamespace&&(report.Adapter is not ("MySql-v1" or "MariaDb-v1")||report.Tables.Any(t=>ScopeMatching.QualifiedParts(t)==null)))throw new InvalidDataException();
        if(report.BehaviorRequests==null||report.BehaviorResults==null||report.Version<3&&(report.BehaviorRequests.Count>0||report.BehaviorResults.Count>0))throw new InvalidDataException();
        if(report.Version==3)
        {
            if(report.Example||!report.Checks.Any(c=>c!=null&&c.Category==Category.Behavior&&c.Selected))throw new InvalidDataException();
            BehaviorReporting.ValidateScope(report.BehaviorRequests,report.BehaviorResults);
        }
        if(report.Version>=2)
        {
            var sources=TableNameBuffer.Unique(report.Mappings.Select(m=>m?.Source).OfType<string>(),namesOwner);
            var targets=TableNameBuffer.Unique(report.Mappings.Select(m=>m?.Target).OfType<string>(),namesOwner);
            foreach(var mapping in report.Mappings)
            {
                if(mapping==null||mapping.Source==null&&mapping.Target==null||mapping.Columns==null||
                   mapping.Source!=null&&!HasTable(mapping.Source)||
                   mapping.Target!=null&&!HasTable(mapping.Target))throw new InvalidDataException();
                if(mapping.Source==null||mapping.Target==null)incompleteTables=true;
                else renamedScope|=report.DatabaseIsNamespace?ScopeMatching.QualifiedParts(mapping.Source)![1]!=ScopeMatching.QualifiedParts(mapping.Target)![1]:mapping.Source!=mapping.Target;
                IEnumerable<CatalogFact> ColumnNames()
                {
                    foreach(var column in mapping.Columns)
                    {
                        if(column==null||column.Source==null&&column.Target==null||
                           column.Source!=null&&(string.IsNullOrWhiteSpace(column.Source)||mapping.Source==null)||
                           column.Target!=null&&(string.IsNullOrWhiteSpace(column.Target)||mapping.Target==null))throw new InvalidDataException();
                        incompleteColumns|=column.Source==null||column.Target==null;renamedScope|=column.Source!=column.Target;
                        if(column.Source!=null)yield return new("source:"+column.Source,null,"name",null);
                        if(column.Target!=null)yield return new("target:"+column.Target,null,"name",null);
                    }
                }
                using var columns=new CatalogComparison.CatalogFactIndex(ColumnNames(),false,default);
                foreach(var entry in columns.Entries(default))if(entry.Duplicate)throw new InvalidDataException();
            }
            if(tableNames.Any(table=>!TableNameBuffer.Contains(sources,table)&&!TableNameBuffer.Contains(targets,table)))throw new InvalidDataException();
            if(report.DatabaseIsNamespace&&new[]{sources,targets}.Any(side=>side.Count>0&&side.Any(t=>ScopeMatching.QualifiedParts(t)![0]!=ScopeMatching.QualifiedParts(side[0])![0])))throw new InvalidDataException();
        }
        if(report.Checks.Any(x=>x==null)) throw new InvalidDataException();
        using var findingIds=new ReportFindingIds();
        if(report.Checks.GroupBy(x=>x.Category).Any(g=>g.Select(x=>x.Selected).Distinct().Count()!=1)) throw new InvalidDataException();
        foreach(var definition in Checks.Definitions)
        {
            var entries=report.Checks.Where(x=>x.CheckId==definition.Id).ToArray();
            if(entries.Length!=1 || entries[0].Category!=definition.Category || entries[0].ResearchId!=definition.Research) throw new InvalidDataException();
            var entry=entries[0];
            if(report.Version==3&&entry.Category==Category.Behavior)
            {
                var expected=BehaviorReporting.Summarize(entry,report.BehaviorRequests,report.BehaviorResults);
                if(entry.Execution!=expected.Execution||entry.Verdict!=expected.Verdict||entry.Reason!=expected.Reason||entry.Findings==null||!entry.Findings.SequenceEqual(expected.Findings)||entry.Issues?.Count!=0||entry.PairingLimits?.Count!=0)throw new InvalidDataException();
            }
            if(entry.PairingLimits==null||entry.PairingLimits.Any(i=>i==null||i.Reason!=Reason.AmbiguousRows||!HasTable(i.Table)||i.Column!=null||i.Attribute!=null)||entry.PairingLimits.Count>0&&(!entry.Selected||entry.CheckId!="DATA-01"||entry.Execution==Execution.Pending))throw new InvalidDataException();
            if(entry.Findings==null || entry.Issues==null || entry.Findings.Any(x=>x==null) || entry.Issues.Any(x=>x==null || x.Reason==Reason.None || !Enum.IsDefined(x.Reason) || (x.Table==null? !report.DatabaseScope||x.Column!=null : !HasTable(x.Table)) || x.Column!=null&&string.IsNullOrWhiteSpace(x.Column))) throw new InvalidDataException();
            if(entry.Issues.Any(x=>x.Attribute!=null&&(!CatalogComparison.Fields.TryGetValue(x.Attribute,out var field)||field.Check!=entry.CheckId)))throw new InvalidDataException();
            if(!Enum.IsDefined(entry.Execution) || !Enum.IsDefined(entry.Verdict) || !Enum.IsDefined(entry.Reason)) throw new InvalidDataException();
            if(entry.Execution==Execution.Running || entry.Execution==Execution.Pending&&entry.Findings.Count>0) throw new InvalidDataException();
            if(!entry.Selected && (entry.Execution!=Execution.Pending || entry.Verdict!=Verdict.Unverified || entry.Reason!=Reason.NotImplemented || entry.Findings.Count>0 || entry.Issues.Count>0)) throw new InvalidDataException();
            if(entry.Verdict==Verdict.Equal && (entry.Execution!=Execution.Completed || entry.Reason!=Reason.None || entry.Findings.Count!=0 || entry.Issues.Count>0 || report.Tables.Count==0&&!report.DatabaseScope)) throw new InvalidDataException();
            if(entry.CheckId=="DATA-01"&&entry.Verdict==Verdict.Equal&&report.Version>=2&&(incompleteTables||incompleteColumns))throw new InvalidDataException();
            if(entry.Category==Category.Schema&&entry.Verdict==Verdict.Equal&&report.Version>=2&&(incompleteTables||entry.CheckId=="SCHEMA-01"&&renamedScope))throw new InvalidDataException();
            if((entry.Verdict==Verdict.Different)!=(entry.Findings.Count>0) || entry.Issues.Count>0&&entry.Reason==Reason.None) throw new InvalidDataException();
            if(entry.Execution==Execution.Interrupted&&entry.Reason==Reason.None || entry.Verdict==Verdict.Indeterminate&&entry.Reason==Reason.None || entry.Verdict==Verdict.Unverified&&entry.Execution!=Execution.Pending) throw new InvalidDataException();
            foreach(var f in entry.Findings)
            {
                if(f.Kind=="TrialExpectation")
                {
                    if(report.Version!=3||entry.Category!=Category.Behavior||!Guid.TryParseExact(f.Id,"N",out _)||!findingIds.Add(f.Id)||f.Count<=0||f.Column!=null||!report.BehaviorResults.Any(r=>r.Id.ToString("N")==f.Id&&r.Table==f.Table))throw new InvalidDataException();
                    continue;
                }
                var catalog=CatalogComparison.Fields.TryGetValue(f.Kind,out var field);
                var integrity=f.Kind is "SourceNotNullViolation" or "TargetNotNullViolation" or "SourceUniqueViolation" or "TargetUniqueViolation" or "SourceReferenceViolation" or "TargetReferenceViolation" or "SourceCheckViolation" or "TargetCheckViolation" or "SourceExclusionViolation" or "TargetExclusionViolation";
                if(!Guid.TryParseExact(f.Id,"N",out _) || !findingIds.Add(f.Id) || f.Count<=0 || (f.Table==null ? !report.DatabaseScope||f.Column!=null||!catalog||!CatalogComparison.DatabaseChecks.Contains(entry.CheckId) : !HasTable(f.Table)) || f.Column!=null&&string.IsNullOrWhiteSpace(f.Column) || !catalog&&!integrity&&f.Kind is not ("TablePresence" or "TableName" or "ColumnDefinition" or "ColumnValue" or "SourceOnlyRow" or "TargetOnlyRow" or "SourceOnlyProjection" or "TargetOnlyProjection") || (f.Kind is "ColumnDefinition" or "ColumnValue"||catalog&&f.Kind.StartsWith("Column",StringComparison.Ordinal))&&f.Column==null || catalog&&entry.CheckId!=field.Check || integrity&&entry.CheckId!="DATA-02" || f.Kind is ("ColumnValue" or "SourceOnlyRow" or "TargetOnlyRow" or "SourceOnlyProjection" or "TargetOnlyProjection")&&entry.CheckId!="DATA-01" || entry.Category!= (catalog ? Checks.Definitions.Single(d=>d.Id==field.Check).Category : f.Kind is "TablePresence" or "TableName" or "ColumnDefinition" ? Category.Schema : Category.Data)) throw new InvalidDataException();
            }
        }
        findingIds.Verify();
    }
}
