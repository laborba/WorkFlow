namespace WorkFlow.API.Contracts.Projects;

public sealed record ListProjectMembersResponse(
    IReadOnlyCollection<ProjectMemberListItemResponse> Items,
    int PageNumber,
    int PageSize,
    int TotalCount,
    int TotalPages);