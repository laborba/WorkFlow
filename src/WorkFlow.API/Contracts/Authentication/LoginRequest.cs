namespace WorkFlow.API.Contracts.Authentication;

public sealed record LoginRequest(
    Guid? TenantPublicId,
    string Email,
    string Password);