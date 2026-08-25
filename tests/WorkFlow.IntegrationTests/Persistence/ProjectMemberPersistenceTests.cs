using Microsoft.EntityFrameworkCore;
using WorkFlow.Domain.Entities;
using WorkFlow.Domain.Enums;
using WorkFlow.IntegrationTests.Infrastructure;
using Xunit;

namespace WorkFlow.IntegrationTests.Persistence;

[Collection(TestCollectionNames.PostgreSql)]
public sealed class ProjectMemberPersistenceTests
{
    private readonly PostgreSqlTestDatabase _database;

    public ProjectMemberPersistenceTests(
        PostgreSqlTestDatabase database)
    {
        _database = database;
    }

    [Fact]
    public async Task
        Add_ShouldPersistActiveProjectMember_WhenDataIsValid()
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

        var user = new User(
            tenant.Id,
            "Membro do Projeto",
            $"member-{uniqueValue}@test.local",
            "member-password-hash",
            UserRole.Member);

        context.Users.AddRange(
            creator,
            user);

        await context.SaveChangesAsync();

        var project = new Project(
            tenant.Id,
            $"Projeto {uniqueValue}",
            creator.Id);

        context.Projects.Add(project);

        await context.SaveChangesAsync();

        var projectMember = new ProjectMember(
            project.Id,
            user.Id,
            creator.Id);

        context.ProjectMembers.Add(projectMember);

        await context.SaveChangesAsync();

        var projectMemberId = projectMember.Id;

        context.ChangeTracker.Clear();

        var persistedMember =
            await context.ProjectMembers.SingleAsync(
                item => item.Id == projectMemberId);

        Assert.Equal(
            project.Id,
            persistedMember.ProjectId);

        Assert.Equal(
            user.Id,
            persistedMember.UserId);

        Assert.Equal(
            creator.Id,
            persistedMember.AddedByUserId);

        Assert.True(
            persistedMember.IsActive);

        Assert.Null(
            persistedMember.RemovedAt);

        Assert.NotEqual(
            default,
            persistedMember.AddedAt);

        await transaction.RollbackAsync();
    }

    [Fact]
    public async Task
    Remove_ShouldPersistRemovedAt_WhenMemberIsActive()
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

        var user = new User(
            tenant.Id,
            "Membro do Projeto",
            $"member-{uniqueValue}@test.local",
            "member-password-hash",
            UserRole.Member);

        context.Users.AddRange(
            creator,
            user);

        await context.SaveChangesAsync();

        var project = new Project(
            tenant.Id,
            $"Projeto {uniqueValue}",
            creator.Id);

        context.Projects.Add(project);

        await context.SaveChangesAsync();

        var projectMember = new ProjectMember(
            project.Id,
            user.Id,
            creator.Id);

        context.ProjectMembers.Add(projectMember);

        await context.SaveChangesAsync();

        var projectMemberId = projectMember.Id;

        projectMember.Remove();

        await context.SaveChangesAsync();

        context.ChangeTracker.Clear();

        var persistedMember =
            await context.ProjectMembers.SingleAsync(
                item => item.Id == projectMemberId);

        Assert.False(
            persistedMember.IsActive);

        Assert.NotNull(
            persistedMember.RemovedAt);

        await transaction.RollbackAsync();
    }

    [Fact]
    public async Task
    SaveChanges_ShouldThrow_WhenActiveMemberIsDuplicated()
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

        var user = new User(
            tenant.Id,
            "Membro do Projeto",
            $"member-{uniqueValue}@test.local",
            "member-password-hash",
            UserRole.Member);

        context.Users.AddRange(
            creator,
            user);

        await context.SaveChangesAsync();

        var project = new Project(
            tenant.Id,
            $"Projeto {uniqueValue}",
            creator.Id);

        context.Projects.Add(project);

        await context.SaveChangesAsync();

        var firstMembership = new ProjectMember(
            project.Id,
            user.Id,
            creator.Id);

        var secondMembership = new ProjectMember(
            project.Id,
            user.Id,
            creator.Id);

        context.ProjectMembers.Add(firstMembership);

        await context.SaveChangesAsync();

        context.ProjectMembers.Add(secondMembership);

        await Assert.ThrowsAsync<DbUpdateException>(
            () => context.SaveChangesAsync());

        await transaction.RollbackAsync();
    }

    [Fact]
    public async Task
    Add_ShouldAllowNewMembership_WhenPreviousMembershipWasRemoved()
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

        var user = new User(
            tenant.Id,
            "Membro do Projeto",
            $"member-{uniqueValue}@test.local",
            "member-password-hash",
            UserRole.Member);

        context.Users.AddRange(
            creator,
            user);

        await context.SaveChangesAsync();

        var project = new Project(
            tenant.Id,
            $"Projeto {uniqueValue}",
            creator.Id);

        context.Projects.Add(project);

        await context.SaveChangesAsync();

        var firstMembership = new ProjectMember(
            project.Id,
            user.Id,
            creator.Id);

        context.ProjectMembers.Add(firstMembership);

        await context.SaveChangesAsync();

        firstMembership.Remove();

        await context.SaveChangesAsync();

        var secondMembership = new ProjectMember(
            project.Id,
            user.Id,
            creator.Id);

        context.ProjectMembers.Add(secondMembership);

        await context.SaveChangesAsync();

        context.ChangeTracker.Clear();

        var persistedMemberships =
            await context.ProjectMembers
                .Where(item =>
                    item.ProjectId == project.Id &&
                    item.UserId == user.Id)
                .ToListAsync();

        Assert.Equal(
            2,
            persistedMemberships.Count);

        Assert.Single(
            persistedMemberships.Where(
                item => item.IsActive));

        Assert.Single(
            persistedMemberships.Where(
                item => !item.IsActive));

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

        var addedByUser = new User(
            tenant.Id,
            "Usuário que Adicionou",
            $"added-by-{uniqueValue}@test.local",
            "added-by-password-hash",
            UserRole.ProjectManager);

        var memberUser = new User(
            tenant.Id,
            "Membro do Projeto",
            $"member-{uniqueValue}@test.local",
            "member-password-hash",
            UserRole.Member);

        context.Users.AddRange(
            addedByUser,
            memberUser);

        await context.SaveChangesAsync();

        var projectMember = new ProjectMember(
            long.MaxValue,
            memberUser.Id,
            addedByUser.Id);

        context.ProjectMembers.Add(projectMember);

        await Assert.ThrowsAsync<DbUpdateException>(
            () => context.SaveChangesAsync());

        await transaction.RollbackAsync();
    }

    [Fact]
    public async Task
    SaveChanges_ShouldThrow_WhenMemberUserDoesNotExist()
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

        var projectMember = new ProjectMember(
            project.Id,
            long.MaxValue,
            creator.Id);

        context.ProjectMembers.Add(projectMember);

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

        var memberUser = new User(
            tenant.Id,
            "Membro do Projeto",
            $"member-{uniqueValue}@test.local",
            "member-password-hash",
            UserRole.ProjectManager);

        context.Users.Add(memberUser);

        await context.SaveChangesAsync();

        var project = new Project(
            tenant.Id,
            $"Projeto {uniqueValue}",
            memberUser.Id);

        context.Projects.Add(project);

        await context.SaveChangesAsync();

        var projectMember = new ProjectMember(
            project.Id,
            memberUser.Id,
            long.MaxValue);

        context.ProjectMembers.Add(projectMember);

        await Assert.ThrowsAsync<DbUpdateException>(
            () => context.SaveChangesAsync());

        await transaction.RollbackAsync();
    }
}