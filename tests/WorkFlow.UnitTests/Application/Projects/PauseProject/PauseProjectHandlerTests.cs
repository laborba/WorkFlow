using WorkFlow.Application.Projects;
using WorkFlow.Application.Projects.PauseProject;
using WorkFlow.Application.Tenants;
using WorkFlow.Application.Users;
using WorkFlow.Domain.Entities;
using WorkFlow.Domain.Enums;
using WorkFlow.UnitTests.Application.Projects.Fakes;
using WorkFlow.UnitTests.Application.Tenants.Fakes;
using WorkFlow.UnitTests.Application.Users.Fakes;
using WorkFlow.UnitTests.Common;
using WorkFlow.UnitTests.Common.Fakes;

namespace WorkFlow.UnitTests.Application.Projects.PauseProject;

public sealed class PauseProjectHandlerTests
{
    [Fact]
    public async Task
    HandleAsync_ShouldPauseProject_WhenRequesterIsTenantAdmin()
    {
        var fixture =
            CreateFixture(
                UserRole.TenantAdmin);

        var result =
            await fixture.Handler.HandleAsync(
                CreateCommand(fixture));

        Assert.True(result.IsSuccess);
        Assert.False(result.IsFailure);
        Assert.Null(result.Error);

        var response =
            Assert.IsType<ProjectStatusResult>(
                result.Value);

        Assert.Equal(
            ProjectStatus.Paused,
            fixture.Project.Status);

        Assert.Equal(
            ProjectStatus.Paused,
            response.Status);

        Assert.Equal(
            fixture.Project.PublicId,
            response.PublicId);

        Assert.Equal(
            fixture.Tenant.PublicId,
            response.TenantPublicId);

        Assert.Equal(
            fixture.Project.DueDate,
            response.DueDate);

        Assert.NotNull(
            fixture.Project.UpdatedAt);

        Assert.Equal(
            1,
            fixture.UnitOfWork.SaveChangesCallCount);

        Assert.Empty(
            fixture.ProjectMemberRepository.GetActiveCalls);

        Assert.Empty(
            fixture.PermissionRepository.IsActivePermissionCalls);
    }

    [Theory]
    [InlineData(UserRole.ProjectManager)]
    [InlineData(UserRole.Member)]
    public async Task
    HandleAsync_ShouldPauseProject_WhenRequesterHasEditProjectPermission(
        UserRole role)
    {
        var fixture =
            CreateFixture(
                role);

        var projectMember =
            GrantEditProjectPermission(
                fixture);

        var result =
            await fixture.Handler.HandleAsync(
                CreateCommand(fixture));

        Assert.True(result.IsSuccess);

        Assert.Equal(
            ProjectStatus.Paused,
            fixture.Project.Status);

        Assert.Contains(
            (
                fixture.Project.Id,
                fixture.Requester.Id
            ),
            fixture.ProjectMemberRepository.GetActiveCalls);

        Assert.Contains(
            (
                projectMember.Id,
                ProjectPermission.EditProject
            ),
            fixture.PermissionRepository.IsActivePermissionCalls);

        Assert.Equal(
            1,
            fixture.UnitOfWork.SaveChangesCallCount);
    }

    [Fact]
    public async Task
    HandleAsync_ShouldReturnFailure_WhenRequesterIsNotActiveProjectMember()
    {
        var fixture =
            CreateFixture(
                UserRole.ProjectManager);

        var result =
            await fixture.Handler.HandleAsync(
                CreateCommand(fixture));

        Assert.True(result.IsFailure);

        Assert.Equal(
            ProjectErrors.StatusChangeNotAllowed,
            result.Error);

        Assert.Equal(
            ProjectStatus.InProgress,
            fixture.Project.Status);

        Assert.Single(
            fixture.ProjectMemberRepository.GetActiveCalls);

        Assert.Empty(
            fixture.PermissionRepository.IsActivePermissionCalls);

        Assert.Equal(
            0,
            fixture.UnitOfWork.SaveChangesCallCount);
    }

    [Fact]
    public async Task
    HandleAsync_ShouldReturnFailure_WhenRequesterDoesNotHaveEditProjectPermission()
    {
        var fixture =
            CreateFixture(
                UserRole.Member);

        var projectMember =
            AddActiveProjectMember(
                fixture);

        var result =
            await fixture.Handler.HandleAsync(
                CreateCommand(fixture));

        Assert.True(result.IsFailure);

        Assert.Equal(
            ProjectErrors.StatusChangeNotAllowed,
            result.Error);

        Assert.Equal(
            ProjectStatus.InProgress,
            fixture.Project.Status);

        Assert.Contains(
            (
                projectMember.Id,
                ProjectPermission.EditProject
            ),
            fixture.PermissionRepository.IsActivePermissionCalls);

        Assert.Equal(
            0,
            fixture.UnitOfWork.SaveChangesCallCount);
    }

    [Fact]
    public async Task
    HandleAsync_ShouldReturnFailure_WhenProjectIsNotInProgress()
    {
        var fixture =
            CreateFixture(
                UserRole.TenantAdmin);

        fixture.Project.Pause(
            "Pausa anterior");

        var result =
            await fixture.Handler.HandleAsync(
                CreateCommand(fixture));

        Assert.True(result.IsFailure);

        Assert.Equal(
            ProjectErrors.InvalidStatusTransition,
            result.Error);

        Assert.Equal(
            ProjectStatus.Paused,
            fixture.Project.Status);

        Assert.Equal(
            0,
            fixture.UnitOfWork.SaveChangesCallCount);
    }

    [Fact]
    public async Task
    HandleAsync_ShouldThrow_WhenReasonIsEmpty()
    {
        var fixture =
            CreateFixture(
                UserRole.TenantAdmin);

        var command =
            new PauseProjectCommand(
                fixture.Tenant.PublicId,
                fixture.Project.PublicId,
                fixture.Requester.PublicId,
                "   ");

        var exception =
            await Assert.ThrowsAsync<ArgumentException>(
                () => fixture.Handler.HandleAsync(
                    command));

        Assert.Equal(
            "Reason",
            exception.ParamName);

        Assert.Equal(
            ProjectStatus.InProgress,
            fixture.Project.Status);

        Assert.Equal(
            0,
            fixture.UnitOfWork.SaveChangesCallCount);
    }

    [Fact]
    public async Task
    HandleAsync_ShouldReturnFailure_WhenTenantDoesNotExist()
    {
        var fixture =
            CreateFixture(
                UserRole.TenantAdmin);

        fixture.TenantRepository.TenantToReturn =
            null;

        var result =
            await fixture.Handler.HandleAsync(
                CreateCommand(fixture));

        Assert.True(result.IsFailure);

        Assert.Equal(
            TenantErrors.NotFound,
            result.Error);

        Assert.Equal(
            0,
            fixture.UnitOfWork.SaveChangesCallCount);
    }

    [Fact]
    public async Task
    HandleAsync_ShouldReturnFailure_WhenTenantIsInactive()
    {
        var fixture =
            CreateFixture(
                UserRole.TenantAdmin);

        fixture.Tenant.Deactivate();

        var result =
            await fixture.Handler.HandleAsync(
                CreateCommand(fixture));

        Assert.True(result.IsFailure);

        Assert.Equal(
            TenantErrors.Inactive,
            result.Error);

        Assert.Equal(
            0,
            fixture.UnitOfWork.SaveChangesCallCount);
    }

    [Fact]
    public async Task
    HandleAsync_ShouldReturnFailure_WhenRequesterDoesNotExist()
    {
        var fixture =
            CreateFixture(
                UserRole.TenantAdmin);

        fixture.UserRepository.UserToReturn =
            null;

        var result =
            await fixture.Handler.HandleAsync(
                CreateCommand(fixture));

        Assert.True(result.IsFailure);

        Assert.Equal(
            UserErrors.NotFound,
            result.Error);

        Assert.Equal(
            0,
            fixture.UnitOfWork.SaveChangesCallCount);
    }

    [Fact]
    public async Task
    HandleAsync_ShouldReturnFailure_WhenRequesterIsInactive()
    {
        var fixture =
            CreateFixture(
                UserRole.TenantAdmin);

        fixture.Requester.Deactivate();

        var result =
            await fixture.Handler.HandleAsync(
                CreateCommand(fixture));

        Assert.True(result.IsFailure);

        Assert.Equal(
            UserErrors.Inactive,
            result.Error);

        Assert.Equal(
            0,
            fixture.UnitOfWork.SaveChangesCallCount);
    }

    [Fact]
    public async Task
    HandleAsync_ShouldReturnFailure_WhenProjectDoesNotExist()
    {
        var fixture =
            CreateFixture(
                UserRole.TenantAdmin);

        fixture.ProjectRepository.ProjectToReturn =
            null;

        var result =
            await fixture.Handler.HandleAsync(
                CreateCommand(fixture));

        Assert.True(result.IsFailure);

        Assert.Equal(
            ProjectErrors.NotFound,
            result.Error);

        Assert.Equal(
            0,
            fixture.UnitOfWork.SaveChangesCallCount);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    public async Task
    HandleAsync_ShouldThrow_WhenRequiredPublicIdIsEmpty(
        int publicIdToClear)
    {
        var fixture =
            CreateFixture(
                UserRole.TenantAdmin);

        var command =
            new PauseProjectCommand(
                publicIdToClear == 1
                    ? Guid.Empty
                    : fixture.Tenant.PublicId,
                publicIdToClear == 2
                    ? Guid.Empty
                    : fixture.Project.PublicId,
                publicIdToClear == 3
                    ? Guid.Empty
                    : fixture.Requester.PublicId,
                "Aguardando definição externa.");

        await Assert.ThrowsAsync<ArgumentException>(
            () => fixture.Handler.HandleAsync(
                command));

        Assert.Equal(
            ProjectStatus.InProgress,
            fixture.Project.Status);

        Assert.Equal(
            0,
            fixture.UnitOfWork.SaveChangesCallCount);
    }

    private static PauseProjectCommand CreateCommand(
        Fixture fixture)
    {
        return new PauseProjectCommand(
            fixture.Tenant.PublicId,
            fixture.Project.PublicId,
            fixture.Requester.PublicId,
            "Aguardando definição externa.");
    }

    private static ProjectMember AddActiveProjectMember(
        Fixture fixture)
    {
        var projectMember =
            new ProjectMember(
                fixture.Project.Id,
                fixture.Requester.Id,
                fixture.Requester.Id);

        EntityTestHelper.SetId(
            projectMember,
            1000);

        fixture.ProjectMemberRepository
            .ActiveMembersToReturn[
                (
                    fixture.Project.Id,
                    fixture.Requester.Id
                )] =
            projectMember;

        return projectMember;
    }

    private static ProjectMember GrantEditProjectPermission(
        Fixture fixture)
    {
        var projectMember =
            AddActiveProjectMember(
                fixture);

        fixture.PermissionRepository
            .IsActivePermissionResults[
                (
                    projectMember.Id,
                    ProjectPermission.EditProject
                )] =
            true;

        return projectMember;
    }

    private static Fixture CreateFixture(
        UserRole requesterRole)
    {
        var tenant =
            new Tenant(
                "Empresa Pause Project",
                $"REG-{Guid.NewGuid():N}",
                $"tenant-{Guid.NewGuid():N}@test.local");

        EntityTestHelper.SetId(
            tenant,
            42);

        var requester =
            new User(
                tenant.Id,
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
                "Projeto",
                requester.Id,
                dueDate:
                    new DateTime(
                        2026,
                        12,
                        31,
                        12,
                        0,
                        0,
                        DateTimeKind.Utc));

        EntityTestHelper.SetId(
            project,
            100);

        project.Start();

        var tenantRepository =
            new FakeTenantRepository
            {
                TenantToReturn = tenant
            };

        var userRepository =
            new FakeUserRepository
            {
                UserToReturn = requester
            };

        var projectRepository =
            new FakeProjectRepository
            {
                ProjectToReturn = project
            };

        var projectMemberRepository =
            new FakeProjectMemberRepository();

        var permissionRepository =
            new FakeProjectMemberPermissionRepository();

        var unitOfWork =
            new FakeUnitOfWork();

        var handler =
            new PauseProjectHandler(
                tenantRepository,
                userRepository,
                projectRepository,
                projectMemberRepository,
                permissionRepository,
                unitOfWork);

        return new Fixture(
            tenant,
            requester,
            project,
            tenantRepository,
            userRepository,
            projectRepository,
            projectMemberRepository,
            permissionRepository,
            unitOfWork,
            handler);
    }

    private sealed record Fixture(
        Tenant Tenant,
        User Requester,
        Project Project,
        FakeTenantRepository TenantRepository,
        FakeUserRepository UserRepository,
        FakeProjectRepository ProjectRepository,
        FakeProjectMemberRepository ProjectMemberRepository,
        FakeProjectMemberPermissionRepository PermissionRepository,
        FakeUnitOfWork UnitOfWork,
        PauseProjectHandler Handler);
}