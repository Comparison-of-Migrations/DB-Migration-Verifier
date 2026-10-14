using System.Data;
using System.Globalization;
using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.Data.SqlClient;
using Verifier.Core;
namespace Verifier.Data;

public sealed partial class SqlServerDatabase
{
    public static async Task<PreparedTrial> PrepareTrialAsync(ConnectionSettings settings,Inventory source,Inventory target,string requested,CancellationToken token)
    {
        if(settings.Kind!=DbKind.SqlServer||string.IsNullOrEmpty(source.Identity)||string.IsNullOrEmpty(target.Identity))throw new VerificationException(Reason.InvalidInput);
        await using var observer=new SqlServerDatabase(settings){preparationTable=requested};var actual=await observer.InspectAsync(token);
        if(actual.Identity==source.Identity||actual.Identity==target.Identity)throw new VerificationException(Reason.SameDatabase);
        var root=actual.Tables.SingleOrDefault(t=>t.Qualified==requested)??throw new VerificationException(Reason.InvalidInput);
        var built=new Dictionary<string,PreparedTrial>(StringComparer.Ordinal);var visiting=new HashSet<string>(StringComparer.Ordinal);
        return await Visit(root);
        async Task<PreparedTrial> Visit(Table table)
        {
            if(built.TryGetValue(table.Qualified,out var prior))return prior;
            // Trial receipts require exclusive table locks. Memory-optimized
            // tables support read snapshots but cannot provide those locks.
            if(observer.tableStore?.MemoryOptimized(table)==true)throw new VerificationException(Reason.NotImplemented);
            if(!table.SafeRead&&table.ReadReason!=Reason.RowFiltered)throw new VerificationException(table.ReadReason);
            visiting.Add(table.Qualified);var parents=new List<PreparedTrial>();var fixedValues=new Dictionary<string,string?>(StringComparer.Ordinal);var issues=new List<TrialInputIssue>();var references=new Dictionary<string,TrialGeneratedReference>(StringComparer.Ordinal);var self=new List<(string Column,string Referenced)>();var foreignColumns=new HashSet<string>(StringComparer.Ordinal);
            var edges=await observer.Native(ForeignQuery,table,token);
            foreach(var group in edges.GroupBy(r=>r[0],StringComparer.Ordinal))
            {
                foreach(var edge in group)foreignColumns.Add(edge[1]!);
                var first=group.First();var parent=actual.Tables.SingleOrDefault(t=>t.Schema==first[2]&&t.Name==first[3]);
                if(parent==null){issues.Add(new(null,"ForeignKeyInput",Reason.VisibilityUnknown));continue;}
                if(parent.Qualified==table.Qualified){self.AddRange(group.Select(r=>(r[1]!,r[4]!)));continue;}
                if(visiting.Contains(parent.Qualified)){issues.Add(new(null,"ForeignKeyCycle",Reason.NotImplemented));continue;}
                var p=await Visit(parent);parents.AddRange(p.Dependencies.Cast<PreparedTrial>());parents.Add(p);
                foreach(var edge in group)
                {
                    var hasValue=p.Draft.Insert.TryGetValue(edge[4]!,out var value)&&value!=null;
                    // A selected parent is inserted from confirmed inputs, not
                    // its preparation-time suggestions. Bind every unambiguous
                    // reference to that owned row, including non-generated keys.
                    if(SameReference(edge))
                    {
                        if(hasValue||p.CanBindGeneratedKey(edge[4]!))
                            references[edge[1]!]=new(p.Id,p.Table,edge[4]!,!hasValue);
                        else issues.Add(new(edge[1],"GeneratedParentKey",Reason.NotImplemented));
                        continue;
                    }
                    if(!hasValue){issues.Add(new(edge[1],"GeneratedParentKey",Reason.NotImplemented));continue;}
                    if(fixedValues.TryGetValue(edge[1]!,out var old)&&old!=value){issues.Add(new(edge[1],"ForeignKeyConflict",Reason.InvalidInput));continue;}
                    fixedValues[edge[1]!]=value;
                }
            }
            // Repeated constraints may describe the same binding. Coalesce only
            // the exact native parent/column identity; distinct parents still need
            // explicit values and native constraint validation.
            bool SameReference(string?[] edge)=>edges.Where(e=>e[1]==edge[1]).All(e=>e[2]==edge[2]&&e[3]==edge[3]&&e[4]==edge[4]);
            var prepared=new PreparedTrial(settings,source.Identity,target.Identity,actual.Identity,table,target,parents.DistinctBy(p=>p.Id).ToArray(),fixedValues,foreignColumns,self,references,issues,await observer.TrialRelationToken(table,token));
            built.Add(table.Qualified,prepared);visiting.Remove(table.Qualified);return prepared;
        }
    }
    private async Task<string> TrialRelationToken(Table table,CancellationToken token)
    {
        var rows=await Native("SELECT object_id,CONVERT(varchar(33),create_date,126),CONVERT(varchar(33),modify_date,126) FROM sys.tables WHERE object_id="+ObjectFilter,table,token);
        if(rows.Count!=1)throw new VerificationException(Reason.ObservationChanged);return SqlServerMetadata.Serialize(rows[0]);
    }
    public sealed partial class PreparedTrial:IPreparedTrial
    {
        private readonly ConnectionSettings settings;
        private readonly string sourceIdentity,targetIdentity,identity,signature,relationToken;
        private readonly Table table;
        private readonly IReadOnlyList<PreparedTrial> parents;
        private readonly IReadOnlyList<string> key;
        public Guid Id{get;}=Guid.NewGuid();
        public DbKind Kind=>DbKind.SqlServer;
        public string Table=>table.Qualified;
        public Verdict TargetStructure{get;}
        public IReadOnlyList<TrialColumn> Columns{get;}
        public TrialDraft Draft{get;}
        public IReadOnlyList<IPreparedTrial> Dependencies=>parents.Cast<IPreparedTrial>().ToArray();
        public override string ToString()=>"SqlServer PreparedTrial (inputs and connection redacted)";
        internal PreparedTrial(ConnectionSettings settings,string source,string target,string identity,Table table,Inventory expected,IReadOnlyList<PreparedTrial> parents,IReadOnlyDictionary<string,string?> fixedValues,IReadOnlySet<string> foreignColumns,IReadOnlyList<(string Column,string Referenced)> self,IReadOnlyDictionary<string,TrialGeneratedReference> references,IEnumerable<TrialInputIssue> issues,string relationToken)
        {
            this.settings=settings;sourceIdentity=source;targetIdentity=target;this.identity=identity;this.parents=parents;this.relationToken=relationToken;signature=Definition(table);
            // Preparation outlives its observer. Only allocator facts are needed
            // afterwards; detach those before the observer's catalog lease ends.
            this.table=table with{Columns=table.Columns.ToArray(),RowKeys=table.RowKeys.Select(key=>(IReadOnlyList<string>)key.ToArray()).ToArray(),Catalog=table.Catalog.Where(s=>s.CheckId=="OBJECT-02").Select(s=>s with{Facts=s.Facts.ToArray()}).ToArray()};
            key=table.RowKeys.FirstOrDefault(k=>k.Count>0&&k.All(n=>table.Columns.Any(c=>c.Name==n&&!c.Nullable&&SqlServerTrialValue.Supports(c)&&c.Type is not("sql_variant" or "geometry" or "geography" or "hierarchyid"))))??[];
            var insert=new Dictionary<string,string?>(StringComparer.Ordinal);var pending=issues.ToList();
            foreach(var c in table.Columns.Where(c=>!SqlServerTrialValue.Generated(c)))
            {
                if(references.ContainsKey(c.Name))continue;
                if(fixedValues.TryGetValue(c.Name,out var value)){insert[c.Name]=value;continue;}
                if(!SqlServerTrialValue.Supports(c)){if(!c.Nullable&&!SqlServerTrialValue.Default(c))pending.Add(new(c.Name,"InputType",Reason.UnsupportedType));continue;}
                insert[c.Name]=Suggestion(c,false);
            }
            foreach(var edge in self)
            {
                if(insert.TryGetValue(edge.Referenced,out var value)&&value!=null)insert[edge.Column]=value;
                else pending.Add(new(edge.Column,"GeneratedSelfReference",Reason.NotImplemented));
            }
            var update=new Dictionary<string,string?>(StringComparer.Ordinal);
            var editable=table.Columns.FirstOrDefault(c=>!SqlServerTrialValue.Generated(c)&&SqlServerTrialValue.Supports(c)&&!key.Contains(c.Name)&&!foreignColumns.Contains(c.Name)&&!self.Any(e=>e.Column==c.Name||e.Referenced==c.Name));
            if(editable!=null)update[editable.Name]=Suggestion(editable,true);else pending.Add(new(null,"UpdateValue",Reason.InvalidInput));
            Draft=new(insert,update,pending);
            Columns=table.Columns.Select(c=>new TrialColumn(c.Name,c.Type,c.Nullable,SqlServerTrialValue.Generated(c),SqlServerTrialValue.Default(c),insert.GetValueOrDefault(c.Name),!SqlServerTrialValue.Generated(c)&&!c.Nullable&&!SqlServerTrialValue.Default(c)&&!insert.ContainsKey(c.Name)&&!references.ContainsKey(c.Name)){GeneratedReference=references.GetValueOrDefault(c.Name),InputFormat=c.Type is "geometry" or "geography"?"sql-spatial-wkt":c.Type is "binary" or "varbinary" or "image"?"hex":c.Type}).ToArray();
            var targetTable=expected.Tables.SingleOrDefault(t=>t.Qualified==table.Qualified);TargetStructure=targetTable==null?Verdict.Different:Verdict.Equal;
            if(targetTable!=null)
            {
                var scope=new MatchedScope(targetTable,table,ScopeMatching.ResolveColumns(targetTable,table,[]));
                foreach(var check in Checks.Definitions.Where(c=>c.Category is Category.Schema or Category.Objects))
                {var compared=CatalogComparison.Compare(check.Id,scope,true);if(compared.Findings.Count>0)TargetStructure=Verdict.Different;else if(compared.Reason!=Reason.None&&TargetStructure==Verdict.Equal)TargetStructure=Verdict.Indeterminate;}
            }
        }
        private static string? Suggestion(Column c,bool updated)=>c.Type switch{
            "sql_variant"=>updated?"{\"type\":\"int\",\"value\":\"2\"}":"{\"type\":\"int\",\"value\":\"1\"}",
            "geometry"=>updated?"SRID=0;POINT (3 4)":"SRID=0;POINT (1 2)","geography"=>updated?"SRID=4326;POINT (3 4)":"SRID=4326;POINT (1 2)","hierarchyid"=>updated?"/2/":"/1/",
            "bit"=>updated?"0":"1","tinyint"=>RandomNumberGenerator.GetInt32(1,100).ToString(CultureInfo.InvariantCulture),
            "smallint"=>RandomNumberGenerator.GetInt32(1,30000).ToString(CultureInfo.InvariantCulture),
            "int" or "bigint"=>RandomNumberGenerator.GetInt32(1,1000000).ToString(CultureInfo.InvariantCulture),
            "decimal" or "numeric" or "money" or "smallmoney" or "real" or "float"=>updated?"2":"1",
            "uniqueidentifier"=>Guid.NewGuid().ToString("D"),"date"=>updated?"2026-10-13":"2026-10-12",
            "datetime" or "datetime2" or "smalldatetime"=>updated?"2026-10-13T00:00:00":"2026-10-12T00:00:00",
            "datetimeoffset"=>updated?"2026-10-13T00:00:00+00:00":"2026-10-12T00:00:00+00:00",
            "time"=>updated?"01:00:00":"00:00:00","binary" or "varbinary" or "image"=>updated?"0x42":"0x41",
            "xml"=>updated?"<synthetic>B</synthetic>":"<synthetic>A</synthetic>",_=>updated?"B":"A"};
        private static string Definition(Table t)=>SqlServerMetadata.Serialize(new{
            Columns=t.Columns.Select(c=>{var d=SqlServerTrialValue.Metadata(c);d[22]=null;return c with{Definition=SqlServerMetadata.Serialize(d)};}),t.SafeRead,t.RowKeys,
            Catalog=t.Catalog.Select(s=>new{s.CheckId,s.Reason,s.Gaps,s.CompleteExceptGaps,Facts=s.Facts.Where(f=>f.Attribute is not("SequenceState" or "AutoIncrementState"))})});
        internal bool CanBindGeneratedKey(string column)=>key.Contains(column);
        private IEnumerable<PreparedTrial> Scope(ConfirmedTrialPlan p)=>parents.Where(d=>p.Dependencies.ContainsKey(d.Id)).Append(this);
        public void ValidateConfirmedPlan(ConfirmedTrialPlan plan)
        {
            if(plan.PreparationId!=Id||plan.Insert.Count==0||plan.Update.Count==0||plan.RoleHost!=null||plan.Role is{Length:0}||plan.Dependencies.Keys.Except(parents.Select(p=>p.Id)).Any())throw new VerificationException(Reason.InvalidInput);
            foreach(var scope in Scope(plan))
            {
                var values=scope==this?plan.Insert:plan.Dependencies[scope.Id];scope.ValidateValues(values);
                if(scope.Columns.Any(c=>c.GeneratedReference is{} reference&&plan.Dependencies.ContainsKey(reference.DependencyId)&&values.ContainsKey(c.Name)))throw new VerificationException(Reason.InvalidInput);
                if(scope.Columns.Any(c=>!c.Generated&&!c.HasDefault&&!c.Nullable&&!values.ContainsKey(c.Name)&&!(c.GeneratedReference is{} reference&&plan.Dependencies.ContainsKey(reference.DependencyId))))throw new VerificationException(Reason.InvalidInput);
            }
            ValidateValues(plan.Update);if(plan.RejectedInput!=null)ValidateValues(plan.RejectedInput,true);
            if(plan.RejectedInput==null){if(plan.RejectedStage!=null||plan.ExpectedNativeError!=null||plan.ExpectedSqlState!=null||plan.ExpectedConstraint!=null)throw new VerificationException(Reason.InvalidInput);}
            else if(plan.RejectedStage is not(TrialStage.RejectInsert or TrialStage.RejectUpdate)||plan.ExpectedSqlState!=null||plan.ExpectedNativeError is not(515 or 547 or 2601 or 2627 or 2628 or 8152 or 8115 or 245 or 241 or 242 or 529 or 6522))throw new VerificationException(Reason.InvalidInput);
        }
        private void ValidateValues(IReadOnlyDictionary<string,string?> values,bool rejected=false)
        {
            foreach(var (name,value) in values)
            {
                var column=table.Columns.SingleOrDefault(c=>c.Name==name)??throw new VerificationException(Reason.InvalidInput);
                if(SqlServerTrialValue.Generated(column))throw new VerificationException(Reason.InvalidInput);
                _=SqlServerTrialValue.Parameter(column,"@value",value,rejected);
            }
        }
    }
}
