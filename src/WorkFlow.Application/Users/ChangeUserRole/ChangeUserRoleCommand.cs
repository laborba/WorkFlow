using WorkFlow.Domain.Enums;

namespace WorkFlow.Application.Users.ChangeUserRole;

public sealed record ChangeUserRoleCommand(
    Guid TenantPublicId,
    Guid UserPublicId,
    UserRole Role);