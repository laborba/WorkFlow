using Microsoft.EntityFrameworkCore;
using WorkFlow.Domain.Entities;
using WorkFlow.Domain.Enums;
using WorkFlow.Infrastructure.Persistence.Repositories;
using WorkFlow.IntegrationTests.Infrastructure;
using Xunit;

namespace WorkFlow.IntegrationTests.Persistence.Projects;

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

    [Fact]
    public async Task
Repository_ShouldAddAndFindActivePermission()
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

        context.Tenants.Add(
            tenant);

        await context.SaveChangesAsync();

        var grantor = new User(
            tenant.Id,
            "Usuário que Concedeu",
            $"grantor-{uniqueValue}@test.local",
            "grantor-password-hash",
            UserRole.TenantAdmin);

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

        context.Projects.Add(
            project);

        await context.SaveChangesAsync();

        var projectMember = new ProjectMember(
            project.Id,
            memberUser.Id,
            grantor.Id);

        context.ProjectMembers.Add(
            projectMember);

        await context.SaveChangesAsync();

        var repository =
            new ProjectMemberPermissionRepository(
                context);

        var permission =
            new ProjectMemberPermission(
                projectMember.Id,
                ProjectPermission.EditProject,
                grantor.Id);

        await repository.AddAsync(
            permission);

        await context.SaveChangesAsync();

        var isActive =
            await repository.IsActivePermissionAsync(
                projectMember.Id,
                ProjectPermission.EditProject);

        Assert.True(
            isActive);

        Assert.NotEqual(
            0,
            permission.Id);

        await transaction.RollbackAsync();
    }

    [Fact]
    public async Task
    Repository_ShouldReturnFalse_WhenPermissionWasRevoked()
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

        context.Tenants.Add(
            tenant);

        await context.SaveChangesAsync();

        var grantor = new User(
            tenant.Id,
            "Usuário que Concedeu",
            $"grantor-{uniqueValue}@test.local",
            "grantor-password-hash",
            UserRole.TenantAdmin);

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

        context.Projects.Add(
            project);

        await context.SaveChangesAsync();

        var projectMember = new ProjectMember(
            project.Id,
            memberUser.Id,
            grantor.Id);

        context.ProjectMembers.Add(
            projectMember);

        await context.SaveChangesAsync();

        var repository =
            new ProjectMemberPermissionRepository(
                context);

        var permission =
            new ProjectMemberPermission(
                projectMember.Id,
                ProjectPermission.ManageProjectMembers,
                grantor.Id);

        await repository.AddAsync(
            permission);

        await context.SaveChangesAsync();

        permission.Revoke(
            grantor.Id);

        await context.SaveChangesAsync();

        var isActive =
            await repository.IsActivePermissionAsync(
                projectMember.Id,
                ProjectPermission.ManageProjectMembers);

        Assert.False(
            isActive);

        await transaction.RollbackAsync();
    }

    [Fact]
    public async Task
    Repository_GetActivePermissionsAsync_ShouldReturnOnlyActivePermissionsWithGrantorPublicId()
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

        var grantor =
            new User(
                tenant.Id,
                "Usuário que Concedeu",
                $"grantor-{uniqueValue}@test.local",
                "grantor-password-hash",
                UserRole.TenantAdmin);

        var memberUser =
            new User(
                tenant.Id,
                "Membro",
                $"member-{uniqueValue}@test.local",
                "member-password-hash",
                UserRole.Member);

        context.Users.AddRange(
            grantor,
            memberUser);

        await context.SaveChangesAsync();

        var project =
            new Project(
                tenant.Id,
                $"Projeto {uniqueValue}",
                grantor.Id);

        context.Projects.Add(
            project);

        await context.SaveChangesAsync();

        var projectMember =
            new ProjectMember(
                project.Id,
                memberUser.Id,
                grantor.Id);

        context.ProjectMembers.Add(
            projectMember);

        await context.SaveChangesAsync();

        var createTaskPermission =
            new ProjectMemberPermission(
                projectMember.Id,
                ProjectPermission.CreateTask,
                grantor.Id);

        var editProjectPermission =
            new ProjectMemberPermission(
                projectMember.Id,
                ProjectPermission.EditProject,
                grantor.Id);

        var revokedPermission =
            new ProjectMemberPermission(
                projectMember.Id,
                ProjectPermission.ManageProjectMembers,
                grantor.Id);

        revokedPermission.Revoke(
            grantor.Id);

        context.ProjectMemberPermissions.AddRange(
            createTaskPermission,
            editProjectPermission,
            revokedPermission);

        await context.SaveChangesAsync();

        var repository =
            new ProjectMemberPermissionRepository(
                context);

        var result =
            await repository.GetActivePermissionsAsync(
                projectMember.Id);

        Assert.Equal(
            2,
            result.Count);

        Assert.DoesNotContain(
            result,
            item =>
                item.Permission ==
                    ProjectPermission.ManageProjectMembers);

        Assert.All(
            result,
            item =>
                Assert.Equal(
                    grantor.PublicId,
                    item.GrantedByUserPublicId));

        var orderedPermissions =
            result
                .Select(item =>
                    item.Permission)
                .ToArray();

        Assert.Equal(
            new[]
            {
            ProjectPermission.EditProject,
            ProjectPermission.CreateTask
            },
            orderedPermissions);

        await transaction.RollbackAsync();
    }

    [Fact]
    public async Task
Repository_GetActiveForUpdateAsync_ShouldReturnTrackedPermissionAndStopReturningItAfterRevocation()
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

        var grantor =
            new User(
                tenant.Id,
                "Usuário que Concedeu",
                $"grantor-{uniqueValue}@test.local",
                "grantor-password-hash",
                UserRole.TenantAdmin);

        var memberUser =
            new User(
                tenant.Id,
                "Membro",
                $"member-{uniqueValue}@test.local",
                "member-password-hash",
                UserRole.Member);

        context.Users.AddRange(
            grantor,
            memberUser);

        await context.SaveChangesAsync();

        var project =
            new Project(
                tenant.Id,
                $"Projeto {uniqueValue}",
                grantor.Id);

        context.Projects.Add(
            project);

        await context.SaveChangesAsync();

        var projectMember =
            new ProjectMember(
                project.Id,
                memberUser.Id,
                grantor.Id);

        context.ProjectMembers.Add(
            projectMember);

        await context.SaveChangesAsync();

        var permission =
            new ProjectMemberPermission(
                projectMember.Id,
                ProjectPermission.EditTask,
                grantor.Id);

        context.ProjectMemberPermissions.Add(
            permission);

        await context.SaveChangesAsync();

        context.ChangeTracker.Clear();

        var repository =
            new ProjectMemberPermissionRepository(
                context);

        var activePermission =
            await repository.GetActiveForUpdateAsync(
                projectMember.Id,
                ProjectPermission.EditTask);

        Assert.NotNull(
            activePermission);

        Assert.True(
            activePermission.IsActive);

        Assert.Equal(
            EntityState.Unchanged,
            context.Entry(activePermission).State);

        activePermission.Revoke(
            grantor.Id);

        Assert.Equal(
            EntityState.Modified,
            context.Entry(activePermission).State);

        await context.SaveChangesAsync();

        context.ChangeTracker.Clear();

        var activePermissionAfterRevocation =
            await repository.GetActiveForUpdateAsync(
                projectMember.Id,
                ProjectPermission.EditTask);

        Assert.Null(
            activePermissionAfterRevocation);

        var persistedPermission =
            await context.ProjectMemberPermissions
                .SingleAsync(
                    item =>
                        item.ProjectMemberId ==
                            projectMember.Id &&
                        item.Permission ==
                            ProjectPermission.EditTask);

        Assert.False(
            persistedPermission.IsActive);

        Assert.NotNull(
            persistedPermission.RevokedAt);

        Assert.Equal(
            grantor.Id,
            persistedPermission.RevokedByUserId);

        await transaction.RollbackAsync();
    }

    [Fact]
    public async Task
Repository_ShouldPersistDefaultProjectManagerPermissionsTogether()
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

        var projectManager =
            new User(
                tenant.Id,
                "Gerente do Projeto",
                $"manager-{uniqueValue}@test.local",
                "manager-password-hash",
                UserRole.ProjectManager);

        context.Users.Add(
            projectManager);

        await context.SaveChangesAsync();

        var project =
            new Project(
                tenant.Id,
                $"Projeto {uniqueValue}",
                projectManager.Id);

        context.Projects.Add(
            project);

        await context.SaveChangesAsync();

        var projectMember =
            new ProjectMember(
                project.Id,
                projectManager.Id,
                projectManager.Id);

        context.ProjectMembers.Add(
            projectMember);

        await context.SaveChangesAsync();

        var repository =
            new ProjectMemberPermissionRepository(
                context);

        var editProject =
            new ProjectMemberPermission(
                projectMember.Id,
                ProjectPermission.EditProject,
                projectManager.Id);

        var manageProjectMembers =
            new ProjectMemberPermission(
                projectMember.Id,
                ProjectPermission.ManageProjectMembers,
                projectManager.Id);

        var manageProjectPermissions =
            new ProjectMemberPermission(
                projectMember.Id,
                ProjectPermission.ManageProjectPermissions,
                projectManager.Id);

        await repository.AddAsync(
            editProject);

        await repository.AddAsync(
            manageProjectMembers);

        await repository.AddAsync(
            manageProjectPermissions);

        await context.SaveChangesAsync();

        context.ChangeTracker.Clear();

        var permissions =
            await repository.GetActivePermissionsAsync(
                projectMember.Id);

        Assert.Equal(
            3,
            permissions.Count);

        Assert.Equal(
            new[]
            {
            ProjectPermission.EditProject,
            ProjectPermission.ManageProjectMembers,
            ProjectPermission.ManageProjectPermissions
            },
            permissions
                .Select(permission =>
                    permission.Permission)
                .ToArray());

        Assert.All(
            permissions,
            permission =>
                Assert.Equal(
                    projectManager.PublicId,
                    permission.GrantedByUserPublicId));

        Assert.True(
            await repository.IsActivePermissionAsync(
                projectMember.Id,
                ProjectPermission.EditProject));

        Assert.True(
            await repository.IsActivePermissionAsync(
                projectMember.Id,
                ProjectPermission.ManageProjectMembers));

        Assert.True(
            await repository.IsActivePermissionAsync(
                projectMember.Id,
                ProjectPermission.ManageProjectPermissions));

        await transaction.RollbackAsync();
    }
}