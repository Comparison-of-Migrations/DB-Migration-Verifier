using System.Data;
using System.Runtime.CompilerServices;
using Npgsql;
using Verifier.Core;

namespace Verifier.Data;

public sealed partial class PostgreSqlDatabase(ConnectionSettings settings) : IReadDatabase
{
    private NpgsqlConnection? connection;
    private NpgsqlTransaction? transaction;
    private Inventory? inventory;
    private static readonly HashSet<string> Supported=new(StringComparer.Ordinal) { "int2","int4","int8","text","varchar","bpchar","bool","uuid","numeric","bytea" };
    public async Task<Inventory> InspectAsync(CancellationToken token)
    {
        if(inventory!=null) return inventory;
        try
        {
            connection=new(settings.PostgreSqlString()); await connection.OpenAsync(token);
            transaction=await connection.BeginTransactionAsync(IsolationLevel.RepeatableRead,token);
            await using(var config=new NpgsqlCommand("SET TRANSACTION READ ONLY; SET LOCAL row_security=off; SET LOCAL search_path=pg_catalog",connection,transaction)) await config.ExecuteNonQueryAsync(token);
            string identity;
            await using(var id=new NpgsqlCommand("SELECT system_identifier::pg_catalog.text || ':' || (SELECT oid::pg_catalog.text FROM pg_catalog.pg_database WHERE datname=pg_catalog.current_database()) FROM pg_catalog.pg_control_system()",connection,transaction))
                identity=(string)(await id.ExecuteScalarAsync(token) ?? throw new VerificationException(Reason.IdentityUnknown));
            if(connection.PostgreSqlVersion.Major!=17) throw new VerificationException(Reason.PlatformNotVerified);
            const string sql="""
            SELECT n.nspname,c.relname,a.attname,t.typname,a.attnum,NOT a.attnotnull,
                   pg_catalog.format_type(a.atttypid,a.atttypmod),pg_catalog.pg_get_expr(d.adbin,d.adrelid),
                   a.attidentity::pg_catalog.text,a.attgenerated::pg_catalog.text,tn.nspname,
                   pg_catalog.has_table_privilege(c.oid,'SELECT'),c.relrowsecurity,c.relkind::pg_catalog.text,
                   cn.nspname,co.collname,c.relhassubclass,am.amname
            FROM pg_catalog.pg_class c JOIN pg_catalog.pg_namespace n ON n.oid=c.relnamespace
            LEFT JOIN pg_catalog.pg_attribute a ON a.attrelid=c.oid AND a.attnum>0 AND NOT a.attisdropped
            LEFT JOIN pg_catalog.pg_type t ON t.oid=a.atttypid LEFT JOIN pg_catalog.pg_namespace tn ON tn.oid=t.typnamespace
            LEFT JOIN pg_catalog.pg_am am ON am.oid=c.relam
            LEFT JOIN pg_catalog.pg_attrdef d ON d.adrelid=c.oid AND d.adnum=a.attnum
            LEFT JOIN pg_catalog.pg_collation co ON co.oid=a.attcollation LEFT JOIN pg_catalog.pg_namespace cn ON cn.oid=co.collnamespace
            WHERE c.relkind IN ('r','p','f') AND pg_catalog.left(n.nspname,3)<>'pg_' AND n.nspname<>'information_schema'
            ORDER BY n.nspname,c.relname,a.attnum
            """;
            var tables=new List<Table>(); var columns=new List<Column>(); string? schema=null,name=null; bool safe=false;
            await using var command=new NpgsqlCommand(sql,connection,transaction); await using var reader=await command.ExecuteReaderAsync(token);
            while(await reader.ReadAsync(token))
            {
                var ns=reader.GetString(0); var table=reader.GetString(1);
                if(schema!=ns || name!=table)
                {
                    if(name!=null) tables.Add(new(schema!,name,columns.ToArray(),safe));
                    columns.Clear(); schema=ns; name=table; safe=reader.GetBoolean(11) && !reader.GetBoolean(12) && reader.GetString(13)=="r" && !reader.GetBoolean(16) && !reader.IsDBNull(17) && reader.GetString(17)=="heap";
                }
                if(reader.IsDBNull(2)) continue;
                var type=reader.GetString(3); var generated=reader.GetString(9);
                // Definition text stays in memory; it is never exported.
                var definition=string.Join('\u001f',reader.GetString(6),reader.IsDBNull(7)?null:reader.GetString(7),reader.GetString(8),generated,reader.IsDBNull(14)?null:reader.GetString(14),reader.IsDBNull(15)?null:reader.GetString(15));
                var codec=reader.GetString(10)=="pg_catalog" && Supported.Contains(type) && generated.Length==0;
                columns.Add(new(reader.GetString(2),type,reader.GetInt16(4),reader.GetBoolean(5),definition,codec));
            }
            if(name!=null) tables.Add(new(schema!,name,columns.ToArray(),safe));
            await reader.DisposeAsync();
            tables=await ReadStructure(tables,token);
            inventory=new(identity,tables,true,"Independent READ ONLY REPEATABLE READ; writes must be stopped; PostgreSQL 17 built-in codecs v1.");
            return inventory;
        }
        catch(PostgresException e) { throw Map(e); }
        catch(NpgsqlException) { throw new VerificationException(Reason.ConnectionLost); }
    }
    public async IAsyncEnumerable<byte[]> RowsAsync(Table table,[EnumeratorCancellation] CancellationToken token)
    {
        if(inventory==null || !inventory.Tables.Contains(table) || !table.SafeRead) throw new VerificationException(Reason.PermissionDenied);
        if(table.Columns.Any(x=>!x.SafeValue)) throw new VerificationException(Reason.UnsupportedType);
        // bpchar::text strips stored padding. Read string columns without that cast.
        var projected=table.Columns.Select(c=>c.Type=="bytea" ? "pg_catalog.encode("+Quote(c.Name)+",'hex')" : c.Type is "text" or "varchar" or "bpchar" ? Quote(c.Name) : Quote(c.Name)+"::pg_catalog.text");
        // No view/routine invocation, ORDER BY collation, LIMIT, OFFSET, or sequence advancement.
        var sql="SELECT "+string.Join(',',projected)+" FROM ONLY "+Quote(table.Schema)+"."+Quote(table.Name);
        await using var command=new NpgsqlCommand(sql,connection,transaction);
        NpgsqlDataReader reader;
        try { reader=await command.ExecuteReaderAsync(CommandBehavior.SequentialAccess,token); } catch(PostgresException e) { throw Map(e); }
        await using(reader)
        {
            while(await Read(reader,token))
            {
                var values=new string?[table.Columns.Count];
                for(var i=0;i<values.Length;i++)
                {
                    if(await reader.IsDBNullAsync(i,token)) continue;
                    using var text=reader.GetTextReader(i); var builder=new System.Text.StringBuilder(); var buffer=new char[4096]; int count;
                    while((count=await text.ReadAsync(buffer.AsMemory(),token))>0)
                    {
                        if((long)(builder.Length+count)*4>ExactRow.MaxRowBytes) throw new VerificationException(Reason.RowTooLarge);
                        builder.Append(buffer,0,count);
                    }
                    values[i]=builder.ToString();
                }
                yield return ExactRow.Encode(values,table.Columns);
            }
        }
    }
    private static async Task<bool> Read(NpgsqlDataReader reader,CancellationToken token)
    { try { return await reader.ReadAsync(token); } catch(PostgresException e) { throw Map(e); } catch(NpgsqlException) { throw new VerificationException(Reason.ConnectionLost); } }
    private static VerificationException Map(PostgresException e) => new(e.SqlState switch { "42501"=>Reason.PermissionDenied,"57014"=>Reason.Timeout,_=>Reason.ObservationChanged });
    private static string Quote(string identifier)=>"\""+identifier.Replace("\"","\"\"",StringComparison.Ordinal)+"\"";
    public async ValueTask DisposeAsync()
    { if(transaction!=null) await transaction.DisposeAsync(); if(connection!=null) await connection.DisposeAsync(); }
}
