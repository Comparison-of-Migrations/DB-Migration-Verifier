namespace Verifier.Core;

public enum DbKind { PostgreSql, SqlServer, MySql, MariaDb }
public enum Verdict { Equal, Different, Indeterminate, Unverified }
public enum Execution { Pending, Running, Completed, Interrupted }
public enum Reason { None, NotImplemented, UnsupportedType, VisibilityUnknown, PermissionDenied, RowFiltered, Cancelled, Timeout, ConnectionLost, TemporaryIntegrity, TemporaryStorage, RowTooLarge, IdentityUnknown, SameDatabase, InvalidInput, ObservationChanged, PlatformNotVerified, NoObjects, AmbiguousScope, MappingIncomplete, AmbiguousRows, Unpopulated, MissingDependency, UnsafeObservation }
public enum Category { Data, Schema, Access, Objects, Behavior }
public sealed record Column(string Name, string Type, int Position, bool Nullable, string? Definition, bool SafeValue)
{
    // Adapter-local expected wire identity; never compared across servers or exported in reports.
    public uint ElementOid { get; init; }
    public uint TypeOid { get; init; }
}
public sealed record Table(string Schema, string Name, IReadOnlyList<Column> Columns, bool SafeRead)
{
    public Reason ReadReason {get;init;}=SafeRead?Reason.None:Reason.RowFiltered;
    public IReadOnlyList<CatalogSection> Catalog { get; init; } = [];
    // Logical value reads include these descendants; schema still observes each table.
    public IReadOnlyList<string> DataDescendants { get; init; } = [];
    // Ordinary inheritance does not extend UNIQUE/FK/index constraints to children.
    public bool Partitioned { get; init; }
    public bool SafeLocalRead { get; init; } = SafeRead;
    // Catalog candidates only. Actual NULLs and duplicates must be checked before pairing rows.
    public IReadOnlyList<IReadOnlyList<string>> RowKeys { get; init; } = [];
    public string Qualified => Segment(Schema)+"."+Segment(Name);
    private static string Segment(string name)=>name.Contains('.')||name.Contains('"')?"\""+name.Replace("\"","\"\"")+"\"":name;
}
// Definitions may contain literals and principal names. Comparison may use
// owner-only authenticated encrypted temporary storage; permanent reports
// contain only a location, a fixed difference kind and an internal ID.
public sealed record CatalogFact(string Key,string? Column,string Attribute,string? Value)
{
    private string? value=Value;
    private CatalogText? text;
    // Explicit callers can still request a string. Comparison and protected
    // copies use the owned text directly, without retaining a full restoration.
    public string? Value {get=>text?.ReadString()??value;init{this.value=value;text=null;}}
    internal CatalogText? Text=>text;
    internal int ValueLength=>text?.Length??value?.Length??-1;
    internal CatalogFact WithText(CatalogText owned)=>this with{text=owned,value=null};
    internal bool ValueEquals(CatalogFact other,CancellationToken token=default)
        =>CatalogText.Equal(text,value,other.text,other.value,token);
    internal bool DefinitionEquals(CatalogFact other,CancellationToken token)
        =>Key==other.Key&&Column==other.Column&&Attribute==other.Attribute&&ValueEquals(other,token);
    public bool Equals(CatalogFact? other)=>other is not null&&DefinitionEquals(other,default);
    // Value can live on disk; equal values still share this hash without reading
    // secret definitions just to place their locations in a set.
    public override int GetHashCode()=>HashCode.Combine(Key,Column,Attribute);
    public override string ToString()=>"CatalogFact (local definition evidence redacted)";
}
public sealed record CatalogGap(string Key,string? Column,string Attribute,string LocalEvidence,Reason Reason)
{
    public override string ToString()=>"CatalogGap (local definition evidence redacted)";
}
public sealed record CatalogSection(string CheckId,IReadOnlyList<CatalogFact> Facts,Reason Reason)
{
    // Complete inventory with exactly located unread attributes. Permission/query
    // failures keep this false; a gap can never establish absence for its own field.
    public bool CompleteExceptGaps {get;init;}
    public IReadOnlyList<CatalogGap> Gaps {get;init;}=[];
}
public sealed record Inventory(string Identity, IReadOnlyList<Table> Tables, bool Complete, string Observation)
{
    public IReadOnlyList<CatalogSection> DatabaseCatalog {get;init;}=[];
    // In MySQL the selected database is the sole schema, rather than an
    // independent namespace below it. Keep actual locations in reports.
    public bool DatabaseIsNamespace {get;init;}
}
public sealed record Finding(string Id, string? Table, string? Column, string Kind, long Count);
public sealed record ScopeIssue(string? Table,string? Column,Reason Reason)
{
    // Fixed catalog field ID only; never a raw definition or configuration name.
    public string? Attribute {get;init;}
}
public sealed record ColumnMapping(string? Source,string? Target);
public sealed record TableMapping(string? Source,string? Target,IReadOnlyList<ColumnMapping> Columns);
public sealed record CheckResult(string CheckId, string ResearchId, Category Category, bool Selected, Execution Execution, Verdict Verdict, Reason Reason, IReadOnlyList<Finding> Findings)
{
    public IReadOnlyList<ScopeIssue> Issues { get; init; } = Array.Empty<ScopeIssue>();
    // A pairing limitation does not invalidate a completed exact multiset comparison.
    public IReadOnlyList<ScopeIssue> PairingLimits { get; init; } = Array.Empty<ScopeIssue>();
}
public sealed record RunReport(int Version, Guid RunId, DateTimeOffset Started, DateTimeOffset Finished, bool Example, string Adapter, IReadOnlyList<string> Tables, IReadOnlyList<CheckResult> Checks):IDisposable
{
    internal RunReportStorage? LocalStorage{get;init;}
    internal IDisposable? AcquireStorage()=>LocalStorage?.Acquire();
    public void Dispose()=>LocalStorage?.Dispose();
    public bool DatabaseScope {get;init;}
    public bool DatabaseIsNamespace {get;init;}
    public IReadOnlyList<TableMapping> Mappings { get; init; } = Array.Empty<TableMapping>();
    public IReadOnlyList<BehaviorResult> BehaviorResults { get; init; } = Array.Empty<BehaviorResult>();
    public IReadOnlyList<TrialRequest> BehaviorRequests { get; init; } = Array.Empty<TrialRequest>();
    public bool HasDifferences => Checks.Any(x => x.Selected && x.CheckId!="DATA-02" && x.Verdict == Verdict.Different);
    public bool HasIntegrityViolations=>Checks.Any(x=>x.Selected&&x.CheckId=="DATA-02"&&x.Findings.Count>0);
    public bool HasUnknown => Checks.Any(x => x.Selected && (x.Execution != Execution.Completed || x.Verdict is Verdict.Unverified or Verdict.Indeterminate || x.Reason != Reason.None || x.Issues.Count>0))
        ||BehaviorResults.Any(r=>r.Cleanup!=TrialCleanup.Complete||r.TargetStructure!=Verdict.Equal||r.Effects.Any(e=>e is TrialEffect.SequenceStateUnverified or TrialEffect.TriggerEffectsUnverified or TrialEffect.ExternalEffectsUnverified));
    public bool AllEqual => (Tables.Count>0 || DatabaseScope) && Checks.Any(x => x.Selected) && Checks.Where(x=>x.Selected).All(x=>x.Verdict==Verdict.Equal) && !HasUnknown;
}
public sealed record ProgressInfo(string Stage, long Rows, IReadOnlyList<CheckResult>? Checks = null);
public interface IReadDatabase : IAsyncDisposable
{
    Task<Inventory> InspectAsync(CancellationToken token);
    IAsyncEnumerable<byte[]> RowsAsync(Table table, CancellationToken token);
}
// The row writer is consumed before requesting the next row; it never outlives its DB reader.
public interface IStreamingReadDatabase
{
    // order is a unique, possibly partial projection of the inspected table's columns.
    IAsyncEnumerable<Func<Stream,CancellationToken,Task>> StreamingRowsAsync(Table table,IReadOnlyList<int> order,CancellationToken token);
}
public sealed record IntegrityViolation(string Kind,string? Column,long Count);
public sealed record IntegrityObservation(IReadOnlyList<IntegrityViolation> Violations,IReadOnlyList<ScopeIssue> Issues);
public interface IIntegrityDatabase
{
    Task<IntegrityObservation> InspectIntegrityAsync(Table table,CancellationToken token);
}
// Optional adapter observation guard. A limit invalidates only its frozen
// catalog check/scope; it must not supply definitions, values or new findings.
public sealed record CatalogObservationLimit(string CheckId,string? Table,Reason Reason,string? Attribute=null);
public interface ICatalogObservationDatabase
{
    Task<IReadOnlyList<CatalogObservationLimit>> InspectCatalogChangesAsync(IReadOnlyList<Table> tables,IReadOnlySet<string> checks,bool databaseScope,CancellationToken token);
}
public sealed class VerificationException(Reason reason) : Exception("Verification could not be completed.")
{
    public Reason Reason { get; } = reason;
}
public static class Checks
{
    public static readonly (string Id, string Research, Category Category)[] Definitions =
    [ ("DATA-01","RP-D01",Category.Data), ("DATA-02","RP-D01",Category.Data),
      ("SCHEMA-01","RP-C01",Category.Schema), ("SCHEMA-02","RP-C01",Category.Schema), ("SCHEMA-03","RP-C02",Category.Schema),
      ("ACCESS-01","RP-A01",Category.Access), ("ACCESS-02","RP-A02",Category.Access),
      ("OBJECT-01","RP-O01",Category.Objects), ("OBJECT-02","RP-O01",Category.Objects), ("OBJECT-03","RP-O01",Category.Objects),
      ("BEHAVIOR-01","RP-B01",Category.Behavior), ("BEHAVIOR-02","RP-A02",Category.Behavior) ];
    public static List<CheckResult> Initial(IReadOnlySet<Category> selected) => Definitions.Select(x =>
        new CheckResult(x.Id,x.Research,x.Category,selected.Contains(x.Category),Execution.Pending,Verdict.Unverified,Reason.NotImplemented,Array.Empty<Finding>())).ToList();
}
