using Npgsql;
using NpgsqlTypes;
using Verifier.Core;
namespace Verifier.Data;
public sealed partial class PostgreSqlDatabase
{
    private sealed record DomainRules(bool NotNull,IReadOnlyList<(string? Expression,string Nodes)> Checks);
    private readonly Dictionary<uint,DomainRules> domainRules=[];
    private async Task ObserveDomainConstraints(Table table,List<ScopeIssue> issues,Func<string,string?,string,Task> count,CancellationToken token)
    {
        foreach(var column in table.Columns.Where(c=>domainValueTypes.Contains(c.TypeOid)))
        {
            var query="SELECT "+Quote(column.Name)+" AS value FROM "+ReadRelation(table);
            await Visit(column.TypeOid,query,[]);
            async Task Visit(uint current,string query,HashSet<uint> seen)
            {
                token.ThrowIfCancellationRequested();
                if(!seen.Add(current)||!valueTypes.TryGetValue(current,out var type))return;
                if(type.Kind=="d")
                {
                    if(!domainRules.TryGetValue(current,out var rules))
                    {
                        await using var command=new NpgsqlCommand(GuardDeparsers("SELECT t.typnotnull,pg_catalog.pg_get_expr(c.conbin,0,false),c.conbin::pg_catalog.text FROM pg_catalog.pg_type t LEFT JOIN pg_catalog.pg_constraint c ON c.contypid=t.oid AND c.contype='c' WHERE t.oid=@id"),connection,transaction);
                        command.Parameters.AddWithValue("id",NpgsqlDbType.Oid,current);var checks=new List<(string?,string)>();bool notNull=false;
                        await using(var reader=await command.ExecuteReaderAsync(token))while(await reader.ReadAsync(token))
                        {notNull=reader.GetBoolean(0);if(!reader.IsDBNull(2))checks.Add((reader.IsDBNull(1)?null:reader.GetString(1),reader.GetString(2)));}
                        rules=new(notNull,checks);domainRules[current]=rules;
                    }
                    if(rules.NotNull)await count("NotNull",column.Name,"SELECT count(*) FROM ("+query+") domain_values WHERE pg_catalog.num_nonnulls(value)=0");
                    foreach(var (expression,nodes) in rules.Checks)
                    {
                        if(expression==null||expression.StartsWith(BlockedDefinition,StringComparison.Ordinal)||!await SafeExpression(nodes,token)){issues.Add(new(table.Qualified,column.Name,Reason.NotImplemented));continue;}
                        // Server-deparsed VALUE resolves to this derived column; no SQL text replacement.
                        await count("Check",column.Name,"SELECT count(*) FROM ("+query+") domain_values WHERE ("+expression+") IS FALSE");
                    }
                    await Visit(type.Base,query,seen);
                }
                else if(type.Element!=0)
                    await Visit(type.Element,"SELECT pg_catalog.unnest(value) AS value FROM ("+query+") domain_arrays",seen);
                else if(type.Fields.Length>0)
                {
                    if(type.Fields.Length!=type.FieldNames.Length)throw new VerificationException(Reason.ObservationChanged);
                    // A SQL-NULL composite has no stored fields. An all-NULL record does,
                    // so IS NOT NULL (which tests its fields) is not appropriate here.
                    for(var i=0;i<type.Fields.Length;i++)
                        await Visit(type.Fields[i],"SELECT (value)."+Quote(type.FieldNames[i])+" AS value FROM ("+query+") domain_records WHERE pg_catalog.num_nonnulls(value)=1",new(seen));
                }
                else if(type.Range!=0)
                {
                    foreach(var bound in new[]{"lower","upper"})
                        await Visit(type.Range,"SELECT pg_catalog."+bound+"(value) AS value FROM ("+query+") domain_ranges WHERE pg_catalog.num_nonnulls(value)=1 AND NOT pg_catalog.isempty(value) AND NOT pg_catalog."+bound+"_inf(value)",new(seen));
                }
                else if(type.MultiRange!=0)
                    await Visit(type.MultiRange,"SELECT pg_catalog.unnest(value) AS value FROM ("+query+") domain_multiranges",seen);
            }
        }
    }
}
