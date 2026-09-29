using WorkFlow.Domain.Enums;

namespace WorkFlow.Application.ProjectTasks.StartProjectTask;

public sealed record StartProjectTaskResult(
    Guid PublicId,
    Guid TenantPublicId,
    Guid ProjectPublicId,
    Guid ResponsibleUserPublicId,
    ProjectTaskStatus Status,
    DateTime? UpdatedAt);