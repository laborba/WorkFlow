namespace WorkFlow.API.Contracts.Tenants;

public sealed record CreateTenantRequest(
    string Name,
    string RegistrationNumber,
    string Email,
    string? Phone = null);