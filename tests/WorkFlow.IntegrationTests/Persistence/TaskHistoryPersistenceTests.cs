using Microsoft.EntityFrameworkCore;
using WorkFlow.Domain.Entities;
using WorkFlow.Domain.Enums;
using WorkFlow.IntegrationTests.Infrastructure;
using Xunit;

namespace WorkFlow.IntegrationTests.Persistence;

[Collection(TestCollectionNames.PostgreSql)]
public sealed class TaskHistoryPersistenceTests
{
    private readonly PostgreSqlTestDatabase _database;

    public TaskHistoryPersistenceTests(
        PostgreSqlTestDatabase database)
    {
        _database = database;
    }

    [Fact]
    public async Task
        Add_ShouldPersistTaskHistory_WhenDataIsValid()
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

        var projectTask = new ProjectTask(
            project.Id,
            $"Tarefa {uniqueValue}",
            ProjectTaskPriority.Low,
            actor.Id);

        context.ProjectTasks.Add(projectTask);

        await context.SaveChangesAsync();

        var history = new TaskHistory(
            projectTask.Id,
            actor.Id,
            TaskHistoryAction.PriorityChanged,
            "A prioridade da tarefa foi alterada.",
            ProjectTaskPriority.Low.ToString(),
            ProjectTaskPriority.High.ToString());

        context.TaskHistories.Add(history);

        await context.SaveChangesAsync();

        var historyId = history.Id;

        context.ChangeTracker.Clear();

        var persistedHistory =
            await context.TaskHistories.SingleAsync(
                item => item.Id == historyId);

        Assert.Equal(
            projectTask.Id,
            persistedHistory.TaskId);

        Assert.Equal(
            actor.Id,
            persistedHistory.ActorUserId);

        Assert.Equal(
            TaskHistoryAction.PriorityChanged,
            persistedHistory.Action);

        Assert.Equal(
            "A prioridade da tarefa foi alterada.",
            persistedHistory.Reason);

        Assert.Equal(
            ProjectTaskPriority.Low.ToString(),
            persistedHistory.OldValue);

        Assert.Equal(
            ProjectTaskPriority.High.ToString(),
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
            "Criador da Tarefa",
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

        var projectTask = new ProjectTask(
            project.Id,
            $"Tarefa {uniqueValue}",
            ProjectTaskPriority.Medium,
            actor.Id);

        context.ProjectTasks.Add(projectTask);

        await context.SaveChangesAsync();

        var history = new TaskHistory(
            projectTask.Id,
            actor.Id,
            TaskHistoryAction.Created);

        context.TaskHistories.Add(history);

        await context.SaveChangesAsync();

        var historyId = history.Id;

        context.ChangeTracker.Clear();

        var persistedHistory =
            await context.TaskHistories.SingleAsync(
                item => item.Id == historyId);

        Assert.Equal(
            TaskHistoryAction.Created,
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
    SaveChanges_ShouldThrow_WhenTaskDoesNotExist()
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

        var history = new TaskHistory(
            long.MaxValue,
            actor.Id,
            TaskHistoryAction.Created);

        context.TaskHistories.Add(history);

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
            "Criador da Tarefa",
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

        var projectTask = new ProjectTask(
            project.Id,
            $"Tarefa {uniqueValue}",
            ProjectTaskPriority.Medium,
            creator.Id);

        context.ProjectTasks.Add(projectTask);

        await context.SaveChangesAsync();

        var history = new TaskHistory(
            projectTask.Id,
            long.MaxValue,
            TaskHistoryAction.Created);

        context.TaskHistories.Add(history);

        await Assert.ThrowsAsync<DbUpdateException>(
            () => context.SaveChangesAsync());

        await transaction.RollbackAsync();
    }
}