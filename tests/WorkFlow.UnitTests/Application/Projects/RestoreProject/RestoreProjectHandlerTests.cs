using WorkFlow.Application.Projects;
using WorkFlow.Application.Projects.RestoreProject;
using WorkFlow.Application.Tenants;
using WorkFlow.Application.Users;
using WorkFlow.Domain.Enums;

namespace WorkFlow.UnitTests.Application.Projects.RestoreProject;

public sealed class RestoreProjectHandlerTests
{
    [Fact]
    public async Task
    HandleAsync_ShouldRestorePlanningProject_WhenRequesterIsTenantAdmin()
    {
        var fixture =
            new ProjectStatusHandlerTestFixture();

        fixture.ArchivePlanningProject();

        var result =
            await CreateHandler(fixture)
                .HandleAsync(
                    CreateCommand(
                        fixture));

        Assert.True(
            result.IsSuccess);

        Assert.Equal(
            ProjectStatus.Planning,
            result.Value!.Status);

        Assert.Equal(
            ProjectStatus.Planning,
            fixture.Project.Status);

        Assert.False(
            fixture.Project.IsArchived);

        Assert.Null(
            fixture.Project.ArchivedAt);

        Assert.Equal(
            1,
            fixture.UnitOfWork.SaveChangesCallCount);
    }

    [Fact]
    public async Task
    HandleAsync_ShouldRestoreCompletedProjectToCompleted()
    {
        var fixture =
            new ProjectStatusHandlerTestFixture();

        fixture.ArchiveCompletedProject();

        var result =
            await CreateHandler(fixture)
                .HandleAsync(
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

        Assert.False(
            fixture.Project.IsArchived);
    }

    [Theory]
    [InlineData(UserRole.ProjectManager)]
    [InlineData(UserRole.Member)]
    public async Task
    HandleAsync_ShouldRestoreProject_WhenMemberHasArchiveProjectPermission(
        UserRole role)
    {
        var fixture =
            new ProjectStatusHandlerTestFixture(
                role);

        fixture.ArchivePlanningProject();

        fixture.Authorize(
            ProjectPermission.ArchiveProject);

        var result =
            await CreateHandler(fixture)
                .HandleAsync(
                    CreateCommand(
                        fixture));

        Assert.True(
            result.IsSuccess);

        Assert.Equal(
            ProjectStatus.Planning,
            fixture.Project.Status);
    }

    [Fact]
    public async Task
    HandleAsync_ShouldReturnStatusChangeNotAllowed_WhenRequesterIsNotActiveMember()
    {
        var fixture =
            new ProjectStatusHandlerTestFixture(
                UserRole.Member);

        fixture.ArchivePlanningProject();

        var result =
            await CreateHandler(fixture)
                .HandleAsync(
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
    HandleAsync_ShouldReturnStatusChangeNotAllowed_WhenPermissionIsMissing()
    {
        var fixture =
            new ProjectStatusHandlerTestFixture(
                UserRole.Member);

        fixture.ArchivePlanningProject();
        fixture.AddActiveMembership();

        var result =
            await CreateHandler(fixture)
                .HandleAsync(
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
    HandleAsync_ShouldReturnInvalidStatusTransition_WhenProjectIsNotArchived()
    {
        var fixture =
            new ProjectStatusHandlerTestFixture();

        var result =
            await CreateHandler(fixture)
                .HandleAsync(
                    CreateCommand(
                        fixture));

        Assert.True(
            result.IsFailure);

        Assert.Equal(
            ProjectErrors.InvalidStatusTransition,
            result.Error);
    }

    [Fact]
    public async Task
    HandleAsync_ShouldReturnTenantInactive_WhenTenantIsInactive()
    {
        var fixture =
            new ProjectStatusHandlerTestFixture();

        fixture.ArchivePlanningProject();
        fixture.Tenant.Deactivate();

        var result =
            await CreateHandler(fixture)
                .HandleAsync(
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

        fixture.ArchivePlanningProject();
        fixture.Requester.Deactivate();

        var result =
            await CreateHandler(fixture)
                .HandleAsync(
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

        var result =
            await CreateHandler(fixture)
                .HandleAsync(
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
            new RestoreProjectCommand(
                emptyField == 0
                    ? Guid.Empty
                    : fixture.Tenant.PublicId,
                emptyField == 1
                    ? Guid.Empty
                    : fixture.Project.PublicId,
                emptyField == 2
                    ? Guid.Empty
                    : fixture.Requester.PublicId);

        await Assert.ThrowsAsync<ArgumentException>(
            () => CreateHandler(fixture)
                .HandleAsync(
                    command));
    }

    private static RestoreProjectHandler CreateHandler(
        ProjectStatusHandlerTestFixture fixture)
    {
        return new RestoreProjectHandler(
            fixture.TenantRepository,
            fixture.UserRepository,
            fixture.ProjectRepository,
            fixture.ProjectMemberRepository,
            fixture.PermissionRepository,
            fixture.UnitOfWork);
    }

    private static RestoreProjectCommand CreateCommand(
        ProjectStatusHandlerTestFixture fixture)
    {
        return new RestoreProjectCommand(
            fixture.Tenant.PublicId,
            fixture.Project.PublicId,
            fixture.Requester.PublicId);
    }
}