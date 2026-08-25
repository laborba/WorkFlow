using Microsoft.EntityFrameworkCore;
using WorkFlow.Domain.Entities;
using WorkFlow.Domain.Enums;
using WorkFlow.IntegrationTests.Infrastructure;
using Xunit;

namespace WorkFlow.IntegrationTests.Persistence;

[Collection(TestCollectionNames.PostgreSql)]
public sealed class ProjectHistoryPersistenceTests
{
    private readonly PostgreSqlTestDatabase _database;

    public ProjectHistoryPersistenceTests(
        PostgreSqlTestDatabase database)
    {
        _database = database;
    }

    [Fact]
    public async Task
        Add_ShouldPersistProjectHistory_WhenDataIsValid()
    {
        await using var context =
            _database.CreateDbContext();

        await using var transaction =
            await context.Database.BeginTransactionAsync();

        var uniqueValue =
            Guid.NewGuid().ToString("N");

        var tenant = new Tenant(
            $"Tenant {uniqueValue}",
            $"REG-{uniqueValue}",
            $"tenant-{uniqueValue}@test.local");

        context.Tenants.Add(tenant);

        await context.SaveChangesAsync();

        var actor = new User(
            tenant.Id,
            "Autor da Alteração",
            $"actor-{uniqueValue}@test.local",
            "actor-password-hash",
            UserRole.ProjectManager);

        context.Users.Add(actor);

        await context.SaveChangesAsync();

        var project = new Project(
            tenant.Id,
            $"Projeto {uniqueValue}",
            actor.Id);

        context.Projects.Add(project);

        await context.SaveChangesAsync();

        var history = new ProjectHistory(
            project.Id,
            actor.Id,
            ProjectHistoryAction.DueDateChanged,
            "O prazo do projeto foi alterado.",
            "2028-01-31",
            "2028-02-29");

        context.ProjectHistories.Add(history);

        await context.SaveChangesAsync();

        var historyId = history.Id;

        context.ChangeTracker.Clear();

        var persistedHistory =
            await context.ProjectHistories.SingleAsync(
                item => item.Id == historyId);

        Assert.Equal(
            project.Id,
            persistedHistory.ProjectId);

        Assert.Equal(
            actor.Id,
            persistedHistory.ActorUserId);

        Assert.Equal(
            ProjectHistoryAction.DueDateChanged,
            persistedHistory.Action);

        Assert.Equal(
            "O prazo do projeto foi alterado.",
            persistedHistory.Reason);

        Assert.Equal(
            "2028-01-31",
            persistedHistory.OldValue);

        Assert.Equal(
            "2028-02-29",
            persistedHistory.NewValue);

        Assert.NotEqual(
            default,
            persistedHistory.CreatedAt);

        await transaction.RollbackAsync();
    }

    [Fact]
    public async Task
    Add_ShouldPersistNullOptionalValues_WhenHistoryIsCreated()
    {
        await using var context =
            _database.CreateDbContext();

        await using var transaction =
            await context.Database.BeginTransactionAsync();

        var uniqueValue =
            Guid.NewGuid().ToString("N");

        var tenant = new Tenant(
            $"Tenant {uniqueValue}",
            $"REG-{uniqueValue}",
            $"tenant-{uniqueValue}@test.local");

        context.Tenants.Add(tenant);

        await context.SaveChangesAsync();

        var actor = new User(
            tenant.Id,
            "Criador do Projeto",
            $"actor-{uniqueValue}@test.local",
            "actor-password-hash",
            UserRole.ProjectManager);

        context.Users.Add(actor);

        await context.SaveChangesAsync();

        var project = new Project(
            tenant.Id,
            $"Projeto {uniqueValue}",
            actor.Id);

        context.Projects.Add(project);

        await context.SaveChangesAsync();

        var history = new ProjectHistory(
            project.Id,
            actor.Id,
            ProjectHistoryAction.Created);

        context.ProjectHistories.Add(history);

        await context.SaveChangesAsync();

        var historyId = history.Id;

        context.ChangeTracker.Clear();

        var persistedHistory =
            await context.ProjectHistories.SingleAsync(
                item => item.Id == historyId);

        Assert.Equal(
            ProjectHistoryAction.Created,
            persistedHistory.Action);

        Assert.Null(
            persistedHistory.Reason);

        Assert.Null(
            persistedHistory.OldValue);

        Assert.Null(
            persistedHistory.NewValue);

        await transaction.RollbackAsync();
    }

    [Fact]
    public async Task
    SaveChanges_ShouldThrow_WhenProjectDoesNotExist()
    {
        await using var context =
            _database.CreateDbContext();

        await using var transaction =
            await context.Database.BeginTransactionAsync();

        var uniqueValue =
            Guid.NewGuid().ToString("N");

        var tenant = new Tenant(
            $"Tenant {uniqueValue}",
            $"REG-{uniqueValue}",
            $"tenant-{uniqueValue}@test.local");

        context.Tenants.Add(tenant);

        await context.SaveChangesAsync();

        var actor = new User(
            tenant.Id,
            "Autor da Alteração",
            $"actor-{uniqueValue}@test.local",
            "actor-password-hash",
            UserRole.ProjectManager);

        context.Users.Add(actor);

        await context.SaveChangesAsync();

        var history = new ProjectHistory(
            long.MaxValue,
            actor.Id,
            ProjectHistoryAction.Created);

        context.ProjectHistories.Add(history);

        await Assert.ThrowsAsync<DbUpdateException>(
            () => context.SaveChangesAsync());

        await transaction.RollbackAsync();
    }

    [Fact]
    public async Task
    SaveChanges_ShouldThrow_WhenActorDoesNotExist()
    {
        await using var context =
            _database.CreateDbContext();

        await using var transaction =
            await context.Database.BeginTransactionAsync();

        var uniqueValue =
            Guid.NewGuid().ToString("N");

        var tenant = new Tenant(
            $"Tenant {uniqueValue}",
            $"REG-{uniqueValue}",
            $"tenant-{uniqueValue}@test.local");

        context.Tenants.Add(tenant);

        await context.SaveChangesAsync();

        var creator = new User(
            tenant.Id,
            "Criador do Projeto",
            $"creator-{uniqueValue}@test.local",
            "creator-password-hash",
            UserRole.ProjectManager);

        context.Users.Add(creator);

        await context.SaveChangesAsync();

        var project = new Project(
            tenant.Id,
            $"Projeto {uniqueValue}",
            creator.Id);

        context.Projects.Add(project);

        await context.SaveChangesAsync();

        var history = new ProjectHistory(
            project.Id,
            long.MaxValue,
            ProjectHistoryAction.Created);

        context.ProjectHistories.Add(history);

        await Assert.ThrowsAsync<DbUpdateException>(
            () => context.SaveChangesAsync());

        await transaction.RollbackAsync();
    }


}