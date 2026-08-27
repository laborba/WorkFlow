namespace WorkFlow.Application.Tenants.ListTenants;

public sealed record ListTenantsQuery(
    int PageNumber = 1,
    int PageSize = 20,
    bool? IsActive = null,
    string? Search = null);