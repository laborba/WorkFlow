using WorkFlow.Domain.Enums;

namespace WorkFlow.Application.ProjectTasks.ListProjectTasks;

public sealed record ListProjectTasksQuery(
    Guid TenantPublicId,
    Guid ProjectPublicId,
    Guid RequestedByUserPublicId,
    int PageNumber = 1,
    int PageSize = 20,
    string? Search = null,
    ProjectTaskStatus? Status = null,
    ProjectTaskPriority? Priority = null,
    Guid? ResponsibleUserPublicId = null,
    bool? IsArchived = null);