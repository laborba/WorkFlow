namespace WorkFlow.API.Contracts.Tenants;

public sealed record UpdateTenantRequest(
    string Name,
    string Email,
    string? Phone);