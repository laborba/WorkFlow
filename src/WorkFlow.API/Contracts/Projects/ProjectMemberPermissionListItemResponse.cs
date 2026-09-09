using WorkFlow.Domain.Enums;

namespace WorkFlow.API.Contracts.Projects;

public sealed record ProjectMemberPermissionListItemResponse(
    ProjectPermission Permission,
    Guid GrantedByUserPublicId,
    DateTime GrantedAt);