using WorkFlow.Domain.Enums;

namespace WorkFlow.API.Contracts.ProjectTasks;

public sealed record RemoveProjectTaskResponsibleResponse(
    Guid PublicId,
    Guid TenantPublicId,
    Guid ProjectPublicId,
    Guid? ResponsibleUserPublicId,
    ProjectTaskStatus Status,
    DateTime? UpdatedAt);