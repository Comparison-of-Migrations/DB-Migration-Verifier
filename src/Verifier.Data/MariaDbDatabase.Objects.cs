using System.Text.Json;
using MySqlConnector;
using Verifier.Core;
namespace Verifier.Data;

public sealed partial class MariaDbDatabase
{
    private async Task<(List<Table> Tables,CatalogSection Database)> ReadObjects(List<Table> tables,CancellationToken token)
    {
        var own=(await Native("SELECT DATABASE()",null,token)).Single()[0]!;
        var mode=(await Native("SELECT @@SESSION.sql_mode",null,token)).Single()[0]!.Split(',').ToHashSet(StringComparer.Ordinal);
        var tableNames=(await Native("SELECT TABLE_NAME FROM information_schema.TABLES WHERE BINARY TABLE_SCHEMA=BINARY DATABASE() ORDER BY BINARY TABLE_NAME",null,token)).Select(r=>r[0]!).ToHashSet(StringComparer.Ordinal);
        var visibleViews=(await Native("SELECT TABLE_NAME FROM information_schema.TABLES WHERE BINARY TABLE_SCHEMA=BINARY DATABASE() AND TABLE_TYPE='VIEW' ORDER BY BINARY TABLE_NAME",null,token)).Select(r=>r[0]!).ToHashSet(StringComparer.Ordinal);
        const string db="BINARY TABLE_SCHEMA=BINARY DATABASE()";
        // MariaDB 11.4 has ALGORITHM but no VIEW_TABLE_USAGE or
        // VIEW_ROUTINE_USAGE. Do not borrow MySQL's nonexistent catalogs or
        // execute views/functions/EXPLAIN to manufacture a dependency graph.
        var views=await Native("SELECT TABLE_NAME,VIEW_DEFINITION,CHECK_OPTION,IS_UPDATABLE,DEFINER,SECURITY_TYPE,CHARACTER_SET_CLIENT,COLLATION_CONNECTION,ALGORITHM FROM information_schema.VIEWS WHERE "+db+" ORDER BY BINARY TABLE_NAME",null,token);
        var triggers=await Native("SELECT TRIGGER_NAME,EVENT_OBJECT_TABLE,EVENT_MANIPULATION,ACTION_ORDER,ACTION_CONDITION,ACTION_STATEMENT,ACTION_ORIENTATION,ACTION_TIMING,ACTION_REFERENCE_OLD_TABLE,ACTION_REFERENCE_NEW_TABLE,ACTION_REFERENCE_OLD_ROW,ACTION_REFERENCE_NEW_ROW,SQL_MODE,DEFINER,CHARACTER_SET_CLIENT,COLLATION_CONNECTION,DATABASE_COLLATION FROM information_schema.TRIGGERS WHERE BINARY TRIGGER_SCHEMA=BINARY DATABASE() ORDER BY BINARY TRIGGER_NAME",null,token);
        var routines=await Native("SELECT ROUTINE_NAME,ROUTINE_TYPE,DTD_IDENTIFIER,ROUTINE_BODY,ROUTINE_DEFINITION,EXTERNAL_NAME,EXTERNAL_LANGUAGE,PARAMETER_STYLE,IS_DETERMINISTIC,SQL_DATA_ACCESS,SQL_PATH,SECURITY_TYPE,SQL_MODE,ROUTINE_COMMENT,DEFINER,CHARACTER_SET_CLIENT,COLLATION_CONNECTION,DATABASE_COLLATION FROM information_schema.ROUTINES WHERE BINARY ROUTINE_SCHEMA=BINARY DATABASE() ORDER BY BINARY ROUTINE_NAME,ROUTINE_TYPE",null,token);
        var parameters=await Native("SELECT SPECIFIC_NAME,ROUTINE_TYPE,ORDINAL_POSITION,PARAMETER_MODE,PARAMETER_NAME,DTD_IDENTIFIER,DATA_TYPE,CHARACTER_MAXIMUM_LENGTH,CHARACTER_OCTET_LENGTH,NUMERIC_PRECISION,NUMERIC_SCALE,DATETIME_PRECISION,CHARACTER_SET_NAME,COLLATION_NAME FROM information_schema.PARAMETERS WHERE BINARY SPECIFIC_SCHEMA=BINARY DATABASE() ORDER BY BINARY SPECIFIC_NAME,ROUTINE_TYPE,ORDINAL_POSITION",null,token);
        var events=await Native("SELECT EVENT_NAME,DEFINER,TIME_ZONE,EVENT_BODY,EVENT_DEFINITION,EVENT_TYPE,EXECUTE_AT,INTERVAL_VALUE,INTERVAL_FIELD,SQL_MODE,STARTS,ENDS,STATUS,ON_COMPLETION,EVENT_COMMENT,CHARACTER_SET_CLIENT,COLLATION_CONNECTION,DATABASE_COLLATION FROM information_schema.EVENTS WHERE BINARY EVENT_SCHEMA=BINARY DATABASE() ORDER BY BINARY EVENT_NAME",null,token);
        var triggerComplete=await ScopePrivilege("TRIGGER",token);var eventComplete=await ScopePrivilege("EVENT",token);
        var inventoryVisible=await InventoryVisible(token);var viewComplete=inventoryVisible&&visibleViews.SetEquals(views.Select(v=>v[0]!));
        var routineComplete=false;
        try{var native=await Native("SELECT name,type FROM mysql.proc WHERE BINARY db=BINARY DATABASE() ORDER BY BINARY name,type",null,token);routineComplete=native.Select(r=>JsonSerializer.Serialize(r)).SequenceEqual(routines.Select(r=>JsonSerializer.Serialize(r.Take(2))));}
        catch(MySqlException e) when(e.Number is 1044 or 1142 or 1143 or 1227){/* Known bodies remain comparable; missing names are not absent. */}
        var complete=triggerComplete&&eventComplete&&viewComplete&&routineComplete;
        var facts=new List<CatalogFact>();var gaps=new List<CatalogGap>();
        var routineNames=routines.Select(r=>r[0]!).ToHashSet(StringComparer.Ordinal);
        var unprovenViews=visibleViews.Except(views.Select(v=>v[0]!),StringComparer.Ordinal).ToHashSet(StringComparer.Ordinal);
        for(var pass=0;pass<=views.Count;pass++){var changed=false;foreach(var v in views)if(!unprovenViews.Contains(v[0]!)&&(string.IsNullOrEmpty(v[1])||!SafeViewMetadataCalls(v[1]!,own,unprovenViews,mode.Contains("NO_BACKSLASH_ESCAPES"),mode.Contains("ANSI_QUOTES"))))changed|=unprovenViews.Add(v[0]!);if(!changed)break;}
        foreach(var v in views)
        {
            var key=JsonSerializer.Serialize(new[]{"view",v[0]});
            if(string.IsNullOrEmpty(v[1])){gaps.Add(new(key,null,"RelatedViews","",Reason.VisibilityUnknown));gaps.Add(new(key,null,"ViewDefinition","",Reason.VisibilityUnknown));continue;}
            facts.Add(new(key,null,"RelatedViews",JsonSerializer.Serialize(v.Where((_,i)=>i>=2&&i!=3))));
            // The shared lexer only recognizes typed own-namespace qualifiers
            // against observed native names; it never infers dependencies,
            // rewrites literals/comments, or executes the returned definition.
            var body=v[1]!.Length<=65536?MySqlViewDefinition.Observe(v[1]!,own,tableNames,routineNames,mode.Contains("NO_BACKSLASH_ESCAPES"),mode.Contains("ANSI_QUOTES")):null;
            if(body==null)gaps.Add(new(key,null,"ViewDefinition","",Reason.NotImplemented));else facts.Add(new(key,null,"ViewDefinition",body));
            if(!inventoryVisible||unprovenViews.Contains(v[0]!))
            {gaps.Add(new(key,null,"ViewSignature","",Reason.NotImplemented));gaps.Add(new(key,null,"ViewUpdatability","",Reason.NotImplemented));continue;}
            facts.Add(new(key,null,"ViewUpdatability",JsonSerializer.Serialize(v[3])));
            try
            {
                var view=new Table(own,v[0]!,[],true);var columns=await Native("SELECT COLUMN_NAME,ORDINAL_POSITION,COLUMN_TYPE,IS_NULLABLE,CHARACTER_SET_NAME,COLLATION_NAME FROM information_schema.COLUMNS WHERE "+TableFilter+" ORDER BY ORDINAL_POSITION",view,token);await CheckWarnings(token);
                if(columns.Count==0)throw new VerificationException(Reason.ObservationChanged);facts.Add(new(key,null,"ViewSignature",JsonSerializer.Serialize(columns)));
            }
            catch(MySqlException e) when(e.Number is 1356 or 1146 or 1054){gaps.Add(new(key,null,"ViewSignature","",Reason.ObservationChanged));}
            catch(MySqlException e) when(e.Number is 1044 or 1142 or 1143 or 1227){gaps.Add(new(key,null,"ViewSignature","",Reason.PermissionDenied));}
            catch(VerificationException e){gaps.Add(new(key,null,"ViewSignature","",e.Reason));}
        }
        foreach(var r in routines)
        {
            var key=JsonSerializer.Serialize(new[]{"routine",r[1],r[0]});
            if(r[4]==null)gaps.Add(new(key,null,"RelatedRoutines","",Reason.VisibilityUnknown));else facts.Add(new(key,null,"RelatedRoutines",JsonSerializer.Serialize(new{definition=r.Skip(1),parameters=parameters.Where(p=>p[0]==r[0]&&p[1]==r[1]).Select(p=>p.Skip(2).ToArray())})));
        }
        foreach(var t in triggers)facts.Add(new(JsonSerializer.Serialize(new[]{"trigger",t[0]}),null,"TriggerDefinitions",JsonSerializer.Serialize(t.Skip(1))));
        foreach(var e in events)
        {var key=JsonSerializer.Serialize(new[]{"event",e[0]});if(e[4]==null)gaps.Add(new(key,null,"EventDefinitions","",Reason.VisibilityUnknown));else facts.Add(new(key,null,"EventDefinitions",JsonSerializer.Serialize(e.Skip(1))));}
        var opaque=views.Count>0||routines.Count>0||triggers.Count>0||events.Count>0;
        if(opaque)gaps.Add(new("database",null,"ObjectDependencies","",Reason.NotImplemented));
        var result=new List<Table>();foreach(var table in tables)
        {
            var local=facts.Where(f=>f.Attribute=="TriggerDefinitions"&&triggers.Any(t=>t[1]==table.Name&&f.Key==JsonSerializer.Serialize(new[]{"trigger",t[0]}))).ToList();
            var localGaps=new List<CatalogGap>();if(opaque||!complete)localGaps.Add(new("table",null,"ObjectDependencies","",complete?Reason.NotImplemented:Reason.VisibilityUnknown));
            result.Add(table with{Catalog=table.Catalog.Append(Section(local,localGaps,triggerComplete&&complete)).ToArray()});
        }
        return(result,Section(facts,gaps,complete));
        static CatalogSection Section(List<CatalogFact> facts,List<CatalogGap> gaps,bool complete)
        {var reason=!complete?Reason.VisibilityUnknown:gaps.Count>0?gaps[0].Reason:Reason.None;return new("OBJECT-01",facts,reason){Gaps=gaps,CompleteExceptGaps=complete&&gaps.Count>0};}
    }
    // Metadata resolution is not safe just because a stored function declares
    // NO SQL/DETERMINISTIC. Inspect only proven unqualified native calls. The
    // lexer mode disables namespace recognition solely for this call inventory.
    private static bool SafeViewMetadataCalls(string sql,string own,IReadOnlySet<string> unprovenViews,bool noBackslash,bool ansiQuotes)
    {
        if(sql.Length>65536)return false;var tokens=MySqlViewDefinition.Observe(sql,"",new HashSet<string>(),new HashSet<string>(),noBackslash,ansiQuotes,false);if(tokens==null)return false;
        using var json=JsonDocument.Parse(tokens);if(json.RootElement.GetArrayLength()>8192)return false;
        if(json.RootElement.EnumerateArray().Any(t=>t[0].GetString()=="comment"&&(t[1].GetString()!.StartsWith("/*!",StringComparison.Ordinal)||t[1].GetString()!.StartsWith("/*M!",StringComparison.Ordinal))))return false;
        var significant=json.RootElement.EnumerateArray().Where(t=>t[0].GetString() is not ("space" or "comment")).Select(t=>(Kind:t[0].GetString(),Text:t[1].GetString()!)).ToArray();
        string? Identifier(int i){if(i<0||i>=significant.Length)return null;var t=significant[i];return t.Kind=="identifier"?t.Text[1..^1].Replace(new string(t.Text[0],2),t.Text[0].ToString(),StringComparison.Ordinal):t.Kind=="word"?t.Text:null;}
        var boundSources=new HashSet<string>(StringComparer.Ordinal);var fromDepths=new HashSet<int>();var depth=0;var expectSource=false;
        for(var i=0;i<significant.Length;i++)
        {
            var t=significant[i];var word=t.Kind=="word"?t.Text.ToUpperInvariant():"";
            if(word=="WITH")return false; // CTE name binding is not proved here.
            if(t.Text=="("){if(expectSource&&(i+1>=significant.Length||significant[i+1].Text.ToUpperInvariant()!="SELECT"))return false;expectSource=false;depth++;continue;}
            if(t.Text==")"){fromDepths.Remove(depth);depth--;continue;}
            if(word is "FROM" or "JOIN"){fromDepths.Add(depth);expectSource=true;continue;}
            if(word is "WHERE" or "GROUP" or "HAVING" or "ORDER" or "LIMIT" or "WINDOW" or "UNION" or "EXCEPT" or "INTERSECT"){fromDepths.Remove(depth);expectSource=false;continue;}
            if(t.Text==","&&fromDepths.Contains(depth)){expectSource=true;continue;}
            if(!expectSource)continue;
            var name=Identifier(i);if(name==null)return false;
            if(i+1<significant.Length&&significant[i+1].Text=="."){if(name!=own||Identifier(i+2) is not {} qualified)return false;name=qualified;i+=2;}
            boundSources.Add(name);expectSource=false;
        }
        if(expectSource||depth!=0)return false;
        for(var i=0;i<significant.Length;i++)
        {
            if(significant[i].Text=="@"||Identifier(i) is {} name&&unprovenViews.Contains(name))return false;
            if(significant[i].Text==".")
            {
                var prefix=Identifier(i-1);var numeric=prefix!=null&&prefix.All(char.IsAsciiDigit)&&Identifier(i+1) is {} suffix&&suffix.All(char.IsAsciiDigit);
                // Accept only own native qualifiers (including their column
                // suffix) or numeric dots. External schemas/ambiguous aliases
                // remain unverified; do not resolve their views indirectly.
                if(!numeric&&prefix!=own&&!boundSources.Contains(prefix??"")&&!(i>=3&&significant[i-2].Text=="."&&Identifier(i-3)==own))return false;
            }
        }
        for(var i=0;i+1<significant.Length;i++)if(significant[i+1].Text=="("&&significant[i].Kind is "word" or "identifier")
        {
            if(significant[i].Kind!="word"||i>0&&significant[i-1].Text==".")return false;
            if(significant[i].Text.ToUpperInvariant() is not ("ABS" or "COALESCE" or "IFNULL" or "NULLIF" or "LOWER" or "LCASE" or "UPPER" or "UCASE" or "CHAR_LENGTH" or "OCTET_LENGTH" or "JSON_VALID" or "JSON_EXTRACT" or "COUNT" or "SUM" or "MIN" or "MAX" or "AVG" or "CAST" or "CONVERT" or "CHAR" or "VARCHAR" or "DECIMAL" or "IN" or "EXISTS" or "OVER"))return false;
        }
        return true;
    }
}
