using System.Globalization;
using System.Text.Json;
using MySqlConnector;
using Verifier.Core;
namespace Verifier.Data;

public sealed partial class MySqlDatabase
{
    private static bool Enumerated(Column c)=>c.Type.Split([' ','('],2)[0] is "enum" or "set";
    private static bool Spatial(Column c)=>c.Type.Split([' ','('],2)[0] is "geometry" or "point" or "linestring" or "polygon" or "multipoint" or "multilinestring" or "multipolygon" or "geomcollection" or "geometrycollection";
    private async Task<CatalogSection> ReadColumnTypes(Table table,IReadOnlyList<Column> columns,IReadOnlyList<CatalogFact> definitions,CancellationToken token,bool inObservation=false)
    {
        if(!inObservation&&columns.Any(c=>Spatial(c)&&!definitions.Any(f=>f.Column==c.Name&&f.Attribute=="ColumnOptions"&&f.Value!=null)))
        {
            // Preflight must not retain MDL locks on all geometry tables until
            // the run ends. Observe row dependencies in a short read-only snapshot;
            // selected OBJECT-03 is freshly checked before/after the real run.
            var observer=await MetadataObserver(token);
            observer.transaction=await observer.connection!.BeginTransactionAsync(System.Data.IsolationLevel.RepeatableRead,true,token);
            try{return await observer.ReadColumnTypes(table,columns,definitions,token,true);}
            finally{await observer.transaction.DisposeAsync();observer.transaction=null;}
        }
        // ENUM/SET are native column declarations, not independent CREATE TYPE
        // objects. Keep members/order, charset and collation without evaluating.
        var facts=columns.Where(Enumerated).Select(c=>new CatalogFact(c.Name,c.Name,"UserTypeDefinitions",
            JsonSerializer.Serialize(definitions.Where(f=>f.Column==c.Name&&f.Attribute is "ColumnType" or "ColumnCollation").Select(f=>new{f.Attribute,f.Value})))).ToList();
        var gaps=new List<CatalogGap>();var srs=new Dictionary<uint,string>();
        // A declared SRS binds even an empty geometry column. Equal SRID/WKB
        // bytes do not establish equal dictionary definitions across servers.
        // An unconstrained column binds every SRS actually referenced by its rows.
        foreach(var column in columns)
        {
            var id=definitions.SingleOrDefault(f=>f.Column==column.Name&&f.Attribute=="ColumnOptions")?.Value;
            if(id==null)
            {
                if(!Spatial(column))continue;
                var rowReason=Reason.None;var rows=new List<string?[]>();
                try
                {
                    if(!table.SafeRead)throw new VerificationException(table.ReadReason);
                    // DISTINCT only the native unsigned identifier, never coordinates.
                    // This short read-only snapshot releases preflight MDL promptly.
                    // Fresh catalog guards repeat against their own snapshot.
                    var relation=Quote(table.Schema)+"."+Quote(table.Name);var c=Quote(column.Name);
                    await using var q=Command("SELECT d.id,s.SRS_NAME,s.ORGANIZATION,s.ORGANIZATION_COORDSYS_ID,s.DEFINITION,s.DESCRIPTION FROM (SELECT DISTINCT ST_SRID("+c+") AS id FROM "+relation+" WHERE "+c+" IS NOT NULL) d LEFT JOIN information_schema.ST_SPATIAL_REFERENCE_SYSTEMS s ON s.SRS_ID=d.id ORDER BY d.id");
                    await using(var r=await q.ExecuteReaderAsync(token))
                    while(await r.ReadAsync(token))
                    {
                        var row=Enumerable.Range(0,6).Select(i=>r.IsDBNull(i)?null:Convert.ToString(r.GetValue(i),CultureInfo.InvariantCulture)).ToArray();
                        if(row[1]==null||row[4]==null)rowReason=Reason.MissingDependency;else rows.Add(row);
                    }
                    if(rowReason==Reason.MissingDependency)await CheckMissingSrsWarnings(token);else await CheckWarnings(token);
                    if(rowReason==Reason.None)facts.Add(new(column.Name,column.Name,"SpatialReferenceDefinition",JsonSerializer.Serialize(rows)));
                }
                catch(MySqlException e){rowReason=Map(e).Reason;}
                catch(VerificationException e){rowReason=e.Reason;}
                if(rowReason!=Reason.None)gaps.Add(new(column.Name,column.Name,"SpatialReferenceDefinition","",rowReason));
                continue;
            }
            var reason=Reason.None;
            if(!uint.TryParse(id,NumberStyles.None,CultureInfo.InvariantCulture,out var number))reason=Reason.ObservationChanged;
            else try
            {
                if(!srs.TryGetValue(number,out var definition))
                {
                    await using var q=Command("SELECT SRS_ID,SRS_NAME,ORGANIZATION,ORGANIZATION_COORDSYS_ID,DEFINITION,DESCRIPTION FROM information_schema.ST_SPATIAL_REFERENCE_SYSTEMS WHERE SRS_ID=@id");q.Parameters.AddWithValue("id",number);
                    await using(var r=await q.ExecuteReaderAsync(token))
                    {
                        if(!await r.ReadAsync(token))reason=Reason.MissingDependency;
                        else
                        {
                            var row=Enumerable.Range(0,6).Select(i=>r.IsDBNull(i)?null:Convert.ToString(r.GetValue(i),CultureInfo.InvariantCulture)).ToArray();
                            if(row[0]!=id||row[1]==null||row[4]==null||await r.ReadAsync(token))reason=Reason.ObservationChanged;
                            else definition=JsonSerializer.Serialize(row);
                        }
                    }
                    await CheckWarnings(token);
                    if(reason==Reason.None)srs.Add(number,definition!);
                }
                if(reason==Reason.None)facts.Add(new(column.Name,column.Name,"SpatialReferenceDefinition",definition));
            }
            catch(MySqlException e){reason=Map(e).Reason;}
            catch(VerificationException e){reason=e.Reason;}
            if(reason!=Reason.None)gaps.Add(new(column.Name,column.Name,"SpatialReferenceDefinition","",reason));
        }
        return new("OBJECT-03",facts,gaps.Count==0?Reason.None:gaps[0].Reason){Gaps=gaps,CompleteExceptGaps=gaps.Count>0};
    }
    private async Task CheckMissingSrsWarnings(CancellationToken token)
    {
        // Native 3519 accompanies an undefined row SRID even though one-argument
        // ST_SRID returns its ID. Accept only this exact warning when the query
        // independently proved a missing dictionary reference; no message text.
        await using var count=Command("SHOW COUNT(*) WARNINGS");var expected=Convert.ToInt64(await count.ExecuteScalarAsync(token));
        if(expected==0)return;
        await using var q=Command("SHOW WARNINGS");await using var r=await q.ExecuteReaderAsync(token);long seen=0;
        while(await r.ReadAsync(token))
        {if(r.GetString(0)!="Warning"||Convert.ToInt32(r.GetValue(1))!=3519)throw new VerificationException(Reason.ObservationChanged);seen++;}
        if(seen!=expected)throw new VerificationException(Reason.ObservationChanged);
    }

}
