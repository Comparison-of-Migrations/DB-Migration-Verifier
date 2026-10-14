using Verifier.Core;

namespace Verifier.Data;

/// <summary>Owns both verified observers. A partially opened pair is never returned.</summary>
public sealed class ConnectedDatabasePair : IAsyncDisposable
{
    public IReadDatabase Source { get; }
    public IReadDatabase Target { get; }
    public Inventory SourceInventory { get; }
    public Inventory TargetInventory { get; }
    private ConnectedDatabasePair(IReadDatabase source,IReadDatabase target,Inventory a,Inventory b)
    { Source=source;Target=target;SourceInventory=a;TargetInventory=b; }

    public static async Task<ConnectedDatabasePair> OpenAsync(ConnectionSettings source,ConnectionSettings target,
        CancellationToken token,Func<ConnectionSettings,IReadDatabase>? factory=null)
    {
        source.Validate();target.Validate();
        if(source.Kind!=target.Kind)throw new VerificationException(Reason.InvalidInput);
        token.ThrowIfCancellationRequested();factory??=DatabaseFactory.Create;
        IReadDatabase? a=null,b=null;
        try
        {
            a=factory(source);var ai=await a.InspectAsync(token);
            b=factory(target);var bi=await b.InspectAsync(token);
            token.ThrowIfCancellationRequested();
            if(string.IsNullOrWhiteSpace(ai.Identity)||string.IsNullOrWhiteSpace(bi.Identity))throw new VerificationException(Reason.IdentityUnknown);
            if(ai.Identity==bi.Identity)throw new VerificationException(Reason.SameDatabase);
            return new(a,b,ai,bi);
        }
        catch
        {
            // Dispose both sides even if opening/inspection failed; preserve the safe original failure.
            try{if(a!=null)await a.DisposeAsync();}catch{}
            try{if(b!=null)await b.DisposeAsync();}catch{}
            throw;
        }
    }
    public async ValueTask DisposeAsync()
    {try{await Source.DisposeAsync();}finally{await Target.DisposeAsync();}}
}
