using WorkFlow.Application.Common.Errors;
using WorkFlow.Application.Projects;
using WorkFlow.Application.ProjectTasks;
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

namespace WorkFlow.UnitTests.Application.ProjectTasks.UpdateProjectTask;

public sealed class UpdateProjectTaskHandlerTests
{
    private static readonly DateTime OriginalDueDate =
        new(2027, 5, 10, 12, 0, 0, DateTimeKind.Utc);

    private static readonly DateTime NewDueDate =
        new(2027, 6, 15, 15, 0, 0, DateTimeKind.Utc);

    [Theory]
    [InlineData(
        UserRole.TenantAdmin,
        ProjectStatus.Planning,
        ProjectTaskStatus.Backlog)]
    [InlineData(
        UserRole.TenantAdmin,
        ProjectStatus.InProgress,
        ProjectTaskStatus.InProgress)]
    [InlineData(
        UserRole.TenantAdmin,
        ProjectStatus.Paused,
        ProjectTaskStatus.Paused)]
    [InlineData(
        UserRole.TenantAdmin,
        ProjectStatus.Completed,
        ProjectTaskStatus.Done)]
    [InlineData(
        UserRole.ProjectManager,
        ProjectStatus.Planning,
        ProjectTaskStatus.Todo)]
    [InlineData(
        UserRole.ProjectManager,
        ProjectStatus.Completed,
        ProjectTaskStatus.Done)]
    [InlineData(
        UserRole.Member,
        ProjectStatus.InProgress,
        ProjectTaskStatus.Validation)]
    [InlineData(
        UserRole.Member,
        ProjectStatus.Paused,
        ProjectTaskStatus.Cancelled)]
    [InlineData(
        UserRole.Member,
        ProjectStatus.Planning,
        ProjectTaskStatus.Backlog)]
    public async Task HandleAsync_ShouldUpdateTask_WhenAuthorized(
        UserRole requesterRole,
        ProjectStatus projectStatus,
        ProjectTaskStatus taskStatus)
    {
        var fixture = CreateFixture(
            requesterRole,
            projectStatus,
            taskStatus);

        var originalCreatedAt = fixture.ProjectTask.CreatedAt;
        var originalCreatorId = fixture.ProjectTask.CreatedByUserId;
        var originalResponsibleId = fixture.ProjectTask.ResponsibleUserId;
        var originalValidatorId = fixture.ProjectTask.ValidatorUserId;
        var originalProjectId = fixture.ProjectTask.ProjectId;
        var originalStatusBeforePause =
            fixture.ProjectTask.StatusBeforePause;

        var before = DateTime.UtcNow;

        var result = await fixture.Handler.HandleAsync(
            fixture.CreateCommand());

        Assert.True(result.IsSuccess);
        Assert.Null(result.Error);

        var response = Assert.IsType<UpdateProjectTaskResult>(
            result.Value);

        Assert.Equal(
            "Título atualizado",
            fixture.ProjectTask.Title);

        Assert.Equal(
            "Descrição atualizada",
            fixture.ProjectTask.Description);

        Assert.Equal(
            ProjectTaskPriority.Critical,
            fixture.ProjectTask.Priority);

        Assert.Equal(
            NewDueDate,
            fixture.ProjectTask.DueDate);

        Assert.Equal(
            taskStatus,
            fixture.ProjectTask.Status);

        Assert.Equal(
            originalCreatorId,
            fixture.ProjectTask.CreatedByUserId);

        Assert.Equal(
            originalResponsibleId,
            fixture.ProjectTask.ResponsibleUserId);

        Assert.Equal(
            originalValidatorId,
            fixture.ProjectTask.ValidatorUserId);

        Assert.Equal(
            originalProjectId,
            fixture.ProjectTask.ProjectId);

        Assert.Equal(
            originalCreatedAt,
            fixture.ProjectTask.CreatedAt);

        Assert.Equal(
            originalStatusBeforePause,
            fixture.ProjectTask.StatusBeforePause);

        Assert.NotNull(fixture.ProjectTask.UpdatedAt);

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

        Assert.Equal(
            fixture.ProjectTask.Title,
            response.Title);

        Assert.Equal(
            fixture.ProjectTask.Description,
            response.Description);

        Assert.Equal(
            fixture.ProjectTask.Priority,
            response.Priority);

        Assert.Equal(
            fixture.ProjectTask.Status,
            response.Status);

        Assert.Equal(
            fixture.ProjectTask.DueDate,
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

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task HandleAsync_ShouldRemoveDescription_WhenEmpty(
        string? description)
    {
        var fixture = CreateFixture();

        var command = fixture.CreateCommand() with
        {
            Description = description
        };

        var result = await fixture.Handler.HandleAsync(command);

        Assert.True(result.IsSuccess);
        Assert.Null(fixture.ProjectTask.Description);
        Assert.Null(result.Value!.Description);

        Assert.Equal(
            1,
            fixture.UnitOfWork.SaveChangesCallCount);

        Assert.Equal(
            1,
            fixture.UnitOfWork.CommitCallCount);
    }

    [Fact]
    public async Task HandleAsync_ShouldRemoveDueDate_WhenNull()
    {
        var fixture = CreateFixture();

        Assert.Equal(
            OriginalDueDate,
            fixture.ProjectTask.DueDate);

        var command = fixture.CreateCommand() with
        {
            DueDate = null
        };

        var result = await fixture.Handler.HandleAsync(command);

        Assert.True(result.IsSuccess);
        Assert.Null(fixture.ProjectTask.DueDate);
        Assert.Null(result.Value!.DueDate);

        Assert.Equal(
            1,
            fixture.UnitOfWork.SaveChangesCallCount);
    }

    [Fact]
    public async Task HandleAsync_ShouldNormalizeLocalDueDateToUtc()
    {
        var fixture = CreateFixture();

        var localDate = new DateTime(
            2027,
            6,
            15,
            12,
            0,
            0,
            DateTimeKind.Local);

        var command = fixture.CreateCommand() with
        {
            DueDate = localDate
        };

        var result = await fixture.Handler.HandleAsync(command);

        Assert.True(result.IsSuccess);

        Assert.Equal(
            localDate.ToUniversalTime(),
            fixture.ProjectTask.DueDate);

        Assert.Equal(
            DateTimeKind.Utc,
            fixture.ProjectTask.DueDate!.Value.Kind);

        Assert.Equal(
            fixture.ProjectTask.DueDate,
            result.Value!.DueDate);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task HandleAsync_ShouldRejectInvalidTitle(
        string? title)
    {
        var fixture = CreateFixture();

        var command = fixture.CreateCommand() with
        {
            Title = title!
        };

        await Assert.ThrowsAsync<ArgumentException>(
            () => fixture.Handler.HandleAsync(command));

        AssertOriginalTask(fixture);
        AssertNothingPersisted(fixture);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(99)]
    public async Task HandleAsync_ShouldRejectInvalidPriority(
        int priority)
    {
        var fixture = CreateFixture();

        var command = fixture.CreateCommand() with
        {
            Priority = (ProjectTaskPriority)priority
        };

        await Assert.ThrowsAsync<ArgumentException>(
            () => fixture.Handler.HandleAsync(command));

        AssertOriginalTask(fixture);
        AssertNothingPersisted(fixture);
    }

    [Theory]
    [InlineData("tenant")]
    [InlineData("project")]
    [InlineData("task")]
    [InlineData("requester")]
    public async Task HandleAsync_ShouldRejectEmptyPublicIds(
        string field)
    {
        var fixture = CreateFixture();

        var command = fixture.CreateCommand();

        command = field switch
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
            () => fixture.Handler.HandleAsync(command));

        AssertOriginalTask(fixture);
        AssertNothingPersisted(fixture);
    }

    [Fact]
    public async Task HandleAsync_ShouldRejectDueDateWithoutTimezone()
    {
        var fixture = CreateFixture();

        var command = fixture.CreateCommand() with
        {
            DueDate = new DateTime(
                2027,
                6,
                15,
                12,
                0,
                0,
                DateTimeKind.Unspecified)
        };

        await Assert.ThrowsAsync<ArgumentException>(
            () => fixture.Handler.HandleAsync(command));

        AssertOriginalTask(fixture);
        AssertNothingPersisted(fixture);
    }

    [Fact]
    public async Task HandleAsync_ShouldThrow_WhenCommandIsNull()
    {
        var fixture = CreateFixture();

        await Assert.ThrowsAsync<ArgumentNullException>(
            () => fixture.Handler.HandleAsync(null!));

        AssertOriginalTask(fixture);
        AssertNothingPersisted(fixture);
    }

    [Fact]
    public async Task HandleAsync_ShouldReturnNotFound_WhenTenantDoesNotExist()
    {
        var fixture = CreateFixture();

        fixture.TenantRepository.TenantToReturn = null;

        await AssertFailureAsync(
            fixture,
            TenantErrors.NotFound,
            transactionStarted: false);
    }

    [Fact]
    public async Task HandleAsync_ShouldRejectInactiveTenant()
    {
        var fixture = CreateFixture();

        fixture.Tenant.Deactivate();

        await AssertFailureAsync(
            fixture,
            TenantErrors.Inactive,
            transactionStarted: false);
    }

    [Fact]
    public async Task HandleAsync_ShouldReturnNotFound_WhenRequesterDoesNotExist()
    {
        var fixture = CreateFixture();

        fixture.UserRepository
            .UsersByPublicIdToReturn
            .Remove(fixture.Requester.PublicId);

        await AssertFailureAsync(
            fixture,
            UserErrors.NotFound,
            transactionStarted: false);
    }

    [Fact]
    public async Task HandleAsync_ShouldRejectInactiveRequester()
    {
        var fixture = CreateFixture();

        fixture.Requester.Deactivate();

        await AssertFailureAsync(
            fixture,
            UserErrors.Inactive,
            transactionStarted: false);
    }

    [Fact]
    public async Task HandleAsync_ShouldReturnNotFound_WhenRequesterIsSystemAdmin()
    {
        var fixture = CreateFixture(UserRole.SystemAdmin);

        await AssertFailureAsync(
            fixture,
            UserErrors.NotFound,
            transactionStarted: false);

        Assert.Empty(
            fixture.MemberRepository.GetActiveCalls);

        Assert.Empty(
            fixture.PermissionRepository.IsActivePermissionCalls);
    }

    [Fact]
    public async Task HandleAsync_ShouldReturnNotFound_WhenProjectDoesNotExist()
    {
        var fixture = CreateFixture();

        fixture.ProjectRepository.ProjectToReturn = null;

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
        var fixture = CreateFixture(role);

        fixture.MemberRepository
            .ActiveMembersToReturn[
                (
                    fixture.Project.Id,
                    fixture.Requester.Id
                )] = null;

        await AssertFailureAsync(
            fixture,
            ProjectTaskErrors.UpdateNotAllowed);

        Assert.Empty(
            fixture.PermissionRepository.IsActivePermissionCalls);
    }

    [Theory]
    [InlineData(UserRole.ProjectManager)]
    [InlineData(UserRole.Member)]
    public async Task HandleAsync_ShouldRejectRequesterWithoutEditTask(
        UserRole role)
    {
        var fixture = CreateFixture(
            role,
            grantEditTask: false);

        await AssertFailureAsync(
            fixture,
            ProjectTaskErrors.UpdateNotAllowed);

        Assert.Equal(
            (
                fixture.RequesterMember!.Id,
                ProjectPermission.EditTask
            ),
            Assert.Single(
                fixture.PermissionRepository
                    .IsActivePermissionCalls));
    }

    [Fact]
    public async Task HandleAsync_ShouldRejectArchivedProject()
    {
        var fixture = CreateFixture(
            projectStatus: ProjectStatus.Archived);

        await AssertFailureAsync(
            fixture,
            ProjectTaskErrors.UpdateBlockedByProjectStatus);
    }

    [Fact]
    public async Task HandleAsync_ShouldReturnNotFound_WhenTaskDoesNotExist()
    {
        var fixture = CreateFixture();

        fixture.TaskRepository.ProjectTaskForUpdateToReturn = null;

        await AssertFailureAsync(
            fixture,
            ProjectTaskErrors.NotFound);
    }

    [Fact]
    public async Task HandleAsync_ShouldRejectArchivedTask()
    {
        var fixture = CreateFixture(
            taskStatus: ProjectTaskStatus.Cancelled);

        fixture.ProjectTask.Archive();

        await AssertFailureAsync(
            fixture,
            ProjectTaskErrors.Archived);

        Assert.True(fixture.ProjectTask.IsArchived);
    }

    [Fact]
    public async Task HandleAsync_ShouldRollback_WhenSaveChangesFails()
    {
        var fixture = CreateFixture();

        fixture.UnitOfWork.OnSaveChanges = _ =>
            throw new InvalidOperationException(
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
        var result = await fixture.Handler.HandleAsync(
            fixture.CreateCommand());

        Assert.True(result.IsFailure);
        Assert.Equal(expectedError, result.Error);

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

        AssertOriginalTask(fixture);
    }

    private static void AssertOriginalTask(Fixture fixture)
    {
        Assert.Equal(
            "Título original",
            fixture.ProjectTask.Title);

        Assert.Equal(
            "Descrição original",
            fixture.ProjectTask.Description);

        Assert.Equal(
            ProjectTaskPriority.Medium,
            fixture.ProjectTask.Priority);

        Assert.Equal(
            OriginalDueDate,
            fixture.ProjectTask.DueDate);
    }

    private static void AssertNothingPersisted(Fixture fixture)
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
        UserRole requesterRole = UserRole.TenantAdmin,
        ProjectStatus projectStatus = ProjectStatus.Planning,
        ProjectTaskStatus taskStatus = ProjectTaskStatus.Backlog,
        bool grantEditTask = true)
    {
        var tenant = new Tenant(
            "Empresa Update Task",
            $"REG-{Guid.NewGuid():N}",
            $"tenant-{Guid.NewGuid():N}@test.local");

        EntityTestHelper.SetId(tenant, 42);

        var requester = new User(
            requesterRole == UserRole.SystemAdmin
                ? null
                : tenant.Id,
            "Usuário Solicitante",
            $"requester-{Guid.NewGuid():N}@test.local",
            "password-hash",
            requesterRole);

        EntityTestHelper.SetId(requester, 10);

        var project = new Project(
            tenant.Id,
            "Projeto Update Task",
            requester.Id);

        EntityTestHelper.SetId(project, 100);

        SetProjectStatus(project, projectStatus);

        var projectTask = new ProjectTask(
            project.Id,
            "Título original",
            ProjectTaskPriority.Medium,
            requester.Id,
            description: "Descrição original",
            responsibleUserId: requester.Id,
            dueDate: OriginalDueDate);

        EntityTestHelper.SetId(projectTask, 200);

        SetTaskStatus(projectTask, taskStatus);

        var tenantRepository = new FakeTenantRepository
        {
            TenantToReturn = tenant
        };

        var userRepository = new FakeUserRepository();

        userRepository.UsersByPublicIdToReturn[
            requester.PublicId] = requester;

        var projectRepository = new FakeProjectRepository
        {
            ProjectToReturn = project
        };

        var memberRepository =
            new FakeProjectMemberRepository();

        var permissionRepository =
            new FakeProjectMemberPermissionRepository();

        ProjectMember? requesterMember = null;

        if (requesterRole == UserRole.ProjectManager ||
            requesterRole == UserRole.Member)
        {
            requesterMember = new ProjectMember(
                project.Id,
                requester.Id,
                requester.Id);

            EntityTestHelper.SetId(requesterMember, 300);

            memberRepository.ActiveMembersToReturn[
                (
                    project.Id,
                    requester.Id
                )] = requesterMember;

            permissionRepository.IsActivePermissionResults[
                (
                    requesterMember.Id,
                    ProjectPermission.EditTask
                )] = grantEditTask;
        }

        var taskRepository = new FakeProjectTaskRepository
        {
            ProjectTaskForUpdateToReturn = projectTask
        };

        var unitOfWork = new FakeUnitOfWork();

        var handler = new UpdateProjectTaskHandler(
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
        UpdateProjectTaskHandler Handler)
    {
        public UpdateProjectTaskCommand CreateCommand()
        {
            return new UpdateProjectTaskCommand(
                Tenant.PublicId,
                Project.PublicId,
                ProjectTask.PublicId,
                Requester.PublicId,
                "  Título atualizado  ",
                "  Descrição atualizada  ",
                ProjectTaskPriority.Critical,
                NewDueDate);
        }
    }
}