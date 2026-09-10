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
using WorkFlow.Domain.Entities;
using WorkFlow.Domain.Enums;
using WorkFlow.UnitTests.Application.Projects.Fakes;
using WorkFlow.UnitTests.Application.Tenants.Fakes;
using WorkFlow.UnitTests.Application.Users.Fakes;
using WorkFlow.UnitTests.Common;

namespace WorkFlow.UnitTests.API.Projects;

public sealed class GrantProjectMemberPermissionControllerTests
{
    [Fact]
    public async Task
    GrantPermission_ShouldReturnCreated_WhenTenantAdminGrantsPermission()
    {
        var fixture =
            CreateFixture(
                UserRole.TenantAdmin);

        var result =
            await ExecuteAsync(
                fixture,
                ProjectPermission.EditProject);

        var created =
            Assert.IsType<ObjectResult>(
                result.Result);

        Assert.Equal(
            StatusCodes.Status201Created,
            created.StatusCode);

        var response =
            Assert.IsType<GrantProjectMemberPermissionResponse>(
                created.Value);

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
            response.GrantedByUserPublicId);

        Assert.NotEqual(
            default,
            response.GrantedAt);

        Assert.Equal(
            1,
            fixture.UnitOfWork.SaveChangesCallCount);
    }

    [Fact]
    public async Task
    GrantPermission_ShouldReturnCreated_WhenMemberCanManagePermissions()
    {
        var fixture =
            CreateFixture(
                UserRole.Member);

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

        var result =
            await ExecuteAsync(
                fixture,
                ProjectPermission.CreateTask);

        var created =
            Assert.IsType<ObjectResult>(
                result.Result);

        Assert.Equal(
            StatusCodes.Status201Created,
            created.StatusCode);
    }

    [Fact]
    public async Task
    GrantPermission_ShouldReturnUnauthorized_WhenUserClaimIsMissing()
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
            await controller.GrantPermission(
                fixture.Tenant.PublicId,
                fixture.Project.PublicId,
                fixture.TargetUser.PublicId,
                new GrantProjectMemberPermissionRequest(
                    ProjectPermission.EditProject),
                CancellationToken.None);

        Assert.IsType<UnauthorizedResult>(
            result.Result);

        Assert.Equal(
            0,
            fixture.UnitOfWork.SaveChangesCallCount);
    }

    [Fact]
    public async Task
    GrantPermission_ShouldReturnNotFound_WhenTenantDoesNotExist()
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
    GrantPermission_ShouldReturnConflict_WhenTenantIsInactive()
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
    GrantPermission_ShouldReturnNotFound_WhenProjectDoesNotExist()
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
    GrantPermission_ShouldReturnForbidden_WhenRequesterCannotManagePermissions()
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
            ProjectMemberPermissionErrors.ManageNotAllowed.Code);
    }

    [Fact]
    public async Task
    GrantPermission_ShouldReturnNotFound_WhenTargetUserDoesNotExist()
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
            WorkFlow.Application.Users.UserErrors.NotFound.Code);
    }

    [Fact]
    public async Task
    GrantPermission_ShouldReturnForbidden_WhenTargetUserIsInactive()
    {
        var fixture =
            CreateFixture(
                UserRole.TenantAdmin);

        fixture.TargetUser.Deactivate();

        var result =
            await ExecuteAsync(
                fixture);

        AssertError(
            result,
            StatusCodes.Status403Forbidden,
            WorkFlow.Application.Users.UserErrors.Inactive.Code);
    }

    [Fact]
    public async Task
    GrantPermission_ShouldReturnNotFound_WhenTargetIsNotActiveProjectMember()
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
    GrantPermission_ShouldReturnConflict_WhenPermissionIsAlreadyActive()
    {
        var fixture =
            CreateFixture(
                UserRole.TenantAdmin);

        fixture.PermissionRepository
            .IsActivePermissionResults[
                (
                    fixture.TargetProjectMember.Id,
                    ProjectPermission.EditProject
                )] = true;

        var result =
            await ExecuteAsync(
                fixture,
                ProjectPermission.EditProject);

        AssertError(
            result,
            StatusCodes.Status409Conflict,
            ProjectMemberPermissionErrors.AlreadyActive.Code);
    }

    [Fact]
    public async Task
    GrantPermission_ShouldReturnConflict_WhenProjectIsArchived()
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
    }

    private static async Task<
        ActionResult<GrantProjectMemberPermissionResponse>>
        ExecuteAsync(
            Fixture fixture,
            ProjectPermission permission =
                ProjectPermission.EditProject)
    {
        var controller =
            CreateController(
                fixture);

        SetAuthenticatedUser(
            controller,
            fixture.Requester.PublicId);

        return await controller.GrantPermission(
            fixture.Tenant.PublicId,
            fixture.Project.PublicId,
            fixture.TargetUser.PublicId,
            new GrantProjectMemberPermissionRequest(
                permission),
            CancellationToken.None);
    }

    private static void AssertError(
        ActionResult<GrantProjectMemberPermissionResponse> result,
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

        var listHandler =
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
            listHandler,
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
                "Empresa Grant Permission API",
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
            new FakeProjectMemberPermissionRepository(),
            new TestUnitOfWork());
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
        FakeTenantRepository TenantRepository,
        FakeUserRepository UserRepository,
        FakeProjectRepository ProjectRepository,
        FakeProjectMemberRepository ProjectMemberRepository,
        FakeProjectMemberPermissionRepository PermissionRepository,
        TestUnitOfWork UnitOfWork);
}