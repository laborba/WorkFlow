namespace WorkFlow.Application.Projects.PauseProject;

public sealed record PauseProjectCommand(
    Guid TenantPublicId,
    Guid ProjectPublicId,
    Guid RequestedByUserPublicId,
    string Reason);