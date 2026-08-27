using WorkFlow.Domain.Entities;
using WorkFlow.Application.Common.Pagination;

namespace WorkFlow.Application.Abstractions.Persistence;

public interface ITenantRepository
{
    Task<bool> ExistsByRegistrationNumberAsync(
        string registrationNumber,
        CancellationToken cancellationToken = default);

    Task<Tenant?> GetByPublicIdAsync(
        Guid publicId,
        CancellationToken cancellationToken = default);

    Task AddAsync(
        Tenant tenant,
        CancellationToken cancellationToken = default);

    Task<Tenant?> GetForUpdateByPublicIdAsync(
        Guid publicId,
        CancellationToken cancellationToken = default);

    Task<PagedData<Tenant>> GetPagedAsync(
        int pageNumber,
        int pageSize,
        bool? isActive,
        string? search,
        CancellationToken cancellationToken = default);
}