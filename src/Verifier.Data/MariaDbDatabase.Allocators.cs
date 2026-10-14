using System.Globalization;
using System.Text.Json;
using MySqlConnector;
using Verifier.Core;
namespace Verifier.Data;

public sealed partial class MariaDbDatabase
{
    private async Task<CatalogSection> ReadAutoIncrement(Table table,IReadOnlyList<Column> columns,string engine,string kind,CancellationToken token)
    {
        try
        {
            // MariaDB reads its native counter here. MySQL's stats_expiry option
            // does not exist in this server; MAX(id) loses deletes and rollbacks.
            var native=await Native("SELECT COLUMN_NAME,COLUMN_TYPE FROM information_schema.COLUMNS WHERE "+TableFilter+" AND EXTRA LIKE '%auto_increment%' ORDER BY ORDINAL_POSITION",table,token);
            if(native.Count==0)return new("OBJECT-02",[],Reason.None);
            if(native.Count!=1||native[0][0]==null||!columns.Any(c=>c.Name==native[0][0]))return new("OBJECT-02",[],Reason.VisibilityUnknown);
            var name=native[0][0]!;var facts=new List<CatalogFact>{new(name,name,"AutoIncrementDefinition",JsonSerializer.Serialize(native[0][1]))};
            if(engine!="InnoDB"||kind!="BASE TABLE")return new("OBJECT-02",facts,Reason.NotImplemented);
            var state=await Native("SELECT AUTO_INCREMENT FROM information_schema.TABLES WHERE "+TableFilter+" AND TABLE_TYPE='BASE TABLE'",table,token);
            if(state.Count!=1||state[0][0]==null||!ulong.TryParse(state[0][0],NumberStyles.None,CultureInfo.InvariantCulture,out var next))return new("OBJECT-02",facts,Reason.VisibilityUnknown);
            await CheckWarnings(token);facts.Add(new(name,name,"AutoIncrementState",next.ToString(CultureInfo.InvariantCulture)));return new("OBJECT-02",facts,Reason.None);
        }
        catch(MySqlException e){return new("OBJECT-02",[],Map(e).Reason);}
        catch(VerificationException e){return new("OBJECT-02",[],e.Reason);}
    }
    private async Task<string> SequenceDefinition(string name,CancellationToken token)
    {
        await using var q=Command("SHOW CREATE SEQUENCE "+Quote(settings.Database)+"."+Quote(name));await using var r=await q.ExecuteReaderAsync(token);
        var prefix="CREATE SEQUENCE "+Quote(name);
        if(!await r.ReadAsync(token)||r.FieldCount!=2||r.IsDBNull(1)||!r.GetString(1).StartsWith(prefix+" ",StringComparison.Ordinal))throw new VerificationException(Reason.ObservationChanged);
        var definition=r.GetString(1)[prefix.Length..];if(await r.ReadAsync(token))throw new VerificationException(Reason.ObservationChanged);return definition;
    }
    private async Task<CatalogSection> ReadSequences(bool complete,CancellationToken token)
    {
        var facts=new List<CatalogFact>();var gaps=new List<CatalogGap>();
        try
        {
            complete&=await InventoryVisible(token);
            var names=await Native("SELECT TABLE_NAME FROM information_schema.TABLES WHERE CAST(TABLE_SCHEMA AS BINARY)=CAST(DATABASE() AS BINARY) AND TABLE_TYPE='SEQUENCE' ORDER BY CAST(TABLE_NAME AS BINARY)",null,token);
            if(names.Count==0)return new("OBJECT-02",facts,complete?Reason.None:Reason.VisibilityUnknown);
            // LOCK TABLES commits an active transaction. Use a dedicated, short
            // session with default READ ONLY, never the stored-value snapshot.
            // LOCK READ pins the object before SHOW's native sequence type check.
            // SHOW alone releases MDL; HANDLER is not supported by ha_sequence.
            await using var pin=new MariaDbDatabase(settings);await pin.Open(token);
            await using(var setup=pin.Command("SET SESSION TRANSACTION READ ONLY;SET SESSION lock_wait_timeout=2"))await setup.ExecuteNonQueryAsync(token);
            foreach(var row in names)
            {
                var name=row[0]!;string? definition=null;var acquiringLock=false;
                try
                {
                    // This native statement does not evaluate sequence values.
                    // Retain a known definition even when LOCK privilege is absent.
                    definition=await pin.SequenceDefinition(name,token);acquiringLock=true;
                    await using(var l=pin.Command("LOCK TABLES "+Quote(settings.Database)+"."+Quote(name)+" READ"))await l.ExecuteNonQueryAsync(token);acquiringLock=false;
                    try
                    {
                        definition=await pin.SequenceDefinition(name,token);
                        var state=await pin.Native("SELECT next_not_cached_value,minimum_value,maximum_value,start_value,increment,cache_size,cycle_option,cycle_count FROM "+Quote(settings.Database)+"."+Quote(name),null,token);
                        if(state.Count!=1||state[0].Length!=8||state[0].Any(x=>x==null||!System.Numerics.BigInteger.TryParse(x,NumberStyles.AllowLeadingSign,CultureInfo.InvariantCulture,out _)))throw new VerificationException(Reason.ObservationChanged);
                        await pin.CheckWarnings(token);
                        facts.Add(new(name,null,"SequenceState",JsonSerializer.Serialize(new[]{state[0][0],state[0][7]})));
                        if(state[0][5]!="0")gaps.Add(new(name,null,"SequenceCachedReservation","Native reserved next values are not exposed by MariaDB11.4; recorded next_not_cached_value is compared separately.",Reason.NotImplemented));
                    }
                    finally{await using var unlock=pin.Command("UNLOCK TABLES");await unlock.ExecuteNonQueryAsync(CancellationToken.None);}
                }
                catch(MySqlException e){Failed(Map(e).Reason);}
                catch(VerificationException e){Failed(e.Reason);}
                void Failed(Reason reason)
                {
                    // If a kind change was observed, the earlier definition is
                    // stale too. Missing definitions also leave state unread;
                    // one gap must never turn the other attribute into absence.
                    if(acquiringLock&&reason==Reason.PermissionDenied)gaps.Add(new(name,null,"SequenceReadLockAccess","SELECT and LOCK TABLES are required for the short read-only kind pin.",reason));
                    if(reason==Reason.ObservationChanged)definition=null;
                    facts.RemoveAll(f=>f.Key==name&&f.Attribute=="SequenceState");
                    if(definition==null)gaps.Add(new(name,null,"SequenceDefinitions","Native sequence definition was not established.",reason));
                    gaps.Add(new(name,null,"SequenceState","Native sequence state was not established.",reason));
                }
                if(definition!=null)facts.Add(new(name,null,"SequenceDefinitions",definition));
            }
            return new("OBJECT-02",facts,!complete?Reason.VisibilityUnknown:gaps.Count==0?Reason.None:gaps[0].Reason){Gaps=gaps,CompleteExceptGaps=complete&&gaps.Count>0};
        }
        catch(MySqlException e){return new("OBJECT-02",[],Map(e).Reason);}
        catch(VerificationException e){return new("OBJECT-02",[],e.Reason);}
    }
}
