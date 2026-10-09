namespace Verifier.Core;

public sealed record MatchedScope(Table? Source,Table? Target,IReadOnlyList<ColumnMapping> Columns)
{
    public string Key=>(Source!=null?"source:":"target:")+(Source?.Qualified??Target!.Qualified);
    public string Location=>Source?.Qualified??Target!.Qualified;
    public TableMapping Snapshot=>new(Source?.Qualified,Target?.Qualified,Columns);
    public bool Complete=>Source!=null&&Target!=null&&Columns.All(c=>c.Source!=null&&c.Target!=null);
}

public static class ScopeMatching
{
    public static IReadOnlyList<MatchedScope> Resolve(Inventory source,Inventory target,IReadOnlyList<TableMapping> overrides)
    {
        ValidateInventory(source);ValidateInventory(target);
        var left=source.Tables.ToDictionary(x=>x.Qualified,StringComparer.Ordinal);
        var right=target.Tables.ToDictionary(x=>x.Qualified,StringComparer.Ordinal);
        if(overrides.Any(x=>x==null||x.Columns==null||x.Source==null||!left.ContainsKey(x.Source)||x.Target!=null&&!right.ContainsKey(x.Target)) || overrides.Select(x=>x.Source).Distinct(StringComparer.Ordinal).Count()!=overrides.Count || overrides.Where(x=>x.Target!=null).Select(x=>x.Target).Distinct(StringComparer.Ordinal).Count()!=overrides.Count(x=>x.Target!=null)) throw new VerificationException(Reason.InvalidInput);
        var manual=overrides.ToDictionary(x=>x.Source!,StringComparer.Ordinal);
        var reserved=overrides.Where(x=>x.Target!=null).Select(x=>x.Target!).ToHashSet(StringComparer.Ordinal);
        var used=new HashSet<string>(StringComparer.Ordinal);var scopes=new List<MatchedScope>();
        foreach(var table in source.Tables.OrderBy(x=>x.Qualified,StringComparer.Ordinal))
        {
            Table? destination=null;IReadOnlyList<ColumnMapping> columns=[];
            if(manual.TryGetValue(table.Qualified,out var mapping))
            { destination=mapping.Target==null?null:right[mapping.Target];columns=mapping.Columns; }
            else if(!reserved.Contains(table.Qualified)) right.TryGetValue(table.Qualified,out destination);
            if(destination!=null && !used.Add(destination.Qualified)) throw new VerificationException(Reason.InvalidInput);
            scopes.Add(new(table,destination,ResolveColumns(table,destination,columns)));
        }
        foreach(var table in target.Tables.Where(t=>!used.Contains(t.Qualified)).OrderBy(t=>t.Qualified,StringComparer.Ordinal))
            scopes.Add(new(null,table,table.Columns.Select(c=>new ColumnMapping(null,c.Name)).ToArray()));
        return scopes;
    }
    public static IReadOnlyList<ColumnMapping> ResolveColumns(Table source,Table? target,IReadOnlyList<ColumnMapping> overrides)
    {
        var names=source.Columns.Select(c=>c.Name).ToHashSet(StringComparer.Ordinal);
        var destinations=(target?.Columns??[]).Select(c=>c.Name).ToHashSet(StringComparer.Ordinal);
        if(overrides.Any(x=>x==null||x.Source==null||!names.Contains(x.Source)||x.Target!=null&&!destinations.Contains(x.Target)) || overrides.Select(x=>x.Source).Distinct(StringComparer.Ordinal).Count()!=overrides.Count || overrides.Where(x=>x.Target!=null).Select(x=>x.Target).Distinct(StringComparer.Ordinal).Count()!=overrides.Count(x=>x.Target!=null)) throw new VerificationException(Reason.InvalidInput);
        var manual=overrides.ToDictionary(x=>x.Source!,StringComparer.Ordinal);
        var reserved=overrides.Where(x=>x.Target!=null).Select(x=>x.Target!).ToHashSet(StringComparer.Ordinal);
        var used=new HashSet<string>(StringComparer.Ordinal);var result=new List<ColumnMapping>();
        foreach(var column in source.Columns)
        {
            var destination=manual.TryGetValue(column.Name,out var mapping)?mapping.Target:!reserved.Contains(column.Name)&&destinations.Contains(column.Name)?column.Name:null;
            if(destination!=null && !used.Add(destination)) throw new VerificationException(Reason.InvalidInput);
            result.Add(new(column.Name,destination));
        }
        foreach(var column in target?.Columns??[])if(!used.Contains(column.Name))result.Add(new(null,column.Name));
        return result;
    }
    private static void ValidateInventory(Inventory inventory)
    {
        if(inventory.Tables.Select(t=>t.Qualified).Distinct(StringComparer.Ordinal).Count()!=inventory.Tables.Count || inventory.Tables.Any(t=>string.IsNullOrWhiteSpace(t.Schema)||string.IsNullOrWhiteSpace(t.Name)||t.Columns.Select(c=>c.Name).Distinct(StringComparer.Ordinal).Count()!=t.Columns.Count||t.Columns.Select(c=>c.Position).Distinct().Count()!=t.Columns.Count)) throw new VerificationException(Reason.AmbiguousScope);
    }
}
