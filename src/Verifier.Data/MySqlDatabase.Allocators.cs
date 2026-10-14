using System.Globalization;
using System.Text.Json;
using Verifier.Core;
namespace Verifier.Data;

public sealed partial class MySqlDatabase
{
    private async Task<CatalogSection> ReadAutoIncrement(Table table,IReadOnlyList<Column> columns,CancellationToken token)
    {
        // TABLES statistics otherwise default to a 24-hour cache. The session
        // stats_expiry=0 set during Inspect reads the engine's current counter.
        // Never allocate a number, run ANALYZE, or infer the counter from MAX(id):
        // deletes and rolled-back allocations deliberately leave gaps.
        try
        {
            var native=await ReadNative("SELECT COLUMN_NAME,COLUMN_TYPE FROM information_schema.COLUMNS WHERE "+TableFilter+" AND EXTRA LIKE '%auto_increment%' ORDER BY ORDINAL_POSITION",table,token);
            if(native.Count==0)return new("OBJECT-02",[],Reason.None);
            if(native.Count!=1||native[0][0]==null||!columns.Any(c=>c.Name==native[0][0]))return new("OBJECT-02",[],Reason.VisibilityUnknown);
            var column=native[0][0]!;
            var facts=new List<CatalogFact>{new(column,column,"AutoIncrementDefinition",JsonSerializer.Serialize(native[0][1]))};
            if(!table.SafeRead)return new("OBJECT-02",facts,Reason.NotImplemented);
            var state=await ReadNative("SELECT AUTO_INCREMENT FROM information_schema.TABLES WHERE "+TableFilter+" AND TABLE_TYPE='BASE TABLE'",table,token);
            if(state.Count!=1||state[0][0]==null||!ulong.TryParse(state[0][0],NumberStyles.None,CultureInfo.InvariantCulture,out var next))
                return new("OBJECT-02",facts,Reason.VisibilityUnknown);
            await CheckWarnings(token);
            facts.Add(new(column,column,"AutoIncrementState",next.ToString(CultureInfo.InvariantCulture)));
            return new("OBJECT-02",facts,Reason.None);
        }
        catch(MySqlConnector.MySqlException e){return new("OBJECT-02",[],Map(e).Reason);}
        catch(VerificationException e){return new("OBJECT-02",[],e.Reason);}
    }
}
