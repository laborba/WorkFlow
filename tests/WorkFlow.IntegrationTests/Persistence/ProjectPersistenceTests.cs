using Microsoft.EntityFrameworkCore;
using WorkFlow.Domain.Entities;
using WorkFlow.Domain.Enums;
using WorkFlow.IntegrationTests.Infrastructure;
using Xunit;

namespace WorkFlow.IntegrationTests.Persistence;

[Collection(TestCollectionNames.PostgreSql)]
public sealed class ProjectPersistenceTests
{
    private readonly PostgreSqlTestDatabase _database;

    public ProjectPersistenceTests(
        PostgreSqlTestDatabase database)
    {
        _database = database;
    }

    [Fact]
    public async Task
        Add_ShouldPersistProject_WhenDataIsValid()
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

        var responsible = new User(
            tenant.Id,
            "Responsável pelo Projeto",
            $"responsible-{uniqueValue}@test.local",
            "responsible-password-hash",
            UserRole.Member);

        context.Users.AddRange(
            creator,
            responsible);

        await context.SaveChangesAsync();

        var dueDate =
            DateTime.UtcNow.AddDays(30);

        var project = new Project(
            tenant.Id,
            $"Projeto {uniqueValue}",
            creator.Id,
            "Descrição do projeto",
            responsible.Id,
            dueDate);

        context.Projects.Add(project);

        await context.SaveChangesAsync();

        var publicId = project.PublicId;

        context.ChangeTracker.Clear();

        var persistedProject =
            await context.Projects.SingleAsync(
                item => item.PublicId == publicId);

        Assert.Equal(
            tenant.Id,
            persistedProject.TenantId);

        Assert.Equal(
            $"Projeto {uniqueValue}",
            persistedProject.Name);

        Assert.Equal(
            "Descrição do projeto",
            persistedProject.Description);

        Assert.Equal(
            creator.Id,
            persistedProject.CreatedByUserId);

        Assert.Equal(
            responsible.Id,
            persistedProject.ResponsibleUserId);

        Assert.Equal(
            ProjectStatus.Planning,
            persistedProject.Status);

        Assert.Null(
            persistedProject.StatusBeforeArchive);

        Assert.NotNull(
            persistedProject.DueDate);

        Assert.Equal(
            dueDate,
            persistedProject.DueDate.Value,
            TimeSpan.FromMicroseconds(1));

        Assert.False(
            persistedProject.IsArchived);

        Assert.Null(
            persistedProject.ArchivedAt);

        await transaction.RollbackAsync();
    }

    [Fact]
    public async Task
    Update_ShouldPersistChanges_WhenProjectIsModified()
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

        var responsible = new User(
            tenant.Id,
            "Responsável pelo Projeto",
            $"responsible-{uniqueValue}@test.local",
            "responsible-password-hash",
            UserRole.Member);

        context.Users.AddRange(
            creator,
            responsible);

        await context.SaveChangesAsync();

        var project = new Project(
            tenant.Id,
            $"Projeto Original {uniqueValue}",
            creator.Id,
            "Descrição original",
            responsible.Id);

        context.Projects.Add(project);

        await context.SaveChangesAsync();

        var publicId = project.PublicId;

        var updatedDueDate = new DateTime(
            2027,
            12,
            31,
            18,
            0,
            0,
            DateTimeKind.Utc);

        project.Rename(
            $"Projeto Atualizado {uniqueValue}");

        project.UpdateDescription(
            "Descrição atualizada");

        project.ChangeDueDate(
            updatedDueDate);

        project.RemoveResponsible();
        project.Start();

        await context.SaveChangesAsync();

        context.ChangeTracker.Clear();

        var persistedProject =
            await context.Projects.SingleAsync(
                item => item.PublicId == publicId);

        Assert.Equal(
            $"Projeto Atualizado {uniqueValue}",
            persistedProject.Name);

        Assert.Equal(
            "Descrição atualizada",
            persistedProject.Description);

        Assert.Equal(
            updatedDueDate,
            persistedProject.DueDate);

        Assert.Null(
            persistedProject.ResponsibleUserId);

        Assert.Equal(
            ProjectStatus.InProgress,
            persistedProject.Status);

        Assert.NotNull(
            persistedProject.UpdatedAt);

        await transaction.RollbackAsync();
    }

    [Fact]
    public async Task
    Archive_ShouldPersistPreviousStatusAndArchivedAt()
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

        var publicId = project.PublicId;

        project.Archive();

        await context.SaveChangesAsync();

        context.ChangeTracker.Clear();

        var persistedProject =
            await context.Projects.SingleAsync(
                item => item.PublicId == publicId);

        Assert.Equal(
            ProjectStatus.Archived,
            persistedProject.Status);

        Assert.Equal(
            ProjectStatus.Planning,
            persistedProject.StatusBeforeArchive);

        Assert.True(
            persistedProject.IsArchived);

        Assert.NotNull(
            persistedProject.ArchivedAt);

        Assert.NotNull(
            persistedProject.UpdatedAt);

        await transaction.RollbackAsync();
    }

    [Fact]
    public async Task
    Restore_ShouldPersistPreviousStatus_WhenProjectIsArchived()
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

        var publicId = project.PublicId;

        project.Archive();

        await context.SaveChangesAsync();

        context.ChangeTracker.Clear();

        var archivedProject =
            await context.Projects.SingleAsync(
                item => item.PublicId == publicId);

        archivedProject.Restore();

        await context.SaveChangesAsync();

        context.ChangeTracker.Clear();

        var restoredProject =
            await context.Projects.SingleAsync(
                item => item.PublicId == publicId);

        Assert.Equal(
            ProjectStatus.Planning,
            restoredProject.Status);

        Assert.Null(
            restoredProject.StatusBeforeArchive);

        Assert.Null(
            restoredProject.ArchivedAt);

        Assert.False(
            restoredProject.IsArchived);

        Assert.NotNull(
            restoredProject.UpdatedAt);

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

        var project = new Project(
            tenant.Id,
            $"Projeto {uniqueValue}",
            long.MaxValue);

        context.Projects.Add(project);

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
            "Criador do Projeto",
            $"creator-{uniqueValue}@test.local",
            "creator-password-hash",
            UserRole.ProjectManager);

        context.Users.Add(creator);

        await context.SaveChangesAsync();

        var project = new Project(
            tenant.Id,
            $"Projeto {uniqueValue}",
            creator.Id,
            responsibleUserId: long.MaxValue);

        context.Projects.Add(project);

        await Assert.ThrowsAsync<DbUpdateException>(
            () => context.SaveChangesAsync());

        await transaction.RollbackAsync();
    }

    [Fact]
    public async Task
    SaveChanges_ShouldThrow_WhenTenantDoesNotExist()
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
            long.MaxValue,
            $"Projeto {uniqueValue}",
            creator.Id);

        context.Projects.Add(project);

        await Assert.ThrowsAsync<DbUpdateException>(
            () => context.SaveChangesAsync());

        await transaction.RollbackAsync();
    }

    [Fact]
    public async Task
    PauseAndResume_ShouldPersistStatusAndNewDueDate()
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

        var publicId = project.PublicId;

        project.Start();
        project.Pause("Projeto temporariamente suspenso.");

        await context.SaveChangesAsync();

        context.ChangeTracker.Clear();

        var pausedProject =
            await context.Projects.SingleAsync(
                item => item.PublicId == publicId);

        Assert.Equal(
            ProjectStatus.Paused,
            pausedProject.Status);

        var newDueDate = new DateTime(
            2028,
            6,
            30,
            18,
            0,
            0,
            DateTimeKind.Utc);

        pausedProject.Resume(newDueDate);

        await context.SaveChangesAsync();

        context.ChangeTracker.Clear();

        var resumedProject =
            await context.Projects.SingleAsync(
                item => item.PublicId == publicId);

        Assert.Equal(
            ProjectStatus.InProgress,
            resumedProject.Status);

        Assert.Equal(
            newDueDate,
            resumedProject.DueDate);

        Assert.NotNull(
            resumedProject.UpdatedAt);

        await transaction.RollbackAsync();
    }

    [Fact]
    public async Task
    CompleteAndReopen_ShouldPersistProjectStatus()
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

        var publicId = project.PublicId;

        project.Start();

        project.Complete(
            new[]
            {
            ProjectTaskStatus.Done,
            ProjectTaskStatus.Cancelled
            });

        await context.SaveChangesAsync();

        context.ChangeTracker.Clear();

        var completedProject =
            await context.Projects.SingleAsync(
                item => item.PublicId == publicId);

        Assert.Equal(
            ProjectStatus.Completed,
            completedProject.Status);

        completedProject.Reopen(
            "O projeto precisa de novos ajustes.");

        await context.SaveChangesAsync();

        context.ChangeTracker.Clear();

        var reopenedProject =
            await context.Projects.SingleAsync(
                item => item.PublicId == publicId);

        Assert.Equal(
            ProjectStatus.InProgress,
            reopenedProject.Status);

        Assert.NotNull(
            reopenedProject.UpdatedAt);

        await transaction.RollbackAsync();
    }

    [Fact]
    public async Task
    Create_ShouldPersistCreatorAsActiveProjectMember()
    {
        await using var context =
            _database.CreateDbContext();

        await using var transaction =
            await context.Database.BeginTransactionAsync();

        var uniqueValue =
            Guid.NewGuid().ToString("N");

        var tenant =
            new Tenant(
                $"Tenant {uniqueValue}",
                $"REG-{uniqueValue}",
                $"tenant-{uniqueValue}@test.local");

        context.Tenants.Add(
            tenant);

        await context.SaveChangesAsync();

        var creator =
            new User(
                tenant.Id,
                "Criador do Projeto",
                $"creator-{uniqueValue}@test.local",
                "creator-password-hash",
                UserRole.ProjectManager);

        context.Users.Add(
            creator);

        await context.SaveChangesAsync();

        var project =
            new Project(
                tenant.Id,
                $"Projeto {uniqueValue}",
                creator.Id);

        context.Projects.Add(
            project);

        await context.SaveChangesAsync();

        var projectMember =
            new ProjectMember(
                project.Id,
                creator.Id,
                creator.Id);

        context.ProjectMembers.Add(
            projectMember);

        await context.SaveChangesAsync();

        context.ChangeTracker.Clear();

        var persistedMember =
            await context.ProjectMembers
                .SingleAsync(
                    member =>
                        member.ProjectId == project.Id &&
                        member.UserId == creator.Id);

        Assert.Equal(
            project.Id,
            persistedMember.ProjectId);

        Assert.Equal(
            creator.Id,
            persistedMember.UserId);

        Assert.Equal(
            creator.Id,
            persistedMember.AddedByUserId);

        Assert.True(
            persistedMember.IsActive);

        Assert.Null(
            persistedMember.RemovedAt);

        await transaction.RollbackAsync();
    }
}