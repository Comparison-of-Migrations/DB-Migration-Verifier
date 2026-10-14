using System.Globalization;
using Verifier.Core;
namespace Verifier.Data;

public sealed partial class SqlServerDatabase
{
    private async Task<IReadOnlyList<string?[]>> ReadModuleRows(string query,HashSet<string> selected,bool databaseScope,Table scope,ProtectedCatalogStore bodies,CancellationToken token)
    {
        var rows=new SqlServerNativeRowBuffer(nativeRows,token);
        using var text=new ProtectedRowStore(token:token);
        IEnumerable<string> Queries()
        {
            if(databaseScope){yield return query.Replace("/*SCOPE*/","",StringComparison.Ordinal);yield break;}
            foreach(var batch in selected.Chunk(256))
            {
                token.ThrowIfCancellationRequested();
                var values=string.Join(',',batch.Select(id=>int.Parse(id,CultureInfo.InvariantCulture).ToString(CultureInfo.InvariantCulture)));
                yield return query.Replace("/*SCOPE*/"," AND o.object_id IN("+values+")",StringComparison.Ordinal);
            }
        }
        foreach(var sql in Queries())
        {
            await using var command=Command(sql);SqlServerMetadata.AddNames(command,scope);
            await using var reader=await command.ExecuteReaderAsync(System.Data.CommandBehavior.SequentialAccess,token);
            while(await reader.ReadAsync(token))
            {
                var row=new string?[reader.FieldCount];row[0]=SqlServerMetadata.ReadField(reader,0,token);
                if(!selected.Contains(row[0]!))continue;
                CatalogText? definition=null;
                for(var i=1;i<row.Length;i++)
                    if(i==4){definition=SqlServerMetadataText.ReadJson(reader,i,text,token);row[i]=definition==null?null:"present";}
                    else row[i]=SqlServerMetadata.ReadField(reader,i,token);
                var key=SqlServerCatalog.Key("module",row[1],row[2],row[3]);
                var attribute=row[3]?.TrimEnd() switch{"V"=>"RelatedViews","TR" or "TA"=>"TriggerDefinitions",_=>"RelatedRoutines"};
                var fact=new CatalogFact(key,null,attribute,null);
                bodies.Add(definition==null?fact:fact.WithText(SqlServerMetadataText.JsonArray(definition,row[5..],text,token)));
                rows.Add(row);
            }
        }
        bodies.Seal();return rows.Finish();
    }
}
