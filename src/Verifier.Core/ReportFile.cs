using System.Security.Cryptography;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Verifier.Core;

public static class ReportFile
{
    private sealed record Envelope(string Format,string Payload,string Sha256);
    private static readonly JsonSerializerOptions Options=new() { WriteIndented=true, UnmappedMemberHandling=JsonUnmappedMemberHandling.Disallow, RespectRequiredConstructorParameters=true, RespectNullableAnnotations=true, AllowDuplicateProperties=false, Converters={new JsonStringEnumConverter(allowIntegerValues:false)} };
    public static async Task SaveAsync(string path,RunReport report)
    {
        Validate(report);
        var bytes=JsonSerializer.SerializeToUtf8Bytes(report,Options);
        var envelope=new Envelope("DMV-RESULT-1",Convert.ToBase64String(bytes),Convert.ToHexString(SHA256.HashData(bytes)));
        var temp=path+"."+Guid.NewGuid().ToString("N")+".tmp";
        try
        {
            var options=new FileStreamOptions { Mode=FileMode.CreateNew,Access=FileAccess.Write };
            if(!OperatingSystem.IsWindows()) options.UnixCreateMode=UnixFileMode.UserRead|UnixFileMode.UserWrite;
            await using(var stream=new FileStream(temp,options))
            { await JsonSerializer.SerializeAsync(stream,envelope,Options); await stream.FlushAsync(); stream.Flush(true); }
            _=await LoadAsync(temp); File.Move(temp,path,true);
        }
        finally { if(File.Exists(temp)) File.Delete(temp); }
    }
    public static async Task<RunReport> LoadAsync(string path)
    {
        await using var stream=File.OpenRead(path);
        var envelope=await JsonSerializer.DeserializeAsync<Envelope>(stream,Options) ?? throw new InvalidDataException();
        if(envelope.Format!="DMV-RESULT-1" || string.IsNullOrEmpty(envelope.Payload) || envelope.Sha256 is not { Length:64 }) throw new InvalidDataException();
        byte[] bytes;
        try { bytes=Convert.FromBase64String(envelope.Payload); } catch(FormatException) { throw new InvalidDataException(); }
        byte[] checksum;
        try { checksum=Convert.FromHexString(envelope.Sha256); } catch(FormatException) { throw new InvalidDataException(); }
        if(!CryptographicOperations.FixedTimeEquals(SHA256.HashData(bytes),checksum)) throw new InvalidDataException();
        var report=JsonSerializer.Deserialize<RunReport>(bytes,Options) ?? throw new InvalidDataException(); Validate(report); return report;
    }
    internal static void Validate(RunReport report)
    {
        if(report.Version is not (1 or 2) || report.RunId==Guid.Empty || report.Started==default || report.Finished<report.Started || report.Checks==null || report.Checks.Count!=Checks.Definitions.Length || report.Tables==null || report.Tables.Any(string.IsNullOrWhiteSpace) || report.Tables.Distinct(StringComparer.Ordinal).Count()!=report.Tables.Count || report.Adapter is not ("Synthetic-v1" or "PostgreSql-v1" or "SqlServer-v1") || report.Example!=(report.Adapter=="Synthetic-v1") || report.Mappings==null) throw new InvalidDataException();
        if(report.Version==1&&report.Mappings.Count>0)throw new InvalidDataException();
        if(report.Version==2)
        {
            if(report.Mappings.Any(m=>m==null||m.Source==null&&m.Target==null||m.Columns==null||m.Source!=null&&!report.Tables.Contains(m.Source,StringComparer.Ordinal)||m.Target!=null&&!report.Tables.Contains(m.Target,StringComparer.Ordinal)))throw new InvalidDataException();
            if(report.Mappings.Where(m=>m.Source!=null).Select(m=>m.Source).Distinct(StringComparer.Ordinal).Count()!=report.Mappings.Count(m=>m.Source!=null)||report.Mappings.Where(m=>m.Target!=null).Select(m=>m.Target).Distinct(StringComparer.Ordinal).Count()!=report.Mappings.Count(m=>m.Target!=null))throw new InvalidDataException();
            if(report.Tables.Except(report.Mappings.SelectMany(m=>new[]{m.Source,m.Target}).OfType<string>(),StringComparer.Ordinal).Any())throw new InvalidDataException();
            foreach(var mapping in report.Mappings)
            {
                if(mapping.Columns.Any(c=>c==null||c.Source==null&&c.Target==null||c.Source!=null&&(string.IsNullOrWhiteSpace(c.Source)||mapping.Source==null)||c.Target!=null&&(string.IsNullOrWhiteSpace(c.Target)||mapping.Target==null)))throw new InvalidDataException();
                if(mapping.Columns.Where(c=>c.Source!=null).Select(c=>c.Source).Distinct(StringComparer.Ordinal).Count()!=mapping.Columns.Count(c=>c.Source!=null)||mapping.Columns.Where(c=>c.Target!=null).Select(c=>c.Target).Distinct(StringComparer.Ordinal).Count()!=mapping.Columns.Count(c=>c.Target!=null))throw new InvalidDataException();
            }
        }
        if(report.Checks.Any(x=>x==null)) throw new InvalidDataException();
        var findingIds=new HashSet<string>(StringComparer.Ordinal);
        if(report.Checks.GroupBy(x=>x.Category).Any(g=>g.Select(x=>x.Selected).Distinct().Count()!=1)) throw new InvalidDataException();
        foreach(var definition in Checks.Definitions)
        {
            var entries=report.Checks.Where(x=>x.CheckId==definition.Id).ToArray();
            if(entries.Length!=1 || entries[0].Category!=definition.Category || entries[0].ResearchId!=definition.Research) throw new InvalidDataException();
            var entry=entries[0];
            if(entry.Findings==null || entry.Issues==null || entry.Findings.Any(x=>x==null) || entry.Issues.Any(x=>x==null || x.Reason==Reason.None || !Enum.IsDefined(x.Reason) || !report.Tables.Contains(x.Table,StringComparer.Ordinal) || x.Column!=null&&string.IsNullOrWhiteSpace(x.Column))) throw new InvalidDataException();
            if(!Enum.IsDefined(entry.Execution) || !Enum.IsDefined(entry.Verdict) || !Enum.IsDefined(entry.Reason)) throw new InvalidDataException();
            if(entry.Execution==Execution.Running || entry.Execution==Execution.Pending&&entry.Findings.Count>0) throw new InvalidDataException();
            if(!entry.Selected && (entry.Execution!=Execution.Pending || entry.Verdict!=Verdict.Unverified || entry.Reason!=Reason.NotImplemented || entry.Findings.Count>0 || entry.Issues.Count>0)) throw new InvalidDataException();
            if(entry.Verdict==Verdict.Equal && (entry.Execution!=Execution.Completed || entry.Reason!=Reason.None || entry.Findings.Count!=0 || entry.Issues.Count>0 || report.Tables.Count==0)) throw new InvalidDataException();
            if(entry.CheckId=="DATA-01"&&entry.Verdict==Verdict.Equal&&report.Version==2&&report.Mappings.Any(m=>m.Source==null||m.Target==null||m.Columns.Any(c=>c.Source==null||c.Target==null)))throw new InvalidDataException();
            if(entry.Category==Category.Schema&&entry.Verdict==Verdict.Equal&&report.Version==2&&report.Mappings.Any(m=>m.Source==null||m.Target==null||entry.CheckId=="SCHEMA-01"&&(m.Source!=m.Target||m.Columns.Any(c=>c.Source!=c.Target))))throw new InvalidDataException();
            if((entry.Verdict==Verdict.Different)!=(entry.Findings.Count>0) || entry.Issues.Count>0&&entry.Reason==Reason.None) throw new InvalidDataException();
            if(entry.Execution==Execution.Interrupted&&entry.Reason==Reason.None || entry.Verdict==Verdict.Indeterminate&&entry.Reason==Reason.None || entry.Verdict==Verdict.Unverified&&entry.Execution!=Execution.Pending) throw new InvalidDataException();
            foreach(var f in entry.Findings)
            {
                var catalog=CatalogComparison.Fields.TryGetValue(f.Kind,out var field);
                if(!Guid.TryParseExact(f.Id,"N",out _) || !findingIds.Add(f.Id) || f.Count<=0 || f.Table==null || !report.Tables.Contains(f.Table,StringComparer.Ordinal) || f.Column!=null&&string.IsNullOrWhiteSpace(f.Column) || !catalog&&f.Kind is not ("TablePresence" or "TableName" or "ColumnDefinition" or "SourceOnlyRow" or "TargetOnlyRow") || (f.Kind=="ColumnDefinition"||catalog&&f.Kind.StartsWith("Column",StringComparison.Ordinal))&&f.Column==null || catalog&&entry.CheckId!=field.Check || entry.Category!= (catalog ? Checks.Definitions.Single(d=>d.Id==field.Check).Category : f.Kind is "TablePresence" or "TableName" or "ColumnDefinition" ? Category.Schema : Category.Data)) throw new InvalidDataException();
            }
        }
    }
}
