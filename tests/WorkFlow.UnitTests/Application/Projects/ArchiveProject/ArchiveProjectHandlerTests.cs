using WorkFlow.Application.Projects;
using WorkFlow.Application.Projects.ArchiveProject;
using WorkFlow.Application.Tenants;
using WorkFlow.Application.Users;
using WorkFlow.Domain.Enums;

namespace WorkFlow.UnitTests.Application.Projects.ArchiveProject;

public sealed class ArchiveProjectHandlerTests
{
    [Fact]
    public async Task
    HandleAsync_ShouldArchivePlanningProject_WhenRequesterIsTenantAdmin()
    {
        var fixture =
            new ProjectStatusHandlerTestFixture();

        var result =
            await CreateHandler(fixture)
                .HandleAsync(
                    CreateCommand(
                        fixture));

        Assert.True(
            result.IsSuccess);

        Assert.Equal(
            ProjectStatus.Archived,
            result.Value!.Status);

        Assert.True(
            fixture.Project.IsArchived);

        Assert.NotNull(
            fixture.Project.ArchivedAt);

        Assert.Equal(
            1,
            fixture.UnitOfWork.SaveChangesCallCount);
    }

    [Fact]
    public async Task
    HandleAsync_ShouldArchiveCompletedProject_WhenRequesterIsTenantAdmin()
    {
        var fixture =
            new ProjectStatusHandlerTestFixture();

        fixture.MoveProjectToCompleted();

        var result =
            await CreateHandler(fixture)
                .HandleAsync(
                    CreateCommand(
                        fixture));

        Assert.True(
            result.IsSuccess);

        Assert.Equal(
            ProjectStatus.Archived,
            fixture.Project.Status);

        Assert.True(
            fixture.Project.IsArchived);
    }

    [Theory]
    [InlineData(UserRole.ProjectManager)]
    [InlineData(UserRole.Member)]
    public async Task
    HandleAsync_ShouldArchiveProject_WhenMemberHasArchiveProjectPermission(
        UserRole role)
    {
        var fixture =
            new ProjectStatusHandlerTestFixture(
                role);

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
            ProjectStatus.Archived,
            fixture.Project.Status);
    }

    [Fact]
    public async Task
    HandleAsync_ShouldReturnStatusChangeNotAllowed_WhenRequesterIsNotActiveMember()
    {
        var fixture =
            new ProjectStatusHandlerTestFixture(
                UserRole.Member);

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
    HandleAsync_ShouldReturnInvalidStatusTransition_WhenProjectIsInProgress()
    {
        var fixture =
            new ProjectStatusHandlerTestFixture();

        fixture.MoveProjectToInProgress();

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

        Assert.False(
            fixture.Project.IsArchived);
    }

    [Fact]
    public async Task
    HandleAsync_ShouldReturnTenantInactive_WhenTenantIsInactive()
    {
        var fixture =
            new ProjectStatusHandlerTestFixture();

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
            new ArchiveProjectCommand(
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

    private static ArchiveProjectHandler CreateHandler(
        ProjectStatusHandlerTestFixture fixture)
    {
        return new ArchiveProjectHandler(
            fixture.TenantRepository,
            fixture.UserRepository,
            fixture.ProjectRepository,
            fixture.ProjectMemberRepository,
            fixture.PermissionRepository,
            fixture.UnitOfWork);
    }

    private static ArchiveProjectCommand CreateCommand(
        ProjectStatusHandlerTestFixture fixture)
    {
        return new ArchiveProjectCommand(
            fixture.Tenant.PublicId,
            fixture.Project.PublicId,
            fixture.Requester.PublicId);
    }
}