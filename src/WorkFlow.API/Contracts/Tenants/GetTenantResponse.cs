namespace WorkFlow.API.Contracts.Tenants;

public sealed record GetTenantResponse(
    Guid PublicId,
    string Name,
    string RegistrationNumber,
    string Email,
    string? Phone,
    bool IsActive,
    DateTime CreatedAt,
    DateTime? UpdatedAt);