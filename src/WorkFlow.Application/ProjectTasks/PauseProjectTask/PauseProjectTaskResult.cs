using WorkFlow.Domain.Enums;

namespace WorkFlow.Application.ProjectTasks.PauseProjectTask;

public sealed record PauseProjectTaskResult(
    Guid PublicId,
    Guid TenantPublicId,
    Guid ProjectPublicId,
    Guid ResponsibleUserPublicId,
    ProjectTaskStatus Status,
    ProjectTaskStatus? StatusBeforePause,
    DateTime? DueDate,
    DateTime? UpdatedAt);