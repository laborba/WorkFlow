using WorkFlow.Application.Tenants.GetTenantByPublicId;
using WorkFlow.Domain.Entities;
using WorkFlow.UnitTests.Application.Tenants.Fakes;
using WorkFlow.Application.Tenants;

namespace WorkFlow.UnitTests.Application.Tenants.GetTenantByPublicId;

public class GetTenantByPublicIdHandlerTests
{
    [Fact]
    public async Task
    HandleAsync_ShouldReturnTenant_WhenTenantExists()
    {
        var tenant = new Tenant(
            "Empresa Consultada",
            "REG-QUERY-001",
            "consulta@test.local",
            "(47) 99999-9999");

        var tenantRepository =
            new FakeTenantRepository
            {
                TenantToReturn = tenant
            };

        var handler =
            new GetTenantByPublicIdHandler(
                tenantRepository);

        var query =
            new GetTenantByPublicIdQuery(
                tenant.PublicId);

        var result =
            await handler.HandleAsync(query);

        Assert.True(result.IsSuccess);
        Assert.False(result.IsFailure);
        Assert.Null(result.Error);

        var response =
            Assert.IsType<GetTenantByPublicIdResult>(
                result.Value);

        Assert.Equal(
            tenant.PublicId,
            tenantRepository.CheckedPublicId);

        Assert.Equal(
            tenant.PublicId,
            response.PublicId);

        Assert.Equal(
            tenant.Name,
            response.Name);

        Assert.Equal(
            tenant.RegistrationNumber,
            response.RegistrationNumber);

        Assert.Equal(
            tenant.Email,
            response.Email);

        Assert.Equal(
            tenant.Phone,
            response.Phone);

        Assert.Equal(
            tenant.IsActive,
            response.IsActive);

        Assert.Equal(
            tenant.CreatedAt,
            response.CreatedAt);

        Assert.Equal(
            tenant.UpdatedAt,
            response.UpdatedAt);
    }

    [Fact]
    public async Task
    HandleAsync_ShouldReturnFailure_WhenTenantDoesNotExist()
    {
        var publicId =
            Guid.NewGuid();

        var tenantRepository =
            new FakeTenantRepository();

        var handler =
            new GetTenantByPublicIdHandler(
                tenantRepository);

        var query =
            new GetTenantByPublicIdQuery(
                publicId);

        var result =
            await handler.HandleAsync(query);

        Assert.False(result.IsSuccess);
        Assert.True(result.IsFailure);
        Assert.Null(result.Value);

        Assert.Equal(
            TenantErrors.NotFound,
            result.Error);

        Assert.Equal(
            publicId,
            tenantRepository.CheckedPublicId);
    }

    [Fact]
    public async Task
    HandleAsync_ShouldThrow_WhenPublicIdIsEmpty()
    {
        var tenantRepository =
            new FakeTenantRepository();

        var handler =
            new GetTenantByPublicIdHandler(
                tenantRepository);

        var query =
            new GetTenantByPublicIdQuery(
                Guid.Empty);

        var exception =
            await Assert.ThrowsAsync<ArgumentException>(
                () => handler.HandleAsync(query));

        Assert.Equal(
            "PublicId",
            exception.ParamName);

        Assert.Null(
            tenantRepository.CheckedPublicId);
    }
}