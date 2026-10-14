using System.Runtime.Versioning;
using System.Security.AccessControl;
using System.Security.Principal;
namespace Verifier.Core;

internal static class PrivateStorage
{
    internal static void CreateDirectory(string path)
    {
        if(Directory.Exists(path)&&(File.GetAttributes(path)&FileAttributes.ReparsePoint)!=0)throw new VerificationException(Reason.TemporaryIntegrity);
        if(OperatingSystem.IsWindows()){CreateWindowsDirectory(path);return;}
        var mode=UnixFileMode.UserRead|UnixFileMode.UserWrite|UnixFileMode.UserExecute;
        Directory.CreateDirectory(path,mode);
        if(File.GetUnixFileMode(path)!=mode)throw new VerificationException(Reason.TemporaryStorage);
    }
    internal static FileStream CreateFile(string path,FileShare share=FileShare.Read)
    {
        var options=new FileStreamOptions{Mode=FileMode.CreateNew,Access=FileAccess.ReadWrite,Share=share,BufferSize=65536};
        if(!OperatingSystem.IsWindows())options.UnixCreateMode=UnixFileMode.UserRead|UnixFileMode.UserWrite;
        var stream=new FileStream(path,options);
        try{if(OperatingSystem.IsWindows())ValidateWindowsFile(path);return stream;}catch{stream.Dispose();throw;}
    }
    [SupportedOSPlatform("windows")]
    private static void CreateWindowsDirectory(string path)
    {
        var sid=WindowsIdentity.GetCurrent().User??throw new VerificationException(Reason.TemporaryStorage);
        var security=new DirectorySecurity();security.SetOwner(sid);security.SetAccessRuleProtection(true,false);
        security.AddAccessRule(new FileSystemAccessRule(sid,FileSystemRights.FullControl,InheritanceFlags.ContainerInherit|InheritanceFlags.ObjectInherit,PropagationFlags.None,AccessControlType.Allow));
        new DirectoryInfo(path).Create(security);
        var actual=new DirectoryInfo(path).GetAccessControl();
        if(!actual.AreAccessRulesProtected||!Equals(actual.GetOwner(typeof(SecurityIdentifier)),sid))throw new VerificationException(Reason.TemporaryStorage);
        ValidateRules(actual,sid);
    }
    [SupportedOSPlatform("windows")]
    internal static void ValidateWindowsFile(string path)
    {
        var sid=WindowsIdentity.GetCurrent().User??throw new VerificationException(Reason.TemporaryStorage);
        ValidateRules(new FileInfo(path).GetAccessControl(),sid);
    }
    [SupportedOSPlatform("windows")]
    private static void ValidateRules(FileSystemSecurity security,SecurityIdentifier sid)
    {
        var rules=security.GetAccessRules(true,true,typeof(SecurityIdentifier)).Cast<FileSystemAccessRule>().ToArray();
        if(rules.Length==0||rules.Any(r=>r.AccessControlType!=AccessControlType.Allow||!Equals(r.IdentityReference,sid))||!rules.Any(r=>(r.FileSystemRights&FileSystemRights.FullControl)==FileSystemRights.FullControl))throw new VerificationException(Reason.TemporaryStorage);
    }
}
