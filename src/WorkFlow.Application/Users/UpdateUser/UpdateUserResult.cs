using WorkFlow.Domain.Enums;

namespace WorkFlow.Application.Users.UpdateUser;

public sealed record UpdateUserResult(
    Guid PublicId,
    Guid TenantPublicId,
    string Name,
    string Email,
    UserRole Role,
    bool IsActive,
    DateTime CreatedAt,
    DateTime? UpdatedAt);