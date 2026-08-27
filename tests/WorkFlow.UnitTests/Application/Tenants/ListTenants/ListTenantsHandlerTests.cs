using WorkFlow.Application.Tenants.ListTenants;
using WorkFlow.Domain.Entities;
using WorkFlow.UnitTests.Application.Tenants.Fakes;

namespace WorkFlow.UnitTests.Application.Tenants.ListTenants;

public class ListTenantsHandlerTests
{
    [Fact]
    public async Task
    HandleAsync_ShouldReturnPagedTenants_WhenQueryIsValid()
    {
        var tenant = new Tenant(
            "Empresa Inativa",
            "REG-LIST-001",
            "inativa@test.local",
            "(47) 99999-0000");

        tenant.Deactivate();

        var tenantRepository =
            new FakeTenantRepository
            {
                PagedTenantsToReturn =
                    new[] { tenant },

                TotalCountToReturn = 5
            };

        var handler =
            new ListTenantsHandler(
                tenantRepository);

        var query =
            new ListTenantsQuery(
                PageNumber: 2,
                PageSize: 2,
                IsActive: false,
                Search: "Empresa");

        var result =
            await handler.HandleAsync(query);

        Assert.True(result.IsSuccess);
        Assert.False(result.IsFailure);
        Assert.Null(result.Error);

        var response =
            Assert.IsType<ListTenantsResult>(
                result.Value);

        var item =
            Assert.Single(response.Items);

        Assert.Equal(
            tenant.PublicId,
            item.PublicId);

        Assert.Equal(
            tenant.Name,
            item.Name);

        Assert.Equal(
            tenant.RegistrationNumber,
            item.RegistrationNumber);

        Assert.Equal(
            tenant.Email,
            item.Email);

        Assert.Equal(
            tenant.Phone,
            item.Phone);

        Assert.Equal(
            tenant.IsActive,
            item.IsActive);

        Assert.Equal(
            tenant.CreatedAt,
            item.CreatedAt);

        Assert.Equal(
            tenant.UpdatedAt,
            item.UpdatedAt);

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
            2,
            tenantRepository.CheckedPageNumber);

        Assert.Equal(
            2,
            tenantRepository.CheckedPageSize);

        Assert.False(
            tenantRepository.CheckedIsActive);

        Assert.Equal(
            "Empresa",
            tenantRepository.CheckedSearch);
    }

    [Fact]
    public async Task
    HandleAsync_ShouldReturnEmptyPage_WhenNoTenantIsFound()
    {
        var tenantRepository =
            new FakeTenantRepository();

        var handler =
            new ListTenantsHandler(
                tenantRepository);

        var query =
            new ListTenantsQuery();

        var result =
            await handler.HandleAsync(query);

        Assert.True(result.IsSuccess);

        var response =
            Assert.IsType<ListTenantsResult>(
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
    }

    [Fact]
    public async Task
    HandleAsync_ShouldThrow_WhenPageNumberIsInvalid()
    {
        var tenantRepository =
            new FakeTenantRepository();

        var handler =
            new ListTenantsHandler(
                tenantRepository);

        var query =
            new ListTenantsQuery(
                PageNumber: 0);

        var exception =
            await Assert.ThrowsAsync<ArgumentOutOfRangeException>(
                () => handler.HandleAsync(query));

        Assert.Equal(
            "PageNumber",
            exception.ParamName);

        Assert.Null(
            tenantRepository.CheckedPageNumber);
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

        var handler =
            new ListTenantsHandler(
                tenantRepository);

        var query =
            new ListTenantsQuery(
                PageSize: pageSize);

        var exception =
            await Assert.ThrowsAsync<ArgumentOutOfRangeException>(
                () => handler.HandleAsync(query));

        Assert.Equal(
            "PageSize",
            exception.ParamName);

        Assert.Null(
            tenantRepository.CheckedPageNumber);
    }
}