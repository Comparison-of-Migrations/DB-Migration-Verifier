using System.Buffers.Binary;
using System.Data;
using System.Globalization;
using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.Data.SqlClient;
using Verifier.Core;

namespace Verifier.Data;

// Explicit catalog projections carry SQL nvarchar bytes, bypassing SqlClient's
// replacement decoder. The marker is authored in our fixed SELECTs, never user SQL.
internal static class SqlServerMetadata
{
    internal const string TextColumn="__dbmv_utf16";
    internal static bool HasUnpairedSurrogate(string value)=>ExactJsonStringConverter.HasUnpairedSurrogate(value);
    internal static string CommandText(string sql)=>HasUnpairedSurrogate(sql)?throw new VerificationException(Reason.ObservationChanged):sql;
    internal static void AddNames(SqlCommand command,Table table)
    {
        Add("@schema",table.Schema);Add("@table",table.Name);
        void Add(string name,string value)
        {
            if(value.Length>128)throw new VerificationException(Reason.ObservationChanged);
            var bytes=new byte[value.Length*2];
            for(var i=0;i<value.Length;i++)BinaryPrimitives.WriteUInt16LittleEndian(bytes.AsSpan(i*2),value[i]);
            command.Parameters.Add(name,SqlDbType.VarBinary,256).Value=bytes;
        }
    }
    internal static string? ReadText(SqlDataReader reader,int ordinal,CancellationToken token=default)
    {
        token.ThrowIfCancellationRequested();
        if(reader.IsDBNull(ordinal))return null;
        long offset=0;
        return ReadUtf16(reader.GetBytes(ordinal,0,null,0,0),(buffer,count)=>
        {
            var read=checked((int)reader.GetBytes(ordinal,offset,buffer,0,count));offset=checked(offset+read);return read;
        },token);
    }
    // SqlClient's sequential PLP length can be unknown. Spool that case once,
    // authenticated and encrypted, rather than accumulating a second full value.
    internal static string ReadUtf16(long length,Func<byte[],int,int> read,CancellationToken token)
    {
        token.ThrowIfCancellationRequested();
        var bytes=new byte[length<0?65536:(int)Math.Min(65536,Math.Max(1,length))];
        try
        {
            if(length<0)
            {
                using var activity=SqlServerDatabase.ObservationActivity.StartActivity("UnknownUtf16");
                using var store=new ProtectedRowStore(token:token);long total=0;
                var row=store.AddAsync((output,_)=>
                {
                    while(true)
                    {
                        token.ThrowIfCancellationRequested();var count=read(bytes,bytes.Length);
                        if(count<0||count>bytes.Length)throw new VerificationException(Reason.ObservationChanged);
                        if(count==0)break;
                        total=checked(total+count);output.Write(bytes,0,count);
                    }
                    return Task.CompletedTask;
                },token).GetAwaiter().GetResult();
                using var input=store.OpenRead(row);
                return ReadUtf16(total,(buffer,count)=>input.Read(buffer,0,count),token);
            }
            if(length%2!=0||length/2>int.MaxValue)throw new VerificationException(Reason.ObservationChanged);
            return string.Create((int)(length/2),(bytes,read,token,length),static(chars,state)=>
            {
                var (buffer,next,cancellation,remaining)=state;var offset=0;int? low=null;
                while(remaining>0)
                {
                    cancellation.ThrowIfCancellationRequested();var requested=(int)Math.Min(buffer.Length,remaining);
                    var count=next(buffer,requested);
                    if(count<=0||count>requested)throw new VerificationException(Reason.ObservationChanged);
                    var i=0;
                    if(low.HasValue){chars[offset++]=(char)(low.Value|(buffer[i++]<<8));low=null;}
                    while(i+1<count)
                    {
                        chars[offset++]=(char)BinaryPrimitives.ReadUInt16LittleEndian(buffer.AsSpan(i));i+=2;
                    }
                    if(i<count)low=buffer[i];remaining-=count;
                }
                cancellation.ThrowIfCancellationRequested();
                if(next(buffer,1)!=0)throw new VerificationException(Reason.ObservationChanged);
            });
        }
        finally{CryptographicOperations.ZeroMemory(bytes);}
    }
    internal static string? ReadField(SqlDataReader reader,int ordinal,CancellationToken token=default)
    {
        token.ThrowIfCancellationRequested();
        if(reader.GetName(ordinal)==TextColumn)return ReadText(reader,ordinal,token);
        if(reader.IsDBNull(ordinal))return null;
        var value=reader.GetValue(ordinal);
        return value is DateTime time?time.ToString("O",CultureInfo.InvariantCulture):Convert.ToString(value,CultureInfo.InvariantCulture);
    }

    private static readonly JsonSerializerOptions Options=new(){Converters={new ExactJsonStringConverter()}};
    internal static string Serialize<T>(T value)=>JsonSerializer.Serialize(value,Options);
    internal static T? Deserialize<T>(string json)=>JsonSerializer.Deserialize<T>(json,Options);
    internal static Task SerializeRowAsync(Stream output,string?[] row,CancellationToken token)=>JsonSerializer.SerializeAsync(output,row,Options,token);
    internal static string?[]? DeserializeRow(Stream input)=>JsonSerializer.Deserialize<string?[]>(input,Options);

}
