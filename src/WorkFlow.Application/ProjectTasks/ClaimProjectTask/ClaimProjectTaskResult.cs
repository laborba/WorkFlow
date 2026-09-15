using WorkFlow.Domain.Enums;

namespace WorkFlow.Application.ProjectTasks.ClaimProjectTask;

public sealed record ClaimProjectTaskResult(
    Guid PublicId,
    Guid TenantPublicId,
    Guid ProjectPublicId,
    Guid ResponsibleUserPublicId,
    ProjectTaskStatus Status,
    DateTime? UpdatedAt);