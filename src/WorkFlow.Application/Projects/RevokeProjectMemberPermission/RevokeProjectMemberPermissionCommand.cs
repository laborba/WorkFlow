using WorkFlow.Domain.Enums;

namespace WorkFlow.Application.Projects.RevokeProjectMemberPermission;

public sealed record RevokeProjectMemberPermissionCommand(
    Guid TenantPublicId,
    Guid ProjectPublicId,
    Guid RequestedByUserPublicId,
    Guid UserPublicId,
    ProjectPermission Permission);