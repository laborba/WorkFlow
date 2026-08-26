using Microsoft.EntityFrameworkCore;
using WorkFlow.Application.Abstractions.Persistence;
using WorkFlow.Domain.Entities;

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

    public async Task AddAsync(
        Tenant tenant,
        CancellationToken cancellationToken = default)
    {
        await _context.Tenants.AddAsync(
            tenant,
            cancellationToken);
    }
}