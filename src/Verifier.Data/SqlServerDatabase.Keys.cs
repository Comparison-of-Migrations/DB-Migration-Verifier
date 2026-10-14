using System.Collections;
using Verifier.Core;

namespace Verifier.Data;

public sealed partial class SqlServerDatabase
{
    private IReadOnlyList<IReadOnlyList<string>> FreezeKeys(IReadOnlyList<IReadOnlyList<string>> keys,CancellationToken token)
    {
        if(keys.Count==0)return keys;
        catalogStore??=new(token);var start=catalogStore.Count;
        foreach(var key in keys)
        {
            token.ThrowIfCancellationRequested();
            catalogStore.Add(new("row-key",null,"columns",SqlServerMetadata.Serialize(key)));
        }
        return new KeyView(catalogStore.Snapshot(start,keys.Count));
    }
    private sealed class KeyView(IReadOnlyList<CatalogFact> records):IFrozenMetadata<IReadOnlyList<string>>
    {
        internal IReadOnlyList<CatalogFact> Records=>records;
        public int Count=>records.Count;
        public IReadOnlyList<string> this[int index]
        {
            get
            {
                var record=records[index];
                if(record.Key!="row-key"||record.Column!=null||record.Attribute!="columns"||record.Value==null)throw new VerificationException(Reason.TemporaryIntegrity);
                try
                {
                    var key=SqlServerMetadata.Deserialize<string[]>(record.Value);
                    if(key==null||key.Length==0||key.Any(string.IsNullOrEmpty)||key.Distinct(StringComparer.Ordinal).Count()!=key.Length)throw new VerificationException(Reason.TemporaryIntegrity);
                    return key;
                }
                catch(System.Text.Json.JsonException){throw new VerificationException(Reason.TemporaryIntegrity);}
            }
        }
        public IEnumerator<IReadOnlyList<string>> GetEnumerator(){for(var i=0;i<Count;i++)yield return this[i];}
        IEnumerator IEnumerable.GetEnumerator()=>GetEnumerator();
    }
}
