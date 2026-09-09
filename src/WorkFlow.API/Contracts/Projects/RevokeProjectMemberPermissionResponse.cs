using WorkFlow.Domain.Enums;

namespace WorkFlow.API.Contracts.Projects;

public sealed record RevokeProjectMemberPermissionResponse(
    Guid ProjectPublicId,
    Guid UserPublicId,
    ProjectPermission Permission,
    Guid RevokedByUserPublicId,
    DateTime RevokedAt);