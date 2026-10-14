using System.Data;
using System.Runtime.CompilerServices;
using Npgsql;
using Verifier.Core;

namespace Verifier.Data;

public sealed partial class PostgreSqlDatabase(ConnectionSettings settings) : IReadDatabase,IIntegrityDatabase,IStreamingReadDatabase,ICatalogObservationDatabase
{
    private NpgsqlConnection? connection;
    private NpgsqlTransaction? transaction;
    private Inventory? inventory;
    // Endpoint-local relation identity, never compared between databases or exported.
    private readonly Dictionary<string,uint> relationIdentities=new(StringComparer.Ordinal);
    private const string IdentitySql="SELECT system_identifier::pg_catalog.text || ':' || (SELECT oid::pg_catalog.text FROM pg_catalog.pg_database WHERE datname=pg_catalog.current_database()) FROM pg_catalog.pg_control_system()";
    private static readonly HashSet<string> Supported=new(StringComparer.Ordinal) { "int2","int4","int8","text","varchar","bpchar","bool","uuid","numeric","bytea" };
    private static readonly IReadOnlyDictionary<string,string> BinaryCodecs=new Dictionary<string,string>(StringComparer.Ordinal)
    {
        ["float4"]="float4send",["float8"]="float8send",["date"]="date_send",["time"]="time_send",["timetz"]="timetz_send",
        ["timestamp"]="timestamp_send",["timestamptz"]="timestamptz_send",["interval"]="interval_send",["json"]="json_send",["jsonb"]="jsonb_send",
        ["bit"]="bit_send",["varbit"]="varbit_send",["inet"]="inet_send",["cidr"]="cidr_send",["macaddr"]="macaddr_send",["macaddr8"]="macaddr8_send",
        ["pg_lsn"]="pg_lsn_send",["point"]="point_send",["lseg"]="lseg_send",["path"]="path_send",["box"]="box_send",["polygon"]="poly_send",["circle"]="circle_send"
    };
    private static readonly HashSet<string> ArrayCodecs=Supported.Concat(BinaryCodecs.Keys).Select(t=>"_"+t).ToHashSet(StringComparer.Ordinal);
    public async Task<Inventory> InspectAsync(CancellationToken token)
    {
        if(inventory!=null) return inventory;
        try
        {
            connection=new(settings.PostgreSqlString()); await connection.OpenAsync(token);
            transaction=await connection.BeginTransactionAsync(IsolationLevel.RepeatableRead,token);
            await using(var config=new NpgsqlCommand("SET TRANSACTION READ ONLY; SET LOCAL row_security=off; SET LOCAL search_path=pg_catalog; SET LOCAL jit=off; SET LOCAL TimeZone='UTC'",connection,transaction)) await config.ExecuteNonQueryAsync(token);
            string identity;
            await using(var id=new NpgsqlCommand(IdentitySql,connection,transaction))
                identity=(string)(await id.ExecuteScalarAsync(token) ?? throw new VerificationException(Reason.IdentityUnknown));
            if(connection.PostgreSqlVersion.Major!=17) throw new VerificationException(Reason.PlatformNotVerified);
            await InspectDeparseSafety(token);
            const string sql="""
            WITH RECURSIVE descendants(root,child) AS (
                SELECT inhparent,inhrelid FROM pg_catalog.pg_inherits
                UNION SELECT d.root,i.inhrelid FROM descendants d JOIN pg_catalog.pg_inherits i ON i.inhparent=d.child
            )
            SELECT n.nspname,c.relname,a.attname,t.typname,a.attnum,NOT a.attnotnull,
                   pg_catalog.format_type(a.atttypid,a.atttypmod),pg_catalog.pg_get_expr(d.adbin,d.adrelid),
                   a.attidentity::pg_catalog.text,a.attgenerated::pg_catalog.text,tn.nspname,
                   pg_catalog.has_table_privilege(c.oid,'SELECT'),c.relrowsecurity,c.relkind::pg_catalog.text,
                   cn.nspname,co.collname,c.relhassubclass,am.amname,
                   ARRAY(SELECT pg_catalog.json_build_array(nn.nspname,cc.relname)::pg_catalog.text FROM descendants ds
                     JOIN pg_catalog.pg_class cc ON cc.oid=ds.child JOIN pg_catalog.pg_namespace nn ON nn.oid=cc.relnamespace WHERE ds.root=c.oid ORDER BY nn.nspname,cc.relname),
                   NOT EXISTS(SELECT 1 FROM descendants ds JOIN pg_catalog.pg_class cc ON cc.oid=ds.child
                     LEFT JOIN pg_catalog.pg_am ca ON ca.oid=cc.relam
                     WHERE ds.root=c.oid AND (cc.relrowsecurity OR cc.relkind NOT IN ('r','p') OR cc.relkind='r' AND ca.amname IS DISTINCT FROM 'heap')),t.typelem,t.oid,d.adbin::pg_catalog.text,a.atttypmod,c.relispopulated,c.oid
            FROM pg_catalog.pg_class c JOIN pg_catalog.pg_namespace n ON n.oid=c.relnamespace
            LEFT JOIN pg_catalog.pg_attribute a ON a.attrelid=c.oid AND a.attnum>0 AND NOT a.attisdropped
            LEFT JOIN pg_catalog.pg_type t ON t.oid=a.atttypid LEFT JOIN pg_catalog.pg_namespace tn ON tn.oid=t.typnamespace
            LEFT JOIN pg_catalog.pg_am am ON am.oid=c.relam
            LEFT JOIN pg_catalog.pg_attrdef d ON d.adrelid=c.oid AND d.adnum=a.attnum
            LEFT JOIN pg_catalog.pg_collation co ON co.oid=a.attcollation LEFT JOIN pg_catalog.pg_namespace cn ON cn.oid=co.collnamespace
            WHERE c.relkind IN ('r','p','f','m') AND pg_catalog.left(n.nspname,3)<>'pg_' AND n.nspname<>'information_schema'
            ORDER BY n.nspname,c.relname,a.attnum
            """;
            await using(var save=new NpgsqlCommand("SAVEPOINT dmv_inventory",connection,transaction))await save.ExecuteNonQueryAsync(token);
            List<Table> tables;
            try { tables=await ReadTables(GuardDeparsers(sql),token); }
            catch(PostgresException e) when(e.SqlState=="42501")
            {
                await using(var restore=new NpgsqlCommand("ROLLBACK TO SAVEPOINT dmv_inventory; RELEASE SAVEPOINT dmv_inventory",connection,transaction))await restore.ExecuteNonQueryAsync(token);
                return inventory=new(identity,[],false,"Authenticated database identity verified; inventory query denied. Absence is not established.");
            }
            await using(var release=new NpgsqlCommand("RELEASE SAVEPOINT dmv_inventory",connection,transaction))await release.ExecuteNonQueryAsync(token);
            tables=await ReadCustomValueCodecs(tables,token);
            tables=await ReadRowKeys(tables,token);
            tables=await ReadStructure(tables,token);
            tables=await ReadSequenceStates(tables,token);
            var databaseCatalog=await ReadDatabaseCatalog(token);
            readShapes=await ReadShapes(connection,transaction,relationIdentities.Values.ToArray(),token);
            inventory=new(identity,tables,true,"Independent READ ONLY REPEATABLE READ; writes must be stopped; PostgreSQL 17 explicit scalar/recursive streaming codecs v3."){DatabaseCatalog=databaseCatalog};
            return inventory;
        }
        catch(PostgresException e) { throw Map(e); }
        catch(NpgsqlException e) { throw Map(e); }
    }
    private async Task<List<Table>> ReadTables(string sql,CancellationToken token)
    {
        var tables=new List<Table>(); var columns=new List<Column>(); string? schema=null,name=null; bool safe=false,localSafe=false,partitioned=false;string[] descendants=[];var readReason=Reason.None;
        await using var command=new NpgsqlCommand(sql,connection,transaction); await using var reader=await command.ExecuteReaderAsync(token);
        while(await reader.ReadAsync(token))
        {
        var ns=reader.GetString(0); var table=reader.GetString(1);
        if(schema!=ns || name!=table)
        {
            if(name!=null) tables.Add(new(schema!,name,columns.ToArray(),safe){DataDescendants=descendants,Partitioned=partitioned,SafeLocalRead=localSafe,ReadReason=readReason});
            columns.Clear(); schema=ns; name=table; partitioned=reader.GetString(13)=="p";
            relationIdentities[new Table(ns,table,[],false).Qualified]=reader.GetFieldValue<uint>(25);
            var kind=reader.GetString(13);var heap=!reader.IsDBNull(17)&&reader.GetString(17)=="heap";
            if(kind=="m")materializedRelations.Add(new Table(ns,table,[],false).Qualified);
            readReason=!reader.GetBoolean(11)?Reason.PermissionDenied:reader.GetBoolean(12)||!reader.GetBoolean(19)?Reason.RowFiltered:kind=="m"&&!reader.GetBoolean(24)?Reason.Unpopulated:!(partitioned||kind is "r" or "m"&&heap)?Reason.NotImplemented:Reason.None;
            safe=readReason==Reason.None;
            localSafe=reader.GetBoolean(11)&&!reader.GetBoolean(12)&&(kind=="r"||kind=="m"&&reader.GetBoolean(24))&&heap;
            descendants=reader.GetFieldValue<string[]>(18).Select(q=>{using var parts=System.Text.Json.JsonDocument.Parse(q);return new Table(parts.RootElement[0].GetString()!,parts.RootElement[1].GetString()!,[],false).Qualified;}).ToArray();
        }
        if(reader.IsDBNull(2)) continue;
        var type=reader.GetString(3); var generated=reader.GetString(9);
        // Definition text stays in memory; it is never exported.
        var typeDefinition=reader.GetString(6).StartsWith(BlockedDefinition,StringComparison.Ordinal)?reader.GetString(10)+"."+type+":"+reader.GetInt32(23).ToString(System.Globalization.CultureInfo.InvariantCulture):reader.GetString(6);
        var definition=string.Join('\u001f',typeDefinition,reader.IsDBNull(7)?null:reader.GetString(7).StartsWith(BlockedDefinition,StringComparison.Ordinal)?"\u001e"+(reader.IsDBNull(22)?"":reader.GetString(22)):reader.GetString(7),reader.GetString(8),generated,reader.IsDBNull(14)?null:reader.GetString(14),reader.IsDBNull(15)?null:reader.GetString(15));
        var codec=reader.GetString(10)=="pg_catalog" && (Supported.Contains(type)||BinaryCodecs.ContainsKey(type)||ArrayCodecs.Contains(type)) && generated is "" or "s";
        columns.Add(new(reader.GetString(2),reader.GetString(10)=="pg_catalog"?type:new Table(reader.GetString(10),type,[],false).Qualified,reader.GetInt16(4),reader.GetBoolean(5),definition,codec){ElementOid=reader.IsDBNull(20)?0:reader.GetFieldValue<uint>(20),TypeOid=reader.IsDBNull(21)?0:reader.GetFieldValue<uint>(21)});
        }
        if(name!=null) tables.Add(new(schema!,name,columns.ToArray(),safe){DataDescendants=descendants,Partitioned=partitioned,SafeLocalRead=localSafe,ReadReason=readReason});
        await reader.DisposeAsync();
        return tables;
    }
    public async IAsyncEnumerable<byte[]> RowsAsync(Table table,[EnumeratorCancellation] CancellationToken token)
    {
        if(inventory==null || !inventory.Tables.Contains(table)) throw new VerificationException(Reason.PermissionDenied);
        if(!table.SafeRead)throw new VerificationException(table.ReadReason);
        if(table.Columns.Any(x=>!x.SafeValue)) throw new VerificationException(Reason.UnsupportedType);
        EnsureReadSession(token);
        await ValidateReadScope(table,token);
        // bpchar::text strips stored padding. Read string columns without that cast.
        var projected=table.Columns.Select(c=>ValueProjection(c,false));
        // No view/routine invocation, ORDER BY collation, LIMIT, OFFSET, or sequence advancement.
        var sql="SELECT "+string.Join(',',projected)+" FROM "+(table.DataDescendants.Count>0?"":"ONLY ")+Quote(table.Schema)+"."+Quote(table.Name);
        await using var command=new NpgsqlCommand(sql,connection,transaction);
        NpgsqlDataReader reader;
        try { reader=await command.ExecuteReaderAsync(CommandBehavior.SequentialAccess,token); }
        catch(NpgsqlException e) { throw ReadFailure(e); }
        catch(InvalidOperationException) when(connection?.State!=ConnectionState.Open){throw LostReadSession();}
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
                    if(customValueCodecs.TryGetValue(table.Columns[i].TypeOid,out var custom))
                    {
                        var bytes=Convert.FromHexString(values[i]!);
                        values[i]=Convert.ToHexString(NormalizeCustom(bytes,custom));
                    }
                    else if(ArrayCodecs.Contains(table.Columns[i].Type))values[i]=Convert.ToHexString(PostgreSqlArrayCodec.Normalize(Convert.FromHexString(values[i]!),table.Columns[i].Type=="_numeric",table.Columns[i].ElementOid));
                }
                yield return ExactRow.Encode(values,table.Columns);
            }
        }
        await ValidateReadScope(table,token);
    }
    private async Task<bool> Read(NpgsqlDataReader reader,CancellationToken token)
    { try { return await reader.ReadAsync(token); } catch(NpgsqlException e) { throw ReadFailure(e); } catch(InvalidOperationException) when(connection?.State!=ConnectionState.Open){throw LostReadSession();} }
    private static VerificationException Map(PostgresException e) => new(e.SqlState switch { "42501"=>Reason.PermissionDenied,"28000" or "28P01" or "3D000" or "57P01" or "57P02" or "57P03" or "57P04"=>Reason.ConnectionLost,"57014" or "55P03" or "25P03" or "25P04"=>Reason.Timeout,var state when state.StartsWith("08",StringComparison.Ordinal)=>Reason.ConnectionLost,_=>Reason.ObservationChanged });
    private static string Quote(string identifier)=>"\""+identifier.Replace("\"","\"\"",StringComparison.Ordinal)+"\"";
    public async ValueTask DisposeAsync()
    { try{if(transaction!=null) await transaction.DisposeAsync();}finally{try{if(connection!=null) await connection.DisposeAsync();}finally{if(readObserver!=null)await readObserver.DisposeAsync();}} }
}
