using System.Net.Security;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using MySqlConnector;
using Verifier.Core;
namespace Verifier.Data;
public sealed partial class MariaDbDatabase
{
    private async Task OpenTls(CancellationToken token)
    {
        // MySqlConnector 2.6.2 can accept MariaDB's password-bound certificate
        // fingerprint despite VerifyFull name/chain errors. Product policy
        // requires the requested hostname AND trusted certificate chain.
        // A Required-mode callback rejects errors during the TLS handshake;
        // it never delegates trust to that post-authentication fallback.
        var options=new MySqlConnectionStringBuilder(settings.MySqlString()){SslMode=MySqlSslMode.Required,SslCa="",ServerRedirectionMode=MySqlServerRedirectionMode.Disabled};
        connection=new(options.ConnectionString);var roots=new X509Certificate2Collection();
        try
        {
            if(!string.IsNullOrWhiteSpace(settings.RootCertificate))roots.ImportFromPemFile(settings.RootCertificate);
            if(!string.IsNullOrWhiteSpace(settings.RootCertificate)&&roots.Count==0)throw new VerificationException(Reason.InvalidInput);
            // Cancellation opens a driver-owned connection and reuses this callback.
            // Retain immutable public DER, never the roots disposed after OpenAsync.
            var encodedRoots=roots.Cast<X509Certificate2>().Select(root=>root.RawData).ToArray();
            connection.RemoteCertificateValidationCallback=(_,certificate,presented,errors)=>
            {
                if(certificate is not X509Certificate2 leaf||!leaf.MatchesHostname(settings.Host,true,true)||(errors&SslPolicyErrors.RemoteCertificateNotAvailable)!=0)return false;
                if(encodedRoots.Length==0)return errors==SslPolicyErrors.None;
                var callbackRoots=new X509Certificate2Collection();
                try
                {
                    foreach(var encoded in encodedRoots)callbackRoots.Add(X509CertificateLoader.LoadCertificate(encoded));
                    using var chain=new X509Chain();chain.ChainPolicy.TrustMode=X509ChainTrustMode.CustomRootTrust;chain.ChainPolicy.CustomTrustStore.AddRange(callbackRoots);chain.ChainPolicy.RevocationMode=X509RevocationMode.Online;chain.ChainPolicy.RevocationFlag=X509RevocationFlag.ExcludeRoot;chain.ChainPolicy.ApplicationPolicy.Add(new Oid("1.3.6.1.5.5.7.3.1"));chain.ChainPolicy.UrlRetrievalTimeout=TimeSpan.FromSeconds(5);
                    if(presented!=null)foreach(var element in presented.ChainElements)chain.ChainPolicy.ExtraStore.Add(element.Certificate);
                    return chain.Build(leaf);
                }
                finally{foreach(var root in callbackRoots)root.Dispose();}
            };
            await connection.OpenAsync(token);
        }
        catch(CryptographicException){throw new VerificationException(Reason.InvalidInput);}
        finally{foreach(var root in roots)root.Dispose();}
    }
}
