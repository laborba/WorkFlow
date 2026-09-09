using WorkFlow.Application.Abstractions.Persistence;
using WorkFlow.Application.Projects;
using WorkFlow.Application.Projects.RemoveProjectMember;
using WorkFlow.Application.Tenants;
using WorkFlow.Application.Users;
using WorkFlow.Domain.Entities;
using WorkFlow.Domain.Enums;
using WorkFlow.UnitTests.Application.Projects.Fakes;
using WorkFlow.UnitTests.Application.Tenants.Fakes;
using WorkFlow.UnitTests.Application.Users.Fakes;
using WorkFlow.UnitTests.Common;

namespace WorkFlow.UnitTests.Application.Projects.RemoveProjectMember;

public sealed class RemoveProjectMemberHandlerTests
{
    [Fact]
    public async Task
    HandleAsync_ShouldRemoveMember_WhenRequesterIsTenantAdmin()
    {
        var fixture =
            CreateFixture(
                UserRole.TenantAdmin);

        var projectMember =
            new ProjectMember(
                fixture.Project.Id,
                fixture.TargetUser.Id,
                fixture.Requester.Id);

        fixture.ProjectMemberRepository.ActiveMemberToReturn =
            projectMember;

        var result =
            await fixture.Handler.HandleAsync(
                CreateCommand(fixture));

        Assert.True(result.IsSuccess);
        Assert.False(result.IsFailure);
        Assert.Null(result.Error);

        var response =
            Assert.IsType<RemoveProjectMemberResult>(
                result.Value);

        Assert.Equal(
            fixture.Project.PublicId,
            response.ProjectPublicId);

        Assert.Equal(
            fixture.TargetUser.PublicId,
            response.UserPublicId);

        Assert.NotNull(
            projectMember.RemovedAt);

        Assert.Equal(
            projectMember.RemovedAt,
            response.RemovedAt);

        Assert.Equal(
            fixture.Project.Id,
            fixture.ProjectMemberRepository
                .LastGetActiveForUpdateProjectId);

        Assert.Equal(
            fixture.TargetUser.Id,
            fixture.ProjectMemberRepository
                .LastGetActiveForUpdateUserId);

        Assert.Empty(
            fixture.ProjectMemberRepository
                .IsActiveMemberCalls);

        Assert.Equal(
            1,
            fixture.UnitOfWork.SaveChangesCallCount);
    }

    [Fact]
    public async Task
    HandleAsync_ShouldRemoveMember_WhenRequesterIsActiveProjectManager()
    {
        var fixture =
            CreateFixture(
                UserRole.ProjectManager);

        fixture.ProjectMemberRepository
            .IsActiveMemberResult = true;

        fixture.ProjectMemberRepository
            .ActiveMemberToReturn =
                new ProjectMember(
                    fixture.Project.Id,
                    fixture.TargetUser.Id,
                    fixture.Requester.Id);

        var result =
            await fixture.Handler.HandleAsync(
                CreateCommand(fixture));

        Assert.True(
            result.IsSuccess);

        var membershipCall =
            Assert.Single(
                fixture.ProjectMemberRepository
                    .IsActiveMemberCalls);

        Assert.Equal(
            (fixture.Project.Id, fixture.Requester.Id),
            membershipCall);

        Assert.Equal(
            1,
            fixture.UnitOfWork.SaveChangesCallCount);
    }

    [Fact]
    public async Task
    HandleAsync_ShouldReturnFailure_WhenProjectManagerIsNotActiveMember()
    {
        var fixture =
            CreateFixture(
                UserRole.ProjectManager);

        fixture.ProjectMemberRepository
            .IsActiveMemberResult = false;

        var result =
            await fixture.Handler.HandleAsync(
                CreateCommand(fixture));

        Assert.True(
            result.IsFailure);

        Assert.Equal(
            ProjectMemberErrors.RemoveNotAllowed,
            result.Error);

        Assert.Single(
            fixture.ProjectMemberRepository
                .IsActiveMemberCalls);

        Assert.Equal(
            1,
            fixture.UserRepository
                .CheckedGetByPublicIdCalls.Count);

        Assert.Null(
            fixture.ProjectMemberRepository
                .LastGetActiveForUpdateProjectId);

        Assert.Equal(
            0,
            fixture.UnitOfWork.SaveChangesCallCount);
    }

    [Fact]
    public async Task
    HandleAsync_ShouldReturnFailure_WhenRequesterIsMember()
    {
        var fixture =
            CreateFixture(
                UserRole.Member);

        var result =
            await fixture.Handler.HandleAsync(
                CreateCommand(fixture));

        Assert.True(
            result.IsFailure);

        Assert.Equal(
            ProjectMemberErrors.RemoveNotAllowed,
            result.Error);

        Assert.Empty(
            fixture.ProjectMemberRepository
                .IsActiveMemberCalls);

        Assert.Equal(
            1,
            fixture.UserRepository
                .CheckedGetByPublicIdCalls.Count);

        Assert.Null(
            fixture.ProjectMemberRepository
                .LastGetActiveForUpdateProjectId);

        Assert.Equal(
            0,
            fixture.UnitOfWork.SaveChangesCallCount);
    }

    [Fact]
    public async Task
    HandleAsync_ShouldReturnFailure_WhenProjectIsArchived()
    {
        var fixture =
            CreateFixture(
                UserRole.TenantAdmin);

        fixture.Project.Archive();

        var result =
            await fixture.Handler.HandleAsync(
                CreateCommand(fixture));

        Assert.True(
            result.IsFailure);

        Assert.Equal(
            ProjectErrors.Archived,
            result.Error);

        Assert.Equal(
            1,
            fixture.UserRepository
                .CheckedGetByPublicIdCalls.Count);

        Assert.Null(
            fixture.ProjectMemberRepository
                .LastGetActiveForUpdateProjectId);

        Assert.Equal(
            0,
            fixture.UnitOfWork.SaveChangesCallCount);
    }

    [Fact]
    public async Task
    HandleAsync_ShouldReturnFailure_WhenTargetUserDoesNotExist()
    {
        var fixture =
            CreateFixture(
                UserRole.TenantAdmin);

        fixture.UserRepository
            .UsersByPublicIdToReturn
            .Remove(
                fixture.TargetUser.PublicId);

        var result =
            await fixture.Handler.HandleAsync(
                CreateCommand(fixture));

        Assert.True(
            result.IsFailure);

        Assert.Equal(
            UserErrors.NotFound,
            result.Error);

        Assert.Null(
            fixture.ProjectMemberRepository
                .LastGetActiveForUpdateProjectId);

        Assert.Equal(
            0,
            fixture.UnitOfWork.SaveChangesCallCount);
    }

    [Fact]
    public async Task
    HandleAsync_ShouldAllowRemovingInactiveTargetUser()
    {
        var fixture =
            CreateFixture(
                UserRole.TenantAdmin);

        fixture.TargetUser.Deactivate();

        var projectMember =
            new ProjectMember(
                fixture.Project.Id,
                fixture.TargetUser.Id,
                fixture.Requester.Id);

        fixture.ProjectMemberRepository
            .ActiveMemberToReturn =
                projectMember;

        var result =
            await fixture.Handler.HandleAsync(
                CreateCommand(fixture));

        Assert.True(
            result.IsSuccess);

        Assert.NotNull(
            projectMember.RemovedAt);

        Assert.Equal(
            1,
            fixture.UnitOfWork.SaveChangesCallCount);
    }

    [Fact]
    public async Task
    HandleAsync_ShouldReturnFailure_WhenTargetHasNoActiveMembership()
    {
        var fixture =
            CreateFixture(
                UserRole.TenantAdmin);

        fixture.ProjectMemberRepository
            .ActiveMemberToReturn = null;

        var result =
            await fixture.Handler.HandleAsync(
                CreateCommand(fixture));

        Assert.True(
            result.IsFailure);

        Assert.Equal(
            ProjectMemberErrors.NotActive,
            result.Error);

        Assert.Equal(
            fixture.Project.Id,
            fixture.ProjectMemberRepository
                .LastGetActiveForUpdateProjectId);

        Assert.Equal(
            fixture.TargetUser.Id,
            fixture.ProjectMemberRepository
                .LastGetActiveForUpdateUserId);

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

        Assert.True(
            result.IsFailure);

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

        Assert.True(
            result.IsFailure);

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

        fixture.UserRepository
            .UsersByPublicIdToReturn
            .Remove(
                fixture.Requester.PublicId);

        var result =
            await fixture.Handler.HandleAsync(
                CreateCommand(fixture));

        Assert.True(
            result.IsFailure);

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

        Assert.True(
            result.IsFailure);

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

        Assert.True(
            result.IsFailure);

        Assert.Equal(
            ProjectErrors.NotFound,
            result.Error);

        Assert.Equal(
            0,
            fixture.UnitOfWork.SaveChangesCallCount);
    }

    [Fact]
    public async Task
    HandleAsync_ShouldThrow_WhenCommandIsNull()
    {
        var fixture =
            CreateFixture(
                UserRole.TenantAdmin);

        await Assert.ThrowsAsync<ArgumentNullException>(
            () =>
                fixture.Handler.HandleAsync(
                    null!));

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
            CreateFixture(
                UserRole.TenantAdmin);

        var command =
            new RemoveProjectMemberCommand(
                publicIdToClear == 1
                    ? Guid.Empty
                    : fixture.Tenant.PublicId,
                publicIdToClear == 2
                    ? Guid.Empty
                    : fixture.Project.PublicId,
                publicIdToClear == 3
                    ? Guid.Empty
                    : fixture.Requester.PublicId,
                publicIdToClear == 4
                    ? Guid.Empty
                    : fixture.TargetUser.PublicId);

        await Assert.ThrowsAsync<ArgumentException>(
            () =>
                fixture.Handler.HandleAsync(
                    command));

        Assert.Equal(
            0,
            fixture.UnitOfWork.SaveChangesCallCount);
    }

    private static Fixture CreateFixture(
        UserRole requesterRole)
    {
        var tenant =
            new Tenant(
                "Empresa Remove Project Member",
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

        var targetUser =
            new User(
                tenant.Id,
                "Usuário Alvo",
                $"target-{Guid.NewGuid():N}@test.local",
                "password-hash",
                UserRole.Member);

        EntityTestHelper.SetId(
            targetUser,
            20);

        var project =
            new Project(
                tenant.Id,
                "Projeto",
                requester.Id);

        EntityTestHelper.SetId(
            project,
            100);

        var tenantRepository =
            new FakeTenantRepository
            {
                TenantToReturn = tenant
            };

        var userRepository =
            new FakeUserRepository();

        userRepository
            .UsersByPublicIdToReturn[requester.PublicId] =
                requester;

        userRepository
            .UsersByPublicIdToReturn[targetUser.PublicId] =
                targetUser;

        var projectRepository =
            new FakeProjectRepository
            {
                ProjectToReturn = project
            };

        var projectMemberRepository =
            new FakeProjectMemberRepository();

        var unitOfWork =
            new TestUnitOfWork();

        var handler =
            new RemoveProjectMemberHandler(
                tenantRepository,
                userRepository,
                projectRepository,
                projectMemberRepository,
                unitOfWork);

        return new Fixture(
            tenant,
            requester,
            targetUser,
            project,
            tenantRepository,
            userRepository,
            projectRepository,
            projectMemberRepository,
            unitOfWork,
            handler);
    }

    private static RemoveProjectMemberCommand CreateCommand(
        Fixture fixture)
    {
        return new RemoveProjectMemberCommand(
            fixture.Tenant.PublicId,
            fixture.Project.PublicId,
            fixture.Requester.PublicId,
            fixture.TargetUser.PublicId);
    }

    private sealed class TestUnitOfWork :
        IUnitOfWork
    {
        public int SaveChangesCallCount { get; private set; }

        public Task<IUnitOfWorkTransaction> BeginTransactionAsync(
            CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException(
                "Este teste não utiliza transações.");
        }

        public Task<int> SaveChangesAsync(
            CancellationToken cancellationToken = default)
        {
            SaveChangesCallCount++;

            return Task.FromResult(1);
        }
    }

    private sealed record Fixture(
        Tenant Tenant,
        User Requester,
        User TargetUser,
        Project Project,
        FakeTenantRepository TenantRepository,
        FakeUserRepository UserRepository,
        FakeProjectRepository ProjectRepository,
        FakeProjectMemberRepository ProjectMemberRepository,
        TestUnitOfWork UnitOfWork,
        RemoveProjectMemberHandler Handler);
}