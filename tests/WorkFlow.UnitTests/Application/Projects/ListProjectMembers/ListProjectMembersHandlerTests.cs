using WorkFlow.Application.Abstractions.Persistence.Models;
using WorkFlow.Application.Common.Pagination;
using WorkFlow.Application.Projects;
using WorkFlow.Application.Projects.ListProjectMembers;
using WorkFlow.Application.Tenants;
using WorkFlow.Application.Users;
using WorkFlow.Domain.Entities;
using WorkFlow.Domain.Enums;
using WorkFlow.UnitTests.Application.Projects.Fakes;
using WorkFlow.UnitTests.Application.Tenants.Fakes;
using WorkFlow.UnitTests.Application.Users.Fakes;
using WorkFlow.UnitTests.Common;

namespace WorkFlow.UnitTests.Application.Projects.ListProjectMembers;

public sealed class ListProjectMembersHandlerTests
{
    [Fact]
    public async Task
    HandleAsync_ShouldReturnPagedMembers_WhenRequesterIsTenantAdmin()
    {
        var fixture =
            CreateFixture(
                UserRole.TenantAdmin);

        var addedByPublicId =
            Guid.NewGuid();

        fixture.ProjectMemberRepository.PagedDataToReturn =
            new PagedData<ProjectMemberListItemData>(
                new[]
                {
                    new ProjectMemberListItemData(
                        Guid.NewGuid(),
                        "Membro Um",
                        "membro@test.local",
                        UserRole.Member,
                        DateTime.UtcNow.AddDays(-2),
                        addedByPublicId)
                },
                1);

        var query =
            new ListProjectMembersQuery(
                fixture.Tenant.PublicId,
                fixture.Project.PublicId,
                fixture.Requester.PublicId,
                2,
                10,
                "membro");

        var result =
            await fixture.Handler.HandleAsync(
                query);

        Assert.True(result.IsSuccess);
        Assert.False(result.IsFailure);
        Assert.Null(result.Error);

        var response =
            Assert.IsType<ListProjectMembersResult>(
                result.Value);

        var item =
            Assert.Single(
                response.Items);

        Assert.Equal(
            "Membro Um",
            item.Name);

        Assert.Equal(
            "membro@test.local",
            item.Email);

        Assert.Equal(
            UserRole.Member,
            item.Role);

        Assert.Equal(
            addedByPublicId,
            item.AddedByUserPublicId);

        Assert.Equal(
            2,
            response.PageNumber);

        Assert.Equal(
            10,
            response.PageSize);

        Assert.Equal(
            1,
            response.TotalCount);

        Assert.Equal(
            1,
            response.TotalPages);

        Assert.Equal(
            fixture.Project.Id,
            fixture.ProjectMemberRepository.LastPagedProjectId);

        Assert.Equal(
            2,
            fixture.ProjectMemberRepository.LastPageNumber);

        Assert.Equal(
            10,
            fixture.ProjectMemberRepository.LastPageSize);

        Assert.Equal(
            "membro",
            fixture.ProjectMemberRepository.LastSearch);

        Assert.Empty(
            fixture.ProjectMemberRepository.IsActiveMemberCalls);
    }

    [Theory]
    [InlineData(UserRole.ProjectManager)]
    [InlineData(UserRole.Member)]
    public async Task
    HandleAsync_ShouldReturnMembers_WhenRequesterIsActiveMember(
        UserRole role)
    {
        var fixture =
            CreateFixture(
                role);

        fixture.ProjectMemberRepository
            .IsActiveMemberResult = true;

        var result =
            await fixture.Handler.HandleAsync(
                CreateQuery(fixture));

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
            fixture.Project.Id,
            fixture.ProjectMemberRepository.LastPagedProjectId);
    }

    [Theory]
    [InlineData(UserRole.ProjectManager)]
    [InlineData(UserRole.Member)]
    public async Task
    HandleAsync_ShouldReturnFailure_WhenRequesterIsNotActiveMember(
        UserRole role)
    {
        var fixture =
            CreateFixture(
                role);

        fixture.ProjectMemberRepository
            .IsActiveMemberResult = false;

        var result =
            await fixture.Handler.HandleAsync(
                CreateQuery(fixture));

        Assert.True(
            result.IsFailure);

        Assert.Equal(
            ProjectErrors.ViewNotAllowed,
            result.Error);

        var membershipCall =
            Assert.Single(
                fixture.ProjectMemberRepository
                    .IsActiveMemberCalls);

        Assert.Equal(
            (fixture.Project.Id, fixture.Requester.Id),
            membershipCall);

        Assert.Null(
            fixture.ProjectMemberRepository.LastPagedProjectId);
    }

    [Fact]
    public async Task
    HandleAsync_ShouldAllowListing_WhenProjectIsArchived()
    {
        var fixture =
            CreateFixture(
                UserRole.TenantAdmin);

        fixture.Project.Archive();

        var result =
            await fixture.Handler.HandleAsync(
                CreateQuery(fixture));

        Assert.True(
            result.IsSuccess);

        Assert.Equal(
            fixture.Project.Id,
            fixture.ProjectMemberRepository.LastPagedProjectId);
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
                CreateQuery(fixture));

        Assert.True(
            result.IsFailure);

        Assert.Equal(
            TenantErrors.NotFound,
            result.Error);

        Assert.Null(
            fixture.ProjectMemberRepository.LastPagedProjectId);
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
                CreateQuery(fixture));

        Assert.True(
            result.IsFailure);

        Assert.Equal(
            TenantErrors.Inactive,
            result.Error);

        Assert.Null(
            fixture.ProjectMemberRepository.LastPagedProjectId);
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
                CreateQuery(fixture));

        Assert.True(
            result.IsFailure);

        Assert.Equal(
            UserErrors.NotFound,
            result.Error);

        Assert.Null(
            fixture.ProjectMemberRepository.LastPagedProjectId);
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
                CreateQuery(fixture));

        Assert.True(
            result.IsFailure);

        Assert.Equal(
            UserErrors.Inactive,
            result.Error);

        Assert.Null(
            fixture.ProjectMemberRepository.LastPagedProjectId);
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
                CreateQuery(fixture));

        Assert.True(
            result.IsFailure);

        Assert.Equal(
            ProjectErrors.NotFound,
            result.Error);

        Assert.Null(
            fixture.ProjectMemberRepository.LastPagedProjectId);
    }

    [Fact]
    public async Task
    HandleAsync_ShouldThrow_WhenQueryIsNull()
    {
        var fixture =
            CreateFixture(
                UserRole.TenantAdmin);

        await Assert.ThrowsAsync<ArgumentNullException>(
            () => fixture.Handler.HandleAsync(
                null!));

        Assert.Null(
            fixture.ProjectMemberRepository.LastPagedProjectId);
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

        var query =
            new ListProjectMembersQuery(
                publicIdToClear == 1
                    ? Guid.Empty
                    : fixture.Tenant.PublicId,
                publicIdToClear == 2
                    ? Guid.Empty
                    : fixture.Project.PublicId,
                publicIdToClear == 3
                    ? Guid.Empty
                    : fixture.Requester.PublicId);

        await Assert.ThrowsAsync<ArgumentException>(
            () => fixture.Handler.HandleAsync(
                query));

        Assert.Null(
            fixture.ProjectMemberRepository.LastPagedProjectId);
    }

    [Fact]
    public async Task
    HandleAsync_ShouldThrow_WhenPageNumberIsInvalid()
    {
        var fixture =
            CreateFixture(
                UserRole.TenantAdmin);

        var query =
            new ListProjectMembersQuery(
                fixture.Tenant.PublicId,
                fixture.Project.PublicId,
                fixture.Requester.PublicId,
                0,
                20);

        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(
            () => fixture.Handler.HandleAsync(
                query));

        Assert.Null(
            fixture.ProjectMemberRepository.LastPagedProjectId);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(101)]
    public async Task
    HandleAsync_ShouldThrow_WhenPageSizeIsInvalid(
        int pageSize)
    {
        var fixture =
            CreateFixture(
                UserRole.TenantAdmin);

        var query =
            new ListProjectMembersQuery(
                fixture.Tenant.PublicId,
                fixture.Project.PublicId,
                fixture.Requester.PublicId,
                1,
                pageSize);

        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(
            () => fixture.Handler.HandleAsync(
                query));

        Assert.Null(
            fixture.ProjectMemberRepository.LastPagedProjectId);
    }

    private static Fixture CreateFixture(
        UserRole requesterRole)
    {
        var tenant =
            new Tenant(
                "Empresa List Project Members",
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

        var handler =
            new ListProjectMembersHandler(
                tenantRepository,
                userRepository,
                projectRepository,
                projectMemberRepository);

        return new Fixture(
            tenant,
            requester,
            project,
            tenantRepository,
            userRepository,
            projectRepository,
            projectMemberRepository,
            handler);
    }

    private static ListProjectMembersQuery CreateQuery(
        Fixture fixture)
    {
        return new ListProjectMembersQuery(
            fixture.Tenant.PublicId,
            fixture.Project.PublicId,
            fixture.Requester.PublicId);
    }

    private sealed record Fixture(
        Tenant Tenant,
        User Requester,
        Project Project,
        FakeTenantRepository TenantRepository,
        FakeUserRepository UserRepository,
        FakeProjectRepository ProjectRepository,
        FakeProjectMemberRepository ProjectMemberRepository,
        ListProjectMembersHandler Handler);
}