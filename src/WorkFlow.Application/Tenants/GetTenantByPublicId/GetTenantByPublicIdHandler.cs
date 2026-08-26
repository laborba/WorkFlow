using WorkFlow.Application.Abstractions.Persistence;
using WorkFlow.Application.Common.Results;

namespace WorkFlow.Application.Tenants.GetTenantByPublicId;

public sealed class GetTenantByPublicIdHandler
{
    private readonly ITenantRepository _tenantRepository;

    public GetTenantByPublicIdHandler(
        ITenantRepository tenantRepository)
    {
        _tenantRepository = tenantRepository;
    }

    public async Task<Result<GetTenantByPublicIdResult>> HandleAsync(
        GetTenantByPublicIdQuery query,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        if (query.PublicId == Guid.Empty)
        {
            throw new ArgumentException(
                "O identificador público da empresa não pode estar vazio.",
                nameof(query.PublicId));
        }

        var tenant =
            await _tenantRepository.GetByPublicIdAsync(
                query.PublicId,
                cancellationToken);

        if (tenant is null)
        {
            return Result<GetTenantByPublicIdResult>.Failure(
                TenantErrors.NotFound);
        }

        return Result<GetTenantByPublicIdResult>.Success(
            new GetTenantByPublicIdResult(
                tenant.PublicId,
                tenant.Name,
                tenant.RegistrationNumber,
                tenant.Email,
                tenant.Phone,
                tenant.IsActive,
                tenant.CreatedAt,
                tenant.UpdatedAt));
    }
}