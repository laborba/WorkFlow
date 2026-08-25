using Microsoft.EntityFrameworkCore;
using WorkFlow.IntegrationTests.Infrastructure;
using Xunit;

namespace WorkFlow.IntegrationTests.Persistence;

[Collection(TestCollectionNames.PostgreSql)]
public sealed class WorkFlowDbContextConnectionTests
{
    private readonly PostgreSqlTestDatabase _database;

    public WorkFlowDbContextConnectionTests(
        PostgreSqlTestDatabase database)
    {
        _database = database;
    }

    [Fact]
    public async Task
        CanConnectAsync_ShouldReturnTrue_WhenUsingTestDatabase()
    {
        await using var context =
            _database.CreateDbContext();

        var canConnect =
            await context.Database.CanConnectAsync();

        Assert.True(canConnect);
    }
}