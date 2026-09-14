using WorkFlow.Domain.Enums;

namespace WorkFlow.Application.ProjectTasks.CreateProjectTask;

public sealed record CreateProjectTaskCommand(
    Guid TenantPublicId,
    Guid ProjectPublicId,
    Guid CreatedByUserPublicId,
    string Title,
    string? Description,
    ProjectTaskPriority Priority,
    DateTime? DueDate);