using System.Text.Json;
using Verifier.Core;
namespace Verifier.Data;

public sealed partial class MySqlDatabase
{
    private const string CurrentGrantee="CONCAT(QUOTE(LEFT(CURRENT_USER(),LENGTH(CURRENT_USER())-LENGTH(SUBSTRING_INDEX(CURRENT_USER(),'@',-1))-1)),'@',QUOTE(SUBSTRING_INDEX(CURRENT_USER(),'@',-1)))";
    private async Task<(List<Table> Tables,CatalogSection Database)> ReadObjects(List<Table> tables,CancellationToken token)
    {
        var actualDatabase=(await ReadNative("SELECT DATABASE()",null,token)).Single()[0]!;
        var sqlMode=(await ReadNative("SELECT @@SESSION.sql_mode",null,token)).Single()[0]!.Split(',').ToHashSet(StringComparer.Ordinal);
        // Native dictionaries only: never SELECT a view, CALL a routine, enable
        // an event, or invoke a trigger while comparing original and target.
        var global=await ReadNative("SELECT PRIVILEGE_TYPE FROM information_schema.USER_PRIVILEGES WHERE CAST(GRANTEE AS BINARY)=CAST("+CurrentGrantee+" AS BINARY)",null,token);
        var schema=await ReadNative("SELECT PRIVILEGE_TYPE FROM information_schema.SCHEMA_PRIVILEGES WHERE CAST(GRANTEE AS BINARY)=CAST("+CurrentGrantee+" AS BINARY) AND CAST(TABLE_SCHEMA AS BINARY)=CAST(DATABASE() AS BINARY)",null,token);
        bool Global(string privilege)=>global.Any(x=>x[0]==privilege);
        bool Granted(string privilege)=>Global(privilege)||schema.Any(x=>x[0]==privilege);
        var full=globalReadCoverage&&Granted("SHOW VIEW")&&Granted("TRIGGER")&&Granted("EVENT");
        const string db="CAST(TABLE_SCHEMA AS BINARY)=CAST(DATABASE() AS BINARY)";
        var views=await ReadNative("SELECT TABLE_NAME,VIEW_DEFINITION,CHECK_OPTION,IS_UPDATABLE,DEFINER,SECURITY_TYPE,CHARACTER_SET_CLIENT,COLLATION_CONNECTION FROM information_schema.VIEWS WHERE "+db+" ORDER BY CAST(TABLE_NAME AS BINARY)",null,token);
        var viewColumns=await ReadNative("SELECT c.TABLE_NAME,c.COLUMN_NAME,c.ORDINAL_POSITION,c.COLUMN_TYPE,c.IS_NULLABLE,c.CHARACTER_SET_NAME,c.COLLATION_NAME FROM information_schema.COLUMNS c JOIN information_schema.VIEWS v ON CAST(c.TABLE_SCHEMA AS BINARY)=CAST(v.TABLE_SCHEMA AS BINARY) AND CAST(c.TABLE_NAME AS BINARY)=CAST(v.TABLE_NAME AS BINARY) WHERE CAST(c.TABLE_SCHEMA AS BINARY)=CAST(DATABASE() AS BINARY) ORDER BY CAST(c.TABLE_NAME AS BINARY),c.ORDINAL_POSITION",null,token);
        bool viewColumnsObserved;await using(var warnings=Command("SHOW COUNT(*) WARNINGS"))viewColumnsObserved=Convert.ToInt64(await warnings.ExecuteScalarAsync(token))==0;
        var usedTables=await ReadNative("SELECT VIEW_NAME,TABLE_SCHEMA,TABLE_NAME FROM information_schema.VIEW_TABLE_USAGE WHERE CAST(VIEW_SCHEMA AS BINARY)=CAST(DATABASE() AS BINARY) ORDER BY CAST(VIEW_NAME AS BINARY),CAST(TABLE_SCHEMA AS BINARY),CAST(TABLE_NAME AS BINARY)",null,token);
        var usedRoutines=await ReadNative("SELECT TABLE_NAME,SPECIFIC_SCHEMA,SPECIFIC_NAME FROM information_schema.VIEW_ROUTINE_USAGE WHERE "+db+" ORDER BY CAST(TABLE_NAME AS BINARY),CAST(SPECIFIC_SCHEMA AS BINARY),CAST(SPECIFIC_NAME AS BINARY)",null,token);
        var triggers=await ReadNative("SELECT TRIGGER_NAME,EVENT_OBJECT_TABLE,EVENT_MANIPULATION,ACTION_ORDER,ACTION_STATEMENT,ACTION_ORIENTATION,ACTION_TIMING,SQL_MODE,DEFINER,CHARACTER_SET_CLIENT,COLLATION_CONNECTION,DATABASE_COLLATION FROM information_schema.TRIGGERS WHERE CAST(TRIGGER_SCHEMA AS BINARY)=CAST(DATABASE() AS BINARY) ORDER BY CAST(TRIGGER_NAME AS BINARY)",null,token);
        var routines=await ReadNative("SELECT ROUTINE_NAME,ROUTINE_TYPE,DTD_IDENTIFIER,ROUTINE_BODY,ROUTINE_DEFINITION,EXTERNAL_NAME,EXTERNAL_LANGUAGE,PARAMETER_STYLE,IS_DETERMINISTIC,SQL_DATA_ACCESS,SECURITY_TYPE,SQL_MODE,ROUTINE_COMMENT,DEFINER,CHARACTER_SET_CLIENT,COLLATION_CONNECTION,DATABASE_COLLATION FROM information_schema.ROUTINES WHERE CAST(ROUTINE_SCHEMA AS BINARY)=CAST(DATABASE() AS BINARY) ORDER BY CAST(ROUTINE_NAME AS BINARY),ROUTINE_TYPE",null,token);
        var parameters=await ReadNative("SELECT SPECIFIC_NAME,ROUTINE_TYPE,ORDINAL_POSITION,PARAMETER_MODE,PARAMETER_NAME,DTD_IDENTIFIER,CHARACTER_SET_NAME,COLLATION_NAME FROM information_schema.PARAMETERS WHERE CAST(SPECIFIC_SCHEMA AS BINARY)=CAST(DATABASE() AS BINARY) ORDER BY CAST(SPECIFIC_NAME AS BINARY),ROUTINE_TYPE,ORDINAL_POSITION",null,token);
        var events=await ReadNative("SELECT EVENT_NAME,DEFINER,TIME_ZONE,EVENT_BODY,EVENT_DEFINITION,EVENT_TYPE,EXECUTE_AT,INTERVAL_VALUE,INTERVAL_FIELD,SQL_MODE,STARTS,ENDS,STATUS,ON_COMPLETION,EVENT_COMMENT,CHARACTER_SET_CLIENT,COLLATION_CONNECTION,DATABASE_COLLATION FROM information_schema.EVENTS WHERE CAST(EVENT_SCHEMA AS BINARY)=CAST(DATABASE() AS BINARY) ORDER BY CAST(EVENT_NAME AS BINARY)",null,token);
        var all=new List<CatalogFact>();var gaps=new List<CatalogGap>();var viewFacts=new Dictionary<string,CatalogFact>(StringComparer.Ordinal);
        foreach(var v in views)
        {
            var key=JsonSerializer.Serialize(new[]{"view",v[0]});
            // VIEWS uses an empty string (not only NULL) for hidden definitions.
            if(string.IsNullOrEmpty(v[1])){gaps.Add(new(key,null,"RelatedViews","",Reason.VisibilityUnknown));continue;}
            var links=usedTables.Where(x=>x[0]==v[0]).Select(x=>new{database=Reference(x[1]),name=x[2]}).ToArray();
            var calls=usedRoutines.Where(x=>x[0]==v[0]).Select(x=>new{database=Reference(x[1]),name=x[2]}).ToArray();
            var fact=new CatalogFact(key,null,"RelatedViews",JsonSerializer.Serialize(v.Skip(2)));all.Add(fact);viewFacts.Add(v[0]!,fact);
            if(globalReadCoverage)all.Add(new(key,null,"ViewDependencies",JsonSerializer.Serialize(new{links,calls})));
            else gaps.Add(new(key,null,"ViewDependencies","",Reason.VisibilityUnknown));
            if(globalReadCoverage&&viewColumnsObserved)all.Add(new(key,null,"ViewSignature",JsonSerializer.Serialize(viewColumns.Where(c=>c[0]==v[0]).Select(c=>c.Skip(1).ToArray()))));
            else gaps.Add(new(key,null,"ViewSignature","",viewColumnsObserved?Reason.VisibilityUnknown:Reason.ObservationChanged));
            var body=MySqlViewDefinition.Observe(v[1]!,actualDatabase,usedTables.Where(x=>x[0]==v[0]&&x[1]==actualDatabase).Select(x=>x[2]!).ToHashSet(StringComparer.Ordinal),usedRoutines.Where(x=>x[0]==v[0]&&x[1]==actualDatabase).Select(x=>x[2]!).ToHashSet(StringComparer.Ordinal),sqlMode.Contains("NO_BACKSLASH_ESCAPES"),sqlMode.Contains("ANSI_QUOTES"));
            if(body==null)gaps.Add(new(key,null,"ViewDefinition","",Reason.NotImplemented));else all.Add(new(key,null,"ViewDefinition",body));
        }
        foreach(var row in triggers)all.Add(new(JsonSerializer.Serialize(new[]{"trigger",row[0]}),null,"TriggerDefinitions",JsonSerializer.Serialize(row.Skip(1))));
        foreach(var row in routines)
        {
            var key=JsonSerializer.Serialize(new[]{"routine",row[1],row[0]});
            if(row[4]==null){gaps.Add(new(key,null,"RelatedRoutines","",Reason.VisibilityUnknown));continue;}
            all.Add(new(key,null,"RelatedRoutines",JsonSerializer.Serialize(new{definition=row.Skip(1).ToArray(),parameters=parameters.Where(x=>x[0]==row[0]&&x[1]==row[1]).Select(x=>x.Skip(2).ToArray()).ToArray()})));
        }
        foreach(var row in events)
        {var key=JsonSerializer.Serialize(new[]{"event",row[0]});if(row[4]==null)gaps.Add(new(key,null,"EventDefinitions","",Reason.VisibilityUnknown));else all.Add(new(key,null,"EventDefinitions",JsonSerializer.Serialize(row.Skip(1))));}
        // MySQL supplies view dependencies, but not a complete catalog graph of
        // SQL inside routines/triggers/events (including dynamic SQL). Do not
        // parse names heuristically or present these bodies as executed tests.
        var opaque=routines.Count>0||triggers.Count>0||events.Count>0;
        if(opaque)gaps.Add(new("database",null,"ObjectDependencies","",Reason.NotImplemented));
        var database=Section(all,gaps,full);
        var result=new List<Table>();
        foreach(var table in tables)
        {
            var facts=all.Where(f=>f.Attribute=="TriggerDefinitions"&&triggers.Any(r=>r[1]==table.Name&&JsonSerializer.Serialize(new[]{"trigger",r[0]})==f.Key)).ToList();
            var related=new HashSet<string>(StringComparer.Ordinal){table.Name};
            bool changed;do{changed=false;foreach(var row in usedTables.Where(r=>r[1]==table.Schema&&related.Contains(r[2]!)))changed|=related.Add(row[0]!);}while(changed);
            foreach(var name in related.Where(n=>n!=table.Name))if(viewFacts.TryGetValue(name,out var fact))facts.AddRange(all.Where(f=>f.Key==fact.Key));
            var localGaps=gaps.Where(g=>related.Any(n=>JsonSerializer.Serialize(new[]{"view",n})==g.Key)).ToList();
            if(opaque)localGaps.Add(new("table",null,"ObjectDependencies","",Reason.NotImplemented));
            result.Add(table with{Catalog=table.Catalog.Append(Section(facts,localGaps,full)).ToArray()});
        }
        return(result,database);
        object? Reference(string? name)=>name==null?null:new{ownDatabase=name==actualDatabase,name=name==actualDatabase?null:name};
        static CatalogSection Section(List<CatalogFact> facts,List<CatalogGap> gaps,bool complete)
        {var reason=!complete||gaps.Any(g=>g.Reason==Reason.VisibilityUnknown)?Reason.VisibilityUnknown:gaps.Count>0?Reason.NotImplemented:Reason.None;return new("OBJECT-01",facts,reason){Gaps=gaps,CompleteExceptGaps=complete&&gaps.Count>0};}
    }
}
