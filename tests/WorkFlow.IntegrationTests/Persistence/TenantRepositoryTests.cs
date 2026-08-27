using Microsoft.EntityFrameworkCore;
using WorkFlow.Domain.Entities;
using WorkFlow.Infrastructure.Persistence.Repositories;
using WorkFlow.IntegrationTests.Infrastructure;
using Xunit;

namespace WorkFlow.IntegrationTests.Persistence;

[Collection(TestCollectionNames.PostgreSql)]
public sealed class TenantRepositoryTests
{
    private readonly PostgreSqlTestDatabase _database;

    public TenantRepositoryTests(
        PostgreSqlTestDatabase database)
    {
        _database = database;
    }

    [Fact]
    public async Task
    GetPagedAsync_ShouldReturnRequestedPageInNameOrder()
    {
        await using var context =
            _database.CreateDbContext();

        await using var transaction =
            await context.Database.BeginTransactionAsync();

        var uniqueValue =
            Guid.NewGuid().ToString("N");

        var marker =
            $"Paged-{uniqueValue}";

        var firstTenant = new Tenant(
            $"{marker} A",
            $"REG-A-{uniqueValue}",
            $"a-{uniqueValue}@test.local");

        var secondTenant = new Tenant(
            $"{marker} B",
            $"REG-B-{uniqueValue}",
            $"b-{uniqueValue}@test.local");

        var thirdTenant = new Tenant(
            $"{marker} C",
            $"REG-C-{uniqueValue}",
            $"c-{uniqueValue}@test.local");

        context.Tenants.AddRange(
            thirdTenant,
            firstTenant,
            secondTenant);

        await context.SaveChangesAsync();

        var repository =
            new TenantRepository(context);

        var result =
            await repository.GetPagedAsync(
                pageNumber: 2,
                pageSize: 1,
                isActive: null,
                search: marker.ToLowerInvariant());

        Assert.Equal(
            3,
            result.TotalCount);

        var item =
            Assert.Single(result.Items);

        Assert.Equal(
            secondTenant.PublicId,
            item.PublicId);

        await transaction.RollbackAsync();
    }

    [Fact]
    public async Task
    GetPagedAsync_ShouldFilterTenantsByStatus()
    {
        await using var context =
            _database.CreateDbContext();

        await using var transaction =
            await context.Database.BeginTransactionAsync();

        var uniqueValue =
            Guid.NewGuid().ToString("N");

        var marker =
            $"Status-{uniqueValue}";

        var activeTenant = new Tenant(
            $"{marker} Ativa",
            $"REG-ACTIVE-{uniqueValue}",
            $"active-{uniqueValue}@test.local");

        var inactiveTenant = new Tenant(
            $"{marker} Inativa",
            $"REG-INACTIVE-{uniqueValue}",
            $"inactive-{uniqueValue}@test.local");

        inactiveTenant.Deactivate();

        context.Tenants.AddRange(
            activeTenant,
            inactiveTenant);

        await context.SaveChangesAsync();

        var repository =
            new TenantRepository(context);

        var activeResult =
            await repository.GetPagedAsync(
                pageNumber: 1,
                pageSize: 20,
                isActive: true,
                search: marker);

        var inactiveResult =
            await repository.GetPagedAsync(
                pageNumber: 1,
                pageSize: 20,
                isActive: false,
                search: marker);

        var returnedActiveTenant =
            Assert.Single(activeResult.Items);

        Assert.Equal(
            activeTenant.PublicId,
            returnedActiveTenant.PublicId);

        Assert.Equal(
            1,
            activeResult.TotalCount);

        var returnedInactiveTenant =
            Assert.Single(inactiveResult.Items);

        Assert.Equal(
            inactiveTenant.PublicId,
            returnedInactiveTenant.PublicId);

        Assert.Equal(
            1,
            inactiveResult.TotalCount);

        await transaction.RollbackAsync();
    }

    [Fact]
    public async Task
    GetPagedAsync_ShouldSearchByRegistrationNumberAndEmail()
    {
        await using var context =
            _database.CreateDbContext();

        await using var transaction =
            await context.Database.BeginTransactionAsync();

        var uniqueValue =
            Guid.NewGuid().ToString("N");

        var registrationTenant = new Tenant(
            "Empresa encontrada pelo registro",
            $"REG-SEARCH-{uniqueValue}",
            $"registration-{uniqueValue}@test.local");

        var emailTenant = new Tenant(
            "Empresa encontrada pelo e-mail",
            $"REG-EMAIL-{uniqueValue}",
            $"email-search-{uniqueValue}@test.local");

        context.Tenants.AddRange(
            registrationTenant,
            emailTenant);

        await context.SaveChangesAsync();

        var repository =
            new TenantRepository(context);

        var registrationResult =
            await repository.GetPagedAsync(
                pageNumber: 1,
                pageSize: 20,
                isActive: null,
                search:
                    $"reg-search-{uniqueValue}");

        var emailResult =
            await repository.GetPagedAsync(
                pageNumber: 1,
                pageSize: 20,
                isActive: null,
                search:
                    $"EMAIL-SEARCH-{uniqueValue}");

        var returnedRegistrationTenant =
            Assert.Single(
                registrationResult.Items);

        Assert.Equal(
            registrationTenant.PublicId,
            returnedRegistrationTenant.PublicId);

        var returnedEmailTenant =
            Assert.Single(
                emailResult.Items);

        Assert.Equal(
            emailTenant.PublicId,
            returnedEmailTenant.PublicId);

        await transaction.RollbackAsync();
    }
}