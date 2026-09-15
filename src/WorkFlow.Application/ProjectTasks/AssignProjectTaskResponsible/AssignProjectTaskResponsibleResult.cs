using WorkFlow.Domain.Enums;

namespace WorkFlow.Application.ProjectTasks.AssignProjectTaskResponsible;

public sealed record AssignProjectTaskResponsibleResult(
    Guid PublicId,
    Guid TenantPublicId,
    Guid ProjectPublicId,
    Guid ResponsibleUserPublicId,
    ProjectTaskStatus Status,
    DateTime? UpdatedAt);