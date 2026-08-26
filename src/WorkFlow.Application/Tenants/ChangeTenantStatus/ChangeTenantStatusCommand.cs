namespace WorkFlow.Application.Tenants.ChangeTenantStatus;

public sealed record ChangeTenantStatusCommand(
    Guid PublicId,
    bool IsActive);