namespace WorkFlow.Application.Projects.StartProject;

public sealed record StartProjectCommand(
    Guid TenantPublicId,
    Guid ProjectPublicId,
    Guid RequestedByUserPublicId);