using System.Data;
using Npgsql;
using Verifier.Core;
namespace Verifier.Data;

public sealed partial class PostgreSqlDatabase
{
    private Reason dataSessionFailure;
    private void EnsureReadSession(CancellationToken token)
    {
        token.ThrowIfCancellationRequested();
        if(dataSessionFailure!=Reason.None)throw new VerificationException(dataSessionFailure);
        if(connection?.State!=ConnectionState.Open)
        {dataSessionFailure=Reason.ConnectionLost;throw new VerificationException(dataSessionFailure);}
    }
    private VerificationException ReadFailure(NpgsqlException error)
    {
        var mapped=Map(error);
        // A cancelled/timed-out statement can leave this RR transaction aborted.
        // Keep its first terminal cause for unread scopes; never reconnect/resume.
        if(dataSessionFailure==Reason.None&&mapped.Reason is Reason.Timeout or Reason.ConnectionLost)dataSessionFailure=mapped.Reason;
        return new(dataSessionFailure==Reason.None?mapped.Reason:dataSessionFailure);
    }
    private VerificationException LostReadSession()
    {if(dataSessionFailure==Reason.None)dataSessionFailure=Reason.ConnectionLost;return new(dataSessionFailure);}
    private static VerificationException Map(NpgsqlException error)
    {
        if(error is PostgresException postgres)return Map(postgres);
        for(Exception? cause=error;cause!=null;cause=cause.InnerException)
            if(cause is TimeoutException)return new(Reason.Timeout);
        return new(Reason.ConnectionLost);
    }
}
