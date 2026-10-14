using MySqlConnector;
using Verifier.Core;
namespace Verifier.Data;

public sealed partial class MariaDbDatabase
{
    public async Task<IntegrityObservation> InspectIntegrityAsync(Table table,CancellationToken token)
    {
        if(inventory==null||!inventory.Tables.Contains(table))throw new VerificationException(Reason.InvalidInput);
        var violations=new List<IntegrityViolation>();var issues=new List<ScopeIssue>();
        if(!table.SafeRead)return new(violations,[new(table.Qualified,null,table.ReadReason)]);
        await ValidateDefinition(table,token);
        var relation=CurrentRelation(table);
        try
        {
            violations.AddRange(await MissingTransactionRecords(table,token));
            foreach(var c in table.Columns.Where(c=>!c.SafeValue))issues.Add(new(table.Qualified,c.Name,Reason.UnsupportedType));
            foreach(var c in table.Columns.Where(c=>!c.Nullable&&c.SafeValue))await Count("NotNull",c.Name,"SELECT COUNT(*) FROM "+relation+" WHERE "+Quote(c.Name)+" IS NULL");
            var stats=await Native("SELECT INDEX_NAME,COLUMN_NAME,SUB_PART,NULL FROM information_schema.STATISTICS WHERE "+TableFilter+" AND NON_UNIQUE=0 ORDER BY CAST(INDEX_NAME AS BINARY),SEQ_IN_INDEX",table,token);
            var periods=await Native("SELECT `PERIOD`,START_COLUMN_NAME,END_COLUMN_NAME FROM information_schema.PERIODS WHERE "+TableFilter,table,token);
            var periodKeys=await Native("SELECT CONSTRAINT_NAME,PERIOD_NAME FROM information_schema.KEY_PERIOD_USAGE WHERE "+TableFilter,table,token);
            foreach(var key in periodKeys)
            {
                var period=periods.SingleOrDefault(p=>p[0]==key[1]);var rows=stats.Where(s=>s[0]==key[0]).ToArray();
                if(period==null||rows.Length<2||rows[^2][1]!=period[2]||rows[^1][1]!=period[1]||rows.TakeLast(2).Any(r=>r[2]!=null))
                {issues.Add(new(table.Qualified,null,Reason.VisibilityUnknown));continue;}
                if(rows.Any(r=>r[1]==null||!table.Columns.Any(c=>c.Name==r[1]&&c.SafeValue))||period.Skip(1).Any(n=>!table.Columns.Any(c=>c.Name==n&&c.SafeValue&&c.Type.Split('(')[0] is "date" or "datetime" or "timestamp")))
                {issues.Add(new(table.Qualified,null,Reason.UnsupportedType));continue;}
                var keys=new List<(string Column,int? Prefix)>();var supported=true;
                foreach(var r in rows.SkipLast(2)){int? prefix=null;if(r[2]!=null){if(!int.TryParse(r[2],out var n)||n<=0){supported=false;break;}prefix=n;}keys.Add((r[1]!,prefix));}
                if(!supported){issues.Add(new(table.Qualified,null,Reason.UnsupportedType));continue;}
                await Count("Exclusion",null,MariaDbPeriodOverlap.Query(table.Schema,table.Name,keys,period[1]!,period[2]!,CurrentEnd(table),CurrentEndLiteral(table)));
            }
            foreach(var group in stats.Where(s=>!periodKeys.Any(k=>k[0]==s[0])).GroupBy(x=>x[0]!,StringComparer.Ordinal))
            {
                var rows=group.ToArray();var expressions=new List<string>();var supported=true;
                foreach(var row in rows)
                {
                    if(row[1]!=null)
                    {
                        if(!table.Columns.Any(c=>c.Name==row[1]&&c.SafeValue)){supported=false;break;}
                        var expression=Quote(row[1]!);
                        if(row[2]!=null)
                        {if(!int.TryParse(row[2],out var prefix)||prefix<=0){supported=false;break;}expression="LEFT("+expression+","+prefix.ToString(System.Globalization.CultureInfo.InvariantCulture)+")";}
                        expressions.Add(expression);
                    }
                    else{supported=false;break;}
                }
                if(!supported||expressions.Count==0){issues.Add(new(table.Qualified,null,Reason.UnsupportedType));continue;}
                var present=string.Join(" AND ",expressions.Select(x=>x+" IS NOT NULL"));
                await Count("Unique",rows.Length==1?rows[0][1]:null,"SELECT COALESCE(SUM(n-1),0) FROM (SELECT COUNT(*) n FROM "+relation+" WHERE "+present+" GROUP BY "+string.Join(',',expressions)+" HAVING COUNT(*)>1) duplicate_groups");
            }
            var refs=await Native("SELECT CONSTRAINT_NAME,COLUMN_NAME,REFERENCED_TABLE_SCHEMA,REFERENCED_TABLE_NAME,REFERENCED_COLUMN_NAME FROM information_schema.KEY_COLUMN_USAGE WHERE "+TableFilter+" AND REFERENCED_TABLE_NAME IS NOT NULL ORDER BY CAST(CONSTRAINT_NAME AS BINARY),ORDINAL_POSITION",table,token);
            var foreignNames=await Native("SELECT CONSTRAINT_NAME FROM information_schema.TABLE_CONSTRAINTS WHERE "+TableFilter+" AND CONSTRAINT_TYPE='FOREIGN KEY'",table,token);
            if(foreignNames.Any(x=>!refs.Any(r=>r[0]==x[0])))issues.Add(new(table.Qualified,null,Reason.VisibilityUnknown));
            foreach(var group in refs.GroupBy(x=>x[0]!,StringComparer.Ordinal))
            {
                var rows=group.ToArray();var first=rows[0];var parent=inventory.Tables.SingleOrDefault(t=>t.Schema==first[2]&&t.Name==first[3]);
                if(parent==null){issues.Add(new(table.Qualified,null,Reason.VisibilityUnknown));continue;}
                if(!parent.SafeRead){issues.Add(new(table.Qualified,null,parent.ReadReason));continue;}
                if(rows.Any(x=>x[2]!=parent.Schema||x[3]!=parent.Name||!table.Columns.Any(c=>c.Name==x[1]&&c.SafeValue)||!parent.Columns.Any(c=>c.Name==x[4]&&c.SafeValue)))
                {issues.Add(new(table.Qualified,null,Reason.UnsupportedType));continue;}
                await ValidateDefinition(parent,token);
                // MariaDB exempts any composite FK containing NULL. Native column
                // equality preserves referenced collation and numeric semantics.
                var present=string.Join(" AND ",rows.Select(x=>"a."+Quote(x[1]!)+" IS NOT NULL"));var match=string.Join(" AND ",rows.Select(x=>"b."+Quote(x[4]!)+"=a."+Quote(x[1]!)));
                await Count("Reference",rows.Length==1?first[1]:null,"SELECT COUNT(*) FROM "+CurrentRelation(table,"a")+" WHERE "+present+" AND NOT EXISTS(SELECT 1 FROM "+CurrentRelation(parent,"b")+" WHERE "+match+")");
                await ValidateDefinition(parent,token);
            }
            var checks=await Native("SELECT CHECK_CLAUSE FROM information_schema.CHECK_CONSTRAINTS WHERE CAST(CONSTRAINT_SCHEMA AS BINARY)=CAST(@schema AS BINARY) AND CAST(TABLE_NAME AS BINARY)=CAST(@table AS BINARY) ORDER BY CAST(CONSTRAINT_NAME AS BINARY)",table,token);
            var safeColumns=table.Columns.Where(c=>c.SafeValue).Select(c=>c.Name).ToHashSet(StringComparer.Ordinal);
            foreach(var c in checks)
            {
                // MariaDB admits loadable UDFs. Evaluate only proven pure
                // scalar grammar, preserving the native expression verbatim.
                if(c[0]==null||!MariaDbExpression.Safe(c[0]!,safeColumns)){issues.Add(new(table.Qualified,null,Reason.NotImplemented));continue;}
                await Count("Check",null,"SELECT COUNT(*) FROM "+relation+" WHERE ("+c[0]+") IS FALSE");
            }
            var definition=table.Catalog.Single(c=>c.CheckId=="SCHEMA-02");
            if(definition.Reason!=Reason.None)issues.Add(new(table.Qualified,null,definition.Reason));
            await ValidateDefinition(table,token);
            return new(violations,issues.Distinct().ToArray());
        }
        catch(MySqlException e){throw Map(e);}
        async Task Count(string kind,string? column,string sql)
        {
            try
            {
                await using var q=Command(sql);var count=Convert.ToInt64(await q.ExecuteScalarAsync(token),System.Globalization.CultureInfo.InvariantCulture);
                if(count<0)throw new VerificationException(Reason.ObservationChanged);
                await CheckWarnings(token);
                if(count>0)violations.Add(new(kind,column,count));
            }
            catch(MySqlException e){issues.Add(new(table.Qualified,column,Map(e).Reason));}
            catch(VerificationException e){issues.Add(new(table.Qualified,column,e.Reason));}
            catch(OverflowException){issues.Add(new(table.Qualified,column,Reason.ObservationChanged));}
        }
    }
}
