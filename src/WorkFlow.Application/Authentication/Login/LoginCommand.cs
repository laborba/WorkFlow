namespace WorkFlow.Application.Authentication.Login;

public sealed record LoginCommand(
    Guid TenantPublicId,
    string Email,
    string Password);