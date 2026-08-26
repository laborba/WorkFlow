namespace WorkFlow.Application.Tenants.ChangeTenantStatus;

public sealed record ChangeTenantStatusResult(
    Guid PublicId,
    bool IsActive,
    DateTime? UpdatedAt);