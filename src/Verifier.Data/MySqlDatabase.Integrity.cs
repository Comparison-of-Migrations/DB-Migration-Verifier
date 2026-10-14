using MySqlConnector;
using Verifier.Core;
namespace Verifier.Data;

public sealed partial class MySqlDatabase
{
    public async Task<IntegrityObservation> InspectIntegrityAsync(Table table,CancellationToken token)
    {
        if(inventory==null||!inventory.Tables.Contains(table))throw new VerificationException(Reason.InvalidInput);
        var violations=new List<IntegrityViolation>();var issues=new List<ScopeIssue>();
        if(!table.SafeRead)return new(violations,[new(table.Qualified,null,table.ReadReason)]);
        await ValidateDefinition(table,token);
        var relation=Quote(table.Schema)+"."+Quote(table.Name);
        try
        {
            foreach(var c in table.Columns.Where(c=>!c.SafeValue))issues.Add(new(table.Qualified,c.Name,Reason.UnsupportedType));
            // ST_SRID(g) reads the native ID even when its dictionary entry is
            // missing. Count missing references read-only; never transform values.
            foreach(var c in table.Columns.Where(Spatial))
                await Count("Reference",c.Name,"SELECT COUNT(*) FROM "+relation+" a LEFT JOIN information_schema.ST_SPATIAL_REFERENCE_SYSTEMS s ON s.SRS_ID=ST_SRID(a."+Quote(c.Name)+") WHERE a."+Quote(c.Name)+" IS NOT NULL AND s.SRS_ID IS NULL",true);
            foreach(var c in table.Columns.Where(c=>!c.Nullable&&c.SafeValue))await Count("NotNull",c.Name,"SELECT COUNT(*) FROM "+relation+" WHERE "+Quote(c.Name)+" IS NULL");
            var stats=await ReadNative("SELECT INDEX_NAME,COLUMN_NAME,SUB_PART,EXPRESSION FROM information_schema.STATISTICS WHERE "+TableFilter+" AND NON_UNIQUE=0 ORDER BY CAST(INDEX_NAME AS BINARY),SEQ_IN_INDEX",table,token);
            foreach(var group in stats.GroupBy(x=>x[0]!,StringComparer.Ordinal))
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
                    else if(!string.IsNullOrWhiteSpace(row[3])&&table.Columns.All(c=>c.SafeValue))expressions.Add("("+row[3]+")");
                    else{supported=false;break;}
                }
                if(!supported||expressions.Count==0){issues.Add(new(table.Qualified,null,Reason.UnsupportedType));continue;}
                var present=string.Join(" AND ",expressions.Select(x=>x+" IS NOT NULL"));
                await Count("Unique",rows.Length==1?rows[0][1]:null,"SELECT COALESCE(SUM(n-1),0) FROM (SELECT COUNT(*) n FROM "+relation+" WHERE "+present+" GROUP BY "+string.Join(',',expressions)+" HAVING COUNT(*)>1) duplicate_groups");
            }
            var refs=await ReadNative("SELECT CONSTRAINT_NAME,COLUMN_NAME,REFERENCED_TABLE_SCHEMA,REFERENCED_TABLE_NAME,REFERENCED_COLUMN_NAME FROM information_schema.KEY_COLUMN_USAGE WHERE "+TableFilter+" AND REFERENCED_TABLE_NAME IS NOT NULL ORDER BY CAST(CONSTRAINT_NAME AS BINARY),ORDINAL_POSITION",table,token);
            var foreignNames=await ReadNative("SELECT CONSTRAINT_NAME FROM information_schema.TABLE_CONSTRAINTS WHERE "+TableFilter+" AND CONSTRAINT_TYPE='FOREIGN KEY'",table,token);
            if(foreignNames.Any(x=>!refs.Any(r=>r[0]==x[0])))issues.Add(new(table.Qualified,null,Reason.VisibilityUnknown));
            foreach(var group in refs.GroupBy(x=>x[0]!,StringComparer.Ordinal))
            {
                var rows=group.ToArray();var first=rows[0];var parent=inventory.Tables.SingleOrDefault(t=>t.Schema==first[2]&&t.Name==first[3]);
                if(parent==null){issues.Add(new(table.Qualified,null,Reason.VisibilityUnknown));continue;}
                if(!parent.SafeRead){issues.Add(new(table.Qualified,null,parent.ReadReason));continue;}
                if(rows.Any(x=>x[2]!=parent.Schema||x[3]!=parent.Name||!table.Columns.Any(c=>c.Name==x[1]&&c.SafeValue)||!parent.Columns.Any(c=>c.Name==x[4]&&c.SafeValue)))
                {issues.Add(new(table.Qualified,null,Reason.UnsupportedType));continue;}
                await ValidateDefinition(parent,token);
                // MySQL exempts any composite FK containing NULL. Native column
                // equality preserves referenced collation and numeric semantics.
                var present=string.Join(" AND ",rows.Select(x=>"a."+Quote(x[1]!)+" IS NOT NULL"));var match=string.Join(" AND ",rows.Select(x=>"b."+Quote(x[4]!)+"=a."+Quote(x[1]!)));
                await Count("Reference",rows.Length==1?first[1]:null,"SELECT COUNT(*) FROM "+relation+" a WHERE "+present+" AND NOT EXISTS(SELECT 1 FROM "+Quote(parent.Schema)+"."+Quote(parent.Name)+" b WHERE "+match+")");
                await ValidateDefinition(parent,token);
            }
            var checks=await ReadNative("SELECT c.CHECK_CLAUSE FROM information_schema.CHECK_CONSTRAINTS c JOIN information_schema.TABLE_CONSTRAINTS t ON CAST(c.CONSTRAINT_SCHEMA AS BINARY)=CAST(t.CONSTRAINT_SCHEMA AS BINARY) AND CAST(c.CONSTRAINT_NAME AS BINARY)=CAST(t.CONSTRAINT_NAME AS BINARY) WHERE CAST(t.TABLE_SCHEMA AS BINARY)=CAST(@schema AS BINARY) AND CAST(t.TABLE_NAME AS BINARY)=CAST(@table AS BINARY) AND t.CONSTRAINT_TYPE='CHECK' ORDER BY CAST(c.CONSTRAINT_NAME AS BINARY)",table,token);
            foreach(var c in checks)
            {
                // Both ENFORCED and NOT ENFORCED are declared contracts. A NULL
                // result satisfies CHECK; only FALSE is a witnessed violation.
                // The server validated this native expression when defining it.
                if(string.IsNullOrWhiteSpace(c[0])||table.Columns.Any(x=>!x.SafeValue)){issues.Add(new(table.Qualified,null,Reason.UnsupportedType));continue;}
                await Count("Check",null,"SELECT COUNT(*) FROM "+relation+" WHERE ("+c[0]+") IS FALSE");
            }
            var definition=table.Catalog.Single(c=>c.CheckId=="SCHEMA-02");
            if(definition.Reason!=Reason.None)issues.Add(new(table.Qualified,null,definition.Reason));
            await ValidateDefinition(table,token);
            return new(violations,issues.Distinct().ToArray());
        }
        catch(MySqlException e){throw Map(e);}
        async Task Count(string kind,string? column,string sql,bool missingSrs=false)
        {
            try
            {
                await using var q=Command(sql);var count=Convert.ToInt64(await q.ExecuteScalarAsync(token),System.Globalization.CultureInfo.InvariantCulture);
                if(count<0)throw new VerificationException(Reason.ObservationChanged);
                if(missingSrs&&count>0)await CheckMissingSrsWarnings(token);else await CheckWarnings(token);
                if(count>0)violations.Add(new(kind,column,count));
            }
            catch(MySqlException e){issues.Add(new(table.Qualified,column,Map(e).Reason));}
            catch(VerificationException e){issues.Add(new(table.Qualified,column,e.Reason));}
            catch(OverflowException){issues.Add(new(table.Qualified,column,Reason.ObservationChanged));}
        }
    }
}
