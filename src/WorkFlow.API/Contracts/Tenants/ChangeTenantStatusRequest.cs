namespace WorkFlow.API.Contracts.Tenants;

public sealed record ChangeTenantStatusRequest(
    bool IsActive);