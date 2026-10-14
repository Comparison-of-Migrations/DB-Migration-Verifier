using System.Buffers.Binary;
namespace Verifier.Core;

public static partial class CatalogComparison
{
    private static IReadOnlyList<ScopeIssue> DeduplicateIssues(IReadOnlyList<ScopeIssue> issues,RunReportStorage? storage,CancellationToken token)
    {
        if(storage==null||issues.Count<=4096)return issues.Distinct().ToArray();
        // Every issue in this comparison has the same table. Length-prefix the
        // nullable column so no identifier can collide with the reason separator.
        using var index=new CatalogFactIndex(issues.Select(i=>new CatalogFact(
            (i.Column?.Length??-1).ToString(System.Globalization.CultureInfo.InvariantCulture)+":"+i.Column+":"+((int)i.Reason).ToString(System.Globalization.CultureInfo.InvariantCulture),
            i.Column,i.Attribute??"",((int)i.Reason).ToString(System.Globalization.CultureInfo.InvariantCulture))),true,token);
        var result=new IssueBuffer(storage);var table=issues[0].Table;
        foreach(var entry in index.Entries(token))
            result.Add(new(table,entry.Fact.Column,(Reason)int.Parse(entry.Fact.Value!,System.Globalization.CultureInfo.InvariantCulture))
            {Attribute=entry.Fact.Attribute.Length==0?null:entry.Fact.Attribute});
        return result;
    }

    // Only normalized comparison metadata enters this per-call sort. Its lease,
    // encryption and completion validation are the same as exact row sorting.
    internal sealed class CatalogFactIndex:IDisposable
    {
        internal sealed record Entry(CatalogFact Fact,bool Duplicate);
        private const long MemoryBudget=2*1024*1024;
        private Dictionary<(string Key,string Attribute),Entry>? memory;
        private long retainedBytes;
        private ProtectedSort? sort;
        private ProtectedRowStore? largeFacts;
        private bool disposed;
        internal CatalogFactIndex(IEnumerable<CatalogFact> facts,bool external,CancellationToken token)
        {
            if(external)sort=new(rowComparer:EncodedComparer.Instance);else memory=[];
            try
            {
                foreach(var fact in facts)
                {
                    token.ThrowIfCancellationRequested();
                    if(sort==null)
                    {
                        var key=(fact.Key,fact.Attribute);var size=EncodedLength(fact);
                        var duplicate=memory!.TryGetValue(key,out var earlier);
                        var nextBytes=retainedBytes-(earlier==null?0:EncodedLength(earlier.Fact))+size;
                        if(nextBytes>MemoryBudget||!duplicate&&memory.Count>=4096)
                        {
                            sort=new(rowComparer:EncodedComparer.Instance);
                            foreach(var entry in memory.Values)
                            {
                                token.ThrowIfCancellationRequested();sort.Add(Encode(entry.Fact,token));
                                // Preserve ambiguity already discovered before
                                // promotion; the external merge must see it too.
                                if(entry.Duplicate)sort.Add(Encode(entry.Fact,token));
                            }
                            memory=null;retainedBytes=0;
                        }
                        else{memory[key]=new(fact,duplicate);retainedBytes=nextBytes;continue;}
                    }
                    sort.Add(Encode(fact,token));
                }
            }
            catch{Dispose();throw;}
        }
        internal IEnumerable<Entry> Entries(CancellationToken token)
        {
            if(memory!=null)
            {
                foreach(var entry in memory.Values.OrderBy(x=>x.Fact.Key,StringComparer.Ordinal).ThenBy(x=>x.Fact.Attribute,StringComparer.Ordinal))
                {token.ThrowIfCancellationRequested();yield return entry;}
                yield break;
            }
            Entry? pending=null;
            largeFacts?.Flush();
            foreach(var encoded in sort!.Finish(token))
            {
                var fact=Decode(encoded,token);
                if(pending==null)pending=new(fact,false);
                else if(CompareKey(pending.Fact,fact)==0)pending=pending with{Duplicate=true};
                else{yield return pending;pending=new(fact,false);}
            }
            if(pending!=null)yield return pending;
        }
        internal static int CompareKey(CatalogFact x,CatalogFact y)
        {var key=StringComparer.Ordinal.Compare(x.Key,y.Key);return key!=0?key:StringComparer.Ordinal.Compare(x.Attribute,y.Attribute);}
        public void Dispose(){if(disposed)return;disposed=true;try{sort?.Dispose();}finally{largeFacts?.Dispose();}}

        // Preserve .NET ordinal UTF-16 code units, including unusual identifiers;
        // a UTF-8 replacement fallback must not make distinct definitions equal.
        private byte[] Encode(CatalogFact fact,CancellationToken token)
        {
            var large=fact.Text!=null||EncodedLength(fact)+1>ExactRow.MaxRowBytes;
            var fields=EncodeFields(large?new(fact.Key,null,fact.Attribute,null):fact);
            if(1L+fields.Length+(large?64:0)>ExactRow.MaxRowBytes)throw new VerificationException(Reason.RowTooLarge);
            byte[]? descriptor=null;
            if(large)
            {
                largeFacts??=new(token:token);
                descriptor=largeFacts.AddAsync((stream,ct)=>
                {CatalogFactWire.Write(fact,stream,ct);return Task.CompletedTask;},token).GetAwaiter().GetResult();
            }
            var encoded=new byte[1+fields.Length+(descriptor?.Length??0)];encoded[0]=large?(byte)1:(byte)0;
            fields.CopyTo(encoded,1);descriptor?.CopyTo(encoded,1+fields.Length);return encoded;
        }
        private static long EncodedLength(CatalogFact fact)=>CatalogFactWire.Length(fact);
        private static byte[] EncodeFields(CatalogFact fact)
        {
            var length=EncodedLength(fact);
            if(length+1>ExactRow.MaxRowBytes)throw new VerificationException(Reason.RowTooLarge);
            var bytes=new byte[(int)length];var offset=0;
            Write(fact.Key);Write(fact.Attribute);Write(fact.Column);Write(fact.Value);return bytes;
            void Write(string? value)
            {
                BinaryPrimitives.WriteInt32LittleEndian(bytes.AsSpan(offset),value?.Length??-1);offset+=4;
                if(value==null)return;
                foreach(var ch in value){BinaryPrimitives.WriteUInt16LittleEndian(bytes.AsSpan(offset),ch);offset+=2;}
            }
        }
        private static ReadOnlySpan<byte> Field(ReadOnlySpan<byte> bytes,ref int offset,out bool absent)
        {
            if(offset>bytes.Length-4)throw new VerificationException(Reason.TemporaryIntegrity);
            var count=BinaryPrimitives.ReadInt32LittleEndian(bytes[offset..]);offset+=4;absent=count==-1;
            if(absent)return [];
            if(count<0||count>(bytes.Length-offset)/2)throw new VerificationException(Reason.TemporaryIntegrity);
            var field=bytes.Slice(offset,count*2);offset+=count*2;return field;
        }
        private CatalogFact Decode(byte[] bytes,CancellationToken token)
        {
            if(bytes.Length<17||bytes[0]>1)throw new VerificationException(Reason.TemporaryIntegrity);
            if(bytes[0]==0)return DecodeFields(bytes.AsMemory(1));
            if(bytes.Length<81||largeFacts==null)throw new VerificationException(Reason.TemporaryIntegrity);
            var ordered=DecodeFields(bytes.AsMemory(1,bytes.Length-65));
            var descriptor=bytes[^64..];
            Stream Open(){ObjectDisposedException.ThrowIf(disposed,this);return largeFacts.OpenRead(descriptor);}
            using var input=Open();
            var fact=CatalogFactWire.Read(input,token,Open);
            if(ordered.Column!=null||ordered.Value!=null||
                !StringComparer.Ordinal.Equals(fact.Key,ordered.Key)||!StringComparer.Ordinal.Equals(fact.Attribute,ordered.Attribute))
                throw new VerificationException(Reason.TemporaryIntegrity);
            return fact;
        }
        private static CatalogFact DecodeFields(ReadOnlyMemory<byte> bytes)
        {
            var offset=0;var key=Read();var attribute=Read();var column=Read();var value=Read();
            if(key==null||attribute==null||offset!=bytes.Length)throw new VerificationException(Reason.TemporaryIntegrity);
            return new(key,column,attribute,value);
            string? Read()
            {
                var field=Field(bytes.Span,ref offset,out var absent);if(absent)return null;
                return string.Create(field.Length/2,bytes.Slice(offset-field.Length,field.Length),static(chars,input)=>
                {
                    for(var i=0;i<chars.Length;i++)chars[i]=(char)BinaryPrimitives.ReadUInt16LittleEndian(input.Span[(i*2)..]);
                });
            }
        }
        private sealed class EncodedComparer:IComparer<byte[]>
        {
            internal static readonly EncodedComparer Instance=new();
            public int Compare(byte[]? x,byte[]? y)
            {
                if(x==null||y==null)throw new VerificationException(Reason.TemporaryIntegrity);
                if(x.Length==0||y.Length==0||x[0]>1||y[0]>1)throw new VerificationException(Reason.TemporaryIntegrity);
                var a=1;var b=1;
                for(var part=0;part<2;part++)
                {
                    var left=Field(x,ref a,out var absentA);var right=Field(y,ref b,out var absentB);
                    if(absentA||absentB)throw new VerificationException(Reason.TemporaryIntegrity);
                    for(var i=0;i<Math.Min(left.Length,right.Length);i+=2)
                    {
                        var order=BinaryPrimitives.ReadUInt16LittleEndian(left[i..]).CompareTo(BinaryPrimitives.ReadUInt16LittleEndian(right[i..]));
                        if(order!=0)return order;
                    }
                    var length=left.Length.CompareTo(right.Length);if(length!=0)return length;
                }
                return 0;
            }
        }
    }
}
