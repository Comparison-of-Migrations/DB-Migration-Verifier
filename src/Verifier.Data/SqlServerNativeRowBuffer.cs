using System.Collections;
using System.Text.Json;
using Verifier.Core;

namespace Verifier.Data;

// Transient catalog query rows; never exported. Small query results stay local,
// while large results reuse the observer's authenticated row/index ownership.
internal sealed class SqlServerNativeRowBuffer(RunReportStorage owner,CancellationToken token)
{
    private const int RowBudget=256;
    private const long ByteBudget=2*1024*1024;
    private List<string?[]>? memory=[];
    private long retainedBytes;
    private StoredRows? disk;
    private IReadOnlyList<string?[]>? completed;
    internal void Add(string?[] row)
    {
        token.ThrowIfCancellationRequested();if(completed!=null)throw new InvalidOperationException();
        if(row==null)throw new VerificationException(Reason.InvalidInput);
        var size=24L+8L*row.Length;foreach(var field in row)if(field!=null)size=checked(size+24L+2L*field.Length);
        if(memory!=null&&(memory.Count==RowBudget||size>ByteBudget-retainedBytes))
        {
            var next=new StoredRows(token);
            try{owner.Add(next);}catch{next.Dispose();throw;}
            foreach(var previous in memory)next.Add(previous);
            disk=next;memory=null;retainedBytes=0;
        }
        if(memory!=null){memory.Add(row.ToArray());retainedBytes+=size;}else disk!.Add(row);
    }
    internal IReadOnlyList<string?[]> Finish()
    {
        token.ThrowIfCancellationRequested();
        return completed??=memory!=null?memory.ToArray():disk!.Seal();
    }
    private sealed class StoredRows:IDisposable
    {
        private readonly ProtectedMetadataStore<byte[]> records;
        private readonly ProtectedRowStore rows;
        private readonly CancellationToken token;
        internal StoredRows(CancellationToken token)
        {
            this.token=token;records=new(d=>d.Length==64);
            try{rows=new(token:token);}catch{records.Dispose();throw;}
        }
        internal void Add(string?[] row)
        {
            token.ThrowIfCancellationRequested();
            records.Add(rows.AddAsync((stream,ct)=>SqlServerMetadata.SerializeRowAsync(stream,row,ct),token).GetAwaiter().GetResult());
        }
        internal IReadOnlyList<string?[]> Seal(){rows.Flush();return new View(this,records.Seal());}
        public void Dispose(){try{records.Dispose();}finally{rows.Dispose();}}
        private sealed class View(StoredRows owner,IReadOnlyList<byte[]> records):IReadOnlyList<string?[]>
        {
            public int Count=>records.Count;
            public string?[] this[int index]
            {
                get
                {
                    owner.token.ThrowIfCancellationRequested();using var input=owner.rows.OpenRead(records[index]);
                    try
                    {
                        var row=SqlServerMetadata.DeserializeRow(input)??throw new VerificationException(Reason.TemporaryIntegrity);
                        if(input.ReadByte()!=-1)throw new VerificationException(Reason.TemporaryIntegrity);
                        owner.token.ThrowIfCancellationRequested();return row;
                    }
                    catch(JsonException){throw new VerificationException(Reason.TemporaryIntegrity);}
                }
            }
            public IEnumerator<string?[]> GetEnumerator(){for(var i=0;i<Count;i++)yield return this[i];}
            IEnumerator IEnumerable.GetEnumerator()=>GetEnumerator();
        }
    }
}
