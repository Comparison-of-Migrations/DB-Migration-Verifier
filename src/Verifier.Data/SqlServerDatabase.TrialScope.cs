using Verifier.Core;

namespace Verifier.Data;

public sealed partial class SqlServerDatabase
{
    private const string TrialPreparationReferences="""
        SELECT CONVERT(varbinary(max),CONVERT(nvarchar(max),cs.name)) AS [__dbmv_utf16],CONVERT(varbinary(max),CONVERT(nvarchar(max),ct.name)) AS [__dbmv_utf16],CONVERT(varbinary(max),CONVERT(nvarchar(max),ps.name)) AS [__dbmv_utf16],CONVERT(varbinary(max),CONVERT(nvarchar(max),pt.name)) AS [__dbmv_utf16]
        FROM sys.foreign_keys fk
        JOIN sys.tables ct ON ct.object_id=fk.parent_object_id
        JOIN sys.schemas cs ON cs.schema_id=ct.schema_id
        JOIN sys.tables pt ON pt.object_id=fk.referenced_object_id
        JOIN sys.schemas ps ON ps.schema_id=pt.schema_id
        """;

    internal static IReadOnlySet<string> ExpandTrialPreparationScope(string root,IEnumerable<string?[]> references,CancellationToken token)
    {
        token.ThrowIfCancellationRequested();
        var parents=new Dictionary<string,List<string>>(StringComparer.Ordinal);
        foreach(var row in references)
        {
            token.ThrowIfCancellationRequested();
            if(row.Length!=4||row.Any(string.IsNullOrEmpty))throw new VerificationException(Reason.ObservationChanged);
            var child=new Table(row[0]!,row[1]!,[],false).Qualified;
            var parent=new Table(row[2]!,row[3]!,[],false).Qualified;
            if(!parents.TryGetValue(child,out var list))parents.Add(child,list=[]);
            list.Add(parent);
        }
        var scope=new HashSet<string>(StringComparer.Ordinal){root};var pending=new Queue<string>();pending.Enqueue(root);
        while(pending.TryDequeue(out var table))
        {
            token.ThrowIfCancellationRequested();
            if(!parents.TryGetValue(table,out var list))continue;
            foreach(var parent in list)
            {
                token.ThrowIfCancellationRequested();
                if(scope.Add(parent))pending.Enqueue(parent);
            }
        }
        return scope;
    }
}
