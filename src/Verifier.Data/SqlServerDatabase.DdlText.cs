using System.Globalization;
using Verifier.Core;
namespace Verifier.Data;

public sealed partial class SqlServerDatabase
{
    private async Task AppendDdlTriggers(string query,HashSet<string> selected,bool databaseScope,Table scope,CatalogFacts facts,CatalogGaps gaps,CancellationToken token)
    {
        IEnumerable<string> Queries()
        {
            if(databaseScope){yield return query.Replace("/*SCOPE*/","",StringComparison.Ordinal);yield break;}
            foreach(var batch in selected.Chunk(256))
            {
                token.ThrowIfCancellationRequested();
                var ids=string.Join(',',batch.Select(id=>int.Parse(id,CultureInfo.InvariantCulture).ToString(CultureInfo.InvariantCulture)));
                yield return query.Replace("/*SCOPE*/"," AND tr.object_id IN("+ids+")",StringComparison.Ordinal);
            }
        }
        ProtectedCatalogStore? rows=null;ProtectedRowStore? text=null;string? name=null;var blocked=false;
        try
        {
            foreach(var sql in Queries())
            {
                await using var command=Command(sql);SqlServerMetadata.AddNames(command,scope);
                await using var reader=await command.ExecuteReaderAsync(System.Data.CommandBehavior.SequentialAccess,token);
                while(await reader.ReadAsync(token))
                {
                    var row=new string?[reader.FieldCount];row[0]=SqlServerMetadata.ReadField(reader,0,token);
                    if(!selected.Contains(row[0]!))continue;
                    row[1]=SqlServerMetadata.ReadField(reader,1,token);
                    if(row[1]==null)throw new VerificationException(Reason.ObservationChanged);
                    if(name!=row[1])
                    {Finish();name=row[1];blocked=false;rows=new(token);text=new(token:token);}
                    CatalogText? body=null;
                    for(var i=2;i<row.Length;i++)
                        if(i==3){body=SqlServerMetadataText.ReadJson(reader,i,text!,token);row[i]=body==null?null:"present";}
                        else row[i]=SqlServerMetadata.ReadField(reader,i,token);
                    blocked|=body==null||row[11]==null||row[12]==null;
                    if(body!=null)
                        rows!.Add(new CatalogFact(rows.Count.ToString(CultureInfo.InvariantCulture),null,"ddl-row",null)
                            .WithText(SqlServerMetadataText.JsonArray(body,[row[2]],row[4..],text!,token)));
                }
                Finish();
            }
        }
        finally{try{rows?.Dispose();}finally{text?.Dispose();}}
        void Finish()
        {
            if(name==null)return;
            try
            {
                token.ThrowIfCancellationRequested();var key=SqlServerCatalog.Key("database-ddl-trigger",name);
                if(blocked)gaps.Add(new(key,null,"TriggerDefinitions","",Reason.VisibilityUnknown));
                else facts.Add(new CatalogFact(key,null,"TriggerDefinitions",null).WithText(SqlServerMetadataText.JsonRows(rows!,text!,token)));
                gaps.Add(new(key,null,"ObjectDependencies","",Reason.UnsafeObservation));
            }
            finally{try{rows?.Dispose();}finally{text?.Dispose();rows=null;text=null;name=null;}}
        }
    }
}
