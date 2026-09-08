namespace WorkFlow.Application.Projects.ListProjects;

public sealed record ListProjectsResult(
    IReadOnlyCollection<ProjectListItemResult> Items,
    int PageNumber,
    int PageSize,
    int TotalCount,
    int TotalPages);