using System.Globalization;
using System.Text.Json;
using Microsoft.Data.SqlClient;
using Verifier.Core;
namespace Verifier.Data;

public sealed partial class SqlServerDatabase
{
    internal const string SequenceQuery="""
        SELECT q.object_id,CONVERT(varbinary(max),CONVERT(nvarchar(max),s.name)) AS [__dbmv_utf16],CONVERT(varbinary(max),CONVERT(nvarchar(max),q.name)) AS [__dbmv_utf16],CONVERT(varbinary(max),CONVERT(nvarchar(max),ts.name)) AS [__dbmv_utf16],CONVERT(varbinary(max),CONVERT(nvarchar(max),ty.name)) AS [__dbmv_utf16],CONVERT(varbinary(max),CONVERT(nvarchar(max),bt.name)) AS [__dbmv_utf16],q.precision,q.scale,
               CONVERT(varchar(100),q.start_value),CONVERT(varchar(100),q.increment),
               CONVERT(varchar(100),q.minimum_value),CONVERT(varchar(100),q.maximum_value),
               q.is_cycling,q.is_cached,q.cache_size,CONVERT(varbinary(max),CONVERT(nvarchar(max),p.name)) AS [__dbmv_utf16],
               CONVERT(varchar(172),p.sid,2),CONVERT(varchar(100),q.current_value),
               CONVERT(varchar(100),q.last_used_value),q.is_exhausted
        FROM sys.sequences q JOIN sys.schemas s ON s.schema_id=q.schema_id
        JOIN sys.types ty ON ty.user_type_id=q.user_type_id JOIN sys.schemas ts ON ts.schema_id=ty.schema_id
        LEFT JOIN sys.types bt ON bt.user_type_id=q.system_type_id AND bt.user_type_id=bt.system_type_id
        LEFT JOIN sys.database_principals p ON p.principal_id=COALESCE(q.principal_id,s.principal_id)
        WHERE q.is_ms_shipped=0 ORDER BY s.name COLLATE Latin1_General_100_BIN2,q.name COLLATE Latin1_General_100_BIN2
        """;
    internal const string DependencyQuery="""
        SELECT d.referencing_id,d.referenced_id,CONVERT(varbinary(max),CONVERT(nvarchar(max),rs.name)) AS [__dbmv_utf16],CONVERT(varbinary(max),CONVERT(nvarchar(max),COALESCE(ro.name,tr.name))) AS [__dbmv_utf16],d.referencing_minor_id,d.referenced_minor_id,
          CONVERT(varbinary(max),CONVERT(nvarchar(max),d.referenced_server_name)) AS [__dbmv_utf16],CONVERT(varbinary(max),CONVERT(nvarchar(max),d.referenced_database_name)) AS [__dbmv_utf16],
          CONVERT(varbinary(max),CONVERT(nvarchar(max),COALESCE(ts.name,tys.name,xs.name,d.referenced_schema_name))) AS [__dbmv_utf16],CONVERT(varbinary(max),CONVERT(nvarchar(max),COALESCE(t.name,ty.name,xc.name,d.referenced_entity_name))) AS [__dbmv_utf16],
          d.referenced_class,d.is_schema_bound_reference,d.is_caller_dependent,d.is_ambiguous,
          CONVERT(varbinary(max),CONVERT(nvarchar(max),CASE WHEN ro.type='SP' AND pp.security_predicate_id IS NOT NULL AND pp.predicate_definition IS NOT NULL
            AND ps.name IS NOT NULL AND po.name IS NOT NULL
            THEN CONCAT(QUOTENAME(ps.name),'.',QUOTENAME(po.name),':',pp.predicate_type,':',pp.operation,':',DATALENGTH(pp.predicate_definition),':',pp.predicate_definition)
            ELSE rc.name END COLLATE Latin1_General_100_BIN2)) AS [__dbmv_utf16],CONVERT(varbinary(max),CONVERT(nvarchar(max),tc.name)) AS [__dbmv_utf16]
        FROM sys.sql_expression_dependencies d
        LEFT JOIN sys.objects ro ON ro.object_id=d.referencing_id AND d.referencing_class=1
        LEFT JOIN sys.triggers tr ON tr.object_id=d.referencing_id AND tr.parent_class=0 AND d.referencing_class=12
        LEFT JOIN sys.schemas rs ON rs.schema_id=ro.schema_id
        LEFT JOIN sys.objects t ON t.object_id=d.referenced_id AND d.referenced_class=1
        LEFT JOIN sys.schemas ts ON ts.schema_id=t.schema_id
        LEFT JOIN sys.types ty ON ty.user_type_id=d.referenced_id AND d.referenced_class=6
        LEFT JOIN sys.schemas tys ON tys.schema_id=ty.schema_id
        LEFT JOIN sys.xml_schema_collections xc ON xc.xml_collection_id=d.referenced_id AND d.referenced_class=10
        LEFT JOIN sys.schemas xs ON xs.schema_id=xc.schema_id
        LEFT JOIN sys.columns rc ON rc.object_id=d.referencing_id AND rc.column_id=d.referencing_minor_id
        LEFT JOIN sys.security_predicates pp ON pp.object_id=d.referencing_id AND pp.security_predicate_id=d.referencing_minor_id AND ro.type='SP'
        LEFT JOIN sys.objects po ON po.object_id=pp.target_object_id
        LEFT JOIN sys.schemas ps ON ps.schema_id=po.schema_id
        LEFT JOIN sys.columns tc ON tc.object_id=d.referenced_id AND tc.column_id=d.referenced_minor_id
        WHERE COALESCE(ro.is_ms_shipped,tr.is_ms_shipped)=0
        ORDER BY rs.name COLLATE Latin1_General_100_BIN2,COALESCE(ro.name,tr.name) COLLATE Latin1_General_100_BIN2,
          d.referencing_minor_id,d.referenced_server_name,d.referenced_database_name,
          COALESCE(ts.name,tys.name,xs.name,d.referenced_schema_name) COLLATE Latin1_General_100_BIN2,
          COALESCE(t.name,ty.name,xc.name,d.referenced_entity_name) COLLATE Latin1_General_100_BIN2,d.referenced_minor_id
        """;
    internal const string SynonymQuery="""
        SELECT sy.object_id,CONVERT(varbinary(max),CONVERT(nvarchar(max),s.name)) AS [__dbmv_utf16],CONVERT(varbinary(max),CONVERT(nvarchar(max),sy.name)) AS [__dbmv_utf16],CONVERT(varbinary(max),CONVERT(nvarchar(max),sy.base_object_name)) AS [__dbmv_utf16],CONVERT(varbinary(max),CONVERT(nvarchar(max),p.name)) AS [__dbmv_utf16],t.object_id,CONVERT(varbinary(max),CONVERT(nvarchar(max),ts.name)) AS [__dbmv_utf16],CONVERT(varbinary(max),CONVERT(nvarchar(max),t.name)) AS [__dbmv_utf16]
        FROM sys.synonyms sy JOIN sys.schemas s ON s.schema_id=sy.schema_id
        LEFT JOIN sys.database_principals p ON p.principal_id=COALESCE(sy.principal_id,s.principal_id)
        LEFT JOIN sys.schemas ts ON ts.name=PARSENAME(sy.base_object_name,2)
          AND PARSENAME(sy.base_object_name,4) IS NULL
          AND (PARSENAME(sy.base_object_name,3) IS NULL OR PARSENAME(sy.base_object_name,3)=DB_NAME())
        LEFT JOIN sys.objects t ON t.schema_id=ts.schema_id AND t.name=PARSENAME(sy.base_object_name,1)
        WHERE sy.is_ms_shipped=0
        ORDER BY s.name COLLATE Latin1_General_100_BIN2,sy.name COLLATE Latin1_General_100_BIN2
        """;
    private async Task<(IReadOnlyList<CatalogSection> Sections,IReadOnlySet<string>? Selected)> ReadObjects(Table? table,CancellationToken token)
    {
        var scope=table??new Table("","",[],false);
        try
        {
            var visibility=await NativeShared("SELECT HAS_PERMS_BY_NAME(DB_NAME(),'DATABASE','VIEW DEFINITION')",scope,token);
            var visible=complete&&visibility.Count==1&&visibility[0][0]=="1";
            var objects=await NativeShared("SELECT o.object_id,o.parent_object_id,CONVERT(varbinary(max),CONVERT(nvarchar(max),s.name)) AS [__dbmv_utf16],CONVERT(varbinary(max),CONVERT(nvarchar(max),o.name)) AS [__dbmv_utf16],o.type FROM sys.objects o JOIN sys.schemas s ON s.schema_id=o.schema_id WHERE o.is_ms_shipped=0 UNION ALL SELECT object_id,0,NULL,CONVERT(varbinary(max),CONVERT(nvarchar(max),name)) AS [__dbmv_utf16],type FROM sys.triggers WHERE parent_class=0 AND is_ms_shipped=0 ORDER BY 1",scope,token);
            var dependencies=await NativeShared(DependencyQuery,scope,token);
            var synonyms=await NativeShared(SynonymQuery,scope,token);
            HashSet<string> selected=table==null?objects.Select(x=>x[0]!).ToHashSet(StringComparer.Ordinal):
                objects.Where(x=>x[2]==table.Schema&&x[3]==table.Name&&x[4]=="U ").Select(x=>x[0]!).ToHashSet(StringComparer.Ordinal);
            // SQL char(2) providers preserve padding; normalize the fixed type
            // tag only, never user names or stored values.
            if(table!=null&&selected.Count==0)selected=objects.Where(x=>x[2]==table.Schema&&x[3]==table.Name&&x[4]?.TrimEnd()=="U").Select(x=>x[0]!).ToHashSet(StringComparer.Ordinal);
            if(table!=null&&selected.Count!=1)throw new VerificationException(Reason.ObservationChanged);
            if(table!=null)
            {
                var synonymEdges=synonyms.Where(r=>r[5]!=null).Select(r=>new string?[]{r[0],r[5],r[1],r[2],"0","0",null,null,r[6],r[7],"1","False","False","False",null,null});
                ExpandObjectScope(selected,objects,dependencies.Concat(synonymEdges),token);
            }
            var facts=CollectCatalogFacts(token);var gaps=CollectCatalogGaps(token);
            var synonymSection=SqlServerCatalog.Synonyms(synonyms.Where(r=>selected.Contains(r[0]!)),visible,facts,gaps);
            var moduleQuery="""
                SELECT o.object_id,CONVERT(varbinary(max),CONVERT(nvarchar(max),s.name)) AS [__dbmv_utf16],CONVERT(varbinary(max),CONVERT(nvarchar(max),o.name)) AS [__dbmv_utf16],o.type,CONVERT(varbinary(max),CONVERT(nvarchar(max),m.definition)) AS [__dbmv_utf16],m.uses_ansi_nulls,m.uses_quoted_identifier,
                  m.is_schema_bound,m.uses_database_collation,m.is_recompiled,m.null_on_null_input,
                  CONVERT(varbinary(max),CONVERT(nvarchar(max),CASE WHEN m.execute_as_principal_id=-2 THEN 'OWNER' WHEN m.execute_as_principal_id IS NULL THEN 'CALLER' ELSE p.name END)) AS [__dbmv_utf16],
                  m.uses_native_compilation,m.is_inlineable,m.inline_type,
                  CONVERT(varbinary(max),CONVERT(nvarchar(max),own.name)) AS [__dbmv_utf16],CONVERT(varbinary(max),CONVERT(nvarchar(max),so.name)) AS [__dbmv_utf16],o.is_published,o.is_schema_published
                FROM sys.objects o JOIN sys.schemas s ON s.schema_id=o.schema_id
                LEFT JOIN sys.sql_modules m ON m.object_id=o.object_id
                LEFT JOIN sys.database_principals p ON p.principal_id=m.execute_as_principal_id
                LEFT JOIN sys.database_principals own ON own.principal_id=o.principal_id
                LEFT JOIN sys.database_principals so ON so.principal_id=s.principal_id
                WHERE o.is_ms_shipped=0 AND o.type IN ('P','RF','V','TR','FN','IF','TF','R','D','PC','FS','FT','AF','TA')
                  AND (o.type<>'D' OR o.parent_object_id=0) /*SCOPE*/
                ORDER BY s.name COLLATE Latin1_General_100_BIN2,o.name COLLATE Latin1_General_100_BIN2
                """;
            // Fetch bodies only for the already discovered dependency scope;
            // unrelated module bodies must not be decoded once per table.
            using var moduleBodies=new ProtectedCatalogStore(token);
            var modules=await ReadModuleRows(moduleQuery,selected,table==null,scope,moduleBodies,token);
            var definitions=moduleBodies.Snapshot(0,moduleBodies.Count);var moduleOrdinal=0;
            foreach(var row in modules)
            {
                var key=SqlServerCatalog.Key("module",row[1],row[2],row[3]);var attribute=row[3]?.TrimEnd() switch{"V"=>"RelatedViews","TR" or "TA"=>"TriggerDefinitions",_=>"RelatedRoutines"};
                var definition=definitions[moduleOrdinal++];
                if(definition.Key!=key||definition.Attribute!=attribute||definition.Column!=null)throw new VerificationException(Reason.TemporaryIntegrity);
                if(row[4]==null)gaps.Add(new(key,null,attribute,"",Reason.VisibilityUnknown));
                else facts.Add(definition);
                if(row[11]==null||row[16]==null)gaps.Add(new(key,null,attribute,"",Reason.VisibilityUnknown));
                var parameters=await Native("""
                    SELECT p.parameter_id,CONVERT(varbinary(max),CONVERT(nvarchar(max),p.name)) AS [__dbmv_utf16],CONVERT(varbinary(max),CONVERT(nvarchar(max),ts.name)) AS [__dbmv_utf16],CONVERT(varbinary(max),CONVERT(nvarchar(max),t.name)) AS [__dbmv_utf16],p.max_length,p.precision,p.scale,p.is_output,p.is_cursor_ref,
                      p.is_readonly,p.is_nullable,p.has_default_value,CONVERT(varbinary(max),CONVERT(nvarchar(max),xs.name)) AS [__dbmv_utf16],CONVERT(varbinary(max),CONVERT(nvarchar(max),xc.name)) AS [__dbmv_utf16],p.is_xml_document,p.encryption_type
                    FROM sys.parameters p JOIN sys.types t ON t.user_type_id=p.user_type_id JOIN sys.schemas ts ON ts.schema_id=t.schema_id
                    LEFT JOIN sys.xml_schema_collections xc ON xc.xml_collection_id=p.xml_collection_id
                    LEFT JOIN sys.schemas xs ON xs.schema_id=xc.schema_id
                    WHERE p.object_id=@object ORDER BY p.parameter_id
                    """.Replace("@object",int.Parse(row[0]!,CultureInfo.InvariantCulture).ToString(CultureInfo.InvariantCulture),StringComparison.Ordinal),scope,token);
                var columns=await Native("""
                    SELECT c.column_id,CONVERT(varbinary(max),CONVERT(nvarchar(max),c.name)) AS [__dbmv_utf16],CONVERT(varbinary(max),CONVERT(nvarchar(max),ts.name)) AS [__dbmv_utf16],CONVERT(varbinary(max),CONVERT(nvarchar(max),t.name)) AS [__dbmv_utf16],c.max_length,c.precision,c.scale,c.is_nullable,CONVERT(varbinary(max),CONVERT(nvarchar(max),c.collation_name)) AS [__dbmv_utf16],
                      CONVERT(varbinary(max),CONVERT(nvarchar(max),xs.name)) AS [__dbmv_utf16],CONVERT(varbinary(max),CONVERT(nvarchar(max),xc.name)) AS [__dbmv_utf16],c.is_xml_document,c.is_identity,c.is_computed
                    FROM sys.columns c JOIN sys.types t ON t.user_type_id=c.user_type_id JOIN sys.schemas ts ON ts.schema_id=t.schema_id
                    LEFT JOIN sys.xml_schema_collections xc ON xc.xml_collection_id=c.xml_collection_id
                    LEFT JOIN sys.schemas xs ON xs.schema_id=xc.schema_id
                    WHERE c.object_id=@object ORDER BY c.column_id
                    """.Replace("@object",int.Parse(row[0]!,CultureInfo.InvariantCulture).ToString(CultureInfo.InvariantCulture),StringComparison.Ordinal),scope,token);
                facts.Add(new(SqlServerCatalog.Key("signature",row[1],row[2],row[3]),null,row[3]?.TrimEnd()=="V"?"ViewSignature":"RelatedRoutines",SqlServerMetadata.Serialize(new{parameters,columns})));
                // SQL modules can contain dynamic SQL invisible to this catalog.
                // Reading a definition is not proof of a complete runtime graph.
                if(row[3]?.TrimEnd() is "P" or "RF" or "TR" or "FN" or "IF" or "TF" or "PC" or "FS" or "FT" or "AF" or "TA")
                    gaps.Add(new(key,null,"ObjectDependencies","",Reason.UnsafeObservation));
            }
            var triggers=await NativeShared("""
                SELECT tr.object_id,CONVERT(varbinary(max),CONVERT(nvarchar(max),s.name)) AS [__dbmv_utf16],CONVERT(varbinary(max),CONVERT(nvarchar(max),o.name)) AS [__dbmv_utf16],tr.is_disabled,tr.is_instead_of_trigger,tr.is_not_for_replication,
                  tr.parent_class,CONVERT(varbinary(max),CONVERT(nvarchar(max),ps.name)) AS [__dbmv_utf16],CONVERT(varbinary(max),CONVERT(nvarchar(max),po.name)) AS [__dbmv_utf16],e.type,e.is_first,e.is_last
                FROM sys.triggers tr JOIN sys.objects o ON o.object_id=tr.object_id JOIN sys.schemas s ON s.schema_id=o.schema_id
                LEFT JOIN sys.objects po ON po.object_id=tr.parent_id LEFT JOIN sys.schemas ps ON ps.schema_id=po.schema_id
                LEFT JOIN sys.trigger_events e ON e.object_id=tr.object_id WHERE tr.is_ms_shipped=0
                ORDER BY s.name COLLATE Latin1_General_100_BIN2,o.name COLLATE Latin1_General_100_BIN2,e.type
                """,scope,token);
            foreach(var group in triggers.Where(x=>selected.Contains(x[0]!)).GroupBy(x=>x[0]!))
            {var row=group.First();facts.Add(new(SqlServerCatalog.Key("trigger-binding",row[1],row[2]),null,"TriggerDefinitions",SqlServerMetadata.Serialize(group.Select(x=>x[3..]))));}
            var dependencySection=SqlServerCatalog.Dependencies(dependencies.Where(x=>selected.Contains(x[0]!)),visible,facts,gaps);
            visible=visible&&(dependencySection.Reason==Reason.None||dependencySection.CompleteExceptGaps);
            await AppendDdlTriggers("""
                SELECT tr.object_id,CONVERT(varbinary(max),CONVERT(nvarchar(max),tr.name)) AS [__dbmv_utf16],tr.type,CONVERT(varbinary(max),CONVERT(nvarchar(max),m.definition)) AS [__dbmv_utf16],tr.is_disabled,tr.is_not_for_replication,
                  m.uses_ansi_nulls,m.uses_quoted_identifier,m.is_schema_bound,m.uses_database_collation,m.is_recompiled,
                  CONVERT(varbinary(max),CONVERT(nvarchar(max),CASE WHEN m.execute_as_principal_id=-2 THEN 'OWNER' WHEN m.execute_as_principal_id IS NULL THEN 'CALLER' ELSE p.name END)) AS [__dbmv_utf16],
                  e.type,CONVERT(varbinary(max),CONVERT(nvarchar(max),e.type_desc)) AS [__dbmv_utf16],e.is_first,e.is_last
                FROM sys.triggers tr LEFT JOIN sys.sql_modules m ON m.object_id=tr.object_id
                LEFT JOIN sys.database_principals p ON p.principal_id=m.execute_as_principal_id
                LEFT JOIN sys.trigger_events e ON e.object_id=tr.object_id
                WHERE tr.parent_class=0 AND tr.is_ms_shipped=0 /*SCOPE*/
                ORDER BY tr.name COLLATE Latin1_General_100_BIN2,e.type
                """,selected,table==null,scope,facts,gaps,token);
            var sequences=await NativeShared(SequenceQuery,scope,token);
            return([SqlServerCatalog.Section("OBJECT-01",facts.Snapshot(),visible,gaps.Snapshot()),
                SqlServerCatalog.Sequences(sequences.Where(x=>selected.Contains(x[0]!)).Select(x=>x[1..]),visible,CollectCatalogFacts(token),CollectCatalogGaps(token))],selected);
        }
        catch(SqlException e){return([new("OBJECT-01",[],FaultReason(e,token)),new("OBJECT-02",[],FaultReason(e,token))],null);}
    }
    internal static void ExpandObjectScope(HashSet<string> selected,IReadOnlyList<string?[]> objects,IEnumerable<string?[]> dependencies,CancellationToken token=default)
    {
        token.ThrowIfCancellationRequested();
        var edges=new Dictionary<string,List<string>>(selected.Comparer);
        foreach(var row in objects)
        {
            token.ThrowIfCancellationRequested();
            if(row[1]!=null)Connect(row[1]!,row[0]!); // Ownership expands only toward children.
        }
        foreach(var row in dependencies)
        {
            token.ThrowIfCancellationRequested();
            if(row[1]==null||row.Length>10&&row[10]!="1")continue;
            Connect(row[0]!,row[1]!);Connect(row[1]!,row[0]!);
        }
        var pending=new Queue<string>(selected);
        while(pending.TryDequeue(out var id))
        {
            token.ThrowIfCancellationRequested();
            if(!edges.TryGetValue(id,out var adjacent))continue;
            foreach(var next in adjacent)
            {
                token.ThrowIfCancellationRequested();
                if(selected.Add(next))pending.Enqueue(next);
            }
        }
        void Connect(string from,string to)
        {if(!edges.TryGetValue(from,out var adjacent))edges.Add(from,adjacent=[]);adjacent.Add(to);}
    }
    private async Task<Table> AddObjects(Table table,CancellationToken token)
    {
        var objects=await ReadObjects(table,token);var sections=objects.Sections;var identity=table.Catalog.Single(x=>x.CheckId=="OBJECT-02");var sequence=sections.Single(x=>x.CheckId=="OBJECT-02");
        var reason=identity.Reason!=Reason.None?identity.Reason:sequence.Reason;
        var facts=CollectCatalogFacts(token);facts.AddRange(identity.Facts);facts.AddRange(sequence.Facts);
        var gaps=CollectCatalogGaps(token);gaps.AddRange(identity.Gaps);gaps.AddRange(sequence.Gaps);
        var merged=new CatalogSection("OBJECT-02",facts.Snapshot(),reason)
        {Gaps=gaps.Snapshot(),CompleteExceptGaps=reason!=Reason.None&&identity.Reason==Reason.None&&sequence.CompleteExceptGaps};
        return table with{Catalog=table.Catalog.Where(x=>x.CheckId!="OBJECT-02").Concat(new[]{sections[0],merged,await ReadTypes(table,token,objects.Selected)}).ToArray()};
    }
}
