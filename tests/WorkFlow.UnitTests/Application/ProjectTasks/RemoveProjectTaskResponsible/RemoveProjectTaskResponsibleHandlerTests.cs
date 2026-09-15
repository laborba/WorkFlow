using WorkFlow.Application.Common.Errors;
using WorkFlow.Application.Projects;
using WorkFlow.Application.ProjectTasks;
using WorkFlow.Application.ProjectTasks.RemoveProjectTaskResponsible;
using WorkFlow.Application.Tenants;
using WorkFlow.Application.Users;
using WorkFlow.Domain.Entities;
using WorkFlow.Domain.Enums;
using WorkFlow.UnitTests.Application.Projects.Fakes;
using WorkFlow.UnitTests.Application.Tenants.Fakes;
using WorkFlow.UnitTests.Application.Users.Fakes;
using WorkFlow.UnitTests.Common;
using WorkFlow.UnitTests.Common.Fakes;

namespace WorkFlow.UnitTests.Application.ProjectTasks.RemoveProjectTaskResponsible;

public sealed class RemoveProjectTaskResponsibleHandlerTests
{
    [Theory]
    [InlineData(
        UserRole.TenantAdmin,
        ProjectStatus.Planning,
        ProjectTaskStatus.Backlog,
        ProjectTaskStatus.Backlog)]
    [InlineData(
        UserRole.TenantAdmin,
        ProjectStatus.InProgress,
        ProjectTaskStatus.Todo,
        ProjectTaskStatus.Todo)]
    [InlineData(
        UserRole.TenantAdmin,
        ProjectStatus.Paused,
        ProjectTaskStatus.InProgress,
        ProjectTaskStatus.Todo)]
    [InlineData(
        UserRole.ProjectManager,
        ProjectStatus.Planning,
        ProjectTaskStatus.Backlog,
        ProjectTaskStatus.Backlog)]
    [InlineData(
        UserRole.Member,
        ProjectStatus.InProgress,
        ProjectTaskStatus.Todo,
        ProjectTaskStatus.Todo)]
    [InlineData(
        UserRole.Member,
        ProjectStatus.Paused,
        ProjectTaskStatus.Paused,
        ProjectTaskStatus.Paused)]
    [InlineData(
        UserRole.TenantAdmin,
        ProjectStatus.Planning,
        ProjectTaskStatus.Done,
        ProjectTaskStatus.Done)]
    [InlineData(
        UserRole.TenantAdmin,
        ProjectStatus.Planning,
        ProjectTaskStatus.Cancelled,
        ProjectTaskStatus.Cancelled)]
    public async Task
        HandleAsync_ShouldRemoveResponsible_WhenAuthorized(
            UserRole requesterRole,
            ProjectStatus projectStatus,
            ProjectTaskStatus initialTaskStatus,
            ProjectTaskStatus expectedTaskStatus)
    {
        var fixture =
            CreateFixture(
                requesterRole,
                projectStatus,
                initialTaskStatus);

        var before =
            DateTime.UtcNow;

        var result =
            await fixture.Handler.HandleAsync(
                fixture.CreateCommand());

        Assert.True(
            result.IsSuccess);

        Assert.Null(
            result.Error);

        var response =
            Assert.IsType<RemoveProjectTaskResponsibleResult>(
                result.Value);

        Assert.Null(
            fixture.ProjectTask.ResponsibleUserId);

        Assert.Equal(
            expectedTaskStatus,
            fixture.ProjectTask.Status);

        Assert.NotNull(
            fixture.ProjectTask.UpdatedAt);

        Assert.InRange(
            fixture.ProjectTask.UpdatedAt!.Value,
            before,
            DateTime.UtcNow);

        Assert.Equal(
            fixture.ProjectTask.PublicId,
            response.PublicId);

        Assert.Equal(
            fixture.Tenant.PublicId,
            response.TenantPublicId);

        Assert.Equal(
            fixture.Project.PublicId,
            response.ProjectPublicId);

        Assert.Null(
            response.ResponsibleUserPublicId);

        Assert.Equal(
            expectedTaskStatus,
            response.Status);

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
                    fixture.MemberRepository
                        .GetActiveCalls));

            Assert.Equal(
                (
                    fixture.RequesterMember!.Id,
                    ProjectPermission.AssignTask
                ),
                Assert.Single(
                    fixture.PermissionRepository
                        .IsActivePermissionCalls));
        }
    }

    [Fact]
    public async Task
        HandleAsync_ShouldChangePausedResumeStatusToTodo_WhenPausedFromInProgress()
    {
        var fixture =
            CreateFixture(
                UserRole.Member,
                ProjectStatus.Paused,
                ProjectTaskStatus.Paused);

        Assert.Equal(
            ProjectTaskStatus.InProgress,
            fixture.ProjectTask.StatusBeforePause);

        var result =
            await fixture.Handler.HandleAsync(
                fixture.CreateCommand());

        Assert.True(
            result.IsSuccess);

        Assert.Null(
            fixture.ProjectTask.ResponsibleUserId);

        Assert.Equal(
            ProjectTaskStatus.Paused,
            fixture.ProjectTask.Status);

        Assert.Equal(
            ProjectTaskStatus.Todo,
            fixture.ProjectTask.StatusBeforePause);

        fixture.ProjectTask.Resume();

        Assert.Equal(
            ProjectTaskStatus.Todo,
            fixture.ProjectTask.Status);

        Assert.Null(
            fixture.ProjectTask.ResponsibleUserId);
    }

    [Fact]
    public async Task
        HandleAsync_ShouldBeIdempotent_WhenTaskAlreadyHasNoResponsible()
    {
        var fixture =
            CreateFixture();

        fixture.ProjectTask.RemoveResponsible();

        Assert.Null(
            fixture.ProjectTask.ResponsibleUserId);

        var result =
            await fixture.Handler.HandleAsync(
                fixture.CreateCommand());

        Assert.True(
            result.IsSuccess);

        Assert.Null(
            result.Error);

        Assert.Null(
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
    }

    [Fact]
    public async Task
        HandleAsync_ShouldThrow_WhenCommandIsNull()
    {
        var fixture =
            CreateFixture();

        await Assert.ThrowsAsync<ArgumentNullException>(
            () =>
                fixture.Handler.HandleAsync(
                    null!));

        AssertNothingPersisted(
            fixture);
    }

    [Theory]
    [InlineData("tenant")]
    [InlineData("project")]
    [InlineData("task")]
    [InlineData("requester")]
    public async Task
        HandleAsync_ShouldThrow_WhenRequiredPublicIdIsEmpty(
            string field)
    {
        var fixture =
            CreateFixture();

        var command =
            fixture.CreateCommand();

        command =
            field switch
            {
                "tenant" =>
                    command with
                    {
                        TenantPublicId =
                            Guid.Empty
                    },

                "project" =>
                    command with
                    {
                        ProjectPublicId =
                            Guid.Empty
                    },

                "task" =>
                    command with
                    {
                        TaskPublicId =
                            Guid.Empty
                    },

                "requester" =>
                    command with
                    {
                        RequestedByUserPublicId =
                            Guid.Empty
                    },

                _ =>
                    throw new ArgumentOutOfRangeException(
                        nameof(field))
            };

        await Assert.ThrowsAsync<ArgumentException>(
            () =>
                fixture.Handler.HandleAsync(
                    command));

        AssertNothingPersisted(
            fixture);
    }

    [Fact]
    public async Task
        HandleAsync_ShouldReturnNotFound_WhenTenantDoesNotExist()
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
    public async Task
        HandleAsync_ShouldRejectInactiveTenant()
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
    public async Task
        HandleAsync_ShouldReturnNotFound_WhenRequesterDoesNotExist()
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
    public async Task
        HandleAsync_ShouldRejectInactiveRequester()
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
    public async Task
        HandleAsync_ShouldReturnNotFound_WhenProjectDoesNotExist()
    {
        var fixture =
            CreateFixture();

        fixture.ProjectRepository.ProjectToReturn =
            null;

        await AssertFailureAsync(
            fixture,
            ProjectErrors.NotFound);
    }

    [Fact]
    public async Task
    HandleAsync_ShouldReturnNotFound_WhenRequesterIsSystemAdmin()
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

    [Theory]
    [InlineData(UserRole.ProjectManager)]
    [InlineData(UserRole.Member)]
    public async Task
        HandleAsync_ShouldRejectRequester_WhenMembershipIsNotActive(
            UserRole requesterRole)
    {
        var fixture =
            CreateFixture(
                requesterRole);

        fixture.MemberRepository
            .ActiveMembersToReturn[
                (
                    fixture.Project.Id,
                    fixture.Requester.Id
                )] =
            null;

        await AssertFailureAsync(
            fixture,
            ProjectTaskErrors
                .ResponsibleRemovalNotAllowed);

        Assert.Empty(
            fixture.PermissionRepository
                .IsActivePermissionCalls);
    }

    [Theory]
    [InlineData(UserRole.ProjectManager)]
    [InlineData(UserRole.Member)]
    public async Task
        HandleAsync_ShouldRejectRequester_WhenAssignTaskPermissionIsMissing(
            UserRole requesterRole)
    {
        var fixture =
            CreateFixture(
                requesterRole,
                grantAssignTask: false);

        await AssertFailureAsync(
            fixture,
            ProjectTaskErrors
                .ResponsibleRemovalNotAllowed);

        Assert.Equal(
            (
                fixture.RequesterMember!.Id,
                ProjectPermission.AssignTask
            ),
            Assert.Single(
                fixture.PermissionRepository
                    .IsActivePermissionCalls));
    }

    [Theory]
    [InlineData(ProjectStatus.Completed)]
    [InlineData(ProjectStatus.Archived)]
    public async Task
        HandleAsync_ShouldRejectRemoval_WhenProjectStatusBlocksIt(
            ProjectStatus projectStatus)
    {
        var fixture =
            CreateFixture(
                UserRole.TenantAdmin,
                projectStatus);

        await AssertFailureAsync(
            fixture,
            ProjectTaskErrors
                .ResponsibleRemovalBlockedByProjectStatus);

        Assert.NotNull(
            fixture.ProjectTask.ResponsibleUserId);
    }

    [Fact]
    public async Task
        HandleAsync_ShouldReturnNotFound_WhenTaskDoesNotExist()
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
    public async Task
        HandleAsync_ShouldRejectRemoval_WhenTaskIsArchived()
    {
        var fixture =
            CreateFixture();

        fixture.ProjectTask.Cancel();
        fixture.ProjectTask.Archive();

        await AssertFailureAsync(
            fixture,
            ProjectTaskErrors.Archived);

        Assert.NotNull(
            fixture.ProjectTask.ResponsibleUserId);

        Assert.True(
            fixture.ProjectTask.IsArchived);
    }

    [Fact]
    public async Task
        HandleAsync_ShouldRejectRemoval_WhenTaskIsInValidation()
    {
        var fixture =
            CreateFixture(
                UserRole.TenantAdmin,
                ProjectStatus.InProgress,
                ProjectTaskStatus.Validation);

        await AssertFailureAsync(
            fixture,
            ProjectTaskErrors
                .ResponsibleRemovalBlockedByTaskStatus);

        Assert.NotNull(
            fixture.ProjectTask.ResponsibleUserId);

        Assert.Equal(
            ProjectTaskStatus.Validation,
            fixture.ProjectTask.Status);
    }

    [Fact]
    public async Task
        HandleAsync_ShouldRollback_WhenSaveChangesFails()
    {
        var fixture =
            CreateFixture();

        fixture.UnitOfWork.OnSaveChanges =
            _ =>
                throw new InvalidOperationException(
                    "Falha simulada ao salvar.");

        var exception =
            await Assert.ThrowsAsync<InvalidOperationException>(
                () =>
                    fixture.Handler.HandleAsync(
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

        Assert.True(
            result.IsFailure);

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
        bool grantAssignTask = true)
    {
        var tenant =
            new Tenant(
                "Empresa Remove Responsible",
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
                "Projeto Remove Responsible",
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
                "Tarefa com responsável",
                ProjectTaskPriority.High,
                requester.Id,
                responsibleUserId: requester.Id);

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

        ProjectMember? requesterMember =
            null;

        var permissionRepository =
            new FakeProjectMemberPermissionRepository();

        if (requesterRole == UserRole.ProjectManager ||
            requesterRole == UserRole.Member)
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
                        ProjectPermission.AssignTask
                    )] =
                grantAssignTask;
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
            new RemoveProjectTaskResponsibleHandler(
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
                return;

            case ProjectTaskStatus.Done:
                task.MoveToTodo();
                task.Start();
                task.SendToValidation();
                task.ClaimValidation(
                    999);
                task.ApproveValidation(
                    999);
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
        RemoveProjectTaskResponsibleHandler Handler)
    {
        public RemoveProjectTaskResponsibleCommand
            CreateCommand()
        {
            return new RemoveProjectTaskResponsibleCommand(
                Tenant.PublicId,
                Project.PublicId,
                ProjectTask.PublicId,
                Requester.PublicId);
        }
    }
}