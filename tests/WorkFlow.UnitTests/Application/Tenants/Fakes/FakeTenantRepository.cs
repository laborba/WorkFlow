using WorkFlow.Application.Abstractions.Persistence;
using WorkFlow.Domain.Entities;

namespace WorkFlow.UnitTests.Application.Tenants.Fakes;

internal sealed class FakeTenantRepository : ITenantRepository
{
    public bool RegistrationNumberExists { get; set; }

    public string? CheckedRegistrationNumber { get; private set; }

    public Tenant? AddedTenant { get; private set; }
    
    public Guid? CheckedPublicId { get; private set; }

    public Tenant? TenantToReturn { get; set; }


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

    public Task AddAsync(
        Tenant tenant,
        CancellationToken cancellationToken = default)
    {
        AddedTenant = tenant;

        return Task.CompletedTask;
    }
}