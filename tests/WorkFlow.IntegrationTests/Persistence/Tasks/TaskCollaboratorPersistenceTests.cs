using Microsoft.EntityFrameworkCore;
using WorkFlow.Domain.Entities;
using WorkFlow.Domain.Enums;
using WorkFlow.IntegrationTests.Infrastructure;
using Xunit;

namespace WorkFlow.IntegrationTests.Persistence.Tasks;

[Collection(TestCollectionNames.PostgreSql)]
public sealed class TaskCollaboratorPersistenceTests
{
    private readonly PostgreSqlTestDatabase _database;

    public TaskCollaboratorPersistenceTests(
        PostgreSqlTestDatabase database)
    {
        _database = database;
    }

    [Fact]
    public async Task
        Add_ShouldPersistActiveCollaborator_WhenDataIsValid()
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

        var collaboratorUser = new User(
            tenant.Id,
            "Colaborador da Tarefa",
            $"collaborator-{uniqueValue}@test.local",
            "collaborator-password-hash",
            UserRole.Member);

        context.Users.AddRange(
            creator,
            collaboratorUser);

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

        var collaborator = new TaskCollaborator(
            projectTask.Id,
            collaboratorUser.Id,
            creator.Id);

        context.TaskCollaborators.Add(
            collaborator);

        await context.SaveChangesAsync();

        var collaboratorId = collaborator.Id;

        context.ChangeTracker.Clear();

        var persistedCollaborator =
            await context.TaskCollaborators
                .SingleAsync(
                    item => item.Id == collaboratorId);

        Assert.Equal(
            projectTask.Id,
            persistedCollaborator.TaskId);

        Assert.Equal(
            collaboratorUser.Id,
            persistedCollaborator.UserId);

        Assert.Equal(
            creator.Id,
            persistedCollaborator.AddedByUserId);

        Assert.True(
            persistedCollaborator.IsActive);

        Assert.Null(
            persistedCollaborator.RemovedAt);

        Assert.Null(
            persistedCollaborator.RemovedByUserId);

        Assert.NotEqual(
            default,
            persistedCollaborator.AddedAt);

        await transaction.RollbackAsync();
    }

    [Fact]
    public async Task
    Remove_ShouldPersistRemoval_WhenCollaboratorIsActive()
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

        var collaboratorUser = new User(
            tenant.Id,
            "Colaborador da Tarefa",
            $"collaborator-{uniqueValue}@test.local",
            "collaborator-password-hash",
            UserRole.Member);

        context.Users.AddRange(
            creator,
            collaboratorUser);

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

        var collaborator = new TaskCollaborator(
            projectTask.Id,
            collaboratorUser.Id,
            creator.Id);

        context.TaskCollaborators.Add(
            collaborator);

        await context.SaveChangesAsync();

        var collaboratorId = collaborator.Id;

        collaborator.Remove(creator.Id);

        await context.SaveChangesAsync();

        context.ChangeTracker.Clear();

        var persistedCollaborator =
            await context.TaskCollaborators
                .SingleAsync(
                    item => item.Id == collaboratorId);

        Assert.False(
            persistedCollaborator.IsActive);

        Assert.NotNull(
            persistedCollaborator.RemovedAt);

        Assert.Equal(
            creator.Id,
            persistedCollaborator.RemovedByUserId);

        await transaction.RollbackAsync();
    }

    [Fact]
    public async Task
    SaveChanges_ShouldThrow_WhenActiveCollaboratorIsDuplicated()
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

        var collaboratorUser = new User(
            tenant.Id,
            "Colaborador da Tarefa",
            $"collaborator-{uniqueValue}@test.local",
            "collaborator-password-hash",
            UserRole.Member);

        context.Users.AddRange(
            creator,
            collaboratorUser);

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

        var firstCollaborator = new TaskCollaborator(
            projectTask.Id,
            collaboratorUser.Id,
            creator.Id);

        var secondCollaborator = new TaskCollaborator(
            projectTask.Id,
            collaboratorUser.Id,
            creator.Id);

        context.TaskCollaborators.Add(
            firstCollaborator);

        await context.SaveChangesAsync();

        context.TaskCollaborators.Add(
            secondCollaborator);

        await Assert.ThrowsAsync<DbUpdateException>(
            () => context.SaveChangesAsync());

        await transaction.RollbackAsync();
    }

    [Fact]
    public async Task
    Add_ShouldAllowCollaborator_WhenPreviousCollaborationWasRemoved()
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

        var collaboratorUser = new User(
            tenant.Id,
            "Colaborador da Tarefa",
            $"collaborator-{uniqueValue}@test.local",
            "collaborator-password-hash",
            UserRole.Member);

        context.Users.AddRange(
            creator,
            collaboratorUser);

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

        var firstCollaborator = new TaskCollaborator(
            projectTask.Id,
            collaboratorUser.Id,
            creator.Id);

        context.TaskCollaborators.Add(
            firstCollaborator);

        await context.SaveChangesAsync();

        firstCollaborator.Remove(creator.Id);

        await context.SaveChangesAsync();

        var secondCollaborator = new TaskCollaborator(
            projectTask.Id,
            collaboratorUser.Id,
            creator.Id);

        context.TaskCollaborators.Add(
            secondCollaborator);

        await context.SaveChangesAsync();

        context.ChangeTracker.Clear();

        var persistedCollaborators =
            await context.TaskCollaborators
                .Where(item =>
                    item.TaskId == projectTask.Id &&
                    item.UserId == collaboratorUser.Id)
                .ToListAsync();

        Assert.Equal(
            2,
            persistedCollaborators.Count);

        Assert.Single(
            persistedCollaborators.Where(
                item => item.IsActive));

        Assert.Single(
            persistedCollaborators.Where(
                item => !item.IsActive));

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

        var addedByUser = new User(
            tenant.Id,
            "Usuário que Adicionou",
            $"added-by-{uniqueValue}@test.local",
            "added-by-password-hash",
            UserRole.ProjectManager);

        var collaboratorUser = new User(
            tenant.Id,
            "Colaborador da Tarefa",
            $"collaborator-{uniqueValue}@test.local",
            "collaborator-password-hash",
            UserRole.Member);

        context.Users.AddRange(
            addedByUser,
            collaboratorUser);

        await context.SaveChangesAsync();

        var collaborator = new TaskCollaborator(
            long.MaxValue,
            collaboratorUser.Id,
            addedByUser.Id);

        context.TaskCollaborators.Add(
            collaborator);

        await Assert.ThrowsAsync<DbUpdateException>(
            () => context.SaveChangesAsync());

        await transaction.RollbackAsync();
    }

    [Fact]
    public async Task
    SaveChanges_ShouldThrow_WhenCollaboratorUserDoesNotExist()
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

        var collaborator = new TaskCollaborator(
            projectTask.Id,
            long.MaxValue,
            creator.Id);

        context.TaskCollaborators.Add(
            collaborator);

        await Assert.ThrowsAsync<DbUpdateException>(
            () => context.SaveChangesAsync());

        await transaction.RollbackAsync();
    }

    [Fact]
    public async Task
    SaveChanges_ShouldThrow_WhenAddedByUserDoesNotExist()
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

        var collaboratorUser = new User(
            tenant.Id,
            "Colaborador da Tarefa",
            $"collaborator-{uniqueValue}@test.local",
            "collaborator-password-hash",
            UserRole.ProjectManager);

        context.Users.Add(collaboratorUser);

        await context.SaveChangesAsync();

        var project = new Project(
            tenant.Id,
            $"Projeto {uniqueValue}",
            collaboratorUser.Id);

        context.Projects.Add(project);

        await context.SaveChangesAsync();

        var projectTask = new ProjectTask(
            project.Id,
            $"Tarefa {uniqueValue}",
            ProjectTaskPriority.Medium,
            collaboratorUser.Id);

        context.ProjectTasks.Add(projectTask);

        await context.SaveChangesAsync();

        var collaborator = new TaskCollaborator(
            projectTask.Id,
            collaboratorUser.Id,
            long.MaxValue);

        context.TaskCollaborators.Add(
            collaborator);

        await Assert.ThrowsAsync<DbUpdateException>(
            () => context.SaveChangesAsync());

        await transaction.RollbackAsync();
    }

    [Fact]
    public async Task
    SaveChanges_ShouldThrow_WhenRemovedByUserDoesNotExist()
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

        var collaboratorUser = new User(
            tenant.Id,
            "Colaborador da Tarefa",
            $"collaborator-{uniqueValue}@test.local",
            "collaborator-password-hash",
            UserRole.Member);

        context.Users.AddRange(
            creator,
            collaboratorUser);

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

        var collaborator = new TaskCollaborator(
            projectTask.Id,
            collaboratorUser.Id,
            creator.Id);

        context.TaskCollaborators.Add(
            collaborator);

        await context.SaveChangesAsync();

        collaborator.Remove(long.MaxValue);

        await Assert.ThrowsAsync<DbUpdateException>(
            () => context.SaveChangesAsync());

        await transaction.RollbackAsync();
    }
}