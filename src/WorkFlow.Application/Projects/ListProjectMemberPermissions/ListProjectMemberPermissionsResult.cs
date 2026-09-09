namespace WorkFlow.Application.Projects.ListProjectMemberPermissions;

public sealed record ListProjectMemberPermissionsResult(
    Guid ProjectPublicId,
    Guid UserPublicId,
    IReadOnlyCollection<ProjectMemberPermissionListItemResult> Permissions);