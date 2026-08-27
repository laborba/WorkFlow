using WorkFlow.Application.Abstractions.Persistence;
using WorkFlow.Application.Common.Results;

namespace WorkFlow.Application.Tenants.ListTenants;

public sealed class ListTenantsHandler
{
    private const int MaximumPageSize = 100;

    private readonly ITenantRepository _tenantRepository;

    public ListTenantsHandler(
        ITenantRepository tenantRepository)
    {
        _tenantRepository = tenantRepository;
    }

    public async Task<Result<ListTenantsResult>> HandleAsync(
        ListTenantsQuery query,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        if (query.PageNumber < 1)
        {
            throw new ArgumentOutOfRangeException(
                nameof(query.PageNumber),
                query.PageNumber,
                "O número da página deve ser maior ou igual a 1.");
        }

        if (query.PageSize < 1 ||
            query.PageSize > MaximumPageSize)
        {
            throw new ArgumentOutOfRangeException(
                nameof(query.PageSize),
                query.PageSize,
                $"O tamanho da página deve estar entre 1 e {MaximumPageSize}.");
        }

        var pagedData =
            await _tenantRepository.GetPagedAsync(
                query.PageNumber,
                query.PageSize,
                query.IsActive,
                query.Search,
                cancellationToken);

        var items =
            pagedData.Items
                .Select(tenant =>
                    new TenantListItemResult(
                        tenant.PublicId,
                        tenant.Name,
                        tenant.RegistrationNumber,
                        tenant.Email,
                        tenant.Phone,
                        tenant.IsActive,
                        tenant.CreatedAt,
                        tenant.UpdatedAt))
                .ToArray();

        var totalPages =
            (int)Math.Ceiling(
                pagedData.TotalCount /
                (double)query.PageSize);

        var response =
            new ListTenantsResult(
                items,
                query.PageNumber,
                query.PageSize,
                pagedData.TotalCount,
                totalPages);

        return Result<ListTenantsResult>.Success(
            response);
    }
}