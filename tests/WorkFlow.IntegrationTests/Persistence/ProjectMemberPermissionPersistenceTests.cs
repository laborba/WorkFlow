using Microsoft.EntityFrameworkCore;
using WorkFlow.Domain.Entities;
using WorkFlow.Domain.Enums;
using WorkFlow.IntegrationTests.Infrastructure;
using Xunit;

namespace WorkFlow.IntegrationTests.Persistence;

[Collection(TestCollectionNames.PostgreSql)]
public sealed class ProjectMemberPermissionPersistenceTests
{
    private readonly PostgreSqlTestDatabase _database;

    public ProjectMemberPermissionPersistenceTests(
        PostgreSqlTestDatabase database)
    {
        _database = database;
    }

    [Fact]
    public async Task
        Add_ShouldPersistActivePermission_WhenDataIsValid()
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

        var grantor = new User(
            tenant.Id,
            "Usuário que Concedeu",
            $"grantor-{uniqueValue}@test.local",
            "grantor-password-hash",
            UserRole.ProjectManager);

        var memberUser = new User(
            tenant.Id,
            "Membro do Projeto",
            $"member-{uniqueValue}@test.local",
            "member-password-hash",
            UserRole.Member);

        context.Users.AddRange(
            grantor,
            memberUser);

        await context.SaveChangesAsync();

        var project = new Project(
            tenant.Id,
            $"Projeto {uniqueValue}",
            grantor.Id);

        context.Projects.Add(project);

        await context.SaveChangesAsync();

        var projectMember = new ProjectMember(
            project.Id,
            memberUser.Id,
            grantor.Id);

        context.ProjectMembers.Add(projectMember);

        await context.SaveChangesAsync();

        var permission =
            new ProjectMemberPermission(
                projectMember.Id,
                ProjectPermission.EditProject,
                grantor.Id);

        context.ProjectMemberPermissions.Add(
            permission);

        await context.SaveChangesAsync();

        var permissionId = permission.Id;

        context.ChangeTracker.Clear();

        var persistedPermission =
            await context.ProjectMemberPermissions
                .SingleAsync(
                    item => item.Id == permissionId);

        Assert.Equal(
            projectMember.Id,
            persistedPermission.ProjectMemberId);

        Assert.Equal(
            ProjectPermission.EditProject,
            persistedPermission.Permission);

        Assert.Equal(
            grantor.Id,
            persistedPermission.GrantedByUserId);

        Assert.True(
            persistedPermission.IsActive);

        Assert.Null(
            persistedPermission.RevokedAt);

        Assert.Null(
            persistedPermission.RevokedByUserId);

        Assert.NotEqual(
            default,
            persistedPermission.GrantedAt);

        await transaction.RollbackAsync();
    }

    [Fact]
    public async Task
    Revoke_ShouldPersistRevocation_WhenPermissionIsActive()
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

        var grantor = new User(
            tenant.Id,
            "Usuário que Concedeu",
            $"grantor-{uniqueValue}@test.local",
            "grantor-password-hash",
            UserRole.ProjectManager);

        var revoker = new User(
            tenant.Id,
            "Usuário que Revogou",
            $"revoker-{uniqueValue}@test.local",
            "revoker-password-hash",
            UserRole.ProjectManager);

        var memberUser = new User(
            tenant.Id,
            "Membro do Projeto",
            $"member-{uniqueValue}@test.local",
            "member-password-hash",
            UserRole.Member);

        context.Users.AddRange(
            grantor,
            revoker,
            memberUser);

        await context.SaveChangesAsync();

        var project = new Project(
            tenant.Id,
            $"Projeto {uniqueValue}",
            grantor.Id);

        context.Projects.Add(project);

        await context.SaveChangesAsync();

        var projectMember = new ProjectMember(
            project.Id,
            memberUser.Id,
            grantor.Id);

        context.ProjectMembers.Add(projectMember);

        await context.SaveChangesAsync();

        var permission =
            new ProjectMemberPermission(
                projectMember.Id,
                ProjectPermission.EditProject,
                grantor.Id);

        context.ProjectMemberPermissions.Add(
            permission);

        await context.SaveChangesAsync();

        var permissionId = permission.Id;

        permission.Revoke(revoker.Id);

        await context.SaveChangesAsync();

        context.ChangeTracker.Clear();

        var persistedPermission =
            await context.ProjectMemberPermissions
                .SingleAsync(
                    item => item.Id == permissionId);

        Assert.False(
            persistedPermission.IsActive);

        Assert.NotNull(
            persistedPermission.RevokedAt);

        Assert.Equal(
            revoker.Id,
            persistedPermission.RevokedByUserId);

        await transaction.RollbackAsync();
    }

    [Fact]
    public async Task
    SaveChanges_ShouldThrow_WhenActivePermissionIsDuplicated()
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

        var grantor = new User(
            tenant.Id,
            "Usuário que Concedeu",
            $"grantor-{uniqueValue}@test.local",
            "grantor-password-hash",
            UserRole.ProjectManager);

        var memberUser = new User(
            tenant.Id,
            "Membro do Projeto",
            $"member-{uniqueValue}@test.local",
            "member-password-hash",
            UserRole.Member);

        context.Users.AddRange(
            grantor,
            memberUser);

        await context.SaveChangesAsync();

        var project = new Project(
            tenant.Id,
            $"Projeto {uniqueValue}",
            grantor.Id);

        context.Projects.Add(project);

        await context.SaveChangesAsync();

        var projectMember = new ProjectMember(
            project.Id,
            memberUser.Id,
            grantor.Id);

        context.ProjectMembers.Add(projectMember);

        await context.SaveChangesAsync();

        var firstPermission =
            new ProjectMemberPermission(
                projectMember.Id,
                ProjectPermission.EditProject,
                grantor.Id);

        var secondPermission =
            new ProjectMemberPermission(
                projectMember.Id,
                ProjectPermission.EditProject,
                grantor.Id);

        context.ProjectMemberPermissions.Add(
            firstPermission);

        await context.SaveChangesAsync();

        context.ProjectMemberPermissions.Add(
            secondPermission);

        await Assert.ThrowsAsync<DbUpdateException>(
            () => context.SaveChangesAsync());

        await transaction.RollbackAsync();
    }

    [Fact]
    public async Task
    Add_ShouldAllowPermission_WhenPreviousPermissionWasRevoked()
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

        var grantor = new User(
            tenant.Id,
            "Usuário que Concedeu",
            $"grantor-{uniqueValue}@test.local",
            "grantor-password-hash",
            UserRole.ProjectManager);

        var memberUser = new User(
            tenant.Id,
            "Membro do Projeto",
            $"member-{uniqueValue}@test.local",
            "member-password-hash",
            UserRole.Member);

        context.Users.AddRange(
            grantor,
            memberUser);

        await context.SaveChangesAsync();

        var project = new Project(
            tenant.Id,
            $"Projeto {uniqueValue}",
            grantor.Id);

        context.Projects.Add(project);

        await context.SaveChangesAsync();

        var projectMember = new ProjectMember(
            project.Id,
            memberUser.Id,
            grantor.Id);

        context.ProjectMembers.Add(projectMember);

        await context.SaveChangesAsync();

        var firstPermission =
            new ProjectMemberPermission(
                projectMember.Id,
                ProjectPermission.EditProject,
                grantor.Id);

        context.ProjectMemberPermissions.Add(
            firstPermission);

        await context.SaveChangesAsync();

        firstPermission.Revoke(grantor.Id);

        await context.SaveChangesAsync();

        var secondPermission =
            new ProjectMemberPermission(
                projectMember.Id,
                ProjectPermission.EditProject,
                grantor.Id);

        context.ProjectMemberPermissions.Add(
            secondPermission);

        await context.SaveChangesAsync();

        context.ChangeTracker.Clear();

        var persistedPermissions =
            await context.ProjectMemberPermissions
                .Where(item =>
                    item.ProjectMemberId == projectMember.Id &&
                    item.Permission ==
                        ProjectPermission.EditProject)
                .ToListAsync();

        Assert.Equal(
            2,
            persistedPermissions.Count);

        Assert.Single(
            persistedPermissions.Where(
                item => item.IsActive));

        Assert.Single(
            persistedPermissions.Where(
                item => !item.IsActive));

        await transaction.RollbackAsync();
    }

    [Fact]
    public async Task
    SaveChanges_ShouldThrow_WhenProjectMemberDoesNotExist()
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

        var grantor = new User(
            tenant.Id,
            "Usuário que Concedeu",
            $"grantor-{uniqueValue}@test.local",
            "grantor-password-hash",
            UserRole.ProjectManager);

        context.Users.Add(grantor);

        await context.SaveChangesAsync();

        var permission =
            new ProjectMemberPermission(
                long.MaxValue,
                ProjectPermission.EditProject,
                grantor.Id);

        context.ProjectMemberPermissions.Add(
            permission);

        await Assert.ThrowsAsync<DbUpdateException>(
            () => context.SaveChangesAsync());

        await transaction.RollbackAsync();
    }

    [Fact]
    public async Task
    SaveChanges_ShouldThrow_WhenGrantedByUserDoesNotExist()
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

        var memberUser = new User(
            tenant.Id,
            "Membro do Projeto",
            $"member-{uniqueValue}@test.local",
            "member-password-hash",
            UserRole.Member);

        context.Users.AddRange(
            creator,
            memberUser);

        await context.SaveChangesAsync();

        var project = new Project(
            tenant.Id,
            $"Projeto {uniqueValue}",
            creator.Id);

        context.Projects.Add(project);

        await context.SaveChangesAsync();

        var projectMember = new ProjectMember(
            project.Id,
            memberUser.Id,
            creator.Id);

        context.ProjectMembers.Add(projectMember);

        await context.SaveChangesAsync();

        var permission =
            new ProjectMemberPermission(
                projectMember.Id,
                ProjectPermission.EditProject,
                long.MaxValue);

        context.ProjectMemberPermissions.Add(
            permission);

        await Assert.ThrowsAsync<DbUpdateException>(
            () => context.SaveChangesAsync());

        await transaction.RollbackAsync();
    }

    [Fact]
    public async Task
    SaveChanges_ShouldThrow_WhenRevokedByUserDoesNotExist()
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

        var grantor = new User(
            tenant.Id,
            "Usuário que Concedeu",
            $"grantor-{uniqueValue}@test.local",
            "grantor-password-hash",
            UserRole.ProjectManager);

        var memberUser = new User(
            tenant.Id,
            "Membro do Projeto",
            $"member-{uniqueValue}@test.local",
            "member-password-hash",
            UserRole.Member);

        context.Users.AddRange(
            grantor,
            memberUser);

        await context.SaveChangesAsync();

        var project = new Project(
            tenant.Id,
            $"Projeto {uniqueValue}",
            grantor.Id);

        context.Projects.Add(project);

        await context.SaveChangesAsync();

        var projectMember = new ProjectMember(
            project.Id,
            memberUser.Id,
            grantor.Id);

        context.ProjectMembers.Add(projectMember);

        await context.SaveChangesAsync();

        var permission =
            new ProjectMemberPermission(
                projectMember.Id,
                ProjectPermission.EditProject,
                grantor.Id);

        context.ProjectMemberPermissions.Add(
            permission);

        await context.SaveChangesAsync();

        permission.Revoke(long.MaxValue);

        await Assert.ThrowsAsync<DbUpdateException>(
            () => context.SaveChangesAsync());

        await transaction.RollbackAsync();
    }
}