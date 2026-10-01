using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Reflection;
using System.Security.Claims;
using WorkFlow.API.Authorization;
using WorkFlow.API.Contracts.ProjectTasks;
using WorkFlow.API.Controllers;
using WorkFlow.Application.Abstractions.Persistence.Models;
using WorkFlow.Application.Common.Pagination;
using WorkFlow.Application.Projects;
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
using WorkFlow.UnitTests.Application.Users.Fakes;
using WorkFlow.UnitTests.Common;
using FakeTenantRepository =
    WorkFlow.UnitTests.Application.Tenants.Fakes.FakeTenantRepository;
using FakeUnitOfWork =
    WorkFlow.UnitTests.Common.Fakes.FakeUnitOfWork;

namespace WorkFlow.UnitTests.API.ProjectTasks;

public sealed class ListProjectTasksControllerTests
{
    [Fact]
    public async Task List_ShouldReturnOk_WithPagedTasks()
    {
        var fixture =
            CreateFixture();

        var responsiblePublicId =
            Guid.NewGuid();

        var validatorPublicId =
            Guid.NewGuid();

        var taskPublicId =
            Guid.NewGuid();

        var dueDate =
            DateTime.UtcNow.AddDays(10);

        var createdAt =
            DateTime.UtcNow.AddDays(-5);

        var updatedAt =
            DateTime.UtcNow.AddDays(-1);

        fixture.TaskRepository.PagedDataToReturn =
            new PagedData<ProjectTaskListItemData>(
                new[]
                {
                    new ProjectTaskListItemData(
                        taskPublicId,
                        fixture.Requester.PublicId,
                        true,
                        responsiblePublicId,
                        "Responsável",
                        true,
                        validatorPublicId,
                        "Tarefa API",
                        "Descrição API",
                        ProjectTaskStatus.Validation,
                        ProjectTaskPriority.High,
                        dueDate,
                        createdAt,
                        updatedAt,
                        null)
                },
                21);

        var controller =
            CreateController(
                fixture,
                fixture.Requester.PublicId.ToString());

        var request =
            new ListProjectTasksRequest(
                2,
                10,
                "api",
                ProjectTaskStatus.Validation,
                ProjectTaskPriority.High,
                responsiblePublicId,
                false);

        var result =
            await controller.List(
                fixture.Tenant.PublicId,
                fixture.Project.PublicId,
                request,
                CancellationToken.None);

        var ok =
            Assert.IsType<OkObjectResult>(
                result.Result);

        Assert.Equal(
            StatusCodes.Status200OK,
            ok.StatusCode);

        var response =
            Assert.IsType<ListProjectTasksResponse>(
                ok.Value);

        Assert.Equal(2, response.PageNumber);
        Assert.Equal(10, response.PageSize);
        Assert.Equal(21, response.TotalCount);
        Assert.Equal(3, response.TotalPages);

        var item =
            Assert.Single(
                response.Items);

        Assert.Equal(
            taskPublicId,
            item.PublicId);

        Assert.Equal(
            fixture.Requester.PublicId,
            item.CreatedByUserPublicId);

        Assert.Equal(
            responsiblePublicId,
            item.ResponsibleUserPublicId);

        Assert.Equal(
            "Responsável",
            item.ResponsibleUserName);

        Assert.Equal(
            validatorPublicId,
            item.ValidatorUserPublicId);

        Assert.Equal(
            "Tarefa API",
            item.Title);

        Assert.Equal(
            "Descrição API",
            item.Description);

        Assert.Equal(
            ProjectTaskStatus.Validation,
            item.Status);

        Assert.Equal(
            ProjectTaskPriority.High,
            item.Priority);

        Assert.Equal(
            dueDate,
            item.DueDate);

        Assert.Equal(
            createdAt,
            item.CreatedAt);

        Assert.Equal(
            updatedAt,
            item.UpdatedAt);

        Assert.Null(
            item.ArchivedAt);

        var repositoryCall =
            Assert.Single(
                fixture.TaskRepository.GetPagedCalls);

        Assert.Equal(
            fixture.Tenant.Id,
            repositoryCall.TenantId);

        Assert.Equal(
            fixture.Project.Id,
            repositoryCall.ProjectId);

        Assert.Equal(
            2,
            repositoryCall.PageNumber);

        Assert.Equal(
            10,
            repositoryCall.PageSize);

        Assert.Equal(
            "api",
            repositoryCall.Search);

        Assert.Equal(
            ProjectTaskStatus.Validation,
            repositoryCall.Status);

        Assert.Equal(
            ProjectTaskPriority.High,
            repositoryCall.Priority);

        Assert.Equal(
            responsiblePublicId,
            repositoryCall.ResponsibleUserPublicId);

        Assert.False(
            repositoryCall.IsArchived);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("claim-invalida")]
    public async Task List_ShouldReturnUnauthorized_WhenUserClaimIsInvalid(
        string? claim)
    {
        var fixture =
            CreateFixture();

        var controller =
            CreateController(
                fixture,
                claim);

        var result =
            await controller.List(
                fixture.Tenant.PublicId,
                fixture.Project.PublicId,
                new ListProjectTasksRequest(),
                CancellationToken.None);

        Assert.IsType<UnauthorizedResult>(
            result.Result);

        Assert.Empty(
            fixture.TaskRepository.GetPagedCalls);
    }

    [Theory]
    [InlineData("tenant-not-found", 404)]
    [InlineData("user-not-found", 404)]
    [InlineData("project-not-found", 404)]
    [InlineData("tenant-inactive", 409)]
    [InlineData("user-inactive", 403)]
    [InlineData("no-membership", 403)]
    public async Task List_ShouldMapApplicationErrors(
        string scenario,
        int expectedStatusCode)
    {
        var fixture =
            scenario == "no-membership"
                ? CreateFixture(
                    UserRole.Member,
                    isActiveMember: false)
                : CreateFixture();

        switch (scenario)
        {
            case "tenant-not-found":
                fixture.TenantRepository.TenantToReturn =
                    null;
                break;

            case "user-not-found":
                fixture.UserRepository.UserToReturn =
                    null;

                fixture.UserRepository
                    .UsersByPublicIdToReturn
                    .Remove(
                        fixture.Requester.PublicId);
                break;

            case "project-not-found":
                fixture.ProjectRepository.ProjectToReturn =
                    null;
                break;

            case "tenant-inactive":
                fixture.Tenant.Deactivate();
                break;

            case "user-inactive":
                fixture.Requester.Deactivate();
                break;

            case "no-membership":
                break;

            default:
                throw new ArgumentOutOfRangeException(
                    nameof(scenario));
        }

        var controller =
            CreateController(
                fixture,
                fixture.Requester.PublicId.ToString());

        var result =
            await controller.List(
                fixture.Tenant.PublicId,
                fixture.Project.PublicId,
                new ListProjectTasksRequest(),
                CancellationToken.None);

        var objectResult =
            Assert.IsAssignableFrom<ObjectResult>(
                result.Result);

        Assert.Equal(
            expectedStatusCode,
            objectResult.StatusCode);

        Assert.Empty(
            fixture.TaskRepository.GetPagedCalls);
    }

    [Fact]
    public void List_ShouldRequireTenantAccessPolicy()
    {
        var method =
            typeof(ProjectTasksController)
                .GetMethod(
                    nameof(ProjectTasksController.List));

        Assert.NotNull(
            method);

        var authorize =
            Assert.Single(
                method!
                    .GetCustomAttributes<AuthorizeAttribute>());

        Assert.Equal(
            AuthorizationPolicyNames.TenantAccess,
            authorize.Policy);
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

        var permissionRepository =
            new FakeProjectMemberPermissionRepository();

        var unitOfWork =
            new FakeUnitOfWork();

        var createProjectTaskHandler =
            new CreateProjectTaskHandler(
                fixture.TenantRepository,
                fixture.UserRepository,
                fixture.ProjectRepository,
                fixture.MemberRepository,
                permissionRepository,
                fixture.TaskRepository,
                unitOfWork);

        var assignProjectTaskResponsibleHandler =
            new AssignProjectTaskResponsibleHandler(
                fixture.TenantRepository,
                fixture.UserRepository,
                fixture.ProjectRepository,
                fixture.MemberRepository,
                permissionRepository,
                fixture.TaskRepository,
                unitOfWork);

        var claimProjectTaskHandler =
            new ClaimProjectTaskHandler(
                fixture.TenantRepository,
                fixture.UserRepository,
                fixture.ProjectRepository,
                fixture.MemberRepository,
                permissionRepository,
                fixture.TaskRepository,
                unitOfWork);

        var removeProjectTaskResponsibleHandler =
            new RemoveProjectTaskResponsibleHandler(
                fixture.TenantRepository,
                fixture.UserRepository,
                fixture.ProjectRepository,
                fixture.MemberRepository,
                permissionRepository,
                fixture.TaskRepository,
                unitOfWork);

        var updateProjectTaskHandler =
            new UpdateProjectTaskHandler(
                fixture.TenantRepository,
                fixture.UserRepository,
                fixture.ProjectRepository,
                fixture.MemberRepository,
                permissionRepository,
                fixture.TaskRepository,
                unitOfWork);

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
                unitOfWork);

        var pauseProjectTaskHandler =
            new PauseProjectTaskHandler(
                fixture.TenantRepository,
                fixture.UserRepository,
                fixture.ProjectRepository,
                fixture.MemberRepository,
                fixture.TaskRepository,
                unitOfWork);

        var resumeProjectTaskHandler =
            new ResumeProjectTaskHandler(
                fixture.TenantRepository,
                fixture.UserRepository,
                fixture.ProjectRepository,
                fixture.MemberRepository,
                fixture.TaskRepository,
                unitOfWork);

        var sendProjectTaskToValidationHandler =
            new SendProjectTaskToValidationHandler(
                fixture.TenantRepository,
                fixture.UserRepository,
                fixture.ProjectRepository,
                fixture.MemberRepository,
                fixture.TaskRepository,
                unitOfWork);

        var moveToTodoHandler =
            new MoveProjectTaskToTodoHandler(
                fixture.TenantRepository,
                fixture.UserRepository,
                fixture.ProjectRepository,
                fixture.MemberRepository,
                permissionRepository,
                fixture.TaskRepository,
                unitOfWork);

        return new ProjectTasksController(
            createProjectTaskHandler,
            assignProjectTaskResponsibleHandler,
            claimProjectTaskHandler,
            removeProjectTaskResponsibleHandler,
            updateProjectTaskHandler,
            listProjectTasksHandler,
            startProjectTaskHandler,
            moveToTodoHandler,
            pauseProjectTaskHandler,
            resumeProjectTaskHandler,
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
        UserRole role = UserRole.TenantAdmin,
        bool isActiveMember = true)
    {
        var tenant =
            new Tenant(
                "Empresa List Tasks API",
                $"REG-{Guid.NewGuid():N}",
                $"tenant-{Guid.NewGuid():N}@test.local");

        EntityTestHelper.SetId(
            tenant,
            42);

        long? tenantId =
            role == UserRole.SystemAdmin
                ? null
                : tenant.Id;

        var requester =
            new User(
                tenantId,
                "Usuário Solicitante",
                $"requester-{Guid.NewGuid():N}@test.local",
                "password-hash",
                role);

        EntityTestHelper.SetId(
            requester,
            84);

        var project =
            new Project(
                tenant.Id,
                "Projeto List Tasks API",
                requester.Id);

        EntityTestHelper.SetId(
            project,
            126);

        var tenantRepository =
            new FakeTenantRepository
            {
                TenantToReturn =
                    tenant
            };

        var userRepository =
            new FakeUserRepository
            {
                UserToReturn =
                    requester
            };

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
            new FakeProjectMemberRepository
            {
                IsActiveMemberResult =
                    isActiveMember
            };

        var taskRepository =
            new FakeProjectTaskRepository();

        return new Fixture(
            tenant,
            requester,
            project,
            tenantRepository,
            userRepository,
            projectRepository,
            memberRepository,
            taskRepository);
    }

    private sealed record Fixture(
        Tenant Tenant,
        User Requester,
        Project Project,
        FakeTenantRepository TenantRepository,
        FakeUserRepository UserRepository,
        FakeProjectRepository ProjectRepository,
        FakeProjectMemberRepository MemberRepository,
        FakeProjectTaskRepository TaskRepository);
}