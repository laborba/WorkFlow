using WorkFlow.Application.Abstractions.Persistence.Models;
using WorkFlow.Application.Projects;
using WorkFlow.Application.Projects.ListProjectMemberPermissions;
using WorkFlow.Application.Tenants;
using WorkFlow.Application.Users;
using WorkFlow.Domain.Entities;
using WorkFlow.Domain.Enums;
using WorkFlow.UnitTests.Application.Projects.Fakes;
using WorkFlow.UnitTests.Application.Tenants.Fakes;
using WorkFlow.UnitTests.Application.Users.Fakes;
using WorkFlow.UnitTests.Common;

namespace WorkFlow.UnitTests.Application.Projects.ListProjectMemberPermissions;

public sealed class ListProjectMemberPermissionsHandlerTests
{
    [Fact]
    public async Task
    HandleAsync_ShouldReturnActivePermissions_WhenRequesterIsTenantAdmin()
    {
        var fixture =
            CreateFixture(
                UserRole.TenantAdmin);

        var grantedByPublicId =
            Guid.NewGuid();

        var grantedAt =
            DateTime.UtcNow.AddHours(-1);

        fixture.PermissionRepository
            .ActivePermissionsToReturn =
                new[]
                {
                    new ProjectMemberPermissionListItemData(
                        ProjectPermission.EditProject,
                        grantedByPublicId,
                        grantedAt),
                    new ProjectMemberPermissionListItemData(
                        ProjectPermission.CreateTask,
                        grantedByPublicId,
                        grantedAt.AddMinutes(1))
                };

        var result =
            await fixture.Handler.HandleAsync(
                CreateQuery(fixture));

        Assert.True(result.IsSuccess);
        Assert.False(result.IsFailure);
        Assert.Null(result.Error);

        var response =
            Assert.IsType<ListProjectMemberPermissionsResult>(
                result.Value);

        Assert.Equal(
            fixture.Project.PublicId,
            response.ProjectPublicId);

        Assert.Equal(
            fixture.TargetUser.PublicId,
            response.UserPublicId);

        Assert.Equal(
            2,
            response.Permissions.Count);

        Assert.Contains(
            response.Permissions,
            item =>
                item.Permission ==
                    ProjectPermission.EditProject &&
                item.GrantedByUserPublicId ==
                    grantedByPublicId &&
                item.GrantedAt ==
                    grantedAt);

        Assert.Contains(
            response.Permissions,
            item =>
                item.Permission ==
                    ProjectPermission.CreateTask);

        Assert.Empty(
            fixture.PermissionRepository
                .IsActivePermissionCalls);

        Assert.Equal(
            fixture.TargetProjectMember.Id,
            fixture.PermissionRepository
                .LastGetActivePermissionsProjectMemberId);
    }

    [Theory]
    [InlineData(UserRole.ProjectManager)]
    [InlineData(UserRole.Member)]
    public async Task
    HandleAsync_ShouldReturnPermissions_WhenRequesterCanManagePermissions(
        UserRole role)
    {
        var fixture =
            CreateFixture(
                role);

        GrantManagePermissionToRequester(
            fixture);

        var result =
            await fixture.Handler.HandleAsync(
                CreateQuery(fixture));

        Assert.True(
            result.IsSuccess);

        Assert.Equal(
            2,
            fixture.ProjectMemberRepository
                .GetActiveCalls.Count);

        Assert.Contains(
            (
                fixture.Project.Id,
                fixture.Requester.Id
            ),
            fixture.ProjectMemberRepository
                .GetActiveCalls);

        Assert.Contains(
            (
                fixture.Project.Id,
                fixture.TargetUser.Id
            ),
            fixture.ProjectMemberRepository
                .GetActiveCalls);

        Assert.Contains(
            (
                fixture.RequesterProjectMember.Id,
                ProjectPermission.ManageProjectPermissions
            ),
            fixture.PermissionRepository
                .IsActivePermissionCalls);

        Assert.Equal(
            fixture.TargetProjectMember.Id,
            fixture.PermissionRepository
                .LastGetActivePermissionsProjectMemberId);
    }

    [Theory]
    [InlineData(UserRole.ProjectManager)]
    [InlineData(UserRole.Member)]
    public async Task
    HandleAsync_ShouldReturnFailure_WhenRequesterHasNoActiveMembership(
        UserRole role)
    {
        var fixture =
            CreateFixture(
                role);

        var result =
            await fixture.Handler.HandleAsync(
                CreateQuery(fixture));

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

        Assert.Null(
            fixture.PermissionRepository
                .LastGetActivePermissionsProjectMemberId);
    }

    [Theory]
    [InlineData(UserRole.ProjectManager)]
    [InlineData(UserRole.Member)]
    public async Task
    HandleAsync_ShouldReturnFailure_WhenRequesterDoesNotHaveManageProjectPermissions(
        UserRole role)
    {
        var fixture =
            CreateFixture(
                role);

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
                CreateQuery(fixture));

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

        Assert.Null(
            fixture.PermissionRepository
                .LastGetActivePermissionsProjectMemberId);
    }

    [Fact]
    public async Task
    HandleAsync_ShouldAllowListing_WhenProjectIsArchived()
    {
        var fixture =
            CreateFixture(
                UserRole.TenantAdmin);

        fixture.Project.Archive();

        var result =
            await fixture.Handler.HandleAsync(
                CreateQuery(fixture));

        Assert.True(
            result.IsSuccess);

        Assert.Equal(
            fixture.TargetProjectMember.Id,
            fixture.PermissionRepository
                .LastGetActivePermissionsProjectMemberId);
    }

    [Fact]
    public async Task
    HandleAsync_ShouldAllowListing_WhenTargetUserIsInactive()
    {
        var fixture =
            CreateFixture(
                UserRole.TenantAdmin);

        fixture.TargetUser.Deactivate();

        var result =
            await fixture.Handler.HandleAsync(
                CreateQuery(fixture));

        Assert.True(
            result.IsSuccess);

        Assert.Equal(
            fixture.TargetProjectMember.Id,
            fixture.PermissionRepository
                .LastGetActivePermissionsProjectMemberId);
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
                CreateQuery(fixture));

        Assert.True(
            result.IsFailure);

        Assert.Equal(
            UserErrors.NotFound,
            result.Error);

        Assert.Null(
            fixture.PermissionRepository
                .LastGetActivePermissionsProjectMemberId);
    }

    [Fact]
    public async Task
    HandleAsync_ShouldReturnFailure_WhenTargetUserIsNotActiveProjectMember()
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
                CreateQuery(fixture));

        Assert.True(
            result.IsFailure);

        Assert.Equal(
            ProjectMemberErrors.NotActive,
            result.Error);

        Assert.Null(
            fixture.PermissionRepository
                .LastGetActivePermissionsProjectMemberId);
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
                CreateQuery(fixture));

        Assert.True(
            result.IsFailure);

        Assert.Equal(
            TenantErrors.NotFound,
            result.Error);
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
                CreateQuery(fixture));

        Assert.True(
            result.IsFailure);

        Assert.Equal(
            TenantErrors.Inactive,
            result.Error);
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
                CreateQuery(fixture));

        Assert.True(
            result.IsFailure);

        Assert.Equal(
            UserErrors.NotFound,
            result.Error);
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
                CreateQuery(fixture));

        Assert.True(
            result.IsFailure);

        Assert.Equal(
            UserErrors.Inactive,
            result.Error);
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
                CreateQuery(fixture));

        Assert.True(
            result.IsFailure);

        Assert.Equal(
            ProjectErrors.NotFound,
            result.Error);
    }

    [Fact]
    public async Task
    HandleAsync_ShouldThrow_WhenQueryIsNull()
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

        var query =
            new ListProjectMemberPermissionsQuery(
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
                    query));
    }

    private static Fixture CreateFixture(
        UserRole requesterRole)
    {
        var tenant =
            new Tenant(
                "Empresa List Permissions",
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

        var handler =
            new ListProjectMemberPermissionsHandler(
                tenantRepository,
                userRepository,
                projectRepository,
                projectMemberRepository,
                permissionRepository);

        return new Fixture(
            tenant,
            requester,
            targetUser,
            project,
            requesterProjectMember,
            targetProjectMember,
            tenantRepository,
            userRepository,
            projectRepository,
            projectMemberRepository,
            permissionRepository,
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

    private static ListProjectMemberPermissionsQuery CreateQuery(
        Fixture fixture)
    {
        return new ListProjectMemberPermissionsQuery(
            fixture.Tenant.PublicId,
            fixture.Project.PublicId,
            fixture.Requester.PublicId,
            fixture.TargetUser.PublicId);
    }

    private sealed record Fixture(
        Tenant Tenant,
        User Requester,
        User TargetUser,
        Project Project,
        ProjectMember RequesterProjectMember,
        ProjectMember TargetProjectMember,
        FakeTenantRepository TenantRepository,
        FakeUserRepository UserRepository,
        FakeProjectRepository ProjectRepository,
        FakeProjectMemberRepository ProjectMemberRepository,
        FakeProjectMemberPermissionRepository PermissionRepository,
        ListProjectMemberPermissionsHandler Handler);
}