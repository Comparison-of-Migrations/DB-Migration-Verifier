using System.Data;
using System.Globalization;
using System.Security.Cryptography;
using System.Text.Json;
using Verifier.Core;

namespace Verifier.Data;

public sealed partial class SqlServerDatabase
{
    // Catalog bytes only: never load an assembly into the client or invoke a
    // user's CLR entry point/serializer on the server.
    private async Task<CatalogSection> WithAssemblyDefinitions(CatalogSection section,Table scope,HashSet<string>? typeIds,IReadOnlySet<string>? objectIds,bool databaseScope,CancellationToken token,CatalogFacts? collected=null,CatalogGaps? collectedGaps=null)
    {
        var assemblies=await NativeShared("""
            SELECT a.assembly_id,CONVERT(varbinary(max),CONVERT(nvarchar(max),a.name)) AS [__dbmv_utf16],CONVERT(varbinary(max),CONVERT(nvarchar(max),a.clr_name)) AS [__dbmv_utf16],CONVERT(varbinary(max),CONVERT(nvarchar(max),a.permission_set_desc)) AS [__dbmv_utf16],a.is_visible,CONVERT(varbinary(max),CONVERT(nvarchar(max),p.name)) AS [__dbmv_utf16]
            FROM sys.assemblies a LEFT JOIN sys.database_principals p ON p.principal_id=a.principal_id
            WHERE a.is_user_defined=1 ORDER BY a.name COLLATE Latin1_General_100_BIN2
            """,scope,token);
        if(assemblies.Count==0)return section;
        var modules=objectIds==null&&!databaseScope?[]:await NativeTypeScope("""
            SELECT m.assembly_id,CONVERT(varbinary(max),CONVERT(nvarchar(max),s.name)) AS [__dbmv_utf16],CONVERT(varbinary(max),CONVERT(nvarchar(max),o.name)) AS [__dbmv_utf16],o.type,CONVERT(varbinary(max),CONVERT(nvarchar(max),m.assembly_class)) AS [__dbmv_utf16],CONVERT(varbinary(max),CONVERT(nvarchar(max),m.assembly_method)) AS [__dbmv_utf16],m.null_on_null_input,
              CONVERT(varbinary(max),CONVERT(nvarchar(max),CASE WHEN m.execute_as_principal_id=-2 THEN 'OWNER' WHEN m.execute_as_principal_id IS NULL THEN 'CALLER' ELSE p.name END)) AS [__dbmv_utf16]
            FROM sys.assembly_modules m JOIN sys.objects o ON o.object_id=m.object_id
            JOIN sys.schemas s ON s.schema_id=o.schema_id
            LEFT JOIN sys.database_principals p ON p.principal_id=m.execute_as_principal_id
            WHERE o.is_ms_shipped=0 /*SCOPE*/ ORDER BY s.name COLLATE Latin1_General_100_BIN2,o.name COLLATE Latin1_General_100_BIN2
            ""","m.object_id",objectIds?.ToHashSet(StringComparer.Ordinal),scope,token);
        var roots=databaseScope?assemblies.Select(a=>a[0]!).ToHashSet(StringComparer.Ordinal):
            (await NativeTypeScope("SELECT assembly_id FROM sys.assembly_types WHERE 1=1 /*SCOPE*/","user_type_id",typeIds??[],scope,token)).Select(r=>r[0]!).Concat(modules.Select(m=>m[0]!)).ToHashSet(StringComparer.Ordinal);
        if(roots.Count==0)return section;
        var references=await NativeShared("""
            SELECT r.assembly_id,r.referenced_assembly_id,CONVERT(varbinary(max),CONVERT(nvarchar(max),a.name)) AS [__dbmv_utf16],CONVERT(varbinary(max),CONVERT(nvarchar(max),a.clr_name)) AS [__dbmv_utf16]
            FROM sys.assembly_references r LEFT JOIN sys.assemblies a ON a.assembly_id=r.referenced_assembly_id
            """,scope,token);
        var selected=ExpandAssemblyScope(roots,references,token);
        var known=assemblies.ToDictionary(a=>a[0]!,StringComparer.Ordinal);var gaps=collectedGaps??CollectCatalogGaps(token);
        if(collectedGaps==null)gaps.AddRange(section.Gaps);
        var facts=collected??CollectCatalogFacts(token);
        if(collected==null)facts.AddRange(section.Facts);
        foreach(var assembly in assemblies.Where(a=>selected.Contains(a[0]!)))
        {
            token.ThrowIfCancellationRequested();var id=assembly[0]!;var name=assembly[1]!;var key=AssemblyKey("assembly",name);
            facts.Add(new(key,null,"UserTypeDefinitions",SqlServerMetadata.Serialize(assembly[2..].Select(AssemblyText))));
            if(assembly[5]==null)gaps.Add(new(key,null,"UserTypeDefinitions","",Reason.VisibilityUnknown));
            foreach(var reference in references.Where(r=>r[0]==id))
            {
                if(reference[2]==null)gaps.Add(new(key,null,"UserTypeDefinitions","",Reason.MissingDependency));
                else facts.Add(new(AssemblyKey("assembly-reference",name,reference[2]),null,"UserTypeDefinitions",AssemblyText(reference[3])));
            }
            foreach(var module in modules.Where(m=>m[0]==id))
            {
                var binding=AssemblyKey("assembly-module",module[1],module[2],module[3]);
                facts.Add(new(binding,null,"UserTypeDefinitions",SqlServerMetadata.Serialize(new{name=AssemblyText(name),definition=module[4..].Select(AssemblyText)})));
                if(module[7]==null)gaps.Add(new(binding,null,"UserTypeDefinitions","",Reason.VisibilityUnknown));
            }
            await AppendAssemblyFiles(id,name,facts.Destination,gaps,token);
        }
        // Unknown IDs can be hidden user dependencies. System references with
        // visible names are compared by their CLR identity, not their local ID.
        foreach(var root in roots.Where(id=>!known.ContainsKey(id)))
            gaps.Add(new("assembly-binding",null,"UserTypeDefinitions","",Reason.VisibilityUnknown));
        var reason=section.Reason!=Reason.None?section.Reason:gaps.Count>0?Reason.VisibilityUnknown:Reason.None;
        return section with{Facts=facts.Snapshot(),Gaps=gaps.Snapshot(),Reason=reason,CompleteExceptGaps=section.CompleteExceptGaps||section.Reason==Reason.None&&gaps.Count>0};
    }

    private static string AssemblyKey(params string?[] parts)=>SqlServerMetadata.Serialize(parts.Select(AssemblyText));
    // Identifier/identity metadata can contain lone UTF-16 units. Encode units
    // before JSON so distinct invalid sequences cannot both become U+FFFD.
    private static string? AssemblyText(string? text)=>text==null?null:string.Create(checked(text.Length*4),text,static(output,input)=>
    {
        const string hex="0123456789ABCDEF";
        for(var i=0;i<input.Length;i++){var c=input[i];var n=i*4;output[n]=hex[c>>12];output[n+1]=hex[(c>>8)&15];output[n+2]=hex[(c>>4)&15];output[n+3]=hex[c&15];}
    });

    internal static IReadOnlySet<string> ExpandAssemblyScope(IReadOnlySet<string> roots,IEnumerable<string?[]> references,CancellationToken token)
    {
        token.ThrowIfCancellationRequested();var parents=new Dictionary<string,List<string>>(StringComparer.Ordinal);
        foreach(var row in references)
        {
            token.ThrowIfCancellationRequested();
            if(row.Length<2||string.IsNullOrEmpty(row[0])||string.IsNullOrEmpty(row[1]))throw new VerificationException(Reason.ObservationChanged);
            if(!parents.TryGetValue(row[0]!,out var list))parents.Add(row[0]!,list=[]);list.Add(row[1]!);
        }
        var selected=new HashSet<string>(roots,StringComparer.Ordinal);var queue=new Queue<string>(roots);
        while(queue.TryDequeue(out var id))
        {
            token.ThrowIfCancellationRequested();if(!parents.TryGetValue(id,out var list))continue;
            foreach(var parent in list){token.ThrowIfCancellationRequested();if(selected.Add(parent))queue.Enqueue(parent);}
        }
        return selected;
    }

    private async Task AppendAssemblyFiles(string nativeId,string assembly,ProtectedCatalogStore store,CatalogGaps gaps,CancellationToken token)
    {
        await using var q=Command("SELECT file_id,CONVERT(varbinary(max),CONVERT(nvarchar(max),name)) AS [__dbmv_utf16],DATALENGTH(content),content FROM sys.assembly_files WHERE assembly_id=@assembly ORDER BY file_id");
        q.Parameters.Add("@assembly",SqlDbType.Int).Value=int.Parse(nativeId,CultureInfo.InvariantCulture);
        await using var reader=await q.ExecuteReaderAsync(CommandBehavior.SequentialAccess,token);
        var buffer=new byte[65536];var main=false;
        try
        {
            while(await reader.ReadAsync(token))
            {
                token.ThrowIfCancellationRequested();var isMain=reader.GetInt32(0)==1;main|=isMain;var name=SqlServerMetadata.ReadText(reader,1,token)!;
                var key=AssemblyKey("assembly-file",assembly,name);var length=reader.IsDBNull(2)?(long?)null:reader.GetInt64(2);
                store.Add(new(key,null,"UserTypeDefinitions",SqlServerMetadata.Serialize(new{isMain,length})));
                if(!length.HasValue||reader.IsDBNull(3)){gaps.Add(new(key,null,"UserTypeDefinitions","",Reason.VisibilityUnknown));continue;}
                using var bytes=reader.GetStream(3);long offset=0;
                while(true)
                {
                    var count=0;
                    while(count<buffer.Length){var read=await bytes.ReadAsync(buffer.AsMemory(count),token);if(read==0)break;count+=read;}
                    if(count==0)break;
                    store.Add(new(AssemblyKey("assembly-file-bytes",assembly,name,offset.ToString(CultureInfo.InvariantCulture)),null,"UserTypeDefinitions",Convert.ToHexString(buffer.AsSpan(0,count))));offset=checked(offset+count);
                }
                if(offset!=length.Value)throw new VerificationException(Reason.ObservationChanged);
            }
            if(!main)gaps.Add(new(AssemblyKey("assembly-file",assembly),null,"UserTypeDefinitions","",Reason.VisibilityUnknown));
        }
        finally{CryptographicOperations.ZeroMemory(buffer);}
    }
}
