namespace WorkFlow.Application.Tenants.UpdateTenant;

public sealed record UpdateTenantResult(
    Guid PublicId,
    string Name,
    string Email,
    string? Phone,
    DateTime? UpdatedAt);