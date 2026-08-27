namespace WorkFlow.Application.Tenants.ListTenants;

public sealed record ListTenantsResult(
    IReadOnlyCollection<TenantListItemResult> Items,
    int PageNumber,
    int PageSize,
    int TotalCount,
    int TotalPages);