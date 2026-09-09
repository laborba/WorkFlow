using WorkFlow.Domain.Enums;

namespace WorkFlow.API.Contracts.Projects;

public sealed record GrantProjectMemberPermissionResponse(
    Guid ProjectPublicId,
    Guid UserPublicId,
    ProjectPermission Permission,
    Guid GrantedByUserPublicId,
    DateTime GrantedAt);