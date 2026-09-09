using WorkFlow.Application.Abstractions.Persistence;
using WorkFlow.Application.Projects;
using WorkFlow.Application.Projects.RevokeProjectMemberPermission;
using WorkFlow.Application.Tenants;
using WorkFlow.Application.Users;
using WorkFlow.Domain.Entities;
using WorkFlow.Domain.Enums;
using WorkFlow.UnitTests.Application.Projects.Fakes;
using WorkFlow.UnitTests.Application.Tenants.Fakes;
using WorkFlow.UnitTests.Application.Users.Fakes;
using WorkFlow.UnitTests.Common;

namespace WorkFlow.UnitTests.Application.Projects.RevokeProjectMemberPermission;

public sealed class RevokeProjectMemberPermissionHandlerTests
{
    [Fact]
    public async Task
    HandleAsync_ShouldRevokePermission_WhenRequesterIsTenantAdmin()
    {
        var fixture =
            CreateFixture(
                UserRole.TenantAdmin);

        var result =
            await fixture.Handler.HandleAsync(
                CreateCommand(fixture));

        Assert.True(result.IsSuccess);
        Assert.False(result.IsFailure);
        Assert.Null(result.Error);

        var response =
            Assert.IsType<RevokeProjectMemberPermissionResult>(
                result.Value);

        Assert.Equal(
            fixture.Project.PublicId,
            response.ProjectPublicId);

        Assert.Equal(
            fixture.TargetUser.PublicId,
            response.UserPublicId);

        Assert.Equal(
            ProjectPermission.EditProject,
            response.Permission);

        Assert.Equal(
            fixture.Requester.PublicId,
            response.RevokedByUserPublicId);

        Assert.NotEqual(
            default,
            response.RevokedAt);

        Assert.False(
            fixture.TargetPermission.IsActive);

        Assert.Equal(
            fixture.Requester.Id,
            fixture.TargetPermission.RevokedByUserId);

        Assert.NotNull(
            fixture.TargetPermission.RevokedAt);

        Assert.Empty(
            fixture.PermissionRepository
                .IsActivePermissionCalls);

        Assert.Contains(
            (
                fixture.TargetProjectMember.Id,
                ProjectPermission.EditProject
            ),
            fixture.PermissionRepository
                .GetActiveForUpdateCalls);

        Assert.Equal(
            1,
            fixture.UnitOfWork.SaveChangesCallCount);
    }

    [Theory]
    [InlineData(UserRole.ProjectManager)]
    [InlineData(UserRole.Member)]
    public async Task
    HandleAsync_ShouldRevokePermission_WhenRequesterCanManagePermissions(
        UserRole requesterRole)
    {
        var fixture =
            CreateFixture(
                requesterRole);

        GrantManagePermissionToRequester(
            fixture);

        var result =
            await fixture.Handler.HandleAsync(
                CreateCommand(fixture));

        Assert.True(
            result.IsSuccess);

        Assert.False(
            fixture.TargetPermission.IsActive);

        Assert.Contains(
            (
                fixture.RequesterProjectMember.Id,
                ProjectPermission.ManageProjectPermissions
            ),
            fixture.PermissionRepository
                .IsActivePermissionCalls);

        Assert.Contains(
            (
                fixture.TargetProjectMember.Id,
                ProjectPermission.EditProject
            ),
            fixture.PermissionRepository
                .GetActiveForUpdateCalls);

        Assert.Equal(
            1,
            fixture.UnitOfWork.SaveChangesCallCount);
    }

    [Theory]
    [InlineData(UserRole.ProjectManager)]
    [InlineData(UserRole.Member)]
    public async Task
    HandleAsync_ShouldReturnFailure_WhenRequesterHasNoActiveMembership(
        UserRole requesterRole)
    {
        var fixture =
            CreateFixture(
                requesterRole);

        var result =
            await fixture.Handler.HandleAsync(
                CreateCommand(fixture));

        Assert.True(
            result.IsFailure);

        Assert.Equal(
            ProjectMemberPermissionErrors.ManageNotAllowed,
            result.Error);

        var membershipCall =
            Assert.Single(
                fixture.ProjectMemberRepository
                    .GetActiveCalls);

        Assert.Equal(
            (fixture.Project.Id, fixture.Requester.Id),
            membershipCall);

        Assert.Empty(
            fixture.PermissionRepository
                .IsActivePermissionCalls);

        Assert.Empty(
            fixture.PermissionRepository
                .GetActiveForUpdateCalls);

        Assert.True(
            fixture.TargetPermission.IsActive);

        Assert.Equal(
            0,
            fixture.UnitOfWork.SaveChangesCallCount);
    }

    [Theory]
    [InlineData(UserRole.ProjectManager)]
    [InlineData(UserRole.Member)]
    public async Task
    HandleAsync_ShouldReturnFailure_WhenRequesterDoesNotHaveManageProjectPermissions(
        UserRole requesterRole)
    {
        var fixture =
            CreateFixture(
                requesterRole);

        AddRequesterMembership(
            fixture);

        fixture.PermissionRepository
            .IsActivePermissionResults[
                (
                    fixture.RequesterProjectMember.Id,
                    ProjectPermission.ManageProjectPermissions
                )] = false;

        var result =
            await fixture.Handler.HandleAsync(
                CreateCommand(fixture));

        Assert.True(
            result.IsFailure);

        Assert.Equal(
            ProjectMemberPermissionErrors.ManageNotAllowed,
            result.Error);

        var permissionCall =
            Assert.Single(
                fixture.PermissionRepository
                    .IsActivePermissionCalls);

        Assert.Equal(
            (
                fixture.RequesterProjectMember.Id,
                ProjectPermission.ManageProjectPermissions
            ),
            permissionCall);

        Assert.Empty(
            fixture.PermissionRepository
                .GetActiveForUpdateCalls);

        Assert.True(
            fixture.TargetPermission.IsActive);

        Assert.Equal(
            0,
            fixture.UnitOfWork.SaveChangesCallCount);
    }

    [Fact]
    public async Task
    HandleAsync_ShouldReturnFailure_WhenProjectIsArchived()
    {
        var fixture =
            CreateFixture(
                UserRole.TenantAdmin);

        fixture.Project.Archive();

        var result =
            await fixture.Handler.HandleAsync(
                CreateCommand(fixture));

        Assert.True(
            result.IsFailure);

        Assert.Equal(
            ProjectErrors.Archived,
            result.Error);

        Assert.Empty(
            fixture.ProjectMemberRepository
                .GetActiveCalls);

        Assert.Empty(
            fixture.PermissionRepository
                .GetActiveForUpdateCalls);

        Assert.True(
            fixture.TargetPermission.IsActive);

        Assert.Equal(
            0,
            fixture.UnitOfWork.SaveChangesCallCount);
    }

    [Fact]
    public async Task
    HandleAsync_ShouldReturnFailure_WhenTargetUserDoesNotExist()
    {
        var fixture =
            CreateFixture(
                UserRole.TenantAdmin);

        fixture.UserRepository
            .UsersByPublicIdToReturn
            .Remove(
                fixture.TargetUser.PublicId);

        var result =
            await fixture.Handler.HandleAsync(
                CreateCommand(fixture));

        Assert.True(
            result.IsFailure);

        Assert.Equal(
            UserErrors.NotFound,
            result.Error);

        Assert.Empty(
            fixture.PermissionRepository
                .GetActiveForUpdateCalls);

        Assert.True(
            fixture.TargetPermission.IsActive);

        Assert.Equal(
            0,
            fixture.UnitOfWork.SaveChangesCallCount);
    }

    [Fact]
    public async Task
    HandleAsync_ShouldAllowRevocation_WhenTargetUserIsInactive()
    {
        var fixture =
            CreateFixture(
                UserRole.TenantAdmin);

        fixture.TargetUser.Deactivate();

        var result =
            await fixture.Handler.HandleAsync(
                CreateCommand(fixture));

        Assert.True(
            result.IsSuccess);

        Assert.False(
            fixture.TargetPermission.IsActive);

        Assert.Equal(
            fixture.Requester.Id,
            fixture.TargetPermission.RevokedByUserId);

        Assert.Equal(
            1,
            fixture.UnitOfWork.SaveChangesCallCount);
    }

    [Fact]
    public async Task
    HandleAsync_ShouldReturnFailure_WhenTargetIsNotActiveProjectMember()
    {
        var fixture =
            CreateFixture(
                UserRole.TenantAdmin);

        fixture.ProjectMemberRepository
            .ActiveMembersToReturn
            .Remove(
                (
                    fixture.Project.Id,
                    fixture.TargetUser.Id
                ));

        var result =
            await fixture.Handler.HandleAsync(
                CreateCommand(fixture));

        Assert.True(
            result.IsFailure);

        Assert.Equal(
            ProjectMemberErrors.NotActive,
            result.Error);

        Assert.Empty(
            fixture.PermissionRepository
                .GetActiveForUpdateCalls);

        Assert.True(
            fixture.TargetPermission.IsActive);

        Assert.Equal(
            0,
            fixture.UnitOfWork.SaveChangesCallCount);
    }

    [Fact]
    public async Task
    HandleAsync_ShouldReturnFailure_WhenPermissionIsNotActive()
    {
        var fixture =
            CreateFixture(
                UserRole.TenantAdmin);

        fixture.PermissionRepository
            .ActivePermissionsForUpdateToReturn
            .Remove(
                (
                    fixture.TargetProjectMember.Id,
                    ProjectPermission.EditProject
                ));

        var result =
            await fixture.Handler.HandleAsync(
                CreateCommand(fixture));

        Assert.True(
            result.IsFailure);

        Assert.Equal(
            ProjectMemberPermissionErrors.NotActive,
            result.Error);

        Assert.Contains(
            (
                fixture.TargetProjectMember.Id,
                ProjectPermission.EditProject
            ),
            fixture.PermissionRepository
                .GetActiveForUpdateCalls);

        Assert.Equal(
            0,
            fixture.UnitOfWork.SaveChangesCallCount);
    }

    [Fact]
    public async Task
    HandleAsync_ShouldAllowMemberToRevokeOwnManageProjectPermissions()
    {
        var fixture =
            CreateFixture(
                UserRole.Member);

        AddRequesterMembership(
            fixture);

        var managePermission =
            new ProjectMemberPermission(
                fixture.RequesterProjectMember.Id,
                ProjectPermission.ManageProjectPermissions,
                fixture.Requester.Id);

        fixture.PermissionRepository
            .IsActivePermissionResults[
                (
                    fixture.RequesterProjectMember.Id,
                    ProjectPermission.ManageProjectPermissions
                )] = true;

        fixture.PermissionRepository
            .ActivePermissionsForUpdateToReturn[
                (
                    fixture.RequesterProjectMember.Id,
                    ProjectPermission.ManageProjectPermissions
                )] =
                    managePermission;

        var command =
            new RevokeProjectMemberPermissionCommand(
                fixture.Tenant.PublicId,
                fixture.Project.PublicId,
                fixture.Requester.PublicId,
                fixture.Requester.PublicId,
                ProjectPermission.ManageProjectPermissions);

        var result =
            await fixture.Handler.HandleAsync(
                command);

        Assert.True(
            result.IsSuccess);

        Assert.False(
            managePermission.IsActive);

        Assert.Equal(
            fixture.Requester.Id,
            managePermission.RevokedByUserId);

        Assert.Equal(
            1,
            fixture.UnitOfWork.SaveChangesCallCount);
    }

    [Fact]
    public async Task
    HandleAsync_ShouldReturnFailure_WhenTenantDoesNotExist()
    {
        var fixture =
            CreateFixture(
                UserRole.TenantAdmin);

        fixture.TenantRepository.TenantToReturn =
            null;

        var result =
            await fixture.Handler.HandleAsync(
                CreateCommand(fixture));

        Assert.True(
            result.IsFailure);

        Assert.Equal(
            TenantErrors.NotFound,
            result.Error);

        Assert.Equal(
            0,
            fixture.UnitOfWork.SaveChangesCallCount);
    }

    [Fact]
    public async Task
    HandleAsync_ShouldReturnFailure_WhenTenantIsInactive()
    {
        var fixture =
            CreateFixture(
                UserRole.TenantAdmin);

        fixture.Tenant.Deactivate();

        var result =
            await fixture.Handler.HandleAsync(
                CreateCommand(fixture));

        Assert.True(
            result.IsFailure);

        Assert.Equal(
            TenantErrors.Inactive,
            result.Error);

        Assert.Equal(
            0,
            fixture.UnitOfWork.SaveChangesCallCount);
    }

    [Fact]
    public async Task
    HandleAsync_ShouldReturnFailure_WhenRequesterDoesNotExist()
    {
        var fixture =
            CreateFixture(
                UserRole.TenantAdmin);

        fixture.UserRepository
            .UsersByPublicIdToReturn
            .Remove(
                fixture.Requester.PublicId);

        var result =
            await fixture.Handler.HandleAsync(
                CreateCommand(fixture));

        Assert.True(
            result.IsFailure);

        Assert.Equal(
            UserErrors.NotFound,
            result.Error);

        Assert.Equal(
            0,
            fixture.UnitOfWork.SaveChangesCallCount);
    }

    [Fact]
    public async Task
    HandleAsync_ShouldReturnFailure_WhenRequesterIsInactive()
    {
        var fixture =
            CreateFixture(
                UserRole.TenantAdmin);

        fixture.Requester.Deactivate();

        var result =
            await fixture.Handler.HandleAsync(
                CreateCommand(fixture));

        Assert.True(
            result.IsFailure);

        Assert.Equal(
            UserErrors.Inactive,
            result.Error);

        Assert.Equal(
            0,
            fixture.UnitOfWork.SaveChangesCallCount);
    }

    [Fact]
    public async Task
    HandleAsync_ShouldReturnFailure_WhenProjectDoesNotExist()
    {
        var fixture =
            CreateFixture(
                UserRole.TenantAdmin);

        fixture.ProjectRepository.ProjectToReturn =
            null;

        var result =
            await fixture.Handler.HandleAsync(
                CreateCommand(fixture));

        Assert.True(
            result.IsFailure);

        Assert.Equal(
            ProjectErrors.NotFound,
            result.Error);

        Assert.Equal(
            0,
            fixture.UnitOfWork.SaveChangesCallCount);
    }

    [Fact]
    public async Task
    HandleAsync_ShouldThrow_WhenPermissionIsInvalid()
    {
        var fixture =
            CreateFixture(
                UserRole.TenantAdmin);

        var command =
            CreateCommand(
                fixture) with
            {
                Permission =
                    (ProjectPermission)999
            };

        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(
            () =>
                fixture.Handler.HandleAsync(
                    command));

        Assert.Equal(
            0,
            fixture.UnitOfWork.SaveChangesCallCount);
    }

    [Fact]
    public async Task
    HandleAsync_ShouldThrow_WhenCommandIsNull()
    {
        var fixture =
            CreateFixture(
                UserRole.TenantAdmin);

        await Assert.ThrowsAsync<ArgumentNullException>(
            () =>
                fixture.Handler.HandleAsync(
                    null!));
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(4)]
    public async Task
    HandleAsync_ShouldThrow_WhenRequiredPublicIdIsEmpty(
        int publicIdToClear)
    {
        var fixture =
            CreateFixture(
                UserRole.TenantAdmin);

        var command =
            new RevokeProjectMemberPermissionCommand(
                publicIdToClear == 1
                    ? Guid.Empty
                    : fixture.Tenant.PublicId,
                publicIdToClear == 2
                    ? Guid.Empty
                    : fixture.Project.PublicId,
                publicIdToClear == 3
                    ? Guid.Empty
                    : fixture.Requester.PublicId,
                publicIdToClear == 4
                    ? Guid.Empty
                    : fixture.TargetUser.PublicId,
                ProjectPermission.EditProject);

        await Assert.ThrowsAsync<ArgumentException>(
            () =>
                fixture.Handler.HandleAsync(
                    command));
    }

    private static Fixture CreateFixture(
        UserRole requesterRole)
    {
        var tenant =
            new Tenant(
                "Empresa Revoke Permission",
                $"REG-{Guid.NewGuid():N}",
                $"tenant-{Guid.NewGuid():N}@test.local");

        EntityTestHelper.SetId(
            tenant,
            42);

        var requester =
            new User(
                tenant.Id,
                "Usuário Solicitante",
                $"requester-{Guid.NewGuid():N}@test.local",
                "password-hash",
                requesterRole);

        EntityTestHelper.SetId(
            requester,
            10);

        var targetUser =
            new User(
                tenant.Id,
                "Usuário Alvo",
                $"target-{Guid.NewGuid():N}@test.local",
                "password-hash",
                UserRole.Member);

        EntityTestHelper.SetId(
            targetUser,
            20);

        var project =
            new Project(
                tenant.Id,
                "Projeto",
                requester.Id);

        EntityTestHelper.SetId(
            project,
            100);

        var requesterProjectMember =
            new ProjectMember(
                project.Id,
                requester.Id,
                requester.Id);

        EntityTestHelper.SetId(
            requesterProjectMember,
            1000);

        var targetProjectMember =
            new ProjectMember(
                project.Id,
                targetUser.Id,
                requester.Id);

        EntityTestHelper.SetId(
            targetProjectMember,
            2000);

        var targetPermission =
            new ProjectMemberPermission(
                targetProjectMember.Id,
                ProjectPermission.EditProject,
                requester.Id);

        var tenantRepository =
            new FakeTenantRepository
            {
                TenantToReturn = tenant
            };

        var userRepository =
            new FakeUserRepository();

        userRepository
            .UsersByPublicIdToReturn[requester.PublicId] =
                requester;

        userRepository
            .UsersByPublicIdToReturn[targetUser.PublicId] =
                targetUser;

        var projectRepository =
            new FakeProjectRepository
            {
                ProjectToReturn = project
            };

        var projectMemberRepository =
            new FakeProjectMemberRepository();

        projectMemberRepository
            .ActiveMembersToReturn[
                (
                    project.Id,
                    targetUser.Id
                )] =
                    targetProjectMember;

        var permissionRepository =
            new FakeProjectMemberPermissionRepository();

        permissionRepository
            .ActivePermissionsForUpdateToReturn[
                (
                    targetProjectMember.Id,
                    ProjectPermission.EditProject
                )] =
                    targetPermission;

        var unitOfWork =
            new TestUnitOfWork();

        var handler =
            new RevokeProjectMemberPermissionHandler(
                tenantRepository,
                userRepository,
                projectRepository,
                projectMemberRepository,
                permissionRepository,
                unitOfWork);

        return new Fixture(
            tenant,
            requester,
            targetUser,
            project,
            requesterProjectMember,
            targetProjectMember,
            targetPermission,
            tenantRepository,
            userRepository,
            projectRepository,
            projectMemberRepository,
            permissionRepository,
            unitOfWork,
            handler);
    }

    private static void AddRequesterMembership(
        Fixture fixture)
    {
        fixture.ProjectMemberRepository
            .ActiveMembersToReturn[
                (
                    fixture.Project.Id,
                    fixture.Requester.Id
                )] =
                    fixture.RequesterProjectMember;
    }

    private static void GrantManagePermissionToRequester(
        Fixture fixture)
    {
        AddRequesterMembership(
            fixture);

        fixture.PermissionRepository
            .IsActivePermissionResults[
                (
                    fixture.RequesterProjectMember.Id,
                    ProjectPermission.ManageProjectPermissions
                )] = true;
    }

    private static RevokeProjectMemberPermissionCommand
        CreateCommand(
            Fixture fixture)
    {
        return new RevokeProjectMemberPermissionCommand(
            fixture.Tenant.PublicId,
            fixture.Project.PublicId,
            fixture.Requester.PublicId,
            fixture.TargetUser.PublicId,
            ProjectPermission.EditProject);
    }

    private sealed class TestUnitOfWork :
        IUnitOfWork
    {
        public int SaveChangesCallCount { get; private set; }

        public Task<IUnitOfWorkTransaction> BeginTransactionAsync(
            CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException(
                "Este teste não utiliza transações.");
        }

        public Task<int> SaveChangesAsync(
            CancellationToken cancellationToken = default)
        {
            SaveChangesCallCount++;

            return Task.FromResult(
                1);
        }
    }

    private sealed record Fixture(
        Tenant Tenant,
        User Requester,
        User TargetUser,
        Project Project,
        ProjectMember RequesterProjectMember,
        ProjectMember TargetProjectMember,
        ProjectMemberPermission TargetPermission,
        FakeTenantRepository TenantRepository,
        FakeUserRepository UserRepository,
        FakeProjectRepository ProjectRepository,
        FakeProjectMemberRepository ProjectMemberRepository,
        FakeProjectMemberPermissionRepository PermissionRepository,
        TestUnitOfWork UnitOfWork,
        RevokeProjectMemberPermissionHandler Handler);
}