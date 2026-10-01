using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;
using System.Reflection;
using System.Security.Claims;
using System.Text.Json;
using WorkFlow.API.Authorization;
using WorkFlow.API.Contracts.Common;
using WorkFlow.API.Contracts.ProjectTasks;
using WorkFlow.API.Controllers;
using WorkFlow.API.Exceptions;
using WorkFlow.Application.Common.Errors;
using WorkFlow.Application.Projects;
using WorkFlow.Application.ProjectTasks;
using WorkFlow.Application.ProjectTasks.AssignProjectTaskResponsible;
using WorkFlow.Application.ProjectTasks.ClaimProjectTask;
using WorkFlow.Application.ProjectTasks.CreateProjectTask;
using WorkFlow.Application.ProjectTasks.ListProjectTasks;
using WorkFlow.Application.ProjectTasks.MoveProjectTaskToTodo;
using WorkFlow.Application.ProjectTasks.PauseProjectTask;
using WorkFlow.Application.ProjectTasks.RemoveProjectTaskResponsible;
using WorkFlow.Application.ProjectTasks.ResumeProjectTask;
using WorkFlow.Application.ProjectTasks.SendProjectTaskToValidation;
using WorkFlow.Application.ProjectTasks.StartProjectTask;
using WorkFlow.Application.ProjectTasks.UpdateProjectTask;
using WorkFlow.Application.Tenants;
using WorkFlow.Application.Users;
using WorkFlow.Domain.Entities;
using WorkFlow.Domain.Enums;
using WorkFlow.UnitTests.Application.Projects.Fakes;
using WorkFlow.UnitTests.Application.Tenants.Fakes;
using WorkFlow.UnitTests.Application.Users.Fakes;
using WorkFlow.UnitTests.Common;
using WorkFlow.UnitTests.Common.Fakes;

namespace WorkFlow.UnitTests.API.ProjectTasks;

public sealed class PauseProjectTaskControllerTests
{
    private static readonly DateTime DueDate =
        new(
            2027,
            6,
            30,
            18,
            0,
            0,
            DateTimeKind.Utc);

    [Theory]
    [InlineData(
        UserRole.TenantAdmin,
        ProjectTaskStatus.Todo)]
    [InlineData(
        UserRole.TenantAdmin,
        ProjectTaskStatus.InProgress)]
    [InlineData(
        UserRole.ProjectManager,
        ProjectTaskStatus.Todo)]
    [InlineData(
        UserRole.ProjectManager,
        ProjectTaskStatus.InProgress)]
    [InlineData(
        UserRole.Member,
        ProjectTaskStatus.Todo)]
    [InlineData(
        UserRole.Member,
        ProjectTaskStatus.InProgress)]
    public async Task
        Pause_ShouldReturnOk_WhenRequesterIsResponsibleAndActiveMember(
            UserRole role,
            ProjectTaskStatus taskStatus)
    {
        var fixture =
            CreateFixture(
                role,
                taskStatus);

        var controller =
            CreateController(
                fixture,
                fixture.Requester.PublicId.ToString());

        var request =
            new PauseProjectTaskRequest(
                "Aguardando retorno do cliente.");

        var result =
            await controller.Pause(
                fixture.Tenant.PublicId,
                fixture.Project.PublicId,
                fixture.ProjectTask.PublicId,
                request,
                CancellationToken.None);

        var ok =
            Assert.IsType<OkObjectResult>(
                result.Result);

        Assert.Equal(
            StatusCodes.Status200OK,
            ok.StatusCode);

        var response =
            Assert.IsType<PauseProjectTaskResponse>(
                ok.Value);

        Assert.Equal(
            fixture.ProjectTask.PublicId,
            response.PublicId);

        Assert.Equal(
            fixture.Tenant.PublicId,
            response.TenantPublicId);

        Assert.Equal(
            fixture.Project.PublicId,
            response.ProjectPublicId);

        Assert.Equal(
            fixture.Requester.PublicId,
            response.ResponsibleUserPublicId);

        Assert.Equal(
            ProjectTaskStatus.Paused,
            response.Status);

        Assert.Equal(
            taskStatus,
            response.StatusBeforePause);

        Assert.Equal(
            DueDate,
            response.DueDate);

        Assert.Equal(
            ProjectTaskStatus.Paused,
            fixture.ProjectTask.Status);

        Assert.Equal(
            taskStatus,
            fixture.ProjectTask.StatusBeforePause);

        Assert.Equal(
            fixture.Requester.Id,
            fixture.ProjectTask.ResponsibleUserId);

        Assert.NotNull(
            response.UpdatedAt);

        Assert.Equal(
            1,
            fixture.UnitOfWork.BeginTransactionCallCount);

        Assert.Equal(
            1,
            fixture.UnitOfWork.SaveChangesCallCount);

        Assert.Equal(
            1,
            fixture.UnitOfWork.CommitCallCount);

        Assert.Equal(
            0,
            fixture.UnitOfWork.RollbackCallCount);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("invalid")]
    [InlineData("00000000-0000-0000-0000-000000000000")]
    public async Task
        Pause_ShouldReturnUnauthorized_WhenUserClaimIsInvalid(
            string? claim)
    {
        var fixture =
            CreateFixture();

        var controller =
            CreateController(
                fixture,
                claim);

        var result =
            await controller.Pause(
                fixture.Tenant.PublicId,
                fixture.Project.PublicId,
                fixture.ProjectTask.PublicId,
                new PauseProjectTaskRequest(
                    "Motivo da pausa."),
                CancellationToken.None);

        Assert.IsType<UnauthorizedResult>(
            result.Result);

        Assert.Equal(
            ProjectTaskStatus.InProgress,
            fixture.ProjectTask.Status);

        Assert.Null(
            fixture.ProjectTask.StatusBeforePause);

        Assert.Equal(
            0,
            fixture.UnitOfWork.BeginTransactionCallCount);

        Assert.Equal(
            0,
            fixture.UnitOfWork.SaveChangesCallCount);
    }

    [Theory]
    [InlineData(
        "tenant-missing",
        StatusCodes.Status404NotFound)]
    [InlineData(
        "requester-missing",
        StatusCodes.Status404NotFound)]
    [InlineData(
        "project-missing",
        StatusCodes.Status404NotFound)]
    [InlineData(
        "task-missing",
        StatusCodes.Status404NotFound)]
    [InlineData(
        "tenant-inactive",
        StatusCodes.Status409Conflict)]
    [InlineData(
        "requester-inactive",
        StatusCodes.Status403Forbidden)]
    [InlineData(
        "project-paused",
        StatusCodes.Status409Conflict)]
    [InlineData(
        "task-archived",
        StatusCodes.Status409Conflict)]
    [InlineData(
        "task-not-pausable",
        StatusCodes.Status409Conflict)]
    [InlineData(
        "without-responsible",
        StatusCodes.Status409Conflict)]
    [InlineData(
        "not-responsible",
        StatusCodes.Status403Forbidden)]
    [InlineData(
        "not-active-member",
        StatusCodes.Status403Forbidden)]
    public async Task Pause_ShouldMapApplicationErrors(
        string scenario,
        int expectedStatusCode)
    {
        var fixture =
            CreateFixture();

        var expectedError =
            ConfigureFailureScenario(
                fixture,
                scenario);

        var controller =
            CreateController(
                fixture,
                fixture.Requester.PublicId.ToString());

        var result =
            await controller.Pause(
                fixture.Tenant.PublicId,
                fixture.Project.PublicId,
                fixture.ProjectTask.PublicId,
                new PauseProjectTaskRequest(
                    "Motivo da pausa."),
                CancellationToken.None);

        var failure =
            Assert.IsAssignableFrom<ObjectResult>(
                result.Result);

        Assert.Equal(
            expectedStatusCode,
            failure.StatusCode);

        var response =
            Assert.IsType<ErrorResponse>(
                failure.Value);

        Assert.Equal(
            expectedError.Code,
            response.Code);

        Assert.Equal(
            expectedError.Message,
            response.Message);

        Assert.Equal(
            0,
            fixture.UnitOfWork.SaveChangesCallCount);
    }

    [Fact]
    public async Task
        Pause_ShouldUseGlobalValidationHandling_WhenReasonIsInvalid()
    {
        var fixture =
            CreateFixture();

        var controller =
            CreateController(
                fixture,
                fixture.Requester.PublicId.ToString());

        var exception =
            await Assert.ThrowsAsync<ArgumentException>(
                () =>
                    controller.Pause(
                        fixture.Tenant.PublicId,
                        fixture.Project.PublicId,
                        fixture.ProjectTask.PublicId,
                        new PauseProjectTaskRequest(
                            "   "),
                        CancellationToken.None));

        var context =
            new DefaultHttpContext();

        await using var body =
            new MemoryStream();

        context.Response.Body =
            body;

        var exceptionHandler =
            new GlobalExceptionHandler(
                NullLogger<GlobalExceptionHandler>.Instance);

        var handled =
            await exceptionHandler.TryHandleAsync(
                context,
                exception,
                CancellationToken.None);

        Assert.True(
            handled);

        Assert.Equal(
            StatusCodes.Status400BadRequest,
            context.Response.StatusCode);

        body.Position =
            0;

        var error =
            await JsonSerializer
                .DeserializeAsync<ErrorResponse>(
                    body,
                    new JsonSerializerOptions(
                        JsonSerializerDefaults.Web));

        Assert.NotNull(
            error);

        Assert.Equal(
            "Validation.InvalidArgument",
            error!.Code);

        Assert.Equal(
            0,
            fixture.UnitOfWork.BeginTransactionCallCount);

        Assert.Equal(
            ProjectTaskStatus.InProgress,
            fixture.ProjectTask.Status);
    }

    [Fact]
    public void
        Pause_ShouldExposeTenantScopedPostProtectedByTenantAccess()
    {
        var controllerType =
            typeof(ProjectTasksController);

        Assert.NotNull(
            controllerType
                .GetCustomAttribute<ApiControllerAttribute>());

        Assert.Equal(
            "api/tenants/{tenantPublicId:guid}/projects/{projectPublicId:guid}/tasks",
            controllerType
                .GetCustomAttribute<RouteAttribute>()!
                .Template);

        var method =
            controllerType.GetMethod(
                nameof(ProjectTasksController.Pause));

        Assert.NotNull(
            method);

        var postAttribute =
            Assert.Single(
                method!
                    .GetCustomAttributes<HttpPostAttribute>());

        Assert.Equal(
            "{taskPublicId:guid}/pause",
            postAttribute.Template);

        Assert.Equal(
            AuthorizationPolicyNames.TenantAccess,
            Assert.Single(
                    method
                        .GetCustomAttributes<AuthorizeAttribute>())
                .Policy);

        Assert.Empty(
            method.GetCustomAttributes<AllowAnonymousAttribute>());

        var bodyParameter =
            Assert.Single(
                method.GetParameters(),
                parameter =>
                    parameter.GetCustomAttribute<FromBodyAttribute>()
                        is not null);

        Assert.Equal(
            typeof(PauseProjectTaskRequest),
            bodyParameter.ParameterType);
    }

    private static Error ConfigureFailureScenario(
        Fixture fixture,
        string scenario)
    {
        switch (scenario)
        {
            case "tenant-missing":
                fixture.TenantRepository.TenantToReturn =
                    null;

                return TenantErrors.NotFound;

            case "requester-missing":
                fixture.UserRepository
                    .UsersByPublicIdToReturn
                    .Remove(
                        fixture.Requester.PublicId);

                return UserErrors.NotFound;

            case "project-missing":
                fixture.ProjectRepository.ProjectToReturn =
                    null;

                return ProjectErrors.NotFound;

            case "task-missing":
                fixture.TaskRepository
                    .ProjectTaskForUpdateToReturn =
                    null;

                return ProjectTaskErrors.NotFound;

            case "tenant-inactive":
                fixture.Tenant.Deactivate();

                return TenantErrors.Inactive;

            case "requester-inactive":
                fixture.Requester.Deactivate();

                return UserErrors.Inactive;

            case "project-paused":
                fixture.Project.Pause(
                    "Projeto pausado para teste.");

                return ProjectTaskErrors
                    .PauseBlockedByProjectStatus;

            case "task-archived":
                fixture.ProjectTask.Cancel();
                fixture.ProjectTask.Archive();

                return ProjectTaskErrors.Archived;

            case "task-not-pausable":
                fixture.ProjectTask.SendToValidation();

                return ProjectTaskErrors
                    .PauseBlockedByTaskStatus;

            case "without-responsible":
                fixture.ProjectTask.RemoveResponsible();

                return ProjectTaskErrors
                    .PauseRequiresResponsible;

            case "not-responsible":
                fixture.ProjectTask.AssignResponsible(
                    fixture.OtherUser.Id);

                return ProjectTaskErrors
                    .PauseNotAllowed;

            case "not-active-member":
                fixture.MemberRepository
                    .IsActiveMemberResults[
                        (
                            fixture.Project.Id,
                            fixture.Requester.Id
                        )] =
                    false;

                return ProjectTaskErrors
                    .PauseNotAllowed;

            default:
                throw new ArgumentOutOfRangeException(
                    nameof(scenario));
        }
    }

    private static ProjectTasksController CreateController(
        Fixture fixture,
        string? claim)
    {
        var claims =
            claim is null
                ? Array.Empty<Claim>()
                : new[]
                {
                    new Claim(
                        ClaimTypes.NameIdentifier,
                        claim)
                };

        var createHandler =
            new CreateProjectTaskHandler(
                fixture.TenantRepository,
                fixture.UserRepository,
                fixture.ProjectRepository,
                fixture.MemberRepository,
                fixture.PermissionRepository,
                fixture.TaskRepository,
                fixture.UnitOfWork);

        var assignHandler =
            new AssignProjectTaskResponsibleHandler(
                fixture.TenantRepository,
                fixture.UserRepository,
                fixture.ProjectRepository,
                fixture.MemberRepository,
                fixture.PermissionRepository,
                fixture.TaskRepository,
                fixture.UnitOfWork);

        var claimHandler =
            new ClaimProjectTaskHandler(
                fixture.TenantRepository,
                fixture.UserRepository,
                fixture.ProjectRepository,
                fixture.MemberRepository,
                fixture.PermissionRepository,
                fixture.TaskRepository,
                fixture.UnitOfWork);

        var removeHandler =
            new RemoveProjectTaskResponsibleHandler(
                fixture.TenantRepository,
                fixture.UserRepository,
                fixture.ProjectRepository,
                fixture.MemberRepository,
                fixture.PermissionRepository,
                fixture.TaskRepository,
                fixture.UnitOfWork);

        var updateHandler =
            new UpdateProjectTaskHandler(
                fixture.TenantRepository,
                fixture.UserRepository,
                fixture.ProjectRepository,
                fixture.MemberRepository,
                fixture.PermissionRepository,
                fixture.TaskRepository,
                fixture.UnitOfWork);

        var listHandler =
            new ListProjectTasksHandler(
                fixture.TenantRepository,
                fixture.UserRepository,
                fixture.ProjectRepository,
                fixture.MemberRepository,
                fixture.TaskRepository);

        var startHandler =
            new StartProjectTaskHandler(
                fixture.TenantRepository,
                fixture.UserRepository,
                fixture.ProjectRepository,
                fixture.MemberRepository,
                fixture.TaskRepository,
                fixture.UnitOfWork);

        var moveToTodoHandler =
            new MoveProjectTaskToTodoHandler(
                fixture.TenantRepository,
                fixture.UserRepository,
                fixture.ProjectRepository,
                fixture.MemberRepository,
                fixture.PermissionRepository,
                fixture.TaskRepository,
                fixture.UnitOfWork);

        var pauseHandler =
            new PauseProjectTaskHandler(
                fixture.TenantRepository,
                fixture.UserRepository,
                fixture.ProjectRepository,
                fixture.MemberRepository,
                fixture.TaskRepository,
                fixture.UnitOfWork);

        var resumeHandler =
            new ResumeProjectTaskHandler(
                fixture.TenantRepository,
                fixture.UserRepository,
                fixture.ProjectRepository,
                fixture.MemberRepository,
                fixture.TaskRepository,
                fixture.UnitOfWork);

        var sendProjectTaskToValidationHandler =
            new SendProjectTaskToValidationHandler(
                fixture.TenantRepository,
                fixture.UserRepository,
                fixture.ProjectRepository,
                fixture.MemberRepository,
                fixture.TaskRepository,
                fixture.UnitOfWork);

        return new ProjectTasksController(
            createHandler,
            assignHandler,
            claimHandler,
            removeHandler,
            updateHandler,
            listHandler,
            startHandler,
            moveToTodoHandler,
            pauseHandler,
            resumeHandler,
            sendProjectTaskToValidationHandler)
        {
            ControllerContext =
                new ControllerContext
                {
                    HttpContext =
                        new DefaultHttpContext
                        {
                            User =
                                new ClaimsPrincipal(
                                    new ClaimsIdentity(
                                        claims,
                                        "Test"))
                        }
                }
        };
    }

    private static Fixture CreateFixture(
        UserRole requesterRole =
            UserRole.Member,
        ProjectTaskStatus taskStatus =
            ProjectTaskStatus.InProgress)
    {
        var tenant =
            new Tenant(
                "Empresa Pause Task API",
                $"REG-{Guid.NewGuid():N}",
                $"tenant-{Guid.NewGuid():N}@test.local");

        EntityTestHelper.SetId(
            tenant,
            42);

        var requester =
            new User(
                tenant.Id,
                "Usuário responsável",
                $"requester-{Guid.NewGuid():N}@test.local",
                "password-hash",
                requesterRole);

        EntityTestHelper.SetId(
            requester,
            10);

        var otherUser =
            new User(
                tenant.Id,
                "Outro usuário",
                $"other-{Guid.NewGuid():N}@test.local",
                "password-hash",
                UserRole.Member);

        EntityTestHelper.SetId(
            otherUser,
            20);

        var project =
            new Project(
                tenant.Id,
                "Projeto Pause Task API",
                requester.Id);

        EntityTestHelper.SetId(
            project,
            100);

        project.Start();

        var projectTask =
            new ProjectTask(
                project.Id,
                "Tarefa para pausar",
                ProjectTaskPriority.Medium,
                requester.Id,
                responsibleUserId:
                    requester.Id,
                dueDate:
                    DueDate);

        EntityTestHelper.SetId(
            projectTask,
            200);

        projectTask.MoveToTodo();

        if (taskStatus ==
            ProjectTaskStatus.InProgress)
        {
            projectTask.Start();
        }
        else if (taskStatus !=
                 ProjectTaskStatus.Todo)
        {
            throw new ArgumentOutOfRangeException(
                nameof(taskStatus));
        }

        var tenantRepository =
            new FakeTenantRepository
            {
                TenantToReturn =
                    tenant
            };

        var userRepository =
            new FakeUserRepository();

        userRepository
            .UsersByPublicIdToReturn[
                requester.PublicId] =
            requester;

        userRepository
            .UsersByPublicIdToReturn[
                otherUser.PublicId] =
            otherUser;

        var projectRepository =
            new FakeProjectRepository
            {
                ProjectToReturn =
                    project
            };

        var memberRepository =
            new FakeProjectMemberRepository();

        var requesterMember =
            new ProjectMember(
                project.Id,
                requester.Id,
                requester.Id);

        EntityTestHelper.SetId(
            requesterMember,
            300);

        memberRepository
            .ActiveMembersToReturn[
                (
                    project.Id,
                    requester.Id
                )] =
            requesterMember;

        memberRepository
            .IsActiveMemberResults[
                (
                    project.Id,
                    requester.Id
                )] =
            true;

        var permissionRepository =
            new FakeProjectMemberPermissionRepository();

        var taskRepository =
            new FakeProjectTaskRepository
            {
                ProjectTaskForUpdateToReturn =
                    projectTask
            };

        var unitOfWork =
            new FakeUnitOfWork();

        return new Fixture(
            tenant,
            requester,
            otherUser,
            project,
            projectTask,
            tenantRepository,
            userRepository,
            projectRepository,
            memberRepository,
            permissionRepository,
            taskRepository,
            unitOfWork);
    }

    private sealed record Fixture(
        Tenant Tenant,
        User Requester,
        User OtherUser,
        Project Project,
        ProjectTask ProjectTask,
        FakeTenantRepository TenantRepository,
        FakeUserRepository UserRepository,
        FakeProjectRepository ProjectRepository,
        FakeProjectMemberRepository MemberRepository,
        FakeProjectMemberPermissionRepository PermissionRepository,
        FakeProjectTaskRepository TaskRepository,
        FakeUnitOfWork UnitOfWork);
}