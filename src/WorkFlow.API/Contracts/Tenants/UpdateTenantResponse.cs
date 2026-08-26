namespace WorkFlow.API.Contracts.Tenants;

public sealed record UpdateTenantResponse(
    Guid PublicId,
    string Name,
    string Email,
    string? Phone,
    DateTime? UpdatedAt);