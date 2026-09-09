using WorkFlow.Domain.Enums;

namespace WorkFlow.Application.Projects.ListProjectMemberPermissions;

public sealed record ProjectMemberPermissionListItemResult(
    ProjectPermission Permission,
    Guid GrantedByUserPublicId,
    DateTime GrantedAt);