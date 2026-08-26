namespace WorkFlow.Application.Tenants.GetTenantByPublicId;

public sealed record GetTenantByPublicIdResult(
    Guid PublicId,
    string Name,
    string RegistrationNumber,
    string Email,
    string? Phone,
    bool IsActive,
    DateTime CreatedAt,
    DateTime? UpdatedAt);