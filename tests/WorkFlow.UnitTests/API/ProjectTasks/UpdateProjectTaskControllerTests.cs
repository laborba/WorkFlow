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
using WorkFlow.Application.ProjectTasks.RemoveProjectTaskResponsible;
using WorkFlow.Application.ProjectTasks.UpdateProjectTask;
using WorkFlow.Application.ProjectTasks.StartProjectTask;
using WorkFlow.Application.ProjectTasks.MoveProjectTaskToTodo;
using WorkFlow.Application.ProjectTasks.PauseProjectTask;
using WorkFlow.Application.ProjectTasks.ResumeProjectTask;
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

public sealed class UpdateProjectTaskControllerTests
{
    [Fact]
    public async Task Update_ShouldReturnOk_WhenUpdateIsValid()
    {
        var fixture = CreateFixture();

        var originalCreatedAt =
            fixture.ProjectTask.CreatedAt;

        var originalCreatedByUserId =
            fixture.ProjectTask.CreatedByUserId;

        var originalStatus =
            fixture.ProjectTask.Status;

        var controller =
            CreateController(
                fixture,
                fixture.Requester.PublicId.ToString());

        var dueDate =
            new DateTime(
                2027, 2, 1, 12, 0, 0,
                DateTimeKind.Utc);

        var request =
            new UpdateProjectTaskRequest(
                "  Novo título  ",
                "  Nova descrição  ",
                ProjectTaskPriority.High,
                dueDate);

        var result =
            await controller.Update(
                fixture.Tenant.PublicId,
                fixture.Project.PublicId,
                fixture.ProjectTask.PublicId,
                request,
                CancellationToken.None);

        var okResult =
            Assert.IsType<OkObjectResult>(
                result.Result);

        Assert.Equal(
            StatusCodes.Status200OK,
            okResult.StatusCode);

        var response =
            Assert.IsType<UpdateProjectTaskResponse>(
                okResult.Value);

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
            "Novo título",
            response.Title);

        Assert.Equal(
            "Nova descrição",
            response.Description);

        Assert.Equal(
            ProjectTaskPriority.High,
            response.Priority);

        Assert.Equal(
            dueDate,
            response.DueDate);

        Assert.Equal(
            originalStatus,
            response.Status);

        Assert.NotNull(
            response.UpdatedAt);

        Assert.Equal(
            originalCreatedAt,
            fixture.ProjectTask.CreatedAt);

        Assert.Equal(
            originalCreatedByUserId,
            fixture.ProjectTask.CreatedByUserId);

        Assert.Null(
            fixture.ProjectTask.ResponsibleUserId);

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
    [InlineData(UserRole.ProjectManager)]
    [InlineData(UserRole.Member)]
    public async Task Update_ShouldReturnOk_WhenRequesterHasEditTaskPermission(
        UserRole role)
    {
        var fixture =
            CreateFixture(role);

        var controller =
            CreateController(
                fixture,
                fixture.Requester.PublicId.ToString());

        var result =
            await controller.Update(
                fixture.Tenant.PublicId,
                fixture.Project.PublicId,
                fixture.ProjectTask.PublicId,
                CreateRequest(),
                CancellationToken.None);

        var okResult =
            Assert.IsType<OkObjectResult>(
                result.Result);

        Assert.Equal(
            StatusCodes.Status200OK,
            okResult.StatusCode);

        Assert.Equal(
            1,
            fixture.UnitOfWork.SaveChangesCallCount);

        Assert.Equal(
            1,
            fixture.UnitOfWork.CommitCallCount);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("invalid")]
    [InlineData("00000000-0000-0000-0000-000000000000")]
    public async Task Update_ShouldReturnUnauthorized_WhenUserClaimIsInvalid(
        string? claim)
    {
        var fixture =
            CreateFixture();

        var controller =
            CreateController(
                fixture,
                claim);

        var result =
            await controller.Update(
                fixture.Tenant.PublicId,
                fixture.Project.PublicId,
                fixture.ProjectTask.PublicId,
                CreateRequest(),
                CancellationToken.None);

        Assert.IsType<UnauthorizedResult>(
            result.Result);

        Assert.Equal(
            0,
            fixture.UnitOfWork.BeginTransactionCallCount);

        Assert.Equal(
            0,
            fixture.UnitOfWork.SaveChangesCallCount);

        Assert.Equal(
            "Tarefa original",
            fixture.ProjectTask.Title);
    }

    [Theory]
    [InlineData("tenant-missing", StatusCodes.Status404NotFound)]
    [InlineData("requester-missing", StatusCodes.Status404NotFound)]
    [InlineData("project-missing", StatusCodes.Status404NotFound)]
    [InlineData("task-missing", StatusCodes.Status404NotFound)]
    [InlineData("tenant-inactive", StatusCodes.Status409Conflict)]
    [InlineData("requester-inactive", StatusCodes.Status403Forbidden)]
    [InlineData("archived-project", StatusCodes.Status409Conflict)]
    [InlineData("task-archived", StatusCodes.Status409Conflict)]
    [InlineData("no-membership", StatusCodes.Status403Forbidden)]
    [InlineData("no-permission", StatusCodes.Status403Forbidden)]
    public async Task Update_ShouldMapApplicationErrors(
        string scenario,
        int expectedStatusCode)
    {
        var fixture =
            scenario is "no-membership" or "no-permission"
                ? CreateFixture(
                    UserRole.Member,
                    grantEditTask: scenario != "no-permission")
                : CreateFixture();

        var expectedError =
            ConfigureFailureScenario(
                fixture,
                scenario);

        var controller =
            CreateController(
                fixture,
                fixture.Requester.PublicId.ToString());

        var result =
            await controller.Update(
                fixture.Tenant.PublicId,
                fixture.Project.PublicId,
                fixture.ProjectTask.PublicId,
                CreateRequest(),
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

    [Theory]
    [InlineData("", 2)]
    [InlineData("   ", 2)]
    [InlineData("Tarefa válida", 0)]
    [InlineData("Tarefa válida", 99)]
    public async Task Update_ShouldUseGlobalValidationHandling_ForInvalidInput(
        string title,
        int priority)
    {
        var fixture =
            CreateFixture();

        var controller =
            CreateController(
                fixture,
                fixture.Requester.PublicId.ToString());

        var request =
            CreateRequest() with
            {
                Title = title,
                Priority = (ProjectTaskPriority)priority
            };

        var exception =
            await Assert.ThrowsAsync<ArgumentException>(
                () => controller.Update(
                    fixture.Tenant.PublicId,
                    fixture.Project.PublicId,
                    fixture.ProjectTask.PublicId,
                    request,
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

        Assert.True(
            await exceptionHandler.TryHandleAsync(
                context,
                exception,
                CancellationToken.None));

        Assert.Equal(
            StatusCodes.Status400BadRequest,
            context.Response.StatusCode);

        body.Position = 0;

        var error =
            await JsonSerializer.DeserializeAsync<ErrorResponse>(
                body,
                new JsonSerializerOptions(
                    JsonSerializerDefaults.Web));

        Assert.Equal(
            "Validation.InvalidArgument",
            error!.Code);

        Assert.Equal(
            0,
            fixture.UnitOfWork.SaveChangesCallCount);
    }

    [Fact]
    public async Task Update_ShouldIgnoreUneditableFieldsFromJson()
    {
        var fixture =
            CreateFixture();

        var originalCreatedByUserId =
            fixture.ProjectTask.CreatedByUserId;

        var originalStatus =
            fixture.ProjectTask.Status;

        var json =
            """
            {
              "title": "Título atualizado",
              "description": "Descrição atualizada",
              "priority": 2,
              "dueDate": null,
              "status": 6,
              "responsibleUserId": 999,
              "responsibleUserPublicId": "11111111-1111-1111-1111-111111111111",
              "validatorUserId": 999,
              "createdByUserId": 999
            }
            """;

        var request =
            JsonSerializer.Deserialize<UpdateProjectTaskRequest>(
                json,
                new JsonSerializerOptions(
                    JsonSerializerDefaults.Web))!;

        var controller =
            CreateController(
                fixture,
                fixture.Requester.PublicId.ToString());

        var result =
            await controller.Update(
                fixture.Tenant.PublicId,
                fixture.Project.PublicId,
                fixture.ProjectTask.PublicId,
                request,
                CancellationToken.None);

        var okResult =
            Assert.IsType<OkObjectResult>(
                result.Result);

        var response =
            Assert.IsType<UpdateProjectTaskResponse>(
                okResult.Value);

        Assert.Equal(
            originalStatus,
            response.Status);

        Assert.Equal(
            originalStatus,
            fixture.ProjectTask.Status);

        Assert.Equal(
            originalCreatedByUserId,
            fixture.ProjectTask.CreatedByUserId);

        Assert.Null(
            fixture.ProjectTask.ResponsibleUserId);

        Assert.Equal(
            "Título atualizado",
            response.Title);

        Assert.Equal(
            1,
            fixture.UnitOfWork.SaveChangesCallCount);
    }

    [Fact]
    public async Task Update_ShouldNormalizeOffsetDueDateToUtc()
    {
        var fixture =
            CreateFixture();

        var json =
            """
            {
              "title": "Tarefa com prazo",
              "description": "Prazo com offset",
              "priority": 2,
              "dueDate": "2027-01-31T18:00:00-03:00"
            }
            """;

        var request =
            JsonSerializer.Deserialize<UpdateProjectTaskRequest>(
                json,
                new JsonSerializerOptions(
                    JsonSerializerDefaults.Web))!;

        var controller =
            CreateController(
                fixture,
                fixture.Requester.PublicId.ToString());

        var result =
            await controller.Update(
                fixture.Tenant.PublicId,
                fixture.Project.PublicId,
                fixture.ProjectTask.PublicId,
                request,
                CancellationToken.None);

        var response =
            Assert.IsType<UpdateProjectTaskResponse>(
                Assert.IsType<OkObjectResult>(
                    result.Result).Value);

        var expectedUtc =
            new DateTime(
                2027, 1, 31, 21, 0, 0,
                DateTimeKind.Utc);

        Assert.Equal(
            expectedUtc,
            response.DueDate);

        Assert.Equal(
            DateTimeKind.Utc,
            response.DueDate!.Value.Kind);

        Assert.Equal(
            expectedUtc,
            fixture.ProjectTask.DueDate);
    }

    [Fact]
    public async Task Update_ShouldClearDueDateAndNormalizeBlankDescription()
    {
        var fixture =
            CreateFixture();

        fixture.ProjectTask.ChangeDueDate(
            new DateTime(
                2027, 3, 1, 12, 0, 0,
                DateTimeKind.Utc));

        var request =
            new UpdateProjectTaskRequest(
                "Tarefa atualizada",
                "   ",
                ProjectTaskPriority.Medium,
                null);

        var controller =
            CreateController(
                fixture,
                fixture.Requester.PublicId.ToString());

        var result =
            await controller.Update(
                fixture.Tenant.PublicId,
                fixture.Project.PublicId,
                fixture.ProjectTask.PublicId,
                request,
                CancellationToken.None);

        var response =
            Assert.IsType<UpdateProjectTaskResponse>(
                Assert.IsType<OkObjectResult>(
                    result.Result).Value);

        Assert.Null(
            response.Description);

        Assert.Null(
            response.DueDate);

        Assert.Null(
            fixture.ProjectTask.Description);

        Assert.Null(
            fixture.ProjectTask.DueDate);
    }

    [Fact]
    public void Update_ShouldExposeTenantScopedPutProtectedByTenantAccess()
    {
        var controllerType =
            typeof(ProjectTasksController);

        Assert.NotNull(
            controllerType.GetCustomAttribute<ApiControllerAttribute>());

        Assert.Equal(
            "api/tenants/{tenantPublicId:guid}/projects/{projectPublicId:guid}/tasks",
            controllerType
                .GetCustomAttribute<RouteAttribute>()!
                .Template);

        var method =
            controllerType.GetMethod(
                nameof(ProjectTasksController.Update))!;

        var putAttribute =
            Assert.Single(
                method.GetCustomAttributes<HttpPutAttribute>());

        Assert.Equal(
            "{taskPublicId:guid}",
            putAttribute.Template);

        Assert.Equal(
            AuthorizationPolicyNames.TenantAccess,
            Assert.Single(
                method.GetCustomAttributes<AuthorizeAttribute>())
                .Policy);

        Assert.Empty(
            method.GetCustomAttributes<AllowAnonymousAttribute>());

        Assert.Equal(
            new[]
            {
                "Description",
                "DueDate",
                "Priority",
                "Title"
            },
            typeof(UpdateProjectTaskRequest)
                .GetProperties()
                .Select(property => property.Name)
                .OrderBy(name => name));

        Assert.Single(
            method.GetParameters(),
            parameter =>
                parameter.GetCustomAttribute<FromBodyAttribute>()
                    is not null);
    }

    private static UpdateProjectTaskRequest CreateRequest()
    {
        return new UpdateProjectTaskRequest(
            "Título atualizado",
            "Descrição atualizada",
            ProjectTaskPriority.High,
            new DateTime(
                2027, 2, 1, 12, 0, 0,
                DateTimeKind.Utc));
    }

    private static Error ConfigureFailureScenario(
        Fixture fixture,
        string scenario)
    {
        switch (scenario)
        {
            case "tenant-missing":
                fixture.TenantRepository.TenantToReturn = null;
                return TenantErrors.NotFound;

            case "requester-missing":
                fixture.UserRepository
                    .UsersByPublicIdToReturn
                    .Remove(fixture.Requester.PublicId);

                return UserErrors.NotFound;

            case "project-missing":
                fixture.ProjectRepository.ProjectToReturn = null;
                return ProjectErrors.NotFound;

            case "task-missing":
                fixture.TaskRepository
                    .ProjectTaskForUpdateToReturn = null;

                return ProjectTaskErrors.NotFound;

            case "tenant-inactive":
                fixture.Tenant.Deactivate();
                return TenantErrors.Inactive;

            case "requester-inactive":
                fixture.Requester.Deactivate();
                return UserErrors.Inactive;

            case "archived-project":
                fixture.Project.Archive();

                return ProjectTaskErrors
                    .UpdateBlockedByProjectStatus;

            case "task-archived":
                fixture.ProjectTask.Cancel();
                fixture.ProjectTask.Archive();

                return ProjectTaskErrors.Archived;

            case "no-membership":
                fixture.MemberRepository
                    .ActiveMembersToReturn[
                        (
                            fixture.Project.Id,
                            fixture.Requester.Id
                        )] = null;

                return ProjectTaskErrors.UpdateNotAllowed;

            case "no-permission":
                return ProjectTaskErrors.UpdateNotAllowed;

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

        var listProjectTasksHandler =
            new ListProjectTasksHandler(
                fixture.TenantRepository,
                fixture.UserRepository,
                fixture.ProjectRepository,
                fixture.MemberRepository,
                fixture.TaskRepository);

        var startProjectTaskHandler =
            new StartProjectTaskHandler(
                fixture.TenantRepository,
                fixture.UserRepository,
                fixture.ProjectRepository,
                fixture.MemberRepository,
                fixture.TaskRepository,
                fixture.UnitOfWork);

        var pauseProjectTaskHandler =
            new PauseProjectTaskHandler(
                fixture.TenantRepository,
                fixture.UserRepository,
                fixture.ProjectRepository,
                fixture.MemberRepository,
                fixture.TaskRepository,
                fixture.UnitOfWork);

        var resumeProjectTaskHandler =
            new ResumeProjectTaskHandler(
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

        return new ProjectTasksController(
            createHandler,
            assignHandler,
            claimHandler,
            removeHandler,
            updateHandler,
            listProjectTasksHandler,
            startProjectTaskHandler,
            moveToTodoHandler,
            pauseProjectTaskHandler,
            resumeProjectTaskHandler)
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
        UserRole requesterRole = UserRole.TenantAdmin,
        bool grantEditTask = true)
    {
        var tenant =
            new Tenant(
                "Empresa Update Task API",
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
                "Projeto Update Task API",
                requester.Id);

        EntityTestHelper.SetId(
            project,
            100);

        var projectTask =
            new ProjectTask(
                project.Id,
                "Tarefa original",
                ProjectTaskPriority.Medium,
                requester.Id);

        EntityTestHelper.SetId(
            projectTask,
            200);

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

        var projectRepository =
            new FakeProjectRepository
            {
                ProjectToReturn = project
            };

        var memberRepository =
            new FakeProjectMemberRepository();

        var permissionRepository =
            new FakeProjectMemberPermissionRepository();

        if (requesterRole == UserRole.ProjectManager ||
            requesterRole == UserRole.Member)
        {
            var member =
                new ProjectMember(
                    project.Id,
                    requester.Id,
                    requester.Id);

            EntityTestHelper.SetId(
                member,
                300);

            memberRepository
                .ActiveMembersToReturn[
                    (
                        project.Id,
                        requester.Id
                    )] =
                member;

            permissionRepository
                .IsActivePermissionResults[
                    (
                        member.Id,
                        ProjectPermission.EditTask
                    )] =
                grantEditTask;
        }

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