using Xunit;

namespace WorkFlow.IntegrationTests.Infrastructure;

public static class TestCollectionNames
{
    public const string PostgreSql = "PostgreSQL";
}

[CollectionDefinition(
    TestCollectionNames.PostgreSql,
    DisableParallelization = true)]
public sealed class PostgreSqlCollection
    : ICollectionFixture<PostgreSqlTestDatabase>
{
}