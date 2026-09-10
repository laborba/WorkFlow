using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using WorkFlow.API.Contracts.Common;
using WorkFlow.API.Contracts.Projects;
using WorkFlow.API.Controllers;
using WorkFlow.Application.Projects;
using WorkFlow.Application.Projects.CreateProject;
using WorkFlow.Application.Projects.GetProjectByPublicId;
using WorkFlow.Application.Projects.ListProjects;
using WorkFlow.Application.Projects.UpdateProject;
using WorkFlow.Application.Tenants;
using WorkFlow.Application.Users;
using WorkFlow.Domain.Entities;
using WorkFlow.Domain.Enums;
using WorkFlow.UnitTests.Application.Projects.Fakes;
using WorkFlow.UnitTests.Application.Tenants.Fakes;
using WorkFlow.UnitTests.Application.Users.Fakes;
using WorkFlow.UnitTests.Common;

namespace WorkFlow.UnitTests.API.Projects;

public sealed class UpdateProjectControllerTests
{
    [Fact]
    public async Task
    Update_ShouldUseAuthenticatedUserAndReturnUpdatedProject_WhenDataIsValid()
    {
        var tenant =
            CreatePersistedTenant();

        var requester =
            CreatePersistedUser(
                tenant.Id,
                10,
                UserRole.TenantAdmin);

        var creator =
            CreatePersistedUser(
                tenant.Id,
                20,
                UserRole.ProjectManager);

        var project =
            CreatePersistedProject(
                tenant.Id,
                creator.Id);

        var tenantRepository =
            new FakeTenantRepository
            {
                TenantToReturn = tenant
            };

        var userRepository =
            new FakeUserRepository
            {
                UserToReturn = requester
            };

        userRepository.UsersByIdToReturn.Add(
            creator.Id,
            creator);

        var projectRepository =
            new FakeProjectRepository
            {
                ProjectToReturn = project
            };

        var projectMemberRepository =
            new FakeProjectMemberRepository();

        var unitOfWork =
            new FakeUnitOfWork();

        var controller =
            CreateController(
                tenantRepository,
                userRepository,
                projectRepository,
                projectMemberRepository,
                unitOfWork);

        SetAuthenticatedUser(
            controller,
            requester.PublicId);

        var dueDate =
            new DateTime(
                2026,
                12,
                20,
                12,
                0,
                0,
                DateTimeKind.Utc);

        var request =
            new UpdateProjectRequest(
                "Projeto Atualizado",
                "Descrição atualizada",
                dueDate);

        var result =
            await controller.Update(
                tenant.PublicId,
                project.PublicId,
                request,
                CancellationToken.None);

        var okResult =
            Assert.IsType<OkObjectResult>(
                result.Result);

        var response =
            Assert.IsType<UpdateProjectResponse>(
                okResult.Value);

        Assert.Equal(
            StatusCodes.Status200OK,
            okResult.StatusCode);

        Assert.Equal(
            project.PublicId,
            response.PublicId);

        Assert.Equal(
            tenant.PublicId,
            response.TenantPublicId);

        Assert.Equal(
            creator.PublicId,
            response.CreatedByUserPublicId);

        Assert.Null(
            response.ResponsibleUserPublicId);

        Assert.Equal(
            "Projeto Atualizado",
            response.Name);

        Assert.Equal(
            "Descrição atualizada",
            response.Description);

        Assert.Equal(
            dueDate,
            response.DueDate);

        Assert.Equal(
            ProjectStatus.Planning,
            response.Status);

        Assert.Equal(
            requester.PublicId,
            userRepository.CheckedPublicId);

        Assert.Equal(
            1,
            unitOfWork.SaveChangesCallCount);
    }

    [Fact]
    public async Task
    Update_ShouldReturnUnauthorized_WhenUserPublicIdClaimIsMissing()
    {
        var tenantRepository =
            new FakeTenantRepository();

        var userRepository =
            new FakeUserRepository();

        var projectRepository =
            new FakeProjectRepository();

        var projectMemberRepository =
            new FakeProjectMemberRepository();

        var controller =
            CreateController(
                tenantRepository,
                userRepository,
                projectRepository,
                projectMemberRepository,
                new FakeUnitOfWork());

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
            await controller.Update(
                Guid.NewGuid(),
                Guid.NewGuid(),
                new UpdateProjectRequest(
                    "Projeto",
                    null,
                    null),
                CancellationToken.None);

        Assert.IsType<UnauthorizedResult>(
            result.Result);

        Assert.Null(
            tenantRepository.CheckedPublicId);

        Assert.Null(
            projectRepository.ProjectToReturn);
    }

    [Fact]
    public async Task
    Update_ShouldReturnNotFound_WhenTenantDoesNotExist()
    {
        var controller =
            CreateController(
                new FakeTenantRepository(),
                new FakeUserRepository(),
                new FakeProjectRepository(),
                new FakeProjectMemberRepository(),
                new FakeUnitOfWork());

        SetAuthenticatedUser(
            controller,
            Guid.NewGuid());

        var result =
            await controller.Update(
                Guid.NewGuid(),
                Guid.NewGuid(),
                new UpdateProjectRequest(
                    "Projeto",
                    null,
                    null),
                CancellationToken.None);

        var notFoundResult =
            Assert.IsType<NotFoundObjectResult>(
                result.Result);

        var response =
            Assert.IsType<ErrorResponse>(
                notFoundResult.Value);

        Assert.Equal(
            StatusCodes.Status404NotFound,
            notFoundResult.StatusCode);

        Assert.Equal(
            TenantErrors.NotFound.Code,
            response.Code);

        Assert.Equal(
            TenantErrors.NotFound.Message,
            response.Message);
    }

    [Fact]
    public async Task
    Update_ShouldReturnNotFound_WhenProjectDoesNotExist()
    {
        var tenant =
            CreatePersistedTenant();

        var requester =
            CreatePersistedUser(
                tenant.Id,
                10,
                UserRole.TenantAdmin);

        var tenantRepository =
            new FakeTenantRepository
            {
                TenantToReturn = tenant
            };

        var userRepository =
            new FakeUserRepository
            {
                UserToReturn = requester
            };

        var controller =
            CreateController(
                tenantRepository,
                userRepository,
                new FakeProjectRepository(),
                new FakeProjectMemberRepository(),
                new FakeUnitOfWork());

        SetAuthenticatedUser(
            controller,
            requester.PublicId);

        var result =
            await controller.Update(
                tenant.PublicId,
                Guid.NewGuid(),
                new UpdateProjectRequest(
                    "Projeto",
                    null,
                    null),
                CancellationToken.None);

        var notFoundResult =
            Assert.IsType<NotFoundObjectResult>(
                result.Result);

        var response =
            Assert.IsType<ErrorResponse>(
                notFoundResult.Value);

        Assert.Equal(
            StatusCodes.Status404NotFound,
            notFoundResult.StatusCode);

        Assert.Equal(
            ProjectErrors.NotFound.Code,
            response.Code);

        Assert.Equal(
            ProjectErrors.NotFound.Message,
            response.Message);
    }

    [Fact]
    public async Task
    Update_ShouldReturnForbidden_WhenRequesterIsMember()
    {
        var tenant =
            CreatePersistedTenant();

        var requester =
            CreatePersistedUser(
                tenant.Id,
                10,
                UserRole.Member);

        var creator =
            CreatePersistedUser(
                tenant.Id,
                20,
                UserRole.TenantAdmin);

        var project =
            CreatePersistedProject(
                tenant.Id,
                creator.Id);

        var tenantRepository =
            new FakeTenantRepository
            {
                TenantToReturn = tenant
            };

        var userRepository =
            new FakeUserRepository
            {
                UserToReturn = requester
            };

        var projectRepository =
            new FakeProjectRepository
            {
                ProjectToReturn = project
            };

        var controller =
            CreateController(
                tenantRepository,
                userRepository,
                projectRepository,
                new FakeProjectMemberRepository(),
                new FakeUnitOfWork());

        SetAuthenticatedUser(
            controller,
            requester.PublicId);

        var result =
            await controller.Update(
                tenant.PublicId,
                project.PublicId,
                new UpdateProjectRequest(
                    "Projeto Atualizado",
                    null,
                    null),
                CancellationToken.None);

        var forbiddenResult =
            Assert.IsType<ObjectResult>(
                result.Result);

        var response =
            Assert.IsType<ErrorResponse>(
                forbiddenResult.Value);

        Assert.Equal(
            StatusCodes.Status403Forbidden,
            forbiddenResult.StatusCode);

        Assert.Equal(
            ProjectErrors.UpdateNotAllowed.Code,
            response.Code);

        Assert.Equal(
            ProjectErrors.UpdateNotAllowed.Message,
            response.Message);
    }

    [Fact]
    public async Task
    Update_ShouldReturnForbidden_WhenRequesterIsInactive()
    {
        var tenant =
            CreatePersistedTenant();

        var requester =
            CreatePersistedUser(
                tenant.Id,
                10,
                UserRole.TenantAdmin);

        requester.Deactivate();

        var tenantRepository =
            new FakeTenantRepository
            {
                TenantToReturn = tenant
            };

        var userRepository =
            new FakeUserRepository
            {
                UserToReturn = requester
            };

        var controller =
            CreateController(
                tenantRepository,
                userRepository,
                new FakeProjectRepository(),
                new FakeProjectMemberRepository(),
                new FakeUnitOfWork());

        SetAuthenticatedUser(
            controller,
            requester.PublicId);

        var result =
            await controller.Update(
                tenant.PublicId,
                Guid.NewGuid(),
                new UpdateProjectRequest(
                    "Projeto",
                    null,
                    null),
                CancellationToken.None);

        var forbiddenResult =
            Assert.IsType<ObjectResult>(
                result.Result);

        var response =
            Assert.IsType<ErrorResponse>(
                forbiddenResult.Value);

        Assert.Equal(
            StatusCodes.Status403Forbidden,
            forbiddenResult.StatusCode);

        Assert.Equal(
            UserErrors.Inactive.Code,
            response.Code);

        Assert.Equal(
            UserErrors.Inactive.Message,
            response.Message);
    }

    [Fact]
    public async Task
    Update_ShouldReturnConflict_WhenTenantIsInactive()
    {
        var tenant =
            CreatePersistedTenant();

        tenant.Deactivate();

        var tenantRepository =
            new FakeTenantRepository
            {
                TenantToReturn = tenant
            };

        var controller =
            CreateController(
                tenantRepository,
                new FakeUserRepository(),
                new FakeProjectRepository(),
                new FakeProjectMemberRepository(),
                new FakeUnitOfWork());

        SetAuthenticatedUser(
            controller,
            Guid.NewGuid());

        var result =
            await controller.Update(
                tenant.PublicId,
                Guid.NewGuid(),
                new UpdateProjectRequest(
                    "Projeto",
                    null,
                    null),
                CancellationToken.None);

        var conflictResult =
            Assert.IsType<ConflictObjectResult>(
                result.Result);

        var response =
            Assert.IsType<ErrorResponse>(
                conflictResult.Value);

        Assert.Equal(
            StatusCodes.Status409Conflict,
            conflictResult.StatusCode);

        Assert.Equal(
            TenantErrors.Inactive.Code,
            response.Code);

        Assert.Equal(
            TenantErrors.Inactive.Message,
            response.Message);
    }

    [Fact]
    public async Task
    Update_ShouldReturnConflict_WhenProjectIsArchived()
    {
        var tenant =
            CreatePersistedTenant();

        var requester =
            CreatePersistedUser(
                tenant.Id,
                10,
                UserRole.TenantAdmin);

        var creator =
            CreatePersistedUser(
                tenant.Id,
                20,
                UserRole.ProjectManager);

        var project =
            CreatePersistedProject(
                tenant.Id,
                creator.Id);

        project.Archive();

        var tenantRepository =
            new FakeTenantRepository
            {
                TenantToReturn = tenant
            };

        var userRepository =
            new FakeUserRepository
            {
                UserToReturn = requester
            };

        var projectRepository =
            new FakeProjectRepository
            {
                ProjectToReturn = project
            };

        var controller =
            CreateController(
                tenantRepository,
                userRepository,
                projectRepository,
                new FakeProjectMemberRepository(),
                new FakeUnitOfWork());

        SetAuthenticatedUser(
            controller,
            requester.PublicId);

        var result =
            await controller.Update(
                tenant.PublicId,
                project.PublicId,
                new UpdateProjectRequest(
                    "Projeto Atualizado",
                    null,
                    null),
                CancellationToken.None);

        var conflictResult =
            Assert.IsType<ConflictObjectResult>(
                result.Result);

        var response =
            Assert.IsType<ErrorResponse>(
                conflictResult.Value);

        Assert.Equal(
            StatusCodes.Status409Conflict,
            conflictResult.StatusCode);

        Assert.Equal(
            ProjectErrors.Archived.Code,
            response.Code);

        Assert.Equal(
            ProjectErrors.Archived.Message,
            response.Message);
    }

    private static ProjectsController CreateController(
    FakeTenantRepository tenantRepository,
    FakeUserRepository userRepository,
    FakeProjectRepository projectRepository,
    FakeProjectMemberRepository projectMemberRepository,
    FakeUnitOfWork unitOfWork)
    {
        var projectMemberPermissionRepository =
            new FakeProjectMemberPermissionRepository();

        var createProjectHandler =
            new CreateProjectHandler(
                tenantRepository,
                userRepository,
                projectRepository,
                projectMemberRepository,
                projectMemberPermissionRepository,
                unitOfWork);

        var getProjectByPublicIdHandler =
            new GetProjectByPublicIdHandler(
                tenantRepository,
                userRepository,
                projectRepository,
                projectMemberRepository);

        var listProjectsHandler =
            new ListProjectsHandler(
                tenantRepository,
                userRepository,
                projectRepository);

        var updateProjectHandler =
            new UpdateProjectHandler(
                tenantRepository,
                userRepository,
                projectRepository,
                projectMemberRepository,
                projectMemberPermissionRepository,
                unitOfWork);

        return new ProjectsController(
            createProjectHandler,
            getProjectByPublicIdHandler,
            listProjectsHandler,
            updateProjectHandler);
    }

    private static void SetAuthenticatedUser(
        ProjectsController controller,
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

    private static Tenant CreatePersistedTenant()
    {
        var tenant =
            new Tenant(
                "Empresa Update Project",
                $"REG-{Guid.NewGuid():N}",
                $"tenant-{Guid.NewGuid():N}@test.local");

        EntityTestHelper.SetId(
            tenant,
            42);

        return tenant;
    }

    private static User CreatePersistedUser(
        long tenantId,
        long id,
        UserRole role)
    {
        var user =
            new User(
                tenantId,
                $"Usuário {id}",
                $"usuario-{id}-{Guid.NewGuid():N}@test.local",
                "password-hash",
                role);

        EntityTestHelper.SetId(
            user,
            id);

        return user;
    }

    private static Project CreatePersistedProject(
        long tenantId,
        long createdByUserId)
    {
        var project =
            new Project(
                tenantId,
                "Projeto Original",
                createdByUserId,
                "Descrição original",
                dueDate: new DateTime(
                    2026,
                    10,
                    31,
                    12,
                    0,
                    0,
                    DateTimeKind.Utc));

        EntityTestHelper.SetId(
            project,
            100);

        return project;
    }
}