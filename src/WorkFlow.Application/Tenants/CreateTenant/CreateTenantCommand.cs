namespace WorkFlow.Application.Tenants.CreateTenant;

public sealed record CreateTenantCommand(
    string Name,
    string RegistrationNumber,
    string Email,
    string? Phone = null);