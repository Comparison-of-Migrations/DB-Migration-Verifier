using System.Security.Cryptography;

namespace Verifier.Core;

internal static class AtomicFile
{
    // A failed write or read-back leaves an existing destination untouched.
    public static async Task WriteAsync(string path,Func<Stream,Task> write)
    {
        var temp=path+"."+Guid.NewGuid().ToString("N")+".tmp";
        try
        {
            var options=new FileStreamOptions { Mode=FileMode.CreateNew,Access=FileAccess.Write,Options=FileOptions.Asynchronous };
            if(!OperatingSystem.IsWindows()) options.UnixCreateMode=UnixFileMode.UserRead|UnixFileMode.UserWrite;
            byte[] expected;
            await using(var stream=new FileStream(temp,options))
            {
                using var hash=SHA256.Create();
                await using(var hashing=new CryptoStream(stream,hash,CryptoStreamMode.Write,leaveOpen:true))
                { await write(hashing); await hashing.FlushFinalBlockAsync(); expected=hash.Hash!; }
                await stream.FlushAsync(); stream.Flush(true);
            }
            await using(var input=File.OpenRead(temp))
            { var actual=await SHA256.HashDataAsync(input); if(!CryptographicOperations.FixedTimeEquals(expected,actual)) throw new InvalidDataException(); }
            File.Move(temp,path,true);
        }
        finally { if(File.Exists(temp)) File.Delete(temp); }
    }
}
