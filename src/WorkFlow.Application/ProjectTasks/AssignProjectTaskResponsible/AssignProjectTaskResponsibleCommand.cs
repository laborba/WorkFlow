namespace WorkFlow.Application.ProjectTasks.AssignProjectTaskResponsible;

public sealed record AssignProjectTaskResponsibleCommand(
    Guid TenantPublicId,
    Guid ProjectPublicId,
    Guid TaskPublicId,
    Guid RequestedByUserPublicId,
    Guid ResponsibleUserPublicId);