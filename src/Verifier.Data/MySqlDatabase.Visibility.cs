namespace Verifier.Data;
public sealed partial class MySqlDatabase
{
    private async Task<Verifier.Core.Reason> ReadTablePermission(Verifier.Core.Table table,IReadOnlyList<Verifier.Core.Column> columns,CancellationToken token)
    {
        if(!table.SafeRead)return table.ReadReason;
        if(columns.Count==0)return Verifier.Core.Reason.VisibilityUnknown;
        try
        {
            // Ask the server to authorize the actual observed projection. WHERE
            // 0 reads no values and naturally includes active roles, column grants
            // and partial revokes. Base InnoDB tables only; MySQL 8.4 forbids
            // stored/loadable functions in generated columns. Never probe a view.
            // A zero-row SELECT still acquires an MDL lock. Release it in a
            // short independent read-only transaction, not the data snapshot.
            var observer=await MetadataObserver(token);
            observer.transaction=await observer.connection!.BeginTransactionAsync(System.Data.IsolationLevel.ReadCommitted,true,token);
            try
            {
                await using(var q=observer.Command("SELECT "+string.Join(',',columns.Select(c=>Quote(c.Name)))+" FROM "+Quote(table.Schema)+"."+Quote(table.Name)+" WHERE 0"))
                await using(var r=await q.ExecuteReaderAsync(token))if(await r.ReadAsync(token))return Verifier.Core.Reason.ObservationChanged;
                await observer.CheckWarnings(token);return Verifier.Core.Reason.None;
            }
            finally{await observer.transaction.DisposeAsync();observer.transaction=null;}
        }
        catch(MySqlConnector.MySqlException e) when(e.Number is 1044 or 1142 or 1143 or 1227){return Verifier.Core.Reason.PermissionDenied;}
        catch(Verifier.Core.VerificationException e){return e.Reason;}
    }
    private async Task<(bool Selected,bool All)> ReadGlobalSelectCoverage(CancellationToken token)
    {
        await using(var q=Command("SELECT @@GLOBAL.partial_revokes"))if(Convert.ToInt32(await q.ExecuteScalarAsync(token))==0)return(true,true);
        try
        {
            // Global SELECT remains listed after a schema partial REVOKE. Read
            // only the public Restrictions JSON path for the current account;
            // never fetch authentication_string or the complete User_attributes.
            await using var q=Command("SELECT COALESCE(JSON_CONTAINS(JSON_EXTRACT(User_attributes,'$.Restrictions'),JSON_OBJECT('Database',DATABASE(),'Privileges',JSON_ARRAY('SELECT'))),0),JSON_SEARCH(JSON_EXTRACT(User_attributes,'$.Restrictions[*].Privileges'),'one','SELECT') IS NOT NULL FROM mysql.user WHERE CAST(CONCAT(QUOTE(User),'@',QUOTE(Host)) AS BINARY)=CAST("+CurrentGrantee+" AS BINARY)");
            await using var r=await q.ExecuteReaderAsync(token);if(!await r.ReadAsync(token))return(false,false);
            var selected=r.GetInt32(0)==0;var all=r.GetInt32(1)==0;if(await r.ReadAsync(token))return(false,false);return(selected,all);
        }
        catch(MySqlConnector.MySqlException e) when(e.Number is 1044 or 1142 or 1143 or 1227){return(false,false);}
    }
}
