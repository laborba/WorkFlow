namespace WorkFlow.API.Contracts.Projects;

public sealed record ListProjectsResponse(
    IReadOnlyCollection<ProjectListItemResponse> Items,
    int PageNumber,
    int PageSize,
    int TotalCount,
    int TotalPages);