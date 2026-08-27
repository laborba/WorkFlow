using Microsoft.EntityFrameworkCore;
using WorkFlow.Application.Abstractions.Persistence;
using WorkFlow.Domain.Entities;
using WorkFlow.Application.Common.Pagination;

namespace WorkFlow.Infrastructure.Persistence.Repositories;

public sealed class TenantRepository : ITenantRepository
{
    private readonly WorkFlowDbContext _context;

    public TenantRepository(
        WorkFlowDbContext context)
    {
        _context = context;
    }

    public Task<bool> ExistsByRegistrationNumberAsync(
        string registrationNumber,
        CancellationToken cancellationToken = default)
    {
        return _context.Tenants.AnyAsync(
            tenant =>
                tenant.RegistrationNumber ==
                registrationNumber,
            cancellationToken);
    }

    public Task<Tenant?> GetByPublicIdAsync(
        Guid publicId,
        CancellationToken cancellationToken = default)
    {
        return _context.Tenants
            .AsNoTracking()
            .SingleOrDefaultAsync(
                tenant =>
                    tenant.PublicId == publicId,
                cancellationToken);
    }

    public Task<Tenant?> GetForUpdateByPublicIdAsync(
        Guid publicId,
        CancellationToken cancellationToken = default)
    {
        return _context.Tenants
            .SingleOrDefaultAsync(
                tenant =>
                    tenant.PublicId == publicId,
                cancellationToken);
    }

    public async Task<PagedData<Tenant>> GetPagedAsync(
        int pageNumber,
        int pageSize,
        bool? isActive,
        string? search,
        CancellationToken cancellationToken = default)
    {
        var query =
            _context.Tenants
                .AsNoTracking()
                .AsQueryable();

        if (isActive.HasValue)
        {
            query = query.Where(
                tenant =>
                    tenant.IsActive == isActive.Value);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var searchPattern =
                $"%{search.Trim()}%";

            query = query.Where(
                tenant =>
                    EF.Functions.ILike(
                        tenant.Name,
                        searchPattern) ||
                    EF.Functions.ILike(
                        tenant.RegistrationNumber,
                        searchPattern) ||
                    EF.Functions.ILike(
                        tenant.Email,
                        searchPattern));
        }

        var totalCount =
            await query.CountAsync(
                cancellationToken);

        var items =
            await query
                .OrderBy(tenant => tenant.Name)
                .ThenBy(tenant => tenant.Id)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(cancellationToken);

        return new PagedData<Tenant>(
            items,
            totalCount);
    }

    public async Task AddAsync(
        Tenant tenant,
        CancellationToken cancellationToken = default)
    {
        await _context.Tenants.AddAsync(
            tenant,
            cancellationToken);
    }
}