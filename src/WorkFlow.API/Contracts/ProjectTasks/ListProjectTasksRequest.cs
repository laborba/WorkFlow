using WorkFlow.Domain.Enums;

namespace WorkFlow.API.Contracts.ProjectTasks;

public sealed record ListProjectTasksRequest(
    int PageNumber = 1,
    int PageSize = 20,
    string? Search = null,
    ProjectTaskStatus? Status = null,
    ProjectTaskPriority? Priority = null,
    Guid? ResponsibleUserPublicId = null,
    bool? IsArchived = null);