using WorkFlow.Application.Common.Errors;
using WorkFlow.Application.Projects;
using WorkFlow.Application.ProjectTasks;
using WorkFlow.Application.ProjectTasks.CreateProjectTask;
using WorkFlow.Application.Tenants;
using WorkFlow.Application.Users;
using WorkFlow.Domain.Entities;
using WorkFlow.Domain.Enums;

namespace WorkFlow.UnitTests.Application.ProjectTasks.CreateProjectTask;

public sealed class CreateProjectTaskHandlerTests
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
    public async Task HandleAsync_ShouldCreateUnassignedBacklogTask_WhenAuthorized(
        UserRole role,
        ProjectStatus status)
    {
        var fixture = new CreateProjectTaskTestFixture(role);
        fixture.SetProjectStatus(status);
        if (role != UserRole.TenantAdmin)
            fixture.Authorize();

        var command = fixture.CreateCommand();
        var before = DateTime.UtcNow;
        var result = await fixture.Handler.HandleAsync(command);

        Assert.True(result.IsSuccess);
        Assert.Null(result.Error);
        var response = Assert.IsType<CreateProjectTaskResult>(result.Value);
        var task = Assert.IsType<ProjectTask>(fixture.TaskRepository.AddedProjectTask);
        Assert.NotEqual(Guid.Empty, task.PublicId);
        Assert.Equal(task.PublicId, response.PublicId);
        Assert.Equal(fixture.Tenant.PublicId, response.TenantPublicId);
        Assert.Equal(fixture.Project.PublicId, response.ProjectPublicId);
        Assert.Equal(fixture.Creator.PublicId, response.CreatedByUserPublicId);
        Assert.Equal(fixture.Project.Id, task.ProjectId);
        Assert.Equal(fixture.Creator.Id, task.CreatedByUserId);
        Assert.Equal("Criar tarefa", task.Title);
        Assert.Equal(task.Title, response.Title);
        Assert.Equal("Descrição da tarefa", task.Description);
        Assert.Equal(task.Description, response.Description);
        Assert.Equal(command.Priority, task.Priority);
        Assert.Equal(task.Priority, response.Priority);
        Assert.Equal(command.DueDate, task.DueDate);
        Assert.Equal(task.DueDate, response.DueDate);
        Assert.Equal(ProjectTaskStatus.Backlog, task.Status);
        Assert.Equal(task.Status, response.Status);
        Assert.Null(task.ResponsibleUserId);
        Assert.Null(response.ResponsibleUserPublicId);
        Assert.Null(task.ValidatorUserId);
        Assert.Null(task.UpdatedAt);
        Assert.Null(task.ArchivedAt);
        Assert.Null(task.StatusBeforePause);
        Assert.InRange(task.CreatedAt, before, DateTime.UtcNow);
        Assert.Equal(task.CreatedAt, response.CreatedAt);
        Assert.Equal(status, fixture.Project.Status);
        Assert.Equal(1, fixture.UnitOfWork.SaveChangesCallCount);
        Assert.Equal(1, fixture.UnitOfWork.BeginTransactionCallCount);
        Assert.Equal(1, fixture.UnitOfWork.CommitCallCount);
        Assert.Equal(0, fixture.UnitOfWork.RollbackCallCount);
        Assert.Equal((fixture.Tenant.Id, fixture.Project.PublicId), 
            Assert.Single(fixture.ProjectRepository.GetForUpdateCalls));
        Assert.Equal(fixture.Tenant.PublicId, fixture.TenantRepository.CheckedPublicId);
        Assert.Equal((fixture.Tenant.Id, fixture.Creator.PublicId),
            Assert.Single(fixture.UserRepository.CheckedGetByPublicIdCalls));

        if (role == UserRole.TenantAdmin)
        {
            Assert.Empty(fixture.MemberRepository.GetActiveCalls);
            Assert.Empty(fixture.PermissionRepository.IsActivePermissionCalls);
        }
        else
        {
            Assert.Equal((fixture.Project.Id, fixture.Creator.Id),
                Assert.Single(fixture.MemberRepository.GetActiveCalls));
            Assert.Equal((50L, ProjectPermission.CreateTask),
                Assert.Single(fixture.PermissionRepository.IsActivePermissionCalls));
        }
    }

    [Theory]
    [InlineData(UserRole.TenantAdmin, ProjectStatus.Completed)]
    [InlineData(UserRole.TenantAdmin, ProjectStatus.Archived)]
    [InlineData(UserRole.ProjectManager, ProjectStatus.Completed)]
    [InlineData(UserRole.ProjectManager, ProjectStatus.Archived)]
    [InlineData(UserRole.Member, ProjectStatus.Completed)]
    [InlineData(UserRole.Member, ProjectStatus.Archived)]
    public async Task HandleAsync_ShouldRejectCreation_WhenProjectStatusBlocksIt(
        UserRole role,
        ProjectStatus status)
    {
        var fixture = new CreateProjectTaskTestFixture(role);
        fixture.SetProjectStatus(status);
        fixture.Authorize();

        await AssertFailureAsync(fixture, ProjectTaskErrors.CreationBlockedByProjectStatus);
        Assert.Equal(status, fixture.Project.Status);
    }

    [Theory]
    [InlineData(UserRole.ProjectManager)]
    [InlineData(UserRole.Member)]
    public async Task HandleAsync_ShouldRejectCreation_WhenMembershipIsNotActive(UserRole role)
    {
        var fixture = new CreateProjectTaskTestFixture(role);
        fixture.PermissionRepository.IsActivePermissionResult = true;

        await AssertFailureAsync(fixture, ProjectTaskErrors.CreationNotAllowed);
        Assert.Empty(fixture.PermissionRepository.IsActivePermissionCalls);
    }

    [Theory]
    [InlineData(UserRole.ProjectManager)]
    [InlineData(UserRole.Member)]
    public async Task HandleAsync_ShouldRejectCreation_WhenCreateTaskPermissionIsMissing(UserRole role)
    {
        var fixture = new CreateProjectTaskTestFixture(role);
        var member = fixture.AddMembership();
        fixture.PermissionRepository.IsActivePermissionResults[
            (member.Id, ProjectPermission.EditProject)] = true;
        fixture.PermissionRepository.IsActivePermissionResults[
            (member.Id, ProjectPermission.AssignTask)] = true;

        await AssertFailureAsync(fixture, ProjectTaskErrors.CreationNotAllowed);
        Assert.Equal((member.Id, ProjectPermission.CreateTask),
            Assert.Single(fixture.PermissionRepository.IsActivePermissionCalls));
    }

    [Fact]
    public async Task HandleAsync_ShouldRejectSystemAdmin_EvenIfRepositoryReturnsIt()
    {
        var fixture = new CreateProjectTaskTestFixture(UserRole.SystemAdmin);
        await AssertFailureAsync(fixture, ProjectTaskErrors.CreationNotAllowed);
        Assert.Empty(fixture.MemberRepository.GetActiveCalls);
    }

    [Fact]
    public async Task HandleAsync_ShouldReturnNotFound_WhenTenantDoesNotExist()
    {
        var fixture = new CreateProjectTaskTestFixture();
        fixture.TenantRepository.TenantToReturn = null;
        await AssertFailureAsync(fixture, TenantErrors.NotFound);
    }

    [Fact]
    public async Task HandleAsync_ShouldRejectInactiveTenant()
    {
        var fixture = new CreateProjectTaskTestFixture();
        fixture.Tenant.Deactivate();
        await AssertFailureAsync(fixture, TenantErrors.Inactive);
    }

    [Fact]
    public async Task HandleAsync_ShouldReturnNotFound_WhenCreatorDoesNotExist()
    {
        var fixture = new CreateProjectTaskTestFixture();
        fixture.UserRepository.UserToReturn = null;
        await AssertFailureAsync(fixture, UserErrors.NotFound);
    }

    [Fact]
    public async Task HandleAsync_ShouldRejectInactiveCreator()
    {
        var fixture = new CreateProjectTaskTestFixture();
        fixture.Creator.Deactivate();
        await AssertFailureAsync(fixture, UserErrors.Inactive);
    }

    [Fact]
    public async Task HandleAsync_ShouldReturnNotFound_WhenProjectDoesNotExist()
    {
        var fixture = new CreateProjectTaskTestFixture();
        fixture.ProjectRepository.ProjectToReturn = null;
        await AssertFailureAsync(fixture, ProjectErrors.NotFound);
    }

    [Fact]
    public async Task HandleAsync_ShouldNotAccessProjectFromAnotherTenant()
    {
        var fixture = new CreateProjectTaskTestFixture();
        var otherProject = new Project(99, "Outro projeto", 100);
        fixture.ProjectRepository.ProjectToReturn = otherProject;
        var command = fixture.CreateCommand() with { ProjectPublicId = otherProject.PublicId };
        var result = await fixture.Handler.HandleAsync(command);
        Assert.Equal(ProjectErrors.NotFound, result.Error);
        AssertNothingPersisted(fixture);
    }

    [Fact]
    public async Task HandleAsync_ShouldNotAcceptCreatorFromAnotherTenant()
    {
        var fixture = new CreateProjectTaskTestFixture();
        var otherUser = new User(99, "Outro usuário", "other@test.local",
            "password-hash", UserRole.TenantAdmin);
        fixture.UserRepository.UsersByPublicIdToReturn[otherUser.PublicId] = otherUser;
        var command = fixture.CreateCommand() with { CreatedByUserPublicId = otherUser.PublicId };
        var result = await fixture.Handler.HandleAsync(command);
        Assert.Equal(UserErrors.NotFound, result.Error);
        AssertNothingPersisted(fixture);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task HandleAsync_ShouldNormalizeOptionalFields(string? description)
    {
        var fixture = new CreateProjectTaskTestFixture();
        var command = fixture.CreateCommand() with { Description = description, DueDate = null };
        var result = await fixture.Handler.HandleAsync(command);
        Assert.True(result.IsSuccess);
        Assert.Null(result.Value!.Description);
        Assert.Null(result.Value.DueDate);
        Assert.Null(fixture.TaskRepository.AddedProjectTask!.Description);
        Assert.Null(fixture.TaskRepository.AddedProjectTask.DueDate);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task HandleAsync_ShouldThrow_WhenTitleIsInvalid(string? title)
    {
        var fixture = new CreateProjectTaskTestFixture();
        await Assert.ThrowsAsync<ArgumentException>(() => fixture.Handler.HandleAsync(
            fixture.CreateCommand() with { Title = title! }));
        AssertNothingPersisted(fixture);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(99)]
    public async Task HandleAsync_ShouldThrow_WhenPriorityIsInvalid(int priority)
    {
        var fixture = new CreateProjectTaskTestFixture();
        await Assert.ThrowsAsync<ArgumentException>(() => fixture.Handler.HandleAsync(
            fixture.CreateCommand() with { Priority = (ProjectTaskPriority)priority }));
        AssertNothingPersisted(fixture);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    public async Task HandleAsync_ShouldThrow_WhenPublicIdIsEmpty(int field)
    {
        var fixture = new CreateProjectTaskTestFixture();
        var command = fixture.CreateCommand();
        command = field switch
        {
            1 => command with { TenantPublicId = Guid.Empty },
            2 => command with { ProjectPublicId = Guid.Empty },
            _ => command with { CreatedByUserPublicId = Guid.Empty }
        };
        await Assert.ThrowsAsync<ArgumentException>(() => fixture.Handler.HandleAsync(command));
        AssertNothingPersisted(fixture);
    }

    [Fact]
    public async Task HandleAsync_ShouldThrow_WhenCommandIsNull()
    {
        var fixture = new CreateProjectTaskTestFixture();
        await Assert.ThrowsAsync<ArgumentNullException>(() => fixture.Handler.HandleAsync(null!));
        AssertNothingPersisted(fixture);
    }

    [Fact]
    public async Task HandleAsync_ShouldPropagatePersistenceFailure()
    {
        var fixture = new CreateProjectTaskTestFixture();
        fixture.UnitOfWork.OnSaveChanges = _ => throw new InvalidOperationException("Falha ao salvar.");
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            fixture.Handler.HandleAsync(fixture.CreateCommand()));
        Assert.Equal(1, fixture.UnitOfWork.SaveChangesCallCount);
        Assert.Equal(1, fixture.UnitOfWork.BeginTransactionCallCount);
        Assert.Equal(0, fixture.UnitOfWork.CommitCallCount);
        Assert.Equal(1, fixture.UnitOfWork.RollbackCallCount);
    }

    [Fact]
    public async Task
    HandleAsync_ShouldNormalizeLocalDueDateToUtc()
    {
        var fixture =
            new CreateProjectTaskTestFixture();

        var localDueDate =
            new DateTime(
                2027,
                1,
                31,
                18,
                0,
                0,
                DateTimeKind.Local);

        var command =
            fixture.CreateCommand() with
            {
                DueDate = localDueDate
            };

        var result =
            await fixture.Handler.HandleAsync(
                command);

        Assert.True(
            result.IsSuccess);

        var expectedUtc =
            localDueDate.ToUniversalTime();

        Assert.Equal(
            expectedUtc,
            result.Value!.DueDate);

        Assert.Equal(
            DateTimeKind.Utc,
            result.Value.DueDate!.Value.Kind);

        Assert.Equal(
            expectedUtc,
            fixture.TaskRepository
                .AddedProjectTask!
                .DueDate);
    }

    [Fact]
    public async Task
    HandleAsync_ShouldThrow_WhenDueDateDoesNotContainTimeZone()
    {
        var fixture =
            new CreateProjectTaskTestFixture();

        var unspecifiedDueDate =
            new DateTime(
                2027,
                1,
                31,
                18,
                0,
                0,
                DateTimeKind.Unspecified);

        var command =
            fixture.CreateCommand() with
            {
                DueDate = unspecifiedDueDate
            };

        var exception =
            await Assert.ThrowsAsync<ArgumentException>(
                () => fixture.Handler.HandleAsync(
                    command));

        Assert.Equal(
            "DueDate",
            exception.ParamName);

        AssertNothingPersisted(
            fixture);

        Assert.Equal(
            0,
            fixture.UnitOfWork.BeginTransactionCallCount);
    }

    private static async Task AssertFailureAsync(CreateProjectTaskTestFixture fixture, Error error)
    {
        var result = await fixture.Handler.HandleAsync(fixture.CreateCommand());
        Assert.True(result.IsFailure);
        Assert.Equal(error, result.Error);
        AssertNothingPersisted(fixture);
    }

    private static void AssertNothingPersisted(CreateProjectTaskTestFixture fixture)
    {
        Assert.Null(fixture.TaskRepository.AddedProjectTask);
        Assert.Equal(0, fixture.UnitOfWork.SaveChangesCallCount);
    }
}
