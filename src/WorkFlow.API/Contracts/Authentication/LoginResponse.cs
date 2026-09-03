using WorkFlow.Domain.Enums;

namespace WorkFlow.API.Contracts.Authentication;

public sealed record LoginResponse(
    string AccessToken,
    DateTime ExpiresAt,
    Guid UserPublicId,
    Guid? TenantPublicId,
    string Name,
    string Email,
    UserRole Role);