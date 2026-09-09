using WorkFlow.Domain.Enums;

namespace WorkFlow.Application.Projects.GrantProjectMemberPermission;

public sealed record GrantProjectMemberPermissionCommand(
    Guid TenantPublicId,
    Guid ProjectPublicId,
    Guid RequestedByUserPublicId,
    Guid UserPublicId,
    ProjectPermission Permission);