using WorkFlow.Application.Projects;
using WorkFlow.Application.Projects.AddProjectMember;
using WorkFlow.Application.Tenants;
using WorkFlow.Application.Users;
using WorkFlow.Domain.Entities;
using WorkFlow.Domain.Enums;
using WorkFlow.UnitTests.Application.Projects.Fakes;
using WorkFlow.UnitTests.Application.Tenants.Fakes;
using WorkFlow.UnitTests.Application.Users.Fakes;
using WorkFlow.UnitTests.Common;

namespace WorkFlow.UnitTests.Application.Projects.AddProjectMember;

public sealed class AddProjectMemberHandlerTests
{
    [Fact]
    public async Task
    HandleAsync_ShouldAddMember_WhenRequesterIsTenantAdmin()
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
            Assert.IsType<AddProjectMemberResult>(
                result.Value);

        var addedMember =
            Assert.IsType<ProjectMember>(
                fixture.ProjectMemberRepository
                    .AddedProjectMember);

        Assert.Equal(
            fixture.Project.Id,
            addedMember.ProjectId);

        Assert.Equal(
            fixture.UserToAdd.Id,
            addedMember.UserId);

        Assert.Equal(
            fixture.Requester.Id,
            addedMember.AddedByUserId);

        Assert.True(
            addedMember.IsActive);

        Assert.Null(
            addedMember.RemovedAt);

        Assert.Equal(
            fixture.Project.PublicId,
            response.ProjectPublicId);

        Assert.Equal(
            fixture.UserToAdd.PublicId,
            response.UserPublicId);

        Assert.Equal(
            fixture.Requester.PublicId,
            response.AddedByUserPublicId);

        Assert.Equal(
            addedMember.AddedAt,
            response.AddedAt);

        Assert.Equal(
            1,
            fixture.UnitOfWork.SaveChangesCallCount);

        var membershipCall =
            Assert.Single(
                fixture.ProjectMemberRepository
                    .IsActiveMemberCalls);

        Assert.Equal(
            (
                fixture.Project.Id,
                fixture.UserToAdd.Id
            ),
            membershipCall);

        Assert.Empty(
            fixture.ProjectMemberRepository
                .GetActiveCalls);

        Assert.Empty(
            fixture.PermissionRepository
                .IsActivePermissionCalls);
    }

    [Theory]
    [InlineData(UserRole.ProjectManager)]
    [InlineData(UserRole.Member)]
    public async Task
    HandleAsync_ShouldAddMember_WhenRequesterHasManageProjectMembersPermission(
        UserRole requesterRole)
    {
        var fixture =
            CreateFixture(
                requesterRole);

        GrantManageProjectMembersPermission(
            fixture);

        var result =
            await fixture.Handler.HandleAsync(
                CreateCommand(fixture));

        Assert.True(result.IsSuccess);
        Assert.False(result.IsFailure);
        Assert.Null(result.Error);

        var addedMember =
            Assert.IsType<ProjectMember>(
                fixture.ProjectMemberRepository
                    .AddedProjectMember);

        Assert.Equal(
            fixture.Project.Id,
            addedMember.ProjectId);

        Assert.Equal(
            fixture.UserToAdd.Id,
            addedMember.UserId);

        Assert.Equal(
            fixture.Requester.Id,
            addedMember.AddedByUserId);

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

        var targetMembershipCall =
            Assert.Single(
                fixture.ProjectMemberRepository
                    .IsActiveMemberCalls);

        Assert.Equal(
            (
                fixture.Project.Id,
                fixture.UserToAdd.Id
            ),
            targetMembershipCall);

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
            ProjectMemberErrors.AddNotAllowed,
            result.Error);

        Assert.Null(
            fixture.ProjectMemberRepository
                .AddedProjectMember);

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

        Assert.Empty(
            fixture.ProjectMemberRepository
                .IsActiveMemberCalls);

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
            ProjectMemberErrors.AddNotAllowed,
            result.Error);

        Assert.Null(
            fixture.ProjectMemberRepository
                .AddedProjectMember);

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

        Assert.Empty(
            fixture.ProjectMemberRepository
                .IsActiveMemberCalls);

        Assert.Single(
            fixture.UserRepository
                .CheckedGetByPublicIdCalls);

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

        Assert.Empty(
            fixture.ProjectMemberRepository
                .IsActiveMemberCalls);

        Assert.Single(
            fixture.UserRepository
                .CheckedGetByPublicIdCalls);

        Assert.Null(
            fixture.ProjectMemberRepository
                .AddedProjectMember);

        Assert.Equal(
            0,
            fixture.UnitOfWork.SaveChangesCallCount);
    }

    [Fact]
    public async Task
    HandleAsync_ShouldReturnFailure_WhenUserToAddDoesNotExist()
    {
        var fixture =
            CreateFixture(
                UserRole.TenantAdmin);

        fixture.UserRepository
            .UsersByPublicIdToReturn
            .Remove(
                fixture.UserToAdd.PublicId);

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
            2,
            fixture.UserRepository
                .CheckedGetByPublicIdCalls.Count);

        Assert.Empty(
            fixture.ProjectMemberRepository
                .GetActiveCalls);

        Assert.Empty(
            fixture.PermissionRepository
                .IsActivePermissionCalls);

        Assert.Empty(
            fixture.ProjectMemberRepository
                .IsActiveMemberCalls);

        Assert.Null(
            fixture.ProjectMemberRepository
                .AddedProjectMember);

        Assert.Equal(
            0,
            fixture.UnitOfWork.SaveChangesCallCount);
    }

    [Fact]
    public async Task
    HandleAsync_ShouldReturnFailure_WhenUserToAddIsInactive()
    {
        var fixture =
            CreateFixture(
                UserRole.TenantAdmin);

        fixture.UserToAdd.Deactivate();

        var result =
            await fixture.Handler.HandleAsync(
                CreateCommand(fixture));

        Assert.True(result.IsFailure);
        Assert.False(result.IsSuccess);
        Assert.Null(result.Value);

        Assert.Equal(
            UserErrors.Inactive,
            result.Error);

        Assert.Empty(
            fixture.ProjectMemberRepository
                .GetActiveCalls);

        Assert.Empty(
            fixture.PermissionRepository
                .IsActivePermissionCalls);

        Assert.Empty(
            fixture.ProjectMemberRepository
                .IsActiveMemberCalls);

        Assert.Null(
            fixture.ProjectMemberRepository
                .AddedProjectMember);

        Assert.Equal(
            0,
            fixture.UnitOfWork.SaveChangesCallCount);
    }

    [Fact]
    public async Task
    HandleAsync_ShouldReturnFailure_WhenUserIsAlreadyActiveMember()
    {
        var fixture =
            CreateFixture(
                UserRole.TenantAdmin);

        fixture.ProjectMemberRepository
            .IsActiveMemberResults[
                (
                    fixture.Project.Id,
                    fixture.UserToAdd.Id
                )] =
                    true;

        var result =
            await fixture.Handler.HandleAsync(
                CreateCommand(fixture));

        Assert.True(result.IsFailure);
        Assert.False(result.IsSuccess);
        Assert.Null(result.Value);

        Assert.Equal(
            ProjectMemberErrors.AlreadyActive,
            result.Error);

        var membershipCall =
            Assert.Single(
                fixture.ProjectMemberRepository
                    .IsActiveMemberCalls);

        Assert.Equal(
            (
                fixture.Project.Id,
                fixture.UserToAdd.Id
            ),
            membershipCall);

        Assert.Null(
            fixture.ProjectMemberRepository
                .AddedProjectMember);

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

        fixture.TenantRepository.TenantToReturn =
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

        Assert.Empty(
            fixture.UserRepository
                .CheckedGetByPublicIdCalls);

        Assert.Null(
            fixture.ProjectMemberRepository
                .AddedProjectMember);

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

        Assert.Empty(
            fixture.UserRepository
                .CheckedGetByPublicIdCalls);

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

        Assert.Single(
            fixture.UserRepository
                .CheckedGetByPublicIdCalls);

        Assert.Null(
            fixture.ProjectMemberRepository
                .AddedProjectMember);

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

        Assert.Single(
            fixture.UserRepository
                .CheckedGetByPublicIdCalls);

        Assert.Null(
            fixture.ProjectMemberRepository
                .AddedProjectMember);

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

        Assert.True(result.IsFailure);
        Assert.False(result.IsSuccess);
        Assert.Null(result.Value);

        Assert.Equal(
            ProjectErrors.NotFound,
            result.Error);

        Assert.Single(
            fixture.UserRepository
                .CheckedGetByPublicIdCalls);

        Assert.Null(
            fixture.ProjectMemberRepository
                .AddedProjectMember);

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

        Assert.Null(
            fixture.ProjectMemberRepository
                .AddedProjectMember);

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
            new AddProjectMemberCommand(
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
                    : fixture.UserToAdd.PublicId);

        await Assert.ThrowsAsync<ArgumentException>(
            () =>
                fixture.Handler.HandleAsync(
                    command));

        Assert.Null(
            fixture.ProjectMemberRepository
                .AddedProjectMember);

        Assert.Equal(
            0,
            fixture.UnitOfWork.SaveChangesCallCount);
    }

    private static Fixture CreateFixture(
        UserRole requesterRole)
    {
        var tenant =
            new Tenant(
                "Empresa Add Project Member",
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

        var userToAdd =
            new User(
                tenant.Id,
                "Usuário a adicionar",
                $"member-{Guid.NewGuid():N}@test.local",
                "password-hash",
                UserRole.Member);

        EntityTestHelper.SetId(
            userToAdd,
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
                userToAdd.PublicId] =
                    userToAdd;

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
            new FakeUnitOfWork();

        var handler =
            new AddProjectMemberHandler(
                tenantRepository,
                userRepository,
                projectRepository,
                projectMemberRepository,
                permissionRepository,
                unitOfWork);

        return new Fixture(
            tenant,
            requester,
            userToAdd,
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

    private static AddProjectMemberCommand CreateCommand(
        Fixture fixture)
    {
        return new AddProjectMemberCommand(
            fixture.Tenant.PublicId,
            fixture.Project.PublicId,
            fixture.Requester.PublicId,
            fixture.UserToAdd.PublicId);
    }

    private sealed record Fixture(
        Tenant Tenant,
        User Requester,
        User UserToAdd,
        Project Project,
        ProjectMember RequesterProjectMember,
        FakeTenantRepository TenantRepository,
        FakeUserRepository UserRepository,
        FakeProjectRepository ProjectRepository,
        FakeProjectMemberRepository ProjectMemberRepository,
        FakeProjectMemberPermissionRepository PermissionRepository,
        FakeUnitOfWork UnitOfWork,
        AddProjectMemberHandler Handler);
}