using System.Collections;
using System.Runtime.CompilerServices;
using Verifier.Core;

namespace Verifier.Data;

public sealed partial class SqlServerDatabase
{
    private sealed record TableName(string Schema,string Name);
    private sealed record MetadataRange(int Start,int Count);
    private sealed record SectionRange(string CheckId,Reason Reason,bool CompleteExceptGaps,MetadataRange Facts,MetadataRange Gaps);
    private sealed record TableEntry(string Schema,string Name,bool SafeRead,Reason ReadReason,bool SafeLocalRead,bool Partitioned,
        string[] Descendants,MetadataRange Columns,MetadataRange Keys,SectionRange[] Catalog,MetadataRange Definition,bool MemoryOptimized);

    // Only small shape/range descriptors are copied. Column bodies, catalog
    // facts, gaps and keys remain in the observer's existing protected owners.
    // Issued handles are weak: selecting a few tables never pins the whole list.
    private sealed class InventoryTables(SqlServerDatabase owner,CancellationToken token):IFrozenMetadata<Table>,ICollection<Table>,IDisposable
    {
        private sealed record Issued(int Ordinal);
        private readonly ProtectedMetadataStore<TableEntry> records=new(Valid);
        private readonly ConditionalWeakTable<Table,Issued> issued=new();
        private IReadOnlyList<TableEntry>? snapshot;
        public int Count=>records.Count;
        public bool IsReadOnly=>true;
        private static bool Valid(TableEntry entry)=>!string.IsNullOrEmpty(entry.Schema)&&!string.IsNullOrEmpty(entry.Name)
            &&Enum.IsDefined(entry.ReadReason)&&entry.Descendants.All(n=>!string.IsNullOrEmpty(n))
            &&Valid(entry.Columns)&&Valid(entry.Keys)&&Valid(entry.Definition)&&entry.Definition.Count>0&&entry.Catalog.All(s=>!string.IsNullOrEmpty(s.CheckId)
                &&Enum.IsDefined(s.Reason)&&Valid(s.Facts)&&Valid(s.Gaps)&&s.Gaps.Count%2==0);
        private static bool Valid(MetadataRange range)=>range.Start>=0&&range.Count>=0&&range.Count<=int.MaxValue-range.Start;
        private static MetadataRange Range(ProtectedCatalogStore? store,IReadOnlyList<CatalogFact> view)
        {
            if(view.Count==0)return new(0,0);
            var range=(store??throw new VerificationException(Reason.TemporaryIntegrity)).Range(view);
            return new(range.Start,range.Count);
        }
        internal void AddObserved(Table table,(int Start,int Count) definition,bool memoryOptimized)
        {
            token.ThrowIfCancellationRequested();
            var columns=table.Columns.Count==0?(0,0):(owner.columnStore??throw new VerificationException(Reason.TemporaryIntegrity)).Range(table.Columns);
            var keys=table.RowKeys.Count==0?new MetadataRange(0,0):table.RowKeys is KeyView key
                ?Range(owner.catalogStore,key.Records):throw new VerificationException(Reason.TemporaryIntegrity);
            var sections=table.Catalog.Select(s=>new SectionRange(s.CheckId,s.Reason,s.CompleteExceptGaps,
                Range(owner.catalogStore,s.Facts),s.Gaps.Count==0?new(0,0):s.Gaps is GapView gap
                    ?Range(owner.gapStore,gap.Records):throw new VerificationException(Reason.TemporaryIntegrity))).ToArray();
            records.Add(new(table.Schema,table.Name,table.SafeRead,table.ReadReason,table.SafeLocalRead,table.Partitioned,
                table.DataDescendants.ToArray(),new(columns.Item1,columns.Item2),keys,sections,new(definition.Start,definition.Count),memoryOptimized));
        }
        internal void Seal()=>snapshot=records.Seal();
        private static IReadOnlyList<CatalogFact> Facts(ProtectedCatalogStore? store,MetadataRange range)
            =>range.Count==0?[]:(store??throw new VerificationException(Reason.TemporaryIntegrity)).Snapshot(range.Start,range.Count);
        public Table this[int index]
        {
            get
            {
                // Read authenticates the ordinal descriptor on every access.
                var entry=(snapshot??throw new InvalidOperationException())[index];
                var columns=(owner.columnStore??throw new VerificationException(Reason.TemporaryIntegrity)).Snapshot(entry.Columns.Start,entry.Columns.Count);
                var sections=entry.Catalog.Select(s=>new CatalogSection(s.CheckId,Facts(owner.catalogStore,s.Facts),s.Reason)
                    {CompleteExceptGaps=s.CompleteExceptGaps,Gaps=s.Gaps.Count==0?[]:new GapView(Facts(owner.gapStore,s.Gaps))}).ToArray();
                var table=new Table(entry.Schema,entry.Name,columns,entry.SafeRead)
                {
                    ReadReason=entry.ReadReason,SafeLocalRead=entry.SafeLocalRead,Partitioned=entry.Partitioned,
                    DataDescendants=entry.Descendants,Catalog=sections,
                    RowKeys=entry.Keys.Count==0?[]:new KeyView(Facts(owner.catalogStore,entry.Keys))
                };
                issued.Add(table,new(index));return table;
            }
        }
        public bool Contains(Table item)
        {
            if(item==null||!issued.TryGetValue(item,out var handle))return false;
            // Also authenticate the descriptor after issuance, so corrupted
            // inventory storage cannot authorize a previously selected handle.
            _=(snapshot??throw new InvalidOperationException())[handle.Ordinal];return true;
        }
        private TableEntry IssuedEntry(Table table)
        {
            if(!issued.TryGetValue(table,out var handle))throw new VerificationException(Reason.PermissionDenied);
            return(snapshot??throw new InvalidOperationException())[handle.Ordinal];
        }
        internal (int Start,int Count) Definition(Table table)
        {
            var range=IssuedEntry(table).Definition;
            if(range.Count==0)throw new VerificationException(Reason.TemporaryIntegrity);
            return(range.Start,range.Count);
        }
        internal bool MemoryOptimized(Table table)=>IssuedEntry(table).MemoryOptimized;
        public IEnumerator<Table> GetEnumerator(){for(var i=0;i<Count;i++)yield return this[i];}
        IEnumerator IEnumerable.GetEnumerator()=>GetEnumerator();
        public void CopyTo(Table[] array,int arrayIndex)
        {ArgumentNullException.ThrowIfNull(array);if(arrayIndex<0||arrayIndex>array.Length||Count>array.Length-arrayIndex)throw new ArgumentOutOfRangeException(nameof(arrayIndex));foreach(var table in this)array[arrayIndex++]=table;}
        void ICollection<Table>.Add(Table item)=>throw new NotSupportedException();
        public bool Remove(Table item)=>throw new NotSupportedException();
        public void Clear()=>throw new NotSupportedException();
        public void Dispose(){issued.Clear();records.Dispose();}
    }
}
