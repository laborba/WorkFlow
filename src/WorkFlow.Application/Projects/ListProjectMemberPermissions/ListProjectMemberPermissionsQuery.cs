namespace WorkFlow.Application.Projects.ListProjectMemberPermissions;

public sealed record ListProjectMemberPermissionsQuery(
    Guid TenantPublicId,
    Guid ProjectPublicId,
    Guid RequestedByUserPublicId,
    Guid UserPublicId);