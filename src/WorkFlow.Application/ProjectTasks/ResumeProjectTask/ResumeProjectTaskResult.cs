using WorkFlow.Domain.Enums;

namespace WorkFlow.Application.ProjectTasks.ResumeProjectTask;

public sealed record ResumeProjectTaskResult(
    Guid PublicId,
    Guid TenantPublicId,
    Guid ProjectPublicId,
    Guid ResponsibleUserPublicId,
    ProjectTaskStatus Status,
    ProjectTaskStatus? StatusBeforePause,
    DateTime? DueDate,
    DateTime? UpdatedAt);