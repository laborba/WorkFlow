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
using WorkFlow.Application.Projects;
using WorkFlow.Application.ProjectTasks;
using WorkFlow.Application.Tenants;
using WorkFlow.Application.Users;
using WorkFlow.Domain.Enums;
using WorkFlow.UnitTests.Application.ProjectTasks;

namespace WorkFlow.UnitTests.API.ProjectTasks;

public sealed class ProjectTasksControllerTests
{
    [Theory]
    [InlineData(UserRole.TenantAdmin)]
    [InlineData(UserRole.ProjectManager)]
    [InlineData(UserRole.Member)]
    public async Task Create_ShouldReturnCreated_WithAuthenticatedCreator(UserRole role)
    {
        var fixture = new CreateProjectTaskTestFixture(role);
        if (role != UserRole.TenantAdmin)
            fixture.Authorize();
        var controller = CreateController(fixture, fixture.Creator.PublicId.ToString());
        var request = CreateRequest();

        var result = await controller.Create(fixture.Tenant.PublicId,
            fixture.Project.PublicId, request, CancellationToken.None);

        var created = Assert.IsType<ObjectResult>(result.Result);
        Assert.Equal(StatusCodes.Status201Created, created.StatusCode);
        var response = Assert.IsType<CreateProjectTaskResponse>(created.Value);
        var task = fixture.TaskRepository.AddedProjectTask!;
        Assert.Equal(task.PublicId, response.PublicId);
        Assert.Equal(fixture.Tenant.PublicId, response.TenantPublicId);
        Assert.Equal(fixture.Project.PublicId, response.ProjectPublicId);
        Assert.Equal(fixture.Creator.PublicId, response.CreatedByUserPublicId);
        Assert.Equal(fixture.Creator.Id, task.CreatedByUserId);
        Assert.Equal(ProjectTaskStatus.Backlog, response.Status);
        Assert.Null(response.ResponsibleUserPublicId);
        Assert.Null(task.ResponsibleUserId);
        Assert.Equal(request.Title.Trim(), response.Title);
        Assert.Equal(request.Description!.Trim(), response.Description);
        Assert.Equal(request.Priority, response.Priority);
        Assert.Equal(request.DueDate, response.DueDate);
        Assert.Equal(task.CreatedAt, response.CreatedAt);
        Assert.Equal(1, fixture.UnitOfWork.SaveChangesCallCount);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("invalid")]
    [InlineData("00000000-0000-0000-0000-000000000000")]
    public async Task Create_ShouldReturnUnauthorized_WhenCreatorClaimIsInvalid(string? claim)
    {
        var fixture = new CreateProjectTaskTestFixture();
        var controller = CreateController(fixture, claim);
        var result = await controller.Create(fixture.Tenant.PublicId,
            fixture.Project.PublicId, CreateRequest(), CancellationToken.None);
        Assert.IsType<UnauthorizedResult>(result.Result);
        AssertNothingPersisted(fixture);
        Assert.Null(fixture.TenantRepository.CheckedPublicId);
    }

    [Theory]
    [InlineData("tenant-missing", StatusCodes.Status404NotFound)]
    [InlineData("user-missing", StatusCodes.Status404NotFound)]
    [InlineData("project-missing", StatusCodes.Status404NotFound)]
    [InlineData("tenant-inactive", StatusCodes.Status409Conflict)]
    [InlineData("user-inactive", StatusCodes.Status403Forbidden)]
    [InlineData("completed", StatusCodes.Status409Conflict)]
    [InlineData("archived", StatusCodes.Status409Conflict)]
    [InlineData("unauthorized", StatusCodes.Status403Forbidden)]
    [InlineData("no-permission", StatusCodes.Status403Forbidden)]
    public async Task Create_ShouldMapApplicationErrors(string scenario, int statusCode)
    {
        var fixture = new CreateProjectTaskTestFixture(UserRole.Member);
        if (scenario != "unauthorized" && scenario != "no-permission")
            fixture.Authorize();
        var expectedError = ProjectTaskErrors.CreationNotAllowed;
        switch (scenario)
        {
            case "tenant-missing":
                fixture.TenantRepository.TenantToReturn = null;
                expectedError = TenantErrors.NotFound;
                break;
            case "user-missing":
                fixture.UserRepository.UserToReturn = null;
                expectedError = UserErrors.NotFound;
                break;
            case "project-missing":
                fixture.ProjectRepository.ProjectToReturn = null;
                expectedError = ProjectErrors.NotFound;
                break;
            case "tenant-inactive":
                fixture.Tenant.Deactivate();
                expectedError = TenantErrors.Inactive;
                break;
            case "user-inactive":
                fixture.Creator.Deactivate();
                expectedError = UserErrors.Inactive;
                break;
            case "completed":
                fixture.SetProjectStatus(ProjectStatus.Completed);
                expectedError = ProjectTaskErrors.CreationBlockedByProjectStatus;
                break;
            case "archived":
                fixture.SetProjectStatus(ProjectStatus.Archived);
                expectedError = ProjectTaskErrors.CreationBlockedByProjectStatus;
                break;
            case "no-permission":
                fixture.AddMembership();
                break;
        }

        var controller = CreateController(fixture, fixture.Creator.PublicId.ToString());
        var result = await controller.Create(fixture.Tenant.PublicId,
            fixture.Project.PublicId, CreateRequest(), CancellationToken.None);

        var failure = Assert.IsAssignableFrom<ObjectResult>(result.Result);
        Assert.Equal(statusCode, failure.StatusCode);
        var response = Assert.IsType<ErrorResponse>(failure.Value);
        Assert.Equal(expectedError.Code, response.Code);
        Assert.Equal(expectedError.Message, response.Message);
        AssertNothingPersisted(fixture);
    }

    [Fact]
    public async Task Create_ShouldRejectSystemAdmin()
    {
        var fixture = new CreateProjectTaskTestFixture(UserRole.SystemAdmin);
        var controller = CreateController(fixture, fixture.Creator.PublicId.ToString());
        var result = await controller.Create(fixture.Tenant.PublicId,
            fixture.Project.PublicId, CreateRequest(), CancellationToken.None);
        var failure = Assert.IsType<ObjectResult>(result.Result);
        Assert.Equal(StatusCodes.Status403Forbidden, failure.StatusCode);
        AssertNothingPersisted(fixture);
    }

    [Fact]
    public async Task Create_ShouldIgnoreCreatorAndResponsibleFieldsFromJson()
    {
        var fixture = new CreateProjectTaskTestFixture();
        var controller = CreateController(fixture, fixture.Creator.PublicId.ToString());
        var json = JsonSerializer.Serialize(new
        {
            title = "Tarefa",
            priority = 2,
            createdByUserId = 999,
            createdByUserPublicId = Guid.NewGuid(),
            responsibleUserId = 999,
            responsibleUserPublicId = Guid.NewGuid(),
            status = ProjectTaskStatus.Done
        });
        var request = JsonSerializer.Deserialize<CreateProjectTaskRequest>(
            json, new JsonSerializerOptions(JsonSerializerDefaults.Web))!;

        var result = await controller.Create(fixture.Tenant.PublicId,
            fixture.Project.PublicId, request, CancellationToken.None);

        var response = Assert.IsType<CreateProjectTaskResponse>(
            Assert.IsType<ObjectResult>(result.Result).Value);
        Assert.Equal(fixture.Creator.PublicId, response.CreatedByUserPublicId);
        Assert.Equal(fixture.Creator.Id, fixture.TaskRepository.AddedProjectTask!.CreatedByUserId);
        Assert.Null(fixture.TaskRepository.AddedProjectTask.ResponsibleUserId);
        Assert.Null(response.ResponsibleUserPublicId);
        Assert.Null(response.Description);
        Assert.Null(response.DueDate);
        Assert.Equal(ProjectTaskStatus.Backlog, response.Status);
    }

    [Theory]
    [InlineData("", 2)]
    [InlineData("   ", 2)]
    [InlineData("Tarefa", 0)]
    [InlineData("Tarefa", 99)]
    public async Task Create_ShouldUseGlobalValidationHandling_ForInvalidInput(string title, int priority)
    {
        var fixture = new CreateProjectTaskTestFixture();
        var controller = CreateController(fixture, fixture.Creator.PublicId.ToString());
        var request = CreateRequest() with { Title = title, Priority = (ProjectTaskPriority)priority };
        var exception = await Assert.ThrowsAsync<ArgumentException>(() => controller.Create(
            fixture.Tenant.PublicId, fixture.Project.PublicId, request, CancellationToken.None));

        var context = new DefaultHttpContext();
        await using var body = new MemoryStream();
        context.Response.Body = body;
        var exceptionHandler = new GlobalExceptionHandler(NullLogger<GlobalExceptionHandler>.Instance);
        Assert.True(await exceptionHandler.TryHandleAsync(context, exception, CancellationToken.None));
        Assert.Equal(StatusCodes.Status400BadRequest, context.Response.StatusCode);
        body.Position = 0;
        var error = await JsonSerializer.DeserializeAsync<ErrorResponse>(
            body, new JsonSerializerOptions(JsonSerializerDefaults.Web));
        Assert.Equal("Validation.InvalidArgument", error!.Code);
        AssertNothingPersisted(fixture);
    }

    [Fact]
    public async Task
    Create_ShouldNormalizeDueDateWithOffsetToUtc()
    {
        var fixture =
            new CreateProjectTaskTestFixture();

        var controller =
            CreateController(
                fixture,
                fixture.Creator.PublicId.ToString());

        var json =
            """
        {
          "title": "Tarefa com offset",
          "description": "Teste de normalização",
          "priority": 2,
          "dueDate": "2027-01-31T18:00:00-03:00"
        }
        """;

        var request =
            JsonSerializer.Deserialize<CreateProjectTaskRequest>(
                json,
                new JsonSerializerOptions(
                    JsonSerializerDefaults.Web))!;

        var result =
            await controller.Create(
                fixture.Tenant.PublicId,
                fixture.Project.PublicId,
                request,
                CancellationToken.None);

        var created =
            Assert.IsType<ObjectResult>(
                result.Result);

        Assert.Equal(
            StatusCodes.Status201Created,
            created.StatusCode);

        var response =
            Assert.IsType<CreateProjectTaskResponse>(
                created.Value);

        var expectedUtc =
            new DateTime(
                2027,
                1,
                31,
                21,
                0,
                0,
                DateTimeKind.Utc);

        Assert.Equal(
            expectedUtc,
            response.DueDate);

        Assert.Equal(
            DateTimeKind.Utc,
            response.DueDate!.Value.Kind);

        Assert.Equal(
            expectedUtc,
            fixture.TaskRepository
                .AddedProjectTask!
                .DueDate);
    }

    [Fact]
    public void Create_ShouldExposeTenantScopedPostProtectedByTenantAccess()
    {
        var controllerType = typeof(ProjectTasksController);
        Assert.NotNull(controllerType.GetCustomAttribute<ApiControllerAttribute>());
        Assert.Equal("api/tenants/{tenantPublicId:guid}/projects/{projectPublicId:guid}/tasks",
            controllerType.GetCustomAttribute<RouteAttribute>()!.Template);
        var method = controllerType.GetMethod(nameof(ProjectTasksController.Create))!;
        Assert.NotNull(method.GetCustomAttribute<HttpPostAttribute>());
        Assert.Equal(AuthorizationPolicyNames.TenantAccess,
            Assert.Single(method.GetCustomAttributes<AuthorizeAttribute>()).Policy);
        Assert.Empty(method.GetCustomAttributes<AllowAnonymousAttribute>());
        Assert.Equal(new[] { "Description", "DueDate", "Priority", "Title" },
            typeof(CreateProjectTaskRequest).GetProperties().Select(property => property.Name).OrderBy(name => name));
    }

    private static ProjectTasksController CreateController(CreateProjectTaskTestFixture fixture, string? claim)
    {
        var claims = claim is null
            ? Array.Empty<Claim>()
            : new[] { new Claim(ClaimTypes.NameIdentifier, claim) };
        return new ProjectTasksController(fixture.Handler)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = new ClaimsPrincipal(new ClaimsIdentity(claims, "Test"))
                }
            }
        };
    }

    private static CreateProjectTaskRequest CreateRequest()
    {
        return new CreateProjectTaskRequest("  Tarefa API  ", "  Descrição API  ",
            ProjectTaskPriority.High, new DateTime(2027, 1, 31, 12, 0, 0, DateTimeKind.Utc));
    }

    private static void AssertNothingPersisted(CreateProjectTaskTestFixture fixture)
    {
        Assert.Null(fixture.TaskRepository.AddedProjectTask);
        Assert.Equal(0, fixture.UnitOfWork.SaveChangesCallCount);
    }
}
