using WorkFlow.Application.Projects;
using WorkFlow.Application.Projects.ReopenProject;
using WorkFlow.Application.Tenants;
using WorkFlow.Application.Users;
using WorkFlow.Domain.Enums;

namespace WorkFlow.UnitTests.Application.Projects.ReopenProject;

public sealed class ReopenProjectHandlerTests
{
    [Fact]
    public async Task
    HandleAsync_ShouldReopenProject_WhenRequesterIsTenantAdmin()
    {
        var fixture =
            new ProjectStatusHandlerTestFixture();

        fixture.MoveProjectToCompleted();

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
            ProjectStatus.InProgress,
            result.Value!.Status);

        Assert.Equal(
            ProjectStatus.InProgress,
            fixture.Project.Status);

        Assert.Equal(
            1,
            fixture.UnitOfWork.SaveChangesCallCount);
    }

    [Theory]
    [InlineData(UserRole.ProjectManager)]
    [InlineData(UserRole.Member)]
    public async Task
    HandleAsync_ShouldReopenProject_WhenMemberHasReopenProjectPermission(
        UserRole role)
    {
        var fixture =
            new ProjectStatusHandlerTestFixture(
                role);

        fixture.MoveProjectToCompleted();

        fixture.Authorize(
            ProjectPermission.ReopenProject);

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
            ProjectStatus.InProgress,
            fixture.Project.Status);
    }

    [Fact]
    public async Task
    HandleAsync_ShouldReturnStatusChangeNotAllowed_WhenRequesterIsNotActiveMember()
    {
        var fixture =
            new ProjectStatusHandlerTestFixture(
                UserRole.Member);

        fixture.MoveProjectToCompleted();

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

        fixture.MoveProjectToCompleted();
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
    HandleAsync_ShouldReturnInvalidStatusTransition_WhenProjectIsNotCompleted()
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
    HandleAsync_ShouldThrow_WhenReasonIsEmpty()
    {
        var fixture =
            new ProjectStatusHandlerTestFixture();

        var command =
            new ReopenProjectCommand(
                fixture.Tenant.PublicId,
                fixture.Project.PublicId,
                fixture.Requester.PublicId,
                "   ");

        await Assert.ThrowsAsync<ArgumentException>(
            () => CreateHandler(fixture)
                .HandleAsync(
                    command));
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
            new ReopenProjectCommand(
                emptyField == 0
                    ? Guid.Empty
                    : fixture.Tenant.PublicId,
                emptyField == 1
                    ? Guid.Empty
                    : fixture.Project.PublicId,
                emptyField == 2
                    ? Guid.Empty
                    : fixture.Requester.PublicId,
                "Necessidade de ajustes adicionais.");

        await Assert.ThrowsAsync<ArgumentException>(
            () => CreateHandler(fixture)
                .HandleAsync(
                    command));
    }

    private static ReopenProjectHandler CreateHandler(
        ProjectStatusHandlerTestFixture fixture)
    {
        return new ReopenProjectHandler(
            fixture.TenantRepository,
            fixture.UserRepository,
            fixture.ProjectRepository,
            fixture.ProjectMemberRepository,
            fixture.PermissionRepository,
            fixture.UnitOfWork);
    }

    private static ReopenProjectCommand CreateCommand(
        ProjectStatusHandlerTestFixture fixture)
    {
        return new ReopenProjectCommand(
            fixture.Tenant.PublicId,
            fixture.Project.PublicId,
            fixture.Requester.PublicId,
            "Necessidade de ajustes adicionais.");
    }
}