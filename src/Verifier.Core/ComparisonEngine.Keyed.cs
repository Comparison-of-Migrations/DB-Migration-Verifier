namespace Verifier.Core;

public sealed partial class ComparisonEngine
{
    private sealed record KeyedDifference(long SourceOnly,long TargetOnly,IReadOnlyDictionary<int,long> Columns,bool Ambiguous);
    private static int[]? CommonKey(Table left,Table right,IReadOnlyList<ColumnMapping> columns)
    {
        foreach(var candidate in left.RowKeys)
        {
            if(candidate.Count==0||candidate.Distinct(StringComparer.Ordinal).Count()!=candidate.Count)continue;
            var indexes=columns.Select((c,i)=>(c,i)).Where(p=>candidate.Contains(p.c.Source!,StringComparer.Ordinal)).Select(p=>p.i).ToArray();
            if(indexes.Length!=candidate.Count)continue;
            var targetNames=indexes.Select(i=>columns[i].Target!).ToHashSet(StringComparer.Ordinal);
            if(right.RowKeys.Any(k=>k.Count==targetNames.Count&&k.Distinct(StringComparer.Ordinal).Count()==k.Count&&targetNames.SetEquals(k)))return indexes;
        }
        return null;
    }
    private static async Task<KeyedDifference> CompareKeyed(IReadDatabase source,IReadDatabase target,Table left,Table right,IReadOnlyList<int> leftOrder,IReadOnlyList<int> rightOrder,int[] keyColumns,string name,IProgress<ProgressInfo>? progress,CancellationToken token)
    {
        using var store=new ProtectedRowStore(token:token);var comparer=new KeyComparer(store);
        using var a=new ProtectedSort(rowComparer:comparer);using var b=new ProtectedSort(rowComparer:comparer);
        var selected=keyColumns.ToHashSet();long rows=0;var nullKey=false;
        await Add(source,left,leftOrder,a);await Add(target,right,rightOrder,b);store.Flush();progress?.Report(new(name,rows));
        var orderedA=a.Finish(token);var orderedB=b.Finish(token);
        var ambiguous=ValidateKeys(orderedA)|ValidateKeys(orderedB)|nullKey;
        if(ambiguous)
        {
            // Retain all multiplicities. A bad declared key must never pair arbitrary duplicate rows.
            using var sa=new ProtectedSort(rowComparer:store);using var sb=new ProtectedSort(rowComparer:store);
            foreach(var item in orderedA)sa.Add(item[64..]);foreach(var item in orderedB)sb.Add(item[64..]);
            var difference=CompareSorted(sa.Finish(token),sb.Finish(token),store,token);
            return new(difference.Item1,difference.Item2,new Dictionary<int,long>(),true);
        }
        var changed=new Dictionary<int,long>();long onlyA=0,onlyB=0;
        using var ia=orderedA.GetEnumerator();using var ib=orderedB.GetEnumerator();var hasA=ia.MoveNext();var hasB=ib.MoveNext();
        while(hasA||hasB)
        {
            token.ThrowIfCancellationRequested();var comparison=!hasA?1:!hasB?-1:comparer.Compare(ia.Current,ib.Current);
            if(comparison==0&&store.Compare(ia.Current[64..],ib.Current[64..])!=0)
            {
                using var ar=store.OpenRead(ia.Current[64..]);using var br=store.OpenRead(ib.Current[64..]);
                foreach(var index in FramedRow.Differences(ar,br,leftOrder.Count,token))changed[index]=changed.GetValueOrDefault(index)+1;
            }
            if(comparison<=0){if(comparison<0)onlyA++;hasA=ia.MoveNext();}if(comparison>=0){if(comparison>0)onlyB++;hasB=ib.MoveNext();}
        }
        return new(onlyA,onlyB,changed,false);

        async Task Add(IReadDatabase database,Table table,IReadOnlyList<int> order,ProtectedSort sort)
        {
            if(database is IStreamingReadDatabase streaming)
            {await foreach(var write in streaming.StreamingRowsAsync(table,order,token))await Row(write);}
            else
            {await foreach(var row in database.RowsAsync(table,token)){var bytes=ExactRow.Project(row,table.Columns.Count,order);await Row((output,cancel)=>output.WriteAsync(bytes,cancel).AsTask());}}
            async Task Row(Func<Stream,CancellationToken,Task> write)
            {
                var row=await store.AddAsync(write,token);
                // Open before AddAsync starts its next record, so completeness validation sees a sealed file.
                using var input=store.OpenRead(row);
                var key=await store.AddAsync((output,cancel)=>{nullKey|=FramedRow.Project(input,output,order.Count,selected,cancel);return Task.CompletedTask;},token);
                var item=new byte[128];key.CopyTo(item,0);row.CopyTo(item,64);sort.Add(item);
                if(++rows%1000==0)progress?.Report(new(name,rows));
            }
        }
        bool ValidateKeys(IEnumerable<byte[]> records)
        {
            byte[]? previous=null;var duplicate=false;
            foreach(var record in records)
            {
                token.ThrowIfCancellationRequested();store.Validate(record[..64],token);store.Validate(record[64..],token);
                if(previous!=null&&comparer.Compare(previous,record)==0)duplicate=true;previous=record;
            }
            return duplicate;
        }
    }
    private sealed class KeyComparer(ProtectedRowStore store):IComparer<byte[]>
    {
        public int Compare(byte[]? a,byte[]? b)
        {if(a is not{Length:128}||b is not{Length:128})throw new VerificationException(Reason.TemporaryIntegrity);return store.Compare(a[..64],b[..64]);}
    }
}
