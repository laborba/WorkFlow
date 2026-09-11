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
using WorkFlow.Application.Projects.PauseProject;
using WorkFlow.Application.Projects.ResumeProject;
using WorkFlow.Application.Projects.StartProject;
using WorkFlow.Application.Projects.UpdateProject;
using WorkFlow.Application.Tenants;
using WorkFlow.Domain.Entities;
using WorkFlow.Domain.Enums;
using WorkFlow.UnitTests.Application.Projects.Fakes;
using WorkFlow.UnitTests.Application.Tenants.Fakes;
using WorkFlow.UnitTests.Application.Users.Fakes;
using WorkFlow.UnitTests.Common;
using WorkFlow.UnitTests.Common.Fakes;

namespace WorkFlow.UnitTests.API.Projects;

public sealed class ProjectStatusControllerTests
{
    [Fact]
    public async Task
    Start_ShouldReturnOk_WhenTenantAdminStartsPlanningProject()
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

        var result =
            await controller.Start(
                fixture.Tenant.PublicId,
                fixture.Project.PublicId,
                CancellationToken.None);

        var okResult =
            Assert.IsType<OkObjectResult>(
                result.Result);

        var response =
            Assert.IsType<ProjectStatusResponse>(
                okResult.Value);

        Assert.Equal(
            StatusCodes.Status200OK,
            okResult.StatusCode);

        Assert.Equal(
            fixture.Project.PublicId,
            response.PublicId);

        Assert.Equal(
            fixture.Tenant.PublicId,
            response.TenantPublicId);

        Assert.Equal(
            ProjectStatus.InProgress,
            response.Status);

        Assert.Equal(
            ProjectStatus.InProgress,
            fixture.Project.Status);

        Assert.Equal(
            fixture.Project.DueDate,
            response.DueDate);

        Assert.Equal(
            1,
            fixture.UnitOfWork.SaveChangesCallCount);
    }

    [Fact]
    public async Task
    Start_ShouldReturnUnauthorized_WhenUserPublicIdClaimIsMissing()
    {
        var fixture =
            CreateFixture(
                UserRole.TenantAdmin);

        var controller =
            CreateController(
                fixture);

        SetUnauthenticatedUser(
            controller);

        var result =
            await controller.Start(
                fixture.Tenant.PublicId,
                fixture.Project.PublicId,
                CancellationToken.None);

        Assert.IsType<UnauthorizedResult>(
            result.Result);

        Assert.Equal(
            ProjectStatus.Planning,
            fixture.Project.Status);

        Assert.Equal(
            0,
            fixture.UnitOfWork.SaveChangesCallCount);
    }

    [Fact]
    public async Task
    Start_ShouldReturnNotFound_WhenTenantDoesNotExist()
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
            await controller.Start(
                fixture.Tenant.PublicId,
                fixture.Project.PublicId,
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

        Assert.Equal(
            0,
            fixture.UnitOfWork.SaveChangesCallCount);
    }

    [Fact]
    public async Task
    Start_ShouldReturnForbidden_WhenRequesterCannotChangeProjectStatus()
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
            await controller.Start(
                fixture.Tenant.PublicId,
                fixture.Project.PublicId,
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
            ProjectErrors.StatusChangeNotAllowed.Code,
            response.Code);

        Assert.Equal(
            ProjectErrors.StatusChangeNotAllowed.Message,
            response.Message);

        Assert.Equal(
            ProjectStatus.Planning,
            fixture.Project.Status);

        Assert.Equal(
            0,
            fixture.UnitOfWork.SaveChangesCallCount);
    }

    [Fact]
    public async Task
    Start_ShouldReturnConflict_WhenTransitionIsInvalid()
    {
        var fixture =
            CreateFixture(
                UserRole.TenantAdmin);

        fixture.Project.Start();

        var controller =
            CreateController(
                fixture);

        SetAuthenticatedUser(
            controller,
            fixture.Requester.PublicId);

        var result =
            await controller.Start(
                fixture.Tenant.PublicId,
                fixture.Project.PublicId,
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
            ProjectErrors.InvalidStatusTransition.Code,
            response.Code);

        Assert.Equal(
            ProjectErrors.InvalidStatusTransition.Message,
            response.Message);

        Assert.Equal(
            ProjectStatus.InProgress,
            fixture.Project.Status);

        Assert.Equal(
            0,
            fixture.UnitOfWork.SaveChangesCallCount);
    }

    [Fact]
    public async Task
    Pause_ShouldReturnOk_WhenTenantAdminPausesInProgressProject()
    {
        var fixture =
            CreateFixture(
                UserRole.TenantAdmin);

        fixture.Project.Start();

        var controller =
            CreateController(
                fixture);

        SetAuthenticatedUser(
            controller,
            fixture.Requester.PublicId);

        var request =
            new PauseProjectRequest(
                "Aguardando retorno do cliente.");

        var result =
            await controller.Pause(
                fixture.Tenant.PublicId,
                fixture.Project.PublicId,
                request,
                CancellationToken.None);

        var okResult =
            Assert.IsType<OkObjectResult>(
                result.Result);

        var response =
            Assert.IsType<ProjectStatusResponse>(
                okResult.Value);

        Assert.Equal(
            StatusCodes.Status200OK,
            okResult.StatusCode);

        Assert.Equal(
            ProjectStatus.Paused,
            response.Status);

        Assert.Equal(
            ProjectStatus.Paused,
            fixture.Project.Status);

        Assert.Equal(
            fixture.Project.DueDate,
            response.DueDate);

        Assert.Equal(
            1,
            fixture.UnitOfWork.SaveChangesCallCount);
    }

    [Fact]
    public async Task
    Pause_ShouldReturnUnauthorized_WhenUserPublicIdClaimIsMissing()
    {
        var fixture =
            CreateFixture(
                UserRole.TenantAdmin);

        fixture.Project.Start();

        var controller =
            CreateController(
                fixture);

        SetUnauthenticatedUser(
            controller);

        var request =
            new PauseProjectRequest(
                "Aguardando retorno do cliente.");

        var result =
            await controller.Pause(
                fixture.Tenant.PublicId,
                fixture.Project.PublicId,
                request,
                CancellationToken.None);

        Assert.IsType<UnauthorizedResult>(
            result.Result);

        Assert.Equal(
            ProjectStatus.InProgress,
            fixture.Project.Status);

        Assert.Equal(
            0,
            fixture.UnitOfWork.SaveChangesCallCount);
    }

    [Fact]
    public async Task
    Resume_ShouldReturnOkAndChangeDueDate_WhenNewDueDateIsProvided()
    {
        var fixture =
            CreateFixture(
                UserRole.TenantAdmin);

        fixture.Project.Start();

        fixture.Project.Pause(
            "Aguardando retorno do cliente.");

        var newDueDate =
            new DateTime(
                2027,
                1,
                31,
                12,
                0,
                0,
                DateTimeKind.Utc);

        var controller =
            CreateController(
                fixture);

        SetAuthenticatedUser(
            controller,
            fixture.Requester.PublicId);

        var request =
            new ResumeProjectRequest(
                newDueDate);

        var result =
            await controller.Resume(
                fixture.Tenant.PublicId,
                fixture.Project.PublicId,
                request,
                CancellationToken.None);

        var okResult =
            Assert.IsType<OkObjectResult>(
                result.Result);

        var response =
            Assert.IsType<ProjectStatusResponse>(
                okResult.Value);

        Assert.Equal(
            StatusCodes.Status200OK,
            okResult.StatusCode);

        Assert.Equal(
            ProjectStatus.InProgress,
            response.Status);

        Assert.Equal(
            ProjectStatus.InProgress,
            fixture.Project.Status);

        Assert.Equal(
            newDueDate,
            response.DueDate);

        Assert.Equal(
            newDueDate,
            fixture.Project.DueDate);

        Assert.Equal(
            1,
            fixture.UnitOfWork.SaveChangesCallCount);
    }

    [Fact]
    public async Task
    Resume_ShouldReturnOkAndKeepDueDate_WhenNewDueDateIsNull()
    {
        var fixture =
            CreateFixture(
                UserRole.TenantAdmin);

        fixture.Project.Start();

        fixture.Project.Pause(
            "Aguardando retorno do cliente.");

        var originalDueDate =
            fixture.Project.DueDate;

        var controller =
            CreateController(
                fixture);

        SetAuthenticatedUser(
            controller,
            fixture.Requester.PublicId);

        var request =
            new ResumeProjectRequest(
                null);

        var result =
            await controller.Resume(
                fixture.Tenant.PublicId,
                fixture.Project.PublicId,
                request,
                CancellationToken.None);

        var okResult =
            Assert.IsType<OkObjectResult>(
                result.Result);

        var response =
            Assert.IsType<ProjectStatusResponse>(
                okResult.Value);

        Assert.Equal(
            ProjectStatus.InProgress,
            response.Status);

        Assert.Equal(
            originalDueDate,
            response.DueDate);

        Assert.Equal(
            originalDueDate,
            fixture.Project.DueDate);

        Assert.Equal(
            1,
            fixture.UnitOfWork.SaveChangesCallCount);
    }

    [Fact]
    public async Task
    Resume_ShouldReturnUnauthorized_WhenUserPublicIdClaimIsMissing()
    {
        var fixture =
            CreateFixture(
                UserRole.TenantAdmin);

        fixture.Project.Start();

        fixture.Project.Pause(
            "Aguardando retorno do cliente.");

        var controller =
            CreateController(
                fixture);

        SetUnauthenticatedUser(
            controller);

        var request =
            new ResumeProjectRequest(
                null);

        var result =
            await controller.Resume(
                fixture.Tenant.PublicId,
                fixture.Project.PublicId,
                request,
                CancellationToken.None);

        Assert.IsType<UnauthorizedResult>(
            result.Result);

        Assert.Equal(
            ProjectStatus.Paused,
            fixture.Project.Status);

        Assert.Equal(
            0,
            fixture.UnitOfWork.SaveChangesCallCount);
    }

    private static ProjectsController CreateController(
        Fixture fixture)
    {
        var createProjectHandler =
            new CreateProjectHandler(
                fixture.TenantRepository,
                fixture.UserRepository,
                fixture.ProjectRepository,
                fixture.ProjectMemberRepository,
                fixture.PermissionRepository,
                fixture.UnitOfWork);

        var getProjectByPublicIdHandler =
            new GetProjectByPublicIdHandler(
                fixture.TenantRepository,
                fixture.UserRepository,
                fixture.ProjectRepository,
                fixture.ProjectMemberRepository);

        var listProjectsHandler =
            new ListProjectsHandler(
                fixture.TenantRepository,
                fixture.UserRepository,
                fixture.ProjectRepository);

        var updateProjectHandler =
            new UpdateProjectHandler(
                fixture.TenantRepository,
                fixture.UserRepository,
                fixture.ProjectRepository,
                fixture.ProjectMemberRepository,
                fixture.PermissionRepository,
                fixture.UnitOfWork);

        var startProjectHandler =
            new StartProjectHandler(
                fixture.TenantRepository,
                fixture.UserRepository,
                fixture.ProjectRepository,
                fixture.ProjectMemberRepository,
                fixture.PermissionRepository,
                fixture.UnitOfWork);

        var pauseProjectHandler =
            new PauseProjectHandler(
                fixture.TenantRepository,
                fixture.UserRepository,
                fixture.ProjectRepository,
                fixture.ProjectMemberRepository,
                fixture.PermissionRepository,
                fixture.UnitOfWork);

        var resumeProjectHandler =
            new ResumeProjectHandler(
                fixture.TenantRepository,
                fixture.UserRepository,
                fixture.ProjectRepository,
                fixture.ProjectMemberRepository,
                fixture.PermissionRepository,
                fixture.UnitOfWork);

        return new ProjectsController(
            createProjectHandler,
            getProjectByPublicIdHandler,
            listProjectsHandler,
            updateProjectHandler,
            startProjectHandler,
            pauseProjectHandler,
            resumeProjectHandler);
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

    private static void SetUnauthenticatedUser(
        ProjectsController controller)
    {
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
    }

    private static Fixture CreateFixture(
        UserRole requesterRole)
    {
        var tenant =
            new Tenant(
                "Empresa Project Status API",
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

        var project =
            new Project(
                tenant.Id,
                "Projeto Status API",
                requester.Id,
                "Projeto utilizado nos testes de status.",
                dueDate:
                    new DateTime(
                        2026,
                        12,
                        31,
                        12,
                        0,
                        0,
                        DateTimeKind.Utc));

        EntityTestHelper.SetId(
            project,
            100);

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

        var projectMemberRepository =
            new FakeProjectMemberRepository();

        var permissionRepository =
            new FakeProjectMemberPermissionRepository();

        var unitOfWork =
            new FakeUnitOfWork();

        return new Fixture(
            tenant,
            requester,
            project,
            tenantRepository,
            userRepository,
            projectRepository,
            projectMemberRepository,
            permissionRepository,
            unitOfWork);
    }

    private sealed record Fixture(
        Tenant Tenant,
        User Requester,
        Project Project,
        FakeTenantRepository TenantRepository,
        FakeUserRepository UserRepository,
        FakeProjectRepository ProjectRepository,
        FakeProjectMemberRepository ProjectMemberRepository,
        FakeProjectMemberPermissionRepository PermissionRepository,
        FakeUnitOfWork UnitOfWork);
}