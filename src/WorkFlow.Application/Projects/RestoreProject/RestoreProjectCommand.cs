namespace WorkFlow.Application.Projects.RestoreProject;

public sealed record RestoreProjectCommand(
    Guid TenantPublicId,
    Guid ProjectPublicId,
    Guid RequestedByUserPublicId);