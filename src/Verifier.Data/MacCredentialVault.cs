using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Security.Cryptography;
using System.Text;

namespace Verifier.Data;

[SupportedOSPlatform("macos")]
internal sealed class MacCredentialVault:ICredentialVault
{
    private const string Security="/System/Library/Frameworks/Security.framework/Security",Foundation="/System/Library/Frameworks/CoreFoundation.framework/CoreFoundation";
    private static readonly IntPtr SecurityHandle=NativeLibrary.Load(Security),FoundationHandle=NativeLibrary.Load(Foundation);
    private static IntPtr Constant(string name)=>Marshal.ReadIntPtr(NativeLibrary.GetExport(SecurityHandle,name));
    public void Write(Guid reference,string password)
    {
        ArgumentNullException.ThrowIfNull(password);using var query=Query(reference);
        var bytes=CredentialVault.Encode(password);
        try
        {
            using var data=new Owned(CFDataCreate(IntPtr.Zero,bytes,bytes.Length));using var update=new Dictionary();update.Set("kSecValueData",data.Value);
            var status=SecItemUpdate(query.Value,update.Value);
            if(status==-25300){query.Set("kSecValueData",data.Value);status=SecItemAdd(query.Value,IntPtr.Zero);}
            Check(status);
        }
        finally{CryptographicOperations.ZeroMemory(bytes);}
    }
    public string? Read(Guid reference)
    {
        using var query=Query(reference);query.Set("kSecReturnData",Marshal.ReadIntPtr(NativeLibrary.GetExport(FoundationHandle,"kCFBooleanTrue")));query.Set("kSecMatchLimit",Constant("kSecMatchLimitOne"));
        var status=SecItemCopyMatching(query.Value,out var result);
        using var data=new Owned(result,false);
        if(status==-25300)return null;Check(status);
        if(result==IntPtr.Zero||CFGetTypeID(result)!=CFDataGetTypeID())throw new CredentialVaultException(VaultFailure.InvalidData);
        var length=CFDataGetLength(result);if(length<0||length>int.MaxValue)throw new CredentialVaultException(VaultFailure.InvalidData);
        var bytes=new byte[(int)length];
        try{if(bytes.Length>0)Marshal.Copy(CFDataGetBytePtr(result),bytes,0,bytes.Length);return CredentialVault.Decode(bytes);}
        finally{CryptographicOperations.ZeroMemory(bytes);}
    }
    public void Delete(Guid reference)
    {using var query=Query(reference);var status=SecItemDelete(query.Value);if(status!=-25300)Check(status);}
    private static Dictionary Query(Guid reference)
    {
        _=CredentialVault.Key(reference);var query=new Dictionary();
        try
        {
            query.Set("kSecClass",Constant("kSecClassGenericPassword"));
            using var service=String(CredentialVault.Service);using var account=String(reference.ToString("N"));
            query.Set("kSecAttrService",service.Value);query.Set("kSecAttrAccount",account.Value);
            // Return a typed failure instead of opening an unexpected authentication dialog.
            query.Set("kSecUseAuthenticationUI",Constant("kSecUseAuthenticationUIFail"));return query;
        }
        catch{query.Dispose();throw;}
    }
    private static Owned String(string text)=>new(CFStringCreateWithCharacters(IntPtr.Zero,text,text.Length));
    private static void Check(int status)
    {if(status!=0)throw new CredentialVaultException(status switch{-25308=>VaultFailure.Locked,-25293 or -34018=>VaultFailure.Denied,_=>VaultFailure.Unavailable},status);}
    private sealed class Owned(IntPtr value,bool required=true):IDisposable
    {
        internal IntPtr Value{get;}=value!=IntPtr.Zero||!required?value:throw new CredentialVaultException(VaultFailure.Unavailable);
        public void Dispose(){if(Value!=IntPtr.Zero)CFRelease(Value);}
    }
    private sealed class Dictionary:IDisposable
    {
        private readonly Owned owner=new(CFDictionaryCreateMutable(IntPtr.Zero,0,NativeLibrary.GetExport(FoundationHandle,"kCFTypeDictionaryKeyCallBacks"),NativeLibrary.GetExport(FoundationHandle,"kCFTypeDictionaryValueCallBacks")));
        internal IntPtr Value=>owner.Value;internal void Set(string key,IntPtr value)=>CFDictionarySetValue(Value,Constant(key),value);
        public void Dispose()=>owner.Dispose();
    }
    [DllImport(Security)] private static extern int SecItemAdd(IntPtr attributes,IntPtr result);
    [DllImport(Security)] private static extern int SecItemUpdate(IntPtr query,IntPtr attributes);
    [DllImport(Security)] private static extern int SecItemCopyMatching(IntPtr query,out IntPtr result);
    [DllImport(Security)] private static extern int SecItemDelete(IntPtr query);
    [DllImport(Foundation)] private static extern IntPtr CFDictionaryCreateMutable(IntPtr allocator,nint capacity,IntPtr keyCallbacks,IntPtr valueCallbacks);
    [DllImport(Foundation)] private static extern void CFDictionarySetValue(IntPtr dictionary,IntPtr key,IntPtr value);
    [DllImport(Foundation,CharSet=CharSet.Unicode)] private static extern IntPtr CFStringCreateWithCharacters(IntPtr allocator,string characters,nint length);
    [DllImport(Foundation)] private static extern IntPtr CFDataCreate(IntPtr allocator,byte[] bytes,nint length);
    [DllImport(Foundation)] private static extern nint CFDataGetLength(IntPtr data);
    [DllImport(Foundation)] private static extern IntPtr CFDataGetBytePtr(IntPtr data);
    [DllImport(Foundation)] private static extern nuint CFDataGetTypeID();
    [DllImport(Foundation)] private static extern nuint CFGetTypeID(IntPtr value);
    [DllImport(Foundation)] private static extern void CFRelease(IntPtr value);
}
