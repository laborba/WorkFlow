namespace WorkFlow.Application.Tenants.ListTenants;

public sealed record TenantListItemResult(
    Guid PublicId,
    string Name,
    string RegistrationNumber,
    string Email,
    string? Phone,
    bool IsActive,
    DateTime CreatedAt,
    DateTime? UpdatedAt);