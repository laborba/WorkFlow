using WorkFlow.Application.Projects;
using WorkFlow.Application.Projects.CompleteProject;
using WorkFlow.Application.Tenants;
using WorkFlow.Application.Users;
using WorkFlow.Domain.Enums;

namespace WorkFlow.UnitTests.Application.Projects.CompleteProject;

public sealed class CompleteProjectHandlerTests
{
    [Fact]
    public async Task
    HandleAsync_ShouldCompleteProject_WhenRequesterIsTenantAdminAndAllTasksAreClosed()
    {
        var fixture =
            new ProjectStatusHandlerTestFixture();

        fixture.MoveProjectToInProgress();

        fixture.ProjectTaskRepository.StatusesToReturn =
            new[]
            {
                ProjectTaskStatus.Done,
                ProjectTaskStatus.Cancelled
            };

        var handler =
            CreateHandler(
                fixture);

        var result =
            await handler.HandleAsync(
                CreateCommand(
                    fixture));

        Assert.True(
            result.IsSuccess);

        Assert.Equal(
            ProjectStatus.Completed,
            result.Value!.Status);

        Assert.Equal(
            ProjectStatus.Completed,
            fixture.Project.Status);

        Assert.Equal(
            fixture.Project.Id,
            fixture.ProjectTaskRepository.CheckedProjectId);

        Assert.Equal(
            1,
            fixture.UnitOfWork.SaveChangesCallCount);
    }

    [Theory]
    [InlineData(UserRole.ProjectManager)]
    [InlineData(UserRole.Member)]
    public async Task
    HandleAsync_ShouldCompleteProject_WhenMemberHasCompleteProjectPermission(
        UserRole role)
    {
        var fixture =
            new ProjectStatusHandlerTestFixture(
                role);

        fixture.MoveProjectToInProgress();

        fixture.Authorize(
            ProjectPermission.CompleteProject);

        fixture.ProjectTaskRepository.StatusesToReturn =
            new[]
            {
                ProjectTaskStatus.Done
            };

        var handler =
            CreateHandler(
                fixture);

        var result =
            await handler.HandleAsync(
                CreateCommand(
                    fixture));

        Assert.True(
            result.IsSuccess);

        Assert.Equal(
            ProjectStatus.Completed,
            fixture.Project.Status);

        Assert.Equal(
            1,
            fixture.UnitOfWork.SaveChangesCallCount);
    }

    [Fact]
    public async Task
    HandleAsync_ShouldReturnStatusChangeNotAllowed_WhenRequesterIsNotActiveMember()
    {
        var fixture =
            new ProjectStatusHandlerTestFixture(
                UserRole.Member);

        fixture.MoveProjectToInProgress();

        var handler =
            CreateHandler(
                fixture);

        var result =
            await handler.HandleAsync(
                CreateCommand(
                    fixture));

        Assert.True(
            result.IsFailure);

        Assert.Equal(
            ProjectErrors.StatusChangeNotAllowed,
            result.Error);

        Assert.Equal(
            0,
            fixture.UnitOfWork.SaveChangesCallCount);
    }

    [Fact]
    public async Task
    HandleAsync_ShouldReturnStatusChangeNotAllowed_WhenRequesterDoesNotHavePermission()
    {
        var fixture =
            new ProjectStatusHandlerTestFixture(
                UserRole.Member);

        fixture.MoveProjectToInProgress();

        fixture.AddActiveMembership();

        var handler =
            CreateHandler(
                fixture);

        var result =
            await handler.HandleAsync(
                CreateCommand(
                    fixture));

        Assert.True(
            result.IsFailure);

        Assert.Equal(
            ProjectErrors.StatusChangeNotAllowed,
            result.Error);
    }

    [Fact]
    public async Task
    HandleAsync_ShouldReturnHasNoTasks_WhenProjectDoesNotHaveTasks()
    {
        var fixture =
            new ProjectStatusHandlerTestFixture();

        fixture.MoveProjectToInProgress();

        fixture.ProjectTaskRepository.StatusesToReturn =
            Array.Empty<ProjectTaskStatus>();

        var handler =
            CreateHandler(
                fixture);

        var result =
            await handler.HandleAsync(
                CreateCommand(
                    fixture));

        Assert.True(
            result.IsFailure);

        Assert.Equal(
            ProjectErrors.HasNoTasks,
            result.Error);

        Assert.Equal(
            ProjectStatus.InProgress,
            fixture.Project.Status);

        Assert.Equal(
            0,
            fixture.UnitOfWork.SaveChangesCallCount);
    }

    [Fact]
    public async Task
    HandleAsync_ShouldReturnHasOpenTasks_WhenProjectHasOpenTask()
    {
        var fixture =
            new ProjectStatusHandlerTestFixture();

        fixture.MoveProjectToInProgress();

        fixture.ProjectTaskRepository.StatusesToReturn =
            new[]
            {
                ProjectTaskStatus.Done,
                ProjectTaskStatus.InProgress
            };

        var handler =
            CreateHandler(
                fixture);

        var result =
            await handler.HandleAsync(
                CreateCommand(
                    fixture));

        Assert.True(
            result.IsFailure);

        Assert.Equal(
            ProjectErrors.HasOpenTasks,
            result.Error);

        Assert.Equal(
            ProjectStatus.InProgress,
            fixture.Project.Status);

        Assert.Equal(
            0,
            fixture.UnitOfWork.SaveChangesCallCount);
    }

    [Fact]
    public async Task
    HandleAsync_ShouldReturnInvalidStatusTransition_WhenProjectIsNotInProgress()
    {
        var fixture =
            new ProjectStatusHandlerTestFixture();

        var handler =
            CreateHandler(
                fixture);

        var result =
            await handler.HandleAsync(
                CreateCommand(
                    fixture));

        Assert.True(
            result.IsFailure);

        Assert.Equal(
            ProjectErrors.InvalidStatusTransition,
            result.Error);

        Assert.Null(
            fixture.ProjectTaskRepository.CheckedProjectId);
    }

    [Fact]
    public async Task
    HandleAsync_ShouldReturnTenantInactive_WhenTenantIsInactive()
    {
        var fixture =
            new ProjectStatusHandlerTestFixture();

        fixture.Tenant.Deactivate();

        var handler =
            CreateHandler(
                fixture);

        var result =
            await handler.HandleAsync(
                CreateCommand(
                    fixture));

        Assert.True(
            result.IsFailure);

        Assert.Equal(
            TenantErrors.Inactive,
            result.Error);
    }

    [Fact]
    public async Task
    HandleAsync_ShouldReturnUserInactive_WhenRequesterIsInactive()
    {
        var fixture =
            new ProjectStatusHandlerTestFixture();

        fixture.Requester.Deactivate();

        var handler =
            CreateHandler(
                fixture);

        var result =
            await handler.HandleAsync(
                CreateCommand(
                    fixture));

        Assert.True(
            result.IsFailure);

        Assert.Equal(
            UserErrors.Inactive,
            result.Error);
    }

    [Fact]
    public async Task
    HandleAsync_ShouldReturnProjectNotFound_WhenProjectDoesNotExist()
    {
        var fixture =
            new ProjectStatusHandlerTestFixture();

        fixture.ProjectRepository.ProjectToReturn =
            null;

        var handler =
            CreateHandler(
                fixture);

        var result =
            await handler.HandleAsync(
                CreateCommand(
                    fixture));

        Assert.True(
            result.IsFailure);

        Assert.Equal(
            ProjectErrors.NotFound,
            result.Error);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    public async Task
    HandleAsync_ShouldThrow_WhenRequiredPublicIdIsEmpty(
        int emptyField)
    {
        var fixture =
            new ProjectStatusHandlerTestFixture();

        var command =
            new CompleteProjectCommand(
                emptyField == 0
                    ? Guid.Empty
                    : fixture.Tenant.PublicId,
                emptyField == 1
                    ? Guid.Empty
                    : fixture.Project.PublicId,
                emptyField == 2
                    ? Guid.Empty
                    : fixture.Requester.PublicId);

        var handler =
            CreateHandler(
                fixture);

        await Assert.ThrowsAsync<ArgumentException>(
            () => handler.HandleAsync(
                command));
    }

    private static CompleteProjectHandler CreateHandler(
        ProjectStatusHandlerTestFixture fixture)
    {
        return new CompleteProjectHandler(
            fixture.TenantRepository,
            fixture.UserRepository,
            fixture.ProjectRepository,
            fixture.ProjectMemberRepository,
            fixture.PermissionRepository,
            fixture.ProjectTaskRepository,
            fixture.UnitOfWork);
    }

    private static CompleteProjectCommand CreateCommand(
        ProjectStatusHandlerTestFixture fixture)
    {
        return new CompleteProjectCommand(
            fixture.Tenant.PublicId,
            fixture.Project.PublicId,
            fixture.Requester.PublicId);
    }
}