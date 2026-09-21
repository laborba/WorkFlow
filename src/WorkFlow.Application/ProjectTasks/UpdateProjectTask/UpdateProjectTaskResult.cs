using WorkFlow.Domain.Enums;

namespace WorkFlow.Application.ProjectTasks.UpdateProjectTask;

public sealed record UpdateProjectTaskResult(
    Guid PublicId,
    Guid TenantPublicId,
    Guid ProjectPublicId,
    string Title,
    string? Description,
    ProjectTaskStatus Status,
    ProjectTaskPriority Priority,
    DateTime? DueDate,
    DateTime? UpdatedAt);