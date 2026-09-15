using WorkFlow.Domain.Enums;

namespace WorkFlow.Application.ProjectTasks.RemoveProjectTaskResponsible;

public sealed record RemoveProjectTaskResponsibleResult(
    Guid PublicId,
    Guid TenantPublicId,
    Guid ProjectPublicId,
    Guid? ResponsibleUserPublicId,
    ProjectTaskStatus Status,
    DateTime? UpdatedAt);