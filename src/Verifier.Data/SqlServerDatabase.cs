using System.Data;
using System.Diagnostics;
using System.Text.Json;
using Microsoft.Data.SqlClient;
using Verifier.Core;

namespace Verifier.Data;

public sealed partial class SqlServerDatabase(ConnectionSettings settings) : IReadDatabase,IStreamingReadDatabase,IIntegrityDatabase,ICatalogObservationDatabase
{
    internal static readonly ActivitySource ObservationActivity=new("DBMV.SqlServer.Observation");
    private SqlConnection? connection;
    private SqlTransaction? transaction;
    private Inventory? inventory;
    private ProtectedCatalogStore? definitionStore;
    private ProtectedCatalogStore? catalogStore;
    private ProtectedCatalogStore? observationCatalogStore;
    private ProtectedCatalogStore? gapStore,observationGapStore;
    private ProtectedColumnStore? columnStore;
    private ProtectedMetadataStore<CatalogObservationLimit>? observationLimits;
    private InventoryTables? tableStore;
    private RunReportStorage nativeRows=new();
    private RunReportStorage inspectionRows=new();
    private readonly Dictionary<string,IReadOnlyList<string?[]>> inspectionQueries=new(StringComparer.Ordinal);
    private CatalogSection? inspectionAccess;
    private IReadOnlyList<CatalogFact>? definitionBaselines;
    private bool hasMemoryOptimizedTables;
    private bool complete;
    private bool writableTrial;
    // Private trial observers inspect only their dependency scope. Public
    // source/target inventories continue to inspect the whole database.
    private IReadOnlySet<string>? trialTables;
    private string? preparationTable;
    internal static string Quote(string value)=>"["+value.Replace("]","]]",StringComparison.Ordinal)+"]";
    private static string Name(Table table)=>Quote(table.Schema)+"."+Quote(table.Name);
    private string ReadRelation(Table table,string? alias=null)=>Name(table)+(alias==null?"":" "+alias)+(tableStore?.MemoryOptimized(table)==true?" WITH (SNAPSHOT)":"");
    private SqlCommand Command(string sql)
    {
        if(connection is not{State:ConnectionState.Open})throw new VerificationException(Reason.ConnectionLost);
        // Client encoding must never silently execute a different native body.
        return new(SqlServerMetadata.CommandText(sql),connection,transaction){CommandTimeout=60};
    }
    public async Task<Inventory> InspectAsync(CancellationToken token)
    {
        if(inventory!=null)return inventory;
        try
        {
            connection=new(settings.SqlServerString());await connection.OpenAsync(token);
            string identity;bool snapshot;
            await using(var q=Command("""
                SELECT CAST(SERVERPROPERTY('ProductMajorVersion') AS int),
                       CONVERT(varbinary(max),CAST(SERVERPROPERTY('ServerName') AS nvarchar(256))) AS [__dbmv_utf16],CAST(DB_ID() AS int),
                       CAST(service_broker_guid AS nvarchar(36)),snapshot_isolation_state,
                       HAS_PERMS_BY_NAME(DB_NAME(),'DATABASE','VIEW DEFINITION')
                FROM sys.databases WHERE database_id=DB_ID()
                """))
            await using(var r=await q.ExecuteReaderAsync(token))
            {
                if(!await r.ReadAsync(token)||r.IsDBNull(1)||r.IsDBNull(3))throw new VerificationException(Reason.IdentityUnknown);
                if(r.GetInt32(0)!=16)throw new VerificationException(Reason.PlatformNotVerified);
                identity=SqlServerMetadata.Serialize(new[]{SqlServerMetadata.ReadText(r,1,token),r.GetInt32(2).ToString(System.Globalization.CultureInfo.InvariantCulture),r.GetString(3)});
                snapshot=r.GetByte(4)==1;complete=!r.IsDBNull(5)&&r.GetInt32(5)==1;
            }
            // SQL Server has no READ ONLY transaction switch. This observer only
            // exposes generated SELECT statements and never changes DB options.
            snapshot=snapshot&&!writableTrial;
            transaction=(SqlTransaction)await connection.BeginTransactionAsync(snapshot?IsolationLevel.Snapshot:IsolationLevel.Serializable,token);
            using var names=new ProtectedMetadataStore<TableName>(n=>!string.IsNullOrEmpty(n.Schema)&&!string.IsNullOrEmpty(n.Name));
            await using(var q=Command("SELECT CONVERT(varbinary(max),CONVERT(nvarchar(max),s.name)) AS [__dbmv_utf16],CONVERT(varbinary(max),CONVERT(nvarchar(max),t.name)) AS [__dbmv_utf16] FROM sys.tables t JOIN sys.schemas s ON s.schema_id=t.schema_id WHERE t.is_ms_shipped=0 ORDER BY s.name,t.name"))
            await using(var r=await q.ExecuteReaderAsync(token))
                while(await r.ReadAsync(token)){token.ThrowIfCancellationRequested();names.Add(new(SqlServerMetadata.ReadText(r,0,token)!,SqlServerMetadata.ReadText(r,1,token)!));}
            var discovered=names.Seal();
            if(preparationTable!=null)
            {
                var root=discovered.Select(n=>new Table(n.Schema,n.Name,[],false)).SingleOrDefault(t=>t.Qualified==preparationTable)??throw new VerificationException(Reason.InvalidInput);
                trialTables=ExpandTrialPreparationScope(root.Qualified,await Native(TrialPreparationReferences,root,token),token);
            }
            tableStore=new(this,token);
            foreach(var name in discovered)
            {
                token.ThrowIfCancellationRequested();var table=new Table(name.Schema,name.Name,[],false);
                if(trialTables==null||trialTables.Contains(table.Qualified))await InspectTable(table,token);
            }
            if(definitionStore!=null){definitionStore.Seal();definitionBaselines=definitionStore.Snapshot(0,definitionStore.Count);}
            databaseBaseline=trialTables==null?FreezeCatalog(await ReadDatabaseScope(token),token):[];
            catalogStore?.Seal();gapStore?.Seal();columnStore?.Seal();
            tableStore.Seal();
            var observationText=snapshot
                ?"Independent SNAPSHOT; writes stopped required; catalog observations are not an atomic cross-server DDL snapshot."
                :"Independent SERIALIZABLE locking reads; SNAPSHOT unavailable; writes stopped required; catalog observations are not an atomic cross-server DDL snapshot.";
            if(hasMemoryOptimizedTables)observationText+=" Memory-optimized tables use native SNAPSHOT table reads; no database option changes.";
            return inventory=new(identity,tableStore,complete&&trialTables==null,observationText){DatabaseCatalog=databaseBaseline};
        }
        catch(SqlException e){throw Map(e,token);}
        finally{try{ReleaseNativeRows();}finally{ReleaseInspectionRows();}}
    }
    private async Task<Table> InspectTable(Table table,CancellationToken token)
    {
        using var activity=ObservationActivity.StartActivity("InspectTable",ActivityKind.Internal,default(ActivityContext),
            tags:[new KeyValuePair<string,object?>("table",table.Qualified)]);
        try
        {
            var observation=await ReadTable(table,token);
            var observed=await ObserveTable(observation.Table,token);
            var baseline=RememberDefinition(DefinitionFacts(observation.Signature,observed,token),token);
            var frozen=observed with{Catalog=FreezeCatalog(observed.Catalog,token),Columns=FreezeColumns(observed.Columns,token),RowKeys=FreezeKeys(observed.RowKeys,token)};
            tableStore?.AddObserved(frozen,baseline,observation.MemoryOptimized);
            hasMemoryOptimizedTables|=observation.MemoryOptimized;return frozen;
        }
        // Only frozen catalog/column/key views escape this checkpoint. Native
        // query rows and the transient column signature have no later consumer.
        finally{ReleaseNativeRows();}
    }
    // Collect directly into the observer's existing protected catalog. Each
    // serial producer captures its range before another section appends; empty
    // sections allocate nothing. Fresh observations have a separate lifetime.
    private CatalogFacts CollectCatalogFacts(CancellationToken token)=>new(()=>
        inventory==null?(catalogStore??=new(token)):(observationCatalogStore??=new(token)));
    internal sealed class CatalogFacts(Func<ProtectedCatalogStore>? storage=null)
    {
        private readonly List<CatalogFact>? memory=storage==null?[]:null;
        private ProtectedCatalogStore? store;
        private int start;
        internal int Count=>memory?.Count??(store==null?0:store.Count-start);
        internal ProtectedCatalogStore Destination
        {
            get
            {
                if(store==null){store=(storage??throw new InvalidOperationException())();start=store.Count;}
                return store;
            }
        }
        internal void Add(CatalogFact fact){if(memory!=null)memory.Add(fact);else Destination.Add(fact);}
        internal void AddRange(IEnumerable<CatalogFact> facts){foreach(var fact in facts)Add(fact);}
        internal IReadOnlyList<CatalogFact> Snapshot(int offset=0)
        {
            if(offset<0||offset>Count)throw new ArgumentOutOfRangeException(nameof(offset));
            return memory!=null?memory.Skip(offset).ToArray():store==null?[]:store.Snapshot(start+offset,Count-offset);
        }
    }
    private IReadOnlyList<Column> FreezeColumns(IReadOnlyList<Column> columns,CancellationToken token)
    {
        // Empty visibility-limited shapes still borrow the native ordinal
        // owner; one empty table must not force whole-scope eager retention.
        columnStore??=new(token);var start=columnStore.Count;
        foreach(var column in columns){token.ThrowIfCancellationRequested();columnStore.Add(column);}
        return columnStore.Snapshot(start,columnStore.Count-start);
    }
    private IReadOnlyList<CatalogSection> FreezeCatalog(IReadOnlyList<CatalogSection> sections,CancellationToken token)
    {
        var frozen=new CatalogSection[sections.Count];
        for(var i=0;i<sections.Count;i++)
        {
            token.ThrowIfCancellationRequested();var section=sections[i];
            var facts=section.Facts;
            if(facts.Count>0&&facts is not IFrozenMetadata<CatalogFact>)
            {
                catalogStore??=new(token);var start=catalogStore.Count;
                foreach(var fact in facts){token.ThrowIfCancellationRequested();catalogStore.Add(fact);}
                facts=catalogStore.Snapshot(start,catalogStore.Count-start);
            }
            frozen[i]=section with{Facts=facts,Gaps=FreezeGaps(section.Gaps,token)};
        }
        return frozen;
    }
    private IReadOnlyList<CatalogGap> FreezeGaps(IReadOnlyList<CatalogGap> gaps,CancellationToken token)
    {
        if(gaps.Count==0||gaps is IFrozenMetadata<CatalogGap>)return gaps;
        var collector=CollectCatalogGaps(token);collector.AddRange(gaps);return collector.Snapshot();
    }
    // Gap records have their own stream: appending a gap must never become
    // part of a concurrently active fact section's ordinal range.
    private CatalogGaps CollectCatalogGaps(CancellationToken token)=>new(()=>
        inventory==null?(gapStore??=new(token)):(observationGapStore??=new(token)));
    internal sealed class CatalogGaps(Func<ProtectedCatalogStore>? storage=null)
    {
        private readonly CatalogFacts records=new(storage);
        internal int Count=>records.Count/2;
        internal void Add(CatalogGap gap)
        {
            records.Add(new("gap",gap.Key,"attribute",gap.Attribute));
            records.Add(new("gap",gap.Column,((int)gap.Reason).ToString(System.Globalization.CultureInfo.InvariantCulture),gap.LocalEvidence));
        }
        internal void AddRange(IEnumerable<CatalogGap> gaps){foreach(var gap in gaps)Add(gap);}
        internal IReadOnlyList<CatalogGap> Snapshot(int offset=0)=>new GapView(records.Snapshot(checked(offset*2)));
    }
    private sealed class GapView(IReadOnlyList<CatalogFact> records):IFrozenMetadata<CatalogGap>
    {
        internal IReadOnlyList<CatalogFact> Records=>records;
        public int Count=>records.Count/2;
        public CatalogGap this[int index]
        {
            get
            {
                if(index<0||index>=Count)throw new ArgumentOutOfRangeException(nameof(index));
                var header=records[index*2];var value=records[index*2+1];
                if(records.Count%2!=0||header.Key!="gap"||header.Attribute!="attribute"||header.Column==null||header.Value==null||
                   value.Key!="gap"||value.Value==null||!int.TryParse(value.Attribute,System.Globalization.NumberStyles.None,System.Globalization.CultureInfo.InvariantCulture,out var reason)||!Enum.IsDefined((Reason)reason))
                    throw new VerificationException(Reason.TemporaryIntegrity);
                return new(header.Column,value.Column,header.Value,value.Value,(Reason)reason);
            }
        }
        public IEnumerator<CatalogGap> GetEnumerator(){for(var i=0;i<Count;i++)yield return this[i];}
        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()=>GetEnumerator();
    }
    internal static Reason ErrorReason(int number)=>number switch
    {
        -2 or 258 or 1205 or 1222=>Reason.Timeout,
        229 or 230 or 262 or 297 or 916 or 18456=>Reason.PermissionDenied,
        207 or 208 or 3961 or 241 or 242 or 245 or 529 or 8114 or 8115 or 8134=>Reason.ObservationChanged,
        _=>Reason.ConnectionLost
    };
    private static Reason FaultReason(SqlException e,CancellationToken token)=>token.IsCancellationRequested?Reason.Cancelled:ErrorReason(e.Number);
    private static VerificationException Map(SqlException e,CancellationToken token)=>new(FaultReason(e,token));
    private async Task ValidateTable(Table table,CancellationToken token)
    {
        // Opt-in diagnostic listeners receive location/timing only, never SQL,
        // connection settings, row keys or values. No listener means no activity.
        using var activity=ObservationActivity.StartActivity("ValidateTable",ActivityKind.Internal,default(ActivityContext),
            tags:[new KeyValuePair<string,object?>("table",table.Qualified)]);
        if(inventory==null||tableStore==null)throw new VerificationException(Reason.PermissionDenied);
        var baseline=tableStore.Definition(table);
        try
        {
            using var columns=ObservationActivity.StartActivity("ReadColumns");
            var current=await ReadTable(table,token);columns?.Stop();
            var structure=await ObserveTable(current.Table,token);
            using var definition=ObservationActivity.StartActivity("CompareDefinition");
            if(!MatchesDefinition(baseline,DefinitionFacts(current.Signature,structure,token),token))throw new VerificationException(Reason.ObservationChanged);
        }
        finally{ReleaseObservationCatalog();}
    }
    private void ReleaseObservationCatalog()
    {var old=observationCatalogStore;var oldGaps=observationGapStore;observationCatalogStore=null;observationGapStore=null;try{old?.Dispose();}finally{try{oldGaps?.Dispose();}finally{ReleaseNativeRows();}}}
    private void ReleaseNativeRows()
    {var old=nativeRows;nativeRows=new();old.Dispose();}
    private void ReleaseInspectionRows()
    {
        inspectionQueries.Clear();inspectionAccess=null;
        var old=inspectionRows;inspectionRows=new();old.Dispose();
    }
    // Native column rows retain their field boundaries without a second full
    // JSON serialization. Length frames distinguish NULL, empty and chunks.
    internal static IEnumerable<CatalogFact> DefinitionFacts(IReadOnlyList<string?[]> columns,Table table,CancellationToken token)
    {
        token.ThrowIfCancellationRequested();
        yield return new("columns",null,"rows",columns.Count.ToString(System.Globalization.CultureInfo.InvariantCulture));
        var buffer=new char[8192];var filled=0;
        try
        {
            foreach(var part in Parts())
            {
                for(var offset=0;offset<part.Length;)
                {
                    token.ThrowIfCancellationRequested();var count=Math.Min(buffer.Length-filled,part.Length-offset);
                    part.CopyTo(offset,buffer,filled,count);filled+=count;offset+=count;
                    if(filled==buffer.Length){yield return new("columns",null,"chunk",new string(buffer));filled=0;}
                }
            }
            if(filled>0)yield return new("columns",null,"chunk",new string(buffer,0,filled));
        }
        finally{Array.Clear(buffer);}
        // Row/field counts and UTF-16 lengths make this sequence unambiguous,
        // including NULL, empty and values that themselves contain separators.
        // Pack small fields together rather than flushing two records per field.
        IEnumerable<string> Parts()
        {
            foreach(var row in columns)
            {
                token.ThrowIfCancellationRequested();
                yield return row.Length.ToString(System.Globalization.CultureInfo.InvariantCulture)+":";
                foreach(var value in row)
                {
                    token.ThrowIfCancellationRequested();
                    yield return (value==null?"-1":value.Length.ToString(System.Globalization.CultureInfo.InvariantCulture))+":";
                    if(value!=null)yield return value;
                }
            }
        }
        foreach(var frame in TableDefinitionFacts(table,token))yield return frame;
    }
    private static IEnumerable<CatalogFact> TableDefinitionFacts(Table table,CancellationToken token)
    {
        foreach(var frame in CatalogDefinitionFacts(table.Catalog,token))yield return frame;
        yield return Count("keys",table.RowKeys.Count);
        foreach(var key in table.RowKeys)
        {
            yield return Count("key-columns",key.Count);
            foreach(var name in key){token.ThrowIfCancellationRequested();yield return new("row-key",null,"column",name);}
        }
        static CatalogFact Count(string name,int count)=>new("frame",null,name,count.ToString(System.Globalization.CultureInfo.InvariantCulture));
    }
    private static IEnumerable<CatalogFact> CatalogDefinitionFacts(IReadOnlyList<CatalogSection> sections,CancellationToken token)
    {
        token.ThrowIfCancellationRequested();
        yield return Count("sections",sections.Count);
        foreach(var section in sections)
        {
            token.ThrowIfCancellationRequested();
            yield return new("section",section.CheckId,"reason",((int)section.Reason).ToString(System.Globalization.CultureInfo.InvariantCulture));
            yield return new("section",null,"complete-except-gaps",section.CompleteExceptGaps?"1":"0");
            yield return Count("facts",section.Facts.Count);
            foreach(var fact in section.Facts){token.ThrowIfCancellationRequested();yield return fact;}
            yield return Count("gaps",section.Gaps.Count);
            foreach(var gap in section.Gaps)
            {
                token.ThrowIfCancellationRequested();
                yield return new("gap",null,"reason",((int)gap.Reason).ToString(System.Globalization.CultureInfo.InvariantCulture));
                yield return new(gap.Key,gap.Column,gap.Attribute,gap.LocalEvidence);
            }
        }
        static CatalogFact Count(string name,int count)=>new("frame",null,name,count.ToString(System.Globalization.CultureInfo.InvariantCulture));
    }
    internal static bool CatalogDefinitionsEqual(IReadOnlyList<CatalogSection> expected,IReadOnlyList<CatalogSection> current,string check,CancellationToken token)
    {
        token.ThrowIfCancellationRequested();
        using var left=CatalogDefinitionFacts(expected.Where(s=>s.CheckId==check).ToArray(),token).GetEnumerator();
        using var right=CatalogDefinitionFacts(current.Where(s=>s.CheckId==check).ToArray(),token).GetEnumerator();
        var equal=true;bool hasLeft,hasRight;
        do
        {
            token.ThrowIfCancellationRequested();hasLeft=left.MoveNext();hasRight=right.MoveNext();
            if(hasLeft!=hasRight||hasLeft&&!left.Current.DefinitionEquals(right.Current,token))equal=false;
        }while(hasLeft||hasRight);
        return equal;
    }
    private (int Start,int Count) RememberDefinition(IEnumerable<CatalogFact> signature,CancellationToken token)
    {
        definitionStore??=new(token);var start=definitionStore.Count;
        foreach(var fact in signature){token.ThrowIfCancellationRequested();definitionStore.Add(fact);}
        return (start,definitionStore.Count-start);
    }
    private bool MatchesDefinition((int Start,int Count) baseline,IEnumerable<CatalogFact> current,CancellationToken token)
    {
        if(definitionBaselines==null)throw new VerificationException(Reason.TemporaryIntegrity);
        using var actual=current.GetEnumerator();var equal=true;
        for(var i=0;i<baseline.Count;i++)
        {
            token.ThrowIfCancellationRequested();var expected=definitionBaselines[baseline.Start+i];
            if(!actual.MoveNext()||!actual.Current.DefinitionEquals(expected,token))equal=false;
        }
        // Authenticate all baseline entries and drain the fresh sequence even
        // after a difference, so damage cannot be hidden behind an early exit.
        while(actual.MoveNext()){token.ThrowIfCancellationRequested();equal=false;}
        return equal;
    }
    private async Task<Table> ObserveTable(Table table,CancellationToken token)
    {
        using var structure=ObservationActivity.StartActivity("ReadStructure");
        table=await AddStructure(table,token);structure?.Stop();
        using var access=ObservationActivity.StartActivity("ReadAccess");
        table=await AddAccess(table,token);access?.Stop();
        using var objects=ObservationActivity.StartActivity("ReadObjects");
        return await AddObjects(table,token);
    }
    public async Task<IReadOnlyList<CatalogObservationLimit>> InspectCatalogChangesAsync(IReadOnlyList<Table> tables,IReadOnlySet<string> checks,bool databaseScope,CancellationToken token)
    {
        observationLimits?.Dispose();observationLimits=null;
        foreach(var table in tables)
        {
            try{await ValidateTable(table,token);}
            catch(VerificationException e){foreach(var check in checks)AddLimit(new(check,table.Qualified,e.Reason));}
            catch(SqlException e){foreach(var check in checks)AddLimit(new(check,table.Qualified,FaultReason(e,token)));}
        }
        var databaseChecks=CatalogComparison.DatabaseChecks.Where(checks.Contains).ToArray();
        if(databaseScope&&databaseChecks.Length>0)
        {
            try
            {
                var fresh=await ReadDatabaseScope(token);
                foreach(var check in databaseChecks)
                    if(databaseBaseline==null||!CatalogDefinitionsEqual(databaseBaseline,fresh,check,token))
                        AddLimit(new(check,null,Reason.ObservationChanged));
            }
            catch(SqlException e){foreach(var check in databaseChecks)AddLimit(new(check,null,FaultReason(e,token)));}
            catch(VerificationException e){foreach(var check in databaseChecks)AddLimit(new(check,null,e.Reason));}
            finally{ReleaseObservationCatalog();}
        }
        return observationLimits?.Seal()??Array.Empty<CatalogObservationLimit>();
        void AddLimit(CatalogObservationLimit limit)
        {
            observationLimits??=new(l=>!string.IsNullOrEmpty(l.CheckId)&&Enum.IsDefined(l.Reason)&&l.Reason!=Reason.None);
            observationLimits.Add(limit);
        }
    }
    public async ValueTask DisposeAsync()
    {try{if(transaction!=null)await transaction.DisposeAsync();}finally{try{if(connection!=null)await connection.DisposeAsync();}finally{try{tableStore?.Dispose();}finally{try{definitionStore?.Dispose();}finally{try{catalogStore?.Dispose();}finally{try{columnStore?.Dispose();}finally{try{gapStore?.Dispose();}finally{try{observationLimits?.Dispose();}finally{try{ReleaseObservationCatalog();}finally{ReleaseInspectionRows();}}}}}}}}}}
}
