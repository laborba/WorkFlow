using WorkFlow.Domain.Enums;

namespace WorkFlow.Application.Users.GetUserByPublicId;

public sealed record GetUserByPublicIdResult(
    Guid PublicId,
    Guid TenantPublicId,
    string Name,
    string Email,
    UserRole Role,
    bool IsActive,
    DateTime CreatedAt,
    DateTime? UpdatedAt);