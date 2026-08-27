namespace WorkFlow.API.Contracts.Tenants;

public sealed record TenantListItemResponse(
    Guid PublicId,
    string Name,
    string RegistrationNumber,
    string Email,
    string? Phone,
    bool IsActive,
    DateTime CreatedAt,
    DateTime? UpdatedAt);