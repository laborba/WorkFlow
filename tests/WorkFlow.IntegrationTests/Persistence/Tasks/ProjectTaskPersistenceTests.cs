using Microsoft.EntityFrameworkCore;
using WorkFlow.Domain.Entities;
using WorkFlow.Domain.Enums;
using WorkFlow.IntegrationTests.Infrastructure;
using Xunit;

namespace WorkFlow.IntegrationTests.Persistence.Tasks;

[Collection(TestCollectionNames.PostgreSql)]
public sealed class ProjectTaskPersistenceTests
{
    private readonly PostgreSqlTestDatabase _database;

    public ProjectTaskPersistenceTests(
        PostgreSqlTestDatabase database)
    {
        _database = database;
    }

    [Fact]
    public async Task
        Add_ShouldPersistProjectTask_WhenDataIsValid()
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

        var responsible = new User(
            tenant.Id,
            "Responsável pela Tarefa",
            $"responsible-{uniqueValue}@test.local",
            "responsible-password-hash",
            UserRole.Member);

        context.Users.AddRange(
            creator,
            responsible);

        await context.SaveChangesAsync();

        var project = new Project(
            tenant.Id,
            $"Projeto {uniqueValue}",
            creator.Id);

        context.Projects.Add(project);

        await context.SaveChangesAsync();

        var dueDate = new DateTime(
            2028,
            12,
            31,
            18,
            0,
            0,
            DateTimeKind.Utc);

        var projectTask = new ProjectTask(
            project.Id,
            $"Tarefa {uniqueValue}",
            ProjectTaskPriority.High,
            creator.Id,
            "Descrição da tarefa",
            responsible.Id,
            dueDate);

        context.ProjectTasks.Add(projectTask);

        await context.SaveChangesAsync();

        var publicId = projectTask.PublicId;

        context.ChangeTracker.Clear();

        var persistedTask =
            await context.ProjectTasks.SingleAsync(
                item => item.PublicId == publicId);

        Assert.Equal(
            project.Id,
            persistedTask.ProjectId);

        Assert.Equal(
            $"Tarefa {uniqueValue}",
            persistedTask.Title);

        Assert.Equal(
            "Descrição da tarefa",
            persistedTask.Description);

        Assert.Equal(
            ProjectTaskStatus.Backlog,
            persistedTask.Status);

        Assert.Equal(
            ProjectTaskPriority.High,
            persistedTask.Priority);

        Assert.Equal(
            responsible.Id,
            persistedTask.ResponsibleUserId);

        Assert.Null(
            persistedTask.ValidatorUserId);

        Assert.Equal(
            dueDate,
            persistedTask.DueDate);

        Assert.Equal(
            creator.Id,
            persistedTask.CreatedByUserId);

        Assert.Null(
            persistedTask.StatusBeforePause);

        Assert.False(
            persistedTask.IsArchived);

        Assert.Null(
            persistedTask.ArchivedAt);

        await transaction.RollbackAsync();
    }

    [Fact]
    public async Task
    Update_ShouldPersistChanges_WhenProjectTaskIsModified()
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

        var responsible = new User(
            tenant.Id,
            "Responsável pela Tarefa",
            $"responsible-{uniqueValue}@test.local",
            "responsible-password-hash",
            UserRole.Member);

        context.Users.AddRange(
            creator,
            responsible);

        await context.SaveChangesAsync();

        var project = new Project(
            tenant.Id,
            $"Projeto {uniqueValue}",
            creator.Id);

        context.Projects.Add(project);

        await context.SaveChangesAsync();

        var projectTask = new ProjectTask(
            project.Id,
            $"Tarefa Original {uniqueValue}",
            ProjectTaskPriority.Low,
            creator.Id);

        context.ProjectTasks.Add(projectTask);

        await context.SaveChangesAsync();

        var publicId = projectTask.PublicId;

        var updatedDueDate = new DateTime(
            2029,
            6,
            30,
            18,
            0,
            0,
            DateTimeKind.Utc);

        projectTask.ChangeTitle(
            $"Tarefa Atualizada {uniqueValue}");

        projectTask.UpdateDescription(
            "Descrição atualizada");

        projectTask.ChangePriority(
            ProjectTaskPriority.Critical);

        projectTask.ChangeDueDate(
            updatedDueDate);

        projectTask.AssignResponsible(
            responsible.Id);

        await context.SaveChangesAsync();

        context.ChangeTracker.Clear();

        var persistedTask =
            await context.ProjectTasks.SingleAsync(
                item => item.PublicId == publicId);

        Assert.Equal(
            $"Tarefa Atualizada {uniqueValue}",
            persistedTask.Title);

        Assert.Equal(
            "Descrição atualizada",
            persistedTask.Description);

        Assert.Equal(
            ProjectTaskPriority.Critical,
            persistedTask.Priority);

        Assert.Equal(
            updatedDueDate,
            persistedTask.DueDate);

        Assert.Equal(
            responsible.Id,
            persistedTask.ResponsibleUserId);

        Assert.NotNull(
            persistedTask.UpdatedAt);

        await transaction.RollbackAsync();
    }

    [Fact]
    public async Task
    PauseAndResume_ShouldPersistStatusBeforePause()
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

        var responsible = new User(
            tenant.Id,
            "Responsável pela Tarefa",
            $"responsible-{uniqueValue}@test.local",
            "responsible-password-hash",
            UserRole.Member);

        context.Users.AddRange(
            creator,
            responsible);

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
            creator.Id,
            responsibleUserId: responsible.Id);

        context.ProjectTasks.Add(projectTask);

        await context.SaveChangesAsync();

        var publicId = projectTask.PublicId;

        projectTask.MoveToTodo();
        projectTask.Start();
        projectTask.Pause("A tarefa precisa aguardar.");

        await context.SaveChangesAsync();

        context.ChangeTracker.Clear();

        var pausedTask =
            await context.ProjectTasks.SingleAsync(
                item => item.PublicId == publicId);

        Assert.Equal(
            ProjectTaskStatus.Paused,
            pausedTask.Status);

        Assert.Equal(
            ProjectTaskStatus.InProgress,
            pausedTask.StatusBeforePause);

        pausedTask.Resume();

        await context.SaveChangesAsync();

        context.ChangeTracker.Clear();

        var resumedTask =
            await context.ProjectTasks.SingleAsync(
                item => item.PublicId == publicId);

        Assert.Equal(
            ProjectTaskStatus.InProgress,
            resumedTask.Status);

        Assert.Null(
            resumedTask.StatusBeforePause);

        await transaction.RollbackAsync();
    }

    [Fact]
    public async Task
    ApproveValidation_ShouldPersistDoneStatusAndClearValidator()
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

        var responsible = new User(
            tenant.Id,
            "Responsável pela Tarefa",
            $"responsible-{uniqueValue}@test.local",
            "responsible-password-hash",
            UserRole.Member);

        var validator = new User(
            tenant.Id,
            "Validador da Tarefa",
            $"validator-{uniqueValue}@test.local",
            "validator-password-hash",
            UserRole.Member);

        context.Users.AddRange(
            creator,
            responsible,
            validator);

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
            ProjectTaskPriority.High,
            creator.Id,
            responsibleUserId: responsible.Id);

        context.ProjectTasks.Add(projectTask);

        await context.SaveChangesAsync();

        var publicId = projectTask.PublicId;

        projectTask.MoveToTodo();
        projectTask.Start();
        projectTask.SendToValidation();
        projectTask.ClaimValidation(validator.Id);

        await context.SaveChangesAsync();

        context.ChangeTracker.Clear();

        var taskInValidation =
            await context.ProjectTasks.SingleAsync(
                item => item.PublicId == publicId);

        Assert.Equal(
            ProjectTaskStatus.Validation,
            taskInValidation.Status);

        Assert.Equal(
            validator.Id,
            taskInValidation.ValidatorUserId);

        taskInValidation.ApproveValidation(
            validator.Id);

        await context.SaveChangesAsync();

        context.ChangeTracker.Clear();

        var approvedTask =
            await context.ProjectTasks.SingleAsync(
                item => item.PublicId == publicId);

        Assert.Equal(
            ProjectTaskStatus.Done,
            approvedTask.Status);

        Assert.Null(
            approvedTask.ValidatorUserId);

        await transaction.RollbackAsync();
    }

    [Fact]
    public async Task
    ArchiveAndRestore_ShouldPersistArchivedAtAndPreserveStatus()
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

        var publicId = projectTask.PublicId;

        projectTask.Cancel();
        projectTask.Archive();

        await context.SaveChangesAsync();

        context.ChangeTracker.Clear();

        var archivedTask =
            await context.ProjectTasks.SingleAsync(
                item => item.PublicId == publicId);

        Assert.Equal(
            ProjectTaskStatus.Cancelled,
            archivedTask.Status);

        Assert.True(
            archivedTask.IsArchived);

        Assert.NotNull(
            archivedTask.ArchivedAt);

        archivedTask.Restore();

        await context.SaveChangesAsync();

        context.ChangeTracker.Clear();

        var restoredTask =
            await context.ProjectTasks.SingleAsync(
                item => item.PublicId == publicId);

        Assert.Equal(
            ProjectTaskStatus.Cancelled,
            restoredTask.Status);

        Assert.False(
            restoredTask.IsArchived);

        Assert.Null(
            restoredTask.ArchivedAt);

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

        var creator = new User(
            tenant.Id,
            "Criador da Tarefa",
            $"creator-{uniqueValue}@test.local",
            "creator-password-hash",
            UserRole.ProjectManager);

        context.Users.Add(creator);

        await context.SaveChangesAsync();

        var projectTask = new ProjectTask(
            long.MaxValue,
            $"Tarefa {uniqueValue}",
            ProjectTaskPriority.Medium,
            creator.Id);

        context.ProjectTasks.Add(projectTask);

        await Assert.ThrowsAsync<DbUpdateException>(
            () => context.SaveChangesAsync());

        await transaction.RollbackAsync();
    }

    [Fact]
    public async Task
    SaveChanges_ShouldThrow_WhenCreatorDoesNotExist()
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

        var projectCreator = new User(
            tenant.Id,
            "Criador do Projeto",
            $"project-creator-{uniqueValue}@test.local",
            "project-creator-password-hash",
            UserRole.ProjectManager);

        context.Users.Add(projectCreator);

        await context.SaveChangesAsync();

        var project = new Project(
            tenant.Id,
            $"Projeto {uniqueValue}",
            projectCreator.Id);

        context.Projects.Add(project);

        await context.SaveChangesAsync();

        var projectTask = new ProjectTask(
            project.Id,
            $"Tarefa {uniqueValue}",
            ProjectTaskPriority.Medium,
            long.MaxValue);

        context.ProjectTasks.Add(projectTask);

        await Assert.ThrowsAsync<DbUpdateException>(
            () => context.SaveChangesAsync());

        await transaction.RollbackAsync();
    }

    [Fact]
    public async Task
    SaveChanges_ShouldThrow_WhenResponsibleUserDoesNotExist()
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
            creator.Id,
            responsibleUserId: long.MaxValue);

        context.ProjectTasks.Add(projectTask);

        await Assert.ThrowsAsync<DbUpdateException>(
            () => context.SaveChangesAsync());

        await transaction.RollbackAsync();
    }

    [Fact]
    public async Task
    SaveChanges_ShouldThrow_WhenValidatorUserDoesNotExist()
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

        var responsible = new User(
            tenant.Id,
            "Responsável pela Tarefa",
            $"responsible-{uniqueValue}@test.local",
            "responsible-password-hash",
            UserRole.Member);

        context.Users.AddRange(
            creator,
            responsible);

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
            ProjectTaskPriority.High,
            creator.Id,
            responsibleUserId: responsible.Id);

        context.ProjectTasks.Add(projectTask);

        await context.SaveChangesAsync();

        projectTask.MoveToTodo();
        projectTask.Start();
        projectTask.SendToValidation();
        projectTask.ClaimValidation(long.MaxValue);

        await Assert.ThrowsAsync<DbUpdateException>(
            () => context.SaveChangesAsync());

        await transaction.RollbackAsync();
    }
}