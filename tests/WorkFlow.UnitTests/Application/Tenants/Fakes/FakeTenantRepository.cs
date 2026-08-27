using WorkFlow.Application.Abstractions.Persistence;
using WorkFlow.Domain.Entities;
using WorkFlow.Application.Common.Pagination;

namespace WorkFlow.UnitTests.Application.Tenants.Fakes;

internal sealed class FakeTenantRepository : ITenantRepository
{
    public bool RegistrationNumberExists { get; set; }

    public string? CheckedRegistrationNumber { get; private set; }

    public Tenant? AddedTenant { get; private set; }
    
    public Guid? CheckedPublicId { get; private set; }

    public Tenant? TenantToReturn { get; set; }

    public Guid? CheckedPublicIdForUpdate { get; private set; }

    public IReadOnlyCollection<Tenant> PagedTenantsToReturn { get; set; } =
        Array.Empty<Tenant>();

    public int TotalCountToReturn { get; set; }

    public int? CheckedPageNumber { get; private set; }

    public int? CheckedPageSize { get; private set; }

    public bool? CheckedIsActive { get; private set; }

    public string? CheckedSearch { get; private set; }

    public Task<bool> ExistsByRegistrationNumberAsync(
        string registrationNumber,
        CancellationToken cancellationToken = default)
    {
        CheckedRegistrationNumber =
            registrationNumber;

        return Task.FromResult(
            RegistrationNumberExists);
    }

    public Task<Tenant?> GetByPublicIdAsync(
        Guid publicId,
        CancellationToken cancellationToken = default)
    {
        CheckedPublicId = publicId;

        return Task.FromResult(
            TenantToReturn);
    }

    public Task<Tenant?> GetForUpdateByPublicIdAsync(
        Guid publicId,
        CancellationToken cancellationToken = default)
    {
        CheckedPublicIdForUpdate =
            publicId;

        return Task.FromResult(
            TenantToReturn);
    }

    public Task<PagedData<Tenant>> GetPagedAsync(
        int pageNumber,
        int pageSize,
        bool? isActive,
        string? search,
        CancellationToken cancellationToken = default)
    {
        CheckedPageNumber =
            pageNumber;

        CheckedPageSize =
            pageSize;

        CheckedIsActive =
            isActive;

        CheckedSearch =
            search;

        var result =
            new PagedData<Tenant>(
                PagedTenantsToReturn,
                TotalCountToReturn);

        return Task.FromResult(
            result);
    }

    public Task AddAsync(
        Tenant tenant,
        CancellationToken cancellationToken = default)
    {
        AddedTenant = tenant;

        return Task.CompletedTask;
    }
}