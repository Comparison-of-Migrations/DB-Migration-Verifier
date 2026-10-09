namespace Verifier.Core;

public enum DbKind { PostgreSql, SqlServer }
public enum Verdict { Equal, Different, Indeterminate, Unverified }
public enum Execution { Pending, Running, Completed, Interrupted }
public enum Reason { None, NotImplemented, UnsupportedType, VisibilityUnknown, PermissionDenied, RowFiltered, Cancelled, Timeout, ConnectionLost, TemporaryIntegrity, TemporaryStorage, RowTooLarge, IdentityUnknown, SameDatabase, InvalidInput, ObservationChanged, PlatformNotVerified, NoObjects, AmbiguousScope, MappingIncomplete }
public enum Category { Data, Schema, Access, Objects, Behavior }
public sealed record Column(string Name, string Type, int Position, bool Nullable, string? Definition, bool SafeValue);
public sealed record Table(string Schema, string Name, IReadOnlyList<Column> Columns, bool SafeRead)
{
    public IReadOnlyList<CatalogSection> Catalog { get; init; } = [];
    public string Qualified => Segment(Schema)+"."+Segment(Name);
    private static string Segment(string name)=>name.Contains('.')||name.Contains('"')?"\""+name.Replace("\"","\"\"")+"\"":name;
}
// Definitions may contain literals and principal names. They stay in memory;
// reports contain only a location, a fixed difference kind and an internal ID.
public sealed record CatalogFact(string Key,string? Column,string Attribute,string? Value);
public sealed record CatalogSection(string CheckId,IReadOnlyList<CatalogFact> Facts,Reason Reason);
public sealed record Inventory(string Identity, IReadOnlyList<Table> Tables, bool Complete, string Observation);
public sealed record Finding(string Id, string? Table, string? Column, string Kind, long Count);
public sealed record ScopeIssue(string Table,string? Column,Reason Reason);
public sealed record ColumnMapping(string? Source,string? Target);
public sealed record TableMapping(string? Source,string? Target,IReadOnlyList<ColumnMapping> Columns);
public sealed record CheckResult(string CheckId, string ResearchId, Category Category, bool Selected, Execution Execution, Verdict Verdict, Reason Reason, IReadOnlyList<Finding> Findings)
{
    public IReadOnlyList<ScopeIssue> Issues { get; init; } = Array.Empty<ScopeIssue>();
}
public sealed record RunReport(int Version, Guid RunId, DateTimeOffset Started, DateTimeOffset Finished, bool Example, string Adapter, IReadOnlyList<string> Tables, IReadOnlyList<CheckResult> Checks)
{
    public IReadOnlyList<TableMapping> Mappings { get; init; } = Array.Empty<TableMapping>();
    public bool HasDifferences => Checks.Any(x => x.Selected && x.Verdict == Verdict.Different);
    public bool HasUnknown => Checks.Any(x => x.Selected && (x.Execution != Execution.Completed || x.Verdict is Verdict.Unverified or Verdict.Indeterminate || x.Reason != Reason.None || x.Issues.Count>0));
    public bool AllEqual => Tables.Count>0 && Checks.Any(x => x.Selected) && Checks.Where(x=>x.Selected).All(x=>x.Verdict==Verdict.Equal) && !HasUnknown;
}
public sealed record ProgressInfo(string Stage, long Rows, IReadOnlyList<CheckResult>? Checks = null);
public interface IReadDatabase : IAsyncDisposable
{
    Task<Inventory> InspectAsync(CancellationToken token);
    IAsyncEnumerable<byte[]> RowsAsync(Table table, CancellationToken token);
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
