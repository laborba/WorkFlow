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
using WorkFlow.Application.ProjectTasks.CreateProjectTask;
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

public sealed class AssignProjectTaskResponsibleControllerTests
{
    [Fact]
    public async Task AssignResponsible_ShouldReturnOk_WhenAssignmentIsValid()
    {
        var fixture =
            CreateFixture();

        var controller =
            CreateController(
                fixture,
                fixture.Requester.PublicId.ToString());

        var request =
            new AssignProjectTaskResponsibleRequest(
                fixture.ResponsibleUser.PublicId);

        var result =
            await controller.AssignResponsible(
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
            Assert.IsType<AssignProjectTaskResponsibleResponse>(
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
            fixture.ResponsibleUser.PublicId,
            response.ResponsibleUserPublicId);

        Assert.Equal(
            ProjectTaskStatus.Backlog,
            response.Status);

        Assert.NotNull(
            response.UpdatedAt);

        Assert.Equal(
            fixture.ResponsibleUser.Id,
            fixture.ProjectTask.ResponsibleUserId);

        Assert.Equal(
            ProjectTaskStatus.Backlog,
            fixture.ProjectTask.Status);

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
    public async Task AssignResponsible_ShouldReturnUnauthorized_WhenRequesterClaimIsInvalid(
        string? claim)
    {
        var fixture =
            CreateFixture();

        var controller =
            CreateController(
                fixture,
                claim);

        var result =
            await controller.AssignResponsible(
                fixture.Tenant.PublicId,
                fixture.Project.PublicId,
                fixture.ProjectTask.PublicId,
                new AssignProjectTaskResponsibleRequest(
                    fixture.ResponsibleUser.PublicId),
                CancellationToken.None);

        Assert.IsType<UnauthorizedResult>(
            result.Result);

        Assert.Equal(
            0,
            fixture.UnitOfWork.BeginTransactionCallCount);

        Assert.Equal(
            0,
            fixture.UnitOfWork.SaveChangesCallCount);

        Assert.Null(
            fixture.ProjectTask.ResponsibleUserId);
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
        "responsible-missing",
        StatusCodes.Status404NotFound)]
    [InlineData(
        "tenant-inactive",
        StatusCodes.Status409Conflict)]
    [InlineData(
        "requester-inactive",
        StatusCodes.Status403Forbidden)]
    [InlineData(
        "completed",
        StatusCodes.Status409Conflict)]
    [InlineData(
        "task-archived",
        StatusCodes.Status409Conflict)]
    [InlineData(
        "responsible-inactive",
        StatusCodes.Status409Conflict)]
    [InlineData(
        "responsible-not-member",
        StatusCodes.Status409Conflict)]
    [InlineData(
        "no-permission",
        StatusCodes.Status403Forbidden)]
    public async Task AssignResponsible_ShouldMapApplicationErrors(
        string scenario,
        int expectedStatusCode)
    {
        var fixture =
            scenario == "no-permission"
                ? CreateFixture(
                    UserRole.Member,
                    grantAssignTask: false)
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
            await controller.AssignResponsible(
                fixture.Tenant.PublicId,
                fixture.Project.PublicId,
                fixture.ProjectTask.PublicId,
                new AssignProjectTaskResponsibleRequest(
                    fixture.ResponsibleUser.PublicId),
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

        Assert.Null(
            fixture.ProjectTask.ResponsibleUserId);
    }

    [Fact]
    public void AssignResponsible_ShouldExposeTenantScopedPatchProtectedByTenantAccess()
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
                nameof(ProjectTasksController.AssignResponsible))!;

        var patchAttribute =
            Assert.Single(
                method.GetCustomAttributes<HttpPatchAttribute>());

        Assert.Equal(
            "{taskPublicId:guid}/responsible",
            patchAttribute.Template);

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
                "ResponsibleUserPublicId"
            },
            typeof(AssignProjectTaskResponsibleRequest)
                .GetProperties()
                .Select(property => property.Name)
                .OrderBy(name => name));
    }

    private static Error ConfigureFailureScenario(
        Fixture fixture,
        string scenario)
    {
        return scenario switch
        {
            "tenant-missing" =>
                Configure(
                    () =>
                        fixture.TenantRepository.TenantToReturn =
                            null,
                    TenantErrors.NotFound),

            "requester-missing" =>
                Configure(
                    () =>
                        fixture.UserRepository
                            .UsersByPublicIdToReturn
                            .Remove(
                                fixture.Requester.PublicId),
                    UserErrors.NotFound),

            "project-missing" =>
                Configure(
                    () =>
                        fixture.ProjectRepository.ProjectToReturn =
                            null,
                    ProjectErrors.NotFound),

            "task-missing" =>
                Configure(
                    () =>
                        fixture.TaskRepository
                            .ProjectTaskForUpdateToReturn =
                            null,
                    ProjectTaskErrors.NotFound),

            "responsible-missing" =>
                Configure(
                    () =>
                        fixture.UserRepository
                            .UsersByPublicIdToReturn
                            .Remove(
                                fixture.ResponsibleUser.PublicId),
                    ProjectTaskErrors.ResponsibleUserNotFound),

            "tenant-inactive" =>
                Configure(
                    fixture.Tenant.Deactivate,
                    TenantErrors.Inactive),

            "requester-inactive" =>
                Configure(
                    fixture.Requester.Deactivate,
                    UserErrors.Inactive),

            "completed" =>
                Configure(
                    () =>
                    {
                        fixture.Project.Start();

                        fixture.Project.Complete(
                            new[]
                            {
                                ProjectTaskStatus.Done
                            });
                    },
                    ProjectTaskErrors
                        .AssignmentBlockedByProjectStatus),

            "task-archived" =>
                Configure(
                    () =>
                    {
                        fixture.ProjectTask.Cancel();
                        fixture.ProjectTask.Archive();
                    },
                    ProjectTaskErrors.Archived),

            "responsible-inactive" =>
                Configure(
                    fixture.ResponsibleUser.Deactivate,
                    ProjectTaskErrors.ResponsibleUserInactive),

            "responsible-not-member" =>
                Configure(
                    () =>
                        fixture.MemberRepository
                            .ActiveMembersToReturn[
                                (
                                    fixture.Project.Id,
                                    fixture.ResponsibleUser.Id
                                )] =
                            null,
                    ProjectTaskErrors
                        .ResponsibleUserNotActiveMember),

            "no-permission" =>
                ProjectTaskErrors.AssignmentNotAllowed,

            _ =>
                throw new ArgumentOutOfRangeException(
                    nameof(scenario))
        };
    }

    private static Error Configure(
        Action action,
        Error error)
    {
        action();

        return error;
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

        return new ProjectTasksController(
            fixture.CreateProjectTaskHandler,
            fixture.AssignHandler)
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
        bool grantAssignTask = true)
    {
        var tenant =
            new Tenant(
                "Empresa Assign Task API",
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

        var responsibleUser =
            new User(
                tenant.Id,
                "Usuário Responsável",
                $"responsible-{Guid.NewGuid():N}@test.local",
                "password-hash",
                UserRole.Member);

        EntityTestHelper.SetId(
            responsibleUser,
            20);

        var project =
            new Project(
                tenant.Id,
                "Projeto Assign Task API",
                requester.Id);

        EntityTestHelper.SetId(
            project,
            100);

        var projectTask =
            new ProjectTask(
                project.Id,
                "Tarefa API",
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

        userRepository
            .UsersByPublicIdToReturn[
                responsibleUser.PublicId] =
            responsibleUser;

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

        if (requesterRole != UserRole.TenantAdmin)
        {
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

            permissionRepository
                .IsActivePermissionResults[
                    (
                        requesterMember.Id,
                        ProjectPermission.AssignTask
                    )] =
                grantAssignTask;
        }

        var responsibleMember =
            new ProjectMember(
                project.Id,
                responsibleUser.Id,
                requester.Id);

        EntityTestHelper.SetId(
            responsibleMember,
            301);

        memberRepository
            .ActiveMembersToReturn[
                (
                    project.Id,
                    responsibleUser.Id
                )] =
            responsibleMember;

        var taskRepository =
            new FakeProjectTaskRepository
            {
                ProjectTaskForUpdateToReturn =
                    projectTask
            };

        var unitOfWork =
            new FakeUnitOfWork();

        var createProjectTaskHandler =
            new CreateProjectTaskHandler(
                tenantRepository,
                userRepository,
                projectRepository,
                memberRepository,
                permissionRepository,
                taskRepository,
                unitOfWork);

        var assignHandler =
            new AssignProjectTaskResponsibleHandler(
                tenantRepository,
                userRepository,
                projectRepository,
                memberRepository,
                permissionRepository,
                taskRepository,
                unitOfWork);

        return new Fixture(
            tenant,
            requester,
            responsibleUser,
            project,
            projectTask,
            tenantRepository,
            userRepository,
            projectRepository,
            memberRepository,
            permissionRepository,
            taskRepository,
            unitOfWork,
            createProjectTaskHandler,
            assignHandler);
    }

    private sealed record Fixture(
        Tenant Tenant,
        User Requester,
        User ResponsibleUser,
        Project Project,
        ProjectTask ProjectTask,
        FakeTenantRepository TenantRepository,
        FakeUserRepository UserRepository,
        FakeProjectRepository ProjectRepository,
        FakeProjectMemberRepository MemberRepository,
        FakeProjectMemberPermissionRepository PermissionRepository,
        FakeProjectTaskRepository TaskRepository,
        FakeUnitOfWork UnitOfWork,
        CreateProjectTaskHandler CreateProjectTaskHandler,
        AssignProjectTaskResponsibleHandler AssignHandler);
}