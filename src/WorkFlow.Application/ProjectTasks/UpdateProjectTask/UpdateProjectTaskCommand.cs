using WorkFlow.Domain.Enums;

namespace WorkFlow.Application.ProjectTasks.UpdateProjectTask;

public sealed record UpdateProjectTaskCommand(
    Guid TenantPublicId,
    Guid ProjectPublicId,
    Guid TaskPublicId,
    Guid RequestedByUserPublicId,
    string Title,
    string? Description,
    ProjectTaskPriority Priority,
    DateTime? DueDate);