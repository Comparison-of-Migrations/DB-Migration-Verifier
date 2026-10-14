using System.Data;
using System.Globalization;
using System.Security.Cryptography;
using System.Text.Json;
using MySqlConnector;
using Verifier.Core;
namespace Verifier.Data;

// MySQL 8.4 and MariaDB 11.4 use native collectors/identity separately. Only
// parameter-bound InnoDB trial operations share this implementation.
public sealed partial class MySqlPreparedTrial:IPreparedTrial
{
    private readonly ConnectionSettings settings;
    private readonly string sourceIdentity,targetIdentity,identity;
    private readonly Table table;
    private readonly IReadOnlyList<string> key;
    private readonly IReadOnlyList<MySqlPreparedTrial> parents;
    private readonly string signature;
    private string nativeRelation="";
    private readonly CatalogSection? allocator;
    public Guid Id {get;}=Guid.NewGuid();
    public DbKind Kind=>settings.Kind;
    public string Table=>table.Qualified;
    public Verdict TargetStructure {get;}
    public IReadOnlyList<TrialColumn> Columns {get;}
    public TrialDraft Draft {get;}
    public IReadOnlyList<IPreparedTrial> Dependencies {get;}
    public override string ToString()=>"MySqlPreparedTrial (connection, values and identity redacted)";
    private MySqlPreparedTrial(ConnectionSettings settings,Inventory source,Inventory target,Inventory actual,Table table,IReadOnlyList<MySqlPreparedTrial> parents,IReadOnlyDictionary<string,string?> fixedValues,IEnumerable<TrialInputIssue> issues,IReadOnlyList<(string Column,string Referenced)> selfReferences)
    {
        this.settings=settings;sourceIdentity=source.Identity;targetIdentity=target.Identity;identity=actual.Identity;this.table=table;this.parents=parents;
        Dependencies=parents.Cast<IPreparedTrial>().ToArray();signature=Definition(table);allocator=table.Catalog.SingleOrDefault(s=>s.CheckId=="OBJECT-02");
        key=table.RowKeys.FirstOrDefault(k=>k.Count>0&&k.All(n=>table.Columns.Any(c=>c.Name==n&&!c.Nullable&&MySqlTrialValue.Supports(c)&&!MySqlTrialGeometry.Is(c))))??[];
        var metadata=table.Catalog.SingleOrDefault(s=>s.CheckId=="SCHEMA-01");
        bool Generated(Column c)
        {
            var fact=metadata?.Facts.SingleOrDefault(f=>f.Column==c.Name&&f.Attribute=="ColumnGenerated");if(fact?.Value==null)return false;
            var parts=JsonSerializer.Deserialize<string?[]>(fact.Value)??throw new VerificationException(Reason.ObservationChanged);
            return parts.Length>1&&!string.IsNullOrEmpty(parts[1])||parts.FirstOrDefault()?.Contains("ROW START",StringComparison.OrdinalIgnoreCase)==true||parts.FirstOrDefault()?.Contains("ROW END",StringComparison.OrdinalIgnoreCase)==true;
        }
        bool Default(Column c)=>metadata?.Facts.Any(f=>f.Column==c.Name&&(f.Attribute=="ColumnIdentity"&&f.Value!=null||f.Attribute=="ColumnDefault"&&f.Value!=null))==true;
        var insert=new Dictionary<string,string?>(StringComparer.Ordinal);var pending=issues.ToList();
        foreach(var c in table.Columns.Where(c=>!Generated(c)))
        {
            if(fixedValues.TryGetValue(c.Name,out var parentValue)){insert[c.Name]=parentValue;continue;}
            if(!MySqlTrialValue.Supports(c)){if(!c.Nullable&&!Default(c))pending.Add(new(c.Name,"InputType",Reason.UnsupportedType));continue;}
            var value=Suggestion(c);if(value!=null)insert[c.Name]=value;else if(c.Nullable)insert[c.Name]=null;else if(!Default(c))pending.Add(new(c.Name,"InputValue",Reason.InvalidInput));
        }
        // Native self-FK INSERT can refer to its own confirmed row. Reuse the
        // proposed referenced value, never invent a different parent row/ID.
        foreach(var reference in selfReferences)
        {
            if(!insert.TryGetValue(reference.Referenced,out var value)||value==null){pending.Add(new(reference.Column,"SelfForeignKeyInput",Reason.InvalidInput));continue;}
            if(fixedValues.TryGetValue(reference.Column,out var fixedValue)&&fixedValue!=value){pending.Add(new(reference.Column,"ForeignKeyConflict",Reason.InvalidInput));continue;}
            insert[reference.Column]=value;
        }
        var update=new Dictionary<string,string?>(StringComparer.Ordinal);var editable=table.Columns.FirstOrDefault(c=>!Generated(c)&&MySqlTrialValue.Supports(c)&&!key.Contains(c.Name)&&!fixedValues.ContainsKey(c.Name)&&!selfReferences.Any(r=>r.Column==c.Name||r.Referenced==c.Name)&&insert.ContainsKey(c.Name));
        if(editable!=null){var value=Suggestion(editable);if(value!=insert[editable.Name])update[editable.Name]=value;else if(MySqlTrialGeometry.Is(editable))update[editable.Name]=MySqlTrialGeometry.Sample(editable,true);else if(JsonAlias(editable))update[editable.Name]="[]";else if(MySqlTrialValue.Base(editable) is "char" or "varchar" or "text" or "longtext" or "tinytext" or "mediumtext")update[editable.Name]="B";else if(MySqlTrialValue.Base(editable)=="bit")update[editable.Name]="0b0";else if(MySqlTrialValue.Base(editable)=="json")update[editable.Name]="[]";else if(MySqlTrialValue.Base(editable)=="inet4")update[editable.Name]="192.0.2.2";else if(MySqlTrialValue.Base(editable)=="inet6")update[editable.Name]="2001:db8::2";}
        if(update.Count==0)pending.Add(new(null,"UpdateValue",Reason.InvalidInput));
        Draft=new(insert,update,pending);Columns=table.Columns.Select(c=>new TrialColumn(c.Name,c.Type,c.Nullable,Generated(c),Default(c),insert.GetValueOrDefault(c.Name),!Generated(c)&&!Default(c)&&!insert.ContainsKey(c.Name)){InputFormat=MySqlTrialGeometry.Is(c)?"geometry-wkb":JsonAlias(c)?"json-text":MySqlTrialValue.Binary(c)?"hex":MySqlTrialValue.Base(c)}).ToArray();
        var expected=target.Tables.SingleOrDefault(t=>t.Name==table.Name);TargetStructure=expected==null?Verdict.Different:Verdict.Equal;
        if(expected!=null)
        {
            var scope=new MatchedScope(expected,table,ScopeMatching.ResolveColumns(expected,table,[]));
            foreach(var check in Checks.Definitions.Where(d=>d.Category is Category.Schema or Category.Access or Category.Objects))
            {var compared=CatalogComparison.Compare(check.Id,scope,target.Complete&&actual.Complete);if(compared.Findings.Count>0)TargetStructure=Verdict.Different;else if(compared.Reason!=Reason.None&&TargetStructure==Verdict.Equal)TargetStructure=Verdict.Indeterminate;}
        }
    }
    public static async Task<MySqlPreparedTrial> PrepareAsync(ConnectionSettings settings,Inventory source,Inventory target,string requested,CancellationToken token)
    {
        if(settings.Kind is not(DbKind.MySql or DbKind.MariaDb)||string.IsNullOrWhiteSpace(source.Identity)||string.IsNullOrWhiteSpace(target.Identity)||!source.DatabaseIsNamespace||!target.DatabaseIsNamespace)throw new VerificationException(Reason.InvalidInput);
        await using var observer=DatabaseFactory.Create(settings);var actual=await observer.InspectAsync(token);
        if(actual.Identity==source.Identity||actual.Identity==target.Identity)throw new VerificationException(Reason.SameDatabase);
        var parts=ScopeMatching.QualifiedParts(requested);if(parts==null)throw new VerificationException(Reason.InvalidInput);
        var root=actual.Tables.SingleOrDefault(t=>t.Name==parts[1])??throw new VerificationException(Reason.InvalidInput);
        var built=new Dictionary<string,MySqlPreparedTrial>(StringComparer.Ordinal);var visiting=new HashSet<string>(StringComparer.Ordinal);
        await using var connection=new MySqlConnection(settings.MySqlString());await connection.OpenAsync(token);await using var tx=await connection.BeginTransactionAsync(IsolationLevel.RepeatableRead,true,token);
        return await Visit(root);
        async Task<MySqlPreparedTrial> Visit(Table table)
        {
            if(built.TryGetValue(table.Qualified,out var prior))return prior;token.ThrowIfCancellationRequested();
            if(!table.SafeRead)throw new VerificationException(table.ReadReason);
            var kind=table.Catalog.SelectMany(s=>s.Facts).SingleOrDefault(f=>f.Attribute=="TableKind")?.Value;
            if(kind!="\"BASE TABLE\"")throw new VerificationException(Reason.NotImplemented);
            visiting.Add(table.Qualified);var parentList=new List<MySqlPreparedTrial>();var fixedValues=new Dictionary<string,string?>(StringComparer.Ordinal);var issues=new List<TrialInputIssue>();var selfReferences=new List<(string Column,string Referenced)>();
            var references=new List<string[]>();await using(var query=new MySqlCommand("SELECT CONSTRAINT_NAME,COLUMN_NAME,REFERENCED_TABLE_SCHEMA,REFERENCED_TABLE_NAME,REFERENCED_COLUMN_NAME FROM information_schema.KEY_COLUMN_USAGE WHERE CAST(TABLE_SCHEMA AS BINARY)=CAST(@schema AS BINARY) AND CAST(TABLE_NAME AS BINARY)=CAST(@table AS BINARY) AND REFERENCED_TABLE_NAME IS NOT NULL ORDER BY CAST(CONSTRAINT_NAME AS BINARY),ORDINAL_POSITION",connection,tx))
            {query.Parameters.AddWithValue("schema",table.Schema);query.Parameters.AddWithValue("table",table.Name);await using var reader=await query.ExecuteReaderAsync(token);while(await reader.ReadAsync(token))references.Add(Enumerable.Range(0,5).Select(reader.GetString).ToArray());}
            foreach(var group in references.GroupBy(r=>r[0],StringComparer.Ordinal))
            {
                var first=group.First();var parent=actual.Tables.SingleOrDefault(t=>t.Schema==first[2]&&t.Name==first[3]);
                if(parent==null){issues.Add(new(null,"ForeignKeyInput",Reason.VisibilityUnknown));continue;}
                if(parent.Qualified==table.Qualified){selfReferences.AddRange(group.Select(r=>(r[1],r[4])));continue;}
                if(visiting.Contains(parent.Qualified)){issues.Add(new(null,"ForeignKeyCycle",Reason.NotImplemented));continue;}
                var p=await Visit(parent);parentList.AddRange(p.parents);parentList.Add(p);
                foreach(var reference in group)
                {
                    if(!p.Draft.Insert.TryGetValue(reference[4],out var value)){issues.Add(new(reference[1],"ForeignKeyInput",Reason.InvalidInput));continue;}
                    if(fixedValues.TryGetValue(reference[1],out var old)&&old!=value){issues.Add(new(reference[1],"ForeignKeyConflict",Reason.InvalidInput));continue;}
                    fixedValues[reference[1]]=value;
                }
            }
            await using(var q=new MySqlCommand("SELECT 1 FROM "+MySqlTrialValue.Quote(table.Schema)+"."+MySqlTrialValue.Quote(table.Name)+" LIMIT 0",connection,tx)){await using var r=await q.ExecuteReaderAsync(token);if(await r.ReadAsync(token))throw new VerificationException(Reason.ObservationChanged);}
            var prepared=new MySqlPreparedTrial(settings,source,target,actual,table,parentList.DistinctBy(p=>p.Id).ToArray(),fixedValues,issues,selfReferences);prepared.nativeRelation=await prepared.RelationIdentity(connection,tx,token);built.Add(table.Qualified,prepared);visiting.Remove(table.Qualified);return prepared;
        }
    }
    private string? Suggestion(Column column)
    {
        var type=MySqlTrialValue.Base(column);
        if(MySqlTrialGeometry.Is(column))return MySqlTrialGeometry.Sample(column);
        if(JsonAlias(column))return "{}";
        if(type is "tinyint" or "smallint" or "mediumint" or "int" or "bigint")return RandomNumberGenerator.GetInt32(1,type=="tinyint"?100:type=="smallint"?30000:1000000).ToString(CultureInfo.InvariantCulture);
        if(type=="json")return "{}";
        if(type=="bit")return "0b1";
        if(type=="uuid")return Guid.NewGuid().ToString("D");
        if(type=="inet4")return "192.0.2.1";
        if(type=="inet6")return "2001:db8::1";
        if(type is "decimal" or "float" or "double")return "1";
        if(type=="year")return "2026";
        if(type is "char" or "varchar" or "tinytext" or "text" or "mediumtext" or "longtext")return "A";
        if(type=="binary"){var m=System.Text.RegularExpressions.Regex.Match(column.Type,@"^binary\(([0-9]+)\)$");if(m.Success&&int.TryParse(m.Groups[1].Value,out var n)&&n is >=0 and <=255)return n==0?"0x":"0x41"+new string('0',(n-1)*2);}
        if(MySqlTrialValue.Binary(column))return "0x41";
        if(type=="date")return "2026-01-02";
        if(type is "datetime" or "timestamp")return "2026-01-02 03:04:05";
        if(type=="time")return "01:02:03";return null;
    }
    private bool JsonAlias(Column column)
    {
        if(Kind!=DbKind.MariaDb||MySqlTrialValue.Base(column) is not("longtext" or "text"))return false;
        foreach(var fact in table.Catalog.Where(s=>s.CheckId=="SCHEMA-02").SelectMany(s=>s.Facts).Where(f=>f.Attribute=="ConstraintDefinition"&&f.Value!=null))
        {
            using var document=JsonDocument.Parse(fact.Value!);
            if(!document.RootElement.TryGetProperty("check",out var checks))continue;
            foreach(var row in checks.EnumerateArray())
                if(row.GetArrayLength()==2&&row[1].ValueKind==JsonValueKind.String&&System.Text.RegularExpressions.Regex.IsMatch(row[1].GetString()!,@"^json_valid\(\s*"+System.Text.RegularExpressions.Regex.Escape(MySqlTrialValue.Quote(column.Name))+@"\s*\)$",System.Text.RegularExpressions.RegexOptions.IgnoreCase|System.Text.RegularExpressions.RegexOptions.CultureInvariant))return true;
        }
        return false;
    }
    private static string Definition(Table table)=>JsonSerializer.Serialize(new{table.Columns,table.SafeRead,table.ReadReason,table.RowKeys,Catalog=table.Catalog.Select(s=>new{s.CheckId,s.Reason,s.CompleteExceptGaps,s.Gaps,Facts=s.Facts.Where(f=>f.Attribute is not("SequenceState" or "AutoIncrementState")).ToArray()}).ToArray()});
    private IEnumerable<MySqlPreparedTrial> Scope(ConfirmedTrialPlan plan)=>parents.Where(p=>plan.Dependencies.ContainsKey(p.Id)).Append(this);
    public void ValidateConfirmedPlan(ConfirmedTrialPlan plan)
    {
        if(plan.RoleHost!=null&&(Kind!=DbKind.MySql||plan.Role==null||plan.RoleHost.Length==0))throw new VerificationException(Reason.InvalidInput);
        if(plan.PreparationId!=Id||plan.Update.Count==0||plan.Insert.Count==0||plan.Role is{Length:0}||plan.Dependencies.Keys.Except(parents.Select(p=>p.Id)).Any())throw new VerificationException(Reason.InvalidInput);
        foreach(var scope in Scope(plan))
        {
            var values=scope==this?plan.Insert:plan.Dependencies[scope.Id];scope.ValidateValues(values);
            if(scope.key.Any(k=>!values.TryGetValue(k,out var value)||value==null))throw new VerificationException(Reason.InvalidInput);
            if(scope.Columns.Any(c=>!c.Generated&&!c.HasDefault&&!c.Nullable&&!values.ContainsKey(c.Name)))throw new VerificationException(Reason.InvalidInput);
        }
        ValidateValues(plan.Update);if(plan.RejectedInput!=null)ValidateValues(plan.RejectedInput,true);
        if(plan.RejectedInput==null){if(plan.RejectedStage!=null||plan.ExpectedSqlState!=null||plan.ExpectedConstraint!=null||plan.ExpectedNativeError!=null)throw new VerificationException(Reason.InvalidInput);}
        else if(plan.RejectedStage is not(TrialStage.RejectInsert or TrialStage.RejectUpdate)||plan.ExpectedSqlState is not("23000" or "22001" or "22003" or "22007" or "22018" or "22032" or "HY000")||plan.ExpectedNativeError is not(1048 or 1062 or 1451 or 1452 or 3819 or 4025 or 1406 or 1264 or 1292 or 1366 or 3140))throw new VerificationException(Reason.InvalidInput);
    }
    private void ValidateValues(IReadOnlyDictionary<string,string?> values,bool rejection=false)
    {
        foreach(var (name,value) in values)
        {var column=table.Columns.SingleOrDefault(c=>c.Name==name)??throw new VerificationException(Reason.InvalidInput);if(Columns.Single(c=>c.Name==name).Generated)throw new VerificationException(Reason.InvalidInput);if(!MySqlTrialValue.Supports(column))throw new VerificationException(Reason.UnsupportedType);if(!rejection)_=MySqlTrialValue.Parameter("@value",column,value);}
    }
}
