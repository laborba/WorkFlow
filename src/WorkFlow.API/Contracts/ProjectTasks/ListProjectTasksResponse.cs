namespace WorkFlow.API.Contracts.ProjectTasks;

public sealed record ListProjectTasksResponse(
    IReadOnlyCollection<ProjectTaskListItemResponse> Items,
    int PageNumber,
    int PageSize,
    int TotalCount,
    int TotalPages);