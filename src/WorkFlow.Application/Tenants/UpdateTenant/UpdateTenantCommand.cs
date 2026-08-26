namespace WorkFlow.Application.Tenants.UpdateTenant;

public sealed record UpdateTenantCommand(
    Guid PublicId,
    string Name,
    string Email,
    string? Phone);