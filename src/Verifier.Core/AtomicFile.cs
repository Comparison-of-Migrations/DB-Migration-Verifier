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
                using var hash=IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
                await using(var hashing=new HashingStream(stream,hash))
                { await write(hashing); await hashing.FlushAsync(); expected=hash.GetHashAndReset(); }
                await stream.FlushAsync(); stream.Flush(true);
            }
            await using(var input=File.OpenRead(temp))
            { var actual=await SHA256.HashDataAsync(input); if(!CryptographicOperations.FixedTimeEquals(expected,actual)) throw new InvalidDataException(); }
            File.Move(temp,path,true);
        }
        finally { if(File.Exists(temp)) File.Delete(temp); }
    }
}
