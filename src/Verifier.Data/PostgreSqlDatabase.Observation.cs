using System.Text.Json;
using Verifier.Core;

namespace Verifier.Data;

public sealed partial class PostgreSqlDatabase
{
    public async Task<IReadOnlyList<CatalogObservationLimit>> InspectCatalogChangesAsync(IReadOnlyList<Table> tables,IReadOnlySet<string> checks,bool databaseScope,CancellationToken token)
    {
        if(inventory==null||tables.Any(t=>!inventory.Tables.Contains(t)))throw new VerificationException(Reason.InvalidInput);
        var requested=checks.Where(c=>Checks.Definitions.Any(d=>d.Id==c&&d.Category is Category.Schema or Category.Access or Category.Objects)).ToHashSet(StringComparer.Ordinal);
        if(requested.Count==0||tables.Count==0&&!databaseScope)return [];
        var limits=new HashSet<CatalogObservationLimit>();
        try
        {
            // A separate authenticated, read-only snapshot observes current catalogs.
            // Never reconnect or replace the original data comparison transaction.
            await using var observer=new PostgreSqlDatabase(settings);
            var current=await observer.InspectAsync(token);
            if(current.Identity!=inventory.Identity)throw new VerificationException(Reason.ObservationChanged);
            foreach(var before in tables)
            {
                var after=current.Tables.SingleOrDefault(t=>t.Qualified==before.Qualified);
                var sameIdentity=relationIdentities.TryGetValue(before.Qualified,out var oldOid)&&observer.relationIdentities.TryGetValue(before.Qualified,out var newOid)&&oldOid==newOid;
                foreach(var check in requested)
                {
                    if(after==null)limits.Add(new(check,before.Qualified,current.Complete?Reason.ObservationChanged:Reason.VisibilityUnknown));
                    else if(!sameIdentity)limits.Add(new(check,before.Qualified,Reason.ObservationChanged));
                    else Compare(before.Catalog,after.Catalog,check,before.Qualified);
                }
            }
            if(databaseScope)
                foreach(var check in requested.Where(CatalogComparison.DatabaseChecks.Contains))
                    Compare(inventory.DatabaseCatalog,current.DatabaseCatalog,check,null);
        }
        catch(VerificationException e)
        {
            foreach(var check in requested)
            {
                foreach(var table in tables)limits.Add(new(check,table.Qualified,e.Reason));
                if(databaseScope&&CatalogComparison.DatabaseChecks.Contains(check))limits.Add(new(check,null,e.Reason));
            }
        }
        return limits.ToArray();

        void Compare(IReadOnlyList<CatalogSection> before,IReadOnlyList<CatalogSection> after,string check,string? location)
        {
            var old=before.SingleOrDefault(c=>c.CheckId==check);var fresh=after.SingleOrDefault(c=>c.CheckId==check);
            if(old==null&&fresh==null)return; // The ordinary comparison still reports unimplemented collectors.
            if(fresh==null){limits.Add(new(check,location,Reason.VisibilityUnknown));return;}
            if(old!=null&&Snapshot(old)==Snapshot(fresh))return;
            limits.Add(new(check,location,fresh.Reason!=Reason.None&&!fresh.CompleteExceptGaps?fresh.Reason:Reason.ObservationChanged));
        }
    }

    // Ordering is not a catalog change. Exact native strings and scoped gaps are.
    private static string Snapshot(CatalogSection section)=>JsonSerializer.Serialize(new
    {
        section.Reason,section.CompleteExceptGaps,
        facts=section.Facts.OrderBy(f=>f.Key,StringComparer.Ordinal).ThenBy(f=>f.Column,StringComparer.Ordinal).ThenBy(f=>f.Attribute,StringComparer.Ordinal).ThenBy(f=>f.Value,StringComparer.Ordinal),
        gaps=section.Gaps.OrderBy(g=>g.Key,StringComparer.Ordinal).ThenBy(g=>g.Column,StringComparer.Ordinal).ThenBy(g=>g.Attribute,StringComparer.Ordinal).ThenBy(g=>g.LocalEvidence,StringComparer.Ordinal).ThenBy(g=>g.Reason)
    });
}
