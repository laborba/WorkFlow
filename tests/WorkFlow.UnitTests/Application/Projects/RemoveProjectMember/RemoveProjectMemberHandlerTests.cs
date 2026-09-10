using WorkFlow.Application.Abstractions.Persistence;
using WorkFlow.Application.Projects;
using WorkFlow.Application.Projects.RemoveProjectMember;
using WorkFlow.Application.Tenants;
using WorkFlow.Application.Users;
using WorkFlow.Domain.Entities;
using WorkFlow.Domain.Enums;
using WorkFlow.UnitTests.Application.Projects.Fakes;
using WorkFlow.UnitTests.Application.Tenants.Fakes;
using WorkFlow.UnitTests.Application.Users.Fakes;
using WorkFlow.UnitTests.Common;

namespace WorkFlow.UnitTests.Application.Projects.RemoveProjectMember;

public sealed class RemoveProjectMemberHandlerTests
{
    [Fact]
    public async Task
    HandleAsync_ShouldRemoveMember_WhenRequesterIsTenantAdmin()
    {
        var fixture =
            CreateFixture(
                UserRole.TenantAdmin);

        var projectMember =
            new ProjectMember(
                fixture.Project.Id,
                fixture.TargetUser.Id,
                fixture.Requester.Id);

        fixture.ProjectMemberRepository
            .ActiveMemberToReturn =
                projectMember;

        var result =
            await fixture.Handler.HandleAsync(
                CreateCommand(fixture));

        Assert.True(result.IsSuccess);
        Assert.False(result.IsFailure);
        Assert.Null(result.Error);

        var response =
            Assert.IsType<RemoveProjectMemberResult>(
                result.Value);

        Assert.Equal(
            fixture.Project.PublicId,
            response.ProjectPublicId);

        Assert.Equal(
            fixture.TargetUser.PublicId,
            response.UserPublicId);

        Assert.NotNull(
            projectMember.RemovedAt);

        Assert.Equal(
            projectMember.RemovedAt,
            response.RemovedAt);

        Assert.Equal(
            fixture.Project.Id,
            fixture.ProjectMemberRepository
                .LastGetActiveForUpdateProjectId);

        Assert.Equal(
            fixture.TargetUser.Id,
            fixture.ProjectMemberRepository
                .LastGetActiveForUpdateUserId);

        Assert.Empty(
            fixture.ProjectMemberRepository
                .GetActiveCalls);

        Assert.Empty(
            fixture.PermissionRepository
                .IsActivePermissionCalls);

        Assert.Equal(
            1,
            fixture.UnitOfWork.SaveChangesCallCount);
    }

    [Theory]
    [InlineData(UserRole.ProjectManager)]
    [InlineData(UserRole.Member)]
    public async Task
    HandleAsync_ShouldRemoveMember_WhenRequesterHasManageProjectMembersPermission(
        UserRole requesterRole)
    {
        var fixture =
            CreateFixture(
                requesterRole);

        GrantManageProjectMembersPermission(
            fixture);

        var projectMember =
            new ProjectMember(
                fixture.Project.Id,
                fixture.TargetUser.Id,
                fixture.Requester.Id);

        fixture.ProjectMemberRepository
            .ActiveMemberToReturn =
                projectMember;

        var result =
            await fixture.Handler.HandleAsync(
                CreateCommand(fixture));

        Assert.True(result.IsSuccess);
        Assert.False(result.IsFailure);
        Assert.Null(result.Error);

        var response =
            Assert.IsType<RemoveProjectMemberResult>(
                result.Value);

        Assert.Equal(
            fixture.Project.PublicId,
            response.ProjectPublicId);

        Assert.Equal(
            fixture.TargetUser.PublicId,
            response.UserPublicId);

        Assert.NotNull(
            projectMember.RemovedAt);

        var requesterMembershipCall =
            Assert.Single(
                fixture.ProjectMemberRepository
                    .GetActiveCalls);

        Assert.Equal(
            (
                fixture.Project.Id,
                fixture.Requester.Id
            ),
            requesterMembershipCall);

        var permissionCall =
            Assert.Single(
                fixture.PermissionRepository
                    .IsActivePermissionCalls);

        Assert.Equal(
            (
                fixture.RequesterProjectMember.Id,
                ProjectPermission.ManageProjectMembers
            ),
            permissionCall);

        Assert.Equal(
            fixture.Project.Id,
            fixture.ProjectMemberRepository
                .LastGetActiveForUpdateProjectId);

        Assert.Equal(
            fixture.TargetUser.Id,
            fixture.ProjectMemberRepository
                .LastGetActiveForUpdateUserId);

        Assert.Equal(
            1,
            fixture.UnitOfWork.SaveChangesCallCount);
    }

    [Fact]
    public async Task
    HandleAsync_ShouldReturnFailure_WhenProjectManagerIsNotActiveMember()
    {
        var fixture =
            CreateFixture(
                UserRole.ProjectManager);

        fixture.ProjectMemberRepository
            .ActiveMembersToReturn
            .Remove(
                (
                    fixture.Project.Id,
                    fixture.Requester.Id
                ));

        var result =
            await fixture.Handler.HandleAsync(
                CreateCommand(fixture));

        Assert.True(result.IsFailure);
        Assert.False(result.IsSuccess);
        Assert.Null(result.Value);

        Assert.Equal(
            ProjectMemberErrors.RemoveNotAllowed,
            result.Error);

        var membershipCall =
            Assert.Single(
                fixture.ProjectMemberRepository
                    .GetActiveCalls);

        Assert.Equal(
            (
                fixture.Project.Id,
                fixture.Requester.Id
            ),
            membershipCall);

        Assert.Empty(
            fixture.PermissionRepository
                .IsActivePermissionCalls);

        Assert.Single(
            fixture.UserRepository
                .CheckedGetByPublicIdCalls);

        Assert.Null(
            fixture.ProjectMemberRepository
                .LastGetActiveForUpdateProjectId);

        Assert.Equal(
            0,
            fixture.UnitOfWork.SaveChangesCallCount);
    }

    [Theory]
    [InlineData(UserRole.ProjectManager)]
    [InlineData(UserRole.Member)]
    public async Task
    HandleAsync_ShouldReturnFailure_WhenRequesterDoesNotHaveManageProjectMembersPermission(
        UserRole requesterRole)
    {
        var fixture =
            CreateFixture(
                requesterRole);

        var result =
            await fixture.Handler.HandleAsync(
                CreateCommand(fixture));

        Assert.True(result.IsFailure);
        Assert.False(result.IsSuccess);
        Assert.Null(result.Value);

        Assert.Equal(
            ProjectMemberErrors.RemoveNotAllowed,
            result.Error);

        var membershipCall =
            Assert.Single(
                fixture.ProjectMemberRepository
                    .GetActiveCalls);

        Assert.Equal(
            (
                fixture.Project.Id,
                fixture.Requester.Id
            ),
            membershipCall);

        var permissionCall =
            Assert.Single(
                fixture.PermissionRepository
                    .IsActivePermissionCalls);

        Assert.Equal(
            (
                fixture.RequesterProjectMember.Id,
                ProjectPermission.ManageProjectMembers
            ),
            permissionCall);

        Assert.Single(
            fixture.UserRepository
                .CheckedGetByPublicIdCalls);

        Assert.Null(
            fixture.ProjectMemberRepository
                .LastGetActiveForUpdateProjectId);

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

        Assert.True(result.IsFailure);
        Assert.False(result.IsSuccess);
        Assert.Null(result.Value);

        Assert.Equal(
            ProjectErrors.Archived,
            result.Error);

        Assert.Empty(
            fixture.ProjectMemberRepository
                .GetActiveCalls);

        Assert.Empty(
            fixture.PermissionRepository
                .IsActivePermissionCalls);

        Assert.Single(
            fixture.UserRepository
                .CheckedGetByPublicIdCalls);

        Assert.Null(
            fixture.ProjectMemberRepository
                .LastGetActiveForUpdateProjectId);

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

        Assert.True(result.IsFailure);
        Assert.False(result.IsSuccess);
        Assert.Null(result.Value);

        Assert.Equal(
            UserErrors.NotFound,
            result.Error);

        Assert.Null(
            fixture.ProjectMemberRepository
                .LastGetActiveForUpdateProjectId);

        Assert.Equal(
            0,
            fixture.UnitOfWork.SaveChangesCallCount);
    }

    [Fact]
    public async Task
    HandleAsync_ShouldAllowRemovingInactiveTargetUser()
    {
        var fixture =
            CreateFixture(
                UserRole.TenantAdmin);

        fixture.TargetUser.Deactivate();

        var projectMember =
            new ProjectMember(
                fixture.Project.Id,
                fixture.TargetUser.Id,
                fixture.Requester.Id);

        fixture.ProjectMemberRepository
            .ActiveMemberToReturn =
                projectMember;

        var result =
            await fixture.Handler.HandleAsync(
                CreateCommand(fixture));

        Assert.True(result.IsSuccess);
        Assert.False(result.IsFailure);
        Assert.Null(result.Error);

        Assert.NotNull(
            projectMember.RemovedAt);

        Assert.Equal(
            1,
            fixture.UnitOfWork.SaveChangesCallCount);
    }

    [Fact]
    public async Task
    HandleAsync_ShouldReturnFailure_WhenTargetHasNoActiveMembership()
    {
        var fixture =
            CreateFixture(
                UserRole.TenantAdmin);

        fixture.ProjectMemberRepository
            .ActiveMemberToReturn =
                null;

        var result =
            await fixture.Handler.HandleAsync(
                CreateCommand(fixture));

        Assert.True(result.IsFailure);
        Assert.False(result.IsSuccess);
        Assert.Null(result.Value);

        Assert.Equal(
            ProjectMemberErrors.NotActive,
            result.Error);

        Assert.Equal(
            fixture.Project.Id,
            fixture.ProjectMemberRepository
                .LastGetActiveForUpdateProjectId);

        Assert.Equal(
            fixture.TargetUser.Id,
            fixture.ProjectMemberRepository
                .LastGetActiveForUpdateUserId);

        Assert.Equal(
            0,
            fixture.UnitOfWork.SaveChangesCallCount);
    }

    [Fact]
    public async Task
    HandleAsync_ShouldReturnFailure_WhenTenantDoesNotExist()
    {
        var fixture =
            CreateFixture(
                UserRole.TenantAdmin);

        fixture.TenantRepository
            .TenantToReturn =
                null;

        var result =
            await fixture.Handler.HandleAsync(
                CreateCommand(fixture));

        Assert.True(result.IsFailure);
        Assert.False(result.IsSuccess);
        Assert.Null(result.Value);

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

        Assert.True(result.IsFailure);
        Assert.False(result.IsSuccess);
        Assert.Null(result.Value);

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

        Assert.True(result.IsFailure);
        Assert.False(result.IsSuccess);
        Assert.Null(result.Value);

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

        Assert.True(result.IsFailure);
        Assert.False(result.IsSuccess);
        Assert.Null(result.Value);

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

        fixture.ProjectRepository
            .ProjectToReturn =
                null;

        var result =
            await fixture.Handler.HandleAsync(
                CreateCommand(fixture));

        Assert.True(result.IsFailure);
        Assert.False(result.IsSuccess);
        Assert.Null(result.Value);

        Assert.Equal(
            ProjectErrors.NotFound,
            result.Error);

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

        Assert.Equal(
            0,
            fixture.UnitOfWork.SaveChangesCallCount);
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
            new RemoveProjectMemberCommand(
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
                    : fixture.TargetUser.PublicId);

        await Assert.ThrowsAsync<ArgumentException>(
            () =>
                fixture.Handler.HandleAsync(
                    command));

        Assert.Equal(
            0,
            fixture.UnitOfWork.SaveChangesCallCount);
    }

    private static Fixture CreateFixture(
        UserRole requesterRole)
    {
        var tenant =
            new Tenant(
                "Empresa Remove Project Member",
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

        var tenantRepository =
            new FakeTenantRepository
            {
                TenantToReturn = tenant
            };

        var userRepository =
            new FakeUserRepository();

        userRepository
            .UsersByPublicIdToReturn[
                requester.PublicId] =
                    requester;

        userRepository
            .UsersByPublicIdToReturn[
                targetUser.PublicId] =
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
                    requester.Id
                )] =
                    requesterProjectMember;

        var permissionRepository =
            new FakeProjectMemberPermissionRepository();

        var unitOfWork =
            new TestUnitOfWork();

        var handler =
            new RemoveProjectMemberHandler(
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
            tenantRepository,
            userRepository,
            projectRepository,
            projectMemberRepository,
            permissionRepository,
            unitOfWork,
            handler);
    }

    private static void GrantManageProjectMembersPermission(
        Fixture fixture)
    {
        fixture.PermissionRepository
            .IsActivePermissionResults[
                (
                    fixture.RequesterProjectMember.Id,
                    ProjectPermission.ManageProjectMembers
                )] =
                    true;
    }

    private static RemoveProjectMemberCommand CreateCommand(
        Fixture fixture)
    {
        return new RemoveProjectMemberCommand(
            fixture.Tenant.PublicId,
            fixture.Project.PublicId,
            fixture.Requester.PublicId,
            fixture.TargetUser.PublicId);
    }

    private sealed class TestUnitOfWork :
        IUnitOfWork
    {
        public int SaveChangesCallCount
        { get; private set; }

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
        FakeTenantRepository TenantRepository,
        FakeUserRepository UserRepository,
        FakeProjectRepository ProjectRepository,
        FakeProjectMemberRepository ProjectMemberRepository,
        FakeProjectMemberPermissionRepository PermissionRepository,
        TestUnitOfWork UnitOfWork,
        RemoveProjectMemberHandler Handler);
}