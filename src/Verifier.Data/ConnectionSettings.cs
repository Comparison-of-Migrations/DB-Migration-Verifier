using Microsoft.Data.SqlClient;
using Npgsql;
using MySqlConnector;
using Verifier.Core;

namespace Verifier.Data;

// Active credentials stay in memory and never enter the report model. Explicit
// optional persistence uses LocalConnectionStore references and the OS vault.
public sealed record ConnectionSettings(DbKind Kind,string Host,int Port,string Database,string User,string Password,string? RootCertificate=null,string? ServerCertificate=null)
{
    public void Validate()
    {
        // SqlClient replaces isolated UTF-16 units on the wire. Reject such
        // connection values before they can identify a different database/user.
        if(Kind==DbKind.SqlServer&&new[]{Host,Database,User,Password}.Any(value=>value!=null&&SqlServerMetadata.HasUnpairedSurrogate(value)))
            throw new VerificationException(Reason.InvalidInput);
        if(Kind!=DbKind.SqlServer&&!string.IsNullOrWhiteSpace(ServerCertificate))throw new VerificationException(Reason.InvalidInput);
        if(string.IsNullOrWhiteSpace(Host) || string.IsNullOrWhiteSpace(Database) || string.IsNullOrWhiteSpace(User) || Port is <1 or >65535)
            throw new VerificationException(Reason.InvalidInput);
    }
    public override string ToString()=>"ConnectionSettings (credentials redacted)";
    public string PostgreSqlString()
    {
        Validate();
        return new NpgsqlConnectionStringBuilder { Host=Host,Port=Port,Database=Database,Username=User,Password=Password,
            SslMode=SslMode.VerifyFull,RootCertificate=RootCertificate,Timeout=10,CommandTimeout=60,Pooling=false,
            IncludeErrorDetail=false,ApplicationName="DB-Migration-Verifier" }.ConnectionString;
    }
    public string SqlServerString()
    {
        Validate();
        // CA chains and exact server-certificate pins are different trust modes.
        // An entered CA path must never silently become a leaf pin.
        if(!string.IsNullOrWhiteSpace(RootCertificate))throw new VerificationException(Reason.InvalidInput);
        var builder=new SqlConnectionStringBuilder { DataSource="tcp:"+Host+","+Port,InitialCatalog=Database,UserID=User,Password=Password,
            Encrypt=SqlConnectionEncryptOption.Mandatory,TrustServerCertificate=false,ConnectTimeout=10,Pooling=false,
            ApplicationName="DB-Migration-Verifier" };
        if(!string.IsNullOrWhiteSpace(ServerCertificate))
        {
            try
            {
                using var certificate=System.Security.Cryptography.X509Certificates.X509CertificateLoader.LoadCertificateFromFile(ServerCertificate);
                if(certificate.Extensions.OfType<System.Security.Cryptography.X509Certificates.X509BasicConstraintsExtension>().Any(x=>x.CertificateAuthority)||
                   certificate.NotBefore.ToUniversalTime()>DateTime.UtcNow||certificate.NotAfter.ToUniversalTime()<DateTime.UtcNow)
                    throw new VerificationException(Reason.InvalidInput);
            }
            catch(Exception e)when(e is IOException or UnauthorizedAccessException or System.Security.Cryptography.CryptographicException)
            {throw new VerificationException(Reason.InvalidInput);}
            builder.ServerCertificate=ServerCertificate;
        }
        return builder.ConnectionString;
    }

    public string MySqlString()
    {
        Validate();
        return new MySqlConnectionStringBuilder{Server=Host,Port=(uint)Port,Database=Database,UserID=User,Password=Password,
            SslMode=MySqlSslMode.VerifyFull,SslCa=RootCertificate,Pooling=false,ConnectionTimeout=10,DefaultCommandTimeout=60,
            AllowLoadLocalInfile=false,AllowPublicKeyRetrieval=false,TreatTinyAsBoolean=false,GuidFormat=MySqlGuidFormat.None,
            ApplicationName="DB-Migration-Verifier"}.ConnectionString;
    }
}
public static class DatabaseFactory
{
    public static IReadDatabase Create(ConnectionSettings settings) => settings.Kind switch
    { DbKind.PostgreSql=>new PostgreSqlDatabase(settings),DbKind.SqlServer=>new SqlServerDatabase(settings),DbKind.MySql=>new MySqlDatabase(settings),DbKind.MariaDb=>new MariaDbDatabase(settings),_=>throw new VerificationException(Reason.NotImplemented) };
}
