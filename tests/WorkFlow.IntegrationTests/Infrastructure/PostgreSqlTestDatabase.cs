using Microsoft.EntityFrameworkCore;
using Npgsql;
using WorkFlow.Infrastructure.Persistence;
using Xunit;

namespace WorkFlow.IntegrationTests.Infrastructure;

public sealed class PostgreSqlTestDatabase : IAsyncLifetime
{
    private const string ConnectionStringVariable =
        "WORKFLOW_TEST_CONNECTION_STRING";

    private readonly string _connectionString;

    public PostgreSqlTestDatabase()
    {
        _connectionString =
            Environment.GetEnvironmentVariable(
                ConnectionStringVariable)
            ?? throw new InvalidOperationException(
                $"A variável de ambiente " +
                $"'{ConnectionStringVariable}' não foi configurada.");

        var connectionStringBuilder =
            new NpgsqlConnectionStringBuilder(
                _connectionString);

        if (!string.Equals(
                connectionStringBuilder.Database,
                "workflow_test",
                StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                "Os testes de integração só podem utilizar " +
                "o banco 'workflow_test'.");
        }
    }

    public WorkFlowDbContext CreateDbContext()
    {
        var options =
            new DbContextOptionsBuilder<WorkFlowDbContext>()
                .UseNpgsql(_connectionString)
                .Options;

        return new WorkFlowDbContext(options);
    }

    public async Task InitializeAsync()
    {
        await using var context = CreateDbContext();

        await context.Database.MigrateAsync();
    }

    public Task DisposeAsync()
    {
        return Task.CompletedTask;
    }
}