using WorkFlow.Application.Common.Errors;
using WorkFlow.Application.Projects;
using WorkFlow.Application.ProjectTasks;
using WorkFlow.Application.ProjectTasks.MoveProjectTaskToTodo;
using WorkFlow.Application.Tenants;
using WorkFlow.Application.Users;
using WorkFlow.Domain.Entities;
using WorkFlow.Domain.Enums;
using WorkFlow.UnitTests.Application.Projects.Fakes;
using WorkFlow.UnitTests.Application.Tenants.Fakes;
using WorkFlow.UnitTests.Application.Users.Fakes;
using WorkFlow.UnitTests.Common;
using WorkFlow.UnitTests.Common.Fakes;

namespace WorkFlow.UnitTests.Application.ProjectTasks.MoveProjectTaskToTodo;

public sealed class MoveProjectTaskToTodoHandlerTests
{
    [Theory]
    [InlineData(UserRole.TenantAdmin, ProjectStatus.Planning)]
    [InlineData(UserRole.TenantAdmin, ProjectStatus.InProgress)]
    [InlineData(UserRole.TenantAdmin, ProjectStatus.Paused)]
    [InlineData(UserRole.ProjectManager, ProjectStatus.Planning)]
    [InlineData(UserRole.ProjectManager, ProjectStatus.InProgress)]
    [InlineData(UserRole.ProjectManager, ProjectStatus.Paused)]
    [InlineData(UserRole.Member, ProjectStatus.Planning)]
    [InlineData(UserRole.Member, ProjectStatus.InProgress)]
    [InlineData(UserRole.Member, ProjectStatus.Paused)]
    public async Task HandleAsync_ShouldMoveBacklogTaskToTodo_WhenAuthorized(
        UserRole requesterRole,
        ProjectStatus projectStatus)
    {
        var fixture =
            CreateFixture(
                requesterRole,
                projectStatus);

        var originalCreatedAt =
            fixture.ProjectTask.CreatedAt;

        var originalCreatorId =
            fixture.ProjectTask.CreatedByUserId;

        var originalResponsibleId =
            fixture.ProjectTask.ResponsibleUserId;

        var originalProjectId =
            fixture.ProjectTask.ProjectId;

        var before =
            DateTime.UtcNow;

        var result =
            await fixture.Handler.HandleAsync(
                fixture.CreateCommand());

        Assert.True(result.IsSuccess);
        Assert.Null(result.Error);

        var response =
            Assert.IsType<MoveProjectTaskToTodoResult>(
                result.Value);

        Assert.Equal(
            ProjectTaskStatus.Todo,
            fixture.ProjectTask.Status);

        Assert.Equal(
            ProjectTaskStatus.Todo,
            response.Status);

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
            fixture.ProjectTask.UpdatedAt,
            response.UpdatedAt);

        Assert.NotNull(
            fixture.ProjectTask.UpdatedAt);

        Assert.InRange(
            fixture.ProjectTask.UpdatedAt!.Value,
            before,
            DateTime.UtcNow);

        Assert.Equal(
            originalCreatedAt,
            fixture.ProjectTask.CreatedAt);

        Assert.Equal(
            originalCreatorId,
            fixture.ProjectTask.CreatedByUserId);

        Assert.Equal(
            originalResponsibleId,
            fixture.ProjectTask.ResponsibleUserId);

        Assert.Equal(
            originalProjectId,
            fixture.ProjectTask.ProjectId);

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

        if (requesterRole == UserRole.TenantAdmin)
        {
            Assert.Empty(
                fixture.MemberRepository.GetActiveCalls);

            Assert.Empty(
                fixture.PermissionRepository
                    .IsActivePermissionCalls);
        }
        else
        {
            Assert.Equal(
                (
                    fixture.Project.Id,
                    fixture.Requester.Id
                ),
                Assert.Single(
                    fixture.MemberRepository.GetActiveCalls));

            Assert.Equal(
                (
                    fixture.RequesterMember!.Id,
                    ProjectPermission.EditTask
                ),
                Assert.Single(
                    fixture.PermissionRepository
                        .IsActivePermissionCalls));
        }
    }

    [Fact]
    public async Task HandleAsync_ShouldAllowTaskWithoutResponsible()
    {
        var fixture =
            CreateFixture(
                responsibleUserId: null);

        Assert.Null(
            fixture.ProjectTask.ResponsibleUserId);

        var result =
            await fixture.Handler.HandleAsync(
                fixture.CreateCommand());

        Assert.True(result.IsSuccess);

        Assert.Equal(
            ProjectTaskStatus.Todo,
            fixture.ProjectTask.Status);

        Assert.Null(
            fixture.ProjectTask.ResponsibleUserId);

        Assert.Equal(
            1,
            fixture.UnitOfWork.SaveChangesCallCount);

        Assert.Equal(
            1,
            fixture.UnitOfWork.CommitCallCount);
    }

    [Fact]
    public async Task HandleAsync_ShouldThrow_WhenCommandIsNull()
    {
        var fixture =
            CreateFixture();

        await Assert.ThrowsAsync<ArgumentNullException>(
            () => fixture.Handler.HandleAsync(null!));

        AssertNothingPersisted(
            fixture);
    }

    [Theory]
    [InlineData("tenant")]
    [InlineData("project")]
    [InlineData("task")]
    [InlineData("requester")]
    public async Task HandleAsync_ShouldRejectEmptyPublicIds(
        string field)
    {
        var fixture =
            CreateFixture();

        var command =
            fixture.CreateCommand();

        command =
            field switch
            {
                "tenant" => command with
                {
                    TenantPublicId = Guid.Empty
                },

                "project" => command with
                {
                    ProjectPublicId = Guid.Empty
                },

                "task" => command with
                {
                    TaskPublicId = Guid.Empty
                },

                "requester" => command with
                {
                    RequestedByUserPublicId = Guid.Empty
                },

                _ => throw new ArgumentOutOfRangeException(
                    nameof(field))
            };

        await Assert.ThrowsAsync<ArgumentException>(
            () => fixture.Handler.HandleAsync(
                command));

        AssertNothingPersisted(
            fixture);
    }

    [Fact]
    public async Task HandleAsync_ShouldReturnNotFound_WhenTenantDoesNotExist()
    {
        var fixture =
            CreateFixture();

        fixture.TenantRepository.TenantToReturn =
            null;

        await AssertFailureAsync(
            fixture,
            TenantErrors.NotFound,
            transactionStarted: false);
    }

    [Fact]
    public async Task HandleAsync_ShouldRejectInactiveTenant()
    {
        var fixture =
            CreateFixture();

        fixture.Tenant.Deactivate();

        await AssertFailureAsync(
            fixture,
            TenantErrors.Inactive,
            transactionStarted: false);
    }

    [Fact]
    public async Task HandleAsync_ShouldReturnNotFound_WhenRequesterDoesNotExist()
    {
        var fixture =
            CreateFixture();

        fixture.UserRepository
            .UsersByPublicIdToReturn
            .Remove(
                fixture.Requester.PublicId);

        await AssertFailureAsync(
            fixture,
            UserErrors.NotFound,
            transactionStarted: false);
    }

    [Fact]
    public async Task HandleAsync_ShouldRejectInactiveRequester()
    {
        var fixture =
            CreateFixture();

        fixture.Requester.Deactivate();

        await AssertFailureAsync(
            fixture,
            UserErrors.Inactive,
            transactionStarted: false);
    }

    [Fact]
    public async Task HandleAsync_ShouldReturnNotFound_WhenRequesterIsSystemAdmin()
    {
        var fixture =
            CreateFixture(
                UserRole.SystemAdmin);

        await AssertFailureAsync(
            fixture,
            UserErrors.NotFound,
            transactionStarted: false);

        Assert.Empty(
            fixture.MemberRepository.GetActiveCalls);

        Assert.Empty(
            fixture.PermissionRepository
                .IsActivePermissionCalls);
    }

    [Fact]
    public async Task HandleAsync_ShouldReturnNotFound_WhenProjectDoesNotExist()
    {
        var fixture =
            CreateFixture();

        fixture.ProjectRepository.ProjectToReturn =
            null;

        await AssertFailureAsync(
            fixture,
            ProjectErrors.NotFound);
    }

    [Theory]
    [InlineData(UserRole.ProjectManager)]
    [InlineData(UserRole.Member)]
    public async Task HandleAsync_ShouldRejectRequesterWithoutActiveMembership(
        UserRole role)
    {
        var fixture =
            CreateFixture(role);

        fixture.MemberRepository
            .ActiveMembersToReturn[
                (
                    fixture.Project.Id,
                    fixture.Requester.Id
                )] =
            null;

        await AssertFailureAsync(
            fixture,
            ProjectTaskErrors.MoveToTodoNotAllowed);

        Assert.Empty(
            fixture.PermissionRepository
                .IsActivePermissionCalls);
    }

    [Theory]
    [InlineData(UserRole.ProjectManager)]
    [InlineData(UserRole.Member)]
    public async Task HandleAsync_ShouldRejectRequesterWithoutEditTask(
        UserRole role)
    {
        var fixture =
            CreateFixture(
                role,
                grantEditTask: false);

        await AssertFailureAsync(
            fixture,
            ProjectTaskErrors.MoveToTodoNotAllowed);

        Assert.Equal(
            (
                fixture.RequesterMember!.Id,
                ProjectPermission.EditTask
            ),
            Assert.Single(
                fixture.PermissionRepository
                    .IsActivePermissionCalls));
    }

    [Theory]
    [InlineData(ProjectStatus.Completed)]
    [InlineData(ProjectStatus.Archived)]
    public async Task HandleAsync_ShouldRejectBlockedProjectStatus(
        ProjectStatus projectStatus)
    {
        var fixture =
            CreateFixture(
                projectStatus: projectStatus);

        await AssertFailureAsync(
            fixture,
            ProjectTaskErrors
                .MoveToTodoBlockedByProjectStatus);
    }

    [Fact]
    public async Task HandleAsync_ShouldReturnNotFound_WhenTaskDoesNotExist()
    {
        var fixture =
            CreateFixture();

        fixture.TaskRepository
            .ProjectTaskForUpdateToReturn =
            null;

        await AssertFailureAsync(
            fixture,
            ProjectTaskErrors.NotFound);
    }

    [Fact]
    public async Task HandleAsync_ShouldRejectArchivedTask()
    {
        var fixture =
            CreateFixture();

        fixture.ProjectTask.Cancel();
        fixture.ProjectTask.Archive();

        await AssertFailureAsync(
            fixture,
            ProjectTaskErrors.Archived);

        Assert.True(
            fixture.ProjectTask.IsArchived);
    }

    [Theory]
    [InlineData(ProjectTaskStatus.Todo)]
    [InlineData(ProjectTaskStatus.InProgress)]
    [InlineData(ProjectTaskStatus.Paused)]
    [InlineData(ProjectTaskStatus.Validation)]
    [InlineData(ProjectTaskStatus.Done)]
    [InlineData(ProjectTaskStatus.Cancelled)]
    public async Task HandleAsync_ShouldRejectTaskThatIsNotBacklog(
        ProjectTaskStatus taskStatus)
    {
        var fixture =
            CreateFixture(
                taskStatus: taskStatus);

        await AssertFailureAsync(
            fixture,
            ProjectTaskErrors
                .MoveToTodoBlockedByTaskStatus);
    }

    [Fact]
    public async Task HandleAsync_ShouldRollback_WhenSaveChangesFails()
    {
        var fixture =
            CreateFixture();

        fixture.UnitOfWork.OnSaveChanges =
            _ => throw new InvalidOperationException(
                "Falha simulada ao salvar.");

        var exception =
            await Assert.ThrowsAsync<InvalidOperationException>(
                () => fixture.Handler.HandleAsync(
                    fixture.CreateCommand()));

        Assert.Equal(
            "Falha simulada ao salvar.",
            exception.Message);

        Assert.Equal(
            1,
            fixture.UnitOfWork.BeginTransactionCallCount);

        Assert.Equal(
            1,
            fixture.UnitOfWork.SaveChangesCallCount);

        Assert.Equal(
            0,
            fixture.UnitOfWork.CommitCallCount);

        Assert.Equal(
            1,
            fixture.UnitOfWork.RollbackCallCount);
    }

    private static async Task AssertFailureAsync(
        Fixture fixture,
        Error expectedError,
        bool transactionStarted = true)
    {
        var result =
            await fixture.Handler.HandleAsync(
                fixture.CreateCommand());

        Assert.True(result.IsFailure);
        Assert.Equal(
            expectedError,
            result.Error);

        Assert.Equal(
            0,
            fixture.UnitOfWork.SaveChangesCallCount);

        Assert.Equal(
            0,
            fixture.UnitOfWork.CommitCallCount);

        if (transactionStarted)
        {
            Assert.Equal(
                1,
                fixture.UnitOfWork.BeginTransactionCallCount);

            Assert.Equal(
                1,
                fixture.UnitOfWork.RollbackCallCount);
        }
        else
        {
            Assert.Equal(
                0,
                fixture.UnitOfWork.BeginTransactionCallCount);

            Assert.Equal(
                0,
                fixture.UnitOfWork.RollbackCallCount);
        }
    }

    private static void AssertNothingPersisted(
        Fixture fixture)
    {
        Assert.Equal(
            0,
            fixture.UnitOfWork.BeginTransactionCallCount);

        Assert.Equal(
            0,
            fixture.UnitOfWork.SaveChangesCallCount);

        Assert.Equal(
            0,
            fixture.UnitOfWork.CommitCallCount);

        Assert.Equal(
            0,
            fixture.UnitOfWork.RollbackCallCount);
    }

    private static Fixture CreateFixture(
        UserRole requesterRole =
            UserRole.TenantAdmin,
        ProjectStatus projectStatus =
            ProjectStatus.Planning,
        ProjectTaskStatus taskStatus =
            ProjectTaskStatus.Backlog,
        bool grantEditTask = true,
        long? responsibleUserId = 10)
    {
        var tenant =
            new Tenant(
                "Empresa Move Task To Todo",
                $"REG-{Guid.NewGuid():N}",
                $"tenant-{Guid.NewGuid():N}@test.local");

        EntityTestHelper.SetId(
            tenant,
            42);

        var requester =
            new User(
                requesterRole == UserRole.SystemAdmin
                    ? null
                    : tenant.Id,
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
                "Projeto Move Task To Todo",
                requester.Id);

        EntityTestHelper.SetId(
            project,
            100);

        SetProjectStatus(
            project,
            projectStatus);

        var projectTask =
            new ProjectTask(
                project.Id,
                "Tarefa no backlog",
                ProjectTaskPriority.Medium,
                requester.Id,
                responsibleUserId:
                    responsibleUserId);

        EntityTestHelper.SetId(
            projectTask,
            200);

        SetTaskStatus(
            projectTask,
            taskStatus);

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

        if (requesterRole ==
                UserRole.ProjectManager ||
            requesterRole ==
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

        var handler =
            new MoveProjectTaskToTodoHandler(
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
            project,
            projectTask,
            requesterMember,
            tenantRepository,
            userRepository,
            projectRepository,
            memberRepository,
            permissionRepository,
            taskRepository,
            unitOfWork,
            handler);
    }

    private static void SetProjectStatus(
        Project project,
        ProjectStatus status)
    {
        switch (status)
        {
            case ProjectStatus.Planning:
                return;

            case ProjectStatus.InProgress:
                project.Start();
                return;

            case ProjectStatus.Paused:
                project.Start();

                project.Pause(
                    "Projeto pausado para teste.");

                return;

            case ProjectStatus.Completed:
                project.Start();

                project.Complete(
                    new[]
                    {
                        ProjectTaskStatus.Done
                    });

                return;

            case ProjectStatus.Archived:
                project.Archive();
                return;

            default:
                throw new ArgumentOutOfRangeException(
                    nameof(status));
        }
    }

    private static void SetTaskStatus(
        ProjectTask task,
        ProjectTaskStatus status)
    {
        switch (status)
        {
            case ProjectTaskStatus.Backlog:
                return;

            case ProjectTaskStatus.Todo:
                task.MoveToTodo();
                return;

            case ProjectTaskStatus.InProgress:
                task.MoveToTodo();
                task.Start();
                return;

            case ProjectTaskStatus.Paused:
                task.MoveToTodo();
                task.Start();

                task.Pause(
                    "Tarefa pausada para teste.");

                return;

            case ProjectTaskStatus.Validation:
                task.MoveToTodo();
                task.Start();
                task.SendToValidation();
                task.ClaimValidation(999);
                return;

            case ProjectTaskStatus.Done:
                task.MoveToTodo();
                task.Start();
                task.SendToValidation();
                task.ClaimValidation(999);
                task.ApproveValidation(999);
                return;

            case ProjectTaskStatus.Cancelled:
                task.Cancel();
                return;

            default:
                throw new ArgumentOutOfRangeException(
                    nameof(status));
        }
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
        FakeUnitOfWork UnitOfWork,
        MoveProjectTaskToTodoHandler Handler)
    {
        public MoveProjectTaskToTodoCommand CreateCommand()
        {
            return new MoveProjectTaskToTodoCommand(
                Tenant.PublicId,
                Project.PublicId,
                ProjectTask.PublicId,
                Requester.PublicId);
        }
    }
}