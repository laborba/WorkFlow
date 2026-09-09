namespace WorkFlow.Application.Projects.ListProjectMembers;

public sealed record ListProjectMembersResult(
    IReadOnlyCollection<ProjectMemberListItemResult> Items,
    int PageNumber,
    int PageSize,
    int TotalCount,
    int TotalPages);