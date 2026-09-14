using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using WorkFlow.API.Contracts.Common;
using WorkFlow.API.Contracts.Projects;
using WorkFlow.API.Controllers;
using WorkFlow.Application.Projects;
using WorkFlow.Application.Projects.ArchiveProject;
using WorkFlow.Application.Projects.CompleteProject;
using WorkFlow.Application.Projects.CreateProject;
using WorkFlow.Application.Projects.GetProjectByPublicId;
using WorkFlow.Application.Projects.ListProjects;
using WorkFlow.Application.Projects.PauseProject;
using WorkFlow.Application.Projects.ReopenProject;
using WorkFlow.Application.Projects.RestoreProject;
using WorkFlow.Application.Projects.ResumeProject;
using WorkFlow.Application.Projects.StartProject;
using WorkFlow.Application.Projects.UpdateProject;
using WorkFlow.Domain.Entities;
using WorkFlow.Domain.Enums;
using WorkFlow.UnitTests.Application.Projects.Fakes;
using WorkFlow.UnitTests.Application.Tenants.Fakes;
using WorkFlow.UnitTests.Application.Users.Fakes;
using WorkFlow.UnitTests.Common;
using WorkFlow.UnitTests.Common.Fakes;

namespace WorkFlow.UnitTests.API.Projects;

public sealed class ProjectLifecycleControllerTests
{
    [Fact]
    public async Task
    Complete_ShouldReturnOk_WhenTenantAdminCompletesProject()
    {
        var fixture =
            CreateFixture();

        fixture.Project.Start();

        fixture.ProjectTaskRepository.StatusesToReturn =
            new[]
            {
                ProjectTaskStatus.Done,
                ProjectTaskStatus.Cancelled
            };

        var controller =
            CreateController(
                fixture);

        SetAuthenticatedUser(
            controller,
            fixture.Requester.PublicId);

        var result =
            await controller.Complete(
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
            ProjectStatus.Completed,
            response.Status);

        Assert.Equal(
            ProjectStatus.Completed,
            fixture.Project.Status);

        Assert.Equal(
            fixture.Project.Id,
            fixture.ProjectTaskRepository.CheckedProjectId);

        Assert.Equal(
            1,
            fixture.UnitOfWork.SaveChangesCallCount);
    }

    [Fact]
    public async Task
    Complete_ShouldReturnConflict_WhenProjectHasNoTasks()
    {
        var fixture =
            CreateFixture();

        fixture.Project.Start();

        fixture.ProjectTaskRepository.StatusesToReturn =
            Array.Empty<ProjectTaskStatus>();

        var controller =
            CreateController(
                fixture);

        SetAuthenticatedUser(
            controller,
            fixture.Requester.PublicId);

        var result =
            await controller.Complete(
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
            ProjectErrors.HasNoTasks.Code,
            response.Code);

        Assert.Equal(
            ProjectErrors.HasNoTasks.Message,
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
    Complete_ShouldReturnConflict_WhenProjectHasOpenTasks()
    {
        var fixture =
            CreateFixture();

        fixture.Project.Start();

        fixture.ProjectTaskRepository.StatusesToReturn =
            new[]
            {
                ProjectTaskStatus.Done,
                ProjectTaskStatus.InProgress
            };

        var controller =
            CreateController(
                fixture);

        SetAuthenticatedUser(
            controller,
            fixture.Requester.PublicId);

        var result =
            await controller.Complete(
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
            ProjectErrors.HasOpenTasks.Code,
            response.Code);

        Assert.Equal(
            ProjectErrors.HasOpenTasks.Message,
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
    Reopen_ShouldReturnOk_WhenTenantAdminReopensCompletedProject()
    {
        var fixture =
            CreateFixture();

        fixture.Project.Start();

        fixture.Project.Complete(
            new[]
            {
                ProjectTaskStatus.Done
            });

        var controller =
            CreateController(
                fixture);

        SetAuthenticatedUser(
            controller,
            fixture.Requester.PublicId);

        var request =
            new ReopenProjectRequest(
                "Novos ajustes foram solicitados.");

        var result =
            await controller.Reopen(
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
            1,
            fixture.UnitOfWork.SaveChangesCallCount);
    }

    [Fact]
    public async Task
    Archive_ShouldReturnOk_WhenTenantAdminArchivesPlanningProject()
    {
        var fixture =
            CreateFixture();

        var controller =
            CreateController(
                fixture);

        SetAuthenticatedUser(
            controller,
            fixture.Requester.PublicId);

        var result =
            await controller.Archive(
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
            ProjectStatus.Archived,
            response.Status);

        Assert.Equal(
            ProjectStatus.Archived,
            fixture.Project.Status);

        Assert.NotNull(
            response.ArchivedAt);

        Assert.True(
            fixture.Project.IsArchived);

        Assert.Equal(
            1,
            fixture.UnitOfWork.SaveChangesCallCount);
    }

    [Fact]
    public async Task
    Restore_ShouldReturnOk_WhenTenantAdminRestoresArchivedProject()
    {
        var fixture =
            CreateFixture();

        fixture.Project.Archive();

        var controller =
            CreateController(
                fixture);

        SetAuthenticatedUser(
            controller,
            fixture.Requester.PublicId);

        var result =
            await controller.Restore(
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
            ProjectStatus.Planning,
            response.Status);

        Assert.Equal(
            ProjectStatus.Planning,
            fixture.Project.Status);

        Assert.Null(
            response.ArchivedAt);

        Assert.False(
            fixture.Project.IsArchived);

        Assert.Equal(
            1,
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

        var completeProjectHandler =
            new CompleteProjectHandler(
                fixture.TenantRepository,
                fixture.UserRepository,
                fixture.ProjectRepository,
                fixture.ProjectMemberRepository,
                fixture.PermissionRepository,
                fixture.ProjectTaskRepository,
                fixture.UnitOfWork);

        var reopenProjectHandler =
            new ReopenProjectHandler(
                fixture.TenantRepository,
                fixture.UserRepository,
                fixture.ProjectRepository,
                fixture.ProjectMemberRepository,
                fixture.PermissionRepository,
                fixture.UnitOfWork);

        var archiveProjectHandler =
            new ArchiveProjectHandler(
                fixture.TenantRepository,
                fixture.UserRepository,
                fixture.ProjectRepository,
                fixture.ProjectMemberRepository,
                fixture.PermissionRepository,
                fixture.UnitOfWork);

        var restoreProjectHandler =
            new RestoreProjectHandler(
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
            resumeProjectHandler,
            completeProjectHandler,
            reopenProjectHandler,
            archiveProjectHandler,
            restoreProjectHandler);
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

    private static Fixture CreateFixture()
    {
        var tenant =
            new Tenant(
                "Empresa Project Lifecycle API",
                $"REG-{Guid.NewGuid():N}",
                $"tenant-{Guid.NewGuid():N}@test.local");

        EntityTestHelper.SetId(
            tenant,
            42);

        var requester =
            new User(
                tenant.Id,
                "Administrador",
                $"admin-{Guid.NewGuid():N}@test.local",
                "password-hash",
                UserRole.TenantAdmin);

        EntityTestHelper.SetId(
            requester,
            10);

        var project =
            new Project(
                tenant.Id,
                "Projeto Lifecycle API",
                requester.Id,
                "Projeto utilizado nos testes de lifecycle.",
                dueDate:
                    new DateTime(
                        2027,
                        1,
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

        var projectTaskRepository =
            new FakeProjectTaskRepository();

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
            projectTaskRepository,
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
        FakeProjectTaskRepository ProjectTaskRepository,
        FakeUnitOfWork UnitOfWork);
}