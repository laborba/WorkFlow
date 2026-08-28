using WorkFlow.Application.Users.ListUsers;
using WorkFlow.Domain.Enums;
using WorkFlow.UnitTests.Application.Tenants.Fakes;
using WorkFlow.UnitTests.Application.Users.Fakes;
using WorkFlow.Application.Tenants;
using WorkFlow.Domain.Entities;
using WorkFlow.UnitTests.Common;

namespace WorkFlow.UnitTests.Application.Users.ListUsers;

public class ListUsersHandlerTests
{
    [Fact]
    public async Task
    HandleAsync_ShouldThrow_WhenTenantPublicIdIsEmpty()
    {
        var tenantRepository =
            new FakeTenantRepository();

        var userRepository =
            new FakeUserRepository();

        var handler =
            new ListUsersHandler(
                tenantRepository,
                userRepository);

        var query =
            new ListUsersQuery(
                TenantPublicId: Guid.Empty);

        var exception =
            await Assert.ThrowsAsync<ArgumentException>(
                () => handler.HandleAsync(query));

        Assert.Equal(
            "TenantPublicId",
            exception.ParamName);

        Assert.Null(
            tenantRepository.CheckedPublicId);

        Assert.Null(
            userRepository.CheckedPageNumber);
    }

    [Fact]
    public async Task
    HandleAsync_ShouldThrow_WhenPageNumberIsInvalid()
    {
        var tenantRepository =
            new FakeTenantRepository();

        var userRepository =
            new FakeUserRepository();

        var handler =
            new ListUsersHandler(
                tenantRepository,
                userRepository);

        var query =
            new ListUsersQuery(
                TenantPublicId: Guid.NewGuid(),
                PageNumber: 0);

        var exception =
            await Assert.ThrowsAsync<ArgumentOutOfRangeException>(
                () => handler.HandleAsync(query));

        Assert.Equal(
            "PageNumber",
            exception.ParamName);

        Assert.Null(
            tenantRepository.CheckedPublicId);

        Assert.Null(
            userRepository.CheckedPageNumber);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(101)]
    public async Task
    HandleAsync_ShouldThrow_WhenPageSizeIsInvalid(
        int pageSize)
    {
        var tenantRepository =
            new FakeTenantRepository();

        var userRepository =
            new FakeUserRepository();

        var handler =
            new ListUsersHandler(
                tenantRepository,
                userRepository);

        var query =
            new ListUsersQuery(
                TenantPublicId: Guid.NewGuid(),
                PageSize: pageSize);

        var exception =
            await Assert.ThrowsAsync<ArgumentOutOfRangeException>(
                () => handler.HandleAsync(query));

        Assert.Equal(
            "PageSize",
            exception.ParamName);

        Assert.Null(
            tenantRepository.CheckedPublicId);

        Assert.Null(
            userRepository.CheckedPageNumber);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(99)]
    public async Task
    HandleAsync_ShouldThrow_WhenRoleIsInvalid(
        int roleValue)
    {
        var tenantRepository =
            new FakeTenantRepository();

        var userRepository =
            new FakeUserRepository();

        var handler =
            new ListUsersHandler(
                tenantRepository,
                userRepository);

        var query =
            new ListUsersQuery(
                TenantPublicId: Guid.NewGuid(),
                Role: (UserRole)roleValue);

        var exception =
            await Assert.ThrowsAsync<ArgumentException>(
                () => handler.HandleAsync(query));

        Assert.Equal(
            "Role",
            exception.ParamName);

        Assert.Null(
            tenantRepository.CheckedPublicId);

        Assert.Null(
            userRepository.CheckedPageNumber);
    }

    [Fact]
    public async Task
HandleAsync_ShouldReturnFailure_WhenTenantDoesNotExist()
    {
        var tenantPublicId =
            Guid.NewGuid();

        var tenantRepository =
            new FakeTenantRepository();

        var userRepository =
            new FakeUserRepository();

        var handler =
            new ListUsersHandler(
                tenantRepository,
                userRepository);

        var query =
            new ListUsersQuery(
                TenantPublicId: tenantPublicId);

        var result =
            await handler.HandleAsync(query);

        Assert.False(result.IsSuccess);
        Assert.True(result.IsFailure);
        Assert.Null(result.Value);

        Assert.Equal(
            TenantErrors.NotFound,
            result.Error);

        Assert.Equal(
            tenantPublicId,
            tenantRepository.CheckedPublicId);

        Assert.Null(
            userRepository.CheckedPageNumber);
    }

    [Fact]
    public async Task
    HandleAsync_ShouldReturnPagedUsers_WhenQueryIsValid()
    {
        var tenant = new Tenant(
            "Empresa Consultada",
            "REG-LIST-USERS-001",
            "empresa-lista@test.local");

        EntityTestHelper.SetId(
            tenant,
            42);

        var firstUser = new User(
            tenant.Id,
            "Lucas",
            "lucas@test.local",
            "password-hash-for-test",
            UserRole.Member);

        var secondUser = new User(
            tenant.Id,
            "Maria",
            "maria@test.local",
            "password-hash-for-test",
            UserRole.Member);

        secondUser.Deactivate();

        var tenantRepository =
            new FakeTenantRepository
            {
                TenantToReturn = tenant
            };

        var userRepository =
            new FakeUserRepository
            {
                UsersToReturn =
                    new[] { firstUser, secondUser },

                TotalCountToReturn = 5
            };

        var handler =
            new ListUsersHandler(
                tenantRepository,
                userRepository);

        var query =
            new ListUsersQuery(
                TenantPublicId: tenant.PublicId,
                PageNumber: 2,
                PageSize: 2,
                Role: UserRole.Member,
                IsActive: false,
                Search: "teste");

        var result =
            await handler.HandleAsync(query);

        Assert.True(result.IsSuccess);
        Assert.False(result.IsFailure);
        Assert.Null(result.Error);

        var response =
            Assert.IsType<ListUsersResult>(
                result.Value);

        Assert.Equal(
            2,
            response.Items.Count);

        Assert.Equal(
            2,
            response.PageNumber);

        Assert.Equal(
            2,
            response.PageSize);

        Assert.Equal(
            5,
            response.TotalCount);

        Assert.Equal(
            3,
            response.TotalPages);

        Assert.Equal(
            tenant.Id,
            userRepository.CheckedPagedTenantId);

        Assert.Equal(
            2,
            userRepository.CheckedPageNumber);

        Assert.Equal(
            2,
            userRepository.CheckedPageSize);

        Assert.Equal(
            UserRole.Member,
            userRepository.CheckedRole);

        Assert.False(
            userRepository.CheckedIsActive);

        Assert.Equal(
            "teste",
            userRepository.CheckedSearch);
    }

    [Fact]
    public async Task
HandleAsync_ShouldReturnEmptyPage_WhenNoUserIsFound()
    {
        var tenant = new Tenant(
            "Empresa Consultada",
            "REG-LIST-USERS-002",
            "empresa-vazia@test.local");

        EntityTestHelper.SetId(
            tenant,
            42);

        var tenantRepository =
            new FakeTenantRepository
            {
                TenantToReturn = tenant
            };

        var userRepository =
            new FakeUserRepository();

        var handler =
            new ListUsersHandler(
                tenantRepository,
                userRepository);

        var query =
            new ListUsersQuery(
                TenantPublicId: tenant.PublicId);

        var result =
            await handler.HandleAsync(query);

        Assert.True(result.IsSuccess);
        Assert.False(result.IsFailure);
        Assert.Null(result.Error);

        var response =
            Assert.IsType<ListUsersResult>(
                result.Value);

        Assert.Empty(
            response.Items);

        Assert.Equal(
            1,
            response.PageNumber);

        Assert.Equal(
            20,
            response.PageSize);

        Assert.Equal(
            0,
            response.TotalCount);

        Assert.Equal(
            0,
            response.TotalPages);

        Assert.Equal(
            tenant.Id,
            userRepository.CheckedPagedTenantId);

        Assert.Equal(
            1,
            userRepository.CheckedPageNumber);

        Assert.Equal(
            20,
            userRepository.CheckedPageSize);
    }
}