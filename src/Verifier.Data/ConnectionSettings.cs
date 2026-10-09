using Microsoft.Data.SqlClient;
using Npgsql;
using Verifier.Core;

namespace Verifier.Data;

// Credentials stay in memory and never enter the report model.
public sealed record ConnectionSettings(DbKind Kind,string Host,int Port,string Database,string User,string Password,string? RootCertificate=null)
{
    public void Validate()
    {
        if(string.IsNullOrWhiteSpace(Host) || string.IsNullOrWhiteSpace(Database) || string.IsNullOrWhiteSpace(User) || Port is <1 or >65535)
            throw new VerificationException(Reason.InvalidInput);
    }
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
        return new SqlConnectionStringBuilder { DataSource=Host+","+Port,InitialCatalog=Database,UserID=User,Password=Password,
            Encrypt=SqlConnectionEncryptOption.Mandatory,TrustServerCertificate=false,ConnectTimeout=10,Pooling=false,
            ApplicationName="DB-Migration-Verifier" }.ConnectionString;
    }
}
public static class DatabaseFactory
{
    public static IReadDatabase Create(ConnectionSettings settings) => settings.Kind switch
    { DbKind.PostgreSql=>new PostgreSqlDatabase(settings),DbKind.SqlServer=>new SqlServerDatabase(settings),_=>throw new VerificationException(Reason.NotImplemented) };
}
