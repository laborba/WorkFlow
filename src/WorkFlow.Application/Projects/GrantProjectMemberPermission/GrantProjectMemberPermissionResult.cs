using WorkFlow.Domain.Enums;

namespace WorkFlow.Application.Projects.GrantProjectMemberPermission;

public sealed record GrantProjectMemberPermissionResult(
    Guid ProjectPublicId,
    Guid UserPublicId,
    ProjectPermission Permission,
    Guid GrantedByUserPublicId,
    DateTime GrantedAt);