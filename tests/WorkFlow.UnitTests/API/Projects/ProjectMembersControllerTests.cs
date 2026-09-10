using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using WorkFlow.API.Contracts.Common;
using WorkFlow.API.Contracts.Projects;
using WorkFlow.API.Controllers;
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

public sealed class ProjectMembersControllerTests
{
    [Fact]
    public async Task
    Add_ShouldUseAuthenticatedUserAndReturnCreated_WhenDataIsValid()
    {
        var fixture =
            CreateFixture(
                UserRole.TenantAdmin);

        var controller =
            CreateController(
                fixture);

        SetAuthenticatedUser(
            controller,
            fixture.Requester.PublicId);

        var request =
            new AddProjectMemberRequest(
                fixture.UserToAdd.PublicId);

        var result =
            await controller.Add(
                fixture.Tenant.PublicId,
                fixture.Project.PublicId,
                request,
                CancellationToken.None);

        var createdResult =
            Assert.IsType<ObjectResult>(
                result.Result);

        Assert.Equal(
            StatusCodes.Status201Created,
            createdResult.StatusCode);

        var response =
            Assert.IsType<AddProjectMemberResponse>(
                createdResult.Value);

        Assert.Equal(
            fixture.Project.PublicId,
            response.ProjectPublicId);

        Assert.Equal(
            fixture.UserToAdd.PublicId,
            response.UserPublicId);

        Assert.Equal(
            fixture.Requester.PublicId,
            response.AddedByUserPublicId);

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

        Assert.Equal(
            1,
            fixture.UnitOfWork.SaveChangesCallCount);
    }

    [Fact]
    public async Task
    Add_ShouldReturnUnauthorized_WhenUserPublicIdClaimIsMissing()
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
            await controller.Add(
                fixture.Tenant.PublicId,
                fixture.Project.PublicId,
                new AddProjectMemberRequest(
                    fixture.UserToAdd.PublicId),
                CancellationToken.None);

        Assert.IsType<UnauthorizedResult>(
            result.Result);

        Assert.Null(
            fixture.ProjectMemberRepository
                .AddedProjectMember);

        Assert.Equal(
            0,
            fixture.UnitOfWork.SaveChangesCallCount);
    }

    [Fact]
    public async Task
    Add_ShouldReturnNotFound_WhenTenantDoesNotExist()
    {
        var fixture =
            CreateFixture(
                UserRole.TenantAdmin);

        fixture.TenantRepository.TenantToReturn =
            null;

        var controller =
            CreateController(
                fixture);

        SetAuthenticatedUser(
            controller,
            fixture.Requester.PublicId);

        var result =
            await controller.Add(
                fixture.Tenant.PublicId,
                fixture.Project.PublicId,
                new AddProjectMemberRequest(
                    fixture.UserToAdd.PublicId),
                CancellationToken.None);

        var notFoundResult =
            Assert.IsType<NotFoundObjectResult>(
                result.Result);

        var response =
            Assert.IsType<ErrorResponse>(
                notFoundResult.Value);

        Assert.Equal(
            TenantErrors.NotFound.Code,
            response.Code);

        Assert.Equal(
            TenantErrors.NotFound.Message,
            response.Message);
    }

    [Fact]
    public async Task
    Add_ShouldReturnNotFound_WhenProjectDoesNotExist()
    {
        var fixture =
            CreateFixture(
                UserRole.TenantAdmin);

        fixture.ProjectRepository.ProjectToReturn =
            null;

        var controller =
            CreateController(
                fixture);

        SetAuthenticatedUser(
            controller,
            fixture.Requester.PublicId);

        var result =
            await controller.Add(
                fixture.Tenant.PublicId,
                Guid.NewGuid(),
                new AddProjectMemberRequest(
                    fixture.UserToAdd.PublicId),
                CancellationToken.None);

        var notFoundResult =
            Assert.IsType<NotFoundObjectResult>(
                result.Result);

        var response =
            Assert.IsType<ErrorResponse>(
                notFoundResult.Value);

        Assert.Equal(
            ProjectErrors.NotFound.Code,
            response.Code);

        Assert.Equal(
            ProjectErrors.NotFound.Message,
            response.Message);
    }

    [Fact]
    public async Task
    Add_ShouldReturnNotFound_WhenUserToAddDoesNotExist()
    {
        var fixture =
            CreateFixture(
                UserRole.TenantAdmin);

        fixture.UserRepository
            .UsersByPublicIdToReturn
            .Remove(
                fixture.UserToAdd.PublicId);

        var controller =
            CreateController(
                fixture);

        SetAuthenticatedUser(
            controller,
            fixture.Requester.PublicId);

        var result =
            await controller.Add(
                fixture.Tenant.PublicId,
                fixture.Project.PublicId,
                new AddProjectMemberRequest(
                    fixture.UserToAdd.PublicId),
                CancellationToken.None);

        var notFoundResult =
            Assert.IsType<NotFoundObjectResult>(
                result.Result);

        var response =
            Assert.IsType<ErrorResponse>(
                notFoundResult.Value);

        Assert.Equal(
            UserErrors.NotFound.Code,
            response.Code);

        Assert.Equal(
            UserErrors.NotFound.Message,
            response.Message);
    }

    [Fact]
    public async Task
    Add_ShouldReturnForbidden_WhenRequesterCannotAddMembers()
    {
        var fixture =
            CreateFixture(
                UserRole.Member);

        var controller =
            CreateController(
                fixture);

        SetAuthenticatedUser(
            controller,
            fixture.Requester.PublicId);

        var result =
            await controller.Add(
                fixture.Tenant.PublicId,
                fixture.Project.PublicId,
                new AddProjectMemberRequest(
                    fixture.UserToAdd.PublicId),
                CancellationToken.None);

        var forbiddenResult =
            Assert.IsType<ObjectResult>(
                result.Result);

        Assert.Equal(
            StatusCodes.Status403Forbidden,
            forbiddenResult.StatusCode);

        var response =
            Assert.IsType<ErrorResponse>(
                forbiddenResult.Value);

        Assert.Equal(
            ProjectMemberErrors.AddNotAllowed.Code,
            response.Code);

        Assert.Equal(
            ProjectMemberErrors.AddNotAllowed.Message,
            response.Message);
    }

    [Fact]
    public async Task
    Add_ShouldReturnForbidden_WhenUserIsInactive()
    {
        var fixture =
            CreateFixture(
                UserRole.TenantAdmin);

        fixture.UserToAdd.Deactivate();

        var controller =
            CreateController(
                fixture);

        SetAuthenticatedUser(
            controller,
            fixture.Requester.PublicId);

        var result =
            await controller.Add(
                fixture.Tenant.PublicId,
                fixture.Project.PublicId,
                new AddProjectMemberRequest(
                    fixture.UserToAdd.PublicId),
                CancellationToken.None);

        var forbiddenResult =
            Assert.IsType<ObjectResult>(
                result.Result);

        Assert.Equal(
            StatusCodes.Status403Forbidden,
            forbiddenResult.StatusCode);

        var response =
            Assert.IsType<ErrorResponse>(
                forbiddenResult.Value);

        Assert.Equal(
            UserErrors.Inactive.Code,
            response.Code);

        Assert.Equal(
            UserErrors.Inactive.Message,
            response.Message);
    }

    [Fact]
    public async Task
    Add_ShouldReturnConflict_WhenUserIsAlreadyActiveMember()
    {
        var fixture =
            CreateFixture(
                UserRole.TenantAdmin);

        fixture.ProjectMemberRepository
            .IsActiveMemberResults[
                (fixture.Project.Id, fixture.UserToAdd.Id)] =
            true;

        var controller =
            CreateController(
                fixture);

        SetAuthenticatedUser(
            controller,
            fixture.Requester.PublicId);

        var result =
            await controller.Add(
                fixture.Tenant.PublicId,
                fixture.Project.PublicId,
                new AddProjectMemberRequest(
                    fixture.UserToAdd.PublicId),
                CancellationToken.None);

        var conflictResult =
            Assert.IsType<ConflictObjectResult>(
                result.Result);

        Assert.Equal(
            StatusCodes.Status409Conflict,
            conflictResult.StatusCode);

        var response =
            Assert.IsType<ErrorResponse>(
                conflictResult.Value);

        Assert.Equal(
            ProjectMemberErrors.AlreadyActive.Code,
            response.Code);

        Assert.Equal(
            ProjectMemberErrors.AlreadyActive.Message,
            response.Message);
    }

    [Fact]
    public async Task
    Add_ShouldReturnConflict_WhenProjectIsArchived()
    {
        var fixture =
            CreateFixture(
                UserRole.TenantAdmin);

        fixture.Project.Archive();

        var controller =
            CreateController(
                fixture);

        SetAuthenticatedUser(
            controller,
            fixture.Requester.PublicId);

        var result =
            await controller.Add(
                fixture.Tenant.PublicId,
                fixture.Project.PublicId,
                new AddProjectMemberRequest(
                    fixture.UserToAdd.PublicId),
                CancellationToken.None);

        var conflictResult =
            Assert.IsType<ConflictObjectResult>(
                result.Result);

        Assert.Equal(
            StatusCodes.Status409Conflict,
            conflictResult.StatusCode);

        var response =
            Assert.IsType<ErrorResponse>(
                conflictResult.Value);

        Assert.Equal(
            ProjectErrors.Archived.Code,
            response.Code);

        Assert.Equal(
            ProjectErrors.Archived.Message,
            response.Message);
    }

    [Fact]
    public async Task
    Add_ShouldReturnConflict_WhenTenantIsInactive()
    {
        var fixture =
            CreateFixture(
                UserRole.TenantAdmin);

        fixture.Tenant.Deactivate();

        var controller =
            CreateController(
                fixture);

        SetAuthenticatedUser(
            controller,
            fixture.Requester.PublicId);

        var result =
            await controller.Add(
                fixture.Tenant.PublicId,
                fixture.Project.PublicId,
                new AddProjectMemberRequest(
                    fixture.UserToAdd.PublicId),
                CancellationToken.None);

        var conflictResult =
            Assert.IsType<ConflictObjectResult>(
                result.Result);

        Assert.Equal(
            StatusCodes.Status409Conflict,
            conflictResult.StatusCode);

        var response =
            Assert.IsType<ErrorResponse>(
                conflictResult.Value);

        Assert.Equal(
            TenantErrors.Inactive.Code,
            response.Code);
    }

    private static ProjectMembersController CreateController(
    Fixture fixture)
    {
        var permissionRepository =
            new FakeProjectMemberPermissionRepository();

        var addProjectMemberHandler =
            new AddProjectMemberHandler(
                fixture.TenantRepository,
                fixture.UserRepository,
                fixture.ProjectRepository,
                fixture.ProjectMemberRepository,
                permissionRepository,
                fixture.UnitOfWork);

        var listProjectMembersHandler =
            new ListProjectMembersHandler(
                fixture.TenantRepository,
                fixture.UserRepository,
                fixture.ProjectRepository,
                fixture.ProjectMemberRepository);

        var removeProjectMemberHandler =
            new RemoveProjectMemberHandler(
                fixture.TenantRepository,
                fixture.UserRepository,
                fixture.ProjectRepository,
                fixture.ProjectMemberRepository,
                permissionRepository,
                fixture.UnitOfWork);

        var grantPermissionHandler =
            new GrantProjectMemberPermissionHandler(
                fixture.TenantRepository,
                fixture.UserRepository,
                fixture.ProjectRepository,
                fixture.ProjectMemberRepository,
                permissionRepository,
                fixture.UnitOfWork);

        var listPermissionsHandler =
            new ListProjectMemberPermissionsHandler(
                fixture.TenantRepository,
                fixture.UserRepository,
                fixture.ProjectRepository,
                fixture.ProjectMemberRepository,
                permissionRepository);

        var revokePermissionHandler =
            new RevokeProjectMemberPermissionHandler(
                fixture.TenantRepository,
                fixture.UserRepository,
                fixture.ProjectRepository,
                fixture.ProjectMemberRepository,
                permissionRepository,
                fixture.UnitOfWork);

        return new ProjectMembersController(
            addProjectMemberHandler,
            listProjectMembersHandler,
            removeProjectMemberHandler,
            grantPermissionHandler,
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
                "Empresa Project Members",
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
                "Novo Membro",
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

        return new Fixture(
            tenant,
            requester,
            userToAdd,
            project,
            tenantRepository,
            userRepository,
            projectRepository,
            new FakeProjectMemberRepository(),
            new FakeUnitOfWork());
    }

    private sealed record Fixture(
        Tenant Tenant,
        User Requester,
        User UserToAdd,
        Project Project,
        FakeTenantRepository TenantRepository,
        FakeUserRepository UserRepository,
        FakeProjectRepository ProjectRepository,
        FakeProjectMemberRepository ProjectMemberRepository,
        FakeUnitOfWork UnitOfWork);
}