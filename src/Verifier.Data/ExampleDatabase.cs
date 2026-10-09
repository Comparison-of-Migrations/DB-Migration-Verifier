using System.Runtime.CompilerServices;
using Verifier.Core;

namespace Verifier.Data;

// Fixed synthetic values only; runs the same comparison engine as actual PostgreSQL rows.
public sealed class ExampleDatabase(bool target=false) : IReadDatabase
{
    private static readonly Column[] Columns=[new("id","int4",1,false,null,true),new("label","text",2,true,null,true),new("amount","numeric",3,false,null,true)];
    private readonly Table table=new("demo","entries",Columns,true);
    public Task<Inventory> InspectAsync(CancellationToken token)=>Task.FromResult(new Inventory(target?"example-target":"example-source",[table],true,"Synthetic example only"));
    public async IAsyncEnumerable<byte[]> RowsAsync(Table table,[EnumeratorCancellation] CancellationToken token)
    {
        string?[][] rows=target ? [["2","Beta ","2.00"],["1","Alpha","1"],["1","Alpha","1"]] : [["1","Alpha","1.0"],["2","Beta","2"],["2","Beta","2"]];
        foreach(var row in rows) { token.ThrowIfCancellationRequested(); await Task.Yield(); yield return ExactRow.Encode(row,Columns); }
    }
    public ValueTask DisposeAsync()=>ValueTask.CompletedTask;
}
