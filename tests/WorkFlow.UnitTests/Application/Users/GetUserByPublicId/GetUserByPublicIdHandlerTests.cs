using WorkFlow.Application.Tenants;
using WorkFlow.Application.Users;
using WorkFlow.Application.Users.GetUserByPublicId;
using WorkFlow.Domain.Entities;
using WorkFlow.Domain.Enums;
using WorkFlow.UnitTests.Application.Tenants.Fakes;
using WorkFlow.UnitTests.Application.Users.Fakes;
using WorkFlow.UnitTests.Common;


namespace WorkFlow.UnitTests.Application.Users.GetUserByPublicId;

public class GetUserByPublicIdHandlerTests
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
            new GetUserByPublicIdHandler(
                tenantRepository,
                userRepository);

        var query =
            new GetUserByPublicIdQuery(
                Guid.Empty,
                Guid.NewGuid());

        var exception =
            await Assert.ThrowsAsync<ArgumentException>(
                () => handler.HandleAsync(query));

        Assert.Equal(
            "TenantPublicId",
            exception.ParamName);

        Assert.Null(
            tenantRepository.CheckedPublicId);

        Assert.Null(
            userRepository.CheckedPublicId);
    }

    [Fact]
    public async Task
    HandleAsync_ShouldThrow_WhenUserPublicIdIsEmpty()
    {
        var tenantRepository =
            new FakeTenantRepository();

        var userRepository =
            new FakeUserRepository();

        var handler =
            new GetUserByPublicIdHandler(
                tenantRepository,
                userRepository);

        var query =
            new GetUserByPublicIdQuery(
                Guid.NewGuid(),
                Guid.Empty);

        var exception =
            await Assert.ThrowsAsync<ArgumentException>(
                () => handler.HandleAsync(query));

        Assert.Equal(
            "UserPublicId",
            exception.ParamName);

        Assert.Null(
            tenantRepository.CheckedPublicId);

        Assert.Null(
            userRepository.CheckedPublicId);
    }

    [Fact]
    public async Task
    HandleAsync_ShouldReturnFailure_WhenTenantDoesNotExist()
    {
        var tenantPublicId =
            Guid.NewGuid();

        var userPublicId =
            Guid.NewGuid();

        var tenantRepository =
            new FakeTenantRepository();

        var userRepository =
            new FakeUserRepository();

        var handler =
            new GetUserByPublicIdHandler(
                tenantRepository,
                userRepository);

        var query =
            new GetUserByPublicIdQuery(
                tenantPublicId,
                userPublicId);

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
            userRepository.CheckedPublicId);
    }

    [Fact]
    public async Task
    HandleAsync_ShouldReturnFailure_WhenUserDoesNotExist()
    {
        var tenant = new Tenant(
            "Empresa Consultada",
            "REG-USER-QUERY-001",
            "empresa@test.local");

        EntityTestHelper.SetId(
            tenant,
            42);

        var userPublicId =
            Guid.NewGuid();

        var tenantRepository =
            new FakeTenantRepository
            {
                TenantToReturn = tenant
            };

        var userRepository =
            new FakeUserRepository();

        var handler =
            new GetUserByPublicIdHandler(
                tenantRepository,
                userRepository);

        var query =
            new GetUserByPublicIdQuery(
                tenant.PublicId,
                userPublicId);

        var result =
            await handler.HandleAsync(query);

        Assert.False(result.IsSuccess);
        Assert.True(result.IsFailure);
        Assert.Null(result.Value);

        Assert.Equal(
            UserErrors.NotFound,
            result.Error);

        Assert.Equal(
            tenant.PublicId,
            tenantRepository.CheckedPublicId);

        Assert.Equal(
            tenant.Id,
            userRepository.CheckedGetByPublicIdTenantId);

        Assert.Equal(
            userPublicId,
            userRepository.CheckedPublicId);
    }

    [Fact]
    public async Task
    HandleAsync_ShouldReturnUser_WhenUserExistsInTenant()
    {
        var tenant = new Tenant(
            "Empresa Consultada",
            "REG-USER-QUERY-002",
            "empresa-usuario@test.local");

        EntityTestHelper.SetId(
            tenant,
            42);

        var user = new User(
            tenant.Id,
            "Usuário Consultado",
            "usuario-consultado@test.local",
            "password-hash-for-test",
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

        var handler =
            new GetUserByPublicIdHandler(
                tenantRepository,
                userRepository);

        var query =
            new GetUserByPublicIdQuery(
                tenant.PublicId,
                user.PublicId);

        var result =
            await handler.HandleAsync(query);

        Assert.True(result.IsSuccess);
        Assert.False(result.IsFailure);
        Assert.Null(result.Error);

        var response =
            Assert.IsType<GetUserByPublicIdResult>(
                result.Value);

        Assert.Equal(
            tenant.PublicId,
            tenantRepository.CheckedPublicId);

        Assert.Equal(
            tenant.Id,
            userRepository.CheckedGetByPublicIdTenantId);

        Assert.Equal(
            user.PublicId,
            userRepository.CheckedPublicId);

        Assert.Equal(
            user.PublicId,
            response.PublicId);

        Assert.Equal(
            tenant.PublicId,
            response.TenantPublicId);

        Assert.Equal(
            user.Name,
            response.Name);

        Assert.Equal(
            user.Email,
            response.Email);

        Assert.Equal(
            user.Role,
            response.Role);

        Assert.Equal(
            user.IsActive,
            response.IsActive);

        Assert.Equal(
            user.CreatedAt,
            response.CreatedAt);

        Assert.Equal(
            user.UpdatedAt,
            response.UpdatedAt);
    }
}