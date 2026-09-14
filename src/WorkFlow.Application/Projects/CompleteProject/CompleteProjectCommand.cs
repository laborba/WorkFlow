namespace WorkFlow.Application.Projects.CompleteProject;

public sealed record CompleteProjectCommand(
    Guid TenantPublicId,
    Guid ProjectPublicId,
    Guid RequestedByUserPublicId);