using System.Globalization;
using Microsoft.Data.SqlClient;
using Verifier.Core;

namespace Verifier.Data;

public sealed partial class SqlServerDatabase
{
    public async Task<IntegrityObservation> InspectIntegrityAsync(Table table,CancellationToken token)
    {
        if(inventory==null||!inventory.Tables.Contains(table))throw new VerificationException(Reason.InvalidInput);
        var violations=new List<IntegrityViolation>();var issues=new List<ScopeIssue>();
        if(!table.SafeRead)return new(violations,[new(table.Qualified,null,table.ReadReason)]);
        try
        {
            await ValidateTable(table,token);
            foreach(var column in table.Columns)
            {
                if(!column.SafeValue){issues.Add(new(table.Qualified,column.Name,Reason.UnsupportedType));continue;}
                if(!column.Nullable)await Count("NotNull",column.Name,"SELECT COUNT_BIG(*) FROM "+ReadRelation(table,"t")+" WHERE t."+Quote(column.Name)+" IS NULL");
            }
            var indexes=await Native(KeyQuery,table,token);
            foreach(var group in indexes.Where(x=>x[1]=="True").GroupBy(x=>x[0],StringComparer.Ordinal))
            {
                var rows=group.ToArray();var first=rows[0];var parts=rows.Where(x=>x[9]=="False"&&int.TryParse(x[7],out var n)&&n>0).OrderBy(x=>int.Parse(x[7]!,CultureInfo.InvariantCulture)).ToArray();
                if(parts.Length==0||parts.Any(x=>!table.Columns.Any(c=>c.Name==x[6]&&c.SafeValue))){issues.Add(new(table.Qualified,null,Reason.UnsupportedType));continue;}
                var filter=first[4]=="True"?SqlServerCheckExpression.Bind(first[10],table.Columns):null;
                if(first[4]=="True"&&filter==null){issues.Add(new(table.Qualified,null,Reason.UnsafeObservation));continue;}
                var fields=string.Join(',',parts.Select(x=>"t."+Quote(x[6]!)));
                // SQL Server unique keys treat equal NULL combinations as
                // duplicates. Do not apply the MySQL NULL exemption here.
                await Count("Unique",parts.Length==1?parts[0][6]:null,"SELECT COALESCE(SUM(n-1),0) FROM (SELECT COUNT_BIG(*) n FROM "+ReadRelation(table,"t")+(filter==null?"":" WHERE ("+filter+")")+" GROUP BY "+fields+" HAVING COUNT_BIG(*)>1) d");
            }
            var references=await Native(ForeignQuery,table,token);
            foreach(var group in references.GroupBy(x=>x[0]!,StringComparer.Ordinal))
            {
                var rows=group.ToArray();var first=rows[0];var parent=inventory.Tables.SingleOrDefault(t=>t.Schema==first[2]&&t.Name==first[3]);
                if(parent==null){issues.Add(new(table.Qualified,null,Reason.VisibilityUnknown));continue;}
                if(!parent.SafeRead){issues.Add(new(table.Qualified,null,parent.ReadReason));continue;}
                if(rows.Any(x=>x[2]!=parent.Schema||x[3]!=parent.Name||!table.Columns.Any(c=>c.Name==x[1]&&c.SafeValue)||!parent.Columns.Any(c=>c.Name==x[4]&&c.SafeValue)))
                {issues.Add(new(table.Qualified,null,Reason.UnsupportedType));continue;}
                await ValidateTable(parent,token);
                var present=string.Join(" AND ",rows.Select(x=>"t."+Quote(x[1]!)+" IS NOT NULL"));
                var match=string.Join(" AND ",rows.Select(x=>"p."+Quote(x[4]!)+"=t."+Quote(x[1]!)));
                await Count("Reference",rows.Length==1?first[1]:null,"SELECT COUNT_BIG(*) FROM "+ReadRelation(table,"t")+" WHERE "+present+" AND NOT EXISTS(SELECT 1 FROM "+ReadRelation(parent,"p")+" WHERE "+match+")");
                await ValidateTable(parent,token);
            }
            var checks=await Native("SELECT CONVERT(varbinary(max),CONVERT(nvarchar(max),definition)) AS [__dbmv_utf16] FROM sys.check_constraints WHERE parent_object_id="+ObjectFilter+" ORDER BY name",table,token);
            foreach(var check in checks)
            {
                var bound=SqlServerCheckExpression.Bind(check[0],table.Columns);
                if(bound==null){issues.Add(new(table.Qualified,null,Reason.UnsafeObservation));continue;}
                // FALSE violates CHECK; UNKNOWN (SQL NULL) satisfies it.
                await Count("Check",null,"SELECT COUNT_BIG(*) FROM "+ReadRelation(table,"t")+" WHERE NOT ("+bound+")");
            }
            await ValidateTable(table,token);
            return new(violations,issues.Distinct().ToArray());
        }
        catch(SqlException e){throw Map(e,token);}
        async Task Count(string kind,string? column,string sql)
        {
            try
            {
                await using var q=Command(sql);var count=Convert.ToInt64(await q.ExecuteScalarAsync(token),CultureInfo.InvariantCulture);
                if(count<0)throw new VerificationException(Reason.ObservationChanged);if(count>0)violations.Add(new(kind,column,count));
            }
            catch(SqlException e){issues.Add(new(table.Qualified,column,FaultReason(e,token)));}
        }
    }
}
