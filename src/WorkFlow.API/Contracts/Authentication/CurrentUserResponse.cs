using WorkFlow.Domain.Enums;

namespace WorkFlow.API.Contracts.Authentication;

public sealed record CurrentUserResponse(
    Guid UserPublicId,
    Guid TenantPublicId,
    string Name,
    string Email,
    UserRole Role);