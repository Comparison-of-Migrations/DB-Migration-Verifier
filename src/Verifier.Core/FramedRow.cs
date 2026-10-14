using System.Buffers.Binary;
using System.Security.Cryptography;
namespace Verifier.Core;

// Reads one cell at a time from the existing v1/v3 row encoding. No cell-size allocation.
internal sealed class FramedRow(Stream input,int count,CancellationToken token)
{
    private readonly int version=ReadHeader(input,count);
    private int position;
    private Cell? active;
    private static VerificationException Invalid()=>new(Reason.TemporaryIntegrity);
    private static int Int(Stream stream)
    {Span<byte> bytes=stackalloc byte[4];try{stream.ReadExactly(bytes);}catch(EndOfStreamException){throw Invalid();}return BinaryPrimitives.ReadInt32LittleEndian(bytes);}
    private static int ReadHeader(Stream input,int count)
    {var version=Int(input);if(version is not(1 or 3)||Int(input)!=count)throw Invalid();return version;}
    internal (bool Present,Stream Value) Next()
    {
        token.ThrowIfCancellationRequested();
        if(position++>=count||active is {Completed:false})throw Invalid();
        var present=input.ReadByte();if(present is not(0 or 1))throw Invalid();
        active=new Cell(input,version,present==1,token);return(present==1,active);
    }
    internal void Complete()
    {if(position!=count||active is {Completed:false}||input.ReadByte()!=-1)throw Invalid();}
    internal static bool Project(Stream input,Stream output,int count,IReadOnlySet<int> selected,CancellationToken token)
    {
        var row=new FramedRow(input,count,token);using var header=new BinaryWriter(output,System.Text.Encoding.UTF8,true);
        header.Write(row.version);header.Write(selected.Count);var buffer=new byte[65536];var nullKey=false;
        try
        {
            for(var i=0;i<count;i++)
            {
                var (present,cell)=row.Next();using(cell)
                {
                    var keep=selected.Contains(i);if(keep){header.Write(present);nullKey|=!present;}
                    if(!present)continue;
                    if(row.version==1&&keep)header.Write(((Cell)cell).Remaining);
                    int n;while((n=cell.ReadAtLeast(buffer,buffer.Length,false))>0)
                    {token.ThrowIfCancellationRequested();if(keep){if(row.version==3)header.Write(n);output.Write(buffer,0,n);}}
                    if(keep&&row.version==3)header.Write(0);
                }
            }
            row.Complete();return nullKey;
        }
        finally{CryptographicOperations.ZeroMemory(buffer);}
    }
    internal static IReadOnlyList<int> Differences(Stream left,Stream right,int count,CancellationToken token)
    {
        var a=new FramedRow(left,count,token);var b=new FramedRow(right,count,token);
        if(a.version!=b.version)throw Invalid();var differences=new List<int>();var aa=new byte[65536];var bb=new byte[65536];
        try
        {
            for(var i=0;i<count;i++)
            {
                var (presentA,cellA)=a.Next();var (presentB,cellB)=b.Next();using(cellA)using(cellB)
                {
                    var different=presentA!=presentB;
                    while(true)
                    {
                        token.ThrowIfCancellationRequested();var n=cellA.ReadAtLeast(aa,aa.Length,false);var m=cellB.ReadAtLeast(bb,bb.Length,false);
                        different|=n!=m||!aa.AsSpan(0,Math.Min(n,m)).SequenceEqual(bb.AsSpan(0,Math.Min(n,m)));
                        if(n==0&&m==0)break;
                    }
                    if(different)differences.Add(i);
                }
            }
            a.Complete();b.Complete();return differences;
        }
        finally{CryptographicOperations.ZeroMemory(aa);CryptographicOperations.ZeroMemory(bb);}
    }
    private sealed class Cell:Stream
    {
        private readonly Stream input;private readonly int version;private readonly CancellationToken token;private bool shortBlock;
        internal int Remaining{get;private set;}
        internal bool Completed{get;private set;}
        internal Cell(Stream input,int version,bool present,CancellationToken token)
        {
            this.input=input;this.version=version;this.token=token;Completed=!present;
            if(present&&version==1){Remaining=Int(input);if(Remaining<0||Remaining>ExactRow.MaxRowBytes)throw Invalid();Completed=Remaining==0;}
        }
        public override int Read(Span<byte> output)
        {
            token.ThrowIfCancellationRequested();if(output.IsEmpty||Completed)return 0;
            if(Remaining==0)
            {
                if(version==1){Completed=true;return 0;}
                Remaining=Int(input);if(Remaining<0||Remaining>65536||shortBlock&&Remaining>0)throw Invalid();
                if(Remaining==0){Completed=true;return 0;}shortBlock=Remaining<65536;
            }
            var n=input.Read(output[..Math.Min(Remaining,output.Length)]);if(n==0)throw Invalid();Remaining-=n;
            if(Remaining==0&&version==1)Completed=true;return n;
        }
        public override int Read(byte[] b,int o,int c)=>Read(b.AsSpan(o,c));
        public override bool CanRead=>true;public override bool CanWrite=>false;public override bool CanSeek=>false;
        public override long Length=>throw new NotSupportedException();public override long Position{get=>throw new NotSupportedException();set=>throw new NotSupportedException();}
        public override void Flush(){}public override void Write(byte[] b,int o,int c)=>throw new NotSupportedException();public override long Seek(long o,SeekOrigin s)=>throw new NotSupportedException();public override void SetLength(long l)=>throw new NotSupportedException();
    }
}
