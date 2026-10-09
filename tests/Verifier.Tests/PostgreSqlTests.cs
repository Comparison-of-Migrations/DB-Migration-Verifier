using Npgsql;
using Verifier.Core;
using Verifier.Data;
using Xunit;

namespace Verifier.Tests;
public sealed class PostgreSqlFact : FactAttribute
{
    public PostgreSqlFact() { if(Environment.GetEnvironmentVariable("DMV_TEST_PG_PORT")==null) Skip="Run tools/test-postgres.sh for the disposable TLS fixture."; }
}
public sealed class PostgreSqlTheory : TheoryAttribute
{
    public PostgreSqlTheory() { if(Environment.GetEnvironmentVariable("DMV_TEST_PG_PORT")==null) Skip="Run tools/test-postgres.sh for the disposable TLS fixture."; }
}
public sealed class PostgreSqlTests
{
    private static ConnectionSettings Settings(string database,string user="postgres",string? root=null)=>new(DbKind.PostgreSql,"localhost",int.Parse(Environment.GetEnvironmentVariable("DMV_TEST_PG_PORT")!),database,user,Environment.GetEnvironmentVariable("DMV_TEST_PG_PASSWORD")!,root??Environment.GetEnvironmentVariable("DMV_TEST_PG_CA"));
    private static async Task<RunReport> Run(string table,string user="postgres")
    {
        await using var a=new PostgreSqlDatabase(Settings("dmv_source",user)); await using var b=new PostgreSqlDatabase(Settings("dmv_target",user));
        return await new ComparisonEngine().RunAsync(a,b,new HashSet<Category>{Category.Data,Category.Schema,Category.Access,Category.Objects},new HashSet<string>{table},"PostgreSql-v1",false,null,default);
    }
    [PostgreSqlFact] public async Task ExactSameRowsDifferentOrderAndBigNumbers()=>Assert.Equal(Verdict.Equal,(await Run("demo.same")).Checks[0].Verdict);
    [PostgreSqlFact] public async Task SameCountDifferentTextAndDuplicates()=>Assert.Equal(Verdict.Different,(await Run("demo.changed")).Checks[0].Verdict);
    [PostgreSqlFact] public async Task LimitedAccountCannotHideDifferenceAsEqual()
    { var r=await Run("demo.hidden","dmv_limited"); Assert.False(r.AllEqual); Assert.NotEqual(Verdict.Equal,r.Checks[0].Verdict); }
    [PostgreSqlFact] public async Task RlsCannotProduceFullEquality()
    { var r=await Run("demo.policy","dmv_limited"); Assert.NotEqual(Verdict.Equal,r.Checks[0].Verdict); Assert.Equal(Reason.RowFiltered,r.Checks[0].Reason); }
    [PostgreSqlFact] public async Task NativeJsonWithoutCodecIsUnverified()=>Assert.Equal(Reason.UnsupportedType,(await Run("demo.unsupported")).Checks[0].Reason);
    [PostgreSqlFact] public async Task OversizedActualCellCannotPass()=>Assert.Equal(Reason.RowTooLarge,(await Run("demo.large")).Checks[0].Reason);
    [PostgreSqlFact] public async Task ZeroColumnTableRowsAreNotLost()=>Assert.Equal(Verdict.Different,(await Run("demo.empty_columns")).Checks[0].Verdict);
    [PostgreSqlFact] public async Task UserSchemaStartingWithPgIsNotSilentlyExcluded()=>Assert.Equal(Verdict.Different,(await Run("pgfoo.observed")).Checks[0].Verdict);
    [PostgreSqlFact] public async Task ActualEmptyDatabasesHaveNoEqualVerdict()
    {
        await using var a=new PostgreSqlDatabase(Settings("dmv_empty_source"));await using var b=new PostgreSqlDatabase(Settings("dmv_empty_target"));
        var result=await new ComparisonEngine().RunAsync(a,b,new HashSet<Category>{Category.Data},null,"PostgreSql-v1",false,null,default);
        Assert.Equal(Reason.NoObjects,result.Checks[0].Reason);Assert.False(result.AllEqual);
    }
    [PostgreSqlFact] public async Task ActualRenamedAndReorderedColumnsCompareWithoutChangingDatabases()
    {
        await using var a=new PostgreSqlDatabase(Settings("dmv_source"));await using var b=new PostgreSqlDatabase(Settings("dmv_target"));
        var report=await new ComparisonEngine().RunAsync(a,b,new HashSet<Category>{Category.Data,Category.Schema},new HashSet<string>{"source:demo.rename_source"},"PostgreSql-v1",false,null,default,[new("demo.rename_source","demo.rename_target",[new("identifier","new_identifier"),new("label","title")])]);
        Assert.Equal(Verdict.Equal,report.Checks[0].Verdict);Assert.Equal(Verdict.Different,report.Checks[2].Verdict);Assert.Equal(2,report.Version);Assert.Single(report.Mappings);
    }
    [PostgreSqlFact] public async Task FullyObservedActualStructureIsEqualWithoutCertifyingOtherCategories()
    {
        var r=await Run("structure.same");Assert.All(r.Checks.Where(c=>c.Category==Category.Schema),c=>{Assert.Equal(Verdict.Equal,c.Verdict);Assert.Equal(Reason.None,c.Reason);Assert.Empty(c.Findings);Assert.Empty(c.Issues);});Assert.False(r.AllEqual);
    }
    [PostgreSqlTheory]
    [InlineData("column_type","ColumnType")][InlineData("column_null","ColumnNullable")][InlineData("column_default","ColumnDefault")]
    [InlineData("column_identity","ColumnIdentity")][InlineData("column_generated","ColumnDefault")][InlineData("column_storage","ColumnStorage")]
    [InlineData("column_compression","ColumnCompression")][InlineData("column_statistics","ColumnStatisticsTarget")][InlineData("column_collation","ColumnCollation")]
    [InlineData("fk_action","ConstraintDefinition")][InlineData("fk_deferred","ConstraintDeferrable")][InlineData("fk_initial","ConstraintDeferred")]
    [InlineData("check_definition","ConstraintDefinition")][InlineData("check_validated","ConstraintValidated")][InlineData("constraint_missing","ConstraintDefinition")]
    [InlineData("exclusion","ConstraintDeferrable")][InlineData("index_key","IndexDefinition")][InlineData("index_include","IndexColumnCount")]
    [InlineData("index_predicate","IndexDefinition")][InlineData("index_expression","IndexDefinition")][InlineData("index_unique","IndexUnique")]
    [InlineData("index_nulls","IndexNulls")][InlineData("index_method","IndexDefinition")][InlineData("index_options","IndexOptions")]
    [InlineData("replica","TableReplicaIdentity")][InlineData("persistence","TablePersistence")][InlineData("partitioned","PartitionChildren")]
    [InlineData("partition_leaf","PartitionBound")][InlineData("inheritance_parent","PartitionChildren")][InlineData("inheritance_child","PartitionParents")]
    public async Task ActualDeclarationChangesAreReportedAtTheRightAttribute(string table,string kind)
    {
        var r=await Run("structure."+table);var check=Assert.Single(r.Checks,c=>c.CheckId==CatalogComparison.Fields[kind].Check);Assert.Equal(Verdict.Different,check.Verdict);Assert.Contains(check.Findings,f=>f.Kind==kind);Assert.All(check.Findings,f=>Assert.Equal("structure."+table,f.Table));
    }
    [PostgreSqlFact] public async Task ForeignDefinitionsAreDiscoveredButCannotCertifyFullStructure()
    {var r=await Run("structure.foreign_observed");Assert.All(r.Checks.Where(c=>c.Category==Category.Schema),c=>{Assert.NotEqual(Verdict.Equal,c.Verdict);Assert.Equal(Reason.NotImplemented,c.Reason);});Assert.NotEqual(Verdict.Equal,r.Checks[0].Verdict);}
    [PostgreSqlFact] public async Task UntrustedCertificateIsRejectedWithoutFallback()
    {
        await using var db=new PostgreSqlDatabase(Settings("dmv_source",root:Environment.GetEnvironmentVariable("DMV_TEST_PG_BAD_CA")));
        var error=await Assert.ThrowsAsync<VerificationException>(()=>db.InspectAsync(default)); Assert.Equal(Reason.ConnectionLost,error.Reason);
    }
    [PostgreSqlFact] public async Task CertificateHostnameMismatchIsRejected()
    {
        await using var db=new PostgreSqlDatabase(Settings("dmv_source") with { Host="127.0.0.1" });
        var error=await Assert.ThrowsAsync<VerificationException>(()=>db.InspectAsync(default)); Assert.Equal(Reason.ConnectionLost,error.Reason);
    }
    [PostgreSqlFact] public async Task ObserverTransactionIsReadOnlyRepeatableAndEncrypted()
    {
        await using var db=new PostgreSqlDatabase(Settings("dmv_source")); _=await db.InspectAsync(default);
        var connection=(NpgsqlConnection)typeof(PostgreSqlDatabase).GetField("connection",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance)!.GetValue(db)!;
        await using var command=new NpgsqlCommand("SELECT pg_catalog.current_setting('transaction_read_only'),pg_catalog.current_setting('transaction_isolation'),pg_catalog.current_setting('row_security'),ssl FROM pg_catalog.pg_stat_ssl WHERE pid=pg_catalog.pg_backend_pid()",connection);
        await using var reader=await command.ExecuteReaderAsync(); Assert.True(await reader.ReadAsync()); Assert.Equal("on",reader.GetString(0)); Assert.Equal("repeatable read",reader.GetString(1)); Assert.Equal("off",reader.GetString(2)); Assert.True(reader.GetBoolean(3));
    }
    [PostgreSqlFact] public async Task ComparisonLeavesDataAndDefinitionsUnchanged()
    {
        var before=await Fingerprint(); _=await Run("demo.changed"); var after=await Fingerprint(); Assert.Equal(before,after);
    }
    private static async Task<string> Fingerprint()
    {
        var all=new System.Text.StringBuilder();
        foreach(var database in new[]{"dmv_source","dmv_target"})
        {
            await using var connection=new NpgsqlConnection(Settings(database).PostgreSqlString()); await connection.OpenAsync();
            await using var command=new NpgsqlCommand("SELECT pg_catalog.md5(pg_catalog.string_agg(row::pg_catalog.text,',' ORDER BY row::pg_catalog.text)) FROM demo.changed row",connection);
            all.Append(await command.ExecuteScalarAsync());
            command.CommandText="SELECT pg_catalog.md5(pg_catalog.string_agg(attname || ':' || atttypid::pg_catalog.text || ':' || attnotnull::pg_catalog.text,',' ORDER BY attnum)) FROM pg_catalog.pg_attribute WHERE attrelid='demo.changed'::pg_catalog.regclass AND attnum>0";
            all.Append(await command.ExecuteScalarAsync());
        }
        return all.ToString();
    }
}
