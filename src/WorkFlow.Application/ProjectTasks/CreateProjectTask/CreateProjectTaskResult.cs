using WorkFlow.Domain.Enums;

namespace WorkFlow.Application.ProjectTasks.CreateProjectTask;

public sealed record CreateProjectTaskResult(
    Guid PublicId,
    Guid TenantPublicId,
    Guid ProjectPublicId,
    Guid CreatedByUserPublicId,
    Guid? ResponsibleUserPublicId,
    string Title,
    string? Description,
    ProjectTaskStatus Status,
    ProjectTaskPriority Priority,
    DateTime? DueDate,
    DateTime CreatedAt);