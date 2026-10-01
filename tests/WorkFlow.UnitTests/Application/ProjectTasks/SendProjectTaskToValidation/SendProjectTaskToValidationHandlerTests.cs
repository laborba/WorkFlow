using System.Reflection;
using WorkFlow.Application.Common.Errors;
using WorkFlow.Application.Projects;
using WorkFlow.Application.ProjectTasks;
using WorkFlow.Application.ProjectTasks.SendProjectTaskToValidation;
using WorkFlow.Application.Tenants;
using WorkFlow.Application.Users;
using WorkFlow.Domain.Entities;
using WorkFlow.Domain.Enums;
using WorkFlow.UnitTests.Application.Projects.Fakes;
using WorkFlow.UnitTests.Application.Tenants.Fakes;
using WorkFlow.UnitTests.Application.Users.Fakes;
using WorkFlow.UnitTests.Common;
using WorkFlow.UnitTests.Common.Fakes;

namespace WorkFlow.UnitTests.Application.ProjectTasks.SendProjectTaskToValidation;

public sealed class SendProjectTaskToValidationHandlerTests
{
    [Theory]
    [InlineData(UserRole.TenantAdmin)]
    [InlineData(UserRole.ProjectManager)]
    [InlineData(UserRole.Member)]
    public async Task
        HandleAsync_ShouldSendTaskToValidation_WhenRequesterIsResponsibleAndActiveMember(
            UserRole role)
    {
        var fixture =
            CreateFixture(role);

        var dueDateBefore =
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
            Assert.IsType<SendProjectTaskToValidationResult>(
                result.Value);

        Assert.Equal(
            ProjectTaskStatus.Validation,
            fixture.ProjectTask.Status);

        Assert.Equal(
            fixture.Requester.Id,
            fixture.ProjectTask.ResponsibleUserId);

        Assert.Null(
            fixture.ProjectTask.ValidatorUserId);

        Assert.Null(
            fixture.ProjectTask.StatusBeforePause);

        Assert.Equal(
            dueDateBefore,
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

        Assert.Null(
            response.ValidatorUserPublicId);

        Assert.Equal(
            ProjectTaskStatus.Validation,
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

        var membershipCall =
            Assert.Single(
                fixture.MemberRepository.GetActiveCalls);

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

        Assert.Equal(
            ProjectTaskStatus.InProgress,
            fixture.ProjectTask.Status);
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

                SentByUserPublicId =
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
            0,
            fixture.UnitOfWork.SaveChangesCallCount);

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

        Assert.Equal(
            ProjectTaskStatus.InProgress,
            fixture.ProjectTask.Status);
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

        Assert.Equal(
            ProjectTaskStatus.InProgress,
            fixture.ProjectTask.Status);
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

        Assert.Equal(
            ProjectTaskStatus.InProgress,
            fixture.ProjectTask.Status);
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
            ProjectTaskErrors
                .SendToValidationBlockedByProjectStatus);

        AssertTransactionalFailure(
            fixture);

        Assert.Empty(
            fixture.TaskRepository.GetForUpdateCalls);

        Assert.Equal(
            ProjectTaskStatus.InProgress,
            fixture.ProjectTask.Status);
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

        fixture.ProjectTask.Cancel();
        fixture.ProjectTask.Archive();

        var updatedAtBefore =
            fixture.ProjectTask.UpdatedAt;

        var result =
            await fixture.Handler.HandleAsync(
                CreateCommand(fixture));

        AssertFailure(
            result,
            ProjectTaskErrors.Archived);

        AssertTransactionalFailure(
            fixture);

        Assert.True(
            fixture.ProjectTask.IsArchived);

        Assert.Equal(
            updatedAtBefore,
            fixture.ProjectTask.UpdatedAt);
    }

    [Theory]
    [InlineData(ProjectTaskStatus.Backlog)]
    [InlineData(ProjectTaskStatus.Todo)]
    [InlineData(ProjectTaskStatus.Paused)]
    [InlineData(ProjectTaskStatus.Validation)]
    [InlineData(ProjectTaskStatus.Done)]
    [InlineData(ProjectTaskStatus.Cancelled)]
    public async Task
        HandleAsync_ShouldReturnFailure_WhenTaskIsNotInProgress(
            ProjectTaskStatus taskStatus)
    {
        var fixture =
            CreateFixture();

        var task =
            CreateTaskWithStatus(
                fixture,
                taskStatus);

        fixture.TaskRepository
            .ProjectTaskForUpdateToReturn =
            task;

        var updatedAtBefore =
            task.UpdatedAt;

        var command =
            new SendProjectTaskToValidationCommand(
                fixture.Tenant.PublicId,
                fixture.Project.PublicId,
                task.PublicId,
                fixture.Requester.PublicId);

        var result =
            await fixture.Handler.HandleAsync(
                command);

        AssertFailure(
            result,
            ProjectTaskErrors
                .SendToValidationBlockedByTaskStatus);

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
            CreateFixture();

        SetResponsibleUserId(
            fixture.ProjectTask,
            null);

        var result =
            await fixture.Handler.HandleAsync(
                CreateCommand(fixture));

        AssertFailure(
            result,
            ProjectTaskErrors
                .SendToValidationRequiresResponsible);

        AssertTransactionalFailure(
            fixture);

        Assert.Equal(
            ProjectTaskStatus.InProgress,
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
            ProjectTaskErrors
                .SendToValidationNotAllowed);

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
            .ActiveMembersToReturn[
                (
                    fixture.Project.Id,
                    fixture.Requester.Id
                )] =
            null;

        var result =
            await fixture.Handler.HandleAsync(
                CreateCommand(fixture));

        AssertFailure(
            result,
            ProjectTaskErrors
                .SendToValidationNotAllowed);

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
                    "Falha simulada ao enviar tarefa para validação.");

        var exception =
            await Assert.ThrowsAsync<InvalidOperationException>(
                () =>
                    fixture.Handler.HandleAsync(
                        CreateCommand(fixture)));

        Assert.Equal(
            "Falha simulada ao enviar tarefa para validação.",
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
        ProjectStatus projectStatus = ProjectStatus.InProgress)
    {
        var tenant =
            new Tenant(
                "Empresa Send Validation",
                $"REG-{Guid.NewGuid():N}",
                $"tenant-{Guid.NewGuid():N}@test.local");

        EntityTestHelper.SetId(
            tenant,
            42);

        var requester =
            new User(
                tenant.Id,
                "Usuário Responsável",
                $"responsible-{Guid.NewGuid():N}@test.local",
                "password-hash",
                requesterRole);

        EntityTestHelper.SetId(
            requester,
            10);

        var project =
            new Project(
                tenant.Id,
                "Projeto Send Validation",
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
                8,
                31,
                18,
                0,
                0,
                DateTimeKind.Utc);

        var projectTask =
            new ProjectTask(
                project.Id,
                "Tarefa em andamento",
                ProjectTaskPriority.Medium,
                requester.Id,
                responsibleUserId:
                    requester.Id,
                dueDate:
                    dueDate);

        EntityTestHelper.SetId(
            projectTask,
            200);

        projectTask.MoveToTodo();
        projectTask.Start();

        var projectMember =
            new ProjectMember(
                project.Id,
                requester.Id,
                requester.Id);

        EntityTestHelper.SetId(
            projectMember,
            300);

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
            .ActiveMembersToReturn[
                (
                    project.Id,
                    requester.Id
                )] =
            projectMember;

        var taskRepository =
            new FakeProjectTaskRepository
            {
                ProjectTaskForUpdateToReturn =
                    projectTask
            };

        var unitOfWork =
            new FakeUnitOfWork();

        var handler =
            new SendProjectTaskToValidationHandler(
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
            projectMember,
            tenantRepository,
            userRepository,
            projectRepository,
            memberRepository,
            taskRepository,
            unitOfWork,
            handler);
    }

    private static SendProjectTaskToValidationCommand CreateCommand(
        Fixture fixture)
    {
        return new SendProjectTaskToValidationCommand(
            fixture.Tenant.PublicId,
            fixture.Project.PublicId,
            fixture.ProjectTask.PublicId,
            fixture.Requester.PublicId);
    }

    private static ProjectTask CreateTaskWithStatus(
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

        switch (status)
        {
            case ProjectTaskStatus.Backlog:
                return task;

            case ProjectTaskStatus.Todo:
                task.MoveToTodo();
                return task;

            case ProjectTaskStatus.Paused:
                task.MoveToTodo();
                task.Start();
                task.Pause(
                    "Pausa para teste.");
                return task;

            case ProjectTaskStatus.Validation:
                task.MoveToTodo();
                task.Start();
                task.SendToValidation();
                return task;

            case ProjectTaskStatus.Done:
                task.MoveToTodo();
                task.Start();
                task.SendToValidation();

                task.ClaimValidation(
                    fixture.Requester.Id);

                task.ApproveValidation(
                    fixture.Requester.Id);

                return task;

            case ProjectTaskStatus.Cancelled:
                task.Cancel();
                return task;

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
                if (project.Status == ProjectStatus.Planning)
                {
                    project.Start();
                }

                return;

            case ProjectStatus.Paused:
                if (project.Status == ProjectStatus.Planning)
                {
                    project.Start();
                }

                project.Pause(
                    "Pausa para teste.");

                return;

            case ProjectStatus.Completed:
                if (project.Status == ProjectStatus.Planning)
                {
                    project.Start();
                }

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

    private static void SetResponsibleUserId(
        ProjectTask projectTask,
        long? responsibleUserId)
    {
        var property =
            typeof(ProjectTask)
                .GetProperty(
                    nameof(ProjectTask.ResponsibleUserId),
                    BindingFlags.Instance |
                    BindingFlags.Public);

        Assert.NotNull(
            property);

        property!.SetValue(
            projectTask,
            responsibleUserId);
    }

    private static void AssertFailure(
        WorkFlow.Application.Common.Results.Result<
            SendProjectTaskToValidationResult> result,
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
        ProjectMember ProjectMember,
        FakeTenantRepository TenantRepository,
        FakeUserRepository UserRepository,
        FakeProjectRepository ProjectRepository,
        FakeProjectMemberRepository MemberRepository,
        FakeProjectTaskRepository TaskRepository,
        FakeUnitOfWork UnitOfWork,
        SendProjectTaskToValidationHandler Handler);
}