using WorkFlow.Application.Abstractions.Persistence.Models;
using WorkFlow.Application.Common.Pagination;
using WorkFlow.Application.Projects.ListProjects;
using WorkFlow.Application.Tenants;
using WorkFlow.Application.Users;
using WorkFlow.Domain.Entities;
using WorkFlow.Domain.Enums;
using WorkFlow.UnitTests.Application.Projects.Fakes;
using WorkFlow.UnitTests.Application.Tenants.Fakes;
using WorkFlow.UnitTests.Application.Users.Fakes;
using WorkFlow.UnitTests.Common;

namespace WorkFlow.UnitTests.Application.Projects.ListProjects;

public sealed class ListProjectsHandlerTests
{
    [Fact]
    public async Task
    HandleAsync_ShouldReturnPagedProjects_WhenTenantAdminRequestsList()
    {
        var tenant =
            CreatePersistedTenant();

        var user =
            CreatePersistedUser(
                tenant.Id,
                UserRole.TenantAdmin);

        var responsiblePublicId =
            Guid.NewGuid();

        var projectPublicId =
            Guid.NewGuid();

        var createdAt =
            DateTime.UtcNow.AddDays(-10);

        var updatedAt =
            DateTime.UtcNow.AddDays(-2);

        var dueDate =
            DateTime.UtcNow.AddDays(20);

        var tenantRepository =
            new FakeTenantRepository
            {
                TenantToReturn = tenant
            };

        var userRepository =
            new FakeUserRepository
            {
                UserToReturn = user
            };

        var projectRepository =
            new FakeProjectRepository
            {
                PagedDataToReturn =
                    new PagedData<ProjectListItemData>(
                        new[]
                        {
                            new ProjectListItemData(
                                projectPublicId,
                                true,
                                responsiblePublicId,
                                "Responsável",
                                "Projeto API",
                                "Descrição do projeto",
                                ProjectStatus.InProgress,
                                dueDate,
                                createdAt,
                                updatedAt,
                                null)
                        },
                        21)
            };

        var handler =
            new ListProjectsHandler(
                tenantRepository,
                userRepository,
                projectRepository);

        var query =
            new ListProjectsQuery(
                tenant.PublicId,
                user.PublicId,
                2,
                10,
                "api",
                ProjectStatus.InProgress,
                responsiblePublicId);

        var result =
            await handler.HandleAsync(query);

        Assert.True(result.IsSuccess);
        Assert.False(result.IsFailure);
        Assert.Null(result.Error);

        var response =
            Assert.IsType<ListProjectsResult>(
                result.Value);

        Assert.Equal(
            2,
            response.PageNumber);

        Assert.Equal(
            10,
            response.PageSize);

        Assert.Equal(
            21,
            response.TotalCount);

        Assert.Equal(
            3,
            response.TotalPages);

        var item =
            Assert.Single(response.Items);

        Assert.Equal(
            projectPublicId,
            item.PublicId);

        Assert.Equal(
            "Projeto API",
            item.Name);

        Assert.Equal(
            "Descrição do projeto",
            item.Description);

        Assert.Equal(
            ProjectStatus.InProgress,
            item.Status);

        Assert.Equal(
            responsiblePublicId,
            item.ResponsibleUserPublicId);

        Assert.Equal(
            "Responsável",
            item.ResponsibleUserName);

        Assert.Equal(
            dueDate,
            item.DueDate);

        Assert.Equal(
            createdAt,
            item.CreatedAt);

        Assert.Equal(
            updatedAt,
            item.UpdatedAt);

        Assert.Null(
            item.ArchivedAt);

        Assert.Equal(
            tenant.Id,
            projectRepository.LastTenantId);

        Assert.Null(
            projectRepository.LastActiveMemberUserId);

        Assert.Equal(
            2,
            projectRepository.LastPageNumber);

        Assert.Equal(
            10,
            projectRepository.LastPageSize);

        Assert.Equal(
            "api",
            projectRepository.LastSearch);

        Assert.Equal(
            ProjectStatus.InProgress,
            projectRepository.LastStatus);

        Assert.Equal(
            responsiblePublicId,
            projectRepository.LastResponsibleUserPublicId);
    }

    [Theory]
    [InlineData(UserRole.ProjectManager)]
    [InlineData(UserRole.Member)]
    public async Task
    HandleAsync_ShouldRestrictByActiveMembership_WhenRoleRequiresMembership(
        UserRole role)
    {
        var tenant =
            CreatePersistedTenant();

        var user =
            CreatePersistedUser(
                tenant.Id,
                role);

        var tenantRepository =
            new FakeTenantRepository
            {
                TenantToReturn = tenant
            };

        var userRepository =
            new FakeUserRepository
            {
                UserToReturn = user
            };

        var projectRepository =
            new FakeProjectRepository();

        var handler =
            new ListProjectsHandler(
                tenantRepository,
                userRepository,
                projectRepository);

        var query =
            new ListProjectsQuery(
                tenant.PublicId,
                user.PublicId);

        var result =
            await handler.HandleAsync(query);

        Assert.True(
            result.IsSuccess);

        Assert.Equal(
            user.Id,
            projectRepository.LastActiveMemberUserId);
    }

    [Fact]
    public async Task
    HandleAsync_ShouldReturnFailure_WhenTenantDoesNotExist()
    {
        var tenantRepository =
            new FakeTenantRepository();

        var userRepository =
            new FakeUserRepository();

        var projectRepository =
            new FakeProjectRepository();

        var handler =
            new ListProjectsHandler(
                tenantRepository,
                userRepository,
                projectRepository);

        var query =
            new ListProjectsQuery(
                Guid.NewGuid(),
                Guid.NewGuid());

        var result =
            await handler.HandleAsync(query);

        Assert.True(
            result.IsFailure);

        Assert.Equal(
            TenantErrors.NotFound,
            result.Error);

        Assert.Null(
            projectRepository.LastTenantId);
    }

    [Fact]
    public async Task
    HandleAsync_ShouldReturnFailure_WhenTenantIsInactive()
    {
        var tenant =
            CreatePersistedTenant();

        tenant.Deactivate();

        var tenantRepository =
            new FakeTenantRepository
            {
                TenantToReturn = tenant
            };

        var userRepository =
            new FakeUserRepository();

        var projectRepository =
            new FakeProjectRepository();

        var handler =
            new ListProjectsHandler(
                tenantRepository,
                userRepository,
                projectRepository);

        var query =
            new ListProjectsQuery(
                tenant.PublicId,
                Guid.NewGuid());

        var result =
            await handler.HandleAsync(query);

        Assert.True(
            result.IsFailure);

        Assert.Equal(
            TenantErrors.Inactive,
            result.Error);

        Assert.Null(
            projectRepository.LastTenantId);
    }

    [Fact]
    public async Task
    HandleAsync_ShouldReturnFailure_WhenRequestedUserDoesNotExist()
    {
        var tenant =
            CreatePersistedTenant();

        var tenantRepository =
            new FakeTenantRepository
            {
                TenantToReturn = tenant
            };

        var userRepository =
            new FakeUserRepository();

        var projectRepository =
            new FakeProjectRepository();

        var handler =
            new ListProjectsHandler(
                tenantRepository,
                userRepository,
                projectRepository);

        var query =
            new ListProjectsQuery(
                tenant.PublicId,
                Guid.NewGuid());

        var result =
            await handler.HandleAsync(query);

        Assert.True(
            result.IsFailure);

        Assert.Equal(
            UserErrors.NotFound,
            result.Error);

        Assert.Null(
            projectRepository.LastTenantId);
    }

    [Fact]
    public async Task
    HandleAsync_ShouldReturnFailure_WhenRequestedUserIsInactive()
    {
        var tenant =
            CreatePersistedTenant();

        var user =
            CreatePersistedUser(
                tenant.Id,
                UserRole.Member);

        user.Deactivate();

        var tenantRepository =
            new FakeTenantRepository
            {
                TenantToReturn = tenant
            };

        var userRepository =
            new FakeUserRepository
            {
                UserToReturn = user
            };

        var projectRepository =
            new FakeProjectRepository();

        var handler =
            new ListProjectsHandler(
                tenantRepository,
                userRepository,
                projectRepository);

        var query =
            new ListProjectsQuery(
                tenant.PublicId,
                user.PublicId);

        var result =
            await handler.HandleAsync(query);

        Assert.True(
            result.IsFailure);

        Assert.Equal(
            UserErrors.Inactive,
            result.Error);

        Assert.Null(
            projectRepository.LastTenantId);
    }

    [Fact]
    public async Task
    HandleAsync_ShouldThrow_WhenQueryIsNull()
    {
        var handler =
            new ListProjectsHandler(
                new FakeTenantRepository(),
                new FakeUserRepository(),
                new FakeProjectRepository());

        await Assert.ThrowsAsync<ArgumentNullException>(
            () => handler.HandleAsync(null!));
    }

    [Fact]
    public async Task
    HandleAsync_ShouldThrow_WhenTenantPublicIdIsEmpty()
    {
        var handler =
            new ListProjectsHandler(
                new FakeTenantRepository(),
                new FakeUserRepository(),
                new FakeProjectRepository());

        var query =
            new ListProjectsQuery(
                Guid.Empty,
                Guid.NewGuid());

        var exception =
            await Assert.ThrowsAsync<ArgumentException>(
                () => handler.HandleAsync(query));

        Assert.Equal(
            "TenantPublicId",
            exception.ParamName);
    }

    [Fact]
    public async Task
    HandleAsync_ShouldThrow_WhenRequestedByUserPublicIdIsEmpty()
    {
        var handler =
            new ListProjectsHandler(
                new FakeTenantRepository(),
                new FakeUserRepository(),
                new FakeProjectRepository());

        var query =
            new ListProjectsQuery(
                Guid.NewGuid(),
                Guid.Empty);

        var exception =
            await Assert.ThrowsAsync<ArgumentException>(
                () => handler.HandleAsync(query));

        Assert.Equal(
            "RequestedByUserPublicId",
            exception.ParamName);
    }

    [Fact]
    public async Task
    HandleAsync_ShouldThrow_WhenPageNumberIsLessThanOne()
    {
        var handler =
            new ListProjectsHandler(
                new FakeTenantRepository(),
                new FakeUserRepository(),
                new FakeProjectRepository());

        var query =
            new ListProjectsQuery(
                Guid.NewGuid(),
                Guid.NewGuid(),
                0);

        var exception =
            await Assert.ThrowsAsync<ArgumentOutOfRangeException>(
                () => handler.HandleAsync(query));

        Assert.Equal(
            "PageNumber",
            exception.ParamName);
    }

    [Fact]
    public async Task
    HandleAsync_ShouldThrow_WhenPageSizeIsLessThanOne()
    {
        var handler =
            new ListProjectsHandler(
                new FakeTenantRepository(),
                new FakeUserRepository(),
                new FakeProjectRepository());

        var query =
            new ListProjectsQuery(
                Guid.NewGuid(),
                Guid.NewGuid(),
                1,
                0);

        var exception =
            await Assert.ThrowsAsync<ArgumentOutOfRangeException>(
                () => handler.HandleAsync(query));

        Assert.Equal(
            "PageSize",
            exception.ParamName);
    }

    [Fact]
    public async Task
    HandleAsync_ShouldThrow_WhenPageSizeIsGreaterThanMaximum()
    {
        var handler =
            new ListProjectsHandler(
                new FakeTenantRepository(),
                new FakeUserRepository(),
                new FakeProjectRepository());

        var query =
            new ListProjectsQuery(
                Guid.NewGuid(),
                Guid.NewGuid(),
                1,
                101);

        var exception =
            await Assert.ThrowsAsync<ArgumentOutOfRangeException>(
                () => handler.HandleAsync(query));

        Assert.Equal(
            "PageSize",
            exception.ParamName);
    }

    [Fact]
    public async Task
    HandleAsync_ShouldThrow_WhenStatusIsInvalid()
    {
        var handler =
            new ListProjectsHandler(
                new FakeTenantRepository(),
                new FakeUserRepository(),
                new FakeProjectRepository());

        var query =
            new ListProjectsQuery(
                Guid.NewGuid(),
                Guid.NewGuid(),
                Status: (ProjectStatus)999);

        var exception =
            await Assert.ThrowsAsync<ArgumentException>(
                () => handler.HandleAsync(query));

        Assert.Equal(
            "Status",
            exception.ParamName);
    }

    [Fact]
    public async Task
    HandleAsync_ShouldThrow_WhenResponsibleUserPublicIdIsEmpty()
    {
        var handler =
            new ListProjectsHandler(
                new FakeTenantRepository(),
                new FakeUserRepository(),
                new FakeProjectRepository());

        var query =
            new ListProjectsQuery(
                Guid.NewGuid(),
                Guid.NewGuid(),
                ResponsibleUserPublicId: Guid.Empty);

        var exception =
            await Assert.ThrowsAsync<ArgumentException>(
                () => handler.HandleAsync(query));

        Assert.Equal(
            "ResponsibleUserPublicId",
            exception.ParamName);
    }

    [Fact]
    public async Task
    HandleAsync_ShouldThrow_WhenResponsibleUserRelationIsInconsistent()
    {
        var tenant =
            CreatePersistedTenant();

        var user =
            CreatePersistedUser(
                tenant.Id,
                UserRole.TenantAdmin);

        var tenantRepository =
            new FakeTenantRepository
            {
                TenantToReturn = tenant
            };

        var userRepository =
            new FakeUserRepository
            {
                UserToReturn = user
            };

        var projectRepository =
            new FakeProjectRepository
            {
                PagedDataToReturn =
                    new PagedData<ProjectListItemData>(
                        new[]
                        {
                            new ProjectListItemData(
                                Guid.NewGuid(),
                                true,
                                null,
                                null,
                                "Projeto inconsistente",
                                null,
                                ProjectStatus.Planning,
                                null,
                                DateTime.UtcNow,
                                null,
                                null)
                        },
                        1)
            };

        var handler =
            new ListProjectsHandler(
                tenantRepository,
                userRepository,
                projectRepository);

        var query =
            new ListProjectsQuery(
                tenant.PublicId,
                user.PublicId);

        var exception =
            await Assert.ThrowsAsync<InvalidOperationException>(
                () => handler.HandleAsync(query));

        Assert.Equal(
            "O usuário responsável associado ao projeto não foi encontrado.",
            exception.Message);
    }

    [Fact]
    public async Task
    HandleAsync_ShouldReturnEmptyPage_WhenNoProjectsAreVisible()
    {
        var tenant =
            CreatePersistedTenant();

        var user =
            CreatePersistedUser(
                tenant.Id,
                UserRole.Member);

        var tenantRepository =
            new FakeTenantRepository
            {
                TenantToReturn = tenant
            };

        var userRepository =
            new FakeUserRepository
            {
                UserToReturn = user
            };

        var projectRepository =
            new FakeProjectRepository();

        var handler =
            new ListProjectsHandler(
                tenantRepository,
                userRepository,
                projectRepository);

        var query =
            new ListProjectsQuery(
                tenant.PublicId,
                user.PublicId);

        var result =
            await handler.HandleAsync(query);

        Assert.True(
            result.IsSuccess);

        var response =
            Assert.IsType<ListProjectsResult>(
                result.Value);

        Assert.Empty(
            response.Items);

        Assert.Equal(
            0,
            response.TotalCount);

        Assert.Equal(
            0,
            response.TotalPages);

        Assert.Equal(
            user.Id,
            projectRepository.LastActiveMemberUserId);
    }

    private static Tenant CreatePersistedTenant()
    {
        var tenant =
            new Tenant(
                "Empresa de Teste",
                $"REG-{Guid.NewGuid():N}",
                $"tenant-{Guid.NewGuid():N}@test.local");

        EntityTestHelper.SetId(
            tenant,
            42);

        return tenant;
    }

    private static User CreatePersistedUser(
        long tenantId,
        UserRole role)
    {
        var user =
            new User(
                tenantId,
                "Usuário de Teste",
                $"usuario-{Guid.NewGuid():N}@test.local",
                "password-hash",
                role);

        EntityTestHelper.SetId(
            user,
            84);

        return user;
    }
}