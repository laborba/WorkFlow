using WorkFlow.Domain.Enums;

namespace WorkFlow.Application.Projects.RevokeProjectMemberPermission;

public sealed record RevokeProjectMemberPermissionResult(
    Guid ProjectPublicId,
    Guid UserPublicId,
    ProjectPermission Permission,
    Guid RevokedByUserPublicId,
    DateTime RevokedAt);