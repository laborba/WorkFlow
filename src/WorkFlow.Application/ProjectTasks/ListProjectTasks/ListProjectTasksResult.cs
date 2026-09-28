namespace WorkFlow.Application.ProjectTasks.ListProjectTasks;

public sealed record ListProjectTasksResult(
    IReadOnlyCollection<ProjectTaskListItemResult> Items,
    int PageNumber,
    int PageSize,
    int TotalCount,
    int TotalPages);