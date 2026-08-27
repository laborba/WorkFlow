namespace WorkFlow.API.Contracts.Tenants;

public sealed record ListTenantsResponse(
    IReadOnlyCollection<TenantListItemResponse> Items,
    int PageNumber,
    int PageSize,
    int TotalCount,
    int TotalPages);