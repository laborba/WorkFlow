using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using WorkFlow.API.Contracts.Common;
using WorkFlow.API.Contracts.Projects;
using WorkFlow.API.Controllers;
using WorkFlow.Application.Abstractions.Persistence;
using WorkFlow.Application.Projects;
using WorkFlow.Application.Projects.AddProjectMember;
using WorkFlow.Application.Projects.GrantProjectMemberPermission;
using WorkFlow.Application.Projects.ListProjectMemberPermissions;
using WorkFlow.Application.Projects.ListProjectMembers;
using WorkFlow.Application.Projects.RemoveProjectMember;
using WorkFlow.Application.Projects.RevokeProjectMemberPermission;
using WorkFlow.Application.Tenants;
using WorkFlow.Application.Users;
using WorkFlow.Domain.Entities;
using WorkFlow.Domain.Enums;
using WorkFlow.UnitTests.Application.Projects.Fakes;
using WorkFlow.UnitTests.Application.Tenants.Fakes;
using WorkFlow.UnitTests.Application.Users.Fakes;
using WorkFlow.UnitTests.Common;

namespace WorkFlow.UnitTests.API.Projects;

public sealed class RevokeProjectMemberPermissionControllerTests
{
    [Fact]
    public async Task
    RevokePermission_ShouldReturnOk_WhenRequesterIsTenantAdmin()
    {
        var fixture =
            CreateFixture(
                UserRole.TenantAdmin);

        var result =
            await ExecuteAsync(
                fixture);

        var ok =
            Assert.IsType<OkObjectResult>(
                result.Result);

        var response =
            Assert.IsType<RevokeProjectMemberPermissionResponse>(
                ok.Value);

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

        Assert.Equal(
            1,
            fixture.UnitOfWork.SaveChangesCallCount);
    }

    [Fact]
    public async Task
    RevokePermission_ShouldReturnOk_WhenMemberCanManagePermissions()
    {
        var fixture =
            CreateFixture(
                UserRole.Member);

        GrantManagePermissionToRequester(
            fixture);

        var result =
            await ExecuteAsync(
                fixture);

        Assert.IsType<OkObjectResult>(
            result.Result);

        Assert.False(
            fixture.TargetPermission.IsActive);

        Assert.Contains(
            (
                fixture.RequesterProjectMember.Id,
                ProjectPermission.ManageProjectPermissions
            ),
            fixture.PermissionRepository
                .IsActivePermissionCalls);

        Assert.Equal(
            1,
            fixture.UnitOfWork.SaveChangesCallCount);
    }

    [Fact]
    public async Task
    RevokePermission_ShouldReturnUnauthorized_WhenUserClaimIsMissing()
    {
        var fixture =
            CreateFixture(
                UserRole.TenantAdmin);

        var controller =
            CreateController(
                fixture);

        controller.ControllerContext =
            new ControllerContext
            {
                HttpContext =
                    new DefaultHttpContext
                    {
                        User =
                            new ClaimsPrincipal(
                                new ClaimsIdentity(
                                    authenticationType: "Test"))
                    }
            };

        var result =
            await controller.RevokePermission(
                fixture.Tenant.PublicId,
                fixture.Project.PublicId,
                fixture.TargetUser.PublicId,
                ProjectPermission.EditProject,
                CancellationToken.None);

        Assert.IsType<UnauthorizedResult>(
            result.Result);

        Assert.True(
            fixture.TargetPermission.IsActive);

        Assert.Equal(
            0,
            fixture.UnitOfWork.SaveChangesCallCount);
    }

    [Fact]
    public async Task
    RevokePermission_ShouldReturnNotFound_WhenTenantDoesNotExist()
    {
        var fixture =
            CreateFixture(
                UserRole.TenantAdmin);

        fixture.TenantRepository.TenantToReturn =
            null;

        var result =
            await ExecuteAsync(
                fixture);

        AssertError(
            result,
            StatusCodes.Status404NotFound,
            TenantErrors.NotFound.Code);
    }

    [Fact]
    public async Task
    RevokePermission_ShouldReturnConflict_WhenTenantIsInactive()
    {
        var fixture =
            CreateFixture(
                UserRole.TenantAdmin);

        fixture.Tenant.Deactivate();

        var result =
            await ExecuteAsync(
                fixture);

        AssertError(
            result,
            StatusCodes.Status409Conflict,
            TenantErrors.Inactive.Code);
    }

    [Fact]
    public async Task
    RevokePermission_ShouldReturnNotFound_WhenProjectDoesNotExist()
    {
        var fixture =
            CreateFixture(
                UserRole.TenantAdmin);

        fixture.ProjectRepository.ProjectToReturn =
            null;

        var result =
            await ExecuteAsync(
                fixture);

        AssertError(
            result,
            StatusCodes.Status404NotFound,
            ProjectErrors.NotFound.Code);
    }

    [Fact]
    public async Task
    RevokePermission_ShouldReturnForbidden_WhenRequesterCannotManagePermissions()
    {
        var fixture =
            CreateFixture(
                UserRole.Member);

        var result =
            await ExecuteAsync(
                fixture);

        AssertError(
            result,
            StatusCodes.Status403Forbidden,
            ProjectMemberPermissionErrors
                .ManageNotAllowed.Code);

        Assert.True(
            fixture.TargetPermission.IsActive);
    }

    [Fact]
    public async Task
    RevokePermission_ShouldReturnNotFound_WhenTargetUserDoesNotExist()
    {
        var fixture =
            CreateFixture(
                UserRole.TenantAdmin);

        fixture.UserRepository
            .UsersByPublicIdToReturn
            .Remove(
                fixture.TargetUser.PublicId);

        var result =
            await ExecuteAsync(
                fixture);

        AssertError(
            result,
            StatusCodes.Status404NotFound,
            UserErrors.NotFound.Code);
    }

    [Fact]
    public async Task
    RevokePermission_ShouldReturnOk_WhenTargetUserIsInactive()
    {
        var fixture =
            CreateFixture(
                UserRole.TenantAdmin);

        fixture.TargetUser.Deactivate();

        var result =
            await ExecuteAsync(
                fixture);

        Assert.IsType<OkObjectResult>(
            result.Result);

        Assert.False(
            fixture.TargetPermission.IsActive);
    }

    [Fact]
    public async Task
    RevokePermission_ShouldReturnNotFound_WhenTargetIsNotActiveProjectMember()
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
            await ExecuteAsync(
                fixture);

        AssertError(
            result,
            StatusCodes.Status404NotFound,
            ProjectMemberErrors.NotActive.Code);
    }

    [Fact]
    public async Task
    RevokePermission_ShouldReturnNotFound_WhenPermissionIsNotActive()
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
            await ExecuteAsync(
                fixture);

        AssertError(
            result,
            StatusCodes.Status404NotFound,
            ProjectMemberPermissionErrors
                .NotActive.Code);

        Assert.Equal(
            0,
            fixture.UnitOfWork.SaveChangesCallCount);
    }

    [Fact]
    public async Task
    RevokePermission_ShouldReturnConflict_WhenProjectIsArchived()
    {
        var fixture =
            CreateFixture(
                UserRole.TenantAdmin);

        fixture.Project.Archive();

        var result =
            await ExecuteAsync(
                fixture);

        AssertError(
            result,
            StatusCodes.Status409Conflict,
            ProjectErrors.Archived.Code);

        Assert.True(
            fixture.TargetPermission.IsActive);
    }

    private static async Task<
        ActionResult<RevokeProjectMemberPermissionResponse>>
        ExecuteAsync(
            Fixture fixture)
    {
        var controller =
            CreateController(
                fixture);

        SetAuthenticatedUser(
            controller,
            fixture.Requester.PublicId);

        return await controller.RevokePermission(
            fixture.Tenant.PublicId,
            fixture.Project.PublicId,
            fixture.TargetUser.PublicId,
            ProjectPermission.EditProject,
            CancellationToken.None);
    }

    private static void AssertError(
        ActionResult<RevokeProjectMemberPermissionResponse> result,
        int expectedStatusCode,
        string expectedCode)
    {
        var objectResult =
            Assert.IsAssignableFrom<ObjectResult>(
                result.Result);

        Assert.Equal(
            expectedStatusCode,
            objectResult.StatusCode);

        var response =
            Assert.IsType<ErrorResponse>(
                objectResult.Value);

        Assert.Equal(
            expectedCode,
            response.Code);
    }

    private static ProjectMembersController CreateController(
    Fixture fixture)
    {
        var addHandler =
            new AddProjectMemberHandler(
                fixture.TenantRepository,
                fixture.UserRepository,
                fixture.ProjectRepository,
                fixture.ProjectMemberRepository,
                fixture.PermissionRepository,
                fixture.UnitOfWork);

        var listMembersHandler =
            new ListProjectMembersHandler(
                fixture.TenantRepository,
                fixture.UserRepository,
                fixture.ProjectRepository,
                fixture.ProjectMemberRepository);

        var removeHandler =
            new RemoveProjectMemberHandler(
                fixture.TenantRepository,
                fixture.UserRepository,
                fixture.ProjectRepository,
                fixture.ProjectMemberRepository,
                fixture.PermissionRepository,
                fixture.UnitOfWork);

        var grantHandler =
            new GrantProjectMemberPermissionHandler(
                fixture.TenantRepository,
                fixture.UserRepository,
                fixture.ProjectRepository,
                fixture.ProjectMemberRepository,
                fixture.PermissionRepository,
                fixture.UnitOfWork);

        var listPermissionsHandler =
            new ListProjectMemberPermissionsHandler(
                fixture.TenantRepository,
                fixture.UserRepository,
                fixture.ProjectRepository,
                fixture.ProjectMemberRepository,
                fixture.PermissionRepository);

        var revokePermissionHandler =
            new RevokeProjectMemberPermissionHandler(
                fixture.TenantRepository,
                fixture.UserRepository,
                fixture.ProjectRepository,
                fixture.ProjectMemberRepository,
                fixture.PermissionRepository,
                fixture.UnitOfWork);

        return new ProjectMembersController(
            addHandler,
            listMembersHandler,
            removeHandler,
            grantHandler,
            listPermissionsHandler,
            revokePermissionHandler);
    }

    private static void SetAuthenticatedUser(
        ProjectMembersController controller,
        Guid userPublicId)
    {
        var identity =
            new ClaimsIdentity(
                new[]
                {
                    new Claim(
                        ClaimTypes.NameIdentifier,
                        userPublicId.ToString())
                },
                "Test");

        controller.ControllerContext =
            new ControllerContext
            {
                HttpContext =
                    new DefaultHttpContext
                    {
                        User =
                            new ClaimsPrincipal(
                                identity)
                    }
            };
    }

    private static Fixture CreateFixture(
        UserRole requesterRole)
    {
        var tenant =
            new Tenant(
                "Empresa Revoke Permission API",
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
            new TestUnitOfWork());
    }

    private static void GrantManagePermissionToRequester(
        Fixture fixture)
    {
        fixture.ProjectMemberRepository
            .ActiveMembersToReturn[
                (
                    fixture.Project.Id,
                    fixture.Requester.Id
                )] =
                    fixture.RequesterProjectMember;

        fixture.PermissionRepository
            .IsActivePermissionResults[
                (
                    fixture.RequesterProjectMember.Id,
                    ProjectPermission.ManageProjectPermissions
                )] = true;
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
        TestUnitOfWork UnitOfWork);
}