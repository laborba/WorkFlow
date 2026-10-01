using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Reflection;
using System.Security.Claims;
using WorkFlow.API.Authorization;
using WorkFlow.API.Contracts.Common;
using WorkFlow.API.Contracts.ProjectTasks;
using WorkFlow.API.Controllers;
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

public sealed class MoveProjectTaskToTodoControllerTests
{
    [Theory]
    [InlineData(UserRole.TenantAdmin)]
    [InlineData(UserRole.ProjectManager)]
    [InlineData(UserRole.Member)]
    public async Task MoveToTodo_ShouldReturnOk_WhenAuthorized(
        UserRole role)
    {
        var fixture =
            CreateFixture(role);

        var controller =
            CreateController(
                fixture,
                fixture.Requester.PublicId.ToString());

        var result =
            await controller.MoveToTodo(
                fixture.Tenant.PublicId,
                fixture.Project.PublicId,
                fixture.ProjectTask.PublicId,
                CancellationToken.None);

        var ok =
            Assert.IsType<OkObjectResult>(
                result.Result);

        Assert.Equal(
            StatusCodes.Status200OK,
            ok.StatusCode);

        var response =
            Assert.IsType<MoveProjectTaskToTodoResponse>(
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
            ProjectTaskStatus.Todo,
            response.Status);

        Assert.Equal(
            ProjectTaskStatus.Todo,
            fixture.ProjectTask.Status);

        Assert.NotNull(
            response.UpdatedAt);

        Assert.Equal(
            fixture.ProjectTask.UpdatedAt,
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
    public async Task MoveToTodo_ShouldReturnUnauthorized_WhenUserClaimIsInvalid(
        string? claim)
    {
        var fixture =
            CreateFixture();

        var controller =
            CreateController(
                fixture,
                claim);

        var result =
            await controller.MoveToTodo(
                fixture.Tenant.PublicId,
                fixture.Project.PublicId,
                fixture.ProjectTask.PublicId,
                CancellationToken.None);

        Assert.IsType<UnauthorizedResult>(
            result.Result);

        Assert.Equal(
            ProjectTaskStatus.Backlog,
            fixture.ProjectTask.Status);

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
        "project-completed",
        StatusCodes.Status409Conflict)]
    [InlineData(
        "project-archived",
        StatusCodes.Status409Conflict)]
    [InlineData(
        "task-archived",
        StatusCodes.Status409Conflict)]
    [InlineData(
        "task-not-backlog",
        StatusCodes.Status409Conflict)]
    [InlineData(
        "not-active-member",
        StatusCodes.Status403Forbidden)]
    [InlineData(
        "without-edit-task",
        StatusCodes.Status403Forbidden)]
    public async Task MoveToTodo_ShouldMapApplicationErrors(
        string scenario,
        int expectedStatusCode)
    {
        var fixture =
            CreateFixture(
                scenario is
                    "not-active-member" or
                    "without-edit-task"
                    ? UserRole.Member
                    : UserRole.TenantAdmin);

        var expectedError =
            ConfigureFailureScenario(
                fixture,
                scenario);

        var controller =
            CreateController(
                fixture,
                fixture.Requester.PublicId.ToString());

        var result =
            await controller.MoveToTodo(
                fixture.Tenant.PublicId,
                fixture.Project.PublicId,
                fixture.ProjectTask.PublicId,
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
    public void MoveToTodo_ShouldExposeTenantScopedPostProtectedByTenantAccess()
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
                nameof(ProjectTasksController.MoveToTodo));

        Assert.NotNull(
            method);

        var postAttribute =
            Assert.Single(
                method!
                    .GetCustomAttributes<HttpPostAttribute>());

        Assert.Equal(
            "{taskPublicId:guid}/todo",
            postAttribute.Template);

        Assert.Equal(
            AuthorizationPolicyNames.TenantAccess,
            Assert.Single(
                    method.GetCustomAttributes<AuthorizeAttribute>())
                .Policy);

        Assert.Empty(
            method.GetCustomAttributes<AllowAnonymousAttribute>());

        Assert.DoesNotContain(
            method.GetParameters(),
            parameter =>
                parameter.GetCustomAttribute<FromBodyAttribute>()
                    is not null);
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

            case "project-completed":
                fixture.Project.Complete(
                    new[]
                    {
                        ProjectTaskStatus.Done
                    });

                return ProjectTaskErrors
                    .MoveToTodoBlockedByProjectStatus;

            case "project-archived":
                fixture.Project.Complete(
                    new[]
                    {
                        ProjectTaskStatus.Done
                    });

                fixture.Project.Archive();

                return ProjectTaskErrors
                    .MoveToTodoBlockedByProjectStatus;

            case "task-archived":
                fixture.ProjectTask.Cancel();
                fixture.ProjectTask.Archive();

                return ProjectTaskErrors.Archived;

            case "task-not-backlog":
                fixture.ProjectTask.MoveToTodo();

                return ProjectTaskErrors
                    .MoveToTodoBlockedByTaskStatus;

            case "not-active-member":
                fixture.MemberRepository
                    .ActiveMembersToReturn[
                        (
                            fixture.Project.Id,
                            fixture.Requester.Id
                        )] =
                    null;

                return ProjectTaskErrors
                    .MoveToTodoNotAllowed;

            case "without-edit-task":
                fixture.PermissionRepository
                    .IsActivePermissionResults[
                        (
                            fixture.RequesterMember!.Id,
                            ProjectPermission.EditTask
                        )] =
                    false;

                return ProjectTaskErrors
                    .MoveToTodoNotAllowed;

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
            UserRole.TenantAdmin)
    {
        var tenant =
            new Tenant(
                "Empresa Move Task To Todo API",
                $"REG-{Guid.NewGuid():N}",
                $"tenant-{Guid.NewGuid():N}@test.local");

        EntityTestHelper.SetId(
            tenant,
            42);

        var requester =
            new User(
                tenant.Id,
                "Usuário solicitante",
                $"requester-{Guid.NewGuid():N}@test.local",
                "password-hash",
                requesterRole);

        EntityTestHelper.SetId(
            requester,
            10);

        var project =
            new Project(
                tenant.Id,
                "Projeto Move Task To Todo API",
                requester.Id);

        EntityTestHelper.SetId(
            project,
            100);

        project.Start();

        var projectTask =
            new ProjectTask(
                project.Id,
                "Tarefa no backlog",
                ProjectTaskPriority.Medium,
                requester.Id);

        EntityTestHelper.SetId(
            projectTask,
            200);

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

        var projectRepository =
            new FakeProjectRepository
            {
                ProjectToReturn =
                    project
            };

        var memberRepository =
            new FakeProjectMemberRepository();

        var permissionRepository =
            new FakeProjectMemberPermissionRepository();

        ProjectMember? requesterMember =
            null;

        if (requesterRole is
            UserRole.ProjectManager or
            UserRole.Member)
        {
            requesterMember =
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

            permissionRepository
                .IsActivePermissionResults[
                    (
                        requesterMember.Id,
                        ProjectPermission.EditTask
                    )] =
                true;
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
            requesterMember,
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
        ProjectMember? RequesterMember,
        FakeTenantRepository TenantRepository,
        FakeUserRepository UserRepository,
        FakeProjectRepository ProjectRepository,
        FakeProjectMemberRepository MemberRepository,
        FakeProjectMemberPermissionRepository PermissionRepository,
        FakeProjectTaskRepository TaskRepository,
        FakeUnitOfWork UnitOfWork);
}