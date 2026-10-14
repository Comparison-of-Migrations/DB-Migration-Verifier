using System.Data;
using System.Text.Json;
using MySqlConnector;
using Verifier.Core;
namespace Verifier.Data;

public sealed partial class MySqlDatabase(ConnectionSettings settings):IReadDatabase,IStreamingReadDatabase,IIntegrityDatabase,ICatalogObservationDatabase
{
    private MySqlConnection? connection;private MySqlTransaction? transaction;private Inventory? inventory;
    private MySqlDatabase? metadataObserver;
    private bool globalReadCoverage;
    private readonly Dictionary<string,string> tableDefinitions=new(StringComparer.Ordinal);
    private readonly Dictionary<string,HashSet<string>> unicodeColumns=new(StringComparer.Ordinal);
    private static string Quote(string value)=>"`"+value.Replace("`","``")+"`";
    private static readonly HashSet<string> Supported=new(StringComparer.Ordinal){"tinyint","smallint","mediumint","int","bigint","decimal","float","double","bit","date","datetime","timestamp","time","year","char","varchar","tinytext","text","mediumtext","longtext","binary","varbinary","tinyblob","blob","mediumblob","longblob","enum","set","geometry","point","linestring","polygon","multipoint","multilinestring","multipolygon","geomcollection","geometrycollection","json"};
    private MySqlCommand Command(string sql)=>connection is{State:ConnectionState.Open}?new(sql,connection,transaction):throw new VerificationException(Reason.ConnectionLost);
    public async Task<Inventory> InspectAsync(CancellationToken token)
    {
        if(inventory!=null)return inventory;
        try
        {
            connection=new(settings.MySqlString());await connection.OpenAsync(token);
            await using(var version=Command("SELECT VERSION()"))
            {var text=(string)(await version.ExecuteScalarAsync(token))!;if(!text.StartsWith("8.4.",StringComparison.Ordinal)||text.Contains("MariaDB",StringComparison.OrdinalIgnoreCase))throw new VerificationException(Reason.PlatformNotVerified);}
            // Session-only state; never change global options or persistent data.
            await using(var setup=Command("SET SESSION time_zone='+00:00';SET SESSION cte_max_recursion_depth=65536;SET SESSION show_gipk_in_create_table_and_information_schema=ON;SET SESSION information_schema_stats_expiry=0"))await setup.ExecuteNonQueryAsync(token);
            transaction=await connection.BeginTransactionAsync(IsolationLevel.RepeatableRead,true,token);
            string identity;bool complete;
            await using(var q=Command("SELECT @@server_uuid,DATABASE(),@@lower_case_table_names"))
            await using(var r=await q.ExecuteReaderAsync(token))
            {
                if(!await r.ReadAsync(token)||r.IsDBNull(0)||r.IsDBNull(1)||!Guid.TryParse(r.GetString(0),out var uuid))throw new VerificationException(Reason.IdentityUnknown);
                var db=r.GetString(1);if(r.GetInt32(2)!=0)db=db.ToLowerInvariant();identity=JsonSerializer.Serialize(new[]{uuid.ToString(),db});
            }
            // INFORMATION_SCHEMA hides objects outside grants. Direct schema/global
            // SELECT establishes this table inventory; inherited-role visibility is
            // not inferred from the mere presence of a few readable tables.
            bool globalSelect;
            await using(var q=Command("SELECT EXISTS(SELECT 1 FROM information_schema.USER_PRIVILEGES WHERE CAST(GRANTEE AS BINARY)=CAST("+CurrentGrantee+" AS BINARY) AND PRIVILEGE_TYPE='SELECT')"))globalSelect=Convert.ToInt32(await q.ExecuteScalarAsync(token))==1;
            var schemaGrants=await ReadNative("SELECT TABLE_SCHEMA,PRIVILEGE_TYPE FROM information_schema.SCHEMA_PRIVILEGES WHERE CAST(GRANTEE AS BINARY)=CAST("+CurrentGrantee+" AS BINARY) AND IF(@@GLOBAL.partial_revokes,CAST(TABLE_SCHEMA AS BINARY)=CAST(DATABASE() AS BINARY),CONVERT(DATABASE() USING utf8mb3) COLLATE utf8mb3_bin LIKE TABLE_SCHEMA ESCAPE 0x5c)",null,token);
            // Native grant patterns include escaped identifiers. Every matching
            // candidate must grant SELECT; do not union weaker/stronger patterns
            // and guess which mysql.db row the server would choose first.
            var schemaSelect=schemaGrants.Count>0&&schemaGrants.GroupBy(x=>x[0],StringComparer.Ordinal).All(g=>g.Any(x=>x[1]=="SELECT"));
            var coverage=globalSelect?await ReadGlobalSelectCoverage(token):(Selected:false,All:false);globalReadCoverage=coverage.All;complete=schemaSelect||coverage.Selected;
            var tables=new List<Table>();
            await using(var q=Command("SELECT TABLE_SCHEMA,TABLE_NAME,ENGINE FROM information_schema.TABLES WHERE CAST(TABLE_SCHEMA AS BINARY)=CAST(DATABASE() AS BINARY) AND TABLE_TYPE='BASE TABLE' ORDER BY CAST(TABLE_NAME AS BINARY)"))
            await using(var r=await q.ExecuteReaderAsync(token))
            {while(await r.ReadAsync(token))tables.Add(new(r.GetString(0),r.GetString(1),[],!r.IsDBNull(2)&&r.GetString(2)=="InnoDB"){ReadReason=!r.IsDBNull(2)&&r.GetString(2)=="InnoDB"?Reason.None:Reason.NotImplemented});}
            for(var i=0;i<tables.Count;i++)
            {
                var table=tables[i];var (columns,facts,signature)=await ReadColumns(table,token);
                facts.Add(new("table",null,"TableKind","\"BASE TABLE\""));
                var metadata=await ReadStructure(table,token);tableDefinitions[table.Qualified]=signature+metadata.Signature;
                var allocator=await ReadAutoIncrement(table,columns,token);
                var readReason=await ReadTablePermission(table,columns,token);
                tables[i]=table with{Columns=columns,SafeRead=readReason==Reason.None,SafeLocalRead=readReason==Reason.None,ReadReason=readReason,RowKeys=metadata.Keys,Partitioned=metadata.Partitioned,Catalog=[new("SCHEMA-01",facts,Reason.None),metadata.Constraints,metadata.Indexes,allocator,await ReadColumnTypes(table,columns,facts,token)]};
            }
            var objects=await ReadObjects(tables,token);tables=objects.Tables;
            var access=await ReadAccess(tables,token);tables=access.Tables;
            var database=await ReadNative("SELECT DEFAULT_CHARACTER_SET_NAME,DEFAULT_COLLATION_NAME,DEFAULT_ENCRYPTION FROM information_schema.SCHEMATA WHERE CAST(SCHEMA_NAME AS BINARY)=CAST(DATABASE() AS BINARY)",null,token);
            return inventory=new(identity,tables,complete,"Independent READ ONLY REPEATABLE READ; InnoDB stored values; writes stopped required; metadata and auto-increment counters are observations, not a cross-server or DDL snapshot."){DatabaseIsNamespace=true,DatabaseCatalog=[new("SCHEMA-01",database.Count==1?[new("database",null,"DatabaseDefinition",JsonSerializer.Serialize(database[0]))]:[],database.Count==1?Reason.None:Reason.VisibilityUnknown),access.Database,objects.Database,new("OBJECT-02",[],Reason.None),new("OBJECT-03",[],Reason.None)]};
        }
        catch(MySqlException e){throw Map(e);}
    }
    private async Task<(IReadOnlyList<Column> Columns,List<CatalogFact> Facts,string Signature)> ReadColumns(Table table,CancellationToken token)
    {
        await using var q=Command("SELECT COLUMN_NAME,DATA_TYPE,COLUMN_TYPE,ORDINAL_POSITION,IS_NULLABLE,COLUMN_DEFAULT,EXTRA,GENERATION_EXPRESSION,CHARACTER_SET_NAME,COLLATION_NAME,SRS_ID,COLUMN_COMMENT FROM information_schema.COLUMNS WHERE CAST(TABLE_SCHEMA AS BINARY)=CAST(@schema AS BINARY) AND CAST(TABLE_NAME AS BINARY)=CAST(@table AS BINARY) ORDER BY ORDINAL_POSITION");q.Parameters.AddWithValue("schema",table.Schema);q.Parameters.AddWithValue("table",table.Name);
        var columns=new List<Column>();var facts=new List<CatalogFact>();var unicode=new HashSet<string>(StringComparer.Ordinal);
        await using var r=await q.ExecuteReaderAsync(token);
        while(await r.ReadAsync(token))
        {
            var name=r.GetString(0);var type=r.GetString(1);var full=r.GetString(2);var position=r.GetInt32(3);var nullable=r.GetString(4)=="YES";
            if(!r.IsDBNull(8)&&r.GetString(8)!="binary")unicode.Add(name);
            var definition=JsonSerializer.Serialize(Enumerable.Range(2,10).Select(i=>r.IsDBNull(i)?null:Convert.ToString(r.GetValue(i),System.Globalization.CultureInfo.InvariantCulture)).ToArray());
            // MySQL 8.4 rejects stored/loadable functions, subqueries and session
            // variables in generated expressions. Native virtual values can be
            // observed read-only; evaluation warnings still invalidate the read.
            columns.Add(new(name,full,position,nullable,definition,Supported.Contains(type)));
            facts.Add(new(name,name,"ColumnName",JsonSerializer.Serialize(name)));facts.Add(new(name,name,"ColumnComment",r.IsDBNull(11)?null:r.GetString(11)));facts.Add(new(name,name,"ColumnIdentity",r.GetString(6).Contains("auto_increment",StringComparison.Ordinal)?"auto_increment":null));
            facts.Add(new(name,name,"ColumnType",JsonSerializer.Serialize(new[]{full,r.IsDBNull(8)?null:r.GetString(8)})));facts.Add(new(name,name,"ColumnPosition",position.ToString(System.Globalization.CultureInfo.InvariantCulture)));
            facts.Add(new(name,name,"ColumnNullable",nullable?"true":"false"));facts.Add(new(name,name,"ColumnDefault",r.IsDBNull(5)?null:JsonSerializer.Serialize(r.GetString(5))));
            facts.Add(new(name,name,"ColumnGenerated",JsonSerializer.Serialize(new[]{r.GetString(6),r.GetString(7)})));facts.Add(new(name,name,"ColumnCollation",r.IsDBNull(9)?null:JsonSerializer.Serialize(r.GetString(9))));
            facts.Add(new(name,name,"ColumnOptions",r.IsDBNull(10)?null:Convert.ToString(r.GetValue(10),System.Globalization.CultureInfo.InvariantCulture)));
        }
        unicodeColumns[table.Qualified]=unicode;
        return(columns,facts,JsonSerializer.Serialize(columns));
    }
    private static VerificationException Map(MySqlException e)=>new(e.ErrorCode==MySqlErrorCode.CommandTimeoutExpired?Reason.Timeout:e.Number switch{1044 or 1045 or 1142 or 1143 or 1227=>Reason.PermissionDenied,1205 or 1317 or 3024=>Reason.Timeout,1146 or 1054 or 1412=>Reason.ObservationChanged,_=>Reason.ConnectionLost});
    private async Task CheckWarnings(CancellationToken token)
    {await using var q=Command("SHOW COUNT(*) WARNINGS");if(Convert.ToInt64(await q.ExecuteScalarAsync(token))!=0)throw new VerificationException(Reason.ObservationChanged);}
    public async ValueTask DisposeAsync()
    {try{if(metadataObserver!=null)await metadataObserver.DisposeAsync();}finally{try{if(transaction!=null)await transaction.DisposeAsync();}finally{if(connection!=null)await connection.DisposeAsync();}}}
}
