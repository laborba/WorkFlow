using WorkFlow.Application.Common.Errors;
using WorkFlow.Application.Common.Results;
using WorkFlow.Application.Projects;
using WorkFlow.Application.ProjectTasks;
using WorkFlow.Application.ProjectTasks.PauseProjectTask;
using WorkFlow.Application.Tenants;
using WorkFlow.Application.Users;
using WorkFlow.Domain.Entities;
using WorkFlow.Domain.Enums;
using WorkFlow.UnitTests.Application.Projects.Fakes;
using WorkFlow.UnitTests.Application.Tenants.Fakes;
using WorkFlow.UnitTests.Application.Users.Fakes;
using WorkFlow.UnitTests.Common;
using WorkFlow.UnitTests.Common.Fakes;

namespace WorkFlow.UnitTests.Application.ProjectTasks.PauseProjectTask;

public sealed class PauseProjectTaskHandlerTests
{
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
        HandleAsync_ShouldPauseTask_WhenRequesterIsResponsibleAndActiveMember(
            UserRole role,
            ProjectTaskStatus statusBeforePause)
    {
        var fixture =
            CreateFixture(
                role,
                statusBeforePause);

        var originalDueDate =
            fixture.ProjectTask.DueDate;

        var result =
            await fixture.Handler.HandleAsync(
                CreateCommand(fixture));

        Assert.True(
            result.IsSuccess);

        Assert.False(
            result.IsFailure);

        Assert.Null(
            result.Error);

        var response =
            Assert.IsType<PauseProjectTaskResult>(
                result.Value);

        Assert.Equal(
            ProjectTaskStatus.Paused,
            fixture.ProjectTask.Status);

        Assert.Equal(
            statusBeforePause,
            fixture.ProjectTask.StatusBeforePause);

        Assert.Equal(
            fixture.Requester.Id,
            fixture.ProjectTask.ResponsibleUserId);

        Assert.Equal(
            originalDueDate,
            fixture.ProjectTask.DueDate);

        Assert.NotNull(
            fixture.ProjectTask.UpdatedAt);

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
            statusBeforePause,
            response.StatusBeforePause);

        Assert.Equal(
            originalDueDate,
            response.DueDate);

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

        var membershipCall =
            Assert.Single(
                fixture.MemberRepository
                    .IsActiveMemberCalls);

        Assert.Equal(
            (
                fixture.Project.Id,
                fixture.Requester.Id
            ),
            membershipCall);

        var taskCall =
            Assert.Single(
                fixture.TaskRepository.GetForUpdateCalls);

        Assert.Equal(
            (
                fixture.Project.Id,
                fixture.ProjectTask.PublicId
            ),
            taskCall);
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

        Assert.Equal(
            0,
            fixture.UnitOfWork.BeginTransactionCallCount);

        Assert.Equal(
            0,
            fixture.UnitOfWork.SaveChangesCallCount);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(4)]
    public async Task
        HandleAsync_ShouldThrow_WhenRequiredPublicIdIsEmpty(
            int publicIdToClear)
    {
        var fixture =
            CreateFixture();

        var command =
            CreateCommand(fixture) with
            {
                TenantPublicId =
                    publicIdToClear == 1
                        ? Guid.Empty
                        : fixture.Tenant.PublicId,

                ProjectPublicId =
                    publicIdToClear == 2
                        ? Guid.Empty
                        : fixture.Project.PublicId,

                TaskPublicId =
                    publicIdToClear == 3
                        ? Guid.Empty
                        : fixture.ProjectTask.PublicId,

                RequestedByUserPublicId =
                    publicIdToClear == 4
                        ? Guid.Empty
                        : fixture.Requester.PublicId
            };

        await Assert.ThrowsAsync<ArgumentException>(
            () =>
                fixture.Handler.HandleAsync(
                    command));

        Assert.Equal(
            0,
            fixture.UnitOfWork.BeginTransactionCallCount);

        Assert.Equal(
            ProjectTaskStatus.InProgress,
            fixture.ProjectTask.Status);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task
        HandleAsync_ShouldThrow_WhenReasonIsInvalid(
            string? reason)
    {
        var fixture =
            CreateFixture();

        var command =
            CreateCommand(fixture) with
            {
                Reason = reason!
            };

        await Assert.ThrowsAsync<ArgumentException>(
            () =>
                fixture.Handler.HandleAsync(
                    command));

        Assert.Equal(
            0,
            fixture.UnitOfWork.BeginTransactionCallCount);

        Assert.Equal(
            ProjectTaskStatus.InProgress,
            fixture.ProjectTask.Status);
    }

    [Fact]
    public async Task
        HandleAsync_ShouldReturnFailure_WhenTenantDoesNotExist()
    {
        var fixture =
            CreateFixture();

        fixture.TenantRepository.TenantToReturn =
            null;

        var result =
            await fixture.Handler.HandleAsync(
                CreateCommand(fixture));

        AssertFailure(
            result,
            TenantErrors.NotFound);

        Assert.Equal(
            0,
            fixture.UnitOfWork.BeginTransactionCallCount);

        Assert.Equal(
            ProjectTaskStatus.InProgress,
            fixture.ProjectTask.Status);
    }

    [Fact]
    public async Task
        HandleAsync_ShouldReturnFailure_WhenTenantIsInactive()
    {
        var fixture =
            CreateFixture();

        fixture.Tenant.Deactivate();

        var result =
            await fixture.Handler.HandleAsync(
                CreateCommand(fixture));

        AssertFailure(
            result,
            TenantErrors.Inactive);

        Assert.Equal(
            0,
            fixture.UnitOfWork.BeginTransactionCallCount);

        Assert.Equal(
            ProjectTaskStatus.InProgress,
            fixture.ProjectTask.Status);
    }

    [Fact]
    public async Task
        HandleAsync_ShouldReturnFailure_WhenRequesterDoesNotExist()
    {
        var fixture =
            CreateFixture();

        fixture.UserRepository
            .UsersByPublicIdToReturn
            .Remove(
                fixture.Requester.PublicId);

        var result =
            await fixture.Handler.HandleAsync(
                CreateCommand(fixture));

        AssertFailure(
            result,
            UserErrors.NotFound);

        Assert.Equal(
            0,
            fixture.UnitOfWork.BeginTransactionCallCount);
    }

    [Fact]
    public async Task
        HandleAsync_ShouldReturnFailure_WhenRequesterIsInactive()
    {
        var fixture =
            CreateFixture();

        fixture.Requester.Deactivate();

        var result =
            await fixture.Handler.HandleAsync(
                CreateCommand(fixture));

        AssertFailure(
            result,
            UserErrors.Inactive);

        Assert.Equal(
            0,
            fixture.UnitOfWork.BeginTransactionCallCount);
    }

    [Fact]
    public async Task
        HandleAsync_ShouldReturnFailure_WhenProjectDoesNotExist()
    {
        var fixture =
            CreateFixture();

        fixture.ProjectRepository.ProjectToReturn =
            null;

        var result =
            await fixture.Handler.HandleAsync(
                CreateCommand(fixture));

        AssertFailure(
            result,
            ProjectErrors.NotFound);

        AssertTransactionalFailure(
            fixture);

        Assert.Empty(
            fixture.TaskRepository.GetForUpdateCalls);
    }

    [Theory]
    [InlineData(ProjectStatus.Planning)]
    [InlineData(ProjectStatus.Paused)]
    [InlineData(ProjectStatus.Completed)]
    [InlineData(ProjectStatus.Archived)]
    public async Task
        HandleAsync_ShouldReturnFailure_WhenProjectIsNotInProgress(
            ProjectStatus projectStatus)
    {
        var fixture =
            CreateFixture(
                projectStatus: projectStatus);

        var result =
            await fixture.Handler.HandleAsync(
                CreateCommand(fixture));

        AssertFailure(
            result,
            ProjectTaskErrors.PauseBlockedByProjectStatus);

        AssertTransactionalFailure(
            fixture);

        Assert.Empty(
            fixture.TaskRepository.GetForUpdateCalls);
    }

    [Fact]
    public async Task
        HandleAsync_ShouldReturnNotFound_WhenTaskDoesNotExist()
    {
        var fixture =
            CreateFixture();

        fixture.TaskRepository.ProjectTaskForUpdateToReturn =
            null;

        var result =
            await fixture.Handler.HandleAsync(
                CreateCommand(fixture));

        AssertFailure(
            result,
            ProjectTaskErrors.NotFound);

        AssertTransactionalFailure(
            fixture);

        var taskCall =
            Assert.Single(
                fixture.TaskRepository.GetForUpdateCalls);

        Assert.Equal(
            (
                fixture.Project.Id,
                fixture.ProjectTask.PublicId
            ),
            taskCall);
    }

    [Fact]
    public async Task
        HandleAsync_ShouldReturnArchived_WhenTaskIsArchived()
    {
        var fixture =
            CreateFixture();

        var archivedTask =
            CreateTaskInStatus(
                fixture,
                ProjectTaskStatus.Cancelled);

        archivedTask.Archive();

        fixture.TaskRepository.ProjectTaskForUpdateToReturn =
            archivedTask;

        var command =
            CreateCommand(
                fixture,
                archivedTask.PublicId);

        var result =
            await fixture.Handler.HandleAsync(
                command);

        AssertFailure(
            result,
            ProjectTaskErrors.Archived);

        AssertTransactionalFailure(
            fixture);

        Assert.True(
            archivedTask.IsArchived);
    }

    [Theory]
    [InlineData(ProjectTaskStatus.Backlog)]
    [InlineData(ProjectTaskStatus.Paused)]
    [InlineData(ProjectTaskStatus.Validation)]
    [InlineData(ProjectTaskStatus.Done)]
    [InlineData(ProjectTaskStatus.Cancelled)]
    public async Task
        HandleAsync_ShouldReturnFailure_WhenTaskStatusCannotBePaused(
            ProjectTaskStatus taskStatus)
    {
        var fixture =
            CreateFixture();

        var task =
            CreateTaskInStatus(
                fixture,
                taskStatus);

        fixture.TaskRepository.ProjectTaskForUpdateToReturn =
            task;

        var updatedAtBefore =
            task.UpdatedAt;

        var result =
            await fixture.Handler.HandleAsync(
                CreateCommand(
                    fixture,
                    task.PublicId));

        AssertFailure(
            result,
            ProjectTaskErrors.PauseBlockedByTaskStatus);

        AssertTransactionalFailure(
            fixture);

        Assert.Equal(
            taskStatus,
            task.Status);

        Assert.Equal(
            updatedAtBefore,
            task.UpdatedAt);
    }

    [Fact]
    public async Task
        HandleAsync_ShouldReturnFailure_WhenTaskHasNoResponsible()
    {
        var fixture =
            CreateFixture(
                taskStatus:
                    ProjectTaskStatus.Todo);

        fixture.ProjectTask.RemoveResponsible();

        var result =
            await fixture.Handler.HandleAsync(
                CreateCommand(fixture));

        AssertFailure(
            result,
            ProjectTaskErrors.PauseRequiresResponsible);

        AssertTransactionalFailure(
            fixture);

        Assert.Equal(
            ProjectTaskStatus.Todo,
            fixture.ProjectTask.Status);

        Assert.Null(
            fixture.ProjectTask.ResponsibleUserId);
    }

    [Fact]
    public async Task
        HandleAsync_ShouldReturnFailure_WhenRequesterIsNotResponsible()
    {
        var fixture =
            CreateFixture();

        fixture.ProjectTask.AssignResponsible(
            999);

        var result =
            await fixture.Handler.HandleAsync(
                CreateCommand(fixture));

        AssertFailure(
            result,
            ProjectTaskErrors.PauseNotAllowed);

        AssertTransactionalFailure(
            fixture);

        Assert.Equal(
            ProjectTaskStatus.InProgress,
            fixture.ProjectTask.Status);

        Assert.Equal(
            999,
            fixture.ProjectTask.ResponsibleUserId);
    }

    [Theory]
    [InlineData(UserRole.TenantAdmin)]
    [InlineData(UserRole.ProjectManager)]
    [InlineData(UserRole.Member)]
    public async Task
        HandleAsync_ShouldReturnFailure_WhenResponsibleIsNotActiveMember(
            UserRole role)
    {
        var fixture =
            CreateFixture(role);

        fixture.MemberRepository
            .IsActiveMemberResults[
                (
                    fixture.Project.Id,
                    fixture.Requester.Id
                )] =
            false;

        var result =
            await fixture.Handler.HandleAsync(
                CreateCommand(fixture));

        AssertFailure(
            result,
            ProjectTaskErrors.PauseNotAllowed);

        AssertTransactionalFailure(
            fixture);

        Assert.Equal(
            ProjectTaskStatus.InProgress,
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
                    "Falha simulada ao pausar tarefa.");

        var exception =
            await Assert.ThrowsAsync<InvalidOperationException>(
                () =>
                    fixture.Handler.HandleAsync(
                        CreateCommand(fixture)));

        Assert.Equal(
            "Falha simulada ao pausar tarefa.",
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

    private static Fixture CreateFixture(
        UserRole requesterRole = UserRole.Member,
        ProjectTaskStatus taskStatus =
            ProjectTaskStatus.InProgress,
        ProjectStatus projectStatus =
            ProjectStatus.InProgress)
    {
        var tenant =
            new Tenant(
                "Empresa Pause Task",
                $"REG-{Guid.NewGuid():N}",
                $"tenant-{Guid.NewGuid():N}@test.local");

        EntityTestHelper.SetId(
            tenant,
            42);

        var requester =
            new User(
                tenant.Id,
                "Usuário Pause",
                $"pause-{Guid.NewGuid():N}@test.local",
                "password-hash",
                requesterRole);

        EntityTestHelper.SetId(
            requester,
            10);

        var project =
            new Project(
                tenant.Id,
                "Projeto Pause Task",
                requester.Id);

        EntityTestHelper.SetId(
            project,
            100);

        SetProjectStatus(
            project,
            projectStatus);

        var dueDate =
            new DateTime(
                2027,
                6,
                30,
                18,
                0,
                0,
                DateTimeKind.Utc);

        var projectTask =
            new ProjectTask(
                project.Id,
                "Tarefa para pausar",
                ProjectTaskPriority.Medium,
                requester.Id,
                responsibleUserId:
                    requester.Id,
                dueDate:
                    dueDate);

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

        memberRepository
            .IsActiveMemberResults[
                (
                    project.Id,
                    requester.Id
                )] =
            true;

        var taskRepository =
            new FakeProjectTaskRepository
            {
                ProjectTaskForUpdateToReturn =
                    projectTask
            };

        var unitOfWork =
            new FakeUnitOfWork();

        var handler =
            new PauseProjectTaskHandler(
                tenantRepository,
                userRepository,
                projectRepository,
                memberRepository,
                taskRepository,
                unitOfWork);

        return new Fixture(
            tenant,
            requester,
            project,
            projectTask,
            tenantRepository,
            userRepository,
            projectRepository,
            memberRepository,
            taskRepository,
            unitOfWork,
            handler);
    }

    private static PauseProjectTaskCommand CreateCommand(
        Fixture fixture,
        Guid? taskPublicId = null)
    {
        return new PauseProjectTaskCommand(
            fixture.Tenant.PublicId,
            fixture.Project.PublicId,
            taskPublicId ??
                fixture.ProjectTask.PublicId,
            fixture.Requester.PublicId,
            "Aguardando retorno necessário.");
    }

    private static ProjectTask CreateTaskInStatus(
        Fixture fixture,
        ProjectTaskStatus status)
    {
        var task =
            new ProjectTask(
                fixture.Project.Id,
                $"Tarefa {status}",
                ProjectTaskPriority.Medium,
                fixture.Requester.Id,
                responsibleUserId:
                    fixture.Requester.Id);

        EntityTestHelper.SetId(
            task,
            201);

        SetTaskStatus(
            task,
            status);

        return task;
    }

    private static void SetTaskStatus(
        ProjectTask projectTask,
        ProjectTaskStatus status)
    {
        switch (status)
        {
            case ProjectTaskStatus.Backlog:
                return;

            case ProjectTaskStatus.Todo:
                projectTask.MoveToTodo();
                return;

            case ProjectTaskStatus.InProgress:
                projectTask.MoveToTodo();
                projectTask.Start();
                return;

            case ProjectTaskStatus.Paused:
                projectTask.MoveToTodo();
                projectTask.Pause(
                    "Pausa anterior.");
                return;

            case ProjectTaskStatus.Validation:
                projectTask.MoveToTodo();
                projectTask.Start();
                projectTask.SendToValidation();
                return;

            case ProjectTaskStatus.Done:
                projectTask.MoveToTodo();
                projectTask.Start();
                projectTask.SendToValidation();
                projectTask.ClaimValidation(
                    projectTask.ResponsibleUserId!.Value);
                projectTask.ApproveValidation(
                    projectTask.ResponsibleUserId.Value);
                return;

            case ProjectTaskStatus.Cancelled:
                projectTask.Cancel();
                return;

            default:
                throw new ArgumentOutOfRangeException(
                    nameof(status));
        }
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
                    "Pausa para teste.");
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

    private static void AssertFailure(
        Result<PauseProjectTaskResult> result,
        Error expectedError)
    {
        Assert.True(
            result.IsFailure);

        Assert.False(
            result.IsSuccess);

        Assert.Equal(
            expectedError,
            result.Error);

        Assert.Null(
            result.Value);
    }

    private static void AssertTransactionalFailure(
        Fixture fixture)
    {
        Assert.Equal(
            1,
            fixture.UnitOfWork.BeginTransactionCallCount);

        Assert.Equal(
            0,
            fixture.UnitOfWork.SaveChangesCallCount);

        Assert.Equal(
            0,
            fixture.UnitOfWork.CommitCallCount);

        Assert.Equal(
            1,
            fixture.UnitOfWork.RollbackCallCount);
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
        FakeProjectTaskRepository TaskRepository,
        FakeUnitOfWork UnitOfWork,
        PauseProjectTaskHandler Handler);
}