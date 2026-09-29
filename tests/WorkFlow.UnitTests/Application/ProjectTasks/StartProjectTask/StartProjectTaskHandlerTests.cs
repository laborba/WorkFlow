using WorkFlow.Application.Common.Errors;
using WorkFlow.Application.Projects;
using WorkFlow.Application.ProjectTasks;
using WorkFlow.Application.ProjectTasks.StartProjectTask;
using WorkFlow.Application.Tenants;
using WorkFlow.Application.Users;
using WorkFlow.Domain.Entities;
using WorkFlow.Domain.Enums;
using WorkFlow.UnitTests.Application.Projects.Fakes;
using WorkFlow.UnitTests.Application.Tenants.Fakes;
using WorkFlow.UnitTests.Application.Users.Fakes;
using WorkFlow.UnitTests.Common;
using WorkFlow.UnitTests.Common.Fakes;

namespace WorkFlow.UnitTests.Application.ProjectTasks.StartProjectTask;

public sealed class StartProjectTaskHandlerTests
{
    [Theory]
    [InlineData(UserRole.TenantAdmin)]
    [InlineData(UserRole.ProjectManager)]
    [InlineData(UserRole.Member)]
    public async Task
        HandleAsync_ShouldStartTask_WhenRequesterIsResponsibleAndActiveMember(
            UserRole role)
    {
        var fixture =
            CreateFixture(role);

        var result =
            await fixture.Handler.HandleAsync(
                CreateCommand(fixture));

        Assert.True(result.IsSuccess);
        Assert.False(result.IsFailure);
        Assert.Null(result.Error);

        var response =
            Assert.IsType<StartProjectTaskResult>(
                result.Value);

        Assert.Equal(
            ProjectTaskStatus.InProgress,
            fixture.ProjectTask.Status);

        Assert.Equal(
            fixture.Requester.Id,
            fixture.ProjectTask.ResponsibleUserId);

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
            ProjectTaskStatus.InProgress,
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

                StartedByUserPublicId =
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
            ProjectTaskStatus.Todo,
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
            ProjectTaskStatus.Todo,
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
            ProjectTaskStatus.Todo,
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
            ProjectTaskStatus.Todo,
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
            ProjectTaskStatus.Todo,
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
            ProjectTaskStatus.Todo,
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
            ProjectTaskErrors.StartBlockedByProjectStatus);

        AssertTransactionalFailure(
            fixture);

        Assert.Empty(
            fixture.TaskRepository.GetForUpdateCalls);

        Assert.Equal(
            ProjectTaskStatus.Todo,
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

        SetTaskStatus(
            fixture.ProjectTask,
            ProjectTaskStatus.Cancelled);

        fixture.ProjectTask.Archive();

        var updatedAtBeforeStart =
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
            updatedAtBeforeStart,
            fixture.ProjectTask.UpdatedAt);
    }

    [Fact]
    public async Task
    HandleAsync_ShouldReturnFailure_WhenTaskIsBacklog()
    {
        var fixture =
            CreateFixture();

        var backlogTask =
            new ProjectTask(
                fixture.Project.Id,
                "Tarefa no backlog",
                ProjectTaskPriority.Medium,
                fixture.Requester.Id,
                responsibleUserId: fixture.Requester.Id);

        EntityTestHelper.SetId(
            backlogTask,
            fixture.ProjectTask.Id);

        fixture.TaskRepository
            .ProjectTaskForUpdateToReturn =
            backlogTask;

        var result =
            await fixture.Handler.HandleAsync(
                new StartProjectTaskCommand(
                    fixture.Tenant.PublicId,
                    fixture.Project.PublicId,
                    backlogTask.PublicId,
                    fixture.Requester.PublicId));

        AssertFailure(
            result,
            ProjectTaskErrors.StartBlockedByTaskStatus);

        AssertTransactionalFailure(
            fixture);

        Assert.Equal(
            ProjectTaskStatus.Backlog,
            backlogTask.Status);
    }

    [Theory]
    [InlineData(ProjectTaskStatus.InProgress)]
    [InlineData(ProjectTaskStatus.Paused)]
    [InlineData(ProjectTaskStatus.Validation)]
    [InlineData(ProjectTaskStatus.Done)]
    [InlineData(ProjectTaskStatus.Cancelled)]
    public async Task
        HandleAsync_ShouldReturnFailure_WhenTaskIsNotTodo(
            ProjectTaskStatus taskStatus)
    {
        var fixture =
            CreateFixture();

        SetTaskStatus(
            fixture.ProjectTask,
            taskStatus);

        var updatedAtBeforeStart =
            fixture.ProjectTask.UpdatedAt;

        var result =
            await fixture.Handler.HandleAsync(
                CreateCommand(fixture));

        AssertFailure(
            result,
            ProjectTaskErrors.StartBlockedByTaskStatus);

        AssertTransactionalFailure(
            fixture);

        Assert.Equal(
            taskStatus,
            fixture.ProjectTask.Status);

        Assert.Equal(
            updatedAtBeforeStart,
            fixture.ProjectTask.UpdatedAt);
    }

    [Fact]
    public async Task
        HandleAsync_ShouldReturnFailure_WhenTaskHasNoResponsible()
    {
        var fixture =
            CreateFixture();

        fixture.ProjectTask.RemoveResponsible();

        var result =
            await fixture.Handler.HandleAsync(
                CreateCommand(fixture));

        AssertFailure(
            result,
            ProjectTaskErrors.StartRequiresResponsible);

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
            ProjectTaskErrors.StartNotAllowed);

        AssertTransactionalFailure(
            fixture);

        Assert.Equal(
            ProjectTaskStatus.Todo,
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
            ProjectTaskErrors.StartNotAllowed);

        AssertTransactionalFailure(
            fixture);

        Assert.Equal(
            ProjectTaskStatus.Todo,
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
                    "Falha simulada ao iniciar tarefa.");

        var exception =
            await Assert.ThrowsAsync<InvalidOperationException>(
                () =>
                    fixture.Handler.HandleAsync(
                        CreateCommand(fixture)));

        Assert.Equal(
            "Falha simulada ao iniciar tarefa.",
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
                "Empresa Start Task",
                $"REG-{Guid.NewGuid():N}",
                $"tenant-{Guid.NewGuid():N}@test.local");

        EntityTestHelper.SetId(
            tenant,
            42);

        var requester =
            new User(
                tenant.Id,
                "Usuário Start",
                $"start-{Guid.NewGuid():N}@test.local",
                "password-hash",
                requesterRole);

        EntityTestHelper.SetId(
            requester,
            10);

        var project =
            new Project(
                tenant.Id,
                "Projeto Start Task",
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
                "Tarefa pronta para iniciar",
                ProjectTaskPriority.Medium,
                requester.Id,
                responsibleUserId: requester.Id);

        EntityTestHelper.SetId(
            projectTask,
            200);

        projectTask.MoveToTodo();

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
            new StartProjectTaskHandler(
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

    private static StartProjectTaskCommand CreateCommand(
        Fixture fixture)
    {
        return new StartProjectTaskCommand(
            fixture.Tenant.PublicId,
            fixture.Project.PublicId,
            fixture.ProjectTask.PublicId,
            fixture.Requester.PublicId);
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
                    project.Start();

                return;

            case ProjectStatus.Paused:
                if (project.Status == ProjectStatus.Planning)
                    project.Start();

                project.Pause(
                    "Pausa para teste.");

                return;

            case ProjectStatus.Completed:
                if (project.Status == ProjectStatus.Planning)
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
    ProjectTask projectTask,
    ProjectTaskStatus status)
    {
        if (projectTask.Status != ProjectTaskStatus.Todo)
        {
            throw new InvalidOperationException(
                "A fixture esperava a tarefa inicialmente em Todo.");
        }

        switch (status)
        {
            case ProjectTaskStatus.Todo:
                return;

            case ProjectTaskStatus.InProgress:
                projectTask.Start();
                return;

            case ProjectTaskStatus.Paused:
                projectTask.Pause(
                    "Pausa para teste.");
                return;

            case ProjectTaskStatus.Validation:
                projectTask.Start();
                projectTask.SendToValidation();
                return;

            case ProjectTaskStatus.Done:
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

    private static void AssertFailure(
        WorkFlow.Application.Common.Results.Result<StartProjectTaskResult> result,
        Error expectedError)
    {
        Assert.True(result.IsFailure);
        Assert.False(result.IsSuccess);
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
        StartProjectTaskHandler Handler);
}