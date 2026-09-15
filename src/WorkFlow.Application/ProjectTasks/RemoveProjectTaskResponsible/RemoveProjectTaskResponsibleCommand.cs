namespace WorkFlow.Application.ProjectTasks.RemoveProjectTaskResponsible;

public sealed record RemoveProjectTaskResponsibleCommand(
    Guid TenantPublicId,
    Guid ProjectPublicId,
    Guid TaskPublicId,
    Guid RequestedByUserPublicId);