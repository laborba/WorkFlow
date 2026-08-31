using WorkFlow.Domain.Enums;

namespace WorkFlow.Application.Users.ChangeUserRole;

public sealed record ChangeUserRoleResult(
    Guid PublicId,
    Guid TenantPublicId,
    UserRole Role,
    DateTime? UpdatedAt);