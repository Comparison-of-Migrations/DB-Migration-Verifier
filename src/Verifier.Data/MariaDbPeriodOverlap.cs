using Verifier.Core;
namespace Verifier.Data;

internal static class MariaDbPeriodOverlap
{
    private static string Q(string name)=>"`"+name.Replace("`","``")+"`";
    // Group equal native keys/intervals first. Multiple CTE references may get
    // different ROW_NUMBER orderings in MariaDB, so ordinals cannot identify
    // unordered pairs. Native endpoint order distinguishes groups; multiplicity
    // accounts exactly for both within-group and cross-group pairs.
    internal static string Query(string schema,string table,IReadOnlyList<(string Column,int? Prefix)> keys,string start,string end,string? currentEnd=null,string currentMarker="TIMESTAMP'2038-01-19 03:14:07.999999'")
    {
        if(keys.Any(k=>k.Prefix is <=0))throw new VerificationException(Reason.InvalidInput);
        var expressions=keys.Select(k=>k.Prefix is{} n?"LEFT("+Q(k.Column)+","+n.ToString(System.Globalization.CultureInfo.InvariantCulture)+")":Q(k.Column)).ToArray();
        var projection=string.Join(',',expressions.Select((e,i)=>e+" k"+i));var group=string.Join(',',expressions.Append(Q(start)).Append(Q(end)));
        if(currentMarker is not ("TIMESTAMP'2038-01-19 03:14:07.999999'" or "18446744073709551615"))throw new VerificationException(Reason.InvalidInput);
        var present=string.Join(" AND ",expressions.Select(e=>e+" IS NOT NULL").Append(Q(start)+"<"+Q(end)).Concat(currentEnd==null?[]:new[]{Q(currentEnd)+"="+currentMarker}));
        var same=string.Concat(Enumerable.Range(0,keys.Count).Select(i=>"a.k"+i+"=b.k"+i+" AND "));
        return "WITH periods AS (SELECT "+(projection.Length>0?projection+",":"")+Q(start)+" s,"+Q(end)+" e,COUNT(*) n FROM "+Q(schema)+"."+Q(table)+(currentEnd==null?"":" FOR SYSTEM_TIME ALL")+" WHERE "+present+" GROUP BY "+group+") SELECT COALESCE((SELECT SUM(CAST(n AS DECIMAL(65,0))*(n-1)/2) FROM periods),0)+COALESCE((SELECT SUM(CAST(a.n AS DECIMAL(65,0))*b.n) FROM periods a JOIN periods b ON "+same+"(a.s<b.s OR (a.s=b.s AND a.e<b.e)) AND a.s<b.e AND b.s<a.e),0)";
    }
}
