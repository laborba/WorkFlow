using WorkFlow.Domain.Enums;

namespace WorkFlow.API.Contracts.ProjectTasks;

public sealed record ResumeProjectTaskResponse(
    Guid PublicId,
    Guid TenantPublicId,
    Guid ProjectPublicId,
    Guid ResponsibleUserPublicId,
    ProjectTaskStatus Status,
    ProjectTaskStatus? StatusBeforePause,
    DateTime? DueDate,
    DateTime? UpdatedAt);