using WorkFlow.Domain.Enums;

namespace WorkFlow.Application.Authentication.Login;

public sealed record LoginResult(
    string AccessToken,
    DateTime ExpiresAt,
    Guid UserPublicId,
    Guid TenantPublicId,
    string Name,
    string Email,
    UserRole Role);