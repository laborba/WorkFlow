using WorkFlow.Domain.Enums;

namespace WorkFlow.API.Contracts.ProjectTasks;

public sealed record PauseProjectTaskResponse(
    Guid PublicId,
    Guid TenantPublicId,
    Guid ProjectPublicId,
    Guid ResponsibleUserPublicId,
    ProjectTaskStatus Status,
    ProjectTaskStatus? StatusBeforePause,
    DateTime? DueDate,
    DateTime? UpdatedAt);