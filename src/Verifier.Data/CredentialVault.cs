using System.Text;

namespace Verifier.Data;

public enum VaultFailure { Unavailable,Denied,Locked,InvalidData }
public sealed class CredentialVaultException(VaultFailure failure,int? nativeCode=null):Exception("The operating system credential store could not complete the request.")
{public VaultFailure Failure{get;}=failure;public int? NativeCode{get;}=nativeCode;}

// Only an app-generated reference identifies a secret. DB endpoints/principal
// names are not vault keys, and no password fallback file exists.
public interface ICredentialVault
{
    void Write(Guid reference,string password);
    string? Read(Guid reference);
    void Delete(Guid reference);
}
public static class CredentialVault
{
    internal const string Service="org.db-migration-verifier.connection";
    internal static readonly Encoding Encoding=new UTF8Encoding(false,true);
    private static ReadOnlySpan<byte> Header=>"DMV-PASSWORD-1\0"u8;
    // A nonempty versioned OS-vault blob distinguishes an empty password from
    // a missing entry and rejects an unknown payload instead of guessing its encoding.
    internal static byte[] Encode(string password)
    {
        try{var bytes=new byte[checked(Header.Length+Encoding.GetByteCount(password))];Header.CopyTo(bytes);Encoding.GetBytes(password.AsSpan(),bytes.AsSpan(Header.Length));return bytes;}
        catch(EncoderFallbackException){throw new CredentialVaultException(VaultFailure.InvalidData);}
    }
    internal static string Decode(ReadOnlySpan<byte> bytes)
    {
        if(!bytes.StartsWith(Header))throw new CredentialVaultException(VaultFailure.InvalidData);
        try{return Encoding.GetString(bytes[Header.Length..]);}catch(DecoderFallbackException){throw new CredentialVaultException(VaultFailure.InvalidData);}
    }
    internal static string Key(Guid reference)=>reference!=Guid.Empty?Service+"/"+reference.ToString("N"):throw new ArgumentException("An owned credential reference is required.");
    public static ICredentialVault Create()=>new PlatformVault();
    private sealed class PlatformVault:ICredentialVault
    {
        private ICredentialVault? _native;
        private T Invoke<T>(Func<ICredentialVault,T> operation)
        {
            try{_native??=OperatingSystem.IsWindows()?new WindowsCredentialVault():OperatingSystem.IsMacOS()?new MacCredentialVault():throw new CredentialVaultException(VaultFailure.Unavailable);return operation(_native);}
            catch(Exception e) when(e is DllNotFoundException or EntryPointNotFoundException or TypeInitializationException){throw new CredentialVaultException(VaultFailure.Unavailable);}
        }
        public void Write(Guid id,string password)=>Invoke(v=>{v.Write(id,password);return true;});
        public string? Read(Guid id)=>Invoke(v=>v.Read(id));
        public void Delete(Guid id)=>Invoke(v=>{v.Delete(id);return true;});
    }
}
