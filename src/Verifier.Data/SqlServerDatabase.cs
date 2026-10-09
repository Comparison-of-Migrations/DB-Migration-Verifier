using Microsoft.Data.SqlClient;
using Verifier.Core;

namespace Verifier.Data;

public sealed class SqlServerDatabase(ConnectionSettings settings) : IReadDatabase
{
    private SqlConnection? connection;
    private Inventory? inventory;
    public async Task<Inventory> InspectAsync(CancellationToken token)
    {
        if(inventory!=null) return inventory;
        try
        {
            connection=new(settings.SqlServerString()); await connection.OpenAsync(token);
            using var command=connection.CreateCommand(); command.CommandTimeout=60;
            command.CommandText="""
            SELECT CAST(SERVERPROPERTY('ServerName') AS nvarchar(256)),CAST(service_broker_guid AS nvarchar(36)),
            HAS_PERMS_BY_NAME(DB_NAME(),'DATABASE','VIEW DEFINITION') FROM sys.databases WHERE name=DB_NAME();
            SELECT s.name,t.name,c.name,ty.name,c.column_id,c.is_nullable,c.max_length,c.precision,c.scale,c.collation_name,
                   dc.definition,c.is_identity,c.is_computed FROM sys.tables t JOIN sys.schemas s ON s.schema_id=t.schema_id
            JOIN sys.columns c ON c.object_id=t.object_id JOIN sys.types ty ON ty.user_type_id=c.user_type_id
            LEFT JOIN sys.default_constraints dc ON dc.object_id=c.default_object_id WHERE t.is_ms_shipped=0
            ORDER BY s.name,t.name,c.column_id;
            """;
            await using var reader=await command.ExecuteReaderAsync(token);
            if(!await reader.ReadAsync(token) || reader.IsDBNull(0) || reader.IsDBNull(1)) throw new VerificationException(Reason.IdentityUnknown);
            var identity=reader.GetString(0)+":"+reader.GetString(1); var visible=!reader.IsDBNull(2) && reader.GetInt32(2)==1;
            await reader.NextResultAsync(token);
            var tables=new List<Table>(); var columns=new List<Column>(); string? schema=null,name=null;
            while(await reader.ReadAsync(token))
            {
                var ns=reader.GetString(0); var table=reader.GetString(1);
                if(schema!=ns || name!=table)
                {
                    if(name!=null) tables.Add(new(schema!,name,columns.ToArray(),false));
                    columns.Clear(); schema=ns; name=table;
                }
                var parts=Enumerable.Range(6,7).Select(i=>reader.IsDBNull(i)?null:Convert.ToString(reader.GetValue(i),System.Globalization.CultureInfo.InvariantCulture));
                columns.Add(new(reader.GetString(2),reader.GetString(3),reader.GetInt32(4),reader.GetBoolean(5),string.Join('\u001f',parts),false));
            }
            if(name!=null) tables.Add(new(schema!,name,columns.ToArray(),false));
            return inventory=new(identity,tables,visible,"Writes stopped required; column metadata only; value/snapshot support not implemented.");
        }
        catch(SqlException e) { throw new VerificationException(e.Number switch { -2=>Reason.Timeout,229 or 230=>Reason.PermissionDenied,_=>Reason.ConnectionLost }); }
    }
    public async IAsyncEnumerable<byte[]> RowsAsync(Table table,[System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken token)
    { await Task.CompletedTask; throw new VerificationException(Reason.NotImplemented);
#pragma warning disable CS0162
      yield break;
#pragma warning restore CS0162
    }
    public async ValueTask DisposeAsync() { if(connection!=null) await connection.DisposeAsync(); }
}
