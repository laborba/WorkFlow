namespace WorkFlow.API.Contracts.Tenants;

public sealed record ChangeTenantStatusResponse(
    Guid PublicId,
    bool IsActive);