using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Security.Cryptography;
using System.Text;

namespace Verifier.Data;

[SupportedOSPlatform("windows")]
internal sealed class WindowsCredentialVault:ICredentialVault
{
    public void Write(Guid reference,string password)
    {
        var name=CredentialVault.Key(reference);ArgumentNullException.ThrowIfNull(password);
        var bytes=CredentialVault.Encode(password);
        var memory=IntPtr.Zero;
        try
        {
            memory=Marshal.AllocHGlobal(bytes.Length);if(bytes.Length>0)Marshal.Copy(bytes,0,memory,bytes.Length);
            var credential=new Credential{Type=1,TargetName=name,UserName=CredentialVault.Service,Comment="DB-Migration-Verifier selected password",CredentialBlobSize=(uint)bytes.Length,CredentialBlob=memory,Persist=2};
            if(!CredWriteW(ref credential,0))throw Failure();
        }
        finally
        {
            CryptographicOperations.ZeroMemory(bytes);
            if(memory!=IntPtr.Zero){if(bytes.Length>0)Marshal.Copy(bytes,0,memory,bytes.Length);Marshal.FreeHGlobal(memory);}
        }
    }
    public string? Read(Guid reference)
    {
        var name=CredentialVault.Key(reference);
        if(!CredReadW(name,1,0,out var result)){if(Marshal.GetLastPInvokeError()==1168)return null;throw Failure();}
        try
        {
            var credential=Marshal.PtrToStructure<Credential>(result);
            if(credential.Type!=1||credential.TargetName!=name||credential.CredentialBlobSize>int.MaxValue||credential.CredentialBlobSize>0&&credential.CredentialBlob==IntPtr.Zero)throw new CredentialVaultException(VaultFailure.InvalidData);
            var bytes=new byte[(int)credential.CredentialBlobSize];
            try{if(bytes.Length>0)Marshal.Copy(credential.CredentialBlob,bytes,0,bytes.Length);return CredentialVault.Decode(bytes);}
            finally{CryptographicOperations.ZeroMemory(bytes);}
        }
        finally{CredFree(result);}
    }
    public void Delete(Guid reference)
    {if(!CredDeleteW(CredentialVault.Key(reference),1,0)&&Marshal.GetLastPInvokeError()!=1168)throw Failure();}
    private static CredentialVaultException Failure(){var code=Marshal.GetLastPInvokeError();return new(code is 5?VaultFailure.Denied:VaultFailure.Unavailable,code);}
    [StructLayout(LayoutKind.Sequential,CharSet=CharSet.Unicode)]
    private struct Credential
    {
        internal uint Flags,Type;internal string TargetName;internal string? Comment;internal long LastWritten;
        internal uint CredentialBlobSize;internal IntPtr CredentialBlob;internal uint Persist,AttributeCount;
        internal IntPtr Attributes;internal string? TargetAlias,UserName;
    }
    [DllImport("advapi32.dll",CharSet=CharSet.Unicode,SetLastError=true,ExactSpelling=true)]
    [return:MarshalAs(UnmanagedType.Bool)] private static extern bool CredWriteW(ref Credential credential,uint flags);
    [DllImport("advapi32.dll",CharSet=CharSet.Unicode,SetLastError=true,ExactSpelling=true)]
    [return:MarshalAs(UnmanagedType.Bool)] private static extern bool CredReadW(string target,uint type,uint flags,out IntPtr credential);
    [DllImport("advapi32.dll",CharSet=CharSet.Unicode,SetLastError=true,ExactSpelling=true)]
    [return:MarshalAs(UnmanagedType.Bool)] private static extern bool CredDeleteW(string target,uint type,uint flags);
    [DllImport("advapi32.dll",ExactSpelling=true)] private static extern void CredFree(IntPtr credential);
}
