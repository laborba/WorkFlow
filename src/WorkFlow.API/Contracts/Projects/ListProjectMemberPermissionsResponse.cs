namespace WorkFlow.API.Contracts.Projects;

public sealed record ListProjectMemberPermissionsResponse(
    Guid ProjectPublicId,
    Guid UserPublicId,
    IReadOnlyCollection<ProjectMemberPermissionListItemResponse> Permissions);